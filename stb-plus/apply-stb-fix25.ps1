# apply-stb-fix25.ps1
# 1.0.1.32：函数窗交互修复——
# 1) 键盘浮层按屏幕工作区夹紧（右边缘对齐图标，超出则左移/上翻），右侧退格/回车/方向键不再被屏幕切掉；
#    并改为 StaysOpen=true，键盘开着时点输入框不会被浮层吞掉第一次点击；
# 2) 输入框支持鼠标点击定位：点哪个方框/哪段文字，光标就落到哪里，点完直接打字；
# 3) 行内绿色键盘图标改为矢量绘制（18×12），不再因字体回退被裁成一条竖线。
param([string]$Root = "plus-src", [string]$Version = "1.0.0.0")

$ErrorActionPreference = 'Stop'
trap { Write-Host ("::error::fix25: " + $_.Exception.Message); throw }

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

$extra = Join-Path $PSScriptRoot 'extra'

# ---------- C0 点击定位 partial ----------
$srcF = Join-Path $extra 'StbMathBox.Click.cs'
Assert (Test-Path -LiteralPath $srcF) 'missing extra/StbMathBox.Click.cs'
Copy-Item -LiteralPath $srcF -Destination (Join-Path $app 'StbMathBox.Click.cs') -Force
Write-Host 'COPY StbMathBox.Click.cs'

# ---------- C1 鼠标按下交给点击定位 ----------
$mc = Join-Path $app 'StbMathBox.cs'
$t = ReadText $mc
$old = @'
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            Focus();
            base.OnMouseDown(e);
        }
'@
$new = @'
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            StbClickHere(e.GetPosition(this));
            e.Handled = true;
        }
'@
$t = ReplaceOnce $t $old $new 'mouse down'
WriteText $mc $t
Write-Host 'PATCHED mouse down'

# ---------- R1 命中字段 ----------
$rd = Join-Path $app 'StbMathBox.Render.cs'
$t = ReadText $rd
$old = @'
        // ================= 布局与绘制 =================
'@
$new = @'
        // ================= 布局与绘制 =================

        private double _hitK = 1;
        private double _hitA;
        private double _hitB;
'@
$t = ReplaceOnce $t $old $new 'render fields'

# ---------- R2 绘制时登记根槽 ----------
$old = @'
            MeasureList(_root);
            DrawList(dc, _root, 8, (h - _root.Size.Height) / 2, _root.Size.Height);
'@
$new = @'
            MeasureList(_root);
            double top = (h - _root.Size.Height) / 2;
            _hitSlots.Clear();
            _hitSlots.Add(new HitSlot { Slot = _root, Rect = new Rect(8, top, _root.Size.Width, _root.Size.Height), Scale = 1, Pad = 0 });
            DrawList(dc, _root, 8, top, _root.Size.Height);
'@
$t = ReplaceOnce $t $old $new 'render root hit'
WriteText $rd $t
Write-Host 'PATCHED render root'

# ---------- R3 每个方框登记命中矩形 ----------
$t = ReadText $rd
$old = @'
        private void DrawSlot(DrawingContext dc, Slot s, double x, double y, double w, double h)
        {
            Size inner = MeasureList(s);
'@
$new = @'
        private void DrawSlot(DrawingContext dc, Slot s, double x, double y, double w, double h)
        {
            _hitSlots.Add(new HitSlot
            {
                Slot = s,
                Rect = new Rect(x * _hitK + _hitA, y * _hitK + _hitB, w * _hitK, h * _hitK),
                Scale = _hitK,
                Pad = 3
            });
            Size inner = MeasureList(s);
'@
$t = ReplaceOnce $t $old $new 'draw slot hit'
WriteText $rd $t
Write-Host 'PATCHED draw slot'

# ---------- R4 缩放槽同步命中坐标 ----------
$t = ReadText $rd
$old = @'
        private void DrawSlotScaled(DrawingContext dc, Slot s, double x, double y, double scale)
        {
            Size sz = SlotSize(s);
            dc.PushTransform(new ScaleTransform(scale, scale, x, y));
            DrawSlot(dc, s, x, y, sz.Width, sz.Height);
            dc.Pop();
        }
'@
$new = @'
        private void DrawSlotScaled(DrawingContext dc, Slot s, double x, double y, double scale)
        {
            Size sz = SlotSize(s);
            double k0 = _hitK, a0 = _hitA, b0 = _hitB;
            _hitK = k0 * scale;
            _hitA = a0 + x * (1 - scale) * k0;
            _hitB = b0 + y * (1 - scale) * k0;
            dc.PushTransform(new ScaleTransform(scale, scale, x, y));
            DrawSlot(dc, s, x, y, sz.Width, sz.Height);
            dc.Pop();
            _hitK = k0;
            _hitA = a0;
            _hitB = b0;
        }
'@
$t = ReplaceOnce $t $old $new 'draw scaled'
WriteText $rd $t
Write-Host 'PATCHED draw scaled'

# ---------- K1 键盘浮层字段 ----------
$kp = Join-Path $app 'StbKeypad.cs'
$t = ReadText $kp
$old = @'
        private Popup _keypadPopup;
'@
$new = @'
        private Popup _keypadPopup;
        private Border _keypadShell;
'@
$t = ReplaceOnce $t $old $new 'keypad field'

# ---------- K2 浮层不吞点击 + 屏幕内夹紧 ----------
$old = @'
            _keypadPopup = new Popup
            {
                StaysOpen = false,
                AllowsTransparency = true,
                PopupAnimation = PopupAnimation.Fade,
                Placement = PlacementMode.Bottom,
                Child = shell
            };
        }

        /// <summary>行内绿色键盘图标：把键盘浮层锚到该行输入框下方（希沃式）。</summary>
        internal void StbOpenKeypadFor(StbMathBox box, Button anchor)
        {
            _activeBox = box;
            if (_keypadPopup == null)
            {
                BuildKeypad();
            }
            _keypadPopup.PlacementTarget = anchor;
            _keypadPopup.HorizontalOffset = -420;
            _keypadPopup.VerticalOffset = 6;
            _keypadPopup.IsOpen = true;
            box.Focus();
        }
'@
$new = @'
            _keypadPopup = new Popup
            {
                // 不自动吞点击：键盘开着时点输入框/别的行，应该立刻能点进去输入
                StaysOpen = true,
                AllowsTransparency = true,
                PopupAnimation = PopupAnimation.Fade,
                Placement = PlacementMode.Bottom,
                Child = shell
            };
            _keypadShell = shell;
            Deactivated += (s, e) =>
            {
                if (_keypadPopup != null)
                {
                    _keypadPopup.IsOpen = false;
                }
            };
            Closed += (s, e) =>
            {
                if (_keypadPopup != null)
                {
                    _keypadPopup.IsOpen = false;
                }
            };
        }

        /// <summary>行内绿色键盘图标：把键盘浮层锚到该行输入框下方（希沃式），并保证整块留在屏幕内。</summary>
        internal void StbOpenKeypadFor(StbMathBox box, Button anchor)
        {
            _activeBox = box;
            if (_keypadPopup == null)
            {
                BuildKeypad();
            }

            _keypadShell.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double pw = _keypadShell.DesiredSize.Width;
            double ph = _keypadShell.DesiredSize.Height;
            if (pw <= 0) { pw = 560; }
            if (ph <= 0) { ph = 210; }

            PresentationSource src = PresentationSource.FromVisual(anchor);
            double dpiX = 1, dpiY = 1;
            if (src != null && src.CompositionTarget != null)
            {
                dpiX = src.CompositionTarget.TransformToDevice.M11;
                dpiY = src.CompositionTarget.TransformToDevice.M22;
            }
            Point sp = anchor.PointToScreen(new Point(0, 0));
            double ax = sp.X / dpiX;
            double ay = sp.Y / dpiY;
            Rect wa = SystemParameters.WorkArea;

            // 右边缘对齐键盘图标，再夹到工作区内，避免右侧的退格/回车/方向键被屏幕切掉
            double left = ax + anchor.ActualWidth - pw;
            left = Math.Max(wa.Left + 8, Math.Min(left, wa.Right - pw - 8));
            double top = ay + anchor.ActualHeight + 6;
            if (top + ph > wa.Bottom - 8)
            {
                top = ay - ph - 6;
            }
            top = Math.Max(wa.Top + 8, top);

            _keypadPopup.Placement = PlacementMode.Absolute;
            _keypadPopup.HorizontalOffset = left;
            _keypadPopup.VerticalOffset = top;
            _keypadPopup.IsOpen = true;
            box.Focus();
        }
'@
$t = ReplaceOnce $t $old $new 'keypad clamp'
WriteText $kp $t
Write-Host 'PATCHED keypad'

# ---------- N1 输入行补 using ----------
$rw = Join-Path $app 'StbFunctionWindow.Rows.cs'
$t = ReadText $rw
$old = @'
using System.Windows.Documents;
using System.Windows.Media;
'@
$new = @'
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
'@
$t = ReplaceOnce $t $old $new 'rows using'

# ---------- N2 矢量键盘图标 ----------
$old = @'
    public partial class StbFunctionWindow
    {
        private void AddRow(string expr)
'@
$new = @'
    public partial class StbFunctionWindow
    {
        /// <summary>绿色键盘图标（矢量，固定 18×12，避免字体缺字被裁成竖条）。</summary>
        private static FrameworkElement StbKbIcon()
        {
            return new Path
            {
                Data = Geometry.Parse("M0,0 H18 V12 H0 Z M2,3 H4 V5 H2 Z M6,3 H8 V5 H6 Z M10,3 H12 V5 H10 Z M14,3 H16 V5 H14 Z M4,8 H14 V10 H4 Z"),
                Stroke = new SolidColorBrush(Color.FromRgb(0x1F, 0x8F, 0x5F)),
                StrokeThickness = 1,
                Fill = Brushes.Transparent,
                Stretch = Stretch.Uniform,
                Width = 16,
                Height = 11,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        private void AddRow(string expr)
'@
$t = ReplaceOnce $t $old $new 'kb icon'

# ---------- N3 键盘按钮换矢量图标 ----------
$old = @'
            Button kb = new Button
            {
                Content = "⌨",
                Width = 26,
                Height = 24,
                FontSize = 13,
                Margin = new Thickness(6, 0, 0, 0),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = new SolidColorBrush(Color.FromRgb(0x1F, 0x8F, 0x5F)),
                Cursor = System.Windows.Input.Cursors.Hand,
                ToolTip = "快捷输入"
            };
'@
$new = @'
            Button kb = new Button
            {
                Content = StbKbIcon(),
                Width = 30,
                Height = 26,
                Padding = new Thickness(0),
                Margin = new Thickness(8, 0, 0, 0),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                ToolTip = "快捷输入"
            };
'@
$t = ReplaceOnce $t $old $new 'kb button'

# ---------- N4 整行可点，点一下就能输入 ----------
$old = @'
            Grid input = new Grid();
            input.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
'@
$new = @'
            Grid input = new Grid();
            input.Background = Brushes.Transparent;
            input.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
'@
$t = ReplaceOnce $t $old $new 'input background'

$old = @'
            input.Children.Add(ylab);
            input.Children.Add(box);
            input.Children.Add(kb);
'@
$new = @'
            input.Children.Add(ylab);
            input.Children.Add(box);
            input.Children.Add(kb);
            input.MouseLeftButtonDown += (s, e) =>
            {
                box.Focus();
                Keyboard.Focus(box);
            };
'@
$t = ReplaceOnce $t $old $new 'input click'

# ---------- N5 删行时关掉浮层 ----------
$old = @'
            del.Click += (s, e) =>
            {
                _rows.Remove(row);
'@
$new = @'
            del.Click += (s, e) =>
            {
                if (_keypadPopup != null && _activeBox == box)
                {
                    _keypadPopup.IsOpen = false;
                }
                _rows.Remove(row);
'@
$t = ReplaceOnce $t $old $new 'del close'
WriteText $rw $t
Write-Host 'PATCHED rows'

# ---------- 验证 ----------
$box = ReadText $mc
$rnd = ReadText $rd
$key = ReadText $kp
$row = ReadText $rw
Assert ($box.Contains('StbClickHere')) 'click hook'
Assert ((ReadText (Join-Path $app 'StbMathBox.Click.cs')).Contains('HitSlot')) 'click partial'
Assert ($rnd.Contains('_hitSlots.Add')) 'hit slots'
Assert ($key.Contains('StaysOpen = true')) 'popup stays open'
Assert ($key.Contains('SystemParameters.WorkArea')) 'popup clamp'
Assert ($row.Contains('StbKbIcon')) 'vector kb icon'
Assert ($row.Contains('Keyboard.Focus(box)')) 'row focus'
Write-Host 'OK fix25 verified'
