# apply-stb-fix13.ps1
# 1.0.1.19：
#   ① 浮动栏编辑窗「当前浮动栏」增加预览条：未接管时显示默认浮动栏样式，接管后显示自己的条目
#   ② 设置页「外观主题」新增「自定义浮动栏 → 编辑浮动栏」入口，方便找到
param(
    [string]$Root = "plus-src",
    [string]$Version = "1.0.0.0"
)

$ErrorActionPreference = 'Stop'
trap {
    Write-Host ("::error::fix13: " + $_.Exception.Message)
    throw
}

$app = Join-Path $Root 'Ink Canvas'
$utf8 = New-Object System.Text.UTF8Encoding($false)

function Assert([bool]$cond, [string]$msg) {
    if (-not $cond) { throw "ANCHOR FAIL: $msg" }
}

Write-Host "== PHASE Z1: copy floatbar xaml and settings entry =="
foreach ($name in @('StbFloatBarWindow.xaml', 'StbSettingsWindow.Bar.cs')) {
    $src = Join-Path $PSScriptRoot ('extra/' + $name)
    Assert (Test-Path -LiteralPath $src) ("missing source " + $name)
    Copy-Item -LiteralPath $src -Destination $app -Force
    Write-Host ("PATCHED  copy " + $name)
}

Write-Host "== PHASE Z1b: preview in floatbar editor code =="
$fcx = Join-Path $app 'StbFloatBarWindow.xaml.cs'
$c = Get-Content -LiteralPath $fcx -Raw

$ctorAnchor = '            BuildActive();'
Assert ($c.Contains($ctorAnchor)) 'ctor anchor missing'
$c = $c.Replace($ctorAnchor, "            BuildPreview();`r`n            BuildActive();", 1)

$commitAnchor = '            StbBarRegistry.SaveItems(_items);'
Assert ($c.Contains($commitAnchor)) 'commit anchor missing'
$c = $c.Replace($commitAnchor, $commitAnchor + "`r`n            BuildPreview();", 1)

$toggleAnchor = '            StbBarRegistry.Enabled = SwEnabled.IsOn;'
Assert ($c.Contains($toggleAnchor)) 'toggle anchor missing'
$c = $c.Replace($toggleAnchor, $toggleAnchor + "`r`n            BuildPreview();", 1)

$methodAnchor = '        private void BuildActive()'
Assert ($c.Contains($methodAnchor)) 'BuildActive anchor missing'
$preview = @'
        /// <summary>浮动栏预览：未接管显示默认浮动栏样式，接管后显示自己的条目。</summary>
        private void BuildPreview()
        {
            PanelPreview.Children.Clear();

            bool custom = StbBarRegistry.Enabled && _items.Count > 0;
            TextPreviewMode.Text = custom ? "（自定义浮动栏，共 " + _items.Count + " 项）" : "（默认浮动栏）";

            if (custom)
            {
                foreach (string id in _items)
                {
                    StbBarItemInfo info = StbBarRegistry.Find(id);
                    if (info != null)
                    {
                        AddPreviewItem(info.Glyph, info.Title, TextMain);
                    }
                }
                return;
            }

            AddPreviewItem("✎", "画笔", TextMain);
            AddPreviewItem("▮", "荧光笔", TextMain);
            AddPreviewItem("▨", "橡皮", TextMain);
            AddPreviewItem("●", "颜色", new SolidColorBrush(Color.FromRgb(0xE5, 0x4D, 0x4D)));
            AddPreviewItem("●", "颜色", new SolidColorBrush(Color.FromRgb(0x2C, 0x6B, 0xED)));
            AddPreviewItem("↶", "撤销", TextMain);
            AddPreviewItem("↷", "重做", TextMain);
            AddPreviewItem("▣", "保存", TextMain);
            AddPreviewItem("⟳", "恢复", TextMain);
            AddPreviewItem("▤", "板中板", TextMain);
            AddPreviewItem("⚙", "设置", TextMain);
        }

        private void AddPreviewItem(string glyph, string label, Brush color)
        {
            StackPanel box = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(7, 0, 7, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            box.Children.Add(new TextBlock
            {
                Text = glyph,
                FontSize = 16,
                Foreground = color,
                HorizontalAlignment = HorizontalAlignment.Center
            });
            box.Children.Add(new TextBlock
            {
                Text = label,
                FontSize = 8,
                Foreground = new SolidColorBrush(Color.FromRgb(0x8A, 0x90, 0x9A)),
                HorizontalAlignment = HorizontalAlignment.Center
            });
            PanelPreview.Children.Add(box);
        }

'@
$c = $c.Replace($methodAnchor, $preview + $methodAnchor, 1)
[IO.File]::WriteAllText($fcx, $c, $utf8)
Write-Host 'PATCHED  floatbar preview method'

Write-Host "== PHASE Z2: settings card =="
$sx = Join-Path $app 'StbSettingsWindow.xaml'
$t = Get-Content -LiteralPath $sx -Raw
$anchor = '<TextBlock Text="外观主题" FontSize="22" FontWeight="SemiBold" Foreground="#22201C" Margin="0,4,0,12" />'
Assert ($t.Contains($anchor)) 'theme title anchor missing'
$card = @'
                            <Border Style="{StaticResource StbCard}">
                                <Grid>
                                    <Grid.ColumnDefinitions><ColumnDefinition Width="*" /><ColumnDefinition Width="Auto" /></Grid.ColumnDefinitions>
                                    <StackPanel Grid.Column="0">
                                        <TextBlock Text="自定义浮动栏" Style="{StaticResource StbCardTitle}" />
                                        <TextBlock Text="用组件方式编排浮动栏上的工具：增删、排序、调图标大小、开关文字" Style="{StaticResource StbCardDesc}" />
                                    </StackPanel>
                                    <Button Grid.Column="1" Content="编辑浮动栏" Padding="14,6" VerticalAlignment="Center"
                                            Click="BtnEditFloatBarSettings_Click" />
                                </Grid>
                            </Border>
'@
$t = $t.Replace($anchor, $anchor + "`r`n" + $card.Trim("`r", "`n"))
[IO.File]::WriteAllText($sx, $t, $utf8)
Write-Host 'PATCHED  settings edit-floatbar entry'

Write-Host "== PHASE Z3: verify =="
$n = Get-Content -LiteralPath $sx -Raw
Assert ($n.Contains('BtnEditFloatBarSettings_Click')) 'settings entry missing'
$fx = Get-Content -LiteralPath (Join-Path $app 'StbFloatBarWindow.xaml') -Raw
Assert ($fx.Contains('x:Name="PanelPreview"')) 'preview panel missing'
Assert ($fx.Contains('x:Name="TextPreviewMode"')) 'preview caption missing'
$fc2 = Get-Content -LiteralPath $fcx -Raw
Assert ($fc2.Contains('private void BuildPreview()')) 'BuildPreview missing'
Assert ($fc2.Contains('（默认浮动栏）')) 'default preview text missing'
Assert (Test-Path -LiteralPath (Join-Path $app 'StbSettingsWindow.Bar.cs')) 'settings partial missing'
Write-Host 'OK  PHASE Z verified'

Write-Host "STB FIX13 PATCHES DONE"
