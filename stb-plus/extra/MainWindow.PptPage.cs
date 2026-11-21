using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Ink;

namespace InkCanvasPlus
{
    public partial class MainWindow
    {
        private const int STB_VK_PRIOR = 0x21;
        private const int STB_VK_NEXT = 0x22;

        private readonly Dictionary<int, byte[]> _stbPptPageInk = new Dictionary<int, byte[]>();
        private int _stbPptLightPage = 1;

        /// <summary>
        /// 1.0.1.39：COM 是否真的可用（有放映窗）。
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
        /// 1.0.1.39：轻量批注翻页。先存本页笔迹、切页码、载入目标页笔迹，再给放映窗发 PageUp/PageDown。
        /// 这样没有 COM 的放映（WPS）也能翻页，而且每一页的笔记跟着页走，不会停在上一页。
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

        /// <summary>给放映窗发一次按键（PageUp / PageDown），用于没有 COM 的放映。</summary>
        private void StbPptSendShowKey(int vk)
        {
            try
            {
                IntPtr h = _stbPptShowHwnd;
                if (h == IntPtr.Zero)
                {
                    h = StbPptFindShowHwnd();
                }
                if (h == IntPtr.Zero)
                {
                    return;
                }
                StbSetForegroundWindow(h);
                System.Threading.Thread.Sleep(80);
                StbKeybdEvent((byte)vk, 0, 0, UIntPtr.Zero);
                StbKeybdEvent((byte)vk, 0, 2, UIntPtr.Zero);
            }
            catch
            {
            }
        }
    }
}
