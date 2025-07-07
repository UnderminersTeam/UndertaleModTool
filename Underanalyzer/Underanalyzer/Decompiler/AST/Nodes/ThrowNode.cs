/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

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

    public bool Duplicated { get; set; }
    public bool Group { get; set; } = false;
    public IGMInstruction.DataType StackType { get; set; } = IGMInstruction.DataType.Variable;
    public bool SemicolonAfter => true;
    public bool EmptyLineBefore => false;
    public bool EmptyLineAfter => false;

    public IExpressionNode Clean(ASTCleaner cleaner)
    {
        Value = Value.Clean(cleaner);
        return this;
    }

    IStatementNode IASTNode<IStatementNode>.Clean(ASTCleaner cleaner)
    {
        Value = Value.Clean(cleaner);
        return this;
    }

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

    public bool RequiresMultipleLines(ASTPrinter printer)
    {
        return Value.RequiresMultipleLines(printer);
    }
}
