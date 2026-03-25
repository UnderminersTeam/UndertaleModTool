/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;

namespace Underanalyzer.Decompiler.AST;

/// <summary>
/// Represents a local variable declaration statement for the enclosing block.
/// </summary>
public class BlockLocalVarDeclNode : IStatementNode
{
    /// <inheritdoc/>
    public bool SemicolonAfter => true;

    /// <inheritdoc/>
    public bool EmptyLineAfter { get; set; }

    /// <inheritdoc/>
    public bool EmptyLineBefore { get => false; set => _ = value; }

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
        List<string> localNames = printer.TopFragmentContext!.LocalVariableNamesList;
        if (localNames.Count > 0)
        {
            printer.Write("var ");
            for (int i = 0; i < localNames.Count; i++)
            {
                printer.Write(localNames[i]);
                if (i != localNames.Count - 1)
                {
                    printer.Write(", ");
                }
            }
        }
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
