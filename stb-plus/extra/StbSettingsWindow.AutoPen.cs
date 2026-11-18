using System;
using System.Windows;
using System.Windows.Controls;

namespace InkCanvasPlus
{
    /// <summary>
    /// 设置页「橡皮用完自动回到书写」开关（partial 文件，不改动 StbSettingsWindow.xaml.cs）。
    /// 值存在 %AppData%\SchoolToolsBoard\prefs.txt 的 autoPenBack，默认开。
    /// </summary>
    public partial class StbSettingsWindow
    {
        private void SwAutoPenBack_Loaded(object sender, RoutedEventArgs e)
        {
            if (SwAutoPenBack != null)
            {
                SwAutoPenBack.IsOn = StbPrefs.AutoPenBack;
            }
        }

        private void SwAutoPenBack_Toggled(object sender, RoutedEventArgs e)
        {
            if (_loading || SwAutoPenBack == null)
            {
                return;
            }
            StbPrefs.AutoPenBack = SwAutoPenBack.IsOn;
        }
    }
}
