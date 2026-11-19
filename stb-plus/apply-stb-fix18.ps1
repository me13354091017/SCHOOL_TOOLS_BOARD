# apply-stb-fix18.ps1
# 1.0.1.25：设置页高级新增 运行图标 / 快捷键盘 / 恢复默认 三张卡片
param([string]$Root = "plus-src", [string]$Version = "1.0.0.0")

$ErrorActionPreference = 'Stop'
trap { Write-Host ("::error::fix18: " + $_.Exception.Message); throw }

$app = Join-Path $Root 'Ink Canvas'
function Assert([bool]$c, [string]$m) { if (-not $c) { throw ("ANCHOR FAIL: " + $m) } }
function ReadText([string]$p) { return (Get-Content -LiteralPath $p -Raw) }
function WriteText([string]$p, [string]$t) { [IO.File]::WriteAllText($p, $t, (New-Object System.Text.UTF8Encoding($false))) }

# ---------- E1 handlers ----------
$more = Join-Path $app 'StbSettingsWindow.More.cs'
$mt = ReadText $more
Assert (-not $mt.Contains('SwRunIcon_Toggled')) 'handlers exist'

$handlers = "        private void SwRunIcon_Toggled(object sender, RoutedEventArgs e)`r`n" +
            "        {`r`n" +
            "            if (_loading || SwRunIcon == null) { return; }`r`n" +
            "            StbPrefs.RunIcon = SwRunIcon.IsOn;`r`n" +
            "            MainWindow mw = Owner as MainWindow;`r`n" +
            "            if (mw == null && Application.Current != null) { mw = Application.Current.MainWindow as MainWindow; }`r`n" +
            "            if (mw != null) { mw.StbApplyFuncButtonVisibility(); }`r`n" +
            "        }`r`n`r`n" +
            "        private void SwKeypad_Toggled(object sender, RoutedEventArgs e)`r`n" +
            "        {`r`n" +
            "            if (_loading || SwKeypad == null) { return; }`r`n" +
            "            StbPrefs.Keypad = SwKeypad.IsOn;`r`n" +
            "        }`r`n`r`n" +
            "        private void StbBtnResetPrefs_Click(object sender, RoutedEventArgs e)`r`n" +
            "        {`r`n" +
            "            MessageBoxResult r = MessageBox.Show(`"恢复默认开关？`", `"SchoolToolsBoard`", MessageBoxButton.OKCancel, MessageBoxImage.Question);`r`n" +
            "            if (r != MessageBoxResult.OK) { return; }`r`n" +
            "            StbPrefs.SmileMenu = true;`r`n" +
            "            StbPrefs.FuncButton = true;`r`n" +
            "            StbPrefs.RunIcon = true;`r`n" +
            "            StbPrefs.Keypad = true;`r`n" +
            "            StbInitMore();`r`n" +
            "            MessageBox.Show(`"已恢复默认，重启后生效。`", `"SchoolToolsBoard`", MessageBoxButton.OK, MessageBoxImage.Information);`r`n" +
            "        }`r`n`r`n"

$hook = 'private void StbBtnOpenFunc_Click'
$hookIdx = $mt.IndexOf($hook)
if ($hookIdx -lt 0) { $hookIdx = $mt.IndexOf('internal void StbInitMore()') }
Assert ($hookIdx -gt 0) 'handlers anchor missing'
$lineStart = $mt.LastIndexOf("`n", $hookIdx) + 1
$mt = $mt.Substring(0, $lineStart) + $handlers + $mt.Substring($lineStart)
Assert ($mt.Contains('StbPrefs.RunIcon = true')) 'reset not inserted'

$sync = "            if (SwRunIcon != null) { SwRunIcon.IsOn = StbPrefs.RunIcon; }`r`n" +
        "            if (SwKeypad != null) { SwKeypad.IsOn = StbPrefs.Keypad; }`r`n"
$syncAnchor = 'SwFuncButton.IsOn = StbPrefs.FuncButton;'
$sIdx = $mt.IndexOf($syncAnchor)
Assert ($sIdx -gt 0) 'StbInitMore anchor missing'
$sEnd = $mt.IndexOf("`n", $sIdx) + 1
$mt = $mt.Substring(0, $sEnd) + $sync + $mt.Substring($sEnd)
WriteText $more $mt
Write-Host 'PATCHED More.cs handlers'

# ---------- E2 cards ----------
$sx = Join-Path $app 'StbSettingsWindow.xaml'
$st = ReadText $sx
$titleAnchor = '<TextBlock Text="高级"'
$ti = $st.IndexOf($titleAnchor)
Assert ($ti -gt 0) 'advanced title missing'
Assert (-not $st.Contains('SwRunIcon')) 'cards exist'
$marginIdx = $st.IndexOf('Margin="0,4,0,12"', $ti)
$titleLineEnd = $st.IndexOf("`n", $st.IndexOf('>', $marginIdx)) + 1

function Card([string]$title, [string]$desc, [string]$body) {
    return "<Border Style=`"{StaticResource StbCard}`">`r`n" +
           "                                <Grid>`r`n" +
           "                                    <Grid.ColumnDefinitions><ColumnDefinition Width=`"*`" /><ColumnDefinition Width=`"Auto`" /></Grid.ColumnDefinitions>`r`n" +
           "                                    <StackPanel Grid.Column=`"0`">`r`n" +
           "                                        <TextBlock Text=`"$title`" Style=`"{StaticResource StbCardTitle}`" />`r`n" +
           "                                        <TextBlock Text=`"$desc`" Style=`"{StaticResource StbCardDesc}`" />`r`n" +
           "                                    </StackPanel>`r`n" +
           $body +
           "                                </Grid>`r`n" +
           "                            </Border>`r`n"
}

$bodyRun = "                                    <ui:ToggleSwitch x:Name=`"SwRunIcon`" Grid.Column=`"1`" OnContent=`"开`" OffContent=`"关`" VerticalAlignment=`"Center`" Toggled=`"SwRunIcon_Toggled`" />`r`n"
$bodyKey = "                                    <ui:ToggleSwitch x:Name=`"SwKeypad`" Grid.Column=`"1`" OnContent=`"开`" OffContent=`"关`" VerticalAlignment=`"Center`" Toggled=`"SwKeypad_Toggled`" />`r`n"
$bodyReset = "                                    <Button Grid.Column=`"1`" Content=`"恢复默认`" Padding=`"14,6`" VerticalAlignment=`"Center`" Click=`"StbBtnResetPrefs_Click`" />`r`n"

$cards = (Card '浮动栏运行图标' '左键打开函数画板，右键弹出应用菜单：组件编辑 / 设置 / 保存 / 打开 / 重启 / 退出' $bodyRun) +
         (Card '函数快捷键盘' '数字、sin cos tan、e pi log2 ln、平方立方、根号、绝对值、括号、退格、清空' $bodyKey) +
         (Card '恢复默认设置' '笑脸菜单、插入函数、运行图标、快捷键盘、自动回笔全部恢复默认' $bodyReset)

$st = $st.Substring(0, $titleLineEnd) + $cards + $st.Substring($titleLineEnd)
try { (New-Object System.Xml.XmlDocument).LoadXml($st) } catch { throw ("settings xaml malformed: " + $_.Exception.Message) }
WriteText $sx $st
Write-Host 'PATCHED settings cards'

# ---------- E3 verify ----------
Assert ((ReadText $more).Contains('SwKeypad_Toggled')) 'handlers missing'
Assert ((ReadText $sx).Contains('StbBtnResetPrefs_Click')) 'reset card missing'
Write-Host 'OK fix18 verified'

# 链到 fix19
$fix19 = Join-Path $PSScriptRoot 'apply-stb-fix19.ps1'
Assert (Test-Path -LiteralPath $fix19) 'missing fix19'
& $fix19 -Root $Root -Version $Version
