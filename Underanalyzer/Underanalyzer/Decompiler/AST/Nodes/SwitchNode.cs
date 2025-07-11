﻿/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;
using Underanalyzer.Decompiler.GameSpecific;

namespace Underanalyzer.Decompiler.AST;

/// <summary>
/// Represents a switch statement in the AST.
/// </summary>
public class SwitchNode(IExpressionNode expression, BlockNode body) : IStatementNode
{
    /// <summary>
    /// The expression being switched upon.
    /// </summary>
    public IExpressionNode Expression { get; private set; } = expression;

    /// <summary>
    /// The main block of the switch statement.
    /// </summary>
    public BlockNode Body { get; private set; } = body;

    /// <inheritdoc/>
    public bool SemicolonAfter => false;

    /// <inheritdoc/>
    public bool EmptyLineBefore { get; set; }

    /// <inheritdoc/>
    public bool EmptyLineAfter { get; set; }

    /// <inheritdoc/>
    public IStatementNode Clean(ASTCleaner cleaner)
    {
        Expression = Expression.Clean(cleaner);
        Body.Clean(cleaner);

        // Handle macro type resolution for cases
        if (Expression is IMacroTypeNode exprTypeNode && exprTypeNode.GetExpressionMacroType(cleaner) is IMacroType exprMacroType)
        {
            foreach (IStatementNode statement in Body.Children)
            {
                if (statement is SwitchCaseNode caseNode && caseNode.Expression is IMacroResolvableNode exprResolvable &&
                    exprResolvable.ResolveMacroType(cleaner, exprMacroType) is IExpressionNode exprResolved)
                {
                    caseNode.Expression = exprResolved;
                }
            }
        }

        EmptyLineAfter = EmptyLineBefore = cleaner.Context.Settings.EmptyLineAroundBranchStatements;

        return this;
    }

    /// <inheritdoc/>
    public IStatementNode PostClean(ASTCleaner cleaner)
    {
        Expression = Expression.PostClean(cleaner);
        Body.PostCleanSwitch(cleaner, this);
        return this;
    }

    /// <inheritdoc/>
    public void Print(ASTPrinter printer)
    {
        printer.Write("switch (");
        Expression.Print(printer);
        printer.Write(')');
        if (printer.Context.Settings.OpenBlockBraceOnSameLine)
        {
            printer.Write(' ');
        }
        Body.Print(printer);
    }

    /// <inheritdoc/>
    public bool RequiresMultipleLines(ASTPrinter printer)
    {
        return true;
    }

    /// <inheritdoc/>
    public IEnumerable<IBaseASTNode> EnumerateChildren()
    {
        yield return Expression;
        yield return Body;
    }
}
