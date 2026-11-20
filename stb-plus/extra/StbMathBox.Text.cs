using System;
using System.Text.RegularExpressions;

namespace InkCanvasPlus
{
    public partial class StbMathBox
    {
        // ================= 序列化 / 解析 =================

        public string Text
        {
            get { return Serialize(_root); }
            set { Parse(value); Changed(); }
        }

        private static bool EndsValue(char c)
        {
            return char.IsLetterOrDigit(c) || c == ')' || c == '|' || c == '!' || c == '\u03C0' || c == '\u221A';
        }

        private static bool StartsValue(char c)
        {
            return char.IsLetterOrDigit(c) || c == '(' || c == '|' || c == '\u03C0' || c == '\u221A';
        }

        /// <summary>相邻两个值直接拼会变成 xsqrt(9) 这种未知标识符，补乘号。</summary>
        private static void AppendPiece(System.Text.StringBuilder sb, string piece)
        {
            if (string.IsNullOrEmpty(piece))
            {
                return;
            }
            if (sb.Length > 0 && EndsValue(sb[sb.Length - 1]) && StartsValue(piece[0]))
            {
                sb.Append('*');
            }
            sb.Append(piece);
        }

        private static string Serialize(Slot s)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (Node n in s.Nodes)
            {
                if (n.Text != null) { AppendPiece(sb, n.Text); continue; }
                string piece;
                switch (n.Kind)
                {
                    case "func": piece = n.Name + "(" + Serialize(n.Slots[0]) + ")"; break;
                    case "logb": piece = "log(" + Serialize(n.Slots[0]) + "," + Serialize(n.Slots[1]) + ")"; break;
                    case "frac": piece = "(" + Serialize(n.Slots[0]) + ")/(" + Serialize(n.Slots[1]) + ")"; break;
                    case "sqrt": piece = "sqrt(" + Serialize(n.Slots[0]) + ")"; break;
                    case "nroot": piece = "root(" + Serialize(n.Slots[0]) + "," + Serialize(n.Slots[1]) + ")"; break;
                    case "pow": piece = "(" + Serialize(n.Slots[0]) + ")^(" + Serialize(n.Slots[1]) + ")"; break;
                    case "abs": piece = "|" + Serialize(n.Slots[0]) + "|"; break;
                    default: piece = string.Empty; break;
                }
                AppendPiece(sb, piece);
            }
            return sb.ToString();
        }

        private void Parse(string v)
        {
            _root.Nodes.Clear();
            _caret.Slot = _root;
            _caret.Idx = 0;
            _caret.Off = 0;
            if (string.IsNullOrEmpty(v))
            {
                return;
            }
            if (v.StartsWith("y="))
            {
                v = v.Substring(2);
            }
            Node n = ParseNode(v);
            _root.Nodes.Add(n);
            FixParents(_root);
            _caret.Idx = _root.Nodes.Count;
        }

        private static void FixParents(Slot s)
        {
            foreach (Node n in s.Nodes)
            {
                if (n.Text == null)
                {
                    foreach (Slot t in n.Slots)
                    {
                        t.Parent = s;
                        FixParents(t);
                    }
                }
            }
        }

        private Node ParseNode(string v)
        {
            Match m;
            if ((m = Regex.Match(v, @"^x\^(-?\d+)$")).Success)
            {
                return Tpl("pow", null, _root, "x", m.Groups[1].Value);
            }
            if ((m = Regex.Match(v, @"^(\d+)\^x$")).Success)
            {
                return Tpl("pow", null, _root, m.Groups[1].Value, "x");
            }
            if ((m = Regex.Match(v, @"^\((.*)\)\^\((.*)\)$")).Success)
            {
                Node n = Tpl("pow", null, _root);
                n.Slots[0].Nodes.Add(ParseNode(m.Groups[1].Value));
                n.Slots[1].Nodes.Add(ParseNode(m.Groups[2].Value));
                return n;
            }
            if ((m = Regex.Match(v, @"^log\(([^,]*),(.*)\)$")).Success)
            {
                Node n = Tpl("logb", null, _root);
                n.Slots[0].Nodes.Add(Run(m.Groups[1].Value));
                n.Slots[1].Nodes.Add(ParseNode(m.Groups[2].Value));
                return n;
            }
            if ((m = Regex.Match(v, @"^(sin|cos|tan|ln)\((.*)\)$")).Success)
            {
                Node n = Tpl("func", m.Groups[1].Value, _root);
                n.Slots[0].Nodes.Add(ParseNode(m.Groups[2].Value));
                return n;
            }
            if ((m = Regex.Match(v, @"^sqrt\((.*)\)$")).Success)
            {
                Node n = Tpl("sqrt", null, _root);
                n.Slots[0].Nodes.Add(ParseNode(m.Groups[1].Value));
                return n;
            }
            if ((m = Regex.Match(v, @"^root\(([^,]*),(.*)\)$")).Success)
            {
                Node n = Tpl("nroot", null, _root);
                n.Slots[0].Nodes.Add(Run(m.Groups[1].Value));
                n.Slots[1].Nodes.Add(ParseNode(m.Groups[2].Value));
                return n;
            }
            if ((m = Regex.Match(v, @"^\|(.*)\|$")).Success)
            {
                Node n = Tpl("abs", null, _root);
                n.Slots[0].Nodes.Add(ParseNode(m.Groups[1].Value));
                return n;
            }
            if ((m = Regex.Match(v, @"^\((.*)\)/\((.*)\)$")).Success)
            {
                Node n = Tpl("frac", null, _root);
                n.Slots[0].Nodes.Add(ParseNode(m.Groups[1].Value));
                n.Slots[1].Nodes.Add(ParseNode(m.Groups[2].Value));
                return n;
            }
            return Run(v);
        }
    }
}
