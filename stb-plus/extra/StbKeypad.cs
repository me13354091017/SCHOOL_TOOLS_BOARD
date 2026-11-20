using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace InkCanvasPlus
{
    /// <summary>
    /// 希沃白板5 同款快捷输入键盘（布局对照 EasiNote 5.2.4 实物）：
    /// 左米色数字区 7 8 9 + / 4 5 6 - / 1 2 3 × / 0(双宽) . ÷ ；
    /// 右白色函数区 sin cos tan x / e π log2 ln / 幂 根 平方 / 绝对值 括号 分数，
    /// 红色退格、绿色回车（跨两行）、左右移动光标键。模板键作用于二维公式框。
    /// </summary>
    public partial class StbFunctionWindow
    {
        private Popup _keypadPopup;

        private static readonly Color CreamBg = Color.FromRgb(0xF7, 0xEC, 0xDF);
        private static readonly Color CreamLine = Color.FromRgb(0xEF, 0xE0, 0xCC);
        private static readonly Color WhiteLine = Color.FromRgb(0xEC, 0xEE, 0xF1);
        private static readonly Color KeyFg = Color.FromRgb(0x4B, 0x4F, 0x55);

        private Button MakeCell(object content, string token, Color bg, Color line, Color fg, double w, double h)
        {
            Button b = new Button
            {
                Content = content,
                Width = w,
                Height = h,
                FontSize = 15,
                Background = new SolidColorBrush(bg),
                Foreground = new SolidColorBrush(fg),
                BorderBrush = new SolidColorBrush(line),
                BorderThickness = new Thickness(0, 0, 1, 1),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            b.Click += (a, c) => StbInsertToken(token);
            return b;
        }

        private static TextBlock Sup(string main, string sup)
        {
            TextBlock t = new TextBlock { FontSize = 15, Foreground = new SolidColorBrush(KeyFg) };
            t.Inlines.Add(new Run(main));
            Run r = new Run(sup) { FontSize = 10, BaselineAlignment = BaselineAlignment.Superscript };
            t.Inlines.Add(r);
            return t;
        }

        private static TextBlock SubLog()
        {
            TextBlock t = new TextBlock { FontSize = 15, Foreground = new SolidColorBrush(KeyFg) };
            t.Inlines.Add(new Run("log"));
            t.Inlines.Add(new Run("□") { FontSize = 9, BaselineAlignment = BaselineAlignment.Subscript });
            t.Inlines.Add(new Run("□") { FontSize = 11 });
            return t;
        }

        private static StackPanel Frac()
        {
            StackPanel p = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
            p.Children.Add(new TextBlock { Text = "□", FontSize = 10, Foreground = new SolidColorBrush(KeyFg), HorizontalAlignment = HorizontalAlignment.Center });
            p.Children.Add(new Border { Height = 1, Width = 16, Background = new SolidColorBrush(KeyFg), Margin = new Thickness(0, 1, 0, 1) });
            p.Children.Add(new TextBlock { Text = "□", FontSize = 10, Foreground = new SolidColorBrush(KeyFg), HorizontalAlignment = HorizontalAlignment.Center });
            return p;
        }

        private void BuildKeypad()
        {
            Border shell = new Border
            {
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xD8, 0xDD, 0xE5)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(0)
            };
            shell.Effect = new DropShadowEffect { BlurRadius = 22, ShadowDepth = 3, Opacity = 0.22 };

            StackPanel hor = new StackPanel { Orientation = Orientation.Horizontal };

            // ---- 左：米色数字区 ----
            Grid num = new Grid();
            for (int i = 0; i < 4; i++)
            {
                num.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(58) });
                num.RowDefinitions.Add(new RowDefinition { Height = new GridLength(52) });
            }
            string[][] nk = new string[][]
            {
                new string[] { "7", "7" }, new string[] { "8", "8" }, new string[] { "9", "9" }, new string[] { "+", "+" },
                new string[] { "4", "4" }, new string[] { "5", "5" }, new string[] { "6", "6" }, new string[] { "−", "-" },
                new string[] { "1", "1" }, new string[] { "2", "2" }, new string[] { "3", "3" }, new string[] { "×", "*" },
                new string[] { "0", "0" }, new string[] { ".", "." }, new string[] { "÷", "/" }
            };
            int[] npos = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 14, 15 };
            for (int i = 0; i < nk.Length; i++)
            {
                Button b = MakeCell(nk[i][0], nk[i][1], CreamBg, CreamLine, KeyFg, 58, 52);
                int cell = npos[i];
                Grid.SetRow(b, cell / 4);
                Grid.SetColumn(b, cell % 4);
                if (i == 12)
                {
                    Grid.SetColumnSpan(b, 2);
                    b.Width = 116;
                }
                num.Children.Add(b);
            }
            hor.Children.Add(num);

            // ---- 右：白色函数区 ----
            Grid fn = new Grid();
            double[] cw = new double[] { 58, 58, 66, 58, 30, 32 };
            for (int i = 0; i < 6; i++)
            {
                fn.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(cw[i]) });
            }
            for (int i = 0; i < 4; i++)
            {
                fn.RowDefinitions.Add(new RowDefinition { Height = new GridLength(52) });
            }

            Action<object, string, int, int, int, int> put = (content, token, r, c, rs, cs) =>
            {
                Button b = MakeCell(content, token, Colors.White, WhiteLine, KeyFg, cw[c], 52);
                Grid.SetRow(b, r);
                Grid.SetColumn(b, c);
                if (rs > 1) { Grid.SetRowSpan(b, rs); b.Height = 52 * rs; }
                if (cs > 1) { Grid.SetColumnSpan(b, cs); }
                fn.Children.Add(b);
            };

            put("sin", "@T:func:sin", 0, 0, 1, 1);
            put("cos", "@T:func:cos", 0, 1, 1, 1);
            put("tan", "@T:func:tan", 0, 2, 1, 1);
            put(new TextBlock { Text = "x", FontStyle = FontStyles.Italic, FontSize = 15, Foreground = new SolidColorBrush(KeyFg) }, "x", 0, 3, 1, 1);
            Button back = MakeCell("⌫", "@BACK", Colors.White, WhiteLine, Color.FromRgb(0xD9, 0x30, 0x25), 62, 52);
            Grid.SetRow(back, 0); Grid.SetColumn(back, 4); Grid.SetColumnSpan(back, 2);
            fn.Children.Add(back);

            put("e", "e", 1, 0, 1, 1);
            put("π", "π", 1, 1, 1, 1);
            put(SubLog(), "@T:logb", 1, 2, 1, 1);
            put("ln□", "@T:func:ln", 1, 3, 1, 1);
            Button enter = MakeCell("↵", "@RUN", Colors.White, WhiteLine, Color.FromRgb(0x1F, 0x8F, 0x5F), 62, 104);
            enter.FontSize = 20;
            Grid.SetRow(enter, 1); Grid.SetColumn(enter, 4); Grid.SetColumnSpan(enter, 2); Grid.SetRowSpan(enter, 2);
            fn.Children.Add(enter);

            put(Sup("□", "x"), "@T:powx", 2, 0, 1, 1);
            put("ⁿ√□", "@T:nroot", 2, 1, 1, 1);
            put("√□", "@T:sqrt", 2, 2, 1, 1);
            put(Sup("x", "□"), "@T:pow", 2, 3, 1, 1);

            put("|□|", "@T:abs", 3, 0, 1, 1);
            put("(", "(", 3, 1, 1, 1);
            put(")", ")", 3, 2, 1, 1);
            put(Frac(), "@T:frac", 3, 3, 1, 1);
            put("←", "@LEFT", 3, 4, 1, 1);
            put("→", "@RIGHT", 3, 5, 1, 1);

            hor.Children.Add(fn);
            shell.Child = hor;

            _keypadPopup = new Popup
            {
                StaysOpen = false,
                AllowsTransparency = true,
                PopupAnimation = PopupAnimation.Fade,
                Placement = PlacementMode.Bottom,
                Child = shell
            };
        }

        /// <summary>行内绿色键盘图标：把键盘浮层锚到该行输入框下方（希沃式）。</summary>
        internal void StbOpenKeypadFor(StbMathBox box, Button anchor)
        {
            _activeBox = box;
            if (_keypadPopup == null)
            {
                BuildKeypad();
            }
            _keypadPopup.PlacementTarget = anchor;
            _keypadPopup.HorizontalOffset = -420;
            _keypadPopup.VerticalOffset = 6;
            _keypadPopup.IsOpen = true;
            box.Focus();
        }

        /// <summary>键盘按键 → 二维公式框操作；@T: 为模板键，其余为字符。</summary>
        private void StbInsertToken(string token)
        {
            if (token == "@RUN")
            {
                RefreshAll();
                Plot.Refresh();
                return;
            }

            StbMathBox box = _activeBox;
            if (box == null && _rows.Count > 0)
            {
                box = _rows[_rows.Count - 1].Box;
            }
            if (box == null)
            {
                return;
            }

            if (token == "@BACK")
            {
                box.Backspace();
            }
            else if (token == "@LEFT")
            {
                box.MoveLeft();
            }
            else if (token == "@RIGHT")
            {
                box.MoveRight();
            }
            else if (token.StartsWith("@T:"))
            {
                box.InsertTemplate(token.Substring(3));
            }
            else
            {
                foreach (char c in token)
                {
                    box.InsertChar(c);
                }
            }
            box.Focus();
        }

        /// <summary>normalize full-width chars and math symbols.</summary>
        private static string StbNorm(string text)
        {
            if (text == null)
            {
                return string.Empty;
            }
            System.Text.StringBuilder sb = new System.Text.StringBuilder(text.Length + 8);
            foreach (char ch in text)
            {
                if (ch >= '！' && ch <= '～')
                {
                    sb.Append((char)(ch - 0xFEE0));
                }
                else if (ch == '　')
                {
                    sb.Append(' ');
                }
                else
                {
                    sb.Append(ch);
                }
            }
            string s = sb.ToString();
            s = s.Replace("²", "^2").Replace("³", "^3")
                 .Replace("×", "*").Replace("÷", "/")
                 .Replace("−", "-").Replace("–", "-")
                 .Replace("π", "pi")
                 .Replace("{", "(").Replace("}", ")");
            return s;
        }
    }
}
