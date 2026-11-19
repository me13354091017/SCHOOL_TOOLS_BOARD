# apply-stb-fix13.ps1
# 1.0.1.19：
#   ① 浮动栏编辑窗「当前浮动栏」增加预览条：未接管时显示默认浮动栏样式（画笔/颜色/橡皮/撤销…），接管后显示自己的条目
#   ② 设置页「外观主题」新增「自定义浮动栏 → 编辑浮动栏」入口，方便找到
param(
    [string]$Root = "plus-src",
    [string]$Version = "1.0.0.0"
)

$ErrorActionPreference = 'Stop'
trap {
    Write-Host ("::error::fix13: " + $_.Exception.Message)
    throw
}

$app = Join-Path $Root 'Ink Canvas'
$utf8 = New-Object System.Text.UTF8Encoding($false)

function Assert([bool]$cond, [string]$msg) {
    if (-not $cond) { throw "ANCHOR FAIL: $msg" }
}

Write-Host "== PHASE Z1: copy floatbar editor and settings entry =="
foreach ($name in @('StbFloatBarWindow.xaml', 'StbFloatBarWindow.xaml.cs', 'StbSettingsWindow.Bar.cs')) {
    $src = Join-Path $PSScriptRoot ('extra/' + $name)
    Assert (Test-Path -LiteralPath $src) ("missing source " + $name)
    Copy-Item -LiteralPath $src -Destination $app -Force
    Write-Host ("PATCHED  copy " + $name)
}

Write-Host "== PHASE Z2: settings card 编辑浮动栏 =="
$sx = Join-Path $app 'StbSettingsWindow.xaml'
$t = Get-Content -LiteralPath $sx -Raw
$anchor = '<TextBlock Text="外观主题" FontSize="22" FontWeight="SemiBold" Foreground="#22201C" Margin="0,4,0,12" />'
Assert ($t.Contains($anchor)) 'theme title anchor missing'
$card = @'
                            <Border Style="{StaticResource StbCard}">
                                <Grid>
                                    <Grid.ColumnDefinitions><ColumnDefinition Width="*" /><ColumnDefinition Width="Auto" /></Grid.ColumnDefinitions>
                                    <StackPanel Grid.Column="0">
                                        <TextBlock Text="自定义浮动栏" Style="{StaticResource StbCardTitle}" />
                                        <TextBlock Text="用组件方式编排浮动栏上的工具：增删、排序、调图标大小、开关文字" Style="{StaticResource StbCardDesc}" />
                                    </StackPanel>
                                    <Button Grid.Column="1" Content="编辑浮动栏" Padding="14,6" VerticalAlignment="Center"
                                            Click="BtnEditFloatBarSettings_Click" />
                                </Grid>
                            </Border>
'@
$t = $t.Replace($anchor, $anchor + "`r`n" + $card.Trim("`r", "`n"))
[IO.File]::WriteAllText($sx, $t, $utf8)
Write-Host 'PATCHED  settings edit-floatbar entry'

Write-Host "== PHASE Z3: verify =="
$n = Get-Content -LiteralPath $sx -Raw
Assert ($n.Contains('BtnEditFloatBarSettings_Click')) 'settings entry missing'
$fx = Get-Content -LiteralPath (Join-Path $app 'StbFloatBarWindow.xaml') -Raw
Assert ($fx.Contains('x:Name="PanelPreview"')) 'preview panel missing'
Assert ($fx.Contains('x:Name="TextPreviewMode"')) 'preview caption missing'
$fc = Get-Content -LiteralPath (Join-Path $app 'StbFloatBarWindow.xaml.cs') -Raw
Assert ($fc.Contains('private void BuildPreview()')) 'BuildPreview missing'
Assert ($fc.Contains('（默认浮动栏）')) 'default preview text missing'
Assert (Test-Path -LiteralPath (Join-Path $app 'StbSettingsWindow.Bar.cs')) 'settings partial missing'
Write-Host 'OK  PHASE Z verified'

Write-Host "STB FIX13 PATCHES DONE"
