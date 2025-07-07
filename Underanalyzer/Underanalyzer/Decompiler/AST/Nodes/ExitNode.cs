/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

namespace Underanalyzer.Decompiler.AST;

/// <summary>
/// Represents an "exit"/"empty return" statement in the AST.
/// </summary>
public class ExitNode : IStatementNode, IBlockCleanupNode
{
    public bool SemicolonAfter => true;
    public bool EmptyLineBefore => false;
    public bool EmptyLineAfter => false;

    public IStatementNode Clean(ASTCleaner cleaner)
    {
        return this;
    }

    public int BlockClean(ASTCleaner cleaner, BlockNode block, int i)
    {
        // Remove duplicated finally statements
        if (cleaner.TopFragmentContext!.FinallyStatementCount.Count > 0)
        {
            int count = 0;
            foreach (int statementCount in cleaner.TopFragmentContext.FinallyStatementCount)
            {
                count += statementCount;
            }
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
        // TODO: check if we're inside of a function (or script in GMS2) and use "return" instead
        printer.Write("exit");
    }

    public bool RequiresMultipleLines(ASTPrinter printer)
    {
        return false;
    }
}
