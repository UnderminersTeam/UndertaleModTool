/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System;

namespace Underanalyzer.Compiler.Lexer;

/// <summary>
/// Helper class to read text faster than regular string building operations, when text is contiguous.
/// </summary>
internal static class ContiguousTextReader
{
    /// <summary>
    /// Reads text until reaching a whitespace, outputting a string containing the text, and returning its end position.
    /// </summary>
    public static int ReadUntilWhitespace(ReadOnlySpan<char> text, int startPosition, out ReadOnlySpan<char> output)
    {
        // Read until no longer identifier characters
        int pos = startPosition;
        while (pos < text.Length)
        {
            if (!char.IsWhiteSpace(text[pos]))
            {
                pos++;
            }
            else
            {
                // No longer identifier characters, so stop
                break;
            }
        }

        output = text[startPosition..pos];
        return pos;
    }

    /// <summary>
    /// Reads text until reaching a specific character, outputting a string containing the text, and returning its end position.
    /// </summary>
    public static int ReadUntilChar(ReadOnlySpan<char> text, int startPosition, char c, out ReadOnlySpan<char> output)
    {
        // Read until no longer identifier characters
        int pos = startPosition;
        while (pos < text.Length)
        {
            if (text[pos] != c)
            {
                pos++;
            }
            else
            {
                // No longer identifier characters, so stop
                break;
            }
        }

        output = text[startPosition..pos];
        return pos;
    }

    /// <summary>
    /// Reads identifier text until reaching its end, outputting a string containing the text, and returning its end position.
    /// </summary>
    public static int ReadWhileIdentifier(ReadOnlySpan<char> text, int startPosition, out ReadOnlySpan<char> output)
    {
        // Read until no longer identifier characters
        int pos = startPosition;
        while (pos < text.Length)
        {
            char c = text[pos];
            if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_')
            {
                pos++;
            }
            else
            {
                // No longer identifier characters, so stop
                break;
            }
        }

        output = text[startPosition..pos];
        return pos;
    }

    /// <summary>
    /// Reads number (decimal) text until reaching its end, outputting a string containing the text, and returning its end position.
    /// </summary>
    public static int ReadWhileNumber(ReadOnlySpan<char> text, int startPosition, out ReadOnlySpan<char> output)
    {
        // Read until no longer number characters
        int pos = startPosition;
        bool usedDot = false;
        while (pos < text.Length)
        {
            char c = text[pos];
            if (char.IsDigit(c) || (!usedDot && c == '.'))
            {
                usedDot |= c == '.';
                pos++;
            }
            else
            {
                // No longer number characters, so stop
                break;
            }
        }

        output = text[startPosition..pos];
        return pos;
    }

    /// <summary>
    /// Reads hex digit text until reaching its end, outputting a string containing the text, and returning its end position.
    /// </summary>
    public static int ReadWhileHex(ReadOnlySpan<char> text, int startPosition, out ReadOnlySpan<char> output)
    {
        // Read until no longer hex digit characters
        int pos = startPosition;
        while (pos < text.Length)
        {
            char c = text[pos];
            if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f'))
            {
                pos++;
            }
            else
            {
                // No longer hex digit characters, so stop
                break;
            }
        }

        output = text[startPosition..pos];
        return pos;
    }
}
