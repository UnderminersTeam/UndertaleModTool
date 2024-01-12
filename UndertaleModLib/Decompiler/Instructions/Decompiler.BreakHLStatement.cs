namespace UndertaleModLib.Decompiler;

public static partial class Decompiler
{
    public class BreakHLStatement : HLStatement
    {
        public override string ToString(DecompileContext context)
        {
            return "break";
        }

        public override Statement CleanStatement(DecompileContext context, BlockHLStatement block)
        {
            return this;
        }
    }
}