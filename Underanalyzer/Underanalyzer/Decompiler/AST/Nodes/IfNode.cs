/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;

namespace Underanalyzer.Decompiler.AST;

/// <summary>
/// Represents an if statement in the AST.
/// </summary>
public class IfNode(IExpressionNode condition, BlockNode trueBlock, BlockNode? elseBlock = null) : IStatementNode
{
    /// <summary>
    /// The condition of the if statement.
    /// </summary>
    public IExpressionNode Condition { get; internal set; } = condition;

    /// <summary>
    /// The main (true) block of the if statement.
    /// </summary>
    public BlockNode TrueBlock { get; internal set; } = trueBlock;

    /// <summary>
    /// The else (false) block of the if statement, or <see langword="null"/> if none exists.
    /// </summary>
    public BlockNode? ElseBlock { get; internal set; } = elseBlock;

    /// <inheritdoc/>
    public bool SemicolonAfter => false;

    /// <inheritdoc/>
    public bool EmptyLineBefore { get; set; }

    /// <inheritdoc/>
    public bool EmptyLineAfter { get; set; }

    /// <inheritdoc/>
    public IStatementNode Clean(ASTCleaner cleaner)
    {
        Condition = Condition.Clean(cleaner);
        Condition.Group = false;
        TrueBlock.Clean(cleaner);
        ElseBlock?.Clean(cleaner);

        EmptyLineAfter = EmptyLineBefore = cleaner.Context.Settings.EmptyLineAroundBranchStatements;

        return this;
    }

    /// <inheritdoc/>
    public IStatementNode PostClean(ASTCleaner cleaner)
    {
        Condition = Condition.PostClean(cleaner);

        cleaner.TopFragmentContext!.PushLocalScope(cleaner.Context, cleaner.TopFragmentContext!.CurrentPostCleanupBlock!, this);
        TrueBlock.PostClean(cleaner);
        cleaner.TopFragmentContext!.PopLocalScope(cleaner.Context);

        cleaner.TopFragmentContext!.PushLocalScope(cleaner.Context, cleaner.TopFragmentContext!.CurrentPostCleanupBlock!, this);
        ElseBlock?.PostClean(cleaner);
        cleaner.TopFragmentContext!.PopLocalScope(cleaner.Context);

        return this;
    }

    /// <summary>
    /// Determines whether the node can be printed without braces, recursively dependent on
    /// the true block and else block's ability to be printed without multiple lines.
    /// </summary>
    private bool CanPrintWithoutBraces(ASTPrinter printer)
    {
        if (TrueBlock.RequiresMultipleLines(printer))
        {
            return false;
        }
        if (ElseBlock is not null)
        {
            if (ElseBlock is { Children: [IfNode elseIf] })
            {
                if (!elseIf.CanPrintWithoutBraces(printer))
                {
                    return false;
                }
            }
            else
            {
                if (ElseBlock.RequiresMultipleLines(printer))
                {
                    return false;
                }
            }
        }
        return true;
    }

    /// <inheritdoc/>
    public void Print(ASTPrinter printer)
    {
        printer.Write("if (");
        Condition.Print(printer);
        printer.Write(')');

        if (printer.Context.Settings.RemoveSingleLineBlockBraces && CanPrintWithoutBraces(printer))
        {
            // Print without braces
            TrueBlock.PrintSingleLine(printer);
            if (ElseBlock is not null)
            {
                printer.EndLine();
                printer.StartLine();
                printer.Write("else");
                if (ElseBlock is { Children: [IfNode elseIf] })
                {
                    printer.Write(' ');
                    elseIf.PrintElseIf(printer, true);
                }
                else
                {
                    ElseBlock.PrintSingleLine(printer);
                }
            }
        }
        else
        {
            // Print with braces
            if (printer.Context.Settings.OpenBlockBraceOnSameLine)
            {
                printer.Write(' ');
            }
            TrueBlock.Print(printer);
            if (ElseBlock is not null)
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
                printer.Write("else");
                if (ElseBlock is { Children: [IfNode elseIf] })
                {
                    printer.Write(' ');
                    elseIf.PrintElseIf(printer, false);
                }
                else
                {
                    if (printer.Context.Settings.OpenBlockBraceOnSameLine)
                    {
                        printer.Write(' ');
                    }
                    ElseBlock.Print(printer);
                }
            }
        }
    }

    /// <summary>
    /// Variation of regular if statement printing code, for printing "else if" chains.
    /// </summary>
    public void PrintElseIf(ASTPrinter printer, bool shouldRemoveBraces)
    {
        printer.Write("if (");
        Condition.Print(printer);
        printer.Write(')');

        if (shouldRemoveBraces)
        {
            // Print without braces
            TrueBlock.PrintSingleLine(printer);
            if (ElseBlock is not null)
            {
                printer.EndLine();
                printer.StartLine();
                printer.Write("else");
                if (ElseBlock is { Children: [IfNode elseIf] })
                {
                    printer.Write(' ');
                    elseIf.PrintElseIf(printer, true);
                }
                else
                {
                    ElseBlock.PrintSingleLine(printer);
                }
            }
        }
        else
        {
            // Print with braces
            if (printer.Context.Settings.OpenBlockBraceOnSameLine)
            {
                printer.Write(' ');
            }
            TrueBlock.Print(printer);
            if (ElseBlock is not null)
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
                printer.Write("else");
                if (ElseBlock is { Children: [IfNode elseIf] })
                {
                    printer.Write(' ');
                    elseIf.PrintElseIf(printer, false);
                }
                else
                {
                    if (printer.Context.Settings.OpenBlockBraceOnSameLine)
                    {
                        printer.Write(' ');
                    }
                    ElseBlock.Print(printer);
                }
            }
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
        yield return TrueBlock;
        if (ElseBlock is not null)
        {
            yield return ElseBlock;
        }
    }
}
