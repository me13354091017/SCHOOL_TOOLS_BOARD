# apply-stb-fix43.ps1
# 1.0.1.51：10MB+ 大笔记流畅化。
#   真机基线（34,000 笔 / 10.7MB）：解析 1.5s、Clear 15s、Add 6s，原流程 UI 冻结约 20s。
#   改为：整份 StrokeCollection 一次性赋给 InkCanvas（实测约 4.8s）+ 进度遮罩；加载时跳过 34k 笔的 StylusPoints 克隆，只补事件订阅。
#   另支持 -icstk <路径> 启动参数（真机基线/双击打开用），保存时释放文件句柄并记录耗时。
param([string]$Root = "plus-src", [string]$Version = "1.0.0.0")

$ErrorActionPreference = 'Stop'
trap { Write-Host ("::error::fix43: " + $_.Exception.Message); throw }

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

# ---------- P1 新增性能实现 ----------
$src = Join-Path $PSScriptRoot 'extra/MainWindow.Performance.cs'
Assert (Test-Path -LiteralPath $src) 'missing extra/MainWindow.Performance.cs'
Copy-Item -LiteralPath $src -Destination (Join-Path $app 'MainWindow.Performance.cs') -Force
Write-Host 'COPIED MainWindow.Performance.cs'

# ---------- P2 构造尾部挂 Loaded，处理 -icstk ----------
$mw = Join-Path $app 'MainWindow.xaml.cs'
$t = ReadText $mw
$old = @'
            UpdateWindowTitle();

            CheckVersionAndShowWelcome();
'@
$new = @'
            UpdateWindowTitle();

            // 1.0.1.51：窗口首帧后处理 -icstk 启动参数 / 大笔记加载
            Loaded += StbPerfOnLoaded;

            CheckVersionAndShowWelcome();
'@
$t = ReplaceOnce $t $old $new 'loaded hook'
WriteText $mw $t

# ---------- P3 打开笔记走渐进加载 ----------
$feat = Join-Path $app 'MainWindow.Features.cs'
$t = ReadText $feat
$old = @'
                    var fileStreamHasNoStroke = false;
                    using (var fs = new FileStream(openFileDialog.FileName, FileMode.Open, FileAccess.Read))
                    {
                        var strokes = new StrokeCollection(fs);
                        fileStreamHasNoStroke = strokes.Count == 0;
                        if (!fileStreamHasNoStroke)
                        {
                            ClearStrokes(true);
                            timeMachine.ClearStrokeHistory();
                            inkCanvas.Strokes.Add(strokes);
                            LogHelper.NewLog(string.Format("Strokes Insert: Strokes Count: {0}", inkCanvas.Strokes.Count.ToString()));
                        }
                    }
                    if (fileStreamHasNoStroke)
                    {
                        using (var ms = new MemoryStream(File.ReadAllBytes(openFileDialog.FileName)))
                        {
                            ms.Seek(0, SeekOrigin.Begin);
                            var strokes = new StrokeCollection(ms);
                            ClearStrokes(true);
                            timeMachine.ClearStrokeHistory();
                            inkCanvas.Strokes.Add(strokes);
                            LogHelper.NewLog(string.Format("Strokes Insert (2): Strokes Count: {0}", strokes.Count.ToString()));
                        }
                    }
'@
$new = @'
                    // 1.0.1.51：大笔记改为一次性切换整份墨迹集合，避免 15s 清屏 + 6s 逐笔添加
                    StbLoadInkFile(openFileDialog.FileName, false);
'@
$t = ReplaceOnce $t $old $new 'open progressive'
WriteText $feat $t

# ---------- P4 保存释放文件句柄 + 计时 ----------
$t = ReadText $feat
$old = @'
                inkCanvas.Strokes.Save(fs);
'@
$new = @'
                System.DateTime stbSaveStart = System.DateTime.Now;
                try
                {
                    inkCanvas.Strokes.Save(fs);
                }
                finally
                {
                    // 1.0.1.51：原实现不释放 FileStream，10MB+ 文件会长期被锁
                    string stbSavePath = fs.Name;
                    fs.Dispose();
                    LogHelper.NewLog(string.Format("StbPerf: save strokes={0} bytes={1} ms={2} path='{3}'",
                        inkCanvas.Strokes.Count, new FileInfo(stbSavePath).Length,
                        (int)(System.DateTime.Now - stbSaveStart).TotalMilliseconds, stbSavePath));
                }
'@
$t = ReplaceOnce $t $old $new 'save dispose timing'
WriteText $feat $t

# ---------- P5 校验 ----------
$perf = ReadText (Join-Path $app 'MainWindow.Performance.cs')
Assert ($perf.Contains('private void StbPerfPump()')) 'perf pump'
Assert ($perf.Contains('-icstk=')) 'perf startup arg'
Assert ($perf.Contains('inkCanvas.Strokes = loaded;')) 'perf assign collection'
Assert ((ReadText $mw).Contains('Loaded += StbPerfOnLoaded;')) 'loaded hook assert'
Assert ((ReadText $feat).Contains('StbLoadInkFile(openFileDialog.FileName, false);')) 'open assert'
Assert ((ReadText $feat).Contains('fs.Dispose();')) 'save assert'
Write-Host 'OK fix43 verified'
