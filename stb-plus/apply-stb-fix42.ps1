# apply-stb-fix42.ps1
# 1.0.1.49：真机 A/B 定位出 WPS Qt 放映窗的最终解法——先给窗口投递鼠标移动/按下/抬起消息，
#   让 Qt 认为被点了一下，再发 Esc（带扫描码）就会立即结束放映；不需要抢焦点、不需要隐藏 STB。
#   隐藏+物理点击的老方案保留为兜底。
param([string]$Root = "plus-src", [string]$Version = "1.0.0.0")

$ErrorActionPreference = 'Stop'
trap { Write-Host ("::error::fix42: " + $_.Exception.Message); throw }

$app = Join-Path $Root 'Ink Canvas'
function Assert([bool]$c, [string]$m) { if (-not $c) { throw ("ANCHOR FAIL: " + $m) } }
function ReadText([string]$p) { return (Get-Content -LiteralPath $p -Raw) }
function WriteText([string]$p, [string]$t) { [IO.File]::WriteAllText($p, $t, (New-Object System.Text.UTF8Encoding($false))) }
function ReplaceOnce([string]$t, [string]$old, [string]$new, [string]$tag) {
    $i = $t.IndexOf($old)
    Assert ($i -ge 0) ($tag + ' missing')
    Assert ($t.IndexOf($old, $i + 1) -lt 0) ($tag + ' not unique')
    return $t.Substring(0, $i) + $new + $t.Substring($i + $old.Length)
}

$pe = Join-Path $app 'MainWindow.PptWatchEnd.cs'
$t = ReadText $pe

$old = @'
        [DllImport("user32.dll")]
        private static extern void StbKeybdEvent(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
'@
$new = @'
        [DllImport("user32.dll")]
        private static extern void StbKeybdEvent(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern IntPtr StbPostMessageShow(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool StbGetClientRect(IntPtr hWnd, out StbShowRect lpRect);

        private struct StbShowRect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
'@
$t = ReplaceOnce $t $old $new 'pinvoke post getrect'

$old = @'
        /// <summary>没有 COM 的放映（WPS / 轻量批注）用 Esc 真正结束放映。</summary>
        private void StbPptSendEsc()
'@
$new = @'
        /// <summary>
        /// 1.0.1.49：给放映窗投递一次鼠标点击（消息级，不需要前台）。
        /// Qt 放映窗只有在收到过一次鼠标点击后才响应 Esc，这是本轮真机 A/B 测出来的关键。
        /// </summary>
        private void StbPptClickShow()
        {
            IntPtr h = _stbPptShowHwnd;
            if (h == IntPtr.Zero)
            {
                h = StbPptFindShowHwnd();
            }
            if (h == IntPtr.Zero || !IsWindowVisible(h))
            {
                return;
            }
            StbShowRect rc;
            if (!StbGetClientRect(h, out rc))
            {
                return;
            }
            int x = (rc.Right - rc.Left) / 2;
            int y = (rc.Bottom - rc.Top) / 2;
            if (x < 0) { x = 0; }
            if (y < 0) { y = 0; }
            IntPtr lp = (IntPtr)((y << 16) | (x & 0xFFFF));
            StbPostMessageShow(h, 0x0200, IntPtr.Zero, lp);
            StbPostMessageShow(h, 0x0201, (IntPtr)1, lp);
            StbPostMessageShow(h, 0x0202, IntPtr.Zero, lp);
        }

        /// <summary>没有 COM 的放映（WPS / 轻量批注）用 Esc 真正结束放映。</summary>
        private void StbPptSendEsc()
'@
$t = ReplaceOnce $t $old $new 'click show method'

$old = @'
                // 1.0.1.48：先投一次 Esc；Qt 不认再隐藏面板、点一下放映窗、发 Esc
                StbPptSendShowKey(0x1B);
                System.Threading.Thread.Sleep(160);
                StbPptHideClickEsc();
                if (_stbPptShowHwnd == IntPtr.Zero || !IsWindowVisible(_stbPptShowHwnd))
                {
                    return;
                }
                StbPptForceCloseShow();
'@
$new = @'
                // 1.0.1.49：先把鼠标点进放映窗（消息级），再发 Esc；实测 WPS Qt 立即关窗
                StbPptClickShow();
                System.Threading.Thread.Sleep(140);
                StbPptSendShowKey(0x1B);
                System.Threading.Thread.Sleep(260);
                if (_stbPptShowHwnd == IntPtr.Zero || !IsWindowVisible(_stbPptShowHwnd))
                {
                    return;
                }
                // 兜底 1：隐藏面板 + 物理点击 + Esc
                StbPptHideClickEsc();
                if (_stbPptShowHwnd == IntPtr.Zero || !IsWindowVisible(_stbPptShowHwnd))
                {
                    return;
                }
                // 兜底 2：WM_CLOSE
                StbPptForceCloseShow();
'@
$t = ReplaceOnce $t $old $new 'click then esc'
WriteText $pe $t

$t2 = ReadText $pe
Assert ($t2.Contains('StbPptClickShow();')) 'click call assert'
Assert ($t2.Contains('StbPostMessageShow(h, 0x0201')) 'click message assert'
Write-Host 'OK fix42 verified'
