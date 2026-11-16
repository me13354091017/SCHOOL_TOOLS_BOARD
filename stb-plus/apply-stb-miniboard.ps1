# apply-stb-miniboard.ps1
# PHASE K：为 IC+ 底座新增「板中板」窗口，并把自动更新地址改指本仓库。
# 用法：pwsh ./stb-plus/apply-stb-miniboard.ps1 -Root plus-src
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

Write-Host "== PHASE K1: 拷入板中板窗口 =="
foreach ($f in @('MiniBoardWindow.xaml', 'MiniBoardWindow.xaml.cs', 'MainWindow.MiniBoard.cs')) {
    $src = Join-Path $extra $f
    Assert (Test-Path -LiteralPath $src) "missing source $f"
    Copy-Item -LiteralPath $src -Destination $app -Force
    Write-Host ("PATCHED  copy " + $f)
    $script:count++
}

Write-Host "== PHASE K2: 自动更新地址改指本仓库 =="
$panel = Join-Path $app 'MainWindow.InputAndPanels.cs'
$oldUrl = 'AutoUpdater.Start($"https://dl.inkcanvasplus.khyan.top/autoupdate.xml?version={Application.ResourceAssembly.GetName().Version}");'
$newUrl = 'AutoUpdater.Start("https://github.com/me13354091017/SCHOOL_TOOLS_BOARD/releases/latest/download/version.xml");'
Edit $panel $oldUrl $newUrl 'CheckForUpdate -> 本仓库 version.xml'

Write-Host "== PHASE K3: 清理残留的更新/官网域名 =="
$hostRepl = @(
    @('https://dl.inkcanvasplus.khyan.top/autoupdate.xml', 'https://github.com/me13354091017/SCHOOL_TOOLS_BOARD/releases/latest/download/version.xml'),
    @('dl.inkcanvasplus.khyan.top', 'me13354091017.github.io/school_tools'),
    @('inkcanvasplus.khyan.top', 'me13354091017.github.io/school_tools')
)
foreach ($f in (Get-ChildItem -LiteralPath $Root -Recurse -File -Include *.cs, *.xaml, *.md, *.txt)) {
    $t = Get-Content -LiteralPath $f.FullName -Raw
    $orig = $t
    foreach ($p in $hostRepl) { $t = $t.Replace($p[0], $p[1]) }
    if ($t -ne $orig) {
        [IO.File]::WriteAllText($f.FullName, $t, $utf8)
        $script:count++
        Write-Host ("PATCHED  residual link in " + $f.Name)
    }
}

Write-Host "== PHASE K4: 主界面加「板中板」入口 =="
$mw = Join-Path $app 'MainWindow.xaml'
$toolAnchor = '<Button x:Name="BtnCountdownTimer" Content="计时器" FontFamily="Microsoft YaHei UI"'
$toolButton = @'
                    <Button x:Name="BtnMiniBoard" Content="板中板" FontFamily="Microsoft YaHei UI"
                            Margin="0,0,0,0"
                            Click="BtnMiniBoard_Click" Foreground="{Binding ElementName=BtnExit, Path=Foreground}" Background="{Binding ElementName=BtnExit, Path=Background}" />
'@
Edit $mw $toolAnchor ($toolButton + $toolAnchor) '工具栏组 + 板中板'

$mainAnchor = '<Button x:Name="BtnScreenshot" Content="截屏" FontFamily="Microsoft YaHei UI"'
$mainButton = @'
        <Button x:Name="BtnMiniBoardMain" Content="板中板" FontFamily="Microsoft YaHei UI"
                Margin="0,10,0,0" Width="{Binding ElementName=StackPanelMain, Path=ActualWidth}"
                Click="BtnMiniBoard_Click" Foreground="{Binding ElementName=BtnExit, Path=Foreground}" Background="{Binding ElementName=BtnExit, Path=Background}" />
'@
Edit $mw $mainAnchor ($mainButton + $mainAnchor) '左侧主工具栏 + 板中板'

Write-Host "== PHASE K5: 校验板中板文件会被编译（SDK 风格工程） =="
Assert (Select-String -LiteralPath (Join-Path $app 'InkCanvasPlus.csproj') -Pattern 'Microsoft.NET.Sdk' -Quiet) 'csproj 非 SDK 风格，需手工加 Page/Compile 条目'
Write-Host 'OK  csproj 为 SDK 风格，xaml/cs 自动纳入编译'

Write-Host ("STB MINIBOARD PATCHES DONE ($count patches)")
