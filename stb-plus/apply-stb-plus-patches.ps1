param([string]$Root = "plus-src", [string]$ThemeSrc = "stb-plus/Styles", [string]$Version = "1.0.0.0")

$ErrorActionPreference = 'Stop'
$app = Join-Path $Root 'Ink Canvas'
$utf8 = New-Object System.Text.UTF8Encoding($false)
$count = 0

function Assert([bool]$cond, [string]$msg) {
    if (-not $cond) { throw "ANCHOR FAIL: $msg" }
}

function Edit([string]$path, [string]$old, [string]$new, [string]$label) {
    Assert (Test-Path -LiteralPath $path) "$label :: file missing $path"
    $t = Get-Content -LiteralPath $path -Raw
    Assert ($t.Contains($old)) "$label :: '$old'"
    $t = $t.Replace($old, $new)
    [IO.File]::WriteAllText($path, $t, $utf8)
    $script:count++
    Write-Host ("PATCHED  " + $label)
}

function EditRegex([string]$path, [string]$pattern, [string]$replacement, [string]$label) {
    Assert (Test-Path -LiteralPath $path) "$label :: file missing $path"
    $t = Get-Content -LiteralPath $path -Raw
    $re = [regex]$pattern
    Assert ($re.IsMatch($t)) "$label :: /$pattern/"
    $t = $re.Replace($t, $replacement, 1)
    [IO.File]::WriteAllText($path, $t, $utf8)
    $script:count++
    Write-Host ("PATCHED  " + $label)
}

Write-Host "== PHASE A: 品牌 / 版本 =="
Edit (Join-Path $app 'InkCanvasPlus.csproj') '<AssemblyName>Ink Canvas</AssemblyName>' '<AssemblyName>SchoolToolsBoard</AssemblyName>' 'csproj AssemblyName'

$asm = Join-Path $app 'Properties/AssemblyInfo.cs'
Edit $asm '[assembly: AssemblyTitle("Ink Canvas Plus")]' '[assembly: AssemblyTitle("School Tools Board")]' 'AssemblyTitle'
Edit $asm '[assembly: AssemblyProduct("Ink Canvas Plus")]' '[assembly: AssemblyProduct("School Tools Board")]' 'AssemblyProduct'
Edit $asm '[assembly: AssemblyCompany("Clover Yan")]' '[assembly: AssemblyCompany("SCHOOL_TOOLS")]' 'AssemblyCompany'
EditRegex $asm 'AssemblyVersion\("[0-9.]+"\)' ('AssemblyVersion("' + $Version + '")') 'AssemblyVersion'
EditRegex $asm 'AssemblyFileVersion\("[0-9.]+"\)' ('AssemblyFileVersion("' + $Version + '")') 'AssemblyFileVersion'

Write-Host "== PHASE B: 主窗口标题 / 黑板底 =="
$mwXaml = Join-Path $app 'MainWindow.xaml'
Edit $mwXaml 'Title="Ink Canvas 画板"' 'Title="School Tools Board 课堂白板"' 'MainWindow Title'
Edit $mwXaml 'Name="GridBackgroundCover" Visibility="Collapsed" Background="#1F1F1F"' 'Name="GridBackgroundCover" Visibility="Collapsed" Background="#101A16"' 'GridBackgroundCover 底色'

Write-Host "== PHASE C: 默认主题指向 STB =="
Edit (Join-Path $app 'App.xaml') '<ResourceDictionary Source="Resources/Styles/Light.xaml" />' '<ResourceDictionary Source="Resources/Styles/STBLight.xaml" />' 'App.xaml 默认主题'
EditRegex (Join-Path $app 'Settings.cs') 'public int Theme \{ get; set; \} = \d+;' 'public int Theme { get; set; } = 18;' 'Settings.Appearance.Theme 默认 18(STB/浅)'

$feat = Join-Path $app 'MainWindow.Features.cs'
$t = Get-Content -LiteralPath $feat -Raw
$re = [regex]'(?s)(?<i>[ \t]*)case 5:\s*\r?\n(?<j>[ \t]*)SetTheme\("Yellow", favor\);\s*\r?\n(?<k>[ \t]*)break;'
Assert ($re.IsMatch($t)) 'MainWindow.Features.cs :: case 5 Yellow'
$m = $re.Match($t)
$insert = "`r`n" + $m.Groups['i'].Value + 'case 6:' + "`r`n" + $m.Groups['j'].Value + 'SetTheme("STB", favor);' + "`r`n" + $m.Groups['k'].Value + 'break;'
$t = $re.Replace($t, { param($mt) $mt.Value + $insert }, 1)
[IO.File]::WriteAllText($feat, $t, $utf8)
$count++
Write-Host 'PATCHED  MainWindow.Features.cs case 6 -> STB'

Write-Host "== PHASE D: 全局文案与配色替换 =="
$files = Get-ChildItem -LiteralPath $app -Recurse -File -Include *.cs, *.xaml |
    Where-Object { $_.FullName -notlike '*\Resources\Styles\*' }
$repl = @(
    @('Ink Canvas Plus', 'School Tools Board'),
    @('Ink Canvas 画板', 'School Tools Board 课堂白板'),
    @('#FFF2F2F2', '#FFF7F8F6'),
    @('#FF1A1A1A', '#FF101A16'),
    @('#1F1F1F', '#101A16')
)
foreach ($f in $files) {
    $t = Get-Content -LiteralPath $f.FullName -Raw
    $orig = $t
    foreach ($p in $repl) { $t = $t.Replace($p[0], $p[1]) }
    if ($t -ne $orig) {
        [IO.File]::WriteAllText($f.FullName, $t, $utf8)
        $count++
        Write-Host ("PATCHED  " + $f.Name)
    }
}

Write-Host "== PHASE E: 拷入 STB 主题字典 =="
$stylesDir = Join-Path $app 'Resources/Styles'
Assert (Test-Path -LiteralPath $stylesDir) 'Resources/Styles 目录不存在'
Copy-Item -LiteralPath (Join-Path $ThemeSrc 'STBLight.xaml') -Destination $stylesDir -Force
Copy-Item -LiteralPath (Join-Path $ThemeSrc 'STBDark.xaml') -Destination $stylesDir -Force
$count += 2
Write-Host ("PATCHED  Styles/STBLight.xaml + STBDark.xaml (" + ((Get-ChildItem -LiteralPath $stylesDir -Filter 'STB*.xaml' -File).Count) + " files)")

Write-Host "== PHASE F: 安装包脚本 =="
$iss = Join-Path $Root 'packaging/setup.iss'
Edit $iss '#define MyAppName "Ink Canvas Plus"' '#define MyAppName "School Tools Board"' 'iss MyAppName'
Edit $iss '#define MyAppPublisher "Clover Yan"' '#define MyAppPublisher "SCHOOL_TOOLS"' 'iss MyAppPublisher'
Edit $iss '#define MyAppURL "https://cloveryan.com/apps/Ink-Canvas-Plus"' '#define MyAppURL "https://github.com/me13354091017/SCHOOL_TOOLS_BOARD"' 'iss MyAppURL'
Edit $iss '#define MyAppExeName "Ink Canvas.exe"' '#define MyAppExeName "SchoolToolsBoard.exe"' 'iss MyAppExeName'
EditRegex $iss '#define MyAppVersion GetVersionNumbersString\("[\s\S]*?"\)' ('#define MyAppVersion "' + $Version + '"') 'iss MyAppVersion'
Edit $iss 'AppId={{4A20B75F-FFBE-4CD7-8A77-24BC4926241A}' 'AppId={{7E4A1C3B-0D82-4F57-9A6E-51D2C8B74A10}' 'iss AppId'
Edit $iss 'DefaultDirName={autopf}\{#MyAppPublisher}\{#MyAppName}' 'DefaultDirName={autopf}\{#MyAppPublisher}\SchoolToolsBoard' 'iss DefaultDirName'
Edit $iss 'OutputBaseFilename=Ink-Canvas-Plus_setup_{#MyAppVersion}' 'OutputBaseFilename=SchoolToolsBoard-Setup-{#MyAppVersion}' 'iss OutputBaseFilename'
Edit $iss 'InfoBeforeFile="infobefore.txt"' '; InfoBeforeFile removed by STB patch' 'iss InfoBeforeFile'
Edit $iss 'BeveledLabel=https://cloveryan.com/ic+ {#MyAppVersion}' 'BeveledLabel=SchoolToolsBoard {#MyAppVersion} - based on Ink Canvas Plus' 'iss BeveledLabel'

$t = Get-Content -LiteralPath $iss -Raw
$reFiles = [regex]'(?s)\[Files\][\s\S]*?\r?\n\[Registry\]'
Assert ($reFiles.IsMatch($t)) 'iss [Files] 段'
$newFiles = "[Files]`r`nSource: `"..\Ink Canvas\bin\Release\*`"; DestDir: `"{app}`"; Flags: ignoreversion recursesubdirs createallsubdirs`r`n; NOTE: STB patch - wildcard copy of whole build output`r`n`r`n[Registry]"
$t = $reFiles.Replace($t, $newFiles, 1)
[IO.File]::WriteAllText($iss, $t, $utf8)
$count++
Write-Host 'PATCHED  iss [Files] wildcard'

Write-Host "== PHASE G: 全量链接替换（含 Manual.md / privacy.txt / README） =="
$allFiles = Get-ChildItem -LiteralPath $Root -Recurse -File -Include *.cs, *.xaml, *.md, *.txt, *.xml, *.json |
    Where-Object { $_.FullName -notlike '*\Resources\Styles\*' -and $_.FullName -notlike '*\packaging\output\*' }
$linkRepl = @(
    @('https://cloveryan.com/apps/Ink-Canvas-Plus/changelog', 'https://github.com/me13354091017/SCHOOL_TOOLS_BOARD/releases'),
    @('https://cloveryan.com/apps/Ink-Canvas-Plus', 'https://github.com/me13354091017/SCHOOL_TOOLS_BOARD'),
    @('https://cloveryan.com/ic+', 'https://github.com/me13354091017/SCHOOL_TOOLS_BOARD'),
    @('https://cloveryan.com', 'https://me13354091017.github.io/school_tools'),
    @('cloveryan.com', 'me13354091017.github.io/school_tools'),
    @('https://github.com/clover-tree/Ink-Canvas-Plus', 'https://github.com/me13354091017/SCHOOL_TOOLS_BOARD'),
    @('github.com/clover-tree/Ink-Canvas-Plus', 'github.com/me13354091017/SCHOOL_TOOLS_BOARD'),
    @('clover-tree/Ink-Canvas-Plus', 'me13354091017/SCHOOL_TOOLS_BOARD'),
    @('top.khyan.InkCanvasPlus', 'top.schooltools.SchoolToolsBoard'),
    @('Copyright © Clover Yan 2025; © XY Wang 2023', 'Copyright © SCHOOL_TOOLS 2026; based on Ink Canvas Plus by Clover Yan; © XY Wang 2023')
)
foreach ($f in $allFiles) {
    $t = Get-Content -LiteralPath $f.FullName -Raw
    $orig = $t
    foreach ($p in $linkRepl) { $t = $t.Replace($p[0], $p[1]) }
    if ($t -ne $orig) {
        [IO.File]::WriteAllText($f.FullName, $t, $utf8)
        $count++
        Write-Host ("PATCHED  links in " + $f.Name)
    }
}

Write-Host "== PHASE G2: 自动更新地址 =="
foreach ($f in (Get-ChildItem -LiteralPath $app -Recurse -File -Include *.cs)) {
    $t = Get-Content -LiteralPath $f.FullName -Raw
    $re = [regex]'AutoUpdater\.Start\(\s*"[^"]*"\s*\)'
    if ($re.IsMatch($t)) {
        $t = $re.Replace($t, 'AutoUpdater.Start("https://github.com/me13354091017/SCHOOL_TOOLS_BOARD/releases/latest/download/version.xml")')
        [IO.File]::WriteAllText($f.FullName, $t, $utf8)
        $count++
        Write-Host ("PATCHED  AutoUpdater url in " + $f.Name)
    }
}

Write-Host "== PHASE H: 欢迎界面改写 =="
$welcomeXaml = Join-Path $app 'WelcomeWindow.xaml'
Edit $welcomeXaml 'Title="欢迎使用 School Tools Board"' 'Title="欢迎使用 School Tools Board 课堂白板"' 'WelcomeWindow Title'
Edit $welcomeXaml 'Text="School Tools Board 已更新到 "' 'Text="School Tools Board 课堂白板 已更新到 "' 'WelcomeWindow 版本文案'
Edit $welcomeXaml 'UriSource="pack://application:,,,/Resources/Clover Yan.png"' 'UriSource="pack://application:,,,/Resources/InkCanvas.png"' 'WelcomeWindow 头像换 STB 图标'
Edit $welcomeXaml '<Image Grid.Column="0" Grid.Row="2" Width="128" Height="128" Stretch="UniformToFill">' '<Image Grid.Column="0" Grid.Row="2" Width="128" Height="128" Stretch="UniformToFill" Visibility="Collapsed">' 'WelcomeWindow 隐藏重复 logo'
Edit $welcomeXaml 'Content="https://github.com/me13354091017/SCHOOL_TOOLS_BOARD"' 'Content="项目主页 · SCHOOL_TOOLS"' 'WelcomeWindow 链接按钮文案'

$welcomeCs = @'
using System;
using System.Reflection;
using System.Windows;
using System.Windows.Documents;

namespace InkCanvasPlus
{
    /// <summary>
    /// WelcomeWindow.xaml 的交互逻辑（STB 版本，由 stb-plus 补丁覆盖）
    /// </summary>
    public partial class WelcomeWindow : Window
    {
        public WelcomeWindow()
        {
            InitializeComponent();
            TextBlockVersion.Text = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            TextBlockUpdateContent.Inlines.Add(new Bold(new Run("School Tools Board（STB）课堂白板  v" + TextBlockVersion.Text + "\n")));
            TextBlockUpdateContent.Inlines.Add(new Run("本版把桌面底座换为 Ink Canvas Plus，并完成界面重构。\n\n"));
            TextBlockUpdateContent.Inlines.Add(new Bold(new Run("【界面】")));
            TextBlockUpdateContent.Inlines.Add(new Run("\n· 新增 STB 象牙白 / STB 石墨两套浮动栏配色，启动默认即 STB 浅色\n· 设置页「颜色主题」新增 3 项 STB 主题（浅色 / 深色 / 跟随系统）\n· 黑板底色改为 #101A16、白板底色改为 #F7F8F6，取色常量全量替换\n· 窗口标题、欢迎页、关于信息与图标统一为 School Tools Board\n"));
            TextBlockUpdateContent.Inlines.Add(new Bold(new Run("【说明】")));
            TextBlockUpdateContent.Inlines.Add(new Run("\n· 底座为 Ink Canvas Plus（GPL-3.0，作者 Clover Yan），STB 只在构建前打补丁，不改上游源码\n· IC+ 不包含板中板；需要板中板请使用 v1.1.5（ICC-CE 底座）归档版\n· 自动更新已指向本仓库 Release，下载页见下方链接\n"));
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ButtonShowMore_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/me13354091017/SCHOOL_TOOLS_BOARD/releases");
        }

        private void HyperlinkButtonWebsite_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/me13354091017/SCHOOL_TOOLS_BOARD");
        }
    }
}
'@
[IO.File]::WriteAllText((Join-Path $app 'WelcomeWindow.xaml.cs'), $welcomeCs, $utf8)
$count++
Write-Host 'PATCHED  WelcomeWindow.xaml.cs 全文改写'

Write-Host "== PHASE I: 设置页「颜色主题」新增 STB 三项 =="
$themeAnchor = '<ComboBoxItem Content="Plus 金秋黄 - 跟随系统" FontFamily="Microsoft YaHei UI" />'
$themeNew = $themeAnchor + "`r`n" +
    '                                        <ComboBoxItem Content="STB 象牙白 - 浅色" FontFamily="Microsoft YaHei UI" />' + "`r`n" +
    '                                        <ComboBoxItem Content="STB 石墨 - 深色" FontFamily="Microsoft YaHei UI" />' + "`r`n" +
    '                                        <ComboBoxItem Content="STB - 跟随系统" FontFamily="Microsoft YaHei UI" />'
Edit $mwXaml $themeAnchor $themeNew '设置页 颜色主题 + 3 项 STB'

Write-Host "== PHASE J: 设置面板底色 =="
Edit $mwXaml 'Name="BorderSettings" Background="White" BorderBrush="Black"' 'Name="BorderSettings" Background="#FBFAF7" BorderBrush="#B9B09D"' '设置面板底色'
Edit $mwXaml '<Grid Background="White" Margin="10,0,10,0" Height="60" VerticalAlignment="Top">' '<Grid Background="Transparent" Margin="10,0,10,0" Height="60" VerticalAlignment="Top">' '设置面板内层底色'

Write-Host ("STB PLUS PATCHES DONE ($count patches)")
