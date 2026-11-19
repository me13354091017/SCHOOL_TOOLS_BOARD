# apply-stb-fix9.ps1
# 1.0.1.15：浮动栏也接入组件系统
#   ① MainWindow 浮动栏里插入 StbCustomBar 容器
#   ② 新增「组件 · 浮动栏」编辑窗（增删 / 排序 / 图标大小 / 文字开关 / 接管开关）
#   ③ 组件窗底部加「编辑浮动栏」入口
param(
    [string]$Root = "plus-src",
    [string]$Version = "1.0.0.0"
)

$ErrorActionPreference = 'Stop'
trap {
    Write-Host ("::error::fix9: " + $_.Exception.Message)
    throw
}

$app = Join-Path $Root 'Ink Canvas'
$utf8 = New-Object System.Text.UTF8Encoding($false)

function Assert([bool]$cond, [string]$msg) {
    if (-not $cond) { throw "ANCHOR FAIL: $msg" }
}

Write-Host "== PHASE V1: copy floatbar component code =="
foreach ($name in @('StbBarRegistry.cs', 'StbFloatBar.cs', 'StbFloatBarWindow.xaml', 'StbFloatBarWindow.xaml.cs', 'StbComponentManagerWindow.Bar.cs')) {
    $src = Join-Path $PSScriptRoot ('extra/' + $name)
    Assert (Test-Path -LiteralPath $src) ("missing source " + $name)
    Copy-Item -LiteralPath $src -Destination $app -Force
    Write-Host ("PATCHED  copy " + $name)
}

Write-Host "== PHASE V2: insert StbCustomBar into floating bar =="
$mw = Join-Path $app 'MainWindow.xaml'
$t = Get-Content -LiteralPath $mw -Raw
$anchor = '<ui:SimpleStackPanel Name="StackPanelFloatingBar" Spacing="10" Orientation="Horizontal">'
$n = ([regex]::Matches($t, [regex]::Escape($anchor))).Count
Assert ($n -eq 1) ('StackPanelFloatingBar anchor count: ' + $n)
$ins = @'
<ui:SimpleStackPanel Name="StackPanelFloatingBar" Spacing="10" Orientation="Horizontal">
                        <StackPanel x:Name="StbCustomBar" Orientation="Horizontal" VerticalAlignment="Center" Visibility="Collapsed" />
'@
$t = $t.Replace($anchor, $ins.Trim("`r", "`n"))
[IO.File]::WriteAllText($mw, $t, $utf8)
Write-Host 'PATCHED  StbCustomBar'

Write-Host "== PHASE V3: hook StbInitFloatBar in Window_Loaded =="
$panel = Join-Path $app 'MainWindow.InputAndPanels.cs'
$t2 = Get-Content -LiteralPath $panel -Raw
$anchor2 = '            StbInitAutoPen();'
Assert ($t2.Contains($anchor2)) 'StbInitAutoPen anchor missing'
$t2 = $t2.Replace($anchor2, $anchor2 + "`r`n            StbInitFloatBar();")
[IO.File]::WriteAllText($panel, $t2, $utf8)
Write-Host 'PATCHED  StbInitFloatBar hook'

Write-Host "== PHASE V4: add edit-floatbar button in component window =="
$cx = Join-Path $app 'StbComponentManagerWindow.xaml'
$t3 = Get-Content -LiteralPath $cx -Raw
$pat = '<Button Grid\.Column="1" Content="进入编辑模式"[\s\S]*?/>'
$hits = ([regex]::Matches($t3, $pat)).Count
Assert ($hits -eq 1) ('edit-mode button count: ' + $hits)
$newBtn = @'
<StackPanel Grid.Column="1" Orientation="Horizontal">
                    <Button Content="编辑浮动栏" Style="{StaticResource PlainButton}"
                            Height="32" Padding="16,0" Click="BtnEditFloatBar_Click" ToolTip="用组件方式编辑浮动栏条目" />
                    <Button Content="进入编辑模式" Style="{StaticResource PlainButton}"
                            Height="32" Padding="16,0" Margin="8,0,0,0" Click="BtnEditMode_Click" />
                </StackPanel>
'@
$t3 = [regex]::Replace($t3, $pat, $newBtn.Trim("`r", "`n"))
[IO.File]::WriteAllText($cx, $t3, $utf8)
Write-Host 'PATCHED  edit floatbar entry'

Write-Host "== PHASE V5: verify =="
$m = Get-Content -LiteralPath $mw -Raw
Assert ($m.Contains('x:Name="StbCustomBar"')) 'StbCustomBar missing'
$p = Get-Content -LiteralPath $panel -Raw
Assert ($p.Contains('StbInitFloatBar();')) 'floatbar hook missing'
$c = Get-Content -LiteralPath $cx -Raw
Assert ($c.Contains('BtnEditFloatBar_Click')) 'button missing'
Assert (Test-Path -LiteralPath (Join-Path $app 'StbBarRegistry.cs')) 'registry missing'
Assert (Test-Path -LiteralPath (Join-Path $app 'StbFloatBarWindow.xaml')) 'editor window missing'
Write-Host 'OK  PHASE V verified'

Write-Host "STB FIX9 PATCHES DONE"
