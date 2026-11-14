/* ==========================================================================
 * 70-main.js — 应用主逻辑：状态、工具调度、撤销、持久化、PDF/图片、导出
 * ========================================================================== */
(function () {
  'use strict';
  const IB = (window.IB = window.IB || {});
  const M = IB.model;
  const { clamp, uid, Store, download, pickFile, fileToDataURL, loadImage, debounce } = IB.util;

  function snapObject(o) {
    if (o.type === 'stroke') {
      return { id: o.id, type: 'stroke', pts: o.pts.map((p) => ({ x: p.x, y: p.y, p: p.p, t: p.t })), w: o.w, color: o.color, alpha: o.alpha, tool: o.tool };
    }
    if (o.type === 'shape') {
      return Object.assign({}, o, { verts: o.verts ? o.verts.map((v) => ({ x: v.x, y: v.y })) : null, _geo: null });
    }
    return Object.assign({}, o);
  }

  function reviveObject(o) {
    const c = Object.assign({}, o);
    delete c._geo;
    if (o.type === 'stroke') c.pts = o.pts.map((p) => ({ x: p.x, y: p.y, p: p.p, t: p.t }));
    if (o.type === 'shape' && o.verts) c.verts = o.verts.map((v) => ({ x: v.x, y: v.y }));
    return c;
  }

  class App {
    constructor() {
      IB.app = this;
      this.style = { color: '#1f2329', width: 7, eraserSize: 22, textSize: 42 };
      this.markerColor = '#ffe066';
      this.autoShape = true;
      this.shapeKind = 'rect';
      this.shapeFill = null;
      this.shapeDash = null;
      this.funcExpr = 'sin(x)';
      this.toolName = 'pen';
      this.selection = new Set();
      this.history = new M.History(80);
      this.dirty = false;

      this.doc = M.createDoc({ name: '未命名白板' });
      this.board = new IB.Board({
        doc: this.doc,
        staticCanvas: document.getElementById('layer-page'),
        liveCanvas: document.getElementById('layer-live'),
      });
      this.input = new IB.Input(this.board, document.getElementById('layer-live'));
      this.persist = debounce(() => this.saveState(), 900);
      this.refreshThumbs = debounce(() => IB.UI.refreshPages(), 700);

      this.onSelectionChange = () => IB.UI.onSelectionChange();
      this.setTool('pen');
    }

    async boot() {
      const saved = await Store.get('doc:current');
      if (saved) {
        const d = M.deserializeDoc(saved);
        if (d) {
          this.doc = d;
          this.board.doc = d;
          this.board.fitPage();
        }
      }
      IB.UI.init(this);
      this.history.reset(this.snapshot());
      this.board.invalidateAll();
      this.updateSaveState();
      this.applyUiScale();
      window.addEventListener('resize', debounce(() => { this.applyUiScale(); this.board.resize(); this.board.fitPage(); }, 200));
      window.addEventListener('beforeunload', () => { if (this.dirty) this.saveState(); });
      document.addEventListener('visibilitychange', () => { if (document.hidden && this.dirty) this.saveState(); });
    }

    applyUiScale() {
      const u = clamp(Math.min(window.innerWidth / 1920, window.innerHeight / 1080), 0.85, 2.2);
      this.uiScale = u;
      document.documentElement.style.setProperty('--u', u.toFixed(2));
    }

    /* ------------------------------ 工具 ------------------------------ */
    setTool(name) {
      this.toolName = name;
      const Ctor = IB.tools[name];
      if (!Ctor) return;
      this.board.tool = name === 'pen' || name === 'marker' ? new Ctor(this.board, name) : new Ctor(this.board);
      this.board.previewObjects = [];
      const live = document.getElementById('layer-live');
      live.style.cursor = this.board.tool.cursor || 'default';
      if (name !== 'select') { this.selection.clear(); IB.UI.onSelectionChange(); }
      IB.UI && IB.UI.setTool && IB.UI.setTool(name);
      this.board.invalidateLive();
    }

    refreshTool() { this.setTool(this.toolName); }

    /* ------------------------------ 撤销 / 快照 ------------------------------ */
    snapshot() {
      return {
        current: this.doc.current,
        pageW: this.doc.pageW,
        pageH: this.doc.pageH,
        pages: this.doc.pages.map((p) => ({
          id: p.id, bg: p.bg, color: p.color,
          objects: p.objects.map(snapObject),
        })),
      };
    }

    restore(snap) {
      this.doc.pageW = snap.pageW;
      this.doc.pageH = snap.pageH;
      this.doc.pages = snap.pages.map((p) => ({
        id: p.id, bg: p.bg, color: p.color,
        objects: p.objects.map(reviveObject),
      }));
      this.doc.current = clamp(snap.current, 0, this.doc.pages.length - 1);
      this.board.doc = this.doc;
      this.selection.clear();
      this.board.fitPage();
      this.board.invalidateAll();
      IB.UI.refreshPages();
      IB.UI.onSelectionChange();
    }

    commit(label) {
      this.history.push(this.snapshot());
      this.dirty = true;
      this.updateSaveState();
      this.persist();
      this.refreshThumbs();
    }

    undo() {
      const s = this.history.undo();
      if (!s) { this.toast('没有可撤销的操作'); return; }
      this.restore(s);
      this.dirty = true; this.persist(); this.refreshThumbs();
      this.toast('已撤销');
    }
    redo() {
      const s = this.history.redo();
      if (!s) { this.toast('没有可重做的操作'); return; }
      this.restore(s);
      this.dirty = true; this.persist(); this.refreshThumbs();
    }

    /* ------------------------------ 页面 ------------------------------ */
    setPage(i) {
      const n = clamp(i, 0, this.doc.pages.length - 1);
      if (n === this.doc.current) return;
      this.doc.current = n;
      this.selection.clear();
      this.board.fitPage();
      this.board.invalidateAll();
      IB.UI.refreshPages();
      IB.UI.onSelectionChange();
    }

    addPage() {
      const p = M.createPage({ bg: this.board.page.bg, color: this.board.page.color });
      this.doc.pages.splice(this.doc.current + 1, 0, p);
      this.doc.current += 1;
      this.board.fitPage();
      this.board.invalidateAll();
      this.commit('新建页');
      IB.UI.refreshPages();
    }

    duplicatePage() {
      const src = this.board.page;
      const p = M.createPage({ bg: src.bg, color: src.color });
      p.objects = src.objects.map((o) => M.cloneObject(o));
      this.doc.pages.splice(this.doc.current + 1, 0, p);
      this.doc.current += 1;
      this.board.invalidateAll();
      this.commit('复制页');
      IB.UI.refreshPages();
    }

    deletePage() {
      if (this.doc.pages.length <= 1) { this.toast('至少保留一页'); return; }
      this.doc.pages.splice(this.doc.current, 1);
      this.doc.current = clamp(this.doc.current, 0, this.doc.pages.length - 1);
      this.board.invalidateAll();
      this.commit('删除页');
      IB.UI.refreshPages();
    }

    setRatio(w, h) {
      const oldW = this.doc.pageW, oldH = this.doc.pageH;
      const k = Math.min(w / oldW, h / oldH);
      this.doc.pageW = w;
      this.doc.pageH = h;
      if (Math.abs(k - 1) > 0.001) {
        // 内容按比例缩放，避免切换比例后跑出页面
        for (const p of this.doc.pages) {
          for (const o of p.objects) {
            M.scaleObject(o, { x: 0, y: 0 }, k, k);
          }
        }
      }
      this.board.fitPage();
      this.board.invalidateAll();
      this.commit('页面比例');
      IB.UI.refreshPages();
    }

    /* ------------------------------ 选择 ------------------------------ */
    selectionBounds() {
      const objs = this.board.page.objects.filter((o) => this.selection.has(o.id));
      return M.bboxOfObjects(objs);
    }

    selectAll() {
      if (this.toolName !== 'select') this.setTool('select');
      this.selection = new Set(this.board.page.objects.map((o) => o.id));
      IB.UI.onSelectionChange();
      this.board.invalidateLive();
    }

    deleteSelection() {
      if (!this.selection.size) return;
      this.board.removeObjects(Array.from(this.selection));
      this.selection.clear();
      IB.UI.onSelectionChange();
    }

    selectionAction(act) {
      const ids = this.selection;
      if (!ids.size) return;
      const objs = this.board.page.objects;
      if (act === 'del') { this.deleteSelection(); return; }
      if (act === 'dup') { this.board.duplicateSelection(); return; }
      const sel = objs.filter((o) => ids.has(o.id));
      if (act === 'front') {
        for (const o of sel) { objs.splice(objs.indexOf(o), 1); objs.push(o); }
      } else if (act === 'back') {
        for (const o of sel) { objs.splice(objs.indexOf(o), 1); objs.unshift(o); }
      }
      this.board.invalidateAll();
      this.commit('调整层级');
    }

    clearPage() {
      if (!this.board.page.objects.length) return;
      if (!confirm('清空当前页的全部内容？')) return;
      this.board.page.objects = [];
      this.selection.clear();
      this.board.invalidateAll();
      this.commit('清空页面');
      IB.UI.onSelectionChange();
    }

    clearDoc() {
      if (!confirm('清空整个白板（所有页面）？此操作可撤销。')) return;
      const p = M.createPage();
      this.doc.pages = [p];
      this.doc.current = 0;
      this.selection.clear();
      this.board.invalidateAll();
      this.commit('清空白板');
      IB.UI.refreshPages();
    }

    /* ------------------------------ 文本 / 图片 ------------------------------ */
    openTextEditor(pt, existing) {
      if (existing) {
        this.setTool('select');
        this.selection = new Set([existing.id]);
        IB.UI.onSelectionChange();
      }
      IB.UI.openTextEditor(pt, existing);
    }

    async insertImageAt(pt) {
      const file = await pickFile('image/*');
      if (!file) return;
      const src = await fileToDataURL(file);
      const img = await loadImage(src).catch(() => null);
      if (!img) { this.toast('图片读取失败'); return; }
      const maxW = this.doc.pageW * 0.6;
      const w = Math.min(maxW, img.naturalWidth);
      const h = w * (img.naturalHeight / img.naturalWidth);
      const o = M.obj.image(pt.x - w / 2, pt.y - h / 2, w, h, src);
      this.board.addObject(o, { undo: false });
      this.commit('插入图片');
      this.setTool('select');
      this.selection = new Set([o.id]);
      IB.UI.onSelectionChange();
    }

    /* ------------------------------ PDF 导入 ------------------------------ */
    async importPDF(file) {
      if (!window.pdfjsLib) { this.toast('PDF 组件未加载（需要联网加载 pdf.js）'); return; }
      this.toast('正在解析 PDF…');
      try {
        const data = await file.arrayBuffer();
        pdfjsLib.GlobalWorkerOptions.workerSrc = 'https://cdn.jsdelivr.net/npm/pdfjs-dist@3.11.174/build/pdf.worker.min.js';
        const pdf = await pdfjsLib.getDocument({ data }).promise;
        const total = Math.min(pdf.numPages, 200);
        let added = 0;
        for (let i = 1; i <= total; i++) {
          const page = await pdf.getPage(i);
          const vp = page.getViewport({ scale: 2 });
          const canvas = document.createElement('canvas');
          canvas.width = Math.floor(vp.width);
          canvas.height = Math.floor(vp.height);
          const ctx = canvas.getContext('2d');
          ctx.fillStyle = '#fff';
          ctx.fillRect(0, 0, canvas.width, canvas.height);
          await page.render({ canvasContext: ctx, viewport: vp, background: '#ffffff' }).promise;
          const src = canvas.toDataURL('image/jpeg', 0.92);

          const p = M.createPage();
          const s = Math.min(this.doc.pageW / canvas.width, this.doc.pageH / canvas.height);
          const w = canvas.width * s, h = canvas.height * s;
          p.objects.push(M.obj.image((this.doc.pageW - w) / 2, (this.doc.pageH - h) / 2, w, h, src));
          this.doc.pages.splice(this.doc.current + 1 + added, 0, p);
          added++;
          if (i % 5 === 0) this.toast(`已导入 ${i} / ${total} 页…`);
        }
        this.doc.current += 1;
        this.board.fitPage();
        this.board.invalidateAll();
        this.commit('导入 PDF');
        IB.UI.refreshPages();
        this.toast(`已导入 ${added} 页`);
        this.setTool('pen');
      } catch (e) {
        console.error(e);
        this.toast('PDF 解析失败：' + (e && e.message ? e.message : '未知错误'));
      }
    }

    /* ------------------------------ 持久化 ------------------------------ */
    saveState() {
      const json = M.serializeDoc(this.doc);
      this.doc.name = document.getElementById('doc-title').value.trim() || this.doc.name;
      Store.set('doc:current', json).then(() => {
        this.dirty = false;
        this.updateSaveState();
      });
    }

    updateSaveState() {
      const s = document.getElementById('save-state');
      if (!s) return;
      s.textContent = this.dirty ? '保存中…' : '已保存';
      s.classList.toggle('dirty', this.dirty);
    }

    saveFile() {
      const name = (this.doc.name || '白板').replace(/[\\/:*?"<>|]/g, '_');
      const json = M.serializeDoc(this.doc);
      download(new Blob([json], { type: 'application/json' }), name + '.inkboard');
      this.toast('已导出白板文件');
    }

    async openFile() {
      const file = await pickFile('.inkboard,.json,application/json');
      if (!file) return;
      try {
        const text = await file.text();
        const d = M.deserializeDoc(text);
        if (!d) throw new Error('文件格式不正确');
        this.doc = d;
        this.board.doc = d;
        this.selection.clear();
        this.board.fitPage();
        this.board.invalidateAll();
        this.history.reset(this.snapshot());
        IB.UI.refreshPages();
        this.commit('打开文件');
        this.toast('已打开：' + d.name);
      } catch (e) {
        this.toast('打开失败：' + e.message);
      }
    }

    /* ------------------------------ 导出 ------------------------------ */
    async exportAs(kind) {
      const name = (this.doc.name || '白板').replace(/[\\/:*?"<>|]/g, '_');
      if (kind === 'json') { this.saveFile(); return; }
      if (kind === 'png-page') {
        const c = await this.board.renderPage(this.board.page, 2);
        c.toBlob((b) => { download(b, `${name}-第${this.doc.current + 1}页.png`); this.toast('已导出 PNG'); }, 'image/png');
        return;
      }
      if (kind === 'png-all') {
        this.toast('正在导出全部页面…');
        for (let i = 0; i < this.doc.pages.length; i++) {
          const c = await this.board.renderPage(this.doc.pages[i], 2);
          await new Promise((res) => c.toBlob((b) => { download(b, `${name}-${i + 1}.png`); setTimeout(res, 400); }, 'image/png'));
        }
        this.toast('已导出 ' + this.doc.pages.length + ' 页');
        return;
      }
      if (kind === 'pdf') { await this.printPages(); return; }
      if (kind === 'svg') {
        const svg = this.pageToSVG(this.board.page);
        download(new Blob([svg], { type: 'image/svg+xml' }), `${name}-第${this.doc.current + 1}页.svg`);
        this.toast('已导出 SVG');
      }
    }

    async printPages() {
      this.toast('正在生成打印文件…');
      const urls = [];
      for (const p of this.doc.pages) {
        const c = await this.board.renderPage(p, 1.5);
        urls.push(c.toDataURL('image/jpeg', 0.92));
      }
      const frames = urls.map((u, i) => `<section><img src="${u}" /><div class="cap">第 ${i + 1} 页</div></section>`).join('');
      const html = `<!DOCTYPE html><html><head><meta charset="utf-8"><title>${this.doc.name}</title>
      <style>
        @page { size: A4 landscape; margin: 10mm; }
        body { margin: 0; font-family: "PingFang SC","Microsoft YaHei",sans-serif; }
        section { page-break-after: always; display: flex; flex-direction: column; align-items: center; justify-content: center; height: 190mm; }
        img { max-width: 100%; max-height: 178mm; object-fit: contain; }
        .cap { font-size: 11px; color: #666; margin-top: 6px; }
      </style></head><body>${frames}</body></html>`;
      const frame = document.createElement('iframe');
      frame.style.cssText = 'position:fixed;right:0;bottom:0;width:0;height:0;border:0;';
      document.body.appendChild(frame);
      const doc = frame.contentDocument;
      doc.open(); doc.write(html); doc.close();
      setTimeout(() => {
        frame.contentWindow.focus();
        frame.contentWindow.print();
        setTimeout(() => frame.remove(), 3000);
      }, 700);
    }

    pageToSVG(page) {
      const w = this.doc.pageW, h = this.doc.pageH;
      const bg = page.bg === 'black' ? '#12161b' : (page.color || '#ffffff');
      const out = [`<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">`];
      out.push(`<rect width="${w}" height="${h}" fill="${bg}"/>`);
      for (const o of page.objects) {
        if (o.type === 'stroke') {
          const d = o.pts.map((p, i) => `${i ? 'L' : 'M'}${p.x.toFixed(1)} ${p.y.toFixed(1)}`).join(' ');
          out.push(`<path d="${d}" fill="none" stroke="${o.color}" stroke-width="${(o.w * 0.92).toFixed(1)}" stroke-linecap="round" stroke-linejoin="round" opacity="${o.alpha == null ? 1 : o.alpha}"/>`);
        } else if (o.type === 'shape') {
          const x = Math.min(o.x1, o.x2), y = Math.min(o.y1, o.y2), ww = Math.abs(o.x2 - o.x1), hh = Math.abs(o.y2 - o.y1);
          const st = `stroke="${o.color}" stroke-width="${o.w}" fill="${o.fill || 'none'}"`;
          if (o.shape === 'line' || o.shape === 'arrow') {
            out.push(`<line x1="${o.x1}" y1="${o.y1}" x2="${o.x2}" y2="${o.y2}" ${st}/>`);
            if (o.shape === 'arrow') {
              const ang = Math.atan2(o.y2 - o.y1, o.x2 - o.x1), s = Math.max(10, o.w * 3.4);
              out.push(`<polygon points="${o.x2},${o.y2} ${o.x2 - s * Math.cos(ang - .42)},${o.y2 - s * Math.sin(ang - .42)} ${o.x2 - s * Math.cos(ang + .42)},${o.y2 - s * Math.sin(ang + .42)}" fill="${o.color}"/>`);
            }
          } else if (o.shape === 'rect') out.push(`<rect x="${x}" y="${y}" width="${ww}" height="${hh}" ${st}/>`);
          else if (o.shape === 'ellipse') out.push(`<ellipse cx="${x + ww / 2}" cy="${y + hh / 2}" rx="${ww / 2}" ry="${hh / 2}" ${st}/>`);
          else if (o.shape === 'triangle') out.push(`<polygon points="${x + ww / 2},${y} ${x + ww},${y + hh} ${x},${y + hh}" ${st}/>`);
          else if (o.shape === 'polygon' && o.verts) out.push(`<polygon points="${o.verts.map((v) => `${v.x},${v.y}`).join(' ')}" ${st}/>`);
          else if (o.shape === 'axis') {
            const ox = x + ww / 2, oy = y + hh / 2;
            out.push(`<line x1="${x}" y1="${oy}" x2="${x + ww}" y2="${oy}" ${st}/>`);
            out.push(`<line x1="${ox}" y1="${y + hh}" x2="${ox}" y2="${y}" ${st}/>`);
          } else if (o.shape === 'func') {
            const pts = [];
            for (let i = 0; i <= 200; i++) {
              const xv = -4 + (8 * i) / 200, yv = IB.geo.evalExpr(o.expr || 'x', xv);
              if (!isFinite(yv)) continue;
              pts.push(`${ox(xv)},${oy(yv)}`);
            }
            if (pts.length) out.push(`<polyline points="${pts.join(' ')}" ${st}/>`);
            function ox(v) { return x + ww / 2 + v * (ww / 8); }
            function oy(v) { return y + hh / 2 - v * (hh / 8); }
          }
        } else if (o.type === 'text') {
          const lines = String(o.text).split('\n').map((l, i) => `<tspan x="${o.x}" dy="${i ? o.size * 1.3 : 0}">${esc(l)}</tspan>`).join('');
          out.push(`<text x="${o.x}" y="${o.y + o.size}" fill="${o.color}" font-size="${o.size}" font-family="PingFang SC,Microsoft YaHei,sans-serif" font-weight="${o.bold ? 700 : 400}" font-style="${o.italic ? 'italic' : 'normal'}">${lines}</text>`);
        } else if (o.type === 'image') {
          out.push(`<image x="${o.x}" y="${o.y}" width="${o.w}" height="${o.h}" xlink:href="${o.src}"/>`);
        }
      }
      out.push('</svg>');
      return out.join('\n');
    }

    toast(msg) { IB.UI.toast(msg); }
  }

  function esc(s) { return String(s).replace(/[&<>]/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;' })[c]); }

  IB.App = App;
  window.addEventListener('DOMContentLoaded', async () => {
    const app = new App();
    await app.boot();
    window.inkboard = app;
    if ('serviceWorker' in navigator && location.protocol.indexOf('http') === 0) {
      navigator.serviceWorker.register('sw.js').catch(() => {});
    }
  });
})();
