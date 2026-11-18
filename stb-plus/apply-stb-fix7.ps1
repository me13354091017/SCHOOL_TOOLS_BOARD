# apply-stb-fix7.ps1
# 1.0.1.13：
#   ① 板中板：点橡皮大小条内部不再被“点画布就收起”逻辑误杀（这就是拖动调节条时面板突然消失的直接原因）
#   ② 新增橡皮自动回笔：黑板 / 白板 / PPT / 批注 与 板中板，擦掉笔迹后自动切回书写
param(
    [string]$Root = "plus-src",
    [string]$Version = "1.0.0.0"
)

$ErrorActionPreference = 'Stop'
trap {
    Write-Host ("::error::fix7: " + $_.Exception.Message)
    throw
}

$app = Join-Path $Root 'Ink Canvas'
$utf8 = New-Object System.Text.UTF8Encoding($false)

function Assert([bool]$cond, [string]$msg) {
    if (-not $cond) { throw "ANCHOR FAIL: $msg" }
}

function RePatch([string]$path, [string]$pat, [string]$rep, [string]$label) {
    Assert (Test-Path -LiteralPath $path) "$label :: file missing"
    $t = Get-Content -LiteralPath $path -Raw
    $n = ([regex]::Matches($t, $pat)).Count
    Assert ($n -ge 1) "$label :: pattern not found"
    [IO.File]::WriteAllText($path, [regex]::Replace($t, $pat, $rep), $utf8)
    Write-Host ("PATCHED  " + $label)
}

Write-Host "== PHASE T1: 拷贝自动回笔代码 =="
foreach ($name in @('MainWindow.Autoswitch.cs', 'MiniBoardWindow.AutoPen.cs')) {
    $src = Join-Path $PSScriptRoot ('extra/' + $name)
    Assert (Test-Path -LiteralPath $src) ("missing source " + $name)
    Copy-Item -LiteralPath $src -Destination $app -Force
    Write-Host ("PATCHED  copy " + $name)
}

Write-Host "== PHASE T2: 板中板橡皮条不再被点击误收 =="
$mbe = Join-Path $app 'MiniBoardWindow.Extras.cs'
$body = @'
        private void StbRootGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DependencyObject stbSource = e.OriginalSource as DependencyObject;
            bool inEraserPanel = InlineEraserPanel != null
                && (stbSource == InlineEraserPanel || InlineEraserPanel.IsAncestorOf(stbSource));
            if (!inEraserPanel)
            {
                CloseMiniPopups();
            }
            if (inEraserPanel)
            {
                return;
            }
'@
RePatch $mbe '        private void StbRootGrid_PreviewMouseLeftButtonDown\(object sender, MouseButtonEventArgs e\)\r?\n        \{\r?\n            CloseMiniPopups\(\);' $body '板中板 橡皮条内点击不收起'

Write-Host "== PHASE T3: 主窗口启动时挂接自动回笔 =="
$mwcs = Join-Path $app 'MainWindow.InputAndPanels.cs'
$hookMain = @'
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            StbInitAutoPen();
'@
RePatch $mwcs '        private void Window_Loaded\(object sender, RoutedEventArgs e\)\r?\n        \{' $hookMain '主窗口 Window_Loaded 挂接自动回笔'

Write-Host "== PHASE T4: 板中板构造时挂接自动回笔 =="
$mbcs = Join-Path $app 'MiniBoardWindow.xaml.cs'
$hookMini = @'
            UpdateWidthLabel();
            SetBoard(true);
            StbInitMiniAutoPen();
'@
RePatch $mbcs '            UpdateWidthLabel\(\);\r?\n            SetBoard\(true\);' $hookMini '板中板 构造时挂接自动回笔'

Write-Host "== PHASE T5: 校验 =="
$n1 = Get-Content -LiteralPath $mbe -Raw
Assert ($n1.Contains('bool inEraserPanel')) '板中板点击判定未写入'
$n2 = Get-Content -LiteralPath $mwcs -Raw
Assert ($n2.Contains('StbInitAutoPen();')) '主窗口未挂接'
$n3 = Get-Content -LiteralPath $mbcs -Raw
Assert ($n3.Contains('StbInitMiniAutoPen();')) '板中板未挂接'
Assert (Test-Path -LiteralPath (Join-Path $app 'MainWindow.Autoswitch.cs')) '自动回笔代码未拷入'
Assert (Test-Path -LiteralPath (Join-Path $app 'MiniBoardWindow.AutoPen.cs')) '板中板回笔代码未拷入'
Write-Host 'OK  PHASE T 校验通过'

Write-Host "STB FIX7 PATCHES DONE"
