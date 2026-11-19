using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace InkCanvasPlus
{
    /// <summary>
    /// 「组件 · 浮动栏」编辑窗：和组件管理窗同一套卡片语言。
    /// 开启接管后，浮动栏由这里的条目组成；关掉则回到内置浮动栏。
    /// </summary>
    public partial class StbFloatBarWindow : Window
    {
        private static StbFloatBarWindow _instance;
        private readonly List<string> _items = new List<string>();
        private bool _loading = true;

        public static void ShowFor(Window owner)
        {
            if (_instance != null)
            {
                _instance.Activate();
                return;
            }
            StbFloatBarWindow w = new StbFloatBarWindow();
            if (owner != null && owner.IsLoaded)
            {
                w.Owner = owner;
            }
            _instance = w;
            w.Closed += (s, e) => { _instance = null; };
            w.Show();
            w.Activate();
        }

        public StbFloatBarWindow()
        {
            InitializeComponent();
            _items.AddRange(StbBarRegistry.LoadItems());
            SwEnabled.IsOn = StbBarRegistry.Enabled;
            SliderSize.Value = StbBarRegistry.Size;
            TextSize.Text = ((int)Math.Round(StbBarRegistry.Size)).ToString();
            ChkText.IsChecked = StbBarRegistry.ShowText;
            BuildActive();
            BuildLibrary();
            _loading = false;
        }

        private static Brush CardBorder { get { return new SolidColorBrush(Color.FromRgb(0xE1, 0xE4, 0xEA)); } }
        private static Brush CardHover { get { return new SolidColorBrush(Color.FromRgb(0xF4, 0xF7, 0xFE)); } }
        private static Brush CardBg { get { return new SolidColorBrush(Color.FromRgb(0xFC, 0xFD, 0xFE)); } }
        private static Brush TextMain { get { return new SolidColorBrush(Color.FromRgb(0x1F, 0x23, 0x29)); } }
        private static Brush TextWeak { get { return new SolidColorBrush(Color.FromRgb(0x8A, 0x90, 0x9A)); } }
        private static Brush Accent { get { return new SolidColorBrush(Color.FromRgb(0x2C, 0x6B, 0xED)); } }

        private void BuildActive()
        {
            PanelActive.Children.Clear();
            if (_items.Count == 0)
            {
                TextBlock empty = new TextBlock
                {
                    Text = "还没有条目，从下方工具库点一下即可添加。",
                    FontSize = 12,
                    Foreground = TextWeak,
                    Margin = new Thickness(4, 8, 0, 0)
                };
                PanelActive.Children.Add(empty);
                return;
            }

            for (int i = 0; i < _items.Count; i++)
            {
                string id = _items[i];
                StbBarItemInfo info = StbBarRegistry.Find(id);
                if (info == null)
                {
                    continue;
                }

                Border card = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(0xF5, 0xF7, 0xFA)),
                    BorderBrush = CardBorder,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(10, 7, 8, 7),
                    Margin = new Thickness(0, 0, 8, 8),
                    MinWidth = 168
                };

                Grid g = new Grid();
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                TextBlock drag = new TextBlock
                {
                    Text = "⋮⋮",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xA8, 0xAF, 0xB8)),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 7, 0)
                };
                g.Children.Add(drag);

                StackPanel body = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                body.Children.Add(new TextBlock
                {
                    Text = info.Glyph,
                    FontSize = 17,
                    Foreground = Accent,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 8, 0)
                });
                body.Children.Add(new TextBlock
                {
                    Text = info.Title,
                    FontSize = 13,
                    Foreground = TextMain,
                    VerticalAlignment = VerticalAlignment.Center
                });
                Grid.SetColumn(body, 1);
                g.Children.Add(body);

                int index = i;
                StackPanel ops = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                ops.Children.Add(TinyButton("↑", "前移", () => Move(index, -1)));
                ops.Children.Add(TinyButton("↓", "后移", () => Move(index, 1)));
                ops.Children.Add(TinyButton("✕", "移除", () => Remove(index)));
                Grid.SetColumn(ops, 2);
                g.Children.Add(ops);

                card.Child = g;
                PanelActive.Children.Add(card);
            }
        }

        private Button TinyButton(string text, string tip, Action act)
        {
            Button b = new Button
            {
                Content = text,
                FontSize = 11,
                Width = 22,
                Height = 22,
                Margin = new Thickness(2, 0, 0, 0),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = TextWeak,
                Cursor = Cursors.Hand,
                ToolTip = tip
            };
            b.Click += (s, e) => act();
            return b;
        }

        private void BuildLibrary()
        {
            PanelLibrary.Children.Clear();
            foreach (StbBarItemInfo info in StbBarRegistry.All)
            {
                bool added = _items.Contains(info.Id);

                Border card = new Border
                {
                    Width = 226,
                    Height = 74,
                    Margin = new Thickness(0, 0, 10, 10),
                    Padding = new Thickness(10, 8, 10, 8),
                    CornerRadius = new CornerRadius(8),
                    BorderThickness = new Thickness(1),
                    BorderBrush = CardBorder,
                    Background = CardBg,
                    Cursor = Cursors.Hand
                };

                Grid g = new Grid();
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                TextBlock drag = new TextBlock
                {
                    Text = "⋮⋮",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xA8, 0xAF, 0xB8)),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 8, 0)
                };
                g.Children.Add(drag);

                Grid right = new Grid();
                right.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                right.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                right.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                Border icon = new Border
                {
                    Width = 22,
                    Height = 22,
                    CornerRadius = new CornerRadius(6),
                    Background = new SolidColorBrush(Color.FromRgb(0xED, 0xF3, 0xFF)),
                    Margin = new Thickness(0, 0, 10, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                icon.Child = new TextBlock
                {
                    Text = info.Glyph,
                    FontSize = 13,
                    Foreground = Accent,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                right.Children.Add(icon);

                StackPanel texts = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                texts.Children.Add(new TextBlock
                {
                    Text = info.Title,
                    FontSize = 13,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = TextMain
                });
                texts.Children.Add(new TextBlock
                {
                    Text = info.Description,
                    FontSize = 11,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = TextWeak,
                    Margin = new Thickness(0, 3, 0, 0)
                });
                Grid.SetColumn(texts, 1);
                right.Children.Add(texts);

                if (added)
                {
                    TextBlock tick = new TextBlock
                    {
                        Text = "已添加",
                        FontSize = 11,
                        Foreground = Accent,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(6, 0, 0, 0)
                    };
                    Grid.SetColumn(tick, 2);
                    right.Children.Add(tick);
                }

                Grid.SetColumn(right, 1);
                g.Children.Add(right);
                card.Child = g;

                StbBarItemInfo captured = info;
                card.MouseEnter += (s, e) => card.Background = CardHover;
                card.MouseLeave += (s, e) => card.Background = CardBg;
                card.MouseLeftButtonUp += (s, e) => AddItem(captured);
                PanelLibrary.Children.Add(card);
            }
        }

        private void AddItem(StbBarItemInfo info)
        {
            if (info == null || _items.Contains(info.Id))
            {
                return;
            }
            _items.Add(info.Id);
            Commit();
        }

        private void Remove(int index)
        {
            if (index < 0 || index >= _items.Count)
            {
                return;
            }
            _items.RemoveAt(index);
            Commit();
        }

        private void Move(int index, int delta)
        {
            int target = index + delta;
            if (index < 0 || index >= _items.Count || target < 0 || target >= _items.Count)
            {
                return;
            }
            string tmp = _items[index];
            _items[index] = _items[target];
            _items[target] = tmp;
            Commit();
        }

        private void Commit()
        {
            StbBarRegistry.SaveItems(_items);
            BuildActive();
            BuildLibrary();
            ApplyToOwner();
        }

        private void ApplyToOwner()
        {
            MainWindow mw = Owner as MainWindow;
            if (mw == null && Application.Current != null)
            {
                mw = Application.Current.MainWindow as MainWindow;
            }
            if (mw != null)
            {
                mw.StbRebuildFloatBar();
            }
        }

        private void SwEnabled_Toggled(object sender, RoutedEventArgs e)
        {
            if (_loading)
            {
                return;
            }
            StbBarRegistry.Enabled = SwEnabled.IsOn;
            ApplyToOwner();
        }

        private void SliderSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TextSize == null)
            {
                return;
            }
            TextSize.Text = ((int)Math.Round(SliderSize.Value)).ToString();
            if (_loading)
            {
                return;
            }
            StbBarRegistry.Size = SliderSize.Value;
            ApplyToOwner();
        }

        private void ChkText_Click(object sender, RoutedEventArgs e)
        {
            if (_loading)
            {
                return;
            }
            StbBarRegistry.ShowText = ChkText.IsChecked == true;
            ApplyToOwner();
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            _items.Clear();
            StbBarRegistry.SaveItems(_items);
            StbBarRegistry.Enabled = false;
            StbBarRegistry.ShowText = true;
            StbBarRegistry.Size = 18d;
            _loading = true;
            SwEnabled.IsOn = false;
            SliderSize.Value = 18d;
            TextSize.Text = "18";
            ChkText.IsChecked = true;
            _loading = false;
            BuildActive();
            BuildLibrary();
            ApplyToOwner();
        }
    }
}
