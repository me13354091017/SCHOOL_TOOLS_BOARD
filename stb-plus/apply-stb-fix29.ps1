# apply-stb-fix29.ps1
# 1.0.1.36：PPT 放映判定回到 1.0.1.29 的做法（只认 COM 放映窗，不再用 Win32 类名/全屏置顶去猜），
# 并补上异常重置：PowerPoint 被任务管理器结束、崩溃、或先关文稿再退出时，把批注界面收回去、COM 重新连。
param([string]$Root = "plus-src", [string]$Version = "1.0.0.0")

$ErrorActionPreference = 'Stop'
trap { Write-Host ("::error::fix29: " + $_.Exception.Message); throw }

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

# ---------- P1 常驻探测只做异常重置 ----------
$pw = Join-Path $app 'MainWindow.PptWatch.cs'
$t = ReadText $pw
$startTag = '        private void StbPptWatchTick()'
$endTag = '        /// <summary>放映中把批注窗重新抬到最上层'
$i = $t.IndexOf($startTag)
$j = $t.IndexOf($endTag)
Assert ($i -ge 0) 'watch tick missing'
Assert ($j -gt $i) 'watch tick end missing'

$newTick = @'
        /// <summary>
        /// 1.0.1.36：放映判定回到 1.0.1.29——只认 COM 放映窗（SlideShowWindows），
        /// 不再用 Win32 类名 / 全屏置顶去猜，编辑窗、最大化窗口、全屏网页都不会被当成放映；
        /// 进入批注仍由上游 PptApplication_SlideShowBegin 负责，这里只做「异常重置」：
        /// PowerPoint 被任务管理器结束、崩溃、先关文稿再退出时，把批注界面收回去，并让 COM 重新连。
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
                    // PowerPoint 全部退出（任务管理器结束 / 崩溃 / 关文稿后退出）：COM 不会再发任何事件
                    if (pptApplication != null)
                    {
                        try { pptApplication = null; } catch { }
                    }
                    if (_stbPptLight || BtnPPTSlideShowEnd.Visibility == Visibility.Visible)
                    {
                        _stbPptLight = false;
                        StbPptExitLight();
                        try { ChangeLockSmithState(lockSmithDesktop); } catch { }
                    }
                    _stbPptMiss = 0;
                    _stbPptStable = 0;
                    _stbPptUserEndUntil = DateTime.MinValue;
                    if (!timerCheckPPT.Enabled)
                    {
                        timerCheckPPT.Start();
                    }
                    return;
                }

                if (pptApplication == null)
                {
                    // 让上游 1 秒定时器重建 COM 并注册放映事件（它连上后会自己停）
                    if (!timerCheckPPT.Enabled)
                    {
                        timerCheckPPT.Start();
                    }
                    return;
                }

                if (StbPptComShowCount() >= 1)
                {
                    _stbPptMiss = 0;
                    _stbPptStable = 0;
                    return;
                }

                // COM 活着但没有放映窗：放映已结束（Esc / 幻灯片浏览 / 关文稿），
                // 上游 SlideShowEnd 没触发时由这里兜底，连续 3 拍（约 2.4 秒）确认
                _stbPptMiss++;
                if (_stbPptMiss < 3)
                {
                    return;
                }
                _stbPptMiss = 0;
                if (_stbPptLight || BtnPPTSlideShowEnd.Visibility == Visibility.Visible)
                {
                    _stbPptLight = false;
                    StbPptExitLight();
                    try { ChangeLockSmithState(lockSmithDesktop); } catch { }
                }
            }
            catch
            {
            }
        }

'@
$t = $t.Substring(0, $i) + $newTick + $t.Substring($j)
WriteText $pw $t
Write-Host 'PATCHED pptwatch -> health only'

# ---------- P2 放映结束时把批注界面收干净 ----------
$ps = Join-Path $app 'MainWindow.PresentationAndSettings.cs'
$t = ReadText $ps
$old = @'
            isEnteredSlideShowEndEvent = true;
'@
$new = @'
            isEnteredSlideShowEndEvent = true;
            // 1.0.1.36：无论 COM 事件是否完整走到最后，先把批注界面收回去
            _stbPptLight = false;
            try
            {
                Application.Current.Dispatcher.Invoke(() => { StbPptExitLight(); });
            }
            catch
            {
            }
'@
$t = ReplaceOnce $t $old $new 'slide show end reset'
WriteText $ps $t
Write-Host 'PATCHED slide show end reset'

# ---------- 验证 ----------
$p2 = ReadText $pw
Assert ($p2.Contains('1.0.1.36：放映判定回到 1.0.1.29')) 'watch rollback comment'
Assert (-not $p2.Contains('if (hasShow && !StbPptInUserEndHold())')) 'scan enter removed'
$s2 = ReadText $ps
Assert ($s2.Contains('StbPptExitLight();')) 'end reset'
Write-Host 'OK fix29 verified'

$fix30 = Join-Path $PSScriptRoot 'apply-stb-fix30.ps1'
Assert (Test-Path -LiteralPath $fix30) 'missing fix30'
& $fix30 -Root $Root -Version $Version
