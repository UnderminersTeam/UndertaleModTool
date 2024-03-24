using System;
using UndertaleModLib.Models;

namespace UndertaleModLib.Decompiler;

public static partial class Decompiler
{
    // Represents a binary expression.
    public class ExpressionTwo : Expression
    {
        public UndertaleInstruction.Opcode Opcode;
        public UndertaleInstruction.DataType Type2;
        public Expression Argument1;
        public Expression Argument2;

        public ExpressionTwo(UndertaleInstruction.Opcode opcode, UndertaleInstruction.DataType targetType, UndertaleInstruction.DataType type2, Expression argument1, Expression argument2)
        {
            this.Opcode = opcode;
            this.Type = targetType;
            this.Type2 = type2;
            this.Argument1 = argument1;
            this.Argument2 = argument2;
        }

        internal override bool IsDuplicationSafe()
        {
            return Argument1.IsDuplicationSafe() && Argument2.IsDuplicationSafe();
        }

        public override Statement CleanStatement(DecompileContext context, BlockHLStatement block)
        {
            Argument1 = Argument1?.CleanExpression(context, block);
            Argument2 = Argument2?.CleanExpression(context, block);
            return this;
        }

        public override string ToString(DecompileContext context)
        {
            if (Opcode == UndertaleInstruction.Opcode.Or || Opcode == UndertaleInstruction.Opcode.And)
            {
                // If both arguments are a boolean type, this is a non-short-circuited logical condition
                if (Type == UndertaleInstruction.DataType.Boolean && Type2 == UndertaleInstruction.DataType.Boolean)
                    return String.Format("({0} {1}{1} {2})", Argument1.ToString(context), OperationToPrintableString(Opcode), Argument2.ToString(context));
            }
            return String.Format("({0} {1} {2})", Argument1.ToString(context), OperationToPrintableString(Opcode), Argument2.ToString(context));
        }

        internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
        {
            // The most likely, but probably rarely happens
            AssetIDType t = Argument1.DoTypePropagation(context, suggestedType);
            Argument2.DoTypePropagation(context, AssetIDType.Other);
            return t;
        }
    }
}