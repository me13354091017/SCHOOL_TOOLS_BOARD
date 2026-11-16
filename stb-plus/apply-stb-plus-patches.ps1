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

Write-Host ("STB PLUS PATCHES DONE ($count patches)")
