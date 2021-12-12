using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DZ1
{

    internal abstract class NodeType
    {
        public abstract string pretty_print(int indent = 0);
    }


    internal class IntNode : NodeType
    {
        private readonly int _value;

        public IntNode(int value) => _value = value;

        public override string pretty_print(int indent = 0) => _value.ToString();

        public static explicit operator int(IntNode n) => n._value;
    }

    internal class StrNode : NodeType
    {
        private readonly string _value;

        public StrNode(string value) => _value = value ?? throw new ArgumentNullException(nameof(value));

        public override string pretty_print(int indent = 0) => $"\"{_value}\"";

        public override string ToString() => _value;
    }


    internal class ListNode : NodeType, IEnumerable<node>
    {
        private readonly List<node> _data;

        public ListNode(List<node> data) => _data = data;

        public override string pretty_print(int indent = 0)
        {
            if (_data.Count == 0)
                return "[]";
            return _data.Aggregate(
                "[",
                (current, v) =>
                    current +
                    (
                        "\n" +
                        string.Concat(Enumerable.Repeat(" ", indent + 4)) +
                        v.pretty_print(indent + 4) +
                        ","
                    )
            ) + "\n" + string.Concat(Enumerable.Repeat(" ", indent)) + "]";
        }

        public IEnumerator<node> GetEnumerator()
        {
            return ((IEnumerable<node>) _data).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    internal class DictNode : NodeType
    {
        private readonly Dictionary<string, node> _data;

        public DictNode(Dictionary<string, node> data) => _data = data;

        public override string pretty_print(int indent = 0)
        {
            if (_data.Count == 0)
                return "{}";
            return _data.Aggregate(
                "{",
                (current, kv) =>
                    current +
                    (
                        "\n" +
                        string.Concat(Enumerable.Repeat(" ", indent + 4)) +
                        kv.Key + ": " +
                        kv.Value.pretty_print(indent + 4) +
                        ","
                    )
            ) + "\n" + string.Concat(Enumerable.Repeat(" ", indent)) + "}";
        }

        public string DrawRoot()
        {
            var w = Math.Min(_GetValue("w", 80), 80);
            var h = Math.Min(_GetValue("h", 11), 11);
            var z = _GetValue("z", 0);

            var env = new Tuple<char, int>[h, w];

            _DrawFrame(w, h, z, 0, 0, ref env, false);
            _DrawChildren(ref env, w, h, z, 1, 1);

            return _ParseResult(env);
        }

        private static string _ParseResult(Tuple<char, int>[,] env)
        {
            var result = "";
            for (var i = 0; i < env.GetLength(0); i++)
            {
                for (var j = 0; j < env.GetLength(1); j++)
                    result += env[i, j].Item1;
                result += '\n';
            }

            return result;
        }

        private void _DrawChildren(
            ref Tuple<char, int>[,] env,
            int parentW,
            int parentH,
            int parentZ,
            int offsetX,
            int offsetY
        )
        {
            if (!_data.ContainsKey("children"))
                return;

            var node = _data["children"].Impl;
            switch (node)
            {
                case IntNode intNode:
                    throw new ArgumentException("Tried drawing IntNode" + intNode);
                case DictNode dictNode:
                    throw new ArgumentException("Tried drawing DictNode");
                case StrNode strNode:
                    _Write(ref env, strNode.ToString(), offsetX, offsetY, parentW - 2, parentH - 2, parentZ);
                    break;

                case ListNode listNode:
                {
                    foreach (var l in listNode)
                    {
                        if (l.Impl is DictNode dictNode)
                            dictNode.Draw(ref env, parentW, parentH, offsetX, offsetY);
                        else
                            throw new ArgumentException("Encountered ListNode with non DictNode element while drawing");
                    }

                    break;
                }
            }
        }

        private void _Write(
            ref Tuple<char, int>[,] env,
            string text,
            int startX,
            int startY,
            int w,
            int h,
            int z
        )
        {
            string trimmed = text.TrimStart();
            int whitespacesCount = Math.Min(text.Length - trimmed.Length, w);
            text = Regex.Replace(trimmed, @"\s+", " ");

            int cursor = 0, row = 0;

            for (var i = 0; i < whitespacesCount; i++)
                _DrawCell(startX + cursor++, startY + row, z, ref env, ' ');

            foreach (var word in text.Split(' '))
            {
                _MaybeLineBreak(word.Length, w, ref row, ref cursor);

                if (row >= h)
                    return;

                foreach (var letter in word)
                {
                    _DrawCell(startX + cursor++, startY + row, z, ref env, letter);

                    if (cursor >= w)
                        _LineBreak(out cursor, ref row);

                    if (row >= h)
                        return;
                }

                if (cursor == 0) continue;
                _DrawCell(startX + cursor++, startY + row, z, ref env, ' ');
            }
        }


        private void _LineBreak(out int cursor, ref int row)
        {
            cursor = 0;
            row++;
        }

        private void _MaybeLineBreak(int wordLength, int w, ref int row, ref int cursor)
        {
            if (wordLength <= w - cursor || cursor == 0) return;
            _LineBreak(out cursor, ref row);
        }

        private void Draw(
            ref Tuple<char, int>[,] env,
            int parentW,
            int parentH,
            int offsetX = 0,
            int offsetY = 0
        )
        {
            var x = _GetValue("x", 0);
            var y = _GetValue("y", 0);
            var z = _GetValue("z", 0);
            var w = Math.Min(_GetValue("w", 80), parentW - x - 2);
            var h = Math.Min(_GetValue("h", 11), parentH - y - 2);

            if (parentW < x || x < 0 || parentH < y || y < 0 || w < 2 || h < 2)
                return;

            _DrawFrame(w, h, z, x + offsetX, y + offsetY, ref env);
            _DrawChildren(ref env, w, h, z, offsetX + x + 1, offsetY + y + 1);
        }

        private void _DrawFrame(
            int w,
            int h,
            int z,
            int startX,
            int startY,
            ref Tuple<char, int>[,] env,
            bool checkZ = true
        )
        {
            _DrawCorners(w, h, z, startX, startY, ref env, checkZ);
            _DrawVerticalEdgesAndInterior(w, h, z, startX, startY, ref env, checkZ);
            _DrawHorizontalEdges(w, h, z, startX, startY, ref env, checkZ);
        }

        private void _DrawHorizontalEdges(int w, int h, int z, int startX, int startY, ref Tuple<char, int>[,] env,
            bool checkZ)
        {
            for (var i = startY + 1; i < startY + h - 1; i++)
            {
                _DrawCell(startX, i, z, ref env, '|', checkZ);
                _DrawCell(startX + w - 1, i, z, ref env, '|', checkZ);
            }
        }

        private void _DrawVerticalEdgesAndInterior(int w, int h, int z, int startX, int startY,
            ref Tuple<char, int>[,] env,
            bool checkZ)
        {
            for (var i = startX + 1; i < startX + w - 1; i++)
            {
                _DrawCell(i, startY, z, ref env, '-', checkZ);
                _DrawCell(i, startY + h - 1, z, ref env, '-', checkZ);

                for (var j = startY + 1; j < startY + h - 1; j++)
                    _DrawCell(i, j, z, ref env, ' ', checkZ);
            }
        }

        private void _DrawCorners(int w, int h, int z, int startX, int startY, ref Tuple<char, int>[,] env, bool checkZ)
        {
            _DrawCell(startX, startY, z, ref env, '/', checkZ);
            _DrawCell(startX + w - 1, startY, z, ref env, '\\', checkZ);
            _DrawCell(startX + w - 1, startY + h - 1, z, ref env, '/', checkZ);
            _DrawCell(startX, startY + h - 1, z, ref env, '\\', checkZ);
        }

        private int _GetValue(string s, int defaultValue
        )
        {
            if (!_data.ContainsKey(s))
                return defaultValue;

            if (_data[s].Impl is IntNode node)
                return (int) node;

            throw new ArgumentException("expected int");
        }

        private void _DrawCell(int x, int y, int z, ref Tuple<char, int>[,] env, char c, bool checkZ = true)
        {
            if (!checkZ || env[y, x].Item2 <= z)
                env[y, x] = new Tuple<char, int>(c, z);
        }
    }


    class node
    {
        public NodeType Impl { get; }

        public node(int value) : this(new IntNode(value)) { }

        public node(string value) : this(new StrNode(value)) { }

        public node(List<node> value) : this(new ListNode(value)) { }

        public node(Dictionary<string, node> value) : this(new DictNode(value)) { }

        private node(NodeType impl) => Impl = impl;


        public string pretty_print(int indent = 0) => Impl.pretty_print(indent);

        public override string ToString() =>
            Impl is DictNode impl
                ? impl.DrawRoot()
                : throw new ArgumentException("Tried calling ToString on non dict node");
    }


    class Program
    {
        static void Main(string[] args)
        {
            node part1 = new node(
                new Dictionary<string, node> {
                    {
                        "children", 
                        new node(
                            new List<node> {
                                new node(
                                    new Dictionary<string, node> {
                                        {"x", new node(2)},
                                        {"y", new node(4)},
                                        {"w", new node(20)},
                                        {"h", new node(4)},
                                        {"z", new node(2)},
                                        {"children", new node("Neki dugacak tekst   neki dugacak tekst")},
                                    }
                                ),
                                new node(
                                    new Dictionary<string, node> {
                                        {"x", new node(18)},
                                        {"y", new node(1)},
                                        {"w", new node(20)},
                                        {"h", new node(5)},
                                        {"z", new node(1)},
                                        {"children", new node("Neki jos dulji tekst neki jos dulji tekst neki jos dulji tekst")},
                                    }
                                ),
                                new node(
                                    new Dictionary<string, node> {
                                        {"x", new node(42)},
                                        {"y", new node(1)},
                                        {"w", new node(32)},
                                        {"h", new node(7)},
                                        {
                                            "children", 
                                            new node(
                                                new List<node> {
                                                    new node(
                                                            new Dictionary<string, node> {
                                                            {"w", new node(6)},
                                                            {"h", new node(3)},
                                                            {"children", new node("")},
                                                        }
                                                    ),
                                                    new node(
                                                        new Dictionary<string, node> {
                                                            {"x", new node(24)},
                                                            {"y", new node(2)},
                                                            {"children", new node(new List<node>() {})},
                                                        }
                                                    ),
                                                }
                                            )
                                        },
                                    }
                                ),
                            }
                        )
                    }
                }
            );
                
            // Prvi dio zadatka
            Console.WriteLine(part1.pretty_print());

            // Drugi dio zadatka
            Console.WriteLine(part1);

            node part2 = new node(
                new Dictionary<string, node> {
                    {"x", new node(20)},
                    {"y", new node(3)},
                    {"w", new node(30)},
                    {"h", new node(123456789)},
                    {"z", new node(-1234)},
                    {
                        "children", 
                        new node(
                            new List<node> {
                                new node(
                                    new Dictionary<string, node> {
                                        {"w", new node(2)},
                                        {"h", new node(2)},
                                        {"z", new node(-12345)},
                                        {"children", new node("Prozor prekriven roditeljem")},
                                    }
                                ),
                                new node(
                                    new Dictionary<string, node> {
                                        {"x", new node(3)},
                                        {"y", new node(2)},
                                        {"w", new node(9)},
                                        {"h", new node(4)},
                                        {"children", new node("Vidljiv prozor")},
                                    }
                                ),
                                new node(
                                    new Dictionary<string, node> {
                                        {"x", new node(15)},
                                        {
                                            "children",                                    
                                            new node(
                                                new List<node> {
                                                    new node(
                                                        new Dictionary<string, node> {
                                                            {"z", new node(2)},
                                                            {"children", new node("Jos jedan nevidljiv")}
                                                        }
                                                    ),
                                                    new node(
                                                        new Dictionary<string, node> {
                                                            {"z", new node(3)},
                                                            {"children", new node("Jos jedan vidljiv")}
                                                        }
                                                    )
                                                }
                                            )
                                        }
                                    }
                                ),
                            }
                        )
                    }
                }
            );
                
            // Prvi dio zadatka
            Console.WriteLine(part2.pretty_print());

            // Drugi dio zadatka
            Console.WriteLine(part2);

            
            node part3 = new node(
                new Dictionary<string, node> {
                    {
                        "children", 
                        new node(
                            new List<node> {
                                new node(
                                    new Dictionary<string, node> {
                                        {"z", new node(-1)},
                                        {
                                            "children", 
                                            new node(
                                                new List<node> {
                                                    new node(
                                                        new Dictionary<string, node> {
                                                            {"x", new node(2)},
                                                            {"y", new node(2)},
                                                            {"w", new node(25)},
                                                            {"h", new node(4)},
                                                            {"children", new node("Vidljivo dijete nevidljivog roditelja")},
                                                        }
                                                    ),
                                                }
                                            )
                                        }
                                    }
                                ),
                                new node(
                                    new Dictionary<string, node> {
                                        {"x", new node(27)},
                                        {"h", new node(5)},
                                        {"z", new node(1)},
                                        {"children", new node("Lorem ipsum dolor sit amet consectetur adipiscing elit Nam hendrerit nisi sed sollicitudin pellentesque Nunc posuere purus rhoncus pulvinar aliquam")},
                                    }
                                ),
                            }
                        )
                    }
                }
            );
    
            // Prvi dio zadatka
            Console.WriteLine(part3.pretty_print());
        
            // Drugi dio zadatka
            Console.WriteLine(part3);

        }
    }
}



