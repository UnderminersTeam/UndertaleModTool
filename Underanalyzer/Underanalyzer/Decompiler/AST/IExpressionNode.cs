/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

namespace Underanalyzer.Decompiler.AST;

/// <summary>
/// Base interface for all expressions in the AST.
/// </summary>
public interface IExpressionNode : IASTNode<IExpressionNode>
{
    /// <summary>
    /// If true, this node was duplicated during simulation.
    /// </summary>
    public bool Duplicated { get; internal set; }

    /// <summary>
    /// Whether or not this expression has to be separately grouped with parentheses.
    /// </summary>
    public bool Group { get; internal set; }

    /// <summary>
    /// The data type assigned to this node on the simulated VM stack.
    /// </summary>
    public IGMInstruction.DataType StackType { get; internal set; }
}

/// <summary>
/// Interface for expression nodes that have multi-token parts (e.g., can have spaces, usually),
/// and thus should have parentheses around them if used in certain expressions.
/// </summary>
public interface IMultiExpressionNode : IExpressionNode
{
}