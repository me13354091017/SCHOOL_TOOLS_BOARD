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
    /// 擦掉笔迹后自动切回书写模式，和希沃“擦完接着写”的手感一致。
    /// 只在橡皮模式下生效；普通书写、选择等模式不受影响。
    /// </summary>
    public partial class MainWindow
    {
        private bool _stbAutoPenHooked = false;

        internal void StbInitAutoPen()
        {
            if (_stbAutoPenHooked || inkCanvas == null)
            {
                return;
            }
            _stbAutoPenHooked = true;
            inkCanvas.Strokes.StrokesChanged += StbInkCanvas_StrokesChanged;
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
