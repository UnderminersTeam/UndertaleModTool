/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;

namespace Underanalyzer.Decompiler.AST;

/// <summary>
/// Represents a try/catch, try/catch/finally, or try/finally statement in the AST.
/// </summary>
public class TryCatchNode(BlockNode tryBlock, BlockNode? catchBlock, VariableNode? catchVariable) : IStatementNode
{
    /// <summary>
    /// The block inside of "try".
    /// </summary>
    public BlockNode Try { get; } = tryBlock;

    /// <summary>
    /// The block inside of "catch", or <see langword="null"/> if none exists.
    /// </summary>
    public BlockNode? Catch { get; } = catchBlock;

    /// <summary>
    /// The variable used to store the thrown value for the catch block, if Catch is not <see langword="null"/>.
    /// </summary>
    public VariableNode? CatchVariable { get; } = catchVariable;

    /// <summary>
    /// The block inside of "finally", or <see langword="null"/> if none exists.
    /// </summary>
    public BlockNode? Finally { get; internal set; } = null;

    /// <summary>
    /// Compiler-generated variable name used for break, or <see langword="null"/> if none.
    /// </summary>
    public string? BreakVariableName { get; internal set; } = null;

    /// <summary>
    /// Compiler-generated variable name used for continue, or <see langword="null"/> if none.
    /// </summary>
    public string? ContinueVariableName { get; internal set; } = null;

    /// <inheritdoc/>
    public bool SemicolonAfter => false;

    /// <inheritdoc/>
    public bool EmptyLineBefore { get; set; }

    /// <inheritdoc/>
    public bool EmptyLineAfter { get; set; }

    /// <summary>
    /// Cleans out compiler-generated control flow from individual try or catch blocks.
    /// </summary>
    private void CleanPart(BlockNode node)
    {
        // Verify we're removing the right compiler-generated code
        if (node is not { Children: [WhileLoopNode whileLoop] })
        {
            return;
        }
        if (whileLoop.Body.Children is not [IfNode ifNode, .., BreakNode])
        {
            return;
        }
        if (ifNode is not { Condition: VariableNode continueVar, TrueBlock.Children: [BreakNode], ElseBlock: null })
        {
            return;
        }
        if (continueVar.Variable.Name.Content != ContinueVariableName)
        {
            return;
        }

        // Remove nodes from while body, and reassign the remaining children to the base node
        whileLoop.Body.Children.RemoveAt(0);
        whileLoop.Body.Children.RemoveAt(whileLoop.Body.Children.Count - 1);
        node.Children = whileLoop.Body.Children;
    }

    /// <inheritdoc/>
    public IStatementNode Clean(ASTCleaner cleaner)
    {
        if (Finally is not null)
        {
            Finally.Clean(cleaner);
            Finally.UseBraces = true;

            if (cleaner.Context.Settings.CleanupTry)
            {
                // Push finally context
                cleaner.TopFragmentContext!.FinallyStatementCount.Push(Finally.Children.Count);
            }
        }

        Try.Clean(cleaner);
        Try.UseBraces = true;
        if (Catch is not null)
        {
            Catch.Clean(cleaner);
            Catch.UseBraces = true;
        }
        CatchVariable?.Clean(cleaner);

        if (cleaner.Context.Settings.CleanupTry)
        {
            if (Finally is not null)
            {
                // Pop finally context
                cleaner.TopFragmentContext!.FinallyStatementCount.Pop();
            }

            // Cleanup continue/break
            if (BreakVariableName is not null && ContinueVariableName is not null)
            {
                // Cleanup compiler-generated control flow
                CleanPart(Try);
                if (Catch is not null)
                {
                    CleanPart(Catch);
                }

                // Remove local variable names
                cleaner.TopFragmentContext!.RemoveLocal(BreakVariableName);
                cleaner.TopFragmentContext.RemoveLocal(ContinueVariableName);
            }
        }

        EmptyLineAfter = EmptyLineBefore = cleaner.Context.Settings.EmptyLineAroundBranchStatements;

        return this;
    }

    /// <inheritdoc/>
    public IStatementNode PostClean(ASTCleaner cleaner)
    {
        cleaner.TopFragmentContext!.PushLocalScope(cleaner.Context, cleaner.TopFragmentContext!.CurrentPostCleanupBlock!, this);
        Finally?.PostClean(cleaner);
        cleaner.TopFragmentContext!.PopLocalScope(cleaner.Context);

        cleaner.TopFragmentContext!.PushLocalScope(cleaner.Context, cleaner.TopFragmentContext!.CurrentPostCleanupBlock!, this);
        Try.PostClean(cleaner);
        cleaner.TopFragmentContext!.PopLocalScope(cleaner.Context);

        cleaner.TopFragmentContext!.PushLocalScope(cleaner.Context, cleaner.TopFragmentContext!.CurrentPostCleanupBlock!, this);
        Catch?.PostClean(cleaner);
        cleaner.TopFragmentContext!.PopLocalScope(cleaner.Context);

        CatchVariable?.PostClean(cleaner);

        return this;
    }

    /// <inheritdoc/>
    public void Print(ASTPrinter printer)
    {
        printer.Write("try");
        if (printer.Context.Settings.OpenBlockBraceOnSameLine)
        {
            printer.Write(' ');
        }
        Try.Print(printer);
        if (Catch is not null)
        {
            if (printer.Context.Settings.OpenBlockBraceOnSameLine)
            {
                printer.Write(' ');
            }
            else
            {
                printer.EndLine();
                printer.StartLine();
            }
            printer.Write("catch (");
            CatchVariable!.Print(printer);
            printer.Write(')');
            if (printer.Context.Settings.OpenBlockBraceOnSameLine)
            {
                printer.Write(' ');
            }
            Catch.Print(printer);
        }
        if (Finally is not null)
        {
            if (printer.Context.Settings.OpenBlockBraceOnSameLine)
            {
                printer.Write(' ');
            }
            else
            {
                printer.EndLine();
                printer.StartLine();
            }
            printer.Write("finally");
            if (printer.Context.Settings.OpenBlockBraceOnSameLine)
            {
                printer.Write(' ');
            }
            Finally.Print(printer);
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
        yield return Try;
        if (CatchVariable is not null)
        {
            yield return CatchVariable;
        }
        if (Catch is not null)
        {
            yield return Catch;
        }
        if (Finally is not null)
        {
            yield return Finally;
        }
    }

    /// <summary>
    /// Represents the location where the finally block of a try statement ends.
    /// Never appears after AST cleaning, and cannot be printed.
    /// </summary>
    public class FinishFinallyNode : IStatementNode, IExpressionNode, IBlockCleanupNode
    {
        /// <inheritdoc/>
        public bool SemicolonAfter => false;

        /// <inheritdoc/>
        public bool EmptyLineBefore { get => false; set => _ = value; }

        /// <inheritdoc/>
        public bool EmptyLineAfter { get => false; set => _ = value; }

        /// <inheritdoc/>
        public bool Duplicated { get; set; }

        /// <inheritdoc/>
        public bool Group { get; set; } = false;

        /// <inheritdoc/>
        public IGMInstruction.DataType StackType { get; set; } = IGMInstruction.DataType.Variable;

        /// <inheritdoc/>
        public IStatementNode Clean(ASTCleaner cleaner)
        {
            return this;
        }

        /// <inheritdoc/>
        IExpressionNode IASTNode<IExpressionNode>.Clean(ASTCleaner cleaner)
        {
            return this;
        }

        /// <inheritdoc/>
        public IStatementNode PostClean(ASTCleaner cleaner)
        {
            return this;
        }

        /// <inheritdoc/>
        IExpressionNode IASTNode<IExpressionNode>.PostClean(ASTCleaner cleaner)
        {
            return this;
        }

        /// <inheritdoc/>
        public int BlockClean(ASTCleaner cleaner, BlockNode block, int i)
        {
            // Search for the try statement this is associated with, and build a block for its finally
            for (int j = i - 1; j >= 0; j--)
            {
                IStatementNode curr = block.Children[j];
                if (curr is TryCatchNode tryCatchNode)
                {
                    // Create finally block with all statements in between
                    BlockNode finallyBlock = new(block.FragmentContext)
                    {
                        UseBraces = true,
                        Children = block.Children.GetRange(j + 1, i - (j + 1))
                    };
                    block.Children.RemoveRange(j + 1, i - (j + 1));

                    // Assign finally block, and re-clean try statement
                    tryCatchNode.Finally = finallyBlock;
                    tryCatchNode.Clean(cleaner);

                    i -= finallyBlock.Children.Count;
                }
            }

            // Remove this statement from AST
            block.Children.RemoveAt(i);
            return i - 1;
        }

        /// <inheritdoc/>
        public void Print(ASTPrinter printer)
        {
            throw new InvalidOperationException();
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
}
