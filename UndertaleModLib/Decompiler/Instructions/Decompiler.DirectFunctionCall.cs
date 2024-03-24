using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UndertaleModLib.Models;

namespace UndertaleModLib.Decompiler;

public static partial class Decompiler
{
    public class DirectFunctionCall : FunctionCall
    {
        internal string OverridenName = string.Empty;
        internal UndertaleFunction Function;

        public DirectFunctionCall(string overridenName, UndertaleFunction function, UndertaleInstruction.DataType returnType, List<Expression> args) : base(returnType, args)
        {
            this.OverridenName = overridenName;
            this.Function = function;
        }

        public DirectFunctionCall(UndertaleFunction function, UndertaleInstruction.DataType returnType, List<Expression> args) : base(returnType, args)
        {
            this.Function = function;
        }

        public override string ToString(DecompileContext context)
        {
            StringBuilder argumentString = new StringBuilder();

            if (Function.Name.Content == "@@NewGMLObject@@") // Creating a new "object" via a constructor OR this is a struct definition
            {
                context.currentFunction = this;

                string constructor;
                var actualArgs = Arguments.Skip(1).ToList();
                if (Arguments[0] is FunctionDefinition def)
                {
                    if (def.Subtype == FunctionDefinition.FunctionType.Struct) // Struct moment
                    {
                        def.PopulateArguments(actualArgs);
                        return def.ToString(context);
                    }
                    else
                        constructor = def.FunctionBodyCodeEntry.Name.Content;
                }
                else
                    constructor = Arguments[0].ToString(context);

                if (constructor.StartsWith("gml_Script_", StringComparison.InvariantCulture))
                    constructor = constructor.Substring(11);
                if (constructor.EndsWith(context.TargetCode.Name.Content, StringComparison.InvariantCulture))
                    constructor = constructor.Substring(0, constructor.Length - context.TargetCode.Name.Content.Length - 1);

                if (AssetTypeResolver.builtin_funcs.TryGetValue(constructor, out AssetIDType[] types))
                {
                    int index = 0;
                    foreach (var arg in actualArgs)
                        arg.DoTypePropagation(context, types[index++]);
                }

                // Don't ask
                if (Arguments[0] is ExpressionCast cast &&
                    cast.Argument is ExpressionConstant constant &&
                    constant.Value is UndertaleInstruction.Reference<UndertaleFunction> reference)
                {
                    var call = new DirectFunctionCall(reference.Target, ReturnType, actualArgs) {
                        OverridenName = constructor
                    };

                    return "new " + call.ToString(context);
                }
                else
                {
                    foreach (Expression exp in actualArgs)
                    {
                        context.currentFunction = this;
                        if (argumentString.Length > 0)
                            argumentString.Append(", ");
                        argumentString.Append(exp.ToString(context));
                    }
                }
                context.currentFunction = null;

                return String.Format("new {0}({1})", constructor, argumentString);
            }
            else
            {
                foreach (Expression exp in Arguments)
                {
                    context.currentFunction = this;
                    if (argumentString.Length > 0)
                        argumentString.Append(", ");
                    argumentString.Append(exp.ToString(context));

                }
                context.currentFunction = null;

                if (Function.Name.Content == "@@NewGMLArray@@") // Inline array definitions
                    return "[" + argumentString.ToString() + "]";

                return String.Format("{0}({1})", OverridenName != string.Empty ? OverridenName : Function.Name.Content, argumentString.ToString());
            }
        }

        public override Statement CleanStatement(DecompileContext context, BlockHLStatement block)
        {
            // Special case for these functions which don't have any purpose in decompiled code
            if (Function?.Name?.Content == "@@This@@")
            {
                return new ExpressionConstant(UndertaleInstruction.DataType.Variable, "self");
            }
            if (Function?.Name?.Content == "@@Other@@")
            {
                return new ExpressionConstant(UndertaleInstruction.DataType.Variable, "other");
            }
            if (Function?.Name?.Content == "@@GetInstance@@")
            {
                Statement res = Arguments[0];
                if (res is ExpressionCast cast)
                    return cast.Argument;
                return res;
            }
            for (var i = 0; i < Arguments.Count; i++)
                Arguments[i] = Arguments[i]?.CleanExpression(context, block);
            return this;
        }

        internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
        {
            string funcName = OverridenName != string.Empty ? OverridenName : Function.Name.Content;
            var script_code = context.GlobalContext.Data?.Scripts.ByName(funcName)?.Code;
            if (script_code != null && !context.GlobalContext.ScriptArgsCache.ContainsKey(funcName))
            {
                context.GlobalContext.ScriptArgsCache.Add(funcName, null); // stop the recursion from looping
                DecompileContext childContext;
                try
                {
                    if (script_code.ParentEntry != null)
                    {
                        childContext = new DecompileContext(context.GlobalContext, script_code.ParentEntry);
                        Dictionary<uint, Block> blocks = Decompiler.PrepareDecompileFlow(script_code.ParentEntry, new List<uint>() { script_code.Offset / 4 });
                        Decompiler.DecompileFromBlock(childContext, blocks, blocks[script_code.Offset / 4]);
                        Decompiler.DoTypePropagation(childContext, blocks); // TODO: This should probably put suggestedType through the "return" statement at the other end
                    }
                    else
                    {
                        childContext = new DecompileContext(context.GlobalContext, script_code);
                        Dictionary<uint, Block> blocks = Decompiler.PrepareDecompileFlow(script_code, new List<uint>() { 0 });
                        Decompiler.DecompileFromBlock(childContext, blocks, blocks[0]);
                        Decompiler.DoTypePropagation(childContext, blocks); // TODO: This should probably put suggestedType through the "return" statement at the other end
                    }
                    context.GlobalContext.ScriptArgsCache[funcName] = new AssetIDType[15];
                    for (int i = 0; i < 15; i++)
                    {
                        var v = childContext.assetTypes.Where((x) => x.Key.Name.Content == "argument" + i);
                        context.GlobalContext.ScriptArgsCache[funcName][i] = v.Any() ? v.First().Value : AssetIDType.Other;
                    }
                }
                catch (Exception e)
                {
                    context.GlobalContext.DecompilerWarnings.Add("/*\nWARNING: Recursive script decompilation (for asset type resolution) failed for " + Function.Name.Content + "\n\n" + e.ToString() + "\n*/");
                }
            }

            AssetIDType[] args = new AssetIDType[Arguments.Count];
            AssetTypeResolver.AnnotateTypesForFunctionCall(funcName, args, context, this);
            for (var i = 0; i < Arguments.Count; i++)
                Arguments[i].DoTypePropagation(context, args[i]);

            return suggestedType; // TODO: maybe we should handle returned values too?
        }
    }
}