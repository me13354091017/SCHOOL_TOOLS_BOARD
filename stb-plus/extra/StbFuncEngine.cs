using System;
using System.Globalization;

namespace InkCanvasPlus
{
    /// <summary>
    /// 轻量函数表达式解析（供「插入函数」使用）：
    /// 支持 x、常数 pi/e、+ - * / ^、括号、sin cos tan ln log(常用对数) log2 sqrt abs。
    /// 传入 "y=log2(x)" 这类文本时自动去掉等号左边。
    /// </summary>
    internal static class StbFuncEngine
    {
        internal static Func<double, double> Compile(string text)
        {
            if (text == null)
            {
                return null;
            }

            string s = text.Trim();
            if (s.Length == 0)
            {
                return null;
            }

            int eq = s.IndexOf('=');
            if (eq >= 0)
            {
                s = s.Substring(eq + 1).Trim();
            }

            try
            {
                StbFuncParser p = new StbFuncParser(s);
                Func<double, double> f = p.ParseExpr();
                p.EnsureEnd();
                return f;
            }
            catch
            {
                return null;
            }
        }
    }

    internal sealed class StbFuncParser
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

        private void ConsumeWord(string w)
        {
            Skip();
            _i += w.Length;
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
            Func<double, double> b = ParseAtom();
            if (Eat('^'))
            {
                Func<double, double> e = ParseUnary();
                return x => Math.Pow(b(x), e(x));
            }
            return b;
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

            if (_i < _s.Length && (char.IsDigit(_s[_i]) || _s[_i] == '.'))
            {
                int j = _i;
                while (j < _s.Length && (char.IsDigit(_s[j]) || _s[j] == '.'))
                {
                    j++;
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
            ConsumeWord(w);
            string lw = w.ToLowerInvariant();

            if (lw == "x")
            {
                return x => x;
            }
            if (lw == "pi")
            {
                return x => Math.PI;
            }
            if (lw == "e")
            {
                return x => Math.E;
            }

            if (lw == "sin" || lw == "cos" || lw == "tan" || lw == "sqrt" || lw == "abs" || lw == "ln" || lw == "log2" || lw == "lg")
            {
                Func<double, double> arg = ParseCall();
                if (lw == "sin")
                {
                    return x => Math.Sin(arg(x));
                }
                if (lw == "cos")
                {
                    return x => Math.Cos(arg(x));
                }
                if (lw == "tan")
                {
                    return x => Math.Tan(arg(x));
                }
                if (lw == "sqrt")
                {
                    return x =>
                    {
                        double v = arg(x);
                        return v < 0d ? double.NaN : Math.Sqrt(v);
                    };
                }
                if (lw == "abs")
                {
                    return x => Math.Abs(arg(x));
                }
                if (lw == "ln")
                {
                    return x =>
                    {
                        double v = arg(x);
                        return v <= 0d ? double.NaN : Math.Log(v);
                    };
                }
                // log2 用换底；lg 用常用对数
                return x =>
                {
                    double v = arg(x);
                    if (v <= 0d)
                    {
                        return double.NaN;
                    }
                    return lw == "log2" ? Math.Log(v, 2d) : Math.Log10(v);
                };
            }

            throw new FormatException();
        }

        private Func<double, double> ParseCall()
        {
            if (!Eat('('))
            {
                throw new FormatException();
            }
            Func<double, double> e = ParseExpr();
            if (!Eat(')'))
            {
                throw new FormatException();
            }
            return e;
        }
    }
}
