using System.Windows;
using System.Windows.Input;

namespace InkCanvasPlus
{
    /// <summary>
    /// STB 组件系统入口：浮动栏「组件」图标 / 左侧竖栏按钮都落到组件管理窗（组件库 + 当前组件）。
    /// 独立文件，不覆盖上游 MainWindow 代码。
    /// </summary>
    public partial class MainWindow
    {
        private void SymbolIconComponents_MouseUp(object sender, MouseButtonEventArgs e)
        {
            StbComponentManagerWindow.ShowFor(this);
            HideSubPanels();
        }

        private void BtnComponents_Click(object sender, RoutedEventArgs e)
        {
            StbComponentManagerWindow.ShowFor(this);
        }
    }
}
