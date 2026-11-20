# apply-stb-fix20.ps1
# 1.0.1.27：板中板默认橡皮 60；函数引擎 v2 与自适应采样兜底拷贝
param([string]$Root = "plus-src", [string]$Version = "1.0.0.0")

$ErrorActionPreference = 'Stop'
trap { Write-Host ("::error::fix20: " + $_.Exception.Message); throw }

$app = Join-Path $Root 'Ink Canvas'
function Assert([bool]$c, [string]$m) { if (-not $c) { throw ("ANCHOR FAIL: " + $m) } }
function ReadText([string]$p) { return (Get-Content -LiteralPath $p -Raw) }
function WriteText([string]$p, [string]$t) { [IO.File]::WriteAllText($p, $t, (New-Object System.Text.UTF8Encoding($false))) }

$extra = Join-Path $PSScriptRoot 'extra'

# ---------- G1 mini-board default eraser 60 ----------
$pens = Join-Path $app 'MiniBoardWindow.Pens.cs'
$pt = ReadText $pens
Assert ($pt.Contains('_eraserSize = 14')) 'eraser default anchor missing'
$pt = $pt.Replace('_eraserSize = 14', '_eraserSize = 60')
WriteText $pens $pt
Write-Host 'PATCHED eraser default 60'

$mx = Join-Path $app 'MiniBoardWindow.xaml'
$mt = ReadText $mx
$sIdx = $mt.IndexOf('x:Name="SliderMBEraser"')
Assert ($sIdx -gt 0) 'SliderMBEraser missing'
$lineEnd = $mt.IndexOf('>', $sIdx)
$seg = $mt.Substring($sIdx, $lineEnd - $sIdx)
Assert ($seg.Contains('Value="14"')) 'slider default 14 missing'
$mt = $mt.Substring(0, $sIdx) + $seg.Replace('Value="14"', 'Value="60"') + $mt.Substring($lineEnd)
$mt = $mt.Replace('直径 14 像素', '直径 60 像素')
try { (New-Object System.Xml.XmlDocument).LoadXml($mt) } catch { throw ("miniboard xaml malformed: " + $_.Exception.Message) }
WriteText $mx $mt
Write-Host 'PATCHED slider default 60'

# ---------- G2 engine v2 + adaptive plot ----------
foreach ($name in @('StbFuncEngine.cs', 'StbFuncParser.Funcs.cs', 'StbFuncPlot.cs')) {
    $src = Join-Path $extra $name
    Assert (Test-Path -LiteralPath $src) ("missing extra/" + $name)
    Copy-Item -LiteralPath $src -Destination (Join-Path $app $name) -Force
    Write-Host ("COPY " + $name)
}

# ---------- G3 verify ----------
Assert ((ReadText $pens).Contains('_eraserSize = 60')) 'eraser 60'
Assert ((ReadText $mx).Contains('Value="60"')) 'slider 60'
Assert ((ReadText (Join-Path $app 'StbFuncEngine.cs')).Contains('ParseArgList')) 'engine v2'
Assert ((ReadText (Join-Path $app 'StbFuncEngine.cs')).Contains('IsUnaryFunc')) 'engine v2 funcs'
Assert ((ReadText (Join-Path $app 'StbFuncPlot.cs')).Contains('private void Refine')) 'adaptive sampling'
Assert ((ReadText (Join-Path $app 'StbFuncPlot.cs')).Contains('OnMouseWheel')) 'zoom kept'
Write-Host 'OK fix20 verified'
