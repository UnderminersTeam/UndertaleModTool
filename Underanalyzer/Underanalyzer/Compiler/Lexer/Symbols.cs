/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

namespace Underanalyzer.Compiler.Lexer;

/// <summary>
/// Helper for symbol (operators and separators) lexing operations.
/// </summary>
internal static class Symbols
{
    /// <summary>
    /// Parses a symbol from the given text position.
    /// </summary>
    public static int Parse(LexContext context, int startPosition, char currChar, char nextChar, out bool success)
    {
        success = true;

        switch (currChar)
        {
            case '{':
                context.Tokens.Add(new TokenSeparator(context, startPosition, SeparatorKind.BlockOpen));
                return startPosition + 1;
            case '}':
                context.Tokens.Add(new TokenSeparator(context, startPosition, SeparatorKind.BlockClose));
                return startPosition + 1;
            case '(':
                context.Tokens.Add(new TokenSeparator(context, startPosition, SeparatorKind.GroupOpen));
                return startPosition + 1;
            case ')':
                context.Tokens.Add(new TokenSeparator(context, startPosition, SeparatorKind.GroupClose));
                return startPosition + 1;
            case ',':
                context.Tokens.Add(new TokenSeparator(context, startPosition, SeparatorKind.Comma));
                return startPosition + 1;
            case '.':
                context.Tokens.Add(new TokenSeparator(context, startPosition, SeparatorKind.Dot));
                return startPosition + 1;
            case ':':
                if (nextChar == '=')
                {
                    context.Tokens.Add(new TokenOperator(context, startPosition, OperatorKind.Assign2));
                    return startPosition + 2;
                }
                context.Tokens.Add(new TokenSeparator(context, startPosition, SeparatorKind.Colon));
                return startPosition + 1;
            case ';':
                context.Tokens.Add(new TokenSeparator(context, startPosition, SeparatorKind.Semicolon));
                return startPosition + 1;
            case '[':
                switch (nextChar)
                {
                    case '|':
                        context.Tokens.Add(new TokenSeparator(context, startPosition, SeparatorKind.ArrayOpenList));
                        return startPosition + 2;
                    case '?':
                        context.Tokens.Add(new TokenSeparator(context, startPosition, SeparatorKind.ArrayOpenMap));
                        return startPosition + 2;
                    case '#':
                        context.Tokens.Add(new TokenSeparator(context, startPosition, SeparatorKind.ArrayOpenGrid));
                        return startPosition + 2;
                    case '@':
                        context.Tokens.Add(new TokenSeparator(context, startPosition, SeparatorKind.ArrayOpenDirect));
                        return startPosition + 2;
                    case '$':
                        context.Tokens.Add(new TokenSeparator(context, startPosition, SeparatorKind.ArrayOpenStruct));
                        return startPosition + 2;
                }
                context.Tokens.Add(new TokenSeparator(context, startPosition, SeparatorKind.ArrayOpen));
                return startPosition + 1;
            case ']':
                context.Tokens.Add(new TokenSeparator(context, startPosition, SeparatorKind.ArrayClose));
                return startPosition + 1;

            case '=':
                if (nextChar == '=')
                {
                    context.Tokens.Add(new TokenOperator(context, startPosition, OperatorKind.CompareEqual));
                    return startPosition + 2;
                }
                context.Tokens.Add(new TokenOperator(context, startPosition, OperatorKind.Assign));
                return startPosition + 1;
            case '+':
                if (nextChar == '+')
                {
                    context.Tokens.Add(new TokenOperator(context, startPosition, OperatorKind.Increment));
                    return startPosition + 2;
                }
                if (nextChar == '=')
                {
                    context.Tokens.Add(new TokenOperator(context, startPosition, OperatorKind.CompoundPlus));
                    return startPosition + 2;
                }
                context.Tokens.Add(new TokenOperator(context, startPosition, OperatorKind.Plus));
                return startPosition + 1;
            case '-':
                if (nextChar == '-')
                {
                    context.Tokens.Add(new TokenOperator(context, startPosition, OperatorKind.Decrement));
                    return startPosition + 2;
                }
                if (nextChar == '=')
                {
                    context.Tokens.Add(new TokenOperator(context, startPosition, OperatorKind.CompoundMinus));
                    return startPosition + 2;
                }
                context.Tokens.Add(new TokenOperator(context, startPosition, OperatorKind.Minus));
                return startPosition + 1;
            case '*':
                if (nextChar == '=')
                {
                    context.Tokens.Add(new TokenOperator(context, startPosition, OperatorKind.CompoundTimes));
                    return startPosition + 2;
                }
                context.Tokens.Add(new TokenOperator(context, startPosition, OperatorKind.Times));
                return startPosition + 1;
            case '/':
                if (nextChar == '=')
                {
                    context.Tokens.Add(new TokenOperator(context, startPosition, OperatorKind.CompoundDivide));
                    return startPosition + 2;
                }
                context.Tokens.Add(new TokenOperator(context, startPosition, OperatorKind.Divide));
                return startPosition + 1;
            case '!':
                if (nextChar == '=')
                {
                    context.Tokens.Add(new TokenOperator(context, startPosition, OperatorKind.CompareNotEqual));
                    return startPosition + 2;
                }
                context.Tokens.Add(new TokenOperator(context, startPosition, OperatorKind.Not));
                return startPosition + 1;
            case '<':
                switch (nextChar)
                {
                    case '=':
                        context.Tokens.Add(new TokenOperator(context, startPosition, OperatorKind.CompareLesserEqual));
                        return startPosition + 2;
                    case '<':
                        context.Tokens.Add(new TokenOperator(context, startPosition, OperatorKind.BitwiseShiftLeft));
                        return startPosition + 2;
                    case '>':
                        context.Tokens.Add(new TokenOperator(context, startPosition, OperatorKind.CompareNotEqual2));
                        return startPosition + 2;
                }
                context.Tokens.Add(new TokenOperator(context, startPosition, OperatorKind.CompareLesser));
                return startPosition + 1;
            case '>':
                switch (nextChar)
                {
                    case '=':
                        context.Tokens.Add(new TokenOperator(context, startPosition, OperatorKind.CompareGreaterEqual));
                        return startPosition + 2;
                    case '>':
                        context.Tokens.Add(new TokenOperator(context, startPosition, OperatorKind.BitwiseShiftRight));
                        return startPosition + 2;
                }
                context.Tokens.Add(new TokenOperator(context, startPosition, OperatorKind.CompareGreater));
                return startPosition + 1;
            case '?':
                if (nextChar == '?')
                {
                    if (startPosition + 2 < context.Text.Length && context.Text[startPosition + 2] == '=')
                    {
                        context.Tokens.Add(new TokenOperator(context, startPosition, OperatorKind.CompoundNullishCoalesce));
                        return startPosition + 3;
                    }
                    context.Tokens.Add(new TokenOperator(context, startPosition, OperatorKind.NullishCoalesce));
                    return startPosition + 2;
                }
                context.Tokens.Add(new TokenOperator(context, startPosition, OperatorKind.Conditional));
                return startPosition + 1;
            case '%':
                if (nextChar == '=')
                {
                    context.Tokens.Add(new TokenOperator(context, startPosition, OperatorKind.CompoundMod));
                    return startPosition + 2;
                }
                context.Tokens.Add(new TokenOperator(context, startPosition, OperatorKind.Mod));
                return startPosition + 1;
            case '&':
                if (nextChar == '&')
                {
                    context.Tokens.Add(new TokenOperator(context, startPosition, OperatorKind.LogicalAnd));
                    return startPosition + 2;
                }
                if (nextChar == '=')
                {
                    context.Tokens.Add(new TokenOperator(context, startPosition, OperatorKind.CompoundBitwiseAnd));
                    return startPosition + 2;
                }
                context.Tokens.Add(new TokenOperator(context, startPosition, OperatorKind.BitwiseAnd));
                return startPosition + 1;
            case '|':
                if (nextChar == '|')
                {
                    context.Tokens.Add(new TokenOperator(context, startPosition, OperatorKind.LogicalOr));
                    return startPosition + 2;
                }
                if (nextChar == '=')
                {
                    context.Tokens.Add(new TokenOperator(context, startPosition, OperatorKind.CompoundBitwiseOr));
                    return startPosition + 2;
                }
                context.Tokens.Add(new TokenOperator(context, startPosition, OperatorKind.BitwiseOr));
                return startPosition + 1;
            case '^':
                if (nextChar == '^')
                {
                    context.Tokens.Add(new TokenOperator(context, startPosition, OperatorKind.LogicalXor));
                    return startPosition + 2;
                }
                if (nextChar == '=')
                {
                    context.Tokens.Add(new TokenOperator(context, startPosition, OperatorKind.CompoundBitwiseXor));
                    return startPosition + 2;
                }
                context.Tokens.Add(new TokenOperator(context, startPosition, OperatorKind.BitwiseXor));
                return startPosition + 1;
            case '~':
                context.Tokens.Add(new TokenOperator(context, startPosition, OperatorKind.BitwiseNegate));
                return startPosition + 1;
        }

        success = false;
        return startPosition + 1;
    }
}
