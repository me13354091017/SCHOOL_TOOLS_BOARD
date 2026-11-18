# apply-stb-polish.ps1
# 1.0.1.4：发行者改为「周之尧」；PPT 轮询换成带缓存的轻量进程检查（提速 + 修句柄泄漏）；
# 板中板最大化按钮改用矢量图标，避免个别字体缺字显示成空框。
param(
    [string]$Root = "plus-src",
    [string]$Version = "1.0.0.0"
)

$ErrorActionPreference = 'Stop'
$app = Join-Path $Root 'Ink Canvas'
$utf8 = New-Object System.Text.UTF8Encoding($false)

function Assert([bool]$cond, [string]$msg) {
    if (-not $cond) { throw "ANCHOR FAIL: $msg" }
}

function Edit([string]$path, [string]$old, [string]$new, [string]$label) {
    Assert (Test-Path -LiteralPath $path) "$label :: file missing $path"
    $t = Get-Content -LiteralPath $path -Raw
    Assert ($t.Contains($old)) "$label :: anchor not found"
    $t = $t.Replace($old, $new)
    [IO.File]::WriteAllText($path, $t, $utf8)
    Write-Host ("PATCHED  " + $label)
}

Write-Host "== PHASE P1: 发行者 / 版权 =="
$asm = Join-Path $app 'Properties/AssemblyInfo.cs'
Edit $asm '[assembly: AssemblyCompany("SCHOOL_TOOLS")]' '[assembly: AssemblyCompany("周之尧")]' '发行者 AssemblyCompany'
Edit $asm 'Copyright © Clover Yan 2025; © XY Wang 2023' 'Copyright © 周之尧 2026；上游 Ink Canvas Plus © Clover Yan 2025 / XY Wang 2023（GPL-3.0）' '版权说明'

$iss = Join-Path $Root 'packaging/setup.iss'
Edit $iss '#define MyAppPublisher "SCHOOL_TOOLS"' '#define MyAppPublisher "周之尧"' 'iss 发行者'
Edit $iss 'DefaultDirName={autopf}\{#MyAppPublisher}\SchoolToolsBoard' 'DefaultDirName={autopf}\SCHOOL_TOOLS\SchoolToolsBoard' 'iss 安装目录保持原路径'

Write-Host "== PHASE P2: 性能（PPT 轮询只做轻量、带缓存的进程检查） =="
$perfSrc = Join-Path $PSScriptRoot 'extra/MainWindow.Perf.cs'
Assert (Test-Path -LiteralPath $perfSrc) 'missing source MainWindow.Perf.cs'
Copy-Item -LiteralPath $perfSrc -Destination $app -Force
Write-Host 'PATCHED  copy MainWindow.Perf.cs'

$mps = Join-Path $app 'MainWindow.PresentationAndSettings.cs'
$oldGuard = 'if (Process.GetProcessesByName("POWERPNT").Length == 0 && Process.GetProcessesByName("wpp").Length == 0) return;'
Edit $mps $oldGuard 'if (!StbOfficeProcessRunning()) return;' 'PPT 轮询改用带缓存的进程检查'

Write-Host "== PHASE P3: 板中板最大化图标 =="
$mb = Join-Path $app 'MiniBoardWindow.xaml'
Edit $mb '<TextBlock Text="⛶" FontSize="11" Foreground="#FFE8EBEF" />' '<Viewbox Width="12" Height="12" HorizontalAlignment="Center"><ui:SymbolIcon Symbol="FullScreen" Foreground="#FF7FD1A0" /></Viewbox>' '板中板 最大化图标改为矢量'

Write-Host "== PHASE P4: 断言 =="
Assert (Select-String -LiteralPath $asm -Pattern 'AssemblyCompany\("周之尧"\)' -Quiet) '发行者未写入'
Assert (Select-String -LiteralPath $iss -Pattern 'MyAppPublisher "周之尧"' -Quiet) 'iss 发行者未写入'
Assert (Select-String -LiteralPath $mps -Pattern 'StbOfficeProcessRunning' -Quiet) 'PPT 轻量检查未生效'
Assert (Test-Path -LiteralPath (Join-Path $app 'MainWindow.Perf.cs')) '性能代码未拷入'
Assert (Select-String -LiteralPath $mb -Pattern 'BtnMBMax_Click' -Quiet) '板中板最大化按钮丢失'
Write-Host 'OK  PHASE P 校验通过'

Write-Host "STB POLISH PATCHES DONE"
