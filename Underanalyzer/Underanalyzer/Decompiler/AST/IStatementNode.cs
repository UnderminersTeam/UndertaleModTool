/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

namespace Underanalyzer.Decompiler.AST;

/// <summary>
/// Base interface for all statements in the AST.
/// </summary>
public interface IStatementNode : IASTNode<IStatementNode>
{
    /// <summary>
    /// When semicolons are enabled, this is true if the statement should have a semicolon printed after it.
    /// </summary>
    public bool SemicolonAfter { get; }

    /// <summary>
    /// If true, an empty line should be printed before this node, unless at the start of a block.
    /// </summary>
    public bool EmptyLineBefore { get; set; }

    /// <summary>
    /// If true, an empty line should be printed after this node, unless at the end of a block.
    /// </summary>
    public bool EmptyLineAfter { get; set; }
}
