namespace UndertaleModLib.Decompiler;

public static partial class Decompiler
{
    // Represents a with statement beginning (pushing to the env stack).
    // This is not seen in high-level output.
    public class PushEnvStatement : Statement
    {
        public Expression NewEnv;

        public PushEnvStatement(Expression newEnv)
        {
            this.NewEnv = newEnv;
        }

        public override string ToString(DecompileContext context)
        {
            return "pushenv " + NewEnv;
        }

        public override Statement CleanStatement(DecompileContext context, BlockHLStatement block)
        {
            NewEnv = NewEnv?.CleanExpression(context, block);
            return this;
        }

        internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
        {
            NewEnv.DoTypePropagation(context, AssetIDType.GameObject);
            return suggestedType;
        }
    }
}