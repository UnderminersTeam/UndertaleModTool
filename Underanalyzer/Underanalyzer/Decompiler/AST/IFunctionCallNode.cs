/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;

namespace Underanalyzer.Decompiler.AST;

/// <summary>
/// Base interface for all nodes that call a function in the AST.
/// </summary>
public interface IFunctionCallNode : IConditionalValueNode, IStatementNode, IExpressionNode
{
    /// <summary>
    /// Name of the function being called, or <see langword="null"/> if none.
    /// </summary>
    public string? FunctionName { get; }

    /// <summary>
    /// List of arguments used to call the function with.
    /// </summary>
    public List<IExpressionNode> Arguments { get; }
}
