using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace InkCanvasPlus
{
    /// <summary>把 y=... 这类纯文本表达式排版成数学样式：根号带横线、上标、空心点等。</summary>
    internal static class StbMath
    {
        internal static FrameworkElement Render(string expr, double fontSize, Brush fg)
        {
            StackPanel host = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            if (expr == null)
            {
                expr = string.Empty;
            }
            int i = 0;
            Build(expr, ref i, host, fontSize, fg);
            return host;
        }

        private static void Build(string s, ref int i, Panel host, double size, Brush fg)
        {
            while (i < s.Length)
            {
                char ch = s[i];

                if ((ch == 's' || ch == 'S') && IsSqrt(s, i))
                {
                    int open = i + 4;
                    int end = MatchClose(s, open);
                    if (end > 0)
                    {
                        host.Children.Add(Radical(s.Substring(open + 1, end - open - 1), size, fg));
                        i = end + 1;
                        continue;
                    }
                }

                if (ch == '\u221A')
                {
                    int open = i + 1;
                    if (open < s.Length && s[open] == '(')
                    {
                        int end = MatchClose(s, open);
                        if (end > 0)
                        {
                            host.Children.Add(Radical(s.Substring(open + 1, end - open - 1), size, fg));
                            i = end + 1;
                            continue;
                        }
                    }
                }

                if (ch == '^')
                {
                    int j = i + 1;
                    string sup;
                    if (j < s.Length && s[j] == '(')
                    {
                        int end = MatchClose(s, j);
                        if (end < 0)
                        {
                            sup = string.Empty;
                            j = i + 1;
                        }
                        else
                        {
                            sup = s.Substring(j + 1, end - j - 1);
                            j = end + 1;
                        }
                    }
                    else
                    {
                        int k = j;
                        while (k < s.Length && (char.IsLetterOrDigit(s[k]) || s[k] == '.' || s[k] == '-'))
                        {
                            k++;
                        }
                        sup = s.Substring(j, k - j);
                        j = k;
                    }
                    if (sup.Length == 0)
                    {
                        host.Children.Add(Plain("^", size * 0.55d, fg, false));
                        i++;
                        continue;
                    }
                    host.Children.Add(Plain(Prettify(sup), size * 0.62d, fg, true));
                    i = j;
                    continue;
                }

                if (ch == '\u00B2')
                {
                    host.Children.Add(Plain("2", size * 0.62d, fg, true));
                    i++;
                    continue;
                }
                if (ch == '\u00B3')
                {
                    host.Children.Add(Plain("3", size * 0.62d, fg, true));
                    i++;
                    continue;
                }

                int start = i;
                while (i < s.Length && s[i] != '^' && s[i] != '\u221A' && s[i] != '\u00B2' && s[i] != '\u00B3'
                       && !((s[i] == 's' || s[i] == 'S') && IsSqrt(s, i)))
                {
                    i++;
                }
                if (i > start)
                {
                    host.Children.Add(Plain(s.Substring(start, i - start), size, fg, false));
                }
            }
        }

        private static FrameworkElement Radical(string inner, double size, Brush fg)
        {
            int j = 0;
            StackPanel content = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Bottom };
            Build(inner, ref j, content, size, fg);
            StackPanel root = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            root.Children.Add(new TextBlock
            {
                Text = "\u221A",
                FontSize = size * 1.15d,
                Foreground = fg,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, -2, 0)
            });
            root.Children.Add(new Border
            {
                BorderBrush = fg,
                BorderThickness = new Thickness(0, 1.2d, 0, 0),
                Padding = new Thickness(2, 1.5d, 2, 0),
                Child = content,
                VerticalAlignment = VerticalAlignment.Center
            });
            return root;
        }

        private static bool IsSqrt(string s, int i)
        {
            return i + 4 < s.Length && s.Substring(i, 5).ToLowerInvariant() == "sqrt(";
        }

        private static string Prettify(string s)
        {
            return s.Replace("pi", "\u03C0").Replace("*", "\u00B7");
        }

        private static TextBlock Plain(string text, double size, Brush fg, bool sup)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = size,
                Foreground = fg,
                VerticalAlignment = sup ? VerticalAlignment.Top : VerticalAlignment.Center,
                Margin = sup ? new Thickness(0, -1, 1, 0) : new Thickness(0)
            };
        }

        private static int MatchClose(string s, int open)
        {
            int depth = 0;
            for (int i = open; i < s.Length; i++)
            {
                if (s[i] == '(')
                {
                    depth++;
                }
                else if (s[i] == ')')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return i;
                    }
                }
            }
            return -1;
        }
    }

    public partial class StbFunctionWindow
    {
        internal static string StbPreloadExpr;

        private void StbRefreshPreview()
        {
            try
            {
                if (PreviewHost == null)
                {
                    return;
                }
                string text = _rows.Count > 0 ? _rows[_rows.Count - 1].Box.Text : string.Empty;
                if (string.IsNullOrWhiteSpace(text))
                {
                    PreviewHost.Child = null;
                    return;
                }
                PreviewHost.Child = StbMath.Render(text, 17d, new SolidColorBrush(Color.FromRgb(0x1F, 0x23, 0x29)));
            }
            catch
            {
            }
        }

        private void StbZoomIn_Click(object sender, RoutedEventArgs e)
        {
            Plot.ZoomBy(1.4d);
        }

        private void StbZoomOut_Click(object sender, RoutedEventArgs e)
        {
            Plot.ZoomBy(0.7d);
        }

        private void StbZoomReset_Click(object sender, RoutedEventArgs e)
        {
            Plot.ResetView();
        }
    }
}
