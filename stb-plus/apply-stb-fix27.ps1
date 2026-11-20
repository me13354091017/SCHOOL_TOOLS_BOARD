# apply-stb-fix27.ps1
# 1.0.1.35：
# 1) 安装目录改成 ClassIsland 式：{app}\app-<ver>-0\ 放程序，{app}\data\ 放配置与日志，{app}\SchoolToolsBoard.exe 为启动器；
# 2) 日志一天一个文件：<data>\Logs\yyyy-MM-dd.log；
# 3) 设置页「高级」新增「日志」卡片，一键打开日志文件夹；
# 4) 修复「无法结束放映 / 结束放映后仍停在批注」：点结束放映先置 5 秒静默期，无 COM 时发 Esc 真正结束放映。
param([string]$Root = "plus-src", [string]$Version = "1.0.0.0")

$ErrorActionPreference = 'Stop'
trap { Write-Host ("::error::fix27: " + $_.Exception.Message); throw }

$app = Join-Path $Root 'Ink Canvas'
$extra = Join-Path $PSScriptRoot 'extra'
function Assert([bool]$c, [string]$m) { if (-not $c) { throw ("ANCHOR FAIL: " + $m) } }
function ReadText([string]$p) { return (Get-Content -LiteralPath $p -Raw) }
function WriteText([string]$p, [string]$t) { [IO.File]::WriteAllText($p, $t, (New-Object System.Text.UTF8Encoding($false))) }
function ReplaceOnce([string]$t, [string]$old, [string]$new, [string]$tag) {
    $i = $t.IndexOf($old)
    Assert ($i -ge 0) ($tag + ' missing')
    Assert ($t.IndexOf($old, $i + 1) -lt 0) ($tag + ' not unique')
    return $t.Substring(0, $i) + $new + $t.Substring($i + $old.Length)
}

# ================= A. 数据目录：安装版写 {app}\data =================
$ap = Join-Path $app 'App.xaml.cs'
$t = ReadText $ap
$old = @'
            if (!StoreHelper.IsStoreApp) RootPath = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
'@
$new = @'
            if (!StoreHelper.IsStoreApp)
            {
                RootPath = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
                // 1.0.1.35：安装版布局 {app}\app-<ver>-0\ + {app}\data\；
                // 只要上一级存在 data 目录，就把配置、日志、存档统一写进 data，程序目录保持只读
                try
                {
                    string stbUp = System.IO.Path.GetFullPath(System.IO.Path.Combine(RootPath, ".."));
                    if (System.IO.Directory.Exists(System.IO.Path.Combine(stbUp, "data")))
                    {
                        RootPath = System.IO.Path.Combine(stbUp, "data") + "\\";
                    }
                }
                catch
                {
                }
            }
'@
$t = ReplaceOnce $t $old $new 'app rootpath'
WriteText $ap $t
Write-Host 'PATCHED App root path'

# ================= B. 日志一天一个文件 =================
$lh = Join-Path $app 'Helpers\LogHelper.cs'
$t = ReadText $lh
$old = @'
        public static string LogFile = "Log.txt";
'@
$new = @'
        public static string LogFile = "Log.txt";

        /// <summary>1.0.1.35：日志目录 &lt;RootPath&gt;\Logs，一天一个 .log。</summary>
        public static string LogDir
        {
            get { return App.RootPath + "Logs\\"; }
        }
'@
$t = ReplaceOnce $t $old $new 'log dir'
$old = @'
                var file = App.RootPath + LogFile;
                if (!Directory.Exists(App.RootPath))
                {
                    Directory.CreateDirectory(App.RootPath);
                }
'@
$new = @'
                string logDir = LogDir;
                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }
                var file = System.IO.Path.Combine(logDir, DateTime.Now.ToString("yyyy-MM-dd") + ".log");
'@
$t = ReplaceOnce $t $old $new 'log daily'
WriteText $lh $t
Write-Host 'PATCHED log daily'

# ================= C. 设置页「高级」加「日志」卡片 =================
$sx = Join-Path $app 'StbSettingsWindow.xaml'
$t = ReadText $sx
$i = $t.IndexOf('StbBtnOpenDataDir_Click')
Assert ($i -ge 0) 'settings data card missing'
$j = $t.IndexOf('</Border>', $i)
Assert ($j -ge 0) 'settings data card close missing'
$card = @'

                            <Border Style="{StaticResource StbCard}">
                                <Grid>
                                    <Grid.ColumnDefinitions><ColumnDefinition Width="*" /><ColumnDefinition Width="Auto" /></Grid.ColumnDefinitions>
                                    <StackPanel Grid.Column="0">
                                        <TextBlock Text="日志" Style="{StaticResource StbCardTitle}" />
                                        <TextBlock Text="一天一个文件，保存在 data\Logs 下（免安装版在程序目录的 Logs）" Style="{StaticResource StbCardDesc}" />
                                    </StackPanel>
                                    <Button Grid.Column="1" Content="打开日志文件夹" Padding="14,6" VerticalAlignment="Center"
                                            Click="StbBtnOpenLogDir_Click" />
                                </Grid>
                            </Border>
'@
$k = $j + '</Border>'.Length
$t = $t.Substring(0, $k) + $card + $t.Substring($k)
WriteText $sx $t
Write-Host 'PATCHED settings log card'

# ================= D. 无法结束放映：点结束放映先静默 + 无 COM 发 Esc =================
$ps = Join-Path $app 'MainWindow.PresentationAndSettings.cs'
$t = ReadText $ps
$old = @'
        private void BtnPPTSlideShowEnd_Click(object sender, RoutedEventArgs e)
        {
            if (_stbPptLight || StbPptComShowCount() < 1)
            {
                _stbPptLight = false;
                StbPptExitLight();
                return;
            }
'@
$new = @'
        private void BtnPPTSlideShowEnd_Click(object sender, RoutedEventArgs e)
        {
            // 1.0.1.35：先进入 5 秒静默期，否则结束瞬间常驻探测又把批注拉回来（点结束放映没反应）
            StbPptBeginUserEnd();
            if (_stbPptLight || StbPptComShowCount() < 1)
            {
                _stbPptLight = false;
                StbPptExitLight();
                StbPptSendEsc();
                return;
            }
'@
$t = ReplaceOnce $t $old $new 'ppt end hold'

$old = @'
            new Thread(new ThreadStart(() =>
            {
                try
                {
                    pptApplication.SlideShowWindows[1].View.Exit();
                }
                catch { }
            })).Start();
'@
$new = @'
            new Thread(new ThreadStart(() =>
            {
                try
                {
                    if (pptApplication == null)
                    {
                        // 1.0.1.35：WPS / 轻量批注场景没有 COM，用 Esc 直接把放映关掉
                        StbPptSendEsc();
                    }
                    else
                    {
                        pptApplication.SlideShowWindows[1].View.Exit();
                    }
                }
                catch { StbPptSendEsc(); }
            })).Start();
'@
$t = ReplaceOnce $t $old $new 'ppt end exit'
WriteText $ps $t
Write-Host 'PATCHED ppt end hold'

# ================= E. 启动器编译 =================
$launcherSrc = Join-Path $PSScriptRoot 'extra\StbLauncher.cs'
Assert (Test-Path -LiteralPath $launcherSrc) 'missing extra/StbLauncher.cs'
$outDir = Join-Path $Root 'packaging\launcher'
if (-not (Test-Path -LiteralPath $outDir)) { New-Item -ItemType Directory -Path $outDir -Force | Out-Null }
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
Assert (Test-Path -LiteralPath $csc) 'csc.exe missing'
$fw = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$icon = Join-Path $app 'Resources\InkCanvas.ico'
$outExe = Join-Path $outDir 'SchoolToolsBoard.exe'
& $csc /nologo /target:winexe /optimize+ "/win32icon:$icon" "/out:$outExe" `
    "/r:$(Join-Path $fw 'System.dll')" "/r:$(Join-Path $fw 'System.Windows.Forms.dll')" $launcherSrc 2>&1 | Write-Host
Assert ($LASTEXITCODE -eq 0) ('csc failed ' + $LASTEXITCODE)
Assert (Test-Path -LiteralPath $outExe) 'launcher exe missing'
Write-Host ('BUILT launcher ' + (Get-Item $outExe).Length + ' bytes')

# ================= F. 安装包布局：程序进 app-<ver>-0，根放启动器 =================
$iss = Join-Path $Root 'packaging\setup.iss'
$t = ReadText $iss
$old = 'Source: "..\Ink Canvas\bin\Release\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs'
$new = 'Source: "..\Ink Canvas\bin\Release\*"; DestDir: "{app}\app-{#MyAppVersion}-0"; Flags: ignoreversion recursesubdirs createallsubdirs' + "`r`n" +
       'Source: "launcher\SchoolToolsBoard.exe"; DestDir: "{app}"; Flags: ignoreversion'
$t = ReplaceOnce $t $old $new 'iss files layout'

$old = @'
[Run]
'@
$new = @'
[Run]
Filename: "{sys}\cmd.exe"; Parameters: "/C if not exist ""{app}\data"" mkdir ""{app}\data"""; Flags: runhidden
Filename: "{sys}\cmd.exe"; Parameters: "/C if exist ""{app}\Settings.json"" if not exist ""{app}\data\Settings.json"" copy /Y ""{app}\Settings.json"" ""{app}\data\Settings.json"""; Flags: runhidden
'@
$t = ReplaceOnce $t $old $new 'iss run migrate'
$t = ReplaceOnce $t 'Settings.json"" ""{app}""' 'Settings.json"" ""{app}\data""' 'iss migrate settings'
$t = ReplaceOnce $t 'Names.txt"" ""{app}""' 'Names.txt"" ""{app}\data""' 'iss migrate names'
WriteText $iss $t
Write-Host 'PATCHED iss layout'

# ================= G. 常驻探测尊重静默期 =================
$pw = Join-Path $app 'MainWindow.PptWatch.cs'
$t = ReadText $pw
$old = @'
                if (hasShow)
'@
$new = @'
                if (hasShow && !StbPptInUserEndHold())
'@
$t = ReplaceOnce $t $old $new 'pptwatch hold'
WriteText $pw $t

$endSrc = Join-Path $extra 'MainWindow.PptWatchEnd.cs'
Assert (Test-Path -LiteralPath $endSrc) 'missing extra/MainWindow.PptWatchEnd.cs'
Copy-Item -LiteralPath $endSrc -Destination (Join-Path $app 'MainWindow.PptWatchEnd.cs') -Force

$logSrc = Join-Path $extra 'StbSettingsWindow.LogDir.cs'
Assert (Test-Path -LiteralPath $logSrc) 'missing extra/StbSettingsWindow.LogDir.cs'
Copy-Item -LiteralPath $logSrc -Destination (Join-Path $app 'StbSettingsWindow.LogDir.cs') -Force
Write-Host 'PATCHED pptwatch hold + end partial + settings logdir partial'

# ================= 验证 =================
$a2 = ReadText $ap
Assert ($a2.Contains('stbUp')) 'rootpath patched'
$l2 = ReadText $lh
Assert ($l2.Contains('LogDir')) 'logdir'
Assert ($l2.Contains('yyyy-MM-dd')) 'log daily name'
$s2 = ReadText $sx
Assert ($s2.Contains('StbBtnOpenLogDir_Click')) 'log card'
$p2 = ReadText $ps
Assert ($p2.Contains('StbPptBeginUserEnd')) 'end hold'
$pw2 = ReadText $pw
Assert ($pw2.Contains('StbPptInUserEndHold')) 'pptwatch hold'
$i2 = ReadText $iss
Assert ($i2.Contains('app-{#MyAppVersion}-0')) 'iss subdir'
Assert ($i2.Contains('launcher\SchoolToolsBoard.exe')) 'iss launcher'
Write-Host 'OK fix27 verified'
