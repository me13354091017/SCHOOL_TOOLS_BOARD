using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace InkBoard
{
    /// <summary>课堂工具</summary>
    public partial class MainWindow
    {
        /* ------------------------------ 课堂工具 ------------------------------ */
        void TogglePresent()
        {
            _presenting = !_presenting;
            if (_presenting)
            {
                WindowStyle = WindowStyle.None;
                WindowState = WindowState.Maximized;
            }
            else
            {
                WindowStyle = WindowStyle.SingleBorderWindow;
                WindowState = WindowState.Maximized;
            }
        }

        void ToggleTimer()
        {
            if (TimerChip.Visibility == Visibility.Visible)
            {
                TimerChip.Visibility = Visibility.Collapsed;
                if (_timer != null) _timer.Stop();
                return;
            }
            TimerChip.Visibility = Visibility.Visible;
            _seconds = 0;
            TimerText.Text = "00:00";
            if (_timer == null)
            {
                _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                _timer.Tick += (s, e) =>
                {
                    _seconds++;
                    TimerText.Text = string.Format("{0:00}:{1:00}", _seconds / 60, _seconds % 60);
                };
            }
            _timer.Start();
        }

        void ToggleSpot()
        {
            if (SpotLayer.Visibility == Visibility.Visible)
            {
                SpotLayer.Visibility = Visibility.Collapsed;
                return;
            }
            if (_spot == null)
            {
                _spot = new RadialGradientBrush { MappingMode = BrushMappingMode.Absolute };
                _spot.GradientStops.Add(new GradientStop(Color.FromArgb(0, 0, 0, 0), 0.0));
                _spot.GradientStops.Add(new GradientStop(Color.FromArgb(0, 0, 0, 0), 0.72));
                _spot.GradientStops.Add(new GradientStop(Color.FromArgb(235, 6, 10, 18), 0.88));
                SpotRect.Fill = _spot;
            }
            SpotLayer.Visibility = Visibility.Visible;
            UpdateSpot(_pointer);
        }

        void UpdateSpot(Point p)
        {
            if (_spot == null) return;
            _spot.Center = p;
            _spot.GradientOrigin = p;
            _spot.RadiusX = _spotRadius;
            _spot.RadiusY = _spotRadius;
        }

        void ToggleCurtain()
        {
            if (Curtain.Visibility == Visibility.Visible) { Curtain.Visibility = Visibility.Collapsed; return; }
            Curtain.Visibility = Visibility.Visible;
            Curtain.Height = 90;
        }

        void OnCurtainDown(object s, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2) { Curtain.Visibility = Visibility.Collapsed; return; }
            _draggingCurtain = true;
            Curtain.CaptureMouse();
            e.Handled = true;
        }

        void OnCurtainMove(object s, MouseEventArgs e)
        {
            if (!_draggingCurtain) return;
            double y = e.GetPosition(this).Y;
            Curtain.Height = Math.Max(0, Math.Min(ActualHeight, y));
        }

        void OnCurtainUp(object s, MouseButtonEventArgs e)
        {
            _draggingCurtain = false;
            Curtain.ReleaseMouseCapture();
        }

        void OnAnyMove(object s, MouseEventArgs e)
        {
            _pointer = e.GetPosition(this);
            if (SpotLayer.Visibility == Visibility.Visible) UpdateSpot(_pointer);
        }

        void OnAnyWheel(object s, MouseWheelEventArgs e)
        {
            if (SpotLayer.Visibility != Visibility.Visible) return;
            _spotRadius = Math.Max(60, Math.Min(900, _spotRadius + (e.Delta > 0 ? 40 : -40)));
            UpdateSpot(_pointer);
        }

    }
}
