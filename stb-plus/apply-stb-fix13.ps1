# apply-stb-fix13.ps1 - 1.0.1.22: tail chains fix14
param([string]$Root = "plus-src", [string]$Version = "1.0.0.0")
$ErrorActionPreference = 'Stop'
trap { Write-Host ("::error::fix13: " + $_.Exception.Message); throw }
$app = Join-Path $Root 'Ink Canvas'
function Assert([bool]$c, [string]$m) { if (-not $c) { throw "ANCHOR FAIL: $m" } }

Write-Host "== PHASE Z1 =="
foreach ($name in @('StbFloatBarWindow.xaml', 'StbSettingsWindow.Bar.cs')) {
    $src = Join-Path $PSScriptRoot ('extra/' + $name)
    Assert (Test-Path -LiteralPath $src) ("missing source " + $name)
    Copy-Item -LiteralPath $src -Destination $app -Force
}
$fcx = Join-Path $app 'StbFloatBarWindow.xaml.cs'
$c = Get-Content -LiteralPath $fcx -Raw
Assert ($c.Contains('            BuildActive();')) 'ctor anchor'
$c = $c.Replace('            BuildActive();', "            BuildPreview();`r`n            BuildActive();", 1)
Assert ($c.Contains('            StbBarRegistry.SaveItems(_items);')) 'commit anchor'
$c = $c.Replace('            StbBarRegistry.SaveItems(_items);', "            StbBarRegistry.SaveItems(_items);`r`n            BuildPreview();", 1)
Assert ($c.Contains('            StbBarRegistry.Enabled = SwEnabled.IsOn;')) 'toggle anchor'
$c = $c.Replace('            StbBarRegistry.Enabled = SwEnabled.IsOn;', "            StbBarRegistry.Enabled = SwEnabled.IsOn;`r`n            BuildPreview();", 1)
$mAnchor = '        private void BuildActive()'
Assert ($c.Contains($mAnchor)) 'method anchor'
$prev = @'
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
                    if (info != null) AddPreviewItem(info.Glyph, info.Title, TextMain);
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
            StackPanel box = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(7, 0, 7, 0), VerticalAlignment = VerticalAlignment.Center };
            box.Children.Add(new TextBlock { Text = glyph, FontSize = 16, Foreground = color, HorizontalAlignment = HorizontalAlignment.Center });
            box.Children.Add(new TextBlock { Text = label, FontSize = 8, Foreground = new SolidColorBrush(Color.FromRgb(0x8A, 0x90, 0x9A)), HorizontalAlignment = HorizontalAlignment.Center });
            PanelPreview.Children.Add(box);
        }

'@
$c = $c.Replace($mAnchor, $prev + $mAnchor, 1)
[IO.File]::WriteAllText($fcx, $c, (New-Object System.Text.UTF8Encoding($false)))

Write-Host "== PHASE Z2 =="
$sx = Join-Path $app 'StbSettingsWindow.xaml'
$t = Get-Content -LiteralPath $sx -Raw
$anchor = '<TextBlock Text="外观主题" FontSize="22" FontWeight="SemiBold" Foreground="#22201C" Margin="0,4,0,12" />'
Assert ($t.Contains($anchor)) 'theme anchor'
$card = '<Border Style="{StaticResource StbCard}">' + "`r`n" +
'                                <Grid>' + "`r`n" +
'                                    <Grid.ColumnDefinitions><ColumnDefinition Width="*" /><ColumnDefinition Width="Auto" /></Grid.ColumnDefinitions>' + "`r`n" +
'                                    <StackPanel Grid.Column="0">' + "`r`n" +
'                                        <TextBlock Text="自定义浮动栏" Style="{StaticResource StbCardTitle}" />' + "`r`n" +
'                                        <TextBlock Text="用组件方式编排浮动栏上的工具：增删、排序、调图标大小、开关文字" Style="{StaticResource StbCardDesc}" />' + "`r`n" +
'                                    </StackPanel>' + "`r`n" +
'                                    <Button Grid.Column="1" Content="编辑浮动栏" Padding="14,6" VerticalAlignment="Center" Click="BtnEditFloatBarSettings_Click" />' + "`r`n" +
'                                </Grid>' + "`r`n" +
'                            </Border>'
$t = $t.Replace($anchor, $anchor + "`r`n" + $card)
[IO.File]::WriteAllText($sx, $t, (New-Object System.Text.UTF8Encoding($false)))
Assert ((Get-Content -LiteralPath $sx -Raw).Contains('BtnEditFloatBarSettings_Click')) 'settings entry missing'
Write-Host 'OK  PHASE Z verified'

Write-Host "STB FIX13 PATCHES DONE"

$fix14 = Join-Path $PSScriptRoot 'apply-stb-fix14.ps1'
Assert (Test-Path -LiteralPath $fix14) 'missing fix14'
& $fix14 -Root $Root -Version $Version
