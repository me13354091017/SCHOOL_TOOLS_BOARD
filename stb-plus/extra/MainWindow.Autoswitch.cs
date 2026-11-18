using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Threading;

namespace InkCanvasPlus
{
    /// <summary>
    /// 主窗口（黑板 / 白板 / PPT / 批注）的橡皮自动回笔：
    /// 一次擦除手势结束后才切回书写，而不是“橡皮刚碰到墨迹就变成笔”——
    /// 后者会导致橡皮按下去第一下就把笔迹删掉，后续拖动直接画出新笔迹。
    /// </summary>
    public partial class MainWindow
    {
        private bool _stbAutoPenHooked = false;
        private bool _stbErasedInGesture = false;

        internal void StbInitAutoPen()
        {
            if (_stbAutoPenHooked || inkCanvas == null)
            {
                return;
            }
            _stbAutoPenHooked = true;

            inkCanvas.Strokes.StrokesChanged += StbInkCanvas_StrokesChanged;
            inkCanvas.AddHandler(UIElement.PreviewStylusUpEvent, new StylusEventHandler(StbAutoPen_StylusUp), true);
            inkCanvas.AddHandler(UIElement.PreviewMouseUpEvent, new MouseButtonEventHandler(StbAutoPen_MouseUp), true);
        }

        private void StbInkCanvas_StrokesChanged(object sender, StrokeCollectionChangedEventArgs e)
        {
            if (e.Removed.Count == 0)
            {
                return;
            }
            if (!StbInEraserMode())
            {
                return;
            }
            // 只记“这一笔擦到东西了”，等抬手后再切回书写
            _stbErasedInGesture = true;
        }

        private void StbAutoPen_StylusUp(object sender, StylusEventArgs e)
        {
            StbFinishEraseGesture();
        }

        private void StbAutoPen_MouseUp(object sender, MouseButtonEventArgs e)
        {
            StbFinishEraseGesture();
        }

        private void StbFinishEraseGesture()
        {
            if (!_stbErasedInGesture)
            {
                return;
            }
            _stbErasedInGesture = false;

            if (!StbInEraserMode())
            {
                return;
            }
            Dispatcher.BeginInvoke(new Action(StbReturnToPen), DispatcherPriority.Input);
        }

        private bool StbInEraserMode()
        {
            if (inkCanvas == null)
            {
                return false;
            }

            InkCanvasEditingMode m = inkCanvas.EditingMode;
            return m == InkCanvasEditingMode.EraseByPoint || m == InkCanvasEditingMode.EraseByStroke;
        }

        /// <summary>从橡皮切回书写；同步浮动栁按钮状态与光标。</summary>
        internal void StbReturnToPen()
        {
            try
            {
                if (!StbInEraserMode())
                {
                    return;
                }

                forceEraser = false;
                forcePointEraser = false;

                inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
                inkCanvas_EditingModeChanged(inkCanvas, new RoutedEventArgs());
                inkCanvas.Cursor = Cursors.Pen;

                if (StbEraserRing != null)
                {
                    StbEraserRing.Visibility = Visibility.Collapsed;
                }
            }
            catch
            {
            }
        }
    }
}
