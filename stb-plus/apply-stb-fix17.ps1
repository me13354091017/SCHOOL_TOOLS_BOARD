# apply-stb-fix17.ps1
# 1.0.1.25：浮动栏运行图标 + 函数画板快捷键盘浮层 + 表达式归一化
param([string]$Root = "plus-src", [string]$Version = "1.0.0.0")

$ErrorActionPreference = 'Stop'
trap { Write-Host ("::error::fix17: " + $_.Exception.Message); throw }

$app = Join-Path $Root 'Ink Canvas'
function Assert([bool]$c, [string]$m) { if (-not $c) { throw ("ANCHOR FAIL: " + $m) } }
function ReadText([string]$p) { return (Get-Content -LiteralPath $p -Raw) }
function WriteText([string]$p, [string]$t) { [IO.File]::WriteAllText($p, $t, (New-Object System.Text.UTF8Encoding($false))) }

$extra = Join-Path $PSScriptRoot 'extra'

# ---------- D1 prefs ----------
$prefs = Join-Path $app 'StbPrefs.cs'
$pt = ReadText $prefs
Assert ($pt.Contains('internal static bool FuncButton')) 'FuncButton missing'
Assert (-not $pt.Contains('bool RunIcon')) 'RunIcon exists'
$add = "        /// <summary>float-bar run icon.</summary>`r`n" +
       "        internal static bool RunIcon`r`n" +
       "        {`r`n" +
       "            get { return ReadFlag(`"runIcon`", true); }`r`n" +
       "            set { WriteFlag(`"runIcon`", value); }`r`n" +
       "        }`r`n`r`n" +
       "        /// <summary>function keypad.</summary>`r`n" +
       "        internal static bool Keypad`r`n" +
       "        {`r`n" +
       "            get { return ReadFlag(`"keypad`", true); }`r`n" +
       "            set { WriteFlag(`"keypad`", value); }`r`n" +
       "        }`r`n`r`n"
$anc = 'internal static bool FuncButton'
$idx = $pt.IndexOf($anc)
Assert ($idx -gt 0) 'FuncButton index'
$lineStart = $pt.LastIndexOf("`n", $idx) + 1
$blockStart = $lineStart
$pt = $pt.Substring(0, $blockStart) + $add + $pt.Substring($blockStart)
WriteText $prefs $pt
Write-Host 'PATCHED prefs RunIcon/Keypad'

# ---------- D2 float bar run icon ----------
$mw = Join-Path $app 'MainWindow.xaml'
$t = ReadText $mw
$settingIdx = $t.IndexOf('ToolTip="设置">')
Assert ($settingIdx -gt 0) 'settings tooltip missing'
$i = $t.LastIndexOf('<StackPanel', $settingIdx)
Assert ($i -gt 0) 'settings stackpanel missing'
Assert (-not $t.Contains('StbBorderRun')) 'run icon exists'
$run = '<Border x:Name="StbBorderRun" Width="24" Height="24" CornerRadius="4" Background="Transparent" ToolTip="运行函数画板" MouseDown="Border_MouseDown" MouseUp="StbRunIcon_MouseUp" MouseRightButtonUp="StbSmileMenu_Open">' + "`r`n" +
       '                                <Viewbox Width="16" Height="16" HorizontalAlignment="Center" VerticalAlignment="Center">' + "`r`n" +
       '                                    <ui:SymbolIcon Symbol="Play" Foreground="{DynamicResource FloatBarForeground}" />' + "`r`n" +
       '                                </Viewbox>' + "`r`n" +
       '                            </Border>' + "`r`n" +
       '                            '
$t = $t.Substring(0, $i) + $run + $t.Substring($i)
WriteText $mw $t
Write-Host 'PATCHED run icon'

# ---------- D3 run handler ----------
$fn = Join-Path $app 'MainWindow.Function.cs'
$ft = ReadText $fn
Assert (-not $ft.Contains('StbRunIcon_MouseUp')) 'handler exists'
$mIdx = $ft.IndexOf('internal void StbApplyFuncButtonVisibility()')
Assert ($mIdx -gt 0) 'visibility method missing'
$head = "        /// <summary>float bar run icon.</summary>`r`n" +
        "        private void StbRunIcon_MouseUp(object sender, MouseButtonEventArgs e)`r`n" +
        "        {`r`n" +
        "            if (!StbPrefs.RunIcon) { return; }`r`n" +
        "            try { BorderTools.Visibility = Visibility.Collapsed; GridNotifications.Visibility = Visibility.Collapsed; }`r`n" +
        "            catch { }`r`n" +
        "            StbOpenFunction();`r`n" +
        "        }`r`n`r`n"
$ft = $ft.Substring(0, $mIdx) + $head + $ft.Substring($mIdx)
$tail = "            try`r`n" +
        "            {`r`n" +
        "                if (StbBorderRun != null)`r`n" +
        "                {`r`n" +
        "                    StbBorderRun.Visibility = StbPrefs.RunIcon ? Visibility.Visible : Visibility.Collapsed;`r`n" +
        "                }`r`n" +
        "            }`r`n" +
        "            catch`r`n" +
        "            {`r`n" +
        "            }`r`n"
$endIdx = $ft.IndexOf("`n        }`r`n", $mIdx + $head.Length)
Assert ($endIdx -gt 0) 'body end missing'
$ft = $ft.Substring(0, $endIdx + 1) + $tail + $ft.Substring($endIdx + 1)
WriteText $fn $ft
Write-Host 'PATCHED run handler'

# ---------- D4 window surgery ----------
$wx = Join-Path $app 'StbFunctionWindow.xaml'
$wxt = ReadText $wx
$b = $wxt.IndexOf('<Border x:Name="PanelKeypad"')
if ($b -gt 0) {
    $e = $wxt.IndexOf('</Border>', $b)
    $nl = $wxt.IndexOf("`r`n", $e)
    if ($nl -lt 0) { $nl = $wxt.IndexOf("`n", $e) }
    $wxt = $wxt.Substring(0, $b) + $wxt.Substring($nl + 1)
    WriteText $wx $wxt
    Write-Host 'PATCHED keypad xaml removed'
}

$wc = Join-Path $app 'StbFunctionWindow.xaml.cs'
$wct = ReadText $wc
if ($wct.Contains('private void BuildKeypad()')) {
    $s1 = $wct.LastIndexOf("`n", $wct.IndexOf('private void BuildKeypad()')) + 1
    $s2raw = $wct.IndexOf('private void BtnInsert_Click(')
    Assert ($s2raw -gt $s1) 'BtnInsert anchor missing'
    $s2 = $wct.LastIndexOf("`n", $s2raw) + 1
    $wct = $wct.Substring(0, $s1) + $wct.Substring($s2)
    Write-Host 'PATCHED old keypad removed'
}
if ($wct.Contains('Popup _keypadPopup')) {
    $wct = [regex]::Replace($wct, '(?m)^\s*private Popup _keypadPopup;\s*\r?\n', '')
    Write-Host 'PATCHED dup field removed'
}
if (-not $wct.Contains('StbNorm(text)')) {
    $wct = $wct.Replace('StbFuncEngine.Compile(text)', 'StbFuncEngine.Compile(StbNorm(text))')
}
WriteText $wc $wct

$keypad = Join-Path $extra 'StbKeypad.cs'
Assert (Test-Path -LiteralPath $keypad) 'missing extra/StbKeypad.cs'
Copy-Item -LiteralPath $keypad -Destination (Join-Path $app 'StbKeypad.cs') -Force
Write-Host 'COPY StbKeypad.cs'

# ---------- D5 verify ----------
Assert ((ReadText $mw).Contains('StbBorderRun')) 'run icon xaml'
Assert ((ReadText $fn).Contains('StbRunIcon_MouseUp')) 'run handler'
Assert ((ReadText $prefs).Contains('bool RunIcon')) 'prefs'
Assert ((ReadText (Join-Path $app 'StbKeypad.cs')).Contains('StbNorm')) 'keypad copy'
Assert ((ReadText $wc).Contains('StbNorm(text)')) 'norm call'
Write-Host 'OK fix17 verified'

$fix18 = Join-Path $PSScriptRoot 'apply-stb-fix18.ps1'
Assert (Test-Path -LiteralPath $fix18) 'missing fix18'
& $fix18 -Root $Root -Version $Version
