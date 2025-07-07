/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using Underanalyzer.Decompiler.GameSpecific;

namespace Underanalyzer.Decompiler.AST;

/// <summary>
/// Represents a predefined double constant in the AST, with one single part.
/// </summary>
public class PredefinedDoubleSingleNode(string value, double originalValue) : IExpressionNode, IConditionalValueNode
{
    public string Value { get; } = value;
    public double OriginalValue { get; } = originalValue;

    public bool Duplicated { get; set; } = false;
    public bool Group { get; set; } = false;
    public IGMInstruction.DataType StackType { get; set; } = IGMInstruction.DataType.Double;

    public string ConditionalTypeName => "PredefinedDouble";
    public string ConditionalValue => Value;

    public IExpressionNode Clean(ASTCleaner cleaner)
    {
        return this;
    }

    public virtual void Print(ASTPrinter printer)
    {
        printer.Write(Value);
    }

    public bool RequiresMultipleLines(ASTPrinter printer)
    {
        return false;
    }

    public IExpressionNode? ResolveMacroType(ASTCleaner cleaner, IMacroType type)
    {
        if (type is IMacroTypeConditional conditional)
        {
            return conditional.Resolve(cleaner, this);
        }
        return null;
    }
}

/// <summary>
/// Represents a predefined double constant in the AST, with multiple parts.
/// </summary>
public class PredefinedDoubleMultiNode(string value, double originalValue) 
    : PredefinedDoubleSingleNode(value, originalValue), IMultiExpressionNode
{
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
