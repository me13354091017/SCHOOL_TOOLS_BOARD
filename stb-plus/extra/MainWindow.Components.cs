using System.Windows;
using System.Windows.Input;

namespace InkCanvasPlus
{
    /// <summary>
    /// STB 组件系统入口（ICCCE 风格的「组件」入口）：浮动栏图标 / 左侧竖栏按钮都落到这里，
    /// 打开组件面板，由面板再开各个组件小窗。独立文件，不覆盖上游 MainWindow 代码。
    /// </summary>
    public partial class MainWindow
    {
        private void SymbolIconComponents_MouseUp(object sender, MouseButtonEventArgs e)
        {
            StbComponentDock.ShowFor(this);
            HideSubPanels();
        }

        private void BtnComponents_Click(object sender, RoutedEventArgs e)
        {
            StbComponentDock.ShowFor(this);
        }
    }
}
