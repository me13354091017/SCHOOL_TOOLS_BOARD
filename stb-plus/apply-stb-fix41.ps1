# apply-stb-fix41.ps1
# 1.0.1.48：轻量批注（WPS Qt 放映窗）结束放映的最终方案。真机定位：
#   纯 PostMessage 的 Esc 会被 Qt 忽略（无论 lParam/重试/前台）；但只要让放映窗真正收到一次鼠标点击，Esc 立刻生效。
#   所以结束放映时：临时隐藏 STB 顶层面板 → 点击放映窗中心 → 发 Esc → 恢复 STB。
param([string]$Root = "plus-src", [string]$Version = "1.0.0.0")

$ErrorActionPreference = 'Stop'
trap { Write-Host ("::error::fix41: " + $_.Exception.Message); throw }

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
        private static extern bool StbShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool StbSetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        private static extern void StbMouseEvent(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);
'@
$t = ReplaceOnce $t $old $new 'pinvoke hide/click'

$old = @'
        /// <summary>没有 COM 的放映（WPS / 轻量批注）用 Esc 真正结束放映。</summary>
        private void StbPptSendEsc()
'@
$new = @'
        /// <summary>
        /// 1.0.1.48：让 WPS 的 Qt 放映窗真正收到一次鼠标点击，再发 Esc。
        /// 纯 PostMessage 的 Esc 会被 Qt 忽略；临时隐藏 STB 面板后点一下放映窗，Esc 立即生效。
        /// </summary>
        private void StbPptHideClickEsc()
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
            IntPtr self = IntPtr.Zero;
            bool top = Topmost;
            try
            {
                self = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                Topmost = false;
                StbShowWindow(self, 0);
                System.Threading.Thread.Sleep(180);
                StbSetForegroundWindow(h);
                int cx = (int)(System.Windows.SystemParameters.PrimaryScreenWidth / 2);
                int cy = (int)(System.Windows.SystemParameters.PrimaryScreenHeight / 2);
                StbSetCursorPos(cx, cy);
                System.Threading.Thread.Sleep(80);
                StbMouseEvent(0x0002, 0, 0, 0, 0);
                StbMouseEvent(0x0004, 0, 0, 0, 0);
                System.Threading.Thread.Sleep(220);
                StbKeybdEvent(0x1B, 0, 0, UIntPtr.Zero);
                StbKeybdEvent(0x1B, 0, 2, UIntPtr.Zero);
                System.Threading.Thread.Sleep(220);
                StbKeybdEvent(0x1B, 0, 0, UIntPtr.Zero);
                StbKeybdEvent(0x1B, 0, 2, UIntPtr.Zero);
                System.Threading.Thread.Sleep(320);
            }
            catch
            {
            }
            finally
            {
                if (self != IntPtr.Zero)
                {
                    StbShowWindow(self, 5);
                }
                Topmost = top;
            }
        }

        /// <summary>没有 COM 的放映（WPS / 轻量批注）用 Esc 真正结束放映。</summary>
        private void StbPptSendEsc()
'@
$t = ReplaceOnce $t $old $new 'hide click esc method'

$old = @'
                // 1.0.1.46：放映窗刚出现时前几次 Esc 会被 WPS 吞掉，重试到窗口消失为止
                for (int stbEscTry = 0; stbEscTry < 8; stbEscTry++)
                {
                    StbPptSendShowKey(0x1B);
                    System.Threading.Thread.Sleep(220);
                    if (_stbPptShowHwnd != IntPtr.Zero && !IsWindowVisible(_stbPptShowHwnd))
                    {
                        return;
                    }
                }
                StbPptForceCloseShow();
'@
$new = @'
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
$t = ReplaceOnce $t $old $new 'hide click call'
WriteText $pe $t

$t2 = ReadText $pe
Assert ($t2.Contains('StbPptHideClickEsc();')) 'call assert'
Assert ($t2.Contains('StbMouseEvent(0x0002')) 'click assert'
Write-Host 'OK fix41 verified'

$fix42 = Join-Path $PSScriptRoot 'apply-stb-fix42.ps1'
Assert (Test-Path -LiteralPath $fix42) 'missing fix42'
& $fix42 -Root $Root -Version $Version
