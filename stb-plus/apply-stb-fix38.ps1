# apply-stb-fix38.ps1
# 1.0.1.45：真机发现 PostMessage(WM_CLOSE) 会被 WPS 的 Qt 放映窗忽略，改为 SendMessageTimeout 同步投递作为最后兜底。
param([string]$Root = "plus-src", [string]$Version = "1.0.0.0")

$ErrorActionPreference = 'Stop'
trap { Write-Host ("::error::fix38: " + $_.Exception.Message); throw }

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

$pp = Join-Path $app 'MainWindow.PptPage.cs'
$t = ReadText $pp
$old = @'
        [DllImport("user32.dll")]
        private static extern bool StbPostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
'@
$new = @'
        [DllImport("user32.dll")]
        private static extern bool StbPostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr StbSendMessageTimeout(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam, uint flags, uint timeout, out IntPtr result);
'@
$t = ReplaceOnce $t $old $new 'sendmessagetimeout decl'

$old = @'
                LogHelper.WriteLogToFile("STB force close show hwnd=" + h, LogHelper.LogType.Event);
                StbPostMessage(h, 0x0010, IntPtr.Zero, IntPtr.Zero);
'@
$new = @'
                LogHelper.WriteLogToFile("STB force close show hwnd=" + h, LogHelper.LogType.Event);
                IntPtr stbR;
                StbSendMessageTimeout(h, 0x0010, IntPtr.Zero, IntPtr.Zero, 0x0002, 500, out stbR);
'@
$t = ReplaceOnce $t $old $new 'send wm_close'
WriteText $pp $t

$t2 = ReadText $pp
Assert ($t2.Contains('StbSendMessageTimeout(h, 0x0010')) 'send close assert'
Write-Host 'OK fix38 verified'

$fix39 = Join-Path $PSScriptRoot 'apply-stb-fix39.ps1'
Assert (Test-Path -LiteralPath $fix39) 'missing fix39'
& $fix39 -Root $Root -Version $Version
