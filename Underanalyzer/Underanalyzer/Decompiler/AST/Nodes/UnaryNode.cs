/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

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

    public bool Duplicated { get; set; } = false;
    public bool Group { get; set; } = false;
    public DataType StackType { get; set; } = instruction.Type1;

    public string ConditionalTypeName => "Unary";
    public string ConditionalValue => ""; // TODO?

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

    public bool RequiresMultipleLines(ASTPrinter printer)
    {
        return Value.RequiresMultipleLines(printer);
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
