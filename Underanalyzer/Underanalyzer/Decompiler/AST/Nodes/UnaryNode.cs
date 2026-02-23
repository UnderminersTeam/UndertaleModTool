/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;
using Underanalyzer.Decompiler.GameSpecific;
using static Underanalyzer.IGMInstruction;

namespace Underanalyzer.Decompiler.AST;

/// <summary>
/// Represents a unary expression, such as not (!) and bitwise negation (~).
/// </summary>
public class UnaryNode(IExpressionNode value, IGMInstruction instruction) : IExpressionNode, IConditionalValueNode
{
    /// <summary>
    /// The expression that this operation is being performed on.
    /// </summary>
    public IExpressionNode Value { get; private set; } = value;

    /// <summary>
    /// The instruction that performs this operation, as in the code.
    /// </summary>
    public IGMInstruction Instruction { get; } = instruction;

    /// <inheritdoc/>
    public bool Duplicated { get; set; } = false;

    /// <inheritdoc/>
    public bool Group { get; set; } = false;

    /// <inheritdoc/>
    public DataType StackType { get; set; } = instruction.Type1;

    /// <inheritdoc/>
    public string ConditionalTypeName => "Unary";

    /// <inheritdoc/>
    public string ConditionalValue => ""; // TODO?

    /// <inheritdoc/>
    public IExpressionNode Clean(ASTCleaner cleaner)
    {
        Value = Value.Clean(cleaner);

        // Ensure operation applies to entire node
        if (Value is IMultiExpressionNode)
        {
            Value.Group = true;
        }

        return this;
    }

    /// <inheritdoc/>
    public IExpressionNode PostClean(ASTCleaner cleaner)
    {
        Value = Value.PostClean(cleaner);
        return this;
    }

    /// <inheritdoc/>
    public void Print(ASTPrinter printer)
    {
        if (Group)
        {
            printer.Write('(');
        }

        char op = Instruction switch
        {
            { Kind: Opcode.Negate } => '-',
            { Kind: Opcode.Not, Type1: DataType.Boolean } => '!',
            { Kind: Opcode.Not } => '~',
            _ => throw new DecompilerException("Failed to match unary instruction to character")
        };
        printer.Write(op);

        Value.Print(printer);

        if (Group)
        {
            printer.Write(')');
        }
    }

    /// <inheritdoc/>
    public bool RequiresMultipleLines(ASTPrinter printer)
    {
        return Value.RequiresMultipleLines(printer);
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
        yield return Value;
    }
}
