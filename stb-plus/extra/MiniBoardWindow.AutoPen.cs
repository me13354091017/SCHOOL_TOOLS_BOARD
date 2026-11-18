using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Threading;

namespace InkCanvasPlus
{
    /// <summary>
    /// 板中板的橡皮自动回笔：擦掉笔迹后自动切回书写、收起橡皮大小条。
    /// </summary>
    public partial class MiniBoardWindow
    {
        private bool _stbMiniAutoPenHooked = false;

        internal void StbInitMiniAutoPen()
        {
            if (_stbMiniAutoPenHooked || MiniInkCanvas == null)
            {
                return;
            }
            _stbMiniAutoPenHooked = true;
            MiniInkCanvas.Strokes.StrokesChanged += StbMiniCanvas_StrokesChanged;
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
