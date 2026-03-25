/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

namespace Underanalyzer.Decompiler.AST;

/// <summary>
/// Interface for all AST nodes that can be used in conditional comparisons.
/// </summary>
public interface IConditionalValueNode : IMacroResolvableNode
{
    /// <summary>
    /// Type name, as used in conditional comparison operations.
    /// </summary>
    public string ConditionalTypeName { get; }

    /// <summary>
    /// String representation of the value of the node, if applicable, or an empty string.
    /// </summary>
    public string ConditionalValue { get; }
}
