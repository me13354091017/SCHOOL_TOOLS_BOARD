# apply-stb-fix35.ps1
# 1.0.1.42：修「无法结束放映」。
#   原版 End 按钮只调 pptApplication.SlideShowWindows[1].View.Exit()，没有 COM（WPS 轻量批注 / COM 断开）时
#   异常被 catch 吞掉，表现就是点了没反应。现在：无 COM 直接给放映窗投 Esc 并手动还原界面；有 COM 仍走 View.Exit，
#   失败时同样降级成 Esc + 还原。Esc 也从「抢前台 + keybd_event」改成 PostMessage（实测只有它能让 WPS 放映窗响应）。
param([string]$Root = "plus-src", [string]$Version = "1.0.0.0")

$ErrorActionPreference = 'Stop'
trap { Write-Host ("::error::fix35: " + $_.Exception.Message); throw }

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

# ---------- P1 Esc 改成 PostMessage ----------
$pe = Join-Path $app 'MainWindow.PptWatchEnd.cs'
$t = ReadText $pe
$old = @'
                IntPtr h = StbPptFindShowHwnd();
                if (h == IntPtr.Zero)
                {
                    return;
                }
                StbSetForegroundWindow(h);
                System.Threading.Thread.Sleep(120);
                StbKeybdEvent(0x1B, 0, 0, UIntPtr.Zero);
                StbKeybdEvent(0x1B, 0, 2, UIntPtr.Zero);
                System.Threading.Thread.Sleep(60);
                StbKeybdEvent(0x1B, 0, 0, UIntPtr.Zero);
                StbKeybdEvent(0x1B, 0, 2, UIntPtr.Zero);
'@
$new = @'
                // 1.0.1.42：WPS 的 Qt 放映窗只认 PostMessage，抢前台 + keybd_event 会被前台锁忽略
                StbPptSendShowKey(0x1B);
                System.Threading.Thread.Sleep(120);
                StbPptSendShowKey(0x1B);
'@
$t = ReplaceOnce $t $old $new 'esc postmessage'
WriteText $pe $t

# ---------- P2 结束放映按钮：无 COM 时 Esc + 还原界面 ----------
$ps = Join-Path $app 'MainWindow.PresentationAndSettings.cs'
$t = ReadText $ps
$old = @'
        private void BtnPPTSlideShowEnd_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
'@
$new = @'
        private void BtnPPTSlideShowEnd_Click(object sender, RoutedEventArgs e)
        {
            if (!StbPptComReady())
            {
                // 1.0.1.42：没有 COM 放映窗时原版 View.Exit() 会静默失败（点了没反应），这里直接发 Esc 并还原界面
                StbPptSendEsc();
                StbPptExitLight();
                _stbPptLight = false;
                return;
            }
            Application.Current.Dispatcher.Invoke(() =>
'@
$t = ReplaceOnce $t $old $new 'end button light exit'

$old = @'
                    pptApplication.SlideShowWindows[1].View.Exit();
                }
                catch { }
'@
$new = @'
                    pptApplication.SlideShowWindows[1].View.Exit();
                }
                catch
                {
                    // 1.0.1.42：COM 中途断开也降级成 Esc + 还原，避免按钮点了没反应
                    StbPptSendEsc();
                    StbPptExitLight();
                    _stbPptLight = false;
                }
'@
$t = ReplaceOnce $t $old $new 'end button fallback esc'
WriteText $ps $t

# ---------- 验证 ----------
$e2 = ReadText $pe
Assert ($e2.Contains('StbPptSendShowKey(0x1B)')) 'esc assert'
$s2 = ReadText $ps
Assert ($s2.Contains('if (!StbPptComReady())')) 'end guard assert'
Assert ($s2.Contains('_stbPptLight = false;')) 'end light flag assert'
Write-Host 'OK fix35 verified'
