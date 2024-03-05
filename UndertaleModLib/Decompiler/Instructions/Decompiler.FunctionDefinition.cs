using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UndertaleModLib.Models;

namespace UndertaleModLib.Decompiler;

public static partial class Decompiler
{
    // Represents an inline function definition
    public class FunctionDefinition : Expression
    {
        public enum FunctionType
        {
            Function,
            Constructor,
            Struct
        }

        public UndertaleFunction Function { get; private set; }
        public UndertaleCode FunctionBodyCodeEntry { get; private set; }
        public Block FunctionBodyEntryBlock { get; private set; }
        public FunctionType Subtype { get; private set; }
        public bool IsStatement = false; // I know it's an expression, yes. But I'm not duplicating the rest.
        public string StatementName = null;

        internal List<Expression> Arguments;

        public FunctionDefinition(UndertaleFunction target, UndertaleCode functionBodyCodeEntry, Block functionBodyEntryBlock, FunctionType type)
        {
            Subtype = type;
            Function = target;
            FunctionBodyCodeEntry = functionBodyCodeEntry;
            FunctionBodyEntryBlock = functionBodyEntryBlock;
        }

        public void PromoteToStruct()
        {
            if (Subtype == FunctionType.Function)
                throw new InvalidOperationException("Cannot promote function to struct");

            Subtype = FunctionType.Struct;
        }

        public void PopulateArguments(params Expression[] arguments)
        {
            PopulateArguments(arguments.ToList());
        }

        public void PopulateArguments(List<Expression> arguments)
        {
            if (Subtype != FunctionType.Struct)
                throw new InvalidOperationException("Cannot populate arguments of non-struct");

            if (Arguments == null)
                Arguments = new List<Expression>();

            Arguments.AddRange(arguments);
        }

        public override Statement CleanStatement(DecompileContext context, BlockHLStatement block)
        {
            return this;
        }

        public override string ToString(DecompileContext context)
        {
            StringBuilder sb = new StringBuilder();
            if (context.Statements.ContainsKey(FunctionBodyEntryBlock.Address.Value))
            {
                FunctionDefinition def;
                var oldDecompilingStruct = context.DecompilingStruct;
                var oldReplacements = context.ArgumentReplacements;

                if (Subtype == FunctionType.Struct)
                    context.DecompilingStruct = true;
                else
                {
                    context.DecompilingStruct = false;
                    sb.Append("function");
                    if (IsStatement)
                    {
                        sb.Append(" ");
                        /*
                        // For further optimization, we could *probably* create a dictionary that's just flipped KVPs (assuming there are no dup. values).
                        // Doing so would save the need for LINQ and what-not. Not that big of an issue, but still an option.
                        Dictionary<string, UndertaleFunction> subFuncs = context.GlobalContext.Data.KnownSubFunctions;
                        KeyValuePair<string, UndertaleFunction> kvp = subFuncs.FirstOrDefault(x => x.Value == Function);

                        // If we found an associated sub-function, use the key as the name.
                        if (kvp.Key != null)
                            sb.Append(kvp.Key);
                        else
                        {
                            //Attempt to find function names before going with the last functions' name
                            bool gotFuncName = false;
                            if (Function.Name.Content.StartsWith("gml_Script_"))
                            {
                                string funcName = Function.Name.Content.Substring("gml_Script_".Length);
                                if (context.Statements[0].Any(x => x is AssignmentStatement && (x as AssignmentStatement).Destination.Var.Name.Content == funcName))
                                {
                                    sb.Append(funcName);
                                    gotFuncName = true;
                                }
                            }
                            if(!gotFuncName)
                                sb.Append((context.Statements[0].Last() as AssignmentStatement).Destination.Var.Name.Content);
                        }
                        */
                        sb.Append(StatementName);
                    }
                    sb.Append("(");
                    for (int i = 0; i < FunctionBodyCodeEntry.ArgumentsCount; ++i)
                    {
                        if (i != 0)
                            sb.Append(", ");
                        sb.Append("argument");
                        sb.Append(i);
                    }
                    sb.Append(") ");
                    if (Subtype == FunctionType.Constructor)
                        sb.Append("constructor ");
                    sb.Append("//");
                    sb.Append(Function.Name.Content);
                }

                var statements = context.Statements[FunctionBodyEntryBlock.Address.Value];
                int numNotReturn = statements.FindAll(stmt => !(stmt is ReturnStatement)).Count;

                if (numNotReturn > 0 || Subtype != FunctionType.Struct)
                {
                    sb.Append("\n");
                    sb.Append(context.Indentation);
                    sb.Append("{\n");
                    context.IndentationLevel++;
                    context.ArgumentReplacements = Arguments;

                    int count = 0;
                    foreach (Statement stmt in statements)
                    {
                        count++;
                        if ((Subtype != FunctionType.Function && stmt is ReturnStatement) || (stmt is AssignmentStatement assign && assign.IsStructDefinition))
                            continue;

                        sb.Append(context.Indentation);

                        // See #614
                        // This is not the place to monkey patch this
                        // issue, but it's like 2am and quite frankly
                        // I don't care anymore.
                        def = null;
                        if (stmt is FunctionDefinition)
                            def = stmt as FunctionDefinition;
                        else if (stmt is TempVarAssignmentStatement reference && reference.Value is FunctionDefinition)
                            def = reference.Value as FunctionDefinition;

                        if (def?.Function == Function)
                        {
                            //sb.Append("// Error decompiling function: function contains its own declaration???\n");
                            sb.Append("\n");
                            break;
                        }
                        else
                        {
                            sb.Append(stmt.ToString(context));
                            if (Subtype == FunctionType.Struct && count < numNotReturn)
                                sb.Append(",");
                        }
                        sb.Append("\n");
                    }
                    context.DecompilingStruct = oldDecompilingStruct;
                    context.ArgumentReplacements = oldReplacements;
                    context.IndentationLevel--;
                    sb.Append(context.Indentation);
                    sb.Append("}");
                    if (!oldDecompilingStruct)
                        sb.Append("\n");
                }
                else
                    sb.Append("{}");
            }
            else
            {
                sb.Append(Function.Name.Content);
            }
            return sb.ToString();
        }

        internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
        {
            return suggestedType;
        }
    }
}