using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib.Models;
using UndertaleModLib.Util;

namespace UndertaleModLib.Decompiler
{
    public static partial class Decompiler
    {
        // Color dictionary for resolving.
        public static readonly Dictionary<uint, string> ColorDictionary = new Dictionary<uint, string>
        {
            [16776960] = "c_aqua",
            [0] = "c_black",
            [16711680] = "c_blue",
            [4210752] = "c_dkgray",
            [16711935] = "c_fuchsia",
            [8421504] = "c_gray",
            [32768] = "c_green",
            [65280] = "c_lime",
            //[12632256] = "c_ltgray",
            [128] = "c_maroon",
            [8388608] = "c_navy",
            [32896] = "c_olive",
            [8388736] = "c_purple",
            [255] = "c_red",
            [12632256] = "c_silver",
            [8421376] = "c_teal",
            [16777215] = "c_white", // maximum color value
            [65535] = "c_yellow",
            [4235519] = "c_orange"
        };
        
        static int GetTypeSize(UndertaleInstruction.DataType type)
        {
            switch (type)
            {
                case UndertaleInstruction.DataType.Int16:
                case UndertaleInstruction.DataType.Int32:
                    return 4;
                case UndertaleInstruction.DataType.Double: // Fallthrough
                case UndertaleInstruction.DataType.Int64:
                    return 8;
                case UndertaleInstruction.DataType.Variable:
                    return 16;
                default:
                    throw new NotImplementedException("Unknown size for data type " + type);
            }
        }
        static int GetTypeSize(Expression e)
        {
            if (e is ExpressionVar || e is ExpressionTempVar)
                return GetTypeSize(UndertaleInstruction.DataType.Variable);
            if (e is FunctionCall)  // function call returns an internal variable
                return GetTypeSize(UndertaleInstruction.DataType.Variable);
            if (e is FunctionDefinition)
                return GetTypeSize(UndertaleInstruction.DataType.Variable);
            if (e is ExpressionTwo exprTwo)
                return GetTypeSize(exprTwo.Type2); // for add.i.v, the output is a var
            return GetTypeSize(e.Type);
        }

        // The core function to decompile a specific block.
        internal static void DecompileFromBlock(DecompileContext context, Dictionary<uint, Block> blocks, Block block, List<TempVarReference> tempvars, Stack<Tuple<Block, List<TempVarReference>>> workQueue)
        {
            if (block.TempVarsOnEntry != null && (block.nextBlockTrue != null || block.nextBlockFalse != null))
            {
                // Reroute tempvars to alias them to our ones
                if (block.TempVarsOnEntry.Count != tempvars.Count)
                {
                    throw new Exception("Reentered block with different amount of vars on stack (Entry: " + block.TempVarsOnEntry.Count + ", Actual Count: " + tempvars.Count + ")");
                }
                else
                {
                    for (int i = 0; i < tempvars.Count; i++)
                    {
                        tempvars[i].Var = block.TempVarsOnEntry[i].Var;
                    }
                }
            }

            // Don't decompile more than once
            if (block.Statements != null)
                return;

            // Recover stack tempvars which may be needed
            block.TempVarsOnEntry = tempvars;
            Stack<Expression> stack = new Stack<Expression>();
            foreach (TempVarReference var in tempvars)
                stack.Push(new ExpressionTempVar(var, var.Var.Type));

            // Iterate through all of the sta
            List<Statement> statements = new List<Statement>();
            bool end = false;
            bool returned = false;
            for (int i = 0; i < block.Instructions.Count; i++)
            {
                if (end)
                    throw new Exception("Expected end of block, but still has instructions");

                var instr = block.Instructions[i];
                switch (instr.Kind)
                {
                    case UndertaleInstruction.Opcode.Neg:
                    case UndertaleInstruction.Opcode.Not:
                        stack.Push(new ExpressionOne(instr.Kind, instr.Type1, stack.Pop()));
                        break;

                    case UndertaleInstruction.Opcode.Dup:
                        if (instr.ComparisonKind != 0)
                        {
                            // This is the GMS 2.3+ stack move / swap instruction
                            if (instr.Type1 == UndertaleInstruction.DataType.Variable)
                            {
                                // This variant seems to do literally nothing...?
                                break;
                            }

                            int bytesToTake = instr.Extra * 4;
                            Stack<Expression> taken = new Stack<Expression>();
                            while (bytesToTake > 0)
                            {
                                Expression e = stack.Pop();
                                taken.Push(e);
                                bytesToTake -= GetTypeSize(e);
                                if (bytesToTake < 0)
                                    throw new InvalidOperationException("The stack got misaligned? Error 0");
                            }

                            int b2 = (byte)instr.ComparisonKind & 0x7F;
                            if ((b2 & 0b111) != 0)
                                throw new InvalidOperationException("Don't know what to do with this");
                            int bytesToMove = (b2 >> 3) * 4;
                            Stack<Expression> moved = new Stack<Expression>();
                            while (bytesToMove > 0)
                            {
                                Expression e = stack.Pop();
                                moved.Push(e);
                                bytesToMove -= GetTypeSize(e);
                                if (bytesToMove < 0)
                                    throw new InvalidOperationException("The stack got misaligned? Error 1");
                            }

                            while (taken.Count > 0)
                                stack.Push(taken.Pop());
                            while (moved.Count > 0)
                                stack.Push(moved.Pop());

                            break;
                        }

                        // Normal dup instruction

                        List<Expression> topExpressions1 = new List<Expression>();
                        List<Expression> topExpressions2 = new List<Expression>();
                        int bytesToDuplicate = (instr.Extra + 1) * GetTypeSize(instr.Type1);
                        while (bytesToDuplicate > 0)
                        {
                            var item = stack.Pop();

                            if (item.IsDuplicationSafe())
                            {
                                item.WasDuplicated = true;
                                topExpressions1.Add(item);
                                topExpressions2.Add(item);
                            }
                            else
                            {
                                TempVar var = context.NewTempVar();
                                var.Type = item.Type;
                                TempVarReference varref = new TempVarReference(var);
                                statements.Add(new TempVarAssignmentStatement(varref, item));

                                topExpressions1.Add(new ExpressionTempVar(varref, varref.Var.Type) { WasDuplicated = true });
                                topExpressions2.Add(new ExpressionTempVar(varref, instr.Type1) { WasDuplicated = true });
                            }

                            bytesToDuplicate -= GetTypeSize(item);
                            if (bytesToDuplicate < 0)
                                throw new InvalidOperationException("The stack got misaligned? Error 2: Attempted to duplicate "
                                    + GetTypeSize(item)
                                    + " bytes, only found "
                                    + (bytesToDuplicate + GetTypeSize(item)));
                        }
                        topExpressions1.Reverse();
                        topExpressions2.Reverse();
                        for (int j = 0; j < topExpressions1.Count; j++)
                            stack.Push(topExpressions1[j]);
                        for (int j = 0; j < topExpressions2.Count; j++)
                            stack.Push(topExpressions2[j]);
                        break;

                    case UndertaleInstruction.Opcode.Ret:
                    case UndertaleInstruction.Opcode.Exit:
                        ReturnStatement stmt = new ReturnStatement(instr.Kind == UndertaleInstruction.Opcode.Ret ? stack.Pop() : null);
                        /*
                            This shouldn't be necessary: all unused things on the stack get converted to tempvars at the end anyway, and this fixes decompilation of repeat()
                            See #85

                            foreach (var expr in stack.Reverse())
                                if (!(expr is ExpressionTempVar))
                                    statements.Add(expr);
                            stack.Clear();
                        */
                        statements.Add(stmt);

                        end = true;
                        returned = true;
                        break;

                    case UndertaleInstruction.Opcode.Popz:
                        Expression popped = stack.Pop();
                        if (!popped.IsDuplicationSafe()) // <- not duplication safe = has side effects and needs to be listed in the output
                            statements.Add(popped);
                        break;

                    case UndertaleInstruction.Opcode.Conv:
                        stack.Push(new ExpressionCast(instr.Type2, stack.Pop()));
                        break;

                    case UndertaleInstruction.Opcode.Mul:
                    case UndertaleInstruction.Opcode.Div:
                    case UndertaleInstruction.Opcode.Rem:
                    case UndertaleInstruction.Opcode.Mod:
                    case UndertaleInstruction.Opcode.Add:
                    case UndertaleInstruction.Opcode.Sub:
                    case UndertaleInstruction.Opcode.And:
                    case UndertaleInstruction.Opcode.Or:
                    case UndertaleInstruction.Opcode.Xor:
                    case UndertaleInstruction.Opcode.Shl:
                    case UndertaleInstruction.Opcode.Shr:
                        Expression a2 = stack.Pop();
                        Expression a1 = stack.Pop();
                        stack.Push(new ExpressionTwo(instr.Kind, instr.Type1, instr.Type2, a1, a2));
                        break;

                    case UndertaleInstruction.Opcode.Cmp:
                        Expression aa2 = stack.Pop();
                        Expression aa1 = stack.Pop();
                        stack.Push(new ExpressionCompare(instr.ComparisonKind, aa1, aa2));
                        break;

                    case UndertaleInstruction.Opcode.B:
                        end = true;
                        break;

                    case UndertaleInstruction.Opcode.Bt:
                    case UndertaleInstruction.Opcode.Bf:
                        block.ConditionStatement = stack.Pop();
                        end = true;
                        break;

                    case UndertaleInstruction.Opcode.PushEnv:
                        if (DecompileContext.GMS2_3 == true)
                        {
                            Expression expr = stack.Pop();

                            // -9 signifies stacktop
                            if (expr is ExpressionConstant c &&
                                c.Type == UndertaleInstruction.DataType.Int16 && (short)c.Value == -9)
                                expr = stack.Pop();

                            statements.Add(new PushEnvStatement(expr));
                        }
                        else
                            statements.Add(new PushEnvStatement(stack.Pop()));
                        end = true;
                        break;

                    case UndertaleInstruction.Opcode.PopEnv:
                        if (!instr.JumpOffsetPopenvExitMagic)
                            statements.Add(new PopEnvStatement());
                        // For JumpOffsetPopenvExitMagic:
                        //  This is just an instruction to make sure the pushenv/popenv stack is cleared on early function return
                        //  Works kinda like 'break', but doesn't have a high-level representation as it's immediately followed by a 'return'
                        end = true;
                        break;

                    case UndertaleInstruction.Opcode.Pop:
                        {
                            if (instr.Destination == null)
                            {
                                // pop.e.v 5/6, strange magic stack operation
                                // TODO: this is probably an older version of the GMS2.3+ swap hidden in dup, but I'm not gonna touch it if it works
                                Expression e1 = stack.Pop();
                                Expression e2 = stack.Pop();
                                for (int j = 0; j < instr.SwapExtra - 4; j++)
                                    stack.Pop();
                                stack.Push(e2);
                                stack.Push(e1);
                                break;
                            }
                            ExpressionVar target = new ExpressionVar(instr.Destination.Target, new ExpressionConstant(UndertaleInstruction.DataType.Int16, instr.TypeInst), instr.Destination.Type);
                            Expression val = null;
                            if (instr.Type1 != UndertaleInstruction.DataType.Int32 && instr.Type1 != UndertaleInstruction.DataType.Variable)
                                throw new Exception("Unrecognized pop instruction, doesn't conform to pop.i.X, pop.v.X, or pop.e.v");
                            if (instr.Type1 == UndertaleInstruction.DataType.Int32)
                                val = stack.Pop();
                            switch (target.VarType)
                            {
                                case UndertaleInstruction.VariableType.Normal:
                                case UndertaleInstruction.VariableType.Instance:
                                    break;
                                case UndertaleInstruction.VariableType.StackTop:
                                    target.InstType = stack.Pop();
                                    break;
                                case UndertaleInstruction.VariableType.Array:
                                    Tuple<Expression, Expression> ind = ExpressionVar.Decompile2DArrayIndex(stack.Pop());
                                    target.ArrayIndices = new List<Expression> { ind.Item1 };
                                    if (ind.Item2 != null)
                                        target.ArrayIndices.Add(ind.Item2);
                                    target.InstType = stack.Pop();
                                    break;
                                default:
                                    throw new NotImplementedException("Don't know how to decompile variable type " + target.VarType);
                            }

                            // Check if instance type is "StackTop"
                            ExpressionConstant instanceTypeConstExpr = null;
                            if (target.InstType is ExpressionConstant c1) {
                                instanceTypeConstExpr = c1;
                            } else if (target.InstType is ExpressionTempVar tempVar) {
                                TempVarAssignmentStatement assignment = context.TempVarMap[tempVar.Var.Var.Name];
                                if (assignment != null && assignment.Value is ExpressionConstant c2) {
                                    instanceTypeConstExpr = c2;
                                }
                            }
                            if (instanceTypeConstExpr != null &&
                                instanceTypeConstExpr.Type == UndertaleInstruction.DataType.Int16 &&
                                (short)instanceTypeConstExpr.Value == (short)UndertaleInstruction.InstanceType.Stacktop) {
                                target.InstType = stack.Pop();
                            }

                            if (instr.Type1 == UndertaleInstruction.DataType.Variable)
                                val = stack.Pop();
                            if (val != null)
                            {
                                if ((target.VarType == UndertaleInstruction.VariableType.StackTop || target.VarType == UndertaleInstruction.VariableType.Array) && target.InstType.WasDuplicated)
                                {
                                    // Almost safe to assume that this is a +=, -=, etc.
                                    // Need to confirm a few things first. It's not certain, could be ++ even.
                                    if (val is ExpressionTwo)
                                    {
                                        var two = (val as ExpressionTwo);
                                        if (two.Opcode != UndertaleInstruction.Opcode.Rem && // Not possible in GML, but possible in bytecode. Don't deal with these,
                                            two.Opcode != UndertaleInstruction.Opcode.Shl && // frankly we don't care enough.
                                            two.Opcode != UndertaleInstruction.Opcode.Shr)
                                        {
                                            var arg = two.Argument1;
                                            if (arg is ExpressionVar)
                                            {
                                                var v = arg as ExpressionVar;
                                                if (v.Var == target.Var && v.InstType == target.InstType &&
                                                    ((v.ArrayIndices == null && target.ArrayIndices == null) ||
                                                      v.ArrayIndices?.SequenceEqual(target.ArrayIndices) == true) && // even if null
                                                    (!(two.Argument2 is ExpressionConstant) || // Also check to make sure it's not a ++ or --
                                                    (!((two.Argument2 as ExpressionConstant).IsPushE && ExpressionConstant.ConvertToInt((two.Argument2 as ExpressionConstant).Value) == 1))))
                                                {
                                                    if (!(context.GlobalContext.Data?.GeneralInfo?.BytecodeVersion > 14 && v.Opcode != UndertaleInstruction.Opcode.Push && instr.Destination.Target.InstanceType != UndertaleInstruction.InstanceType.Self))
                                                    {
                                                        statements.Add(new OperationEqualsStatement(target, two.Opcode, two.Argument2));
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            else
                                Debug.Fail("Pop value is null.");
                            statements.Add(new AssignmentStatement(target, val));
                        }
                        break;

                    case UndertaleInstruction.Opcode.Push:
                    case UndertaleInstruction.Opcode.PushLoc:
                    case UndertaleInstruction.Opcode.PushGlb:
                    case UndertaleInstruction.Opcode.PushBltn:
                    case UndertaleInstruction.Opcode.PushI:
                        if (instr.Value is UndertaleInstruction.Reference<UndertaleVariable>)
                        {
                            ExpressionVar pushTarget = new ExpressionVar((instr.Value as UndertaleInstruction.Reference<UndertaleVariable>).Target, new ExpressionConstant(UndertaleInstruction.DataType.Int16, instr.TypeInst), (instr.Value as UndertaleInstruction.Reference<UndertaleVariable>).Type);
                            pushTarget.Opcode = instr.Kind;
                            switch(pushTarget.VarType)
                            {
                                case UndertaleInstruction.VariableType.Normal:
                                case UndertaleInstruction.VariableType.Instance:
                                    break;
                                case UndertaleInstruction.VariableType.StackTop:
                                    pushTarget.InstType = stack.Pop();
                                    break;
                                case UndertaleInstruction.VariableType.Array:
                                    Tuple<Expression, Expression> ind = ExpressionVar.Decompile2DArrayIndex(stack.Pop());
                                    pushTarget.ArrayIndices = new List<Expression>() { ind.Item1 };
                                    if (ind.Item2 != null)
                                        pushTarget.ArrayIndices.Add(ind.Item2);
                                    pushTarget.InstType = stack.Pop();
                                    break;
                                case UndertaleInstruction.VariableType.ArrayPopAF:
                                case UndertaleInstruction.VariableType.ArrayPushAF:
                                    pushTarget.ArrayIndices = new List<Expression>() { stack.Pop() };
                                    pushTarget.InstType = stack.Pop();
                                    break;
                                default:
                                    throw new NotImplementedException("Don't know how to decompile variable type " + pushTarget.VarType);
                            }
                            if (pushTarget.InstType is ExpressionConstant c &&
                                c.Type == UndertaleInstruction.DataType.Int16 && (short)c.Value == -9)
                                pushTarget.InstType = stack.Pop();
                            stack.Push(pushTarget);
                        }
                        else
                        {
                            bool isPushE = (instr.Kind == UndertaleInstruction.Opcode.Push && instr.Type1 == UndertaleInstruction.DataType.Int16);
                            Expression pushTarget = new ExpressionConstant(instr.Type1, instr.Value, isPushE);
                            if (isPushE && pushTarget.Type == UndertaleInstruction.DataType.Int16 && Convert.ToInt32((pushTarget as ExpressionConstant).Value) == 1)
                            {
                                // Check for expression ++ or --
                                if (((i >= 1 && block.Instructions[i - 1].Kind == UndertaleInstruction.Opcode.Dup && block.Instructions[i - 1].Type1 == UndertaleInstruction.DataType.Variable) ||
                                     (i >= 2 && block.Instructions[i - 2].Kind == UndertaleInstruction.Opcode.Dup && block.Instructions[i - 2].Type1 == UndertaleInstruction.DataType.Variable &&
                                      block.Instructions[i - 1].Kind == UndertaleInstruction.Opcode.Pop && block.Instructions[i - 1].Type1 == UndertaleInstruction.DataType.Int16 && block.Instructions[i - 1].Type2 == UndertaleInstruction.DataType.Variable)) &&
                                    (i + 1 < block.Instructions.Count && (block.Instructions[i + 1].Kind == UndertaleInstruction.Opcode.Add || block.Instructions[i + 1].Kind == UndertaleInstruction.Opcode.Sub)))
                                {
                                    // We've detected a post increment/decrement (i.e., x = y++)
                                    // Remove duplicate from stack
                                    stack.Pop();

                                    // Do the magic
                                    stack.Push(new ExpressionPost(block.Instructions[i + 1].Kind, stack.Pop()));

                                    while (i < block.Instructions.Count && (block.Instructions[i].Kind != UndertaleInstruction.Opcode.Pop || (block.Instructions[i].Type1 == UndertaleInstruction.DataType.Int16 && block.Instructions[i].Type2 == UndertaleInstruction.DataType.Variable)))
                                        i++;
                                }
                                else if (i + 2 < block.Instructions.Count && (block.Instructions[i + 1].Kind == UndertaleInstruction.Opcode.Add || block.Instructions[i + 1].Kind == UndertaleInstruction.Opcode.Sub) &&
                                        block.Instructions[i + 2].Kind == UndertaleInstruction.Opcode.Dup && block.Instructions[i + 2].Type1 == UndertaleInstruction.DataType.Variable)
                                {
                                    // We've detected a pre increment/decrement (i.e., x = ++y)
                                    // Do the magic
                                    stack.Push(new ExpressionPre(block.Instructions[i + 1].Kind, stack.Pop()));

                                    while (i < block.Instructions.Count && block.Instructions[i].Kind != UndertaleInstruction.Opcode.Pop)
                                        i++;
                                    var _inst = block.Instructions[i];
                                    if (_inst.Type1 == UndertaleInstruction.DataType.Int16 && _inst.Type2 == UndertaleInstruction.DataType.Variable)
                                    {
                                        Expression e = stack.Pop();
                                        stack.Pop();
                                        stack.Push(e);
                                        i++;
                                    }
                                }
                                else
                                {
                                    stack.Push(pushTarget);
                                }
                            }
                            else
                            {
                                stack.Push(pushTarget);
                            }
                        }
                        break;

                    case UndertaleInstruction.Opcode.Call:
                        {
                            List<Expression> args = new List<Expression>();
                            for (int j = 0; j < instr.ArgumentsCount; j++)
                                args.Add(stack.Pop());

                            if (instr.Function.Target.Name.Content == "method" && args.Count == 2)
                            {
                                // Special case - method creation
                                // See if the body should be inlined

                                Expression arg1 = args[0];
                                while (arg1 is ExpressionCast cast)
                                    arg1 = cast.Argument;
                                Expression arg2 = args[1];
                                while (arg2 is ExpressionCast cast)
                                    arg2 = cast.Argument;

                                if (arg2 is ExpressionConstant argCode && argCode.Type == UndertaleInstruction.DataType.Int32 &&
                                    argCode.Value is UndertaleInstruction.Reference<UndertaleFunction> argCodeFunc)
                                {
                                    UndertaleCode functionBody = context.GlobalContext.Data.Code.First(x => x.Name.Content == argCodeFunc.Target.Name.Content);

                                    FunctionDefinition.FunctionType type = FunctionDefinition.FunctionType.Function;
                                    bool processChildEntry;

                                    if (arg1 is DirectFunctionCall call && call.Function.Name.Content == "@@NullObject@@")
                                    {
                                        type = FunctionDefinition.FunctionType.Constructor;
                                        processChildEntry = true;
                                    }
                                    else
                                        processChildEntry = context.AlreadyProcessed.Add(functionBody);

                                    if (context.TargetCode.ChildEntries.Contains(functionBody) && processChildEntry)
                                    {
                                        // This function is somewhere inside this UndertaleCode block
                                        // inline the definition
                                        Block functionBodyEntryBlock = blocks[functionBody.Offset / 4];
                                        stack.Push(new FunctionDefinition(argCodeFunc.Target, functionBody, functionBodyEntryBlock, type));
                                        workQueue.Push(new Tuple<Block, List<TempVarReference>>(functionBodyEntryBlock, new List<TempVarReference>()));
                                        break;
                                    }
                                }
                            }

                            UndertaleCode callTargetBody = context.GlobalContext.Data?.Code.FirstOrDefault(x => x.Name.Content == instr.Function.Target.Name.Content);
                            if (callTargetBody != null && callTargetBody.ParentEntry != null && !context.DisableAnonymousFunctionNameResolution)
                            {
                                // Special case: this is a direct reference to a method variable
                                // Figure out what its actual name is

                                static string FindActualNameForAnonymousCodeObject(DecompileContext context, UndertaleCode anonymousCodeObject)
                                {
                                    // Decompile the parent object, and find the anonymous function assignment
                                    DecompileContext childContext = new DecompileContext(context.GlobalContext, anonymousCodeObject.ParentEntry);
                                    childContext.DisableAnonymousFunctionNameResolution = true; // prevent recursion - we don't even need the names in the child block
                                    try
                                    {
                                        Dictionary<uint, Block> blocks2 = PrepareDecompileFlow(anonymousCodeObject.ParentEntry, new List<uint>() { 0 });
                                        DecompileFromBlock(childContext, blocks2, blocks2[0]);
                                        // This hack handles decompilation of code entries getting shorter, but not longer or out of place.
                                        // Probably is no longer needed since we now update Length mostly-correctly
                                        Block lastBlock;
                                        if (!blocks2.TryGetValue(anonymousCodeObject.Length / 4, out lastBlock))
                                            lastBlock = blocks2[blocks2.Keys.Max()];
                                        List<Statement> statements = HLDecompile(childContext, blocks2, blocks2[0], lastBlock);
                                        foreach (Statement stmt2 in statements)
                                        {
                                            if (stmt2 is AssignmentStatement assign &&
                                                assign.Value is FunctionDefinition funcDef &&
                                                funcDef.FunctionBodyCodeEntry == anonymousCodeObject)
                                            {
                                                if (funcDef.FunctionBodyEntryBlock.Address == anonymousCodeObject.Offset / 4)
                                                    return assign.Destination.Var.Name.Content;
                                                else
                                                    return string.Empty; //throw new Exception("Non-matching offset: " + funcDef.FunctionBodyEntryBlock.Address.ToString() + " versus " + (anonymousCodeObject.Offset / 4).ToString() + " (got name " + assign.Destination.Var.Name.Content + ")");
                                            }
                                        }
                                        throw new Exception("Unable to find the var name for anonymous code object " + anonymousCodeObject.Name.Content);
                                    }
                                    catch (Exception e)
                                    {
                                        context.GlobalContext.DecompilerWarnings.Add("/*\nWARNING: Recursive script decompilation (for member variable name resolution) failed for " + anonymousCodeObject.Name.Content + "\n\n" + e.ToString() + "\n*/");
                                        return string.Empty;
                                    }
                                }

                                string funcName;
                                if (!context.GlobalContext.AnonymousFunctionNameCache.TryGetValue(instr.Function.Target, out funcName))
                                {
                                    funcName = FindActualNameForAnonymousCodeObject(context, callTargetBody);
                                    context.GlobalContext.AnonymousFunctionNameCache.Add(instr.Function.Target, funcName);
                                }
                                if (funcName != string.Empty)
                                {
                                    stack.Push(new DirectFunctionCall(funcName, instr.Function.Target, instr.Type1, args));
                                    break;
                                }
                            }

                            stack.Push(new DirectFunctionCall(instr.Function.Target, instr.Type1, args));
                        }
                        break;

                    case UndertaleInstruction.Opcode.CallV:
                        {
                            Expression func = stack.Pop();
                            Expression func_this = stack.Pop();
                            List<Expression> args = new List<Expression>();
                            for (int j = 0; j < instr.Extra; j++)
                                args.Add(stack.Pop());
                            stack.Push(new IndirectFunctionCall(func_this, func, instr.Type1, args));
                        }
                        break;

                    case UndertaleInstruction.Opcode.Break:
                        // GMS 2.3 sub-opcodes
                        if (DecompileContext.GMS2_3 == true)
                        {
                            switch ((short)instr.Value)
                            {
                                case -2: // GMS2.3+, pushaf
                                    {
                                        // TODO, work out more specifics here, like ++
                                        Expression ind = stack.Pop();
                                        Expression target = stack.Pop();
                                        if (target is ExpressionVar targetVar)
                                        {
                                            if (targetVar.VarType != UndertaleInstruction.VariableType.ArrayPushAF && targetVar.VarType != UndertaleInstruction.VariableType.ArrayPopAF) // The popaf arrays support pushaf as well, judging by how they are used with dup
                                                throw new InvalidOperationException("Tried to pushaf on var of type " + targetVar.VarType);

                                            ExpressionVar newVar = new ExpressionVar(targetVar.Var, targetVar.InstType, targetVar.VarType);
                                            newVar.Opcode = instr.Kind;
                                            newVar.ArrayIndices = new List<Expression>(targetVar.ArrayIndices);
                                            newVar.ArrayIndices.Add(ind);
                                            stack.Push(newVar);
                                        }
                                        else
                                            throw new InvalidOperationException("Tried to pushaf on something that is not a var");
                                    }
                                    break;
                                case -3: // GMS2.3+, popaf
                                    {
                                        // TODO, work out more specifics here, like ++
                                        Expression ind = stack.Pop();
                                        Expression target = stack.Pop();
                                        if (target is ExpressionVar targetVar)
                                        {
                                            if (targetVar.VarType != UndertaleInstruction.VariableType.ArrayPopAF)
                                                throw new InvalidOperationException("Tried to popaf on var of type " + targetVar.VarType);

                                            ExpressionVar newVar = new ExpressionVar(targetVar.Var, targetVar.InstType, targetVar.VarType);
                                            newVar.Opcode = instr.Kind;
                                            newVar.ArrayIndices = new List<Expression>(targetVar.ArrayIndices);
                                            newVar.ArrayIndices.Add(ind);

                                            Expression value = stack.Pop();
                                            statements.Add(new AssignmentStatement(newVar, value));
                                        }
                                        else
                                            throw new InvalidOperationException("Tried to popaf on something that is not a var");
                                    }
                                    break;
                                case -4: // GMS2.3+, pushac
                                    {
                                        Expression ind = stack.Pop();
                                        Expression target = stack.Pop();
                                        if (target is ExpressionVar targetVar)
                                        {
                                            if (targetVar.VarType != UndertaleInstruction.VariableType.ArrayPushAF && targetVar.VarType != UndertaleInstruction.VariableType.ArrayPopAF)
                                                throw new InvalidOperationException("Tried to pushac on var of type " + targetVar.VarType);

                                            ExpressionVar newVar = new ExpressionVar(targetVar.Var, targetVar.InstType, targetVar.VarType);
                                            newVar.Opcode = instr.Kind;
                                            newVar.ArrayIndices = new List<Expression>(targetVar.ArrayIndices);
                                            newVar.ArrayIndices.Add(ind);
                                            stack.Push(newVar);
                                        }
                                        else
                                            throw new InvalidOperationException("Tried to pushac on something that is not a var");
                                    }
                                    break;
                                case -5: // GMS2.3+, setowner
                                    // Stop 'setowner' values from leaking into the decompiled output as tempvars.
                                    // Used in the VM to let copy-on-write functionality work, but unnecessary for decompilation
                                    stack.Pop();
                                    /*
                                    var statement = stack.Pop();
                                    object owner;
                                    if (statement is ExpressionConstant)
                                        owner = (statement as ExpressionConstant).Value?.ToString();
                                    else
                                        owner = statement.ToString(context);
                                    statements.Add(new CommentStatement("setowner: " + (owner ?? "<null>")));
                                    */
                                    break;
                                case -10: // GMS2.3+, chknullish

                                    // TODO: Implement nullish operator in decompiled output.
                                    // Appearance in assembly is:

                                    /* <push var>
                                     * chknullish
                                     * bf [block2]
                                     *
                                     * :[block1]
                                     * popz.v
                                     * <var is nullish, evaluate new value>
                                     *
                                     * :[block2]
                                     * <use value>
                                     */

                                    // Note that this operator peeks from the stack, it does not pop directly.
                                    break;
                                case -11: // GM 2023.8+, pushref
                                    stack.Push(new ExpressionAssetRef(instr.IntArgument));
                                    break;
                            }
                        }

                        // chkindex is used for checking bounds in 2D arrays
                        // I'm not sure of the specifics but I guess it causes a debug breakpoint if the top of the stack is >= 32000
                        // anyway, that's not important when decompiling to high-level code so just ignore it
                        break;
                }
            }

            // Convert everything that remains on the stack to a temp var
            List<TempVarReference> leftovers = new List<TempVarReference>();
            for (int i = stack.Count - 1; i >= 0; i--)
            {
                if (i < tempvars.Count)
                {
                    Expression val = stack.Pop();
                    if (!(val is ExpressionTempVar) || (val as ExpressionTempVar).Var != tempvars[i]) {
                        var assignment = new TempVarAssignmentStatement(tempvars[i], val);
                        statements.Add(assignment);

                        if (val is ExpressionConstant) {
                            context.TempVarMap[tempvars[i].Var.Name] = assignment;
                        }
                    }

                    leftovers.Add(tempvars[i]);
                }
                else
                {
                    Expression val = stack.Pop();
                    TempVar var = context.NewTempVar();
                    var.Type = val.Type;
                    TempVarReference varref = new TempVarReference(var);
                    var assignment = new TempVarAssignmentStatement(varref, val);
                    statements.Add(assignment);
                    leftovers.Add(varref);

                    if (val is ExpressionConstant) {
                        context.TempVarMap[var.Name] = assignment;
                    }
                }
            }
            leftovers.Reverse();

            block.Statements = statements;
            // If we returned from this block, don't go to the "next" block, because that's totally wrong.
            if (!returned)
            {
                if (block.nextBlockFalse != null)
                    workQueue.Push(new Tuple<Block, List<TempVarReference>>(block.nextBlockFalse, leftovers));
                if (block.nextBlockTrue != null)
                    workQueue.Push(new Tuple<Block, List<TempVarReference>>(block.nextBlockTrue, leftovers));
            }
            else if (block.nextBlockFalse != null && block.nextBlockFalse.nextBlockFalse == null)
            {
                // Last block- make an exception for this one.
                workQueue.Push(new Tuple<Block, List<TempVarReference>>(block.nextBlockFalse, leftovers));
            }
        }

        public static void DecompileFromBlock(DecompileContext context, Dictionary<uint, Block> blocks, Block block)
        {
            Stack<Tuple<Block, List<TempVarReference>>> workQueue = new Stack<Tuple<Block, List<TempVarReference>>>();
            workQueue.Push(new Tuple<Block, List<TempVarReference>>(block, new List<TempVarReference>()));
            while (workQueue.Count > 0)
            {
                var item = workQueue.Pop();
                DecompileFromBlock(context, blocks, item.Item1, item.Item2, workQueue);
            }
        }

        public static Dictionary<uint, Block> DecompileFlowGraph(UndertaleCode code, List<uint> entryPoints)
        {
            Dictionary<uint, Block> blockByAddress = new Dictionary<uint, Block>();
            foreach(uint entryPoint in entryPoints)
                blockByAddress[entryPoint] = new Block(entryPoint);
            Block entryBlock = new Block(null);
            Block finalBlock = new Block(code.Length / 4);
            blockByAddress[code.Length / 4] = finalBlock;
            Block currentBlock = entryBlock;

            foreach (var instr in code.Instructions)
            {
                if (blockByAddress.ContainsKey(instr.Address))
                {
                    if (currentBlock != null)
                    {
                        currentBlock.conditionalExit = false;
                        currentBlock.nextBlockTrue = blockByAddress[instr.Address];
                        currentBlock.nextBlockFalse = blockByAddress[instr.Address];
                        blockByAddress[instr.Address].entryPoints.Add(currentBlock);
                    }
                    currentBlock = blockByAddress[instr.Address];
                }

                if (currentBlock == null)
                {
                    blockByAddress[instr.Address] = currentBlock = new Block(instr.Address);
                }

                currentBlock.Instructions.Add(instr);

                Func<uint, Block> GetBlock = (uint addr) =>
                {
                    Block nextBlock;
                    if (!blockByAddress.TryGetValue(addr, out nextBlock))
                    {
                        if (addr <= instr.Address)
                        {
                            // We have a jump into INSIDE one of previous blocks
                            // This is likely a loop or something
                            // We'll have to split that block into two

                            // First, find the block we have to split
                            Block blockToSplit = null;
                            foreach (var block in blockByAddress)
                            {
                                if (block.Key < addr && (blockToSplit == null || block.Key > blockToSplit.Address))
                                    blockToSplit = block.Value;
                            }

                            // Now, split the list of instructions into two
                            List<UndertaleInstruction> instrBefore = new List<UndertaleInstruction>();
                            List<UndertaleInstruction> instrAfter = new List<UndertaleInstruction>();
                            foreach (UndertaleInstruction inst in blockToSplit.Instructions)
                            {
                                if (inst.Address < addr)
                                    instrBefore.Add(inst);
                                else
                                    instrAfter.Add(inst);
                            }

                            // Create the newly split block
                            Block newBlock = new Block(addr);
                            blockToSplit.Instructions = instrBefore;
                            newBlock.Instructions = instrAfter;
                            newBlock.conditionalExit = blockToSplit.conditionalExit;
                            newBlock.nextBlockTrue = blockToSplit.nextBlockTrue;
                            newBlock.nextBlockFalse = blockToSplit.nextBlockFalse;
                            blockToSplit.conditionalExit = false;
                            blockToSplit.nextBlockTrue = newBlock;
                            blockToSplit.nextBlockFalse = newBlock;
                            blockByAddress[addr] = newBlock;
                            return newBlock;
                        }
                        else
                        {
                            blockByAddress.Add(addr, nextBlock = new Block(addr));
                        }
                    }
                    return nextBlock;
                };

                if (instr.Kind == UndertaleInstruction.Opcode.B)
                {
                    uint addr = (uint)(instr.Address + instr.JumpOffset);
                    Block nextBlock = GetBlock(addr);
                    currentBlock.conditionalExit = false;
                    currentBlock.nextBlockTrue = nextBlock;
                    currentBlock.nextBlockFalse = nextBlock;
                    currentBlock = null;
                }
                else if (instr.Kind == UndertaleInstruction.Opcode.Bt || instr.Kind == UndertaleInstruction.Opcode.Bf)
                {
                    Block nextBlockIfMet = GetBlock((uint)(instr.Address + instr.JumpOffset));
                    Block nextBlockIfNotMet = GetBlock(instr.Address + 1);
                    currentBlock.conditionalExit = true;
                    currentBlock.nextBlockTrue = instr.Kind == UndertaleInstruction.Opcode.Bt ? nextBlockIfMet : nextBlockIfNotMet;
                    currentBlock.nextBlockFalse = instr.Kind == UndertaleInstruction.Opcode.Bt ? nextBlockIfNotMet : nextBlockIfMet;
                    currentBlock = null;
                }
                else if (instr.Kind == UndertaleInstruction.Opcode.PushEnv || instr.Kind == UndertaleInstruction.Opcode.PopEnv)
                {
                    Block nextBlock = GetBlock(instr.Address + 1);
                    currentBlock.conditionalExit = false;
                    currentBlock.nextBlockTrue = nextBlock;
                    currentBlock.nextBlockFalse = nextBlock;
                    currentBlock = null;
                }
                else if (instr.Kind == UndertaleInstruction.Opcode.Ret || instr.Kind == UndertaleInstruction.Opcode.Exit)
                {
                    Block nextBlock = GetBlock(instr.Address + 1);
                    currentBlock.conditionalExit = false;
                    currentBlock.nextBlockTrue = nextBlock;
                    currentBlock.nextBlockFalse = nextBlock;
                    currentBlock = null;
                }
            }
            if (currentBlock != null)
            {
                currentBlock.nextBlockTrue = finalBlock;
                currentBlock.nextBlockFalse = finalBlock;
            }
            foreach (var block in blockByAddress.Values)
            {
                if (block.nextBlockTrue != null && !block.nextBlockTrue.entryPoints.Contains(block))
                    block.nextBlockTrue.entryPoints.Add(block);
                if (block.nextBlockFalse != null && !block.nextBlockFalse.entryPoints.Contains(block))
                    block.nextBlockFalse.entryPoints.Add(block);
            }
            return blockByAddress;
        }

        // Based on http://www.backerstreet.com/decompiler/loop_analysis.php
        public static Dictionary<Block, List<Block>> ComputeReverseDominators(Dictionary<uint, Block> blocks, Block entryBlock)
        {
            Block[] blockList = blocks.Values.ToArray();
            CustomBitArray[] dominators = new CustomBitArray[blockList.Length];

            int entryBlockId = -1;
            {
                int i;
                for (i = 0; i < blockList.Length; i++)
                {
                    Block b = blockList[i];
                    b._CachedIndex = i;

                    CustomBitArray ba;

                    if (blockList[i] == entryBlock)
                    {
                        entryBlockId = i;
                        ba = new CustomBitArray(blockList.Length);
                        ba.SetTrue(i);
                        dominators[i] = ba;
                        break;
                    }

                    ba = new CustomBitArray(blockList.Length);
                    ba.SetAllTrue();
                    dominators[i] = ba;
                }
                for (i++; i < blockList.Length; i++)
                {
                    blockList[i]._CachedIndex = i;
                    CustomBitArray ba = new CustomBitArray(blockList.Length);
                    ba.SetAllTrue();
                    dominators[i] = ba;
                }
            }

            bool changed;
            Block[] reverseUse1 = { null };
            Block[] reverseUse2 = { null, null };
            do
            {
                changed = false;
                for (int i = 0; i < blockList.Length; i++)
                {
                    if (i == entryBlockId)
                        continue;

                    Block b = blockList[i];

                    Block[] e;
                    if (b.conditionalExit)
                    {
                        reverseUse2[0] = b.nextBlockTrue;
                        reverseUse2[1] = b.nextBlockFalse;
                        e = reverseUse2;
                    }
                    else
                    {
                        reverseUse1[0] = b.nextBlockTrue;
                        e = reverseUse1;
                    }

                    foreach (Block pred in e)
                        changed |= pred != null && dominators[i].And(dominators[pred._CachedIndex], i);
                }
            } while (changed);

            Dictionary<Block, List<Block>> result = new Dictionary<Block, List<Block>>(blockList.Length);
            for (var i = 0; i < blockList.Length; i++)
            {
                CustomBitArray curr = dominators[i];
                result[blockList[i]] = new List<Block>(4);
                for (var j = 0; j < blockList.Length; j++)
                {
                    if (curr.Get(j))
                        result[blockList[i]].Add(blockList[j]);
                }
            }

            return result;
        }

        private static List<Block> NaturalLoopForEdge(Block header, Block tail)
        {
            Stack<Block> workList = new Stack<Block>(16);
            List<Block> loopBlocks = new List<Block>(8);

            loopBlocks.Add(header);
            if (header != tail)
            {
                loopBlocks.Add(tail);
                workList.Push(tail);
            }

            while (workList.Count > 0)
            {
                Block block = workList.Pop();
                foreach (Block pred in block.entryPoints)
                {
                    if (!loopBlocks.Contains(pred))
                    {
                        loopBlocks.Add(pred);
                        workList.Push(pred);
                    }
                }
            }

            return loopBlocks;
        }

        private static Dictionary<Block, List<Block>> ComputeNaturalLoops(Dictionary<uint, Block> blocks, Block entryBlock)
        {
            Dictionary<Block, List<Block>> loopSet = new Dictionary<Block, List<Block>>();

            foreach (var block in blocks.Values)
            {
                // Every successor that dominates its predecessor
                // must be the header of a loop.
                // That is, block -> succ is a back edge.

                // Future update: We're going to take a much more efficient but assuming
                // route that the compiler outputs in a specific order, which it always should

                if (block.nextBlockTrue != null && !loopSet.ContainsKey(block.nextBlockTrue))
                {
                    if (block.nextBlockTrue.Address <= block.Address)
                        loopSet.Add(block.nextBlockTrue, NaturalLoopForEdge(block.nextBlockTrue, block));
                }
                if (block.nextBlockFalse != null && block.nextBlockTrue != block.nextBlockFalse && !loopSet.ContainsKey(block.nextBlockFalse))
                {
                    if (block.nextBlockFalse.Address <= block.Address)
                        loopSet.Add(block.nextBlockFalse, NaturalLoopForEdge(block.nextBlockFalse, block));
                }
            }

            return loopSet;
        }

        public static Block FindFirstMeetPoint(Block ifStart, Dictionary<Block, List<Block>> reverseDominators)
        {
            DebugUtil.Assert(ifStart.conditionalExit, "If start does not have a conditional exit");
            var commonDominators = reverseDominators[ifStart.nextBlockTrue].Intersect(reverseDominators[ifStart.nextBlockFalse]);

            // Find the closest one of them
            List<Block> visited = new List<Block>();
            visited.Add(ifStart);
            Queue<Block> q = new Queue<Block>();
            q.Enqueue(ifStart.nextBlockTrue);
            q.Enqueue(ifStart.nextBlockFalse);
            while (q.Count > 0)
            {
                Block b = q.Dequeue();
                if (commonDominators.Contains(b))
                    return b;
                visited.Add(b);
                if (b.nextBlockTrue != null && !visited.Contains(b.nextBlockTrue) && !q.Contains(b.nextBlockTrue))
                    q.Enqueue(b.nextBlockTrue);
                if (b.nextBlockFalse != null && !visited.Contains(b.nextBlockFalse) && !q.Contains(b.nextBlockFalse))
                    q.Enqueue(b.nextBlockFalse);
            }
            return null;
        }

        // Process the base decompilation: clean up, make it readable, identify structures
        private static BlockHLStatement HLDecompileBlocks(DecompileContext context, ref Block block, Dictionary<uint, Block> blocks, Dictionary<Block, List<Block>> loops, Dictionary<Block, List<Block>> reverseDominators, List<Block> alreadyVisited, Block currentLoop = null, Block stopAt = null, Block breakTo = null, bool decompileTheLoop = false, uint depth = 0)
        {
            if (depth > 200)
                throw new Exception("Excessive recursion while processing blocks.");

            BlockHLStatement output = new BlockHLStatement();

            Block lastBlock = null;
            bool popenvDrop = false;
            while (block != stopAt && block != null)
            {
                lastBlock = block;

                if (loops.ContainsKey(block) && !decompileTheLoop)
                {
                    if (block == currentLoop)
                    {
                        output.Statements.Add(new ContinueHLStatement());
                        break;
                    }
                    else
                    {
                        LoopHLStatement statement = new LoopHLStatement() { Block = HLDecompileBlocks(context, ref block, blocks, loops, reverseDominators, alreadyVisited, block, null, block.nextBlockFalse, true, depth + 1) };
                        output.Statements.Add(statement);
                        continue;
                    }
                }
                else if (currentLoop != null && !loops[currentLoop].Contains(block) && decompileTheLoop)
                {
                    break;
                }

                if (block.Statements == null)
                {
                    // This is possible with unused blocks (due to return)
                    block = stopAt;
                    continue;
                }

                if (!alreadyVisited.Contains(block))
                    alreadyVisited.Add(block);


                for (int i = 0; i < block.Statements.Count; i++)
                {
                    Statement stmt = block.Statements[i];
                    if (!(stmt is PushEnvStatement) && !(stmt is PopEnvStatement))
                        output.Statements.Add(stmt);
                }

                if (output.Statements.Count >= 1 && output.Statements[output.Statements.Count - 1] is TempVarAssignmentStatement &&
                    block.Instructions.Count >= 1 && block.Instructions[block.Instructions.Count - 1].Kind == UndertaleInstruction.Opcode.Bt &&
                    block.conditionalExit && block.ConditionStatement is ExpressionCompare &&
                    (block.ConditionStatement as ExpressionCompare).Opcode == UndertaleInstruction.ComparisonType.EQ)
                {
                    // Switch statement
                    Expression switchExpression = (output.Statements[output.Statements.Count - 1] as TempVarAssignmentStatement).Value;
                    TempVar switchTempVar = (output.Statements[output.Statements.Count - 1] as TempVarAssignmentStatement).Var.Var;
                    output.Statements.RemoveAt(output.Statements.Count - 1);

                    Block meetPoint = FindFirstMeetPoint(block, reverseDominators);
                    if (meetPoint == null)
                        throw new Exception("End of switch not found");

                    Dictionary<Block, List<Expression>> caseEntries = new Dictionary<Block, List<Expression>>();
                    while (block != meetPoint)
                    {
                        Expression caseExpr = null;
                        if (block.ConditionStatement != null)
                        {
                            ExpressionCompare cmp = (ExpressionCompare)block.ConditionStatement;
                            if (cmp.Argument1 != switchExpression &&
                                (!(cmp.Argument1 is ExpressionTempVar) || !(switchExpression is ExpressionTempVar) || (cmp.Argument1 as ExpressionTempVar).Var.Var != (switchExpression as ExpressionTempVar).Var.Var) &&
                                (!(cmp.Argument1 is ExpressionTempVar) || (cmp.Argument1 as ExpressionTempVar).Var.Var != switchTempVar))
                                throw new Exception("Malformed switch statement: bad condition var (" + cmp.Argument1.ToString(context) + ")");
                            if (cmp.Opcode != UndertaleInstruction.ComparisonType.EQ)
                                throw new Exception("Malformed switch statement: bad contition type (" + cmp.Opcode.ToString().ToUpper(CultureInfo.InvariantCulture) + ")");
                            caseExpr = cmp.Argument2;
                        }

                        if (!caseEntries.ContainsKey(block.nextBlockTrue))
                            caseEntries.Add(block.nextBlockTrue, new List<Expression>());
                        caseEntries[block.nextBlockTrue].Add(caseExpr);

                        if (!block.conditionalExit)
                        {
                            // Seems to be "default", and we simply want to go to the exit now.
                            // This is a little hack, but it should fully work. The compiler always
                            // emits "default" at the end it looks like. Also this navigates down the
                            // "false" branching paths over others- this should lead to the correct
                            // block. Without this, branching at the start of "default" will break
                            // this switch detection.
                            while (block.nextBlockTrue != meetPoint)
                            {
                                if (block.nextBlockFalse != null)
                                    block = block.nextBlockFalse;
                                else if (block.nextBlockTrue != null)
                                    block = block.nextBlockTrue;
                                else
                                    break;
                            }

                            break;
                        }

                        block = block.nextBlockFalse;
                    }

                    List<HLSwitchCaseStatement> cases = new List<HLSwitchCaseStatement>();
                    HLSwitchCaseStatement defaultCase = null;

                    for (var i = 0; i < caseEntries.Count; i++)
                    {
                        var x = caseEntries.ElementAt(i);
                        Block temp = x.Key;

                        Block switchEnd = DetermineSwitchEnd(temp, caseEntries.Count > (i + 1) ? caseEntries.ElementAt(i + 1).Key : null, meetPoint);

                        HLSwitchCaseStatement result = new HLSwitchCaseStatement(x.Value, HLDecompileBlocks(context, ref temp, blocks, loops, reverseDominators, alreadyVisited, currentLoop, switchEnd, switchEnd, false, depth + 1));
                        cases.Add(result);
                        if (result.CaseExpressions.Contains(null))
                            defaultCase = result;

                        DebugUtil.Assert(temp == switchEnd, "temp != switchEnd");
                    }


                    if (defaultCase != null && defaultCase.Block.Statements.Count == 0)
                    {
                        // Handles default case.
                        UndertaleInstruction breakInstruction = context.TargetCode.GetInstructionFromAddress((uint)block.Address + 1);

                        if (breakInstruction.Kind == UndertaleInstruction.Opcode.B)
                        {
                            // This is the default-case meet-point if it is b.
                            uint instructionId = ((uint)block.Address + 1 + (uint)breakInstruction.JumpOffset);
                            if (!blocks.ContainsKey(instructionId))
                                Debug.Fail("Switch statement default: Bad target [" + block.Address + ", " + breakInstruction.JumpOffset + "]: " + breakInstruction.ToString());
                            Block switchEnd = blocks[instructionId];

                            Block start = meetPoint;
                            defaultCase.Block = HLDecompileBlocks(context, ref start, blocks, loops, reverseDominators, alreadyVisited, currentLoop, switchEnd, switchEnd, false, depth + 1);
                            block = start; // Start changed in HLDecompileBlocks.
                        }
                        else
                        {
                            // If there is no default-case, remove the default break, since that creates different bytecode.
                            cases.Remove(defaultCase);
                        }
                    }
                    else
                    {
                        block = block.nextBlockTrue;
                    }

                    output.Statements.Add(new HLSwitchStatement(switchExpression, cases));
                    continue;
                }

                if (block.Statements.Count > 0 && block.Statements.Last() is PushEnvStatement)
                {
                    DebugUtil.Assert(!block.conditionalExit, "Block ending with pushenv does not have a conditional exit");
                    PushEnvStatement stmt = (block.Statements.Last() as PushEnvStatement);
                    block = block.nextBlockTrue;
                    output.Statements.Add(new WithHLStatement()
                    {
                        NewEnv = stmt.NewEnv,
                        Block = HLDecompileBlocks(context, ref block, blocks, loops, reverseDominators, alreadyVisited, null, stopAt, null, false, depth + 1)
                    });
                    if (block == null)
                        break;
                }
                else if (block.Statements.Count > 0 && block.Statements.Last() is PopEnvStatement)
                {
                    DebugUtil.Assert(!block.conditionalExit, "Block ending in popenv does not have a conditional exit");
                    break;
                }

                if (popenvDrop)
                    break;

                if (block.conditionalExit && block.ConditionStatement != null) // If statement
                {
                    Block meetPoint = FindFirstMeetPoint(block, reverseDominators);
                    if (meetPoint == null)
                        throw new Exception("End of if not found");

                    IfHLStatement cond = new IfHLStatement();
                    cond.condition = block.ConditionStatement;

                    Block blTrue = block.nextBlockTrue, blFalse = block.nextBlockFalse;
                    cond.trueBlock = HLDecompileBlocks(context, ref blTrue, blocks, loops, reverseDominators, alreadyVisited, currentLoop, meetPoint, breakTo, false, depth + 1);
                    cond.falseBlock = HLDecompileBlocks(context, ref blFalse, blocks, loops, reverseDominators, alreadyVisited, currentLoop, meetPoint, breakTo, false, depth + 1);
                    output.Statements.Add(cond); // Add the if statement.
                    block = meetPoint;
                }
                else
                {
                    // Don't continue if there's a return/exit, except for last block
                    if (block.Instructions.Count == 0)
                        block = block.nextBlockTrue;
                    else
                    {
                        var last = block.Instructions.Last();
                        var lastKind = last.Kind;
                        if (lastKind == UndertaleInstruction.Opcode.PopEnv && last.JumpOffsetPopenvExitMagic)
                        {
                            block = block.nextBlockTrue;
                            popenvDrop = true;
                        } else
                            block = ((lastKind != UndertaleInstruction.Opcode.Ret && lastKind != UndertaleInstruction.Opcode.Exit)
                                || (block.nextBlockTrue != null && block.nextBlockTrue.nextBlockFalse == null)) ? block.nextBlockTrue : stopAt;
                    }
                }
            }

            if (breakTo != null && lastBlock?.nextBlockFalse == breakTo && lastBlock?.Instructions.Last()?.Kind == UndertaleInstruction.Opcode.B)
                output.Statements.Add(new BreakHLStatement());

            return output;
        }

        private static Statement UnCast(Statement statement)
        {
            if (statement is ExpressionCast cast)
                return UnCast(cast.Argument);

            return statement;
        }

        private static bool TestNumber(Statement statement, int number, DecompileContext context = null)
        {
            statement = UnCast(statement);
            return (statement is ExpressionConstant constant) && constant.EqualsNumber(number);
        }

        public static List<Statement> HLDecompile(DecompileContext context, Dictionary<uint, Block> blocks, Block entryPoint, Block rootExitPoint)
        {
            Dictionary<Block, List<Block>> loops = ComputeNaturalLoops(blocks, entryPoint);
            var reverseDominators = ComputeReverseDominators(blocks, rootExitPoint);
            Block bl = entryPoint;
            return (HLDecompileBlocks(context, ref bl, blocks, loops, reverseDominators, new List<Block>()).CleanBlockStatement(context)).Statements;
        }

        public static Dictionary<uint, Block> PrepareDecompileFlow(UndertaleCode code, List<uint> entryPoints)
        {
            if (code.ParentEntry != null)
                throw new InvalidOperationException("This code block represents a function nested inside " + code.ParentEntry.Name + " - decompile that instead");
            code.UpdateAddresses();

            Dictionary<uint, Block> blocks = DecompileFlowGraph(code, entryPoints);

            return blocks;
        }

        public static Dictionary<uint, Block> PrepareDecompileFlow(UndertaleCode code)
        {
            List<uint> entryPoints = new List<uint>();
            entryPoints.Add(0);
            foreach (UndertaleCode duplicate in code.ChildEntries)
                entryPoints.Add(duplicate.Offset / 4);

            return PrepareDecompileFlow(code, entryPoints);
        }

        private static string MakeLocalVars(DecompileContext context, string decompiledCode)
        {
            // Mark local variables as local.
            UndertaleCode code = context.TargetCode;
            StringBuilder tempBuilder = new StringBuilder();
            UndertaleCodeLocals locals = context.GlobalContext.Data?.CodeLocals.For(code);

            List<string> possibleVars = new List<string>();
            if (locals != null)
            {
                foreach (var local in locals.Locals)
                    possibleVars.Add(local.Name.Content);
            }
            else
            {
                // Time to search through this thing manually.
                for (int i = 0; i < code.Instructions.Count; i++)
                {
                    var inst = code.Instructions[i];
                    if (inst.Kind == UndertaleInstruction.Opcode.PushLoc)
                    {
                        string name = (inst.Value as UndertaleInstruction.Reference<UndertaleVariable>)?.Target?.Name?.Content;
                        if (name != null && !possibleVars.Contains(name))
                            possibleVars.Add(name);
                    }
                    else if (inst.Kind == UndertaleInstruction.Opcode.Pop && inst.TypeInst == UndertaleInstruction.InstanceType.Local)
                    {
                        string name = inst.Destination.Target?.Name?.Content;
                        if (name != null && !possibleVars.Contains(name))
                            possibleVars.Add(name);
                    }
                }
            }

            foreach (var possibleName in possibleVars)
            {
                if (possibleName == "arguments" || possibleName == "$$$$temp$$$$" || context.LocalVarDefines.Contains(possibleName))
                    continue;

                if (tempBuilder.Length > 0)
                    tempBuilder.Append(", ");

                tempBuilder.Append(possibleName);
            }

            // Add tempvars to locals
            string oldStr = tempBuilder.ToString();
            for (int i = 0; i < context.TempVarId; i++)
            {
                string tempVarName = TempVar.MakeTemporaryVarName(i + 1);
                if (decompiledCode.Contains(tempVarName) && !oldStr.Contains(tempVarName))
                {
                    if (tempBuilder.Length > 0)
                        tempBuilder.Append(", ");
                    tempBuilder.Append(tempVarName);
                }
            }

            string result = "";
            if (tempBuilder.Length > 0)
                result = "var " + tempBuilder.ToString() + ";\n";
            return result;
        }

        public static string Decompile(UndertaleCode code, GlobalDecompileContext globalContext, Action<string> msgDelegate = null)
        {
            globalContext.DecompilerWarnings.Clear();
            DecompileContext context = new DecompileContext(globalContext, code);

            if (msgDelegate is not null)
                msgDelegate("Building the cache of all sub-functions...");
            BuildSubFunctionCache(globalContext.Data);
            if (msgDelegate is not null)
                msgDelegate("Decompiling, please wait... This can take a while on complex scripts.");

            try
            {
                if (globalContext.Data != null && globalContext.Data.ToolInfo.ProfileMode)
                {
                    string GMLPath = Path.Combine(globalContext.Data.ToolInfo.AppDataProfiles,
                                                  globalContext.Data.ToolInfo.CurrentMD5, "Temp", code.Name.Content + ".gml");
                    if (File.Exists(GMLPath))
                        return File.ReadAllText(GMLPath);
                }
            }
            catch
            {
                // Just ignore the exception and decompile normally
            }

            Dictionary<uint, Block> blocks = PrepareDecompileFlow(code);
            DecompileFromBlock(context, blocks, blocks[0]);
            DoTypePropagation(context, blocks);
            context.Statements = new Dictionary<uint, List<Statement>>();
            context.Statements.Add(0, HLDecompile(context, blocks, blocks[0], blocks[code.Length / 4]));
            foreach (UndertaleCode duplicate in code.ChildEntries) {
                List<Statement> statements = HLDecompile(context, blocks, blocks[duplicate.Offset / 4], blocks[code.Length / 4]);

                // 2.3 scripts add exits to every script, even those that lack a return
                // This removes that
                if (DecompileContext.GMS2_3 && statements.Last() is ReturnStatement)
                    statements.RemoveAt(statements.Count - 1);

                context.Statements.Add(duplicate.Offset / 4, statements);
            }

            // Write code.
            context.IndentationLevel = 0;
            StringBuilder sb = new StringBuilder();
            foreach (var warn in globalContext.DecompilerWarnings)
                sb.Append(warn + "\n");
            foreach (var stmt in context.Statements[0])
            {
                // Ignore initial struct definitions, they clutter
                // decompiled output and generally make code more
                // confusing to read.
                if (stmt is AssignmentStatement assign && assign.IsStructDefinition)
                    continue;

                sb.Append(stmt.ToString(context) + "\n");
            }

            globalContext.DecompilerWarnings.Clear();
            context.Statements = null;

            string decompiledCode = sb.ToString();
            return MakeLocalVars(context, decompiledCode) + decompiledCode;
        }

        public static void BuildSubFunctionCache(UndertaleData data)
        {
            // Find all functions defined in GlobalScripts
            // Use the cache so this only gets calculated once
            if (data == null || !data.IsVersionAtLeast(2, 3) || data.KnownSubFunctions != null)
                return;

            // There's no "ConcurrentHashSet<>"; values aren't used.
            ConcurrentDictionary<string, string> processingCodeList = new();
            byte elapsedSec = 1;
            Task mainTask = Task.Run(() =>
            {
                HashSet<string> codeNames = new(data.Code.Select(c => c.Name?.Content));
                foreach (var func in data.Functions)
                {
                    if (codeNames.Contains(func.Name.Content))
                        func.Autogenerated = true;
                }
                data.KnownSubFunctions = new Dictionary<string, UndertaleFunction>();
                GlobalDecompileContext globalDecompileContext = new GlobalDecompileContext(data, false);

                // TODO: Is this necessary?
                // Doesn't the latter `Parallel.ForEach()` already cover this?
                foreach (var func in data.Functions)
                {
                    if (func.Name.Content.StartsWith("gml_Script_"))
                    {
                        var funcName = func.Name.Content[("gml_Script_".Length)..];
                        data.KnownSubFunctions.TryAdd(funcName, func);
                    }
                }

                Parallel.ForEach(data.GlobalInitScripts, globalScript =>
                {
                    UndertaleCode scriptCode = globalScript.Code;
                    processingCodeList[scriptCode.Name.Content] = null;
                    try
                    {
                        DecompileContext childContext = new DecompileContext(globalDecompileContext, scriptCode, false);
                        childContext.DisableAnonymousFunctionNameResolution = true;
                        Dictionary<uint, Block> blocks2 = PrepareDecompileFlow(scriptCode, new List<uint>() { 0 });
                        DecompileFromBlock(childContext, blocks2, blocks2[0]);
                        List<Statement> statements = HLDecompile(childContext, blocks2, blocks2[0], blocks2[scriptCode.Length / 4]);
                        foreach (Statement stmt2 in statements)
                        {
                            if (stmt2 is AssignmentStatement assign &&
                                assign.Value is FunctionDefinition funcDef)
                            {
                                lock (data.KnownSubFunctions)
                                {
                                    data.KnownSubFunctions.TryAdd(assign.Destination.Var.Name.Content, funcDef.Function);
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.ToString());
                    }
                    processingCodeList.Remove(scriptCode.Name.Content, out _);
                });
                elapsedSec = 3 * 60;
            });

            Task timeoutTask = Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(1000);

                    if (++elapsedSec > 3 * 60)
                        return;
                }
            });

            // If the timeout task ended earlier than the main task
            if (Task.WaitAny(mainTask, timeoutTask) == 1)
            {
                throw new TimeoutException("The building cache process hung.\n" +
                                           "The function code entries that didn't manage to decompile:\n" +
                                           String.Join('\n', processingCodeList.Keys) + "\n\n" +
                                           "You should save the game data (if it's necessary) and re-open the app.\n");
            }
        }

        private static void DoTypePropagation(DecompileContext context, Dictionary<uint, Block> blocks)
        {
            foreach (var b in blocks.Values.Cast<Block>().Reverse())
            {
                if (b.Statements != null) // With returns not allowing all blocks coverage, make sure it's even been processed
                    foreach (var s in b.Statements.Cast<Statement>().Reverse())
                        s.DoTypePropagation(context, AssetIDType.Other);

                b.ConditionStatement?.DoTypePropagation(context, AssetIDType.Other);
            }
        }

        private static Block DetermineSwitchEnd(Block start, Block end, Block meetPoint)
        {
            if (end == null)
                return meetPoint;

            Queue<Block> blocks = new Queue<Block>();
            // Preventing the same block and its children from being queued repeatedly
            // becomes increasingly important on large switches. The HashSet should give
            // good performance while preventing this type of duplication.
            HashSet<Block> usedBlocks = new HashSet<Block>(); 

            blocks.Enqueue(start);
            usedBlocks.Add(start);
            while (blocks.Count > 0)
            {
                Block test = blocks.Dequeue();

                if (test == end)
                    return end;
                if (test == meetPoint)
                    return meetPoint;
                if (!usedBlocks.Contains(test.nextBlockTrue))
                {
                    blocks.Enqueue(test.nextBlockTrue);
                    usedBlocks.Add(test.nextBlockTrue);
                }
                if (!usedBlocks.Contains(test.nextBlockFalse))
                {
                    blocks.Enqueue(test.nextBlockFalse);
                    usedBlocks.Add(test.nextBlockFalse);
                }

            }

            return meetPoint;
        }

        public static string ExportFlowGraph(Dictionary<uint, Block> blocks)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("digraph G {");
            foreach (var pair in blocks)
            {
                var block = pair.Value;
                sb.Append("    block_" + pair.Key + " [label=\"");
                sb.Append("[" + block.ToString() + ", Exit: " + block.conditionalExit + (block.nextBlockTrue != null ? ", T: " + block.nextBlockTrue.Address : "") + (block.nextBlockFalse != null ? ", F: " + block.nextBlockFalse.Address : "") + "]\n");
                foreach (var instr in block.Instructions)
                    sb.Append(instr.ToString().Replace("\"", "\\\"", StringComparison.InvariantCulture) + "\\n");
                sb.Append('"');
                sb.Append(pair.Key == 0 ? ", color=\"blue\"" : "");
                sb.AppendLine(", shape=\"box\"];");
            }
            sb.AppendLine("");
            foreach (var block in blocks)
            {
                if (block.Value.conditionalExit)
                {
                    if (block.Value.nextBlockTrue != null)
                        sb.AppendLine("    block_" + block.Key + " -> block_" + block.Value.nextBlockTrue.Address + " [color=\"green\"];");
                    if (block.Value.nextBlockFalse != null)
                        sb.AppendLine("    block_" + block.Key + " -> block_" + block.Value.nextBlockFalse.Address + " [color=\"red\"];");
                }
                else
                {
                    if (block.Value.nextBlockTrue != null)
                        sb.AppendLine("    block_" + block.Key + " -> block_" + block.Value.nextBlockTrue.Address + ";");
                }
            }
            sb.AppendLine("}");
            return sb.ToString();
        }
    }
}
