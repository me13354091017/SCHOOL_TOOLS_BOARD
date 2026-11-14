/* ==========================================================================
 * 60-ui.js — 界面装配：工具栏 / 页面栏 / 课堂工具 / 弹窗 / 文本编辑 / 导出
 * ========================================================================== */
(function () {
  'use strict';
  const IB = (window.IB = window.IB || {});
  const { clamp, formatTime, pickFile, download, fileToDataURL, loadImage, Store } = IB.util;
  const M = IB.model;

  const COLORS = ['#1f2329', '#64748b', '#ffffff', '#ef4444', '#f97316', '#eab308', '#22c55e', '#0ea5e9', '#2563eb', '#ec4899'];
  const MARKER_COLORS = ['#ffe066', '#a7f3d0', '#bfdbfe', '#fbcfe8', '#fed7aa'];
  const WIDTHS = [4, 7, 10];
  const ERASERS = [14, 22, 34];
  const SHAPES = [
    { k: 'line', n: '直线' }, { k: 'arrow', n: '箭头' }, { k: 'rect', n: '矩形' },
    { k: 'ellipse', n: '圆形' }, { k: 'triangle', n: '三角形' }, { k: 'axis', n: '坐标系' },
    { k: 'func', n: '函数图像' }, { k: 'polygon', n: '多边形' },
  ];
  const FUNC_PRESETS = ['sin(x)', 'cos(x)', 'x^2', 'x^3', '1/x', 'sqrt(x)', 'abs(x)', 'ln(x)', 'sin(x)*x'];

  const el = (id) => document.getElementById(id);
  const pick = (sel, root) => (root || document).querySelector(sel);
  const pickAll = (sel, root) => Array.from((root || document).querySelectorAll(sel));

  function hexToRgba(hex, a) {
    const h = hex.replace('#', '');
    const v = h.length === 3 ? h.split('').map((c) => c + c).join('') : h;
    const n = parseInt(v, 16);
    return `rgba(${(n >> 16) & 255},${(n >> 8) & 255},${n & 255},${a})`;
  }

  const UI = {
    init(app) {
      this.app = app;
      this.board = app.board;
      this._buildColors();
      this._buildShapes();
      this._bindTools();
      this._bindTopbar();
      this._bindPages();
      this._bindClassroom();
      this._bindTimer();
      this._bindRollcall();
      this._bindTextEditor();
      this._bindExport();
      this._bindShortcuts();
      this._bindModals();
      this.refreshPages();
      this.setTool(app.toolName || 'pen');
    },

    /* ------------------------------ 颜色 / 粗细 / 图形 ------------------------------ */
    _buildColors() {
      const sw = el('swatches');
      sw.innerHTML = '';
      COLORS.forEach((c) => {
        const b = document.createElement('button');
        b.className = 'swatch' + (c === this.app.style.color ? ' active' : '');
        b.style.background = c;
        b.title = c;
        b.dataset.color = c;
        b.onclick = () => { this.app.style.color = c; this._syncColor(); this._closePops(); };
        sw.appendChild(b);
      });

      const mk = el('swatches-marker');
      mk.innerHTML = '';
      MARKER_COLORS.forEach((c) => {
        const b = document.createElement('button');
        b.className = 'swatch' + (c === this.app.markerColor ? ' active' : '');
        b.style.background = c;
        b.dataset.marker = c;
        b.onclick = () => { this.app.markerColor = c; this._syncColor(); this._closePops(); };
        mk.appendChild(b);
      });

      const widths = el('widths');
      widths.innerHTML = '';
      WIDTHS.forEach((w) => {
        const b = document.createElement('button');
        b.className = 'width-opt' + (w === this.app.style.width ? ' active' : '');
        b.innerHTML = `<b style="width:${Math.min(w * 2.2, 18)}px;height:${Math.min(w * 2.2, 18)}px"></b>`;
        b.onclick = () => { this.app.style.width = w; this._syncColor(); this._closePops(); };
        b.dataset.width = w;
        widths.appendChild(b);
      });

      const quick = el('width-quick');
      quick.innerHTML = '';
      WIDTHS.forEach((w) => {
        const b = document.createElement('button');
        b.className = 'width-dot' + (w === this.app.style.width ? ' active' : '');
        b.innerHTML = `<b style="width:${Math.min(w * 2, 15)}px;height:${Math.min(w * 2, 15)}px"></b>`;
        b.title = w + ' px';
        b.onclick = () => {
          this.app.style.width = w;
          this._syncColor();
        };
        quick.appendChild(b);
      });

      el('btn-color').onclick = (e) => { e.stopPropagation(); this._togglePop('pop-color'); };
    },

    _syncColor() {
      el('color-dot').style.background = this.app.style.color;
      pickAll('.swatch', el('swatches')).forEach((b) => b.classList.toggle('active', b.dataset.color === this.app.style.color));
      pickAll('.swatch', el('swatches-marker')).forEach((b) => b.classList.toggle('active', b.dataset.marker === this.app.markerColor));
      pickAll('.width-opt').forEach((b) => b.classList.toggle('active', Number(b.dataset.width) === this.app.style.width));
      pickAll('.width-dot').forEach((b) => b.classList.toggle('active', Number(b.dataset.width) === this.app.style.width));
    },

    _buildShapes() {
      const grid = el('shape-grid');
      grid.innerHTML = '';
      SHAPES.forEach((s) => {
        const b = document.createElement('button');
        b.className = 'shape-opt' + (s.k === this.app.shapeKind ? ' active' : '');
        b.dataset.shape = s.k;
        b.innerHTML = shapeIcon(s.k) + `<span>${s.n}</span>`;
        b.onclick = () => {
          this.app.shapeKind = s.k;
          pickAll('.shape-opt').forEach((x) => x.classList.toggle('active', x.dataset.shape === s.k));
          this.app.setTool('shape');
          if (s.k === 'func') this.openModal('modal-func');
          this._closePops();
        };
        grid.appendChild(b);
      });

      el('btn-shape-kind').onclick = (e) => { e.stopPropagation(); this._togglePop('pop-shape'); };

      const auto = el('auto-shape');
      auto.checked = this.app.autoShape !== false;
      auto.onchange = () => { this.app.autoShape = auto.checked; };

      const fill = el('fill-shape');
      fill.checked = !!this.app.shapeFill;
      fill.onchange = () => { this.app.shapeFill = fill.checked ? hexToRgba(this.app.style.color, 0.16) : null; };

      // 函数预设
      const fp = el('func-presets');
      fp.innerHTML = '';
      FUNC_PRESETS.forEach((f) => {
        const b = document.createElement('button');
        b.textContent = f;
        b.onclick = () => { el('func-expr').value = f; };
        fp.appendChild(b);
      });
      el('func-expr').onchange = () => { this.app.funcExpr = el('func-expr').value || 'x'; };
    },

    _togglePop(id) {
      const pop = el(id);
      const show = !!pop.hidden;
      this._closePops();
      pop.hidden = !show;
    },
    _closePops() { ['pop-color', 'pop-shape'].forEach((id) => { el(id).hidden = true; }); },

    /* ------------------------------ 工具切换 ------------------------------ */
    _bindTools() {
      pickAll('.tool').forEach((btn) => {
        btn.onclick = () => this.app.setTool(btn.dataset.tool);
      });
      el('btn-undo').onclick = () => this.app.undo();
      el('btn-redo').onclick = () => this.app.redo();
      el('btn-delete').onclick = () => this.app.deleteSelection();
      el('btn-clear-page').onclick = () => this.app.clearPage();
      el('btn-clear-doc').onclick = () => this.app.clearDoc();

      pickAll('#sel-bar button').forEach((b) => {
        b.onclick = () => this.app.selectionAction(b.dataset.sel);
      });

      document.addEventListener('pointerdown', (e) => {
        if (!e.target.closest('.popover') && !e.target.closest('#btn-color') && !e.target.closest('#btn-shape-kind')) this._closePops();
      });
    },

    setTool(name) {
      if (!this.app) return;
      pickAll('.tool').forEach((b) => b.classList.toggle('active', b.dataset.tool === name));
      const app = el('app');
      app.dataset.tool = name;
      // 切换到选择工具时，把形状/颜色浮层关掉
      this._closePops();
      // 橡皮：显示粗细选择
      if (name === 'eraser') this._showEraserUI();
      if (name === 'pen' || name === 'marker') this._hideEraserUI();
    },

    _showEraserUI() {
      const wq = el('width-quick');
      wq.dataset.mode = 'eraser';
      if (wq.dataset.built === 'eraser') return;
      wq.dataset.built = 'eraser';
      wq.innerHTML = '';
      ERASERS.forEach((n) => {
        const b = document.createElement('button');
        b.className = 'width-dot' + (n === (this.app.style.eraserSize || 22) ? ' active' : '');
        b.innerHTML = `<b style="width:${n / 2}px;height:${n / 2}px;opacity:.55"></b>`;
        b.title = '橡皮 ' + n;
        b.onclick = () => {
          this.app.style.eraserSize = n;
          pickAll('.width-dot', wq).forEach((x) => x.classList.toggle('active', x === b));
          this.app.refreshTool();
        };
        wq.appendChild(b);
      });
    },
    _hideEraserUI() {
      const wq = el('width-quick');
      if (wq.dataset.built !== 'eraser') return;
      wq.dataset.built = 'pen';
      wq.innerHTML = '';
      WIDTHS.forEach((w) => {
        const b = document.createElement('button');
        b.className = 'width-dot' + (w === this.app.style.width ? ' active' : '');
        b.innerHTML = `<b style="width:${Math.min(w * 2, 15)}px;height:${Math.min(w * 2, 15)}px"></b>`;
        b.onclick = () => { this.app.style.width = w; this._syncColor(); };
        wq.appendChild(b);
      });
    },

    /* ------------------------------ 顶栏 ------------------------------ */
    _bindTopbar() {
      const title = el('doc-title');
      title.onchange = () => { this.app.doc.name = title.value.trim() || '未命名白板'; this.app.persist(); };
      el('btn-open').onclick = () => this.app.openFile();
      el('btn-save').onclick = () => this.app.saveFile();
      el('btn-export').onclick = () => this.openModal('modal-export');
      el('btn-zoom').onclick = () => this.board.fitPage();
      el('btn-perf').onclick = (e) => {
        this.board.showPerf = !this.board.showPerf;
        e.currentTarget.classList.toggle('active', this.board.showPerf);
        this.board.invalidateAll(false);
      };
      el('btn-fullscreen').onclick = () => this.togglePresent();
      document.addEventListener('fullscreenchange', () => {
        el('app').classList.toggle('presenting', !!document.fullscreenElement);
        this.board.resize();
      });
      el('btn-classroom').onclick = () => { el('drawer').hidden = !el('drawer').hidden; };
      el('drawer-close').onclick = () => { el('drawer').hidden = true; };
    },

    async togglePresent() {
      const appEl = el('app');
      try {
        if (!document.fullscreenElement) {
          await appEl.requestFullscreen();
          appEl.classList.add('presenting');
        } else {
          await document.exitFullscreen();
          appEl.classList.remove('presenting');
          el('pagerail').classList.remove('hide');
        }
      } catch (e) {
        appEl.classList.toggle('presenting');
      }
      setTimeout(() => this.board.resize(), 120);
    },

    /* ------------------------------ 页面 ------------------------------ */
    _bindPages() {
      el('btn-add-page').onclick = () => this.app.addPage();
      el('btn-dup-page').onclick = () => this.app.duplicatePage();
      el('btn-del-page').onclick = () => this.app.deletePage();
      el('btn-page-bg').onclick = () => this.openModal('modal-bg');

      const bg = el('bg-grid');
      bg.innerHTML = '';
      [['plain', '纯白'], ['grid', '方格'], ['dots', '点阵'], ['lines', '横线'], ['black', '黑板']].forEach(([k, n]) => {
        const b = document.createElement('button');
        b.className = 'bg-opt' + (this.board.page.bg === k ? ' active' : '');
        b.dataset.bg = k;
        b.textContent = n;
        b.onclick = () => {
          this.board.page.bg = k;
          pickAll('.bg-opt').forEach((x) => x.classList.toggle('active', x.dataset.bg === k));
          this.board.invalidateAll();
          this.app.commit('背景');
          this.refreshPages();
        };
        bg.appendChild(b);
      });

      pickAll('#ratio-row button').forEach((b) => {
        b.onclick = () => {
          const [w, h] = b.dataset.ratio.split(':').map(Number);
          this.app.setRatio(w * 120, h * 120);
          pickAll('#ratio-row button').forEach((x) => x.classList.toggle('active', x === b));
        };
      });
    },

    refreshPages() {
      const wrap = el('page-thumbs');
      const doc = this.app.doc;
      wrap.innerHTML = '';
      doc.pages.forEach((p, i) => {
        const d = document.createElement('div');
        d.className = 'thumb' + (i === doc.current ? ' active' : '');
        const c = document.createElement('canvas');
        c.width = 160; c.height = 90;
        const n = document.createElement('span');
        n.className = 'num'; n.textContent = i + 1;
        d.appendChild(c); d.appendChild(n);
        d.onclick = () => this.app.setPage(i);
        wrap.appendChild(d);
        this._paintThumb(c, p);
      });
      el('doc-title').value = doc.name;
    },

    async _paintThumb(canvas, page) {
      const ctx = canvas.getContext('2d');
      ctx.fillStyle = page.bg === 'black' ? '#12161b' : (page.color || '#ffffff');
      ctx.fillRect(0, 0, canvas.width, canvas.height);
      const s = canvas.width / this.app.doc.pageW;
      ctx.save();
      ctx.scale(s, s);
      const saved = this.app.doc.pages[this.app.doc.current];
      this.app.doc.pages[this.app.doc.current] = page;
      for (const o of page.objects) this.board.drawObject(ctx, o);
      this.app.doc.pages[this.app.doc.current] = saved;
      ctx.restore();
    },
  };

  IB.UI = UI;
})();
