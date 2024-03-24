namespace UndertaleModLib.Decompiler;

public static partial class Decompiler
{
    // Represents a with statement ending (popping from or clearing the env stack).
    // This is not seen in high-level output.
    public class PopEnvStatement : Statement
    {
        public override string ToString(DecompileContext context)
        {
            return "popenv";
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