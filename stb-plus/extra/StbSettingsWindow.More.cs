using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace InkCanvasPlus
{
    /// <summary>
    /// 设置页「高级」新增的几项：浮动栏笑脸菜单、插入函数按钮、函数画板、数据目录、重启 / 退出；
    /// 以及「关于应用」直达入口（笑脸菜单点头部时用）。
    /// </summary>
    public partial class StbSettingsWindow
    {
        public static void ShowAbout(Window owner)
        {
            if (_instance != null)
            {
                _instance.Activate();
                _instance.SelectSection(6);
                return;
            }

            StbSettingsWindow w = new StbSettingsWindow();
            if (owner != null && owner.IsLoaded)
            {
                w.Owner = owner;
            }
            _instance = w;
            w.Closed += (s, e) => { _instance = null; };
            w.Show();
            w.SelectSection(6);
            w.Activate();
        }

        /// <summary>构造后同步新开关的初始状态。</summary>
        internal void StbInitMore()
        {
            SwSmileMenu.IsOn = StbPrefs.SmileMenu;
            SwFuncButton.IsOn = StbPrefs.FuncButton;
        }

        private void SwSmileMenu_Toggled(object sender, RoutedEventArgs e)
        {
            if (_loading)
            {
                return;
            }
            StbPrefs.SmileMenu = SwSmileMenu.IsOn;
        }

        private void SwFuncButton_Toggled(object sender, RoutedEventArgs e)
        {
            if (_loading)
            {
                return;
            }
            StbPrefs.FuncButton = SwFuncButton.IsOn;
            MainWindow mw = Owner as MainWindow;
            if (mw == null && Application.Current != null)
            {
                mw = Application.Current.MainWindow as MainWindow;
            }
            if (mw != null)
            {
                mw.StbApplyFuncButtonVisibility();
            }
        }

        private void StbBtnOpenFunc_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mw = Owner as MainWindow;
            if (mw == null && Application.Current != null)
            {
                mw = Application.Current.MainWindow as MainWindow;
            }
            if (mw != null)
            {
                mw.StbOpenFunction();
            }
        }

        private void StbBtnOpenDataDir_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "SchoolToolsBoard");
                Directory.CreateDirectory(dir);
                Process.Start("explorer.exe", dir);
            }
            catch
            {
            }
        }

        private void StbBtnRestart_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string exe = Process.GetCurrentProcess().MainModule.FileName;
                Process.Start(exe);
            }
            catch
            {
            }
            Application.Current.Shutdown();
        }

        private void StbBtnQuit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
