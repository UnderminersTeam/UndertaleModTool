/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;
using Underanalyzer.Decompiler.GameSpecific;

namespace Underanalyzer.Decompiler.AST;

/// <summary>
/// Represents a nullish coalescing operator (??) in the AST.
/// </summary>
public class NullishCoalesceNode(IExpressionNode left, IExpressionNode right) : IMultiExpressionNode, IConditionalValueNode
{
    /// <summary>
    /// The left side of the operator.
    /// </summary>
    public IExpressionNode Left { get; private set; } = left;

    /// <summary>
    /// The right side of the operator.
    /// </summary>
    public IExpressionNode Right { get; private set; } = right;

    /// <inheritdoc/>
    public bool Duplicated { get; set; }

    /// <inheritdoc/>
    public bool Group { get; set; } = false;

    /// <inheritdoc/>
    public IGMInstruction.DataType StackType { get; set; } = IGMInstruction.DataType.Variable;

    /// <inheritdoc/>
    public string ConditionalTypeName => "NullishCoalesce";

    /// <inheritdoc/>
    public string ConditionalValue => ""; // TODO?

    /// <inheritdoc/>
    public IExpressionNode Clean(ASTCleaner cleaner)
    {
        Left = Left.Clean(cleaner);
        Right = Right.Clean(cleaner);

        if (Left is IMultiExpressionNode)
        {
            Left.Group = true;
        }
        if (Right is IMultiExpressionNode)
        {
            Right.Group = true;
        }

        return this;
    }

    /// <inheritdoc/>
    public IExpressionNode PostClean(ASTCleaner cleaner)
    {
        Left = Left.PostClean(cleaner);
        Right = Right.PostClean(cleaner);
        return this;
    }

    /// <inheritdoc/>
    public void Print(ASTPrinter printer)
    {
        if (Group)
        {
            printer.Write('(');
        }

        Left.Print(printer);
        printer.Write(" ?? ");
        Right.Print(printer);

        if (Group)
        {
            printer.Write(')');
        }
    }

    /// <inheritdoc/>
    public bool RequiresMultipleLines(ASTPrinter printer)
    {
        return Left.RequiresMultipleLines(printer) || Right.RequiresMultipleLines(printer);
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
        yield return Left;
        yield return Right;
    }
}
