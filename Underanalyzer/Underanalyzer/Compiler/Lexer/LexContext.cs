/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;

namespace Underanalyzer.Compiler.Lexer;

/// <summary>
/// A context for the lexer, for overarching code entries, or for smaller parts like macros or string interpolation.
/// </summary>
internal sealed class LexContext : ISubCompileContext
{
    /// <inheritdoc/>
    public CompileContext CompileContext { get; }

    /// <inheritdoc/>
    public FunctionScope CurrentScope
    {
        get => throw new InvalidOperationException();
        set => throw new InvalidOperationException();
    }

    /// <inheritdoc/>
    public FunctionScope RootScope
    {
        get => throw new InvalidOperationException();
        set => throw new InvalidOperationException();
    }

    /// <summary>
    /// The text being processed by this context.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Macro name for this context, if part of a macro.
    /// </summary>
    public string? MacroName { get; }

    /// <summary>
    /// List of tokens produced by this context.
    /// </summary>
    public List<IToken> Tokens { get; } = new(128);

    /// <summary>
    /// Computed indices of lines in the text, used primarily for error messages.
    /// </summary>
    private List<int>? _lineIndices = null;

    public LexContext(CompileContext context, string text)
    {
        CompileContext = context;
        Text = text;
    }

    public LexContext(CompileContext context, string text, string macroName)
    {
        CompileContext = context;
        Text = text;
        MacroName = macroName;
    }

    /// <summary>
    /// Tokenizes the input text until reaching the end.
    /// </summary>
    public void Tokenize()
    {
        ReadOnlySpan<char> text = Text;
        int pos = 0;
        bool newStrings = CompileContext.GameContext.UsingGMS2OrLater;

        while (pos < text.Length)
        {
            // Skip whitespace to get to actual tokens
            pos = Whitespace.Skip(text, pos);

            // If at end of text, stop tokenizing
            if (pos >= text.Length)
            {
                break;
            }

            // If not at end of text, get next two characters (if available)
            char currChar = text[pos];
            char nextChar = (pos + 1 < text.Length) ? text[pos + 1] : '\0';

            // Delegate tokenizing based on these two characters
            if (currChar == '#' && nextChar != '\0')
            {
                pos = Tags.Parse(this, pos);
            }
            else if ((currChar >= 'a' && currChar <= 'z') || (currChar >= 'A' && currChar <= 'Z') || currChar == '_')
            {
                pos = Identifiers.Parse(this, pos);
            }
            else if (currChar == '$' || (currChar == '0' && nextChar == 'x'))
            {
                pos = Numbers.ParseHex(this, pos, currChar == '$');
            }
            else if (char.IsDigit(currChar) || (currChar == '.' && char.IsDigit(nextChar)))
            {
                pos = Numbers.ParseDecimal(this, pos);
            }
            else if (currChar == '@' && (nextChar == '"' || nextChar == '\'') && newStrings)
            {
                pos = Strings.ParseVerbatim(this, pos, '@');
            }
            else if (currChar == '"')
            {
                if (newStrings)
                {
                    pos = Strings.ParseRegular(this, pos);
                }
                else
                {
                    pos = Strings.ParseVerbatim(this, pos, '"');
                }
            }
            else if (currChar == '\'' && !newStrings)
            {
                pos = Strings.ParseVerbatim(this, pos, '\'');
            }
            else
            {
                pos = Symbols.Parse(this, pos, currChar, nextChar, out bool success);

                // If failed to match against anything, push an error
                if (!success)
                {
                    CompileContext.PushError("Unrecognized token", this, pos);
                }
            }
        }
    }

    /// <summary>
    /// Post-processes tokens, changing and inserting tokens as necessary.
    /// </summary>
    public void PostProcessTokens()
    {
        List<int> macroExpansionEnd = new(4);
        for (int i = 0; i < Tokens.Count; i++)
        {
            // Track end of macro expansions
            if (macroExpansionEnd.Count >= 128)
            {
                CompileContext.PushError("Macro expansion limit exceeded", this, Tokens[i].TextPosition);
                return;
            }
            while (macroExpansionEnd.Count > 0 && i >= macroExpansionEnd[^1])
            {
                macroExpansionEnd.RemoveAt(macroExpansionEnd.Count - 1);
            }

            // Post-process identifiers
            if (Tokens[i] is TokenIdentifier { Text: string text } identifier)
            {
                // Check if this identifier references a macro name
                if (CompileContext.Macros.TryGetValue(text, out Macro? macro))
                {
                    // Replace this token with the tokens of the macro
                    List<IToken> tokensToInsert = macro.LexContext.Tokens;
                    Tokens.RemoveAt(i);
                    Tokens.InsertRange(i, tokensToInsert);

                    if (tokensToInsert.Count > 0)
                    {
                        // Make sure to post-process the contents of the macro itself, now
                        i--;
                        
                        // Push existing macro expansion endings back by however many tokens we just added
                        for (int j = 0; j < macroExpansionEnd.Count; j++)
                        {
                            macroExpansionEnd[j] += tokensToInsert.Count - 1;
                        }

                        // Create a new macro expansion ending
                        macroExpansionEnd.Add(i + tokensToInsert.Count + 1);
                    }

                    continue;
                }

                // Check if this identifier is part of a simple function call
                if (i + 1 < Tokens.Count && Tokens[i + 1] is TokenSeparator { Kind: SeparatorKind.GroupOpen })
                {
                    // Lookup builtin function, and transform to function token
                    IBuiltinFunction? builtinFunction = CompileContext.GameContext.Builtins.LookupBuiltinFunction(text);
                    Tokens[i] = new TokenFunction(identifier.Context, identifier.TextPosition, identifier.Text, builtinFunction);
                    continue;
                }

                // Check if this identifier is a game asset (but not a script)
                bool isAsset = CompileContext.GameContext.GetAssetId(text, out int assetId);
                bool isRoomInstanceAsset = false;
                if (!isAsset)
                {
                    isRoomInstanceAsset = CompileContext.GameContext.GetRoomInstanceId(text, out assetId);
                }
                if ((isAsset || isRoomInstanceAsset) && !CompileContext.GameContext.GetScriptId(text, out _))
                {
                    Tokens[i] = new TokenAssetReference(identifier.Context, identifier.TextPosition, identifier.Text, assetId, isRoomInstanceAsset);
                    continue;
                }

                // Check if the identifier is true or false
                if (text is "true" or "false")
                {
                    Tokens[i] = new TokenBoolean(identifier.Context, identifier.TextPosition, text == "true");
                    continue;
                }

                // Check if the identifier is a builtin constant double
                if (CompileContext.GameContext.Builtins.LookupConstantDouble(text, out double constantDouble))
                {
                    Tokens[i] = new TokenNumber(identifier.Context, identifier.TextPosition, text, constantDouble, true);
                    continue;
                }

                // Otherwise, rewrite this identifier as a generic variable (but look up builtin variables as well)
                IBuiltinVariable? builtinVariable = CompileContext.GameContext.Builtins.LookupBuiltinVariable(text);
                CompileContext.GameContext.CodeBuilder.OnParseNameIdentifier(text);
                Tokens[i] = new TokenVariable(identifier.Context, identifier.TextPosition, text, builtinVariable);
                continue;
            }
        }
    }

    /// <summary>
    /// Takes in a text position, and returns the line and column numbers as a pair.
    /// </summary>
    public (int Line, int Column) GetLineAndColumnFromPos(int textPosition)
    {
        if (_lineIndices is null)
        {
            // Create line indices for the first time
            _lineIndices = new(128);

            // Add indices of each newline character
            ReadOnlySpan<char> text = Text;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n')
                {
                    _lineIndices.Add(i);
                }
            }
        }

        // Binary search for the line number
        int lineIndex = _lineIndices.BinarySearch(textPosition);
        if (lineIndex >= 0)
        {
            // Exact match of the newline character itself (this is rare).
            // We want to be one-indexed, so add one to the raw index.
            return (lineIndex + 1, 1);
        }

        // Usually, lineIndex will be negative, which is the bitwise complement of the next line index.
        lineIndex = ~lineIndex - 1;
        int column = textPosition - (lineIndex == -1 ? -1 : _lineIndices[lineIndex]) - 1;
        return (lineIndex + 2, column);
    }
}
