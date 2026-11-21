# apply-stb-fix31.ps1
# 1.0.1.38：PPT 的「功能」全部回到原版（Ink Canvas Plus 上游原文），只保留新的放映识别。
#   回退项：
#   · 结束放映按钮：去掉 5 秒静默期、去掉轻量批注的 Esc 兜底，回到上游原文（COM View.Exit()）
#   · 上一页 / 下一页：去掉 1.0.1.34 加的 COM 放映窗守卫，回到上游原文
#   · 放映结束事件：去掉 1.0.1.36 在 SlideShowEnd 里塞的批注界面回收（上游本来就会还原界面）
#   · 常驻探测：不再使用「用户点了结束放映」的静默期，识别只按窗口判据走
# 保留（属于识别）：MainWindow.PptDetect.cs / StbDetectPptShow / 轻量批注 EnterLight·ExitLight / 进程消失复位
param([string]$Root = "plus-src", [string]$Version = "1.0.0.0")

$ErrorActionPreference = 'Stop'
trap { Write-Host ("::error::fix31: " + $_.Exception.Message); throw }

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

$ps = Join-Path $app 'MainWindow.PresentationAndSettings.cs'
$t = ReadText $ps

# ---------- P1 结束放映按钮回到原版 ----------
$old = @'
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
$new = @'
        private void BtnPPTSlideShowEnd_Click(object sender, RoutedEventArgs e)
        {
'@
$t = ReplaceOnce $t $old $new 'end click original'

$old = @'
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
$new = @'
            new Thread(new ThreadStart(() =>
            {
                try
                {
                    pptApplication.SlideShowWindows[1].View.Exit();
                }
                catch { }
            })).Start();
'@
$t = ReplaceOnce $t $old $new 'end exit original'

# ---------- P2 上一页 / 下一页回到原版 ----------
$old = @'
            _isPptClickingBtnTurned = true;
            // 1.0.1.34：无 COM 放映窗（WPS / 轻量批注 / COM 断开）时不要再取 SlideShowWindows[1]，
            // 否则抛 0x8000000B 未处理异常（日志里成片出现）
            if (pptApplication == null || StbPptComShowCount() < 1)
            {
                return;
            }
'@
$new = @'
            _isPptClickingBtnTurned = true;
'@
$t = ReplaceOnce $t $old $new 'slides up original'

$old = @'
            _isPptClickingBtnTurned = true;
            // 1.0.1.34：同上一页，先确认放映窗存在
            if (pptApplication == null || StbPptComShowCount() < 1)
            {
                return;
            }
'@
$new = @'
            _isPptClickingBtnTurned = true;
'@
$t = ReplaceOnce $t $old $new 'slides down original'

# ---------- P3 放映结束事件回到原版 ----------
$old = @'
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
$new = @'
            isEnteredSlideShowEndEvent = true;
'@
$t = ReplaceOnce $t $old $new 'slide show end original'
WriteText $ps $t
Write-Host 'PATCHED ppt functions -> upstream original'

# ---------- P4 常驻探测不再用结束放映静默期 ----------
$pw = Join-Path $app 'MainWindow.PptWatch.cs'
$t = ReadText $pw
$old = 'if (showing && !StbPptInUserEndHold())'
$new = 'if (showing)'
$t = ReplaceOnce $t $old $new 'watch hold removed'
WriteText $pw $t
Write-Host 'PATCHED watch -> no user end hold'

# ---------- 验证 ----------
$s2 = ReadText $ps
Assert (-not $s2.Contains('StbPptBeginUserEnd()')) 'end hold still present'
Assert (-not $s2.Contains('StbPptComShowCount() < 1')) 'com guard still present'
Assert (-not $s2.Contains('StbPptExitLight();')) 'end reset still present'
Assert ($s2.Contains('pptApplication.SlideShowWindows[1].View.Exit();')) 'original exit missing'
$p2 = ReadText $pw
Assert (-not $p2.Contains('StbPptInUserEndHold()')) 'hold still used'
Assert ($p2.Contains('StbDetectPptShow')) 'detect kept'
Write-Host 'OK fix31 verified'
