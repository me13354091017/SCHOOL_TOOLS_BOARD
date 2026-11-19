using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace InkCanvasPlus
{
    /// <summary>「插入函数」窗口：选类型 → 填表达式 → 画坐标图 → 插入到白板。</summary>
    public partial class StbFunctionWindow : Window
    {
        private const int MaxExpr = 6;
        private static StbFunctionWindow _instance;

        private static readonly Color[] Palette = new Color[]
        {
            Color.FromRgb(0x1F, 0x23, 0x29),
            Color.FromRgb(0xD9, 0x30, 0x25),
            Color.FromRgb(0x1A, 0x73, 0xE8),
            Color.FromRgb(0x1E, 0x8E, 0x3E),
            Color.FromRgb(0xE8, 0x71, 0x0A),
            Color.FromRgb(0x8E, 0x24, 0xAA)
        };

        private sealed class Row
        {
            internal Border Root;
            internal TextBox Box;
            internal Color Color;
            internal string Expr;
        }

        private readonly List<Row> _rows = new List<Row>();
        private MainWindow _owner;

        public static void ShowFor(MainWindow owner)
        {
            if (_instance != null)
            {
                _instance.Activate();
                return;
            }
            StbFunctionWindow w = new StbFunctionWindow(owner);
            _instance = w;
            w.Closed += (s, e) => { _instance = null; };
            w.Show();
            w.Activate();
        }

        public StbFunctionWindow(MainWindow owner)
        {
            _owner = owner;
            InitializeComponent();
            if (owner != null && owner.IsLoaded)
            {
                Owner = owner;
            }
            AddRow("y=x");
            RefreshAll();
        }

        private void BtnCat_Click(object sender, RoutedEventArgs e)
        {
            Button b = sender as Button;
            if (b == null || b.Tag == null)
            {
                return;
            }
            string expr = b.Tag.ToString();
            if (_rows.Count == 0)
            {
                AddRow(expr);
            }
            else
            {
                _rows[_rows.Count - 1].Box.Text = expr;
            }
        }

        private void BtnAddExpr_Click(object sender, RoutedEventArgs e)
        {
            AddRow("y=");
        }

        private void AddRow(string expr)
        {
            if (_rows.Count >= MaxExpr)
            {
                return;
            }

            int index = _rows.Count;
            Color color = Palette[index % Palette.Length];

            Border colorBar = new Border
            {
                Width = 4,
                CornerRadius = new CornerRadius(2),
                Background = new SolidColorBrush(color),
                Margin = new Thickness(0, 3, 8, 3)
            };

            TextBox box = new TextBox
            {
                Text = expr,
                FontSize = 14,
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xE3, 0xE6, 0xEC)),
                Background = Brushes.White,
                Padding = new Thickness(6, 4, 6, 4),
                VerticalContentAlignment = VerticalAlignment.Center,
                Width = 196
            };

            Button del = new Button
            {
                Content = "×",
                Width = 24,
                Height = 24,
                Margin = new Thickness(8, 0, 0, 0),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = new SolidColorBrush(Color.FromRgb(0x8A, 0x90, 0x9A)),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            StackPanel line = new StackPanel { Orientation = Orientation.Horizontal };
            line.Children.Add(colorBar);
            line.Children.Add(box);
            line.Children.Add(del);

            Border root = new Border
            {
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xE3, 0xE6, 0xEC)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(8, 6, 8, 6),
                Margin = new Thickness(0, 0, 0, 8),
                Child = line
            };

            Row row = new Row { Root = root, Box = box, Color = color, Expr = expr };
            _rows.Add(row);

            box.TextChanged += (s, e) => { row.Expr = box.Text; RefreshAll(); };
            del.Click += (s, e) =>
            {
                _rows.Remove(row);
                PanelExpr.Children.Remove(root);
                RecolorRows();
                RefreshAll();
            };

            PanelExpr.Children.Add(root);
            RefreshAll();
        }

        private void RecolorRows()
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                Row r = _rows[i];
                r.Color = Palette[i % Palette.Length];
                StackPanel line = r.Root.Child as StackPanel;
                if (line != null && line.Children.Count > 0)
                {
                    Border bar = line.Children[0] as Border;
                    if (bar != null)
                    {
                        bar.Background = new SolidColorBrush(r.Color);
                    }
                }
            }
        }

        private void RefreshAll()
        {
            List<StbCurve> curves = new List<StbCurve>();
            foreach (Row r in _rows)
            {
                Func<double, double> f = StbFuncEngine.Compile(r.Box.Text);
                if (f == null)
                {
                    continue;
                }
                curves.Add(new StbCurve { Expr = r.Box.Text, Color = r.Color, F = f });
            }

            Plot.Curves = curves;
            Plot.Refresh();
            TextExprCount.Text = _rows.Count + " / " + MaxExpr;
            BtnAddExpr.IsEnabled = _rows.Count < MaxExpr;
        }

        private void BtnInsert_Click(object sender, RoutedEventArgs e)
        {
            if (_owner == null)
            {
                return;
            }
            try
            {
                double w = Math.Max(1d, Plot.ActualWidth);
                double h = Math.Max(1d, Plot.ActualHeight);

                RenderTargetBitmap bmp = new RenderTargetBitmap(
                    (int)Math.Round(w * 2d), (int)Math.Round(h * 2d), 192d, 192d, PixelFormats.Pbgra32);
                bmp.Render(Plot);

                string caption = "函数图像";
                foreach (StbCurve c in Plot.Curves)
                {
                    caption = c.Expr;
                    break;
                }

                _owner.StbInsertFunctionImage(bmp, caption);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("插入函数失败：" + ex.Message, "SchoolToolsBoard",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Plot.Refresh();
        }
    }
}
