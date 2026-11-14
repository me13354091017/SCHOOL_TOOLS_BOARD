/* ==========================================================================
 * 40-input.js — 指针/手势输入层：压感笔、合并事件、双指缩放、手掌防误触
 * ========================================================================== */
(function () {
  'use strict';
  const IB = (window.IB = window.IB || {});

  class Input {
    constructor(board, el) {
      this.board = board;
      this.el = el;
      this.pointers = new Map();
      this.drawId = null;
      this.gesture = null;
      this.penActiveAt = -1e9;
      this.spaceDown = false;
      this.tempPan = false;
      this._bind();
    }

    local(e) {
      const r = this.el.getBoundingClientRect();
      return { x: e.clientX - r.left, y: e.clientY - r.top };
    }

    _bind() {
      const el = this.el;
      el.addEventListener('pointerdown', (e) => this.onDown(e), { passive: false });
      el.addEventListener('pointermove', (e) => this.onMove(e), { passive: false });
      el.addEventListener('pointerup', (e) => this.onUp(e));
      el.addEventListener('pointercancel', (e) => this.onUp(e));
      el.addEventListener('contextmenu', (e) => e.preventDefault());
      el.addEventListener('wheel', (e) => this.onWheel(e), { passive: false });

      window.addEventListener('keydown', (e) => {
        if (e.code === 'Space' && !e.repeat && !isTyping(e.target)) { this.spaceDown = true; this.setCursor('grab'); e.preventDefault(); }
      });
      window.addEventListener('keyup', (e) => {
        if (e.code === 'Space') { this.spaceDown = false; this.setCursor(this.board.tool ? this.board.tool.cursor : 'default'); }
      });
      window.addEventListener('blur', () => { this.spaceDown = false; this.pointers.clear(); });
    }

    setCursor(c) { this.el.style.cursor = c; }

    currentToolCtx(e, pt) {
      return {
        board: this.board,
        app: IB.app,
        pt,
        screen: pt,
        pressure: e && e.pressure != null && e.pressure > 0 ? e.pressure : 0.5,
        pointerType: e ? e.pointerType : 'mouse',
        shift: !!(e && e.shiftKey),
        alt: !!(e && e.altKey),
        ctrl: !!(e && (e.ctrlKey || e.metaKey)),
      };
    }

    onDown(e) {
      if (e.button === 1 || e.button === 2) { return; }
      const pt = this.local(e);
      this.pointers.set(e.pointerId, { x: pt.x, y: pt.y, type: e.pointerType, pressure: e.pressure });
      if (e.pointerType === 'pen') this.penActiveAt = performance.now();
      this.el.setPointerCapture && this.el.setPointerCapture(e.pointerId);

      // 手掌防误触：笔刚离开 600ms 内的触摸不落笔
      const palm = e.pointerType === 'touch' && performance.now() - this.penActiveAt < 600;

      if (this.pointers.size >= 2 && (e.pointerType === 'touch' || this.pointers.get(this.drawId)?.type === 'touch')) {
        this.startGesture();
        return;
      }
      if (this.spaceDown || e.button === 1) { this.tempPan = { x: pt.x, y: pt.y, tx: this.board.view.tx, ty: this.board.view.ty }; return; }
      if (palm) return;

      const tool = this.board.tool;
      if (!tool) return;
      this.drawId = e.pointerId;
      const ctx = this.currentToolCtx(e, this.board.toWorld(pt.x, pt.y));
      ctx.screen = pt;
      tool.begin(ctx);
    }

    onMove(e) {
      const pt = this.local(e);
      const rec = this.pointers.get(e.pointerId);
      if (rec) { rec.x = pt.x; rec.y = pt.y; }
      if (e.pointerType === 'pen') this.penActiveAt = performance.now();

      if (this.tempPan) {
        const v = this.board.view;
        v.tx = this.tempPan.tx + (pt.x - this.tempPan.x);
        v.ty = this.tempPan.ty + (pt.y - this.tempPan.y);
        this.board.invalidateAll(false);
        return;
      }
      if (this.gesture) { this.updateGesture(pt); return; }
      if (e.pointerId !== this.drawId) {
        this.board.tool && this.board.tool.hover && this.board.tool.hover(this.currentToolCtx(e, this.board.toWorld(pt.x, pt.y)));
        return;
      }

      const tool = this.board.tool;
      if (!tool) return;
      const events = e.getCoalescedEvents ? e.getCoalescedEvents() : [e];
      for (const ev of events) {
        const p = ev === e ? pt : this.local(ev);
        const ctx = this.currentToolCtx(ev, this.board.toWorld(p.x, p.y));
        ctx.screen = p;
        tool.move(ctx);
      }
    }

    onUp(e) {
      const pt = this.local(e);
      if (this.pointers.has(e.pointerId)) this.pointers.delete(e.pointerId);

      if (this.tempPan) { this.tempPan = null; return; }

      if (this.gesture) {
        if (this.pointers.size < 2) this.gesture = null;
        if (this.drawId == null) return;
      }
      if (e.pointerId !== this.drawId) {
        if (this.pointers.size === 0 && this.gesture) this.gesture = null;
        return;
      }
      this.drawId = null;
      const tool = this.board.tool;
      if (!tool) return;
      const ctx = this.currentToolCtx(e, this.board.toWorld(pt.x, pt.y));
      ctx.screen = pt;
      tool.end(ctx);
    }

    /* ------------------------------ 双指手势 ------------------------------ */
    startGesture() {
      const tool = this.board.tool;
      if (this.drawId != null && tool && tool.cancel) tool.cancel();
      this.drawId = null;
      const pts = Array.from(this.pointers.values()).slice(0, 2);
      this.gesture = {
        startDist: Math.hypot(pts[0].x - pts[1].x, pts[0].y - pts[1].y) || 1,
        startScale: this.board.view.scale,
        startMid: { x: (pts[0].x + pts[1].x) / 2, y: (pts[0].y + pts[1].y) / 2 },
        startTx: this.board.view.tx,
        startTy: this.board.view.ty,
        moved: false,
      };
    }

    updateGesture(pt) {
      const pts = Array.from(this.pointers.values()).slice(0, 2);
      if (pts.length < 2) return;
      const g = this.gesture;
      const mid = { x: (pts[0].x + pts[1].x) / 2, y: (pts[0].y + pts[1].y) / 2 };
      const d = Math.hypot(pts[0].x - pts[1].x, pts[0].y - pts[1].y) || 1;
      const k = Math.max(0.15, Math.min(6, g.startScale * (d / g.startDist))) / g.startScale;
      const v = this.board.view;
      v.scale = g.startScale * k;
      v.tx = mid.x - (g.startMid.x - g.startTx) * k;
      v.ty = mid.y - (g.startMid.y - g.startTy) * k;
      this.board.invalidateAll(false);
    }

    onWheel(e) {
      e.preventDefault();
      const pt = this.local(e);
      if (e.ctrlKey || e.metaKey) {
        this.board.zoomAt(pt.x, pt.y, Math.exp(-e.deltaY * 0.0016));
      } else {
        this.board.zoomAt(pt.x, pt.y, Math.exp(-e.deltaY * 0.0012));
      }
    }
  }

  function isTyping(t) {
    if (!t) return false;
    const tag = (t.tagName || '').toLowerCase();
    return tag === 'input' || tag === 'textarea' || t.isContentEditable;
  }

  IB.Input = Input;
})();
