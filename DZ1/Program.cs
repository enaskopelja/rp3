using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

abstract class NodeType
{
    public abstract string pretty_print(int indent = 0);
}


class IntNode : NodeType
{
    private readonly int _value;

    public IntNode(int value)
    {
        _value = value;
    }

    public override string pretty_print(int indent = 0) => _value.ToString();
    public static implicit operator int(IntNode n) => n._value;
}

class StrNode : NodeType
{
    private readonly string _value;

    public string asString => _value;

    public StrNode(string value) => _value = value ?? throw new ArgumentNullException(nameof(value));

    public override string pretty_print(int indent = 0) => "\"" + _value + "\"";

    public override string ToString() => _value;
}


class ListNode : NodeType, IEnumerable<node>
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

class DictNode : NodeType
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
                    ", "
                )
        ) + "\n" + string.Concat(Enumerable.Repeat(" ", indent)) + "}";
    }

    public string DrawRoot()
    {
        int w = Math.Min(_NodeToInt("w", 80), 80);
        int h = Math.Min(_NodeToInt("h", 11), 11);
        int z = _NodeToInt("z", 0);

        Tuple<char, int>[,] env = new Tuple<char, int>[h, w];

        for (var row = 0; row < h; row++)
        {
            for (var col = 0; col < w; col++)
            {
                env[row, col] = new Tuple<char, int>('x', z);
            }
        }

        _DrawFrame(w, h, z, 0, 0, ref env, false);
        _DrawChildren(ref env, w, h, 0, 0, z, 1, 1);

        for (var i = 0; i < h; i++)
        {
            for (var j = 0; j < w; j++)
                Console.Write(env[i, j].Item1);
            Console.WriteLine();
        }

        return "bok";
    }

    private void _DrawChildren(
        ref Tuple<char, int>[,] env,
        int parentW,
        int parentH,
        int parentX,
        int parentY,
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
            case IntNode:
                throw new ArgumentException("Tried drawing IntNode");
            case DictNode:
                throw new ArgumentException("Tried drawing DictNode");
            case StrNode strNode:
                _Write(
                    ref env,
                    strNode.asString,
                    offsetX,
                    offsetY,
                    parentW - 2,
                    parentH - 2,
                    parentZ
                );

                break;

            case ListNode listNode:
            {
                foreach (var l in listNode)
                {
                    if (l.Impl is DictNode dictNode)
                    {
                        dictNode.Draw(ref env, parentW, parentH, offsetX, offsetY);
                    }
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
        {
            _DrawCell(startX + cursor, startY + row, z, ref env, ' ');
            for (var k = 0; k < 11; k++)
            {
                for (var j = 0; j < 30; j++)
                    Console.Write(env[k, j].Item1);
                Console.WriteLine();
            }

            cursor++;
            if (cursor >= w)
            {
                cursor = 0;
                row++;
            }
        }

        foreach (var word in text.Split(' '))
        {
            if (word.Length > w - cursor && cursor > 0)
            {
                row += 1;
                cursor = 0;
            }

            if (row >= h)
                return;

            foreach (var letter in word)
            {
                _DrawCell(startX + cursor, startY + row, z, ref env, letter);
                for (var k = 0; k < 11; k++)
                {
                    for (var j = 0; j < 30; j++)
                        Console.Write(env[k, j].Item1);
                    Console.WriteLine();
                }

                cursor++;
                if (cursor >= w)
                {
                    cursor = 0;
                    row++;
                }

                if (row > h)
                    return;
            }

            if (cursor != 0)
            {
                _DrawCell(startX + cursor, startY + row, z, ref env, ' ');
                cursor++;
            }
        }
    }

    private void Draw(
        ref Tuple<char, int>[,] env,
        int parentW,
        int parentH,
        int offsetX = 0,
        int offsetY = 0
    )
    {
        int x = _NodeToInt("x", 0);
        int y = _NodeToInt("y", 0);
        int z = _NodeToInt("z", 0);
        int w = Math.Min(_NodeToInt("w", 80), parentW - x - 2);
        int h = Math.Min(_NodeToInt("h", 11), parentH - y - 2);

        if (parentW < x || x < 0 || parentH < y || y < 0)
            return;

        if (w < 2 || h < 2)
            return;

        _DrawFrame(
            w,
            h,
            z,
            x + offsetX,
            y + offsetY,
            ref env
        );

        _DrawChildren(ref env, w, h, offsetX + x, offsetY + y, z, offsetX + x + 1, offsetY + y + 1);
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
        _DrawCell(startX, startY, z, ref env, '/', checkZ);
        _DrawCell(startX + w - 1, startY, z, ref env, '\\', checkZ);
        _DrawCell(startX + w - 1, startY + h - 1, z, ref env, '/', checkZ);
        _DrawCell(startX, startY + h - 1, z, ref env, '\\', checkZ);

        for (int i = startX + 1; i < startX + w - 1; i++)
        {
            _DrawCell(i, startY, z, ref env, '-', checkZ);
            _DrawCell(i, startY + h - 1, z, ref env, '-', checkZ);

            for (int j = startY + 1; j < startY + h - 1; j++)
                _DrawCell(i, j, z, ref env, ' ', checkZ);
        }

        for (int i = startY + 1; i < startY + h - 1; i++)
        {
            _DrawCell(startX, i, z, ref env, '|', checkZ);
            _DrawCell(startX + w - 1, i, z, ref env, '|', checkZ);
        }
    }

    private int _NodeToInt(string s, int defaultValue
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
        {
            env[y, x] = new Tuple<char, int>(c, z);
        }
    }
}


class node
{
    private readonly NodeType _impl;

    public NodeType Impl => _impl;

    public node(int value)
    {
        _impl = new IntNode(value);
    }

    public node(string value)
    {
        _impl = new StrNode(value);
    }

    public node(List<node> value)
    {
        _impl = new ListNode(value);
    }

    public node(Dictionary<string, node> value)
    {
        _impl = new DictNode(value);
    }


    public string pretty_print(int indent = 0)
    {
        return _impl.pretty_print(indent);
    }

    public override string ToString()
    {
        if (_impl is DictNode impl)
            return impl.DrawRoot();
        throw new ArgumentException("Tried calling ToString on non dict node");
    }
}


class Program
{
    static void Main(string[] args)
    {
        node x = new node(
            new Dictionary<string, node>
            {
                {"x", new node(20)},
                {"y", new node(3)},
                {"w", new node(30)},
                {"h", new node(123456789)},
                {"z", new node(-1234)},
                {
                    "children",
                    new node(
                        new List<node>
                        {
                            new node(
                                new Dictionary<string, node>
                                {
                                    {"w", new node(2)},
                                    {"h", new node(2)},
                                    {"z", new node(-12345)},
                                    {"children", new node("Prozor prekriven roditeljem")},
                                }
                            ),
                            new node(
                                new Dictionary<string, node>
                                {
                                    {"x", new node(3)},
                                    {"y", new node(2)},
                                    {"w", new node(9)},
                                    {"h", new node(4)},
                                    {"children", new node("Vidljiv prozor")},
                                }
                            ),
                            new node(
                                new Dictionary<string, node>
                                {
                                    {"x", new node(15)},
                                    {
                                        "children",
                                        new node(
                                            new List<node>
                                            {
                                                new node(
                                                    new Dictionary<string, node>
                                                    {
                                                        {"z", new node(2)},
                                                        {"children", new node("Jos jedan nevidljiv")}
                                                    }
                                                ),
                                                new node(
                                                    new Dictionary<string, node>
                                                    {
                                                        {"z", new node(3)},
                                                        {"children", new node("Jos                   jedan vidljiv")}
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
        Console.WriteLine(x.pretty_print());

// Drugi dio zadatka
        Console.WriteLine(x);
    }
}