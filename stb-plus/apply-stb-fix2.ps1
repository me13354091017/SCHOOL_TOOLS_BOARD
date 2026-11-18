# apply-stb-fix2.ps1
# 1.0.1.6：调色盘选色仍为黑色的根因修复（GetBaseColor 把 -1 回落成黑）、保存目录改名、
# 浮动栏「保存」旁加「保存到其它」、板中板笔尖蓝框（圆环仍被另一处代码点亮）彻底移除、设置页更新说明改写。
param(
    [string]$Root = "plus-src",
    [string]$Version = "1.0.0.0"
)

$ErrorActionPreference = 'Stop'
$app = Join-Path $Root 'Ink Canvas'
$utf8 = New-Object System.Text.UTF8Encoding($false)

function Assert([bool]$cond, [string]$msg) {
    if (-not $cond) { throw "ANCHOR FAIL: $msg" }
}

function Edit([string]$path, [string]$old, [string]$new, [string]$label, [int]$times = 1) {
    Assert (Test-Path -LiteralPath $path) "$label :: file missing $path"
    $t = Get-Content -LiteralPath $path -Raw
    $n = ([regex]::Matches($t, [regex]::Escape($old))).Count
    Assert ($n -ge $times) "$label :: anchor not found (count=$n)"
    $t = $t.Replace($old, $new)
    [IO.File]::WriteAllText($path, $t, $utf8)
    Write-Host ("PATCHED  " + $label + "  (x" + $n + ")")
}

try {

    Write-Host "== PHASE G1: 保存到其它 =="
    $saveAsSrc = Join-Path $PSScriptRoot 'extra/MainWindow.SaveAs.cs'
    Assert (Test-Path -LiteralPath $saveAsSrc) 'missing source MainWindow.SaveAs.cs'
    Copy-Item -LiteralPath $saveAsSrc -Destination $app -Force
    Write-Host 'PATCHED  copy MainWindow.SaveAs.cs'

    # 浮动栏「保存」图标右侧加一个「保存到其它」入口（OpenFile 图标 = 另存到指定目录）
    $mw = Join-Path $app 'MainWindow.xaml'
    Edit $mw '<ui:SymbolIcon Symbol="Save" Foreground="{DynamicResource FloatBarForeground}" MouseDown="Border_MouseDown" MouseUp="SymbolIconSaveStrokes_MouseUp" />' @'
<ui:SymbolIcon Symbol="Save" Foreground="{DynamicResource FloatBarForeground}" MouseDown="Border_MouseDown" MouseUp="SymbolIconSaveStrokes_MouseUp" />
                                                <ui:SymbolIcon Symbol="OpenFile" ToolTip="保存到其它位置" Foreground="{DynamicResource FloatBarForeground}" MouseDown="Border_MouseDown" MouseUp="StbSymbolIconSaveAs_MouseUp" />
'@ '浮动栏 保存到其它入口'

    Write-Host "== PHASE G2: 调色盘颜色被回落的根因 =="
    $inp = Join-Path $app 'MainWindow.InputAndPanels.cs'
    # GetBaseColor 的 default 分支把未知索引（调色盘自定义色）回落成黑色，导致点完色又变回黑笔
    Edit $inp @'
                case 5: return StringToColor("#FFFEFEFE");
                default: return Colors.Black;
'@ @'
                case 5: return StringToColor("#FFFEFEFE");
                default: return stbCustomInkColor ?? Colors.Black;
'@ 'GetBaseColor 支持自定义色'

    Write-Host "== PHASE G3: 墨迹与截图目录改名 =="
    foreach ($f in @('MainWindow.Features.cs', 'MainWindow.PresentationAndSettings.cs')) {
        $p = Join-Path $app $f
        Edit $p 'Ink Canvas Strokes' 'SchoolToolsBoard\Strokes' "$f 墨迹目录改名" 2
        Edit $p 'Ink Canvas Screenshots' 'SchoolToolsBoard\Screenshots' "$f 截图目录改名" 2
    }

    Write-Host "== PHASE G4: 板中板笔尖蓝框 =="
    # MiniBoardWindow.xaml.cs 里还有一套自己的圆环逻辑（真正在跑的 MouseMove），一并关掉
    $mbcs = Join-Path $app 'MiniBoardWindow.xaml.cs'
    Edit $mbcs '            MiniEraserRing.Visibility = Visibility.Visible;' '            MiniEraserRing.Visibility = Visibility.Collapsed;' '板中板 xaml.cs 圆环不显示'
    Edit $mbcs '            MiniInkCanvas.Cursor = Cursors.None;' '            MiniInkCanvas.Cursor = Cursors.Pen;' '板中板 xaml.cs 光标恢复'

    # 圆环元素本身改成全透明，任何路径点亮也看不到蓝框
    foreach ($x in @('MainWindow.xaml', 'MiniBoardWindow.xaml')) {
        $p = Join-Path $app $x
        Edit $p 'Stroke="#CC1F6FC4" StrokeThickness="1.5"' 'Stroke="Transparent" StrokeThickness="0"' "$x 圆环描边透明"
    }

    Write-Host "== PHASE G5: 设置页下载更新说明 =="
    $sx = Join-Path $app 'StbSettingsWindow.xaml'
    Edit $sx '<TextBlock Text="检查更新会读取本仓库 Release 上的 version.xml" Style="{StaticResource StbCardDesc}" />' @'
<TextBlock Text="检查更新会读取 GitHub Release 上的 version.xml；有新版本时可直接下载安装包，也可以去发布页手动下载" Style="{StaticResource StbCardDesc}" TextWrapping="Wrap" MaxWidth="430" />
'@ '设置页 下载更新说明'

    Write-Host "== PHASE G6: 断言 =="
    Assert (Test-Path -LiteralPath (Join-Path $app 'MainWindow.SaveAs.cs')) '保存到其它代码未拷入'
    Assert (Select-String -LiteralPath $mw -Pattern 'StbSymbolIconSaveAs_MouseUp' -Quiet) '浮动栏 保存到其它入口未插入'
    Assert (Select-String -LiteralPath $inp -Pattern 'stbCustomInkColor' -Quiet) 'GetBaseColor 未接入自定义色'
    $feat = Get-Content -LiteralPath (Join-Path $app 'MainWindow.Features.cs') -Raw
    Assert (-not $feat.Contains('Ink Canvas Strokes')) '墨迹目录仍为旧名'
    Assert (-not $feat.Contains('Ink Canvas Screenshots')) '截图目录仍为旧名'
    $mb = Get-Content -LiteralPath $mbcs -Raw
    Assert (-not $mb.Contains('Cursors.None')) '板中板 橡皮仍是隐藏光标'
    Assert (-not $mb.Contains('MiniEraserRing.Visibility = Visibility.Visible')) '板中板 圆环仍被点亮'
    Write-Host 'OK  PHASE G 校验通过'
}
catch {
    Write-Host ("::error::STB fix2 failed: " + $_.Exception.Message)
    throw
}

Write-Host "STB FIX2 PATCHES DONE"
