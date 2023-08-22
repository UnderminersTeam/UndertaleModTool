using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using UndertaleModLib.Models;
using static UndertaleModLib.Compiler.Compiler.Lexer.Token;
using UndertaleModLib.Decompiler;

namespace UndertaleModLib.Compiler
{
    public static partial class Compiler
    {
        public static class Parser
        {
            private static Queue<Statement> remainingStageOne = new Queue<Statement>();
            public static List<string> ErrorMessages = new List<string>();
            private static bool hasError = false; // temporary variable that clears in several places

            // Struct function names that haven't been used yet.
            private static Queue<string> usableStructNames = new();

            // Not really universally unique nor does it follow the UUID spec,
            // it just needs to be unique in the same script.
            private static int uuidCounter = 0;

            public class ExpressionConstant
            {
                public Kind kind = Kind.None;
                public bool isBool; // if true, uses the double value
                public double valueNumber;
                public string valueString;
                public long valueInt64;

                public enum Kind
                {
                    None,
                    Number,
                    String,
                    Constant,
                    Int64,
                    Reference
                }

                public ExpressionConstant(double val)
                {
                    kind = Kind.Number;
                    valueNumber = val;
                }

                public ExpressionConstant(string str)
                {
                    kind = Kind.String;
                    valueString = str;
                }

                public ExpressionConstant(long val)
                {
                    kind = Kind.Int64;
                    valueInt64 = val;
                }

                public ExpressionConstant(ExpressionConstant copyFrom)
                {
                    kind = copyFrom.kind;
                    valueNumber = copyFrom.valueNumber;
                    valueString = copyFrom.valueString;
                    valueInt64 = copyFrom.valueInt64;
                }
            }

            public static void ThrowException(string msg, Lexer.Token context)
            {
                throw new Exception(msg + (context.Location != null ?
                    string.Format("\nAround line {0} column {1}.", context.Location.Line, context.Location.Column)
                    : "\nUnknown location."));
            }

            public class Statement
            {
                public StatementKind Kind;
                public Lexer.Token Token;
                public string Text;
                public UndertaleInstruction.DataType? DataType;
                public List<Statement> Children;
                public ExpressionConstant Constant;
                private int _ID = 0;
                public int ID { get { return _ID; } set { _ID = value; WasIDSet = true; } }
                public bool WasIDSet = false; // Hack to fix addressing the first object index in code

                public enum StatementKind
                {
                    Block,
                    Assign,
                    ForLoop,
                    WhileLoop,
                    DoUntilLoop,
                    With,
                    RepeatLoop,
                    Switch,
                    SwitchCase,
                    SwitchDefault,
                    FunctionCall,
                    FunctionDef,
                    Break,
                    Continue,
                    Exit,
                    Return,
                    TempVarDeclare, // also assign if it's there
                    GlobalVarDeclare, // special version
                    VariableName,
                    If,
                    Enum,
                    Pre, // ++ or -- before variable, AS AN EXPRESSION OR STATEMENT
                    Post, // ++ or -- after variable, AS AN EXPRESSION OR STATEMENT

                    ExprConstant,
                    ExprBinaryOp,
                    ExprArray, // maybe?
                    ExprStruct,
                    ExprFunctionCall,
                    ExprUnary,
                    ExprConditional,
                    ExprVariableRef,
                    ExprSingleVariable,
                    ExprFuncName,

                    Token,
                    Discard // optimization stage produces this
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public Statement()
                {
                    Children = new List<Statement>();
                }

                // Copy
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public Statement(Statement s)
                {
                    Kind = s.Kind;
                    Token = s.Token;
                    Text = s.Text;
                    ID = s.ID;
                    WasIDSet = s.WasIDSet;
                    DataType = s.DataType;
                    Children = new List<Statement>(s.Children);
                    if (s.Constant != null)
                        Constant = new ExpressionConstant(s.Constant);
                }

                // Copy with new token kind
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public Statement(TokenKind newType, Statement s)
                {
                    Token = s.Token;
                    Token.Kind = newType;
                    Text = s.Text;
                    ID = s.ID;
                    WasIDSet = s.WasIDSet;
                    DataType = s.DataType;
                    Children = new List<Statement>(s.Children);
                    if (s.Constant != null)
                        Constant = new ExpressionConstant(s.Constant);
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public Statement(StatementKind kind)
                {
                    Kind = kind;
                    Children = new List<Statement>();
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public Statement(StatementKind kind, string text)
                {
                    Kind = kind;
                    Text = text;
                    Children = new List<Statement>();
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public Statement(StatementKind kind, Lexer.Token token)
                {
                    Kind = kind;
                    Token = token;
                    if (token.Content != null)
                        Text = token.Content;
                    Children = new List<Statement>();
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public Statement(StatementKind kind, Lexer.Token token, ExpressionConstant constant)
                {
                    Kind = kind;
                    Token = token;
                    if (token.Content != null)
                        Text = token.Content;
                    Children = new List<Statement>();
                    if (constant != null)
                        Constant = new ExpressionConstant(constant);
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public Statement(TokenKind newKind, Lexer.Token copyFrom)
                {
                    Kind = StatementKind.Token;
                    Token = new Lexer.Token(newKind);
                    Token.Content = copyFrom.Content;
                    Token.Location = copyFrom.Location;
                    if (copyFrom.Content != null)
                        Text = copyFrom.Content;
                    Children = new List<Statement>();
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public Statement(TokenKind newKind, Lexer.Token copyFrom, int id)
                {
                    Kind = StatementKind.Token;
                    Token = new Lexer.Token(newKind);
                    Token.Content = copyFrom.Content;
                    Token.Location = copyFrom.Location;
                    if (Token.Content != null)
                        Text = Token.Content;
                    this.ID = id;
                    Children = new List<Statement>();
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public Statement(TokenKind newKind, Lexer.Token copyFrom, ExpressionConstant constant)
                {
                    Kind = StatementKind.Token;
                    Token = new Lexer.Token(newKind);
                    Token.Content = copyFrom.Content;
                    Token.Location = copyFrom.Location;
                    if (Token.Content != null)
                        Text = Token.Content;
                    this.Constant = constant;
                    Children = new List<Statement>();
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Statement EnsureStatementKind(Statement.StatementKind kind)
            {
                if (remainingStageOne.Count == 0)
                {
                    ReportCodeError("Unexpected end of code.", false);
                    return null;
                }
                if (remainingStageOne.Peek().Kind == kind)
                {
                    return remainingStageOne.Dequeue();
                }
                else
                {
                    Statement s = remainingStageOne.Peek();
                    ReportCodeError("Expected statement kind " + kind.ToString() + ", got " + s.Kind.ToString() + ".", s?.Token, true);
                    return null;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Statement EnsureTokenKind(TokenKind kind)
            {
                if (remainingStageOne.Count == 0)
                {
                    ReportCodeError("Unexpected end of code.", false);
                    return null;
                }
                if (remainingStageOne.Peek()?.Token?.Kind == kind)
                {
                    return remainingStageOne.Dequeue();
                }
                else
                {
                    Statement s = remainingStageOne.Peek();
                    ReportCodeError("Expected token kind " + kind.ToString() + ", got " + s?.Token?.Kind.ToString() + ".", s?.Token, true);
                    return null;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool IsNextToken(TokenKind kind)
            {
                if (remainingStageOne.Count == 0)
                {
                    ReportCodeError("Unexpected end of code.", false);
                    return false;
                }
                var t = remainingStageOne.Peek().Token;
                if (t == null)
                    return false;
                return t.Kind == kind;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool IsNextToken(TokenKind kind, TokenKind kind2)
            {
                if (remainingStageOne.Count == 0)
                {
                    ReportCodeError("Unexpected end of code.", false);
                    return false;
                }
                var t = remainingStageOne.Peek().Token;
                if (t == null)
                    return false;
                TokenKind actualKind = t.Kind;
                return actualKind == kind || actualKind == kind2;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool IsNextToken(TokenKind kind, TokenKind kind2, TokenKind kind3)
            {
                if (remainingStageOne.Count == 0)
                {
                    ReportCodeError("Unexpected end of code.", false);
                    return false;
                }
                var t = remainingStageOne.Peek().Token;
                if (t == null)
                    return false;
                TokenKind actualKind = t.Kind;
                return actualKind == kind || actualKind == kind2 || actualKind == kind3;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool IsNextToken(TokenKind kind, TokenKind kind2, TokenKind kind3, TokenKind kind4)
            {
                if (remainingStageOne.Count == 0)
                {
                    ReportCodeError("Unexpected end of code.", false);
                    return false;
                }
                var t = remainingStageOne.Peek().Token;
                if (t == null)
                    return false;
                TokenKind actualKind = t.Kind;
                return actualKind == kind || actualKind == kind2 || actualKind == kind3 || actualKind == kind4;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool IsNextToken(params TokenKind[] kinds)
            {
                if (remainingStageOne.Count == 0)
                {
                    ReportCodeError("Unexpected end of code.", false);
                    return false;
                }
                var t = remainingStageOne.Peek().Token;
                if (t == null)
                    return false;
                return t.Kind.In(kinds);
            }

            // Discards token if the next token kind is of <kinds>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool IsNextTokenDiscard(TokenKind kind)
            {
                if (remainingStageOne.Count == 0)
                {
                    ReportCodeError("Unexpected end of code.", false);
                    return false;
                }
                var t = remainingStageOne.Peek().Token;
                if (t == null)
                    return false;
                if (t.Kind == kind)
                {
                    remainingStageOne.Dequeue();
                    return true;
                }
                else
                {
                    return false;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static TokenKind GetNextTokenKind()
            {
                if (remainingStageOne.Count == 0)
                {
                    ReportCodeError("Unexpected end of code.", false);
                    return TokenKind.Error;
                }
                var t = remainingStageOne.Peek().Token;
                if (t == null)
                    return TokenKind.Error;
                return t.Kind;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void ReportCodeError(string msg, bool synchronize)
            {
                ErrorMessages.Add(msg);
                hasError = true;
                if (synchronize)
                    Synchronize();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void ReportCodeError(string msg, Lexer.Token context, bool synchronize)
            {
                if (context != null)
                {
                    if (msg.EndsWith(".", StringComparison.InvariantCulture))
                        msg = msg.Remove(msg.Length - 1);

                    if (context.Location != null)
                    {
                        msg += string.Format(" around line {0}, column {1}", context.Location.Line, context.Location.Column);
                    } else if (context.Kind == TokenKind.EOF)
                    {
                        msg += " around EOF (end of file)";
                    }
                    if (context.Content != null && context.Content.Length > 0)
                        msg += " (" + context.Content + ")";
                    ReportCodeError(msg + ".", synchronize);
                }
                else
                {
                    ReportCodeError(msg, synchronize);
                }
            }

            // If an error or something like that occurs, this attempts to move the parser to a place
            // that can be parsed properly. Doing so allows multiple errors to be thrown.
            private static void Synchronize()
            {
                while (remainingStageOne.Count > 0)
                {
                    if (IsNextToken(TokenKind.EndStatement) || IsKeyword(GetNextTokenKind()))
                        break;
                    remainingStageOne.Dequeue();
                }
            }
            public static Statement ParseTokens(CompileContext context, List<Lexer.Token> tokens)
            {
                // Basic initialization
                remainingStageOne.Clear();
                ErrorMessages.Clear();
                context.LocalVars.Clear();
                if ((context.Data?.GeneralInfo?.BytecodeVersion ?? 15) >= 15)
                    context.LocalVars["arguments"] = "arguments";
                context.GlobalVars.Clear();
                context.Enums.Clear();
                context.FunctionsToObliterate.Clear();
                uuidCounter = 0;
                hasError = false;

                usableStructNames.Clear();
                foreach (UndertaleCode child in context.OriginalCode.ChildEntries) {
                    if (child.Name.Content.StartsWith("gml_Script____struct___")) {
                        usableStructNames.Enqueue(child.Name.Content["gml_Script_".Length..]);
                    }
                }

                // Ensuring an EOF exists
                if (tokens.Count == 0 || tokens[tokens.Count - 1].Kind != TokenKind.EOF)
                    tokens.Add(new Lexer.Token(TokenKind.EOF));

                // Run first parse stage- basic abstraction into functions and constants
                List<Statement> firstPass = new List<Statement>();

                bool chainedVariableReference = false;
                for (int i = 0; i < tokens.Count; i++)
                {
                    if (tokens[i].Kind == TokenKind.Identifier &&
                        tokens[i + 1].Kind == TokenKind.OpenParen)
                    {
                        // Differentiate between variable reference identifiers and functions
                        firstPass.Add(new Statement(TokenKind.ProcFunction, tokens[i]));
                    }
                    else if (tokens[i].Kind == TokenKind.Identifier)
                    {
                        // Convert identifiers into their proper references, at least sort of.
                        bool runOtherIdentifierStuff = true;
                        // Compile Pizza Tower state names
                        if (
                            GlobalDecompileContext.PTAutoStates && tokens.Count >= 3 &&
                            tokens[i + 1].Kind == TokenKind.Dot &&
                            tokens[i + 2].Kind == TokenKind.Identifier
                        ) {
                            if (
                                tokens[i].Content == "states" && 
                                AssetTypeResolver.PTStates.ContainsValue(tokens[i + 2].Content)
                            ) {
                                int? stateID = null;
                                foreach (var entry in AssetTypeResolver.PTStates)
                                {
                                    if (!entry.Value.Equals(tokens[i + 2].Content))
                                    {
                                        continue;
                                    }
                                    stateID = entry.Key;
                                }

                                if (stateID != null) {
                                    firstPass.Add(
                                        new Statement(
                                            TokenKind.ProcConstant,
                                            tokens[i],
                                            new ExpressionConstant((long)stateID)
                                        )
                                    );
                                    i += 2;
                                    runOtherIdentifierStuff = false;
                                }
                            }
                        }

                        if (runOtherIdentifierStuff) {
                            if ((i != 0 && tokens[i - 1].Kind == TokenKind.Dot) || 
                                !ResolveIdentifier(context, tokens[i].Content, out ExpressionConstant constant))
                            {
                                int ID = GetVariableID(context, tokens[i].Content, out _);
                                if (ID >= 0 && ID < 100000)
                                    firstPass.Add(new Statement(TokenKind.ProcVariable, tokens[i], -1)); // becomes self anyway?
                                else
                                    firstPass.Add(new Statement(TokenKind.ProcVariable, tokens[i], ID));
                            }
                            else
                            {
                                firstPass.Add(new Statement(TokenKind.ProcConstant, tokens[i], constant));
                            }
                        }
                    }
                    else if (tokens[i].Kind == TokenKind.Number)
                    {
                        // Convert number literals to their raw numerical value
                        Lexer.Token t = tokens[i];
                        ExpressionConstant constant = null;
                        if (t.Content[0] == '$' || t.Content.StartsWith("0x", StringComparison.InvariantCulture))
                        {
                            long val;
                            try
                            {
                                val = Convert.ToInt64(t.Content.Substring(t.Content[0] == '$' ? 1 : 2), 16);
                            } catch (Exception)
                            {
                                ReportCodeError("Invalid hex literal.", t, false);
                                constant = new ExpressionConstant(0);
                                firstPass.Add(new Statement(TokenKind.ProcConstant, t, constant));
                                continue;
                            }
                            if (val > int.MaxValue || val < int.MinValue)
                            {
                                constant = new ExpressionConstant(val);
                            }
                            else
                            {
                                constant = new ExpressionConstant((double)val);
                            }
                        }
                        else
                        {
                            if (!double.TryParse(t.Content, System.Globalization.NumberStyles.Float,
                                                 System.Globalization.CultureInfo.InvariantCulture,
                                                 out double val))
                            {
                                ReportCodeError("Invalid double number format.", t, false);
                            }
                            constant = new ExpressionConstant(val);
                        }
                        firstPass.Add(new Statement(TokenKind.ProcConstant, t, constant));
                    }
                    else if (tokens[i].Kind == TokenKind.String)
                    {
                        // Convert strings to their proper constant form
                        firstPass.Add(new Statement(TokenKind.ProcConstant, tokens[i],
                                                    new ExpressionConstant(tokens[i].Content)));
                    }
                    else
                    {
                        // Everything else that doesn't need to be pre-processed
                        firstPass.Add(new Statement(Statement.StatementKind.Token, tokens[i]));
                    }
                    chainedVariableReference = (tokens[tokens.Count - 1].Kind == TokenKind.Dot);
                }

                // Run the main parse stage- full abstraction, so that it's ready to be compiled
                Statement rootBlock = new Statement(Statement.StatementKind.Block);
                firstPass.ForEach(remainingStageOne.Enqueue);

                rootBlock = ParseBlock(context, true);
                if (hasError)
                    return null;
                
                // Remove any unused struct functions
                while (usableStructNames.Count > 0)
                    context.FunctionsToObliterate.Add(usableStructNames.Dequeue());

                return rootBlock;
            }

            private static Statement ParseBlock(CompileContext context, bool isRoot = false)
            {
                Statement s = new Statement(Statement.StatementKind.Block);

                if (!isRoot)
                    EnsureTokenKind(TokenKind.OpenBlock);

                while (remainingStageOne.Count > 0 && (isRoot || !IsNextToken(TokenKind.CloseBlock)) && !IsNextToken(TokenKind.EOF))
                {
                    Statement parsed = ParseStatement(context);
                    if (parsed != null) // Sometimes it can be null, for instance if there's a bunch of semicolons, or an error
                        s.Children.Add(parsed);
                }

                if (!isRoot)
                    EnsureTokenKind(TokenKind.CloseBlock);

                return s;
            }

            private static Statement ParseStatement(CompileContext context)
            {
                hasError = false;
                Statement s = null;
                switch (GetNextTokenKind())
                {
                    case TokenKind.OpenBlock:
                        s = ParseBlock(context);
                        break;
                    case TokenKind.ProcFunction:
                    case TokenKind.KeywordNew:
                        s = ParseFunctionCall(context);
                        break;
                    case TokenKind.KeywordVar:
                        s = ParseLocalVarDeclare(context); // can be multiple
                        break;
                    case TokenKind.KeywordGlobalVar:
                        s = ParseGlobalVarDeclare(context); // can be multiple
                        break;
                    case TokenKind.KeywordBreak:
                        s = new Statement(Statement.StatementKind.Break, remainingStageOne.Dequeue().Token);
                        break;
                    case TokenKind.KeywordContinue:
                        s = new Statement(Statement.StatementKind.Continue, remainingStageOne.Dequeue().Token);
                        break;
                    case TokenKind.KeywordExit:
                        s = new Statement(Statement.StatementKind.Exit, remainingStageOne.Dequeue().Token);
                        break;
                    case TokenKind.KeywordReturn:
                        s = ParseReturn(context);
                        break;
                    case TokenKind.KeywordWith:
                        s = ParseWith(context);
                        break;
                    case TokenKind.KeywordWhile:
                        s = ParseWhile(context);
                        break;
                    case TokenKind.KeywordRepeat:
                        s = ParseRepeat(context);
                        break;
                    case TokenKind.KeywordFor:
                        s = ParseFor(context);
                        break;
                    case TokenKind.KeywordSwitch:
                        s = ParseSwitch(context);
                        break;
                    case TokenKind.KeywordCase:
                        s = ParseSwitchCase(context);
                        break;
                    case TokenKind.KeywordDefault:
                        s = ParseSwitchDefault(context);
                        break;
                    case TokenKind.KeywordIf:
                        s = ParseIf(context);
                        break;
                    case TokenKind.KeywordDo:
                        s = ParseDoUntil(context);
                        break;
                    case TokenKind.EOF:
                        ReportCodeError("Unexpected end of code.", false);
                        break;
                    case TokenKind.Enum:
                        s = ParseEnum(context);
                        break;
                    case TokenKind.EndStatement:
                        break;
                    case TokenKind.Increment:
                    case TokenKind.Decrement:
                        s = new Statement(Statement.StatementKind.Pre, remainingStageOne.Dequeue().Token);
                        s.Children.Add(ParsePostAndRef(context));
                        break;
                    case TokenKind.Error:
                        if (remainingStageOne.Count > 0)
                        {
                            ReportCodeError("Unexpected error token (invalid code).", remainingStageOne.Peek().Token, true);
                        }
                        break;
                    case TokenKind.KeywordFunction:
                        s = ParseFunction(context);
                        break;
                    default:
                        // Assumes it's a variable assignment
                        if (remainingStageOne.Count > 0)
                            s = ParseAssign(context);
                        break;
                }
                // Ignore any semicolons
                while (remainingStageOne.Count > 0 && remainingStageOne.Peek().Token?.Kind == TokenKind.EndStatement)
                    remainingStageOne.Dequeue();
                return s;
            }

            private static Statement ParseRepeat(CompileContext context)
            {
                Statement result = new Statement(Statement.StatementKind.RepeatLoop, EnsureTokenKind(TokenKind.KeywordRepeat).Token);
                result.Children.Add(ParseExpression(context));
                result.Children.Add(ParseStatement(context));
                return result;
            }

            private static Statement ParseFunction(CompileContext context)
            {
                Statement result = new Statement(Statement.StatementKind.FunctionDef, EnsureTokenKind(TokenKind.KeywordFunction).Token);
                Statement args = new Statement();
                bool expressionMode = true;
                Statement destination = null;

                if (GetNextTokenKind() == TokenKind.ProcFunction)
                {
                    expressionMode = false;
                    Statement s = remainingStageOne.Dequeue();
                    if (s.Text.StartsWith("___struct___")) {
                        ReportCodeError("Function names cannot start with ___struct___ (they are reserved for structs).", s.Token, false);
                    }
                    destination = new Statement(Statement.StatementKind.ExprFuncName, s.Token) { ID = s.ID };
                }

                EnsureTokenKind(TokenKind.OpenParen);

                while (remainingStageOne.Count > 0 && !hasError && !IsNextToken(TokenKind.EOF, TokenKind.CloseParen))
                {
                    Statement expr = ParseExpression(context);
                    if (expr != null)
                        args.Children.Add(expr);
                    if (!IsNextTokenDiscard(TokenKind.Comma))
                    {
                        if (!IsNextToken(TokenKind.CloseParen))
                        {
                            ReportCodeError("Expected ',' or ')' after argument in function call.", result.Token, true);
                            break;
                        }
                    }
                }
                result.Children.Add(args);

                if (EnsureTokenKind(TokenKind.CloseParen) == null) return null;

                // Semi-hack, this Statement is just used as a boolean
                // to see if a function is a constructor or not
                Statement isConstructor = new Statement();
                isConstructor.Text = "";
                if (IsNextTokenDiscard(TokenKind.KeywordConstructor)) {
                    if (context.Data.IsVersionAtLeast(2, 3)) {
                        isConstructor.Text = "constructor";
                    } else {
                        ReportCodeError("Cannot use constructors prior to GMS2.3.", result.Token, true);
                    }
                }
                result.Children.Add(isConstructor);

                result.Children.Add(ParseStatement(context));
                if (expressionMode)
                    return result;
                else // Whatever you call non-anonymous definitions
                {
                    Statement trueResult = new Statement(Statement.StatementKind.Assign, new Lexer.Token(TokenKind.Assign));
                    trueResult.Children.Add(destination);
                    trueResult.Children.Add(new Statement(Statement.StatementKind.Token, trueResult.Token));
                    trueResult.Children.Add(result);
                    return trueResult;
                }
            }

            private static Statement ParseFor(CompileContext context)
            {
                Statement result = new Statement(Statement.StatementKind.ForLoop, EnsureTokenKind(TokenKind.KeywordFor).Token);
                EnsureTokenKind(TokenKind.OpenParen);

                // Parse initialization statement
                if (IsNextToken(TokenKind.EndStatement))
                {
                    // Nonexistent
                    result.Children.Add(new Statement(Statement.StatementKind.Block, remainingStageOne.Dequeue().Token));
                }
                else
                {
                    result.Children.Add(ParseStatement(context));
                }

                // Parse expression/condition
                if (IsNextToken(TokenKind.EndStatement))
                {
                    // Nonexistent: always true
                    remainingStageOne.Dequeue();
                    result.Children.Add(new Statement(Statement.StatementKind.ExprConstant, new Lexer.Token(TokenKind.ProcConstant, "1"), new ExpressionConstant(1L)));
                }
                else
                {
                    result.Children.Add(ParseExpression(context));
                    IsNextTokenDiscard(TokenKind.EndStatement);
                }

                // Parse statement that calls each iteration
                if (IsNextToken(TokenKind.CloseParen))
                {
                    // Nonexistent
                    result.Children.Add(new Statement(Statement.StatementKind.Block, remainingStageOne.Dequeue().Token));
                }
                else
                {
                    result.Children.Add(ParseStatement(context));
                    EnsureTokenKind(TokenKind.CloseParen);
                }

                // Parse the body
                result.Children.Add(ParseStatement(context));

                return result;
            }

            private static Statement ParseAssign(CompileContext context)
            {
                Statement left = ParsePostAndRef(context);
                if (left != null)
                {
                    if (left.Kind != Statement.StatementKind.Pre && left.Kind != Statement.StatementKind.Post)
                    {
                        // hack because I don't know what I'm doing
                        string name;
                        if (left.Children.Count == 0 || left.Kind == Statement.StatementKind.ExprSingleVariable)
                            name = left.Text;
                        else
                            name = left.Children[left.Children.Count - 1]?.Text;
                        if (name == null)
                            return null;

                        VariableInfo vi;
                        if ((context.BuiltInList.GlobalNotArray.TryGetValue(name, out vi) ||
                            context.BuiltInList.GlobalArray.TryGetValue(name, out vi) ||
                            context.BuiltInList.Instance.TryGetValue(name, out vi) ||
                            context.BuiltInList.InstanceLimitedEvent.TryGetValue(name, out vi)
                            ) && !vi.CanSet)
                        {
                            ReportCodeError("Attempt to set a read-only variable.", left.Token, false);
                        }

                        if (remainingStageOne.Count == 0)
                        {
                            ReportCodeError("Malformed assignment statement.", true);
                            return null;
                        }
                        Statement assign = new Statement(Statement.StatementKind.Assign, remainingStageOne.Dequeue().Token);
                        assign.Children.Add(left);

                        if (assign.Token.Kind.In(
                            TokenKind.Assign,
                            TokenKind.AssignAnd,
                            TokenKind.AssignDivide,
                            TokenKind.AssignMinus,
                            TokenKind.AssignMod,
                            TokenKind.AssignOr,
                            TokenKind.AssignPlus,
                            TokenKind.AssignTimes,
                            TokenKind.AssignXor
                            ))
                        {
                            assign.Children.Add(new Statement(Statement.StatementKind.Token, assign.Token));
                            assign.Children.Add(ParseExpression(context));
                        }
                        else
                        {
                            ReportCodeError("Expected assignment operator.", assign.Token, true);
                        }

                        return assign;
                    }
                    else
                    {
                        return left;
                    }
                }
                else
                {
                    ReportCodeError("Malformed assignment statement.", true);
                    return null;
                }
            }

            private static Statement ParseEnum(CompileContext context)
            {
                ReportCodeError("Enums not currently supported.", true);
                return null;
                /*
                Statement result = new Statement(Statement.StatementKind.Enum, EnsureTokenKind(TokenKind.Enum).Token);
                Dictionary<string, int> values = new Dictionary<string, int>();

                Statement name = EnsureTokenKind(TokenKind.ProcVariable);
                if (name == null)
                    return null;
                if (name.ID < 100000)
                    ReportCodeError("Enum name redeclares a builtin variable.", name.Token, false);
                result.Text = name.Text;
                result.ID = name.ID;

                if (EnsureTokenKind(TokenKind.OpenBlock) == null) return null;

                if (Enums.ContainsKey(name.Text))
                {
                    ReportCodeError("Enum \"" + name.Text + "\" is defined more than once.", name.Token, true);
                } else
                {
                    Enums[name.Text] = values;
                }

                int incrementingValue = 0;
                while (!hasError && !IsNextToken(TokenKind.CloseBlock))
                {
                    Statement val = new Statement(Statement.StatementKind.VariableName, remainingStageOne.Dequeue().Token);
                    result.Children.Add(val);
                    
                    if (IsNextTokenDiscard(TokenKind.Assign))
                    {
                        Statement expr = ParseExpression();
                        val.Children.Add(expr);
                        Statement optimized = Optimize(expr);
                        if (expr.Token.Kind == TokenKind.Constant && (expr.Kind != Statement.StatementKind.ExprConstant ||
                             expr.Constant.kind == ExpressionConstant.Kind.Constant || expr.Constant.kind == ExpressionConstant.Kind.Number))
                        {
                            incrementingValue = (int)optimized.Constant.valueNumber;
                        } else
                        {
                            ReportCodeError("Enum value must be an integer constant value.", expr.Token, true);
                        }
                    }

                    if (values.ContainsKey(val.Text))
                    {
                        ReportCodeError("Duplicate enum value found.", val.Token, true);
                    }

                    values[val.Text] = incrementingValue++;

                    if (!IsNextTokenDiscard(TokenKind.Comma))
                    {
                        EnsureTokenKind(TokenKind.CloseBlock);
                        break;
                    }
                }

                return result;*/
            }

            private static Statement ParseDoUntil(CompileContext context)
            {
                Statement result = new Statement(Statement.StatementKind.DoUntilLoop, EnsureTokenKind(TokenKind.KeywordDo).Token);
                result.Children.Add(ParseStatement(context));
                EnsureTokenKind(TokenKind.KeywordUntil);
                result.Children.Add(ParseExpression(context));
                return result;
            }

            private static Statement ParseIf(CompileContext context)
            {
                Statement result = new Statement(Statement.StatementKind.If, EnsureTokenKind(TokenKind.KeywordIf).Token);
                result.Children.Add(ParseExpression(context));
                IsNextTokenDiscard(TokenKind.KeywordThen);
                result.Children.Add(ParseStatement(context));
                if (IsNextTokenDiscard(TokenKind.KeywordElse))
                    result.Children.Add(ParseStatement(context));
                return result;
            }

            private static Statement ParseSwitchDefault(CompileContext context)
            {
                Statement result = new Statement(Statement.StatementKind.SwitchDefault, EnsureTokenKind(TokenKind.KeywordDefault).Token);
                EnsureTokenKind(TokenKind.Colon);
                return result;
            }

            private static Statement ParseSwitchCase(CompileContext context)
            {
                Statement result = new Statement(Statement.StatementKind.SwitchCase, EnsureTokenKind(TokenKind.KeywordCase).Token);
                result.Children.Add(ParseExpression(context));
                EnsureTokenKind(TokenKind.Colon);
                return result;
            }

            private static Statement ParseSwitch(CompileContext context)
            {
                Statement result = new Statement(Statement.StatementKind.Switch, EnsureTokenKind(TokenKind.KeywordSwitch).Token);
                result.Children.Add(ParseExpression(context));
                EnsureTokenKind(TokenKind.OpenBlock);

                while (!hasError && remainingStageOne.Count > 0 && !IsNextToken(TokenKind.CloseBlock, TokenKind.EOF))
                {
                    // Apparently the compiler allows any statement here, no validation until later
                    Statement c = ParseStatement(context);
                    if (c != null)
                        result.Children.Add(c);
                }

                EnsureTokenKind(TokenKind.CloseBlock);

                return result;
            }

            private static Statement ParseWhile(CompileContext context)
            {
                Statement result = new Statement(Statement.StatementKind.WhileLoop, EnsureTokenKind(TokenKind.KeywordWhile).Token);
                result.Children.Add(ParseExpression(context));
                IsNextTokenDiscard(TokenKind.KeywordDo);
                result.Children.Add(ParseStatement(context));
                return result;
            }

            private static Statement ParseWith(CompileContext context)
            {
                Statement result = new Statement(Statement.StatementKind.With, EnsureTokenKind(TokenKind.KeywordWith).Token);
                result.Children.Add(ParseExpression(context));
                IsNextTokenDiscard(TokenKind.KeywordDo);
                result.Children.Add(ParseStatement(context));
                return result;
            }

            private static Statement ParseReturn(CompileContext context)
            {
                Statement result = new Statement(Statement.StatementKind.Return, EnsureTokenKind(TokenKind.KeywordReturn).Token);
                if (remainingStageOne.Count > 0 && !IsKeyword(GetNextTokenKind()) && !IsNextToken(TokenKind.EndStatement, TokenKind.EOF))
                {
                    result.Children.Add(ParseExpression(context));
                }
                return result;
            }

            private static Statement ParseLocalVarDeclare(CompileContext context)
            {
                Statement result = new Statement(Statement.StatementKind.TempVarDeclare, EnsureTokenKind(TokenKind.KeywordVar).Token);
                while (remainingStageOne.Count > 0 && IsNextToken(TokenKind.ProcVariable))
                {
                    Statement var = remainingStageOne.Dequeue();

                    // Error checking on variable
                    if (var.ID < 100000)
                        ReportCodeError("Redeclaration of builtin variable.", var.Token, false);
                    if (context.BuiltInList.Functions.ContainsKey(var.Text) || context.scripts.Contains(var.Text))
                        ReportCodeError(string.Format("Variable name {0} cannot be used; a function or script already has the name.", var.Text), var.Token, false);
                    if (context.assetIds.ContainsKey(var.Text))
                        ReportCodeError(string.Format("Variable name {0} cannot be used; a resource already has the name.", var.Text), var.Token, false);

                    Statement variable = new Statement(var) { Kind = Statement.StatementKind.ExprSingleVariable };
                    result.Children.Add(variable);
                    context.LocalVars[var.Text] = var.Text;

                    // Read assignments if necessary
                    if (remainingStageOne.Count > 0 && IsNextToken(TokenKind.Assign))
                    {
                        Statement a = new Statement(Statement.StatementKind.Assign, remainingStageOne.Dequeue().Token);
                        variable.Children.Add(a);

                        Statement left = new Statement(var) { Kind = Statement.StatementKind.ExprSingleVariable };
                        left.ID = var.ID;

                        a.Children.Add(left);
                        a.Children.Add(new Statement(TokenKind.Assign, a.Token));
                        a.Children.Add(ParseExpression(context));
                    }

                    if (!IsNextTokenDiscard(TokenKind.Comma))
                        break;
                }
                if (result.Children.Count == 0)
                    ReportCodeError("Expected local variable declaration, got nothing.", result.Token, false);

                return result;
            }

            private static Statement ParseGlobalVarDeclare(CompileContext context)
            {
                ReportCodeError("Global variable declaration currently unsupported", remainingStageOne.Dequeue().Token, true);

                return null;
            }

            private static Statement ParseFunctionCall(CompileContext context, bool expression = false)
            {
                bool isNew = false;
                if (IsNextToken(TokenKind.KeywordNew)) {
                    var nextStatement = remainingStageOne.Dequeue();
                    if (CompileContext.GMS2_3) {
                        isNew = true;
                    } else {
                        ReportCodeError("Cannot use constructors prior to GMS2.3.", nextStatement.Token, true);
                    }
                }

                Statement s = EnsureTokenKind(TokenKind.ProcFunction);

                // gml_pragma processing can be done here, however we don't need to do that yet really

                EnsureTokenKind(TokenKind.OpenParen); // this should be guaranteed

                Statement result = new Statement(expression ? Statement.StatementKind.ExprFunctionCall :
                                                 Statement.StatementKind.FunctionCall, s.Token);
                
                Statement newStatement = new Statement();
                newStatement.Text = isNew ? "new" : "";
                result.Children.Add(newStatement);

                // Parse the parameters/arguments
                while (remainingStageOne.Count > 0 && !hasError && !IsNextToken(TokenKind.EOF) && !IsNextToken(TokenKind.CloseParen))
                {
                    Statement expr = ParseExpression(context);
                    if (expr != null)
                        result.Children.Add(expr);
                    if (!IsNextTokenDiscard(TokenKind.Comma))
                    {
                        if (!IsNextToken(TokenKind.CloseParen))
                        {
                            ReportCodeError("Expected ',' or ')' after argument in function call.", s.Token, true);
                            break;
                        }
                    }
                }

                if (EnsureTokenKind(TokenKind.CloseParen) == null) return null;

                // Check for proper argument count, at least for builtins
                if (context.BuiltInList.Functions.TryGetValue(s.Text, out FunctionInfo fi) && 
                    fi.ArgumentCount != -1 && (result.Children.Count - 1) != fi.ArgumentCount)
                    ReportCodeError(string.Format("Function {0} expects {1} arguments, got {2}.",
                                                  s.Text, fi.ArgumentCount, (result.Children.Count - 1))
                                                  , s.Token, false);

                return result;
            }

            private static Statement ParseExpression(CompileContext context)
            {
                return ParseConditionalOp(context);
            }

            private static Statement ParseConditionalOp(CompileContext context)
            {
                Statement left = ParseOrOp(context);
                if (!hasError && IsNextToken(TokenKind.Conditional))
                {
                    if (context.Data?.GeneralInfo.Major < 2)
                    {
                        ReportCodeError("Attempt to use conditional operator in GameMaker version earlier than 2.", remainingStageOne.Dequeue().Token, true);
                        return left;
                    }

                    Statement result = new Statement(Statement.StatementKind.ExprConditional,
                                                    EnsureTokenKind(TokenKind.Conditional).Token);

                    Statement expr1 = ParseOrOp(context);

                    if (EnsureTokenKind(TokenKind.Colon) != null)
                    {
                        Statement expr2 = ParseExpression(context);

                        result.Children.Add(left);
                        result.Children.Add(expr1);
                        result.Children.Add(expr2);
                        return result;
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return left;
                }
            }

            private static Statement ParseOrOp(CompileContext context)
            {
                Statement left = ParseAndOp(context);
                if (!hasError && IsNextToken(TokenKind.LogicalOr))
                {
                    Statement result = new Statement(Statement.StatementKind.ExprBinaryOp,
                                                     EnsureTokenKind(TokenKind.LogicalOr).Token);
                    result.Children.Add(left);
                    result.Children.Add(ParseAndOp(context));
                    while (remainingStageOne.Count > 0 && IsNextTokenDiscard(TokenKind.LogicalOr))
                    {
                        result.Children.Add(ParseAndOp(context));
                    }
                    
                    return result;
                }
                else
                {
                    return left;
                }
            }

            private static Statement ParseAndOp(CompileContext context)
            {
                Statement left = ParseXorOp(context);
                if (!hasError && IsNextToken(TokenKind.LogicalAnd))
                {
                    Statement result = new Statement(Statement.StatementKind.ExprBinaryOp,
                                                     EnsureTokenKind(TokenKind.LogicalAnd).Token);
                    result.Children.Add(left);
                    result.Children.Add(ParseXorOp(context));
                    while (remainingStageOne.Count > 0 && IsNextTokenDiscard(TokenKind.LogicalAnd))
                    {
                        result.Children.Add(ParseXorOp(context));
                    }

                    return result;
                }
                else
                {
                    return left;
                }
            }

            private static Statement ParseXorOp(CompileContext context)
            {
                Statement left = ParseCompare(context);
                if (!hasError && IsNextToken(TokenKind.LogicalXor))
                {
                    Statement result = new Statement(Statement.StatementKind.ExprBinaryOp,
                                                     EnsureTokenKind(TokenKind.LogicalXor).Token);
                    Statement right = ParseCompare(context);

                    result.Children.Add(left);
                    result.Children.Add(right);
                    return result;
                }
                else
                {
                    return left;
                }
            }

            private static Statement ParseCompare(CompileContext context)
            {
                Statement left = ParseBitwise(context);
                if (!hasError && IsNextToken(
                    TokenKind.CompareEqual,
                    TokenKind.Assign, // Legacy
                    TokenKind.CompareGreater,
                    TokenKind.CompareGreaterEqual,
                    TokenKind.CompareLess,
                    TokenKind.CompareLessEqual,
                    TokenKind.CompareNotEqual
                    ))
                {
                    Lexer.Token t = remainingStageOne.Dequeue().Token;
                    // Repair legacy comparison
                    if (t.Kind == TokenKind.Assign)
                        t.Kind = TokenKind.CompareEqual;

                    Statement result = new Statement(Statement.StatementKind.ExprBinaryOp, t);

                    Statement right = ParseBitwise(context);

                    result.Children.Add(left);
                    result.Children.Add(right);
                    return result;
                }
                else
                {
                    return left;
                }
            }

            private static Statement ParseBitwise(CompileContext context)
            {
                Statement left = ParseBitShift(context);
                if (!hasError && IsNextToken(
                    TokenKind.BitwiseOr,
                    TokenKind.BitwiseAnd,
                    TokenKind.BitwiseXor
                    ))
                {
                    Statement result = new Statement(Statement.StatementKind.ExprBinaryOp, remainingStageOne.Dequeue().Token);

                    Statement right = ParseBitShift(context);

                    result.Children.Add(left);
                    result.Children.Add(right);

                    while (IsNextToken(
                            TokenKind.BitwiseOr,
                            TokenKind.BitwiseAnd,
                            TokenKind.BitwiseXor
                    ))
                    {
                        Statement nextRight = new Statement(Statement.StatementKind.ExprBinaryOp, remainingStageOne.Dequeue().Token);
                        nextRight.Children.Add(result);
                        nextRight.Children.Add(ParseBitShift(context));
                        result = nextRight;
                    }

                    return result;
                }
                else
                {
                    return left;
                }
            }

            private static Statement ParseBitShift(CompileContext context)
            {
                Statement left = ParseAddSub(context);
                if (!hasError && IsNextToken(
                    TokenKind.BitwiseShiftLeft,
                    TokenKind.BitwiseShiftRight
                    ))
                {
                    Statement result = new Statement(Statement.StatementKind.ExprBinaryOp, remainingStageOne.Dequeue().Token);

                    Statement right = ParseAddSub(context);

                    result.Children.Add(left);
                    result.Children.Add(right);

                    while (IsNextToken(
                            TokenKind.BitwiseShiftLeft,
                            TokenKind.BitwiseShiftRight
                            ))
                    {
                        Statement nextRight = new Statement(Statement.StatementKind.ExprBinaryOp, remainingStageOne.Dequeue().Token);
                        nextRight.Children.Add(result);
                        nextRight.Children.Add(ParseAddSub(context));
                        result = nextRight;
                    }

                    return result;
                }
                else
                {
                    return left;
                }
            }

            private static Statement ParseAddSub(CompileContext context)
            {
                Statement left = ParseMulDiv(context);
                if (!hasError && IsNextToken(
                    TokenKind.Plus,
                    TokenKind.Minus
                    ))
                {
                    Statement result = new Statement(Statement.StatementKind.ExprBinaryOp, remainingStageOne.Dequeue().Token);

                    Statement right = ParseMulDiv(context);

                    result.Children.Add(left);
                    result.Children.Add(right);

                    while (IsNextToken(
                            TokenKind.Plus,
                            TokenKind.Minus
                            ))
                    {
                        Statement nextRight = new Statement(Statement.StatementKind.ExprBinaryOp, remainingStageOne.Dequeue().Token);
                        nextRight.Children.Add(result);
                        nextRight.Children.Add(ParseMulDiv(context));
                        result = nextRight;
                    }

                    return result;
                }
                else
                {
                    return left;
                }
            }

            private static Statement ParseMulDiv(CompileContext context)
            {
                Statement left = ParsePostAndRef(context);
                if (!hasError && IsNextToken(
                    TokenKind.Times,
                    TokenKind.Divide,
                    TokenKind.Div,
                    TokenKind.Mod
                    ))
                {
                    Statement result = new Statement(Statement.StatementKind.ExprBinaryOp, remainingStageOne.Dequeue().Token);

                    Statement right = ParsePostAndRef(context);

                    result.Children.Add(left);
                    result.Children.Add(right);

                    while (IsNextToken(
                        TokenKind.Times,
                        TokenKind.Divide,
                        TokenKind.Div,
                        TokenKind.Mod
                            ))
                    {
                        Statement nextRight = new Statement(Statement.StatementKind.ExprBinaryOp, remainingStageOne.Dequeue().Token);
                        nextRight.Children.Add(result);
                        nextRight.Children.Add(ParsePostAndRef(context));
                        result = nextRight;
                    }

                    return result;
                }
                else
                {
                    return left;
                }
            }

            private static Statement ParsePostAndRef(CompileContext context)
            {
                Statement left = ParseLowLevel(context);
                if (!hasError && IsNextToken(TokenKind.Dot))
                {
                    // Parse chain variable reference
                    Statement result = new Statement(Statement.StatementKind.ExprVariableRef, remainingStageOne.Peek().Token);
                    bool combine = false;
                    if (left.Kind != Statement.StatementKind.ExprConstant || left.Constant.kind == ExpressionConstant.Kind.Reference /* TODO: will this ever change? */)
                        result.Children.Add(left);
                    else
                        combine = true;
                    while (remainingStageOne.Count > 0 && IsNextTokenDiscard(TokenKind.Dot))
                    {
                        Statement next = ParseSingleVar(context);
                        if (combine)
                        {
                            if (left.Constant.kind != ExpressionConstant.Kind.Number)
                                ReportCodeError("Expected constant to be number in variable reference.", left.Token, false);
                            if (next != null)
                                next.ID = (int)left.Constant.valueNumber;
                            combine = false;
                        }
                        result.Children.Add(next);
                    }

                    // Post increment/decrement check
                    if (remainingStageOne.Count > 0 && IsNextToken(TokenKind.Increment, TokenKind.Decrement))
                    {
                        Statement newResult = new Statement(Statement.StatementKind.Post, remainingStageOne.Dequeue().Token);
                        newResult.Children.Add(result);
                        return newResult;
                    }
                    else
                    {
                        return result;
                    }
                }
                else
                {
                    return left;
                }
            }

            private static Statement ParseSingleVar(CompileContext context)
            {
                Statement s = EnsureTokenKind(TokenKind.ProcVariable);
                if (s == null)
                    return null;

                // Check to make sure we aren't overriding a script/function name
                if (context.BuiltInList.Functions.ContainsKey(s.Text) || context.scripts.Contains(s.Text))
                {
                    //ReportCodeError(string.Format("Variable name {0} cannot be used; a function or script already has the name.", s.Text), false);
                    return new Statement(Statement.StatementKind.ExprFuncName, s.Token) { ID = s.ID };       
                }

                Statement result = new Statement(Statement.StatementKind.ExprSingleVariable, s.Token);
                result.ID = s.ID;
                // Check for array
                bool array = false; // hack because you can't else if a while
                while (remainingStageOne.Count > 0 && IsNextToken(
                    TokenKind.OpenArray,
                    TokenKind.OpenArrayBaseArray,
                    TokenKind.OpenArrayGrid,
                    TokenKind.OpenArrayList,
                    TokenKind.OpenArrayMap))
                {
                    array = true;
                    Lexer.Token tok = remainingStageOne.Dequeue().Token;
                    TokenKind t = tok.Kind;

                    // Add accessor info
                    if (t != TokenKind.OpenArray)
                        result.Children.Add(new Statement(t, tok));

                    // Index
                    Statement index = ParseExpression(context);
                    result.Children.Add(index);
                    if (!hasError && t != TokenKind.OpenArrayMap)
                    {
                        // Make sure the map accessor is the only one that uses strings
                        CheckNormalArrayIndex(context, index);
                    }

                    // Second index (2D array)
                    if (IsNextTokenDiscard(TokenKind.Comma))
                    {
                        Statement index2d = ParseExpression(context);
                        result.Children.Add(index2d);
                        if (!hasError && t != TokenKind.OpenArrayMap)
                        {
                            // Make sure the map accessor is the only one that uses strings
                            CheckNormalArrayIndex(context, index2d);
                        }
                    }

                    // TODO: Remove this once support is added
                    if (t != TokenKind.OpenArray)
                        ReportCodeError("Accessors are currently unsupported in this compiler- use the DS functions themselves instead (internally they're the same).", false);

                    if (EnsureTokenKind(TokenKind.CloseArray) == null) return null;
                }
                if (!array && (context.BuiltInList.GlobalArray.TryGetValue(result.Text, out _) || context.BuiltInList.InstanceLimitedEvent.TryGetValue(result.Text, out _)))
                {
                    // The compiler apparently does this
                    // I think this makes some undefined value for whatever reason
                    Statement something = new Statement(Statement.StatementKind.ExprConstant, "0");
                    something.Constant = new ExpressionConstant(0L);
                    result.Children.Add(something);
                }

                return result;
            }

            private static void CheckNormalArrayIndex(CompileContext context, Statement index)
            {
                Statement optimized = Optimize(context, index);
                if (optimized.Kind == Statement.StatementKind.ExprConstant
                    && optimized.Constant?.kind == ExpressionConstant.Kind.String)
                    ReportCodeError("Strings cannot be used for array indices, unless in a map accessor.", index.Token, false);
            }

            private static Statement ParseLowLevel(CompileContext context)
            {
                switch (GetNextTokenKind())
                {
                    case TokenKind.OpenArray:
                        if (context.Data?.GeneralInfo?.Major >= 2)
                            return ParseArrayLiteral(context);
                        ReportCodeError("Cannot use array literal prior to GMS2 version.", remainingStageOne.Dequeue().Token, true);
                        return null;
                    case TokenKind.OpenParen:
                        {
                            remainingStageOne.Dequeue();
                            Statement expr = ParseExpression(context);
                            EnsureTokenKind(TokenKind.CloseParen);
                            return expr;
                        }
                    case TokenKind.ProcConstant:
                        {
                            Statement next = remainingStageOne.Dequeue();
                            return new Statement(Statement.StatementKind.ExprConstant, next.Token, next.Constant);
                        }
                    case TokenKind.ProcFunction:
                    case TokenKind.KeywordNew:
                        return ParseFunctionCall(context, true);
                    case TokenKind.KeywordFunction:
                        return ParseFunction(context);
                    case TokenKind.ProcVariable:
                        {
                            Statement variableRef = ParseSingleVar(context);
                            if (!IsNextToken(TokenKind.Increment, TokenKind.Decrement))
                            {
                                return variableRef;
                            }
                            else
                            {
                                Statement final = new Statement(Statement.StatementKind.Post, remainingStageOne.Dequeue().Token);
                                final.Children.Add(variableRef);
                                return final;
                            }
                        }
                    case TokenKind.OpenBlock:
                        // todo? maybe?
                        if (context.Data.IsVersionAtLeast(2, 3))
                            return ParseStructLiteral(context);
                        ReportCodeError("Cannot use struct literal prior to GMS2.3.", remainingStageOne.Dequeue().Token, true);
                        break;
                    case TokenKind.Increment:
                    case TokenKind.Decrement:
                        {
                            Statement pre = new Statement(Statement.StatementKind.Pre, remainingStageOne.Dequeue().Token);
                            pre.Children.Add(ParsePostAndRef(context));
                            if (pre.Children[0].Kind == Statement.StatementKind.Post)
                                ReportCodeError("Unexpected pre/post combination.", pre.Token, true);
                            return pre;
                        }
                    case TokenKind.Not:
                    case TokenKind.Plus:
                    case TokenKind.Minus:
                    case TokenKind.BitwiseNegate:
                        {
                            Statement unary = new Statement(Statement.StatementKind.ExprUnary, remainingStageOne.Dequeue().Token);
                            unary.Children.Add(ParsePostAndRef(context));
                            return unary;
                        }
                }
                if (remainingStageOne.Count > 0)
                    ReportCodeError("Unexpected token in expression.", remainingStageOne.Dequeue().Token, true);
                else
                    ReportCodeError("Unexpected end of code.", false);
                return null;
            }

            // Example: [1, 2, 3, 4]
            private static Statement ParseArrayLiteral(CompileContext context)
            {
                Statement result = new Statement(Statement.StatementKind.ExprFunctionCall,
                                                EnsureTokenKind(TokenKind.OpenArray)?.Token);

                accessorFunc.Children.Add(new Statement() { Text = "" }); // `new`

                // It literally converts into a function call
                result.Text = "@@NewGMLArray@@";

                while (!hasError && remainingStageOne.Count > 0 && !IsNextToken(TokenKind.CloseArray, TokenKind.EOF))
                {
                    result.Children.Add(ParseExpression(context));
                    if (!IsNextTokenDiscard(TokenKind.Comma))
                    {
                        if (!IsNextToken(TokenKind.CloseArray))
                        {
                            if (remainingStageOne.Any())
                                ReportCodeError("Expected ',' or ']' after value in inline array.", remainingStageOne.Peek().Token, true);
                            else
                                ReportCodeError("Malformed array literal.", false);
                            break;
                        }
                    }
                }

                if (EnsureTokenKind(TokenKind.CloseArray) == null) return null;

                return result;
            }
            
            // Example: {key: 123, key2: "asd"}
            private static Statement ParseStructLiteral(CompileContext context)
            {
                Statement result = new Statement(Statement.StatementKind.ExprStruct,
                                                EnsureTokenKind(TokenKind.OpenBlock)?.Token);

                Statement nextStatement = remainingStageOne.Peek();
                Statement procVar;

                // Non-constants are passed to the function through arguments
                // I call this leaking the variable
                // (because the values "leak" out of the struct function in the assembly)
                Statement leakedVars = new Statement();
                result.Children.Add(leakedVars);

                string varName;
                // Check if we can reuse any struct functions
                if (usableStructNames.Count > 0) {
                    varName = usableStructNames.Dequeue();
                } else {
                    // Create a new function
                    int i = context.Data.Code.Count;
                    do {
                        varName = "___struct___utmt_" + context.OriginalCode.Name.Content +
                            "__" + uuidCounter++.ToString();
                        i++;
                    } while (context.Data.KnownSubFunctions.ContainsKey(varName));
                }

                int ID = GetVariableID(context, varName, out _);
                if (ID >= 0 && ID < 100000)
                    procVar = new Statement(TokenKind.ProcVariable, nextStatement.Token, -1); // becomes self anyway?
                else
                    procVar = new Statement(TokenKind.ProcVariable, nextStatement.Token, ID);

                Statement function = new Statement(Statement.StatementKind.FunctionDef, procVar.Token);
                function.Text = varName;
                Statement args = new Statement();
                Statement destination = new Statement(Statement.StatementKind.ExprFuncName, result.Token)
                    { ID = procVar.ID, Text = varName };
                Statement body = new Statement();

                // If we don't set IsConstructor, the game will crash
                // when creating the struct
                Statement isConstructor = new Statement();
                isConstructor.Text = "constructor";

                function.Children.Add(args);
                function.Children.Add(isConstructor);
                function.Children.Add(body);

                Statement functionAssign = new Statement(Statement.StatementKind.Assign, new Lexer.Token(TokenKind.Assign));
                functionAssign.Children.Add(destination);
                functionAssign.Children.Add(new Statement(Statement.StatementKind.Token, functionAssign.Token));
                functionAssign.Children.Add(function);

                result.Children.Add(functionAssign);

                // this is a total mess
                int argumentsID = context.GetAssetIndexByName("argument");
                Lexer.Token varToken = new Lexer.Token(TokenKind.ProcVariable);
                varToken.Content = "argument";
                Statement argumentsVar = new Statement(TokenKind.ProcVariable, varToken);
                argumentsVar.ID = argumentsID;

                while (!hasError && remainingStageOne.Count > 0 && !IsNextToken(TokenKind.CloseBlock, TokenKind.EOF))
                {
                    if (!IsNextToken(TokenKind.ProcVariable))
                    {
                        if (remainingStageOne.Any())
                            ReportCodeError("Expected variable name in inline struct.", remainingStageOne.Peek().Token, true);
                        else
                            ReportCodeError("Malformed struct literal.", false);
                        break;
                    }
                    Statement variable = remainingStageOne.Dequeue();
                    if (context.BuiltInList.Functions.ContainsKey(variable.Text) || context.scripts.Contains(variable.Text))
                        ReportCodeError(string.Format("Struct variable name {0} cannot be used; a function or script already has the name.", variable.Text), variable.Token, false);
                    if (context.assetIds.ContainsKey(variable.Text))
                        ReportCodeError(string.Format("Struct variable name {0} cannot be used; a resource already has the name.", variable.Text), variable.Token, false);
                    if (!IsNextTokenDiscard(TokenKind.Colon))
                    {
                        if (remainingStageOne.Any())
                            ReportCodeError("Expected ':' after key in inline struct.", remainingStageOne.Peek().Token, true);
                        else
                            ReportCodeError("Malformed struct literal.", false);
                        break;
                    }

                    Statement a = new Statement(Statement.StatementKind.Assign, variable.Token);
                    body.Children.Add(a);

                    Statement left = new Statement(variable) { Kind = Statement.StatementKind.ExprSingleVariable };
                    left.ID = variable.ID;

                    a.Children.Add(left);
                    a.Children.Add(new Statement(TokenKind.Assign, a.Token));

                    Statement expr = Optimize(context, ParseExpression(context));
                    if (expr.Kind == Statement.StatementKind.ExprConstant) {
                        // Constants can be inlined
                        a.Children.Add(expr);
                    } else {
                        Statement argumentsAccess =
                            new Statement(argumentsVar) { Kind = Statement.StatementKind.ExprSingleVariable };
                        argumentsAccess.ID = argumentsVar.ID;
                        Statement index = new Statement(Statement.StatementKind.ExprConstant, expr.Token);
                        index.Constant = new ExpressionConstant((double)leakedVars.Children.Count);
                        argumentsAccess.Children.Add(index);

                        leakedVars.Children.Add(expr);
                        a.Children.Add(argumentsAccess);
                    }

                    if (!IsNextTokenDiscard(TokenKind.Comma))
                    {
                        if (!IsNextToken(TokenKind.CloseBlock))
                        {
                            if (remainingStageOne.Any())
                                ReportCodeError("Expected ',' or '}' after value in inline struct.", remainingStageOne.Peek().Token, true);
                            else
                                ReportCodeError("Malformed struct literal.", false);
                            break;
                        }
                    }
                }

                if (EnsureTokenKind(TokenKind.CloseBlock) == null) return null;

                return result;
            }

            public static Statement Optimize(CompileContext context, Statement s)
            {
                // Process children (if we can)
                Statement result;
                if (s.Kind != Statement.StatementKind.ExprVariableRef && s.Kind != Statement.StatementKind.Assign)
                {
                    if (s.Children.Count == 0 || (
                        (s.Kind == Statement.StatementKind.ExprFunctionCall || s.Kind == Statement.StatementKind.FunctionCall)
                        && s.Children.Count == 1
                    ))
                    {
                        // There's nothing to optimize here, don't waste time checking
                        return s;
                    }
                    else
                    {
                        result = new Statement(s);
                        for (int i = 0; i < result.Children.Count; i++)
                        {
                            if (result.Children[i] == null)
                                result.Children[i] = new Statement(Statement.StatementKind.Discard);
                            else
                                result.Children[i] = Optimize(context, result.Children[i]);
                        }
                    }
                } else
                    result = new Statement(s);
                Statement child0 = result.Children[0];

                switch (s.Kind)
                {
                    case Statement.StatementKind.Assign:
                        Statement left = child0;
                        bool isVarRef = (left.Kind == Statement.StatementKind.ExprVariableRef);
                        if (isVarRef || (left.Kind == Statement.StatementKind.ExprSingleVariable && left.Children.Count >= 2 && left.Children[0].Kind == Statement.StatementKind.Token))
                        {
                            if (!isVarRef)
                            {
                                // Become a var ref!
                                Statement varRef = new Statement(Statement.StatementKind.ExprVariableRef);
                                varRef.Children.Add(left);
                                left = varRef;
                            }

                            // Check for accessor stuff
                            for (int i = 0; i < left.Children.Count; i++)
                            {
                                if (left.Children[i].Children.Count != 2 || left.Children[i].Children[0].Kind != Statement.StatementKind.Token)
                                    left.Children[i] = Optimize(context, left.Children[i]);
                                else
                                {
                                    // Change accessors to proper functions, embedding inside each other if needed
                                    Statement curr = left.Children[i];
                                    AccessorInfo ai = GetAccessorInfoFromStatement(context, curr);
                                    if (ai != null)
                                    {
                                        if ((i + 1) >= left.Children.Count)
                                        {
                                            // Final set function
                                            Statement accessorFunc = new Statement(Statement.StatementKind.FunctionCall, ai.LFunc);
                                            accessorFunc.Children.Add(new Statement() { Text = "" }); // `new`
                                            accessorFunc.Children.Add(Optimize(context, curr.Children[1]));
                                            if (curr.Children.Count == 3)
                                                accessorFunc.Children.Add(Optimize(context, curr.Children[2]));
                                            curr.Children.Clear();
                                            if (left.Children.Count == 1)
                                                left = left.Children[0];
                                            accessorFunc.Children.Insert(0, left);
                                            accessorFunc.Children.Add(Optimize(context, result.Children[2]));
                                            return accessorFunc;
                                        } else
                                        {
                                            // Not the final set function
                                            Statement accessorFunc = new Statement(Statement.StatementKind.ExprFunctionCall, ai.RFunc);
                                            accessorFunc.Children.Add(new Statement() { Text = "" }); // `new`
                                            accessorFunc.Children.Add(Optimize(context, curr.Children[1]));
                                            if (curr.Children.Count == 3)
                                                accessorFunc.Children.Add(Optimize(context, curr.Children[2]));
                                            curr.Children.Clear();
                                            Statement newVarChain = new Statement(Statement.StatementKind.ExprVariableRef);
                                            newVarChain.Children.AddRange(left.Children.GetRange(0, i + 1));
                                            if (newVarChain.Children.Count == 1)
                                                newVarChain = newVarChain.Children[0];
                                            accessorFunc.Children.Insert(0, newVarChain);
                                            left.Children.RemoveRange(0, i + 1);
                                            left.Children.Insert(0, accessorFunc);
                                            i = 0; // runs i = 1 next iteration
                                        }
                                    }
                                }
                            }

                            result.Children[0] = left;

                            for (int i = 0; i < result.Children.Count; i++)
                            {
                                result.Children[i] = Optimize(context, result.Children[i]);
                            }
                        } else
                        {
                            for (int i = 0; i < result.Children.Count; i++)
                            {
                                result.Children[i] = Optimize(context, result.Children[i]);
                            }

                            // (Don't use "left" here because it's not optimized)
                            if (result.Children.Count == 3 && result.Children[1].Token?.Kind == TokenKind.Assign &&
                              result.Children[0].Children.Count == 0 && result.Children[0].Kind == Statement.StatementKind.ExprSingleVariable &&
                              result.Children[2].Children.Count == 0 && result.Children[2].Kind == Statement.StatementKind.ExprSingleVariable &&
                              result.Children[0].Text == result.Children[2].Text && result.Children[0].ID == result.Children[2].ID)
                            {
                                // Remove redundant assignments, like "a = a", except for bytecode 14 and below.
                                if (context.Data?.GeneralInfo.BytecodeVersion > 14)
                                {
                                    result = new Statement(Statement.StatementKind.Discard);
                                }
                            }
                        }
                        break;
                    case Statement.StatementKind.Pre:
                    case Statement.StatementKind.Post:
                        // todo: convert accessors for this and somewhere else
                        break;
                    case Statement.StatementKind.If:
                        // Optimize if statements like "if(false)" or "if(0)"
                        if (result.Children.Count >= 2 && child0.Kind == Statement.StatementKind.ExprConstant)
                        {
                            if (child0.Constant.kind == ExpressionConstant.Kind.Number &&
                                child0.Constant.valueNumber <= 0.5d)
                            {
                                if (result.Children.Count == 3)
                                {
                                    // Replace the if statement with the else clause
                                    result = result.Children[2];
                                }
                                else
                                {
                                    // Remove the if statement altogether
                                    result = new Statement(Statement.StatementKind.Discard);
                                }
                            }
                            // Optimize if statements like "if(true)" or "if(1)"
                            else if (child0.Constant.kind == ExpressionConstant.Kind.Number &&
                                     child0.Constant.valueNumber > 0.5d)
                            {
                                result = result.Children[1];
                            }
                        }
                        break;
                    case Statement.StatementKind.FunctionCall:
                    case Statement.StatementKind.ExprFunctionCall:
                        // Optimize a few basic functions if possible

                        var child1 = result.Children[1];

                        // Rule out any non-constant parameters
                        if (child1.Kind != Statement.StatementKind.ExprConstant)
                            return result;
                        for (int i = 2; i < result.Children.Count; i++)
                        {
                            if (result.Children[i].Kind != Statement.StatementKind.ExprConstant)
                                return result;
                        }

                        switch (result.Text)
                        {
                            case "string":
                                {
                                    // Ignore the optimization for GMS build versions less than 1763 and not equal to 1539.
                                    if ((context.Data?.GeneralInfo.Build >= 1763) || (context.Data?.GeneralInfo.Major >= 2) || (context.Data?.GeneralInfo.Build == 1539))
                                    {
                                        string conversion;
                                        switch (child1.Constant.kind)
                                        {
                                            case ExpressionConstant.Kind.Number:
                                                conversion = child1.Constant.valueNumber.ToString();
                                                break;
                                            case ExpressionConstant.Kind.Int64:
                                                conversion = child1.Constant.valueInt64.ToString();
                                                break;
                                            case ExpressionConstant.Kind.String:
                                                conversion = child1.Constant.valueString;
                                                break;
                                            default:
                                                return result; // This shouldn't happen
                                        }
                                        result = new Statement(Statement.StatementKind.ExprConstant);
                                        result.Constant = new ExpressionConstant(conversion);
                                    }
                                }
                                break;
                            case "real":
                                {
                                    // Ignore the optimization for GMS build versions less than 1763 and not equal to 1539.
                                    if ((context.Data?.GeneralInfo.Build >= 1763) || (context.Data?.GeneralInfo.Major >= 2) || (context.Data?.GeneralInfo.Build == 1539))
                                    {
                                        double conversion;
                                        switch (child1.Constant.kind)
                                        {
                                            case ExpressionConstant.Kind.Number:
                                                conversion = child1.Constant.valueNumber;
                                                break;
                                            case ExpressionConstant.Kind.Int64:
                                                conversion = child1.Constant.valueInt64;
                                                break;
                                            case ExpressionConstant.Kind.String:
                                                if (!double.TryParse(child1.Constant.valueString, out conversion))
                                                {
                                                    ReportCodeError("Cannot convert non-number string to a number.", child0.Token, false);
                                                }
                                                break;
                                            default:
                                                return result; // This shouldn't happen
                                        }
                                        result = new Statement(Statement.StatementKind.ExprConstant);
                                        result.Constant = new ExpressionConstant(conversion);
                                    }
                                }
                                break;
                            case "int64":
                                {
                                    long conversion;
                                    switch (child1.Constant.kind)
                                    {
                                        case ExpressionConstant.Kind.Number:
                                            conversion = Convert.ToInt64(child1.Constant.valueNumber);
                                            break;
                                        case ExpressionConstant.Kind.Int64:
                                            conversion = child1.Constant.valueInt64;
                                            break;
                                        default:
                                            return result; // This happens if you input a string for some reason
                                    }
                                    result = new Statement(Statement.StatementKind.ExprConstant);
                                    result.Constant = new ExpressionConstant(conversion);
                                }
                                break;
                            case "chr":
                                {
                                    string conversion;
                                    switch (child1.Constant.kind)
                                    {
                                        case ExpressionConstant.Kind.Number:
                                            conversion = ((char)(ushort)Convert.ToInt64(child1.Constant.valueNumber)).ToString();
                                            break;
                                        case ExpressionConstant.Kind.Int64:
                                            conversion = ((char)(ushort)(child1.Constant.valueInt64)).ToString();
                                            break;
                                        default:
                                            return result; // This happens if you input a string for some reason
                                    }
                                    result = new Statement(Statement.StatementKind.ExprConstant);
                                    result.Constant = new ExpressionConstant(conversion);
                                }
                                break;
                            case "ord":
                                {
                                    double conversion = 0d;
                                    if (child1.Constant.kind == ExpressionConstant.Kind.String &&
                                        child1.Constant.valueString != "")
                                    {
                                        conversion = (double)(int)child1.Constant.valueString[0];
                                    }
                                    result = new Statement(Statement.StatementKind.ExprConstant);
                                    result.Constant = new ExpressionConstant(conversion);
                                }
                                break;
                            default:
                                return result;
                        }
                        break;
                    case Statement.StatementKind.ExprBinaryOp:
                        return OptimizeBinaryOp(result);
                    case Statement.StatementKind.ExprUnary:
                        {
                            if (child0.Kind != Statement.StatementKind.ExprConstant)
                                break;
                            bool optimized = true;
                            Statement newConstant = new Statement(Statement.StatementKind.ExprConstant);
                            ExpressionConstant val = child0.Constant;
                            switch (result.Token.Kind)
                            {
                                case TokenKind.Not:
                                    switch (val.kind)
                                    {
                                        case ExpressionConstant.Kind.Number:
                                            newConstant.Constant = new ExpressionConstant((double)(!(val.valueNumber > 0.5) ? 1 : 0));
                                            break;
                                        case ExpressionConstant.Kind.Int64:
                                            newConstant.Constant = new ExpressionConstant((long)(!((double)val.valueInt64 > 0.5) ? 1 : 0));
                                            break;
                                        default:
                                            optimized = false;
                                            break;
                                    }
                                    break;
                                case TokenKind.BitwiseNegate:
                                    switch (val.kind)
                                    {
                                        case ExpressionConstant.Kind.Number:
                                            newConstant.Constant = new ExpressionConstant((double)(~(long)val.valueNumber));
                                            break;
                                        case ExpressionConstant.Kind.Int64:
                                            newConstant.Constant = new ExpressionConstant(~val.valueInt64);
                                            break;
                                        default:
                                            optimized = false;
                                            break;
                                    }
                                    break;
                                case TokenKind.Minus:
                                    switch (val.kind)
                                    {
                                        case ExpressionConstant.Kind.Number:
                                            newConstant.Constant = new ExpressionConstant(-val.valueNumber);
                                            break;
                                        case ExpressionConstant.Kind.Int64:
                                            newConstant.Constant = new ExpressionConstant(-val.valueInt64);
                                            break;
                                        default:
                                            optimized = false;
                                            break;
                                    }
                                    break;
                                default:
                                    optimized = false;
                                    break;
                            }

                            if (optimized)
                            {
                                result = newConstant;
                            }
                        }
                        break;
                    case Statement.StatementKind.ExprSingleVariable:
                        if (result.Children.Count >= 2 && child0.Kind == Statement.StatementKind.Token)
                        {
                            AccessorInfo ai = GetAccessorInfoFromStatement(context, result);
                            if (ai != null)
                            {
                                Statement accessorFunc = new Statement(Statement.StatementKind.ExprFunctionCall, ai.RFunc);
                                accessorFunc.Children.Add(new Statement() { Text = "" }); // `new`
                                accessorFunc.Children.Add(result.Children[1]);
                                if (result.Children.Count == 3)
                                    accessorFunc.Children.Add(result.Children[2]);
                                result.Children.Clear();
                                accessorFunc.Children.Insert(0, result);
                                result = accessorFunc;
                            }
                        }
                        break;
                    case Statement.StatementKind.ExprVariableRef:
                        for (int i = 0; i < result.Children.Count; i++)
                        {
                            if (result.Children[i].Children.Count != 2 || result.Children[i].Children[0].Kind != Statement.StatementKind.Token)
                                result.Children[i] = Optimize(context, result.Children[i]);
                            else
                            {
                                // Change accessors to proper right-value functions, embedding inside each other if needed
                                Statement curr = result.Children[i];

                                AccessorInfo ai = GetAccessorInfoFromStatement(context, curr);
                                if (ai != null)
                                {
                                    Statement accessorFunc = new Statement(Statement.StatementKind.ExprFunctionCall, ai.RFunc);
                                    accessorFunc.Children.Add(new Statement() { Text = "" }); // `new`
                                    accessorFunc.Children.Add(Optimize(context, curr.Children[1]));
                                    if (curr.Children.Count == 3)
                                        accessorFunc.Children.Add(Optimize(context, curr.Children[2]));
                                    curr.Children.Clear();
                                    Statement newVarChain = new Statement(Statement.StatementKind.ExprVariableRef);
                                    newVarChain.Children.AddRange(result.Children.GetRange(0, i + 1));
                                    if (newVarChain.Children.Count == 1)
                                        newVarChain = newVarChain.Children[0];
                                    accessorFunc.Children.Insert(0, newVarChain);
                                    result.Children.RemoveRange(0, i + 1);
                                    result.Children.Insert(0, accessorFunc);
                                    i = 0; // runs i = 1 next iteration
                                }
                            }
                        }
                        break;
                    case Statement.StatementKind.SwitchCase:
                        if (child0.Kind != Statement.StatementKind.ExprConstant &&
                            child0.Kind != Statement.StatementKind.ExprVariableRef &&
                            child0.Kind != Statement.StatementKind.ExprSingleVariable)
                        {
                            ReportCodeError("Case argument must be constant.", result.Token, false);
                        }
                        break;
                    // todo: parse enum references
                }
                return result;
            }

            private static AccessorInfo GetAccessorInfoFromStatement(CompileContext context, Statement s)
            {
                AccessorInfo ai = null;
                TokenKind kind = s.Children[0].Token.Kind;
                if (s.Children.Count == 2)
                {
                    if (context.BuiltInList.Accessors1D.ContainsKey(kind))
                        ai = context.BuiltInList.Accessors1D[kind];
                    else
                        ReportCodeError("Accessor has incorrect number of arguments", s.Children[0].Token, false);
                } else
                {
                    if (context.BuiltInList.Accessors2D.ContainsKey(kind))
                        ai = context.BuiltInList.Accessors2D[kind];
                    else
                        ReportCodeError("Accessor has incorrect number of arguments", s.Children[0].Token, false);
                }
                return ai;
            }

            // This is probably the messiest function. I can't think of any easy ways to clean it right now though.
            private static Statement OptimizeBinaryOp(Statement s)
            {
                Statement result = new Statement(s);
                while (result.Children.Count >= 2 && result.Children[0].Kind == Statement.StatementKind.ExprConstant && result.Children[1].Kind == Statement.StatementKind.ExprConstant)
                {
                    ExpressionConstant left = result.Children[0].Constant;
                    ExpressionConstant right = result.Children[1].Constant;
                    Statement newConstant = new Statement(Statement.StatementKind.ExprConstant);
                    bool optimized = true;
                    switch (s.Token.Kind)
                    {
                        // AND
                        case TokenKind.LogicalAnd:
                            switch (left.kind)
                            {
                                case ExpressionConstant.Kind.Number:
                                    switch (right.kind)
                                    {
                                        case ExpressionConstant.Kind.Number:
                                            newConstant.Constant = new ExpressionConstant((left.valueNumber > 0.5 && right.valueNumber > 0.5) ? 1d : 0d);
                                            break;
                                        case ExpressionConstant.Kind.Int64:
                                            newConstant.Constant = new ExpressionConstant((left.valueNumber > 0.5 && (double)right.valueInt64 > 0.5) ? 1d : 0d);
                                            break;
                                        default:
                                            optimized = false;
                                            break;
                                    }
                                    break;
                                case ExpressionConstant.Kind.Int64:
                                    switch (right.kind)
                                    {
                                        case ExpressionConstant.Kind.Number:
                                            newConstant.Constant = new ExpressionConstant(((double)left.valueInt64 > 0.5 && right.valueNumber > 0.5) ? 1d : 0d);
                                            break;
                                        case ExpressionConstant.Kind.Int64:
                                            newConstant.Constant = new ExpressionConstant(((double)left.valueInt64 > 0.5 && (double)right.valueInt64 > 0.5) ? 1d : 0d);
                                            break;
                                        default:
                                            optimized = false;
                                            break;
                                    }
                                    break;
                                default:
                                    optimized = false;
                                    break;
                            }
                            break;
                        // OR
                        case TokenKind.LogicalOr:
                            switch (left.kind)
                            {
                                case ExpressionConstant.Kind.Number:
                                    switch (right.kind)
                                    {
                                        case ExpressionConstant.Kind.Number:
                                            newConstant.Constant = new ExpressionConstant((left.valueNumber > 0.5 || right.valueNumber > 0.5) ? 1d : 0d);
                                            break;
                                        case ExpressionConstant.Kind.Int64:
                                            newConstant.Constant = new ExpressionConstant((left.valueNumber > 0.5 || (double)right.valueInt64 > 0.5) ? 1d : 0d);
                                            break;
                                        default:
                                            optimized = false;
                                            break;
                                    }
                                    break;
                                case ExpressionConstant.Kind.Int64:
                                    switch (right.kind)
                                    {
                                        case ExpressionConstant.Kind.Number:
                                            newConstant.Constant = new ExpressionConstant(((double)left.valueInt64 > 0.5 || right.valueNumber > 0.5) ? 1d : 0d);
                                            break;
                                        case ExpressionConstant.Kind.Int64:
                                            newConstant.Constant = new ExpressionConstant(((double)left.valueInt64 > 0.5 || (double)right.valueInt64 > 0.5) ? 1d : 0d);
                                            break;
                                        default:
                                            optimized = false;
                                            break;
                                    }
                                    break;
                                default:
                                    optimized = false;
                                    break;
                            }
                            break;
                        // PLUS
                        case TokenKind.Plus:
                            switch (left.kind)
                            {
                                case ExpressionConstant.Kind.Number:
                                    switch (right.kind)
                                    {
                                        case ExpressionConstant.Kind.Number:
                                            newConstant.Constant = new ExpressionConstant(left.valueNumber + right.valueNumber);
                                            break;
                                        case ExpressionConstant.Kind.Int64:
                                            newConstant.Constant = new ExpressionConstant((long)left.valueNumber + right.valueInt64);
                                            break;
                                        default:
                                            optimized = false;
                                            break;
                                    }
                                    break;
                                case ExpressionConstant.Kind.Int64:
                                    switch (right.kind)
                                    {
                                        case ExpressionConstant.Kind.Number:
                                            newConstant.Constant = new ExpressionConstant(left.valueInt64 + (long)right.valueNumber);
                                            break;
                                        case ExpressionConstant.Kind.Int64:
                                            newConstant.Constant = new ExpressionConstant(left.valueInt64 + right.valueInt64);
                                            break;
                                        default:
                                            optimized = false;
                                            break;
                                    }
                                    break;
                                case ExpressionConstant.Kind.String:
                                    if (right.kind == ExpressionConstant.Kind.String)
                                    {
                                        newConstant.Constant = new ExpressionConstant(left.valueString + right.valueString);
                                    }
                                    else
                                        optimized = false;
                                    break;
                                default:
                                    optimized = false;
                                    break;
                            }
                            break;
                        // MINUS
                        case TokenKind.Minus:
                            switch (left.kind)
                            {
                                case ExpressionConstant.Kind.Number:
                                    switch (right.kind)
                                    {
                                        case ExpressionConstant.Kind.Number:
                                            newConstant.Constant = new ExpressionConstant(left.valueNumber - right.valueNumber);
                                            break;
                                        case ExpressionConstant.Kind.Int64:
                                            newConstant.Constant = new ExpressionConstant((long)left.valueNumber - right.valueInt64);
                                            break;
                                        default:
                                            optimized = false;
                                            break;
                                    }
                                    break;
                                case ExpressionConstant.Kind.Int64:
                                    switch (right.kind)
                                    {
                                        case ExpressionConstant.Kind.Number:
                                            newConstant.Constant = new ExpressionConstant(left.valueInt64 - (long)right.valueNumber);
                                            break;
                                        case ExpressionConstant.Kind.Int64:
                                            newConstant.Constant = new ExpressionConstant(left.valueInt64 - right.valueInt64);
                                            break;
                                        default:
                                            optimized = false;
                                            break;
                                    }
                                    break;
                                default:
                                    optimized = false;
                                    break;
                            }
                            break;
                        // TIMES
                        case TokenKind.Times:
                            switch (left.kind)
                            {
                                case ExpressionConstant.Kind.Number:
                                    switch (right.kind)
                                    {
                                        case ExpressionConstant.Kind.Number:
                                            newConstant.Constant = new ExpressionConstant(left.valueNumber * right.valueNumber);
                                            break;
                                        case ExpressionConstant.Kind.Int64:
                                            newConstant.Constant = new ExpressionConstant((long)left.valueNumber * right.valueInt64);
                                            break;
                                        case ExpressionConstant.Kind.String:
                                            // Apparently this exists
                                            StringBuilder newString = new StringBuilder();
                                            for (int i = 0; i < (int)left.valueNumber; i++)
                                                newString.Append(right.valueString);
                                            newConstant.Constant = new ExpressionConstant(newString.ToString());
                                            break;
                                        default:
                                            optimized = false;
                                            break;
                                    }
                                    break;
                                case ExpressionConstant.Kind.Int64:
                                    switch (right.kind)
                                    {
                                        case ExpressionConstant.Kind.Number:
                                            newConstant.Constant = new ExpressionConstant(left.valueInt64 * (long)right.valueNumber);
                                            break;
                                        case ExpressionConstant.Kind.Int64:
                                            newConstant.Constant = new ExpressionConstant(left.valueInt64 * right.valueInt64);
                                            break;
                                        default:
                                            optimized = false;
                                            break;
                                    }
                                    break;
                                default:
                                    optimized = false;
                                    break;
                            }
                            break;
                        // DIVIDE
                        case TokenKind.Divide:
                            switch (left.kind)
                            {
                                case ExpressionConstant.Kind.Number:
                                    switch (right.kind)
                                    {
                                        case ExpressionConstant.Kind.Number:
                                            if (right.valueNumber == 0)
                                            {
                                                ReportCodeError("Division by zero.", s.Children[1].Token, false);
                                                optimized = false;
                                                break;
                                            }
                                            newConstant.Constant = new ExpressionConstant(left.valueNumber / right.valueNumber);
                                            break;
                                        case ExpressionConstant.Kind.Int64:
                                            if (right.valueInt64 == 0)
                                            {
                                                ReportCodeError("Division by zero.", s.Children[1].Token, false);
                                                optimized = false;
                                                break;
                                            }
                                            newConstant.Constant = new ExpressionConstant((long)left.valueNumber / right.valueInt64);
                                            break;
                                        default:
                                            optimized = false;
                                            break;
                                    }
                                    break;
                                case ExpressionConstant.Kind.Int64:
                                    switch (right.kind)
                                    {
                                        case ExpressionConstant.Kind.Number:
                                            if (right.valueNumber == 0)
                                            {
                                                ReportCodeError("Division by zero.", s.Children[1].Token, false);
                                                optimized = false;
                                                break;
                                            }
                                            newConstant.Constant = new ExpressionConstant(left.valueInt64 / (long)right.valueNumber);
                                            break;
                                        case ExpressionConstant.Kind.Int64:
                                            if (right.valueInt64 == 0)
                                            {
                                                ReportCodeError("Division by zero.", s.Children[1].Token, false);
                                                optimized = false;
                                                break;
                                            }
                                            newConstant.Constant = new ExpressionConstant(left.valueInt64 / right.valueInt64);
                                            break;
                                        default:
                                            optimized = false;
                                            break;
                                    }
                                    break;
                            }
                            break;
                        // DIV
                        case TokenKind.Div:
                            switch (left.kind)
                            {
                                case ExpressionConstant.Kind.Number:
                                    switch (right.kind)
                                    {
                                        case ExpressionConstant.Kind.Number:
                                            if ((int)right.valueNumber == 0)
                                            {
                                                ReportCodeError("Division by zero.", s.Children[1].Token, false);
                                                optimized = false;
                                                break;
                                            }
                                            newConstant.Constant = new ExpressionConstant((double)((int)left.valueNumber / (int)right.valueNumber));
                                            break;
                                        case ExpressionConstant.Kind.Int64:
                                            if (right.valueInt64 == 0)
                                            {
                                                ReportCodeError("Division by zero.", s.Children[1].Token, false);
                                                optimized = false;
                                                break;
                                            }
                                            newConstant.Constant = new ExpressionConstant((long)left.valueNumber / right.valueInt64);
                                            break;
                                        default:
                                            optimized = false;
                                            break;
                                    }
                                    break;
                                case ExpressionConstant.Kind.Int64:
                                    switch (right.kind)
                                    {
                                        case ExpressionConstant.Kind.Number:
                                            if ((int)right.valueNumber == 0)
                                            {
                                                ReportCodeError("Division by zero.", s.Children[1].Token, false);
                                                optimized = false;
                                                break;
                                            }
                                            newConstant.Constant = new ExpressionConstant(left.valueInt64 / (long)right.valueNumber);
                                            break;
                                        case ExpressionConstant.Kind.Int64:
                                            if (right.valueInt64 == 0)
                                            {
                                                ReportCodeError("Division by zero.", s.Children[1].Token, false);
                                                optimized = false;
                                                break;
                                            }
                                            newConstant.Constant = new ExpressionConstant(left.valueInt64 / right.valueInt64);
                                            break;
                                        default:
                                            optimized = false;
                                            break;
                                    }
                                    break;
                                default:
                                    optimized = false;
                                    break;
                            }
                            break;
                        // MOD
                        case TokenKind.Mod:
                            switch (left.kind)
                            {
                                case ExpressionConstant.Kind.Number:
                                    switch (right.kind)
                                    {
                                        case ExpressionConstant.Kind.Number:
                                            if ((int)right.valueNumber == 0)
                                            {
                                                ReportCodeError("Modulo by zero.", s.Children[1].Token, false);
                                                optimized = false;
                                                break;
                                            }
                                            newConstant.Constant = new ExpressionConstant(left.valueNumber % right.valueNumber);
                                            break;
                                        case ExpressionConstant.Kind.Int64:
                                            if (right.valueInt64 == 0)
                                            {
                                                ReportCodeError("Modulo by zero.", s.Children[1].Token, false);
                                                optimized = false;
                                                break;
                                            }
                                            newConstant.Constant = new ExpressionConstant((long)left.valueNumber % right.valueInt64);
                                            break;
                                        default:
                                            optimized = false;
                                            break;
                                    }
                                    break;
                                case ExpressionConstant.Kind.Int64:
                                    switch (right.kind)
                                    {
                                        case ExpressionConstant.Kind.Number:
                                            if ((int)right.valueNumber == 0)
                                            {
                                                ReportCodeError("Modulo by zero.", s.Children[1].Token, false);
                                                optimized = false;
                                                break;
                                            }
                                            newConstant.Constant = new ExpressionConstant(left.valueInt64 % (long)right.valueNumber);
                                            break;
                                        case ExpressionConstant.Kind.Int64:
                                            if (right.valueInt64 == 0)
                                            {
                                                ReportCodeError("Modulo by zero.", s.Children[1].Token, false);
                                                optimized = false;
                                                break;
                                            }
                                            newConstant.Constant = new ExpressionConstant(left.valueInt64 % right.valueInt64);
                                            break;
                                        default:
                                            optimized = false;
                                            break;
                                    }
                                    break;
                                default:
                                    optimized = false;
                                    break;
                            }
                            break;
                        // XOR
                        case TokenKind.LogicalXor:
                            switch (left.kind)
                            {
                                case ExpressionConstant.Kind.Number:
                                    switch (right.kind)
                                    {
                                        case ExpressionConstant.Kind.Number:
                                            newConstant.Constant = new ExpressionConstant(((left.valueNumber > 0.5) ? 1 : 0) ^ ((right.valueNumber > 0.5) ? 1 : 0));
                                            break;
                                        case ExpressionConstant.Kind.Int64:
                                            newConstant.Constant = new ExpressionConstant(((left.valueNumber > 0.5) ? 1 : 0) ^ (((double)right.valueInt64 > 0.5) ? 1 : 0));
                                            break;
                                        default:
                                            optimized = false;
                                            break;
                                    }
                                    break;
                                case ExpressionConstant.Kind.Int64:
                                    switch (right.kind)
                                    {
                                        case ExpressionConstant.Kind.Number:
                                            newConstant.Constant = new ExpressionConstant((((double)left.valueInt64 > 0.5) ? 1 : 0) ^ ((right.valueNumber > 0.5) ? 1 : 0));
                                            break;
                                        case ExpressionConstant.Kind.Int64:
                                            newConstant.Constant = new ExpressionConstant((((double)left.valueInt64 > 0.5) ? 1 : 0) ^ (((double)right.valueInt64 > 0.5) ? 1 : 0));
                                            break;
                                        default:
                                            optimized = false;
                                            break;
                                    }
                                    break;
                                default:
                                    optimized = false;
                                    break;
                            }
                            break;
                        // BITWISE OR
                        case TokenKind.BitwiseOr:
                            switch (left.kind)
                            {
                                case ExpressionConstant.Kind.Number:
                                    switch (right.kind)
                                    {
                                        case ExpressionConstant.Kind.Number:
                                            newConstant.Constant = new ExpressionConstant((long)left.valueNumber | (long)right.valueNumber);
                                            break;
                                        case ExpressionConstant.Kind.Int64:
                                            newConstant.Constant = new ExpressionConstant((long)left.valueNumber | right.valueInt64);
                                            break;
                                        default:
                                            optimized = false;
                                            break;
                                    }
                                    break;
                                case ExpressionConstant.Kind.Int64:
                                    switch (right.kind)
                                    {
                                        case ExpressionConstant.Kind.Number:
                                            newConstant.Constant = new ExpressionConstant(left.valueInt64 | (long)right.valueNumber);
                                            break;
                                        case ExpressionConstant.Kind.Int64:
                                            newConstant.Constant = new ExpressionConstant(left.valueInt64 | right.valueInt64);
                                            break;
                                        default:
                                            optimized = false;
                                            break;
                                    }
                                    break;
                                default:
                                    optimized = false;
                                    break;
                            }
                            break;
                        // BITWISE AND
                        case TokenKind.BitwiseAnd:
                            switch (left.kind)
                            {
                                case ExpressionConstant.Kind.Number:
                                    switch (right.kind)
                                    {
                                        case ExpressionConstant.Kind.Number:
                                            newConstant.Constant = new ExpressionConstant((long)left.valueNumber & (long)right.valueNumber);
                                            break;
                                        case ExpressionConstant.Kind.Int64:
                                            newConstant.Constant = new ExpressionConstant((long)left.valueNumber & right.valueInt64);
                                            break;
                                        default:
                                            optimized = false;
                                            break;
                                    }
                                    break;
                                case ExpressionConstant.Kind.Int64:
                                    switch (right.kind)
                                    {
                                        case ExpressionConstant.Kind.Number:
                                            newConstant.Constant = new ExpressionConstant(left.valueInt64 & (long)right.valueNumber);
                                            break;
                                        case ExpressionConstant.Kind.Int64:
                                            newConstant.Constant = new ExpressionConstant(left.valueInt64 & right.valueInt64);
                                            break;
                                        default:
                                            optimized = false;
                                            break;
                                    }
                                    break;
                                default:
                                    optimized = false;
                                    break;
                            }
                            break;
                        // BITWISE XOR
                        case TokenKind.BitwiseXor:
                            switch (left.kind)
                            {
                                case ExpressionConstant.Kind.Number:
                                    switch (right.kind)
                                    {
                                        case ExpressionConstant.Kind.Number:
                                            newConstant.Constant = new ExpressionConstant((long)left.valueNumber ^ (long)right.valueNumber);
                                            break;
                                        case ExpressionConstant.Kind.Int64:
                                            newConstant.Constant = new ExpressionConstant((long)left.valueNumber ^ right.valueInt64);
                                            break;
                                        default:
                                            optimized = false;
                                            break;
                                    }
                                    break;
                                case ExpressionConstant.Kind.Int64:
                                    switch (right.kind)
                                    {
                                        case ExpressionConstant.Kind.Number:
                                            newConstant.Constant = new ExpressionConstant(left.valueInt64 ^ (long)right.valueNumber);
                                            break;
                                        case ExpressionConstant.Kind.Int64:
                                            newConstant.Constant = new ExpressionConstant(left.valueInt64 ^ right.valueInt64);
                                            break;
                                        default:
                                            optimized = false;
                                            break;
                                    }
                                    break;
                                default:
                                    optimized = false;
                                    break;
                            }
                            break;
                        // BITWISE SHIFT LEFT
                        case TokenKind.BitwiseShiftLeft:
                            switch (left.kind)
                            {
                                case ExpressionConstant.Kind.Number:
                                    switch (right.kind)
                                    {
                                        case ExpressionConstant.Kind.Number:
                                            newConstant.Constant = new ExpressionConstant((long)left.valueNumber << (int)right.valueNumber);
                                            break;
                                        case ExpressionConstant.Kind.Int64:
                                            newConstant.Constant = new ExpressionConstant((long)left.valueNumber << (int)right.valueInt64);
                                            break;
                                        default:
                                            optimized = false;
                                            break;
                                    }
                                    break;
                                case ExpressionConstant.Kind.Int64:
                                    switch (right.kind)
                                    {
                                        case ExpressionConstant.Kind.Number:
                                            newConstant.Constant = new ExpressionConstant(left.valueInt64 << (int)right.valueNumber);
                                            break;
                                        case ExpressionConstant.Kind.Int64:
                                            newConstant.Constant = new ExpressionConstant(left.valueInt64 << (int)right.valueInt64);
                                            break;
                                        default:
                                            optimized = false;
                                            break;
                                    }
                                    break;
                                default:
                                    optimized = false;
                                    break;
                            }
                            break;
                        // BITWISE SHIFT RIGHT
                        case TokenKind.BitwiseShiftRight:
                            switch (left.kind)
                            {
                                case ExpressionConstant.Kind.Number:
                                    switch (right.kind)
                                    {
                                        case ExpressionConstant.Kind.Number:
                                            newConstant.Constant = new ExpressionConstant((long)left.valueNumber >> (int)right.valueNumber);
                                            break;
                                        case ExpressionConstant.Kind.Int64:
                                            newConstant.Constant = new ExpressionConstant((long)left.valueNumber >> (int)right.valueInt64);
                                            break;
                                        default:
                                            optimized = false;
                                            break;
                                    }
                                    break;
                                case ExpressionConstant.Kind.Int64:
                                    switch (right.kind)
                                    {
                                        case ExpressionConstant.Kind.Number:
                                            newConstant.Constant = new ExpressionConstant(left.valueInt64 >> (int)right.valueNumber);
                                            break;
                                        case ExpressionConstant.Kind.Int64:
                                            newConstant.Constant = new ExpressionConstant(left.valueInt64 >> (int)right.valueInt64);
                                            break;
                                        default:
                                            optimized = false;
                                            break;
                                    }
                                    break;
                                default:
                                    optimized = false;
                                    break;
                            }
                            break;
                        // COMPARISONS
                        case TokenKind.CompareEqual:
                        case TokenKind.CompareGreater:
                        case TokenKind.CompareGreaterEqual:
                        case TokenKind.CompareLess:
                        case TokenKind.CompareLessEqual:
                        case TokenKind.CompareNotEqual:
                            // First, calculate "difference" number
                            double differenceValue = 0;
                            switch (left.kind)
                            {
                                case ExpressionConstant.Kind.String:
                                    if (right.kind == ExpressionConstant.Kind.String)
                                    {
                                        differenceValue = string.Compare(left.valueString, right.valueString);
                                    }
                                    else
                                    {
                                        optimized = false;
                                    }
                                    break;
                                case ExpressionConstant.Kind.Int64:
                                    switch (right.kind)
                                    {
                                        case ExpressionConstant.Kind.Int64:
                                            differenceValue = left.valueInt64 - right.valueInt64;
                                            break;
                                        case ExpressionConstant.Kind.Number:
                                            differenceValue = left.valueInt64 - (long)right.valueNumber;
                                            break;
                                        default:
                                            optimized = false;
                                            break;
                                    }
                                    break;
                                case ExpressionConstant.Kind.Number:
                                    switch (right.kind)
                                    {
                                        case ExpressionConstant.Kind.Int64:
                                            differenceValue = (long)left.valueNumber - right.valueInt64;
                                            break;
                                        case ExpressionConstant.Kind.Number:
                                            differenceValue = left.valueNumber - right.valueNumber;
                                            break;
                                        default:
                                            optimized = false;
                                            break;
                                    }
                                    break;
                                default:
                                    optimized = false;
                                    break;
                            }

                            if (optimized)
                            {
                                newConstant.Constant = new ExpressionConstant(0d) { isBool = true };

                                switch (s.Token.Kind)
                                {
                                    case TokenKind.CompareEqual:
                                        newConstant.Constant.valueNumber = (differenceValue == 0) ? 1 : 0;
                                        break;
                                    case TokenKind.CompareNotEqual:
                                        newConstant.Constant.valueNumber = (differenceValue != 0) ? 1 : 0;
                                        break;
                                    case TokenKind.CompareLess:
                                        newConstant.Constant.valueNumber = (differenceValue < 0) ? 1 : 0;
                                        break;
                                    case TokenKind.CompareLessEqual:
                                        newConstant.Constant.valueNumber = (differenceValue <= 0) ? 1 : 0;
                                        break;
                                    case TokenKind.CompareGreater:
                                        newConstant.Constant.valueNumber = (differenceValue > 0) ? 1 : 0;
                                        break;
                                    case TokenKind.CompareGreaterEqual:
                                        newConstant.Constant.valueNumber = (differenceValue >= 0) ? 1 : 0;
                                        break;
                                }
                            }
                            break;
                        default:
                            optimized = false;
                            break;
                    }
                    if (!optimized)
                    {
                        return result; // result is a copy of "s"
                    }
                    result.Children.RemoveRange(0, 2);
                    result.Children.Insert(0, newConstant);
                }
                if (result.Children.Count == 1)
                    result = result.Children[0];
                return result;
            }

            private static bool IsKeyword(TokenKind t)
            {
                return t.In(
                    TokenKind.KeywordBreak,
                    TokenKind.KeywordCase,
                    TokenKind.KeywordContinue,
                    TokenKind.KeywordDefault,
                    TokenKind.KeywordDo,
                    TokenKind.KeywordElse,
                    TokenKind.KeywordExit,
                    TokenKind.KeywordFor,
                    TokenKind.KeywordGlobalVar,
                    TokenKind.KeywordIf,
                    TokenKind.KeywordRepeat,
                    TokenKind.KeywordReturn,
                    TokenKind.KeywordStruct,
                    TokenKind.KeywordSwitch,
                    TokenKind.KeywordThen,
                    TokenKind.KeywordUntil,
                    TokenKind.KeywordVar,
                    TokenKind.KeywordWhile,
                    TokenKind.KeywordWith);
            }

            private static bool ResolveIdentifier(CompileContext context, string identifier, out ExpressionConstant constant)
            {
                constant = new ExpressionConstant(0d);
                int index = context.GetAssetIndexByName(identifier);
                if (index == -1)
                {
                    if (context.BuiltInList.Constants.TryGetValue(identifier, out double val))
                    {
                        constant.valueNumber = val;
                        return true;
                    }
                    return false;
                }
                if (context.TypedAssetRefs)
                    constant.kind = ExpressionConstant.Kind.Reference;
                constant.valueNumber = (double)index;
                return true;
            }

            private static int GetVariableID(CompileContext context, string name, out bool isGlobalBuiltin)
            {
                VariableInfo vi;

                isGlobalBuiltin = true;
                if (!context.BuiltInList.GlobalNotArray.TryGetValue(name, out vi) && !context.BuiltInList.GlobalArray.TryGetValue(name, out vi))
                {
                    isGlobalBuiltin = false;
                    if (!context.BuiltInList.Instance.TryGetValue(name, out vi) && !context.BuiltInList.InstanceLimitedEvent.TryGetValue(name, out vi) && !context.userDefinedVariables.TryGetValue(name, out vi))
                    {
                        vi = new VariableInfo()
                        {
                            ID = 100000 + context.userDefinedVariables.Count
                        };
                        context.userDefinedVariables[name] = vi;
                    }
                }

                if (vi.ID >= context.BuiltInList.Argument0ID && vi.ID <= context.BuiltInList.Argument15ID)
                {
                    int arg_index = vi.ID - context.BuiltInList.Argument0ID + 1;
                    if (arg_index > context.LastCompiledArgumentCount)
                    {
                        context.LastCompiledArgumentCount = arg_index;
                    }
                }

                return vi.ID;
            }
        }
    }
}
