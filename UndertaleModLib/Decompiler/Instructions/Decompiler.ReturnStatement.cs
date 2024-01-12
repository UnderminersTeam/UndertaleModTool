using System;
using System.Linq;
using UndertaleModLib.Models;

namespace UndertaleModLib.Decompiler;

public static partial class Decompiler
{
    // Represents a high-level return statement, or an exit in Studio version < 2 if there is no value.
    public class ReturnStatement : Statement
    {
        public Expression Value;

        public ReturnStatement(Expression value)
        {
            Value = value;
        }

        public override string ToString(DecompileContext context)
        {
            if (Value != null)
            {
                if (AssetTypeResolver.return_types.ContainsKey(context.TargetCode.Name.Content))
                    Value.DoTypePropagation(context, AssetTypeResolver.return_types[context.TargetCode.Name.Content]);
                if (context.GlobalContext.Data != null && !DecompileContext.GMS2_3)
                {
                    // We might be decompiling a legacy script - resolve it's name
                    UndertaleScript script = context.GlobalContext.Data.Scripts.FirstOrDefault(x => x.Code == context.TargetCode);
                    if (script != null && AssetTypeResolver.return_types.ContainsKey(script.Name.Content))
                        Value.DoTypePropagation(context, AssetTypeResolver.return_types[script.Name.Content]);
                }

                string cleanVal = Value.ToString(context);
                if (cleanVal.EndsWith("\n", StringComparison.InvariantCulture))
                    cleanVal = cleanVal.Substring(0, cleanVal.Length - 1);

                return "return " + cleanVal + ";";
            }
            else
                return (context.GlobalContext.Data?.IsGameMaker2() ?? false ? "return;" : "exit");
        }

        public override Statement CleanStatement(DecompileContext context, BlockHLStatement block)
        {
            Value = Value?.CleanExpression(context, block);
            return this;
        }

        internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
        {
            return Value?.DoTypePropagation(context, suggestedType) ?? suggestedType;
        }
    }
}