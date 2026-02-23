/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;

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

    /// <inheritdoc/>
    public bool SemicolonAfter { get => false; }

    /// <inheritdoc/>
    public bool EmptyLineBefore { get; set; }

    /// <inheritdoc/>
    public bool EmptyLineAfter { get; set; }

    /// <inheritdoc/>
    public IStatementNode Clean(ASTCleaner cleaner)
    {
        IStatementNode res = this;

        // Clean up initializer, if we have one
        Initializer = Initializer?.Clean(cleaner);

        // Check if we're a for (;;) loop, or similar, for the condition
        if (Condition is Int64Node i64 && i64.Value == 1)
        {
            Condition = null;
        }

        // Remove incrementor if empty
        if (Incrementor is { Children: [] })
        {
            Incrementor = null;
        }

        // Clean up condition and incrementor, if we have them
        if (Condition is not null)
        {
            Condition = Condition.Clean(cleaner);
            Condition.Group = false;
        }
        Incrementor?.Clean(cleaner);

        // Clean up body
        ElseToContinueCleanup.Clean(cleaner, Body);
        Body.Clean(cleaner);

        EmptyLineAfter = EmptyLineBefore = cleaner.Context.Settings.EmptyLineAroundBranchStatements;

        return res;
    }

    /// <inheritdoc/>
    public int BlockClean(ASTCleaner cleaner, BlockNode block, int i)
    {
        // Check if this for loop needs an initializer, and if so (and there's a readable one), add it
        if (Initializer is null && i > 0 && block.Children[i - 1] is AssignNode assign &&
            assign.Value is (Int16Node or Int32Node or Int64Node or VariableNode) &&
            (Condition is not null || Incrementor is { Children: [.., AssignNode] }))
        {
            // Perform additional check to see if incrementor and initializer are similar/readable
            if (Incrementor is { Children: [.., AssignNode incrementor] })
            {
                // For readability, just stick to integer and variable assignments/compound operations
                if (incrementor.Value is not (Int16Node or Int32Node or Int64Node or VariableNode) &&
                    incrementor.AssignKind != AssignNode.AssignType.Prefix &&
                    incrementor.AssignKind != AssignNode.AssignType.Postfix)
                {
                    return i;
                }
                if (incrementor.AssignKind is not (AssignNode.AssignType.Compound or
                    AssignNode.AssignType.Prefix or AssignNode.AssignType.Postfix))
                {
                    return i;
                }

                // Also for readability, make sure the initializer and incrementor variables are similar
                if (assign.Variable is not VariableNode initVariable ||
                    incrementor.Variable is not VariableNode incVariable)
                {
                    return i;
                }
                if (!initVariable.SimilarToInForIncrementor(incVariable))
                {
                    return i;
                }
            }

            // Move the initializer in!
            Initializer = assign;
            block.Children.RemoveAt(i - 1);
            block.Children[i - 1] = Clean(cleaner);

            return i - 1;
        }

        return i;
    }

    /// <inheritdoc/>
    public IStatementNode PostClean(ASTCleaner cleaner)
    {
        cleaner.TopFragmentContext!.PushLocalScope(cleaner.Context, cleaner.TopFragmentContext!.CurrentPostCleanupBlock!, this);

        Initializer = Initializer?.PostClean(cleaner);
        Condition = Condition?.PostClean(cleaner);
        Incrementor?.PostClean(cleaner);

        cleaner.TopFragmentContext!.PushLocalScope(cleaner.Context, cleaner.TopFragmentContext!.CurrentPostCleanupBlock!, this);
        Body.PostClean(cleaner);
        cleaner.TopFragmentContext!.PopLocalScope(cleaner.Context);

        cleaner.TopFragmentContext!.PopLocalScope(cleaner.Context);

        return this;
    }

    /// <inheritdoc/>
    public void Print(ASTPrinter printer)
    {
        printer.Write("for (");
        if (Condition is null && Incrementor is null)
        {
            Initializer?.Print(printer);
            printer.Write(";;");
        }
        else
        {
            Initializer?.Print(printer);
            printer.Write("; ");
            Condition?.Print(printer);
            if (Incrementor is not null)
            {
                printer.Write("; ");
                IStatementNode shortestStatement = Incrementor.GetShortestStatement();
                if (shortestStatement is not BlockNode { Children: [] })
                {
                    shortestStatement.Print(printer);
                }
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

    /// <inheritdoc/>
    public bool RequiresMultipleLines(ASTPrinter printer)
    {
        return true;
    }

    /// <inheritdoc/>
    public IEnumerable<IBaseASTNode> EnumerateChildren()
    {
        if (Initializer is not null)
        {
            yield return Initializer;
        }
        if (Condition is not null)
        {
            yield return Condition;
        }
        if (Incrementor is not null)
        {
            yield return Incrementor;
        }
        yield return Body;
    }
}
