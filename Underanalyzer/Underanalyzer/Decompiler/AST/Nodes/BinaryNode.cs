/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System;
using Underanalyzer.Decompiler.GameSpecific;
using static Underanalyzer.IGMInstruction;

namespace Underanalyzer.Decompiler.AST;

/// <summary>
/// Represents a binary expression, such as basic two-operand arithmetic operations.
/// </summary>
public class BinaryNode : IMultiExpressionNode, IMacroResolvableNode, IConditionalValueNode
{
    /// <summary>
    /// Left side of the binary operation.
    /// </summary>
    public IExpressionNode Left { get; private set; }

    /// <summary>
    /// Right side of the binary operation.
    /// </summary>
    public IExpressionNode Right { get; private set; }
    
    /// <summary>
    /// The instruction that performs this operation, as in the code.
    /// </summary>
    public IGMInstruction Instruction { get; }

    public bool Duplicated { get; set; } = false;
    public bool Group { get; set; } = false;
    public IGMInstruction.DataType StackType { get; set; }

    public string ConditionalTypeName => "Binary";
    public string ConditionalValue => ""; // TODO?

    public BinaryNode(IExpressionNode left, IExpressionNode right, IGMInstruction instruction)
    {
        Left = left;
        Right = right;  
        Instruction = instruction;

        if (Instruction.Kind == Opcode.Compare)
        {
            // For comparison operations, the result is always a boolean.
            StackType = DataType.Boolean;
        }
        else
        {
            // Type1 and Type2 on the instruction represent the data types of Left and Right on the stack.
            // Choose whichever type has a higher bias, or if equal, the smaller numerical data type value.
            int bias1 = StackTypeBias(instruction.Type1);
            int bias2 = StackTypeBias(instruction.Type2);
            if (bias1 == bias2)
            {
                StackType = (DataType)Math.Min((byte)instruction.Type1, (byte)instruction.Type2);
            }
            else
            {
                StackType = (bias1 > bias2) ? instruction.Type1 : instruction.Type2;
            }
        }
    }

    private static int StackTypeBias(DataType type)
    {
        return type switch
        {
            DataType.Int32 or DataType.Boolean or DataType.String => 0,
            DataType.Double or DataType.Int64 => 1,
            DataType.Variable => 2,
            _ => throw new DecompilerException("Unknown stack type in binary operation")
        };
    }

    private void CheckGroup(IExpressionNode node)
    {
        // TODO: verify that this works for all cases
        if (node is BinaryNode binary)
        {
            if (binary.Instruction.Kind != Instruction.Kind)
            {
                binary.Group = true;
            }
            if (binary.Instruction.Kind == Opcode.Compare)
            {
                binary.Group = true;
            }
        }
        else if (node is IMultiExpressionNode)
        {
            node.Group = true;
        }
    }

    public IExpressionNode Clean(ASTCleaner cleaner)
    {
        Left = Left.Clean(cleaner);
        Right = Right.Clean(cleaner);

        // Resolve macro types carrying between left and right nodes
        if (Left is IMacroTypeNode leftTypeNode && Right is IMacroResolvableNode rightResolvableNode &&
            leftTypeNode.GetExpressionMacroType(cleaner) is IMacroType leftMacroType &&
            rightResolvableNode.ResolveMacroType(cleaner, leftMacroType) is IExpressionNode rightResolved)
        {
            Right = rightResolved;
        }
        else if (Right is IMacroTypeNode rightTypeNode && Left is IMacroResolvableNode leftResolvableNode &&
                 rightTypeNode.GetExpressionMacroType(cleaner) is IMacroType rightMacroType &&
                 leftResolvableNode.ResolveMacroType(cleaner, rightMacroType) is IExpressionNode leftResolved)
        {
            Left = leftResolved;
        }

        CheckGroup(Left);
        CheckGroup(Right);

        // If the right side of the binary is another BinaryNode, that implies it was evaluated earlier
        // than this one, meaning it should have parentheses around it.
        if (Right is BinaryNode)
        {
            Right.Group = true;
        }

        return this;
    }

    public void Print(ASTPrinter printer)
    {
        if (Group)
        {
            printer.Write('(');
        }

        Left.Print(printer);

        string op = Instruction switch
        {
            { Kind: Opcode.Add } => " + ",
            { Kind: Opcode.Subtract } => " - ",
            { Kind: Opcode.Multiply } => " * ",
            { Kind: Opcode.Divide } => " / ",
            { Kind: Opcode.GMLDivRemainder } => " div ",
            { Kind: Opcode.GMLModulo } => " % ",
            { Kind: Opcode.And, Type1: DataType.Boolean, Type2: DataType.Boolean } => " && ",
            { Kind: Opcode.And } => " & ",
            { Kind: Opcode.Or, Type1: DataType.Boolean, Type2: DataType.Boolean } => " || ",
            { Kind: Opcode.Or } => " | ",
            { Kind: Opcode.Xor, Type1: DataType.Boolean, Type2: DataType.Boolean } => " ^^ ",
            { Kind: Opcode.Xor } => " ^ ",
            { Kind: Opcode.ShiftLeft } => " << ",
            { Kind: Opcode.ShiftRight } => " >> ",
            { Kind: Opcode.Compare, ComparisonKind: ComparisonType.LesserThan } => " < ",
            { Kind: Opcode.Compare, ComparisonKind: ComparisonType.LesserEqualThan } => " <= ",
            { Kind: Opcode.Compare, ComparisonKind: ComparisonType.EqualTo } => " == ",
            { Kind: Opcode.Compare, ComparisonKind: ComparisonType.NotEqualTo } => " != ",
            { Kind: Opcode.Compare, ComparisonKind: ComparisonType.GreaterEqualThan } => " >= ",
            { Kind: Opcode.Compare, ComparisonKind: ComparisonType.GreaterThan } => " > ",
            _ => throw new DecompilerException("Failed to match binary instruction to string")
        };
        printer.Write(op);

        Right.Print(printer);

        if (Group)
        {
            printer.Write(')');
        }
    }

    public bool RequiresMultipleLines(ASTPrinter printer)
    {
        return Left.RequiresMultipleLines(printer) || Right.RequiresMultipleLines(printer);
    }

    public IExpressionNode? ResolveMacroType(ASTCleaner cleaner, IMacroType type)
    {
        bool didAnything = false;

        if (Left is IMacroResolvableNode leftResolvable &&
            leftResolvable.ResolveMacroType(cleaner, type) is IExpressionNode leftResolved)
        {
            Left = leftResolved;
            didAnything = true;
        }
        if (Right is IMacroResolvableNode rightResolvable &&
            rightResolvable.ResolveMacroType(cleaner, type) is IExpressionNode rightResolved)
        {
            Right = rightResolved;
            didAnything = true;
        }

        return didAnything ? this : null;
    }
}
