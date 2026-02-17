/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System;

namespace Underanalyzer.Compiler.Lexer;

internal interface IToken
{
    /// <summary>
    /// Context that this token belongs to, for information retrieval.
    /// </summary>
    public LexContext Context { get; }

    /// <summary>
    /// Text position of the token within the token's context. Can be used to look up line numbers, for instance.
    /// </summary>
    public int TextPosition { get; }
}

/// <summary>
/// Kinds of separators that can be used in code.
/// </summary>
internal enum SeparatorKind
{
    BlockOpen,          // {
    BlockClose,         // }
    GroupOpen,          // (
    GroupClose,         // )
    Dot,                // .
    Comma,              // ,
    Semicolon,          // ;
    Colon,              // :
    ArrayOpen,          // [
    ArrayOpenList,      // [|
    ArrayOpenMap,       // [?
    ArrayOpenGrid,      // [#
    ArrayOpenDirect,    // [@
    ArrayOpenStruct,    // [$
    ArrayClose          // ]
}

/// <summary>
/// Token representing a separator in code.
/// </summary>
/// <param name="Kind">Separator kind for this token.</param>
internal sealed record TokenSeparator(LexContext Context, int TextPosition, SeparatorKind Kind) : IToken
{
    public static string KindToString(SeparatorKind kind)
    {
        return kind switch
        {
            SeparatorKind.BlockOpen => "{",
            SeparatorKind.BlockClose => "}",
            SeparatorKind.GroupOpen => "(",
            SeparatorKind.GroupClose => ")",
            SeparatorKind.Dot => ".",
            SeparatorKind.Comma => ",",
            SeparatorKind.Semicolon => ";",
            SeparatorKind.Colon => ":",
            SeparatorKind.ArrayOpen => "[",
            SeparatorKind.ArrayOpenList => "[|",
            SeparatorKind.ArrayOpenMap => "[?",
            SeparatorKind.ArrayOpenGrid => "[#",
            SeparatorKind.ArrayOpenDirect => "[@",
            SeparatorKind.ArrayOpenStruct => "[$",
            SeparatorKind.ArrayClose => "]",
            _ => throw new Exception($"Unknown separator kind {kind}")
        };
    }

    public override string ToString()
    {
        return KindToString(Kind);
    }
}

/// <summary>
/// Kinds of operators that can be used in code.
/// </summary>
internal enum OperatorKind
{
    Assign,                     // =
    Assign2,                    // :=

    CompareEqual,               // ==
    CompareNotEqual,            // !=
    CompareNotEqual2,           // <>
    CompareGreater,             // >
    CompareGreaterEqual,        // >=
    CompareLesser,              // <
    CompareLesserEqual,         // <=

    Plus,                       // +
    Minus,                      // -
    Times,                      // *
    Divide,                     // /
    Mod,                        // %
    Not,                        // !
    Conditional,                // ?
    NullishCoalesce,            // ??
    LogicalAnd,                 // &&
    LogicalOr,                  // ||
    LogicalXor,                 // ^^
    BitwiseAnd,                 // &
    BitwiseOr,                  // |
    BitwiseXor,                 // ^
    BitwiseNegate,              // ~
    BitwiseShiftLeft,           // <<
    BitwiseShiftRight,          // >>

    Increment,                  // ++
    Decrement,                  // --

    CompoundPlus,               // +=
    CompoundMinus,              // -=
    CompoundTimes,              // *=
    CompoundDivide,             // /=
    CompoundMod,                // %=
    CompoundNullishCoalesce,    // ??=
    CompoundBitwiseAnd,         // &=
    CompoundBitwiseOr,          // |=
    CompoundBitwiseXor          // ^=
}

/// <summary>
/// Token representing an operator in code.
/// </summary>
/// <param name="Kind">Operator kind for this token.</param>
internal sealed record TokenOperator(LexContext Context, int TextPosition, OperatorKind Kind) : IToken
{
    public static string KindToString(OperatorKind kind)
    {
        return kind switch
        {
            OperatorKind.Assign => "=",
            OperatorKind.Assign2 => ":=",
            OperatorKind.CompareEqual => "==",
            OperatorKind.CompareNotEqual => "!=",
            OperatorKind.CompareNotEqual2 => "<>",
            OperatorKind.CompareGreater => ">",
            OperatorKind.CompareGreaterEqual => ">=",
            OperatorKind.CompareLesser => "<",
            OperatorKind.CompareLesserEqual => "<=",
            OperatorKind.Plus => "+",
            OperatorKind.Minus => "-",
            OperatorKind.Times => "*",
            OperatorKind.Divide => "/",
            OperatorKind.Mod => "%",
            OperatorKind.Not => "!",
            OperatorKind.Conditional => "?",
            OperatorKind.NullishCoalesce => "??",
            OperatorKind.LogicalAnd => "&&",
            OperatorKind.LogicalOr => "||",
            OperatorKind.LogicalXor => "^^",
            OperatorKind.BitwiseAnd => "&",
            OperatorKind.BitwiseOr => "|",
            OperatorKind.BitwiseXor => "^",
            OperatorKind.BitwiseNegate => "~",
            OperatorKind.BitwiseShiftLeft => "<<",
            OperatorKind.BitwiseShiftRight => ">>",
            OperatorKind.Increment => "++",
            OperatorKind.Decrement => "--",
            OperatorKind.CompoundPlus => "+=",
            OperatorKind.CompoundMinus => "-=",
            OperatorKind.CompoundTimes => "*=",
            OperatorKind.CompoundDivide => "/=",
            OperatorKind.CompoundMod => "%=",
            OperatorKind.CompoundNullishCoalesce => "??=",
            OperatorKind.CompoundBitwiseAnd => "&=",
            OperatorKind.CompoundBitwiseOr => "|=",
            OperatorKind.CompoundBitwiseXor => "^=",
            _ => throw new Exception($"Unknown operator kind {kind}")
        };
    }

    public override string ToString()
    {
        return KindToString(Kind);
    }
}

/// <summary>
/// Kinds of keywords that can be used in code.
/// </summary>
internal enum KeywordKind
{
    None,

    If,         // if
    Then,       // then
    Else,       // else
    Switch,     // switch
    Case,       // case
    Default,    // default

    Begin,      // begin
    End,        // end

    Break,      // break
    Continue,   // continue
    Exit,       // exit
    Return,     // return

    While,      // while
    For,        // for
    Repeat,     // repeat
    Do,         // do
    Until,      // until
    With,       // with

    Var,        // var
    Globalvar,  // globalvar

    Not,        // not
    Div,        // div
    Mod,        // mod

    And,        // and
    Or,         // or
    Xor,        // xor

    Enum,       // enum

    Try,        // try
    Catch,      // catch
    Finally,    // finally
    Throw,      // throw

    New,        // new
    Delete,     // delete
    Function,   // function
    Static      // static
}

/// <summary>
/// Token representing a keyword in code.
/// </summary>
/// <param name="Kind">Keyword kind for this token.</param>
internal sealed record TokenKeyword(LexContext Context, int TextPosition, KeywordKind Kind) : IToken
{
    public static string KindToString(KeywordKind kind)
    {
        return kind switch
        {
            KeywordKind.If => "if",
            KeywordKind.Then => "then",
            KeywordKind.Else => "else",
            KeywordKind.Switch => "switch",
            KeywordKind.Case => "case",
            KeywordKind.Default => "default",
            KeywordKind.Begin => "begin",
            KeywordKind.End => "end",
            KeywordKind.Break => "break",
            KeywordKind.Continue => "continue",
            KeywordKind.Exit => "exit",
            KeywordKind.Return => "return",
            KeywordKind.While => "while",
            KeywordKind.For => "for",
            KeywordKind.Repeat => "repeat",
            KeywordKind.Do => "do",
            KeywordKind.Until => "until",
            KeywordKind.With => "with",
            KeywordKind.Var => "var",
            KeywordKind.Globalvar => "globalvar",
            KeywordKind.Not => "not",
            KeywordKind.Div => "div",
            KeywordKind.Mod => "mod",
            KeywordKind.And => "and",
            KeywordKind.Or => "or",
            KeywordKind.Xor => "xor",
            KeywordKind.Enum => "enum",
            KeywordKind.Try => "try",
            KeywordKind.Catch => "catch",
            KeywordKind.Finally => "finally",
            KeywordKind.Throw => "throw",
            KeywordKind.New => "new",
            KeywordKind.Delete => "delete",
            KeywordKind.Function => "function",
            KeywordKind.Static => "static",
            _ => throw new Exception($"Unknown keyword kind {kind}")
        };
    }

    public override string ToString()
    {
        return KindToString(Kind);
    }
}

/// <summary>
/// Token representing an identifier in code.
/// </summary>
/// <param name="Text">Verbatim text used for the identifier.</param>
internal sealed record TokenIdentifier(LexContext Context, int TextPosition, string Text) : IToken
{
    public override string ToString()
    {
        return Text;
    }
}

/// <summary>
/// Token representing a constant 64-bit floating point number in code (which may be an integer).
/// </summary>
/// <param name="Text">Verbatim text used for the token.</param>
/// <param name="Value">64-bit floating point value.</param>
/// <param name="IsConstant">Whether this token came from a constant.</param>
internal sealed record TokenNumber(LexContext Context, int TextPosition, string Text, double Value, bool IsConstant = false) : IToken
{
    public override string ToString()
    {
        return Text;
    }
}

/// <summary>
/// Token representing a constant 64-bit integer in code.
/// </summary>
/// <param name="Text">Verbatim text used for the token.</param>
/// <param name="Value">64-bit integer value.</param>
internal sealed record TokenInt64(LexContext Context, int TextPosition, string Text, long Value) : IToken
{
    public override string ToString()
    {
        return Text;
    }
}

/// <summary>
/// Token representing a constant boolean in code.
/// </summary>
/// <param name="Value">Boolean value.</param>
internal sealed record TokenBoolean(LexContext Context, int TextPosition, bool Value) : IToken
{
    public override string ToString()
    {
        return Value ? "true" : "false";
    }
}

/// <summary>
/// Token representing a constant string in code.
/// </summary>
/// <param name="Text">Verbatim text used for the token.</param>
/// <param name="Value">String value.</param>
internal sealed record TokenString(LexContext Context, int TextPosition, string Text, string Value) : IToken
{
    public override string ToString()
    {
        return Text;
    }
}

/// <summary>
/// Token representing a simple function call in code.
/// </summary>
/// <param name="Text">Verbatim text used for the function identifier.</param>
internal sealed record TokenFunction(LexContext Context, int TextPosition, string Text, IBuiltinFunction? BuiltinFunction) : IToken
{
    public override string ToString()
    {
        return Text;
    }
}

/// <summary>
/// Token representing a simple variable in code.
/// </summary>
internal sealed record TokenVariable : IToken
{
    /// <inheritdoc/>
    public LexContext Context { get; }

    /// <inheritdoc/>
    public int TextPosition { get; }

    /// <summary>
    /// Verbatim text used for the variable identifier.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Builtin variable associated with the identifier, if one exists.
    /// </summary>
    public IBuiltinVariable? BuiltinVariable { get; }

    public TokenVariable(LexContext context, int textPosition, string text, IBuiltinVariable? builtinVariable)
    {
        Context = context;
        TextPosition = textPosition;
        Text = text;
        BuiltinVariable = builtinVariable;
    }

    public TokenVariable(TokenAssetReference assetReference)
    {
        Context = assetReference.Context;
        TextPosition = assetReference.TextPosition;
        Text = assetReference.Text;
        BuiltinVariable = Context.CompileContext.GameContext.Builtins.LookupBuiltinVariable(assetReference.Text);
    }

    public TokenVariable(TokenNumber number)
    {
        Context = number.Context;
        TextPosition = number.TextPosition;
        Text = number.Text;
        BuiltinVariable = Context.CompileContext.GameContext.Builtins.LookupBuiltinVariable(number.Text);
    }

    public TokenVariable(TokenString str)
    {
        Context = str.Context;
        TextPosition = str.TextPosition;
        Text = str.Value;
        BuiltinVariable = Context.CompileContext.GameContext.Builtins.LookupBuiltinVariable(str.Value);
    }

    public TokenVariable(TokenKeyword keyword)
    {
        Context = keyword.Context;
        TextPosition = keyword.TextPosition;
        Text = keyword.ToString();
        BuiltinVariable = Context.CompileContext.GameContext.Builtins.LookupBuiltinVariable(Text);
    }

    public override string ToString()
    {
        return Text;
    }
}

/// <summary>
/// Token representing an asset name reference in code.
/// </summary>
/// <param name="Text">Verbatim text used for the asset reference identifier.</param>
internal sealed record TokenAssetReference(LexContext Context, int TextPosition, string Text, int AssetId, bool IsRoomInstanceAsset) : IToken
{
    public override string ToString()
    {
        return Text;
    }
}