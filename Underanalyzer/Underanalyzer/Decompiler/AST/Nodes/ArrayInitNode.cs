/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;
using Underanalyzer.Decompiler.GameSpecific;

namespace Underanalyzer.Decompiler.AST;

/// <summary>
/// Represents an array literal in the AST.
/// </summary>
public class ArrayInitNode(List<IExpressionNode> elements) : IExpressionNode, IMacroResolvableNode, IConditionalValueNode
{
    /// <summary>
    /// List of elements in this array literal.
    /// </summary>
    public List<IExpressionNode> Elements { get; } = elements;

    /// <inheritdoc/>
    public bool Duplicated { get; set; } = false;

    /// <inheritdoc/>
    public bool Group { get; set; } = false;

    /// <inheritdoc/>
    public IGMInstruction.DataType StackType { get; set; } = IGMInstruction.DataType.Variable;

    /// <inheritdoc/>
    public string ConditionalTypeName => "ArrayInit";

    /// <inheritdoc/>
    public string ConditionalValue => "";

    /// <inheritdoc/>
    public IExpressionNode Clean(ASTCleaner cleaner)
    {
        for (int i = 0; i < Elements.Count; i++)
        {
            Elements[i] = Elements[i].Clean(cleaner);
        }
        return this;
    }

    /// <inheritdoc/>
    public IExpressionNode PostClean(ASTCleaner cleaner)
    {
        for (int i = 0; i < Elements.Count; i++)
        {
            Elements[i] = Elements[i].PostClean(cleaner);
        }
        return this;
    }

    /// <inheritdoc/>
    public void Print(ASTPrinter printer)
    {
        printer.Write('[');
        for (int i = 0; i < Elements.Count; i++)
        {
            Elements[i].Print(printer);
            if (i != Elements.Count - 1)
            {
                printer.Write(", ");
            }
        }
        printer.Write(']');
    }

    /// <inheritdoc/>
    public bool RequiresMultipleLines(ASTPrinter printer)
    {
        for (int i = 0; i < Elements.Count; i++)
        {
            if (Elements[i].RequiresMultipleLines(printer))
            {
                return true;
            }
        }
        return false;
    }

    /// <inheritdoc/>
    public IExpressionNode? ResolveMacroType(ASTCleaner cleaner, IMacroType type)
    {
        if (type is IMacroTypeArrayInit typeArrayInit)
        {
            return typeArrayInit.Resolve(cleaner, this);
        }
        return null;
    }

    /// <inheritdoc/>
    public IEnumerable<IBaseASTNode> EnumerateChildren()
    {
        return Elements;
    }
}
