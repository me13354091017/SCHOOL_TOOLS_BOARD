using System.Windows;

namespace InkCanvasPlus
{
    /// <summary>
    /// 主窗口新增的「板中板」入口（由 stb-plus 补丁新增的 partial 文件）。
    /// </summary>
    public partial class MainWindow
    {
        private void BtnMiniBoard_Click(object sender, RoutedEventArgs e)
        {
            MiniBoardWindow.ShowBoard(this);
        }
    }
}
