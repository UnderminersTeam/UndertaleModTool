/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

namespace Underanalyzer.Decompiler.AST;

/// <summary>
/// Represents a switch case (or default case) in the AST.
/// </summary>
public class SwitchCaseNode(IExpressionNode? expression) : IStatementNode, IBlockCleanupNode
{
    /// <summary>
    /// The case expression, or <see langword="null"/> if default.
    /// </summary>
    public IExpressionNode? Expression { get; internal set; } = expression;

    public bool SemicolonAfter => false;
    public bool EmptyLineBefore { get; private set; }
    public bool EmptyLineAfter { get; private set; }

    public IStatementNode Clean(ASTCleaner cleaner)
    {
        Expression = Expression?.Clean(cleaner);
        return this;
    }

    public int BlockClean(ASTCleaner cleaner, BlockNode block, int i)
    {
        if (cleaner.Context.Settings.EmptyLineBeforeSwitchCases)
        {
            if (i > 0 && block.Children[i - 1] is not SwitchCaseNode)
            {
                EmptyLineBefore = true;
            }
        }
        if (cleaner.Context.Settings.EmptyLineAfterSwitchCases)
        {
            if (i < block.Children.Count - 1 && block.Children[i + 1] is not SwitchCaseNode)
            {
                EmptyLineAfter = true;
            }
        }

        return i;
    }

    public void Print(ASTPrinter printer)
    {
        if (Expression is not null)
        {
            printer.Write("case ");
            Expression.Print(printer);
            printer.Write(':');
        }
        else
        {
            printer.Write("default:");
        }
    }

    public bool RequiresMultipleLines(ASTPrinter printer)
    {
        return Expression?.RequiresMultipleLines(printer) ?? false;
    }
}
