# apply-stb-fix5.ps1
# 1.0.1.11：性能与卡顿修复
#   ① 板中板拖拽角缩放：限制为每 40ms 提交一次窗口尺寸，松手时补一次最终尺寸，去掉每次鼠标移动都重排导致的卡顿
#   ② 板中板橡皮大小：滑杆拖动过程中只更新文字，松手（或失去鼠标捕获）时才真正重建 EraserShape，避免整窗重绘
#   ③ 主窗口 PPT / 批注模式橡皮大小：同上
param(
    [string]$Root = "plus-src",
    [string]$Version = "1.0.0.0"
)

$ErrorActionPreference = 'Stop'
trap {
    Write-Host ("::error::fix5: " + $_.Exception.Message)
    throw
}

$app = Join-Path $Root 'Ink Canvas'
$utf8 = New-Object System.Text.UTF8Encoding($false)

function Assert([bool]$cond, [string]$msg) {
    if (-not $cond) { throw "ANCHOR FAIL: $msg" }
}

function RePatch([string]$path, [string]$pat, [string]$rep, [string]$label, [int]$minHit = 1) {
    Assert (Test-Path -LiteralPath $path) "$label :: file missing $path"
    $t = Get-Content -LiteralPath $path -Raw
    $n = ([regex]::Matches($t, $pat)).Count
    Assert ($n -ge $minHit) "$label :: pattern not found (count=$n)"
    $o = [regex]::Replace($t, $pat, $rep)
    [IO.File]::WriteAllText($path, $o, $utf8)
    Write-Host ("PATCHED  " + $label + "  (x" + $n + ")")
}

Write-Host "== PHASE R1: 板中板拖拽角缩放节流 =="
$mbcs = Join-Path $app 'MiniBoardWindow.xaml.cs'

$fields = @'
private Rect _startRect = new Rect(0, 0, 0, 0);

        private DateTime _lastResizeApply = DateTime.MinValue;
        private Rect _pendingRect = new Rect(0, 0, 0, 0);
        private bool _hasPendingRect = false;

        /// <summary>把挂起的尺寸真正写进窗口；节流调用，松手时无条件补一次。</summary>
        private void ApplyPendingRect()
        {
            if (!_hasPendingRect)
            {
                return;
            }
            _hasPendingRect = false;
            _lastResizeApply = DateTime.UtcNow;

            double w = _pendingRect.Width;
            double h = _pendingRect.Height;
            if (w < MinW) { w = MinW; }
            if (h < MinH) { h = MinH; }

            Left = _pendingRect.X;
            Top = _pendingRect.Y;
            Width = w;
            Height = h;
        }
'@
RePatch $mbcs 'private Rect _startRect = new Rect\(0, 0, 0, 0\);' $fields '板中板 缩放字段与提交方法'

$resizeBody = @'
                _pendingRect = new Rect(l, t, w, h);
                _hasPendingRect = true;
                if ((DateTime.UtcNow - _lastResizeApply).TotalMilliseconds >= 40d)
                {
                    ApplyPendingRect();
                }
'@
RePatch $mbcs 'Left = l;\r?\n[ \t]*Top = t;\r?\n[ \t]*Width = w;\r?\n[ \t]*Height = h;' $resizeBody '板中板 缩放改为 40ms 节流'

$upRep = "_resizing = false;`n" + '$1' + "ApplyPendingRect();`n" + '$1' + 'RootGrid.ReleaseMouseCapture();'
RePatch $mbcs '_resizing = false;\r?\n([ \t]*)RootGrid\.ReleaseMouseCapture\(\);' $upRep '板中板 松手提交最终尺寸' 2

RePatch $mbcs 'RootGrid\.LostMouseCapture \+= \(s, e\) => \{ _resizing = false; \};' 'RootGrid.LostMouseCapture += (s, e) => { _resizing = false; ApplyPendingRect(); };' '板中板 失去捕获时补提交'

Write-Host "== PHASE R2: 板中板橡皮大小改为松手才生效 =="
$mbp = Join-Path $app 'MiniBoardWindow.Pens.cs'
RePatch $mbp '_eraserApplyTimer\.Interval = TimeSpan\.FromMilliseconds\(160\);' '_eraserApplyTimer.Interval = TimeSpan.FromMilliseconds(500);' '板中板 橡皮防抖放宽到 500ms'

$hook = @'
private bool _eraserHookDone = false;

        /// <summary>滑杆只在松手 / 失去捕获 / 按键抬起时提交橡皮尺寸，拖动过程只更新提示文字。</summary>
        private void HookEraserSlider()
        {
            if (_eraserHookDone || SliderMBEraser == null)
            {
                return;
            }
            _eraserHookDone = true;
            SliderMBEraser.PreviewMouseLeftButtonUp += (s, e) => CommitEraserSize();
            SliderMBEraser.LostMouseCapture += (s, e) => CommitEraserSize();
            SliderMBEraser.PreviewKeyUp += (s, e) => CommitEraserSize();
        }

        private void CommitEraserSize()
        {
            if (_eraserApplyTimer != null)
            {
                _eraserApplyTimer.Stop();
            }
            ApplyEraserShape();
        }

        /// <summary>橡皮为点擦模式，直径写进 EraserShape（4–60 像素）。</summary>
        private void ApplyEraserShape()
'@
RePatch $mbp '/// <summary>橡皮为点擦模式，直径写进 EraserShape（4–60 像素）。</summary>\r?\n[ \t]*private void ApplyEraserShape\(\)' $hook '板中板 橡皮滑杆松手提交'

$callHook = @'
            ApplyEraserShape();
            HookEraserSlider();
            SyncEraserUi();
'@
RePatch $mbp '            ApplyEraserShape\(\);\r?\n            SyncEraserUi\(\);' $callHook '板中板 打开橡皮弹层时挂接滑杆'

Write-Host "== PHASE R3: PPT / 批注橡皮大小改为松手才生效 =="
$ppt = Join-Path $app 'MainWindow.PptPalette.cs'
RePatch $ppt '_pptEraserTimer\.Interval = TimeSpan\.FromMilliseconds\(160\);' '_pptEraserTimer.Interval = TimeSpan.FromMilliseconds(500);' 'PPT 橡皮防抖放宽到 500ms'

$pptHook = @'
                s.Value = _pptEraserPx;
            }
            UpdatePptEraserInfo();
            if (s != null)
            {
                s.PreviewMouseLeftButtonUp += (o, args) =>
                {
                    if (_pptEraserTimer != null) { _pptEraserTimer.Stop(); }
                    ApplyPptEraserShape();
                };
                s.LostMouseCapture += (o, args) =>
                {
                    if (_pptEraserTimer != null) { _pptEraserTimer.Stop(); }
                    ApplyPptEraserShape();
                };
            }
'@
RePatch $ppt '                s\.Value = _pptEraserPx;\r?\n            \}\r?\n            UpdatePptEraserInfo\(\);' $pptHook 'PPT 橡皮滑杆松手提交'

Write-Host "== PHASE R4: 校验 =="
$mbcsT = Get-Content -LiteralPath $mbcs -Raw
$mbpT = Get-Content -LiteralPath $mbp -Raw
$pptT = Get-Content -LiteralPath $ppt -Raw
Assert ($mbcsT.Contains('_pendingRect = new Rect(l, t, w, h);')) '缩放节流未写入'
Assert ($mbcsT.Contains('private void ApplyPendingRect()')) '提交方法缺失'
Assert ($mbpT.Contains('private void HookEraserSlider()')) '橡皮滑杆挂接缺失'
Assert ($mbpT.Contains('CommitEraserSize')) '橡皮提交方法缺失'
Assert ($pptT.Contains('ApplyPptEraserShape();')) 'PPT 橡皮提交缺失'
Write-Host 'OK  PHASE R 校验通过'

Write-Host "STB FIX5 PATCHES DONE"
