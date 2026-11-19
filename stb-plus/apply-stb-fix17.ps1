# apply-stb-fix17.ps1
# 1.0.1.25：浮动栏「运行」图标（右键菜单）+ 更多设置项 + 快捷键盘开关
param(
    [string]$Root = "plus-src",
    [string]$Version = "1.0.0.0"
)

$ErrorActionPreference = 'Stop'
trap {
    Write-Host ("::error::fix17: " + $_.Exception.Message)
    throw
}

$app = Join-Path $Root 'Ink Canvas'

function Assert([bool]$cond, [string]$msg) {
    if (-not $cond) { throw "ANCHOR FAIL: $msg" }
}

function ReadText([string]$path) { return (Get-Content -LiteralPath $path -Raw) }
function WriteText([string]$path, [string]$text) { [IO.File]::WriteAllText($path, $text, (New-Object System.Text.UTF8Encoding($false))) }

Write-Host "== PHASE D1: prefs RunIcon / Keypad =="
$prefs = Join-Path $app 'StbPrefs.cs'
$pt = ReadText $prefs
Assert ($pt.Contains('internal static bool FuncButton')) 'FuncButton anchor missing'
Assert (-not $pt.Contains('internal static bool RunIcon')) 'already has RunIcon'
$add = @'
        /// <summary>浮动栏运行图标；默认开。</summary>
        internal static bool RunIcon
        {
            get { return ReadFlag("runIcon", true); }
            set { WriteFlag("runIcon", value); }
        }

        /// <summary>函数画板快捷输入键盘；默认开。</summary>
        internal static bool Keypad
        {
            get { return ReadFlag("keypad", true); }
            set { WriteFlag("keypad", value); }
        }

'@
$anchor = '        /// <summary>浮动栏常驻「插入函数」按钮；默认开。</summary>'
Assert ($pt.Contains($anchor)) 'FuncButton doc anchor missing'
$pt = $pt.Replace($anchor, $add + $anchor)
WriteText $prefs $pt
Write-Host 'PATCHED  StbPrefs RunIcon / Keypad'

Write-Host "== PHASE D2: run icon on float bar =="
$mw = Join-Path $app 'MainWindow.xaml'
$t = ReadText $mw
$settingPanelAnchor = '<StackPanel Background="Transparent" Orientation="Vertical" HorizontalAlignment="Center" VerticalAlignment="Center" MouseUp="SymbolIconSettings_Click" ToolTip="设置">'
Assert ($t.Contains($settingPanelAnchor)) 'settings panel anchor missing'
Assert (-not $t.Contains('StbBorderRun')) 'already has run icon'
$run = '<Border x:Name="StbBorderRun" Width="24" Height="24" CornerRadius="4" Background="Transparent" ToolTip="运行函数画板（右键打开菜单）" MouseDown="Border_MouseDown" MouseUp="StbRunIcon_MouseUp" MouseRightButtonUp="StbSmileMenu_Open">' + "`r`n" +
'                                <Viewbox Width="16" Height="16" HorizontalAlignment="Center" VerticalAlignment="Center">' + "`r`n" +
'                                    <ui:SymbolIcon Symbol="Play" Foreground="{DynamicResource FloatBarForeground}" />' + "`r`n" +
'                                </Viewbox>' + "`r`n" +
'                            </Border>'
$t = $t.Replace($settingPanelAnchor, $run + "`r`n                            " + $settingPanelAnchor)
WriteText $mw $t
Write-Host 'PATCHED  run icon'

Write-Host "== PHASE D3: settings cards =="
$sx = Join-Path $app 'StbSettingsWindow.xaml'
$st = ReadText $sx
$titleAnchor = '<TextBlock Text="高级" FontSize="22" FontWeight="SemiBold" Foreground="#22201C" Margin="0,4,0,12" />'
Assert ($st.Contains($titleAnchor)) 'advanced title anchor missing'
Assert (-not $st.Contains('SwRunIcon')) 'already has run icon card'

$cardRun = '<Border Style="{StaticResource StbCard}">' + "`r`n" +
'                                <Grid>' + "`r`n" +
'                                    <Grid.ColumnDefinitions><ColumnDefinition Width="*" /><ColumnDefinition Width="Auto" /></Grid.ColumnDefinitions>' + "`r`n" +
'                                    <StackPanel Grid.Column="0">' + "`r`n" +
'                                        <TextBlock Text="浮动栏运行图标" Style="{StaticResource StbCardTitle}" />' + "`r`n" +
'                                        <TextBlock Text="左键打开函数画板，右键弹出应用菜单（组件编辑 / 设置 / 保存 / 打开 / 重启 / 退出）" Style="{StaticResource StbCardDesc}" />' + "`r`n" +
'                                    </StackPanel>' + "`r`n" +
'                                    <ui:ToggleSwitch x:Name="SwRunIcon" Grid.Column="1" OnContent="开" OffContent="关" VerticalAlignment="Center" Toggled="SwRunIcon_Toggled" />' + "`r`n" +
'                                </Grid>' + "`r`n" +
'                            </Border>'

$cardKey = '<Border Style="{StaticResource StbCard}">' + "`r`n" +
'                                <Grid>' + "`r`n" +
'                                    <Grid.ColumnDefinitions><ColumnDefinition Width="*" /><ColumnDefinition Width="Auto" /></Grid.ColumnDefinitions>' + "`r`n" +
'                                    <StackPanel Grid.Column="0">' + "`r`n" +
'                                        <TextBlock Text="函数快捷键盘" Style="{StaticResource StbCardTitle}" />' + "`r`n" +
'                                        <TextBlock Text="数字、sin cos tan、e pi log2 ln、x2 x3 sqrt abs、括号、退格、清空" Style="{StaticResource StbCardDesc}" />' + "`r`n" +
'                                    </StackPanel>' + "`r`n" +
'                                    <ui:ToggleSwitch x:Name="SwKeypad" Grid.Column="1" OnContent="开" OffContent="关" VerticalAlignment="Center" Toggled="SwKeypad_Toggled" />' + "`r`n" +
'                                </Grid>' + "`r`n" +
'                            </Border>'

$cardReset = '<Border Style="{StaticResource StbCard}">' + "`r`n" +
'                                <Grid>' + "`r`n" +
'                                    <Grid.ColumnDefinitions><ColumnDefinition Width="*" /><ColumnDefinition Width="Auto" /></Grid.ColumnDefinitions>' + "`r`n" +
'                                    <StackPanel Grid.Column="0">' + "`r`n" +
'                                        <TextBlock Text="恢复默认设置" Style="{StaticResource StbCardTitle}" />' + "`r`n" +
'                                        <TextBlock Text="笑脸菜单、插入函数、运行图标、快捷键盘、自动回笔全部恢复默认" Style="{StaticResource StbCardDesc}" />' + "`r`n" +
'                                    </StackPanel>' + "`r`n" +
'                                    <Button Grid.Column="1" Content="恢复默认" Padding="14,6" VerticalAlignment="Center" Click="StbBtnResetPrefs_Click" />' + "`r`n" +
'                                </Grid>' + "`r`n" +
'                            </Border>'

$st = $st.Replace($titleAnchor, $titleAnchor + "`r`n" + $cardRun + "`r`n" + $cardKey + "`r`n" + $cardReset)
try { (New-Object System.Xml.XmlDocument).LoadXml($st) } catch { throw ("settings xaml malformed: " + $_.Exception.Message) }
WriteText $sx $st
Write-Host 'PATCHED  settings cards'

Write-Host "== PHASE D4: verify =="
Assert ((ReadText $mw).Contains('x:Name="StbBorderRun"')) 'run icon missing'
Assert ((ReadText $prefs).Contains('internal static bool RunIcon')) 'prefs missing'
Assert ((ReadText $sx).Contains('SwRunIcon')) 'card missing'
Assert ((ReadText (Join-Path $app 'StbSettingsWindow.More.cs')).Contains('SwKeypad_Toggled')) 'handlers missing'
Assert ((ReadText (Join-Path $app 'MainWindow.Function.cs')).Contains('StbRunIcon_MouseUp')) 'run handler missing'
Assert ((ReadText (Join-Path $app 'StbFuncEngine.cs')).Contains('Normalize')) 'engine normalize missing'
Write-Host 'OK  PHASE D verified'

Write-Host "STB FIX17 PATCHES DONE"
