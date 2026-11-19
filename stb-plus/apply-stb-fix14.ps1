# apply-stb-fix14.ps1
# 1.0.1.20：笑脸右键菜单 / 插入函数 / 设置页新增项
# 1.0.1.22：尾部串接 fix15（恢复打开图标）
param(
    [string]$Root = "plus-src",
    [string]$Version = "1.0.0.0"
)

$ErrorActionPreference = 'Stop'
trap {
    Write-Host ("::error::fix14: " + $_.Exception.Message)
    throw
}

$app = Join-Path $Root 'Ink Canvas'

function Assert([bool]$cond, [string]$msg) {
    if (-not $cond) { throw "ANCHOR FAIL: $msg" }
}

function ReadText([string]$path) { return (Get-Content -LiteralPath $path -Raw) }
function WriteText([string]$path, [string]$text) { [IO.File]::WriteAllText($path, $text, (New-Object System.Text.UTF8Encoding($false))) }

Write-Host "== PHASE A1: copy new sources =="
$files = @(
    'StbFuncEngine.cs', 'StbFuncPlot.cs',
    'StbFunctionWindow.xaml', 'StbFunctionWindow.xaml.cs',
    'MainWindow.Function.cs', 'MainWindow.SmileMenu.cs',
    'StbSettingsWindow.More.cs'
)
foreach ($name in $files) {
    $src = Join-Path $PSScriptRoot ('extra/' + $name)
    Assert (Test-Path -LiteralPath $src) ("missing source " + $name)
    Copy-Item -LiteralPath $src -Destination $app -Force
}

Write-Host "== PHASE A2: prefs flags =="
$prefs = Join-Path $app 'StbPrefs.cs'
$pt = ReadText $prefs
Assert ($pt.Contains('private static bool ReadFlag(string key, bool fallback)')) 'prefs anchor'
$ins = "        internal static bool SmileMenu`r`n        {`r`n            get { return ReadFlag(`"smileMenu`", true); }`r`n            set { WriteFlag(`"smileMenu`", value); }`r`n        }`r`n`r`n        internal static bool FuncButton`r`n        {`r`n            get { return ReadFlag(`"funcButton`", true); }`r`n            set { WriteFlag(`"funcButton`", value); }`r`n        }`r`n`r`n"
$pt = $pt.Replace('        private static bool ReadFlag(string key, bool fallback)', $ins + '        private static bool ReadFlag(string key, bool fallback)')
WriteText $prefs $pt

Write-Host "== PHASE A3: main window xaml =="
$mw = Join-Path $app 'MainWindow.xaml'
$t = ReadText $mw
$emojiAnchor = '<Border x:Name="BorderFloatingBarEmoji" FlowDirection="LeftToRight"'
Assert ($t.Contains($emojiAnchor)) 'emoji anchor'
$t = $t.Replace($emojiAnchor, '<Border x:Name="BorderFloatingBarEmoji" MouseRightButtonUp="StbSmileMenu_Open" FlowDirection="LeftToRight"')

$settingAnchor = '<ui:SymbolIcon Symbol="Setting" Foreground="{DynamicResource FloatBarForeground}" MouseUp="SymbolIconSettings_Click" />'
Assert ($t.Contains($settingAnchor)) 'settings anchor'
$funcBtn = '<Border x:Name="StbBorderFunc" Width="24" Height="24" CornerRadius="4" Background="Transparent" ToolTip="插入函数" MouseDown="Border_MouseDown" MouseUp="SymbolIconFunc_MouseUp">' + "`r`n" +
'                                <TextBlock Text="f(x)" FontSize="12" FontWeight="Bold" Foreground="{DynamicResource FloatBarForeground}" HorizontalAlignment="Center" VerticalAlignment="Center" />' + "`r`n" +
'                            </Border>'
$t = $t.Replace($settingAnchor, $settingAnchor + "`r`n" + $funcBtn)

$pptAnchor = 'ValueChanged="SliderPptPenWidth_ValueChanged" Loaded="SliderPptPenWidth_Loaded" />'
Assert ($t.Contains($pptAnchor)) 'ppt anchor'
$pptBtn = '<Button Content="插入函数" Margin="2,8,0,0" HorizontalAlignment="Left" Padding="10,4" Background="#FF2A3630" Foreground="#FFE8EBEF" BorderThickness="0" Cursor="Hand" Click="StbBtnInsertFunc_Click" />'
$t = $t.Replace($pptAnchor, $pptAnchor + "`r`n" + '                    ' + $pptBtn)
WriteText $mw $t

Write-Host "== PHASE A4: startup visibility =="
$ip = Join-Path $app 'MainWindow.InputAndPanels.cs'
$it = ReadText $ip
Assert ($it.Contains('StbInitFloatBar();')) 'loaded anchor'
$it = $it.Replace('StbInitFloatBar();', "StbInitFloatBar();`r`n            StbApplyFuncButtonVisibility();", 1)
WriteText $ip $it

Write-Host "== PHASE A5: tool library =="
$reg = Join-Path $app 'StbBarRegistry.cs'
$rt = ReadText $reg
$rAnchor = '                Add(list, "stb.bar.clear", "清屏", "✕", "清空当前画布的所有笔迹。");'
Assert ($rt.Contains($rAnchor)) 'library anchor'
Assert (-not $rt.Contains('stb.bar.func')) 'library already has func'
$rt = $rt.Replace($rAnchor, '                Add(list, "stb.bar.func", "插入函数", "f(x)", "打开函数画板，画出图像后插入白板。");' + "`r`n" + $rAnchor)
WriteText $reg $rt

$fbb = Join-Path $app 'StbFloatBar.cs'
$ft = ReadText $fbb
Assert ($ft.Contains('                    case "stb.bar.clear":')) 'action anchor'
$ft = $ft.Replace('                    case "stb.bar.clear":', "                    case `"stb.bar.func`":`r`n                        StbOpenFunction();`r`n                        return;`r`n                    case `"stb.bar.clear`":")
WriteText $fbb $ft

Write-Host "== PHASE A6: settings cards =="
$sx = Join-Path $app 'StbSettingsWindow.xaml'
$st = ReadText $sx
$titleAnchor = '<TextBlock Text="高级" FontSize="22" FontWeight="SemiBold" Foreground="#22201C" Margin="0,4,0,12" />'
Assert ($st.Contains($titleAnchor)) 'advanced anchor'
$c1 = '<Border Style="{StaticResource StbCard}">' + "`r`n" +
'                                <Grid>' + "`r`n" +
'                                    <Grid.ColumnDefinitions><ColumnDefinition Width="*" /><ColumnDefinition Width="Auto" /></Grid.ColumnDefinitions>' + "`r`n" +
'                                    <StackPanel Grid.Column="0">' + "`r`n" +
'                                        <TextBlock Text="浮动栏笑脸菜单" Style="{StaticResource StbCardTitle}" />' + "`r`n" +
'                                        <TextBlock Text="右键浮动栏最左侧笑脸：组件编辑、设置、保存、另存到、打开、重启、退出" Style="{StaticResource StbCardDesc}" />' + "`r`n" +
'                                    </StackPanel>' + "`r`n" +
'                                    <ui:ToggleSwitch x:Name="SwSmileMenu" Grid.Column="1" OnContent="开" OffContent="关" VerticalAlignment="Center" Toggled="SwSmileMenu_Toggled" />' + "`r`n" +
'                                </Grid>' + "`r`n" +
'                            </Border>'
$c2 = '<Border Style="{StaticResource StbCard}">' + "`r`n" +
'                                <Grid>' + "`r`n" +
'                                    <Grid.ColumnDefinitions><ColumnDefinition Width="*" /><ColumnDefinition Width="Auto" /></Grid.ColumnDefinitions>' + "`r`n" +
'                                    <StackPanel Grid.Column="0">' + "`r`n" +
'                                        <TextBlock Text="浮动栏显示「插入函数」" Style="{StaticResource StbCardTitle}" />' + "`r`n" +
'                                        <TextBlock Text="常驻 f(x) 按钮，PPT 批注时也能打开函数画板" Style="{StaticResource StbCardDesc}" />' + "`r`n" +
'                                    </StackPanel>' + "`r`n" +
'                                    <ui:ToggleSwitch x:Name="SwFuncButton" Grid.Column="1" OnContent="开" OffContent="关" VerticalAlignment="Center" Toggled="SwFuncButton_Toggled" />' + "`r`n" +
'                                </Grid>' + "`r`n" +
'                            </Border>'
$c3 = '<Border Style="{StaticResource StbCard}">' + "`r`n" +
'                                <Grid>' + "`r`n" +
'                                    <Grid.ColumnDefinitions><ColumnDefinition Width="*" /><ColumnDefinition Width="Auto" /></Grid.ColumnDefinitions>' + "`r`n" +
'                                    <StackPanel Grid.Column="0">' + "`r`n" +
'                                        <TextBlock Text="函数画板" Style="{StaticResource StbCardTitle}" />' + "`r`n" +
'                                        <TextBlock Text="一次 / 二次 / 幂 / 指数 / 对数 / 三角，画完一键插入白板" Style="{StaticResource StbCardDesc}" />' + "`r`n" +
'                                    </StackPanel>' + "`r`n" +
'                                    <Button Grid.Column="1" Content="打开函数画板" Padding="14,6" VerticalAlignment="Center" Click="StbBtnOpenFunc_Click" />' + "`r`n" +
'                                </Grid>' + "`r`n" +
'                            </Border>'
$c4 = '<Border Style="{StaticResource StbCard}">' + "`r`n" +
'                                <Grid>' + "`r`n" +
'                                    <Grid.ColumnDefinitions><ColumnDefinition Width="*" /><ColumnDefinition Width="Auto" /></Grid.ColumnDefinitions>' + "`r`n" +
'                                    <StackPanel Grid.Column="0">' + "`r`n" +
'                                        <TextBlock Text="数据目录" Style="{StaticResource StbCardTitle}" />' + "`r`n" +
'                                        <TextBlock Text="%AppData%\SchoolToolsBoard：偏好、浮动栏、组件配置都在这" Style="{StaticResource StbCardDesc}" />' + "`r`n" +
'                                    </StackPanel>' + "`r`n" +
'                                    <Button Grid.Column="1" Content="打开目录" Padding="14,6" VerticalAlignment="Center" Click="StbBtnOpenDataDir_Click" />' + "`r`n" +
'                                </Grid>' + "`r`n" +
'                            </Border>'
$c5 = '<Border Style="{StaticResource StbCard}">' + "`r`n" +
'                                <Grid>' + "`r`n" +
'                                    <Grid.ColumnDefinitions><ColumnDefinition Width="*" /><ColumnDefinition Width="Auto" /></Grid.ColumnDefinitions>' + "`r`n" +
'                                    <StackPanel Grid.Column="0">' + "`r`n" +
'                                        <TextBlock Text="应用控制" Style="{StaticResource StbCardTitle}" />' + "`r`n" +
'                                        <TextBlock Text="改完配置可重启生效，或直接退出" Style="{StaticResource StbCardDesc}" />' + "`r`n" +
'                                    </StackPanel>' + "`r`n" +
'                                    <StackPanel Grid.Column="1" Orientation="Horizontal" VerticalAlignment="Center">' + "`r`n" +
'                                        <Button Content="重启" Padding="14,6" Click="StbBtnRestart_Click" />' + "`r`n" +
'                                        <Button Content="退出" Padding="14,6" Margin="8,0,0,0" Click="StbBtnQuit_Click" />' + "`r`n" +
'                                    </StackPanel>' + "`r`n" +
'                                </Grid>' + "`r`n" +
'                            </Border>'
$st = $st.Replace($titleAnchor, $titleAnchor + "`r`n" + $c1 + "`r`n" + $c2 + "`r`n" + $c3 + "`r`n" + $c4 + "`r`n" + $c5)
try { (New-Object System.Xml.XmlDocument).LoadXml($st) } catch { throw ("settings xaml malformed: " + $_.Exception.Message) }
WriteText $sx $st

Write-Host "== PHASE A7: init toggles =="
$sc = Join-Path $app 'StbSettingsWindow.xaml.cs'
$sct = ReadText $sc
Assert ($sct.Contains('LoadFromSettings();')) 'ctor anchor'
$sct = $sct.Replace('            LoadFromSettings();', "            LoadFromSettings();`r`n            StbInitMore();", 1)
WriteText $sc $sct

Write-Host "== PHASE A8: verify =="
$n = ReadText $mw
Assert ($n.Contains('MouseRightButtonUp="StbSmileMenu_Open"')) 'smile missing'
Assert ($n.Contains('x:Name="StbBorderFunc"')) 'func btn missing'
Assert ($n.Contains('StbBtnInsertFunc_Click')) 'ppt btn missing'
Assert ((ReadText $reg).Contains('stb.bar.func')) 'library missing'
Assert ((ReadText $fbb).Contains('StbOpenFunction();')) 'action missing'
Assert ((ReadText $sx).Contains('SwSmileMenu')) 'card missing'
Assert ((ReadText $prefs).Contains('SmileMenu')) 'prefs missing'
Write-Host 'OK  PHASE A verified'
Write-Host "STB FIX14 PATCHES DONE"

Write-Host "== PHASE B: 打开图标 =="
$fix15 = Join-Path $PSScriptRoot 'apply-stb-fix15.ps1'
Assert (Test-Path -LiteralPath $fix15) 'missing fix15'
& $fix15 -Root $Root -Version $Version
