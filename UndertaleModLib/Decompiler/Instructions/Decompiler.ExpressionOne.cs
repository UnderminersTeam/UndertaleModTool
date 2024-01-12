using System;
using UndertaleModLib.Models;

namespace UndertaleModLib.Decompiler;

public static partial class Decompiler
{
    // Represents a unary expression.
    public class ExpressionOne : Expression
    {
        public UndertaleInstruction.Opcode Opcode;
        public Expression Argument;

        public ExpressionOne(UndertaleInstruction.Opcode opcode, UndertaleInstruction.DataType targetType, Expression argument)
        {
            this.Opcode = opcode;
            this.Type = targetType;
            this.Argument = argument;
        }

        internal override bool IsDuplicationSafe()
        {
            return Argument.IsDuplicationSafe();
        }

        public override Statement CleanStatement(DecompileContext context, BlockHLStatement block)
        {
            Argument = Argument?.CleanExpression(context, block);
            return this;
        }

        public override string ToString(DecompileContext context)
        {
            string op = OperationToPrintableString(Opcode);
            if (Opcode == UndertaleInstruction.Opcode.Not && Type == UndertaleInstruction.DataType.Boolean)
                op = "!"; // This is a logical negation instead, see #93
            string arg = Argument.ToString(context);
            if (arg.Contains(' ', StringComparison.InvariantCulture))
                return String.Format("({0}({1}))", op, arg);
            return String.Format("({0}{1})", op, arg);
        }

        internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
        {
            return Argument.DoTypePropagation(context, suggestedType);
        }
    }
}