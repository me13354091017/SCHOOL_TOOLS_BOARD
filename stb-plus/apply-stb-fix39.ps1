# apply-stb-fix39.ps1
# 1.0.1.46：真机发现放映窗刚出现时头一两次 Esc 会被 WPS 吞掉（装载动画 / 首帧未就绪），
#   改成最多 8 次、每次间隔 220ms，窗口消失就立即停；仍不关再强制关窗。
param([string]$Root = "plus-src", [string]$Version = "1.0.0.0")

$ErrorActionPreference = 'Stop'
trap { Write-Host ("::error::fix39: " + $_.Exception.Message); throw }

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
                StbPptSendShowKey(0x1B);
                System.Threading.Thread.Sleep(160);
                StbPptSendShowKey(0x1B);
                System.Threading.Thread.Sleep(260);
                StbPptForceCloseShow();
'@
$new = @'
                // 1.0.1.46：放映窗刚出现时前几次 Esc 会被 WPS 吞掉，重试到窗口消失为止
                for (int stbEscTry = 0; stbEscTry < 8; stbEscTry++)
                {
                    StbPptSendShowKey(0x1B);
                    System.Threading.Thread.Sleep(220);
                    if (StbPptFindShowHwndAny() == IntPtr.Zero)
                    {
                        return;
                    }
                }
                StbPptForceCloseShow();
'@
$t = ReplaceOnce $t $old $new 'esc retry loop'
WriteText $pe $t

$t2 = ReadText $pe
Assert ($t2.Contains('stbEscTry < 8')) 'loop assert'
Write-Host 'OK fix39 verified'
