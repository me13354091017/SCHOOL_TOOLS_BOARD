# build-launcher.ps1
# 编译安装版根目录启动器（{app}\SchoolToolsBoard.exe）。
# 必须在 Build icon 之后调用，这样启动器才带上 STB 自己的图标。
param([string]$Root = "plus-src")

$ErrorActionPreference = 'Stop'
trap { Write-Host ("::error::build-launcher: " + $_.Exception.Message); throw }

$app = Join-Path $Root 'Ink Canvas'
$src = Join-Path $PSScriptRoot 'extra\StbLauncher.cs'
if (-not (Test-Path -LiteralPath $src)) { throw 'missing stb-plus/extra/StbLauncher.cs' }

$outDir = Join-Path $Root 'packaging\launcher'
if (-not (Test-Path -LiteralPath $outDir)) { New-Item -ItemType Directory -Path $outDir -Force | Out-Null }

$fw = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$csc = Join-Path $fw 'csc.exe'
if (-not (Test-Path -LiteralPath $csc)) { throw 'csc.exe not found' }

$icon = Join-Path $app 'Resources\InkCanvas.ico'
$outExe = Join-Path $outDir 'SchoolToolsBoard.exe'
$r1 = Join-Path $fw 'System.dll'
$r2 = Join-Path $fw 'System.Windows.Forms.dll'

if (Test-Path -LiteralPath $icon) {
    & $csc /nologo /target:winexe /optimize+ "/win32icon:$icon" "/out:$outExe" "/r:$r1" "/r:$r2" $src 2>&1 | ForEach-Object { Write-Host $_ }
} else {
    Write-Host '::warning::launcher icon missing, build without icon'
    & $csc /nologo /target:winexe /optimize+ "/out:$outExe" "/r:$r1" "/r:$r2" $src 2>&1 | ForEach-Object { Write-Host $_ }
}

if ($LASTEXITCODE -ne 0) { throw ("csc failed " + $LASTEXITCODE) }
if (-not (Test-Path -LiteralPath $outExe)) { throw 'launcher exe missing' }
Write-Host ("BUILT launcher " + (Get-Item $outExe).Length + " bytes")
