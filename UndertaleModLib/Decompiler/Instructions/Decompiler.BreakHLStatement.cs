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

        internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
        {
            return suggestedType;
        }
    }
}