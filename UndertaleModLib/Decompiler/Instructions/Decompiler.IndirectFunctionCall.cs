using System;
using System.Collections.Generic;
using System.Text;
using UndertaleModLib.Models;

namespace UndertaleModLib.Decompiler;

public static partial class Decompiler
{
    public class IndirectFunctionCall : FunctionCall
    {
        internal Expression FunctionThis;
        internal Expression Function;

        public IndirectFunctionCall(Expression func_this, Expression func, UndertaleInstruction.DataType returnType, List<Expression> args) : base(returnType, args)
        {
            this.FunctionThis = func_this;
            this.Function = func;
        }

        public override string ToString(DecompileContext context)
        {
            StringBuilder argumentString = new StringBuilder();
            foreach (Expression exp in Arguments)
            {
                if (argumentString.Length > 0)
                    argumentString.Append(", ");
                argumentString.Append(exp.ToString(context));
            }

            if (Function is FunctionDefinition)
                return String.Format("{0}({1})", Function.ToString(context), argumentString.ToString());

            return String.Format("{0}.{1}({2})", FunctionThis.ToString(context), Function.ToString(context), argumentString.ToString());
        }

        public override Statement CleanStatement(DecompileContext context, BlockHLStatement block)
        {
            FunctionThis = (Expression)FunctionThis?.CleanStatement(context, block);
            Function = (Expression)Function?.CleanStatement(context, block);
            for (var i = 0; i < Arguments.Count; i++)
                Arguments[i] = Arguments[i]?.CleanExpression(context, block);
            return this;
        }

        internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
        {
            FunctionThis.DoTypePropagation(context, AssetIDType.GameObject);
            Function.DoTypePropagation(context, suggestedType);
            AssetIDType[] args = new AssetIDType[Arguments.Count];
            for (var i = 0; i < Arguments.Count; i++)
                Arguments[i].DoTypePropagation(context, args[i]);

            return suggestedType;
        }
    }
}