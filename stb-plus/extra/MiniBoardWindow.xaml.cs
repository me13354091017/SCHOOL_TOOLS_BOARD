using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;

namespace InkCanvasPlus
{
    /// <summary>
    /// 板中板（MiniBoard）：叠在主窗口上方的小白板，可拖动、可换底色、可单独书写与清屏。
    /// 由 stb-plus 补丁新增，不依赖上游代码。
    /// </summary>
    public partial class MiniBoardWindow : Window
    {
        private static MiniBoardWindow _instance;
        private double _penWidth = 3d;
        private Color _penColor = Colors.Black;
        private bool _darkBoard = false;

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
