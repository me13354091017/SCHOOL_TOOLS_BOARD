using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
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

    /// <summary>函数画板：画坐标网格 + 若干条曲线（范围 -15..15）。</summary>
    public sealed class StbFuncPlot : FrameworkElement
    {
        internal List<StbCurve> Curves = new List<StbCurve>();
        internal double Range = 15d;

        internal void Refresh()
        {
            InvalidateVisual();
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            double w = double.IsInfinity(availableSize.Width) || availableSize.Width <= 0d ? 600d : availableSize.Width;
            double h = double.IsInfinity(availableSize.Height) || availableSize.Height <= 0d ? 520d : availableSize.Height;
            return new Size(w, h);
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

            double side = Math.Min(w, h);
            double unit = side / (Range * 2d);
            double cx = w / 2d;
            double cy = h / 2d;

            Pen gridPen = new Pen(new SolidColorBrush(Color.FromRgb(0xEA, 0xED, 0xF1)), 1d);
            Pen axisPen = new Pen(new SolidColorBrush(Color.FromRgb(0x44, 0x4A, 0x52)), 1.4d);

            for (int i = -15; i <= 15; i++)
            {
                if (i == 0)
                {
                    continue;
                }
                double x = cx + i * unit;
                double y = cy - i * unit;
                dc.DrawLine(gridPen, new Point(x, cy - side / 2d), new Point(x, cy + side / 2d));
                dc.DrawLine(gridPen, new Point(cx - side / 2d, y), new Point(cx + side / 2d, y));
            }

            dc.DrawLine(axisPen, new Point(cx - side / 2d, cy), new Point(cx + side / 2d, cy));
            dc.DrawLine(axisPen, new Point(cx, cy - side / 2d), new Point(cx, cy + side / 2d));

            Typeface tf = new Typeface("Microsoft YaHei UI");
            double dpi = 1d;
            try
            {
                dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            }
            catch
            {
            }

            for (int i = -15; i <= 15; i += 5)
            {
                if (i == 0)
                {
                    continue;
                }
                dc.DrawText(MakeText(i.ToString(), tf, dpi), new Point(cx + i * unit - 8d, cy + 4d));
                dc.DrawText(MakeText(i.ToString(), tf, dpi), new Point(cx + 6d, cy - i * unit - 14d));
            }
            dc.DrawText(MakeText("O", tf, dpi), new Point(cx + 5d, cy + 4d));

            if (Curves == null || Curves.Count == 0)
            {
                FormattedText tip = new FormattedText("在右侧填写表达式，例如 y=x^2、y=sin(x)，曲线会实时画出来",
                    CultureInfo.InvariantCulture, FlowDirection.LeftToRight, tf, 12d,
                    new SolidColorBrush(Color.FromRgb(0x9A, 0xA1, 0xAA)), dpi);
                dc.DrawText(tip, new Point(cx - tip.Width / 2d, cy + 24d));
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

                StreamGeometry geo = new StreamGeometry();
                using (StreamGeometryContext ctx = geo.Open())
                {
                    bool open = false;
                    double prevY = 0d;
                    for (double px = cx - side / 2d; px <= cx + side / 2d; px += 0.5d)
                    {
                        double xv = (px - cx) / unit;
                        double yv;
                        try
                        {
                            yv = c.F(xv);
                        }
                        catch
                        {
                            yv = double.NaN;
                        }
                        if (double.IsNaN(yv) || double.IsInfinity(yv))
                        {
                            open = false;
                            continue;
                        }
                        double py = cy - yv * unit;
                        if (py < cy - side / 2d - unit || py > cy + side / 2d + unit)
                        {
                            open = false;
                            continue;
                        }
                        if (!open)
                        {
                            ctx.BeginFigure(new Point(px, py), false, false);
                            open = true;
                        }
                        else if (Math.Abs(py - prevY) > unit * 2d)
                        {
                            ctx.BeginFigure(new Point(px, py), false, false);
                        }
                        else
                        {
                            ctx.LineTo(new Point(px, py), true, false);
                        }
                        prevY = py;
                    }
                }
                geo.Freeze();
                dc.DrawGeometry(null, pen, geo);
            }
        }

        private static FormattedText MakeText(string text, Typeface tf, double dpi)
        {
            return new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                tf, 11d, new SolidColorBrush(Color.FromRgb(0x33, 0x38, 0x40)), dpi);
        }
    }
}
