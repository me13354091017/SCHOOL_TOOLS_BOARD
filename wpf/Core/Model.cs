using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;

namespace InkBoard
{
    /// <summary>单个绘制对象：stroke（笔迹）/ shape（图形）/ text（文字）</summary>
    public class Obj
    {
        public string Id = Guid.NewGuid().ToString("N").Substring(0, 8);
        public string Type = "stroke";
        public string Color = "#111827";
        public double W = 7;
        public double Alpha = 1;
        public bool Marker;
        public List<Point> Pts = new List<Point>();
        public List<double> Ws = new List<double>();
        public string Shape = "line";
        public double X1, Y1, X2, Y2;
        public string Expr = "sin(x)";
        public bool Fill;
        public string FillColor = "#0e9f76";
        public bool Dashed;
        public double X, Y, Size = 40;
        public string Text = "";

        [System.Text.Json.Serialization.JsonIgnore] public Geometry Geo;
        [System.Text.Json.Serialization.JsonIgnore] public Geometry Aux;

        public Rect Bounds()
        {
            if (Type == "stroke")
            {
                if (Pts == null || Pts.Count == 0) return new Rect(0, 0, 0, 0);
                double x0 = double.MaxValue, y0 = double.MaxValue, x1 = -double.MaxValue, y1 = -double.MaxValue;
                foreach (var p in Pts)
                {
                    if (p.X < x0) x0 = p.X;
                    if (p.X > x1) x1 = p.X;
                    if (p.Y < y0) y0 = p.Y;
                    if (p.Y > y1) y1 = p.Y;
                }
                double m = Math.Max(6, W * 1.6);
                return new Rect(x0 - m, y0 - m, x1 - x0 + 2 * m, y1 - y0 + 2 * m);
            }
            if (Type == "text")
            {
                int lines = 1;
                foreach (char c in (Text ?? "")) if (c == '\n') lines++;
                double w = Math.Max(24, Size * 0.7 * (Text == null ? 4 : Math.Max(4, Text.Length)));
                return new Rect(X, Y, w, Size * 1.35 * lines);
            }
            var r = ShapeGeom.Norm(this);
            r.Inflate(Math.Max(8, W * 2), Math.Max(8, W * 2));
            return r;
        }

        public void DropCache() { Geo = null; Aux = null; }
    }

    public class Page
    {
        public string Bg = "grid";          // plain / grid / dots / lines / black
        public string Color = "#ffffff";
        public List<Obj> Objects = new List<Obj>();

        /// <summary>导入 PDF 后作为页面底图（不参与序列化，撤销后需重新导入）</summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public System.Windows.Media.Imaging.BitmapSource BackImage;
    }

    public class Doc
    {
        public double PageW = 1920;
        public double PageH = 1080;
        public int Current = 0;
        public List<Page> Pages = new List<Page>();

        [System.Text.Json.Serialization.JsonIgnore]
        public Page Cur => Pages.Count == 0 ? null : Pages[Math.Min(Current, Pages.Count - 1)];

        public static Doc Create()
        {
            var d = new Doc();
            d.Pages.Add(new Page());
            return d;
        }
    }

    public class History
    {
        public static readonly JsonSerializerOptions Json = new JsonSerializerOptions { IncludeFields = true };
        readonly List<string> _stack = new List<string>();
        int _idx = -1;
        const int MaxSteps = 80;

        public bool CanUndo => _idx > 0;
        public bool CanRedo => _idx >= 0 && _idx < _stack.Count - 1;

        public void Reset(Doc d) { _stack.Clear(); _idx = -1; Push(d); }

        public void Push(Doc d)
        {
            string s = JsonSerializer.Serialize(d, Json);
            if (_idx >= 0 && _stack[_idx] == s) return;
            while (_stack.Count > _idx + 1) _stack.RemoveAt(_stack.Count - 1);
            _stack.Add(s);
            _idx = _stack.Count - 1;
            if (_stack.Count > MaxSteps) { _stack.RemoveAt(0); _idx--; }
        }

        public Doc Undo()
        {
            if (!CanUndo) return null;
            _idx--;
            return JsonSerializer.Deserialize<Doc>(_stack[_idx], Json);
        }

        public Doc Redo()
        {
            if (!CanRedo) return null;
            _idx++;
            return JsonSerializer.Deserialize<Doc>(_stack[_idx], Json);
        }
    }

    public static class ShapeGeom
    {
        public static Rect Norm(Obj o)
        {
            return new Rect(Math.Min(o.X1, o.X2), Math.Min(o.Y1, o.Y2), Math.Abs(o.X2 - o.X1), Math.Abs(o.Y2 - o.Y1));
        }

        public static Geometry Build(Obj o)
        {
            switch (o.Shape)
            {
                case "line": return Poly(new[] { new Point(o.X1, o.Y1), new Point(o.X2, o.Y2) }, false);
                case "arrow": return Poly(new[] { new Point(o.X1, o.Y1), new Point(o.X2, o.Y2) }, false);
                case "rect": return Frozen(new RectangleGeometry(Norm(o)));
                case "ellipse":
                    {
                        var r = Norm(o);
                        return Frozen(new EllipseGeometry(new Point(r.X + r.Width / 2, r.Y + r.Height / 2),
                            Math.Max(0.5, r.Width / 2), Math.Max(0.5, r.Height / 2)));
                    }
                case "triangle":
                    {
                        var r = Norm(o);
                        return Poly(new[]
                        {
                            new Point(r.X + r.Width / 2, r.Y),
                            new Point(r.X + r.Width, r.Y + r.Height),
                            new Point(r.X, r.Y + r.Height)
                        }, true);
                    }
                case "func": return FuncCurve(o);
                default: return null;   // axis 单独绘制
            }
        }

        public static Geometry ArrowHead(Obj o, double size)
        {
            double ang = Math.Atan2(o.Y2 - o.Y1, o.X2 - o.X1);
            var g = new StreamGeometry();
            using (var c = g.Open())
            {
                c.BeginFigure(new Point(o.X2, o.Y2), true, true);
                c.LineTo(new Point(o.X2 - size * Math.Cos(ang - 0.42), o.Y2 - size * Math.Sin(ang - 0.42)), false, true);
                c.LineTo(new Point(o.X2 - size * Math.Cos(ang + 0.42), o.Y2 - size * Math.Sin(ang + 0.42)), false, true);
            }
            g.Freeze();
            return g;
        }

        /// <summary>把 o.Expr 采样成折线（bbox 内 x∈[-5,5]，unit = min(w,h)/10，与坐标系一致）</summary>
        public static Geometry FuncCurve(Obj o)
        {
            var r = Norm(o);
            if (r.Width < 2 || r.Height < 2) return null;
            double unit = Math.Min(r.Width, r.Height) / 10.0;
            double cx = r.X + r.Width / 2, cy = r.Y + r.Height / 2;
            Func<double, double> f;
            try { f = Expr.Compile(o.Expr); } catch (Exception) { f = x => double.NaN; }

            var pts = new List<Point>();
            int n = 240;
            for (int i = 0; i <= n; i++)
            {
                double x = -5 + 10.0 * i / n;
                double y = f(x);
                if (double.IsNaN(y) || double.IsInfinity(y) || Math.Abs(y) > 60) { if (pts.Count > 1) AddSeg(pts); pts = new List<Point>(); continue; }
                pts.Add(new Point(cx + x * unit, cy - y * unit));
            }
            var g = new StreamGeometry();
            using (var c = g.Open())
            {
                bool first = true;
                foreach (var p in pts)
                {
                    if (first) { c.BeginFigure(p, false, false); first = false; }
                    else c.LineTo(p, true, true);
                }
            }
            g.Freeze();
            return g;
        }

        static void AddSeg(List<Point> pts) { }

        static Geometry Poly(Point[] pts, bool closed)
        {
            var g = new StreamGeometry();
            using (var c = g.Open())
            {
                c.BeginFigure(pts[0], closed, closed);
                for (int i = 1; i < pts.Length; i++) c.LineTo(pts[i], true, true);
            }
            g.Freeze();
            return g;
        }

        static Geometry Frozen(Geometry g) { g.Freeze(); return g; }
    }

    /// <summary>自研表达式求值（不用脚本引擎），支持 sin/cos/tan/sqrt/abs/log/ln/exp/floor/ceil/round/sign、^、pi/e/x</summary>
    public static class Expr
    {
        public static Func<double, double> Compile(string src)
        {
            var p = new Reader(src ?? "");
            var node = p.ParseExpr();
            p.SkipSpace();
            if (!p.AtEnd) throw new FormatException("多余的字符: " + p.Rest);
            return node;
        }

        sealed class Reader
        {
            readonly string s;
            int i;
            public Reader(string src) { s = src; i = 0; }
            public bool AtEnd => i >= s.Length;
            public string Rest => s.Substring(Math.Min(i, s.Length));
            public void SkipSpace() { while (i < s.Length && char.IsWhiteSpace(s[i])) i++; }

            public Func<double, double> ParseExpr()
            {
                var left = ParseTerm();
                while (true)
                {
                    SkipSpace();
                    if (i >= s.Length || s[i] != '+' && s[i] != '-') return left;
                    char op = s[i++];
                    var right = ParseTerm();
                    var l = left;
                    left = op == '+' ? (Func<double, double>)(x => l(x) + right(x)) : (x => l(x) - right(x));
                }
            }

            Func<double, double> ParseTerm()
            {
                var left = ParseUnary();
                while (true)
                {
                    SkipSpace();
                    if (i >= s.Length) return left;
                    char op = s[i];
                    if (op != '*' && op != '/') return left;
                    i++;
                    var right = ParseUnary();
                    var l = left;
                    left = op == '*' ? (Func<double, double>)(x => l(x) * right(x)) : (x => l(x) / right(x));
                }
            }

            Func<double, double> ParseUnary()
            {
                SkipSpace();
                if (i < s.Length && s[i] == '-') { i++; var v = ParseUnary(); return x => -v(x); }
                if (i < s.Length && s[i] == '+') { i++; return ParseUnary(); }
                return ParsePower();
            }

            Func<double, double> ParsePower()
            {
                var b = ParseAtom();
                SkipSpace();
                if (i < s.Length && s[i] == '^') { i++; var e = ParseUnary(); return x => Math.Pow(b(x), e(x)); }
                return b;
            }

            Func<double, double> ParseAtom()
            {
                SkipSpace();
                if (i >= s.Length) throw new FormatException("表达式不完整");
                if (s[i] == '(') { i++; var v = ParseExpr(); SkipSpace(); Expect(')'); return v; }
                if (char.IsDigit(s[i]) || s[i] == '.')
                {
                    int start = i;
                    while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '.')) i++;
                    double d = double.Parse(s.Substring(start, i - start), System.Globalization.CultureInfo.InvariantCulture);
                    return x => d;
                }
                if (char.IsLetter(s[i]))
                {
                    int start = i;
                    while (i < s.Length && char.IsLetterOrDigit(s[i])) i++;
                    string name = s.Substring(start, i - start).ToLowerInvariant();
                    if (name == "x") return x => x;
                    if (name == "pi") return x => Math.PI;
                    if (name == "e") return x => Math.E;
                    SkipSpace();
                    if (i < s.Length && s[i] == '(')
                    {
                        i++;
                        var arg = ParseExpr();
                        SkipSpace();
                        Expect(')');
                        return Unary(name, arg);
                    }
                    throw new FormatException("未知标识符: " + name);
                }
                throw new FormatException("无法解析: " + s[i]);
            }

            void Expect(char ch)
            {
                if (i >= s.Length || s[i] != ch) throw new FormatException("缺少 " + ch);
                i++;
            }

            static Func<double, double> Unary(string name, Func<double, double> a)
            {
                switch (name)
                {
                    case "sin": return x => Math.Sin(a(x));
                    case "cos": return x => Math.Cos(a(x));
                    case "tan": return x => Math.Tan(a(x));
                    case "sqrt": return x => Math.Sqrt(a(x));
                    case "abs": return x => Math.Abs(a(x));
                    case "log": return x => Math.Log10(a(x));
                    case "ln": return x => Math.Log(a(x));
                    case "exp": return x => Math.Exp(a(x));
                    case "floor": return x => Math.Floor(a(x));
                    case "ceil": return x => Math.Ceiling(a(x));
                    case "round": return x => Math.Round(a(x));
                    case "sign": return x => Math.Sign(a(x));
                    default: throw new FormatException("未知函数: " + name);
                }
            }
        }
    }
}
