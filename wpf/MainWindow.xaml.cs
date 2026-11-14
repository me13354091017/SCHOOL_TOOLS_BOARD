using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;

namespace InkBoard
{
    public partial class MainWindow : Window
    {
        Doc _doc;
        BoardView _board;
        readonly History _history = new History();
        ToolKind _tool = ToolKind.Pen;
        ShapeKind _shapeKind = ShapeKind.Line;
        string _color = "#111827";
        double _penWidth = 7;
        bool _presenting;
        bool _draggingCurtain;
        RadialGradientBrush _spot;
        double _spotRadius = 220;
        Point _pointer;
        DispatcherTimer _timer;
        int _seconds;

        static readonly string[] Palette =
        { "#111827", "#ef4444", "#f59e0b", "#0e9f76", "#2563eb", "#7c3aed", "#db2777", "#0ea5e9", "#65a30d", "#64748b" };
        static readonly string[] MarkerPalette = { "#fde047", "#f472b6", "#67e8f9", "#86efac", "#fdba74" };
        static readonly double[] Widths = { 4, 7, 10 };

        public MainWindow()
        {
            InitializeComponent();

            _doc = Doc.Create();
            _board = new BoardView { Doc = _doc };
            _board.Commit = () => _history.Push(_doc);
            _board.Changed = UpdateTitle;
            _board.AskText = AskTextDialog;
            BoardHost.Children.Add(_board);

            BuildTopBar();
            BuildToolBar();
            BuildRailFooter();
            RefreshPages();
            _history.Reset(_doc);

            Loaded += (s, e) => { _board.FitPage(); };
            PreviewKeyDown += OnKeyDown;
            PreviewMouseMove += OnAnyMove;
            PreviewMouseWheel += OnAnyWheel;

            Curtain.MouseLeftButtonDown += OnCurtainDown;
            Curtain.MouseMove += OnCurtainMove;
            Curtain.MouseLeftButtonUp += OnCurtainUp;
            TimerChip.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ClickCount == 2) { _seconds = 0; TimerText.Text = "00:00"; }
                else if (_timer != null) { if (_timer.IsEnabled) _timer.Stop(); else _timer.Start(); }
            };
        }

        /* ------------------------------ UI 构建 ------------------------------ */
        Style ToolStyle => (Style)Application.Current.Resources["ToolButton"];
        Brush Accent => (Brush)Application.Current.Resources["InkAccent"];

        Button Btn(string text, Action act, string tip, double pad = 10)
        {
            var b = new Button { Content = text, Style = ToolStyle, ToolTip = tip };
            b.Padding = new Thickness(pad, 6, pad, 6);
            b.Click += (s, e) => act();
            return b;
        }

        FrameworkElement Sep()
        {
            return new Border { Width = 1, Margin = new Thickness(8, 4, 8, 4), Background = new SolidColorBrush(Color.FromRgb(0xE2, 0xE8, 0xF0)) };
        }

        void BuildTopBar()
        {
            TopBar.Children.Add(Btn("打开", () => OpenDoc(), "Ctrl+O"));
            TopBar.Children.Add(Btn("保存", () => SaveDoc(), "Ctrl+S"));
            TopBar.Children.Add(Btn("导出 PNG", ExportPng, "导出当前页"));
            TopBar.Children.Add(Btn("导出 PDF", ExportPdf, "打印为 PDF（系统打印机）"));
            TopBar.Children.Add(Sep());
            TopBar.Children.Add(Btn("导入 PDF", () => ImportPdf(), "把 PDF 每页作为白板背景"));
            TopBar.Children.Add(Btn("适应页面", () => _board.FitPage(), "Ctrl+0"));
            TopBar.Children.Add(Btn("性能", () => { _board.ShowPerf = !_board.ShowPerf; _board.InvalidateStatic(); }, "FPS 面板"));
            TopBar.Children.Add(Sep());
            TopBar.Children.Add(Btn("全屏授课", TogglePresent, "F / Esc 退出"));
            TopBar.Children.Add(Btn("计时器", ToggleTimer, "点击计时器暂停/继续，双击归零"));
            TopBar.Children.Add(Btn("聚光灯", ToggleSpot, "滚轮调半径"));
            TopBar.Children.Add(Btn("遮罩", ToggleCurtain, "向下拖拽揭幕，双击关闭"));
        }

        void BuildToolBar()
        {
            AddTool("笔", ToolKind.Pen, "P");
            AddTool("荧光", ToolKind.Marker, "M");
            AddTool("橡皮", ToolKind.Eraser, "E");
            AddTool("图形", ToolKind.Shape, "S");
            AddTool("选择", ToolKind.Select, "V");
            AddTool("文字", ToolKind.Text, "T");
            AddTool("激光", ToolKind.Laser, "L");
            AddTool("漫游", ToolKind.Hand, "H");
            ToolBar.Children.Add(Sep());

            foreach (var c in Palette) ToolBar.Children.Add(Swatch(c, false));
            ToolBar.Children.Add(Sep());
            foreach (var c in MarkerPalette) ToolBar.Children.Add(Swatch(c, true));
            ToolBar.Children.Add(Sep());

            foreach (var w in Widths)
            {
                double ww = w;
                ToolBar.Children.Add(Btn(ww < 5 ? "细" : (ww < 9 ? "中" : "粗"), () => { _penWidth = ww; _board.PenWidth = ww; }, ww + "px"));
            }
            ToolBar.Children.Add(Sep());

            AddShape("直线", ShapeKind.Line);
            AddShape("箭头", ShapeKind.Arrow);
            AddShape("矩形", ShapeKind.Rect);
            AddShape("圆", ShapeKind.Ellipse);
            AddShape("三角", ShapeKind.Triangle);
            AddShape("坐标系", ShapeKind.Axis);
            AddShape("函数", ShapeKind.Func);
            ToolBar.Children.Add(Sep());

            var smart = new Button { Content = "智能转图形", Style = ToolStyle, ToolTip = "手绘自动变成直线 / 三角 / 矩形 / 圆" };
            PaintChip(smart, _board.SmartShape);
            smart.Click += (s, e) => { _board.SmartShape = !_board.SmartShape; PaintChip(smart, _board.SmartShape); };
            ToolBar.Children.Add(smart);

            var fill = new Button { Content = "填充", Style = ToolStyle, ToolTip = "图形填充" };
            fill.Click += (s, e) =>
            {
                _board.FillShape = !_board.FillShape;
                PaintChip(fill, _board.FillShape);
            };
            ToolBar.Children.Add(fill);
        }

        void PaintChip(Button b, bool on)
        {
            b.Background = on ? Accent : (Brush)new SolidColorBrush(Color.FromRgb(0xF1, 0xF5, 0xF9));
            b.Foreground = on ? Brushes.White : (Brush)new SolidColorBrush(Color.FromRgb(0x33, 0x41, 0x55));
        }

        void AddTool(string text, ToolKind kind, string key)
        {
            var b = Btn(text, () => SetTool(kind), key + " 快捷键");
            b.Tag = kind;
            ToolBar.Children.Add(b);
        }

        Button Swatch(string hex, bool marker)
        {
            var b = new Button
            {
                Width = 28, Height = 28, Margin = new Thickness(2),
                Background = FromHex(hex),
                Style = ToolStyle,
                ToolTip = hex
            };
            b.Padding = new Thickness(0);
            b.Click += (s, e) =>
            {
                _color = hex;
                _board.ColorHex = hex;
                if (marker) SetTool(ToolKind.Marker);
            };
            return b;
        }

        void AddShape(string text, ShapeKind kind)
        {
            var b = Btn(text, () =>
            {
                if (kind == ShapeKind.Func)
                {
                    var expr = AskTextDialog(_board.FuncExpr);
                    if (expr == null) return;
                    _board.FuncExpr = expr;
                }
                _shapeKind = kind;
                _board.Shape = kind;
                SetTool(ToolKind.Shape);
            }, "");
            ToolBar.Children.Add(b);
        }

        void BuildRailFooter()
        {
            var bg = new ComboBox { Margin = new Thickness(0, 0, 0, 6) };
            foreach (var v in new[] { "plain", "grid", "dots", "lines", "black" }) bg.Items.Add(v);
            bg.SelectedIndex = 1;
            bg.SelectionChanged += (s, e) =>
            {
                if (_doc.Cur == null) return;
                _doc.Cur.Bg = (string)bg.SelectedItem;
                _board.InvalidateStatic();
                _history.Push(_doc);
            };
            RailFooter.Children.Add(new TextBlock { Text = "页面背景", FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B)) });
            RailFooter.Children.Add(bg);

            var ratio = new ComboBox { Margin = new Thickness(0, 6, 0, 0) };
            ratio.Items.Add("16:9");
            ratio.Items.Add("4:3");
            ratio.SelectedIndex = 0;
            ratio.SelectionChanged += (s, e) =>
            {
                _doc.PageW = ratio.SelectedIndex == 1 ? 1440 : 1920;
                _doc.PageH = 1080;
                _board.FitPage();
                _board.InvalidateAll();
            };
            RailFooter.Children.Add(new TextBlock { Text = "页面比例", FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B)) });
            RailFooter.Children.Add(ratio);
        }

        void SetTool(ToolKind kind)
        {
            _tool = kind;
            _board.Tool = kind;
            foreach (var child in ToolBar.Children)
            {
                var b = child as Button;
                if (b == null || b.Tag == null) continue;
                bool active = (ToolKind)b.Tag == kind;
                b.Background = active ? Accent : (Brush)new SolidColorBrush(Color.FromRgb(0xF1, 0xF5, 0xF9));
                b.Foreground = active ? Brushes.White : (Brush)new SolidColorBrush(Color.FromRgb(0x33, 0x41, 0x55));
            }
            if (kind == ToolKind.Marker) _penWidth = 10; else if (_penWidth > 10) _penWidth = 7;
            _board.PenWidth = _penWidth;
        }

        /* ------------------------------ 页面列表 ------------------------------ */
        void RefreshPages()
        {
            PageList.Children.Clear();
            for (int i = 0; i < _doc.Pages.Count; i++)
            {
                int idx = i;
                var b = new Button
                {
                    Content = (i + 1).ToString(),
                    Style = ToolStyle,
                    Height = 70,
                    Margin = new Thickness(0, 0, 0, 6),
                    FontSize = 16
                };
                if (i == _doc.Current)
                {
                    b.Background = Accent;
                    b.Foreground = Brushes.White;
                }
                b.Click += (s, e) => { _doc.Current = idx; _board.InvalidateAll(); RefreshPages(); };
                PageList.Children.Add(b);
            }
            UpdateTitle();
        }

        void UpdateTitle()
        {
            Title = string.Format("墨板 InkBoard · 第 {0}/{1} 页", _doc.Current + 1, _doc.Pages.Count);
        }

    }
}
