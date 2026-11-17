# apply-stb-components.ps1
# PHASE N：组件系统（ICCCE 风格）+ 浮动栏重绘 + 设置页「退出/重启应用」+ PPT 灵敏度（N7）+ 浮动栏调色板（N8）。
# 由 apply-stb-miniboard.ps1 末尾串联，在 PHASE L / M 之后执行。
param(
    [string]$Root = "plus-src",
    [string]$Version = "1.0.0.0"
)

$ErrorActionPreference = 'Stop'
$app = Join-Path $Root 'Ink Canvas'
$utf8 = New-Object System.Text.UTF8Encoding($false)
$NL = "`r`n"

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

$extra = Join-Path $PSScriptRoot 'extra'
$mw = Join-Path $app 'MainWindow.xaml'
$setXaml = Join-Path $app 'StbSettingsWindow.xaml'
$setCs = Join-Path $app 'StbSettingsWindow.xaml.cs'

Write-Host "== PHASE N1: 拷入组件系统文件 =="
foreach ($f in @('StbComponents.cs', 'StbComponentWindow.xaml', 'StbComponentWindow.xaml.cs', 'StbComponentDock.xaml', 'StbComponentDock.xaml.cs', 'MainWindow.Components.cs', 'MiniBoardWindow.Pens.cs', 'MainWindow.PenPalette.cs')) {
    $src = Join-Path $extra $f
    Assert (Test-Path -LiteralPath $src) "missing source $f"
    Copy-Item -LiteralPath $src -Destination $app -Force
    Write-Host ("PATCHED  copy " + $f)
}

Write-Host "== PHASE N2: 浮动栏新增「组件」入口（图标+文字） =="
$miniAnchor = '<StackPanel Background="Transparent" Orientation="Vertical" HorizontalAlignment="Center" VerticalAlignment="Center" MouseUp="SymbolIconMiniBoard_MouseUp" ToolTip="板中板">'
$compEntry = @'
<StackPanel Background="Transparent" Orientation="Vertical" HorizontalAlignment="Center" VerticalAlignment="Center" MouseUp="SymbolIconComponents_MouseUp" ToolTip="组件">
                                <Viewbox Width="15" Height="15" HorizontalAlignment="Center">
                                    <ui:SymbolIcon Symbol="Contact" Foreground="{DynamicResource FloatBarForeground}" />
                                </Viewbox>
                                <TextBlock Text="组件" FontSize="8" HorizontalAlignment="Center" Margin="0,-1,0,0" Foreground="{DynamicResource FloatBarForeground}" />
                            </StackPanel>
'@
Edit $mw $miniAnchor ($compEntry.Trim("`r", "`n") + $NL + "                            " + $miniAnchor) 'floatbar 组件 icon+text'

Write-Host "== PHASE N3: 左侧竖栏新增「组件」按钮 =="
$oldMiniBtn = '<Button x:Name="BtnMiniBoardMain" FontFamily="Microsoft YaHei UI" Padding="0,4,0,4" ToolTip="板中板"'
$compBtn = @'
<Button x:Name="BtnComponentsMain" FontFamily="Microsoft YaHei UI" Padding="0,4,0,4" ToolTip="组件"
                Margin="0,0,0,0" Width="{Binding ElementName=StackPanelMain, Path=ActualWidth}"
                Click="BtnComponents_Click" Foreground="{Binding ElementName=BtnExit, Path=Foreground}" Background="{Binding ElementName=BtnExit, Path=Background}">
            <StackPanel HorizontalAlignment="Center">
                <Viewbox Width="18" Height="18" HorizontalAlignment="Center">
                    <ui:SymbolIcon Symbol="Contact" Foreground="{Binding ElementName=BtnExit, Path=Foreground}" />
                </Viewbox>
                <TextBlock Text="组件" FontSize="10" HorizontalAlignment="Center" Margin="0,-1,0,0" Foreground="{Binding ElementName=BtnExit, Path=Foreground}" />
            </StackPanel>
        </Button>
'@
Edit $mw $oldMiniBtn ($compBtn.Trim("`r", "`n") + $NL + "                " + $oldMiniBtn) 'leftbar 组件 icon+text'

Write-Host "== PHASE N4: 浮动栏重绘（胶囊更圆 + 投影 + 收紧间距） =="
Edit $mw 'Height="36" Name="BorderFloatingBarMainControls"' 'Height="40" Name="BorderFloatingBarMainControls"' 'floatbar 高度 36 -> 40'
Edit $mw 'Name="BorderFloatingBarMainControls" Background="{DynamicResource FloatBarBackground}" CornerRadius="18"' 'Name="BorderFloatingBarMainControls" Background="{DynamicResource FloatBarBackground}" CornerRadius="14"' 'floatbar 圆角 18 -> 14'
Edit $mw 'x:Name="BorderFloatingBarEmoji" FlowDirection="LeftToRight" Width="36" Height="36" CornerRadius="18"' 'x:Name="BorderFloatingBarEmoji" FlowDirection="LeftToRight" Width="32" Height="32" CornerRadius="16"' 'floatbar emoji 按钮 36 -> 32'
$shadow = '<Border.Effect>' + $NL + '                    <DropShadowEffect BlurRadius="14" ShadowDepth="2" Direction="270" Opacity="0.35" Color="#000000" />' + $NL + '                </Border.Effect>' + $NL + '                '
Edit $mw '<ui:SimpleStackPanel Name="StackPanelFloatingBar"' ($shadow + '<ui:SimpleStackPanel Name="StackPanelFloatingBar"') 'floatbar 投影'
Edit $mw 'Name="StackPanelCanvacMain" Margin="10,5,0,5" Spacing="10"' 'Name="StackPanelCanvacMain" Margin="8,2,0,2" Spacing="8"' 'floatbar 间距收紧(主组)'
Edit $mw 'Name="StackPanelCanvasControls" Visibility="Visible" Margin="10,5,0,5" Spacing="10"' 'Name="StackPanelCanvasControls" Visibility="Visible" Margin="8,2,0,2" Spacing="8"' 'floatbar 间距收紧(画布组)'

Write-Host "== PHASE N5: 设置页新增「重启应用 / 退出应用」 =="
$panel6 = '<StackPanel x:Name="Panel6" Visibility="Collapsed">'
$appCtrl = @'
<Border Style="{StaticResource StbCard}">
                                <StackPanel>
                                    <TextBlock Text="应用控制" Style="{StaticResource StbCardTitle}" />
                                    <TextBlock Text="重启会先保存当前设置，再启动一个新实例并退出当前进程" Style="{StaticResource StbCardDesc}" />
                                    <StackPanel Orientation="Horizontal" Margin="0,10,0,0">
                                        <Button Content="重启应用" Style="{StaticResource StbLink}" Click="BtnRestartApp_Click" />
                                        <Button Content="退出应用" Style="{StaticResource StbLink}" Click="BtnExitApp_Click" />
                                    </StackPanel>
                                </StackPanel>
                            </Border>
'@
Edit $setXaml $panel6 ($panel6 + $NL + "                            " + $appCtrl.Trim("`r", "`n")) '设置页 应用控制卡片'

$homeHandler = '        private void BtnOpenHome_Click(object sender, RoutedEventArgs e)'
$appHandlers = @'
        /// <summary>重启应用：先落盘设置，再起一个新进程，最后退出当前进程。</summary>
        private void BtnRestartApp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Save();
                string exe = Process.GetCurrentProcess().MainModule.FileName;
                Process.Start(new ProcessStartInfo { FileName = exe, UseShellExecute = true });
            }
            catch
            {
            }
            Application.Current.Shutdown();
        }

        /// <summary>退出应用：保存设置后关闭整个程序。</summary>
        private void BtnExitApp_Click(object sender, RoutedEventArgs e)
        {
            Save();
            Application.Current.Shutdown();
        }

'@
Edit $setCs $homeHandler ($appHandlers + $homeHandler) '设置页 重启/退出处理器'

Write-Host "== PHASE N7: PPT 更灵敏（更快识别放映 / 翻页联动） =="
$mwCs = Join-Path $app 'MainWindow.xaml.cs'
Edit $mwCs 'timerCheckPPT.Interval = 1000;' 'timerCheckPPT.Interval = 300;' 'PPT 轮询 1000ms -> 300ms'

$mps = Join-Path $app 'MainWindow.PresentationAndSettings.cs'
$pptGuardOld = 'if (IsShowingRestoreHiddenSlidesWindow) return;'
$pptGuardNew = $pptGuardOld + $NL + '                if (Process.GetProcessesByName("POWERPNT").Length == 0 && Process.GetProcessesByName("wpp").Length == 0) return;'
Edit $mps $pptGuardOld $pptGuardNew 'PPT 轮询先判断进程（省掉 COM 空转）'

Write-Host "== PHASE N8: 浮动栏「更多颜色」调色板 + 统一图标尺寸 =="
$penPopupLines = @(
    '<Popup x:Name="PopupPenPalette" PlacementTarget="{Binding ElementName=FontIconPenPalette}" Placement="Bottom"',
    '               StaysOpen="False" AllowsTransparency="True" PopupAnimation="Fade" VerticalOffset="6">',
    '            <Border Background="#FF161D18" BorderBrush="#40FFFFFF" BorderThickness="1" CornerRadius="10" Padding="10,9">',
    '                <StackPanel Width="212">',
    '                    <TextBlock Text="画笔颜色 · 更多" FontSize="10" Foreground="#99E8EBEF" Margin="2,0,0,5" />',
    '                    <UniformGrid Columns="6" Rows="2">',
    '                        <Border Width="26" Height="26" Margin="2" CornerRadius="13" Background="#FF000000" Tag="#FF000000" MouseUp="BtnPenPaletteColor_MouseUp" ToolTip="黑" Cursor="Hand" />',
    '                        <Border Width="26" Height="26" Margin="2" CornerRadius="13" Background="#FFFEFEFE" Tag="#FFFEFEFE" MouseUp="BtnPenPaletteColor_MouseUp" ToolTip="白" Cursor="Hand" />',
    '                        <Border Width="26" Height="26" Margin="2" CornerRadius="13" Background="#FFD32F2F" Tag="#FFD32F2F" MouseUp="BtnPenPaletteColor_MouseUp" ToolTip="红" Cursor="Hand" />',
    '                        <Border Width="26" Height="26" Margin="2" CornerRadius="13" Background="#FFF57C00" Tag="#FFF57C00" MouseUp="BtnPenPaletteColor_MouseUp" ToolTip="橙" Cursor="Hand" />',
    '                        <Border Width="26" Height="26" Margin="2" CornerRadius="13" Background="#FFF7C948" Tag="#FFF7C948" MouseUp="BtnPenPaletteColor_MouseUp" ToolTip="黄" Cursor="Hand" />',
    '                        <Border Width="26" Height="26" Margin="2" CornerRadius="13" Background="#FF2E9E5B" Tag="#FF2E9E5B" MouseUp="BtnPenPaletteColor_MouseUp" ToolTip="绿" Cursor="Hand" />',
    '                        <Border Width="26" Height="26" Margin="2" CornerRadius="13" Background="#FF00A3A3" Tag="#FF00A3A3" MouseUp="BtnPenPaletteColor_MouseUp" ToolTip="青" Cursor="Hand" />',
    '                        <Border Width="26" Height="26" Margin="2" CornerRadius="13" Background="#FF1F6FC4" Tag="#FF1F6FC4" MouseUp="BtnPenPaletteColor_MouseUp" ToolTip="蓝" Cursor="Hand" />',
    '                        <Border Width="26" Height="26" Margin="2" CornerRadius="13" Background="#FF7B5BD6" Tag="#FF7B5BD6" MouseUp="BtnPenPaletteColor_MouseUp" ToolTip="紫" Cursor="Hand" />',
    '                        <Border Width="26" Height="26" Margin="2" CornerRadius="13" Background="#FFE45C9A" Tag="#FFE45C9A" MouseUp="BtnPenPaletteColor_MouseUp" ToolTip="粉" Cursor="Hand" />',
    '                        <Border Width="26" Height="26" Margin="2" CornerRadius="13" Background="#FF8D6E63" Tag="#FF8D6E63" MouseUp="BtnPenPaletteColor_MouseUp" ToolTip="棕" Cursor="Hand" />',
    '                        <Border Width="26" Height="26" Margin="2" CornerRadius="13" Background="#FF9AA0A6" Tag="#FF9AA0A6" MouseUp="BtnPenPaletteColor_MouseUp" ToolTip="灰" Cursor="Hand" />',
    '                    </UniformGrid>',
    '                    <TextBlock Text="点一下即生效；非内置色时六色勾选会自动清空" FontSize="9" Foreground="#80E8EBEF" Margin="2,6,0,0" />',
    '                </StackPanel>',
    '            </Border>',
    '        </Popup>'
)
$penPopup = ($penPopupLines -join $NL) + $NL + '        '
Edit $mw '<Viewbox Name="ViewboxFloatingBar"' ($penPopup + '<Viewbox Name="ViewboxFloatingBar"') 'floatbar 更多颜色 Popup'

$penIconLines = @(
    '<ui:FontIcon Name="FontIconPenPalette" Glyph="&#xE790;" FontSize="18" Cursor="Hand"',
    '                                         Foreground="{DynamicResource FloatBarForeground}"',
    '                                         ToolTip="更多画笔颜色" MouseUp="FontIconPenPalette_MouseUp" />'
)
$penIcon = ($penIconLines -join $NL) + $NL + '                            '
$colorGroup = '<ui:SimpleStackPanel Orientation="{Binding ElementName=StackPanelFloatingBar, Path=Orientation}" Spacing="8" Margin="0,1">'
Edit $mw $colorGroup ($penIcon + $colorGroup) 'floatbar 调色板图标'

Edit $mw '<ui:SymbolIcon Symbol="Setting" Foreground="{DynamicResource FloatBarForeground}" MouseUp="SymbolIconSettings_Click" />' '<ui:SymbolIcon Symbol="Setting" FontSize="18" Foreground="{DynamicResource FloatBarForeground}" MouseUp="SymbolIconSettings_Click" />' '统一图标尺寸: 设置'
Edit $mw '<ui:SymbolIcon Symbol="TouchPointer" Foreground="{DynamicResource FloatBarForeground}" />' '<ui:SymbolIcon Symbol="TouchPointer" FontSize="18" Foreground="{DynamicResource FloatBarForeground}" />' '统一图标尺寸: 触控'

# 光标图标块含换行，先把 MainWindow.xaml 统一成 LF 再改，避免 CRLF/LF 差异导致锚点落空
$tmpMw = [IO.File]::ReadAllText($mw) -replace "`r`n", "`n"
[IO.File]::WriteAllText($mw, $tmpMw, $utf8)
$lf = "`n"
$cursorOld = '<controls:ClickSensitiveGrid Click="SymbolIconCursor_Click" Margin="-10,0,0,0">' + $lf + '                                <ui:SymbolIcon Symbol="Edit" Foreground="{DynamicResource FloatBarForeground}" />' + $lf + '                            </controls:ClickSensitiveGrid>'
$cursorNew = '<controls:ClickSensitiveGrid Click="SymbolIconCursor_Click" Margin="-10,0,0,0">' + $lf + '                                <ui:SymbolIcon Symbol="Edit" FontSize="18" Foreground="{DynamicResource FloatBarForeground}" />' + $lf + '                            </controls:ClickSensitiveGrid>'
Edit $mw $cursorOld $cursorNew '统一图标尺寸: 光标/笔'

Write-Host "== PHASE N6: 断言 =="
Assert (Select-String -LiteralPath $mw -Pattern 'MouseUp="SymbolIconComponents_MouseUp"' -Quiet) '浮动栏组件入口未插入'
Assert (Select-String -LiteralPath $mw -Pattern 'Click="BtnComponents_Click"' -Quiet) '左栏组件按钮未插入'
Assert (Select-String -LiteralPath $mw -Pattern '<DropShadowEffect' -Quiet) '浮动栏投影未插入'
Assert (Select-String -LiteralPath (Join-Path $app 'MainWindow.Components.cs') -Pattern 'StbComponentDock.ShowFor' -Quiet) '组件入口处理器未写入'
Assert (Select-String -LiteralPath $setXaml -Pattern 'Click="BtnRestartApp_Click"' -Quiet) '设置页重启按钮未插入'
Assert (Select-String -LiteralPath $setCs -Pattern 'BtnExitApp_Click' -Quiet) '设置页退出处理器未写入'
Assert (Test-Path -LiteralPath (Join-Path $app 'StbComponents.cs')) '组件源码未拷入'
Assert (Test-Path -LiteralPath (Join-Path $app 'MiniBoardWindow.Pens.cs')) '板中板调色板代码未拷入'
Assert (Select-String -LiteralPath (Join-Path $app 'MainWindow.xaml.cs') -Pattern 'timerCheckPPT.Interval = 300;' -Quiet) 'PPT 轮询间隔未改'
Assert (Select-String -LiteralPath (Join-Path $app 'MainWindow.PresentationAndSettings.cs') -Pattern 'GetProcessesByName\("POWERPNT"\)' -Quiet) 'PPT 进程预判未插入'
Assert (Select-String -LiteralPath $mw -Pattern 'PopupPenPalette' -Quiet) '浮动栏调色板 Popup 未插入'
Assert (Select-String -LiteralPath $mw -Pattern 'FontIconPenPalette' -Quiet) '浮动栏调色板图标未插入'
Assert (Test-Path -LiteralPath (Join-Path $app 'MainWindow.PenPalette.cs')) '调色板处理代码未拷入'
Write-Host 'OK  PHASE N 校验通过'

Write-Host "STB COMPONENTS PATCHES DONE"
