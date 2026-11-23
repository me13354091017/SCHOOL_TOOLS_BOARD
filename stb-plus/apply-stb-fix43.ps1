# apply-stb-fix43.ps1
# 1.0.1.61：保留 fix43 性能补丁；追加全局批注 + 浮动栏/窗口层级。
param([string]$Root = "plus-src", [string]$Version = "1.0.0.0")
$ErrorActionPreference = 'Stop'
try {
    $urlFile = Join-Path $PSScriptRoot 'fix44-assets.url'
    if (-not (Test-Path -LiteralPath $urlFile)) { throw 'missing fix44-assets.url' }
    $url = (Get-Content -LiteralPath $urlFile -Raw).Trim()
    if (-not $url) { throw 'empty fix44-assets.url' }
    $zip = Join-Path $env:RUNNER_TEMP 'stb-fix43-fix44-assets.zip'
    Invoke-WebRequest -UseBasicParsing -MaximumRedirection 5 -Uri $url -OutFile $zip
    $repoRoot = Split-Path -Parent $PSScriptRoot
    Expand-Archive -LiteralPath $zip -DestinationPath $repoRoot -Force
    $core = Join-Path $PSScriptRoot 'apply-stb-fix43-core.ps1'
    if (-not (Test-Path -LiteralPath $core)) { throw 'missing apply-stb-fix43-core.ps1 after extract' }
    & $core -Root $Root -Version $Version
    $fix44 = Join-Path $PSScriptRoot 'apply-stb-fix44-stage2.ps1'
    if (-not (Test-Path -LiteralPath $fix44)) { throw 'missing apply-stb-fix44-stage2.ps1 after extract' }
    & $fix44 -Root $Root -Version $Version
    $fix45 = Join-Path $PSScriptRoot 'apply-stb-fix45-stage3.ps1'
    if (-not (Test-Path -LiteralPath $fix45)) { throw 'missing apply-stb-fix45-stage3.ps1 after extract' }
    & $fix45 -Root $Root -Version $Version
    $fix46 = Join-Path $PSScriptRoot 'apply-stb-fix46-stage4.ps1'
    if (-not (Test-Path -LiteralPath $fix46)) { throw 'missing apply-stb-fix46-stage4.ps1 after extract' }
    & $fix46 -Root $Root -Version $Version
}
catch {
    Write-Host ("::error::fix43 wrapper: " + $_.Exception.Message)
    throw
}
