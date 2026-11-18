using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;

namespace InkCanvasPlus
{
    /// <summary>
    /// PPT/批注模式下的快速调色板与橡皮大小（partial 文件，不改动上游 MainWindow 代码）：
    ///   · 「更多颜色」弹层底部加粗细滑杆（1–20，复用上游 InkWidthSlider 的换算）
    ///   · 浮动栏新增「橡皮大小」图标，点开滑杆直接改橡皮直径（4–60 px）
    /// </summary>
    public partial class MainWindow
    {
        private double _pptEraserPx = 14d;

        private void SliderPptPenWidth_Loaded(object sender, RoutedEventArgs e)
        {
            Slider s = sender as Slider;
            if (s == null)
            {
                return;
            }
            s.Value = InkWidthSlider != null ? InkWidthSlider.Value : 5d;
        }

        private void SliderPptPenWidth_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (InkWidthSlider == null)
            {
                return;
            }
            InkWidthSlider.Value = Math.Max(1d, Math.Min(20d, e.NewValue));
        }

        private void SliderPptEraser_Loaded(object sender, RoutedEventArgs e)
        {
            Slider s = sender as Slider;
            if (s != null)
            {
                s.Value = _pptEraserPx;
            }
            UpdatePptEraserInfo();
        }

        private void SliderPptEraser_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            double v = e.NewValue;
            if (v < 4d) v = 4d;
            if (v > 60d) v = 60d;
            _pptEraserPx = v;
            ApplyPptEraserShape();
            UpdatePptEraserInfo();
        }

        private void BorderPptEraser_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                inkCanvas.EditingMode = InkCanvasEditingMode.EraseByPoint;
                forceEraser = true;
                ApplyPptEraserShape();
                if (SliderPptEraser != null)
                {
                    SliderPptEraser.Value = _pptEraserPx;
                }
                UpdatePptEraserInfo();
                if (PopupPptEraser != null)
                {
                    PopupPptEraser.IsOpen = !PopupPptEraser.IsOpen;
                }
            }
            catch
            {
            }
            e.Handled = true;
        }

        private void ApplyPptEraserShape()
        {
            try
            {
                inkCanvas.EraserShape = new EllipseStylusShape(_pptEraserPx, _pptEraserPx);
            }
            catch
            {
            }
        }

        private void UpdatePptEraserInfo()
        {
            if (TextBlockPptEraserInfo != null)
            {
                TextBlockPptEraserInfo.Text = "直径 " + ((int)Math.Round(_pptEraserPx)) + " 像素";
            }
        }
    }
}
