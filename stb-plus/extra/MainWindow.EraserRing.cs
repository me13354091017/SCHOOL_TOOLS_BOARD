using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;

namespace InkCanvasPlus
{
    /// <summary>
    /// 橡皮跟随圆环（partial 文件，不改动上游代码）：
    /// 在橡皮模式（点擦 / 整笔擦 / forceEraser）下，把画布上的光标换成「跟着笔尖走、直径等于橡皮直径」的圆环，
    /// 这样黑板、批注（透明批注模式）里用橡皮都能看见橡皮有多大、擦到哪里。
    /// </summary>
    public partial class MainWindow
    {
        private void StbEraserRing_MouseMove(object sender, MouseEventArgs e)
        {
            UpdateStbEraserRing(e.GetPosition(inkCanvas));
        }

        private void StbEraserRing_MouseLeave(object sender, MouseEventArgs e)
        {
            if (StbEraserRing != null)
            {
                StbEraserRing.Visibility = Visibility.Collapsed;
                inkCanvas.Cursor = Cursors.Pen;
            }
        }

        private bool IsStbEraserActive()
        {
            return forceEraser
                || inkCanvas.EditingMode == InkCanvasEditingMode.EraseByPoint
                || inkCanvas.EditingMode == InkCanvasEditingMode.EraseByStroke;
        }

        private void UpdateStbEraserRing(Point p)
        {
            if (StbEraserRing == null || inkCanvas == null)
            {
                return;
            }

            if (!IsStbEraserActive())
            {
                StbEraserRing.Visibility = Visibility.Collapsed;
                inkCanvas.Cursor = Cursors.Pen;
                return;
            }

            // 与上游同一套口径：点擦直径 = 50 * 系数，整笔擦为固定小圆
            double d = forcePointEraser ? 50d * GetEraserSizeCoefficient() : 5d;
            if (d < 6d)
            {
                d = 6d;
            }
            if (d > 400d)
            {
                d = 400d;
            }

            StbEraserRing.Width = d;
            StbEraserRing.Height = d;
            Canvas.SetLeft(StbEraserRing, p.X - d / 2d);
            Canvas.SetTop(StbEraserRing, p.Y - d / 2d);
            StbEraserRing.Visibility = Visibility.Visible;
            inkCanvas.Cursor = Cursors.None;
        }
    }
}
