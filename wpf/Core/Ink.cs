using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace InkBoard
{
    /// <summary>
    /// 笔迹几何：重采样 → 速度定宽 → 起收笔时间包络（笔锋）→ 法线轮廓 → 可填充 Path
    /// </summary>
    public static class Ink
    {
        public const double Spacing = 4.0;

        public static void Build(Obj o)
        {
            if (o.Pts == null || o.Pts.Count == 0) { o.Geo = null; return; }
            List<Point> pts;
            List<double> ws;
            Resample(o.Pts, o.W, o.Marker, out pts, out ws);
            o.Geo = Outline(pts, ws);
        }

        /// <summary>按固定间距重采样，并按“原始点间距”（≈运笔速度）决定该处宽度</summary>
        static void Resample(List<Point> src, double baseW, bool marker, out List<Point> outPts, out List<double> outW)
        {
            outPts = new List<Point>();
            outW = new List<double>();
            if (src.Count == 1)
            {
                outPts.Add(src[0]);
                outW.Add(marker ? baseW * 1.6 : baseW);
                return;
            }

            double acc = 0;
            for (int i = 1; i < src.Count; i++)
            {
                var a = src[i - 1];
                var b = src[i];
                double len = Math.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y));
                if (len < 0.001) continue;
                double gap = len;                       // 采样间隔越大 = 运笔越快
                if (i == 1) { outPts.Add(a); outW.Add(W(gap, baseW, marker)); }

                double t = 0;
                while (acc + len - t >= Spacing)
                {
                    double step = Spacing - acc;
                    t += step;
                    double k = t / len;
                    outPts.Add(new Point(a.X + (b.X - a.X) * k, a.Y + (b.Y - a.Y) * k));
                    outW.Add(W(gap, baseW, marker));
                    acc = 0;
                }
                acc += len - t;
                if (i == src.Count - 1)
                {
                    outPts.Add(b);
                    outW.Add(W(gap, baseW, marker));
                }
            }

            if (outPts.Count == 0)
            {
                outPts.Add(src[0]);
                outW.Add(marker ? baseW * 1.6 : baseW);
            }

            // 起笔包络
            if (outW.Count > 2 && !marker) { outW[0] *= 0.34; outW[1] *= 0.72; }

            // 收笔包络（抬笔即停，最后一点收窄形成笔锋）
            if (outW.Count > 2 && !marker)
            {
                int n = outW.Count;
                double min = baseW * 0.18;
                outW[n - 1] = Math.Max(min, outW[n - 1] * 0.35);
                outW[n - 2] = Math.Max(min, outW[n - 2] * 0.78);
            }
        }

        static double W(double gap, double baseW, bool marker)
        {
            if (marker) return baseW * 1.9;
            double k = Math.Min(1.0, Math.Max(0.0, (gap - 4.0) / 26.0));   // 快→细
            return baseW * (1.0 - 0.42 * k) * 0.98 + baseW * 0.02;
        }

        static Geometry Outline(List<Point> pts, List<double> ws)
        {
            if (pts.Count < 2)
            {
                double r = Math.Max(1.2, ws[0] / 2);
                var dot = new EllipseGeometry(pts[0], r, r);
                dot.Freeze();
                return dot;
            }

            int n = pts.Count;
            var left = new Point[n];
            var right = new Point[n];
            for (int i = 0; i < n; i++)
            {
                int ip = Math.Max(0, i - 1), inx = Math.Min(n - 1, i + 1);
                double dx = pts[inx].X - pts[ip].X;
                double dy = pts[inx].Y - pts[ip].Y;
                double len = Math.Sqrt(dx * dx + dy * dy);
                if (len < 0.001) { dx = 1; dy = 0; len = 1; }
                double nx = -dy / len, ny = dx / len;
                double h = ws[i] / 2;
                left[i] = new Point(pts[i].X + nx * h, pts[i].Y + ny * h);
                right[i] = new Point(pts[i].X - nx * h, pts[i].Y - ny * h);
            }

            var g = new StreamGeometry();
            using (var c = g.Open())
            {
                c.BeginFigure(left[0], true, true);
                for (int i = 1; i < n; i++) c.LineTo(left[i], false, false);
                for (int i = n - 1; i >= 0; i--) c.LineTo(right[i], false, false);
            }
            g.Freeze();
            return g;
        }

        /// <summary>命中测试：边线容差随笔宽放大（端点用圆帽近似）</summary>
        public static bool Hit(Obj o, Point p, double tol)
        {
            if (o.Type == "stroke")
            {
                double t = Math.Max(tol, o.W * 0.75);
                for (int i = 1; i < o.Pts.Count; i++)
                    if (SegDist(p, o.Pts[i - 1], o.Pts[i]) <= t) return true;
                if (o.Pts.Count == 1) return (p - o.Pts[0]).Length <= o.W;
                return false;
            }
            if (o.Type == "text")
                return o.Bounds().Contains(p);
            var r = ShapeGeom.Norm(o);
            if (o.Shape == "rect" || o.Shape == "ellipse" || o.Shape == "triangle" || o.Shape == "func")
                return r.Contains(p) || NearRect(r, p, Math.Max(6, o.W));
            return SegDist(p, new Point(o.X1, o.Y1), new Point(o.X2, o.Y2)) <= Math.Max(tol, o.W * 0.8);
        }

        static bool NearRect(Rect r, Point p, double tol)
        {
            var outer = r; outer.Inflate(tol, tol);
            if (!outer.Contains(p)) return false;
            var inner = r; inner.Inflate(-tol, -tol);
            return inner.Width > 0 && inner.Height > 0 && !inner.Contains(p);
        }

        public static double SegDist(Point p, Point a, Point b)
        {
            double dx = b.X - a.X, dy = b.Y - a.Y;
            double len2 = dx * dx + dy * dy;
            if (len2 < 0.0001) return (p - a).Length;
            double t = ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / len2;
            t = Math.Max(0, Math.Min(1, t));
            var q = new Point(a.X + dx * t, a.Y + dy * t);
            return (p - q).Length;
        }
    }
}
