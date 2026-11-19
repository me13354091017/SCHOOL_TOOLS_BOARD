# apply-stb-fix16.ps1
# 1.0.1.23：笑脸按钮「右键」改为「连点三次」弹菜单；函数画板启动钩子
param(
    [string]$Root = "plus-src",
    [string]$Version = "1.0.0.0"
)

$ErrorActionPreference = 'Stop'
trap {
    Write-Host ("::error::fix16: " + $_.Exception.Message)
    throw
}

$app = Join-Path $Root 'Ink Canvas'

function Assert([bool]$cond, [string]$msg) {
    if (-not $cond) { throw "ANCHOR FAIL: $msg" }
}

function ReadText([string]$path) { return (Get-Content -LiteralPath $path -Raw) }
function WriteText([string]$path, [string]$text) { [IO.File]::WriteAllText($path, $text, (New-Object System.Text.UTF8Encoding($false))) }

Write-Host "== PHASE C1: remove smile rightclick attr =="
$mw = Join-Path $app 'MainWindow.xaml'
$t = ReadText $mw
$withAttr = '<Border x:Name="BorderFloatingBarEmoji" MouseRightButtonUp="StbSmileMenu_Open" FlowDirection="LeftToRight"'
$plain = '<Border x:Name="BorderFloatingBarEmoji" FlowDirection="LeftToRight"'
Assert ($t.Contains($withAttr)) 'smile rightclick attr missing'
$t = $t.Replace($withAttr, $plain)
Assert (-not $t.Contains('StbSmileMenu_Open')) 'attr not removed'
WriteText $mw $t
Write-Host 'PATCHED  smile rightclick removed'

Write-Host "== PHASE C2: hook triple-click =="
$ip = Join-Path $app 'MainWindow.InputAndPanels.cs'
$it = ReadText $ip
$anchor = '            StbApplyFuncButtonVisibility();'
Assert ($it.Contains($anchor)) 'startup hook anchor missing'
Assert (-not $it.Contains('StbInitSmileMenu();')) 'already hooked'
$it = $it.Replace($anchor, $anchor + "`r`n" + '            StbInitSmileMenu();')
WriteText $ip $it
Write-Host 'PATCHED  StbInitSmileMenu hooked'

Write-Host "== PHASE C3: verify =="
$smt = ReadText (Join-Path $app 'MainWindow.SmileMenu.cs')
Assert ($smt.Contains('internal void StbInitSmileMenu()')) 'smile hook missing'
Assert ($smt.Contains('StbSmileEmoji_MouseUp')) 'triple-click handler missing'
$ft = ReadText (Join-Path $app 'MainWindow.Function.cs')
Assert ($ft.Contains('函数画板打开失败')) 'function error hint missing'
Assert ((ReadText (Join-Path $app 'StbFunctionWindow.xaml')).Contains('Topmost="True"')) 'function window not topmost'
Write-Host 'OK  PHASE C verified'

Write-Host "STB FIX16 PATCHES DONE"
