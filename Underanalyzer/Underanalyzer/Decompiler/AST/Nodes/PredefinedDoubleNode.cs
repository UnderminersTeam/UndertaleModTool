/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;
using Underanalyzer.Decompiler.GameSpecific;

namespace Underanalyzer.Decompiler.AST;

/// <summary>
/// Represents a predefined double constant in the AST, with one single part (i.e., no parentheses required).
/// </summary>
public class PredefinedDoubleSingleNode(string value, double originalValue) : IExpressionNode, IConditionalValueNode
{
    /// <summary>
    /// String of code to directly print in place of the original double value.
    /// </summary>
    public string Value { get; } = value;

    /// <summary>
    /// Original value before being detected and converted to this node.
    /// </summary>
    public double OriginalValue { get; } = originalValue;

    /// <inheritdoc/>
    public bool Duplicated { get; set; } = false;

    /// <inheritdoc/>
    public bool Group { get; set; } = false;

    /// <inheritdoc/>
    public IGMInstruction.DataType StackType { get; set; } = IGMInstruction.DataType.Double;

    /// <inheritdoc/>
    public string ConditionalTypeName => "PredefinedDouble";

    /// <inheritdoc/>
    public string ConditionalValue => Value;

    /// <inheritdoc/>
    public IExpressionNode Clean(ASTCleaner cleaner)
    {
        return this;
    }

    /// <inheritdoc/>
    public IExpressionNode PostClean(ASTCleaner cleaner)
    {
        return this;
    }

    /// <inheritdoc/>
    public virtual void Print(ASTPrinter printer)
    {
        printer.Write(Value);
    }

    /// <inheritdoc/>
    public bool RequiresMultipleLines(ASTPrinter printer)
    {
        return false;
    }

    /// <inheritdoc/>
    public IExpressionNode? ResolveMacroType(ASTCleaner cleaner, IMacroType type)
    {
        if (type is IMacroTypeConditional conditional)
        {
            return conditional.Resolve(cleaner, this);
        }
        return null;
    }

    /// <inheritdoc/>
    public IEnumerable<IBaseASTNode> EnumerateChildren()
    {
        return [];
    }
}

/// <summary>
/// Represents a predefined double constant in the AST, with multiple parts (i.e., parentheses may be required).
/// </summary>
public class PredefinedDoubleMultiNode(string value, double originalValue) 
    : PredefinedDoubleSingleNode(value, originalValue), IMultiExpressionNode
{
    /// <inheritdoc/>
    public override void Print(ASTPrinter printer)
    {
        if (Group)
        {
            printer.Write('(');
        }
        base.Print(printer);
        if (Group)
        {
            printer.Write(')');
        }
    }
}
