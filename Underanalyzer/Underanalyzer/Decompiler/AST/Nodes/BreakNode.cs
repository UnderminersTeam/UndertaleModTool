/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;

namespace Underanalyzer.Decompiler.AST;

/// <summary>
/// Represents a "break" statement in the AST.
/// </summary>
public class BreakNode : IStatementNode
{
    /// <inheritdoc/>
    public bool SemicolonAfter => true;

    /// <inheritdoc/>
    public bool EmptyLineBefore { get => false; set => _ = value; }

    /// <inheritdoc/>
    public bool EmptyLineAfter { get => false; set => _ = value; }

    /// <inheritdoc/>
    public IStatementNode Clean(ASTCleaner cleaner)
    {
        return this;
    }

    /// <inheritdoc/>
    public IStatementNode PostClean(ASTCleaner cleaner)
    {
        return this;
    }

    /// <inheritdoc/>
    public void Print(ASTPrinter printer)
    {
        printer.Write("break");
    }

    /// <inheritdoc/>
    public bool RequiresMultipleLines(ASTPrinter printer)
    {
        return false;
    }

    /// <inheritdoc/>
    public IEnumerable<IBaseASTNode> EnumerateChildren()
    {
        return [];
    }
}
