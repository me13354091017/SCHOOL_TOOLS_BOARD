using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;

namespace InkCanvasPlus
{
    /// <summary>
    /// 浮动栏「保存」旁的「保存到其它」入口（partial 文件，不改上游保存逻辑）：
    /// 弹另存对话框，把当前墨迹直接写到用户指定位置，不影响默认目录下的历史文件。
    /// </summary>
    public partial class MainWindow
    {
        private void StbSymbolIconSaveAs_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lastBorderMouseDownObject != sender || inkCanvas.Visibility != Visibility.Visible)
            {
                return;
            }
            StbSaveAsNow();
        }

        /// <summary>弹出另存对话框，把当前墨迹写到用户指定位置（浮动栏组件也调它）。</summary>
        internal void StbSaveAsNow()
        {
            try
            {
                BorderTools.Visibility = Visibility.Collapsed;
                GridNotifications.Visibility = Visibility.Collapsed;
            }
            catch
            {
            }

            try
            {
                SaveFileDialog dialog = new SaveFileDialog();
                dialog.Title = "保存墨迹到其它位置";
                dialog.Filter = "白板墨迹 (*.icstk)|*.icstk|所有文件 (*.*)|*.*";
                dialog.DefaultExt = "icstk";
                dialog.AddExtension = true;
                dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                dialog.FileName = "SchoolToolsBoard-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".icstk";

                if (dialog.ShowDialog() == true)
                {
                    using (FileStream stream = new FileStream(dialog.FileName, FileMode.Create))
                    {
                        inkCanvas.Strokes.Save(stream);
                    }
                    ShowNotification("墨迹已保存至 " + dialog.FileName);
                }
            }
            catch (Exception ex)
            {
                try
                {
                    ShowNotification("保存失败：" + ex.Message);
                }
                catch
                {
                }
            }
        }
    }
}
