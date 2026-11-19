using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;

namespace InkCanvasPlus
{
    /// <summary>
    /// 浮动栏「组件系统」：把浮动栏上的工具做成可配置组件（增删 / 排序 / 图标大小 / 是否带文字），
    /// 开启接管时隐藏内置工具组（StackPanelCanvacMain、StackPanelCanvasControls），只显示组件条目。
    /// 编辑入口：组件窗 → 编辑浮动栏。
    /// </summary>
    public partial class MainWindow
    {
        private bool _stbFloatBarInited = false;

        internal void StbInitFloatBar()
        {
            if (_stbFloatBarInited)
            {
                return;
            }
            _stbFloatBarInited = true;
            StbRebuildFloatBar();
        }

        /// <summary>按配置重建浮动栏组件条目，并切换内置工具组的显隐。</summary>
        internal void StbRebuildFloatBar()
        {
            try
            {
                if (StbCustomBar == null)
                {
                    return;
                }

                List<string> ids = StbBarRegistry.LoadItems();
                bool useCustom = StbBarRegistry.Enabled && ids.Count > 0;

                StbCustomBar.Children.Clear();
                if (!useCustom)
                {
                    StbCustomBar.Visibility = Visibility.Collapsed;
                    if (StackPanelCanvacMain != null)
                    {
                        StackPanelCanvacMain.Visibility = Visibility.Visible;
                    }
                    if (StackPanelCanvasControls != null)
                    {
                        StackPanelCanvasControls.Visibility = Visibility.Visible;
                    }
                    return;
                }

                double size = StbBarRegistry.Size;
                bool showText = StbBarRegistry.ShowText;

                foreach (string id in ids)
                {
                    StbBarItemInfo info = StbBarRegistry.Find(id);
                    if (info == null)
                    {
                        continue;
                    }
                    StbCustomBar.Children.Add(StbBuildBarItem(info, size, showText));
                }

                StbCustomBar.Visibility = Visibility.Visible;
                if (StackPanelCanvacMain != null)
                {
                    StackPanelCanvacMain.Visibility = Visibility.Collapsed;
                }
                if (StackPanelCanvasControls != null)
                {
                    StackPanelCanvasControls.Visibility = Visibility.Collapsed;
                }
            }
            catch
            {
            }
        }

        private FrameworkElement StbBuildBarItem(StbBarItemInfo info, double size, bool showText)
        {
            Brush fg = StbFloatBrush();

            Border host = new Border
            {
                Background = Brushes.Transparent,
                Margin = new Thickness(2, 0, 2, 0),
                Padding = new Thickness(3, 1, 3, 1),
                Cursor = Cursors.Hand,
                ToolTip = info.Title
            };

            StackPanel box = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            TextBlock glyph = new TextBlock
            {
                Text = info.Glyph,
                FontSize = size,
                LineHeight = size + 2,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = fg,
                TextAlignment = TextAlignment.Center
            };
            box.Children.Add(glyph);

            if (showText)
            {
                TextBlock label = new TextBlock
                {
                    Text = info.Title,
                    FontSize = 8,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, -1, 0, 0),
                    Foreground = fg
                };
                box.Children.Add(label);
            }

            host.Child = box;

            string captured = info.Id;
            host.MouseLeftButtonUp += (s, e) =>
            {
                StbRunBarItem(captured);
                e.Handled = true;
            };
            host.MouseEnter += (s, e) => host.Opacity = 0.72;
            host.MouseLeave += (s, e) => host.Opacity = 1.0;

            return host;
        }

        private Brush StbFloatBrush()
        {
            try
            {
                Brush b = TryFindResource("FloatBarForeground") as Brush;
                if (b != null)
                {
                    return b;
                }
            }
            catch
            {
            }
            return new SolidColorBrush(Color.FromRgb(0xE8, 0xEB, 0xEF));
        }

        private void StbRunBarItem(string id)
        {
            try
            {
                switch (id)
                {
                    case "stb.bar.miniboard":
                        MiniBoardWindow.ShowBoard(this);
                        return;
                    case "stb.bar.components":
                        StbComponentManagerWindow.ShowFor(this);
                        return;
                    case "stb.bar.settings":
                        StbSettingsWindow.ShowFor(this);
                        return;
                    case "stb.bar.pen":
                        StbSetInk();
                        return;
                    case "stb.bar.highlighter":
                        StbSetHighlighter();
                        return;
                    case "stb.bar.eraser":
                        StbSetEraser();
                        return;
                    case "stb.bar.select":
                        inkCanvas.EditingMode = InkCanvasEditingMode.Select;
                        inkCanvas.Cursor = Cursors.Arrow;
                        return;
                    case "stb.bar.clear":
                        inkCanvas.Strokes.Clear();
                        return;
                    case "stb.bar.fullscreen":
                        WindowState = WindowState == WindowState.Maximized
                            ? WindowState.Normal
                            : WindowState.Maximized;
                        return;
                    case "stb.bar.exit":
                        Close();
                        return;
                }
            }
            catch
            {
            }
        }

        private void StbSetInk()
        {
            forceEraser = false;
            forcePointEraser = false;
            inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
            inkCanvas.Cursor = Cursors.Pen;
            HideSubPanels();
        }

        private void StbSetHighlighter()
        {
            DrawingAttributes attrs = new DrawingAttributes();
            attrs.Color = Color.FromRgb(0xFF, 0xD5, 0x4F);
            attrs.Width = 12d;
            attrs.Height = 12d;
            attrs.StylusTip = StylusTip.Rectangle;
            attrs.IsHighlighter = true;
            inkCanvas.DefaultDrawingAttributes = attrs;

            forceEraser = false;
            forcePointEraser = false;
            inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
            inkCanvas.Cursor = Cursors.Pen;
            HideSubPanels();
        }

        private void StbSetEraser()
        {
            forceEraser = true;
            forcePointEraser = false;
            inkCanvas.EditingMode = InkCanvasEditingMode.EraseByStroke;
            HideSubPanels();
        }
    }
}
