using fNbt;
using System.Text;

namespace NetherGate.Core.Data;

/// <summary>
/// SNBT (Stringified NBT) 解析器
/// 用于解析 Minecraft 的字符串化 NBT 数据
/// </summary>
public class SnbtParser
{
    /// <summary>
    /// 解析 SNBT 字符串为 NBT 标签
    /// </summary>
    public static NbtTag? Parse(string snbt)
    {
        if (string.IsNullOrWhiteSpace(snbt))
            return null;

        snbt = snbt.Trim();
        
        // 尝试解析
        try
        {
            var context = new ParseContext(snbt);
            return ParseValue(context);
        }
        catch
        {
            return null;
        }
    }

    private static NbtTag? ParseValue(ParseContext context)
    {
        context.SkipWhitespace();
        
        if (context.Peek() == '{')
        {
            return ParseCompound(context);
        }
        else if (context.Peek() == '[')
        {
            return ParseList(context);
        }
        else if (context.Peek() == '"' || context.Peek() == '\'')
        {
            return new NbtString(ParseQuotedString(context));
        }
        else
        {
            return ParsePrimitive(context);
        }
    }

    private static NbtCompound ParseCompound(ParseContext context)
    {
        var compound = new NbtCompound();
        context.Expect('{');
        context.SkipWhitespace();

        while (context.Peek() != '}' && !context.IsAtEnd)
        {
            context.SkipWhitespace();
            
            // 读取键
            string key;
            if (context.Peek() == '"' || context.Peek() == '\'')
            {
                key = ParseQuotedString(context);
            }
            else
            {
                key = ParseUnquotedString(context);
            }

            context.SkipWhitespace();
            context.Expect(':');
            context.SkipWhitespace();

            // 读取值
            var value = ParseValue(context);
            if (value != null)
            {
                value.Name = key;
                compound.Add(value);
            }

            context.SkipWhitespace();
            if (context.Peek() == ',')
            {
                context.Read();
                context.SkipWhitespace();
            }
        }

        context.Expect('}');
        return compound;
    }

    private static NbtTag ParseList(ParseContext context)
    {
        context.Expect('[');
        context.SkipWhitespace();

        // 检查是否是数组类型（如 [I;1,2,3]）
        if (context.Peek(1) == ';')
        {
            var arrayType = context.Read();
            context.Expect(';');
            
            if (arrayType == 'I' || arrayType == 'i')
            {
                return ParseIntArray(context);
            }
            else if (arrayType == 'L' || arrayType == 'l')
            {
                return ParseLongArray(context);
            }
            else if (arrayType == 'B' || arrayType == 'b')
            {
                return ParseByteArray(context);
            }
        }

        // 普通列表
        var items = new List<NbtTag>();
        while (context.Peek() != ']' && !context.IsAtEnd)
        {
            context.SkipWhitespace();
            var item = ParseValue(context);
            if (item != null)
            {
                items.Add(item);
            }

            context.SkipWhitespace();
            if (context.Peek() == ',')
            {
                context.Read();
            }
        }

        context.Expect(']');

        // 创建对应类型的 NbtList
        if (items.Count == 0)
        {
            return new NbtList(NbtTagType.End);
        }

        var listType = items[0].TagType;
        var list = new NbtList(listType);
        foreach (var item in items)
        {
            list.Add(item);
        }
        return list;
    }

    private static NbtIntArray ParseIntArray(ParseContext context)
    {
        var values = new List<int>();
        
        while (context.Peek() != ']' && !context.IsAtEnd)
        {
            context.SkipWhitespace();
            var numStr = ParseNumberString(context);
            if (int.TryParse(numStr, out var value))
            {
                values.Add(value);
            }

            context.SkipWhitespace();
            if (context.Peek() == ',')
            {
                context.Read();
            }
        }

        context.Expect(']');
        return new NbtIntArray(values.ToArray());
    }

    private static NbtLongArray ParseLongArray(ParseContext context)
    {
        var values = new List<long>();
        
        while (context.Peek() != ']' && !context.IsAtEnd)
        {
            context.SkipWhitespace();
            var numStr = ParseNumberString(context);
            if (long.TryParse(numStr.TrimEnd('L', 'l'), out var value))
            {
                values.Add(value);
            }

            context.SkipWhitespace();
            if (context.Peek() == ',')
            {
                context.Read();
            }
        }

        context.Expect(']');
        return new NbtLongArray(values.ToArray());
    }

    private static NbtByteArray ParseByteArray(ParseContext context)
    {
        var values = new List<byte>();
        
        while (context.Peek() != ']' && !context.IsAtEnd)
        {
            context.SkipWhitespace();
            var numStr = ParseNumberString(context);
            if (byte.TryParse(numStr.TrimEnd('B', 'b'), out var value))
            {
                values.Add(value);
            }

            context.SkipWhitespace();
            if (context.Peek() == ',')
            {
                context.Read();
            }
        }

        context.Expect(']');
        return new NbtByteArray(values.ToArray());
    }

    private static NbtTag ParsePrimitive(ParseContext context)
    {
        var str = ParseNumberString(context);
        
        // 检查类型后缀
        if (str.EndsWith("b", StringComparison.OrdinalIgnoreCase))
        {
            if (byte.TryParse(str[..^1], out var value))
                return new NbtByte(value);
        }
        else if (str.EndsWith("s", StringComparison.OrdinalIgnoreCase))
        {
            if (short.TryParse(str[..^1], out var value))
                return new NbtShort(value);
        }
        else if (str.EndsWith("l", StringComparison.OrdinalIgnoreCase))
        {
            if (long.TryParse(str[..^1], out var value))
                return new NbtLong(value);
        }
        else if (str.EndsWith("f", StringComparison.OrdinalIgnoreCase))
        {
            if (float.TryParse(str[..^1], out var value))
                return new NbtFloat(value);
        }
        else if (str.EndsWith("d", StringComparison.OrdinalIgnoreCase))
        {
            if (double.TryParse(str[..^1], out var value))
                return new NbtDouble(value);
        }
        else if (str.Contains('.'))
        {
            if (double.TryParse(str, out var value))
                return new NbtDouble(value);
        }
        else if (int.TryParse(str, out var intValue))
        {
            return new NbtInt(intValue);
        }
        else if (str.Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            return new NbtByte(1);
        }
        else if (str.Equals("false", StringComparison.OrdinalIgnoreCase))
        {
            return new NbtByte(0);
        }

        // 默认作为字符串
        return new NbtString(str);
    }

    private static string ParseQuotedString(ParseContext context)
    {
        var quote = context.Read();
        var sb = new StringBuilder();

        while (!context.IsAtEnd)
        {
            var ch = context.Read();
            if (ch == '\\')
            {
                if (!context.IsAtEnd)
                {
                    var next = context.Read();
                    sb.Append(next);
                }
            }
            else if (ch == quote)
            {
                break;
            }
            else
            {
                sb.Append(ch);
            }
        }

        return sb.ToString();
    }

    private static string ParseUnquotedString(ParseContext context)
    {
        var sb = new StringBuilder();
        while (!context.IsAtEnd)
        {
            var ch = context.Peek();
            if (char.IsWhiteSpace(ch) || ch == ':' || ch == ',' || ch == '}' || ch == ']')
            {
                break;
            }
            sb.Append(context.Read());
        }
        return sb.ToString();
    }

    private static string ParseNumberString(ParseContext context)
    {
        var sb = new StringBuilder();
        while (!context.IsAtEnd)
        {
            var ch = context.Peek();
            if (char.IsDigit(ch) || ch == '.' || ch == '-' || ch == '+' || ch == 'e' || ch == 'E' || 
                ch == 'b' || ch == 'B' || ch == 's' || ch == 'S' || ch == 'l' || ch == 'L' || 
                ch == 'f' || ch == 'F' || ch == 'd' || ch == 'D')
            {
                sb.Append(context.Read());
            }
            else
            {
                break;
            }
        }
        return sb.ToString();
    }

    private class ParseContext
    {
        private readonly string _input;
        private int _position;

        public ParseContext(string input)
        {
            _input = input;
            _position = 0;
        }

        public bool IsAtEnd => _position >= _input.Length;

        public char Peek(int ahead = 0)
        {
            var pos = _position + ahead;
            return pos < _input.Length ? _input[pos] : '\0';
        }

        public char Read()
        {
            return IsAtEnd ? '\0' : _input[_position++];
        }

        public void Expect(char expected)
        {
            var ch = Read();
            if (ch != expected)
            {
                throw new FormatException($"Expected '{expected}' but got '{ch}' at position {_position - 1}");
            }
        }

        public void SkipWhitespace()
        {
            while (!IsAtEnd && char.IsWhiteSpace(Peek()))
            {
                Read();
            }
        }
    }
}

