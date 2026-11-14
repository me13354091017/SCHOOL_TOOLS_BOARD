/* ==========================================================================
 * 30-render.js — 画板引擎：双画布分层渲染、视图变换、增量重绘、对象绘制
 * ========================================================================== */
(function () {
  'use strict';
  const IB = (window.IB = window.IB || {});
  const M = IB.model;
  const { clamp, uid } = IB.util;

  const imageCache = new Map();

  function getImage(src) {
    let rec = imageCache.get(src);
    if (rec) return rec;
    rec = { img: new Image(), ready: false, src };
    rec.img.onload = () => { rec.ready = true; if (IB.app && IB.app.board) IB.app.board.invalidateAll(false); };
    rec.img.onerror = () => { rec.error = true; };
    rec.img.src = src;
    imageCache.set(src, rec);
    return rec;
  }

  class Board {
    constructor(opts) {
      this.doc = opts.doc;
      this.staticCanvas = opts.staticCanvas;
      this.liveCanvas = opts.liveCanvas;
      this.sctx = this.staticCanvas.getContext('2d', { alpha: false });
      this.lctx = this.liveCanvas.getContext('2d', { alpha: true });
      this.dpr = 1;
      this.view = { scale: 1, tx: 0, ty: 0 };
      this.staticDirty = true;
      this.liveDirty = true;
      this.tool = null;
      this.previewObjects = [];   // 正在绘制、尚未提交的对象
      this.showPerf = false;
      this.fps = 0;
      this._raf = 0;
      this._frameCount = 0;
      this._fpsTick = 0;
      this.bgColor = '#eef1f5';
      this.onLiveDraw = null;     // 工具叠加层
      this.resize();
      this.fitPage();
      this.schedule();
    }

    /* ------------------------------ 视图 ------------------------------ */
    get page() { return this.doc.pages[this.doc.current]; }
    get pageRect() { return { x: 0, y: 0, w: this.doc.pageW, h: this.doc.pageH }; }

    resize() {
      const rect = this.staticCanvas.parentElement.getBoundingClientRect();
      this.width = Math.max(1, Math.floor(rect.width));
      this.height = Math.max(1, Math.floor(rect.height));
      // 4K 大屏限制像素比，保证填充率可控
      const area = this.width * this.height;
      this.dpr = Math.min(window.devicePixelRatio || 1, area > 2600000 ? 1.25 : 2);
      for (const c of [this.staticCanvas, this.liveCanvas]) {
        c.width = Math.floor(this.width * this.dpr);
        c.height = Math.floor(this.height * this.dpr);
        c.style.width = this.width + 'px';
        c.style.height = this.height + 'px';
      }
      this.staticDirty = true;
      this.liveDirty = true;
      this.schedule();
    }

    fitPage(pad) {
      const rail = document.querySelector('.pagerail');
      const railVisible = rail && getComputedStyle(rail).display !== 'none' && !rail.classList.contains('hide');
      const u = parseFloat(getComputedStyle(document.documentElement).getPropertyValue('--u')) || 1;
      const padL = (railVisible ? rail.offsetWidth + 26 * u : 22 * u);
      const padR = 22 * u, padT = 78 * u, padB = 104 * u;
      const availW = Math.max(120, this.width - padL - padR);
      const availH = Math.max(120, this.height - padT - padB);
      const s = Math.min(availW / this.doc.pageW, availH / this.doc.pageH);
      this.view.scale = s;
      this.view.tx = padL + (availW - this.doc.pageW * s) / 2;
      this.view.ty = padT + (availH - this.doc.pageH * s) / 2;
      this.staticDirty = true;
      this.liveDirty = true;
      this.schedule();
    }

    toWorld(sx, sy) {
      return { x: (sx - this.view.tx) / this.view.scale, y: (sy - this.view.ty) / this.view.scale };
    }
    toScreen(wx, wy) {
      return { x: wx * this.view.scale + this.view.tx, y: wy * this.view.scale + this.view.ty };
    }

    panBy(dx, dy) {
      this.view.tx += dx; this.view.ty += dy;
      this.invalidateAll(false);
    }

    zoomAt(sx, sy, factor) {
      const v = this.view;
      const nx = clamp(v.scale * factor, 0.15, 6);
      const k = nx / v.scale;
      v.tx = sx - (sx - v.tx) * k;
      v.ty = sy - (sy - v.ty) * k;
      v.scale = nx;
      this.invalidateAll(false);
    }

    /* ------------------------------ 脏标记 / 渲染循环 ------------------------------ */
    invalidateAll(relayout) {
      if (relayout) this.fitPage();
      this.staticDirty = true;
      this.liveDirty = true;
      this.schedule();
    }
    invalidateLive() { this.liveDirty = true; this.schedule(); }

    schedule() {
      if (this._raf) return;
      this._raf = requestAnimationFrame((t) => {
        this._raf = 0;
        this.renderFrame(t);
      });
    }

    renderFrame(t) {
      if (this.staticDirty) { this.drawStatic(); this.staticDirty = false; }
      if (this.liveDirty) { this.drawLive(); this.liveDirty = false; }

      this._frameCount++;
      if (t - this._fpsTick > 1000) {
        this.fps = Math.round((this._frameCount * 1000) / (t - this._fpsTick));
        this._frameCount = 0; this._fpsTick = t;
      }
      if (this.showPerf) this.drawPerf();
    }

    /* ------------------------------ 静态层 ------------------------------ */
    drawStatic() {
      const ctx = this.sctx;
      const d = this.dpr;
      ctx.setTransform(d, 0, 0, d, 0, 0);
      ctx.fillStyle = this.bgColor;
      ctx.fillRect(0, 0, this.width, this.height);

      const v = this.view, pr = this.pageRect;

      // 页面投影
      ctx.save();
      ctx.shadowColor = 'rgba(15,23,42,0.14)';
      ctx.shadowBlur = 18;
      ctx.shadowOffsetY = 6;
      ctx.fillStyle = this.page.bg === 'black' ? '#12161b' : (this.page.color || '#ffffff');
      ctx.fillRect(v.tx, v.ty, pr.w * v.scale, pr.h * v.scale);
      ctx.restore();

      // 页面背景纹理
      ctx.save();
      ctx.beginPath();
      ctx.rect(v.tx, v.ty, pr.w * v.scale, pr.h * v.scale);
      ctx.clip();
      this.drawBackground(ctx);
      ctx.restore();

      // 对象（视口剔除：四边均判断，仅内容完全在视口外时才跳过）
      ctx.save();
      this.applyTransform(ctx);
      const objects = this.page.objects;
      const vr = this.viewRect();
      for (const o of objects) {
        const b = M.objBBox(o);
        if (b.x > vr.x + vr.w || b.x + b.w < vr.x || b.y > vr.y + vr.h || b.y + b.h < vr.y) continue;
        this.drawObject(ctx, o);
      }
      ctx.restore();

      // 页面边框
      ctx.setTransform(d, 0, 0, d, 0, 0);
      ctx.strokeStyle = 'rgba(15,23,42,0.10)';
      ctx.lineWidth = 1;
      ctx.strokeRect(v.tx + 0.5, v.ty + 0.5, pr.w * v.scale, pr.h * v.scale);
    }

    viewRect() {
      const a = this.toWorld(0, 0), b = this.toWorld(this.width, this.height);
      return { x: a.x, y: a.y, w: b.x - a.x, h: b.y - a.y };
    }

    applyTransform(ctx) {
      const v = this.view, d = this.dpr;
      ctx.setTransform(d * v.scale, 0, 0, d * v.scale, d * v.tx, d * v.ty);
    }

    drawBackground(ctx) {
      const p = this.page, r = this.pageRect;
      const dark = p.bg === 'black';
      ctx.fillStyle = dark ? '#12161b' : (p.color || '#ffffff');
      ctx.fillRect(r.x, r.y, r.w, r.h);

      const kind = p.bg === 'black' ? 'grid' : p.bg;
      if (kind === 'plain' || kind === 'black') return;

      const step = 60;
      const v = this.view;
      const lineW = 1 / v.scale;
      ctx.save();
      ctx.strokeStyle = dark ? 'rgba(255,255,255,0.10)' : 'rgba(15,23,42,0.08)';
      ctx.lineWidth = lineW;
      if (kind === 'grid') {
        ctx.beginPath();
        for (let x = 0; x <= r.w + 0.1; x += step) { ctx.moveTo(x, 0); ctx.lineTo(x, r.h); }
        for (let y = 0; y <= r.h + 0.1; y += step) { ctx.moveTo(0, y); ctx.lineTo(r.w, y); }
        ctx.stroke();
        // 主网格
        ctx.strokeStyle = dark ? 'rgba(255,255,255,0.20)' : 'rgba(15,23,42,0.16)';
        ctx.beginPath();
        for (let x = 0; x <= r.w + 0.1; x += step * 5) { ctx.moveTo(x, 0); ctx.lineTo(x, r.h); }
        for (let y = 0; y <= r.h + 0.1; y += step * 5) { ctx.moveTo(0, y); ctx.lineTo(r.w, y); }
        ctx.stroke();
      } else if (kind === 'dots') {
        ctx.fillStyle = dark ? 'rgba(255,255,255,0.18)' : 'rgba(15,23,42,0.18)';
        const rr = 1.6 / v.scale;
        for (let x = step; x < r.w; x += step) {
          for (let y = step; y < r.h; y += step) {
            ctx.beginPath(); ctx.arc(x, y, rr, 0, Math.PI * 2); ctx.fill();
          }
        }
      } else if (kind === 'lines') {
        ctx.beginPath();
        for (let y = step * 1.6; y < r.h; y += step * 1.6) { ctx.moveTo(0, y + 0.5); ctx.lineTo(r.w, y + 0.5); }
        ctx.stroke();
      }
      ctx.restore();
    }

    /* ------------------------------ 动态层 ------------------------------ */
    drawLive() {
      const ctx = this.lctx;
      const d = this.dpr;
      ctx.setTransform(1, 0, 0, 1, 0, 0);
      ctx.clearRect(0, 0, this.liveCanvas.width, this.liveCanvas.height);
      ctx.setTransform(d, 0, 0, d, 0, 0);

      ctx.save();
      this.applyTransform(ctx);
      for (const o of this.previewObjects) this.drawObject(ctx, o);
      ctx.restore();

      if (this.onLiveDraw) {
        ctx.save();
        this.onLiveDraw(ctx, this);
        ctx.restore();
      }

      if (this.showPerf) this.drawPerf();
    }

    drawPerf() {
      const ctx = this.lctx;
      ctx.setTransform(this.dpr, 0, 0, this.dpr, 0, 0);
      ctx.fillStyle = 'rgba(0,0,0,0.6)';
      ctx.fillRect(10, 10, 132, 30);
      ctx.fillStyle = '#8ef0c0';
      ctx.font = '14px ui-monospace,monospace';
      ctx.fillText(`${this.fps} FPS  ${this.page.objects.length} obj`, 18, 30);
    }

    /* ------------------------------ 对象绘制 ------------------------------ */
    drawObject(ctx, o) {
      const v = this.view;
      ctx.save();
      ctx.globalAlpha = o.alpha == null ? 1 : o.alpha;
      switch (o.type) {
        case 'stroke': {
          if (!o._geo) o._geo = IB.geo.strokeGeometry(o.pts, o.w, { flat: o.tool === 'marker' });
          ctx.fillStyle = o.color;
          if (o.tool === 'marker') {
            ctx.globalAlpha = (o.alpha == null ? 0.32 : o.alpha);
          }
          ctx.fill(o._geo.path);
          break;
        }
        case 'shape': this.drawShape(ctx, o, v); break;
        case 'text': this.drawText(ctx, o); break;
        case 'image': this.drawImageObj(ctx, o); break;
        default: break;
      }
      ctx.restore();
    }

    drawShape(ctx, o, v) {
      const p = M.shapePath(o);
      ctx.strokeStyle = o.color;
      ctx.lineWidth = Math.max(0.5, o.w);
      ctx.lineJoin = 'round';
      ctx.lineCap = 'round';
      if (o.dash) ctx.setLineDash(o.dash);

      if (o.fill && (o.shape === 'rect' || o.shape === 'ellipse' || o.shape === 'triangle' || o.shape === 'polygon')) {
        ctx.fillStyle = o.fill;
        ctx.fill(p);
      }
      if (o.shape === 'axis') this.drawAxis(ctx, o, v);
      else if (o.shape === 'arrow') {
        ctx.stroke(p);
        this.drawArrowHead(ctx, o);
      } else if (o.shape === 'func') {
        const pts = M.shapePath(o); // 已包含曲线
        ctx.stroke(pts);
      } else if (o.shape !== 'axis') {
        ctx.stroke(p);
      }
      ctx.setLineDash([]);
    }

    drawArrowHead(ctx, o) {
      const ang = Math.atan2(o.y2 - o.y1, o.x2 - o.x1);
      const size = Math.max(10, o.w * 3.4);
      const head = new Path2D();
      head.moveTo(o.x2, o.y2);
      head.lineTo(o.x2 - size * Math.cos(ang - 0.42), o.y2 - size * Math.sin(ang - 0.42));
      head.lineTo(o.x2 - size * Math.cos(ang + 0.42), o.y2 - size * Math.sin(ang + 0.42));
      head.closePath();
      ctx.fillStyle = o.color;
      ctx.fill(head);
    }

    drawAxis(ctx, o, v) {
      const bb = M.shapePath(o) && { x: Math.min(o.x1, o.x2), y: Math.min(o.y1, o.y2), w: Math.abs(o.x2 - o.x1), h: Math.abs(o.y2 - o.y1) };
      const ox = bb.x + bb.w / 2, oy = bb.y + bb.h / 2;
      const unit = Math.min(bb.w, bb.h) / 10;
      ctx.strokeStyle = o.color;
      ctx.lineWidth = Math.max(0.6, o.w);
      // 网格
      ctx.save();
      ctx.globalAlpha = 0.16;
      ctx.strokeStyle = o.color;
      ctx.beginPath();
      for (let i = -5; i <= 5; i++) {
        ctx.moveTo(ox + i * unit, bb.y); ctx.lineTo(ox + i * unit, bb.y + bb.h);
        ctx.moveTo(bb.x, oy + i * unit); ctx.lineTo(bb.x + bb.w, oy + i * unit);
      }
      ctx.stroke();
      ctx.restore();
      // 轴 + 箭头
      ctx.beginPath();
      ctx.moveTo(bb.x, oy); ctx.lineTo(bb.x + bb.w, oy);
      ctx.moveTo(ox, bb.y + bb.h); ctx.lineTo(ox, bb.y);
      ctx.stroke();
      const size = Math.max(8, o.w * 3);
      ctx.beginPath();
      ctx.moveTo(bb.x + bb.w, oy); ctx.lineTo(bb.x + bb.w - size, oy - size / 2); ctx.lineTo(bb.x + bb.w - size, oy + size / 2); ctx.closePath();
      ctx.moveTo(ox, bb.y); ctx.lineTo(ox - size / 2, bb.y + size); ctx.lineTo(ox + size / 2, bb.y + size); ctx.closePath();
      ctx.fillStyle = o.color;
      ctx.fill();
      // 刻度
      ctx.beginPath();
      for (let i = -5; i <= 5; i++) {
        if (!i) continue;
        ctx.moveTo(ox + i * unit, oy - 5); ctx.lineTo(ox + i * unit, oy + 5);
        ctx.moveTo(ox - 5, oy + i * unit); ctx.lineTo(ox + 5, oy + i * unit);
      }
      ctx.stroke();
      ctx.fillStyle = o.color;
      ctx.font = `${Math.max(11, unit * 0.5)}px system-ui,sans-serif`;
      ctx.fillText('O', ox - unit * 0.75, oy + unit * 0.8);
      ctx.fillText('x', bb.x + bb.w - size * 1.2, oy + unit * 0.8);
      ctx.fillText('y', ox + unit * 0.35, bb.y + size * 0.6);
    }

    drawText(ctx, o) {
      ctx.fillStyle = o.color;
      ctx.font = M.textFont(o);
      ctx.textBaseline = 'top';
      const lines = String(o.text || '').split('\n');
      lines.forEach((line, i) => ctx.fillText(line, o.x, o.y + i * o.size * 1.3));
    }

    drawImageObj(ctx, o) {
      const rec = getImage(o.src);
      if (rec.ready) {
        try { ctx.drawImage(rec.img, o.x, o.y, o.w, o.h); } catch (e) { /* ignore */ }
      } else {
        ctx.fillStyle = 'rgba(15,23,42,0.06)';
        ctx.fillRect(o.x, o.y, o.w, o.h);
      }
    }

    /* ------------------------------ 对象操作 ------------------------------ */
    addObject(o, opts) {
      opts = opts || {};
      this.page.objects.push(o);
      if (opts.undo !== false) IB.app && IB.app.commit('添加');
      this.drawIncremental(o);
    }

    /** 增量把单个对象画进静态层（避免整页重绘） */
    drawIncremental(o) {
      const b = M.objBBox(o);
      const ctx = this.sctx;
      const d = this.dpr;
      const s = this.view.scale;
      const sx = Math.floor((b.x * s + this.view.tx - 2) * d);
      const sy = Math.floor((b.y * s + this.view.ty - 2) * d);
      const sw = Math.ceil((b.w * s + 4) * d);
      const sh = Math.ceil((b.h * s + 4) * d);
      if (sw <= 0 || sh <= 0 || sw > this.staticCanvas.width * 1.05 || sh > this.staticCanvas.height * 1.05) {
        this.staticDirty = true; this.schedule(); return;
      }
      ctx.save();
      ctx.beginPath();
      ctx.rect(sx / d, sy / d, sw / d, sh / d);
      ctx.clip();
      // 只在该区域重画：先补背景，再画与区域相交的所有对象
      ctx.setTransform(d, 0, 0, d, 0, 0);
      ctx.fillStyle = this.bgColor;
      ctx.fillRect(sx / d, sy / d, sw / d, sh / d);
      this.drawPageRegion(sx / d, sy / d, sw / d, sh / d);
      ctx.restore();
    }

    drawPageRegion(x, y, w, h) {
      const ctx = this.sctx;
      const d = this.dpr, v = this.view, pr = this.pageRect;
      const px = v.tx, py = v.ty, pw = pr.w * v.scale, ph = pr.h * v.scale;
      ctx.setTransform(d, 0, 0, d, 0, 0);

      // 页面底
      ctx.fillStyle = this.page.bg === 'black' ? '#12161b' : (this.page.color || '#ffffff');
      ctx.fillRect(Math.max(x, px), Math.max(y, py), Math.min(x + w, px + pw) - Math.max(x, px), Math.min(y + h, py + ph) - Math.max(y, py));

      const region = this.toWorld(x, y);
      const region2 = this.toWorld(x + w, y + h);
      ctx.save();
      this.applyTransform(ctx);
      ctx.beginPath();
      ctx.rect(region.x, region.y, region2.x - region.x, region2.y - region.y);
      ctx.clip();
      for (const o of this.page.objects) {
        const b = M.objBBox(o);
        if (b.x > region2.x || b.x + b.w < region.x || b.y > region2.y || b.y + b.h < region.y) continue;
        this.drawObject(ctx, o);
      }
      ctx.restore();
    }

    removeObjects(ids) {
      const set = new Set(ids);
      this.page.objects = this.page.objects.filter((o) => !set.has(o.id));
      this.invalidateAll();
      IB.app && IB.app.commit('删除');
    }

    replaceObject(oldObj, newObjs) {
      const i = this.page.objects.indexOf(oldObj);
      if (i < 0) return;
      this.page.objects.splice(i, 1, ...newObjs);
      this.staticDirty = true; this.schedule();
    }

    duplicateSelection() {
      const app = IB.app;
      if (!app.selection.size) return;
      const objs = this.page.objects.filter((o) => app.selection.has(o.id));
      const copies = objs.map((o) => { const c = M.cloneObject(o); M.translate(c, 26, 26); return c; });
      copies.forEach((c) => this.page.objects.push(c));
      app.selection = new Set(copies.map((c) => c.id));
      this.invalidateAll();
      app.commit('复制');
    }

    objectsInRect(rect) {
      return this.page.objects.filter((o) => {
        const b = M.objBBox(o);
        return b.x <= rect.x + rect.w && b.x + b.w >= rect.x && b.y <= rect.y + rect.h && b.y + b.h >= rect.y;
      });
    }

    /* ------------------------------ 导出渲染 ------------------------------ */
    renderPage(page, scale) {
      const canvas = document.createElement('canvas');
      canvas.width = Math.round(this.doc.pageW * (scale || 1));
      canvas.height = Math.round(this.doc.pageH * (scale || 1));
      const ctx = canvas.getContext('2d');
      ctx.fillStyle = page.bg === 'black' ? '#12161b' : (page.color || '#ffffff');
      ctx.fillRect(0, 0, canvas.width, canvas.height);
      ctx.scale(scale || 1, scale || 1);
      const savedPage = this.doc.pages[this.doc.current];
      this.doc.pages[this.doc.current] = page;
      const asyncImages = [];
      for (const o of page.objects) {
        this.drawObject(ctx, o);
        if (o.type === 'image') {
          const rec = getImage(o.src);
          if (!rec.ready) asyncImages.push(rec);
        }
      }
      this.doc.pages[this.doc.current] = savedPage;
      if (asyncImages.length) {
        return Promise.all(asyncImages.map((r) => new Promise((res) => {
          if (r.ready) res();
          else { r.img.addEventListener('load', res, { once: true }); r.img.addEventListener('error', res, { once: true }); }
        }))).then(() => {
          const c2 = document.createElement('canvas');
          c2.width = canvas.width; c2.height = canvas.height;
          const ctx2 = c2.getContext('2d');
          ctx2.fillStyle = page.bg === 'black' ? '#12161b' : (page.color || '#ffffff');
          ctx2.fillRect(0, 0, c2.width, c2.height);
          ctx2.scale(scale || 1, scale || 1);
          this.doc.pages[this.doc.current] = page;
          for (const o of page.objects) this.drawObject(ctx2, o);
          this.doc.pages[this.doc.current] = savedPage;
          return c2;
        });
      }
      return Promise.resolve(canvas);
    }
  }

  IB.Board = Board;
})();
