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
    /// 橡皮擦掉笔迹后**不立即**切回书写，先挂起，等抬笔（手指 / 笔 / 鼠标离开画布）再切回去，
    /// 这样一次擦除手势可以连续擦多笔，不会被中途打断。开关在设置页「墨迹书写 → 橡皮用完自动回到书写」。
    /// </summary>
    public partial class MainWindow
    {
        private bool _stbAutoPenHooked = false;
        private bool _stbErasePending = false;

        internal void StbInitAutoPen()
        {
            if (_stbAutoPenHooked || inkCanvas == null)
            {
                return;
            }
            _stbAutoPenHooked = true;
            inkCanvas.Strokes.StrokesChanged += StbInkCanvas_StrokesChanged;
            inkCanvas.PreviewStylusUp += (s, e) => StbFlushEraserReturn();
            inkCanvas.PreviewMouseLeftButtonUp += (s, e) => StbFlushEraserReturn();
            inkCanvas.PreviewTouchUp += (s, e) => StbFlushEraserReturn();
            inkCanvas.LostMouseCapture += (s, e) => StbFlushEraserReturn();
        }

        /// <summary>擦掉了笔迹：只登记，不立即回笔。</summary>
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
            if (!StbPrefs.AutoPenBack)
            {
                return;
            }
            _stbErasePending = true;
        }

        /// <summary>抬笔：如果这次手势里擦过东西，才切回书写。</summary>
        private void StbFlushEraserReturn()
        {
            if (!_stbErasePending)
            {
                return;
            }
            _stbErasePending = false;

            if (!StbPrefs.AutoPenBack || !StbInEraserMode())
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
