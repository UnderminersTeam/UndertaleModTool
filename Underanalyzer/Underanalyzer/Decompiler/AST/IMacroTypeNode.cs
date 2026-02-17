/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using Underanalyzer.Decompiler.GameSpecific;

namespace Underanalyzer.Decompiler.AST;

/// <summary>
/// Interface for nodes that can have defined macro types when used in an expression.
/// </summary>
public interface IMacroTypeNode
{
    /// <summary>
    /// Returns the macro type for this node as used in an expression, or <see langword="null"/> if none exists.
    /// </summary>
    public IMacroType? GetExpressionMacroType(ASTCleaner cleaner);
}
