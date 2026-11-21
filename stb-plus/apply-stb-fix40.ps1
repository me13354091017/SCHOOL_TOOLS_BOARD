# apply-stb-fix40.ps1
# 1.0.1.47：fix39 的重试循环用 StbPptFindShowHwndAny() 判断“窗口已消失”，
#   但该方法在这台 WPS 上会误报 0，导致只发一次 Esc 就 return；改成用 _stbPptShowHwnd 判断。
param([string]$Root = "plus-src", [string]$Version = "1.0.0.0")

$ErrorActionPreference = 'Stop'
trap { Write-Host ("::error::fix40: " + $_.Exception.Message); throw }

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
                    System.Threading.Thread.Sleep(220);
                    if (StbPptFindShowHwndAny() == IntPtr.Zero)
                    {
                        return;
                    }
'@
$new = @'
                    StbPptSendShowKey(0x1B);
                    System.Threading.Thread.Sleep(220);
                    if (_stbPptShowHwnd != IntPtr.Zero && !IsWindowVisible(_stbPptShowHwnd))
                    {
                        return;
                    }
'@
$t = ReplaceOnce $t $old $new 'loop visibility by handle'
WriteText $pe $t

$t2 = ReadText $pe
Assert ($t2.Contains('IsWindowVisible(_stbPptShowHwnd)')) 'handle visibility assert'
Write-Host 'OK fix40 verified'

$fix41 = Join-Path $PSScriptRoot 'apply-stb-fix41.ps1'
Assert (Test-Path -LiteralPath $fix41) 'missing fix41'
& $fix41 -Root $Root -Version $Version
