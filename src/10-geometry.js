/* ==========================================================================
 * 10-geometry.js — 笔迹几何（时间笔锋 / 可变宽度轮廓）、图形识别、表达式解析
 * ========================================================================== */
(function () {
  'use strict';
  const IB = (window.IB = window.IB || {});

  const clamp = (v, a, b) => (v < a ? a : v > b ? b : v);

  /* ----------------------------------------------------------------------
   * 可变宽度笔迹轮廓：把中心线 + 每点宽度 变成一条闭合填充路径
   * 时间笔锋：速度越快越细、压感越强越粗，起收笔收峰
   * -------------------------------------------------------------------- */
  function strokeGeometry(pts, baseW, opts) {
    opts = opts || {};
    const n = pts.length;
    const flat = !!opts.flat;           // 荧光笔：等宽、平头
    if (n === 0) return null;

    // 单点：画一个圆点
    if (n === 1) {
      const r = Math.max(0.6, baseW / 2);
      const p = new Path2D();
      p.arc(pts[0].x, pts[0].y, r, 0, Math.PI * 2);
      return { path: p, hit: p, bbox: { x: pts[0].x - r, y: pts[0].y - r, w: r * 2, h: r * 2 }, width: baseW };
    }

    // ---- 0a. 重采样：快写时采样点稀疏，按 4px 间距补齐，避免轮廓折角 ----
    let dense = pts;
    {
      const out = [pts[0]];
      for (let i = 1; i < n; i++) {
        const a = pts[i - 1], b = pts[i];
        const d = Math.hypot(b.x - a.x, b.y - a.y);
        const steps = Math.min(28, Math.floor(d / 4));
        for (let k = 1; k < steps; k++) {
          const u = k / steps;
          out.push({ x: a.x + (b.x - a.x) * u, y: a.y + (b.y - a.y) * u, p: (a.p || 0.5) + ((b.p || 0.5) - (a.p || 0.5)) * u, t: (a.t || 0) + ((b.t || 0) - (a.t || 0)) * u });
        }
        out.push(b);
      }
      dense = out;
    }

    // ---- 0b. 中心线平滑（抑制高速采样的折角，避免轮廓自交鼓起） ----
    let sp = dense;
    const nd = dense.length;
    if (nd > 6) {
      const base = dense;
      sp = base.map((p, i) => {
        if (i === 0 || i === nd - 1) return p;
        const a = base[i - 1], b = base[i + 1];
        return { x: (a.x + p.x * 2 + b.x) / 4, y: (a.y + p.y * 2 + b.y) / 4, p: p.p, t: p.t };
      });
    }
    const n2 = sp.length;

    // ---- 1. 每点宽度 ----
    const w = new Float32Array(n2);
    const V_REF = 5.6;  // 参考速度：世界像素 / 毫秒
    const WMIN = baseW * 0.55, WMAX = baseW * 1.12;
    for (let i = 0; i < n2; i++) {
      const p = sp[i];
      let speed = 0;
      if (i > 0) {
        const dt = Math.max(1, (p.t || 0) - (sp[i - 1].t || 0));
        speed = Math.hypot(p.x - sp[i - 1].x, p.y - sp[i - 1].y) / dt;
      }
      let ww;
      if (flat) {
        ww = baseW;                       // 荧光笔等宽
      } else {
        const s = clamp(speed / V_REF, 0, 1);
        // 慢写厚重、快写轻细（0.60 ~ 1.0 倍）
        ww = baseW * (0.60 + 0.40 * (1 - Math.pow(s, 0.7)));
        const pres = p.p;
        if (pres != null && pres !== 0.5) ww *= 0.82 + 0.34 * clamp(pres * 1.2, 0, 1);
        ww = clamp(ww, WMIN, WMAX);
      }
      w[i] = ww;
    }

    // ---- 2. 宽度平滑（避免毛刺） ----
    if (n2 > 4) {
      const tmp = Float32Array.from(w);
      for (let i = 0; i < n2; i++) {
        const a = tmp[Math.max(0, i - 2)], b = tmp[Math.max(0, i - 1)];
        const c = tmp[i];
        const d = tmp[Math.min(n2 - 1, i + 1)], e = tmp[Math.min(n2 - 1, i + 2)];
        w[i] = (a + b * 2 + c * 3 + d * 2 + e) / 8;
      }
    }

    // ---- 3. 起收笔收峰（时间笔锋） ----
    if (!flat && n2 >= 4) {
      // 起笔：迅速铺开；收笔：收成笔锋
      const inF = [0.42, 0.72, 0.9];
      const outF = [0.88, 0.66, 0.42, 0.24, 0.12];
      for (let i = 0; i < Math.min(inF.length, n2); i++) w[i] *= inF[i];
      for (let i = 0; i < Math.min(outF.length, n2); i++) w[n2 - 1 - i] *= outF[i];
    }

    // ---- 4. 生成左右轮廓 ----
    const L = new Float32Array(n2 * 2), R = new Float32Array(n2 * 2);
    let minX = Infinity, minY = Infinity, maxX = -Infinity, maxY = -Infinity;
    for (let i = 0; i < n2; i++) {
      // 用 ±2 邻域估方向，转角处更稳定
      const prev = sp[Math.max(0, i - 2)];
      const next = sp[Math.min(n2 - 1, i + 2)];
      let dx = next.x - prev.x, dy = next.y - prev.y;
      const len = Math.hypot(dx, dy) || 1;
      dx /= len; dy /= len;
      // 急转弯处收窄，避免轮廓自交形成墨团
      let hw = Math.max(0.35, w[i] / 2);
      if (i > 1 && i < n2 - 2) {
        const ax = sp[i].x - sp[i - 1].x, ay = sp[i].y - sp[i - 1].y;
        const bx = sp[i + 1].x - sp[i].x, by = sp[i + 1].y - sp[i].y;
        const la = Math.hypot(ax, ay) || 1, lb = Math.hypot(bx, by) || 1;
        const cosA = (ax * bx + ay * by) / (la * lb);
        if (cosA < 0.25) hw *= 0.62 + 0.38 * (cosA + 1) / 1.25;
      }
      const nx = -dy, ny = dx;
      const lx = sp[i].x + nx * hw, ly = sp[i].y + ny * hw;
      const rx = sp[i].x - nx * hw, ry = sp[i].y - ny * hw;
      L[i * 2] = lx; L[i * 2 + 1] = ly;
      R[i * 2] = rx; R[i * 2 + 1] = ry;
      if (lx < minX) minX = lx; if (lx > maxX) maxX = lx;
      if (ly < minY) minY = ly; if (ly > maxY) maxY = ly;
      if (rx < minX) minX = rx; if (rx > maxX) maxX = rx;
      if (ry < minY) minY = ry; if (ry > maxY) maxY = ry;
    }

    const path = new Path2D();
    path.moveTo(L[0], L[1]);
    for (let i = 1; i < n2 - 1; i++) {
      const mx = (L[i * 2] + L[i * 2 + 2]) / 2, my = (L[i * 2 + 1] + L[i * 2 + 3]) / 2;
      path.quadraticCurveTo(L[i * 2], L[i * 2 + 1], mx, my);
    }
    path.lineTo(L[(n2 - 1) * 2], L[(n2 - 1) * 2 + 1]);
    path.lineTo(R[(n2 - 1) * 2], R[(n2 - 1) * 2 + 1]);
    for (let i = n2 - 1; i > 0; i--) {
      const mx = (R[i * 2] + R[(i - 1) * 2]) / 2, my = (R[i * 2 + 1] + R[(i - 1) * 2 + 1]) / 2;
      path.quadraticCurveTo(R[i * 2], R[i * 2 + 1], mx, my);
    }
    path.closePath();

    // 命中路径（中心线，配合 isPointInStroke 使用）
    const hit = centerPath(sp);

    return {
      path, hit,
      bbox: { x: minX, y: minY, w: maxX - minX, h: maxY - minY },
      width: WMAX,
    };
  }

  function centerPath(pts) {
    const n = pts.length;
    const p = new Path2D();
    if (n === 1) { p.arc(pts[0].x, pts[0].y, 1, 0, Math.PI * 2); return p; }
    p.moveTo(pts[0].x, pts[0].y);
    for (let i = 1; i < n - 1; i++) {
      const mx = (pts[i].x + pts[i + 1].x) / 2, my = (pts[i].y + pts[i + 1].y) / 2;
      p.quadraticCurveTo(pts[i].x, pts[i].y, mx, my);
    }
    p.lineTo(pts[n - 1].x, pts[n - 1].y);
    return p;
  }

  /* ----------------------------------------------------------------------
   * 点抽稀（RDP）：笔迹落笔时压缩，减少存储与绘制开销
   * -------------------------------------------------------------------- */
  function rdp(points, eps) {
    if (points.length <= 2) return points.slice();
    const sqEps = eps * eps;
    const keep = new Uint8Array(points.length);
    keep[0] = keep[points.length - 1] = 1;

    function segDistSq(p, a, b) {
      let x = a.x, y = a.y, dx = b.x - x, dy = b.y - y;
      if (dx !== 0 || dy !== 0) {
        const t = ((p.x - x) * dx + (p.y - y) * dy) / (dx * dx + dy * dy);
        if (t > 1) { x = b.x; y = b.y; } else if (t > 0) { x += dx * t; y += dy * t; }
      }
      dx = p.x - x; dy = p.y - y;
      return dx * dx + dy * dy;
    }

    const stack = [[0, points.length - 1]];
    while (stack.length) {
      const [s, e] = stack.pop();
      let maxD = -1, idx = -1;
      for (let i = s + 1; i < e; i++) {
        const d = segDistSq(points[i], points[s], points[e]);
        if (d > maxD) { maxD = d; idx = i; }
      }
      if (maxD > sqEps && idx > 0) {
        keep[idx] = 1;
        stack.push([s, idx], [idx, e]);
      }
    }
    const out = [];
    for (let i = 0; i < points.length; i++) if (keep[i]) out.push(points[i]);
    return out;
  }

  /* ----------------------------------------------------------------------
   * 手绘 → 标准图形
   * -------------------------------------------------------------------- */
  function pathLength(pts) {
    let len = 0;
    for (let i = 1; i < pts.length; i++) len += Math.hypot(pts[i].x - pts[i - 1].x, pts[i].y - pts[i - 1].y);
    return len;
  }

  function bboxOf(pts) {
    let minX = Infinity, minY = Infinity, maxX = -Infinity, maxY = -Infinity;
    for (const p of pts) {
      if (p.x < minX) minX = p.x; if (p.x > maxX) maxX = p.x;
      if (p.y < minY) minY = p.y; if (p.y > maxY) maxY = p.y;
    }
    return { x: minX, y: minY, w: maxX - minX, h: maxY - minY };
  }

  function polygonArea(pts) {
    let a = 0;
    for (let i = 0, j = pts.length - 1; i < pts.length; j = i++) {
      a += (pts[j].x + pts[i].x) * (pts[j].y - pts[i].y);
    }
    return Math.abs(a / 2);
  }

  function cornersOf(pts) {
    // 用抽稀 + 角度判断拐角数量
    const s = rdp(pts, Math.max(4, pathLength(pts) / 40));
    const verts = s.length > 2 && distLite(s[0], s[s.length - 1]) < 12 ? s.slice(0, -1) : s;
    return verts;
  }

  function distLite(a, b) { return Math.hypot(a.x - b.x, a.y - b.y); }

  function recognize(pts, pageRect) {
    if (!pts || pts.length < 3) return null;
    const len = pathLength(pts);
    if (len < 26) return null;
    const first = pts[0], last = pts[pts.length - 1];
    const chord = distLite(first, last);
    const straightness = chord / len;
    const closed = chord < Math.max(26, len * 0.09);
    const bb = bboxOf(pts);
    if (bb.w < 8 || bb.h < 8) return null;

    // 直线
    if (!closed && straightness > 0.86) {
      return { shape: 'line', x1: first.x, y1: first.y, x2: last.x, y2: last.y, confidence: straightness };
    }

    if (!closed || len < 70) return null;

    // 圆 / 椭圆：各点到质心的距离稳定
    let cx = 0, cy = 0;
    for (const p of pts) { cx += p.x; cy += p.y; }
    cx /= pts.length; cy /= pts.length;
    const radii = pts.map((p) => Math.hypot(p.x - cx, p.y - cy));
    const meanR = radii.reduce((a, b) => a + b, 0) / radii.length;
    const sd = Math.sqrt(radii.reduce((a, r) => a + (r - meanR) * (r - meanR), 0) / radii.length);
    const circularity = sd / (meanR || 1);

    const verts = cornersOf(pts);
    const area = polygonArea(verts);
    const fillRatio = area / Math.max(1, bb.w * bb.h);

    // 三角形
    if (verts.length === 3 && fillRatio > 0.35 && fillRatio < 0.68) {
      return { shape: 'triangle', x1: bb.x, y1: bb.y, x2: bb.x + bb.w, y2: bb.y + bb.h, confidence: 0.8 };
    }
    // 矩形 / 四边形
    if (verts.length === 4 && fillRatio > 0.62) {
      return { shape: 'rect', x1: bb.x, y1: bb.y, x2: bb.x + bb.w, y2: bb.y + bb.h, confidence: 0.85 };
    }
    // 椭圆
    if (circularity < 0.16) {
      return { shape: 'ellipse', x1: bb.x, y1: bb.y, x2: bb.x + bb.w, y2: bb.y + bb.h, confidence: 0.9 };
    }
    // 多角形兜底
    if (verts.length >= 5 && verts.length <= 8 && fillRatio > 0.25) {
      return { shape: 'polygon', x1: bb.x, y1: bb.y, x2: bb.x + bb.w, y2: bb.y + bb.h, verts: verts.map((v) => ({ x: v.x, y: v.y })), confidence: 0.5 };
    }
    return null;
  }

  /* ----------------------------------------------------------------------
   * 点 / 线段距离
   * -------------------------------------------------------------------- */
  function pointSegmentDistance(px, py, ax, ay, bx, by) {
    const dx = bx - ax, dy = by - ay;
    const l2 = dx * dx + dy * dy;
    let t = l2 ? ((px - ax) * dx + (py - ay) * dy) / l2 : 0;
    t = clamp(t, 0, 1);
    return Math.hypot(px - (ax + t * dx), py - (ay + t * dy));
  }

  /* ----------------------------------------------------------------------
   * 极简数学表达式解析器（函数图像用，不使用 eval）
   * -------------------------------------------------------------------- */
    const FUNCS = {
    sin: Math.sin, cos: Math.cos, tan: Math.tan, asin: Math.asin, acos: Math.acos, atan: Math.atan,
    sqrt: Math.sqrt, abs: Math.abs, log: Math.log10, ln: Math.log, exp: Math.exp,
    floor: Math.floor, ceil: Math.ceil, round: Math.round, sign: Math.sign,
  };

  function evalExpr(src, x) {
    let i = 0;
    const s = String(src || '').replace(/\s+/g, '').replace(/π/g, 'pi').toLowerCase();
    if (!s) return NaN;

    function peek() { return s[i]; }
    function eat(ch) { if (s[i] === ch) { i++; return true; } return false; }

    function parseExpr() {
      let v = parseTerm();
      while (peek() === '+' || peek() === '-') {
        const op = s[i++];
        const r = parseTerm();
        v = op === '+' ? v + r : v - r;
      }
      return v;
    }
    function parseTerm() {
      let v = parseUnary();
      while (peek() === '*' || peek() === '/' || peek() === '%') {
        const op = s[i++];
        const r = parseUnary();
        v = op === '*' ? v * r : op === '/' ? v / r : v % r;
      }
      return v;
    }
    function parseUnary() {
      if (eat('-')) return -parseUnary();
      if (eat('+')) return parseUnary();
      return parsePower();
    }
    function parsePower() {
      const base = parseAtom();
      if (peek() === '^') { i++; return Math.pow(base, parseUnary()); }
      return base;
    }
    function parseAtom() {
      const ch = peek();
      if (ch === '(') { i++; const v = parseExpr(); eat(')'); return v; }
      const num = /^(\d+(\.\d+)?|\.\d+)/.exec(s.slice(i));
      if (num) { i += num[0].length; let v = parseFloat(num[0]); if (peek() === 'x' || peek() === 'π') { /* 隐式乘 */ } return v; }
      const id = /^[a-z]+/.exec(s.slice(i));
      if (id) {
        const name = id[0];
        i += name.length;
        if (name === 'x') return x;
        if (name === 'pi') return Math.PI;
        if (name === 'e') return Math.E;
        if (FUNCS[name]) {
          if (eat('(')) { const v = parseExpr(); eat(')'); return FUNCS[name](v); }
          return FUNCS[name](parseUnary());
        }
        throw new Error('unknown ident ' + name);
      }
      throw new Error('bad token');
    }

    try {
      const v = parseExpr();
      return i >= s.length - 1 ? v : NaN;
    } catch (e) { return NaN; }
  }

  IB.geo = { strokeGeometry, centerPath, rdp, recognize, bboxOf, pathLength, polygonArea, pointSegmentDistance, evalExpr };
})();
