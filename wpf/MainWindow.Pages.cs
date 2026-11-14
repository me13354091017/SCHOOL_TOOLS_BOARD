using System.Text.Json;
using System.Windows;
using System.Windows.Media;

namespace InkBoard
{
    /// <summary>页面增删复制</summary>
    public partial class MainWindow
    {
        static Brush FromHex(string hex)
        {
            try { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)); }
            catch (System.Exception) { return Brushes.Black; }
        }

        void AfterPageChange()
        {
            _board.InvalidateAll();
            RefreshPages();
            _history.Push(_doc);
        }

        void OnAddPage(object s, RoutedEventArgs e)
        {
            var p = new Page();
            if (_doc.Cur != null)
            {
                p.Bg = _doc.Cur.Bg;
                p.Color = _doc.Cur.Color;
            }
            _doc.Pages.Add(p);
            _doc.Current = _doc.Pages.Count - 1;
            AfterPageChange();
        }

        void OnDupPage(object s, RoutedEventArgs e)
        {
            if (_doc.Cur == null) return;
            string json = JsonSerializer.Serialize(_doc.Cur, History.Json);
            var copy = JsonSerializer.Deserialize<Page>(json, History.Json);
            if (copy == null) return;
            copy.BackImage = null;
            _doc.Pages.Insert(_doc.Current + 1, copy);
            _doc.Current = _doc.Current + 1;
            AfterPageChange();
        }

        void OnDelPage(object s, RoutedEventArgs e)
        {
            if (_doc.Pages.Count <= 1) return;
            _doc.Pages.RemoveAt(_doc.Current);
            if (_doc.Current >= _doc.Pages.Count) _doc.Current = _doc.Pages.Count - 1;
            AfterPageChange();
        }
    }
}
