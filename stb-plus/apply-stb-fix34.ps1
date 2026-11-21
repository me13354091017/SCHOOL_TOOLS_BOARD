# apply-stb-fix34.ps1
# 1.0.1.41 收尾：
# 1) PptPage.cs 里用了 LogHelper，补 InkCanvasPlus.Helpers 的 using（1.0.1.41 首轮构建就挂在这）；
# 2) 翻页加去重：一次点击若被触发两次，会和原生翻页叠加导致跳页，120ms 内只认一次。
param([string]$Root = "plus-src", [string]$Version = "1.0.0.0")

$ErrorActionPreference = 'Stop'
trap { Write-Host ("::error::fix34: " + $_.Exception.Message); throw }

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

$g = Join-Path $app 'MainWindow.PptPage.cs'
$t = ReadText $g

$old = @'
using System.Windows.Ink;
'@
$new = @'
using System.Windows.Ink;
using InkCanvasPlus.Helpers;
'@
$t = ReplaceOnce $t $old $new 'using helpers'

$old = @'
        private IntPtr _stbPptLastKeyHwnd = IntPtr.Zero;
'@
$new = @'
        private IntPtr _stbPptLastKeyHwnd = IntPtr.Zero;
        private int _stbPptTurnAt = 0;
'@
$t = ReplaceOnce $t $old $new 'turn field'

$old = @'
            if (!_stbPptLight)
            {
                return false;
            }
'@
$new = @'
            if (!_stbPptLight)
            {
                return false;
            }
            int stbNow = Environment.TickCount;
            if (stbNow - _stbPptTurnAt >= 0 && stbNow - _stbPptTurnAt < 120)
            {
                LogHelper.WriteLogToFile("STB paging duplicate ignored", LogHelper.LogType.Event);
                return true;
            }
            _stbPptTurnAt = stbNow;
'@
$t = ReplaceOnce $t $old $new 'turn throttle'
WriteText $g $t

$g2 = ReadText $g
Assert ($g2.Contains('using InkCanvasPlus.Helpers;')) 'using assert'
Assert ($g2.Contains('_stbPptTurnAt')) 'throttle assert'
Write-Host 'OK fix34 verified'
