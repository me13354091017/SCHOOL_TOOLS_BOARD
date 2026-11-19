# apply-stb-fix8.ps1
# 1.0.1.14：抬笔后才回笔 + 设置页开关
#   ① 橡皮自动回笔改为「抬笔后才回」：擦的过程中保持橡皮，一次手势可连续擦多笔
#   ② 设置页「墨迹书写」新增开关：橡皮用完自动回到书写（默认开，存 %AppData%\SchoolToolsBoard\prefs.txt）
# 1.0.1.15：尾部串接 fix9（浮动栏接入组件系统）
param(
    [string]$Root = "plus-src",
    [string]$Version = "1.0.0.0"
)

$ErrorActionPreference = 'Stop'
trap {
    Write-Host ("::error::fix8: " + $_.Exception.Message)
    throw
}

$app = Join-Path $Root 'Ink Canvas'
$utf8 = New-Object System.Text.UTF8Encoding($false)

function Assert([bool]$cond, [string]$msg) {
    if (-not $cond) { throw "ANCHOR FAIL: $msg" }
}

Write-Host "== PHASE U1: copy auto-pen / prefs sources =="
foreach ($name in @('MainWindow.Autoswitch.cs', 'MiniBoardWindow.AutoPen.cs', 'StbPrefs.cs', 'StbSettingsWindow.AutoPen.cs')) {
    $src = Join-Path $PSScriptRoot ('extra/' + $name)
    Assert (Test-Path -LiteralPath $src) ("missing source " + $name)
    Copy-Item -LiteralPath $src -Destination $app -Force
    Write-Host ("PATCHED  copy " + $name)
}

Write-Host "== PHASE U2: settings switch card =="
$sx = Join-Path $app 'StbSettingsWindow.xaml'
Assert (Test-Path -LiteralPath $sx) 'StbSettingsWindow.xaml missing'
$t = Get-Content -LiteralPath $sx -Raw
$anchor = '                        <!-- 1 外观主题 -->'
Assert ($t.Contains($anchor)) 'theme comment anchor missing'
$card = @'
                            <Border Style="{StaticResource StbCard}">
                                <Grid>
                                    <Grid.ColumnDefinitions><ColumnDefinition Width="*" /><ColumnDefinition Width="Auto" /></Grid.ColumnDefinitions>
                                    <StackPanel Grid.Column="0">
                                        <TextBlock Text="橡皮用完自动回到书写" Style="{StaticResource StbCardTitle}" />
                                        <TextBlock Text="橡皮擦掉笔迹、抬笔后自动切回画笔；关闭后擦完仍保持橡皮" Style="{StaticResource StbCardDesc}" />
                                    </StackPanel>
                                    <ui:ToggleSwitch x:Name="SwAutoPenBack" Grid.Column="1" OnContent="开" OffContent="关"
                                                     VerticalAlignment="Center" Loaded="SwAutoPenBack_Loaded"
                                                     Toggled="SwAutoPenBack_Toggled" />
                                </Grid>
                            </Border>
'@
$t = $t.Replace($anchor, $card + $anchor)
[IO.File]::WriteAllText($sx, $t, $utf8)
Write-Host 'PATCHED  settings switch card'

Write-Host "== PHASE U3: verify =="
$n = Get-Content -LiteralPath $sx -Raw
Assert ($n.Contains('x:Name="SwAutoPenBack"')) 'switch not inserted'
Assert ($n.Contains('SwAutoPenBack_Toggled')) 'switch event not bound'
$auto = Get-Content -LiteralPath (Join-Path $app 'MainWindow.Autoswitch.cs') -Raw
Assert ($auto.Contains('_stbErasePending')) 'main auto-pen not deferred'
Assert ($auto.Contains('PreviewStylusUp')) 'main stylus-up hook missing'
$mini = Get-Content -LiteralPath (Join-Path $app 'MiniBoardWindow.AutoPen.cs') -Raw
Assert ($mini.Contains('_stbMiniErasePending')) 'mini auto-pen not deferred'
Assert (Test-Path -LiteralPath (Join-Path $app 'StbPrefs.cs')) 'prefs file missing'
Write-Host 'OK  PHASE U verified'

Write-Host "STB FIX8 PATCHES DONE"

Write-Host "== PHASE V: floatbar component system =="
$fix9 = Join-Path $PSScriptRoot 'apply-stb-fix9.ps1'
Assert (Test-Path -LiteralPath $fix9) 'missing stb-plus/apply-stb-fix9.ps1'
& $fix9 -Root $Root -Version $Version
