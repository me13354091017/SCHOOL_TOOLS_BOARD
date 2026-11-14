using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace InkBoard
{
    public partial class MainWindow
    {
        /* ------------------------------ 文字输入 ------------------------------ */
        string AskTextDialog(string init)
        {
            var win = new Window
            {
                Title = "文字",
                Width = 460,
                Height = 210,
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush(Color.FromRgb(0xF8, 0xFA, 0xFC))
            };
            var grid = new Grid { Margin = new Thickness(14) };
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var tb = new TextBox
            {
                Text = init ?? "",
                AcceptsReturn = true,
                FontSize = 18,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            Grid.SetRow(tb, 0);
            grid.Children.Add(tb);
            var ok = new Button { Content = "确定", Width = 92, Height = 34, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 10, 0, 0), Style = ToolStyle };
            Grid.SetRow(ok, 1);
            grid.Children.Add(ok);
            string result = null;
            ok.Click += (s, e) => { result = tb.Text; win.DialogResult = true; };
            win.Content = grid;
            win.Loaded += (s, e) => { tb.Focus(); tb.SelectAll(); };
            return win.ShowDialog() == true ? result : null;
        }
    }
}
