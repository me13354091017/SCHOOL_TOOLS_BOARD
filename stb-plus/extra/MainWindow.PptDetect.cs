using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace InkCanvasPlus
{
    public partial class MainWindow
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int StbGetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int StbGetWindowText(IntPtr hWnd, StringBuilder text, int count);

        private const int STB_WS_MAXIMIZE = 0x01000000;

        /// <summary>
        /// 1.0.1.37：放映判定换成 GitHub 上验证过的三重判据（QTimer / is_ppt_slideshow_active，
        /// 配合 RimePPT 的放映窗观察思路）：
        /// 1) 窗口类名是放映专用类：screenClass（PowerPoint）、paneClassDC、MSO_SubWindow_Class、
        ///    kslideshow / wps_slideshow / wpp_slideshow_class（WPS 各版本）；
        /// 2) 窗口属于放映进程（powerpnt / pptview / wpp / wps），且标题带放映关键词，或者是「真全屏」；
        /// 3) 进程名读不到（权限受限）时，要求「标题带放映关键词 + 真全屏」。
        /// 真全屏 = 窗口矩形与所在显示器矩形相差 ≤6px 且不是最大化窗口 —— 编辑窗最大化不算放映，
        /// 这是 1.0.1.33 误判问题的根因，也是这套判据最关键的一条。
        /// </summary>
        private bool StbDetectPptShow(out bool isWps, out IntPtr showHwnd)
        {
            bool found = false;
            bool wps = false;
            IntPtr hit = IntPtr.Zero;
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
                if (pid == 0 || pid == selfPid)
                {
                    return true;
                }

                StringBuilder cls = new StringBuilder(96);
                GetClassName(h, cls, 96);
                string c = cls.ToString().ToLowerInvariant();

                bool showClass = c == "screenclass"
                    || c == "paneclassdc"
                    || c == "mso_subwindow_class"
                    || c == "kslideshow"
                    || c == "wps_slideshow"
                    || c == "wpp_slideshow_class";

                string proc = StbPptProcName(pid);
                bool showProc = proc == "powerpnt" || proc == "pptview" || proc == "wpp" || proc == "wps";
                string title = StbPptWindowTitle(h);
                bool titleHint = StbPptTitleLooksLikeShow(title);

                bool ok;
                if (showClass)
                {
                    ok = true;
                }
                else if (showProc)
                {
                    ok = titleHint || StbPptIsExactFullscreen(h);
                }
                else if (proc == null)
                {
                    ok = titleHint && StbPptIsExactFullscreen(h);
                }
                else
                {
                    ok = false;
                }

                if (!ok)
                {
                    return true;
                }

                found = true;
                hit = h;
                wps = c.StartsWith("wps") || c == "kslideshow" || c == "wpp_slideshow_class"
                    || proc == "wpp" || proc == "wps";
                return false;
            }, IntPtr.Zero);

            isWps = wps;
            showHwnd = hit;
            return found;
        }

        /// <summary>窗口矩形 ≈ 所在显示器矩形（容差 6px），且不是最大化窗口。</summary>
        private bool StbPptIsExactFullscreen(IntPtr h)
        {
            try
            {
                if ((GetWindowLong(h, STB_GWL_STYLE) & STB_WS_MAXIMIZE) != 0)
                {
                    return false;
                }
                StbRect r;
                if (!GetWindowRect(h, out r))
                {
                    return false;
                }
                IntPtr mon = MonitorFromWindow(h, STB_MONITOR_DEFAULTTONEAREST);
                if (mon == IntPtr.Zero)
                {
                    return false;
                }
                StbMonitorInfo mi = new StbMonitorInfo();
                mi.cbSize = Marshal.SizeOf(typeof(StbMonitorInfo));
                if (!GetMonitorInfo(mon, ref mi))
                {
                    return false;
                }
                int tol = 6;
                return Math.Abs(r.Left - mi.rcMonitor.Left) <= tol
                    && Math.Abs(r.Top - mi.rcMonitor.Top) <= tol
                    && Math.Abs(r.Right - mi.rcMonitor.Right) <= tol
                    && Math.Abs(r.Bottom - mi.rcMonitor.Bottom) <= tol;
            }
            catch
            {
                return false;
            }
        }

        private static string StbPptWindowTitle(IntPtr h)
        {
            try
            {
                int len = StbGetWindowTextLength(h);
                if (len <= 0)
                {
                    return string.Empty;
                }
                StringBuilder sb = new StringBuilder(len + 2);
                StbGetWindowText(h, sb, sb.Capacity);
                return sb.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }

        private static bool StbPptTitleLooksLikeShow(string title)
        {
            if (string.IsNullOrEmpty(title))
            {
                return false;
            }
            string t = title.ToLowerInvariant();
            return t.Contains("幻灯片放映")
                || t.Contains("幻燈片放映")
                || t.Contains("slide show")
                || t.Contains("slideshow")
                || t.Contains("wps presentation")
                || t.Contains("演示放映")
                || t.Contains("全屏放映");
        }

        private static string StbPptProcName(uint pid)
        {
            try
            {
                using (Process p = Process.GetProcessById((int)pid))
                {
                    return (p.ProcessName ?? string.Empty).ToLowerInvariant();
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
