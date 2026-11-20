# apply-stb-fix28.ps1
# 1.0.1.35 补强：
# 1) 数据目录识别加硬条件：只有「程序目录名以 app- 开头」且上一级存在 data 时，才把 RootPath 指到上一级 data，
#    避免免安装版放在 D:\Tools 这类目录时，碰巧存在的 data 目录把设置、日志带走；
# 2) 升级安装时清理根目录上一版遗留的程序文件（dll / exe / config / pdb / zh / Resources 等），
#    只保留 Settings.json、Names.txt 这类数据，[Run] 里再迁进 data\，根目录保持只有启动器 + app-* + data。
param([string]$Root = "plus-src", [string]$Version = "1.0.0.0")

$ErrorActionPreference = 'Stop'
trap { Write-Host ("::error::fix28: " + $_.Exception.Message); throw }

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

# ---------- A. 收紧数据目录判定 ----------
$ap = Join-Path $app 'App.xaml.cs'
$t = ReadText $ap
$old = @'
                    if (System.IO.Directory.Exists(System.IO.Path.Combine(stbUp, "data")))
                    {
                        RootPath = System.IO.Path.Combine(stbUp, "data") + "\\";
                    }
'@
$new = @'
                    string stbSelf = new System.IO.DirectoryInfo(RootPath.TrimEnd('\\')).Name;
                    if (stbSelf.StartsWith("app-", StringComparison.OrdinalIgnoreCase)
                        && System.IO.Directory.Exists(System.IO.Path.Combine(stbUp, "data")))
                    {
                        RootPath = System.IO.Path.Combine(stbUp, "data") + "\\";
                    }
'@
Assert ($t.Contains('string stbUp')) 'fix27 rootpath missing'
$t = ReplaceOnce $t $old $new 'rootpath tighten'
WriteText $ap $t
Write-Host 'PATCHED rootpath tighten'

# ---------- B. 升级时清掉根目录旧程序 ----------
$iss = Join-Path $Root 'packaging\setup.iss'
$t = ReadText $iss
$old = @'
[InstallDelete]
'@
$clean = @'
[InstallDelete]
; 1.0.1.35：程序已进 app-<ver>-0，根目录只留启动器；清掉上一版平铺的旧文件，避免升级后根目录一堆 dll
Type: filesandordirs; Name: "{app}\zh"
Type: filesandordirs; Name: "{app}\Resources"
Type: filesandordirs; Name: "{app}\Styles"
Type: filesandordirs; Name: "{app}\runtimes"
Type: files; Name: "{app}\*.dll"
Type: files; Name: "{app}\*.exe"
Type: files; Name: "{app}\*.config"
Type: files; Name: "{app}\*.pdb"
Type: files; Name: "{app}\*.winmd"
Type: files; Name: "{app}\Log.txt"
Type: files; Name: "{app}\Versions.ini"
Type: files; Name: "{app}\Version.ini"
'@
$t = ReplaceOnce $t $old $clean 'iss install delete'
WriteText $iss $t
Write-Host 'PATCHED iss install delete'

# ---------- 验证 ----------
$a2 = ReadText $ap
Assert ($a2.Contains('StartsWith("app-"')) 'rootpath tighten assert'
$i2 = ReadText $iss
Assert ($i2.Contains('{app}\runtimes')) 'install delete assert'
Write-Host 'OK fix28 verified'

$fix29 = Join-Path $PSScriptRoot 'apply-stb-fix29.ps1'
Assert (Test-Path -LiteralPath $fix29) 'missing fix29'
& $fix29 -Root $Root -Version $Version
