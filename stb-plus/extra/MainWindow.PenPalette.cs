using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace InkCanvasPlus
{
    /// <summary>
    /// 浮动栏「更多画笔颜色」调色板（partial 文件，不改动上游 MainWindow 代码）：
    /// 点调色板图标弹出 12 色，选色后直接作用于 inkCanvas 的 DefaultDrawingAttributes，并复用上游 ColorSwitchCheck 的收尾逻辑。
    /// </summary>
    public partial class MainWindow
    {
        private void BorderPenPalette_MouseUp(object sender, MouseButtonEventArgs e)
        {
            bool willOpen = !PopupPenPalette.IsOpen;
            if (willOpen)
            {
                HideSubPanels();
            }
            PopupPenPalette.IsOpen = willOpen;
            e.Handled = true;
        }

        private void BtnPenPaletteColor_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Border dot = sender as Border;
            if (dot != null && dot.Tag != null)
            {
                try
                {
                    object raw = ColorConverter.ConvertFromString(dot.Tag.ToString());
                    if (raw is Color)
                    {
                        // 非六个内置色，清掉勾选，避免出现「勾在黑上但笔是紫的」
                        inkColor = -1;
                        forceEraser = false;
                        inkCanvas.DefaultDrawingAttributes.Color = (Color)raw;
                        ColorSwitchCheck();
                    }
                }
                catch
                {
                }
            }

            PopupPenPalette.IsOpen = false;
            HideSubPanels();
            e.Handled = true;
        }
    }
}
