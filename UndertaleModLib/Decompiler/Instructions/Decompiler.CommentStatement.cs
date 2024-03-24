namespace UndertaleModLib.Decompiler;

public static partial class Decompiler
{
    // Represents a code comment, for debugging use (or minor error reporting).
    public class CommentStatement : Statement
    {
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