using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace InkBoard
{
    /// <summary>绘制与导出相关：图形/坐标系/文字/底色纹理/PNG 导出</summary>
    public partial class BoardView
    {
        /* ------------------------------ 对象绘制 ------------------------------ */
        public void DrawObject(DrawingContext dc, Obj o)
        {
            switch (o.Type)
            {
                case "stroke":
                    if (o.Geo == null) Ink.Build(o);
                    if (o.Geo != null) dc.DrawGeometry(BrushA(o.Color, o.Alpha), null, o.Geo);
                    break;
                case "shape":
                    if (o.Shape == "axis") { DrawAxis(dc, o); break; }
                    if (o.Geo == null) o.Geo = ShapeGeom.Build(o);
                    {
                        var pen = new Pen(BrushA(o.Color, o.Alpha), Math.Max(0.5, o.W));
                        if (o.Dashed) pen.DashStyle = DashStyles.Dash;
                        var fill = o.Fill ? BrushA(o.FillColor, o.Alpha * 0.92) : null;
                        if (o.Geo != null) dc.DrawGeometry(fill, pen, o.Geo);
                        if (o.Shape == "arrow")
                        {
                            if (o.Aux == null) o.Aux = ShapeGeom.ArrowHead(o, Math.Max(10, o.W * 3.4));
                            dc.DrawGeometry(BrushA(o.Color, o.Alpha), null, o.Aux);
                        }
                    }
                    break;
                case "text":
                    {
                        var b = BrushA(o.Color, o.Alpha);
                        double lineH = o.Size * 1.3;
                        var lines = (o.Text ?? "").Split('\n');
                        for (int i = 0; i < lines.Length; i++)
                            dc.DrawText(Fmt(lines[i], o.Size, b), new Point(o.X, o.Y + i * lineH));
                    }
                    break;
            }
        }

        void DrawAxis(DrawingContext dc, Obj o)
        {
            var bb = ShapeGeom.Norm(o);
            double ox = bb.X + bb.Width / 2, oy = bb.Y + bb.Height / 2;
            double unit = Math.Min(bb.Width, bb.Height) / 10.0;
            var b = BrushA(o.Color, o.Alpha);
            if (unit < 1) return;

            var gridPen = new Pen(BrushA(o.Color, 0.16), Math.Max(0.5, o.W * 0.6));
            for (int i = -5; i <= 5; i++)
            {
                dc.DrawLine(gridPen, new Point(ox + i * unit, bb.Y), new Point(ox + i * unit, bb.Y + bb.Height));
                dc.DrawLine(gridPen, new Point(bb.X, oy + i * unit), new Point(bb.X + bb.Width, oy + i * unit));
            }

            var pen = new Pen(b, Math.Max(0.6, o.W));
            dc.DrawLine(pen, new Point(bb.X, oy), new Point(bb.X + bb.Width, oy));
            dc.DrawLine(pen, new Point(ox, bb.Y + bb.Height), new Point(ox, bb.Y));

            double size = Math.Max(8, o.W * 3);
            var head = new StreamGeometry();
            using (var c = head.Open())
            {
                c.BeginFigure(new Point(bb.X + bb.Width, oy), true, true);
                c.LineTo(new Point(bb.X + bb.Width - size, oy - size / 2), false, true);
                c.LineTo(new Point(bb.X + bb.Width - size, oy + size / 2), false, true);
                c.BeginFigure(new Point(ox, bb.Y), true, true);
                c.LineTo(new Point(ox - size / 2, bb.Y + size), false, true);
                c.LineTo(new Point(ox + size / 2, bb.Y + size), false, true);
            }
            head.Freeze();
            dc.DrawGeometry(b, null, head);

            for (int i = -5; i <= 5; i++)
            {
                if (i == 0) continue;
                dc.DrawLine(pen, new Point(ox + i * unit, oy - 5), new Point(ox + i * unit, oy + 5));
                dc.DrawLine(pen, new Point(ox - 5, oy + i * unit), new Point(ox + 5, oy + i * unit));
            }
            var f = Math.Max(11, unit * 0.5);
            dc.DrawText(Fmt("O", f, b), new Point(ox - unit * 0.7, oy + unit * 0.15));
            dc.DrawText(Fmt("x", f, b), new Point(bb.X + bb.Width - size * 1.3, oy + unit * 0.15));
            dc.DrawText(Fmt("y", f, b), new Point(ox + unit * 0.35, bb.Y + size * 0.3));
        }

        /* ------------------------------ 画刷与文字 ------------------------------ */
        SolidColorBrush Brush(string hex) { return BrushA(hex, 1); }

        SolidColorBrush BrushA(string hex, double alpha)
        {
            string key = hex + "|" + Math.Round(alpha, 2);
            var cached = _brushCache.TryGetValue(key, out var b) ? b : null;
            if (cached != null) return cached;
            Color c;
            try { c = (Color)ColorConverter.ConvertFromString(hex); } catch (Exception) { c = Colors.Black; }
            var brush = new SolidColorBrush(c) { Opacity = Math.Max(0, Math.Min(1, alpha)) };
            brush.Freeze();
            _brushCache[key] = brush;
            return brush;
        }

        FormattedText Fmt(string text, double size, Brush brush)
        {
            return new FormattedText(text ?? "", System.Globalization.CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight, new Typeface("Microsoft YaHei UI"), size, brush,
                _dpi > 0 ? _dpi : 1);
        }

        void DrawPattern(DrawingContext dc, Rect r, string bg)
        {
            string kind = bg == "black" ? "grid" : bg;
            if (kind == "plain" || kind == "black") return;
            bool dark = bg == "black";
            double step = 60;
            double lw = 1 / Math.Max(0.2, Scale);
            if (kind == "grid")
            {
                var pen = new Pen(Brush(dark ? "#22ffffff" : "#160f172a"), lw);
                for (double x = 0; x <= r.Width; x += step) dc.DrawLine(pen, new Point(x, 0), new Point(x, r.Height));
                for (double y = 0; y <= r.Height; y += step) dc.DrawLine(pen, new Point(0, y), new Point(r.Width, y));
                var pen2 = new Pen(Brush(dark ? "#40ffffff" : "#280f172a"), lw);
                for (double x = 0; x <= r.Width; x += step * 5) dc.DrawLine(pen2, new Point(x, 0), new Point(x, r.Height));
                for (double y = 0; y <= r.Height; y += step * 5) dc.DrawLine(pen2, new Point(0, y), new Point(r.Width, y));
            }
            else if (kind == "dots")
            {
                var b = Brush(dark ? "#3affffff" : "#330f172a");
                for (double x = step; x < r.Width; x += step)
                    for (double y = step; y < r.Height; y += step)
                        dc.DrawEllipse(b, null, new Point(x, y), 1.8, 1.8);
            }
            else if (kind == "lines")
            {
                var pen = new Pen(Brush(dark ? "#22ffffff" : "#160f172a"), lw);
                for (double y = step * 1.6; y < r.Height; y += step * 1.6) dc.DrawLine(pen, new Point(0, y), new Point(r.Width, y));
            }
        }

        /* ------------------------------ 导出 ------------------------------ */
        public RenderTargetBitmap ExportPage(Page page, double scale)
        {
            int pw = (int)Math.Round(Doc.PageW * scale), ph = (int)Math.Round(Doc.PageH * scale);
            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                var world = new Rect(0, 0, Doc.PageW, Doc.PageH);
                dc.PushTransform(new MatrixTransform(new Matrix(scale, 0, 0, scale, 0, 0)));
                dc.DrawRectangle(Brush(page.Bg == "black" ? "#12161b" : page.Color), null, world);
                dc.PushClip(new RectangleGeometry(world));
                DrawPatternFor(page.Bg, dc, scale);
                dc.Pop();
                if (page.BackImage != null) DrawBackImage(dc, page.BackImage, world);
                foreach (var o in page.Objects) DrawObject(dc, o);
                dc.Pop();
            }
            var bmp = new RenderTargetBitmap(Math.Max(1, pw), Math.Max(1, ph), 96, 96, PixelFormats.Pbgra32);
            bmp.Render(dv);
            return bmp;
        }

        void DrawPatternFor(string bg, DrawingContext dc, double scale)
        {
            string kind = bg == "black" ? "grid" : bg;
            if (kind == "plain" || kind == "black") return;
            bool dark = bg == "black";
            const double step = 60;
            var pen = new Pen(Brush(dark ? "#22ffffff" : "#160f172a"), 1 / scale);
            if (kind == "grid")
            {
                for (double x = 0; x <= Doc.PageW; x += step) dc.DrawLine(pen, new Point(x, 0), new Point(x, Doc.PageH));
                for (double y = 0; y <= Doc.PageH; y += step) dc.DrawLine(pen, new Point(0, y), new Point(Doc.PageW, y));
                var pen2 = new Pen(Brush(dark ? "#40ffffff" : "#280f172a"), 1 / scale);
                for (double x = 0; x <= Doc.PageW; x += step * 5) dc.DrawLine(pen2, new Point(x, 0), new Point(x, Doc.PageH));
                for (double y = 0; y <= Doc.PageH; y += step * 5) dc.DrawLine(pen2, new Point(0, y), new Point(Doc.PageW, y));
            }
            else if (kind == "dots")
            {
                var b = Brush(dark ? "#3affffff" : "#330f172a");
                for (double x = step; x < Doc.PageW; x += step)
                    for (double y = step; y < Doc.PageH; y += step)
                        dc.DrawEllipse(b, null, new Point(x, y), 1.8, 1.8);
            }
            else if (kind == "lines")
            {
                for (double y = step * 1.6; y < Doc.PageH; y += step * 1.6) dc.DrawLine(pen, new Point(0, y), new Point(Doc.PageW, y));
            }
        }

        /// <summary>页面底图（PDF 页面）按等比居中铺满页面</summary>
        void DrawBackImage(DrawingContext dc, BitmapSource img, Rect world)
        {
            double sw = img.PixelWidth, sh = img.PixelHeight;
            if (sw <= 0 || sh <= 0) return;
            double k = Math.Min(world.Width / sw, world.Height / sh);
            double w = sw * k, h = sh * k;
            dc.DrawImage(img, new Rect((world.Width - w) / 2, (world.Height - h) / 2, w, h));
        }
    }
}
