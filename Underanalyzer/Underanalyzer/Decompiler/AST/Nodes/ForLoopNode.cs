/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

namespace Underanalyzer.Decompiler.AST;

/// <summary>
/// Represents a for loop in the AST.
/// </summary>
public class ForLoopNode(IStatementNode? initializer, IExpressionNode? condition, BlockNode? incrementor, BlockNode body) 
    : IStatementNode, IBlockCleanupNode
{
    /// <summary>
    /// The initialization statement before the loop, or <see langword="null"/> if none.
    /// </summary>
    public IStatementNode? Initializer { get; internal set; } = initializer;

    /// <summary>
    /// The condition of the loop.
    /// </summary>
    public IExpressionNode? Condition { get; private set; } = condition;

    /// <summary>
    /// The code executed between iterations of the loop.
    /// </summary>
    public BlockNode? Incrementor { get; private set; } = incrementor;

    /// <summary>
    /// The main block of the loop.
    /// </summary>
    public BlockNode Body { get; private set; } = body;

    public bool SemicolonAfter { get => false; }
    public bool EmptyLineBefore { get; internal set; }
    public bool EmptyLineAfter { get; internal set; }

    public IStatementNode Clean(ASTCleaner cleaner)
    {
        Initializer = Initializer?.Clean(cleaner);
        Condition = Condition!.Clean(cleaner);
        Condition.Group = false;
        Incrementor?.Clean(cleaner);

        ElseToContinueCleanup.Clean(cleaner, Body);
        Body.Clean(cleaner);

        IStatementNode res = this;

        // Check if we're a for (;;) loop
        if (Condition is Int64Node i64 && i64.Value == 1 && Incrementor is { Children: [] })
        {
            // We have no condition or incrementor, so rewrite this as for (;;)
            Condition = null;
            Incrementor = null;

            if (Initializer is not null && (Initializer is not BlockNode || Initializer is BlockNode block && block.Children is not []))
            {
                // Move initializer above loop
                BlockNode newBlock = new(cleaner.TopFragmentContext!);
                newBlock.Children.Add(Initializer);
                newBlock.Children.Add(this);
                res = newBlock;
            }

            Initializer = null;
        }

        EmptyLineAfter = EmptyLineBefore = cleaner.Context.Settings.EmptyLineAroundBranchStatements;

        return res;
    }

    public int BlockClean(ASTCleaner cleaner, BlockNode block, int i)
    {
        // Check if this for loop needs an initializer, and if so (and there's a readable one), add it
        if (Initializer is null && i > 0 && block.Children[i - 1] is AssignNode assign &&
            assign.Value is (Int16Node or Int32Node or Int64Node or VariableNode) &&
            Condition is not null)
        {
            Initializer = assign;
            block.Children.RemoveAt(i - 1);
            block.Children[i - 1] = Clean(cleaner);

            return i - 1;
        }

        return i;
    }

    public void Print(ASTPrinter printer)
    {
        printer.Write("for (");
        if (Condition is null && Incrementor is null)
        {
            if (Initializer is not null)
            {
                throw new DecompilerException("Expected initializer to be null in for (;;) loop");
            }
            printer.Write(";;");
        }
        else
        {
            Initializer?.Print(printer);
            printer.Write("; ");
            Condition!.Print(printer);
            if (Incrementor is not null)
            {
                printer.Write("; ");
                Incrementor.GetShortestStatement().Print(printer);
            }
            else
            {
                printer.Write(';');
            }
        }
        printer.Write(')');

        if (printer.Context.Settings.RemoveSingleLineBlockBraces && !Body.RequiresMultipleLines(printer))
        {
            Body.PrintSingleLine(printer);
        }
        else
        {
            if (printer.Context.Settings.OpenBlockBraceOnSameLine)
            {
                printer.Write(' ');
            }
            Body.Print(printer);
        }
    }

    public bool RequiresMultipleLines(ASTPrinter printer)
    {
        return true;
    }
}
