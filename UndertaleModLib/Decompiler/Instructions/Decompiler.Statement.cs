namespace UndertaleModLib.Decompiler;

public static partial class Decompiler
{
    // Represents all kinds of high-level decompilation results, to be stringified at the end.
    public abstract class Statement
    {
        public abstract string ToString(DecompileContext context);
        internal abstract AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType);
        public abstract Statement CleanStatement(DecompileContext context, BlockHLStatement block);
    }
}