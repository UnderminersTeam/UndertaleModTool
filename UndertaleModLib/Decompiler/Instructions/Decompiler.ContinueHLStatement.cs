namespace UndertaleModLib.Decompiler;

public static partial class Decompiler
{
    public class ContinueHLStatement : HLStatement
    {
        public override string ToString(DecompileContext context)
        {
            return "continue";
        }

        public override Statement CleanStatement(DecompileContext context, BlockHLStatement block)
        {
            return this;
        }
    }
}