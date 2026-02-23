/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;

namespace Underanalyzer.Decompiler.AST;

/// <summary>
/// Represents the "throw" keyword being used to throw an object/exception in the AST.
/// </summary>
public class ThrowNode(IExpressionNode value) : IExpressionNode, IStatementNode, IBlockCleanupNode
{
    /// <summary>
    /// The value being thrown.
    /// </summary>
    public IExpressionNode Value { get; private set; } = value;

    /// <inheritdoc/>
    public bool Duplicated { get; set; }

    /// <inheritdoc/>
    public bool Group { get; set; } = false;

    /// <inheritdoc/>
    public IGMInstruction.DataType StackType { get; set; } = IGMInstruction.DataType.Variable;

    /// <inheritdoc/>
    public bool SemicolonAfter => true;

    /// <inheritdoc/>
    public bool EmptyLineBefore { get => false; set => _ = value; }

    /// <inheritdoc/>
    public bool EmptyLineAfter { get => false; set => _ = value; }

    /// <inheritdoc/>
    public IExpressionNode Clean(ASTCleaner cleaner)
    {
        Value = Value.Clean(cleaner);
        return this;
    }

    /// <inheritdoc/>
    IStatementNode IASTNode<IStatementNode>.Clean(ASTCleaner cleaner)
    {
        Value = Value.Clean(cleaner);
        return this;
    }

    /// <inheritdoc/>
    public IExpressionNode PostClean(ASTCleaner cleaner)
    {
        Value = Value.PostClean(cleaner);
        return this;
    }

    /// <inheritdoc/>
    IStatementNode IASTNode<IStatementNode>.PostClean(ASTCleaner cleaner)
    {
        Value = Value.PostClean(cleaner);
        return this;
    }

    /// <inheritdoc/>
    public int BlockClean(ASTCleaner cleaner, BlockNode block, int i)
    {
        // Remove duplicated finally statements
        if (cleaner.TopFragmentContext!.FinallyStatementCount.Count > 0 &&
            cleaner.Context.GameContext.UsingFinallyBeforeThrow)
        {
            int count = cleaner.TopFragmentContext.FinallyStatementCount.Peek();
            if (i - count >= 0)
            {
                block.Children.RemoveRange(i - count, count);
                return i - count;
            }
        }

        return i;
    }

    /// <inheritdoc/>
    public void Print(ASTPrinter printer)
    {
        if (Group)
        {
            printer.Write('(');
        }

        printer.Write("throw ");
        Value.Print(printer);

        if (Group)
        {
            printer.Write(')');
        }
    }

    /// <inheritdoc/>
    public bool RequiresMultipleLines(ASTPrinter printer)
    {
        return Value.RequiresMultipleLines(printer);
    }

    /// <inheritdoc/>
    public IEnumerable<IBaseASTNode> EnumerateChildren()
    {
        yield return Value;
    }
}
