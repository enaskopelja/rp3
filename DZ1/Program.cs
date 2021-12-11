using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

abstract class NodeType
{
    public abstract string pretty_print(int indent = 0);
}


class IntNode : NodeType
{
    private int _value;

    public IntNode(int value)
    {
        _value = value;
    }

    public override string pretty_print(int indent = 0) => _value.ToString();
    public static implicit operator int(IntNode n) => n._value;
}

class StrNode : NodeType
{
    private string _value;

    public StrNode(string value) => _value = value;

    public override string pretty_print(int indent = 0) => "\"" + _value + "\"";

    public override string ToString() => _value;
}


class ListNode : NodeType, IEnumerable<node>
{
    private List<node> _data;

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
        foreach (var l in _data)
        {
            yield return l;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

class DictNode : NodeType
{
    private Dictionary<string, node> _data;

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
        int w = _NodeToInt("w", 80);
        int h = _NodeToInt("h", 11);
        int z = _NodeToInt("z", 0);
        
        Tuple<char, int>[,] env = new Tuple<char, int>[11, 80];

        for (int row = 0; row < 11; row++)
        {
            for (int col = 0; col < 80; col++)
            {
                env[row, col] = new Tuple<char, int>('x', z);
            }
        }

        _DrawFrame(w, h, z, 0, 0, ref env, false);
        _DrawFrame(w, h, z, 0, 0, ref env, false);
    
        for (int i = 0; i < 11; i++)
        {
            for (int j = 0; j < 80; j++)
            {
                Console.Write(env[i, j].Item1);
            }
    
            Console.Write('\n');
        }
        
        _DrawChildren(ref env, w, h, 0,0, z, 1,1);

        for (int i = 0; i < 11; i++)
        {
            for (int j = 0; j < 80; j++)
            {
                Console.Write(env[i, j].Item1);
            }
    
            Console.Write('\n');
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
        if(!_data.ContainsKey("children"))
            return;

        if (_data["children"].Impl is IntNode)
            throw new ArgumentException("Tried drawing IntNode");
        if (_data["children"].Impl is DictNode)
            throw new ArgumentException("Tried drawing DictNode");
        if (_data["children"].Impl is StrNode)
        {
            Console.WriteLine("Should draw " + _data["children"].Impl);
        }
        else if (_data["children"].Impl is ListNode)
        {
            ListNode listNode = _data["children"].Impl as ListNode;
             
            foreach (var l in listNode)
            {
                if (l.Impl is DictNode)
                {
                    DictNode d = l.Impl as DictNode;
                    d.Draw(ref env, parentW, parentH, offsetX, offsetY);
                }
                else
                    throw new ArgumentException("Encountered ListNode with non DictNode element while drawing");
            }
        }
        else
            throw new ArgumentException("Unknown node type");

    }

    private void Draw(
        ref Tuple<char, int>[,] env,
        int parentW,
        int parentH,
        int offsetX = 0,
        int offsetY = 0
    )
    {
        int w = _NodeToInt("w", 80);
        int h = _NodeToInt("h", 11);
        int x = _NodeToInt("x", 0);
        int y = _NodeToInt("y", 0);
        int z = _NodeToInt("z", 0);

        Console.WriteLine("Drawing:");
        Console.Write("x: " + x.ToString() + " ");
        Console.Write("y: " + y.ToString() + " ");
        Console.Write("z: " + y.ToString() + " ");
        Console.Write("w: " + w.ToString() + " "); 
        Console.Write("h: " + h.ToString() + " ");
        Console.Write("offsetX: " + offsetX.ToString() + " ");
        Console.Write("offsetY: " + offsetY + " ");
        Console.WriteLine();
        
        if (parentW < x || x < 0 || parentH < y || y < 0)
        {
            Console.WriteLine("pao na 1 " + " parentW " + parentW + " parentH " + parentH);
            return;
        }

        if (x + w > parentW - 2)
            w = parentW - x - 2;

        if (y + h > parentH - 2)
            h = parentH - y - 2;

        if (w < 2 || h < 2)
        {
            Console.WriteLine("pao na 2");
            return;
        }

        _DrawFrame(
            w,
            h,
            z,
            x + offsetX,
            y + offsetY,
            ref env
        );
        
        Console.WriteLine("Done: ");
        for (int i = 0; i < 11; i++)
        {
            for (int j = 0; j < 80; j++)
            {
                Console.Write(env[i, j].Item1);
            }
    
            Console.Write('\n');
        }
        
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
        _DrawCell(startX + w -1, startY + h - 1, z, ref env, '/', checkZ);
        _DrawCell(startX, startY + h - 1, z, ref env, '\\', checkZ);

        for (int i = startX + 1; i < startX + w - 1; i++)
        {
            _DrawCell(i, startY, z, ref env, '-', checkZ);
            _DrawCell(i, startY + h - 1, z, ref env, '-', checkZ);
        }

        for (int i = startY + 1; i < startY + h - 1; i++)
        {
            _DrawCell(startX, i, z, ref env, '|', checkZ);
            _DrawCell(startX + w - 1, i, z, ref env, '|', checkZ);
        }
    }

    private int _NodeToInt(string s, int default_
    )
    {
        if (!_data.ContainsKey(s))
        {
            return default_;
        }
        
        NodeType n = _data[s].Impl;
        if (n is IntNode)
        {
            return (int) (n as IntNode);
        }

        throw new ArgumentException("expected int");
    }

    private void _DrawCell(int x, int y, int z, ref Tuple<char, int>[,] env, char c, bool checkZ)
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

    public NodeType Impl
    {
        get => _impl;
    }

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
        if (_impl is DictNode)
        {
            DictNode impl = _impl as DictNode;
            return impl.DrawRoot();
        }
        else
        {
            throw new ArgumentException("Tried calling ToString on non dict node");
        }
    }
}


class Program
{
    static void Main(string[] args)
    {
        node x = new node(
            new Dictionary<string, node>
            {
                {
                    "children",
                    new node(
                        new List<node>
                        {
                            new node(
                                new Dictionary<string, node>
                                {
                                    {"x", new node(2)},
                                    {"y", new node(4)},
                                    {"w", new node(20)},
                                    {"h", new node(4)},
                                    {"z", new node(2)},
                                    {"children", new node("Neki dugacak tekst   neki dugacak tekst")},
                                }
                            ),
                            new node(
                                new Dictionary<string, node>
                                {
                                    {"x", new node(18)},
                                    {"y", new node(1)},
                                    {"w", new node(20)},
                                    {"h", new node(5)},
                                    {"z", new node(1)},
                                    {
                                        "children",
                                        new node("Neki jos dulji tekst neki jos dulji tekst neki jos dulji tekst")
                                    },
                                }
                            ),
                            new node(
                                new Dictionary<string, node>
                                {
                                    {"x", new node(42)},
                                    {"y", new node(1)},
                                    {"w", new node(32)},
                                    {"h", new node(7)},
                                    {
                                        "children",
                                        new node(
                                            new List<node>
                                            {
                                                new node(
                                                    new Dictionary<string, node>
                                                    {
                                                        {"w", new node(6)},
                                                        {"h", new node(3)},
                                                        {"children", new node("")},
                                                    }
                                                ),
                                                new node(
                                                    new Dictionary<string, node>
                                                    {
                                                        {"x", new node(24)},
                                                        {"y", new node(2)},
                                                        {"children", new node(new List<node>() { })},
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
        Console.WriteLine(x.pretty_print());
        // Drugi dio zadatka
        Console.WriteLine(x);
    }
}