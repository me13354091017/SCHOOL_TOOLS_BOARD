# apply-stb-fix30.ps1
# 1.0.1.37：把 PPT 放映识别换成 GitHub 上验证过的实现。
#   参考两份成熟实现（都已读过源码）：
#   · QTimer (Qwejay/QTimer, main.py) 的 is_ppt_slideshow_active：类名 / 进程名 / 标题 / 真全屏 三重判据，
#     真全屏 = 窗口矩形 ≈ 显示器矩形（±6px）且不是最大化窗口 —— 编辑窗最大化不算放映，这是误判的解药。
#   · RimePPT (WRMYX/RimePPT) 的 PowerPointSlideShowWindowWatcher：按放映窗句柄观察，
#     窗口在就进、窗口没了就退、进程没了就整体复位。
#   STB 不依赖 COM 也能跑，所以用「500ms 轮询 + 三重判据」落地；
#   进入批注优先走上游 COM（PptApplication_SlideShowBegin），拿不到 COM 才用轻量批注。
param([string]$Root = "plus-src", [string]$Version = "1.0.0.0")

$ErrorActionPreference = 'Stop'
trap { Write-Host ("::error::fix30: " + $_.Exception.Message); throw }

$app = Join-Path $Root 'Ink Canvas'
function Assert([bool]$c, [string]$m) { if (-not $c) { throw ("ANCHOR FAIL: " + $m) } }
function ReadText([string]$p) { return (Get-Content -LiteralPath $p -Raw) }
function WriteText([string]$p, [string]$t) { [IO.File]::WriteAllText($p, $t, (New-Object System.Text.UTF8Encoding($false))) }

# ---------- P1 新增识别实现 ----------
$src = Join-Path $PSScriptRoot 'extra/MainWindow.PptDetect.cs'
Assert (Test-Path -LiteralPath $src) 'missing extra/MainWindow.PptDetect.cs'
Copy-Item -LiteralPath $src -Destination (Join-Path $app 'MainWindow.PptDetect.cs') -Force
Write-Host 'COPIED MainWindow.PptDetect.cs'

# ---------- P2 常驻探测改为「三重判据识别 + 自动进批注 + 异常复位」 ----------
$pw = Join-Path $app 'MainWindow.PptWatch.cs'
$t = ReadText $pw

$marker = '        /// 1.0.1.36：放映判定回到 1.0.1.29'
$k = $t.IndexOf($marker)
Assert ($k -ge 0) 'old tick doc marker missing'
$i = $t.LastIndexOf('        /// <summary>', $k)
Assert ($i -ge 0) 'old tick doc start missing'
$endTag = '        /// <summary>放映中把批注窗重新抬到最上层'
$j = $t.IndexOf($endTag)
Assert ($j -gt $i) 'watch tick end missing'

$newTick = @'
        /// <summary>
        /// 1.0.1.37：放映识别换成 GitHub 上验证过的实现 —— QTimer 的 is_ppt_slideshow_active
        /// （类名 / 进程名 / 标题 / 真全屏 三重判据）配合 RimePPT 的放映窗观察思路：
        /// 窗口在就进批注、窗口没了就退、进程被结束/崩溃就整体复位。
        /// 不再用「置顶 + 面积 85%」去猜，最大化编辑窗不会被当成放映；
        /// WPS 的 kslideshow / wps_slideshow / wpp_slideshow_class 以及真全屏的 wpp.exe 一并认。
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
                    // PowerPoint / WPS 全部退出（任务管理器结束 / 崩溃 / 先关文稿再退出）
                    if (pptApplication != null)
                    {
                        try
                        {
                            pptApplication = null;
                        }
                        catch
                        {
                        }
                    }
                    if (_stbPptLight || BtnPPTSlideShowEnd.Visibility == Visibility.Visible)
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
                    _stbPptMiss = 0;
                    _stbPptStable = 0;
                    _stbPptShowHwnd = IntPtr.Zero;
                    if (!timerCheckPPT.Enabled)
                    {
                        timerCheckPPT.Start();
                    }
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

                if (showing && !StbPptInUserEndHold())
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
                            // COM 拿不到（WPS / 权限隔离 / 保护视图）：用轻量批注，界面一样能用
                            StbPptEnterLight(isWps);
                        }
                    }
                    else
                    {
                        StbPptKeepTop();
                    }
                    return;
                }

                // 没在放映，或者用户刚按过「结束放映」（静默 5 秒内不自动回批注）
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

# ---------- P3 轮询 800ms -> 500ms，进批注更快 ----------
$t = $t.Replace('TimeSpan.FromMilliseconds(800)', 'TimeSpan.FromMilliseconds(500)')
WriteText $pw $t
Write-Host 'PATCHED ppt watch -> triple-signal detect'

# ---------- 验证 ----------
$p2 = ReadText $pw
Assert ($p2.Contains('StbDetectPptShow')) 'detect call missing'
Assert ($p2.Contains('StbPptEnterLight')) 'light enter missing'
Assert ($p2.Contains('StbPptKeepTop')) 'keeptop missing'
Assert ($p2.Contains('screenclass')) 'screenclass anchor missing'
Assert ($p2.Contains('FromMilliseconds(500)')) 'watch interval'
$d = ReadText (Join-Path $app 'MainWindow.PptDetect.cs')
Assert ($d.Contains('wpp_slideshow_class')) 'wps class list'
Assert ($d.Contains('STB_WS_MAXIMIZE')) 'maximize guard'
Assert ($d.Contains('StbPptIsExactFullscreen')) 'fullscreen guard'
Write-Host 'OK fix30 verified'
