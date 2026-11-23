# apply-stb-fix43.ps1
# 1.0.1.59：保留 1.0.1.58 全部性能补丁，并在末尾追加函数页批注。
# 为减少大文件推送，原 fix43 实现改名为 fix43-core 放入构建资产包。
param([string]$Root = "plus-src", [string]$Version = "1.0.0.0")
$ErrorActionPreference = 'Stop'
try {
    $zip = Join-Path $env:RUNNER_TEMP 'stb-fix43-fix44-assets.zip'
    Invoke-WebRequest -UseBasicParsing -MaximumRedirection 5 -Uri 'https://aka.doubaocdn.com/s/ateVLqwhLj' -OutFile $zip
    $repoRoot = Split-Path -Parent $PSScriptRoot
    Expand-Archive -LiteralPath $zip -DestinationPath $repoRoot -Force
    $core = Join-Path $PSScriptRoot 'apply-stb-fix43-core.ps1'
    if (-not (Test-Path -LiteralPath $core)) { throw 'missing apply-stb-fix43-core.ps1 after extract' }
    & $core -Root $Root -Version $Version
    $fix44 = Join-Path $PSScriptRoot 'apply-stb-fix44-stage2.ps1'
    if (-not (Test-Path -LiteralPath $fix44)) { throw 'missing apply-stb-fix44-stage2.ps1 after extract' }
    & $fix44 -Root $Root -Version $Version
}
catch {
    Write-Host ("::error::fix43 wrapper: " + $_.Exception.Message)
    throw
}
