/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using Underanalyzer.Decompiler.GameSpecific;

namespace Underanalyzer.Decompiler.AST;

/// <summary>
/// A struct declaration/instantiation within the AST.
/// </summary>
public class StructNode(BlockNode body, ASTFragmentContext fragmentContext) : IFragmentNode, IExpressionNode, IConditionalValueNode
{
    /// <summary>
    /// The body of the struct (typically a block with assignments).
    /// </summary>
    public BlockNode Body { get; private set; } = body;

    /// <inheritdoc/>
    public bool Duplicated { get; set; } = false;

    /// <inheritdoc/>
    public bool Group { get; set; } = false;

    /// <inheritdoc/>
    public IGMInstruction.DataType StackType { get; set; } = IGMInstruction.DataType.Variable;

    /// <inheritdoc/>
    public bool SemicolonAfter => false;

    /// <inheritdoc/>
    public bool EmptyLineBefore { get => false; set => _ = value; }

    /// <inheritdoc/>
    public bool EmptyLineAfter { get => false; set => _ = value; }

    /// <inheritdoc/>
    public ASTFragmentContext FragmentContext { get; } = fragmentContext;

    /// <inheritdoc/>
    public string ConditionalTypeName => "Struct";

    /// <inheritdoc/>
    public string ConditionalValue => "";

    /// <inheritdoc/>
    public IExpressionNode Clean(ASTCleaner cleaner)
    {
        Body.Clean(cleaner);
        return this;
    }

    /// <inheritdoc/>
    public IExpressionNode PostClean(ASTCleaner cleaner)
    {
        Body.PostCleanStruct(cleaner);
        return this;
    }

    /// <inheritdoc/>
    IStatementNode IASTNode<IStatementNode>.Clean(ASTCleaner cleaner)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    IStatementNode IASTNode<IStatementNode>.PostClean(ASTCleaner cleaner)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void Print(ASTPrinter printer)
    {
        if (Body.Children.Count == 0)
        {
            // Don't print a normal block in this case; condense down
            printer.Write("{}");
        }
        else
        {
            Body.Print(printer);
        }
    }

    /// <inheritdoc/>
    public bool RequiresMultipleLines(ASTPrinter printer)
    {
        return Body.Children.Count != 0;
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
        yield return Body;
    }
}
