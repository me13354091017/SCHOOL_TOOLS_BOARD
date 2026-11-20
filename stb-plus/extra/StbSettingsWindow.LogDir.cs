using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace InkCanvasPlus
{
    public partial class StbSettingsWindow
    {
        /// <summary>1.0.1.35：打开日志文件夹（安装版在 {app}\data\Logs，免安装版在程序目录\Logs）。</summary>
        private void StbBtnOpenLogDir_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string dir = InkCanvasPlus.Helpers.LogHelper.LogDir;
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                Process.Start("explorer.exe", dir);
            }
            catch
            {
            }
        }
    }
}
