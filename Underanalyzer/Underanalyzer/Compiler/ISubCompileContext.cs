/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

namespace Underanalyzer.Compiler;

/// <summary>
/// Interface for sub-contexts of a compile context.
/// </summary>
internal interface ISubCompileContext
{
    /// <summary>
    /// The compile context for the overarching code entry.
    /// </summary>
    public CompileContext CompileContext { get; }

    /// <summary>
    /// Current function scope being used during parsing and bytecode generation.
    /// </summary>
    public FunctionScope CurrentScope { get; set; }

    /// <summary>
    /// Root function scope being used during parsing and bytecode generation.
    /// </summary>
    public FunctionScope RootScope { get; set; }
}
