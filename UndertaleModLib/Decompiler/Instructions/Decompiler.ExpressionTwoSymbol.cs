using System;
using UndertaleModLib.Models;

namespace UndertaleModLib.Decompiler;

public static partial class Decompiler
{
    // This is basically ExpressionTwo, but allows for using symbols like && or || without creating new opcodes.
    public class ExpressionTwoSymbol : Expression
    {
        public string Symbol;
        public Expression Argument1;
        public Expression Argument2;

        public ExpressionTwoSymbol(string symbol, UndertaleInstruction.DataType targetType, Expression argument1, Expression argument2)
        {
            this.Symbol = symbol;
            this.Type = targetType;
            this.Argument1 = argument1;
            this.Argument2 = argument2;
        }

        internal override bool IsDuplicationSafe()
        {
            return Argument1.IsDuplicationSafe() && Argument2.IsDuplicationSafe();
        }

        public override Statement CleanStatement(DecompileContext context, BlockHLStatement block)
        {
            Argument1 = Argument1?.CleanExpression(context, block);
            Argument2 = Argument2?.CleanExpression(context, block);
            return this;
        }

        public override string ToString(DecompileContext context)
        {
            string arg1;
            if (Argument1 is ExpressionTwoSymbol && (Argument1 as ExpressionTwoSymbol).Symbol == Symbol)
                arg1 = (Argument1 as ExpressionTwoSymbol).ToStringNoParen(context);
            else
                arg1 = Argument1.ToString(context);
            string arg2;
            if (Argument2 is ExpressionTwoSymbol && (Argument2 as ExpressionTwoSymbol).Symbol == Symbol)
                arg2 = (Argument2 as ExpressionTwoSymbol).ToStringNoParen(context);
            else
                arg2 = Argument2.ToString(context);
            return String.Format("({0} {1} {2})", arg1, Symbol, arg2);
        }

        public string ToStringNoParen(DecompileContext context)
        {
            string arg1;
            if (Argument1 is ExpressionTwoSymbol && (Argument1 as ExpressionTwoSymbol).Symbol == Symbol)
                arg1 = (Argument1 as ExpressionTwoSymbol).ToStringNoParen(context);
            else
                arg1 = Argument1.ToString(context);
            string arg2;
            if (Argument2 is ExpressionTwoSymbol && (Argument2 as ExpressionTwoSymbol).Symbol == Symbol)
                arg2 = (Argument2 as ExpressionTwoSymbol).ToStringNoParen(context);
            else
                arg2 = Argument2.ToString(context);
            return String.Format("{0} {1} {2}", arg1, Symbol, arg2);
        }

        internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
        {
            // The most likely, but probably rarely happens
            AssetIDType t = Argument1.DoTypePropagation(context, suggestedType);
            Argument2.DoTypePropagation(context, AssetIDType.Other);
            return t;
        }
    }
}