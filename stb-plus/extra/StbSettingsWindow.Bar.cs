using System;
using System.Windows;

namespace InkCanvasPlus
{
    /// <summary>
    /// 设置页「外观主题」里的「自定义浮动栏 → 编辑浮动栏」入口（partial 文件）。
    /// </summary>
    public partial class StbSettingsWindow
    {
        private void BtnEditFloatBarSettings_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mw = Owner as MainWindow;
            if (mw == null && Application.Current != null)
            {
                mw = Application.Current.MainWindow as MainWindow;
            }
            StbFloatBarWindow.ShowFor(mw);
        }
    }
}
