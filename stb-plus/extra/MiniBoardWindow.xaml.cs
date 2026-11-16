using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;

namespace InkCanvasPlus
{
    /// <summary>
    /// 板中板（MiniBoard）：叠在主窗口上方的小白板，可拖动、可四边四角拖拽缩放、可直接手写。
    /// 由 stb-plus 补丁新增，不依赖上游代码。
    /// </summary>
    public partial class MiniBoardWindow : Window
    {
        private const double EdgeGrip = 8d;
        private const double MinW = 280d;
        private const double MinH = 220d;

        private static MiniBoardWindow _instance;
        private double _penWidth = 3d;
        private Color _penColor = Colors.Black;
        private bool _darkBoard = false;

        private string _grip = "None";
        private bool _resizing = false;
        private Point _startCursor = new Point(0, 0);
        private Rect _startRect = new Rect(0, 0, 0, 0);

        public static void ShowBoard(Window owner)
        {
            if (_instance != null)
            {
                if (_instance.WindowState == WindowState.Minimized)
                {
                    _instance.WindowState = WindowState.Normal;
                }
                _instance.Activate();
                return;
            }

            var w = new MiniBoardWindow();
            if (owner != null && owner.IsLoaded)
            {
                w.Owner = owner;
            }
            _instance = w;
            w.Closed += (s, e) => { _instance = null; };

            Rect wa = SystemParameters.WorkArea;
            w.Left = Math.Max(wa.Left, wa.Right - w.Width - 60);
            w.Top = Math.Max(wa.Top, wa.Bottom - w.Height - 90);

            w.Show();
            w.Activate();
        }

        public MiniBoardWindow()
        {
            InitializeComponent();
            RootGrid.LostMouseCapture += (s, e) => { _resizing = false; };
            ApplyPen();
        }

        private void ApplyPen()
        {
            MiniInkCanvas.EditingMode = InkCanvasEditingMode.Ink;
            DrawingAttributes da = MiniInkCanvas.DefaultDrawingAttributes.Clone();
            da.Color = _penColor;
            da.Width = _penWidth;
            da.Height = _penWidth;
            da.FitToCurve = false;
            da.IgnorePressure = true;
            MiniInkCanvas.DefaultDrawingAttributes = da;
        }

        private void SetBoard(bool dark)
        {
            _darkBoard = dark;
            MiniInkCanvas.Background = new SolidColorBrush(dark
                ? Color.FromRgb(0x10, 0x1A, 0x16)
                : Colors.White);
            _penColor = dark ? Colors.White : Colors.Black;
            TextBlockMiniBoardStatus.Text = dark ? "板中板 · STB（黑板）" : "板中板 · STB（白板）";
            ApplyPen();
        }

        /// <summary>把客户端坐标换算成屏幕坐标（DIP，与 Left/Top/Width/Height 同一坐标系）。</summary>
        private Point ScreenDip(Point clientPoint)
        {
            Point px = PointToScreen(clientPoint);
            PresentationSource src = PresentationSource.FromVisual(this);
            if (src == null || src.CompositionTarget == null)
            {
                return px;
            }
            Matrix m = src.CompositionTarget.TransformToDevice;
            double sx = m.M11 == 0d ? 1d : m.M11;
            double sy = m.M22 == 0d ? 1d : m.M22;
            return new Point(px.X / sx, px.Y / sy);
        }

        private string HitGrip(Point p)
        {
            bool left = p.X <= EdgeGrip;
            bool right = p.X >= ActualWidth - EdgeGrip;
            bool top = p.Y <= EdgeGrip;
            bool bottom = p.Y >= ActualHeight - EdgeGrip;
            if (top && left) return "TL";
            if (top && right) return "TR";
            if (bottom && left) return "BL";
            if (bottom && right) return "BR";
            if (left) return "L";
            if (right) return "R";
            if (top) return "T";
            if (bottom) return "B";
            return "None";
        }

        private void SetGripCursor(string g)
        {
            switch (g)
            {
                case "L":
                case "R":
                    RootGrid.Cursor = Cursors.SizeWE;
                    break;
                case "T":
                case "B":
                    RootGrid.Cursor = Cursors.SizeNS;
                    break;
                case "TL":
                case "BR":
                    RootGrid.Cursor = Cursors.SizeNWSE;
                    break;
                case "TR":
                case "BL":
                    RootGrid.Cursor = Cursors.SizeNESW;
                    break;
                default:
                    RootGrid.Cursor = Cursors.Arrow;
                    break;
            }
        }

        private void RootGrid_MouseMove(object sender, MouseEventArgs e)
        {
            if (_resizing)
            {
                Point cur = ScreenDip(e.GetPosition(this));
                double dx = cur.X - _startCursor.X;
                double dy = cur.Y - _startCursor.Y;

                double l = _startRect.X;
                double t = _startRect.Y;
                double w = _startRect.Width;
                double h = _startRect.Height;

                if (_grip.Contains("L"))
                {
                    l = _startRect.X + dx;
                    w = _startRect.Width - dx;
                }
                if (_grip.Contains("R"))
                {
                    w = _startRect.Width + dx;
                }
                if (_grip.Contains("T"))
                {
                    t = _startRect.Y + dy;
                    h = _startRect.Height - dy;
                }
                if (_grip.Contains("B"))
                {
                    h = _startRect.Height + dy;
                }

                if (w < MinW)
                {
                    if (_grip.Contains("L"))
                    {
                        l = _startRect.X + _startRect.Width - MinW;
                    }
                    w = MinW;
                }
                if (h < MinH)
                {
                    if (_grip.Contains("T"))
                    {
                        t = _startRect.Y + _startRect.Height - MinH;
                    }
                    h = MinH;
                }

                Left = l;
                Top = t;
                Width = w;
                Height = h;
                e.Handled = true;
                return;
            }

            Point p = e.GetPosition(RootGrid);
            _grip = HitGrip(p);
            SetGripCursor(_grip);
        }

        private void RootGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point p = e.GetPosition(RootGrid);
            string grip = HitGrip(p);
            if (grip == "None")
            {
                return;
            }
            _grip = grip;
            _resizing = true;
            _startCursor = ScreenDip(e.GetPosition(this));
            _startRect = new Rect(Left, Top, ActualWidth, ActualHeight);
            RootGrid.CaptureMouse();
            e.Handled = true;
        }

        private void RootGrid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_resizing)
            {
                return;
            }
            _resizing = false;
            RootGrid.ReleaseMouseCapture();
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }
            try
            {
                DragMove();
            }
            catch
            {
            }
        }

        private void BtnMBClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BtnMBInkBlack_Click(object sender, RoutedEventArgs e)
        {
            _penColor = Colors.Black;
            ApplyPen();
        }

        private void BtnMBInkRed_Click(object sender, RoutedEventArgs e)
        {
            _penColor = Color.FromRgb(0xD3, 0x2F, 0x2F);
            ApplyPen();
        }

        private void BtnMBInkBlue_Click(object sender, RoutedEventArgs e)
        {
            _penColor = Color.FromRgb(0x1F, 0x6F, 0xC4);
            ApplyPen();
        }

        private void BtnMBInkWhite_Click(object sender, RoutedEventArgs e)
        {
            _penColor = Colors.White;
            ApplyPen();
        }

        private void BtnMBThin_Click(object sender, RoutedEventArgs e)
        {
            _penWidth = 2d;
            ApplyPen();
        }

        private void BtnMBMedium_Click(object sender, RoutedEventArgs e)
        {
            _penWidth = 4d;
            ApplyPen();
        }

        private void BtnMBThick_Click(object sender, RoutedEventArgs e)
        {
            _penWidth = 7d;
            ApplyPen();
        }

        private void BtnMBEraser_Click(object sender, RoutedEventArgs e)
        {
            MiniInkCanvas.EditingMode = InkCanvasEditingMode.EraseByStroke;
        }

        private void BtnMBInk_Click(object sender, RoutedEventArgs e)
        {
            ApplyPen();
        }

        private void BtnMBClear_Click(object sender, RoutedEventArgs e)
        {
            MiniInkCanvas.Strokes.Clear();
        }

        private void BtnMBBg_Click(object sender, RoutedEventArgs e)
        {
            SetBoard(!_darkBoard);
        }

        private void BtnMBSizeS_Click(object sender, RoutedEventArgs e)
        {
            Width = 340;
            Height = 260;
        }

        private void BtnMBSizeM_Click(object sender, RoutedEventArgs e)
        {
            Width = 480;
            Height = 360;
        }

        private void BtnMBSizeL_Click(object sender, RoutedEventArgs e)
        {
            Width = 760;
            Height = 560;
        }
    }
}
