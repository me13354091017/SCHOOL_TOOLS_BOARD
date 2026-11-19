# apply-stb-fix6.ps1
# 1.0.1.12：板中板橡皮大小条改为窗口内嵌（去掉 Popup）
# 1.0.1.13：尾部串接 fix7（橡皮自动回笔 + 点条误收修复）
# 1.0.1.14：尾部再串 fix8（抬笔后才回笔 + 设置页开关）
#   板中板是 AllowsTransparency 的分层窗口，橡皮大小条原先用 Popup（又是一个分层窗口），
#   拖动滑杆时两层都要重绘，表现为“一拖就闪断 / 卡死”。改为窗内 Border 覆盖层，控件名保持不变。
param(
    [string]$Root = "plus-src",
    [string]$Version = "1.0.0.0"
)

$ErrorActionPreference = 'Stop'
trap {
    Write-Host ("::error::fix6: " + $_.Exception.Message)
    throw
}

$app = Join-Path $Root 'Ink Canvas'
$utf8 = New-Object System.Text.UTF8Encoding($false)

function Assert([bool]$cond, [string]$msg) {
    if (-not $cond) { throw "ANCHOR FAIL: $msg" }
}

Write-Host "== PHASE S1: 移除橡皮 Popup =="
$mbx = Join-Path $app 'MiniBoardWindow.xaml'
Assert (Test-Path -LiteralPath $mbx) 'MiniBoardWindow.xaml missing'
$t = Get-Content -LiteralPath $mbx -Raw
$n = ([regex]::Matches($t, '<Popup x:Name="PopupEraser"', 'IgnoreCase')).Count
Assert ($n -eq 1) ("PopupEraser count: " + $n)
$t = [regex]::Replace($t, '<Popup x:Name="PopupEraser".*?</Popup>\r?\n', '', [Text.RegularExpressions.RegexOptions]::Singleline)
Write-Host 'PATCHED  removed PopupEraser'

Write-Host "== PHASE S2: 窗口内嵌橡皮大小条 =="
$panel = @'
<Border x:Name="InlineEraserPanel" Visibility="Collapsed" Panel.ZIndex="40"
                        HorizontalAlignment="Left" VerticalAlignment="Bottom"
                        Margin="10,0,0,38" Padding="10,8" CornerRadius="10"
                        Background="#F2161D18" BorderBrush="#40FFFFFF" BorderThickness="1">
                    <StackPanel Width="196">
                        <TextBlock Text="橡皮大小" FontSize="10" Foreground="#99E8EBEF" Margin="2,0,0,5" />
                        <Slider x:Name="SliderMBEraser" Minimum="4" Maximum="60" Value="14"
                                TickFrequency="1" IsSnapToTickEnabled="True" SmallChange="1" LargeChange="4"
                                ValueChanged="SliderMBEraser_ValueChanged" />
                        <TextBlock x:Name="TextBlockMBEraserInfo" Text="直径 14 像素"
                                   FontSize="10" Foreground="#99E8EBEF" Margin="2,3,0,0" />
                        <TextBlock Text="松手后生效；调大一次擦掉更多，调小更精细"
                                   FontSize="9" Foreground="#80E8EBEF" TextWrapping="Wrap" Margin="2,3,0,0" />
                    </StackPanel>
                </Border>

                <Border x:Name="ToolBar"
'@
$anchor = '<Border x:Name="ToolBar"'
Assert ($t.Contains($anchor)) 'ToolBar anchor missing'
$t = $t.Replace($anchor, $panel)
[IO.File]::WriteAllText($mbx, $t, $utf8)
Write-Host 'PATCHED  inserted InlineEraserPanel'

Write-Host "== PHASE S3: verify =="
$n = Get-Content -LiteralPath $mbx -Raw
Assert (-not $n.Contains('PopupEraser')) 'PopupEraser still present'
Assert ($n.Contains('x:Name="InlineEraserPanel"')) 'inline panel missing'
Assert ($n.Contains('x:Name="SliderMBEraser"')) 'slider name lost'
Assert ($n.Contains('x:Name="TextBlockMBEraserInfo"')) 'info text lost'
Write-Host 'OK  PHASE S verified'

Write-Host "== PHASE T: 橡皮自动回笔 =="
$fix7 = Join-Path $PSScriptRoot 'apply-stb-fix7.ps1'
Assert (Test-Path -LiteralPath $fix7) 'missing stb-plus/apply-stb-fix7.ps1'
& $fix7 -Root $Root -Version $Version

Write-Host "== PHASE U: 抬笔回笔 + 设置开关 =="
$fix8 = Join-Path $PSScriptRoot 'apply-stb-fix8.ps1'
Assert (Test-Path -LiteralPath $fix8) 'missing stb-plus/apply-stb-fix8.ps1'
& $fix8 -Root $Root -Version $Version

Write-Host "STB FIX6 PATCHES DONE"
