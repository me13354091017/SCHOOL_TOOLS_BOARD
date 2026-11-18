# apply-stb-fix3.ps1
# 1.0.1.7：板中板橡皮大小改为单击直接弹滑杆、弹层不点外面不消失（StaysOpen=True）；
# 把板中板的调色板/粗细能力带到 PPT（批注）模式的浮动栏。
# 1.0.1.9：删除浮动栏上的「橡皮大小」按钮，并接入新的组件库（组件管理窗）。
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

    Write-Host "== PHASE H1: 拷贝 PPT 调色板代码 =="
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

    # 1.0.1.9：不再向浮动栏插「橡皮大小」图标（橡皮大小仍可在设置页 / 板中板内调节）

    $popup = @(
        '<Popup x:Name="PopupPptEraser" PlacementTarget="{Binding ElementName=BorderPenPalette}" Placement="Bottom"',
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
    Edit $mw '<Viewbox Name="ViewboxFloatingBar"' ($popup + $lf + '        <Viewbox Name="ViewboxFloatingBar"') '浮动栏橡皮大小弹层（隐藏备用）'

    Write-Host "== PHASE H3: 板中板弹层 StaysOpen=True =="
    $mbx = Join-Path $app 'MiniBoardWindow.xaml'
    Edit $mbx 'StaysOpen="False"' 'StaysOpen="True"' '板中板 笔/橡皮弹层可拖滑杆' 2

    Write-Host "== PHASE H5: 组件库 / 组件管理窗 =="
    foreach ($name in @('StbComponentRegistry.cs', 'StbComponentManagerWindow.xaml', 'StbComponentManagerWindow.xaml.cs', 'MainWindow.Components.cs')) {
        $src = Join-Path $PSScriptRoot ('extra/' + $name)
        Assert (Test-Path -LiteralPath $src) ("missing source " + $name)
        Copy-Item -LiteralPath $src -Destination $app -Force
        Write-Host ("PATCHED  copy " + $name)
    }
    Assert (Select-String -LiteralPath (Join-Path $app 'MainWindow.Components.cs') -Pattern 'StbComponentManagerWindow.ShowFor' -Quiet) '组件入口未指向组件管理窗'
    Assert (Select-String -LiteralPath (Join-Path $app 'StbComponentRegistry.cs') -Pattern 'stb.week' -Quiet) '组件库缺少新组件'
    try {
        $xml = New-Object System.Xml.XmlDocument
        $xml.Load((Join-Path $app 'StbComponentManagerWindow.xaml'))
    } catch {
        throw ("StbComponentManagerWindow.xaml 不是良构 XML: " + $_.Exception.Message)
    }
    Write-Host 'OK  PHASE H5 校验通过'

    Write-Host "== PHASE H4: 断言 =="
    Assert (Test-Path -LiteralPath (Join-Path $app 'MainWindow.PptPalette.cs')) 'PPT 调色板代码未拷入'
    Assert (Select-String -LiteralPath $mw -Pattern 'SliderPptPenWidth' -Quiet) '粗细滑杆未插入'
    Assert ((Select-String -LiteralPath $mw -Pattern 'x:Name="BorderPptEraser"' -Quiet) -eq $false) '浮动栏不应再有橡皮大小按钮'
    Assert ((Select-String -LiteralPath $mbx -Pattern 'StaysOpen="False"' -Quiet) -eq $false) '板中板弹层仍会点击即关'
    Write-Host 'OK  PHASE H 校验通过'
}
catch {
    Write-Host ("::error::STB fix3 failed: " + $_.Exception.Message)
    throw
}

Write-Host "STB FIX3 PATCHES DONE"
