using UndertaleModLib.Models;

namespace UndertaleModLib.Decompiler;

public static partial class Decompiler
{
    // This is basically ExpressionTwo, but allows for using symbols like && or || without creating new opcodes.
    public class ExpressionTernary : Expression
    {
        public Expression Condition;
        public Expression TrueExpression;
        public Expression FalseExpression;

        public ExpressionTernary(UndertaleInstruction.DataType targetType, Expression Condition, Expression argument1, Expression argument2)
        {
            this.Type = targetType;
            this.Condition = Condition;
            this.TrueExpression = argument1;
            this.FalseExpression = argument2;
        }

        internal override bool IsDuplicationSafe()
        {
            return Condition.IsDuplicationSafe() && TrueExpression.IsDuplicationSafe() && FalseExpression.IsDuplicationSafe();
        }

        public override Statement CleanStatement(DecompileContext context, BlockHLStatement block)
        {
            Condition = Condition?.CleanExpression(context, block);
            TrueExpression = TrueExpression?.CleanExpression(context, block);
            FalseExpression = FalseExpression?.CleanExpression(context, block);
            return this;
        }

        public override string ToString(DecompileContext context)
        {
            string condStr = Condition.ToString(context);
            if (TestNumber(TrueExpression, 1) && TestNumber(FalseExpression, 0))
                return condStr; // Default values, yes = true, no = false.

            return "(" + condStr + " ? " + TrueExpression.ToString(context) + " : " + FalseExpression.ToString(context) + ")";
        }

        internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
        {
            // The most likely, but probably rarely happens
            AssetIDType t = TrueExpression.DoTypePropagation(context, suggestedType);
            FalseExpression.DoTypePropagation(context, AssetIDType.Other);
            return t;
        }
    }
}