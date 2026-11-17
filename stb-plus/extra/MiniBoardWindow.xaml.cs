using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;

namespace InkCanvasPlus
{
    /// <summary>
    /// 板中板（MiniBoard）：叠在主窗口上方的小黑板，可拖动、可四边四角拖拽缩放、可直接手写。
    /// 外观与主程序黑板界面一致：同样 #101A16 深墨绿底、默认白色笔迹、深色浮动工具条。
    /// 画笔粗细也沿用主界面同一套规则：1–20 档（整数），实际笔宽 = 档位 / 2（默认 5 档 = 2.5 磅）。
    /// 由 stb-plus 补丁新增，不依赖上游代码。
    /// </summary>
    public partial class MiniBoardWindow : Window
    {
        private const double EdgeGrip = 8d;
        private const double MinW = 280d;
        private const double MinH = 220d;

        // 与主界面 InkWidthSlider 的取值范围一致（Minimum=1，Maximum=20，TickFrequency=1）
        private const int MinPenLevel = 1;
        private const int MaxPenLevel = 20;

        private static MiniBoardWindow _instance;
        private int _penLevel = 5;
        private Color _penColor = Colors.White;
        private bool _darkBoard = true;

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
            UpdateWidthLabel();
            SetBoard(true);
        }

        /// <summary>当前档位对应的实际笔宽，换算方式和主界面「Settings.Canvas.InkWidth = 档位 / 2」相同。</summary>
        private double PenWidth
        {
            get { return _penLevel / 2.0; }
        }

        private void UpdateWidthLabel()
        {
            if (TextBlockMBWidth != null)
            {
                TextBlockMBWidth.Text = _penLevel.ToString();
            }
        }

        private void ApplyPen()
        {
            MiniInkCanvas.EditingMode = InkCanvasEditingMode.Ink;
            DrawingAttributes da = MiniInkCanvas.DefaultDrawingAttributes.Clone();
            da.Color = _penColor;
            da.Width = PenWidth;
            da.Height = PenWidth;
            da.FitToCurve = false;
            da.IgnorePressure = true;
            MiniInkCanvas.DefaultDrawingAttributes = da;
        }

        private void SetBoard(bool dark)
        {
            _darkBoard = dark;

            Color board = dark ? Color.FromRgb(0x10, 0x1A, 0x16) : Color.FromRgb(0xF7, 0xF8, 0xF6);
            RootBorder.Background = new SolidColorBrush(board);
            RootBorder.BorderBrush = new SolidColorBrush(dark
                ? Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF)
                : Color.FromArgb(0x33, 0x33, 0x32, 0x2F));

            Color fg = dark ? Color.FromArgb(0xB3, 0xE8, 0xEB, 0xEF) : Color.FromArgb(0xB3, 0x33, 0x32, 0x2F);
            TextBlockMiniBoardStatus.Foreground = new SolidColorBrush(fg);
            BtnMBClose.Foreground = new SolidColorBrush(fg);
            HeaderBar.Background = new SolidColorBrush(dark
                ? Color.FromArgb(0x14, 0x00, 0x00, 0x00)
                : Color.FromArgb(0x10, 0x00, 0x00, 0x00));
            ToolBar.Background = new SolidColorBrush(dark
                ? Color.FromArgb(0xCC, 0x17, 0x1E, 0x1A)
                : Color.FromArgb(0xCC, 0xF5, 0xF3, 0xEE));
            ToolBar.BorderBrush = new SolidColorBrush(dark
                ? Color.FromArgb(0x40, 0xFF, 0xFF, 0xFF)
                : Color.FromArgb(0x33, 0x33, 0x32, 0x2F));

            TextBlockMiniBoardStatus.Text = dark ? "板中板 · 黑板" : "板中板 · 白板";
            _penColor = dark ? Colors.White : Colors.Black;
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

        /// <summary>画笔细一档，和主界面「画笔粗细 －」按钮完全同样：档位 -1，夹在 1–20。</summary>
        private void BtnMBWidthDown_Click(object sender, RoutedEventArgs e)
        {
            if (_penLevel <= MinPenLevel)
            {
                return;
            }
            _penLevel -= 1;
            UpdateWidthLabel();
            ApplyPen();
        }

        /// <summary>画笔粗一档，和主界面「画笔粗细 ＋」按钮完全同样：档位 +1，夹在 1–20。</summary>
        private void BtnMBWidthUp_Click(object sender, RoutedEventArgs e)
        {
            if (_penLevel >= MaxPenLevel)
            {
                return;
            }
            _penLevel += 1;
            UpdateWidthLabel();
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
