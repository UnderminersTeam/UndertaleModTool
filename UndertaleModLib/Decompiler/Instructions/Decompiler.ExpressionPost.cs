using UndertaleModLib.Models;

namespace UndertaleModLib.Decompiler;

public static partial class Decompiler
{
    // Represents post increments and decrements, such as a++ and a--.
    public class ExpressionPost : Expression
    {
        public UndertaleInstruction.Opcode Opcode;
        public Expression Variable;

        public ExpressionPost(UndertaleInstruction.Opcode opcode, Expression variable)
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
            Variable = Variable?.CleanStatement(context, block) as Expression;
            return this;
        }

        public override string ToString(DecompileContext context)
        {
            return Variable.ToString(context) + (Opcode == UndertaleInstruction.Opcode.Add ? "++" : "--");
        }

        internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
        {
            return Variable.DoTypePropagation(context, suggestedType);
        }
    }
}