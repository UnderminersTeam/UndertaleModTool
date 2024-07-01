using UndertaleModLib.Models;

namespace UndertaleModLib.Decompiler;

public static partial class Decompiler
{
    // Represents an expression converted to one of another data type - makes no difference on high-level code.
    public class ExpressionCast : Expression
    {
        public Expression Argument;

        public ExpressionCast(UndertaleInstruction.DataType targetType, Expression argument)
        {
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
            return Argument.ToString(context);
        }

        internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
        {
            return Argument.DoTypePropagation(context, suggestedType);
        }
    }
}