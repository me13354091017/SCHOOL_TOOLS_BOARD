using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace InkCanvasPlus
{
    public partial class StbMathBox
    {
        private sealed class HitSlot
        {
            internal Slot Slot;
            internal Rect Rect;
            internal double Scale = 1;
            internal double Pad;
        }

        private readonly List<HitSlot> _hitSlots = new List<HitSlot>();

        /// <summary>鼠标点在哪个框（或哪段文字），就把光标放到哪：点方框即可直接填数。</summary>
        internal void StbClickHere(Point p)
        {
            Focus();
            Keyboard.Focus(this);

            Slot target = _root;
            HitSlot hit = null;
            for (int i = _hitSlots.Count - 1; i >= 0; i--)
            {
                if (_hitSlots[i].Rect.Contains(p))
                {
                    hit = _hitSlots[i];
                    break;
                }
            }
            if (hit != null)
            {
                target = hit.Slot;
            }

            double k = hit != null && hit.Scale > 0 ? hit.Scale : 1;
            double localX = hit != null ? (p.X - hit.Rect.Left) / k - hit.Pad : p.X - 8;
            int idx = 0;
            int off = 0;
            double cx = 0;
            bool placed = false;
            for (int i = 0; i < target.Nodes.Count; i++)
            {
                Node n = target.Nodes[i];
                if (localX < cx + n.Size.Width)
                {
                    idx = i;
                    if (n.Text != null)
                    {
                        off = NearestOffset(n.Text, localX - cx);
                    }
                    placed = true;
                    break;
                }
                cx += n.Size.Width;
            }
            if (!placed)
            {
                idx = target.Nodes.Count;
            }

            _caret.Slot = target;
            _caret.Idx = idx;
            _caret.Off = off;
            _focused = true;
            InvalidateVisual();
        }

        private int NearestOffset(string text, double dx)
        {
            int best = 0;
            double bestGap = Math.Abs(dx);
            for (int i = 1; i <= text.Length; i++)
            {
                double w = Ft(text.Substring(0, i), Fs).Width;
                double gap = Math.Abs(dx - w);
                if (gap < bestGap)
                {
                    bestGap = gap;
                    best = i;
                }
            }
            return best;
        }
    }
}
