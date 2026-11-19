# apply-stb-fix14.ps1
# 1.0.1.20：
#   ① 浮动栏笑脸右键菜单（图标+名字→关于应用；组件编辑 / 设置 / 保存 / 另存到 / 打开 / 重启 / 退出）
#   ② 「插入函数」：函数画板窗 + 浮动栏 f(x) 按钮 + 工具库条目 + PPT 批注调色盘按钮
#   ③ 设置页高级新增：笑脸菜单、插入函数按钮、函数画板、数据目录、重启 / 退出
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
$utf8 = New-Object System.Text.UTF8Encoding($false)

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
    Write-Host ("PATCHED  copy " + $name)
}

Write-Host "== PHASE A2: StbPrefs new flags =="
$prefs = Join-Path $app 'StbPrefs.cs'
$pt = ReadText $prefs
Assert ($pt.Contains('private static bool ReadFlag(string key, bool fallback)')) 'prefs anchor missing'
$ins = @'
        /// <summary>浮动栏笑脸右键菜单；默认开。</summary>
        internal static bool SmileMenu
        {
            get { return ReadFlag("smileMenu", true); }
            set { WriteFlag("smileMenu", value); }
        }

        /// <summary>浮动栏常驻「插入函数」按钮；默认开。</summary>
        internal static bool FuncButton
        {
            get { return ReadFlag("funcButton", true); }
            set { WriteFlag("funcButton", value); }
        }

'@
$pt = $pt.Replace('        private static bool ReadFlag(string key, bool fallback)', $ins + '        private static bool ReadFlag(string key, bool fallback)')
WriteText $prefs $pt
Write-Host 'PATCHED  StbPrefs smileMenu / funcButton'

Write-Host "== PHASE A3: main window xaml =="
$mw = Join-Path $app 'MainWindow.xaml'
$t = ReadText $mw

$emojiAnchor = '<Border x:Name="BorderFloatingBarEmoji" FlowDirection="LeftToRight"'
Assert ($t.Contains($emojiAnchor)) 'emoji border anchor missing'
$t = $t.Replace($emojiAnchor, '<Border x:Name="BorderFloatingBarEmoji" MouseRightButtonUp="StbSmileMenu_Open" FlowDirection="LeftToRight"')

$settingAnchor = '<ui:SymbolIcon Symbol="Setting" Foreground="{DynamicResource FloatBarForeground}" MouseUp="SymbolIconSettings_Click" />'
Assert ($t.Contains($settingAnchor)) 'settings icon anchor missing'
$funcBtn = @'
                            <Border x:Name="StbBorderFunc" Width="24" Height="24" CornerRadius="4" Background="Transparent"
                                    ToolTip="插入函数" MouseDown="Border_MouseDown" MouseUp="SymbolIconFunc_MouseUp">
                                <TextBlock Text="f(x)" FontSize="12" FontWeight="Bold"
                                           Foreground="{DynamicResource FloatBarForeground}"
                                           HorizontalAlignment="Center" VerticalAlignment="Center" />
                            </Border>
'@
$t = $t.Replace($settingAnchor, $settingAnchor + "`r`n" + $funcBtn.Trim("`r", "`n"))

$pptAnchor = 'ValueChanged="SliderPptPenWidth_ValueChanged" Loaded="SliderPptPenWidth_Loaded" />'
Assert ($t.Contains($pptAnchor)) 'ppt slider anchor missing'
$pptBtn = @'
                    <Button Content="插入函数" Margin="2,8,0,0" HorizontalAlignment="Left" Padding="10,4"
                            Background="#FF2A3630" Foreground="#FFE8EBEF" BorderThickness="0"
                            Cursor="Hand" Click="StbBtnInsertFunc_Click" />
'@
$t = $t.Replace($pptAnchor, $pptAnchor + "`r`n" + $pptBtn.Trim("`r", "`n"))

WriteText $mw $t
Write-Host 'PATCHED  MainWindow.xaml smile rightclick / f(x) / ppt button'

Write-Host "== PHASE A4: Window_Loaded sync visibility =="
$ip = Join-Path $app 'MainWindow.InputAndPanels.cs'
$it = ReadText $ip
Assert ($it.Contains('StbInitFloatBar();')) 'StbInitFloatBar anchor missing'
$it = $it.Replace('StbInitFloatBar();', "StbInitFloatBar();`r`n            StbApplyFuncButtonVisibility();", 1)
WriteText $ip $it
Write-Host 'PATCHED  startup visibility'

Write-Host "== PHASE A5: tool library insert-function =="
$reg = Join-Path $app 'StbBarRegistry.cs'
$rt = ReadText $reg
$rAnchor = '                Add(list, "stb.bar.clear", "清屏", "✕", "清空当前画布的所有笔迹。");'
Assert ($rt.Contains($rAnchor)) 'library anchor missing'
Assert (-not $rt.Contains('stb.bar.func')) 'library already has insert-function'
$rt = $rt.Replace($rAnchor, '                Add(list, "stb.bar.func", "插入函数", "f(x)", "打开函数画板，画出图像后插入白板。");' + "`r`n" + $rAnchor)
WriteText $reg $rt

$fbb = Join-Path $app 'StbFloatBar.cs'
$ft = ReadText $fbb
$fAnchor = '                    case "stb.bar.clear":'
Assert ($ft.Contains($fAnchor)) 'floatbar action anchor missing'
$ft = $ft.Replace($fAnchor, '                    case "stb.bar.func":' + "`r`n" + '                        StbOpenFunction();' + "`r`n" + '                        return;' + "`r`n" + $fAnchor)
WriteText $fbb $ft
Write-Host 'PATCHED  library insert-function'

Write-Host "== PHASE A6: settings advanced cards =="
$sx = Join-Path $app 'StbSettingsWindow.xaml'
$st = ReadText $sx
$titleAnchor = '<TextBlock Text="高级" FontSize="22" FontWeight="SemiBold" Foreground="#22201C" Margin="0,4,0,12" />'
Assert ($st.Contains($titleAnchor)) 'advanced title anchor missing'
$cards = @'
                            <Border Style="{StaticResource StbCard}">
                                <Grid>
                                    <Grid.ColumnDefinitions><ColumnDefinition Width="*" /><ColumnDefinition Width="Auto" /></Grid.ColumnDefinitions>
                                    <StackPanel Grid.Column="0">
                                        <TextBlock Text="浮动栏笑脸菜单" Style="{StaticResource StbCardTitle}" />
                                        <TextBlock Text="右键浮动栏最左侧笑脸：组件编辑、设置、保存、另存到、打开、重启、退出" Style="{StaticResource StbCardDesc}" />
                                    </StackPanel>
                                    <ui:ToggleSwitch x:Name="SwSmileMenu" Grid.Column="1" OnContent="开" OffContent="关"
                                                     VerticalAlignment="Center" Toggled="SwSmileMenu_Toggled" />
                                </Grid>
                            </Border>
                            <Border Style="{StaticResource StbCard}">
                                <Grid>
                                    <Grid.ColumnDefinitions><ColumnDefinition Width="*" /><ColumnDefinition Width="Auto" /></Grid.ColumnDefinitions>
                                    <StackPanel Grid.Column="0">
                                        <TextBlock Text="浮动栏显示「插入函数」" Style="{StaticResource StbCardTitle}" />
                                        <TextBlock Text="常驻 f(x) 按钮，PPT 批注时也能打开函数画板" Style="{StaticResource StbCardDesc}" />
                                    </StackPanel>
                                    <ui:ToggleSwitch x:Name="SwFuncButton" Grid.Column="1" OnContent="开" OffContent="关"
                                                     VerticalAlignment="Center" Toggled="SwFuncButton_Toggled" />
                                </Grid>
                            </Border>
                            <Border Style="{StaticResource StbCard}">
                                <Grid>
                                    <Grid.ColumnDefinitions><ColumnDefinition Width="*" /><ColumnDefinition Width="Auto" /></Grid.ColumnDefinitions>
                                    <StackPanel Grid.Column="0">
                                        <TextBlock Text="函数画板" Style="{StaticResource StbCardTitle}" />
                                        <TextBlock Text="一次 / 二次 / 幂 / 指数 / 对数 / 三角，画完一键插入白板" Style="{StaticResource StbCardDesc}" />
                                    </StackPanel>
                                    <Button Grid.Column="1" Content="打开函数画板" Padding="14,6" VerticalAlignment="Center"
                                            Click="StbBtnOpenFunc_Click" />
                                </Grid>
                            </Border>
                            <Border Style="{StaticResource StbCard}">
                                <Grid>
                                    <Grid.ColumnDefinitions><ColumnDefinition Width="*" /><ColumnDefinition Width="Auto" /></Grid.ColumnDefinitions>
                                    <StackPanel Grid.Column="0">
                                        <TextBlock Text="数据目录" Style="{StaticResource StbCardTitle}" />
                                        <TextBlock Text="%AppData%\SchoolToolsBoard：偏好、浮动栏、组件配置都在这" Style="{StaticResource StbCardDesc}" />
                                    </StackPanel>
                                    <Button Grid.Column="1" Content="打开目录" Padding="14,6" VerticalAlignment="Center"
                                            Click="StbBtnOpenDataDir_Click" />
                                </Grid>
                            </Border>
                            <Border Style="{StaticResource StbCard}">
                                <Grid>
                                    <Grid.ColumnDefinitions><ColumnDefinition Width="*" /><ColumnDefinition Width="Auto" /></Grid.ColumnDefinitions>
                                    <StackPanel Grid.Column="0">
                                        <TextBlock Text="应用控制" Style="{StaticResource StbCardTitle}" />
                                        <TextBlock Text="改完配置可重启生效，或直接退出" Style="{StaticResource StbCardDesc}" />
                                    </StackPanel>
                                    <StackPanel Grid.Column="1" Orientation="Horizontal" VerticalAlignment="Center">
                                        <Button Content="重启" Padding="14,6" Click="StbBtnRestart_Click" />
                                        <Button Content="退出" Padding="14,6" Margin="8,0,0,0" Click="StbBtnQuit_Click" />
                                    </StackPanel>
                                </Grid>
                            </Border>
'@
$st = $st.Replace($titleAnchor, $titleAnchor + "`r`n" + $cards.Trim("`r", "`n"))
try {
    $xml = New-Object System.Xml.XmlDocument
    $xml.LoadXml($st)
} catch {
    throw ("settings xaml malformed: " + $_.Exception.Message)
}
WriteText $sx $st
Write-Host 'PATCHED  settings advanced cards'

Write-Host "== PHASE A7: init toggles in ctor =="
$sc = Join-Path $app 'StbSettingsWindow.xaml.cs'
$sct = ReadText $sc
Assert ($sct.Contains('LoadFromSettings();')) 'LoadFromSettings anchor missing'
$sct = $sct.Replace('            LoadFromSettings();', "            LoadFromSettings();`r`n            StbInitMore();", 1)
WriteText $sc $sct
Write-Host 'PATCHED  StbInitMore call'

Write-Host "== PHASE A8: verify =="
$n = ReadText $mw
Assert ($n.Contains('MouseRightButtonUp="StbSmileMenu_Open"')) 'smile rightclick missing'
Assert ($n.Contains('x:Name="StbBorderFunc"')) 'f(x) button missing'
Assert ($n.Contains('StbBtnInsertFunc_Click')) 'ppt button missing'
Assert ($n.Contains('ValueChanged="SliderPptPenWidth_ValueChanged"')) 'ppt slider damaged'
Assert ((ReadText $reg).Contains('stb.bar.func')) 'library item missing'
Assert ((ReadText $fbb).Contains('case "stb.bar.func":')) 'floatbar action missing'
Assert ((ReadText $sx).Contains('SwSmileMenu')) 'settings card missing'
Assert ((ReadText $prefs).Contains('internal static bool FuncButton')) 'prefs flag missing'
foreach ($name in $files) {
    Assert (Test-Path -LiteralPath (Join-Path $app $name)) ("not copied " + $name)
}
Write-Host 'OK  PHASE A verified'

Write-Host "STB FIX14 PATCHES DONE"
