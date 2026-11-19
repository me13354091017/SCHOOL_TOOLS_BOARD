using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace InkCanvasPlus
{
    public partial class StbFunctionWindow
    {
        private Popup _keypadPopup;

        private Button MakeKey(string label, double w, double h, Color bg, Color fg)
        {
            return new Button
            {
                Content = label,
                Width = w,
                Height = h,
                Margin = new Thickness(0, 0, 5, 5),
                FontSize = 13,
                Background = new SolidColorBrush(bg),
                Foreground = new SolidColorBrush(fg),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xE3, 0xE6, 0xEC)),
                BorderThickness = new Thickness(1),
                Cursor = System.Windows.Input.Cursors.Hand
            };
        }

        private void BuildKeypad()
        {
            Border shell = new Border
            {
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xD8, 0xDD, 0xE5)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(10, 8, 10, 10)
            };
            shell.Effect = new DropShadowEffect { BlurRadius = 22, ShadowDepth = 3, Opacity = 0.22 };

            StackPanel col = new StackPanel { Width = 486 };

            Grid head = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            head.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            head.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBlock title = new TextBlock
            {
                Text = "\u5FEB\u6377\u8F93\u5165",
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x1F, 0x23, 0x29)),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(title, 0);
            head.Children.Add(title);

            Button close = MakeKey("\u2715", 26, 22, Color.FromRgb(0xD9, 0x30, 0x25), Colors.White);
            close.Margin = new Thickness(0);
            close.Click += (a, b) => StbInsertToken("@CLOSE");
            Grid.SetColumn(close, 1);
            head.Children.Add(close);
            col.Children.Add(head);

            WrapPanel keys = new WrapPanel { Width = 486 };

            string[][] list = new string[][]
            {
                new string[] { "7", "7" }, new string[] { "8", "8" }, new string[] { "9", "9" }, new string[] { "+", "+" },
                new string[] { "sin", "sin(" }, new string[] { "cos", "cos(" }, new string[] { "tan", "tan(" }, new string[] { "x", "x" },
                new string[] { "4", "4" }, new string[] { "5", "5" }, new string[] { "6", "6" }, new string[] { "-", "-" },
                new string[] { "e", "e" }, new string[] { "pi", "pi" }, new string[] { "log2", "log2(" }, new string[] { "ln", "ln(" },
                new string[] { "1", "1" }, new string[] { "2", "2" }, new string[] { "3", "3" }, new string[] { "*", "*" },
                new string[] { "x2", "^2" }, new string[] { "x3", "^3" }, new string[] { "sqrt", "sqrt(" }, new string[] { "abs", "abs(" },
                new string[] { "0", "0" }, new string[] { ".", "." }, new string[] { "/", "/" }, new string[] { "(", "(" },
                new string[] { ")", ")" }, new string[] { "DEL", "@BACK" }, new string[] { "CLR", "@CLS" }, new string[] { "OK", "@RUN" }
            };

            foreach (string[] k in list)
            {
                string token = k[1];
                Color bg = Color.FromRgb(0xF3, 0xF5, 0xF8);
                Color fg = Color.FromRgb(0x1F, 0x23, 0x29);
                bool digit = token.Length == 1 && (char.IsDigit(token[0]) || token == "." || token == "+" || token == "-" || token == "*" || token == "/");
                if (digit || token == "@BACK" || token == "@CLS")
                {
                    bg = Colors.White;
                }
                else if (token == "@RUN")
                {
                    bg = Color.FromRgb(0x1F, 0x8F, 0x5F);
                    fg = Colors.White;
                }
                Button b = MakeKey(k[0], 56, 34, bg, fg);
                b.Click += (a, c) => StbInsertToken(token);
                keys.Children.Add(b);
            }

            col.Children.Add(keys);
            shell.Child = col;

            _keypadPopup = new Popup
            {
                StaysOpen = true,
                AllowsTransparency = true,
                PopupAnimation = PopupAnimation.Fade,
                PlacementTarget = BtnToggleKeypad,
                Placement = PlacementMode.Top,
                VerticalOffset = -4,
                Child = shell
            };
        }

        private void BtnToggleKeypad_Click(object sender, RoutedEventArgs e)
        {
            if (_keypadPopup == null)
            {
                BuildKeypad();
            }
            _keypadPopup.PlacementTarget = BtnToggleKeypad;
            _keypadPopup.IsOpen = !_keypadPopup.IsOpen;
            if (_keypadPopup.IsOpen && _activeBox == null && _rows.Count > 0)
            {
                _activeBox = _rows[_rows.Count - 1].Box;
            }
        }

        /// <summary>insert token at caret of current expression box.</summary>
        private void StbInsertToken(string token)
        {
            if (token == "@CLOSE")
            {
                if (_keypadPopup != null)
                {
                    _keypadPopup.IsOpen = false;
                }
                return;
            }
            if (token == "@RUN")
            {
                RefreshAll();
                Plot.Refresh();
                return;
            }

            TextBox box = _activeBox;
            if (box == null && _rows.Count > 0)
            {
                box = _rows[_rows.Count - 1].Box;
            }
            if (box == null)
            {
                return;
            }

            if (token == "@CLS")
            {
                box.Text = string.Empty;
                box.Focus();
                box.CaretIndex = 0;
                return;
            }

            string current = box.Text == null ? string.Empty : box.Text;
            int start = box.SelectionStart < 0 ? current.Length : box.SelectionStart;
            int sel = box.SelectionLength < 0 ? 0 : box.SelectionLength;
            int caret = start;

            if (token == "@BACK")
            {
                if (sel > 0)
                {
                    box.Text = current.Remove(start, sel);
                }
                else if (start > 0)
                {
                    box.Text = current.Remove(start - 1, 1);
                    caret = start - 1;
                }
            }
            else
            {
                box.Text = current.Substring(0, start) + token + current.Substring(start + sel);
                caret = start + token.Length;
            }

            box.Focus();
            box.CaretIndex = caret;
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
                if (ch >= '\uFF01' && ch <= '\uFF5E')
                {
                    sb.Append((char)(ch - 0xFEE0));
                }
                else if (ch == '\u3000')
                {
                    sb.Append(' ');
                }
                else
                {
                    sb.Append(ch);
                }
            }
            string s = sb.ToString();
            s = s.Replace("\u00B2", "^2").Replace("\u00B3", "^3")
                 .Replace("\u00D7", "*").Replace("\u00F7", "/")
                 .Replace("\u2212", "-").Replace("\u2013", "-")
                 .Replace("\u03C0", "pi")
                 .Replace("{", "(").Replace("}", ")");
            return s;
        }
    }
}
