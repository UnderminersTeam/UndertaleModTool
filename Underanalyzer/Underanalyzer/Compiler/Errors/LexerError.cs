/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using Underanalyzer.Compiler.Lexer;

namespace Underanalyzer.Compiler.Errors;

/// <summary>
/// Represents an error that occurred when lexing code.
/// </summary>
public sealed class LexerError : ICompileError
{
    /// <inheritdoc/>
    public string BaseMessage { get; }

    private readonly LexContext _lexContext;
    private readonly int _textPosition; 

    /// <summary>
    /// Internal constructor for lexer errors. Takes the basic message, 
    /// the context the error occurred in, and the text position it occurred at.
    /// </summary>
    internal LexerError(string baseMessage, LexContext context, int textPosition)
    {
        BaseMessage = baseMessage;
        _lexContext = context;
        _textPosition = textPosition;
    }

    /// <inheritdoc/>
    public string GenerateMessage()
    {
        (int line, int column) = _lexContext.GetLineAndColumnFromPos(_textPosition);
        if (_lexContext.MacroName is string macroName)
        {
            return $"{BaseMessage} on line {line}, column {column} of macro \"{macroName}\"";
        }
        return $"{BaseMessage} on line {line}, column {column}";
    }
}
