# apply-stb-fix12.ps1
# 1.0.1.18：快捷设置面板修正 + 「保存到其它」可辨识
# 1.0.1.19：尾部串接 fix13
param(
    [string]$Root = "plus-src",
    [string]$Version = "1.0.0.0"
)

$ErrorActionPreference = 'Stop'
trap {
    Write-Host ("::error::fix12: " + $_.Exception.Message)
    throw
}

$app = Join-Path $Root 'Ink Canvas'
$utf8 = New-Object System.Text.UTF8Encoding($false)
$mw = Join-Path $app 'MainWindow.xaml'

function Assert([bool]$cond, [string]$msg) {
    if (-not $cond) { throw "ANCHOR FAIL: $msg" }
}

$t = Get-Content -LiteralPath $mw -Raw

Write-Host "== PHASE Y1: widen quick settings panel =="
$oldBorder = '<Border Name="BorderTools" Width="250" VerticalAlignment="Top" Margin="37,-286,-190,-6" CornerRadius="18"'
Assert ($t.Contains($oldBorder)) 'BorderTools anchor missing'
$t = $t.Replace($oldBorder, '<Border Name="BorderTools" Width="292" VerticalAlignment="Top" Margin="37,-286,-232,-6" CornerRadius="18"')

$oldSlider = '<Slider Grid.Column="0" Minimum="1" Maximum="20" Width="168"'
Assert ($t.Contains($oldSlider)) 'ink width slider anchor missing'
$t = $t.Replace($oldSlider, '<Slider Grid.Column="0" Minimum="1" Maximum="20" Width="186"')

Write-Host "== PHASE Y2: ink row with captions =="
$startMark = '<ui:SymbolIcon Symbol="Save" Width="20" Height="20"'
$si = $t.IndexOf($startMark)
Assert ($si -ge 0) 'save icon not found'

$replayMark = 'MouseUp="GridInkReplayButton_MouseUp">'
$ri = $t.IndexOf($replayMark, $si)
Assert ($ri -ge 0) 'replay button not found'
$ge = $t.IndexOf('</Grid>', $ri)
Assert ($ge -gt $ri) 'replay grid end not found'
$end = $ge + '</Grid>'.Length

$newRow = @'
<StackPanel Margin="0,0,18,0">
                                                    <ui:SymbolIcon Symbol="Save" Width="20" Height="20" HorizontalAlignment="Left"
                                                                   MouseDown="Border_MouseDown" MouseUp="SymbolIconSaveStrokes_MouseUp" />
                                                    <TextBlock Text="保存" FontSize="9" Margin="0,2,0,0"
                                                               Foreground="{DynamicResource FloatBarForeground}" Opacity="0.85" />
                                                </StackPanel>
                                                <StackPanel Margin="0,0,18,0">
                                                    <ui:SymbolIcon Symbol="Copy" Width="20" Height="20" HorizontalAlignment="Left"
                                                                   ToolTip="保存到其它位置"
                                                                   MouseDown="Border_MouseDown" MouseUp="StbSymbolIconSaveAs_MouseUp" />
                                                    <TextBlock Text="保存到其它" FontSize="9" Margin="0,2,0,0"
                                                               Foreground="{DynamicResource FloatBarForeground}" Opacity="0.85" />
                                                </StackPanel>
                                                <StackPanel>
                                                    <Grid Width="20" Height="20" HorizontalAlignment="Left"
                                                          MouseDown="Border_MouseDown" MouseUp="GridInkReplayButton_MouseUp">
                                                        <ui:SymbolIcon Symbol="Edit" Width="20" Height="20" />
                                                        <Image Source="{DynamicResource AndroidRefreshDrawingImage}" Width="10" Height="10"
                                                               VerticalAlignment="Bottom" HorizontalAlignment="Right" Margin="0,0,-4,-4" />
                                                    </Grid>
                                                    <TextBlock Text="回放" FontSize="9" Margin="0,2,0,0"
                                                               Foreground="{DynamicResource FloatBarForeground}" Opacity="0.85" />
                                                </StackPanel>
'@
$t = $t.Remove($si, $end - $si).Insert($si, $newRow.Trim("`r", "`n"))

[IO.File]::WriteAllText($mw, $t, $utf8)
Write-Host 'PATCHED  ink row with captions'

Write-Host "== PHASE Y3: verify =="
$n = Get-Content -LiteralPath $mw -Raw
Assert ($n.Contains('Width="292" VerticalAlignment="Top"')) 'panel not widened'
Assert ($n.Contains('Text="保存到其它"')) 'save-as caption missing'
Assert ($n.Contains('Text="回放"')) 'replay caption missing'
Assert ($n.Contains('StbSymbolIconSaveAs_MouseUp')) 'save-as handler lost'
Assert ($n.Contains('GridInkReplayButton_MouseUp')) 'replay handler lost'
Write-Host 'OK  PHASE Y verified'

Write-Host "STB FIX12 PATCHES DONE"

Write-Host "== PHASE Z: floatbar preview + settings entry =="
$fix13 = Join-Path $PSScriptRoot 'apply-stb-fix13.ps1'
Assert (Test-Path -LiteralPath $fix13) 'missing stb-plus/apply-stb-fix13.ps1'
& $fix13 -Root $Root -Version $Version
