using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace InkCanvasPlus
{
    /// <summary>一条待绘制的曲线：表达式、颜色、编译后的求值函数。</summary>
    internal sealed class StbCurve
    {
        internal string Expr;
        internal Color Color;
        internal Func<double, double> F;
    }

    /// <summary>函数画板：网格 + 曲线，支持滚轮缩放与拖动平移。</summary>
    public sealed class StbFuncPlot : FrameworkElement
    {
        internal List<StbCurve> Curves = new List<StbCurve>();
        internal double Range = 15d;
        internal double Zoom = 1d;
        internal double CenterX = 0d;
        internal double CenterY = 0d;

        private bool _dragging;
        private Point _dragOrigin;

        public StbFuncPlot()
        {
            ClipToBounds = true;
            Focusable = true;
        }

        internal void Refresh()
        {
            InvalidateVisual();
        }

        internal void ResetView()
        {
            Zoom = 1d;
            CenterX = 0d;
            CenterY = 0d;
            Refresh();
        }

        internal void ZoomBy(double factor)
        {
            Zoom = Math.Min(60d, Math.Max(0.15d, Zoom * factor));
            Refresh();
        }

        internal double UnitPerPx(double w, double h)
        {
            double side = Math.Min(w, h);
            return side / (Range * 2d) * Zoom;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            double w = double.IsInfinity(availableSize.Width) || availableSize.Width <= 0d ? 600d : availableSize.Width;
            double h = double.IsInfinity(availableSize.Height) || availableSize.Height <= 0d ? 520d : availableSize.Height;
            return new Size(w, h);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            double w = ActualWidth;
            double h = ActualHeight;
            if (w <= 0d || h <= 0d)
            {
                return;
            }
            double unitOld = UnitPerPx(w, h);
            Point p = e.GetPosition(this);
            double wx = CenterX + (p.X - w / 2d) / unitOld;
            double wy = CenterY - (p.Y - h / 2d) / unitOld;

            double factor = e.Delta > 0 ? 1.25d : 0.8d;
            Zoom = Math.Min(60d, Math.Max(0.15d, Zoom * factor));
            double unitNew = UnitPerPx(w, h);
            CenterX = wx - (p.X - w / 2d) / unitNew;
            CenterY = wy + (p.Y - h / 2d) / unitNew;
            Refresh();
            e.Handled = true;
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            _dragging = true;
            _dragOrigin = e.GetPosition(this);
            CaptureMouse();
            e.Handled = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!_dragging)
            {
                return;
            }
            double w = ActualWidth;
            double h = ActualHeight;
            if (w <= 0d || h <= 0d)
            {
                return;
            }
            Point p = e.GetPosition(this);
            double unit = UnitPerPx(w, h);
            CenterX -= (p.X - _dragOrigin.X) / unit;
            CenterY += (p.Y - _dragOrigin.Y) / unit;
            _dragOrigin = p;
            Refresh();
            e.Handled = true;
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            if (_dragging)
            {
                _dragging = false;
                ReleaseMouseCapture();
                e.Handled = true;
            }
        }

        private static double NiceStep(double raw)
        {
            if (raw <= 0d || double.IsNaN(raw))
            {
                return 1d;
            }
            double exp = Math.Floor(Math.Log10(raw));
            double baseVal = raw / Math.Pow(10d, exp);
            double nice = baseVal <= 1d ? 1d : baseVal <= 2d ? 2d : baseVal <= 5d ? 5d : 10d;
            return nice * Math.Pow(10d, exp);
        }

        protected override void OnRender(DrawingContext dc)
        {
            double w = ActualWidth;
            double h = ActualHeight;
            if (w <= 0d || h <= 0d)
            {
                return;
            }

            dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, w, h));

            double unit = UnitPerPx(w, h);
            double cx = w / 2d - CenterX * unit;
            double cy = h / 2d + CenterY * unit;

            Typeface tf = new Typeface("Microsoft YaHei UI");
            double dpi = 1d;
            try
            {
                dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            }
            catch
            {
            }

            Pen gridPen = new Pen(new SolidColorBrush(Color.FromRgb(0xEA, 0xED, 0xF1)), 1d);
            Pen subPen = new Pen(new SolidColorBrush(Color.FromRgb(0xE3, 0xE8, 0xEE)), 1d);
            Pen axisPen = new Pen(new SolidColorBrush(Color.FromRgb(0x44, 0x4A, 0x52)), 1.4d);

            double step = NiceStep(28d / unit);
            double minor = step / 5d;

            int guard = 0;
            double offX = cx % (minor * unit);
            for (double gx = offX; gx <= w && guard < 3000; gx += minor * unit, guard++)
            {
                dc.DrawLine(subPen, new Point(gx, 0), new Point(gx, h));
            }
            guard = 0;
            double offY = cy % (minor * unit);
            for (double gy = offY; gy <= h && guard < 3000; gy += minor * unit, guard++)
            {
                dc.DrawLine(subPen, new Point(0, gy), new Point(w, gy));
            }

            double wx0 = CenterX - (w / 2d) / unit;
            double wx1 = CenterX + (w / 2d) / unit;
            double wy0 = CenterY - (h / 2d) / unit;
            double wy1 = CenterY + (h / 2d) / unit;

            double startX = Math.Ceiling(wx0 / step) * step;
            for (double v = startX; v <= wx1; v += step)
            {
                double px = w / 2d + (v - CenterX) * unit;
                dc.DrawLine(gridPen, new Point(px, 0), new Point(px, h));
                if (Math.Abs(v) > step / 2d)
                {
                    dc.DrawText(MakeText(Fmt(v, step), tf, dpi), new Point(px + 3d, Math.Min(h - 15d, Math.Max(2d, cy + 3d))));
                }
            }

            double startY = Math.Ceiling(wy0 / step) * step;
            for (double v = startY; v <= wy1; v += step)
            {
                double py = h / 2d - (v - CenterY) * unit;
                dc.DrawLine(gridPen, new Point(0, py), new Point(w, py));
                if (Math.Abs(v) > step / 2d)
                {
                    dc.DrawText(MakeText(Fmt(v, step), tf, dpi), new Point(Math.Min(w - 36d, 3d), py - 15d));
                }
            }

            if (cy >= 0d && cy <= h)
            {
                dc.DrawLine(axisPen, new Point(0, cy), new Point(w, cy));
            }
            if (cx >= 0d && cx <= w)
            {
                dc.DrawLine(axisPen, new Point(cx, 0), new Point(cx, h));
            }
            dc.DrawText(MakeText("O", tf, dpi), new Point(Math.Max(2d, cx + 4d), Math.Min(h - 15d, cy + 4d)));

            if (Curves == null || Curves.Count == 0)
            {
                FormattedText tip = new FormattedText("滚轮缩放 · 按住拖动平移 · 右侧填表达式",
                    CultureInfo.InvariantCulture, FlowDirection.LeftToRight, tf, 12d,
                    new SolidColorBrush(Color.FromRgb(0x9A, 0xA1, 0xAA)), dpi);
                dc.DrawText(tip, new Point(Math.Max(10d, (w - tip.Width) / 2d), Math.Min(h - 20d, cy + 24d)));
                return;
            }

            foreach (StbCurve c in Curves)
            {
                if (c == null || c.F == null)
                {
                    continue;
                }

                Pen pen = new Pen(new SolidColorBrush(c.Color), 1.8d);
                pen.Freeze();

                // 自适应采样（参考 Desmos / padowan Graph）：粗网格 + 递归细分，
                // 曲率大的地方加密，定义域边界二分逼近，渐近线处断开不画竖线。
                List<Point?> samples = new List<Point?>(1024);
                double stepPx = 6d;
                double prevPx = -stepPx;
                double prevPy = SampleY(c.F, prevPx, w, h, unit);
                for (double px = 0d; px <= w + stepPx; px += stepPx)
                {
                    double clamped = Math.Min(px, w);
                    double py = SampleY(c.F, clamped, w, h, unit);
                    Refine(c.F, prevPx, prevPy, clamped, py, 0, samples, w, h, unit);
                    prevPx = clamped;
                    prevPy = py;
                    if (clamped >= w)
                    {
                        break;
                    }
                }
                samples.Add(MakePoint(prevPx, ToScreenY(prevPy, h, unit), h));

                StreamGeometry geo = new StreamGeometry();
                using (StreamGeometryContext ctx = geo.Open())
                {
                    bool open = false;
                    Point prev = new Point();
                    double prevWorld = 0d;
                    foreach (Point? nullable in samples)
                    {
                        if (!nullable.HasValue)
                        {
                            open = false;
                            continue;
                        }
                        Point p = nullable.Value;
                        double world = CenterY - (p.Y - h / 2d) / unit;
                        if (!open)
                        {
                            ctx.BeginFigure(p, false, false);
                            open = true;
                        }
                        else
                        {
                            double jump = Math.Abs(p.Y - prev.Y);
                            bool opposite = (world - CenterY) * (prevWorld - CenterY) < 0d;
                            bool asymptote = false;
                            if (jump > h * 0.85d && opposite)
                            {
                                double xm = (prev.X + p.X) / 2d;
                                double ym = SampleY(c.F, xm, w, h, unit);
                                double maxEdge = Math.Max(Math.Abs(world - CenterY), Math.Abs(prevWorld - CenterY));
                                asymptote = double.IsNaN(ym) || Math.Abs(ym - CenterY) > 2d * maxEdge;
                            }
                            if (asymptote)
                            {
                                ctx.BeginFigure(p, false, false);
                            }
                            else
                            {
                                ctx.LineTo(p, true, false);
                            }
                        }
                        prev = p;
                        prevWorld = world;
                    }
                }
                geo.Freeze();
                dc.DrawGeometry(null, pen, geo);
            }
        }

        private double SampleY(Func<double, double> f, double px, double w, double h, double unit)
        {
            double xv = (px - w / 2d) / unit + CenterX;
            try
            {
                double yv = f(xv);
                if (double.IsNaN(yv) || double.IsInfinity(yv))
                {
                    return double.NaN;
                }
                return yv;
            }
            catch
            {
                return double.NaN;
            }
        }

        private static Point? MakePoint(double px, double worldY, double h)
        {
            if (double.IsNaN(worldY))
            {
                return null;
            }
            return new Point(px, worldY);
        }

        /// <summary>递归细分：中点偏离弦超过 0.3px 就继续切；NaN 边界二分找定义域端点。</summary>
        private void Refine(Func<double, double> f, double x0, double y0, double x1, double y1,
            int depth, List<Point?> samples, double w, double h, double unit)
        {
            if (depth >= 7 || x1 - x0 < 0.35d)
            {
                samples.Add(MakePoint(x0, ToScreenY(y0, h, unit), h));
                return;
            }

            bool f0 = !double.IsNaN(y0);
            bool f1 = !double.IsNaN(y1);
            double xm = (x0 + x1) / 2d;
            double ym = SampleY(f, xm, w, h, unit);
            bool fm = !double.IsNaN(ym);

            bool need;
            if (f0 && f1 && fm)
            {
                double sy0 = ToScreenY(y0, h, unit);
                double sy1 = ToScreenY(y1, h, unit);
                double sym = ToScreenY(ym, h, unit);
                bool allAbove = sy0 < -h * 0.5d && sy1 < -h * 0.5d && sym < -h * 0.5d;
                bool allBelow = sy0 > h * 1.5d && sy1 > h * 1.5d && sym > h * 1.5d;
                need = !allAbove && !allBelow && Math.Abs(sym - (sy0 + sy1) / 2d) > 0.3d;
            }
            else
            {
                need = f0 != f1 || f0 != fm || f1 != fm;
                if (!f0 && !f1 && !fm && depth >= 3)
                {
                    need = false;
                }
            }

            if (need)
            {
                Refine(f, x0, y0, xm, ym, depth + 1, samples, w, h, unit);
                Refine(f, xm, ym, x1, y1, depth + 1, samples, w, h, unit);
            }
            else
            {
                samples.Add(MakePoint(x0, ToScreenY(y0, h, unit), h));
            }
        }

        private double ToScreenY(double worldY, double h, double unit)
        {
            if (double.IsNaN(worldY))
            {
                return double.NaN;
            }
            double py = h / 2d - (worldY - CenterY) * unit;
            if (py < -h * 3d)
            {
                py = -h * 3d;
            }
            if (py > h * 4d)
            {
                py = h * 4d;
            }
            return py;
        }

        private static string Fmt(double v, double step)
        {
            int digits = step >= 1d ? 0 : step >= 0.1d ? 1 : step >= 0.01d ? 2 : 3;
            string fmt = digits == 0 ? "0" : "0." + new string('0', digits);
            return v.ToString(fmt, CultureInfo.InvariantCulture);
        }

        private static FormattedText MakeText(string text, Typeface tf, double dpi)
        {
            return new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                tf, 11d, new SolidColorBrush(Color.FromRgb(0x33, 0x38, 0x40)), dpi);
        }
    }
}
