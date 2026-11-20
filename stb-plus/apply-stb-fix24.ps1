# apply-stb-fix24.ps1
# 1.0.1.31：修复二维输入框两个实际缺陷——
# 1) 插入分数/根号/上下标后只重绘不重新测量，控件边界停留在旧宽度，边框只包住前半截（截图里灰框只套住 x），
#    根号与数字画到框外，右侧绿色键盘图标被挤过来压在上面；
# 2) 序列化把相邻的 x 与 sqrt(9) 直接拼成 xsqrt(9)，引擎当成未知函数名 → 红框「抱歉…」；
#    现在相邻值之间自动补乘号，输出 x*sqrt(9)。
param([string]$Root = "plus-src", [string]$Version = "1.0.0.0")

$ErrorActionPreference = 'Stop'
trap { Write-Host ("::error::fix24: " + $_.Exception.Message); throw }

$app = Join-Path $Root 'Ink Canvas'
function Assert([bool]$c, [string]$m) { if (-not $c) { throw ("ANCHOR FAIL: " + $m) } }
function ReadText([string]$p) { return (Get-Content -LiteralPath $p -Raw) }

$extra = Join-Path $PSScriptRoot 'extra'

foreach ($f in @('StbMathBox.cs', 'StbMathBox.Text.cs')) {
    $srcF = Join-Path $extra $f
    Assert (Test-Path -LiteralPath $srcF) ('missing extra/' + $f)
    Copy-Item -LiteralPath $srcF -Destination (Join-Path $app $f) -Force
    Write-Host ('COPY ' + $f)
}

$box = ReadText (Join-Path $app 'StbMathBox.cs')
$txt = ReadText (Join-Path $app 'StbMathBox.Text.cs')
Assert ($box.Contains('InvalidateMeasure();')) 'box measure'
Assert ($txt.Contains('AppendPiece')) 'serialize product'
Assert ($txt.Contains("sb.Append('*');")) 'serialize star'
Assert ($txt.Contains('sqrt(')) 'sqrt serialize'
Write-Host 'OK fix24 verified'

$fix25 = Join-Path $PSScriptRoot 'apply-stb-fix25.ps1'
Assert (Test-Path -LiteralPath $fix25) 'missing fix25'
& $fix25 -Root $Root -Version $Version

$fix26 = Join-Path $PSScriptRoot 'apply-stb-fix26.ps1'
Assert (Test-Path -LiteralPath $fix26) 'missing fix26'
& $fix26 -Root $Root -Version $Version
