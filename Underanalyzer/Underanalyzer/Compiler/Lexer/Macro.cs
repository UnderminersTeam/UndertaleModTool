/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

namespace Underanalyzer.Compiler.Lexer;

/// <summary>
/// Declaration for a macro in the lexer.
/// </summary>
public sealed class Macro
{
    /// <summary>
    /// Name of the macro.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Lexing context for the macro.
    /// </summary>
    internal LexContext LexContext { get; }

    internal Macro(LexContext lexContext, string name)
    {
        Name = name;
        LexContext = lexContext;
    }
}
