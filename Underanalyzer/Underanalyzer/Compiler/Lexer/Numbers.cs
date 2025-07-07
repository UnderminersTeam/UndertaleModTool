/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System;
using System.Globalization;

namespace Underanalyzer.Compiler.Lexer;

/// <summary>
/// Helper for number and hex literal lexing operations.
/// </summary>
internal static class Numbers
{
    /// <summary>
    /// Parses a decimal number from the given text position.
    /// </summary>
    public static int ParseDecimal(LexContext context, int startPosition)
    {
        int pos = ContiguousTextReader.ReadWhileNumber(context.Text, startPosition, out ReadOnlySpan<char> number);

        // Parse number
        if (long.TryParse(number, NumberStyles.Integer, CultureInfo.InvariantCulture, out long intValue))
        {
            // Check if the integer can be represented losslessly as a double
            if ((long)(double)intValue == intValue)
            {
                context.Tokens.Add(new TokenNumber(context, startPosition, number.ToString(), intValue));
            }
            else
            {
                context.Tokens.Add(new TokenInt64(context, startPosition, number.ToString(), intValue));
            }
        }
        else if (double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out double floatValue))
        {
            context.Tokens.Add(new TokenNumber(context, startPosition, number.ToString(), floatValue));
        }
        else
        {
            context.CompileContext.PushError($"Invalid number \"{number.ToString()}\"", context, startPosition);
        }

        return pos;
    }

    /// <summary>
    /// Parses a hex literal from the given text position.
    /// </summary>
    public static int ParseHex(LexContext context, int startPosition, bool dollarSignSyntax)
    {
        string prefix = dollarSignSyntax ? "$" : "0x";
        int pos = ContiguousTextReader.ReadWhileHex(context.Text, startPosition + (dollarSignSyntax ? 1 : 2), out ReadOnlySpan<char> hex);
        
        // Parse hex as a number
        if (long.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out long value))
        {
            if (value >= int.MinValue && value <= int.MaxValue)
            {
                context.Tokens.Add(new TokenNumber(context, startPosition, $"{prefix}{hex.ToString()}", value));
            }
            else
            {
                context.Tokens.Add(new TokenInt64(context, startPosition, $"{prefix}{hex.ToString()}", value));
            }
        }
        else
        {
            context.CompileContext.PushError("Invalid hex literal", context, startPosition);
        }

        return pos;
    }
}
