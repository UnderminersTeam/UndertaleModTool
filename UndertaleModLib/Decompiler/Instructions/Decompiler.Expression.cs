using System.Globalization;
using UndertaleModLib.Models;

namespace UndertaleModLib.Decompiler;

public static partial class Decompiler
{
    // Represents all expressions.
    public abstract class Expression : Statement
    {
        public UndertaleInstruction.DataType Type;
        public bool WasDuplicated = false;

        // Helper function to convert opcode operations to "printable" strings.
        public static string OperationToPrintableString(UndertaleInstruction.Opcode op)
        {
            return op switch
            {
                UndertaleInstruction.Opcode.Mul => "*",
                UndertaleInstruction.Opcode.Div => "/",
                UndertaleInstruction.Opcode.Rem => "div",
                UndertaleInstruction.Opcode.Mod => "%",
                UndertaleInstruction.Opcode.Add => "+",
                UndertaleInstruction.Opcode.Sub => "-",
                UndertaleInstruction.Opcode.And => "&",
                UndertaleInstruction.Opcode.Or => "|",
                UndertaleInstruction.Opcode.Xor => "^",
                UndertaleInstruction.Opcode.Neg => "-",
                UndertaleInstruction.Opcode.Not => "~",
                UndertaleInstruction.Opcode.Shl => "<<",
                UndertaleInstruction.Opcode.Shr => ">>",
                _ => op.ToString().ToUpper(CultureInfo.InvariantCulture),
            };
        }

        // Helper function to convert opcode comparisons to "printable" strings.
        public static string OperationToPrintableString(UndertaleInstruction.ComparisonType op)
        {
            return op switch
            {
                UndertaleInstruction.ComparisonType.LT => "<",
                UndertaleInstruction.ComparisonType.LTE => "<=",
                UndertaleInstruction.ComparisonType.EQ => "==",
                UndertaleInstruction.ComparisonType.NEQ => "!=",
                UndertaleInstruction.ComparisonType.GTE => ">=",
                UndertaleInstruction.ComparisonType.GT => ">",
                _ => op.ToString().ToUpper(CultureInfo.InvariantCulture),
            };
        }

        internal virtual bool IsDuplicationSafe()
        {
            return false;
        }

        // Used for converting int constants to bool constants for 2.3.7+ code
        internal virtual void CastToBoolean()
        {
            Type = UndertaleInstruction.DataType.Boolean;
        }

        public Expression CleanExpression(DecompileContext context, BlockHLStatement block)
        {
            return CleanStatement(context, block) as Expression;
        }
    }
}