# apply-stb-fix37.ps1
# 1.0.1.44：轻量批注结束放映的兜底。真机实测：STB 进程内 SetForegroundWindow 拿不到前台权，
#   Qt 放映窗仍会忽略带 lParam 的 Esc；改为向放映窗发 WM_CLOSE。
param([string]$Root = "plus-src", [string]$Version = "1.0.0.0")

$ErrorActionPreference = 'Stop'
trap { Write-Host ("::error::fix37: " + $_.Exception.Message); throw }

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

$pp = Join-Path $app 'MainWindow.PptPage.cs'
$t = ReadText $pp
$old = @'
        /// <summary>
        /// 兜底找放映窗：类名命中放映专用窗口，或 Qt（WPS 新版放映窗）且标题带放映字样。
        /// </summary>
'@
$new = @'
        /// <summary>
        /// 1.0.1.44：结束放映的兜底。WPS 的 Qt 放映窗在 STB 进程里抢不到前台时不会响应 Esc，
        /// 改为直接向放映窗发 WM_CLOSE。
        /// </summary>
        private void StbPptForceCloseShow()
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
                if (h == IntPtr.Zero || !IsWindowVisible(h))
                {
                    return;
                }
                LogHelper.WriteLogToFile("STB force close show hwnd=" + h, LogHelper.LogType.Event);
                StbPostMessage(h, 0x0010, IntPtr.Zero, IntPtr.Zero);
            }
            catch
            {
            }
        }

        /// <summary>
        /// 兜底找放映窗：类名命中放映专用窗口，或 Qt（WPS 新版放映窗）且标题带放映字样。
        /// </summary>
'@
$t = ReplaceOnce $t $old $new 'force close method'
WriteText $pp $t

$pe = Join-Path $app 'MainWindow.PptWatchEnd.cs'
$t = ReadText $pe
$old = @'
                StbPptSendShowKey(0x1B);
                System.Threading.Thread.Sleep(120);
                StbPptSendShowKey(0x1B);
'@
$new = @'
                StbPptSendShowKey(0x1B);
                System.Threading.Thread.Sleep(160);
                StbPptSendShowKey(0x1B);
                System.Threading.Thread.Sleep(260);
                StbPptForceCloseShow();
'@
$t = ReplaceOnce $t $old $new 'force close after esc'
WriteText $pe $t

$p2 = ReadText $pp
Assert ($p2.Contains('StbPptForceCloseShow')) 'force close assert'
$e2 = ReadText $pe
Assert ($e2.Contains('StbPptForceCloseShow();')) 'esc call assert'
Write-Host 'OK fix37 verified'

$fix38 = Join-Path $PSScriptRoot 'apply-stb-fix38.ps1'
Assert (Test-Path -LiteralPath $fix38) 'missing fix38'
& $fix38 -Root $Root -Version $Version
