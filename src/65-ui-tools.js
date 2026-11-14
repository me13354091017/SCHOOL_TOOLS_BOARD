/* ==========================================================================
 * 65-ui-tools.js — 课堂工具：聚光灯 / 遮罩 / 放大镜 / 板中板 / 计时器 / 点名 / 文本 / 导出
 * ========================================================================== */
(function () {
  'use strict';
  const IB = (window.IB = window.IB || {});
  const { clamp, formatTime, pickFile, download, fileToDataURL, loadImage, Store } = IB.util;
  const M = IB.model;

  const el = (id) => document.getElementById(id);
  const pick = (sel, root) => (root || document).querySelector(sel);
  const pickAll = (sel, root) => Array.from((root || document).querySelectorAll(sel));

  // 后半部分方法挂到同一个 UI 对象上（60-ui.js 先加载）
  Object.assign(IB.UI, {
    /* ------------------------------ 课堂工具 ------------------------------ */
    _bindClassroom() {
      pickAll('.tile').forEach((t) => {
        t.onclick = () => {
          const act = t.dataset.act;
          if (act === 'timer') this.openModal('modal-timer');
          else if (act === 'rollcall') this.openModal('modal-rollcall');
          else if (act === 'spotlight') this.toggleSpotlight();
          else if (act === 'curtain') this.toggleCurtain();
          else if (act === 'magnifier') this.toggleMagnifier();
          else if (act === 'subboard') this.toggleSubboard();
          else if (act === 'bg') this.openModal('modal-bg');
          else if (act === 'pdf') this.openModal('modal-help-import');
          else if (act === 'shortcuts') this.openModal('modal-shortcuts');
        };
      });

      el('btn-pick-pdf').onclick = async () => {
        const file = await pickFile('application/pdf,.pdf');
        if (!file) return;
        this.closeModals();
        this.app.importPDF(file);
      };
      el('bf-close').onclick = () => { el('board-float').hidden = true; el('board-float').classList.remove('on'); };
      el('bf-to-board').onclick = () => {
        const text = el('bf-text').value.trim();
        if (!text) return;
        const p = this.board.toWorld(this.board.width / 2 - 200, this.board.height / 2 - 100);
        const t = M.obj.text(p.x, p.y, text, { size: 40, color: '#1f2329' });
        this.board.addObject(t, { undo: false });
        this.app.commit('板中板');
        el('bf-text').value = '';
        el('board-float').hidden = true;
      };
    },

    /* --- 聚光灯 --- */
    toggleSpotlight() {
      const sp = el('spotlight');
      if (sp.hidden) {
        sp.hidden = false;
        const box = sp.getBoundingClientRect();
        this._spot = { x: box.width / 2, y: box.height / 2, r: Math.min(box.width, box.height) * 0.36 };
        const apply = () => {
          const { x, y, r } = this._spot;
          const m = `radial-gradient(circle ${r}px at ${x}px ${y}px, transparent 0 ${Math.max(0, r - 1)}px, #000 ${r}px)`;
          sp.style.webkitMaskImage = m;
          sp.style.maskImage = m;
        };
        apply();
        const move = (e) => {
          const r = sp.getBoundingClientRect();
          this._spot.x = clamp(e.clientX - r.left, 0, r.width);
          this._spot.y = clamp(e.clientY - r.top, 0, r.height);
          apply();
        };
        const wheel = (e) => {
          this._spot.r = clamp(this._spot.r + (e.deltaY > 0 ? 36 : -36), 60, Math.min(sp.clientWidth, sp.clientHeight) * 0.95);
          apply();
        };
        this._spotCleanup = () => { window.removeEventListener('pointermove', move); window.removeEventListener('wheel', wheel); };
        window.addEventListener('pointermove', move);
        window.addEventListener('wheel', wheel, { passive: true });
        this.app.toast('聚光灯已开启：移动指针聚焦，滚轮调整光斑大小');
        pick('.tile[data-act="spotlight"]').classList.add('on');
      } else {
        sp.hidden = true;
        if (this._spotCleanup) this._spotCleanup();
        pick('.tile[data-act="spotlight"]').classList.remove('on');
      }
    },

    /* --- 遮罩揭幕 --- */
    toggleCurtain() {
      const c = el('curtain');
      const body = pick('.curtain-body', c);
      const handle = pick('.curtain-handle', c);
      if (c.hidden) {
        c.hidden = false;
        const H = el('stage').clientHeight;
        body.style.height = H + 'px';
        handle.style.top = (H - 20) + 'px';
        let dragging = false, startY = 0, startH = 0;
        const down = (e) => { dragging = true; startY = e.clientY; startH = body.getBoundingClientRect().height; handle.setPointerCapture(e.pointerId); };
        const move = (e) => {
          if (!dragging) return;
          const h = clamp(startH - (e.clientY - startY), 0, H);
          body.style.height = h + 'px';
          handle.style.top = (h - 20) + 'px';
        };
        const up = () => { dragging = false; };
        handle.onpointerdown = down;
        handle.onpointermove = move;
        handle.onpointerup = up;
        handle.onpointercancel = up;
        c.ondblclick = () => { c.hidden = true; };
        this.app.toast('向下拖动拉手即可揭幕，双击关闭遮罩');
        pick('.tile[data-act="curtain"]').classList.add('on');
      } else {
        c.hidden = true;
        pick('.tile[data-act="curtain"]').classList.remove('on');
      }
    },

    /* --- 放大镜 --- */
    toggleMagnifier() {
      const mag = el('magnifier');
      mag.hidden = !mag.hidden;
      const canvas = el('mag-canvas');
      const ctx = canvas.getContext('2d');
      if (mag.hidden) { mag.onpointermove = null; return; }
      canvas.width = 320; canvas.height = 320;

      const drawAt = (clientX, clientY) => {
        const stage = el('stage').getBoundingClientRect();
        const x = clientX - stage.left, y = clientY - stage.top;
        const src = this.board.staticCanvas;
        const zoom = 1.9;
        const sw = canvas.width / zoom, sh = canvas.height / zoom;
        ctx.clearRect(0, 0, canvas.width, canvas.height);
        ctx.fillStyle = '#fff';
        ctx.fillRect(0, 0, canvas.width, canvas.height);
        // 先用静态层，再叠动态层
        ctx.drawImage(src, (x - sw / 2) * this.board.dpr, (y - sh / 2) * this.board.dpr, sw * this.board.dpr, sh * this.board.dpr, 0, 0, canvas.width, canvas.height);
        ctx.drawImage(this.board.liveCanvas, (x - sw / 2) * this.board.dpr, (y - sh / 2) * this.board.dpr, sw * this.board.dpr, sh * this.board.dpr, 0, 0, canvas.width, canvas.height);
        mag.style.left = (clientX - 160) + 'px';
        mag.style.top = (clientY - 160) + 'px';
      };
      mag.onpointermove = (e) => drawAt(e.clientX, e.clientY);
      window.addEventListener('pointermove', (e) => { if (!mag.hidden) drawAt(e.clientX, e.clientY); }, { passive: true });
      this.app.toast('移动指针即可放大局部，再次点击关闭');
      pick('.tile[data-act="magnifier"]').classList.add('on');
    },

    /* --- 板中板 --- */
    toggleSubboard() {
      const bf = el('board-float');
      bf.hidden = !bf.hidden;
      if (!bf.hidden) pick('#bf-text').focus();
      pick('.tile[data-act="subboard"]').classList.toggle('on', !bf.hidden);
    },

    /* ------------------------------ 计时器 ------------------------------ */
    _bindTimer() {
      const state = { total: 300, remain: 300, running: false, raf: 0, last: 0 };
      this.timer = state;
      const display = el('timer-time');
      const ring = el('ring-fg');
      const sync = () => {
        display.textContent = formatTime(state.remain);
        const len = 553;
        const p = state.total ? clamp(state.remain / state.total, 0, 1) : 0;
        ring.style.strokeDashoffset = String(len * (1 - p));
      };
      state.sync = sync;

      const tick = (t) => {
        if (!state.running) return;
        if (!state.last) state.last = t;
        const dt = (t - state.last) / 1000;
        state.last = t;
        if (dt > 0.4) return;
        state.remain = Math.max(0, state.remain - dt);
        sync();
        if (state.remain <= 0) {
          state.running = false;
          el('timer-toggle').textContent = '开始';
          this.app.toast('⏰ 时间到');
          beep();
          return;
        }
        state.raf = requestAnimationFrame(tick);
      };

      el('timer-toggle').onclick = () => {
        state.running = !state.running;
        state.last = 0;
        el('timer-toggle').textContent = state.running ? '暂停' : '开始';
        if (state.running) { if (state.remain <= 0) state.remain = state.total; state.raf = requestAnimationFrame(tick); }
        else cancelAnimationFrame(state.raf);
      };
      el('timer-reset').onclick = () => {
        state.running = false; state.remain = state.total;
        el('timer-toggle').textContent = '开始';
        sync();
      };
      pickAll('.timer-presets button').forEach((b) => {
        b.onclick = () => {
          state.total = Number(b.dataset.min) * 60;
          state.remain = state.total;
          state.running = false;
          el('timer-toggle').textContent = '开始';
          cancelAnimationFrame(state.raf);
          sync();
        };
      });
      sync();
    },

    /* ------------------------------ 随机点名 ------------------------------ */
    _bindRollcall() {
      const state = { names: [], running: false, timer: 0, idx: 0 };
      this.roll = state;
      const list = el('roll-list');
      const nameEl = el('roll-name');
      const count = el('roll-count');

      const parse = () => {
        state.names = list.value.split(/[\n,，、;；\s]+/).map((s) => s.trim()).filter(Boolean);
        count.textContent = `共 ${state.names.length} 人 · 名单会自动保存在本机`;
      };
      const load = async () => {
        const saved = await Store.get('rollcall');
        if (saved) list.value = saved;
        parse();
      };
      list.oninput = () => { parse(); Store.set('rollcall', list.value); };
      load();

      let rollTimer = 0;
      const startRoll = () => {
        if (!state.names.length) { this.app.toast('请先填写名单'); return; }
        state.running = true;
        nameEl.classList.add('rolling');
        const step = () => {
          if (!state.running) return;
          nameEl.textContent = state.names[Math.floor(Math.random() * state.names.length)];
          rollTimer = setTimeout(step, 55);
        };
        step();
      };
      const stopRoll = () => {
        state.running = false;
        clearTimeout(rollTimer);
        nameEl.classList.remove('rolling');
        if (state.names.length) {
          const pickName = state.names[Math.floor(Math.random() * state.names.length)];
          nameEl.textContent = pickName;
          beep();
        }
      };
      el('roll-start').onclick = () => { state.running ? stopRoll() : startRoll(); };
      el('roll-once').onclick = () => {
        if (!state.names.length) { this.app.toast('请先填写名单'); return; }
        nameEl.classList.remove('rolling');
        nameEl.textContent = state.names[Math.floor(Math.random() * state.names.length)];
        beep();
      };
      pick('#modal-rollcall').addEventListener('transitionend', parse);
    },

    /* ------------------------------ 文本编辑 ------------------------------ */
    _bindTextEditor() {
      const box = el('text-editor');
      const input = el('text-input');
      let editing = null;   // 已有文本对象
      let at = null;

      const commit = () => {
        const text = input.value.replace(/\s+$/g, '');
        box.hidden = true;
        if (!text.trim()) {
          if (editing) { this.board.removeObjects([editing.id]); }
          editing = null; at = null;
          return;
        }
        if (editing) {
          editing.text = text;
          M.reflowText(editing);
          M.invalidate(editing);
          this.board.invalidateAll();
          this.app.commit('编辑文本');
        } else {
          const t = M.obj.text(at.x, at.y, text, { size: this.app.style.textSize || 42, color: this.app.style.color, bold: this.app.style.textBold });
          this.board.addObject(t, { undo: false });
          this.app.commit('添加文本');
        }
        editing = null; at = null;
      };
      const cancel = () => { box.hidden = true; editing = null; at = null; };

      this._textCommit = commit;
      this._textCancel = cancel;

      el('te-done').onclick = commit;

      pickAll('.te-tools button[data-size]').forEach((b) => {
        b.onclick = () => {
          const cur = parseFloat(input.style.fontSize) || 26;
          const next = clamp(cur + Number(b.dataset.size) * 4, 12, 96);
          input.style.fontSize = next + 'px';
          if (!editing) this.app.style.textSize = next / this.board.view.scale;
        };
      });

      input.addEventListener('keydown', (e) => {
        if (e.key === 'Escape') { e.preventDefault(); cancel(); }
        if (e.key === 'Enter' && (e.ctrlKey || e.metaKey)) { e.preventDefault(); commit(); }
        e.stopPropagation();
      });
      input.addEventListener('blur', () => { setTimeout(() => { if (!box.hidden) commit(); }, 60); });
      input.addEventListener('input', () => {
        input.style.height = 'auto';
        input.style.height = input.scrollHeight + 'px';
      });

      this.openTextEditor = (pt, existing) => {
        const b = this.board;
        const screen = b.toScreen(pt.x, pt.y);
        box.hidden = false;
        const size = existing ? existing.size : (this.app.style.textSize || 42);
        input.value = existing ? existing.text : '';
        input.style.fontSize = Math.max(14, size * b.view.scale) + 'px';
        input.style.height = 'auto';
        const left = clamp(screen.x, 80, window.innerWidth - 320);
        const top = clamp(screen.y, 70, window.innerHeight - 160);
        box.style.left = left + 'px';
        box.style.top = top + 'px';
        box.style.width = '300px';
        editing = existing || null;
        at = pt;
        setTimeout(() => { input.focus(); input.setSelectionRange(input.value.length, input.value.length); }, 30);
      };
    },

    /* ------------------------------ 导出 / 打开 ------------------------------ */
    _bindExport() {
      pickAll('[data-export]').forEach((b) => {
        b.onclick = () => {
          this.closeModals();
          this.app.exportAs(b.dataset.export);
        };
      });
      // 函数弹窗确认
      el('func-expr').addEventListener('change', () => { this.app.funcExpr = el('func-expr').value; });
    },

    /* ------------------------------ 快捷键 ------------------------------ */
    _bindShortcuts() {
      const map = {
        p: 'pen', m: 'marker', e: 'eraser', s: 'shape', t: 'text', v: 'select', l: 'laser', h: 'hand',
      };
      window.addEventListener('keydown', (e) => {
        const typing = e.target && (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA' || e.target.isContentEditable);
        if (typing) return;
        const key = e.key.toLowerCase();
        if ((e.ctrlKey || e.metaKey) && key === 'z') { e.preventDefault(); e.shiftKey ? this.app.redo() : this.app.undo(); return; }
        if ((e.ctrlKey || e.metaKey) && key === 'y') { e.preventDefault(); this.app.redo(); return; }
        if ((e.ctrlKey || e.metaKey) && key === 's') { e.preventDefault(); this.app.saveFile(); return; }
        if ((e.ctrlKey || e.metaKey) && key === 'd') { e.preventDefault(); this.app.board.duplicateSelection(); return; }
        if ((e.ctrlKey || e.metaKey) && key === 'a') { e.preventDefault(); this.app.selectAll(); return; }
        if (key === 'delete' || key === 'backspace') {
          if (this.app.selection.size) { e.preventDefault(); this.app.deleteSelection(); }
          return;
        }
        if (key === 'escape') {
          if (!el('text-editor').hidden) { this._textCancel(); return; }
          this.closeModals();
          el('spotlight').hidden = true;
          this.app.selection.clear(); this.app.onSelectionChange(); this.board.invalidateLive();
          return;
        }
        if (key === 'arrowleft') { this.app.setPage(this.app.doc.current - 1); return; }
        if (key === 'arrowright') { this.app.setPage(this.app.doc.current + 1); return; }
        if (key === 'f') { this.togglePresent(); return; }
        if (map[key]) this.app.setTool(map[key]);
      });
    },

    /* ------------------------------ 弹窗 ------------------------------ */
    _bindModals() {
      pickAll('[data-close]').forEach((b) => { b.onclick = () => this.closeModals(); });
      el('modal-layer').addEventListener('pointerdown', (e) => {
        if (e.target === el('modal-layer')) this.closeModals();
      });
    },
    openModal(id) {
      el('modal-layer').hidden = false;
      pickAll('.modal').forEach((m) => { m.hidden = m.id !== id; });
    },
    closeModals() {
      el('modal-layer').hidden = true;
      pickAll('.modal').forEach((m) => { m.hidden = true; });
    },

    toast(msg) {
      const t = el('toast');
      t.textContent = msg;
      t.hidden = false;
      clearTimeout(this._toastTimer);
      this._toastTimer = setTimeout(() => { t.hidden = true; }, 1800);
    },

    onSelectionChange() {
      if (!this.app) return;
      const bar = el('sel-bar');
      const app = this.app;
      const has = app.selection.size > 0 && app.toolName === 'select';
      bar.hidden = !has;
      if (has) {
        const b = app.selectionBounds();
        if (b) {
          const s = this.board.toScreen(b.x + b.w / 2, b.y);
          bar.style.left = clamp(s.x, 220, window.innerWidth - 220) + 'px';
          bar.style.bottom = clamp(window.innerHeight - s.y + 20, 96, window.innerHeight - 100) + 'px';
        }
      }
    }
  });

  function beep() {
    try {
      const ac = new AudioContext();
      const o = ac.createOscillator();
      const g = ac.createGain();
      o.connect(g); g.connect(ac.destination);
      o.frequency.value = 880;
      g.gain.value = 0.12;
      o.start();
      setTimeout(() => { o.frequency.value = 660; }, 250);
      setTimeout(() => { o.stop(); ac.close(); }, 700);
    } catch (e) { /* 静默 */ }
  }

  function shapeIcon(k) {
    const s = '<svg viewBox="0 0 24 24">';
    const map = {
      line: '<path d="M4 20L20 4"/>',
      arrow: '<path d="M4 20L18 6M12 6h6v6"/>',
      rect: '<rect x="4" y="6" width="16" height="12"/>',
      ellipse: '<ellipse cx="12" cy="12" rx="8" ry="6"/>',
      triangle: '<path d="M12 4l8 15H4z"/>',
      axis: '<path d="M4 12h16M12 20V4M4 8v8M18 8v8"/>',
      func: '<path d="M3 18c4 0 4-12 9-12s5 12 9 12"/>',
      polygon: '<path d="M12 3l8 6-3 10H7L4 9z"/>',
    };
    return s + (map[k] || '') + '</svg>';
  }

  IB.shapeIcon = shapeIcon;
  // 60-ui.js 在装配形状按钮时会调用同名函数
  window.shapeIcon = shapeIcon;
})();
