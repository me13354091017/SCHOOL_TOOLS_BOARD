# apply-stb-fix15.ps1
# 1.0.1.21：「打开」图标找回
#   ① 快捷设置「墨迹」行新增「打开」（保存 / 保存到其它 / 打开 / 回放）
#   ② 浮动栏工具库新增「打开」条目
param(
    [string]$Root = "plus-src",
    [string]$Version = "1.0.0.0"
)

$ErrorActionPreference = 'Stop'
trap {
    Write-Host ("::error::fix15: " + $_.Exception.Message)
    throw
}

$app = Join-Path $Root 'Ink Canvas'
$utf8 = New-Object System.Text.UTF8Encoding($false)

function Assert([bool]$cond, [string]$msg) {
    if (-not $cond) { throw "ANCHOR FAIL: $msg" }
}

$mw = Join-Path $app 'MainWindow.xaml'
$t = Get-Content -LiteralPath $mw -Raw

Write-Host "== PHASE B1: quick settings open icon =="
$anchor = @'
                                                <StackPanel>
                                                    <Grid Width="20" Height="20" HorizontalAlignment="Left"
'@
$anchor = $anchor.Trim("`r", "`n")
Assert ($t.Contains($anchor)) 'replay block anchor missing'

$openBlock = @'
<StackPanel Margin="0,0,18,0">
                                                    <Border Width="20" Height="20" HorizontalAlignment="Left" Background="Transparent"
                                                            MouseDown="Border_MouseDown" MouseUp="StbSymbolIconOpen_Click" ToolTip="打开墨迹文件">
                                                        <TextBlock Text="▱" FontSize="16" HorizontalAlignment="Center" VerticalAlignment="Center"
                                                                   Foreground="{DynamicResource FloatBarForeground}" />
                                                    </Border>
                                                    <TextBlock Text="打开" FontSize="9" Margin="0,2,0,0"
                                                               Foreground="{DynamicResource FloatBarForeground}" Opacity="0.85" />
                                                </StackPanel>
                                                <StackPanel>
                                                    <Grid Width="20" Height="20" HorizontalAlignment="Left"
'@
$openBlock = $openBlock.Trim("`r", "`n")
$t = $t.Replace($anchor, $openBlock)
[IO.File]::WriteAllText($mw, $t, $utf8)
Write-Host 'PATCHED  quick settings open icon'

Write-Host "== PHASE B2: library open =="
$reg = Join-Path $app 'StbBarRegistry.cs'
$rt = Get-Content -LiteralPath $reg -Raw
$rAnchor = '                Add(list, "stb.bar.clear", "清屏", "✕", "清空当前画布的所有笔迹。");'
Assert ($rt.Contains($rAnchor)) 'library anchor missing'
Assert (-not $rt.Contains('stb.bar.open')) 'library already has open'
$rt = $rt.Replace($rAnchor, '                Add(list, "stb.bar.open", "打开", "▱", "打开之前保存的墨迹文件。");' + "`r`n" + $rAnchor)
[IO.File]::WriteAllText($reg, $rt, $utf8)

$fbb = Join-Path $app 'StbFloatBar.cs'
$ft = Get-Content -LiteralPath $fbb -Raw
$fAnchor = '                    case "stb.bar.clear":'
Assert ($ft.Contains($fAnchor)) 'floatbar anchor missing'
$ft = $ft.Replace($fAnchor, '                    case "stb.bar.open":' + "`r`n" + '                        StbOpenStrokesNow();' + "`r`n" + '                        return;' + "`r`n" + $fAnchor)
[IO.File]::WriteAllText($fbb, $ft, $utf8)
Write-Host 'PATCHED  library open'

Write-Host "== PHASE B3: open handler =="
$sm = Join-Path $app 'MainWindow.SmileMenu.cs'
$st = Get-Content -LiteralPath $sm -Raw
$hAnchor = '        private void StbOpenStrokesNow()'
Assert ($st.Contains($hAnchor)) 'open method anchor missing'
$handler = @'
        private void StbSymbolIconOpen_Click(object sender, MouseButtonEventArgs e)
        {
            if (lastBorderMouseDownObject != sender)
            {
                return;
            }
            try
            {
                BorderTools.Visibility = Visibility.Collapsed;
                GridNotifications.Visibility = Visibility.Collapsed;
            }
            catch
            {
            }
            StbOpenStrokesNow();
        }

'@
$st = $st.Replace($hAnchor, $handler + $hAnchor)
[IO.File]::WriteAllText($sm, $st, $utf8)
Write-Host 'PATCHED  open handler'

Write-Host "== PHASE B4: verify =="
$n = Get-Content -LiteralPath $mw -Raw
Assert ($n.Contains('StbSymbolIconOpen_Click')) 'open icon missing'
Assert ($n.Contains('Text="打开"')) 'open caption missing'
Assert ((Get-Content -LiteralPath $reg -Raw).Contains('stb.bar.open')) 'library open missing'
Assert ((Get-Content -LiteralPath $fbb -Raw).Contains('case "stb.bar.open":')) 'floatbar open missing'
Assert ((Get-Content -LiteralPath $sm -Raw).Contains('private void StbSymbolIconOpen_Click')) 'handler missing'
Write-Host 'OK  PHASE B verified'

Write-Host "STB FIX15 PATCHES DONE"
