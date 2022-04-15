using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModLib.Compiler
{
    public static partial class Compiler
    {
        public static class Lexer
        {
            public class PositionInfo
            {
                public int Line; // Starts from 1
                public int Column; // Starts from 1
                public int Index; // Starts from 0
            }

            // Convenient class that *safely* reads the code string for lexing
            // You can even (try to) read out of bounds and it won't die. It'll just return '\0'
            public class CodeReader
            {
                public CompileContext compileContext;
                private int _Position;
                public int Position
                {
                    get { return _Position; }
                    set
                    {
                        _Position = value;
                        if (_Position >= Text?.Length)
                        {
                            _Position = Text.Length - 1;
                            EOF = true;
                        }
                    }
                }
                public string Text;
                public bool EOF; // end-of-file
                public List<int> LineStartPositions;

                public CodeReader(CompileContext context, string text)
                {
                    compileContext = context;
                    Position = 0;
                    Text = text;
                    EOF = (Text.Length == 0);

                    // Add another line to the end
                    if (!Text.EndsWith("\n"))
                    {
                        if (Text.Contains("\r"))
                        {
                            Text += "\r\n";
                        }
                        else
                        {
                            Text += "\n";
                        }
                    }

                    // Figure out where each newline is
                    LineStartPositions = new List<int>() { 0 };
                    int i;
                    for (i = 0; i < Text.Length; i++)
                    {
                        if (Text[i] == '\n')
                        {
                            LineStartPositions.Add(i + 1);
                        }
                    }
                    LineStartPositions.Add(i + 1);
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public char ReadChar()
                {
                    if (EOF)
                        return '\0';
                    return Text[Position++];
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public char PeekChar()
                {
                    if (EOF)
                        return '\0';
                    return Text[Position];
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public char PeekCharAhead(int lookahead = 1)
                {
                    if (EOF)
                        return '\0';
                    int index = Position + lookahead;
                    if (index >= Text.Length)
                        return '\0';
                    return Text[index];
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public void AdvancePosition(int amount = 1)
                {
                    Position += amount;
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public void JumpTo(int position)
                {
                    Position = position;
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public PositionInfo GetPositionInfo()
                {
                    return GetPositionInfo(Position);
                }

                public int LineStartSearch = 0;

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public PositionInfo GetPositionInfo(int index)
                {
                    int i;
                    for (i = LineStartSearch; i < LineStartPositions.Count - 1; i++)
                    {
                        if (index >= LineStartPositions[i] && index < LineStartPositions[i + 1])
                            break;
                    }
                    LineStartSearch = i;
                    int line = i + 1;
                    int column = index - LineStartPositions[i] + 1;

                    return new PositionInfo()
                    {
                        Line = line,
                        Column = column,
                        Index = index
                    };
                }
            }

            public static List<Token> LexString(CompileContext context, string input)
            {
                List<Token> output = new List<Token>(128);
                CodeReader reader = new CodeReader(context, input);
                Token currentToken = null;
                bool gms2 = context.Data?.IsGameMaker2() ?? false;
                while (currentToken?.Kind != Token.TokenKind.EOF)
                {
                    currentToken = ReadToken(reader, gms2);
                    output.Add(currentToken);
                }
                return output;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Token ReadToken(CodeReader cr, bool gms2)
            {
                SkipWhitespaceAndComments(cr);
                if (cr.EOF)
                {
                    return new Token(Token.TokenKind.EOF);
                }

                if (cr.PeekChar() == '#')
                {
                    // Skip preprocessor directive/macro/etc. Maybe support could be added later... but not yet.
                    while (!cr.EOF)
                    {
                        if (cr.PeekChar() == '\n')
                            break;
                        cr.AdvancePosition();
                    }
                }

                char c = cr.PeekChar();
                if (!char.IsLetter(c) && c != '_')
                {
                    // Numbers/hex
                    if (c == '$' || (c == '0' && cr.PeekCharAhead() == 'x'))
                    {
                        return ReadHexLiteral(cr);
                    }
                    if (char.IsDigit(c) || (c == '.' && char.IsDigit(cr.PeekCharAhead())))
                    {
                        return ReadNumberLiteral(cr);
                    }

                    // Strings
                    if (gms2)
                    {
                        if (c == '@')
                        {
                            char c2 = cr.PeekCharAhead();
                            if (c2 == '"' || c2 == '\'')
                            {
                                cr.AdvancePosition();
                                return ReadStringLiteralNoEscape(cr);
                            }
                        }
                        else if (c == '"')
                        {
                            return ReadStringLiteral(cr);
                        }
                    }
                    else
                    {
                        if (c == '"' || c == '\'')
                        {
                            return ReadStringLiteral(cr);
                        }
                    }

                    // Operators/everything else
                    switch (c)
                    {
                        case '{':
                            cr.AdvancePosition();
                            return new Token(Token.TokenKind.OpenBlock, cr.GetPositionInfo(cr.Position - 1));
                        case '}':
                            cr.AdvancePosition();
                            return new Token(Token.TokenKind.CloseBlock, cr.GetPositionInfo(cr.Position - 1));
                        case '(':
                            cr.AdvancePosition();
                            return new Token(Token.TokenKind.OpenParen, cr.GetPositionInfo(cr.Position - 1));
                        case ')':
                            cr.AdvancePosition();
                            return new Token(Token.TokenKind.CloseParen, cr.GetPositionInfo(cr.Position - 1));
                        case '|':
                            {
                                char ahead = cr.PeekCharAhead();
                                if (ahead != '\0')
                                {
                                    switch (ahead)
                                    {
                                        case '|':
                                            cr.AdvancePosition(2);
                                            return new Token(Token.TokenKind.LogicalOr, cr.GetPositionInfo(cr.Position - 2));
                                        case '=':
                                            cr.AdvancePosition(2);
                                            return new Token(Token.TokenKind.AssignOr, cr.GetPositionInfo(cr.Position - 2));
                                    }
                                }
                            }
                            cr.AdvancePosition();
                            return new Token(Token.TokenKind.BitwiseOr, cr.GetPositionInfo(cr.Position - 1));
                        case '^':
                            {
                                char ahead = cr.PeekCharAhead();
                                if (ahead != '\0')
                                {
                                    switch (ahead)
                                    {
                                        case '^':
                                            cr.AdvancePosition(2);
                                            return new Token(Token.TokenKind.LogicalXor, cr.GetPositionInfo(cr.Position - 2));
                                        case '=':
                                            cr.AdvancePosition(2);
                                            return new Token(Token.TokenKind.AssignXor, cr.GetPositionInfo(cr.Position - 2));
                                    }
                                }
                            }
                            cr.AdvancePosition();
                            return new Token(Token.TokenKind.BitwiseXor, cr.GetPositionInfo(cr.Position - 1));
                        case '&':
                            {
                                char ahead = cr.PeekCharAhead();
                                if (ahead != '\0')
                                {
                                    switch (ahead)
                                    {
                                        case '&':
                                            cr.AdvancePosition(2);
                                            return new Token(Token.TokenKind.LogicalAnd, cr.GetPositionInfo(cr.Position - 2));
                                        case '=':
                                            cr.AdvancePosition(2);
                                            return new Token(Token.TokenKind.AssignAnd, cr.GetPositionInfo(cr.Position - 2));
                                    }
                                }
                            }
                            cr.AdvancePosition();
                            return new Token(Token.TokenKind.BitwiseAnd, cr.GetPositionInfo(cr.Position - 1));
                        case '%':
                            {
                                if (cr.PeekCharAhead() == '=')
                                {
                                    cr.AdvancePosition(2);
                                    return new Token(Token.TokenKind.AssignMod, cr.GetPositionInfo(cr.Position - 2));
                                }
                            }
                            cr.AdvancePosition();
                            return new Token(Token.TokenKind.Mod, cr.GetPositionInfo(cr.Position - 1));
                        case '~':
                            cr.AdvancePosition();
                            return new Token(Token.TokenKind.BitwiseNegate, cr.GetPositionInfo(cr.Position - 1));
                        case '!':
                            cr.AdvancePosition();
                            if (cr.PeekChar() == '=')
                            {
                                cr.AdvancePosition();
                                return new Token(Token.TokenKind.CompareNotEqual, cr.GetPositionInfo(cr.Position - 2));
                            }
                            return new Token(Token.TokenKind.Not, cr.GetPositionInfo(cr.Position - 1));
                        case '[':
                            {
                                char ahead = cr.PeekCharAhead();
                                if (ahead != '\0')
                                {
                                    switch (ahead)
                                    {
                                        case '?':
                                            cr.AdvancePosition(2);
                                            return new Token(Token.TokenKind.OpenArrayMap, cr.GetPositionInfo(cr.Position - 2));
                                        case '@':
                                            cr.AdvancePosition(2);
                                            return new Token(Token.TokenKind.OpenArrayBaseArray, cr.GetPositionInfo(cr.Position - 2));
                                        case '#':
                                            cr.AdvancePosition(2);
                                            return new Token(Token.TokenKind.OpenArrayGrid, cr.GetPositionInfo(cr.Position - 2));
                                        case '|':
                                            cr.AdvancePosition(2);
                                            return new Token(Token.TokenKind.OpenArrayList, cr.GetPositionInfo(cr.Position - 2));
                                    }
                                }
                            }
                            cr.AdvancePosition();
                            return new Token(Token.TokenKind.OpenArray, cr.GetPositionInfo(cr.Position - 1));
                        case ']':
                            cr.AdvancePosition();
                            return new Token(Token.TokenKind.CloseArray, cr.GetPositionInfo(cr.Position - 1));
                        case '*':
                            cr.AdvancePosition();
                            if (cr.PeekChar() == '=')
                            {
                                cr.AdvancePosition();
                                return new Token(Token.TokenKind.AssignTimes, cr.GetPositionInfo(cr.Position - 2));
                            }
                            return new Token(Token.TokenKind.Times, cr.GetPositionInfo(cr.Position - 1));
                        case '/':
                            cr.AdvancePosition();
                            if (cr.PeekChar() == '=')
                            {
                                cr.AdvancePosition();
                                return new Token(Token.TokenKind.AssignDivide, cr.GetPositionInfo(cr.Position - 2));
                            }
                            return new Token(Token.TokenKind.Divide, cr.GetPositionInfo(cr.Position - 1));
                        case '+':
                            cr.AdvancePosition();
                            {
                                char next = cr.PeekChar();
                                if (next == '=')
                                {
                                    cr.AdvancePosition();
                                    return new Token(Token.TokenKind.AssignPlus, cr.GetPositionInfo(cr.Position - 2));
                                }
                                if (next == '+')
                                {
                                    cr.AdvancePosition();
                                    return new Token(Token.TokenKind.Increment, cr.GetPositionInfo(cr.Position - 2));
                                }
                            }
                            return new Token(Token.TokenKind.Plus, cr.GetPositionInfo(cr.Position - 1));
                        case '-':
                            cr.AdvancePosition();
                            {
                                char next = cr.PeekChar();
                                if (next == '=')
                                {
                                    cr.AdvancePosition();
                                    return new Token(Token.TokenKind.AssignMinus, cr.GetPositionInfo(cr.Position - 2));
                                }
                                if (next == '-')
                                {
                                    cr.AdvancePosition();
                                    return new Token(Token.TokenKind.Decrement, cr.GetPositionInfo(cr.Position - 2));
                                }
                            }
                            return new Token(Token.TokenKind.Minus, cr.GetPositionInfo(cr.Position - 1)); // converted to negate later if necessary
                        case ',':
                            cr.AdvancePosition();
                            return new Token(Token.TokenKind.Comma, cr.GetPositionInfo(cr.Position - 1));
                        case '.':
                            cr.AdvancePosition();
                            return new Token(Token.TokenKind.Dot, cr.GetPositionInfo(cr.Position - 1));
                        case ':':
                            cr.AdvancePosition();
                            if (cr.PeekChar() == '=')
                            {
                                cr.AdvancePosition();
                                return new Token(Token.TokenKind.Assign, ":=", cr.GetPositionInfo(cr.Position - 2)); // Apparently this exists
                            }
                            return new Token(Token.TokenKind.Colon, cr.GetPositionInfo(cr.Position - 1));
                        case ';':
                            cr.AdvancePosition();
                            return new Token(Token.TokenKind.EndStatement, cr.GetPositionInfo(cr.Position - 1));
                        case '=':
                            cr.AdvancePosition();
                            if (cr.PeekChar() == '=')
                            {
                                cr.AdvancePosition();
                                return new Token(Token.TokenKind.CompareEqual, cr.GetPositionInfo(cr.Position - 2));
                            }
                            return new Token(Token.TokenKind.Assign, "=", cr.GetPositionInfo(cr.Position - 1));
                        case '<':
                            {
                                char ahead = cr.PeekCharAhead();
                                if (ahead != '\0')
                                {
                                    switch (ahead)
                                    {
                                        case '<':
                                            cr.AdvancePosition(2);
                                            return new Token(Token.TokenKind.BitwiseShiftLeft, cr.GetPositionInfo(cr.Position - 2));
                                        case '=':
                                            cr.AdvancePosition(2);
                                            return new Token(Token.TokenKind.CompareLessEqual, cr.GetPositionInfo(cr.Position - 2));
                                        case '>':
                                            cr.AdvancePosition(2);
                                            return new Token(Token.TokenKind.CompareNotEqual, cr.GetPositionInfo(cr.Position - 2)); // <> exists, it's just legacy
                                    }
                                }
                            }
                            cr.AdvancePosition();
                            return new Token(Token.TokenKind.CompareLess, cr.GetPositionInfo(cr.Position - 1));
                        case '>':
                            {
                                char ahead = cr.PeekCharAhead();
                                if (ahead != '\0')
                                {
                                    switch (ahead)
                                    {
                                        case '>':
                                            cr.AdvancePosition(2);
                                            return new Token(Token.TokenKind.BitwiseShiftRight, cr.GetPositionInfo(cr.Position - 2));
                                        case '=':
                                            cr.AdvancePosition(2);
                                            return new Token(Token.TokenKind.CompareGreaterEqual, cr.GetPositionInfo(cr.Position - 2));
                                    }
                                }
                            }
                            cr.AdvancePosition();
                            return new Token(Token.TokenKind.CompareGreater, cr.GetPositionInfo(cr.Position - 1));
                        case '?':
                            cr.AdvancePosition();
                            return new Token(Token.TokenKind.Conditional, cr.GetPositionInfo(cr.Position - 1));
                        default:
                            cr.AdvancePosition();
                            return new Token(Token.TokenKind.Error, c.ToString(), cr.GetPositionInfo(cr.Position - 1));
                    }
                }
                else
                {
                    // Identifier
                    return ReadIdentifier(cr);
                }
            }

            private static Token ReadIdentifier(CodeReader cr)
            {
                StringBuilder sb = new StringBuilder();

                int index = cr.Position;

                sb.Append(cr.ReadChar());
                char c = cr.PeekChar();
                while (!cr.EOF && (char.IsLetterOrDigit(c) || c == '_'))
                {
                    sb.Append(c);
                    cr.AdvancePosition();
                    c = cr.PeekChar();
                }

                string identifierText = sb.ToString();
                return identifierText switch
                {
                    "and" => new Token(Token.TokenKind.LogicalAnd, cr.GetPositionInfo(index)),
                    "or" => new Token(Token.TokenKind.LogicalOr, cr.GetPositionInfo(index)),
                    "xor" => new Token(Token.TokenKind.LogicalXor, cr.GetPositionInfo(index)),
                    "while" => new Token(Token.TokenKind.KeywordWhile, cr.GetPositionInfo(index)),
                    "with" => new Token(Token.TokenKind.KeywordWith, cr.GetPositionInfo(index)),
                    "if" => new Token(Token.TokenKind.KeywordIf, cr.GetPositionInfo(index)),
                    "do" => new Token(Token.TokenKind.KeywordDo, cr.GetPositionInfo(index)),
                    "not" => new Token(Token.TokenKind.Not, cr.GetPositionInfo(index)),
                    "enum" => new Token(Token.TokenKind.Enum, cr.GetPositionInfo(index)),
                    "begin" => new Token(Token.TokenKind.OpenBlock, cr.GetPositionInfo(index)),
                    "end" => new Token(Token.TokenKind.CloseBlock, cr.GetPositionInfo(index)),
                    "var" => new Token(Token.TokenKind.KeywordVar, cr.GetPositionInfo(index)),
                    "globalvar" => new Token(Token.TokenKind.KeywordGlobalVar, cr.GetPositionInfo(index)),
                    "return" => new Token(Token.TokenKind.KeywordReturn, cr.GetPositionInfo(index)),
                    "default" => new Token(Token.TokenKind.KeywordDefault, cr.GetPositionInfo(index)),
                    "struct" => new Token(Token.TokenKind.KeywordStruct, cr.GetPositionInfo(index)),
                    "function" when cr.compileContext.Data.GMS2_3 => new Token(Token.TokenKind.KeywordFunction, cr.GetPositionInfo(index)),
                    "for" => new Token(Token.TokenKind.KeywordFor, cr.GetPositionInfo(index)),
                    "case" => new Token(Token.TokenKind.KeywordCase, cr.GetPositionInfo(index)),
                    "switch" => new Token(Token.TokenKind.KeywordSwitch, cr.GetPositionInfo(index)),
                    "until" => new Token(Token.TokenKind.KeywordUntil, cr.GetPositionInfo(index)),
                    "continue" => new Token(Token.TokenKind.KeywordContinue, cr.GetPositionInfo(index)),
                    "break" => new Token(Token.TokenKind.KeywordBreak, cr.GetPositionInfo(index)),
                    "else" => new Token(Token.TokenKind.KeywordElse, cr.GetPositionInfo(index)),
                    "repeat" => new Token(Token.TokenKind.KeywordRepeat, cr.GetPositionInfo(index)),
                    "exit" => new Token(Token.TokenKind.KeywordExit, cr.GetPositionInfo(index)),
                    "then" => new Token(Token.TokenKind.KeywordThen, cr.GetPositionInfo(index)),
                    "mod" => new Token(Token.TokenKind.Mod, cr.GetPositionInfo(index)),
                    "div" => new Token(Token.TokenKind.Div, cr.GetPositionInfo(index)),
                    _ => new Token(Token.TokenKind.Identifier, identifierText, cr.GetPositionInfo(index)),
                };
            }

            private static bool IsHexCharacter(char c)
            {
                return (char.IsDigit(c) || (char.ToLower(c) >= 'a' && char.ToLower(c) <= 'f'));
            }

            private static Token ReadStringLiteral(CodeReader cr)
            {
                StringBuilder sb = new StringBuilder();

                int index = cr.Position;
                char type = cr.PeekChar();

                cr.AdvancePosition();

                char c = cr.PeekChar();
                char c2;

                while (!cr.EOF)
                {
                    switch (c)
                    {
                        // Escape character
                        case '\\':
                            if (cr.compileContext.Data?.IsGameMaker2() == false)
                            {
                                sb.Append(c);
                                cr.AdvancePosition();
                                c = cr.PeekChar();
                                continue;
                            }
                            cr.AdvancePosition();
                            c = cr.PeekChar();
                            c2 = cr.PeekCharAhead();
                            switch (c)
                            {
                                case 'a':
                                    sb.Append('\a');
                                    break;
                                case 'n':
                                    sb.Append('\n');
                                    break;
                                case 'r':
                                    sb.Append('\r');
                                    break;
                                case 't':
                                    sb.Append('\t');
                                    break;
                                case 'v':
                                    sb.Append('\v');
                                    break;
                                case 'f':
                                    sb.Append('\f');
                                    break;
                                case 'b':
                                    sb.Append('\b');
                                    break;
                                case '\n':
                                    break;
                                case 'u':
                                    {
                                        int calc = 0;
                                        for (int i = 0; i < 4 && (IsHexCharacter(c2)); i++)
                                        {
                                            calc *= 16;
                                            calc = (!char.IsDigit(c2)) ? (calc + char.ToLower(c2) - 87) : (calc + c2 - 48);
                                            cr.AdvancePosition();
                                            c2 = cr.PeekCharAhead();
                                        }
                                        sb.Append((char)(ushort)calc);
                                    }
                                    break;
                                case 'x':
                                    cr.AdvancePosition();
                                    c = cr.PeekChar();
                                    c2 = cr.PeekCharAhead();
                                    if (IsHexCharacter(c))
                                    {
                                        char[] arr = new char[2] { c, ' ' };
                                        for (int i = 1; i < 2; i++)
                                        {
                                            if (!IsHexCharacter(c2))
                                                break;
                                            c = cr.PeekChar();
                                            c2 = cr.PeekCharAhead();
                                            arr[i] = c;
                                        }
                                        char value = (char)Convert.ToInt32(new string(arr), 16);
                                        sb.Append(value);
                                    }
                                    break;
                                default:
                                    if (c >= '0' && c <= '7')
                                    {
                                        // Octal
                                        StringBuilder sb2 = new StringBuilder();
                                        sb2.Append(c);
                                        for (int i = 1; i < 3; i++)
                                        {
                                            if (c2 < '0' || c > '7')
                                                break;
                                            cr.AdvancePosition();
                                            c = cr.PeekChar();
                                            if (c == '\0')
                                                c = ' ';
                                            c2 = cr.PeekCharAhead();
                                            if (c2 == '\0')
                                                c2 = ' ';

                                            sb2.Append(c);
                                        }
                                        sb.Append(Convert.ToInt32(sb2.ToString(), 8));
                                    }
                                    else
                                    {
                                        sb.Append(c);
                                    }
                                    break;
                            }
                            cr.AdvancePosition();
                            c = cr.PeekChar();
                            continue;
                        default:
                            if (c == type)
                                break;
                            sb.Append(c);
                            cr.AdvancePosition();
                            c = cr.PeekChar();
                            continue;
                    }
                    break;
                }

                cr.AdvancePosition();

                return new Token(Token.TokenKind.String, sb.ToString(), cr.GetPositionInfo(index));
            }

            private static Token ReadStringLiteralNoEscape(CodeReader cr)
            {
                StringBuilder sb = new StringBuilder();

                int index = cr.Position;

                char quoteChar = cr.ReadChar();

                while (!cr.EOF && cr.PeekChar() != quoteChar)
                {
                    sb.Append(cr.ReadChar());
                }

                if (!cr.EOF)
                    cr.AdvancePosition();

                return new Token(Token.TokenKind.String, sb.ToString(), cr.GetPositionInfo(index));
            }

            private static Token ReadNumberLiteral(CodeReader cr)
            {
                StringBuilder sb = new StringBuilder();

                int index = cr.Position;

                // Read the first character, because it's guaranteed
                char first = cr.ReadChar();
                sb.Append(first);
                bool hasUsedDot = (first == '.');

                // Read the digits
                while (!cr.EOF)
                {
                    char current = cr.PeekChar();
                    if (!char.IsDigit(current))
                    {
                        if (hasUsedDot)
                            break;
                        if (current != '.')
                            break;
                        hasUsedDot = true;
                    }
                    sb.Append(current);
                    cr.AdvancePosition();
                }

                return new Token(Token.TokenKind.Number, sb.ToString(), cr.GetPositionInfo(index));
            }

            private static Token ReadHexLiteral(CodeReader cr)
            {
                StringBuilder sb = new StringBuilder();

                int index = cr.Position;

                // Read the prefix ($ or 0x)
                sb.Append(cr.ReadChar());
                if (cr.PeekChar() == 'x')
                    sb.Append(cr.ReadChar());

                // Read the digits
                while (!cr.EOF)
                {
                    char current = cr.PeekChar();
                    if (!IsHexCharacter(current))
                        break;
                    sb.Append(current);
                    cr.AdvancePosition();
                }

                return new Token(Token.TokenKind.Number, sb.ToString(), cr.GetPositionInfo(index));
            }

            private static void SkipWhitespaceAndComments(CodeReader cr)
            {
                bool isStillWhiteSpace = true;
                while (isStillWhiteSpace)
                {
                    while (!cr.EOF && char.IsWhiteSpace(cr.PeekChar()))
                    {
                        cr.AdvancePosition();
                    }

                    if (!cr.EOF && cr.PeekChar() == '/')
                    {
                        char ahead = cr.PeekCharAhead();
                        if (ahead != '\0')
                        {
                            switch (ahead)
                            {
                                case '/':
                                    cr.AdvancePosition(2);
                                    while (!cr.EOF)
                                    {
                                        if (cr.PeekChar() == '\n')
                                            break;
                                        cr.AdvancePosition();
                                    }
                                    break;
                                case '*':
                                    cr.AdvancePosition(2);
                                    while (!cr.EOF)
                                    {
                                        if (cr.PeekChar() == '*' && cr.PeekCharAhead() == '/')
                                            break;
                                        cr.AdvancePosition();
                                    }
                                    if (!cr.EOF)
                                    {
                                        cr.AdvancePosition(2);
                                    }
                                    break;
                                default:
                                    // Ran into a non-comment; whitespace must be over
                                    isStillWhiteSpace = false;
                                    break;
                            }
                        }
                        else
                        {
                            // EOF ahead
                            isStillWhiteSpace = false;
                        }
                    }
                    else
                    {
                        // Ran into a non-comment; whitespace must be over
                        isStillWhiteSpace = false;
                    }
                }
            }


            public class Token
            {
                public PositionInfo Location;
                public TokenKind Kind;
                public string Content;

                public enum TokenKind
                {
                    EOF,
                    Identifier,
                    Number,
                    String,
                    Constant, // Maybe this can be parsed eventually
                    KeywordGlobalVar,
                    KeywordVar,
                    KeywordIf,
                    KeywordThen,
                    KeywordElse,
                    KeywordWhile,
                    KeywordDo,
                    KeywordFor,
                    KeywordUntil,
                    KeywordRepeat,
                    KeywordWith,
                    KeywordSwitch,
                    KeywordCase,
                    KeywordDefault,
                    KeywordReturn,
                    KeywordExit,
                    KeywordBreak,
                    KeywordContinue,
                    KeywordStruct, // Apparently this exists
                    KeywordFunction,
                    OpenBlock, // {
                    CloseBlock, // }
                    OpenArray, // [
                    CloseArray, // ]
                    OpenArrayMap, // [?
                    OpenArrayBaseArray, // [@
                    OpenArrayGrid, // [#
                    OpenArrayList, // [|
                    OpenParen, // (
                    CloseParen, // )
                    LogicalAnd, // &&
                    LogicalOr, // ||
                    LogicalXor, // ^^
                    Not, // !
                    CompareEqual, // ==   This can also be = depending on the context
                    CompareNotEqual, // !=
                    CompareGreater, // >
                    CompareLess, // <
                    CompareGreaterEqual, // >=
                    CompareLessEqual, // <=
                    Assign, // =
                    AssignOr, // |=
                    AssignAnd, // &=
                    AssignXor, // ^=
                    AssignMod, // %=
                    AssignPlus, // +=
                    AssignMinus, // -=
                    AssignTimes, // *=
                    AssignDivide, // /=
                    EndStatement, // ;
                    Comma, // ,
                    Dot, // .
                    Plus, // +
                    Minus, // -
                    Times, // *
                    Divide, // /
                    Div, // div
                    Mod, // mod
                    BitwiseOr, // |
                    BitwiseAnd, // &
                    BitwiseXor, // ^
                    BitwiseNegate, // ~
                    BitwiseShiftLeft, // <<
                    BitwiseShiftRight, // >>
                    Increment, // ++
                    Decrement, // --
                    Enum, // Maybe these could be parsed???
                    Colon, // :
                    Conditional, // ?

                    // Specially-processed tokens
                    ProcVariable,
                    ProcFunction,
                    ProcConstant,

                    Error
                }

                public Token()
                {

                }

                public Token(TokenKind kind)
                {
                    Kind = kind;
                }

                public Token(TokenKind kind, PositionInfo location)
                {
                    Kind = kind;
                    Location = location;
                }

                public Token(TokenKind kind, string value)
                {
                    Kind = kind;
                    Content = value;
                }

                public Token(TokenKind kind, string value, PositionInfo location)
                {
                    Kind = kind;
                    Content = value;
                    Location = location;
                }
            }
        }
    }
}
