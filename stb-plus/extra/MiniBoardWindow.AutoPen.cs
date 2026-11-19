using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Threading;

namespace InkCanvasPlus
{
    /// <summary>
    /// 板中板的橡皮自动回笔：擦掉笔迹后先挂起，抬笔（离开画布）再切回书写并收起橡皮大小条。
    /// 开关与主窗口同一份：设置页「橡皮用完自动回到书写」。
    /// </summary>
    public partial class MiniBoardWindow
    {
        private bool _stbMiniAutoPenHooked = false;
        private bool _stbMiniErasePending = false;

        internal void StbInitMiniAutoPen()
        {
            if (_stbMiniAutoPenHooked || MiniInkCanvas == null)
            {
                return;
            }
            _stbMiniAutoPenHooked = true;
            MiniInkCanvas.Strokes.StrokesChanged += StbMiniCanvas_StrokesChanged;
            MiniInkCanvas.PreviewStylusUp += (s, e) => StbMiniFlushEraserReturn();
            MiniInkCanvas.PreviewMouseLeftButtonUp += (s, e) => StbMiniFlushEraserReturn();
            MiniInkCanvas.PreviewTouchUp += (s, e) => StbMiniFlushEraserReturn();
            MiniInkCanvas.LostMouseCapture += (s, e) => StbMiniFlushEraserReturn();
        }

        private void StbMiniCanvas_StrokesChanged(object sender, StrokeCollectionChangedEventArgs e)
        {
            if (e.Removed.Count == 0)
            {
                return;
            }

            InkCanvasEditingMode m = MiniInkCanvas.EditingMode;
            if (m != InkCanvasEditingMode.EraseByPoint && m != InkCanvasEditingMode.EraseByStroke)
            {
                return;
            }
            if (!StbPrefs.AutoPenBack)
            {
                return;
            }
            _stbMiniErasePending = true;
        }

        private void StbMiniFlushEraserReturn()
        {
            if (!_stbMiniErasePending)
            {
                return;
            }
            _stbMiniErasePending = false;

            if (!StbPrefs.AutoPenBack)
            {
                return;
            }

            InkCanvasEditingMode m = MiniInkCanvas.EditingMode;
            if (m != InkCanvasEditingMode.EraseByPoint && m != InkCanvasEditingMode.EraseByStroke)
            {
                return;
            }
            Dispatcher.BeginInvoke(new Action(StbMiniReturnToPen), DispatcherPriority.Input);
        }

        private void StbMiniReturnToPen()
        {
            try
            {
                InkCanvasEditingMode m = MiniInkCanvas.EditingMode;
                if (m != InkCanvasEditingMode.EraseByPoint && m != InkCanvasEditingMode.EraseByStroke)
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
