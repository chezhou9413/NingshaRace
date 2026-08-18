using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace NingshaRaceLib.GiantTomb.Metadata
{
    //类职责：以零第三方依赖方式解析metadata使用的标准JSON对象、数组、字符串和数值。
    internal sealed class GiantTombJsonParser
    {
        private readonly string text;
        private int position;

        //函数职责：建立针对一段完整JSON文本的有状态解析器。
        private GiantTombJsonParser(string text)
        {
            this.text = text ?? throw new ArgumentNullException(nameof(text));
        }

        //函数职责：解析完整JSON并拒绝根值之后的多余字符。
        public static object Parse(string text)
        {
            GiantTombJsonParser parser = new GiantTombJsonParser(text);
            object result = parser.ReadValue();
            parser.SkipWhitespace();
            if (parser.position != parser.text.Length)
            {
                throw parser.Error("JSON根值后存在多余字符");
            }
            return result;
        }

        //函数职责：根据当前首字符分派到对应JSON值解析流程。
        private object ReadValue()
        {
            SkipWhitespace();
            if (position >= text.Length) throw Error("JSON意外结束");
            char current = text[position];
            if (current == '{') return ReadObject();
            if (current == '[') return ReadArray();
            if (current == '"') return ReadString();
            if (current == '-' || char.IsDigit(current)) return ReadNumber();
            if (MatchLiteral("true")) return true;
            if (MatchLiteral("false")) return false;
            if (MatchLiteral("null")) return null;
            throw Error("JSON包含未知值起始字符: " + current);
        }

        //函数职责：读取对象成员并拒绝重复键、缺失冒号和缺失分隔符。
        private Dictionary<string, object> ReadObject()
        {
            Expect('{');
            Dictionary<string, object> result = new Dictionary<string, object>(StringComparer.Ordinal);
            SkipWhitespace();
            if (TryConsume('}')) return result;
            while (true)
            {
                SkipWhitespace();
                if (position >= text.Length || text[position] != '"') throw Error("JSON对象键必须是字符串");
                string key = ReadString();
                if (result.ContainsKey(key)) throw Error("JSON对象包含重复键: " + key);
                SkipWhitespace();
                Expect(':');
                result.Add(key, ReadValue());
                SkipWhitespace();
                if (TryConsume('}')) return result;
                Expect(',');
            }
        }

        //函数职责：读取数组元素并拒绝缺失分隔符或未闭合数组。
        private List<object> ReadArray()
        {
            Expect('[');
            List<object> result = new List<object>();
            SkipWhitespace();
            if (TryConsume(']')) return result;
            while (true)
            {
                result.Add(ReadValue());
                SkipWhitespace();
                if (TryConsume(']')) return result;
                Expect(',');
            }
        }

        //函数职责：读取JSON字符串并解码标准转义序列和UTF-16码元。
        private string ReadString()
        {
            Expect('"');
            StringBuilder builder = new StringBuilder();
            while (position < text.Length)
            {
                char current = text[position++];
                if (current == '"') return builder.ToString();
                if (current < 0x20) throw Error("JSON字符串包含未转义控制字符");
                if (current != '\\')
                {
                    builder.Append(current);
                    continue;
                }
                if (position >= text.Length) throw Error("JSON字符串转义不完整");
                char escaped = text[position++];
                switch (escaped)
                {
                    case '"': builder.Append('"'); break;
                    case '\\': builder.Append('\\'); break;
                    case '/': builder.Append('/'); break;
                    case 'b': builder.Append('\b'); break;
                    case 'f': builder.Append('\f'); break;
                    case 'n': builder.Append('\n'); break;
                    case 'r': builder.Append('\r'); break;
                    case 't': builder.Append('\t'); break;
                    case 'u': builder.Append(ReadUnicodeCodeUnit()); break;
                    default: throw Error("JSON字符串包含未知转义: " + escaped);
                }
            }
            throw Error("JSON字符串未闭合");
        }

        //函数职责：读取四位十六进制Unicode码元。
        private char ReadUnicodeCodeUnit()
        {
            if (position + 4 > text.Length) throw Error("JSON Unicode转义不完整");
            string digits = text.Substring(position, 4);
            position += 4;
            if (!ushort.TryParse(digits, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out ushort value))
            {
                throw Error("JSON Unicode转义无效: " + digits);
            }
            return (char)value;
        }

        //函数职责：读取JSON数值并按是否包含小数或指数返回整数或双精度数。
        private object ReadNumber()
        {
            int start = position;
            if (text[position] == '-') position++;
            ReadDigits();
            bool floating = false;
            if (position < text.Length && text[position] == '.')
            {
                floating = true;
                position++;
                ReadDigits();
            }
            if (position < text.Length && (text[position] == 'e' || text[position] == 'E'))
            {
                floating = true;
                position++;
                if (position < text.Length && (text[position] == '+' || text[position] == '-')) position++;
                ReadDigits();
            }
            string number = text.Substring(start, position - start);
            if (!floating && long.TryParse(number, NumberStyles.Integer, CultureInfo.InvariantCulture, out long integer)) return integer;
            if (double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out double real) && !double.IsInfinity(real) && !double.IsNaN(real)) return real;
            throw Error("JSON数值无效: " + number);
        }

        //函数职责：至少读取一个十进制数字并推进当前位置。
        private void ReadDigits()
        {
            int start = position;
            while (position < text.Length && char.IsDigit(text[position])) position++;
            if (position == start) throw Error("JSON数值缺少数字");
        }

        //函数职责：在当前位置匹配并消费固定JSON字面量。
        private bool MatchLiteral(string literal)
        {
            if (position + literal.Length > text.Length || string.CompareOrdinal(text, position, literal, 0, literal.Length) != 0) return false;
            position += literal.Length;
            return true;
        }

        //函数职责：跳过JSON允许的空白字符。
        private void SkipWhitespace()
        {
            while (position < text.Length && (text[position] == ' ' || text[position] == '\t' || text[position] == '\r' || text[position] == '\n')) position++;
        }

        //函数职责：在当前字符符合预期时消费它。
        private bool TryConsume(char expected)
        {
            if (position >= text.Length || text[position] != expected) return false;
            position++;
            return true;
        }

        //函数职责：强制消费指定字符并在不匹配时报告位置。
        private void Expect(char expected)
        {
            if (!TryConsume(expected)) throw Error("JSON期望字符: " + expected);
        }

        //函数职责：建立包含当前位置的精确JSON格式异常。
        private InvalidDataException Error(string message)
        {
            return new InvalidDataException(message + "，位置 " + position);
        }
    }
}
