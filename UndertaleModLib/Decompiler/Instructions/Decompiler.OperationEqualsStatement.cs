using System;
using UndertaleModLib.Models;

namespace UndertaleModLib.Decompiler;

public static partial class Decompiler
{
    // Represents a high-level operation-equals statement, such as a += 1.
    public class OperationEqualsStatement : Statement
    {
        public ExpressionVar Destination;
        public UndertaleInstruction.Opcode Operation;
        public Expression Value;

        public OperationEqualsStatement(ExpressionVar destination, UndertaleInstruction.Opcode operation, Expression value)
        {
            Destination = destination;
            Operation = operation;
            Value = value;
        }

        public override string ToString(DecompileContext context)
        {
            return String.Format("{0} {1}= {2}", Destination.ToString(context), Expression.OperationToPrintableString(Operation), Value.ToString(context));
        }

        public override Statement CleanStatement(DecompileContext context, BlockHLStatement block)
        {
            Value = Value?.CleanExpression(context, block);
            return this;
        }

        internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
        {
            return Value.DoTypePropagation(context, Destination.DoTypePropagation(context, suggestedType));
        }
    }
}