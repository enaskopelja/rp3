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
        private readonly Dictionary<string, int> _defaultValues;

        public DictNode(Dictionary<string, node> data)
        {
            _data = data;
            _defaultValues = new Dictionary<string, int>();
            _defaultValues.Add("x", 0);
            _defaultValues.Add("y", 0);
            _defaultValues.Add("w", 80);
            _defaultValues.Add("h", 11);
            _defaultValues.Add("z", 0);
        } 

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
        
        public int Value(string s)
        {
            if (!_data.ContainsKey(s))
                return _defaultValues[s];
            
            if (_data[s].Impl is IntNode node)
                return (int) node;

            throw new ArgumentException("expected int");
        }
        
        public void MaybeAdjustWh(int maxW, int maxH)
        {
            if (Value("w") > maxW)
                _data["w"] = new node(maxW);

            if (Value("h") > maxH)
                _data["h"] = new node(maxH);
        }
        
        public bool HasChildren() => _data.ContainsKey("children");

        public node GetChildren() => _data["children"];
    }

    class Drawer
    {
        private Tuple<char, int>[,] _env;

        public string Draw(DictNode dictNode)
        {
            var z = dictNode.Value("z");

            dictNode.MaybeAdjustWh(80, 11);
            var w = dictNode.Value("w"); 
            var h = dictNode.Value("h"); 

            _env = new Tuple<char, int>[h, w];

            _DrawFrame(w, h, z, 0, 0, false);
            _DrawChildren(dictNode, 1, 1);

            return _ParseResult();
        }
        
        private void Draw(
            DictNode dictNode,
            int maxW,
            int maxH,
            int offsetX = 0, 
            int offsetY = 0
        )
        {
            var x = dictNode.Value("x");
            var y = dictNode.Value("y");
            var z = dictNode.Value("z");
            
            dictNode.MaybeAdjustWh(maxW - x - 2, maxH - y - 2);
            var w = dictNode.Value("w");
            var h = dictNode.Value("h");

            if (maxW < x || x < 0 || maxH < y || y < 0 || w < 2 || h < 2)
                return;

            _DrawFrame(w, h, z, x + offsetX, y + offsetY);
            if(dictNode.HasChildren())
                _DrawChildren(dictNode, x + offsetX + 1, y + offsetY + 1);
        }
        
        private void _DrawChildren(
            DictNode parent,
            int offsetX,
            int offsetY
        )
        {
            var node = parent.GetChildren().Impl;
            switch (node)
            {
                case IntNode intNode:
                    throw new ArgumentException("Tried drawing IntNode" + intNode);
                case DictNode _:
                    throw new ArgumentException("Tried drawing DictNode");
                case StrNode strNode:
                    _Write(
                        strNode.ToString(), 
                        offsetX, 
                        offsetY, 
                        parent.Value("w") - 2, 
                        parent.Value("h") - 2, 
                        parent.Value("z")
                        );
                    break;

                case ListNode listNode:
                {
                    foreach (var l in listNode)
                    {
                        if (l.Impl is DictNode dictNode)
                            Draw(dictNode, parent.Value("w"), parent.Value("h"), offsetX, offsetY);
                        else
                            throw new ArgumentException("Encountered ListNode with non DictNode element while drawing");
                    }

                    break;
                }
            }
        }

        
        private void _DrawFrame(
            int w,
            int h,
            int z,
            int startX,
            int startY,
            bool checkZ = true
        )
        {
            _DrawCorners(w, h, z, startX, startY, checkZ);
            _DrawVerticalEdgesAndInterior(w, h, z, startX, startY, checkZ);
            _DrawHorizontalEdges(w, h, z, startX, startY, checkZ);
        }

        private void _DrawHorizontalEdges(int w, int h, int z, int startX, int startY, bool checkZ)
        {
            for (var i = startY + 1; i < startY + h - 1; i++)
            {
                _DrawCell(startX, i, z, '|', checkZ);
                _DrawCell(startX + w - 1, i, z, '|', checkZ);
            }
        }

        private void _DrawVerticalEdgesAndInterior(int w, int h, int z, int startX, int startY, bool checkZ)
        {
            for (var i = startX + 1; i < startX + w - 1; i++)
            {
                _DrawCell(i, startY, z, '-', checkZ);
                _DrawCell(i, startY + h - 1, z, '-', checkZ);

                for (var j = startY + 1; j < startY + h - 1; j++)
                    _DrawCell(i, j, z, ' ', checkZ);
            }
        }

        private void _DrawCorners(int w, int h, int z, int startX, int startY, bool checkZ)
        {
            _DrawCell(startX, startY, z, '/', checkZ);
            _DrawCell(startX + w - 1, startY, z, '\\', checkZ);
            _DrawCell(startX + w - 1, startY + h - 1, z, '/', checkZ);
            _DrawCell(startX, startY + h - 1, z, '\\', checkZ);
        }

        private void _DrawCell(int x, int y, int z, char c, bool checkZ = true)
        {
            if (!checkZ || _env[y, x].Item2 <= z)
                _env[y, x] = new Tuple<char, int>(c, z);
        }
        
        private void _Write(
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
                _DrawCell(startX + cursor++, startY + row, z, ' ');

            foreach (var word in text.Split(' '))
            {
                _MaybeLineBreak(word.Length, w, ref row, ref cursor);

                if (row >= h)
                    return;

                foreach (var letter in word)
                {
                    _DrawCell(startX + cursor++, startY + row, z, letter);

                    if (cursor >= w)
                        _LineBreak(out cursor, ref row);

                    if (row >= h)
                        return;
                }

                if (cursor == 0) continue;
                _DrawCell(startX + cursor++, startY + row, z, ' ');
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

        private string _ParseResult()
        {
            var result = "";
            for (var i = 0; i < _env.GetLength(0); i++)
            {
                for (var j = 0; j < _env.GetLength(1); j++)
                    result += _env[i, j].Item1;
                result += '\n';
            }

            return result;
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

        public override string ToString()
        {
            if (Impl is DictNode impl)
            {
                Drawer drawer = new Drawer();
                return drawer.Draw(impl);
            }
                
            throw new ArgumentException("Tried calling ToString on non dict node");
        }
            
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



