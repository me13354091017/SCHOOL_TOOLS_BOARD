# apply-stb-fix23.ps1
# 1.0.1.30：PPT 识别增强 —— 常驻 Win32 放映窗探测（screenClass / WPS 全屏），
# PowerPoint 重启/崩溃/先关文稿再退出后可重新识别；COM 不可用时轻量批注兜底；
# 放映期间保持批注窗置顶不抢焦点；结束批注按钮在无 COM 时也能正常退出。
param([string]$Root = "plus-src", [string]$Version = "1.0.0.0")

$ErrorActionPreference = 'Stop'
trap { Write-Host ("::error::fix23: " + $_.Exception.Message); throw }

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

$extra = Join-Path $PSScriptRoot 'extra'

# ---------- P1 常驻探测 partial ----------
$srcF = Join-Path $extra 'MainWindow.PptWatch.cs'
Assert (Test-Path -LiteralPath $srcF) 'missing extra/MainWindow.PptWatch.cs'
Copy-Item -LiteralPath $srcF -Destination (Join-Path $app 'MainWindow.PptWatch.cs') -Force
Write-Host 'COPY MainWindow.PptWatch.cs'

# ---------- P2 启动常驻探测（间隔已被组件补丁改为 300，按正则匹配任意值） ----------
$wc = Join-Path $app 'MainWindow.xaml.cs'
$t = ReadText $wc
$m = [regex]::Match($t, 'timerCheckPPT\.Interval\s*=\s*\d+;')
Assert ($m.Success) 'init interval anchor missing'
Assert ($t.IndexOf('StbInitPptWatch') -lt 0) 'init watch already patched'
$ins = $m.Value + "`r`n            StbInitPptWatch();"
$t = $t.Substring(0, $m.Index) + $ins + $t.Substring($m.Index + $m.Length)
WriteText $wc $t
Write-Host 'PATCHED init watch'

# ---------- P3 结束批注：无 COM / 轻量模式安全退出 ----------
$ps = Join-Path $app 'MainWindow.PresentationAndSettings.cs'
$t = ReadText $ps
$old = @'
        private void BtnPPTSlideShowEnd_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
'@
$new = @'
        private void BtnPPTSlideShowEnd_Click(object sender, RoutedEventArgs e)
        {
            if (_stbPptLight || StbPptComShowCount() < 1)
            {
                _stbPptLight = false;
                StbPptExitLight();
                return;
            }
            Application.Current.Dispatcher.Invoke(() =>
'@
$t = ReplaceOnce $t $old $new 'ppt end guard'
WriteText $ps $t
Write-Host 'PATCHED ppt end guard'

# ---------- P4 验证 ----------
$watch = ReadText (Join-Path $app 'MainWindow.PptWatch.cs')
Assert ($watch.Contains('screenclass')) 'watch screenClass'
Assert ($watch.Contains('StbPptKeepTop')) 'watch topmost'
Assert ($watch.Contains('StbPptEnterLight')) 'watch light'
Assert ((ReadText $wc).Contains('StbInitPptWatch();')) 'watch init'
Assert ((ReadText $ps).Contains('_stbPptLight || StbPptComShowCount() < 1')) 'end guard'
Write-Host 'OK fix23 verified'

$fix24 = Join-Path $PSScriptRoot 'apply-stb-fix24.ps1'
Assert (Test-Path -LiteralPath $fix24) 'missing fix24'
& $fix24 -Root $Root -Version $Version