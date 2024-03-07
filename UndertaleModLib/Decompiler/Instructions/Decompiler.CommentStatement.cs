namespace UndertaleModLib.Decompiler;

public static partial class Decompiler
{
    /// <summary>
    /// Represents a code comment, for debugging use (or minor error reporting). 
    /// </summary>
    public class CommentStatement : Statement
    {
        /// <summary>
        /// The code comment.
        /// </summary>
        public string Message;

        public CommentStatement(string message)
        {
            Message = message;
        }

        public override string ToString(DecompileContext context)
        {
            return "// " + Message;
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