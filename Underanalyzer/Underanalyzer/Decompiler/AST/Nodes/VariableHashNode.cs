/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;
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

    /// <inheritdoc/>
    public bool Duplicated { get; set; }

    /// <inheritdoc/>
    public bool Group { get; set; } = false;

    /// <inheritdoc/>
    public IGMInstruction.DataType StackType { get; set; } = IGMInstruction.DataType.Int32;

    /// <inheritdoc/>
    public bool SemicolonAfter => true;

    /// <inheritdoc/>
    public bool EmptyLineBefore { get => false; set => _ = value; }

    /// <inheritdoc/>
    public bool EmptyLineAfter { get => false; set => _ = value; }

    /// <inheritdoc/>
    public string ConditionalTypeName => "VariableHash";

    /// <inheritdoc/>
    public string ConditionalValue => Variable.Name.Content; // TODO?

    /// <inheritdoc/>
    IExpressionNode IASTNode<IExpressionNode>.Clean(ASTCleaner cleaner)
    {
        return this;
    }

    /// <inheritdoc/>
    IStatementNode IASTNode<IStatementNode>.Clean(ASTCleaner cleaner)
    {
        return this;
    }

    /// <inheritdoc/>
    IExpressionNode IASTNode<IExpressionNode>.PostClean(ASTCleaner cleaner)
    {
        return this;
    }

    /// <inheritdoc/>
    IStatementNode IASTNode<IStatementNode>.PostClean(ASTCleaner cleaner)
    {
        return this;
    }

    /// <inheritdoc/>
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

