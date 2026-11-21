using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Ink;

namespace InkCanvasPlus
{
    public partial class MainWindow
    {
        private const int STB_VK_PRIOR = 0x21;
        private const int STB_VK_NEXT = 0x22;
        private const int STB_WM_KEYDOWN = 0x0100;
        private const int STB_WM_KEYUP = 0x0101;

        [DllImport("user32.dll")]
        private static extern bool StbPostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        private readonly Dictionary<int, byte[]> _stbPptPageInk = new Dictionary<int, byte[]>();
        private int _stbPptLightPage = 1;
        private IntPtr _stbPptLastKeyHwnd = IntPtr.Zero;

        /// <summary>
        /// 1.0.1.40：COM 是否真的可用（有放映窗）。
        /// 轻量批注（WPS / 权限隔离 / 保护视图）时 pptApplication 可能为 null 或拿不到放映窗，
        /// 这时绝不能去碰 SlideShowWindows[1]，否则 UI 线程抛未处理异常直接崩。
        /// </summary>
        private bool StbPptComReady()
        {
            try
            {
                if (pptApplication == null)
                {
                    return false;
                }
                return StbPptComShowCount() >= 1;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>进入轻量批注时重置页码与每页笔迹缓存。</summary>
        private void StbPptLightResetPages()
        {
            _stbPptLightPage = 1;
            _stbPptPageInk.Clear();
            try
            {
                PptNavigationTextBlock.Text = "第 1 页";
            }
            catch
            {
            }
        }

        private void StbPptLightSavePage()
        {
            try
            {
                MemoryStream ms = new MemoryStream();
                inkCanvas.Strokes.Save(ms);
                if (ms.Length > 8)
                {
                    _stbPptPageInk[_stbPptLightPage] = ms.ToArray();
                }
                else
                {
                    _stbPptPageInk.Remove(_stbPptLightPage);
                }
            }
            catch
            {
            }
        }

        private void StbPptLightLoadPage(int page)
        {
            try
            {
                ClearStrokes(true);
                timeMachine.ClearStrokeHistory();
                byte[] data;
                if (_stbPptPageInk.TryGetValue(page, out data) && data != null && data.Length > 8)
                {
                    inkCanvas.Strokes.Add(new StrokeCollection(new MemoryStream(data)));
                }
                PptNavigationTextBlock.Text = "第 " + page + " 页";
            }
            catch
            {
            }
        }

        /// <summary>
        /// 1.0.1.40：轻量批注翻页。先存本页笔迹、切页码、载入目标页笔迹，再把 PageUp/PageDown 投递到放映窗。
        /// 投递用 PostMessage 直接发给放映窗（WPS 的 Qt5QWindowIcon 放映窗只吃这种投递，
        /// SetForegroundWindow + keybd_event 会因为前台锁被系统忽略，实测点了按钮放映不动）。
        /// </summary>
        private bool StbPptLightTurn(int delta)
        {
            if (!_stbPptLight)
            {
                return false;
            }
            if (_stbPptLightPage < 1)
            {
                _stbPptLightPage = 1;
            }
            StbPptLightSavePage();
            int target = _stbPptLightPage + delta;
            if (target < 1)
            {
                target = 1;
            }
            if (target != _stbPptLightPage)
            {
                _stbPptLightPage = target;
                StbPptLightLoadPage(target);
            }
            StbPptSendShowKey(delta > 0 ? STB_VK_NEXT : STB_VK_PRIOR);
            return true;
        }

        /// <summary>把一次按键投递给放映窗（无 COM 的放映用）。</summary>
        private void StbPptSendShowKey(int vk)
        {
            try
            {
                IntPtr h = _stbPptShowHwnd;
                if (h == IntPtr.Zero)
                {
                    h = _stbPptLastKeyHwnd;
                }
                if (h == IntPtr.Zero)
                {
                    h = StbPptFindShowHwndAny();
                }
                if (h == IntPtr.Zero)
                {
                    return;
                }
                _stbPptLastKeyHwnd = h;
                StbPostMessage(h, STB_WM_KEYDOWN, new IntPtr(vk), IntPtr.Zero);
                StbPostMessage(h, STB_WM_KEYUP, new IntPtr(vk), IntPtr.Zero);
            }
            catch
            {
            }
        }

        /// <summary>
        /// 兜底找放映窗：类名命中放映专用窗口，或 Qt（WPS 新版放映窗）且标题带放映字样。
        /// </summary>
        private IntPtr StbPptFindShowHwndAny()
        {
            IntPtr hit = IntPtr.Zero;
            try
            {
                EnumWindows((h, l) =>
                {
                    if (hit != IntPtr.Zero || !IsWindowVisible(h))
                    {
                        return true;
                    }
                    uint pid;
                    GetWindowThreadProcessId(h, out pid);
                    if (pid == 0)
                    {
                        return true;
                    }
                    string proc = StbPptProcName(pid);
                    if (!(proc == "powerpnt" || proc == "pptview" || proc == "wpp" || proc == "wps"))
                    {
                        return true;
                    }
                    System.Text.StringBuilder cls = new System.Text.StringBuilder(96);
                    GetClassName(h, cls, 96);
                    string c = cls.ToString().ToLowerInvariant();
                    bool showClass = c == "screenclass" || c == "showwnd" || c == "pptshow"
                        || c == "wppshow" || c == "kslideshow" || c == "wps_slideshow"
                        || c == "wpp_slideshow_class";
                    bool qtShow = (c == "qt5qwindowicon" || c == "qt5qwindowtooltip")
                        && StbPptTitleLooksLikeShow(StbPptWindowTitle(h));
                    if (showClass || qtShow)
                    {
                        hit = h;
                        return false;
                    }
                    return true;
                }, IntPtr.Zero);
            }
            catch
            {
            }
            return hit;
        }
    }
}
