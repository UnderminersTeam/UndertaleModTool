/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

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

    public bool SemicolonAfter => true;
    public bool EmptyLineBefore { get; private set; }
    public bool EmptyLineAfter { get; private set; }

    public IStatementNode Clean(ASTCleaner cleaner)
    {
        ElseToContinueCleanup.Clean(cleaner, Body);
        Body.Clean(cleaner);
        Condition = Condition.Clean(cleaner);
        Condition.Group = false;

        EmptyLineAfter = EmptyLineBefore = cleaner.Context.Settings.EmptyLineAroundBranchStatements;

        return this;
    }

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

    public bool RequiresMultipleLines(ASTPrinter printer)
    {
        return true;
    }
}
