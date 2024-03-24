using System;

namespace UndertaleModLib.Decompiler;

public static partial class Decompiler
{
    public abstract class HLStatement : Statement
    {
        internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
        {
            throw new NotImplementedException();
        }
    };
}