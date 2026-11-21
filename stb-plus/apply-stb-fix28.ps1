# apply-stb-fix28.ps1
# 1.0.1.35：数据目录判定收紧 + 升级时清理根目录上一版平铺的程序文件。
# 1.0.1.41：链尾依次执行 fix29 →（fix30）→ fix31 → fix32 → fix33 → fix34。
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

# 反斜杠字符：脚本里不出现真实反斜杠，注入时再替换占位符
$bs = [string][char]92
function UnBs([string]$s) { return $s.Replace('@BS@', $bs) }

# ---------- A. 收紧数据目录判定 ----------
$ap = Join-Path $app 'App.xaml.cs'
$t = ReadText $ap
$old = @'
                    if (System.IO.Directory.Exists(System.IO.Path.Combine(stbUp, "data")))
                    {
                        RootPath = System.IO.Path.Combine(stbUp, "data") + "@BS@@BS@";
                    }
'@
$new = @'
                    string stbSelf = new System.IO.DirectoryInfo(RootPath.TrimEnd('@BS@@BS@')).Name;
                    if (stbSelf.StartsWith("app-", StringComparison.OrdinalIgnoreCase)
                        && System.IO.Directory.Exists(System.IO.Path.Combine(stbUp, "data")))
                    {
                        RootPath = System.IO.Path.Combine(stbUp, "data") + "@BS@@BS@";
                    }
'@
$old = UnBs $old
$new = UnBs $new
Assert ($t.Contains('string stbUp')) 'fix27 rootpath missing'
$t = ReplaceOnce $t $old $new 'rootpath tighten'
WriteText $ap $t
Write-Host 'PATCHED rootpath tighten'

# ---------- B. 升级时清掉根目录旧程序 ----------
$iss = Join-Path (Join-Path $Root 'packaging') 'setup.iss'
$t = ReadText $iss
$old = @'
[InstallDelete]
'@
$clean = @'
[InstallDelete]
; 1.0.1.35：程序已进 app-<ver>-0，根目录只留启动器；清掉上一版平铺的旧文件
Type: filesandordirs; Name: "{app}@BS@zh"
Type: filesandordirs; Name: "{app}@BS@Resources"
Type: filesandordirs; Name: "{app}@BS@Styles"
Type: filesandordirs; Name: "{app}@BS@runtimes"
Type: files; Name: "{app}@BS@*.dll"
Type: files; Name: "{app}@BS@*.exe"
Type: files; Name: "{app}@BS@*.config"
Type: files; Name: "{app}@BS@*.pdb"
Type: files; Name: "{app}@BS@*.winmd"
Type: files; Name: "{app}@BS@Log.txt"
Type: files; Name: "{app}@BS@Versions.ini"
Type: files; Name: "{app}@BS@Version.ini"
'@
$clean = UnBs $clean
$t = ReplaceOnce $t $old $clean 'iss install delete'
WriteText $iss $t
Write-Host 'PATCHED iss install delete'

# ---------- 验证 ----------
$a2 = ReadText $ap
Assert ($a2.Contains('StartsWith("app-"')) 'rootpath tighten assert'
$i2 = ReadText $iss
Assert ($i2.Contains('runtimes')) 'install delete assert'
Write-Host 'OK fix28 verified'

# ---------- 链 ----------
$fix29 = Join-Path $PSScriptRoot 'apply-stb-fix29.ps1'
Assert (Test-Path -LiteralPath $fix29) 'missing fix29'
& $fix29 -Root $Root -Version $Version

$fix31 = Join-Path $PSScriptRoot 'apply-stb-fix31.ps1'
Assert (Test-Path -LiteralPath $fix31) 'missing fix31'
& $fix31 -Root $Root -Version $Version

$fix32 = Join-Path $PSScriptRoot 'apply-stb-fix32.ps1'
Assert (Test-Path -LiteralPath $fix32) 'missing fix32'
& $fix32 -Root $Root -Version $Version

$fix33 = Join-Path $PSScriptRoot 'apply-stb-fix33.ps1'
Assert (Test-Path -LiteralPath $fix33) 'missing fix33'
& $fix33 -Root $Root -Version $Version

$fix34 = Join-Path $PSScriptRoot 'apply-stb-fix34.ps1'
Assert (Test-Path -LiteralPath $fix34) 'missing fix34'
& $fix34 -Root $Root -Version $Version
