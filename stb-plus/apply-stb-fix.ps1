# apply-stb-fix.ps1
# 1.0.1.5：去掉用橡皮时跟随笔尖的蓝色圆框；设置页新增「橡皮擦大小」档位调节（-、+ 按钮，7 档，立即生效）。
# 调色盘逻辑已整体放进 stb-plus/extra/MainWindow.PenPalette.cs，这里不再重复改。
param(
    [string]$Root = "plus-src",
    [string]$Version = "1.0.0.0"
)

$ErrorActionPreference = 'Stop'
$app = Join-Path $Root 'Ink Canvas'
$utf8 = New-Object System.Text.UTF8Encoding($false)

function Assert([bool]$cond, [string]$msg) {
    if (-not $cond) { throw "ANCHOR FAIL: $msg" }
}

function Edit([string]$path, [string]$old, [string]$new, [string]$label) {
    Assert (Test-Path -LiteralPath $path) "$label :: file missing $path"
    $t = Get-Content -LiteralPath $path -Raw
    Assert ($t.Contains($old)) "$label :: anchor not found"
    $t = $t.Replace($old, $new)
    [IO.File]::WriteAllText($path, $t, $utf8)
    Write-Host ("PATCHED  " + $label)
}

try {

    Write-Host "== PHASE F2: 去掉橡皮时的蓝色跟随圈 =="
    $ring = Join-Path $app 'MainWindow.EraserRing.cs'
    Edit $ring '            StbEraserRing.Visibility = Visibility.Visible;' '            StbEraserRing.Visibility = Visibility.Collapsed;' '黑板 橡皮圆环不显示'
    Edit $ring '            inkCanvas.Cursor = Cursors.None;' '            inkCanvas.Cursor = Cursors.Pen;' '黑板 橡皮光标恢复正常'

    $mbe = Join-Path $app 'MiniBoardWindow.Extras.cs'
    Edit $mbe '            MiniEraserRing.Visibility = Visibility.Visible;' '            MiniEraserRing.Visibility = Visibility.Collapsed;' '板中板 橡皮圆环不显示'
    Edit $mbe '            MiniInkCanvas.Cursor = Cursors.None;' '            MiniInkCanvas.Cursor = Cursors.Pen;' '板中板 橡皮光标恢复正常'

    Write-Host "== PHASE F3: 设置页 橡皮擦大小 =="
    $eraserSrc = Join-Path $PSScriptRoot 'extra/StbSettingsWindow.Eraser.cs'
    Assert (Test-Path -LiteralPath $eraserSrc) 'missing source StbSettingsWindow.Eraser.cs'
    Copy-Item -LiteralPath $eraserSrc -Destination $app -Force
    Write-Host 'PATCHED  copy StbSettingsWindow.Eraser.cs'

    # 主窗口补一个「立刻按档位刷新橡皮形状」的方法
    $perf = Join-Path $app 'MainWindow.Perf.cs'
    Edit $perf 'using System.Diagnostics;' @'
using System.Diagnostics;
using System.Windows.Ink;
'@ 'Perf.cs 引入 System.Windows.Ink'

    Edit $perf '        private static bool StbAnyProcessNamed(string name)' @'
        /// <summary>设置页改完橡皮档位后立刻生效（上游只在切到橡皮时才按系数算形状）。</summary>
        internal void StbRefreshEraserShape()
        {
            try
            {
                if (inkCanvas == null)
                {
                    return;
                }
                double d = 50d * GetEraserSizeCoefficient();
                inkCanvas.EraserShape = forcePointEraser ? new EllipseStylusShape(d, d) : new EllipseStylusShape(5d, 5d);
            }
            catch
            {
            }
        }

        private static bool StbAnyProcessNamed(string name)
'@ 'MainWindow 橡皮形状刷新方法'

    $sx = Join-Path $app 'StbSettingsWindow.xaml'
    $sxText = Get-Content -LiteralPath $sx -Raw
    $nl = "`n"
    if ($sxText.Contains("`r`n")) { $nl = "`r`n" }
    $anchor =
        '</Border>' + $nl +
        '                            <Border Style="{StaticResource StbCard}">' + $nl +
        '                                <Grid>' + $nl +
        '                                    <Grid.ColumnDefinitions><ColumnDefinition Width="*" /><ColumnDefinition Width="Auto" /></Grid.ColumnDefinitions>' + $nl +
        '                                    <StackPanel Grid.Column="0">' + $nl +
        '                                        <TextBlock Text="显示画笔光标" Style="{StaticResource StbCardTitle}" />'
    Assert ($sxText.Contains($anchor)) '设置页 橡皮擦大小 :: 卡片插入锚点未找到'

    $card =
        '                            <Border Style="{StaticResource StbCard}">' + $nl +
        '                                <Grid>' + $nl +
        '                                    <Grid.ColumnDefinitions><ColumnDefinition Width="*" /><ColumnDefinition Width="Auto" /></Grid.ColumnDefinitions>' + $nl +
        '                                    <StackPanel Grid.Column="0">' + $nl +
        '                                        <TextBlock Text="橡皮擦大小" Style="{StaticResource StbCardTitle}" />' + $nl +
        '                                        <TextBlock Text="点擦橡皮的直径档位，共 7 档，改完立刻生效" Style="{StaticResource StbCardDesc}" />' + $nl +
        '                                    </StackPanel>' + $nl +
        '                                    <StackPanel Grid.Column="1" Orientation="Horizontal" VerticalAlignment="Center" Margin="16,0,0,0">' + $nl +
        '                                        <Button Content="－" MinWidth="34" Height="30" Margin="0,0,6,0" Style="{StaticResource StbLink}" Click="BtnEraserSizeMinus_Click" />' + $nl +
        '                                        <TextBlock x:Name="TextEraserSize" MinWidth="92" TextAlignment="Center" VerticalAlignment="Center" Foreground="#5B5749" Loaded="TextEraserSize_Loaded" />' + $nl +
        '                                        <Button Content="＋" MinWidth="34" Height="30" Margin="6,0,0,0" Style="{StaticResource StbLink}" Click="BtnEraserSizePlus_Click" />' + $nl +
        '                                    </StackPanel>' + $nl +
        '                                </Grid>' + $nl +
        '                            </Border>'

    $sxText = $sxText.Replace($anchor,
        '</Border>' + $nl + $card + $nl +
        '                            <Border Style="{StaticResource StbCard}">' + $nl +
        '                                <Grid>' + $nl +
        '                                    <Grid.ColumnDefinitions><ColumnDefinition Width="*" /><ColumnDefinition Width="Auto" /></Grid.ColumnDefinitions>' + $nl +
        '                                    <StackPanel Grid.Column="0">' + $nl +
        '                                        <TextBlock Text="显示画笔光标" Style="{StaticResource StbCardTitle}" />')
    [IO.File]::WriteAllText($sx, $sxText, $utf8)
    Write-Host 'PATCHED  设置页 橡皮擦大小卡片'

    Write-Host "== PHASE F4: 断言 =="
    $ringText = Get-Content -LiteralPath $ring -Raw
    Assert (-not $ringText.Contains('Cursors.None')) '黑板 橡皮仍是隐藏光标'
    $mbeText = Get-Content -LiteralPath $mbe -Raw
    Assert (-not $mbeText.Contains('Cursors.None')) '板中板 橡皮仍是隐藏光标'

    Assert (Test-Path -LiteralPath (Join-Path $app 'StbSettingsWindow.Eraser.cs')) '设置页橡皮档位代码未拷入'
    Assert (Select-String -LiteralPath $perf -Pattern 'StbRefreshEraserShape' -Quiet) '橡皮形状刷新方法未写入'
    Assert (Select-String -LiteralPath $sx -Pattern 'BtnEraserSizePlus_Click' -Quiet) '设置页橡皮档位按钮未插入'
    Write-Host 'OK  PHASE F 校验通过'
}
catch {
    Write-Host ("::error::STB fix failed: " + $_.Exception.Message)
    throw
}

Write-Host "STB FIX PATCHES DONE"
