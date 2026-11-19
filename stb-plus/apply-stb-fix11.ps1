# apply-stb-fix11.ps1
# 1.0.1.17：只保留「编辑浮动栏」
#   ① 浮动栏 / 左侧竖栏的「组件」入口改为直接打开「组件 · 浮动栏」编辑窗（旧的主界面组件窗不再出现）
#   ② 两个入口的文字由“组件”改为“浮动栏”
#   ③ 工具库新增「保存到其它」（复用另存对话框，StbSaveAsNow）
param(
    [string]$Root = "plus-src",
    [string]$Version = "1.0.0.0"
)

$ErrorActionPreference = 'Stop'
trap {
    Write-Host ("::error::fix11: " + $_.Exception.Message)
    throw
}

$app = Join-Path $Root 'Ink Canvas'
$utf8 = New-Object System.Text.UTF8Encoding($false)

function Assert([bool]$cond, [string]$msg) {
    if (-not $cond) { throw "ANCHOR FAIL: $msg" }
}

Write-Host "== PHASE X1: copy entry / save-as / floatbar sources =="
foreach ($name in @('MainWindow.Components.cs', 'MainWindow.SaveAs.cs', 'StbBarRegistry.cs', 'StbFloatBar.cs')) {
    $src = Join-Path $PSScriptRoot ('extra/' + $name)
    Assert (Test-Path -LiteralPath $src) ("missing source " + $name)
    Copy-Item -LiteralPath $src -Destination $app -Force
    Write-Host ("PATCHED  copy " + $name)
}

Write-Host "== PHASE X1b: add save-as item to tool library =="
$reg = Join-Path $app 'StbBarRegistry.cs'
$rt = Get-Content -LiteralPath $reg -Raw
$rAnchor = '                Add(list, "stb.bar.clear", "清屏", "✕", "清空当前画布的所有笔迹。");'
Assert ($rt.Contains($rAnchor)) 'clear item anchor missing'
Assert (-not $rt.Contains('stb.bar.saveas')) 'saveas item already exists'
$rt = $rt.Replace($rAnchor, '                Add(list, "stb.bar.saveas", "保存到其它", "⤓", "把当前墨迹另存到指定位置。");' + "`r`n" + $rAnchor)
[IO.File]::WriteAllText($reg, $rt, $utf8)

$fbb = Join-Path $app 'StbFloatBar.cs'
$ft = Get-Content -LiteralPath $fbb -Raw
$fAnchor = '                    case "stb.bar.clear":'
Assert ($ft.Contains($fAnchor)) 'clear action anchor missing'
$ft = $ft.Replace($fAnchor, '                    case "stb.bar.saveas":' + "`r`n" +
    '                        StbSaveAsNow();' + "`r`n" +
    '                        return;' + "`r`n" + $fAnchor)
[IO.File]::WriteAllText($fbb, $ft, $utf8)
Write-Host 'PATCHED  tool library save-as'

Write-Host "== PHASE X2: labels 组件 -> 浮动栏 =="
$mw = Join-Path $app 'MainWindow.xaml'
$t = Get-Content -LiteralPath $mw -Raw
$n1 = ([regex]::Matches($t, 'Text="组件" FontSize="8"')).Count
$n2 = ([regex]::Matches($t, 'Text="组件" FontSize="10"')).Count
Assert ($n1 -eq 1) ('floatbar label count: ' + $n1)
Assert ($n2 -eq 1) ('leftbar label count: ' + $n2)
$t = $t.Replace('Text="组件" FontSize="8"', 'Text="浮动栏" FontSize="8"')
$t = $t.Replace('Text="组件" FontSize="10"', 'Text="浮动栏" FontSize="10"')
[IO.File]::WriteAllText($mw, $t, $utf8)
Write-Host 'PATCHED  labels -> 浮动栏'

Write-Host "== PHASE X3: verify =="
$c = Get-Content -LiteralPath (Join-Path $app 'MainWindow.Components.cs') -Raw
Assert ($c.Contains('StbFloatBarWindow.ShowFor(this);')) 'entry not redirected'
Assert (-not $c.Contains('StbComponentManagerWindow.ShowFor')) 'still opens legacy manager'
$s = Get-Content -LiteralPath (Join-Path $app 'MainWindow.SaveAs.cs') -Raw
Assert ($s.Contains('internal void StbSaveAsNow()')) 'save-as not exposed'
$r = Get-Content -LiteralPath (Join-Path $app 'StbBarRegistry.cs') -Raw
Assert ($r.Contains('stb.bar.saveas')) 'library missing save-as'
$f = Get-Content -LiteralPath (Join-Path $app 'StbFloatBar.cs') -Raw
Assert ($f.Contains('StbSaveAsNow();')) 'floatbar save-as action missing'
$m = Get-Content -LiteralPath $mw -Raw
Assert ($m.Contains('Text="浮动栏" FontSize="8"')) 'floatbar label missing'
Write-Host 'OK  PHASE X verified'

Write-Host "STB FIX11 PATCHES DONE"
