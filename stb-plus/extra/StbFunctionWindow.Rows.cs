using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace InkCanvasPlus
{
    /// <summary>
    /// 希沃白板5 同款表达式行：标题「表达式 n」+ 右上 ×；输入行 y= + 输入框 + 框右绿色键盘图标。
    /// 错误提示、增加表达式 n/6、插入按钮禁用态均对照希沃实物。
    /// </summary>
    public partial class StbFunctionWindow
    {
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

            TextBlock hdr = new TextBlock
            {
                Text = "表达式 " + (index + 1),
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(0x6B, 0x72, 0x80)),
                VerticalAlignment = VerticalAlignment.Center
            };

            Button del = new Button
            {
                Content = "×",
                Width = 22,
                Height = 22,
                FontSize = 13,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = new SolidColorBrush(Color.FromRgb(0x8A, 0x90, 0x9A)),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            Grid head = new Grid();
            head.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            head.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Grid.SetColumn(hdr, 0);
            Grid.SetColumn(del, 1);
            head.Children.Add(hdr);
            head.Children.Add(del);

            TextBlock ylab = new TextBlock
            {
                Text = "y=",
                FontStyle = FontStyles.Italic,
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(0x1F, 0x23, 0x29)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0)
            };

            TextBox box = new TextBox
            {
                Text = expr.StartsWith("y=") ? expr.Substring(2) : expr,
                FontSize = 14,
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xE3, 0xE6, 0xEC)),
                Background = Brushes.White,
                Padding = new Thickness(6, 4, 6, 4),
                VerticalContentAlignment = VerticalAlignment.Center
            };

            Button kb = new Button
            {
                Content = "⌨",
                Width = 26,
                Height = 24,
                FontSize = 13,
                Margin = new Thickness(6, 0, 0, 0),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = new SolidColorBrush(Color.FromRgb(0x1F, 0x8F, 0x5F)),
                Cursor = System.Windows.Input.Cursors.Hand,
                ToolTip = "快捷输入"
            };
            if (!StbPrefs.Keypad)
            {
                kb.Visibility = Visibility.Collapsed;
            }

            Grid input = new Grid();
            input.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            input.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            input.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Grid.SetColumn(ylab, 0);
            Grid.SetColumn(box, 1);
            Grid.SetColumn(kb, 2);
            input.Children.Add(ylab);
            input.Children.Add(box);
            input.Children.Add(kb);

            StackPanel inner = new StackPanel();
            inner.Children.Add(head);
            inner.Children.Add(input);

            StackPanel line = new StackPanel { Orientation = Orientation.Horizontal };
            line.Children.Add(colorBar);
            line.Children.Add(inner);

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

            box.GotFocus += (s, e) => { _activeBox = box; };
            box.TextChanged += (s, e) => { row.Expr = "y=" + box.Text; RefreshAll(); };
            kb.Click += (s, e) => StbOpenKeypadFor(box, kb);
            del.Click += (s, e) =>
            {
                _rows.Remove(row);
                if (_activeBox == box)
                {
                    _activeBox = null;
                }
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
                if (line == null || line.Children.Count < 2)
                {
                    continue;
                }
                Border bar = line.Children[0] as Border;
                if (bar != null)
                {
                    bar.Background = new SolidColorBrush(r.Color);
                }
                StackPanel inner = line.Children[1] as StackPanel;
                if (inner != null && inner.Children.Count > 0)
                {
                    Grid head = inner.Children[0] as Grid;
                    if (head != null && head.Children.Count > 0)
                    {
                        TextBlock hdr = head.Children[0] as TextBlock;
                        if (hdr != null)
                        {
                            hdr.Text = "表达式 " + (i + 1);
                        }
                    }
                }
            }
        }

        private void RefreshAll()
        {
            StbRefreshPreview();
            List<StbCurve> curves = new List<StbCurve>();
            int bad = 0;

            foreach (Row r in _rows)
            {
                string text = r.Box.Text == null ? string.Empty : r.Box.Text;
                if (!text.StartsWith("y="))
                {
                    text = "y=" + text;
                }
                Func<double, double> f = StbFuncEngine.Compile(StbNorm(text));
                if (f == null)
                {
                    bool blank = text.Replace(" ", string.Empty).Replace("y=", string.Empty).Length == 0;
                    if (!blank)
                    {
                        bad++;
                    }
                    r.Root.BorderBrush = new SolidColorBrush(blank
                        ? Color.FromRgb(0xE3, 0xE6, 0xEC)
                        : Color.FromRgb(0xD9, 0x30, 0x25));
                    continue;
                }
                r.Root.BorderBrush = new SolidColorBrush(Color.FromRgb(0xE3, 0xE6, 0xEC));
                StbCurve c = new StbCurve { Expr = text, Color = r.Color, F = f };
                curves.Add(c);
            }

            Plot.Curves = curves;
            Plot.Refresh();
            TextExprCount.Text = "已画 " + curves.Count + " 条";
            BtnAddExpr.IsEnabled = _rows.Count < MaxExpr;

            TextBlock add = new TextBlock { FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(0x1F, 0x23, 0x29)) };
            add.Inlines.Add(new Run("＋ ") { Foreground = new SolidColorBrush(Color.FromRgb(0x1F, 0x8F, 0x5F)), FontWeight = FontWeights.Bold });
            add.Inlines.Add(new Run("增加表达式  "));
            add.Inlines.Add(new Run(_rows.Count + "/" + MaxExpr) { Foreground = new SolidColorBrush(Color.FromRgb(0x1F, 0x8F, 0x5F)) });
            BtnAddExpr.Content = add;

            if (bad > 0)
            {
                TextHint.Text = "抱歉，你的表达式有些难懂。";
                TextHint.Visibility = Visibility.Visible;
            }
            else
            {
                TextHint.Visibility = Visibility.Collapsed;
            }

            BtnInsert.IsEnabled = curves.Count > 0;
            BtnInsert.Background = new SolidColorBrush(curves.Count > 0
                ? Color.FromRgb(0x1F, 0x8F, 0x5F)
                : Color.FromRgb(0xBF, 0xE3, 0xD2));
        }
    }
}
