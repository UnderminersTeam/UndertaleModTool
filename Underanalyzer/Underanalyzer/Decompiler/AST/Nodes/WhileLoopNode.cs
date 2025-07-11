﻿/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;

namespace Underanalyzer.Decompiler.AST;

/// <summary>
/// Represents a while loop in the AST.
/// </summary>
public class WhileLoopNode(IExpressionNode condition, BlockNode body, bool mustBeWhileLoop) : IStatementNode, IBlockCleanupNode
{
    /// <summary>
    /// The condition of the loop.
    /// </summary>
    public IExpressionNode Condition { get; private set; } = condition;

    /// <summary>
    /// The main block of the loop.
    /// </summary>
    public BlockNode Body { get; private set; } = body;

    /// <summary>
    /// True if this loop was specifically detected to be a while loop already.
    /// That is, if true, this cannot be rewritten as a for loop.
    /// </summary>
    public bool MustBeWhileLoop { get; } = mustBeWhileLoop;

    /// <inheritdoc/>
    public bool SemicolonAfter { get => false; }

    /// <inheritdoc/>
    public bool EmptyLineBefore { get; set; }

    /// <inheritdoc/>
    public bool EmptyLineAfter { get; set; }

    /// <inheritdoc/>
    public IStatementNode Clean(ASTCleaner cleaner)
    {
        EmptyLineAfter = EmptyLineBefore = cleaner.Context.Settings.EmptyLineAroundBranchStatements;

        if (!MustBeWhileLoop)
        {
            // Check if we can turn into a for (;;) loop
            if (Condition is Int64Node i64 && i64.Value == 1)
            {
                ElseToContinueCleanup.Clean(cleaner, Body);
                return new ForLoopNode(null, null, null, (BlockNode)Body.Clean(cleaner))
                {
                    EmptyLineBefore = EmptyLineBefore,
                    EmptyLineAfter = EmptyLineAfter
                };
            }
        }

        Condition = Condition.Clean(cleaner);
        Condition.Group = false;
        Body.Clean(cleaner);

        return this;
    }

    /// <inheritdoc/>
    public int BlockClean(ASTCleaner cleaner, BlockNode block, int i)
    {
        // Check if we should convert this loop into a for loop
        if (!MustBeWhileLoop && i > 0 && block.Children[i - 1] is AssignNode initializer &&
            Body.Children is [.., AssignNode incrementor])
        {
            // For readability, just stick to integer and variable assignments/compound operations
            if (initializer.Value is not (Int16Node or Int32Node or Int64Node or VariableNode) ||
                (incrementor.Value is not (Int16Node or Int32Node or Int64Node or VariableNode) &&
                 incrementor.AssignKind != AssignNode.AssignType.Prefix &&
                 incrementor.AssignKind != AssignNode.AssignType.Postfix))
            {
                return i;
            }
            if (incrementor.AssignKind is not (AssignNode.AssignType.Compound or
                AssignNode.AssignType.Prefix or AssignNode.AssignType.Postfix))
            {
                return i;
            }

            // Also for readability, make sure the initializer and incrementor variables are similar
            if (initializer.Variable is not VariableNode initVariable ||
                incrementor.Variable is not VariableNode incVariable)
            {
                return i;
            }
            if (!initVariable.SimilarToInForIncrementor(incVariable))
            {
                return i;
            }

            // Finally, if the for loop body would be empty, there's no need to convert over
            if (Body.Children.Count == 1)
            {
                return i;
            }

            // Convert into for loop!
            Body.Children.RemoveAt(Body.Children.Count - 1);
            BlockNode incrementorBlock = new(Body.FragmentContext);
            incrementorBlock.Children.Add(incrementor);
            block.Children.RemoveAt(i - 1);
            block.Children[i - 1] = new ForLoopNode(initializer, Condition, incrementorBlock, Body);
            block.Children[i - 1] = block.Children[i - 1].Clean(cleaner);

            return i - 1;
        }

        return i;
    }

    /// <inheritdoc/>
    public IStatementNode PostClean(ASTCleaner cleaner)
    {
        Condition = Condition.PostClean(cleaner);
        Condition.Group = false;

        cleaner.TopFragmentContext!.PushLocalScope(cleaner.Context, cleaner.TopFragmentContext!.CurrentPostCleanupBlock!, this);
        Body.PostClean(cleaner);
        cleaner.TopFragmentContext!.PopLocalScope(cleaner.Context);

        return this;
    }

    /// <inheritdoc/>
    public void Print(ASTPrinter printer)
    {
        printer.Write("while (");
        Condition.Print(printer);
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
        yield return Condition;
        yield return Body;
    }
}
