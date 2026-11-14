/* ==========================================================================
 * 20-model.js — 文档 / 页面 / 对象模型、命中测试、撤销栈
 * ========================================================================== */
(function () {
  'use strict';
  const IB = (window.IB = window.IB || {});
  const { uid, clamp } = IB.util;
  const geo = IB.geo;

  const measureCanvas = document.createElement('canvas');
  const measureCtx = measureCanvas.getContext('2d');

  const DEFAULT_DOC = {
    version: 1,
    name: '未命名白板',
    pageW: 1920,
    pageH: 1080,
    pages: [],
    current: 0,
  };

  function createPage(opts) {
    const o = opts || {};
    return {
      id: uid('p'),
      bg: o.bg || 'plain',       // plain | grid | dots | lines | black
      color: o.color || '#ffffff',
      objects: [],
    };
  }

  function createDoc(opts) {
    const o = opts || {};
    const doc = Object.assign({}, DEFAULT_DOC, o);
    doc.id = uid('d');
    doc.pages = o.pages || [createPage(), createPage()];
    doc.current = 0;
    return doc;
  }

  /* ---------------------------- 对象工厂 ---------------------------- */
  const obj = {
    stroke(pts, style) {
      return {
        id: uid('s'), type: 'stroke', pts,
        w: style.w || 4, color: style.color || '#1f2329',
        alpha: style.alpha != null ? style.alpha : 1,
        tool: style.tool || 'pen',
      };
    },
    shape(kind, x1, y1, x2, y2, style) {
      return {
        id: uid('g'), type: 'shape', shape: kind,
        x1, y1, x2, y2,
        w: style.w || 3, color: style.color || '#1f2329',
        fill: style.fill != null ? style.fill : null,
        alpha: style.alpha != null ? style.alpha : 1,
        expr: style.expr || null,
        verts: style.verts || null,
        dash: style.dash || null,
      };
    },
    text(x, y, text, style) {
      const size = style.size || 40;
      const t = {
        id: uid('t'), type: 'text', x, y, text,
        size, color: style.color || '#1f2329',
        bold: !!style.bold, italic: !!style.italic, align: 'left',
        w: 10, h: size * 1.3,
      };
      reflowText(t);
      return t;
    },
    image(x, y, w, h, src) {
      return { id: uid('i'), type: 'image', x, y, w, h, src, alpha: 1 };
    },
  };

  function textFont(t) {
    return (t.italic ? 'italic ' : '') + (t.bold ? '700 ' : '') + t.size + 'px "PingFang SC","Microsoft YaHei",system-ui,sans-serif';
  }

  function reflowText(t) {
    measureCtx.font = textFont(t);
    const lines = String(t.text || '').split('\n');
    let maxW = 0;
    for (const line of lines) maxW = Math.max(maxW, measureCtx.measureText(line).width);
    t.w = Math.max(8, maxW);
    t.h = lines.length * t.size * 1.3;
  }

  /* ---------------------------- 几何缓存 ---------------------------- */
  function invalidate(o) { o._geo = null; }

  function shapeBBox(o) {
    return {
      x: Math.min(o.x1, o.x2), y: Math.min(o.y1, o.y2),
      w: Math.abs(o.x2 - o.x1), h: Math.abs(o.y2 - o.y1),
    };
  }

  function shapePath(o) {
    const bb = shapeBBox(o);
    const p = new Path2D();
    const kind = o.shape;
    if (kind === 'line' || kind === 'arrow') {
      p.moveTo(o.x1, o.y1); p.lineTo(o.x2, o.y2);
    } else if (kind === 'rect') {
      p.rect(bb.x, bb.y, bb.w, bb.h);
    } else if (kind === 'ellipse') {
      p.ellipse(bb.x + bb.w / 2, bb.y + bb.h / 2, Math.max(1, bb.w / 2), Math.max(1, bb.h / 2), 0, 0, Math.PI * 2);
    } else if (kind === 'triangle') {
      p.moveTo(bb.x + bb.w / 2, bb.y); p.lineTo(bb.x + bb.w, bb.y + bb.h); p.lineTo(bb.x, bb.y + bb.h); p.closePath();
    } else if (kind === 'polygon' && o.verts && o.verts.length > 2) {
      p.moveTo(o.verts[0].x, o.verts[0].y);
      for (let i = 1; i < o.verts.length; i++) p.lineTo(o.verts[i].x, o.verts[i].y);
      p.closePath();
    } else if (kind === 'func') {
      const pts = funcPoints(o);
      if (pts.length) {
        p.moveTo(pts[0].x, pts[0].y);
        for (let i = 1; i < pts.length; i++) p.lineTo(pts[i].x, pts[i].y);
      }
    } else if (kind === 'axis') {
      // 坐标系：外框 + 两轴 + 刻度
      const ox = o.x1 + (o.x2 - o.x1) / 2, oy = o.y1 + (o.y2 - o.y1) / 2;
      p.moveTo(o.x1, oy); p.lineTo(o.x2, oy);
      p.moveTo(ox, o.y1); p.lineTo(ox, o.y2);
    }
    return p;
  }

  function funcPoints(o) {
    const bb = shapeBBox(o);
    const pts = [];
    const N = 260;
    const cx = (o.x1 + o.x2) / 2;
    const cy = (o.y1 + o.y2) / 2;
    // 与坐标系保持一致：bbox 内按 10 个单位（±5）映射，取短边为准
    const unit = Math.max(1, Math.min(bb.w, bb.h) / 10);
    for (let i = 0; i <= N; i++) {
      const xv = -5 + (10 * i) / N;
      const yv = geo.evalExpr(o.expr || 'x', xv);
      if (!isFinite(yv)) continue;
      pts.push({ x: cx + xv * unit, y: cy - yv * unit });
    }
    return pts;
  }

  function objBBox(o) {
    if (o._geo && o._geo.bbox) return expand(o._geo.bbox, o);
    if (o.type === 'text') {
      return { x: o.x, y: o.y, w: o.w, h: o.h };
    }
    if (o.type === 'image') {
      return { x: o.x, y: o.y, w: o.w, h: o.h };
    }
    if (o.type === 'stroke') {
      if (!o._geo) o._geo = geo.strokeGeometry(o.pts, o.w, { flat: o.tool === 'marker' });
      return o._geo.bbox;
    }
    if (o.type === 'shape') {
      const bb = shapeBBox(o);
      return expand(bb, o);
    }
    return { x: 0, y: 0, w: 0, h: 0 };
  }

  function expand(bb, o) {
    const pad = ((o.w || 2) / 2) + 2;
    return { x: bb.x - pad, y: bb.y - pad, w: bb.w + pad * 2, h: bb.h + pad * 2 };
  }

  function bboxOfObjects(objects) {
    if (!objects.length) return null;
    let minX = Infinity, minY = Infinity, maxX = -Infinity, maxY = -Infinity;
    for (const o of objects) {
      const b = objBBox(o);
      minX = Math.min(minX, b.x); minY = Math.min(minY, b.y);
      maxX = Math.max(maxX, b.x + b.w); maxY = Math.max(maxY, b.y + b.h);
    }
    return { x: minX, y: minY, w: maxX - minX, h: maxY - minY };
  }

  function hitTest(o, pt, tol) {
    tol = tol || 6;
    const b = objBBox(o);
    if (pt.x < b.x - tol || pt.x > b.x + b.w + tol || pt.y < b.y - tol || pt.y > b.y + b.h + tol) return false;

    if (o.type === 'stroke') {
      if (!o._hitCtx) o._hitCtx = measureCtx;
      measureCtx.save();
      measureCtx.setTransform(1, 0, 0, 1, 0, 0);
      measureCtx.lineWidth = Math.max(o.w * 1.4, 10);
      measureCtx.lineCap = 'round';
      const hitPath = o._geo ? o._geo.hit : geo.strokeGeometry(o.pts, o.w, { flat: o.tool === 'marker' }).hit;
      const res = measureCtx.isPointInStroke(hitPath, pt.x, pt.y);
      measureCtx.restore();
      return res;
    }
    if (o.type === 'shape') {
      const p = shapePath(o);
      if (o.shape === 'axis' || o.shape === 'func' || o.shape === 'line' || o.shape === 'arrow') {
        measureCtx.save();
        measureCtx.setTransform(1, 0, 0, 1, 0, 0);
        measureCtx.lineWidth = Math.max(o.w * 1.5, 12);
        measureCtx.lineCap = 'round';
        const res = measureCtx.isPointInStroke(p, pt.x, pt.y);
        measureCtx.restore();
        return res;
      }
      return measureCtx.isPointInPath(p, pt.x, pt.y) || edgeHit(o, pt, tol);
    }
    return true; // 文本 / 图片：盒子命中
  }

  function edgeHit(o, pt, tol) {
    const bb = shapeBBox(o);
    const corners = [
      { x: bb.x, y: bb.y }, { x: bb.x + bb.w, y: bb.y },
      { x: bb.x + bb.w, y: bb.y + bb.h }, { x: bb.x, y: bb.y + bb.h },
    ];
    for (let i = 0; i < 4; i++) {
      const a = corners[i], b = corners[(i + 1) % 4];
      if (geo.pointSegmentDistance(pt.x, pt.y, a.x, a.y, b.x, b.y) < tol + o.w) return true;
    }
    return false;
  }

  /* ---------------------------- 变换 ---------------------------- */
  function translate(o, dx, dy) {
    if (o.type === 'stroke') {
      for (const p of o.pts) { p.x += dx; p.y += dy; }
      invalidate(o);
    } else {
      if (o.type === 'shape') { o.x1 += dx; o.x2 += dx; o.y1 += dy; o.y2 += dy; if (o.verts) o.verts.forEach((v) => { v.x += dx; v.y += dy; }); }
      else { o.x += dx; o.y += dy; }
      invalidate(o);
    }
  }

  /** 以 origin 为基点缩放对象（用于选择框缩放） */
  function scaleObject(o, origin, sx, sy) {
    const map = (p) => ({ x: origin.x + (p.x - origin.x) * sx, y: origin.y + (p.y - origin.y) * sy });
    if (o.type === 'stroke') {
      o.pts = o.pts.map((p) => Object.assign(map(p), { p: p.p, t: p.t }));
      o.w = Math.max(0.8, o.w * Math.abs((sx + sy) / 2));
      invalidate(o);
    } else if (o.type === 'shape') {
      const a = map({ x: o.x1, y: o.y1 }), b = map({ x: o.x2, y: o.y2 });
      o.x1 = a.x; o.y1 = a.y; o.x2 = b.x; o.y2 = b.y;
      if (o.verts) o.verts = o.verts.map(map);
      o.w = Math.max(0.8, o.w * Math.abs((sx + sy) / 2));
      invalidate(o);
    } else if (o.type === 'text') {
      const a = map({ x: o.x, y: o.y });
      o.x = a.x; o.y = a.y;
      o.size = Math.max(8, o.size * Math.abs(sy));
      reflowText(o);
    } else if (o.type === 'image') {
      const a = map({ x: o.x, y: o.y });
      o.x = a.x; o.y = a.y; o.w = Math.max(8, o.w * Math.abs(sx)); o.h = Math.max(8, o.h * Math.abs(sy));
    }
  }

  function cloneObject(o) {
    const c = JSON.parse(JSON.stringify(o));
    c.id = uid(o.type === 'stroke' ? 's' : o.type === 'shape' ? 'g' : o.type === 'text' ? 't' : 'i');
    delete c._geo; delete c._hitCtx;
    return c;
  }

  /* ---------------------------- 序列化 ---------------------------- */
  function serializeDoc(doc) {
    return JSON.stringify({
      version: 1, id: doc.id, name: doc.name, pageW: doc.pageW, pageH: doc.pageH,
      current: doc.current,
      pages: doc.pages.map((p) => ({ id: p.id, bg: p.bg, color: p.color, objects: p.objects.map(strip) })),
    });
  }
  function strip(o) {
    const c = Object.assign({}, o);
    delete c._geo; delete c._hitCtx;
    return c;
  }

  function deserializeDoc(json) {
    const raw = typeof json === 'string' ? JSON.parse(json) : json;
    if (!raw || !Array.isArray(raw.pages)) return null;
    const doc = {
      version: raw.version || 1, id: raw.id || uid('d'), name: raw.name || '未命名白板',
      pageW: raw.pageW || 1920, pageH: raw.pageH || 1080, current: raw.current || 0,
      pages: raw.pages.map((p) => ({
        id: p.id || uid('p'), bg: p.bg || 'plain', color: p.color || '#ffffff',
        objects: (p.objects || []).map((o) => { const c = Object.assign({}, o); delete c._geo; return c; }),
      })),
    };
    doc.current = clamp(doc.current, 0, doc.pages.length - 1);
    return doc;
  }

  /* ---------------------------- 撤销栈 ---------------------------- */
  class History {
    constructor(limit = 60) { this.limit = limit; this.stack = []; this.index = -1; }
    reset(snapshot) { this.stack = [snapshot]; this.index = 0; }
    push(snapshot) {
      if (this.index >= 0 && this.stack[this.index] === snapshot) return;
      this.stack.splice(this.index + 1);
      this.stack.push(snapshot);
      if (this.stack.length > this.limit) this.stack.shift();
      this.index = this.stack.length - 1;
    }
    get canUndo() { return this.index > 0; }
    get canRedo() { return this.index < this.stack.length - 1; }
    undo() { if (!this.canUndo) return null; return this.stack[--this.index]; }
    redo() { if (!this.canRedo) return null; return this.stack[++this.index]; }
  }

  IB.model = {
    createDoc, createPage, obj, invalidate, objBBox, bboxOfObjects, hitTest,
    translate, scaleObject, cloneObject, serializeDoc, deserializeDoc, reflowText, textFont, shapePath, History,
  };
})();
