using System;
using System.Collections.Generic;
using System.Globalization;
using UndertaleModLib.Models;

namespace UndertaleModLib.Decompiler;

public static partial class Decompiler
{
    // Represents a variable in an expression, of any type.
    public class ExpressionVar : Expression
    {
        public UndertaleVariable Var;
        public Expression InstType; // UndertaleInstruction.InstanceType
        public UndertaleInstruction.VariableType VarType;
        public List<Expression> ArrayIndices = null;
        public UndertaleInstruction.Opcode Opcode;

        public ExpressionVar(UndertaleVariable var, Expression instType, UndertaleInstruction.VariableType varType)
        {
            Var = var;
            InstType = instType;
            VarType = varType;
        }

        internal override bool IsDuplicationSafe()
        {
            bool res = (InstType?.IsDuplicationSafe() ?? true);

            if (ArrayIndices == null)
                return res;
            foreach (Expression e in ArrayIndices)
                res &= (e?.IsDuplicationSafe() ?? true);

            return res;
        }

        public override Statement CleanStatement(DecompileContext context, BlockHLStatement block)
        {
            if (Var.Name?.Content == "$$$$temp$$$$" && context.CompilerTempVar != null)
            {
                block.Statements.Remove(context.CompilerTempVar);
                return context.CompilerTempVar.Value.CleanStatement(context, block);
            }

            InstType = InstType?.CleanExpression(context, block);
            if (ArrayIndices == null)
                return this;
            foreach (Expression e in ArrayIndices)
                e?.CleanExpression(context, block);
            return this;
        }

        public static Tuple<Expression, Expression> Decompile2DArrayIndex(Expression index)
        {
            Expression ind1 = index;
            Expression ind2 = null;
            if (ind1 is ExpressionTwo && (ind1 as ExpressionTwo).Opcode == UndertaleInstruction.Opcode.Add)
            {
                var arg1 = (ind1 as ExpressionTwo).Argument1;
                var arg2 = (ind1 as ExpressionTwo).Argument2;
                if (arg1 is ExpressionTwo && (arg1 as ExpressionTwo).Opcode == UndertaleInstruction.Opcode.Mul)
                {
                    var arg11 = (arg1 as ExpressionTwo).Argument1;
                    var arg12 = (arg1 as ExpressionTwo).Argument2;
                    if (arg12 is ExpressionConstant && (arg12 as ExpressionConstant).Value is int && (int)(arg12 as ExpressionConstant).Value == 32000)
                    {
                        ind1 = arg11;
                        ind2 = arg2;
                    }
                }
            }
            return new Tuple<Expression, Expression>(ind1, ind2);
        }

        public override string ToString(DecompileContext context)
        {
            string name = Var.Name.Content;
            if (ArrayIndices != null)
            {
                if (DecompileContext.GMS2_3 == true)
                {
                    if (name == "argument" && context.DecompilingStruct && context.ArgumentReplacements != null && ArrayIndices.Count == 1)
                    {
                        var replacements = context.ArgumentReplacements;
                        if (int.TryParse(ArrayIndices[0].ToString(context), out int index) && index >= 0 && index < replacements.Count && this != replacements[index])
                            return replacements[index].ToString(context);
                    }
                    foreach (Expression e in ArrayIndices)
                        name += "[" + e.ToString(context) + "]";

                }
                else
                {
                    if (ArrayIndices.Count == 2 && ArrayIndices[0] != null && ArrayIndices[1] != null)
                        name += "[" + ArrayIndices[0].ToString(context) + ", " + ArrayIndices[1].ToString(context) + "]";
                    else if (ArrayIndices[0] != null)
                        name += "[" + ArrayIndices[0].ToString(context) + "]";
                }
            }

            // NOTE: The "var" prefix is handled in Decompiler.Decompile.

            if (VarType == UndertaleInstruction.VariableType.Instance)
            {
                if (InstType is ExpressionConstant c)
                {
                    int? val = ExpressionConstant.ConvertToInt(c.Value);
                    if (val == null)
                        throw new InvalidOperationException("Unable to parse the instance ID to int");
                    // TODO: This is a reference to an object instance in the room. Resolving these is non-trivial since you don't exactly have a reference to the room where this script is used when decompiling...
                    return (val + 100000) + "." + name;
                }
                else throw new InvalidOperationException("Instance variable type used with non-const InstType"); // TODO: can this happen?
            }
            if (InstType is ExpressionConstant constant) // Only use "global." and "other.", not "self." or "local.". GMS doesn't recognize those.
            {
                string prefix = InstType.ToString(context) + ".";
                if (!(constant.Value is Int64))
                {
                    int? val = ExpressionConstant.ConvertToInt(constant.Value);
                    if (val != null)
                    {
                        if (constant.AssetType == AssetIDType.GameObject && val < 0)
                        {
                            UndertaleInstruction.InstanceType instanceType = (UndertaleInstruction.InstanceType)val;
                            prefix = (instanceType == UndertaleInstruction.InstanceType.Global || instanceType == UndertaleInstruction.InstanceType.Other) ? prefix.ToLower(CultureInfo.InvariantCulture) : "";
                        }
                    }
                }
                return prefix + name;
            }
            else if (InstType is ExpressionCast cast && !(cast.Argument is ExpressionVar))
            {
                return "(" + InstType.ToString(context) + ")." + name; // Make sure to put parentheses around these cases
            }

            return InstType.ToString(context) + "." + name;
        }

        internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
        {
            InstType?.DoTypePropagation(context, AssetIDType.GameObject);
            if (ArrayIndices != null)
            {
                foreach (Expression e in ArrayIndices)
                    e?.DoTypePropagation(context, AssetIDType.Other);
            }

            AssetIDType current = context.assetTypes.ContainsKey(Var) ? context.assetTypes[Var] : AssetIDType.Other;
            if (current == AssetIDType.Other && suggestedType != AssetIDType.Other)
                current = suggestedType;
            AssetIDType builtinSuggest = AssetTypeResolver.AnnotateTypeForVariable(context, Var.Name.Content);
            if (builtinSuggest != AssetIDType.Other)
                current = builtinSuggest;

            if ((VarType != UndertaleInstruction.VariableType.Array || (ArrayIndices != null && !(ArrayIndices[0] is ExpressionConstant))))
                context.assetTypes[Var] = current; // This is a messy fix to arrays messing up exported variable types.
            return current;
        }
    }
}