namespace UndertaleModLib.Decompiler;

public static partial class Decompiler
{
    public class WithHLStatement : HLStatement
    {
        public Expression NewEnv;
        public BlockHLStatement Block;

        public override string ToString(DecompileContext context)
        {
            return "with (" + NewEnv.ToString(context) + ")\n" + context.Indentation + Block.ToString(context);
        }

        public override Statement CleanStatement(DecompileContext context, BlockHLStatement block)
        {
            NewEnv = NewEnv?.CleanExpression(context, block);
            Block = Block?.CleanBlockStatement(context);
            return this;
        }

        internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
        {
            NewEnv.DoTypePropagation(context, AssetIDType.GameObject);
            return Block.DoTypePropagation(context, AssetIDType.Other);
        }
    }
}