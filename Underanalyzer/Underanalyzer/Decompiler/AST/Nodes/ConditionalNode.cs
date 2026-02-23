/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;
using Underanalyzer.Decompiler.GameSpecific;

namespace Underanalyzer.Decompiler.AST;

/// <summary>
/// Represents a conditional expression in the AST.
/// </summary>
public class ConditionalNode(IExpressionNode condition, IExpressionNode trueExpr, IExpressionNode falseExpr) 
    : IMultiExpressionNode, IMacroResolvableNode, IConditionalValueNode
{
    /// <summary>
    /// The condition of the conditional expression.
    /// </summary>
    public IExpressionNode Condition { get; private set; } = condition;

    /// <summary>
    /// The expression that is returned when the condition is true.
    /// </summary>
    public IExpressionNode True { get; private set; } = trueExpr;

    /// <summary>
    /// The expression that is returned when the condition is false.
    /// </summary>
    public IExpressionNode False { get; private set; } = falseExpr;

    /// <inheritdoc/>
    public bool Duplicated { get; set; } = false;

    /// <inheritdoc/>
    public bool Group { get; set; } = false;

    /// <inheritdoc/>
    public IGMInstruction.DataType StackType { get; set; } = IGMInstruction.DataType.Variable;

    /// <inheritdoc/>
    public string ConditionalTypeName => "Conditional";

    /// <inheritdoc/>
    public string ConditionalValue => ""; // TODO?

    /// <inheritdoc/>
    public IExpressionNode Clean(ASTCleaner cleaner)
    {
        Condition = Condition.Clean(cleaner);
        True = True.Clean(cleaner);
        False = False.Clean(cleaner);

        // Ensure proper precedence
        if (Condition is IMultiExpressionNode)
        {
            Condition.Group = true;
        }
        if (True is IMultiExpressionNode)
        {
            True.Group = true;
        }
        if (False is IMultiExpressionNode)
        {
            False.Group = true;
        }

        return this;
    }

    /// <inheritdoc/>
    public IExpressionNode PostClean(ASTCleaner cleaner)
    {
        Condition = Condition.PostClean(cleaner);
        True = True.PostClean(cleaner);
        False = False.PostClean(cleaner);
        return this;
    }

    /// <inheritdoc/>
    public void Print(ASTPrinter printer)
    {
        if (Group)
        {
            printer.Write('(');
        }

        Condition.Print(printer);
        printer.Write(" ? ");
        True.Print(printer);
        printer.Write(" : ");
        False.Print(printer);

        if (Group)
        {
            printer.Write(')');
        }
    }

    /// <inheritdoc/>
    public bool RequiresMultipleLines(ASTPrinter printer)
    {
        return Condition.RequiresMultipleLines(printer) || 
               True.RequiresMultipleLines(printer) || 
               False.RequiresMultipleLines(printer);
    }

    /// <inheritdoc/>
    public IExpressionNode? ResolveMacroType(ASTCleaner cleaner, IMacroType type)
    {
        if (type is IMacroTypeConditional conditional)
        {
            return conditional.Resolve(cleaner, this);
        }

        bool didAnything = false;

        if (True is IMacroResolvableNode trueResolvable && 
            trueResolvable.ResolveMacroType(cleaner, type) is IExpressionNode trueResolved)
        {
            True = trueResolved;
            didAnything = true;
        }
        if (False is IMacroResolvableNode falseResolvable &&
            falseResolvable.ResolveMacroType(cleaner, type) is IExpressionNode falseResolved)
        {
            False = falseResolved;
            didAnything = true;
        }

        return didAnything ? this : null;
    }

    /// <inheritdoc/>
    public IEnumerable<IBaseASTNode> EnumerateChildren()
    {
        yield return Condition;
        yield return True;
        yield return False;
    }
}
