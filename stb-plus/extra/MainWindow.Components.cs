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

        /// <summary>
        /// 旧版组件面板入口（1.0.1.9 起由组件管理窗接管）。
        /// 保留为可回退的独立方法，不再绑定任何按钮。
        /// </summary>
        private void ShowComponentDockLegacy()
        {
            StbComponentDock.ShowFor(this);
        }
    }
}
