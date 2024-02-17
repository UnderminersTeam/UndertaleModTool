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

        private string AddParensIfNeeded(Expression argument, DecompileContext context)
        {
            string arg = argument.ToString(context);
            bool needsParens;
            if (arg[0] != '(' &&
                argument is not ExpressionConstant &&
                arg.Contains(' ', StringComparison.InvariantCulture))
                needsParens = true;
            else
                needsParens = false;


            if (argument is ExpressionTwo argumentAsBinaryExpression)
            {
                int outerPriorityLevel = Opcode switch
                {
                    UndertaleInstruction.Opcode.Mul or UndertaleInstruction.Opcode.Div => 2,
                    UndertaleInstruction.Opcode.Add or UndertaleInstruction.Opcode.Sub => 1,
                    _ => 0,
                };

                // First, no parentheses on this type
                arg = argumentAsBinaryExpression.ToStringNoParens(context);

                int argPriorityLevel = argumentAsBinaryExpression.Opcode switch
                {
                    UndertaleInstruction.Opcode.Mul or UndertaleInstruction.Opcode.Div => 2,
                    UndertaleInstruction.Opcode.Add or UndertaleInstruction.Opcode.Sub => 1,
                    _ => 0,
                };


                // Suppose we have "(arg1a argOp arg1b) opcode argument2", and are wondering whether the depicted parentheses are needed
                // If the argument's opcode is more highly-prioritized than our own, such as it being multiplication
                // while we use addition, then no parentheses are required.
                // If the argument's opcode doesn't fall into typical math rules (that is, I don't know my full order of operations)
                // Assume it has lower priority and needs parentheses to clarify.
                // Parentheses are also not needed for operations of the same level, especially string concatenation.
                needsParens = (outerPriorityLevel > argPriorityLevel);
                if (outerPriorityLevel == 0)
                    needsParens = true; // Better safe than sorry
            }

            return (needsParens ? String.Format("({0})", arg) : arg);
        }

        public string ToStringNoParens(DecompileContext context)
        {
            string arg1 = AddParensIfNeeded(Argument1, context);
            string arg2 = AddParensIfNeeded(Argument2, context);

            if (Opcode == UndertaleInstruction.Opcode.Or || Opcode == UndertaleInstruction.Opcode.And)
            {
                // If both arguments are a boolean type, this is a non-short-circuited logical condition
                if (Type == UndertaleInstruction.DataType.Boolean && Type2 == UndertaleInstruction.DataType.Boolean)
                    return String.Format("{0} {1}{1} {2}", arg1, OperationToPrintableString(Opcode), arg2);
            }
            return String.Format("{0} {1} {2}", arg1, OperationToPrintableString(Opcode), arg2);
        }

        public override string ToString(DecompileContext context)
        {
            return String.Format("({0})", ToStringNoParens(context));
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