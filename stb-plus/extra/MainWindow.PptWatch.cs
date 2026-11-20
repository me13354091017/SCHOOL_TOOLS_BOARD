using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace InkCanvasPlus
{
    /// <summary>
    /// 常驻 PPT 放映探测（不依赖 COM 事件，永不停止）：
    /// 用 Win32 枚举放映窗口（PowerPoint 的 screenClass / WPS 全屏放映窗），一旦出现就强制进入批注模式；
    /// PowerPoint 重启、异常退出、先关文稿再退出、应用后启动等场景都能重新识别；
    /// COM 不可用（WPS / 权限隔离）时退化为轻量批注模式，仍可写字；
    /// 放映期间周期性把批注窗重新抬到最上层且不抢焦点，避免点下一页后批注层被 PPT 压住。
    /// </summary>
    public partial class MainWindow
    {
        private DispatcherTimer _stbPptWatch;
        private bool _stbPptLight;
        private int _stbPptMiss;

        private const int STB_HWND_TOPMOST = -1;
        private const uint STB_SWP_NOSIZE = 0x0001;
        private const uint STB_SWP_NOMOVE = 0x0002;
        private const uint STB_SWP_NOACTIVATE = 0x0010;

        [StructLayout(LayoutKind.Sequential)]
        private struct StbRect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(StbEnumProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out StbRect lpRect);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private delegate bool StbEnumProc(IntPtr hWnd, IntPtr lParam);

        private void StbInitPptWatch()
        {
            if (_stbPptWatch != null)
            {
                return;
            }
            _stbPptWatch = new DispatcherTimer(DispatcherPriority.Background);
            _stbPptWatch.Interval = TimeSpan.FromMilliseconds(1200);
            _stbPptWatch.Tick += (s, e) => StbPptWatchTick();
            _stbPptWatch.Start();
        }

        private static HashSet<int> StbPptPids()
        {
            HashSet<int> ids = new HashSet<int>();
            foreach (string name in new string[] { "POWERPNT", "wpp", "wps" })
            {
                try
                {
                    foreach (Process p in Process.GetProcessesByName(name))
                    {
                        ids.Add(p.Id);
                    }
                }
                catch
                {
                }
            }
            return ids;
        }

        /// <summary>枚举顶层窗口找放映窗；PowerPoint 看 screenClass，WPS 看全屏且类名含 screen/wpp/ppt。</summary>
        private static bool StbScanPptShow(HashSet<int> pids, out bool isWps)
        {
            bool found = false;
            bool wps = false;
            double pw = SystemParameters.PrimaryScreenWidth;
            double ph = SystemParameters.PrimaryScreenHeight;
            uint selfPid = (uint)Process.GetCurrentProcess().Id;

            EnumWindows((h, l) =>
            {
                if (found)
                {
                    return false;
                }
                if (!IsWindowVisible(h))
                {
                    return true;
                }
                uint pid;
                GetWindowThreadProcessId(h, out pid);
                if (pid == 0 || pid == selfPid || !pids.Contains((int)pid))
                {
                    return true;
                }
                StringBuilder sb = new StringBuilder(160);
                GetClassName(h, sb, 160);
                string cls = sb.ToString().ToLowerInvariant();
                bool msShow = cls == "screenclass";
                bool wpsShow = cls.Contains("screen") || cls.Contains("wpp") || cls.Contains("ppt");
                if (!msShow && !wpsShow)
                {
                    return true;
                }
                StbRect r;
                if (!GetWindowRect(h, out r))
                {
                    return true;
                }
                double w = r.Right - r.Left;
                double hh = r.Bottom - r.Top;
                if (msShow || (w >= pw * 0.85 && hh >= ph * 0.85))
                {
                    found = true;
                    wps = !msShow;
                    return false;
                }
                return true;
            }, IntPtr.Zero);

            isWps = wps;
            return found;
        }

        /// <summary>COM 放映窗数量；对象失效时顺手清空，下一拍让上游定时器重连。</summary>
        private int StbPptComShowCount()
        {
            if (pptApplication == null)
            {
                return 0;
            }
            try
            {
                return pptApplication.SlideShowWindows.Count;
            }
            catch
            {
                pptApplication = null;
                return 0;
            }
        }

        private bool StbPptUiActive()
        {
            return StackPanelPPTControls.Visibility == Visibility.Visible
                && BtnPPTSlideShowEnd.Visibility == Visibility.Visible;
        }

        private void StbPptWatchTick()
        {
            if (!IsLoaded)
            {
                return;
            }
            if (!Settings.PowerPointSettings.PowerPointSupport)
            {
                return;
            }

            try
            {
                HashSet<int> pids = StbPptPids();
                if (pids.Count == 0)
                {
                    _stbPptMiss = 0;
                    if (_stbPptLight)
                    {
                        _stbPptLight = false;
                        StbPptExitLight();
                    }
                    return;
                }

                bool isWps;
                bool hasShow = StbScanPptShow(pids, out isWps);
                int comShows = StbPptComShowCount();
                if (pptApplication == null)
                {
                    // 让上游定时器重新建立 COM 连接并注册放映事件（它连上后会自己停）
                    timerCheckPPT.Start();
                }

                if (hasShow)
                {
                    if (!StbPptUiActive())
                    {
                        _stbPptMiss++;
                        if (comShows >= 1)
                        {
                            try
                            {
                                PptApplication_SlideShowBegin(pptApplication.SlideShowWindows[1]);
                                _stbPptMiss = 0;
                            }
                            catch
                            {
                            }
                        }
                        // COM 迟迟连不上（WPS / 权限隔离 / 保护视图），退化为轻量批注
                        if (!StbPptUiActive() && _stbPptMiss >= 4)
                        {
                            StbPptEnterLight(isWps);
                        }
                    }
                    else
                    {
                        _stbPptMiss = 0;
                        StbPptKeepTop();
                    }
                }
                else
                {
                    _stbPptMiss = 0;
                    if (_stbPptLight)
                    {
                        _stbPptLight = false;
                        StbPptExitLight();
                    }
                }
            }
            catch
            {
            }
        }

        /// <summary>放映中把批注窗重新抬到最上层，但不抢 PPT 的焦点。</summary>
        private void StbPptKeepTop()
        {
            try
            {
                IntPtr hwnd = new WindowInteropHelper(this).Handle;
                if (hwnd != IntPtr.Zero && GetForegroundWindow() != hwnd)
                {
                    SetWindowPos(hwnd, new IntPtr(STB_HWND_TOPMOST), 0, 0, 0, 0,
                        STB_SWP_NOSIZE | STB_SWP_NOMOVE | STB_SWP_NOACTIVATE);
                }
            }
            catch
            {
            }
        }

        /// <summary>COM 不可用时的批注模式：叠在放映窗上、画布可写、工具栏与结束按钮可用。</summary>
        private void StbPptEnterLight(bool isWps)
        {
            try
            {
                currentMode = 0;
                GridBackgroundCover.Visibility = Visibility.Collapsed;
                if (Main_Grid.Background == Brushes.Transparent)
                {
                    BtnHideInkCanvas_Click(BtnHideInkCanvas, null);
                }
                StackPanelPPTControls.Visibility = Visibility.Visible;
                BtnPPTSlideShow.Visibility = Visibility.Collapsed;
                BtnPPTSlideShowEnd.Visibility = Visibility.Visible;
                ViewBoxStackPanelMain.Margin = new Thickness(10, 10, 10, 10);
                ChangeLockSmithState(!Settings.PowerPointSettings.IsEnableTwoFingerGestureInPresentationMode);
                SetBorderFloatingBarMainControlsVisibility(true, false);
                BorderPenColorRed_MouseUp(BorderPenColorRed, null);
                PptNavigationTextBlock.Text = (isWps ? "WPS 放映 · 批注模式" : "PPT 放映 · 批注模式");
                Topmost = true;
                _stbPptLight = true;
                StbPptKeepTop();
            }
            catch
            {
                _stbPptLight = false;
            }
        }

        private void StbPptExitLight()
        {
            try
            {
                StackPanelPPTControls.Visibility = Visibility.Collapsed;
                BtnPPTSlideShow.Visibility = Visibility.Visible;
                BtnPPTSlideShowEnd.Visibility = Visibility.Collapsed;
                ViewBoxStackPanelMain.Margin = new Thickness(10, 10, 10, 55);
                Main_Grid.Background = new SolidColorBrush(StringToColor("#01FFFFFF"));
                ViewboxFloatingBar.Margin = new Thickness(pointDesktop.X, pointDesktop.Y, -2000, -200);
            }
            catch
            {
            }
        }
    }
}
