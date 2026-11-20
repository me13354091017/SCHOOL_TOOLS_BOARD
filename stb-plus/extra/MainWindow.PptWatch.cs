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
    /// 只认「真正在放映」的窗口，刚打开 PPT 还在编辑时不切换（1.0.1.33 修正）：
    /// 判定顺序——COM 已有放映窗为权威；其次 screenClass；其次 WPS 放映窗类名；
    /// 最后才用「无标题栏 + 覆盖 85% 显示器 + 置顶层」兜底，编辑窗最大化不会被认成放映。
    /// 放映中保持置顶；COM 不可用时退化为轻量批注模式，仍可写字。
    /// </summary>
    public partial class MainWindow
    {
        private DispatcherTimer _stbPptWatch;
        private bool _stbPptLight;
        private int _stbPptMiss;
        private int _stbPptStable;

        private const int STB_HWND_TOPMOST = -1;
        private const uint STB_SWP_NOSIZE = 0x0001;
        private const uint STB_SWP_NOMOVE = 0x0002;
        private const uint STB_SWP_NOACTIVATE = 0x0010;
        private const int STB_WS_CAPTION = 0x00C00000;
        private const int STB_GWL_STYLE = -16;
        private const int STB_GWL_EXSTYLE = -20;
        private const int STB_WS_EX_TOPMOST = 0x00000008;
        private const int STB_MONITOR_DEFAULTTONEAREST = 2;

        [StructLayout(LayoutKind.Sequential)]
        private struct StbRect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct StbMonitorInfo
        {
            public int cbSize;
            public StbRect rcMonitor;
            public StbRect rcWork;
            public int dwFlags;
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

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hWnd, int dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref StbMonitorInfo lpmi);

        private delegate bool StbEnumProc(IntPtr hWnd, IntPtr lParam);

        private void StbInitPptWatch()
        {
            if (_stbPptWatch != null)
            {
                return;
            }
            _stbPptWatch = new DispatcherTimer(DispatcherPriority.Background);
            _stbPptWatch.Interval = TimeSpan.FromMilliseconds(800);
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

        /// <summary>
        /// 找放映窗。只有三类算：
        /// 1：screenClass —— PowerPoint 放映专用窗口类；
        /// 2：WPS / 其它放映窗类名（showwnd / wppshow / pptshow），且全屏或置顶；
        /// 3：兜底——无标题栏 + 覆盖显示器 85% 且置顶，或 COM 已报告有放映窗。
        /// 编辑窗口最大化不置顶，因此不会走进第 3 类。
        /// </summary>
        private static bool StbScanPptShow(HashSet<int> pids, int comShows, out bool isWps)
        {
            bool found = false;
            bool wps = false;
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
                StringBuilder sb = new StringBuilder(200);
                GetClassName(h, sb, 200);
                string cls = sb.ToString().ToLowerInvariant();

                StbRect r;
                if (!GetWindowRect(h, out r))
                {
                    return true;
                }
                double w = r.Right - r.Left;
                double hh = r.Bottom - r.Top;
                if (w < 320 || hh < 240)
                {
                    return true;
                }

                bool msShow = cls == "screenclass";
                bool namedShow = cls.Contains("showwnd") || cls.Contains("wppshow") || cls.Contains("pptshow");

                double mw = SystemParameters.PrimaryScreenWidth;
                double mh = SystemParameters.PrimaryScreenHeight;
                try
                {
                    IntPtr mon = MonitorFromWindow(h, STB_MONITOR_DEFAULTTONEAREST);
                    StbMonitorInfo mi = new StbMonitorInfo();
                    mi.cbSize = Marshal.SizeOf(typeof(StbMonitorInfo));
                    if (mon != IntPtr.Zero && GetMonitorInfo(mon, ref mi))
                    {
                        mw = mi.rcMonitor.Right - mi.rcMonitor.Left;
                        mh = mi.rcMonitor.Bottom - mi.rcMonitor.Top;
                    }
                }
                catch
                {
                }
                bool full = mw > 0 && mh > 0 && (w * hh) >= (mw * mh) * 0.85;

                bool borderless = false;
                bool topmost = false;
                try
                {
                    borderless = (GetWindowLong(h, STB_GWL_STYLE) & STB_WS_CAPTION) == 0;
                    topmost = (GetWindowLong(h, STB_GWL_EXSTYLE) & STB_WS_EX_TOPMOST) != 0;
                }
                catch
                {
                }

                bool hit = msShow
                    || (namedShow && (full || topmost))
                    || (borderless && full && (topmost || comShows >= 1));

                if (hit)
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

        /// <summary>白板被最小化/藏在后面时先还原，否则切了模式用户也看不到。</summary>
        private void StbPptEnsureVisible()
        {
            try
            {
                if (WindowState == WindowState.Minimized)
                {
                    WindowState = WindowState.Normal;
                }
                if (!IsVisible)
                {
                    Show();
                }
            }
            catch
            {
            }
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
                    _stbPptStable = 0;
                    if (_stbPptLight)
                    {
                        _stbPptLight = false;
                        StbPptExitLight();
                    }
                    return;
                }

                int comShows = StbPptComShowCount();
                bool isWps;
                bool hasShow = StbScanPptShow(pids, comShows, out isWps) || comShows >= 1;

                if (pptApplication == null)
                {
                    // 让上游定时器重新建立 COM 连接并注册放映事件（它连上后会自己停）
                    timerCheckPPT.Start();
                }

                if (hasShow)
                {
                    if (!StbPptUiActive())
                    {
                        // 连续两拍都识别到放映才切，刚打开 PPT 时的瞬时全屏/置顶不会误触发；
                        // COM 已报告放映窗时是权威信号，立即切。
                        if (comShows >= 1)
                        {
                            _stbPptStable = 2;
                        }
                        else
                        {
                            _stbPptStable++;
                        }
                        if (_stbPptStable < 2)
                        {
                            return;
                        }

                        _stbPptMiss++;
                        StbPptEnsureVisible();
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
                        if (!StbPptUiActive())
                        {
                            StbPptEnterLight(isWps);
                        }
                    }
                    else
                    {
                        _stbPptMiss = 0;
                        _stbPptStable = 0;
                        StbPptKeepTop();
                    }
                }
                else
                {
                    _stbPptMiss = 0;
                    _stbPptStable = 0;
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
                StbPptEnsureVisible();
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
                Topmost = false;
            }
            catch
            {
            }
        }
    }
}
