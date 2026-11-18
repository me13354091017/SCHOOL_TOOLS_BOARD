using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;

namespace InkCanvasPlus
{
    /// <summary>
    /// 浮动栏「更多画笔颜色」调色板（partial 文件，不改动上游 MainWindow 代码）：
    /// 点调色板图标弹出 12 色，选色后作用于 inkCanvas 的笔迹属性，并让上游 GetBaseColor 认这个自定义色。
    /// </summary>
    public partial class MainWindow
    {
        /// <summary>调色板选定的自定义颜色；上游 SetColorByIndex / GetBaseColor 会优先用它。</summary>
        private Color? stbCustomInkColor;

        private bool _paletteClickHookReady;

        private void BorderPenPalette_MouseUp(object sender, MouseButtonEventArgs e)
        {
            EnsurePaletteClickHook();

            // Popup 有自己的名字域，XAML 里 ElementName 绑定会失效导致弹层跟着鼠标跑：这里直接指定锚点
            try
            {
                PopupPenPalette.PlacementTarget = BorderPenPalette;
                PopupPenPalette.Placement = PlacementMode.Top;
                PopupPenPalette.HorizontalOffset = -98;
                PopupPenPalette.VerticalOffset = 8;
            }
            catch
            {
            }

            bool willOpen = !PopupPenPalette.IsOpen;
            if (willOpen)
            {
                HideSubPanels();
            }
            PopupPenPalette.IsOpen = willOpen;
            e.Handled = true;
        }

        private void EnsurePaletteClickHook()
        {
            if (_paletteClickHookReady)
            {
                return;
            }
            UIElement root = PopupPenPalette.Child as UIElement;
            if (root == null)
            {
                return;
            }
            HookPaletteDots(root);
            _paletteClickHookReady = true;
        }

        private void HookPaletteDots(DependencyObject node)
        {
            if (node == null)
            {
                return;
            }
            Border border = node as Border;
            if (border != null && border.Tag is string)
            {
                border.AddHandler(UIElement.PreviewMouseLeftButtonDownEvent,
                    new MouseButtonEventHandler(PaletteDot_PreviewMouseLeftButtonDown), true);
            }
            int count = VisualTreeHelper.GetChildrenCount(node);
            for (int i = 0; i < count; i++)
            {
                HookPaletteDots(VisualTreeHelper.GetChild(node, i));
            }
        }

        /// <summary>弹层 StaysOpen=false，点击会先关弹层导致 MouseUp 收不到，所以在 Preview 阶段接住。</summary>
        private void PaletteDot_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Border dot = sender as Border;
            if (dot != null && dot.Tag is string)
            {
                e.Handled = true;
                ApplyPenPaletteColor(dot.Tag.ToString());
            }
        }

        private void BtnPenPaletteColor_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Border dot = sender as Border;
            if (dot != null && dot.Tag != null)
            {
                ApplyPenPaletteColor(dot.Tag.ToString());
            }
            e.Handled = true;
        }

        private void ApplyPenPaletteColor(string colorText)
        {
            try
            {
                object raw = ColorConverter.ConvertFromString(colorText);
                if (raw is Color)
                {
                    Color color = (Color)raw;
                    stbCustomInkColor = color;
                    // -1 表示「非六个内置色」：SetColorByIndex 不会把颜色改回内置色
                    inkColor = -1;
                    forceEraser = false;

                    // 直接改 DefaultDrawingAttributes.Color 在某些实例上会抛异常（Frozen），改为克隆后整体赋值
                    DrawingAttributes attrs = inkCanvas.DefaultDrawingAttributes.Clone();
                    attrs.Color = color;
                    inkCanvas.DefaultDrawingAttributes = attrs;

                    // ColorSwitchCheck 末尾的 ApplyMarkerMode 会用 GetBaseColor 重设颜色，
                    // 已把 GetBaseColor 的 default 分支改成优先取 stbCustomInkColor
                    ColorSwitchCheck();

                    if (inkCanvas.DefaultDrawingAttributes.Color != color)
                    {
                        DrawingAttributes attrs2 = inkCanvas.DefaultDrawingAttributes.Clone();
                        attrs2.Color = color;
                        inkCanvas.DefaultDrawingAttributes = attrs2;
                    }
                }
            }
            catch
            {
            }

            PopupPenPalette.IsOpen = false;
            HideSubPanels();
        }
    }
}
