# apply-stb-fix26.ps1
# 1.0.1.34：日志里 0x8000000B 崩溃修复 —— 无放映窗时点「上一页/下一页」按钮，
# SlideShowWindows[1] 会抛 COMException（WPS / 轻量批注 / COM 断开时必然触发）；
# 现在两个方法先确认 COM 放映窗存在，否则直接返回。
param([string]$Root = "plus-src", [string]$Version = "1.0.0.0")

$ErrorActionPreference = 'Stop'
trap { Write-Host ("::error::fix26: " + $_.Exception.Message); throw }

$app = Join-Path $Root 'Ink Canvas'
function Assert([bool]$c, [string]$m) { if (-not $c) { throw ("ANCHOR FAIL: " + $m) } }
function ReadText([string]$p) { return (Get-Content -LiteralPath $p -Raw) }
function WriteText([string]$p, [string]$t) { [IO.File]::WriteAllText($p, $t, (New-Object System.Text.UTF8Encoding($false))) }
function ReplaceOnce([string]$t, [string]$old, [string]$new, [string]$tag) {
    $i = $t.IndexOf($old)
    Assert ($i -ge 0) ($tag + ' missing')
    Assert ($t.IndexOf($old, $i + 1) -lt 0) ($tag + ' not unique')
    return $t.Substring(0, $i) + $new + $t.Substring($i + $old.Length)
}

$ps = Join-Path $app 'MainWindow.PresentationAndSettings.cs'
$t = ReadText $ps

# ---------- S1 上一页 ----------
$old = @'
        private void BtnPPTSlidesUp_Click(object sender, RoutedEventArgs e)
        {
            if (currentMode == 1)
            {
                GridBackgroundCover.Visibility = Visibility.Collapsed;
                currentMode = 0;
            }

            _isPptClickingBtnTurned = true;
            if (inkCanvas.Strokes.Count > Settings.Automation.MinimumAutomationStrokeNumber &&
'@
$new = @'
        private void BtnPPTSlidesUp_Click(object sender, RoutedEventArgs e)
        {
            if (currentMode == 1)
            {
                GridBackgroundCover.Visibility = Visibility.Collapsed;
                currentMode = 0;
            }

            _isPptClickingBtnTurned = true;
            // 1.0.1.34：无 COM 放映窗（WPS / 轻量批注 / COM 断开）时不要再取 SlideShowWindows[1]，
            // 否则抛 0x8000000B 未处理异常（日志里成片出现）
            if (pptApplication == null || StbPptComShowCount() < 1)
            {
                return;
            }
            if (inkCanvas.Strokes.Count > Settings.Automation.MinimumAutomationStrokeNumber &&
'@
$t = ReplaceOnce $t $old $new 'slides up guard'

# ---------- S2 下一页 ----------
$old = @'
        private void BtnPPTSlidesDown_Click(object sender, RoutedEventArgs e)
        {
            if (currentMode == 1)
            {
                GridBackgroundCover.Visibility = Visibility.Collapsed;
                currentMode = 0;
            }
            _isPptClickingBtnTurned = true;
            if (inkCanvas.Strokes.Count > Settings.Automation.MinimumAutomationStrokeNumber &&
'@
$new = @'
        private void BtnPPTSlidesDown_Click(object sender, RoutedEventArgs e)
        {
            if (currentMode == 1)
            {
                GridBackgroundCover.Visibility = Visibility.Collapsed;
                currentMode = 0;
            }
            _isPptClickingBtnTurned = true;
            // 1.0.1.34：同上一页，先确认放映窗存在
            if (pptApplication == null || StbPptComShowCount() < 1)
            {
                return;
            }
            if (inkCanvas.Strokes.Count > Settings.Automation.MinimumAutomationStrokeNumber &&
'@
$t = ReplaceOnce $t $old $new 'slides down guard'
WriteText $ps $t
Write-Host 'PATCHED slides guards'

# ---------- 验证 ----------
$w = ReadText $ps
Assert ($w.Contains('StbPptComShowCount() < 1')) 'guards present'
Assert ($w.Contains('1.0.1.34')) 'guard comment'
$fx = ReadText (Join-Path $app 'StbFunctionWindow.xaml')
Assert ($fx.Contains('M8,0 V16 M0,8 H16')) 'zoom in vector'
Assert ($fx.Contains('M10,4 A5,5')) 'zoom reset vector'
Assert (-not $fx.Contains('Content="＋"')) 'old plus glyph gone'
Assert (-not $fx.Contains('Content="⟲"')) 'old reset glyph gone'
Write-Host 'OK fix26 verified'
