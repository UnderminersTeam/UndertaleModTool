/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using Underanalyzer.Decompiler.GameSpecific;

namespace Underanalyzer.Decompiler.AST;

/// <summary>
/// Interface for all nodes that can be (in whole or in part) resolved as a specific macro type.
/// When anything at all is resolved, a new copy of the entire node is returned with the resolutions.
/// </summary>
public interface IMacroResolvableNode : IExpressionNode
{
    /// <summary>
    /// Returns the node, but with macros resolved using the given macro type.
    /// If any modifications are made, this should return a reference; otherwise, <see langword="null"/>.
    /// </summary>
    public IExpressionNode? ResolveMacroType(ASTCleaner cleaner, IMacroType type);
}
