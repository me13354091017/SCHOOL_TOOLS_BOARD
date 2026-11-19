# apply-stb-fix10.ps1
# 1.0.1.16：修两处版式问题
#   ① 设置页「橡皮用完自动回到书写」卡片原先插在 Panel0 之外，被拉伸成整页高；修回 Panel0 内部
#   ② 浮动栏「快捷设置」面板改为顶端对齐，避免背景向下拉出一大块空白
# 1.0.1.17：尾部串接 fix11（只保留编辑浮动栏 + 工具库保存到其它）
param(
    [string]$Root = "plus-src",
    [string]$Version = "1.0.0.0"
)

$ErrorActionPreference = 'Stop'
trap {
    Write-Host ("::error::fix10: " + $_.Exception.Message)
    throw
}

$app = Join-Path $Root 'Ink Canvas'
$utf8 = New-Object System.Text.UTF8Encoding($false)

function Assert([bool]$cond, [string]$msg) {
    if (-not $cond) { throw "ANCHOR FAIL: $msg" }
}

Write-Host "== PHASE W1: move auto-pen switch card into Panel0 =="
$sx = Join-Path $app 'StbSettingsWindow.xaml'
Assert (Test-Path -LiteralPath $sx) 'StbSettingsWindow.xaml missing'
$t = Get-Content -LiteralPath $sx -Raw

$marker = 'x:Name="SwAutoPenBack"'
$i = $t.IndexOf($marker)
Assert ($i -ge 0) 'SwAutoPenBack not found'
$start = $t.LastIndexOf('<Border Style="{StaticResource StbCard}">', $i)
Assert ($start -ge 0) 'card start Border not found'
$endTag = '</Border>'
$end = $t.IndexOf($endTag, $i)
Assert ($end -gt $i) 'card end Border not found'
$end = $end + $endTag.Length

$card = $t.Substring($start, $end - $start)
Write-Host ("captured card length = " + $card.Length)

$t = $t.Remove($start, $end - $start)

$anchor = '                        <!-- 1 外观主题 -->'
$ai = $t.IndexOf($anchor)
Assert ($ai -ge 0) 'theme comment anchor missing'
$close = $t.LastIndexOf('</StackPanel>', $ai)
Assert ($close -gt 0) 'Panel0 closing tag not found'

$t = $t.Insert($close, $card + "`r`n")

[IO.File]::WriteAllText($sx, $t, $utf8)
Write-Host 'PATCHED  auto-pen card moved into Panel0'

Write-Host "== PHASE W2: quick settings panel top aligned =="
$mw = Join-Path $app 'MainWindow.xaml'
$t2 = Get-Content -LiteralPath $mw -Raw
$old = '<Border Name="BorderTools" Width="250" Margin="37,-286,-190,-6" CornerRadius="18"'
Assert ($t2.Contains($old)) 'BorderTools anchor missing'
$t2 = $t2.Replace($old, '<Border Name="BorderTools" Width="250" VerticalAlignment="Top" Margin="37,-286,-190,-6" CornerRadius="18"')
[IO.File]::WriteAllText($mw, $t2, $utf8)
Write-Host 'PATCHED  BorderTools top aligned'

Write-Host "== PHASE W3: verify =="
$n = Get-Content -LiteralPath $sx -Raw
Assert ($n.Contains('x:Name="SwAutoPenBack"')) 'switch lost'
$idxPanel = $n.IndexOf('x:Name="Panel0"')
$idxSw = $n.IndexOf('x:Name="SwAutoPenBack"')
$idxComment = $n.IndexOf('<!-- 1 外观主题 -->')
Assert ($idxPanel -ge 0 -and $idxSw -gt $idxPanel) 'switch not after Panel0'
Assert ($idxSw -lt $idxComment) 'switch still outside Panel0'
$nm = Get-Content -LiteralPath $mw -Raw
Assert ($nm.Contains('<Border Name="BorderTools" Width="250" VerticalAlignment="Top"')) 'top align missing'
Write-Host 'OK  PHASE W verified'

Write-Host "STB FIX10 PATCHES DONE"

Write-Host "== PHASE X: 只保留编辑浮动栏 =="
$fix11 = Join-Path $PSScriptRoot 'apply-stb-fix11.ps1'
Assert (Test-Path -LiteralPath $fix11) 'missing stb-plus/apply-stb-fix11.ps1'
& $fix11 -Root $Root -Version $Version
