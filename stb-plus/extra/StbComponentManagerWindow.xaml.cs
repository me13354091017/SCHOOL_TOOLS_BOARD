using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace InkCanvasPlus
{
    /// <summary>
    /// 「组件」管理窗：上方是当前组件（会显示在主界面上），下方是组件库，点组件库条目即添加。
    /// 配置保存在 %AppData%\\SchoolToolsBoard\\components.txt。
    /// </summary>
    public partial class StbComponentManagerWindow : Window
    {
        private static StbComponentManagerWindow _instance;
        private readonly List<string> _active = new List<string>();

        public static void ShowFor(Window owner)
        {
            if (_instance != null)
            {
                _instance.Activate();
                return;
            }
            StbComponentManagerWindow w = new StbComponentManagerWindow();
            if (owner != null && owner.IsLoaded)
            {
                w.Owner = owner;
            }
            _instance = w;
            w.Closed += (s, e) => { _instance = null; };
            w.Show();
            w.Activate();
        }

        public StbComponentManagerWindow()
        {
            InitializeComponent();
            _active.AddRange(StbComponentRegistry.LoadActive());
            BuildActive();
            BuildLibrary();
        }

        private static Brush CardBorder { get { return new SolidColorBrush(Color.FromRgb(0xE1, 0xE4, 0xEA)); } }
        private static Brush CardHover { get { return new SolidColorBrush(Color.FromRgb(0xF4, 0xF7, 0xFE)); } }

        private void BuildActive()
        {
            PanelActive.Children.Clear();
            if (_active.Count == 0)
            {
                TextBlock empty = new TextBlock
                {
                    Text = "还没有组件，从下方组件库点一下即可添加。",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x8A, 0x90, 0x9A)),
                    Margin = new Thickness(4, 8, 0, 0)
                };
                PanelActive.Children.Add(empty);
                return;
            }

            foreach (string id in _active.ToArray())
            {
                StbComponentInfo info = StbComponentRegistry.Find(id);
                if (info == null)
                {
                    _active.Remove(id);
                    continue;
                }

                Border card = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(0xF5, 0xF7, 0xFA)),
                    BorderBrush = CardBorder,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(12, 8, 8, 8),
                    Margin = new Thickness(0, 0, 8, 8),
                    MinWidth = 156
                };

                Grid g = new Grid();
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                TextBlock drag = new TextBlock
                {
                    Text = "⋮⋮",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xA8, 0xAF, 0xB8)),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 8, 0)
                };
                g.Children.Add(drag);

                TextBlock name = new TextBlock
                {
                    Text = info.Title,
                    FontSize = 13,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x1F, 0x23, 0x29)),
                    VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                Grid.SetColumn(name, 1);
                g.Children.Add(name);

                Button remove = new Button
                {
                    Content = "✕",
                    FontSize = 11,
                    Width = 22,
                    Height = 22,
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Foreground = new SolidColorBrush(Color.FromRgb(0x8A, 0x90, 0x9A)),
                    Cursor = Cursors.Hand,
                    ToolTip = "从当前组件中移除"
                };
                string captured = id;
                remove.Click += (s, e) => RemoveComponent(captured);
                Grid.SetColumn(remove, 2);
                g.Children.Add(remove);

                card.Child = g;
                card.MouseLeftButtonUp += (s, e) => OpenComponent(captured);
                card.Cursor = Cursors.Hand;
                PanelActive.Children.Add(card);
            }
        }

        private void BuildLibrary()
        {
            PanelLibrary.Children.Clear();
            foreach (StbComponentInfo info in StbComponentRegistry.All)
            {
                bool added = _active.Contains(info.Id);

                Border card = new Border
                {
                    Width = 246,
                    Height = 78,
                    Margin = new Thickness(0, 0, 10, 10),
                    Padding = new Thickness(10, 8, 10, 8),
                    CornerRadius = new CornerRadius(8),
                    BorderThickness = new Thickness(1),
                    BorderBrush = CardBorder,
                    Background = new SolidColorBrush(Color.FromRgb(0xFC, 0xFD, 0xFE)),
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
                    Text = info.Title.Substring(0, 1),
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x2C, 0x6B, 0xED)),
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
                    Foreground = new SolidColorBrush(Color.FromRgb(0x1F, 0x23, 0x29))
                });
                texts.Children.Add(new TextBlock
                {
                    Text = info.Description,
                    FontSize = 11,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x8A, 0x90, 0x9A)),
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
                        Foreground = new SolidColorBrush(Color.FromRgb(0x2C, 0x6B, 0xED)),
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(6, 0, 0, 0)
                    };
                    Grid.SetColumn(tick, 2);
                    right.Children.Add(tick);
                }

                Grid.SetColumn(right, 1);
                g.Children.Add(right);
                card.Child = g;

                StbComponentInfo captured = info;
                card.MouseEnter += (s, e) => card.Background = CardHover;
                card.MouseLeave += (s, e) => card.Background = new SolidColorBrush(Color.FromRgb(0xFC, 0xFD, 0xFE));
                card.MouseLeftButtonUp += (s, e) => AddComponent(captured);
                PanelLibrary.Children.Add(card);
            }
        }

        private void AddComponent(StbComponentInfo info)
        {
            if (info == null)
            {
                return;
            }
            if (!_active.Contains(info.Id))
            {
                _active.Add(info.Id);
                StbComponentRegistry.SaveActive(_active);
            }
            BuildActive();
            BuildLibrary();
            OpenComponent(info.Id);
        }

        private void RemoveComponent(string id)
        {
            _active.Remove(id);
            StbComponentRegistry.SaveActive(_active);
            BuildActive();
            BuildLibrary();
        }

        private void OpenComponent(string id)
        {
            StbComponentInfo info = StbComponentRegistry.Find(id);
            if (info == null)
            {
                return;
            }
            try
            {
                StbComponentWindow.ShowFor(this, info.Create());
            }
            catch
            {
            }
        }

        private void BtnReload_Click(object sender, RoutedEventArgs e)
        {
            _active.Clear();
            _active.AddRange(StbComponentRegistry.LoadActive());
            BuildActive();
            BuildLibrary();
        }

        private void BtnEditMode_Click(object sender, RoutedEventArgs e)
        {
            StbComponentDock.ShowFor(Owner ?? this);
        }
    }
}
