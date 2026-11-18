# apply-stb-fix4.ps1
# 1.0.1.10：重排浮动栏「快捷设置」面板（消除重叠）、「保存到其它」换成独立另存为图标。
param(
    [string]$Root = "plus-src",
    [string]$Version = "1.0.0.0"
)

$ErrorActionPreference = 'Stop'
trap {
    Write-Host ("::error::fix4: " + $_.Exception.Message)
    throw
}

$app = Join-Path $Root 'Ink Canvas'
$utf8 = New-Object System.Text.UTF8Encoding($false)

function Assert([bool]$cond, [string]$msg) {
    if (-not $cond) { throw "ANCHOR FAIL: $msg" }
}

function Find-BlockEnd([string]$text, [int]$start) {
    $i = $start
    $depth = 0
    while ($i -lt $text.Length) {
        $o = $text.IndexOf('<Border', $i)
        $c = $text.IndexOf('</Border>', $i)
        if ($c -lt 0) { throw 'unclosed Border block' }
        if ($o -ge 0 -and $o -lt $c) {
            $ch = $text.Substring($o + 7, 1)
            if ($ch -match '[\s/>]') {
                $gt = $text.IndexOf('>', $o)
                if ($gt -lt 0) { throw 'bad Border tag' }
                $prev = $text.Substring($gt - 1, 1)
                if ($prev -ne '/') { $depth++ }
                $i = $gt + 1
            } else {
                $i = $o + 7
            }
        } else {
            $depth--
            $i = $c + 9
            if ($depth -eq 0) { return $i }
        }
    }
    throw 'unbalanced Border block'
}

Write-Host "== PHASE Q1: 重排「快捷设置」面板 =="
$mw = Join-Path $app 'MainWindow.xaml'
$t = Get-Content -LiteralPath $mw -Raw
$start = $t.IndexOf('<Border Name="BorderTools"')
Assert ($start -ge 0) 'BorderTools 未找到'
$end = Find-BlockEnd $t $start
$oldLength = $end - $start
Write-Host ("BorderTools block length = " + $oldLength)

$newPanel = @'
<Border Name="BorderTools" Width="250" Margin="37,-286,-190,-6" CornerRadius="18"
        Background="{DynamicResource FloatBarBackground}" BorderThickness="1"
        BorderBrush="{DynamicResource FloatBarBorderBrush}"
        TextElement.Foreground="{DynamicResource FloatBarForeground}">
    <StackPanel Margin="14,10,14,12">
        <TextBlock Text="快捷设置" FontSize="14" Margin="0,0,0,8" />
        <TextBlock Text="画笔粗细" FontSize="12" Margin="0,0,0,2" />
        <Grid Margin="0,0,0,10">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*" />
                <ColumnDefinition Width="Auto" />
            </Grid.ColumnDefinitions>
            <Slider Grid.Column="0" Minimum="1" Maximum="20" Width="168"
                    HorizontalAlignment="Left" VerticalAlignment="Center"
                    IsSnapToTickEnabled="True" TickFrequency="1" TickPlacement="Both"
                    Value="{Binding ElementName=InkWidthSlider, Path=Value, Mode=TwoWay}" />
            <TextBlock Grid.Column="1" Width="18" TextAlignment="Right" VerticalAlignment="Center"
                       FontSize="13" Margin="8,0,0,0"
                       Text="{Binding Value, ElementName=InkWidthSlider, Mode=OneWay}" />
        </Grid>
        <Grid Margin="0,0,0,10">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*" />
                <ColumnDefinition Width="*" />
            </Grid.ColumnDefinitions>
            <StackPanel Grid.Column="0">
                <TextBlock Text="墨迹识别" FontSize="12" Margin="0,0,0,2" />
                <Viewbox Height="24" HorizontalAlignment="Left">
                    <ui:ToggleSwitch Width="72" MinWidth="0" OnContent="开" OffContent="关"
                                     FontFamily="Microsoft YaHei UI"
                                     Toggled="ToggleSwitchEnableInkToShape_Toggled"
                                     IsOn="{Binding ElementName=ToggleSwitchEnableInkToShape, Path=IsOn}" />
                </Viewbox>
            </StackPanel>
            <StackPanel Grid.Column="1">
                <TextBlock Text="手指模式" FontSize="12" Margin="0,0,0,2" />
                <Viewbox Height="24" HorizontalAlignment="Left">
                    <ui:ToggleSwitch Width="72" MinWidth="0" OnContent="开" OffContent="关"
                                     FontFamily="Microsoft YaHei UI"
                                     IsOn="{Binding ElementName=ToggleSwitchModeFinger, Path=IsOn}" />
                </Viewbox>
            </StackPanel>
        </Grid>
        <TextBlock Text="墨迹" FontSize="13" Margin="0,0,0,6" />
        <StackPanel Orientation="Horizontal" Margin="0,0,0,10">
            <ui:SymbolIcon Symbol="Save" Width="20" Height="20" VerticalAlignment="Center"
                           MouseDown="Border_MouseDown" MouseUp="SymbolIconSaveStrokes_MouseUp" />
            <ui:SymbolIcon Symbol="Copy" Width="20" Height="20" Margin="16,0,0,0" VerticalAlignment="Center"
                           ToolTip="保存到其它位置"
                           MouseDown="Border_MouseDown" MouseUp="StbSymbolIconSaveAs_MouseUp" />
            <Border Width="1" Height="18" Margin="16,0" VerticalAlignment="Center"
                    Background="{DynamicResource FloatBarForeground}" Opacity="0.55" />
            <Grid Width="20" Height="20" VerticalAlignment="Center"
                  MouseDown="Border_MouseDown" MouseUp="GridInkReplayButton_MouseUp">
                <ui:SymbolIcon Symbol="Edit" Width="20" Height="20" />
                <Image Source="{DynamicResource AndroidRefreshDrawingImage}" Width="10" Height="10"
                       VerticalAlignment="Bottom" HorizontalAlignment="Right" Margin="0,0,-4,-4" />
            </Grid>
        </StackPanel>
        <TextBlock Text="工具" FontSize="13" Margin="0,0,0,6" />
        <StackPanel Orientation="Horizontal">
            <Grid Width="20" Height="20" VerticalAlignment="Center">
                <Image Margin="-7,-2,-7,-2" MouseDown="Border_MouseDown" MouseUp="ImageCountdownTimer_MouseUp">
                    <Image.Source>
                        <DrawingImage>
                            <DrawingImage.Drawing>
                                <DrawingGroup ClipGeometry="M0,0 V64 H64 V0 H0 Z">
                                    <DrawingGroup Opacity="1">
                                        <GeometryDrawing Brush="{DynamicResource FloatBarForeground}" Geometry="F1 M64,64z M0,0z M42.811,22.789C44.393,24.383 45.612,26.126 46.467,28.018 47.322,29.91 47.75,31.988 47.75,34.249 47.75,35.702 47.562,37.1 47.188,38.441 46.814,39.782 46.283,41.037 45.597,42.203 44.911,43.369 44.091,44.43 43.136,45.385 42.181,46.34 41.12,47.16 39.954,47.846 38.788,48.532 37.534,49.062 36.192,49.437 34.85,49.812 33.453,50 32,50 30.547,50 29.149,49.812 27.808,49.438 26.467,49.064 25.212,48.533 24.046,47.847 22.88,47.161 21.819,46.341 20.864,45.386 19.909,44.431 19.089,43.37 18.403,42.204 17.717,41.038 17.187,39.784 16.812,38.442 16.437,37.1 16.25,35.703 16.25,34.25 16.25,32.949 16.405,31.684 16.716,30.453 17.027,29.222 17.466,28.062 18.034,26.973 18.602,25.884 19.291,24.872 20.099,23.941 20.907,23.01 21.81,22.183 22.806,21.462 23.802,20.741 24.886,20.141 26.058,19.66 27.23,19.179 28.46,18.846 29.749,18.658L29.749,16.25 25.249,16.25 25.249,14 36.5,14 36.5,16.25 32,16.25 32,18.5C33.629,18.5 35.214,18.74 36.755,19.221 38.296,19.702 39.729,20.416 41.053,21.366L44.709,17.71 46.291,19.292 42.811,22.789z M32,47.75C33.863,47.75 35.612,47.396 37.247,46.687 38.882,45.978 40.311,45.011 41.536,43.787 42.761,42.563 43.727,41.133 44.436,39.498 45.145,37.863 45.5,36.113 45.5,34.25 45.5,32.387 45.146,30.638 44.437,29.003 43.728,27.368 42.761,25.939 41.537,24.714 40.313,23.489 38.883,22.523 37.248,21.814 35.613,21.105 33.863,20.75 32,20.75 30.137,20.75 28.388,21.104 26.753,21.813 25.118,22.522 23.689,23.489 22.464,24.713 21.239,25.937 20.273,27.367 19.564,29.002 18.855,30.637 18.5,32.387 18.5,34.25 18.5,36.113 18.854,37.862 19.563,39.497 20.272,41.132 21.239,42.561 22.463,43.786 23.687,45.011 25.117,45.977 26.752,46.686 28.387,47.395 30.137,47.75 32,47.75z M38.75,34.25L38.75,36.5 29.75,36.5 29.75,25.25 32,25.25 32,34.25 38.75,34.25z" />
                                    </DrawingGroup>
                                </DrawingGroup>
                            </DrawingImage.Drawing>
                        </DrawingImage>
                    </Image.Source>
                </Image>
            </Grid>
            <Border Width="1" Height="18" Margin="16,0" VerticalAlignment="Center"
                    Background="{DynamicResource FloatBarForeground}" Opacity="0.55" />
            <ui:SymbolIcon Symbol="Shuffle" Width="20" Height="20" VerticalAlignment="Center"
                           MouseDown="Border_MouseDown" MouseUp="SymbolIconRand_MouseUp" />
            <Grid Width="22" Height="22" Margin="16,0,0,0" VerticalAlignment="Center"
                  MouseUp="SymbolIconRandOne_MouseUp" MouseDown="Border_MouseDown">
                <ui:SymbolIcon Symbol="Shuffle" Width="20" Height="20" />
                <Border CornerRadius="50" BorderThickness="1"
                        BorderBrush="{DynamicResource FloatBarForeground}"
                        HorizontalAlignment="Right" VerticalAlignment="Bottom"
                        Height="10" Width="10" Margin="0,0,-2,-3">
                    <TextBlock Text="1" FontSize="7" HorizontalAlignment="Center"
                               VerticalAlignment="Center" Margin="0,-1,0,0" />
                </Border>
            </Grid>
        </StackPanel>
    </StackPanel>
</Border>
'@

$t = $t.Remove($start, $oldLength).Insert($start, $newPanel)
[IO.File]::WriteAllText($mw, $t, $utf8)
Write-Host 'PATCHED  快捷设置面板重排'

Write-Host "== PHASE Q2: 校验 =="
$n = Get-Content -LiteralPath $mw -Raw
Assert ($n.Contains('Margin="37,-286,-190,-6"')) '新面板未写入'
Assert ($n.Contains('Symbol="Copy"')) '另存为图标未替换'
Assert ($n.Contains('StbSymbolIconSaveAs_MouseUp')) '另存为处理器未绑定'
Assert (-not $n.Contains('Margin="0,-32,10,0"')) '仍存在旧的负边距布局'
Assert ($n.Contains('Text="画笔粗细"')) '画笔粗细行缺失'
Assert ($n.Contains('Text="手指模式"')) '手指模式行缺失'
Write-Host 'OK  PHASE Q 校验通过'

Write-Host "STB FIX4 PATCHES DONE"
