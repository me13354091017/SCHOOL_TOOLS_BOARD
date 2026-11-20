using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace InkCanvasPlus
{
    /// <summary>
    /// 函数表达式引擎 v2（参考 Desmos / padowan Graph / math.js 的解析规则）：
    /// x、pi/e/tau、+ - * / ^（右结合）、隐式乘法（2x、3sin(x)、(x+1)(x-1)）、|x| 绝对值、√、
    /// sin cos tan sec csc cot、asin acos atan（arcsin 等别名）、sinh cosh tanh、
    /// ln lg log2 log(b,x)、exp sqrt root(n,x)、abs floor ceil round sign、min/max/mod/pow、n!。
    /// 函数可省略括号：sin x、sqrt x+1 按 sin(x)、sqrt(x)+1 处理。
    /// </summary>
    internal static class StbFuncEngine
    {
        internal static string Normalize(string text)
        {
            if (text == null)
            {
                return string.Empty;
            }
            StringBuilder sb = new StringBuilder(text.Length + 8);
            foreach (char ch in text)
            {
                if (ch >= '\uFF01' && ch <= '\uFF5E')
                {
                    sb.Append((char)(ch - 0xFEE0));
                }
                else if (ch == '\u3000')
                {
                    sb.Append(' ');
                }
                else
                {
                    sb.Append(ch);
                }
            }
            string s = sb.ToString();
            s = s.Replace("\u00B2", "^2").Replace("\u00B3", "^3")
                 .Replace("\u00D7", "*").Replace("\u00F7", "/")
                 .Replace("\u2212", "-").Replace("\u2013", "-").Replace("\u2014", "-")
                 .Replace("\u03C0", "pi").Replace("\u00B7", "*")
                 .Replace("{", "(").Replace("}", ")")
                 .Replace("[", "(").Replace("]", ")")
                 .Replace("**", "^");
            return s;
        }

        internal static Func<double, double> Compile(string text)
        {
            if (text == null)
            {
                return null;
            }
            string s = Normalize(text).Trim();
            if (s.Length == 0)
            {
                return null;
            }
            int eq = s.IndexOf('=');
            if (eq >= 0)
            {
                s = s.Substring(eq + 1).Trim();
            }
            if (s.Length == 0)
            {
                return null;
            }
            try
            {
                StbFuncParser p = new StbFuncParser(s);
                Func<double, double> f = p.ParseExpr();
                p.EnsureEnd();
                double probe = f(1.2345);
                if (double.IsNaN(probe) && double.IsNaN(f(0.5)) && double.IsNaN(f(-2.7)))
                {
                    // 全 NaN 也允许（如 sqrt(-x^2-1)），交由画板显示空曲线
                }
                return f;
            }
            catch
            {
                return null;
            }
        }
    }

    internal sealed partial class StbFuncParser
    {
        private readonly string _s;
        private int _i;

        internal StbFuncParser(string s)
        {
            _s = s;
            _i = 0;
        }

        internal void EnsureEnd()
        {
            Skip();
            if (_i < _s.Length)
            {
                throw new FormatException();
            }
        }

        private void Skip()
        {
            while (_i < _s.Length && char.IsWhiteSpace(_s[_i]))
            {
                _i++;
            }
        }

        private bool Peek(char c)
        {
            Skip();
            return _i < _s.Length && _s[_i] == c;
        }

        private bool Eat(char c)
        {
            if (Peek(c))
            {
                _i++;
                return true;
            }
            return false;
        }

        private string PeekWord()
        {
            Skip();
            int j = _i;
            while (j < _s.Length && (char.IsLetter(_s[j]) || _s[j] == '_'))
            {
                j++;
            }
            return _s.Substring(_i, j - _i);
        }

        private bool ImplicitMulAhead()
        {
            Skip();
            if (_i >= _s.Length)
            {
                return false;
            }
            char c = _s[_i];
            return char.IsLetter(c) || char.IsDigit(c) || c == '(' || c == '|' || c == '\u221A';
        }

        internal Func<double, double> ParseExpr()
        {
            Func<double, double> left = ParseTerm();
            while (true)
            {
                if (Eat('+'))
                {
                    Func<double, double> r = ParseTerm();
                    Func<double, double> l = left;
                    left = x => l(x) + r(x);
                }
                else if (Eat('-'))
                {
                    Func<double, double> r = ParseTerm();
                    Func<double, double> l = left;
                    left = x => l(x) - r(x);
                }
                else
                {
                    return left;
                }
            }
        }

        private Func<double, double> ParseTerm()
        {
            Func<double, double> left = ParseUnary();
            while (true)
            {
                if (Eat('*'))
                {
                    Func<double, double> r = ParseUnary();
                    Func<double, double> l = left;
                    left = x => l(x) * r(x);
                }
                else if (Eat('/'))
                {
                    Func<double, double> r = ParseUnary();
                    Func<double, double> l = left;
                    left = x => l(x) / r(x);
                }
                else if (ImplicitMulAhead())
                {
                    Func<double, double> r = ParseUnary();
                    Func<double, double> l = left;
                    left = x => l(x) * r(x);
                }
                else
                {
                    return left;
                }
            }
        }

        private Func<double, double> ParseUnary()
        {
            if (Eat('-'))
            {
                Func<double, double> e = ParseUnary();
                return x => -e(x);
            }
            if (Eat('+'))
            {
                return ParseUnary();
            }
            return ParsePower();
        }

        private Func<double, double> ParsePower()
        {
            Func<double, double> b = ParsePostfix();
            if (Eat('^'))
            {
                Func<double, double> e = ParseUnary();
                return x => Math.Pow(b(x), e(x));
            }
            return b;
        }

        private Func<double, double> ParsePostfix()
        {
            Func<double, double> b = ParseAtom();
            while (Eat('!'))
            {
                Func<double, double> inner = b;
                b = x => Factorial(inner(x));
            }
            return b;
        }

        private static double Factorial(double v)
        {
            if (double.IsNaN(v) || v < 0d || v > 170d || Math.Abs(v - Math.Round(v)) > 1e-9)
            {
                return double.NaN;
            }
            double r = 1d;
            for (int k = 2; k <= (int)Math.Round(v); k++)
            {
                r *= k;
            }
            return r;
        }

        private Func<double, double> ParseAtom()
        {
            Skip();
            if (Eat('('))
            {
                Func<double, double> e = ParseExpr();
                if (!Eat(')'))
                {
                    throw new FormatException();
                }
                return e;
            }

            if (Eat('|'))
            {
                Func<double, double> e = ParseExpr();
                if (!Eat('|'))
                {
                    throw new FormatException();
                }
                return x => Math.Abs(e(x));
            }

            if (Eat('\u221A'))
            {
                Func<double, double> e = ParseUnary();
                return x => e(x) < 0d ? double.NaN : Math.Sqrt(e(x));
            }

            if (_i < _s.Length && (char.IsDigit(_s[_i]) || _s[_i] == '.'))
            {
                int j = _i;
                while (j < _s.Length && (char.IsDigit(_s[j]) || _s[j] == '.'))
                {
                    j++;
                }
                if (j < _s.Length && (_s[j] == 'e' || _s[j] == 'E')
                    && j + 1 < _s.Length && (char.IsDigit(_s[j + 1]) || _s[j + 1] == '-' || _s[j + 1] == '+'))
                {
                    j++;
                    if (_s[j] == '-' || _s[j] == '+')
                    {
                        j++;
                    }
                    while (j < _s.Length && char.IsDigit(_s[j]))
                    {
                        j++;
                    }
                }
                double v = double.Parse(_s.Substring(_i, j - _i), CultureInfo.InvariantCulture);
                _i = j;
                return x => v;
            }

            string w = PeekWord();
            if (w.Length == 0)
            {
                throw new FormatException();
            }
            _i += w.Length;
            string lw = w.ToLowerInvariant();

            if (lw == "x")
            {
                return x => x;
            }
            if (lw == "pi")
            {
                return x => Math.PI;
            }
            if (lw == "tau")
            {
                return x => Math.PI * 2d;
            }
            if (lw == "e")
            {
                return x => Math.E;
            }

            if (lw == "arcsin")
            {
                lw = "asin";
            }
            if (lw == "arccos")
            {
                lw = "acos";
            }
            if (lw == "arctan")
            {
                lw = "atan";
            }

            if (lw == "min" || lw == "max" || lw == "mod" || lw == "pow" || lw == "root" || lw == "log")
            {
                if (Peek('('))
                {
                    List<Func<double, double>> args = ParseArgList();
                    if (lw == "min" && args.Count >= 2)
                    {
                        return x =>
                        {
                            double m = args[0](x);
                            for (int k = 1; k < args.Count; k++)
                            {
                                m = Math.Min(m, args[k](x));
                            }
                            return m;
                        };
                    }
                    if (lw == "max" && args.Count >= 2)
                    {
                        return x =>
                        {
                            double m = args[0](x);
                            for (int k = 1; k < args.Count; k++)
                            {
                                m = Math.Max(m, args[k](x));
                            }
                            return m;
                        };
                    }
                    if (lw == "mod" && args.Count == 2)
                    {
                        return x => args[0](x) - Math.Floor(args[0](x) / args[1](x)) * args[1](x);
                    }
                    if (lw == "pow" && args.Count == 2)
                    {
                        return x => Math.Pow(args[0](x), args[1](x));
                    }
                    if (lw == "root" && args.Count == 2)
                    {
                        return x =>
                        {
                            double n = args[0](x);
                            double v = args[1](x);
                            if (n == 0d)
                            {
                                return double.NaN;
                            }
                            if (v < 0d && Math.Abs(n - Math.Round(n)) < 1e-9 && ((long)Math.Round(n)) % 2 == 1)
                            {
                                return -Math.Pow(-v, 1d / n);
                            }
                            return v < 0d ? double.NaN : Math.Pow(v, 1d / n);
                        };
                    }
                    if (lw == "log" && args.Count == 2)
                    {
                        return x =>
                        {
                            double b = args[0](x);
                            double v = args[1](x);
                            return b <= 0d || b == 1d || v <= 0d ? double.NaN : Math.Log(v, b);
                        };
                    }
                    if (lw == "log" && args.Count == 1)
                    {
                        Func<double, double> a = args[0];
                        return x => a(x) <= 0d ? double.NaN : Math.Log10(a(x));
                    }
                    throw new FormatException();
                }
                if (lw == "log")
                {
                    Func<double, double> a = ParseUnary();
                    return x => a(x) <= 0d ? double.NaN : Math.Log10(a(x));
                }
                throw new FormatException();
            }

            if (IsUnaryFunc(lw))
            {
                Func<double, double> arg = Peek('(') ? ParseCall() : ParseUnary();
                return WrapUnary(lw, arg);
            }

            throw new FormatException();
        }

    }
}
