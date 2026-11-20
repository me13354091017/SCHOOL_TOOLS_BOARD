# apply-stb-fix21.ps1
# 1.0.1.28：插入函数全面对照希沃白板5实物 —— 同款快捷键盘（米色数字区+白色函数区、红退格绿回车、左右光标键）、
# 表达式行（表达式 n 标题 + 行内绿色键盘图标）、希沃错误文案、增加表达式 n/6、插入按钮禁用态、
# 类别小图标与幂函数默认 x^-1、白板函数卡片工具条补「克隆」。样式一律照希沃，不自创。
param([string]$Root = "plus-src", [string]$Version = "1.0.0.0")

$ErrorActionPreference = 'Stop'
trap { Write-Host ("::error::fix21: " + $_.Exception.Message); throw }

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

# ---------- K1 希沃同款键盘 / 表达式行 / 窗口 xaml ----------
foreach ($f in @('StbKeypad.cs', 'StbFunctionWindow.Rows.cs', 'StbFunctionWindow.xaml')) {
    $srcF = Join-Path $extra $f
    Assert (Test-Path -LiteralPath $srcF) ('missing extra/' + $f)
    Copy-Item -LiteralPath $srcF -Destination (Join-Path $app $f) -Force
    Write-Host ('COPY ' + $f)
}

# ---------- K2 主代码：移除旧行/刷新实现（改由 Rows partial 提供），类别点击去 y= 前缀 ----------
$wc = Join-Path $app 'StbFunctionWindow.xaml.cs'
$t = ReadText $wc
$s1 = $t.IndexOf('private void AddRow(string expr)')
$s2 = $t.IndexOf('private void BtnInsert_Click(')
Assert ($s1 -gt 0 -and $s2 -gt $s1) 'rows region anchors'
$ls1 = $t.LastIndexOf("`n", $s1) + 1
$ls2 = $t.LastIndexOf("`n", $s2) + 1
$t = $t.Substring(0, $ls1) + $t.Substring($ls2)
Write-Host 'PATCHED old rows region removed'
$t = ReplaceOnce $t '_rows[_rows.Count - 1].Box.Text = expr;' @'
string catExpr = expr.StartsWith("y=") ? expr.Substring(2) : expr;
                _rows[_rows.Count - 1].Box.Text = catExpr;
'@ 'cat assign'
WriteText $wc $t
Write-Host 'PATCHED category assign'

# ---------- K3 白板卡片工具条补「克隆」（希沃：编辑/置顶/克隆/删除） ----------
$fn = Join-Path $app 'MainWindow.Function.cs'
$t = ReadText $fn
Assert ($t.Contains('tbDel.Click')) 'fix19 toolbar missing'
$t = ReplaceOnce $t 'Button tbDel = mkBtn("删除");' @'
Button tbClone = mkBtn("克隆");
                Button tbDel = mkBtn("删除");
'@ 'clone btn'
$t = ReplaceOnce $t 'tools.Children.Add(tbDel);' @'
tools.Children.Add(tbClone);
                tools.Children.Add(tbDel);
'@ 'clone add'
$t = ReplaceOnce $t 'tbDel.Click += (s2, e2) => { layer.Children.Remove(card); e2.Handled = true; };' @'
tbDel.Click += (s2, e2) => { layer.Children.Remove(card); e2.Handled = true; };
                tbClone.Click += (s2, e2) =>
                {
                    StbInsertFunctionImage(bmp, caption);
                    FrameworkElement last = layer.Children[layer.Children.Count - 1] as FrameworkElement;
                    if (last != null)
                    {
                        System.Windows.Controls.Canvas.SetLeft(last, System.Windows.Controls.Canvas.GetLeft(last) + 28);
                        System.Windows.Controls.Canvas.SetTop(last, System.Windows.Controls.Canvas.GetTop(last) + 28);
                    }
                    e2.Handled = true;
                };
'@ 'clone handler'
WriteText $fn $t
Write-Host 'PATCHED clone toolbar'

# ---------- K4 验证 ----------
Assert (-not (ReadText $wc).Contains('private void AddRow')) 'old rows still'
Assert ((ReadText $wc).Contains('catExpr')) 'cat assign'
Assert ((ReadText $wc).Contains('StbPreloadExpr')) 'preload kept'
Assert ((ReadText (Join-Path $app 'StbFunctionWindow.Rows.cs')).Contains('抱歉，你的表达式有些难懂。')) 'seewo error text'
Assert ((ReadText (Join-Path $app 'StbKeypad.cs')).Contains('@T:nroot')) 'seewo keypad'
Assert ((ReadText (Join-Path $app 'StbKeypad.cs')).Contains('StbOpenKeypadFor')) 'row keypad anchor'
Assert ((ReadText (Join-Path $app 'StbFunctionWindow.xaml')).Contains('y=x^-1')) 'power default'
Assert ((ReadText (Join-Path $app 'StbFunctionWindow.xaml')).Contains('左侧快捷选择或直接输入')) 'seewo hint'
Assert ((ReadText $fn).Contains('tbClone.Click')) 'clone handler'
Write-Host 'OK fix21 verified'

$fix22 = Join-Path $PSScriptRoot 'apply-stb-fix22.ps1'
Assert (Test-Path -LiteralPath $fix22) 'missing fix22'
& $fix22 -Root $Root -Version $Version
