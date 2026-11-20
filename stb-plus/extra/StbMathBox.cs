using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace InkCanvasPlus
{
    /// <summary>
    /// 希沃 MathEditor 式二维公式输入框（自有实现）：真分数竖排、根号横线、上下标、
    /// □ 占位框、光标在框间跳（←→）、退格删框；序列化为引擎文本供绘图。
    /// </summary>
    public partial class StbMathBox : FrameworkElement
    {
        public class Node
        {
            public string Text;          // 非空=普通文本段
            public string Kind;          // func/logb/frac/sqrt/nroot/pow/abs
            public string Name;
            public List<Slot> Slots;
            public Size Size;
        }

        public class Slot
        {
            public List<Node> Nodes = new List<Node>();
            public Node Owner;
            public int Index;
            public Slot Parent;
            public Size Size;
        }

        private sealed class Caret
        {
            internal Slot Slot;
            internal int Idx;
            internal int Off;
        }

        private readonly Slot _root = new Slot();
        private readonly Caret _caret = new Caret();
        private bool _focused;

        public event EventHandler TextChanged;

        public double FontSize { get; set; } = 14;

        private static readonly Brush Ink = new SolidColorBrush(Color.FromRgb(0x1F, 0x23, 0x29));
        private static readonly Brush Ghost = new SolidColorBrush(Color.FromRgb(0xB6, 0xBC, 0xC6));
        private static readonly Brush CaretBrush = new SolidColorBrush(Color.FromRgb(0x1F, 0x23, 0x29));
        private static readonly Brush BoxBg = Brushes.White;
        private static readonly Pen BoxPen = new Pen(new SolidColorBrush(Color.FromRgb(0xE3, 0xE6, 0xEC)), 1);
        private static readonly Pen BarPen = new Pen(Ink, 1.2);

        public StbMathBox()
        {
            Focusable = true;
            Cursor = Cursors.IBeam;
            _caret.Slot = _root;
            _caret.Idx = 0;
            _caret.Off = 0;
            SnapsToDevicePixels = true;
        }

        private double Fs { get { return FontSize > 0 ? FontSize : 14; } }
        private double LineH { get { return Fs * 1.4; } }

        private FormattedText Ft(string text, double size)
        {
            return new FormattedText(text == null ? string.Empty : text, CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight, new Typeface("Microsoft YaHei UI"),
                size, Ink);
        }

        private static Node Run(string t)
        {
            return new Node { Text = t };
        }

        private Slot NewSlot(Node owner, int i, Slot parent)
        {
            return new Slot { Owner = owner, Index = i, Parent = parent };
        }

        private Node Tpl(string kind, string name, Slot parent, params string[] prefill)
        {
            Node n = new Node { Kind = kind, Name = name, Slots = new List<Slot>() };
            int count = kind == "frac" || kind == "nroot" || kind == "logb" || kind == "pow" ? 2 : 1;
            for (int i = 0; i < count; i++)
            {
                Slot s = NewSlot(n, i, parent);
                if (i < prefill.Length && !string.IsNullOrEmpty(prefill[i]))
                {
                    s.Nodes.Add(Run(prefill[i]));
                }
                n.Slots.Add(s);
            }
            return n;
        }

        // ================= 编辑操作 =================

        public void InsertChar(char c)
        {
            Slot s = _caret.Slot;
            int idx = _caret.Idx;
            if (idx < s.Nodes.Count && s.Nodes[idx].Text != null)
            {
                Node r = s.Nodes[idx];
                r.Text = r.Text.Substring(0, _caret.Off) + c + r.Text.Substring(_caret.Off);
                _caret.Off++;
            }
            else if (idx > 0 && s.Nodes[idx - 1].Text != null)
            {
                Node r = s.Nodes[idx - 1];
                r.Text += c;
                _caret.Idx = idx - 1;
                _caret.Off = r.Text.Length;
            }
            else
            {
                s.Nodes.Insert(idx, Run(c.ToString()));
                _caret.Off = 1;
            }
            Changed();
        }

        public void Backspace()
        {
            Slot s = _caret.Slot;
            int idx = _caret.Idx;
            if (idx < s.Nodes.Count && s.Nodes[idx].Text != null)
            {
                Node r = s.Nodes[idx];
                if (_caret.Off > 0)
                {
                    r.Text = r.Text.Remove(_caret.Off - 1, 1);
                    _caret.Off--;
                    if (r.Text.Length == 0)
                    {
                        s.Nodes.RemoveAt(idx);
                    }
                }
                else if (idx > 0)
                {
                    Node prev = s.Nodes[idx - 1];
                    if (prev.Text != null)
                    {
                        int pl = prev.Text.Length;
                        prev.Text += r.Text;
                        s.Nodes.RemoveAt(idx);
                        _caret.Idx = idx - 1;
                        _caret.Off = pl;
                    }
                    else
                    {
                        s.Nodes.RemoveAt(idx - 1);
                        _caret.Idx = idx - 1;
                        _caret.Off = 0;
                    }
                }
            }
            else if (idx < s.Nodes.Count)
            {
                s.Nodes.RemoveAt(idx);
            }
            else if (idx > 0)
            {
                Node prev = s.Nodes[idx - 1];
                if (prev.Text != null)
                {
                    prev.Text = prev.Text.Substring(0, prev.Text.Length - 1);
                    _caret.Idx = idx - 1;
                    _caret.Off = prev.Text.Length;
                    if (prev.Text.Length == 0)
                    {
                        s.Nodes.RemoveAt(idx - 1);
                        _caret.Idx = idx - 1;
                    }
                }
                else
                {
                    s.Nodes.RemoveAt(idx - 1);
                    _caret.Idx = idx - 1;
                }
            }
            else if (s.Owner != null)
            {
                Slot p = s.Parent;
                int oi = s.Parent.Nodes.IndexOf(s.Owner);
                p.Nodes.Remove(s.Owner);
                _caret.Slot = p;
                _caret.Idx = oi;
                _caret.Off = 0;
            }
            Changed();
        }

        public void MoveRight()
        {
            Slot s = _caret.Slot;
            int idx = _caret.Idx;
            if (idx < s.Nodes.Count)
            {
                Node n = s.Nodes[idx];
                if (n.Text != null)
                {
                    if (_caret.Off < n.Text.Length) { _caret.Off++; }
                    else { _caret.Idx = idx + 1; _caret.Off = 0; }
                }
                else
                {
                    _caret.Slot = n.Slots[0];
                    _caret.Idx = 0;
                    _caret.Off = 0;
                }
            }
            else if (s.Owner != null)
            {
                _caret.Slot = s.Parent;
                _caret.Idx = s.Parent.Nodes.IndexOf(s.Owner) + 1;
                _caret.Off = 0;
            }
            InvalidateVisual();
        }

        public void MoveLeft()
        {
            Slot s = _caret.Slot;
            int idx = _caret.Idx;
            if (idx < s.Nodes.Count && s.Nodes[idx].Text != null && _caret.Off > 0)
            {
                _caret.Off--;
            }
            else if (idx > 0)
            {
                Node prev = s.Nodes[idx - 1];
                if (prev.Text != null)
                {
                    _caret.Idx = idx - 1;
                    _caret.Off = prev.Text.Length;
                }
                else
                {
                    Slot last = prev.Slots[prev.Slots.Count - 1];
                    _caret.Slot = last;
                    _caret.Idx = last.Nodes.Count;
                    _caret.Off = 0;
                }
            }
            else if (s.Owner != null)
            {
                _caret.Slot = s.Parent;
                _caret.Idx = s.Parent.Nodes.IndexOf(s.Owner);
                _caret.Off = 0;
            }
            InvalidateVisual();
        }

        /// <summary>kind: func:sin / logb / frac / sqrt / nroot / pow(x^□) / powx(□ˣ) / abs</summary>
        public void InsertTemplate(string kind)
        {
            Slot s = _caret.Slot;
            int idx = _caret.Idx;
            Node n;
            int target = 0;
            if (kind.StartsWith("func:"))
            {
                n = Tpl("func", kind.Substring(5), s);
            }
            else if (kind == "pow")
            {
                n = Tpl("pow", null, s, "x", null);
                target = 1;
            }
            else if (kind == "powx")
            {
                n = Tpl("pow", null, s, null, "x");
                target = 0;
            }
            else if (kind == "logb")
            {
                n = Tpl("logb", null, s, "2", null);
                target = 1;
            }
            else
            {
                n = Tpl(kind, null, s);
            }

            if (idx < s.Nodes.Count && s.Nodes[idx].Text != null)
            {
                Node r = s.Nodes[idx];
                string left = r.Text.Substring(0, _caret.Off);
                string right = r.Text.Substring(_caret.Off);
                if (left.Length > 0) { r.Text = left; s.Nodes.Insert(idx + 1, n); }
                else { s.Nodes[idx] = n; }
                if (right.Length > 0)
                {
                    s.Nodes.Insert(s.Nodes.IndexOf(n) + 1, Run(right));
                }
            }
            else
            {
                s.Nodes.Insert(idx, n);
            }
            _caret.Slot = n.Slots[target];
            _caret.Idx = 0;
            _caret.Off = 0;
            Changed();
        }

        private void Changed()
        {
            InvalidateVisual();
            if (TextChanged != null)
            {
                TextChanged(this, EventArgs.Empty);
            }
        }

        protected override void OnGotFocus(RoutedEventArgs e)
        {
            _focused = true;
            InvalidateVisual();
            base.OnGotFocus(e);
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            _focused = false;
            InvalidateVisual();
            base.OnLostFocus(e);
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            Focus();
            base.OnMouseDown(e);
        }

        protected override void OnTextInput(TextCompositionEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Text))
            {
                foreach (char c in e.Text)
                {
                    if (c >= ' ')
                    {
                        InsertChar(c);
                    }
                }
                e.Handled = true;
            }
            base.OnTextInput(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Back) { Backspace(); e.Handled = true; }
            else if (e.Key == Key.Left) { MoveLeft(); e.Handled = true; }
            else if (e.Key == Key.Right) { MoveRight(); e.Handled = true; }
            base.OnKeyDown(e);
        }
    }
}
