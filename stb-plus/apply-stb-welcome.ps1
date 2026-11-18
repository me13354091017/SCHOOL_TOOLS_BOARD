# apply-stb-welcome.ps1
# 每次发版同步应用内「更新弹窗 / 欢迎页」的更新说明：
# 内容单一来源为 stb-plus/extra/welcome.txt，构建时生成 StbWelcomeContent.cs 并由 WelcomeWindow 载入。
param(
    [string]$Root = "plus-src",
    [string]$Version = "1.0.0.0"
)

$ErrorActionPreference = 'Stop'
$app = Join-Path $Root 'Ink Canvas'
$utf8 = New-Object System.Text.UTF8Encoding($false)

function Assert([bool]$cond, [string]$msg) {
    if (-not $cond) { throw "ANCHOR FAIL: $msg" }
}

try {
    $txtPath = Join-Path $PSScriptRoot 'extra/welcome.txt'
    Assert (Test-Path -LiteralPath $txtPath) 'missing source welcome.txt'
    $raw = Get-Content -LiteralPath $txtPath -Raw
    $raw = $raw -replace "`r`n", "`n"
    $escaped = $raw.Replace('"', '""')

    $cs = @"
using System;
using System.Windows.Controls;
using System.Windows.Documents;

namespace InkCanvasPlus
{
    /// <summary>应用内欢迎 / 更新弹窗的说明文本（构建时由 stb-plus/extra/welcome.txt 生成）。</summary>
    internal static class StbWelcomeContent
    {
        internal static readonly string Text = @"$escaped";

        internal static void Fill(TextBlock target)
        {
            if (target == null)
            {
                return;
            }
            target.Inlines.Clear();
            string[] lines = Text.Replace("\r\n", "\n").Split('\n');
            foreach (string line in lines)
            {
                string t = line.TrimEnd();
                if (t.Length == 0)
                {
                    target.Inlines.Add(new LineBreak());
                    continue;
                }
                bool isHead = t.StartsWith("【") && t.EndsWith("】");
                target.Inlines.Add(isHead ? (Inline)new Bold(new Run(t)) : (Inline)new Run(t));
                target.Inlines.Add(new LineBreak());
            }
        }
    }
}
"@
    [IO.File]::WriteAllText((Join-Path $app 'StbWelcomeContent.cs'), $cs, $utf8)
    Write-Host 'PATCHED  generate StbWelcomeContent.cs'

    $ww = Join-Path $app 'WelcomeWindow.xaml.cs'
    Assert (Test-Path -LiteralPath $ww) 'missing WelcomeWindow.xaml.cs'
    $t = Get-Content -LiteralPath $ww -Raw
    Assert ($t.Contains('            InitializeComponent();')) 'WelcomeWindow InitializeComponent anchor not found'
    Assert (-not $t.Contains('StbWelcomeContent.Fill')) 'WelcomeWindow 已注入过，勿重复'
    $t = $t.Replace(
        '            InitializeComponent();',
        @'
            InitializeComponent();
            Loaded += (s, e) =>
            {
                TextBlockUpdateContent.Inlines.Clear();
                StbWelcomeContent.Fill(TextBlockUpdateContent);
            };
'@)
    [IO.File]::WriteAllText($ww, $t, $utf8)
    Write-Host 'PATCHED  WelcomeWindow 注入当前版本更新说明'

    Assert (Select-String -LiteralPath (Join-Path $app 'StbWelcomeContent.cs') -Pattern ([regex]::Escape($Version)) -Quiet) "welcome.txt 未包含当前版本号 $Version"
    Assert (Select-String -LiteralPath $ww -Pattern 'StbWelcomeContent.Fill' -Quiet) 'WelcomeWindow 注入失败'
    Write-Host 'OK  welcome 校验通过'
}
catch {
    Write-Host ("::error::STB welcome failed: " + $_.Exception.Message)
    throw
}

Write-Host "STB WELCOME PATCH DONE"
