/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using Underanalyzer.Compiler.Lexer;

namespace Underanalyzer.Compiler.Errors;

/// <summary>
/// Represents an error that occurred when parsing code into an AST.
/// </summary>
public sealed class ParserError : ICompileError
{
    /// <inheritdoc/>
    public string BaseMessage { get; }

    private readonly IToken? _nearbyToken;

    /// <summary>
    /// Internal constructor for parser errors. Takes a nearby token, or null if there is none.
    /// </summary>
    internal ParserError(string baseMessage, IToken? nearbyToken = null)
    {
        BaseMessage = baseMessage;
        _nearbyToken = nearbyToken;
    }

    /// <inheritdoc/>
    public string GenerateMessage()
    {
        if (_nearbyToken is IToken token)
        {
            (int line, int column) = token.Context.GetLineAndColumnFromPos(token.TextPosition);
            if (token.Context.MacroName is string macroName)
            {
                return $"{BaseMessage} around line {line}, column {column} of macro \"{macroName}\"";
            }
            return $"{BaseMessage} around line {line}, column {column}";
        }
        return BaseMessage;
    }
}
