/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System;

namespace Underanalyzer.Compiler.Lexer;

/// <summary>
/// Helper for whitespace lexing operations.
/// </summary>
internal static class Whitespace
{
    /// <summary>
    /// Skips whitespace and comments starting at the provided <paramref name="startPosition"/> within <paramref name="text"/>,
    /// returning the end position of the whitespace.
    /// </summary>
    public static int Skip(ReadOnlySpan<char> text, int startPosition)
    {
        int pos = startPosition;

        // Skip whitespace until there is no more
        while (true)
        {
            // Skip simple whitespace characters
            while (pos < text.Length && char.IsWhiteSpace(text[pos]))
            {
                pos++;
            }

            // Skip comments
            if (pos + 1 < text.Length && text[pos] == '/' && (text[pos + 1] == '/' || text[pos + 1] == '*'))
            {
                switch (text[pos + 1])
                {
                    case '/':
                        // Single-line comment: skip until newline
                        pos += 2;
                        while (pos < text.Length)
                        {
                            if (text[pos++] == '\n')
                            {
                                break;
                            }
                        }
                        break;
                    case '*':
                        // Multi-line comment: skip until end sequence (or end of text)
                        pos += 2;
                        while (pos < text.Length)
                        {
                            if (text[pos++] == '*')
                            {
                                if (pos < text.Length && text[pos] == '/')
                                {
                                    pos++;
                                    break;
                                }
                            }
                        }
                        break;
                }
            }
            else
            {
                // End of text, or not a comment
                break;
            }
        }

        return pos;
    }
}
