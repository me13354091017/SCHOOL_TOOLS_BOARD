using System;
using System.Collections.Generic;
using System.Windows;

namespace InkBoard
{
    /// <summary>手绘转标准图形：抬笔时判断这笔画是不是直线/三角形/矩形/圆</summary>
    public static class Recognize
    {
        /// <summary>识别成功返回标准图形对象，否则返回 null</summary>
        public static Obj Try(Obj stroke)
        {
            var pts = stroke.Pts;
            if (pts == null || pts.Count < 6) return null;

            double len = PathLen(pts);
            if (len < 60) return null;

            var a = pts[0];
            var b = pts[pts.Count - 1];
            double chord = Dist(a, b);
            double dev = MaxDev(pts, a, b);

            /* ---- 直线：弦长接近路径长，且几乎不弯 ---- */
            if (chord > 40 && chord / len > 0.92 && dev / chord < 0.055)
                return Make(stroke, "line", a.X, a.Y, b.X, b.Y);

            /* ---- 闭合判断 ---- */
            if (chord > 0.18 * len) return null;      // 没闭合
            if (len < 90) return null;

            double area = Math.Abs(Shoelace(pts));
            double circ = 4 * Math.PI * area / (len * len);   // 1.0 = 正圆
            var bb = BBox(pts);
            if (bb.Width < 20 || bb.Height < 20) return null;
            double aspect = Math.Max(bb.Width, bb.Height) / Math.Min(bb.Width, bb.Height);

            /* ---- 圆 / 椭圆 ---- */
            if (circ > 0.82 && aspect < 2.2)
                return Make(stroke, "ellipse", bb.Left, bb.Top, bb.Right, bb.Bottom);

            /* ---- 多边形 ---- */
            var v = Rdp(pts, Math.Max(6, len * 0.02));
            while (v.Count > 3 && Dist(v[0], v[v.Count - 1]) < 14) v.RemoveAt(v.Count - 1);
            int n = v.Count;

            if (n == 3 && area > 200)
                return Make(stroke, "triangle", bb.Left, bb.Top, bb.Right, bb.Bottom);

            if (n == 4 && area > 200)
                return Make(stroke, "rect", bb.Left, bb.Top, bb.Right, bb.Bottom);

            if (n >= 5 && n <= 7 && circ > 0.6 && aspect < 1.9)
                return Make(stroke, "ellipse", bb.Left, bb.Top, bb.Right, bb.Bottom);

            return null;
        }

        static Obj Make(Obj src, string shape, double x1, double y1, double x2, double y2)
        {
            var o = new Obj
            {
                Type = "shape",
                Shape = shape,
                Color = src.Color,
                W = src.W,
                Alpha = src.Alpha,
                X1 = x1, Y1 = y1, X2 = x2, Y2 = y2,
                Fill = false
            };
            o.Geo = null;
            return o;
        }

        /* ------------------------------ 基础几何 ------------------------------ */
        static double Dist(Point a, Point b) { return (a - b).Length; }

        static double PathLen(List<Point> pts)
        {
            double s = 0;
            for (int i = 1; i < pts.Count; i++) s += Dist(pts[i - 1], pts[i]);
            return s;
        }

        static double MaxDev(List<Point> pts, Point a, Point b)
        {
            double m = 0;
            for (int i = 1; i < pts.Count - 1; i++)
            {
                double d = PerpDist(pts[i], a, b);
                if (d > m) m = d;
            }
            return m;
        }

        public static double PerpDist(Point p, Point a, Point b)
        {
            double dx = b.X - a.X, dy = b.Y - a.Y;
            double l2 = dx * dx + dy * dy;
            if (l2 < 1e-9) return Dist(p, a);
            double t = ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / l2;
            if (t < 0) return Dist(p, a);
            if (t > 1) return Dist(p, b);
            return Dist(p, new Point(a.X + t * dx, a.Y + t * dy));
        }

        static double Shoelace(List<Point> pts)
        {
            double s = 0;
            for (int i = 0; i < pts.Count; i++)
            {
                var p = pts[i];
                var q = pts[(i + 1) % pts.Count];
                s += p.X * q.Y - q.X * p.Y;
            }
            return s / 2;
        }

        static Rect BBox(List<Point> pts)
        {
            double x0 = double.MaxValue, y0 = double.MaxValue, x1 = -double.MaxValue, y1 = -double.MaxValue;
            foreach (var p in pts)
            {
                if (p.X < x0) x0 = p.X;
                if (p.X > x1) x1 = p.X;
                if (p.Y < y0) y0 = p.Y;
                if (p.Y > y1) y1 = p.Y;
            }
            return new Rect(x0, y0, x1 - x0, y1 - y0);
        }

        /// <summary>Douglas-Peucker 简化（迭代版，避免深递归）</summary>
        public static List<Point> Rdp(List<Point> pts, double eps)
        {
            if (pts.Count < 3) return new List<Point>(pts);
            var keep = new bool[pts.Count];
            keep[0] = true;
            keep[pts.Count - 1] = true;

            var stack = new Stack<int[]>();
            stack.Push(new[] { 0, pts.Count - 1 });
            while (stack.Count > 0)
            {
                var seg = stack.Pop();
                int i0 = seg[0], i1 = seg[1];
                double maxD = -1;
                int idx = -1;
                for (int i = i0 + 1; i < i1; i++)
                {
                    double d = PerpDist(pts[i], pts[i0], pts[i1]);
                    if (d > maxD) { maxD = d; idx = i; }
                }
                if (maxD > eps && idx > 0)
                {
                    keep[idx] = true;
                    stack.Push(new[] { i0, idx });
                    stack.Push(new[] { idx, i1 });
                }
            }

            var res = new List<Point>();
            for (int i = 0; i < pts.Count; i++) if (keep[i]) res.Add(pts[i]);
            return res;
        }
    }
}
