using System.Text;
using System.Text.RegularExpressions;

namespace UanUiMarkup;

public sealed class UiParser
{
    private readonly string _src;
    private int _i;

    public UiParser(string source)
    {
        _src = source ?? throw new ArgumentNullException(nameof(source));
        _i = 0;
    }

    public UiNode[] Parse()
    {
        var nodes = new List<UiNode>();
        SkipWs();
        while (!Eof())
        {
            nodes.Add(ParseElement());
            SkipWs();
        }
        return [.. nodes];
    }

    private UiNode ParseElement()
    {
        SkipWs();
        var id = ReadIdentifier(required: true, allowDots: false);
        SkipWs();
        Expect('<');
        SkipWs();
        var type = ReadIdentifier(required: true, allowDots: false, allowHyphen: true);
        SkipWs();

        var node = new UiNode { Id = id, Type = type };

        if (Match(';'))
        {
            SkipWs();
            // attributes until '>' or '/>' 
            while (true)
            {
                SkipWs();
                if (Peek() == '>' || (Peek() == '/' && Peek(1) == '>'))
                    break;

                var key = ReadIdentifier(required: true, allowDots: false, allowHyphen: true);
                SkipWs();
                Expect('=');
                SkipWs();
                var value = ReadValue();
                node.Attributes[key] = value;
                SkipWs();
            }
        }

        // end of start tag 
        if (Match('/', '>'))
        {
            return node;
        }
        Expect('>');

        SkipWs();
        if (Match('{'))
        {
            // children until '}' 
            SkipWs();
            while (!Match('}'))
            {
                var child = ParseElement();
                node.Children.Add(child);
                SkipWs();
            }
        }

        return node;
    }


    private object ReadValue()
    {
        char c = Peek();
        if (c == '"' || c == '\'')
        {
            return ReadQuoted();
        }

        // unquoted token until whitespace or one of delimiters 
        var token = ReadUntilStop();
        if (token.Length == 0)
            throw Error("Expected a value");

        // if purely digits -> int 
        if (Regex.IsMatch(token, @"^\d+$"))
        {
            // safe int parse with overflow check 
            if (!int.TryParse(token, out var n))
                throw Error($"Integer out of range: {token}");
            return n;
        }
        // if matches digits '.' digits -> throw 
        if (Regex.IsMatch(token, @"^\d+\.\d+$"))
        {
            throw Error($"Floating-point values are not allowed: {token}");
        }

        // if matches 'true' or 'false' -> bool 
        if (token.Equals("true", StringComparison.OrdinalIgnoreCase))
            return true;
        if (token.Equals("false", StringComparison.OrdinalIgnoreCase))
            return false;

        // otherwise -> string 
        return token;
    }

    private string ReadQuoted()
    {
        char quote = Next(); // consume quote 
        var sb = new StringBuilder();
        while (!Eof())
        {
            char c = Next();
            if (c == quote)
                return sb.ToString();
            if (c == '\\')
            {
                if (Eof()) throw Error("Unterminated escape in string");
                char e = Next();
                sb.Append(e switch
                {
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    '\\' => '\\',
                    '"' => '"',
                    '\'' => '\'',
                    _ => e
                });
            }
            else
            {
                sb.Append(c);
            }
        }
        throw Error("Unterminated quoted string");
    }

    private string ReadUntilStop()
    {
        var sb = new StringBuilder();
        while (!Eof())
        {
            char c = Peek();
            if (char.IsWhiteSpace(c) || c == '>' || c == '{' || c == '}' || c == '/')
                break;
            sb.Append(c);
            _i++;
        }
        return sb.ToString();
    }

    private string ReadIdentifier(bool required, bool allowDots, bool allowHyphen = true)
    {
        SkipWs();
        int start = _i;
        while (!Eof())
        {
            char c = Peek();
            bool ok = char.IsLetterOrDigit(c) || c == '_' || (allowHyphen && c == '-') || (allowDots && c == '.');
            if (!ok) break;
            _i++;
        }
        if (_i == start)
        {
            if (required) throw Error("Expected identifier");
            return "";
        }
        return _src[start.._i];
    }

    private void SkipWs()
    {
        while (!Eof())
        {
            char c = Peek();
            if (char.IsWhiteSpace(c)) { _i++; continue; }
            // line comments: // ... 
            if (c == '/' && Peek(1) == '/')
            {
                _i += 2;
                while (!Eof() && Peek() != '\n') _i++;
                continue;
            }
            // block comments: /* ... */ 
            if (c == '/' && Peek(1) == '*')
            {
                _i += 2;
                while (!Eof() && !(Peek() == '*' && Peek(1) == '/')) _i++;
                if (!Eof()) _i += 2;
                continue;
            }
            break;
        }
    }

    private bool Match(char a, char b)
    {
        if (!Eof() && Peek() == a && Peek(1) == b)
        {
            _i += 2;
            return true;
        }
        return false;
    }

    private bool Match(char c)
    {
        if (!Eof() && Peek() == c)
        {
            _i++;
            return true;
        }
        return false;
    }

    private void Expect(char c)
    {
        if (Eof() || Next() != c)
            throw Error($"Expected '{c}'");
    }

    private char Next()
    {
        if (Eof()) throw Error("Unexpected end of input");
        return _src[_i++];
    }

    private char Peek(int lookahead = 0)
    {
        int idx = _i + lookahead;
        return idx >= 0 && idx < _src.Length ? _src[idx] : '\0';
    }

    private bool Eof() => _i >= _src.Length;

    private UiParseException Error(string message) => new(message, _i);
}
