using System.Windows;
using System.Windows.Input;

namespace InkBoard
{
    /// <summary>快捷键与页面切换</summary>
    public partial class MainWindow
    {
        /* ------------------------------ 键盘 ------------------------------ */
        void OnKeyDown(object s, KeyEventArgs e)
        {
            bool ctrl = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
            if (ctrl)
            {
                switch (e.Key)
                {
                    case Key.Z: Undo(); e.Handled = true; return;
                    case Key.Y: Redo(); e.Handled = true; return;
                    case Key.S: SaveDoc(); e.Handled = true; return;
                    case Key.O: OpenDoc(); e.Handled = true; return;
                    case Key.D: _board.DuplicateSelection(); e.Handled = true; return;
                    case Key.A: SelectAll(); e.Handled = true; return;
                    case Key.D0: _board.FitPage(); e.Handled = true; return;
                }
                return;
            }
            switch (e.Key)
            {
                case Key.P: SetTool(ToolKind.Pen); break;
                case Key.M: SetTool(ToolKind.Marker); break;
                case Key.E: SetTool(ToolKind.Eraser); break;
                case Key.S: SetTool(ToolKind.Shape); break;
                case Key.V: SetTool(ToolKind.Select); break;
                case Key.T: SetTool(ToolKind.Text); break;
                case Key.L: SetTool(ToolKind.Laser); break;
                case Key.H: SetTool(ToolKind.Hand); break;
                case Key.Delete: _board.DeleteSelection(); break;
                case Key.Escape:
                    if (Curtain.Visibility == Visibility.Visible) Curtain.Visibility = Visibility.Collapsed;
                    else if (SpotLayer.Visibility == Visibility.Visible) SpotLayer.Visibility = Visibility.Collapsed;
                    else if (_presenting) TogglePresent();
                    break;
                case Key.F: TogglePresent(); break;
                case Key.Left: PrevPage(); break;
                case Key.Right: NextPage(); break;
                default: return;
            }
            e.Handled = true;
        }

        void SelectAll()
        {
            _board.Selection.Clear();
            foreach (var o in _doc.Cur.Objects) _board.Selection.Add(o);
            _board.InvalidateLive();
        }

        void PrevPage() { if (_doc.Current > 0) { _doc.Current--; _board.InvalidateAll(); RefreshPages(); } }

        void NextPage()
        {
            if (_doc.Current < _doc.Pages.Count - 1) { _doc.Current++; _board.InvalidateAll(); RefreshPages(); }
        }

        void Undo()
        {
            var d = _history.Undo();
            if (d == null) return;
            _doc = d;
            _board.SetDoc(d);
            RefreshPages();
        }

        void Redo()
        {
            var d = _history.Redo();
            if (d == null) return;
            _doc = d;
            _board.SetDoc(d);
            RefreshPages();
        }

    }
}
