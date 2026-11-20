# apply-stb-fix22.ps1
# 1.0.1.29：输入框升级为希沃 MathEditor 式二维公式输入（自有实现 StbMathBox）：
# 真分数竖排、根号横线、上下标、□占位框、←→光标在框间跳、退格删框；键盘模板键改 @T 指令。
param([string]$Root = "plus-src", [string]$Version = "1.0.0.0")

$ErrorActionPreference = 'Stop'
trap { Write-Host ("::error::fix22: " + $_.Exception.Message); throw }

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

# ---------- M1 拷贝二维输入框 / 新键盘 / 新行 ----------
foreach ($f in @('StbMathBox.cs', 'StbMathBox.Text.cs', 'StbMathBox.Render.cs', 'StbKeypad.cs', 'StbFunctionWindow.Rows.cs')) {
    $srcF = Join-Path $extra $f
    Assert (Test-Path -LiteralPath $srcF) ('missing extra/' + $f)
    Copy-Item -LiteralPath $srcF -Destination (Join-Path $app $f) -Force
    Write-Host ('COPY ' + $f)
}

# ---------- M2 主代码类型手术：TextBox → StbMathBox ----------
$wc = Join-Path $app 'StbFunctionWindow.xaml.cs'
$t = ReadText $wc
$t = ReplaceOnce $t 'private TextBox _activeBox;' 'private StbMathBox _activeBox;' 'activeBox type'
$t = ReplaceOnce $t 'internal TextBox Box;' 'internal StbMathBox Box;' 'row box type'
WriteText $wc $t
Write-Host 'PATCHED box types'

# ---------- M3 验证 ----------
Assert ((ReadText $wc).Contains('private StbMathBox _activeBox;')) 'activeBox'
Assert ((ReadText $wc).Contains('internal StbMathBox Box;')) 'row box'
Assert ((ReadText (Join-Path $app 'StbMathBox.cs')).Contains('InsertTemplate')) 'mathbox'
Assert ((ReadText (Join-Path $app 'StbMathBox.Text.cs')).Contains('FixParents')) 'mathbox parents'
Assert ((ReadText (Join-Path $app 'StbKeypad.cs')).Contains('@T:nroot')) 'keypad templates'
Assert ((ReadText (Join-Path $app 'StbFunctionWindow.Rows.cs')).Contains('StbMathBox box')) 'rows mathbox'
Write-Host 'OK fix22 verified'
