namespace UndertaleModLib.Decompiler;

public static partial class Decompiler
{
    /// <summary>
    /// Represents all kinds of high-level decompilation results, to be stringified at the end. 
    /// </summary>
    public abstract class Statement
    {
        /// <summary>
        /// Creates a string representation of the statement.
        /// </summary>
        /// <param name="context">The decompile context of the current code entry.</param>
        /// <returns>A string representation of the statement.</returns>
        public abstract string ToString(DecompileContext context);
        
        
        internal abstract AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType);
        public abstract Statement CleanStatement(DecompileContext context, BlockHLStatement block);
    }
}