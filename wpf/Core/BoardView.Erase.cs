using System;
using System.Collections.Generic;
using System.Windows;

namespace InkBoard
{
    /// <summary>橡皮：按点切割，只擦掉经过的一段，整笔不会整条消失</summary>
    public partial class BoardView
    {
        void EraseAt(Point wp)
        {
            var page = Doc.Cur;
            double radius = Math.Max(10, PenWidth * 1.6);
            for (int i = page.Objects.Count - 1; i >= 0; i--)
            {
                var o = page.Objects[i];
                if (o.Type == "stroke")
                {
                    if (!StrokeTouched(o, wp, radius)) continue;
                    var pieces = CutStroke(o, wp, radius);
                    page.Objects.RemoveAt(i);
                    for (int k = 0; k < pieces.Count; k++) page.Objects.Insert(i + k, pieces[k]);
                    if (_selected.Contains(o))
                    {
                        _selected.Remove(o);
                        foreach (var pc in pieces) _selected.Add(pc);
                    }
                    _erased = true;
                    InvalidateStatic();
                }
                else
                {
                    var bb = o.Bounds();
                    bb.Inflate(6, 6);
                    if (bb.Contains(wp) && Ink.Hit(o, wp, radius))
                    {
                        page.Objects.RemoveAt(i);
                        _selected.Remove(o);
                        _erased = true;
                        InvalidateStatic();
                    }
                }
            }
        }

        static bool StrokeTouched(Obj o, Point c, double radius)
        {
            double rr = radius + o.W * 0.5;
            foreach (var p in o.Pts) if ((p - c).Length <= rr) return true;
            for (int i = 1; i < o.Pts.Count; i++)
                if (Recognize.PerpDist(c, o.Pts[i - 1], o.Pts[i]) <= rr) return true;
            return false;
        }

        /// <summary>按点切割：只切掉经过橡皮圈的一段，笔迹分成几段保留</summary>
        static List<Obj> CutStroke(Obj o, Point c, double radius)
        {
            double rr = radius + o.W * 0.5;
            var dense = Densify(o.Pts, 3);
            var pieces = new List<Obj>();
            var run = new List<Point>();
            foreach (var p in dense)
            {
                if ((p - c).Length <= rr)
                {
                    FlushRun(run, o, pieces);
                    run = new List<Point>();
                }
                else run.Add(p);
            }
            FlushRun(run, o, pieces);
            return pieces;
        }

        static void FlushRun(List<Point> run, Obj src, List<Obj> outList)
        {
            if (run.Count < 2) return;
            var p = new Obj
            {
                Type = "stroke",
                Color = src.Color,
                W = src.W,
                Alpha = src.Alpha,
                Marker = src.Marker
            };
            p.Pts.AddRange(run);
            Ink.Build(p);
            outList.Add(p);
        }

        static List<Point> Densify(List<Point> pts, double step)
        {
            var res = new List<Point>();
            if (pts.Count == 0) return res;
            res.Add(pts[0]);
            for (int i = 1; i < pts.Count; i++)
            {
                var a = pts[i - 1];
                var b = pts[i];
                double d = (b - a).Length;
                int n = (int)Math.Floor(d / step);
                for (int k = 1; k <= n; k++)
                {
                    double t = (double)k / (n + 1);
                    res.Add(new Point(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t));
                }
                if (d > 0) res.Add(b);
            }
            return res;
        }

    }
}
