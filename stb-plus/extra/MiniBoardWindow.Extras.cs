using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;

namespace InkCanvasPlus
{
    /// <summary>
    /// 板中板的补充交互（partial 文件，不改动 MiniBoardWindow.xaml.cs 原代码）：
    ///   · 拖动条只移动窗口，不再误触发四边四角缩放（缩放改由 Preview 事件统一判定）
    ///   · 标题栏新增「最大化 / 还原」按钮
    ///   · 橡皮模式下显示跟随笔尖的圆环（直径 = 橡皮直径）
    /// </summary>
    public partial class MiniBoardWindow
    {
        // ── 缩放判定（替代原来的 RootGrid_MouseLeftButtonDown 绑定）────────────
        private void StbRootGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            DependencyObject src = e.OriginalSource as DependencyObject;
            if (src != null && (src == HeaderBar || HeaderBar.IsAncestorOf(src)))
            {
                // 标题栏这一条由 Header_MouseLeftButtonDown 负责拖动，不参与缩放判定
                return;
            }

            Point p = e.GetPosition(RootGrid);
            string grip = HitGrip(p);
            if (grip == "None")
            {
                return;
            }

            _grip = grip;
            _resizing = true;
            _startCursor = ScreenDip(e.GetPosition(this));
            _startRect = new Rect(Left, Top, ActualWidth, ActualHeight);
            RootGrid.CaptureMouse();
            e.Handled = true;
        }

        // ── 最大化 / 还原 ──────────────────────────────────────────────────
        private bool _maximized = false;
        private Rect _preMaxRect = Rect.Empty;

        private void BtnMBMax_Click(object sender, RoutedEventArgs e)
        {
            ToggleMaximize();
        }

        private void ToggleMaximize()
        {
            if (_collapsed)
            {
                ExpandBoard();
            }

            Rect wa = SystemParameters.WorkArea;
            if (!_maximized)
            {
                _preMaxRect = new Rect(Left, Top, ActualWidth, ActualHeight);
                _maximized = true;
                Left = wa.Left + 8d;
                Top = wa.Top + 8d;
                Width = Math.Max(MinW, wa.Width - 16d);
                Height = Math.Max(MinH, wa.Height - 16d);
            }
            else
            {
                _maximized = false;
                if (_preMaxRect.Width > 0d && _preMaxRect.Height > 0d)
                {
                    Left = _preMaxRect.X;
                    Top = _preMaxRect.Y;
                    Width = _preMaxRect.Width;
                    Height = _preMaxRect.Height;
                }
                ClampToWorkArea();
            }

            UpdateWidthLabel();
        }

        // ── 橡皮跟随圆环 ───────────────────────────────────────────────────
        private void MiniInkCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (MiniEraserRing == null || MiniInkCanvas == null)
            {
                return;
            }

            if (MiniInkCanvas.EditingMode != InkCanvasEditingMode.EraseByPoint
                && MiniInkCanvas.EditingMode != InkCanvasEditingMode.EraseByStroke)
            {
                MiniEraserRing.Visibility = Visibility.Collapsed;
                MiniInkCanvas.Cursor = Cursors.Pen;
                return;
            }

            double d = (double)_eraserSize;
            if (d < 6d)
            {
                d = 6d;
            }

            Point p = e.GetPosition(MiniInkCanvas);
            MiniEraserRing.Width = d;
            MiniEraserRing.Height = d;
            Canvas.SetLeft(MiniEraserRing, p.X - d / 2d);
            Canvas.SetTop(MiniEraserRing, p.Y - d / 2d);
            MiniEraserRing.Visibility = Visibility.Visible;
            MiniInkCanvas.Cursor = Cursors.None;
        }

        private void MiniInkCanvas_MouseLeave(object sender, MouseEventArgs e)
        {
            if (MiniEraserRing != null)
            {
                MiniEraserRing.Visibility = Visibility.Collapsed;
            }
        }
    }
}
