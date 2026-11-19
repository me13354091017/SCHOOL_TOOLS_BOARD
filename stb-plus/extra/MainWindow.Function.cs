using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

namespace InkCanvasPlus
{
    /// <summary>
    /// 「插入函数」相关：浮动栏 f(x) 按钮、函数画板入口，以及把画好的坐标图作为可拖动图片放到白板上。
    /// </summary>
    public partial class MainWindow
    {
        private Canvas _stbFuncLayer;

        private void StbBtnInsertFunc_Click(object sender, RoutedEventArgs e)
        {
            StbOpenFunction();
        }

        private void SymbolIconFunc_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lastBorderMouseDownObject != sender || inkCanvas.Visibility != Visibility.Visible)
            {
                return;
            }
            try
            {
                BorderTools.Visibility = Visibility.Collapsed;
                GridNotifications.Visibility = Visibility.Collapsed;
            }
            catch
            {
            }
            StbOpenFunction();
        }

        internal void StbApplyFuncButtonVisibility()
        {
            try
            {
                if (StbBorderFunc != null)
                {
                    StbBorderFunc.Visibility = StbPrefs.FuncButton ? Visibility.Visible : Visibility.Collapsed;
                }
            }
            catch
            {
            }
        }

        internal void StbOpenFunction()
        {
            StbFunctionWindow.ShowFor(this);
        }

        private Canvas StbEnsureFuncLayer()
        {
            if (_stbFuncLayer != null)
            {
                return _stbFuncLayer;
            }
            Grid root = Content as Grid;
            if (root == null)
            {
                return null;
            }

            Canvas layer = new Canvas();
            layer.Background = null;
            Grid.SetRowSpan(layer, Math.Max(1, root.RowDefinitions.Count));
            Grid.SetColumnSpan(layer, Math.Max(1, root.ColumnDefinitions.Count));
            Panel.SetZIndex(layer, 5000);
            root.Children.Add(layer);
            _stbFuncLayer = layer;
            return layer;
        }

        /// <summary>把函数画板渲染出的位图插入白板（可拖动、右键删除）。</summary>
        internal void StbInsertFunctionImage(BitmapSource bmp, string caption)
        {
            try
            {
                Canvas layer = StbEnsureFuncLayer();
                if (layer == null || bmp == null)
                {
                    return;
                }

                double width = 520d;
                double height = width * ((double)bmp.PixelHeight / Math.Max(1d, bmp.PixelWidth));

                Image img = new Image
                {
                    Source = bmp,
                    Width = width,
                    Height = height,
                    Stretch = Stretch.Fill,
                    IsHitTestVisible = false
                };

                TextBlock cap = new TextBlock
                {
                    Text = caption,
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x8A, 0x90, 0x9A)),
                    Margin = new Thickness(2, 4, 2, 0),
                    IsHitTestVisible = false
                };

                StackPanel body = new StackPanel();
                body.Children.Add(img);
                body.Children.Add(cap);

                Border card = new Border
                {
                    Background = Brushes.White,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(0xD8, 0xDD, 0xE5)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(6),
                    Child = body,
                    ToolTip = "拖动可移动，右键删除",
                    Cursor = Cursors.SizeAll
                };
                card.Effect = new DropShadowEffect
                {
                    BlurRadius = 12,
                    ShadowDepth = 2,
                    Opacity = 0.25
                };

                layer.Children.Add(card);

                double left = Math.Max(20d, (layer.ActualWidth - width) / 2d);
                double top = 90d;
                Canvas.SetLeft(card, left);
                Canvas.SetTop(card, top);

                Point start = new Point();
                bool dragging = false;

                card.MouseLeftButtonDown += (s, ev) =>
                {
                    dragging = true;
                    start = ev.GetPosition(layer);
                    card.CaptureMouse();
                    ev.Handled = true;
                };
                card.MouseMove += (s, ev) =>
                {
                    if (!dragging)
                    {
                        return;
                    }
                    Point p = ev.GetPosition(layer);
                    Canvas.SetLeft(card, Math.Max(0d, Canvas.GetLeft(card) + p.X - start.X));
                    Canvas.SetTop(card, Math.Max(0d, Canvas.GetTop(card) + p.Y - start.Y));
                    start = p;
                };
                card.MouseLeftButtonUp += (s, ev) =>
                {
                    dragging = false;
                    card.ReleaseMouseCapture();
                    ev.Handled = true;
                };
                card.MouseRightButtonUp += (s, ev) =>
                {
                    layer.Children.Remove(card);
                    ev.Handled = true;
                };

                ShowNotification("已插入函数图像 " + caption);
            }
            catch (Exception ex)
            {
                try
                {
                    ShowNotification("插入函数图像失败：" + ex.Message);
                }
                catch
                {
                }
            }
        }
    }
}
