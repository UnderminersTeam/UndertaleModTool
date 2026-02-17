/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

namespace Underanalyzer.Decompiler.AST;

/// <summary>
/// Base node for values that have a constant value.
/// </summary>
public interface IConstantNode<T> : IExpressionNode
{
    /// <summary>
    /// The constant value of this node.
    /// </summary>
    public T Value { get; }
}
