using System;
using System.Collections.Generic;

namespace InkCanvasPlus
{
    internal sealed partial class StbFuncParser
    {
        private static bool IsUnaryFunc(string lw)
        {
            switch (lw)
            {
                case "sin":
                case "cos":
                case "tan":
                case "sec":
                case "csc":
                case "cot":
                case "asin":
                case "acos":
                case "atan":
                case "sinh":
                case "cosh":
                case "tanh":
                case "sqrt":
                case "abs":
                case "ln":
                case "lg":
                case "log2":
                case "exp":
                case "floor":
                case "ceil":
                case "round":
                case "sign":
                    return true;
                default:
                    return false;
            }
        }

        private static Func<double, double> WrapUnary(string lw, Func<double, double> a)
        {
            switch (lw)
            {
                case "sin": return x => Math.Sin(a(x));
                case "cos": return x => Math.Cos(a(x));
                case "tan": return x => Math.Tan(a(x));
                case "sec": return x => 1d / Math.Cos(a(x));
                case "csc": return x => 1d / Math.Sin(a(x));
                case "cot": return x => 1d / Math.Tan(a(x));
                case "asin": return x => Math.Abs(a(x)) > 1d ? double.NaN : Math.Asin(a(x));
                case "acos": return x => Math.Abs(a(x)) > 1d ? double.NaN : Math.Acos(a(x));
                case "atan": return x => Math.Atan(a(x));
                case "sinh": return x => Math.Sinh(a(x));
                case "cosh": return x => Math.Cosh(a(x));
                case "tanh": return x => Math.Tanh(a(x));
                case "sqrt": return x => a(x) < 0d ? double.NaN : Math.Sqrt(a(x));
                case "abs": return x => Math.Abs(a(x));
                case "ln": return x => a(x) <= 0d ? double.NaN : Math.Log(a(x));
                case "lg": return x => a(x) <= 0d ? double.NaN : Math.Log10(a(x));
                case "log2": return x => a(x) <= 0d ? double.NaN : Math.Log(a(x), 2d);
                case "exp": return x => Math.Exp(a(x));
                case "floor": return x => Math.Floor(a(x));
                case "ceil": return x => Math.Ceiling(a(x));
                case "round": return x => Math.Round(a(x), MidpointRounding.AwayFromZero);
                case "sign": return x => Math.Sign(a(x));
                default: throw new FormatException();
            }
        }

        private List<Func<double, double>> ParseArgList()
        {
            if (!Eat('('))
            {
                throw new FormatException();
            }
            List<Func<double, double>> args = new List<Func<double, double>>();
            args.Add(ParseExpr());
            while (Eat(','))
            {
                args.Add(ParseExpr());
            }
            if (!Eat(')'))
            {
                throw new FormatException();
            }
            return args;
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
