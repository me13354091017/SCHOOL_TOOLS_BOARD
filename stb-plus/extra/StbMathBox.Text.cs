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

        private static string Serialize(Slot s)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (Node n in s.Nodes)
            {
                if (n.Text != null) { sb.Append(n.Text); continue; }
                switch (n.Kind)
                {
                    case "func": sb.Append(n.Name).Append('(').Append(Serialize(n.Slots[0])).Append(')'); break;
                    case "logb": sb.Append("log(").Append(Serialize(n.Slots[0])).Append(',').Append(Serialize(n.Slots[1])).Append(')'); break;
                    case "frac": sb.Append('(').Append(Serialize(n.Slots[0])).Append(")/(").Append(Serialize(n.Slots[1])).Append(')'); break;
                    case "sqrt": sb.Append("sqrt(").Append(Serialize(n.Slots[0])).Append(')'); break;
                    case "nroot": sb.Append("root(").Append(Serialize(n.Slots[0])).Append(',').Append(Serialize(n.Slots[1])).Append(')'); break;
                    case "pow": sb.Append('(').Append(Serialize(n.Slots[0])).Append(")^(").Append(Serialize(n.Slots[1])).Append(')'); break;
                    case "abs": sb.Append('|').Append(Serialize(n.Slots[0])).Append('|'); break;
                }
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
