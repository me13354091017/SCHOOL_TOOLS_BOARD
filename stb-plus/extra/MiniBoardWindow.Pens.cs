using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;

namespace InkCanvasPlus
{
    /// <summary>
    /// 板中板画笔 / 橡皮的交互扩展（partial 文件，不改动 MiniBoardWindow.xaml.cs 原有代码）：
    ///   · 单击「笔」→ 弹出调色板（12 色）+ 粗细滑杆（1–20 档，实际笔宽 = 档位 / 2）
    ///   · 单击「橡皮」→ 切到点擦，同时直接弹出橡皮大小滑杆（4–60），点画布自动收起
    /// 颜色、橡皮尺寸都与主界面同一套观感。
    /// </summary>
    public partial class MiniBoardWindow
    {
        private int _eraserSize = 14;
        private bool _syncingPenUi = false;

        /// <summary>关闭笔 / 橡皮两个弹层（点画布空白处时调用）。</summary>
        private void CloseMiniPopups()
        {
            if (PopupPen != null)
            {
                PopupPen.IsOpen = false;
            }
            if (PopupEraser != null)
            {
                PopupEraser.IsOpen = false;
            }
        }

        /// <summary>单击「笔」：切回书写并弹出调色板；已经打开时再点一次收起。</summary>
        private void BtnMBInkPanel_Click(object sender, RoutedEventArgs e)
        {
            ApplyPen();
            if (PopupEraser != null)
            {
                PopupEraser.IsOpen = false;
            }
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

        /// <summary>单击「橡皮」：切到点擦，并直接弹出橡皮大小滑杆；再点一次收起（大小已实时生效）。</summary>
        private void BtnMBEraserPanel_Click(object sender, RoutedEventArgs e)
        {
            if (PopupPen != null)
            {
                PopupPen.IsOpen = false;
            }

            MiniInkCanvas.EditingMode = InkCanvasEditingMode.EraseByPoint;
            ApplyEraserShape();
            SyncEraserUi();

            if (PopupEraser != null)
            {
                PopupEraser.IsOpen = !PopupEraser.IsOpen;
            }
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
                MiniInkCanvas.EraserShape = new EllipseStylusShape(_eraserSize, _eraserSize);
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
            _eraserSize = v;
            UpdateEraserInfo();
            ApplyEraserShape();
        }
    }
}
