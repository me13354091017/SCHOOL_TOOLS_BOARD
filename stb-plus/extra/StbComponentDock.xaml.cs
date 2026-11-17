using System;
using System.Windows;
using System.Windows.Input;

namespace InkCanvasPlus
{
    /// <summary>组件面板：列出可用组件，点一行就开一扇对应的组件小窗。</summary>
    public partial class StbComponentDock : Window
    {
        private static StbComponentDock _instance;

        public static void ShowFor(Window owner)
        {
            if (_instance != null)
            {
                _instance.Activate();
                return;
            }

            var w = new StbComponentDock();
            if (owner != null && owner.IsLoaded)
            {
                w.Owner = owner;
            }
            _instance = w;
            w.Closed += (s, e) => { _instance = null; };

            Rect wa = SystemParameters.WorkArea;
            w.Left = Math.Max(wa.Left, wa.Right - w.Width - 40);
            w.Top = Math.Max(wa.Top, wa.Top + 110);

            w.Show();
            w.Activate();
        }

        public StbComponentDock()
        {
            InitializeComponent();
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }
            try
            {
                DragMove();
            }
            catch
            {
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BtnCompClock_Click(object sender, RoutedEventArgs e)
        {
            StbComponentWindow.ShowFor(this, new StbClockComponent());
        }

        private void BtnCompCountdown_Click(object sender, RoutedEventArgs e)
        {
            StbComponentWindow.ShowFor(this, new StbCountdownComponent());
        }

        private void BtnCompRandom_Click(object sender, RoutedEventArgs e)
        {
            StbComponentWindow.ShowFor(this, new StbRandomPickerComponent());
        }
    }
}
