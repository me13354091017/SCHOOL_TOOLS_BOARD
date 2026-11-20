# apply-stb-fix19.ps1
# 1.0.1.26：函数画板缩放/平移、数学排版预览、已插入函数图管理按钮
param([string]$Root = "plus-src", [string]$Version = "1.0.0.0")

$ErrorActionPreference = 'Stop'
trap { Write-Host ("::error::fix19: " + $_.Exception.Message); throw }

$app = Join-Path $Root 'Ink Canvas'
function Assert([bool]$c, [string]$m) { if (-not $c) { throw ("ANCHOR FAIL: " + $m) } }
function ReadText([string]$p) { return (Get-Content -LiteralPath $p -Raw) }
function WriteText([string]$p, [string]$t) { [IO.File]::WriteAllText($p, $t, (New-Object System.Text.UTF8Encoding($false))) }

$extra = Join-Path $PSScriptRoot 'extra'

# ---------- F1 copy new sources ----------
foreach ($name in @('StbFuncPlot.cs', 'StbMathText.cs', 'StbFunctionWindow.xaml')) {
    $src = Join-Path $extra $name
    Assert (Test-Path -LiteralPath $src) ("missing extra/" + $name)
    Copy-Item -LiteralPath $src -Destination (Join-Path $app $name) -Force
    Write-Host ("COPY " + $name)
}

# ---------- F2 window code: preload + preview ----------
$wc = Join-Path $app 'StbFunctionWindow.xaml.cs'
$wct = ReadText $wc
if (-not $wct.Contains('StbPreloadExpr')) {
    $old = 'AddRow("y=x");'
    Assert ($wct.Contains($old)) 'AddRow default missing'
    $new = "AddRow(string.IsNullOrEmpty(StbPreloadExpr) ? `"y=x`" : StbPreloadExpr);`r`n" +
           "            StbPreloadExpr = null;"
    $wct = $wct.Replace($old, $new)
    Write-Host 'PATCHED preload'
}
if (-not $wct.Contains('StbRefreshPreview();')) {
    $rIdx = $wct.IndexOf('private void RefreshAll()')
    Assert ($rIdx -gt 0) 'RefreshAll missing'
    $bIdx = $wct.IndexOf('{', $rIdx)
    Assert ($bIdx -gt 0) 'RefreshAll body missing'
    $wct = $wct.Substring(0, $bIdx + 1) + "`r`n            StbRefreshPreview();" + $wct.Substring($bIdx + 1)
    Write-Host 'PATCHED preview hook'
}
WriteText $wc $wct

# ---------- F3 inserted card toolbar ----------
$fn = Join-Path $app 'MainWindow.Function.cs'
$ft = ReadText $fn
if (-not $ft.Contains('StbOpenFunctionWith')) {
    $oIdx = $ft.IndexOf('internal void StbOpenFunction()')
    Assert ($oIdx -gt 0) 'StbOpenFunction missing'
    $helper = "        internal void StbOpenFunctionWith(string expr)`r`n" +
              "        {`r`n" +
              "            StbFunctionWindow.StbPreloadExpr = expr;`r`n" +
              "            StbOpenFunction();`r`n" +
              "        }`r`n`r`n"
    $ft = $ft.Substring(0, $oIdx) + $helper + $ft.Substring($oIdx)
    Write-Host 'PATCHED preload helper'
}

if (-not $ft.Contains('tbDel.Click')) {
    $anchor = 'layer.Children.Add(card);'
    Assert ($ft.Contains($anchor)) 'card add anchor missing'
    $toolbar = @'
                System.Windows.Controls.Grid cardRoot = new System.Windows.Controls.Grid();
                card.Child = null;
                cardRoot.Children.Add(body);
                Func<string, Button> mkBtn = (label) => new Button
                {
                    Content = label,
                    FontSize = 10,
                    Padding = new Thickness(6, 1, 6, 1),
                    Margin = new Thickness(3, 0, 0, 0),
                    Background = new SolidColorBrush(Color.FromArgb(235, 255, 255, 255)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(0xD8, 0xDD, 0xE5)),
                    BorderThickness = new Thickness(1),
                    Cursor = Cursors.Hand
                };
                Button tbEdit = mkBtn("编辑");
                Button tbTop = mkBtn("置顶");
                Button tbDel = mkBtn("删除");
                System.Windows.Controls.StackPanel tools = new System.Windows.Controls.StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Top
                };
                tools.Children.Add(tbEdit);
                tools.Children.Add(tbTop);
                tools.Children.Add(tbDel);
                cardRoot.Children.Add(tools);
                card.Child = cardRoot;

                tbDel.Click += (s2, e2) => { layer.Children.Remove(card); e2.Handled = true; };
                tbTop.Click += (s2, e2) => { System.Windows.Controls.Panel.SetZIndex(card, 2000); e2.Handled = true; };
                tbEdit.Click += (s2, e2) => { StbOpenFunctionWith(caption); e2.Handled = true; };

'@
    $toolbar = $toolbar.Replace("`r`n", "`r`n")
    $i = $ft.IndexOf($anchor)
    $lineStart = $ft.LastIndexOf("`n", $i) + 1
    $ft = $ft.Substring(0, $lineStart) + $toolbar + $ft.Substring($lineStart)
    Write-Host 'PATCHED card toolbar'
}
WriteText $fn $ft

# ---------- F4 verify ----------
Assert ((ReadText $wc).Contains('StbRefreshPreview();')) 'preview hook'
Assert ((ReadText $fn).Contains('tbDel.Click')) 'delete button'
Assert ((ReadText $fn).Contains('StbOpenFunctionWith')) 'preload helper'
Assert ((ReadText (Join-Path $app 'StbFuncPlot.cs')).Contains('OnMouseWheel')) 'zoom'
Assert ((ReadText (Join-Path $app 'StbMathText.cs')).Contains('StbMath')) 'math renderer'
Assert ((ReadText (Join-Path $app 'StbFunctionWindow.xaml')).Contains('StbZoomIn_Click')) 'zoom buttons'
Write-Host 'OK fix19 verified'

# 链到 fix20
$fix20 = Join-Path $PSScriptRoot 'apply-stb-fix20.ps1'
Assert (Test-Path -LiteralPath $fix20) 'missing fix20'
& $fix20 -Root $Root -Version $Version
