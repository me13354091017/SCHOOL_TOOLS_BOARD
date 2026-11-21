# apply-stb-fix36.ps1
# 1.0.1.43：修「轻量批注下点结束放映没反应」。
#   给 Esc 补上扫描码 lParam（lParam=0 会被 Qt 放映窗直接丢掉）。
param([string]$Root = "plus-src", [string]$Version = "1.0.0.0")

$ErrorActionPreference = 'Stop'
trap { Write-Host ("::error::fix36: " + $_.Exception.Message); throw }

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
                _stbPptLastKeyHwnd = h;
                LogHelper.WriteLogToFile("STB paging send vk=" + vk + " hwnd=" + h, LogHelper.LogType.Event);
                StbPostMessage(h, STB_WM_KEYDOWN, new IntPtr(vk), IntPtr.Zero);
                StbPostMessage(h, STB_WM_KEYUP, new IntPtr(vk), IntPtr.Zero);
'@
$new = @'
                _stbPptLastKeyHwnd = h;
                LogHelper.WriteLogToFile("STB paging send vk=" + vk + " hwnd=" + h, LogHelper.LogType.Event);
                // 1.0.1.43：WPS 的 Qt 放映窗只认带扫描码的 lParam；lParam=0 会被直接丢掉
                StbSetForegroundWindow(h);
                System.Threading.Thread.Sleep(80);
                int stbScan = vk == 0x1B ? 0x01 : (vk == 0x22 ? 0x51 : (vk == 0x21 ? 0x49 : 0));
                int stbDown = 1 | (stbScan << 16);
                IntPtr stbLpDown = new IntPtr(stbDown);
                IntPtr stbLpUp = new IntPtr(unchecked((int)0xC0000000) | stbDown);
                StbPostMessage(h, STB_WM_KEYDOWN, new IntPtr(vk), stbLpDown);
                StbPostMessage(h, STB_WM_KEYUP, new IntPtr(vk), stbLpUp);
'@
$t = ReplaceOnce $t $old $new 'scan code lparam'
WriteText $pp $t
$t2 = ReadText $pp
Assert ($t2.Contains('stbLpDown')) 'lparam assert'
Assert ($t2.Contains('StbSetForegroundWindow(h);')) 'foreground assert'
Write-Host 'OK fix36 verified'

$fix37 = Join-Path $PSScriptRoot 'apply-stb-fix37.ps1'
Assert (Test-Path -LiteralPath $fix37) 'missing fix37'
& $fix37 -Root $Root -Version $Version
