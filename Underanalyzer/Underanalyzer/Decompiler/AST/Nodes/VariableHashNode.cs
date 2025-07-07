/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using Underanalyzer.Decompiler.GameSpecific;

namespace Underanalyzer.Decompiler.AST;

/// <summary>
/// Represents a variable hash in the AST, generated at compile-time in more recent GMLv2 versions.
/// </summary>
public class VariableHashNode(IGMVariable variable) : IExpressionNode, IStatementNode, IConditionalValueNode
{
    /// <summary>
    /// The variable being referenced.
    /// </summary>
    public IGMVariable Variable = variable;

    public bool Duplicated { get; set; }
    public bool Group { get; set; } = false;
    public IGMInstruction.DataType StackType { get; set; } = IGMInstruction.DataType.Int32;
    public bool SemicolonAfter => true;
    public bool EmptyLineBefore => false;
    public bool EmptyLineAfter => false;

    public string ConditionalTypeName => "VariableHash";
    public string ConditionalValue => Variable.Name.Content; // TODO?

    IExpressionNode IASTNode<IExpressionNode>.Clean(ASTCleaner cleaner)
    {
        return this;
    }

    IStatementNode IASTNode<IStatementNode>.Clean(ASTCleaner cleaner)
    {
        return this;
    }

    public void Print(ASTPrinter printer)
    {
        if (Group)
        {
            printer.Write("(");
        }

        printer.Write("variable_get_hash(\"");
        printer.Write(Variable.Name.Content);
        printer.Write("\")");

        if (Group)
        {
            printer.Write(")");
        }
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

