using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace InkCanvasPlus
{
    /// <summary>
    /// 组件窗口：一个组件一扇小窗，同一种组件只会开一扇（再点就激活已有的）。
    /// 顶部标题条可拖动，右上角关闭；关闭时回调组件的 OnClose 释放定时器。
    /// </summary>
    public partial class StbComponentWindow : Window
    {
        private static readonly Dictionary<string, StbComponentWindow> _open = new Dictionary<string, StbComponentWindow>();
        private StbComponentBase _component;

        public static void ShowFor(Window owner, StbComponentBase component)
        {
            if (component == null)
            {
                return;
            }

            StbComponentWindow existing;
            if (_open.TryGetValue(component.Id, out existing))
            {
                existing.Activate();
                return;
            }

            var w = new StbComponentWindow();
            w._component = component;
            w.TextBlockTitle.Text = component.Title;
            w.HostContent.Content = component.BuildView();

            if (owner != null && owner.IsLoaded)
            {
                w.Owner = owner;
            }

            _open[component.Id] = w;
            string id = component.Id;
            StbComponentBase comp = component;
            w.Closed += (s, e) =>
            {
                _open.Remove(id);
                comp.OnClose();
            };

            Rect wa = SystemParameters.WorkArea;
            w.Left = Math.Max(wa.Left, wa.Right - w.Width - 620);
            w.Top = Math.Max(wa.Top, wa.Top + 120);

            w.Show();
            w.Activate();
        }

        public StbComponentWindow()
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
    }
}
