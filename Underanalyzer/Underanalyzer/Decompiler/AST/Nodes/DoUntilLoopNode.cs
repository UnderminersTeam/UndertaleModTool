/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;

namespace Underanalyzer.Decompiler.AST;

/// <summary>
/// Represents a do..until loop in the AST.
/// </summary>
public class DoUntilLoopNode(BlockNode body, IExpressionNode condition) : IStatementNode
{
    /// <summary>
    /// The main block of the loop.
    /// </summary>
    public BlockNode Body { get; private set; } = body;

    /// <summary>
    /// The condition of the loop.
    /// </summary>
    public IExpressionNode Condition { get; private set; } = condition;

    /// <inheritdoc/>
    public bool SemicolonAfter => true;

    /// <inheritdoc/>
    public bool EmptyLineBefore { get; set; }

    /// <inheritdoc/>
    public bool EmptyLineAfter { get; set; }

    /// <inheritdoc/>
    public IStatementNode Clean(ASTCleaner cleaner)
    {
        ElseToContinueCleanup.Clean(cleaner, Body);
        Body.Clean(cleaner);
        Condition = Condition.Clean(cleaner);
        Condition.Group = false;

        EmptyLineAfter = EmptyLineBefore = cleaner.Context.Settings.EmptyLineAroundBranchStatements;

        return this;
    }

    /// <inheritdoc/>
    public IStatementNode PostClean(ASTCleaner cleaner)
    {
        cleaner.TopFragmentContext!.PushLocalScope(cleaner.Context, cleaner.TopFragmentContext!.CurrentPostCleanupBlock!, this);
        Body.PostClean(cleaner);
        cleaner.TopFragmentContext!.PopLocalScope(cleaner.Context);

        Condition = Condition.PostClean(cleaner);
        return this;
    }

    /// <inheritdoc/>
    public void Print(ASTPrinter printer)
    {
        printer.Write("do");
        if (printer.Context.Settings.RemoveSingleLineBlockBraces && !Body.RequiresMultipleLines(printer))
        {
            Body.PrintSingleLine(printer);
            printer.EndLine();
            printer.StartLine();
        }
        else
        {
            if (printer.Context.Settings.OpenBlockBraceOnSameLine)
            {
                printer.Write(' ');
                Body.Print(printer);
                printer.Write(' ');
            }
            else
            {
                Body.Print(printer);
                printer.EndLine();
                printer.StartLine();
            }
        }
        printer.Write("until (");
        Condition.Print(printer);
        printer.Write(')');
    }

    /// <inheritdoc/>
    public bool RequiresMultipleLines(ASTPrinter printer)
    {
        return true;
    }

    /// <inheritdoc/>
    public IEnumerable<IBaseASTNode> EnumerateChildren()
    {
        yield return Body;
        yield return Condition;
    }
}
