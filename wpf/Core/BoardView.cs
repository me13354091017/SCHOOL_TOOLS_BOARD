using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace InkBoard
{
    public enum ToolKind { Pen, Marker, Eraser, Shape, Select, Text, Laser, Hand }
    public enum ShapeKind { Line, Arrow, Rect, Ellipse, Triangle, Axis, Func }

    /// <summary>双图层画板：静态层只在内容变化时重绘，动态层跟随指针重绘</summary>
    public partial class BoardView : Grid
    {
        public Doc Doc = Doc.Create();
        public double Scale = 1;
        public double Tx, Ty;
        public ToolKind Tool = ToolKind.Pen;
        public string ColorHex = "#111827";
        public double PenWidth = 7;
        public ShapeKind Shape = ShapeKind.Line;
        public bool FillShape;
        public bool ShowPerf;
        public string FuncExpr = "sin(x)";

        public Action Commit;       // 需要记入撤销栈时回调
        public Action Changed;
        public Func<string, string> AskText;

        readonly StaticLayer _st;
        readonly LiveLayer _lv;
        readonly List<Point> _laser = new List<Point>();
        readonly List<Obj> _selected = new List<Obj>();
        readonly Dictionary<string, SolidColorBrush> _brushCache = new Dictionary<string, SolidColorBrush>();
        Obj _cur;
        Point _startWorld, _lastWorld, _downScreen;
        Rect _selRect;
        bool _drawing, _panning, _moving, _rubber;
        double _dpi = 1;
        int _frames;
        DateTime _fpsMark = DateTime.Now;
        DispatcherTimer _laserTimer;
        double _fps = 60;

        public BoardView()
        {
            Background = Brushes.Transparent;
            IsManipulationEnabled = true;
            _st = new StaticLayer(this);
            _lv = new LiveLayer(this);
            _st.IsHitTestVisible = false;
            _lv.IsHitTestVisible = false;
            Children.Add(_st);
            Children.Add(_lv);
        }

        /* ------------------------------ 视图 ------------------------------ */
        public Point ToWorld(Point s) { return new Point((s.X - Tx) / Scale, (s.Y - Ty) / Scale); }
        public Point ToScreen(Point w) { return new Point(w.X * Scale + Tx, w.Y * Scale + Ty); }

        public void FitPage()
        {
            double w = ActualWidth > 0 ? ActualWidth : 1600;
            double h = ActualHeight > 0 ? ActualHeight : 900;
            double padL = 40, padR = 40, padT = 24, padB = 24;
            double s = Math.Min((w - padL - padR) / Doc.PageW, (h - padT - padB) / Doc.PageH);
            Scale = Math.Max(0.05, s);
            Tx = padL + (w - padL - padR - Doc.PageW * Scale) / 2;
            Ty = padT + (h - padT - padB - Doc.PageH * Scale) / 2;
            InvalidateAll();
        }

        public void ZoomAt(Point screen, double factor)
        {
            double ns = Math.Max(0.15, Math.Min(6, Scale * factor));
            double k = ns / Scale;
            Tx = screen.X - (screen.X - Tx) * k;
            Ty = screen.Y - (screen.Y - Ty) * k;
            Scale = ns;
            InvalidateAll();
        }

        public void PanBy(double dx, double dy) { Tx += dx; Ty += dy; InvalidateAll(); }

        public void InvalidateAll() { _st.InvalidateVisual(); _lv.InvalidateVisual(); }
        public void InvalidateStatic() { _st.InvalidateVisual(); }
        public void InvalidateLive() { _lv.InvalidateVisual(); }

        public void SetDoc(Doc d) { Doc = d; _selected.Clear(); _cur = null; InvalidateAll(); }

        /* ------------------------------ 静态层绘制 ------------------------------ */
        internal void RenderStatic(DrawingContext dc)
        {
            _dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            var size = new Rect(0, 0, Math.Max(1, ActualWidth), Math.Max(1, ActualHeight));
            dc.DrawRectangle(Brush("#eef1f5"), null, size);

            var pr = new Rect(Tx, Ty, Doc.PageW * Scale, Doc.PageH * Scale);
            dc.DrawRectangle(Brush("#330f172a"), null, new Rect(pr.X + 4, pr.Y + 8, pr.Width, pr.Height));

            dc.PushTransform(new MatrixTransform(new Matrix(Scale, 0, 0, Scale, Tx, Ty)));
            var page = Doc.Cur;
            var world = new Rect(0, 0, Doc.PageW, Doc.PageH);
            dc.DrawRectangle(Brush(page.Bg == "black" ? "#12161b" : page.Color), null, world);
            dc.PushClip(new RectangleGeometry(world));
            DrawPattern(dc, world, page.Bg);
            dc.Pop();
            if (page.BackImage != null) DrawBackImage(dc, page.BackImage, world);

            if (page != null)
                foreach (var o in page.Objects) DrawObject(dc, o);
            dc.Pop();

            dc.DrawRectangle(null, new Pen(Brush("#cfd6de"), 1), pr);
            if (ShowPerf)
            {
                dc.DrawRectangle(Brush("#cc0b1220"), null, new Rect(8, 8, 160, 26));
                dc.DrawText(Fmt(_fps.ToString("F0") + " FPS  " + Doc.Cur.Objects.Count + " obj", 13, Brush("#8ef0c0")), new Point(16, 12));
            }
        }

        internal void RenderLive(DrawingContext dc)
        {
            dc.PushTransform(new MatrixTransform(new Matrix(Scale, 0, 0, Scale, Tx, Ty)));
            if (_cur != null) DrawObject(dc, _cur);
            if (_laser.Count > 1)
            {
                var b = Brush(ColorHex);
                for (int i = 0; i < _laser.Count; i++)
                {
                    double k = (double)(i + 1) / _laser.Count;
                    dc.DrawEllipse(b, null, _laser[i], 9 * k, 9 * k);
                }
            }
            dc.Pop();

            if (_rubber)
            {
                var r = new Rect(ToScreen(_selRect.TopLeft), ToScreen(_selRect.BottomRight));
                dc.DrawRectangle(Brush("#220e9f76"), new Pen(Brush("#0e9f76"), 1), r);
            }
            if (_selected.Count > 0)
                foreach (var o in _selected)
                {
                    var b = o.Bounds();
                    var r = new Rect(ToScreen(b.TopLeft), ToScreen(b.BottomRight));
                    r.Inflate(3, 3);
                    dc.DrawRectangle(null, new Pen(Brush("#0e9f76"), 1.4), r);
                }
            _frames++;
            var now = DateTime.Now;
            if ((now - _fpsMark).TotalMilliseconds >= 1000)
            {
                _fps = _frames * 1000.0 / (now - _fpsMark).TotalMilliseconds;
                _frames = 0;
                _fpsMark = now;
            }
        }

        /* ------------------------------ 输入 ------------------------------ */
        protected override void OnStylusDown(StylusDownEventArgs e)
        {
            base.OnStylusDown(e);
            var dev = e.StylusDevice;
            if (dev == null) return;
            if (dev.TabletDevice != null && dev.TabletDevice.Type == TabletDeviceType.Touch) return;   // 手指交给 Manipulation
            var pts = e.GetStylusPoints(this);
            if (pts.Count == 0) return;
            dev.Capture(this);
            Press(pts[0].ToPoint());
            e.Handled = true;
        }

        protected override void OnStylusMove(StylusEventArgs e)
        {
            base.OnStylusMove(e);
            if (!_drawing && !_panning) return;
            foreach (var sp in e.GetStylusPoints(this)) MoveTo(sp.ToPoint());
            e.Handled = true;
        }

        protected override void OnStylusUp(StylusEventArgs e)
        {
            base.OnStylusUp(e);
            Release();
            e.Handled = true;
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            if (e.StylusDevice != null) return;             // 已由笔事件处理
            Press(e.GetPosition(this));
            e.Handled = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (e.StylusDevice != null) return;
            if (_drawing || _panning) { MoveTo(e.GetPosition(this)); e.Handled = true; }
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            if (e.StylusDevice != null) return;
            Release();
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.ChangedButton == MouseButton.Middle) { _panning = true; _downScreen = e.GetPosition(this); CaptureMouse(); }
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.ChangedButton == MouseButton.Middle) { _panning = false; ReleaseMouseCapture(); }
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            ZoomAt(e.GetPosition(this), e.Delta > 0 ? 1.12 : 1 / 1.12);
            e.Handled = true;
        }

        protected override void OnManipulationDelta(ManipulationDeltaEventArgs e)
        {
            base.OnManipulationDelta(e);
            if (_drawing) return;
            var o = e.ManipulationOrigin;
            var d = e.DeltaManipulation;
            if (d.Scale.X > 0) ZoomAt(o, d.Scale.X);
            PanBy(d.Translation.X, d.Translation.Y);
        }

        /* ------------------------------ 工具逻辑 ------------------------------ */
        void Press(Point sp)
        {
            _downScreen = sp;
            var wp = ToWorld(sp);
            _startWorld = wp;
            _lastWorld = wp;

            switch (Tool)
            {
                case ToolKind.Pen:
                case ToolKind.Marker:
                    _cur = new Obj { Type = "stroke", Color = ColorHex, W = PenWidth, Marker = Tool == ToolKind.Marker, Alpha = Tool == ToolKind.Marker ? 0.32 : 1 };
                    _cur.Pts.Add(wp);
                    _drawing = true;
                    break;

                case ToolKind.Laser:
                    _laser.Clear();
                    _laser.Add(wp);
                    _drawing = true;
                    StartLaserTimer();
                    break;

                case ToolKind.Eraser:
                    _drawing = true;
                    EraseAt(wp);
                    break;

                case ToolKind.Shape:
                    _cur = NewShape(wp, wp);
                    _drawing = true;
                    break;

                case ToolKind.Select:
                    {
                        var hit = HitTop(wp);
                        if (hit != null)
                        {
                            if (!_selected.Contains(hit)) { _selected.Clear(); _selected.Add(hit); }
                            _moving = true;
                        }
                        else { _selected.Clear(); _rubber = true; _selRect = new Rect(wp, wp); }
                        InvalidateLive();
                        break;
                    }

                case ToolKind.Text:
                    {
                        var text = AskText != null ? AskText("") : null;
                        if (!string.IsNullOrEmpty(text))
                        {
                            var t = new Obj { Type = "text", Text = text, X = wp.X, Y = wp.Y, Color = ColorHex, Size = Math.Max(24, PenWidth * 4.6) };
                            Doc.Cur.Objects.Add(t);
                            AfterEdit();
                        }
                        break;
                    }

                case ToolKind.Hand:
                    _panning = true;
                    _lastWorld = wp;
                    break;
            }
            if (_cur != null) InvalidateLive();
        }

        void MoveTo(Point sp)
        {
            var wp = ToWorld(sp);

            if (_panning)
            {
                PanBy(sp.X - _downScreen.X, sp.Y - _downScreen.Y);
                _downScreen = sp;
                return;
            }

            switch (Tool)
            {
                case ToolKind.Pen:
                case ToolKind.Marker:
                    if (_cur != null)
                    {
                        var last = _cur.Pts[_cur.Pts.Count - 1];
                        if ((wp - last).Length >= 0.6) { _cur.Pts.Add(wp); }
                        _cur.Geo = null;
                        Ink.Build(_cur);
                        InvalidateLive();
                    }
                    break;

                case ToolKind.Laser:
                    _laser.Add(wp);
                    StartLaserTimer();
                    InvalidateLive();
                    break;

                case ToolKind.Eraser:
                    EraseAt(wp);
                    break;

                case ToolKind.Shape:
                    if (_cur != null)
                    {
                        _cur.X2 = wp.X; _cur.Y2 = wp.Y;
                        _cur.Geo = null; _cur.Aux = null;
                        InvalidateLive();
                    }
                    break;

                case ToolKind.Select:
                    if (_moving)
                    {
                        double dx = wp.X - _lastWorld.X, dy = wp.Y - _lastWorld.Y;
                        foreach (var o in _selected) Translate(o, dx, dy);
                        _lastWorld = wp;
                        InvalidateStatic(); InvalidateLive();
                    }
                    else if (_rubber)
                    {
                        _selRect = new Rect(ToWorld(_downScreen), wp);
                        _selected.Clear();
                        foreach (var o in Doc.Cur.Objects)
                            if (o.Bounds().IntersectsWith(_selRect)) _selected.Add(o);
                        InvalidateLive();
                    }
                    break;
            }
            if (Tool != ToolKind.Hand) _lastWorld = wp;
        }

        void Release()
        {
            if (_panning) { _panning = false; return; }
            switch (Tool)
            {
                case ToolKind.Pen:
                case ToolKind.Marker:
                    if (_cur != null)
                    {
                        if (_cur.Pts.Count > 0)
                        {
                            Ink.Build(_cur);
                            Doc.Cur.Objects.Add(_cur);
                        }
                        _cur = null;
                        AfterEdit();
                    }
                    break;

                case ToolKind.Shape:
                    if (_cur != null)
                    {
                        var w = Math.Abs(_cur.X2 - _cur.X1), h = Math.Abs(_cur.Y2 - _cur.Y1);
                        if (w > 4 || h > 4) Doc.Cur.Objects.Add(_cur);
                        _cur = null;
                        AfterEdit();
                    }
                    break;

                case ToolKind.Eraser:
                    if (_erased) AfterEdit();
                    _erased = false;
                    break;

                case ToolKind.Select:
                    _moving = false; _rubber = false;
                    InvalidateLive();
                    break;
            }
            _drawing = false;
        }

        bool _erased;

        Obj NewShape(Point a, Point b)
        {
            var o = new Obj
            {
                Type = "shape",
                Color = ColorHex,
                W = PenWidth,
                Fill = FillShape,
                FillColor = ColorHex,
                X1 = a.X, Y1 = a.Y, X2 = b.X, Y2 = b.Y,
                Expr = FuncExpr
            };
            switch (Shape)
            {
                case ShapeKind.Line: o.Shape = "line"; break;
                case ShapeKind.Arrow: o.Shape = "arrow"; break;
                case ShapeKind.Rect: o.Shape = "rect"; break;
                case ShapeKind.Ellipse: o.Shape = "ellipse"; break;
                case ShapeKind.Triangle: o.Shape = "triangle"; break;
                case ShapeKind.Axis: o.Shape = "axis"; break;
                case ShapeKind.Func: o.Shape = "func"; break;
            }
            return o;
        }

        void EraseAt(Point wp)
        {
            var page = Doc.Cur;
            double tol = Math.Max(10, PenWidth * 1.6);
            for (int i = page.Objects.Count - 1; i >= 0; i--)
            {
                var o = page.Objects[i];
                var bb = o.Bounds();
                bb.Inflate(8, 8);
                if (bb.Contains(wp) && Ink.Hit(o, wp, tol))
                {
                    page.Objects.RemoveAt(i);
                    _erased = true;
                    InvalidateStatic();
                }
            }
        }

        static void Translate(Obj o, double dx, double dy)
        {
            if (o.Type == "stroke")
            {
                for (int i = 0; i < o.Pts.Count; i++) o.Pts[i] = new Point(o.Pts[i].X + dx, o.Pts[i].Y + dy);
                o.Geo = null;
                Ink.Build(o);
            }
            else if (o.Type == "text") { o.X += dx; o.Y += dy; }
            else { o.X1 += dx; o.Y1 += dy; o.X2 += dx; o.Y2 += dy; o.Geo = null; o.Aux = null; }
        }

        Obj HitTop(Point wp)
        {
            var page = Doc.Cur;
            for (int i = page.Objects.Count - 1; i >= 0; i--)
                if (Ink.Hit(page.Objects[i], wp, Math.Max(10, PenWidth * 1.4))) return page.Objects[i];
            return null;
        }

        public List<Obj> Selection => _selected;

        public void DeleteSelection()
        {
            if (_selected.Count == 0) return;
            foreach (var o in _selected) Doc.Cur.Objects.Remove(o);
            _selected.Clear();
            AfterEdit();
        }

        public void DuplicateSelection()
        {
            if (_selected.Count == 0) return;
            var copies = new List<Obj>();
            foreach (var o in _selected)
            {
                var c = Clone(o);
                Translate(c, 26, 26);
                Doc.Cur.Objects.Add(c);
                copies.Add(c);
            }
            _selected.Clear();
            _selected.AddRange(copies);
            AfterEdit();
        }

        public static Obj Clone(Obj o)
        {
            var c = new Obj
            {
                Type = o.Type, Color = o.Color, W = o.W, Alpha = o.Alpha, Marker = o.Marker,
                Shape = o.Shape, X1 = o.X1, Y1 = o.Y1, X2 = o.X2, Y2 = o.Y2, Expr = o.Expr,
                Fill = o.Fill, FillColor = o.FillColor, Dashed = o.Dashed,
                X = o.X, Y = o.Y, Size = o.Size, Text = o.Text
            };
            c.Pts.AddRange(o.Pts);
            if (o.Type == "stroke") Ink.Build(c); else c.Geo = null;
            return c;
        }

        void AfterEdit()
        {
            InvalidateStatic();
            InvalidateLive();
            Commit?.Invoke();
            Changed?.Invoke();
        }

        /* ------------------------------ 激光尾迹 ------------------------------ */
        void StartLaserTimer()
        {
            if (_laserTimer != null) return;
            _laserTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(30) };
            _laserTimer.Tick += (s, e) =>
            {
                if (_laser.Count > 0) _laser.RemoveAt(0);
                InvalidateLive();
                if (_laser.Count == 0) { _laserTimer.Stop(); _laserTimer = null; }
            };
            _laserTimer.Start();
        }
    }

    public sealed class StaticLayer : FrameworkElement
    {
        readonly BoardView _o;
        public StaticLayer(BoardView o) { _o = o; }
        protected override void OnRender(DrawingContext dc) { _o.RenderStatic(dc); }
    }

    public sealed class LiveLayer : FrameworkElement
    {
        readonly BoardView _o;
        public LiveLayer(BoardView o) { _o = o; }
        protected override void OnRender(DrawingContext dc) { _o.RenderLive(dc); }
    }
}
