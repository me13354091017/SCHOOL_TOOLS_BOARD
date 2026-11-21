# apply-stb-fix33.ps1
# 1.0.1.40：真机测试暴露的三处问题。
#   1) 轻量翻页送键改成 PostMessage 直投放映窗（实测 WPS 的 Qt5QWindowIcon 放映窗只认这个，
#      SetForegroundWindow + keybd_event 会被前台锁忽略 —— 这正是“点下一页，放映没动、只有页码变了”的根因）。
#   2) 兜底找放映窗：类名 / Qt 窗口 + 标题。
#   3) 修退出崩溃：ShowNotification 的后台线程在程序退出后仍会 Dispatcher.Invoke，
#      此时 Application.Current 为空 → NullReferenceException → 进程直接退出。
#   4) 进程被结束 / 崩溃 / 先关文稿再退出：按用户要求不收回批注、不清 COM，只把轻量批注标记与
#      上游界面状态对齐，避免页码残留。
param([string]$Root = "plus-src", [string]$Version = "1.0.0.0")

$ErrorActionPreference = 'Stop'
trap { Write-Host ("::error::fix33: " + $_.Exception.Message); throw }

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

# ---------- P1 覆盖轻量翻页实现（PostMessage 送键 + Qt 窗口兜底） ----------
$src = Join-Path $PSScriptRoot 'extra/MainWindow.PptPage.cs'
Assert (Test-Path -LiteralPath $src) 'missing extra/MainWindow.PptPage.cs'
Copy-Item -LiteralPath $src -Destination (Join-Path $app 'MainWindow.PptPage.cs') -Force
Write-Host 'COPIED MainWindow.PptPage.cs (PostMessage)'

# ---------- P2 进程没了：只对齐轻量批注标记 ----------
$pw = Join-Path $app 'MainWindow.PptWatch.cs'
$t = ReadText $pw
$old = @'
                if (pids.Count == 0)
                {
                    // 1.0.1.39：进程没了不做任何复位（不收批注、不清 COM、不重启定时器）
                    _stbPptMiss = 0;
                    return;
                }
'@
$new = @'
                if (pids.Count == 0)
                {
                    // 1.0.1.40：进程没了不做复位（不收批注、不清 COM、不重启定时器）；
                    // 上游 PresentationClose 若已还原界面，这里只把轻量批注标记对齐，避免页码残留
                    if (_stbPptLight && !StbPptUiActive())
                    {
                        _stbPptLight = false;
                    }
                    _stbPptMiss = 0;
                    return;
                }
'@
$t = ReplaceOnce $t $old $new 'pids zero sync'
WriteText $pw $t
Write-Host 'PATCHED pids zero -> light flag sync'

# ---------- P3 修 ShowNotification 退出时崩溃 ----------
$ft = Join-Path $app 'MainWindow.Features.cs'
$t = ReadText $ft
$m1 = @'
            new Thread(new ThreadStart(() =>
            {
                Thread.Sleep(notificationShowTime + 200);
'@
$r1 = @'
            new Thread(new ThreadStart(() =>
            {
                try
                {
                Thread.Sleep(notificationShowTime + 200);
'@
$t = ReplaceOnce $t $m1 $r1 'notification try open'

$m2 = @'
                if (Environment.TickCount - lastNotificationShowTime >= notificationShowTime)
                {
                    Application.Current.Dispatcher.Invoke(() =>
'@
$r2 = @'
                if (Environment.TickCount - lastNotificationShowTime >= notificationShowTime)
                {
                    // 1.0.1.40：退出后 Application.Current 为空，再 Invoke 会空引用把进程带崩
                    var stbApp = Application.Current;
                    if (stbApp == null)
                    {
                        return;
                    }
                    stbApp.Dispatcher.Invoke(() =>
'@
$t = ReplaceOnce $t $m2 $r2 'notification null guard'

$m3 = @'
                        //})).Start();
                    });
                }
            })).Start();
'@
$r3 = @'
                        //})).Start();
                    });
                }
                }
                catch
                {
                }
            })).Start();
'@
$t = ReplaceOnce $t $m3 $r3 'notification try close'
WriteText $ft $t
Write-Host 'PATCHED ShowNotification null guard'

# ---------- 验证 ----------
$g = ReadText (Join-Path $app 'MainWindow.PptPage.cs')
Assert ($g.Contains('StbPostMessage')) 'postmessage key'
Assert ($g.Contains('qt5qwindowicon')) 'qt window fallback'
$p2 = ReadText $pw
Assert ($p2.Contains('1.0.1.40：进程没了不做复位')) 'pids sync comment'
$f2 = ReadText $ft
Assert ($f2.Contains('var stbApp = Application.Current;')) 'notification guard'
Assert ($f2.Contains('stbApp.Dispatcher.Invoke(')) 'notification invoke'
Write-Host 'OK fix33 verified'
