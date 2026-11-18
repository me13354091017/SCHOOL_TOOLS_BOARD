using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace InkCanvasPlus
{
    /// <summary>
    /// 板中板画笔 / 橡皮的交互扩展（partial 文件，不改动 MiniBoardWindow.xaml.cs 原有代码）：
    ///   · 单击「笔」→ 弹出调色板（12 色）+ 粗细滑杆（1–20 档，实际笔宽 = 档位 / 2）
    ///   · 单击「橡皮」→ 切到点擦，并在板中板窗口内展开橡皮大小条（4–60）
    ///     不再使用 Popup：板中板本身是透明分层窗口，Popup 又是另一个分层窗口，
    ///     拖动滑杆时两层都要重绘，正是“拖动橡皮调节条就闪断 / 卡死”的来源。
    /// 颜色、橡皮尺寸都与主界面同一套观感。
    /// </summary>
    public partial class MiniBoardWindow
    {
        private int _eraserSize = 14;
        private bool _syncingPenUi = false;
        private DispatcherTimer _eraserApplyTimer;
        private EllipseStylusShape _eraserShapeCache;
        private int _eraserShapeCacheSize = -1;

        /// <summary>拖滑杆时只记数值，停手后再重建 EraserShape，避免每一格都重建导致卡顿。</summary>
        private void QueueEraserApply()
        {
            if (_eraserApplyTimer == null)
            {
                _eraserApplyTimer = new DispatcherTimer();
                _eraserApplyTimer.Interval = TimeSpan.FromMilliseconds(160);
                _eraserApplyTimer.Tick += (s, e) =>
                {
                    _eraserApplyTimer.Stop();
                    ApplyEraserShape();
                };
            }
            _eraserApplyTimer.Stop();
            _eraserApplyTimer.Start();
        }

        /// <summary>关闭笔弹层与窗内橡皮条（点画布空白处时调用）。</summary>
        private void CloseMiniPopups()
        {
            if (PopupPen != null)
            {
                PopupPen.IsOpen = false;
            }
            SetEraserPanel(false);
        }

        /// <summary>窗内橡皮大小条：不走 Popup，直接在板中板窗口里展开 / 收起。</summary>
        private void SetEraserPanel(bool open)
        {
            if (InlineEraserPanel != null)
            {
                InlineEraserPanel.Visibility = open ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private bool IsEraserPanelOpen
        {
            get
            {
                return InlineEraserPanel != null && InlineEraserPanel.Visibility == Visibility.Visible;
            }
        }

        /// <summary>单击「笔」：切回书写并弹出调色板；已经打开时再点一次收起。</summary>
        private void BtnMBInkPanel_Click(object sender, RoutedEventArgs e)
        {
            ApplyPen();
            SetEraserPanel(false);
            bool willOpen = PopupPen != null && !PopupPen.IsOpen;
            if (willOpen)
            {
                SyncPenUi();
            }
            if (PopupPen != null)
            {
                PopupPen.IsOpen = willOpen;
            }
        }

        /// <summary>单击「橡皮」：切到点擦，并展开橡皮大小条；再点一次收起（大小松手生效）。</summary>
        private void BtnMBEraserPanel_Click(object sender, RoutedEventArgs e)
        {
            if (PopupPen != null)
            {
                PopupPen.IsOpen = false;
            }

            MiniInkCanvas.EditingMode = InkCanvasEditingMode.EraseByPoint;
            ApplyEraserShape();
            SyncEraserUi();

            SetEraserPanel(!IsEraserPanelOpen);
        }

        /// <summary>调色板里点某个颜色：换成该色、收起面板、回到书写。</summary>
        private void BtnMBPaletteColor_Click(object sender, RoutedEventArgs e)
        {
            Button b = sender as Button;
            if (b == null || b.Tag == null)
            {
                return;
            }
            try
            {
                object raw = ColorConverter.ConvertFromString(b.Tag.ToString());
                if (raw is Color)
                {
                    _penColor = (Color)raw;
                }
            }
            catch
            {
                return;
            }
            ApplyPen();
            if (PopupPen != null)
            {
                PopupPen.IsOpen = false;
            }
        }

        /// <summary>橡皮为点擦模式，直径写进 EraserShape（4–60 像素）。</summary>
        private void ApplyEraserShape()
        {
            try
            {
                if (_eraserSize < 4)
                {
                    return;
                }
                if (_eraserShapeCache == null || _eraserShapeCacheSize != _eraserSize)
                {
                    _eraserShapeCache = new EllipseStylusShape(_eraserSize, _eraserSize);
                    _eraserShapeCacheSize = _eraserSize;
                }
                MiniInkCanvas.EraserShape = _eraserShapeCache;
            }
            catch
            {
            }
        }

        private void SyncPenUi()
        {
            if (SliderMBPenWidth != null)
            {
                _syncingPenUi = true;
                SliderMBPenWidth.Value = _penLevel;
                _syncingPenUi = false;
            }
            UpdateWidthLabel();
            UpdatePenInfo();
            SyncEraserUi();
        }

        private void SyncEraserUi()
        {
            if (SliderMBEraser != null)
            {
                SliderMBEraser.Value = _eraserSize;
            }
            UpdateEraserInfo();
        }

        private void UpdatePenInfo()
        {
            if (TextBlockMBPenWidthInfo != null)
            {
                TextBlockMBPenWidthInfo.Text = "粗细 " + _penLevel + " 档 · 实际 " + PenWidth.ToString("0.0") + " 磅";
            }
        }

        private void UpdateEraserInfo()
        {
            if (TextBlockMBEraserInfo != null)
            {
                TextBlockMBEraserInfo.Text = "直径 " + _eraserSize + " 像素";
            }
        }

        private void SliderMBPenWidth_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_syncingPenUi)
            {
                return;
            }
            int v = (int)Math.Round(e.NewValue);
            if (v < MinPenLevel) v = MinPenLevel;
            if (v > MaxPenLevel) v = MaxPenLevel;
            _penLevel = v;
            UpdateWidthLabel();
            UpdatePenInfo();
            ApplyPen();
        }

        private void SliderMBEraser_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int v = (int)Math.Round(e.NewValue);
            if (v < 4) v = 4;
            if (v > 60) v = 60;
            if (v == _eraserSize)
            {
                return;
            }
            _eraserSize = v;
            UpdateEraserInfo();
            QueueEraserApply();
        }
    }
}
