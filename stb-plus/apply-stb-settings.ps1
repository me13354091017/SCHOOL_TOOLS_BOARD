# apply-stb-settings.ps1
# PHASE L：把 IC+ 的设置界面换成 SCHOOL_TOOLS_PDF 那种「左侧分类导航 + 右侧卡片」整页设置，
# 并把「设置」按钮改为打开新窗口（旧面板保留但不再显示）。
# 用法：pwsh ./stb-plus/apply-stb-settings.ps1 -Root plus-src
param(
    [string]$Root = "plus-src",
    [string]$Version = "1.0.0.0"
)

$ErrorActionPreference = 'Stop'
$app = Join-Path $Root 'Ink Canvas'
$utf8 = New-Object System.Text.UTF8Encoding($false)
$count = 0

function Assert([bool]$cond, [string]$msg) {
    if (-not $cond) { throw "ANCHOR FAIL: $msg" }
}

function Edit([string]$path, [string]$old, [string]$new, [string]$label) {
    Assert (Test-Path -LiteralPath $path) "$label :: file missing $path"
    $t = Get-Content -LiteralPath $path -Raw
    Assert ($t.Contains($old)) "$label :: anchor not found"
    $t = $t.Replace($old, $new)
    [IO.File]::WriteAllText($path, $t, $utf8)
    $script:count++
    Write-Host ("PATCHED  " + $label)
}

$extra = Join-Path $PSScriptRoot 'extra'

Write-Host "== PHASE L1: 拷入 STB 设置页 =="
foreach ($f in @('StbSettingsWindow.xaml', 'StbSettingsWindow.xaml.cs')) {
    $src = Join-Path $extra $f
    Assert (Test-Path -LiteralPath $src) "missing source $f"
    Copy-Item -LiteralPath $src -Destination $app -Force
    Write-Host ("PATCHED  copy " + $f)
    $script:count++
}

Write-Host "== PHASE L2: 放宽三个方法的可见性 =="
$feat = Join-Path $app 'MainWindow.Features.cs'
Edit $feat 'private void SetTheme(string theme, int favor)' 'public void SetTheme(string theme, int favor)' 'SetTheme -> public'
Edit $feat 'private void SystemEvents_UserPreferenceChanged(object sender, Microsoft.Win32.UserPreferenceChangedEventArgs e)' 'public void SystemEvents_UserPreferenceChanged(object sender, Microsoft.Win32.UserPreferenceChangedEventArgs e)' 'SystemEvents_UserPreferenceChanged -> public'
$panel = Join-Path $app 'MainWindow.InputAndPanels.cs'
Edit $panel 'private void CheckForUpdate()' 'public void CheckForUpdate()' 'CheckForUpdate -> public'

Write-Host "== PHASE L3: 新增 ApplyStbTheme()（按当前 Settings.Appearance.Theme 重新应用主题） =="
$themeHelper = @'

        /// <summary>STB 设置页调用：按当前 Theme 索引重新应用主题。</summary>
        public void ApplyStbTheme()
        {
            SystemEvents_UserPreferenceChanged(null, new Microsoft.Win32.UserPreferenceChangedEventArgs(Microsoft.Win32.UserPreferenceCategory.General));
        }
'@
Edit $panel '        private void BtnSettings_Click(object sender, RoutedEventArgs e)' ($themeHelper.TrimEnd("`r", "`n") + "`r`n`r`n        private void BtnSettings_Click(object sender, RoutedEventArgs e)") 'insert ApplyStbTheme'

Write-Host "== PHASE L4: 设置按钮改为打开 STB 设置窗口 =="
$oldOpen = '        private async void BtnSettings_Click(object sender, RoutedEventArgs e)'
$newOpen = @'
        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            StbSettingsWindow.ShowFor(this);
        }

        private async void BtnSettings_Click_Legacy(object sender, RoutedEventArgs e)
'@
Edit $panel $oldOpen $newOpen.TrimEnd("`r", "`n") 'BtnSettings_Click -> StbSettingsWindow'

Write-Host "== PHASE L5: 旧设置面板默认不再可弹（入口统一走新窗口） =="
Edit $panel 'if (BorderSettings.Tag as Visibility? == Visibility.Visible)' 'if (false && BorderSettings.Tag as Visibility? == Visibility.Visible)' 'legacy panel gate'

Write-Host ("STB SETTINGS PATCHES DONE ($count patches)")
