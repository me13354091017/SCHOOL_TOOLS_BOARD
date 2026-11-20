using System;
using System.Windows;
using System.Windows.Media;

namespace InkCanvasPlus
{
    public partial class StbMathBox
    {
        // ================= 布局与绘制 =================

        private Size MeasureList(Slot s)
        {
            double w = 0, h = LineH;
            foreach (Node n in s.Nodes)
            {
                Size sz = MeasureNode(n);
                n.Size = sz;
                w += sz.Width;
                h = Math.Max(h, sz.Height);
            }
            s.Size = new Size(w, h);
            return s.Size;
        }

        private Size SlotSize(Slot s)
        {
            Size sz = MeasureList(s);
            return new Size(Math.Max(sz.Width, 12) + 6, Math.Max(sz.Height, LineH * 0.8));
        }

        private Size MeasureNode(Node n)
        {
            if (n.Text != null)
            {
                return new Size(Ft(n.Text, Fs).Width, LineH);
            }
            double w = 0, h = LineH;
            switch (n.Kind)
            {
                case "func":
                    {
                        Size a = SlotSize(n.Slots[0]);
                        w = Ft(n.Name, Fs).Width + a.Width + 2;
                        h = Math.Max(LineH, a.Height);
                        break;
                    }
                case "logb":
                    {
                        Size b = SlotSize(n.Slots[0]);
                        Size a = SlotSize(n.Slots[1]);
                        w = Ft("log", Fs).Width + b.Width * 0.75 + a.Width + 2;
                        h = Math.Max(LineH, a.Height + b.Height * 0.4);
                        break;
                    }
                case "frac":
                    {
                        Size a = SlotSize(n.Slots[0]);
                        Size b = SlotSize(n.Slots[1]);
                        w = Math.Max(a.Width, b.Width) + 8;
                        h = a.Height + b.Height + 6;
                        break;
                    }
                case "sqrt":
                    {
                        Size a = SlotSize(n.Slots[0]);
                        w = 12 + a.Width + 2;
                        h = a.Height + 5;
                        break;
                    }
                case "nroot":
                    {
                        Size i = SlotSize(n.Slots[0]);
                        Size a = SlotSize(n.Slots[1]);
                        w = i.Width * 0.7 + 12 + a.Width + 2;
                        h = a.Height + 5 + i.Height * 0.3;
                        break;
                    }
                case "pow":
                    {
                        Size b = SlotSize(n.Slots[0]);
                        Size e = SlotSize(n.Slots[1]);
                        w = b.Width + e.Width * 0.75 + 2;
                        h = Math.Max(LineH, b.Height * 0.6 + e.Height * 0.75);
                        break;
                    }
                case "abs":
                    {
                        Size a = SlotSize(n.Slots[0]);
                        w = a.Width + 10;
                        h = Math.Max(LineH, a.Height);
                        break;
                    }
            }
            return new Size(w, h);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            Size sz = MeasureList(_root);
            return new Size(sz.Width + 16, Math.Max(sz.Height + 10, 30));
        }

        protected override void OnRender(DrawingContext dc)
        {
            double w = ActualWidth, h = ActualHeight;
            dc.DrawRectangle(BoxBg, BoxPen, new Rect(0.5, 0.5, Math.Max(0, w - 1), Math.Max(0, h - 1)));
            MeasureList(_root);
            DrawList(dc, _root, 8, (h - _root.Size.Height) / 2, _root.Size.Height);
        }

        private void DrawList(DrawingContext dc, Slot s, double x, double y, double h)
        {
            double cx = x;
            foreach (Node n in s.Nodes)
            {
                double ny = y + (h - n.Size.Height) / 2;
                DrawNode(dc, n, cx, ny);
                cx += n.Size.Width;
            }
            if (_focused && _caret.Slot == s)
            {
                double px = x;
                for (int i = 0; i < _caret.Idx && i < s.Nodes.Count; i++)
                {
                    px += s.Nodes[i].Size.Width;
                }
                if (_caret.Idx < s.Nodes.Count && s.Nodes[_caret.Idx].Text != null)
                {
                    string pre = s.Nodes[_caret.Idx].Text.Substring(0, Math.Min(_caret.Off, s.Nodes[_caret.Idx].Text.Length));
                    px += Ft(pre, Fs).Width;
                }
                dc.DrawLine(new Pen(CaretBrush, 1.4), new Point(px, y + 2), new Point(px, y + h - 2));
            }
        }

        private void DrawSlot(DrawingContext dc, Slot s, double x, double y, double w, double h)
        {
            Size inner = MeasureList(s);
            if (s.Nodes.Count == 0)
            {
                FormattedText g = Ft("□", Fs * 0.9);
                g.SetForegroundBrush(Ghost);
                dc.DrawText(g, new Point(x + (w - g.Width) / 2, y + (h - g.Height) / 2));
            }
            else
            {
                DrawList(dc, s, x + 3, y + (h - inner.Height) / 2, inner.Height);
            }
            if (_focused && _caret.Slot == s && s.Nodes.Count == 0)
            {
                dc.DrawLine(new Pen(CaretBrush, 1.4), new Point(x + 3, y + 2), new Point(x + 3, y + h - 2));
            }
        }

        private void DrawNode(DrawingContext dc, Node n, double x, double y)
        {
            if (n.Text != null)
            {
                dc.DrawText(Ft(n.Text, Fs), new Point(x, y));
                return;
            }
            double w = n.Size.Width, h = n.Size.Height;
            switch (n.Kind)
            {
                case "func":
                    {
                        dc.DrawText(Ft(n.Name, Fs), new Point(x, y + (h - LineH) / 2));
                        double nx = x + Ft(n.Name, Fs).Width + 2;
                        Size a = SlotSize(n.Slots[0]);
                        DrawSlot(dc, n.Slots[0], nx, y + (h - a.Height) / 2, a.Width, a.Height);
                        break;
                    }
                case "logb":
                    {
                        dc.DrawText(Ft("log", Fs), new Point(x, y + (h - LineH) / 2));
                        double lx = x + Ft("log", Fs).Width;
                        Size b = SlotSize(n.Slots[0]);
                        Size a = SlotSize(n.Slots[1]);
                        DrawSlotScaled(dc, n.Slots[0], lx, y + h - b.Height * 0.75, 0.75);
                        DrawSlot(dc, n.Slots[1], lx + b.Width * 0.75 + 2, y + (h - a.Height) / 2, a.Width, a.Height);
                        break;
                    }
                case "frac":
                    {
                        Size a = SlotSize(n.Slots[0]);
                        Size b = SlotSize(n.Slots[1]);
                        double nw = Math.Max(a.Width, b.Width);
                        DrawSlot(dc, n.Slots[0], x + 4 + (nw - a.Width) / 2, y, a.Width, a.Height);
                        dc.DrawLine(BarPen, new Point(x + 2, y + a.Height + 2.5), new Point(x + w - 2, y + a.Height + 2.5));
                        DrawSlot(dc, n.Slots[1], x + 4 + (nw - b.Width) / 2, y + a.Height + 6, b.Width, b.Height);
                        break;
                    }
                case "sqrt":
                    {
                        Size a = SlotSize(n.Slots[0]);
                        dc.DrawText(Ft("√", Fs * 1.1), new Point(x, y + 1));
                        dc.DrawLine(BarPen, new Point(x + 10, y + 1.5), new Point(x + w, y + 1.5));
                        DrawSlot(dc, n.Slots[0], x + 12, y + 4, a.Width, a.Height);
                        break;
                    }
                case "nroot":
                    {
                        Size i = SlotSize(n.Slots[0]);
                        Size a = SlotSize(n.Slots[1]);
                        DrawSlotScaled(dc, n.Slots[0], x, y, 0.7);
                        double rx = x + i.Width * 0.7;
                        dc.DrawText(Ft("√", Fs * 1.1), new Point(rx, y + i.Height * 0.3));
                        dc.DrawLine(BarPen, new Point(rx + 10, y + i.Height * 0.3 + 1.5), new Point(x + w, y + i.Height * 0.3 + 1.5));
                        DrawSlot(dc, n.Slots[1], rx + 12, y + i.Height * 0.3 + 4, a.Width, a.Height);
                        break;
                    }
                case "pow":
                    {
                        Size b = SlotSize(n.Slots[0]);
                        Size e = SlotSize(n.Slots[1]);
                        DrawSlot(dc, n.Slots[0], x, y + (h - b.Height) / 2 + e.Height * 0.25, b.Width, b.Height);
                        DrawSlotScaled(dc, n.Slots[1], x + b.Width + 2, y, 0.75);
                        break;
                    }
                case "abs":
                    {
                        Size a = SlotSize(n.Slots[0]);
                        dc.DrawLine(BarPen, new Point(x + 2, y + 1), new Point(x + 2, y + h - 1));
                        DrawSlot(dc, n.Slots[0], x + 5, y + (h - a.Height) / 2, a.Width, a.Height);
                        dc.DrawLine(BarPen, new Point(x + w - 2, y + 1), new Point(x + w - 2, y + h - 1));
                        break;
                    }
            }
        }

        private void DrawSlotScaled(DrawingContext dc, Slot s, double x, double y, double scale)
        {
            Size sz = SlotSize(s);
            dc.PushTransform(new ScaleTransform(scale, scale, x, y));
            DrawSlot(dc, s, x, y, sz.Width, sz.Height);
            dc.Pop();
        }
    }
}
