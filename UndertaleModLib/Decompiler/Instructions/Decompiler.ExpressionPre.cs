using UndertaleModLib.Models;

namespace UndertaleModLib.Decompiler;

public static partial class Decompiler
{
    // Represents pre increments and decrements, such as ++a and --a.
    public class ExpressionPre : Expression
    {
        public UndertaleInstruction.Opcode Opcode;
        public Expression Variable;

        public ExpressionPre(UndertaleInstruction.Opcode opcode, Expression variable)
        {
            Opcode = opcode;
            Variable = variable;
        }

        internal override bool IsDuplicationSafe()
        {
            return Variable.IsDuplicationSafe();
        }

        public override Statement CleanStatement(DecompileContext context, BlockHLStatement block)
        {
            Variable = Variable?.CleanExpression(context, block);
            return this;
        }

        public override string ToString(DecompileContext context)
        {
            return (Opcode == UndertaleInstruction.Opcode.Add ? "++" : "--") + Variable.ToString(context);
        }

        internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
        {
            return Variable.DoTypePropagation(context, suggestedType);
        }
    }
}