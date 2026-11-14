/* ==========================================================================
 * 50-tools.js — 工具集：钢笔(时间笔锋)/荧光笔/橡皮/图形/选择/文本/图片/激光/漫游
 * ========================================================================== */
(function () {
  'use strict';
  const IB = (window.IB = window.IB || {});
  const M = IB.model;
  const geo = IB.geo;
  const { clamp, dist, uid } = IB.util;

  const app = () => IB.app;

  /* ------------------------------ 钢笔 / 荧光笔 ------------------------------ */
  class PenTool {
    constructor(board, kind) {
      this.board = board;
      this.kind = kind || 'pen';
      this.cursor = this.kind === 'marker' ? 'cell' : 'crosshair';
      this.pts = null;
    }
    width() {
      const s = app().style;
      return this.kind === 'marker' ? s.width * 2.4 : s.width;
    }
    color() {
      return this.kind === 'marker' ? (app().markerColor || '#ffe066') : app().style.color;
    }
    begin(ctx) {
      this.t0 = performance.now();
      const o = M.obj.stroke([], {
        w: this.width(), color: this.color(),
        alpha: this.kind === 'marker' ? 0.34 : 1,
        tool: this.kind === 'marker' ? 'marker' : 'pen',
      });
      this.obj = o;
      this.pts = o.pts;
      this.board.previewObjects = [o];
      this.addPoint(ctx, true);
    }
    addPoint(ctx, force) {
      const last = this.pts[this.pts.length - 1];
      const p = { x: ctx.pt.x, y: ctx.pt.y, p: ctx.pressure, t: performance.now() - this.t0 };
      if (!last || force || dist(last, p) > 0.9) {
        // 两点间插值，避免高速度下笔迹断裂
        if (last && dist(last, p) > 26) {
          const n = Math.min(6, Math.floor(dist(last, p) / 12));
          for (let i = 1; i < n; i++) {
            this.pts.push({ x: last.x + (p.x - last.x) * (i / n), y: last.y + (p.y - last.y) * (i / n), p: p.p, t: last.t + (p.t - last.t) * (i / n) });
          }
        }
        this.pts.push(p);
      } else {
        last.x = p.x; last.y = p.y; last.p = p.p;
      }
      this.board.invalidateLive();
    }
    move(ctx) { this.addPoint(ctx); }
    end(ctx) {
      const pts = this.pts;
      if (ctx && ctx.pt && pts && pts.length) {
        const last = pts[pts.length - 1];
        if (Math.hypot(last.x - ctx.pt.x, last.y - ctx.pt.y) > 1) {
          pts.push({ x: ctx.pt.x, y: ctx.pt.y, p: ctx.pressure, t: performance.now() - this.t0 });
        }
      }
      this.board.previewObjects = [];
      if (!pts || pts.length === 0) { this.board.invalidateLive(); return; }
      const fin = pts.length > 2 ? geo.rdp(pts, 0.9) : pts;
      if (fin.length < 2) fin.push({ x: fin[0].x + 0.01, y: fin[0].y, p: fin[0].p, t: 1 });

      // 手绘 → 标准图形
      if (this.kind === 'pen' && app().autoShape) {
        const rec = geo.recognize(fin);
        if (rec && (rec.confidence || 0) > 0.55) {
          const shape = M.obj.shape(rec.shape, rec.x1, rec.y1, rec.x2, rec.y2, {
            w: app().style.width, color: app().style.color, verts: rec.verts || null,
          });
          this.board.addObject(shape, { undo: false });
          app().commit('图形识别');
          app().toast('识别为' + shapeName(rec.shape));
          return;
        }
      }
      this.obj.pts = fin;
      this.obj._geo = null;
      this.board.addObject(this.obj, { undo: false });
      app().commit('书写');
    }
    cancel() {
      this.board.previewObjects = [];
      this.board.invalidateLive();
    }
  }

  function shapeName(s) {
    return ({ line: '直线', arrow: '箭头', rect: '矩形', ellipse: '圆形', triangle: '三角形', polygon: '多边形', axis: '坐标系', func: '函数图像' })[s] || '图形';
  }

  /* ------------------------------ 橡皮 ------------------------------ */
  class EraserTool {
    constructor(board) {
      this.board = board;
      this.cursor = 'none';
      this.radius = 16;
      this.pos = null;
      this.changed = false;
    }
    sizeWorld() { return app().style.eraserSize || 18; }
    begin(ctx) { this.pos = ctx.pt; this.changed = false; this.erase(ctx.pt); }
    hover(ctx) { this.pos = ctx.pt; this.board.invalidateLive(); }
    move(ctx) {
      this.pos = ctx.pt;
      this.erase(ctx.pt);
      this.board.invalidateLive();
    }
    end() {
      this.pos = null;
      this.board.invalidateAll();
      if (this.changed) app().commit('擦除');
      this.changed = false;
    }
    cancel() { this.pos = null; this.board.invalidateLive(); }

    erase(pt) {
      const page = this.board.page;
      const r = this.sizeWorld();
      for (let i = page.objects.length - 1; i >= 0; i--) {
        const o = page.objects[i];
        if (!M.hitTest(o, pt, r * 0.5)) continue;
        if (o.type === 'stroke') {
          const runs = splitStroke(o, pt, r);
          page.objects.splice(i, 1, ...runs);
          if (runs.length) app().selection.delete(o.id);
        } else {
          page.objects.splice(i, 1);
          app().selection.delete(o.id);
        }
        this.changed = true;
        this.board.staticDirty = true;
        return; // 一次只擦最上层
      }
    }

    drawOverlay(ctx, board) {
      if (!this.pos) return;
      const v = board.view;
      ctx.setTransform(board.dpr, 0, 0, board.dpr, 0, 0);
      const s = board.toScreen(this.pos.x, this.pos.y);
      ctx.strokeStyle = 'rgba(15,23,42,0.55)';
      ctx.lineWidth = 1.5;
      ctx.beginPath();
      ctx.arc(s.x, s.y, this.sizeWorld() * v.scale, 0, Math.PI * 2);
      ctx.stroke();
      ctx.fillStyle = 'rgba(255,255,255,0.35)';
      ctx.fill();
    }
  }

  function splitStroke(o, pt, r) {
    const runs = [];
    let cur = [];
    for (const p of o.pts) {
      if (dist(p, pt) <= r) {
        if (cur.length > 1) runs.push(cur);
        cur = [];
      } else {
        cur.push(p);
      }
    }
    if (cur.length > 1) runs.push(cur);
    return runs.map((run) => M.obj.stroke(run, { w: o.w, color: o.color, alpha: o.alpha, tool: o.tool }));
  }

  /* ------------------------------ 图形 ------------------------------ */
  class ShapeTool {
    constructor(board) {
      this.board = board;
      this.cursor = 'crosshair';
      this.obj = null;
    }
    begin(ctx) {
      this.start = ctx.pt;
      this.bounds = { minX: ctx.pt.x, minY: ctx.pt.y, maxX: ctx.pt.x, maxY: ctx.pt.y };
      this.obj = M.obj.shape(app().shapeKind, ctx.pt.x, ctx.pt.y, ctx.pt.x, ctx.pt.y, {
        w: app().style.width, color: app().style.color,
        fill: app().shapeFill, expr: app().funcExpr, dash: app().shapeDash,
      });
      if (this.obj.shape === 'func') {
        this.obj.y2 = ctx.pt.y - 200;
        this.obj.x2 = ctx.pt.x + 320;
      }
      this.board.previewObjects = [this.obj];
      this.board.invalidateLive();
    }
    move(ctx) {
      if (!this.obj) return;
      const kind = this.obj.shape;
      let x2 = ctx.pt.x, y2 = ctx.pt.y;
      const x1 = this.start.x, y1 = this.start.y;
      const b = this.bounds;
      if (b) {
        b.minX = Math.min(b.minX, x2); b.minY = Math.min(b.minY, y2);
        b.maxX = Math.max(b.maxX, x2); b.maxY = Math.max(b.maxY, y2);
      }

      if (ctx.shift) {
        if (kind === 'line' || kind === 'arrow') {
          const dx = x2 - x1, dy = y2 - y1;
          const ang = Math.round(Math.atan2(dy, dx) / (Math.PI / 4)) * (Math.PI / 4);
          const len = Math.hypot(dx, dy);
          x2 = x1 + Math.cos(ang) * len; y2 = y1 + Math.sin(ang) * len;
        } else {
          const size = Math.max(Math.abs(x2 - x1), Math.abs(y2 - y1));
          x2 = x1 + Math.sign(x2 - x1 || 1) * size;
          y2 = y1 + Math.sign(y2 - y1 || 1) * size;
        }
      }
      if (ctx.alt) {
        x2 = x1 + (x2 - x1) * 2;
        y2 = y1 + (y2 - y1) * 2;
        this.obj.x1 = x1 - (x2 - x1) / 2;
        this.obj.y1 = y1 - (y2 - y1) / 2;
      } else {
        this.obj.x1 = x1; this.obj.y1 = y1;
      }
      if (kind === 'line' || kind === 'arrow') {
        this.obj.x2 = x2; this.obj.y2 = y2;
      } else if (kind !== 'func' && b) {
        // 用整段拖动的包围盒，支持“描一圈”式画法
        this.obj.x1 = b.minX; this.obj.y1 = b.minY;
        this.obj.x2 = b.maxX; this.obj.y2 = b.maxY;
      } else {
        this.obj.x2 = x2; this.obj.y2 = y2;
      }
      M.invalidate(this.obj);
      this.board.invalidateLive();
    }
    end(ctx) {
      this.board.previewObjects = [];
      const o = this.obj;
      this.obj = null;
      if (!o) return;
      // 抬笔位置兜底：极快拖动时可能没有收到最后的 pointermove
      if (ctx && ctx.pt && o.shape !== 'func') {
        const dx = Math.abs(ctx.pt.x - o.x1), dy = Math.abs(ctx.pt.y - o.y1);
        if (dx > Math.abs(o.x2 - o.x1) || dy > Math.abs(o.y2 - o.y1)) {
          o.x2 = ctx.pt.x; o.y2 = ctx.pt.y;
          M.invalidate(o);
        }
      }
      let w = Math.abs(o.x2 - o.x1), h = Math.abs(o.y2 - o.y1);
      if (o.shape === 'axis' || o.shape === 'func') {
        if (w < 40 || h < 40) { w = 520; h = 400; o.x2 = o.x1 + w; o.y2 = o.y1 + h; M.invalidate(o); }
      } else if ((o.shape === 'line' || o.shape === 'arrow') && Math.hypot(o.x2 - o.x1, o.y2 - o.y1) < 12) {
        this.board.invalidateLive(); return;
      } else if (w < 10 || h < 10) {
        this.board.invalidateLive(); return;
      }
      this.board.addObject(o, { undo: false });
      app().commit('绘制' + shapeName(o.shape));
      this.board.invalidateLive();
    }
    cancel() { this.board.previewObjects = []; this.board.invalidateLive(); }
  }

  /* ------------------------------ 选择 ------------------------------ */
  class SelectTool {
    constructor(board) {
      this.board = board;
      this.cursor = 'default';
      this.mode = 'idle';
      this.dragStart = null;
      this.last = null;
      this.moved = false;
      this.orig = null;
      this.anchor = null;
      this.marquee = null;
    }

    selectionBounds() {
      const objs = this.board.page.objects.filter((o) => app().selection.has(o.id));
      return M.bboxOfObjects(objs);
    }

    handles() {
      const b = this.selectionBounds();
      if (!b || b.w === 0 || b.h === 0) return [];
      const pad = 6 / this.board.view.scale;
      const bb = { x: b.x - pad, y: b.y - pad, w: b.w + pad * 2, h: b.h + pad * 2 };
      return [
        { id: 'nw', x: bb.x, y: bb.y, ax: bb.x + bb.w, ay: bb.y + bb.h },
        { id: 'ne', x: bb.x + bb.w, y: bb.y, ax: bb.x, ay: bb.y + bb.h },
        { id: 'se', x: bb.x + bb.w, y: bb.y + bb.h, ax: bb.x, ay: bb.y },
        { id: 'sw', x: bb.x, y: bb.y + bb.h, ax: bb.x + bb.w, ay: bb.y },
      ];
    }

    handleAt(pt) {
      const tol = 14 / this.board.view.scale;
      for (const h of this.handles()) if (Math.hypot(pt.x - h.x, pt.y - h.y) <= tol) return h;
      return null;
    }

    topObjectAt(pt) {
      const objs = this.board.page.objects;
      for (let i = objs.length - 1; i >= 0; i--) {
        if (M.hitTest(objs[i], pt, 8 / this.board.view.scale)) return objs[i];
      }
      return null;
    }

    begin(ctx) {
      this.moved = false;
      this.dragStart = ctx.pt;
      this.last = ctx.pt;
      const A = app();

      const handle = A.selection.size ? this.handleAt(ctx.pt) : null;
      if (handle) {
        this.mode = 'scale';
        this.anchor = { x: handle.ax, y: handle.ay };
        this.orig = this.selectedOrig();
        return;
      }
      const hit = this.topObjectAt(ctx.pt);
      if (hit) {
        if (!A.selection.has(hit.id)) {
          if (ctx.shift) A.selection.add(hit.id);
          else A.selection = new Set([hit.id]);
        } else if (ctx.shift) {
          A.selection.delete(hit.id);
          if (!A.selection.size) { this.mode = 'idle'; return; }
        }
        this.mode = 'move';
        this.orig = this.selectedOrig();
        A.onSelectionChange && A.onSelectionChange();
        this.board.invalidateLive();
        return;
      }
      this.mode = 'marquee';
      this.marquee = { x: ctx.pt.x, y: ctx.pt.y, w: 0, h: 0 };
      if (!ctx.shift) { A.selection.clear(); A.onSelectionChange && A.onSelectionChange(); }
      this.board.invalidateLive();
    }

    selectedOrig() {
      return this.board.page.objects
        .filter((o) => app().selection.has(o.id))
        .map((o) => ({ id: o.id, data: M.cloneObject(o) }));
    }

    applyOrig() {
      // 把对象恢复成拖动前的状态（用于缩放时不累积误差）
      for (const rec of this.orig) {
        const target = this.board.page.objects.find((o) => o.id === rec.id);
        if (!target) continue;
        const src = rec.data;
        if (src.type === 'stroke') {
          target.pts = src.pts.map((p) => ({ x: p.x, y: p.y, p: p.p, t: p.t }));
          target.w = src.w;
        } else if (src.type === 'shape') {
          target.x1 = src.x1; target.y1 = src.y1; target.x2 = src.x2; target.y2 = src.y2;
          target.verts = src.verts ? src.verts.map((v) => ({ x: v.x, y: v.y })) : null;
          target.w = src.w;
        } else if (src.type === 'text') {
          target.x = src.x; target.y = src.y; target.size = src.size;
          M.reflowText(target);
        } else if (src.type === 'image') {
          target.x = src.x; target.y = src.y; target.w = src.w; target.h = src.h;
        }
        M.invalidate(target);
      }
    }

    move(ctx) {
      if (this.mode === 'move') {
        const dx = this.last.x - ctx.pt.x, dy = this.last.y - ctx.pt.y;
        if (Math.abs(ctx.pt.x - this.dragStart.x) > 1 || Math.abs(ctx.pt.y - this.dragStart.y) > 1) this.moved = true;
        const objs = this.board.page.objects.filter((o) => app().selection.has(o.id));
        for (const o of objs) M.translate(o, -dx, -dy);
        this.last = ctx.pt;
        this.board.staticDirty = true;
        this.board.invalidateLive();
      } else if (this.mode === 'scale') {
        const a = this.anchor;
        const sx = Math.abs(ctx.pt.x - a.x) / Math.max(1, Math.abs(this.dragStart.x - a.x));
        const sy = Math.abs(ctx.pt.y - a.y) / Math.max(1, Math.abs(this.dragStart.y - a.y));
        const s = ctx.shift ? Math.max(sx, sy) : 0;
        this.moved = true;
        this.applyOrig();
        const objs = this.board.page.objects.filter((o) => app().selection.has(o.id));
        for (const o of objs) M.scaleObject(o, a, ctx.shift ? s : sx, ctx.shift ? s : sy);
        this.board.staticDirty = true;
        this.board.invalidateLive();
      } else if (this.mode === 'marquee') {
        this.marquee = {
          x: Math.min(this.dragStart.x, ctx.pt.x), y: Math.min(this.dragStart.y, ctx.pt.y),
          w: Math.abs(ctx.pt.x - this.dragStart.x), h: Math.abs(ctx.pt.y - this.dragStart.y),
        };
        const picked = this.board.objectsInRect(this.marquee);
        if (ctx.shift) picked.forEach((o) => app().selection.add(o.id));
        else app().selection = new Set(picked.map((o) => o.id));
        app().onSelectionChange && app().onSelectionChange();
        this.board.invalidateLive();
      }
    }

    end() {
      if (this.mode === 'move' || this.mode === 'scale') {
        if (this.moved) app().commit(this.mode === 'move' ? '移动' : '缩放');
      }
      this.mode = 'idle';
      this.marquee = null;
      this.orig = null;
      this.board.invalidateAll();
    }
    cancel() {
      if (this.orig) this.applyOrig();
      this.mode = 'idle'; this.marquee = null; this.board.invalidateAll();
    }

    drawOverlay(ctx, board) {
      const s = 1 / board.view.scale;
      const A = app();

      if (this.marquee) {
        ctx.strokeStyle = '#0e9f76';
        ctx.fillStyle = 'rgba(14,159,118,0.10)';
        ctx.lineWidth = 1.5 * s;
        ctx.setLineDash([6 * s, 4 * s]);
        ctx.fillRect(this.marquee.x, this.marquee.y, this.marquee.w, this.marquee.h);
        ctx.strokeRect(this.marquee.x, this.marquee.y, this.marquee.w, this.marquee.h);
        ctx.setLineDash([]);
      }

      const b = this.selectionBounds();
      if (b) {
        const pad = 6 * s;
        ctx.strokeStyle = '#0e9f76';
        ctx.lineWidth = 1.4 * s;
        ctx.setLineDash([8 * s, 5 * s]);
        ctx.strokeRect(b.x - pad, b.y - pad, b.w + pad * 2, b.h + pad * 2);
        ctx.setLineDash([]);

        if (A.selection.size && this.mode !== 'move') {
          const hs = 5 * s;
          for (const h of this.handles()) {
            ctx.fillStyle = '#ffffff';
            ctx.strokeStyle = '#0e9f76';
            ctx.lineWidth = 1.6 * s;
            ctx.beginPath();
            ctx.rect(h.x - hs, h.y - hs, hs * 2, hs * 2);
            ctx.fill(); ctx.stroke();
          }
        } else if (this.mode === 'move' || A.selection.size) {
          const hs = 4 * s;
          ctx.fillStyle = '#0e9f76';
          for (const h of this.handles()) {
            ctx.beginPath(); ctx.rect(h.x - hs, h.y - hs, hs * 2, hs * 2); ctx.fill();
          }
        }
      }
    }
  }

  /* ------------------------------ 文本 ------------------------------ */
  class TextTool {
    constructor(board) { this.board = board; this.cursor = 'text'; }
    begin(ctx) {
      const hit = this.topText(ctx.pt);
      IB.app.openTextEditor(ctx.pt, hit);
    }
    hover() {}
    move() {}
    end() {}
    cancel() {}
    topText(pt) {
      const objs = this.board.page.objects;
      for (let i = objs.length - 1; i >= 0; i--) {
        if (objs[i].type === 'text' && M.hitTest(objs[i], pt, 2)) return objs[i];
      }
      return null;
    }
  }

  /* ------------------------------ 图片 ------------------------------ */
  class ImageTool {
    constructor(board) { this.board = board; this.cursor = 'copy'; }
    begin(ctx) { IB.app.insertImageAt(ctx.pt); }
    move() {} end() {} cancel() {}
  }

  /* ------------------------------ 激光笔 ------------------------------ */
  class LaserTool {
    constructor(board) {
      this.board = board;
      this.cursor = 'none';
      this.points = [];
      this.last = 0;
      this.active = false;
    }
    begin(ctx) { this.active = true; this.points = []; this.push(ctx); this.loop(); }
    move(ctx) { this.push(ctx); }
    end() { this.active = false; setTimeout(() => this.fadeOut(), 700); }
    cancel() { this.active = false; this.points = []; this.board.invalidateLive(); }
    push(ctx) {
      const t = performance.now();
      if (t - this.last < 8) return;
      this.last = t;
      this.points.push({ x: ctx.pt.x, y: ctx.pt.y, t });
      if (this.points.length > 60) this.points.shift();
      this.board.invalidateLive();
    }
    loop() {
      if (!this.board) return;
      this.board.invalidateLive();
      if (this.active) requestAnimationFrame(() => this.loop());
    }
    fadeOut() {
      const t0 = performance.now();
      const step = () => {
        const age = performance.now() - t0;
        this.board.invalidateLive();
        if (age > 900) { this.points = []; this.board.invalidateLive(); return; }
        requestAnimationFrame(step);
      };
      step();
    }
    drawOverlay(ctx, board) {
      if (!this.points.length) return;
      const now = performance.now();
      const v = board.view;
      ctx.setTransform(board.dpr, 0, 0, board.dpr, 0, 0);
      for (let i = 1; i < this.points.length; i++) {
        const a = this.points[i - 1], b = this.points[i];
        const age = now - b.t;
        const alpha = Math.max(0, 1 - age / 1400);
        const pa = { x: a.x * v.scale + v.tx, y: a.y * v.scale + v.ty };
        const pb = { x: b.x * v.scale + v.tx, y: b.y * v.scale + v.ty };
        ctx.strokeStyle = `rgba(239,68,68,${alpha})`;
        ctx.lineWidth = 3 + 4 * alpha;
        ctx.lineCap = 'round';
        ctx.beginPath(); ctx.moveTo(pa.x, pa.y); ctx.lineTo(pb.x, pb.y); ctx.stroke();
      }
      const lastP = this.points[this.points.length - 1];
      const s = board.toScreen(lastP.x, lastP.y);
      const g = ctx.createRadialGradient(s.x, s.y, 0, s.x, s.y, 16);
      g.addColorStop(0, 'rgba(255,120,120,0.95)');
      g.addColorStop(1, 'rgba(239,68,68,0)');
      ctx.fillStyle = g;
      ctx.beginPath(); ctx.arc(s.x, s.y, 16, 0, Math.PI * 2); ctx.fill();
    }
  }

  /* ------------------------------ 漫游 ------------------------------ */
  class HandTool {
    constructor(board) { this.board = board; this.cursor = 'grab'; this.drag = null; }
    begin(ctx) { this.drag = { x: ctx.screen.x, y: ctx.screen.y, tx: this.board.view.tx, ty: this.board.view.ty }; }
    move(ctx) {
      if (!this.drag) return;
      const v = this.board.view;
      v.tx = this.drag.tx + (ctx.screen.x - this.drag.x);
      v.ty = this.drag.ty + (ctx.screen.y - this.drag.y);
      this.board.invalidateAll(false);
    }
    end() { this.drag = null; }
    cancel() { this.drag = null; }
  }

  IB.tools = { pen: PenTool, marker: PenTool, eraser: EraserTool, shape: ShapeTool, select: SelectTool, text: TextTool, image: ImageTool, laser: LaserTool, hand: HandTool };
})();
