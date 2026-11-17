# apply-stb-floatbar.ps1
# PHASE M：把「设置」「板中板」做成「图标在上、文字在下」的按钮，
# 同时放进顶部浮动栏（新版 UI）与左侧竖栏（旧版 UI），解决「找不到设置入口」。
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

$mw = Join-Path $app 'MainWindow.xaml'
$mbcs = Join-Path $app 'MainWindow.MiniBoard.cs'
$NL = "`r`n"

Write-Host "== PHASE M1: 浮动栏「设置」图标 -> 图标+文字 =="
$oldSetting = '<ui:SymbolIcon Symbol="Setting" Foreground="{DynamicResource FloatBarForeground}" MouseUp="SymbolIconSettings_Click" />'
$newSetting = @'
<StackPanel Background="Transparent" Orientation="Vertical" HorizontalAlignment="Center" VerticalAlignment="Center" MouseUp="SymbolIconSettings_Click" ToolTip="设置">
                                <Viewbox Width="15" Height="15" HorizontalAlignment="Center">
                                    <ui:SymbolIcon Symbol="Setting" Foreground="{DynamicResource FloatBarForeground}" />
                                </Viewbox>
                                <TextBlock Text="设置" FontSize="8" HorizontalAlignment="Center" Margin="0,-1,0,0" Foreground="{DynamicResource FloatBarForeground}" />
                            </StackPanel>
'@
$newSetting = $newSetting.Trim("`r", "`n")
Edit $mw $oldSetting $newSetting 'floatbar 设置 icon+text'

Write-Host "== PHASE M2: 浮动栏新增「板中板」图标+文字 =="
$setAnchor = '<StackPanel Background="Transparent" Orientation="Vertical" HorizontalAlignment="Center" VerticalAlignment="Center" MouseUp="SymbolIconSettings_Click" ToolTip="设置">'
$miniFloat = @'
<StackPanel Background="Transparent" Orientation="Vertical" HorizontalAlignment="Center" VerticalAlignment="Center" MouseUp="SymbolIconMiniBoard_MouseUp" ToolTip="板中板">
                                <Viewbox Width="15" Height="15" HorizontalAlignment="Center">
                                    <ui:SymbolIcon Symbol="Edit" Foreground="{DynamicResource FloatBarForeground}" />
                                </Viewbox>
                                <TextBlock Text="板中板" FontSize="8" HorizontalAlignment="Center" Margin="0,-1,0,0" Foreground="{DynamicResource FloatBarForeground}" />
                            </StackPanel>
'@
$miniFloat = $miniFloat.Trim("`r", "`n")
Edit $mw $setAnchor ($miniFloat + $NL + "                            " + $setAnchor) 'floatbar 板中板 icon+text'

Write-Host "== PHASE M3: 左侧竖栏「设置」按钮 -> 图标+文字 =="
$oldBtnSettings = @'
<Button Name="BtnSettings" Content="设置" FontFamily="Microsoft YaHei UI"
                            Margin="0,10,0,0" Width="{Binding ElementName=StackPanelMain, Path=ActualWidth}"
                            Click="BtnSettings_Click" Foreground="{Binding ElementName=BtnExit, Path=Foreground}" Background="{Binding ElementName=BtnExit, Path=Background}" />
'@
$newBtnSettings = @'
<Button Name="BtnSettings" FontFamily="Microsoft YaHei UI" Padding="0,4,0,4" ToolTip="设置"
                            Margin="0,10,0,0" Width="{Binding ElementName=StackPanelMain, Path=ActualWidth}"
                            Click="BtnSettings_Click" Foreground="{Binding ElementName=BtnExit, Path=Foreground}" Background="{Binding ElementName=BtnExit, Path=Background}">
                        <StackPanel HorizontalAlignment="Center">
                            <Viewbox Width="18" Height="18" HorizontalAlignment="Center">
                                <ui:SymbolIcon Symbol="Setting" Foreground="{Binding ElementName=BtnExit, Path=Foreground}" />
                            </Viewbox>
                            <TextBlock Text="设置" FontSize="10" HorizontalAlignment="Center" Margin="0,-1,0,0" Foreground="{Binding ElementName=BtnExit, Path=Foreground}" />
                        </StackPanel>
                    </Button>
'@
Edit $mw $oldBtnSettings.Trim("`r", "`n") $newBtnSettings.Trim("`r", "`n") 'leftbar 设置 icon+text'

Write-Host "== PHASE M4: 左侧竖栏「板中板」按钮 -> 图标+文字 =="
$oldMini = @'
<Button x:Name="BtnMiniBoardMain" Content="板中板" FontFamily="Microsoft YaHei UI"
                Margin="0,10,0,0" Width="{Binding ElementName=StackPanelMain, Path=ActualWidth}"
                Click="BtnMiniBoard_Click" Foreground="{Binding ElementName=BtnExit, Path=Foreground}" Background="{Binding ElementName=BtnExit, Path=Background}" />
'@
$newMini = @'
<Button x:Name="BtnMiniBoardMain" FontFamily="Microsoft YaHei UI" Padding="0,4,0,4" ToolTip="板中板"
                Margin="0,10,0,0" Width="{Binding ElementName=StackPanelMain, Path=ActualWidth}"
                Click="BtnMiniBoard_Click" Foreground="{Binding ElementName=BtnExit, Path=Foreground}" Background="{Binding ElementName=BtnExit, Path=Background}">
            <StackPanel HorizontalAlignment="Center">
                <Viewbox Width="18" Height="18" HorizontalAlignment="Center">
                    <ui:SymbolIcon Symbol="Edit" Foreground="{Binding ElementName=BtnExit, Path=Foreground}" />
                </Viewbox>
                <TextBlock Text="板中板" FontSize="10" HorizontalAlignment="Center" Margin="0,-1,0,0" Foreground="{Binding ElementName=BtnExit, Path=Foreground}" />
            </StackPanel>
        </Button>
'@
Edit $mw $oldMini.Trim("`r", "`n") $newMini.Trim("`r", "`n") 'leftbar 板中板 icon+text'

Write-Host "== PHASE M5: 补浮动栏「板中板」的 MouseUp 处理器 =="
$mbOld = '        private void BtnMiniBoard_Click(object sender, RoutedEventArgs e)'
$mbNew = @'
        private void SymbolIconMiniBoard_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            MiniBoardWindow.ShowBoard(this);
        }

        private void BtnMiniBoard_Click(object sender, RoutedEventArgs e)
'@
Edit $mbcs $mbOld $mbNew.Trim("`r", "`n") 'miniboard MouseUp handler'

Write-Host "== PHASE M6: 断言 =="
Assert (Select-String -LiteralPath $mw -Pattern 'MouseUp="SymbolIconMiniBoard_MouseUp"' -Quiet) '浮动栏板中板入口未插入'
Assert (Select-String -LiteralPath $mbcs -Pattern 'SymbolIconMiniBoard_MouseUp' -Quiet) '板中板 MouseUp 处理器未写入'
Assert ((Select-String -LiteralPath $mw -Pattern 'Symbol="Setting"' -AllMatches).Matches.Count -ge 2) '设置图标数量不足（应为浮动栏 + 左栏）'
Write-Host 'OK  floatbar + leftbar 图标文字入口校验通过'

Write-Host 'STB FLOATBAR PATCHES DONE'
