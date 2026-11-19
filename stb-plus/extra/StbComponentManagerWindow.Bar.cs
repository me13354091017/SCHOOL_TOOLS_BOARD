using System;
using System.Windows;

namespace InkCanvasPlus
{
    /// <summary>组件窗底部的「编辑浮动栏」入口（partial，不改动 StbComponentManagerWindow.xaml.cs）。</summary>
    public partial class StbComponentManagerWindow
    {
        private void BtnEditFloatBar_Click(object sender, RoutedEventArgs e)
        {
            Window owner = Owner;
            if (owner == null && Application.Current != null)
            {
                owner = Application.Current.MainWindow;
            }
            StbFloatBarWindow.ShowFor(owner);
        }
    }
}
