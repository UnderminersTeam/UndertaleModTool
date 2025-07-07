/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System;
using System.Globalization;
using System.Text;

namespace Underanalyzer.Compiler.Lexer;

/// <summary>
/// Helper for tags (preprocessor, color literals) lexing operations.
/// </summary>
internal static class Tags
{
    /// <summary>
    /// Parses preprocessors and/or color literals from the given text position, returning the end position.
    /// </summary>
    public static int Parse(LexContext context, int startPosition)
    {
        ReadOnlySpan<char> text = context.Text;

        // Read directive type
        int pos = ContiguousTextReader.ReadWhileIdentifier(text, startPosition + 1, out ReadOnlySpan<char> directiveType);

        // Read specific types
        switch (directiveType)
        {
            case "macro":
                {
                    // Ensure the code is not recursively defining macros
                    if (context.MacroName is not null)
                    {
                        context.CompileContext.PushError($"Invalid #macro syntax found", context, startPosition);
                        break;
                    }

                    // Parse macro name
                    pos = ContiguousTextReader.ReadUntilWhitespace(text, pos + 1, out ReadOnlySpan<char> macroName);

                    // Parse macro content until newline (allowing for escapes to get multiple lines)
                    pos++;
                    StringBuilder macroContent = new(64);
                    while (pos < text.Length && text[pos] != '\n')
                    {
                        char curr = text[pos];
                        if (curr == '\\' && pos + 1 < text.Length)
                        {
                            // Escaped; ignore whitespace through to the end of the line
                            int backslashPos = pos++;
                            do
                            {
                                curr = text[pos++];
                            }
                            while (pos < text.Length && char.IsWhiteSpace(curr) && curr != '\n');

                            if (curr == '\n')
                            {
                                // Found a newline - ignore it and keep reading the macro
                                continue;
                            }
                            else
                            {
                                // No more whitespace, but also no newline, so backtrack and
                                // assume the backslash is part of the macro's code.
                                pos = backslashPos + 1;
                                macroContent.Append('\\');
                                continue;
                            }
                        }
                        macroContent.Append(curr);
                        pos++;
                    }

                    // Try to add the macro
                    string macroNameStr = macroName.ToString();
                    LexContext newLexContext = new(context.CompileContext, macroContent.ToString(), macroNameStr);
                    Macro newMacro = new(newLexContext, macroNameStr);
                    if (!context.CompileContext.Macros.TryAdd(macroNameStr, newMacro))
                    {
                        // The macro was already defined!
                        context.CompileContext.PushError($"Duplicate macro \"{macroNameStr}\" found", context, startPosition);
                    }
                    else
                    {
                        // Tokenize macro contents
                        newLexContext.Tokenize();
                    }
                    break;
                }
            case "region":
            case "endregion":
                // Skip regions, as they have no effect on compilation
                while (pos < text.Length && text[pos] != '\n')
                {
                    pos++;
                }
                break;
            default:
                // Attempt to parse an RGB color literal
                if (directiveType.Length is 6 or 8)
                {
                    ReadOnlySpan<char> color = directiveType;

                    // Ensure all characters are valid hex
                    bool valid = true;
                    foreach (char hex in color)
                    {
                        if ((hex < '0' || hex > '9') && (hex < 'A' || hex > 'F') && (hex < 'a' || hex > 'f'))
                        {
                            valid = false;
                            break;
                        }
                    }

                    // If valid hex, proceed with parsing a RGB literal token
                    if (valid)
                    {
                        // Convert RGB to BGR and parse
                        Span<char> converted = stackalloc char[8];
                        converted[0] = color[4];
                        converted[1] = color[5];
                        converted[2] = color[2];
                        converted[3] = color[3];
                        converted[4] = color[0];
                        converted[5] = color[1];
                        if (color.Length == 8)
                        {
                            converted[6] = color[6];
                            converted[7] = color[7];
                        }
                        if (long.TryParse(converted[..color.Length], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out long value))
                        {
                            if (value >= int.MinValue && value <= int.MaxValue)
                            {
                                context.Tokens.Add(new TokenNumber(context, startPosition, $"#{color.ToString()}", value));
                            }
                            else
                            {
                                context.Tokens.Add(new TokenInt64(context, startPosition, $"#{color.ToString()}", value));
                            }
                            return pos;
                        }
                    }
                }

                // Failed to recognize any tag or directive
                context.CompileContext.PushError($"Unrecognized tag or directive \"{directiveType.ToString()}\"", context, startPosition);
                break;
        }

        return pos;
    }
}
