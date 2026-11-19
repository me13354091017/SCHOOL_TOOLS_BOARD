using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;

namespace InkCanvasPlus
{
    /// <summary>
    /// 浮动栏笑脸按钮的右键菜单：
    /// 应用图标 + 名字（点击进「关于应用」）、组件编辑、设置、保存、另存到、打开、重启、退出。
    /// </summary>
    public partial class MainWindow
    {
        private Popup _stbSmilePopup;

        private void StbSmileMenu_Open(object sender, MouseButtonEventArgs e)
        {
            if (!StbPrefs.SmileMenu)
            {
                return;
            }

            try
            {
                FrameworkElement target = sender as FrameworkElement;
                if (target == null)
                {
                    target = BorderFloatingBarEmoji;
                }

                if (_stbSmilePopup != null && _stbSmilePopup.IsOpen)
                {
                    _stbSmilePopup.IsOpen = false;
                    e.Handled = true;
                    return;
                }

                _stbSmilePopup = new Popup
                {
                    StaysOpen = false,
                    AllowsTransparency = true,
                    PlacementTarget = target,
                    Placement = PlacementMode.Bottom,
                    VerticalOffset = 8,
                    PopupAnimation = PopupAnimation.Fade,
                    Child = BuildSmileCard()
                };
                _stbSmilePopup.IsOpen = true;
            }
            catch
            {
            }
            e.Handled = true;
        }

        private Border BuildSmileCard()
        {
            StackPanel stack = new StackPanel { Width = 236 };

            Grid head = new Grid { Margin = new Thickness(2, 2, 2, 8), Cursor = Cursors.Hand };
            head.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            head.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            FrameworkElement iconEl = BuildAppIcon(34);
            Grid.SetColumn(iconEl, 0);
            head.Children.Add(iconEl);

            StackPanel titles = new StackPanel { Margin = new Thickness(10, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            titles.Children.Add(new TextBlock
            {
                Text = "SchoolToolsBoard",
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x1F, 0x23, 0x29))
            });
            titles.Children.Add(new TextBlock
            {
                Text = "课堂白板 v" + SafeVersion(),
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(0x8A, 0x90, 0x9A))
            });
            Grid.SetColumn(titles, 1);
            head.Children.Add(titles);

            head.MouseLeftButtonUp += (s, e) =>
            {
                if (_stbSmilePopup != null)
                {
                    _stbSmilePopup.IsOpen = false;
                }
                StbSettingsWindow.ShowAbout(this);
                e.Handled = true;
            };
            stack.Children.Add(head);

            stack.Children.Add(new Border
            {
                Height = 1,
                Background = new SolidColorBrush(Color.FromRgb(0xEC, 0xEF, 0xF4)),
                Margin = new Thickness(0, 0, 0, 6)
            });

            stack.Children.Add(SmileRow("▤", "组件编辑", () => StbFloatBarWindow.ShowFor(this)));
            stack.Children.Add(SmileRow("⚙", "设置", () => StbSettingsWindow.ShowFor(this)));
            stack.Children.Add(SmileRow("▣", "保存", () => SaveInkCanvasStrokes()));
            stack.Children.Add(SmileRow("⤓", "另存到", () => StbSaveAsNow()));
            stack.Children.Add(SmileRow("▱", "打开", () => StbOpenStrokesNow()));
            stack.Children.Add(SmileRow("⟳", "重启", () => StbRestartApp()));
            stack.Children.Add(SmileRow("⏻", "退出", () => Close()));

            return new Border
            {
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xD8, 0xDD, 0xE5)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(10, 10, 10, 8),
                Child = stack
            };
        }

        private FrameworkElement BuildAppIcon(double size)
        {
            ImageSource src = null;
            try
            {
                src = Icon;
            }
            catch
            {
            }

            if (src != null)
            {
                return new Image
                {
                    Source = src,
                    Width = size,
                    Height = size,
                    Stretch = Stretch.Uniform
                };
            }

            return new Border
            {
                Width = size,
                Height = size,
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(Color.FromRgb(0x1F, 0x8F, 0x5F)),
                Child = new TextBlock
                {
                    Text = "STB",
                    FontSize = 11,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
        }

        private FrameworkElement SmileRow(string glyph, string label, Action action)
        {
            Border row = new Border
            {
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 6, 8, 6),
                Background = Brushes.Transparent,
                Cursor = Cursors.Hand
            };

            StackPanel line = new StackPanel { Orientation = Orientation.Horizontal };
            line.Children.Add(new TextBlock
            {
                Text = glyph,
                Width = 20,
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(0x5B, 0x63, 0x6E))
            });
            line.Children.Add(new TextBlock
            {
                Text = label,
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(0x2A, 0x2F, 0x36))
            });
            row.Child = line;

            row.MouseEnter += (s, e) => row.Background = new SolidColorBrush(Color.FromRgb(0xF2, 0xF5, 0xF9));
            row.MouseLeave += (s, e) => row.Background = Brushes.Transparent;
            row.MouseLeftButtonUp += (s, e) =>
            {
                if (_stbSmilePopup != null)
                {
                    _stbSmilePopup.IsOpen = false;
                }
                try
                {
                    action();
                }
                catch
                {
                }
                e.Handled = true;
            };
            return row;
        }

        private static string SafeVersion()
        {
            try
            {
                return Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
            }
            catch
            {
                return "1.0";
            }
        }

        private void StbOpenStrokesNow()
        {
            try
            {
                OpenFileDialog dialog = new OpenFileDialog();
                dialog.Title = "打开墨迹文件";
                dialog.Filter = "白板墨迹 (*.icstk)|*.icstk|所有文件 (*.*)|*.*";
                string dir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\Ink Canvas Strokes\User Saved";
                if (System.IO.Directory.Exists(dir))
                {
                    dialog.InitialDirectory = dir;
                }

                if (dialog.ShowDialog() != true)
                {
                    return;
                }

                using (System.IO.FileStream fs = new System.IO.FileStream(dialog.FileName, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                {
                    System.Windows.Ink.StrokeCollection strokes = new System.Windows.Ink.StrokeCollection(fs);
                    if (strokes.Count > 0)
                    {
                        ClearStrokes(true);
                        inkCanvas.Strokes.Add(strokes);
                        ShowNotification("已打开 " + dialog.FileName);
                    }
                }
            }
            catch (Exception ex)
            {
                try
                {
                    ShowNotification("打开失败：" + ex.Message);
                }
                catch
                {
                }
            }
        }

        private void StbRestartApp()
        {
            try
            {
                string exe = Process.GetCurrentProcess().MainModule.FileName;
                Process.Start(exe);
            }
            catch
            {
            }
            Application.Current.Shutdown();
        }
    }
}
