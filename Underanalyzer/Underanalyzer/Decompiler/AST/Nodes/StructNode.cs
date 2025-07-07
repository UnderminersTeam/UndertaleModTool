/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System;
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

    public bool Duplicated { get; set; } = false;
    public bool Group { get; set; } = false;
    public IGMInstruction.DataType StackType { get; set; } = IGMInstruction.DataType.Variable;
    public bool SemicolonAfter => false;
    public bool EmptyLineBefore => false;
    public bool EmptyLineAfter => false;
    public ASTFragmentContext FragmentContext { get; } = fragmentContext;

    public string ConditionalTypeName => "Struct";
    public string ConditionalValue => "";

    public IExpressionNode Clean(ASTCleaner cleaner)
    {
        Body.Clean(cleaner);
        return this;
    }

    IStatementNode IASTNode<IStatementNode>.Clean(ASTCleaner cleaner)
    {
        throw new NotImplementedException();
    }

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

    public bool RequiresMultipleLines(ASTPrinter printer)
    {
        return Body.Children.Count != 0;
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
