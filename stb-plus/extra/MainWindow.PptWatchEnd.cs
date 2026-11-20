using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace InkCanvasPlus
{
    public partial class MainWindow
    {
        private IntPtr _stbPptShowHwnd = IntPtr.Zero;
        private DateTime _stbPptUserEndUntil = DateTime.MinValue;

        [DllImport("user32.dll")]
        private static extern bool StbSetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern void StbKeybdEvent(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        /// <summary>用户刚点过「结束放映」的 5 秒静默期。</summary>
        private bool StbPptInUserEndHold()
        {
            return DateTime.Now < _stbPptUserEndUntil;
        }

        /// <summary>点结束放映：进入静默期，这一拍探测不再自动切批注。</summary>
        private void StbPptBeginUserEnd()
        {
            _stbPptUserEndUntil = DateTime.Now.AddSeconds(5);
            _stbPptStable = 0;
            _stbPptMiss = 0;
        }

        /// <summary>找放映窗句柄（判据与 StbScanPptShow 一致），找到就记住，找不到用上次的。</summary>
        private IntPtr StbPptFindShowHwnd()
        {
            IntPtr hit = IntPtr.Zero;
            try
            {
                HashSet<int> pids = StbPptPids();
                if (pids.Count > 0)
                {
                    uint selfPid = (uint)Process.GetCurrentProcess().Id;
                    EnumWindows((h, l) =>
                    {
                        if (hit != IntPtr.Zero)
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
                        bool named = cls == "screenclass"
                            || cls.Contains("showwnd")
                            || cls.Contains("wppshow")
                            || cls.Contains("pptshow");
                        if (!named)
                        {
                            return true;
                        }
                        StbRect r;
                        if (!GetWindowRect(h, out r))
                        {
                            return true;
                        }
                        if ((r.Right - r.Left) < 320 || (r.Bottom - r.Top) < 240)
                        {
                            return true;
                        }
                        hit = h;
                        return false;
                    }, IntPtr.Zero);
                }
            }
            catch
            {
            }
            if (hit != IntPtr.Zero)
            {
                _stbPptShowHwnd = hit;
            }
            return _stbPptShowHwnd;
        }

        /// <summary>没有 COM 的放映（WPS / 轻量批注）用 Esc 真正结束放映。</summary>
        private void StbPptSendEsc()
        {
            try
            {
                IntPtr h = StbPptFindShowHwnd();
                if (h == IntPtr.Zero)
                {
                    return;
                }
                StbSetForegroundWindow(h);
                System.Threading.Thread.Sleep(120);
                StbKeybdEvent(0x1B, 0, 0, UIntPtr.Zero);
                StbKeybdEvent(0x1B, 0, 2, UIntPtr.Zero);
                System.Threading.Thread.Sleep(60);
                StbKeybdEvent(0x1B, 0, 0, UIntPtr.Zero);
                StbKeybdEvent(0x1B, 0, 2, UIntPtr.Zero);
            }
            catch
            {
            }
        }
    }
}
