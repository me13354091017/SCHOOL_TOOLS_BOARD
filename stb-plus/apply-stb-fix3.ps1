# apply-stb-fix3.ps1
# 1.0.1.7：板中板橡皮大小改为单击直接弹滑杆、弹层不点外面不消失（StaysOpen=True）；
# 把板中板的调色板/粗细/橡皮大小能力带到 PPT（批注）模式的浮动栏。
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

function Edit([string]$path, [string]$old, [string]$new, [string]$label, [int]$times = 1) {
    Assert (Test-Path -LiteralPath $path) "$label :: file missing $path"
    $t = Get-Content -LiteralPath $path -Raw
    $n = ([regex]::Matches($t, [regex]::Escape($old))).Count
    Assert ($n -ge $times) "$label :: anchor not found (count=$n)"
    if ($n -gt 0) {
        $t = $t.Replace($old, $new)
        [IO.File]::WriteAllText($path, $t, $utf8)
    }
    Write-Host ("PATCHED  " + $label + "  (x" + $n + ")")
}

try {

    Write-Host "== PHASE H1: 拷贝 PPT 调色板/橡皮代码 =="
    $pptSrc = Join-Path $PSScriptRoot 'extra/MainWindow.PptPalette.cs'
    Assert (Test-Path -LiteralPath $pptSrc) 'missing source MainWindow.PptPalette.cs'
    Copy-Item -LiteralPath $pptSrc -Destination $app -Force
    Write-Host 'PATCHED  copy MainWindow.PptPalette.cs'

    Write-Host "== PHASE H2: 主窗口（PPT/批注）浮动栏能力 =="
    $mw = Join-Path $app 'MainWindow.xaml'
    $lf = "`n"

    $hint = '<TextBlock Text="点一下即生效；非内置色时六色勾选会自动清空" FontSize="9" Foreground="#80E8EBEF" Margin="2,6,0,0" />'
    $penSlider = @(
        '<TextBlock Text="粗细" FontSize="10" Foreground="#99E8EBEF" Margin="2,6,0,4" />',
        '<Slider x:Name="SliderPptPenWidth" Minimum="1" Maximum="20" Value="5"',
        '        TickFrequency="1" IsSnapToTickEnabled="True" SmallChange="1" LargeChange="2"',
        '        ValueChanged="SliderPptPenWidth_ValueChanged" Loaded="SliderPptPenWidth_Loaded" />'
    ) -join $lf
    Edit $mw $hint ($hint + $lf + '                    ' + $penSlider) '更多颜色弹层加粗细滑杆'

    $palTail = '                    <Border Background="#FF2E9E5B" CornerRadius="0,0,2,0" />' + $lf +
               '                </UniformGrid>' + $lf +
               '            </Border>'
    $eraserIcon = @(
        '            <Border x:Name="BorderPptEraser" Width="16" Height="16" CornerRadius="4" Background="#33FFFFFF"',
        '                    BorderBrush="#59FFFFFF" BorderThickness="1" Cursor="Hand" VerticalAlignment="Center"',
        '                    ToolTip="橡皮大小（点开拖滑杆）" MouseUp="BorderPptEraser_MouseUp">',
        '                <Viewbox Width="14" Height="14" HorizontalAlignment="Center" VerticalAlignment="Center">',
        '                    <ui:SymbolIcon Symbol="Remove" Foreground="{DynamicResource FloatBarForeground}" />',
        '                </Viewbox>',
        '            </Border>'
    ) -join $lf
    Edit $mw $palTail ($palTail + $lf + '                            ' + $eraserIcon) '浮动栏橡皮大小图标'

    $popup = @(
        '<Popup x:Name="PopupPptEraser" PlacementTarget="{Binding ElementName=BorderPptEraser}" Placement="Bottom"',
        '       StaysOpen="False" AllowsTransparency="True" PopupAnimation="Fade" VerticalOffset="6">',
        '    <Border Background="#FF161D18" BorderBrush="#40FFFFFF" BorderThickness="1" CornerRadius="10" Padding="10,9">',
        '        <StackPanel Width="196">',
        '            <TextBlock Text="橡皮大小" FontSize="10" Foreground="#99E8EBEF" Margin="2,0,0,5" />',
        '            <Slider x:Name="SliderPptEraser" Minimum="4" Maximum="60" Value="14"',
        '                    TickFrequency="1" IsSnapToTickEnabled="True" SmallChange="1" LargeChange="4"',
        '                    ValueChanged="SliderPptEraser_ValueChanged" Loaded="SliderPptEraser_Loaded" />',
        '            <TextBlock x:Name="TextBlockPptEraserInfo" Text="直径 14 像素" FontSize="10" Foreground="#99E8EBEF" Margin="2,3,0,0" />',
        '        </StackPanel>',
        '    </Border>',
        '</Popup>'
    ) -join $lf
    Edit $mw '<Viewbox Name="ViewboxFloatingBar"' ($popup + $lf + '        <Viewbox Name="ViewboxFloatingBar"') '浮动栏橡皮大小弹层'

    Write-Host "== PHASE H3: 板中板弹层 StaysOpen=True =="
    $mbx = Join-Path $app 'MiniBoardWindow.xaml'
    Edit $mbx 'StaysOpen="False"' 'StaysOpen="True"' '板中板 笔/橡皮弹层可拖滑杆' 2

    Write-Host "== PHASE H4: 断言 =="
    Assert (Test-Path -LiteralPath (Join-Path $app 'MainWindow.PptPalette.cs')) 'PPT 调色板代码未拷入'
    Assert (Select-String -LiteralPath $mw -Pattern 'SliderPptPenWidth' -Quiet) '粗细滑杆未插入'
    Assert (Select-String -LiteralPath $mw -Pattern 'BorderPptEraser' -Quiet) '橡皮大小图标未插入'
    Assert (Select-String -LiteralPath $mw -Pattern 'PopupPptEraser' -Quiet) '橡皮大小弹层未插入'
    Assert ((Select-String -LiteralPath $mbx -Pattern 'StaysOpen="False"' -Quiet) -eq $false) '板中板弹层仍会点击即关'
    Write-Host 'OK  PHASE H 校验通过'
}
catch {
    Write-Host ("::error::STB fix3 failed: " + $_.Exception.Message)
    throw
}

Write-Host "STB FIX3 PATCHES DONE"
