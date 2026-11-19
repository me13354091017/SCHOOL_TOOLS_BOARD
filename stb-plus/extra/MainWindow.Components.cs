using System.Windows;
using System.Windows.Input;

namespace InkCanvasPlus
{
    /// <summary>
    /// STB 组件系统入口：浮动栏「浮动栏」图标 / 左侧竖栏按钮都直接打开「组件 · 浮动栏」编辑窗。
    /// 旧的主界面组件管理窗不再对外暴露（类保留，仅作历史回退）。
    /// </summary>
    public partial class MainWindow
    {
        private void SymbolIconComponents_MouseUp(object sender, MouseButtonEventArgs e)
        {
            StbFloatBarWindow.ShowFor(this);
            HideSubPanels();
        }

        private void BtnComponents_Click(object sender, RoutedEventArgs e)
        {
            StbFloatBarWindow.ShowFor(this);
        }

        /// <summary>旧版组件面板入口（不再绑定任何按钮）。</summary>
        private void ShowComponentDockLegacy()
        {
            StbComponentDock.ShowFor(this);
        }
    }
}
