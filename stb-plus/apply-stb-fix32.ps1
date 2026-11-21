# apply-stb-fix32.ps1
# 1.0.1.39：修 PPT 翻页（按钮不生效 / 崩溃 / 笔记不跟着翻页），并按要求去掉「进程消失就收回批注 + 重建 COM」的复位。
#   1) 去掉进程消失复位：常驻探测发现 PowerPoint/WPS 进程全没了时什么都不做（不再收批注、不再清 COM）。
#   2) 翻页不再强依赖 COM：无放映窗时走轻量翻页（本页笔迹入缓存 -> 切页 -> 载入目标页笔迹 -> 给放映窗发 PageUp/PageDown）。
#   3) 修崩溃：上游 SaveScreenShot 那行没包 try/catch，pptApplication 为空 / 无放映窗时点按钮直接崩；现改为先判 COM 可用，再把该行包进 try/catch。
#   4) 修「笔记停在上一页」：上游 SlideShowBegin 把 previousSlideID 置 0，第一页笔迹被存进 0 号槽，回第一页永远是空；改成为当前放映页。
param([string]$Root = "plus-src", [string]$Version = "1.0.0.0")

$ErrorActionPreference = 'Stop'
trap { Write-Host ("::error::fix32: " + $_.Exception.Message); throw }

$app = Join-Path $Root 'Ink Canvas'
function Assert([bool]$c, [string]$m) { if (-not $c) { throw ("ANCHOR FAIL: " + $m) } }
function ReadText([string]$p) { return (Get-Content -LiteralPath $p -Raw) }
function WriteText([string]$p, [string]$t) { [IO.File]::WriteAllText($p, $t, (New-Object System.Text.UTF8Encoding($false))) }
function ReplaceOnce([string]$t, [string]$old, [string]$new, [string]$tag) {
    $i = $t.IndexOf($old)
    Assert ($i -ge 0) ($tag + ' missing')
    Assert ($t.IndexOf($old, $i + 1) -lt 0) ($tag + ' not unique')
    return $t.Substring(0, $i) + $new + $t.Substring($i + $old.Length)
}

$src = Join-Path $PSScriptRoot 'extra/MainWindow.PptPage.cs'
Assert (Test-Path -LiteralPath $src) 'missing extra/MainWindow.PptPage.cs'
Copy-Item -LiteralPath $src -Destination (Join-Path $app 'MainWindow.PptPage.cs') -Force
Write-Host 'COPIED MainWindow.PptPage.cs'

$ps = Join-Path $app 'MainWindow.PresentationAndSettings.cs'
$t = ReadText $ps

$old = @'
                slidescount = Wn.Presentation.Slides.Count;
                previousSlideID = 0;
                memoryStreams = new MemoryStream[slidescount + 2];
'@
$new = @'
                slidescount = Wn.Presentation.Slides.Count;
                // 1.0.1.39：原来这里置 0，第一页笔迹会被存进 0 号槽，回到第一页永远是空
                previousSlideID = Wn.View.CurrentShowPosition;
                currentShowPosition = previousSlideID;
                memoryStreams = new MemoryStream[slidescount + 2];
'@
$t = ReplaceOnce $t $old $new 'begin previous slide id'

$old = @'
        private void BtnPPTSlidesUp_Click(object sender, RoutedEventArgs e)
        {
            if (currentMode == 1)
            {
                GridBackgroundCover.Visibility = Visibility.Collapsed;
                currentMode = 0;
            }

            _isPptClickingBtnTurned = true;
            if (inkCanvas.Strokes.Count > Settings.Automation.MinimumAutomationStrokeNumber &&
                Settings.PowerPointSettings.IsAutoSaveScreenShotInPowerPoint)
                SaveScreenShot(true, pptApplication.SlideShowWindows[1].Presentation.Name + "/" + pptApplication.SlideShowWindows[1].View.CurrentShowPosition);
'@
$new = @'
        private void BtnPPTSlidesUp_Click(object sender, RoutedEventArgs e)
        {
            if (currentMode == 1)
            {
                GridBackgroundCover.Visibility = Visibility.Collapsed;
                currentMode = 0;
            }

            _isPptClickingBtnTurned = true;
            if (!StbPptComReady())
            {
                // 1.0.1.39：没有 COM 放映窗（WPS / COM 断开）：本页笔迹入缓存、切到上一页、载入上页笔迹，再给放映窗发 PageUp
                StbPptLightTurn(-1);
                return;
            }
            try
            {
                if (inkCanvas.Strokes.Count > Settings.Automation.MinimumAutomationStrokeNumber &&
                    Settings.PowerPointSettings.IsAutoSaveScreenShotInPowerPoint)
                    SaveScreenShot(true, pptApplication.SlideShowWindows[1].Presentation.Name + "/" + pptApplication.SlideShowWindows[1].View.CurrentShowPosition);
            }
            catch
            {
            }
'@
$t = ReplaceOnce $t $old $new 'slides up light turn'

$old = @'
        private void BtnPPTSlidesDown_Click(object sender, RoutedEventArgs e)
        {
            if (currentMode == 1)
            {
                GridBackgroundCover.Visibility = Visibility.Collapsed;
                currentMode = 0;
            }
            _isPptClickingBtnTurned = true;
            if (inkCanvas.Strokes.Count > Settings.Automation.MinimumAutomationStrokeNumber &&
                Settings.PowerPointSettings.IsAutoSaveScreenShotInPowerPoint)
                SaveScreenShot(true, pptApplication.SlideShowWindows[1].Presentation.Name + "/" + pptApplication.SlideShowWindows[1].View.CurrentShowPosition);
'@
$new = @'
        private void BtnPPTSlidesDown_Click(object sender, RoutedEventArgs e)
        {
            if (currentMode == 1)
            {
                GridBackgroundCover.Visibility = Visibility.Collapsed;
                currentMode = 0;
            }
            _isPptClickingBtnTurned = true;
            if (!StbPptComReady())
            {
                // 1.0.1.39：没有 COM 放映窗（WPS / COM 断开）：本页笔迹入缓存、切到下一页、载入下页笔迹，再给放映窗发 PageDown
                StbPptLightTurn(1);
                return;
            }
            try
            {
                if (inkCanvas.Strokes.Count > Settings.Automation.MinimumAutomationStrokeNumber &&
                    Settings.PowerPointSettings.IsAutoSaveScreenShotInPowerPoint)
                    SaveScreenShot(true, pptApplication.SlideShowWindows[1].Presentation.Name + "/" + pptApplication.SlideShowWindows[1].View.CurrentShowPosition);
            }
            catch
            {
            }
'@
$t = ReplaceOnce $t $old $new 'slides down light turn'
WriteText $ps $t
Write-Host 'PATCHED ppt slides -> light turn + crash guard'

$pw = Join-Path $app 'MainWindow.PptWatch.cs'
$t = ReadText $pw
$marker = '        /// 1.0.1.37：放映识别换成 GitHub'
$k = $t.IndexOf($marker)
Assert ($k -ge 0) 'watch doc marker missing'
$i = $t.LastIndexOf('        /// <summary>', $k)
Assert ($i -ge 0) 'watch doc start missing'
$endTag = '        /// <summary>放映中把批注窗重新抬到最上层'
$j = $t.IndexOf($endTag)
Assert ($j -gt $i) 'watch tick end missing'

$newTick = @'
        /// <summary>
        /// 1.0.1.39：放映识别沿用 1.0.1.37 的三重判据（类名 / 进程 + 标题 / 真全屏且非最大化）。
        /// 按要求去掉了「进程被结束、崩溃、先关文稿再退出就收回批注并重建 COM」的复位：
        /// 进程全没了这里什么都不做，批注是否退出只看放映窗还在不在。
        /// </summary>
        private void StbPptWatchTick()
        {
            if (!IsLoaded)
            {
                return;
            }
            if (!Settings.PowerPointSettings.PowerPointSupport)
            {
                return;
            }

            try
            {
                HashSet<int> pids = StbPptPids();
                if (pids.Count == 0)
                {
                    // 1.0.1.39：进程没了不做任何复位（不收批注、不清 COM、不重启定时器）
                    _stbPptMiss = 0;
                    return;
                }

                bool isWps;
                IntPtr showHwnd;
                bool showing = StbDetectPptShow(out isWps, out showHwnd);
                if (showHwnd != IntPtr.Zero)
                {
                    _stbPptShowHwnd = showHwnd;
                }

                int comShows = StbPptComShowCount();
                if (comShows >= 1)
                {
                    // COM 明确报告有放映窗：权威信号
                    showing = true;
                }

                if (pptApplication == null && !timerCheckPPT.Enabled)
                {
                    // 让上游定时器重新建立 COM 连接（它连上后会自己停）
                    timerCheckPPT.Start();
                }

                if (showing)
                {
                    _stbPptMiss = 0;
                    if (!StbPptUiActive() && !_stbPptLight)
                    {
                        if (comShows >= 1)
                        {
                            try
                            {
                                PptApplication_SlideShowBegin(pptApplication.SlideShowWindows[1]);
                            }
                            catch
                            {
                            }
                        }
                        if (!StbPptUiActive() && !_stbPptLight)
                        {
                            // COM 拿不到（WPS / 权限隔离 / 保护视图）：用轻量批注 + 轻量翻页
                            StbPptEnterLight(isWps);
                            StbPptLightResetPages();
                        }
                    }
                    else
                    {
                        StbPptKeepTop();
                    }
                    return;
                }

                // 没在放映：连续两拍（约 1 秒）确认后退出批注
                _stbPptMiss++;
                if (_stbPptMiss < 2)
                {
                    return;
                }
                _stbPptMiss = 0;
                if (_stbPptLight)
                {
                    _stbPptLight = false;
                    StbPptExitLight();
                    try
                    {
                        ChangeLockSmithState(lockSmithDesktop);
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }
        }

'@
$t = $t.Substring(0, $i) + $newTick + $t.Substring($j)
WriteText $pw $t
Write-Host 'PATCHED watch -> no process reset, page reset on light'

$s2 = ReadText $ps
Assert ($s2.Contains('previousSlideID = Wn.View.CurrentShowPosition;')) 'begin slide id'
Assert ($s2.Contains('StbPptLightTurn(-1)')) 'up light turn'
Assert ($s2.Contains('StbPptLightTurn(1)')) 'down light turn'
Assert ($s2.Contains('StbPptComReady()')) 'com ready guard'
$p2 = ReadText $pw
Assert ($p2.Contains('1.0.1.39：进程没了不做任何复位')) 'process reset removed'
Assert ($p2.Contains('StbPptLightResetPages()')) 'page reset call'
Assert ($p2.Contains('StbDetectPptShow')) 'detect kept'
$g = ReadText (Join-Path $app 'MainWindow.PptPage.cs')
Assert ($g.Contains('StbPptLightTurn')) 'page file turn'
Assert ($g.Contains('StbPptSendShowKey')) 'page file key'
Write-Host 'OK fix32 verified'
