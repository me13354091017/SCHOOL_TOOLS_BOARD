using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Threading;

namespace InkCanvasPlus
{
    /// <summary>
    /// 板中板的橡皮自动回笔：一次擦除手势结束后才切回书写、收起橡皮大小条。
    /// 不在“橡皮刚碰到墨迹”时切换，避免后续拖动变成画线。
    /// </summary>
    public partial class MiniBoardWindow
    {
        private bool _stbMiniAutoPenHooked = false;
        private bool _stbMiniErasedInGesture = false;

        internal void StbInitMiniAutoPen()
        {
            if (_stbMiniAutoPenHooked || MiniInkCanvas == null)
            {
                return;
            }
            _stbMiniAutoPenHooked = true;

            MiniInkCanvas.Strokes.StrokesChanged += StbMiniCanvas_StrokesChanged;
            MiniInkCanvas.AddHandler(UIElement.PreviewStylusUpEvent, new StylusEventHandler(StbMiniAutoPen_StylusUp), true);
            MiniInkCanvas.AddHandler(UIElement.PreviewMouseUpEvent, new MouseButtonEventHandler(StbMiniAutoPen_MouseUp), true);
        }

        private bool StbMiniInEraserMode()
        {
            InkCanvasEditingMode m = MiniInkCanvas.EditingMode;
            return m == InkCanvasEditingMode.EraseByPoint || m == InkCanvasEditingMode.EraseByStroke;
        }

        private void StbMiniCanvas_StrokesChanged(object sender, StrokeCollectionChangedEventArgs e)
        {
            if (e.Removed.Count == 0)
            {
                return;
            }
            if (!StbMiniInEraserMode())
            {
                return;
            }
            _stbMiniErasedInGesture = true;
        }

        private void StbMiniAutoPen_StylusUp(object sender, StylusEventArgs e)
        {
            StbMiniFinishEraseGesture();
        }

        private void StbMiniAutoPen_MouseUp(object sender, MouseButtonEventArgs e)
        {
            StbMiniFinishEraseGesture();
        }

        private void StbMiniFinishEraseGesture()
        {
            if (!_stbMiniErasedInGesture)
            {
                return;
            }
            _stbMiniErasedInGesture = false;

            if (!StbMiniInEraserMode())
            {
                return;
            }
            Dispatcher.BeginInvoke(new Action(StbMiniReturnToPen), DispatcherPriority.Input);
        }

        private void StbMiniReturnToPen()
        {
            try
            {
                if (!StbMiniInEraserMode())
                {
                    return;
                }

                MiniInkCanvas.EditingMode = InkCanvasEditingMode.Ink;
                MiniInkCanvas.Cursor = Cursors.Pen;
                if (MiniEraserRing != null)
                {
                    MiniEraserRing.Visibility = Visibility.Collapsed;
                }
                SetEraserPanel(false);
            }
            catch
            {
            }
        }
    }
}
