using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib.Models;

namespace UndertaleModLib.Decompiler
{
    public static class Decompiler
    {
        /**
         * Howdy! Yeah, I don't know how any of this works anymore either, so... have fun
         */

        public class Block
        {
            public uint? Address;
            public List<UndertaleInstruction> Instructions = new List<UndertaleInstruction>();
            public List<Statement> Statements = null;
            public Expression ConditionStatement = null;
            public bool conditionalExit;
            public Block nextBlockTrue;
            public Block nextBlockFalse;
            public List<Block> entryPoints = new List<Block>();
            internal List<TempVarReference> TempVarsOnEntry;

            public Block(uint? address)
            {
                Address = address;
            }

            public override string ToString()
            {
                return "Block " + Address;
            }
        }

        public abstract class Statement
        {
            public abstract override string ToString();
        }

        public abstract class Expression : Statement
        {
            public UndertaleInstruction.DataType Type;

            public static string OperationToPrintableString(UndertaleInstruction.Opcode op)
            {
                switch(op)
                {
                    case UndertaleInstruction.Opcode.Mul:
                        return "*";
                    case UndertaleInstruction.Opcode.Div:
                        return "/";
                    /*case UndertaleInstruction.Opcode.Rem:
                        return "%";*/ // TODO: ?
                    case UndertaleInstruction.Opcode.Mod:
                        return "%";
                    case UndertaleInstruction.Opcode.Add:
                        return "+";
                    case UndertaleInstruction.Opcode.Sub:
                        return "-";
                    case UndertaleInstruction.Opcode.And:
                        return "&";
                    case UndertaleInstruction.Opcode.Or:
                        return "|";
                    case UndertaleInstruction.Opcode.Xor:
                        return "^";
                    case UndertaleInstruction.Opcode.Neg:
                        return "-";
                    case UndertaleInstruction.Opcode.Not:
                        return "~";
                    case UndertaleInstruction.Opcode.Shl:
                        return "<<";
                    case UndertaleInstruction.Opcode.Shr:
                        return ">>";
                    default:
                        return op.ToString().ToUpper();
                }
            }

            public static string OperationToPrintableString(UndertaleInstruction.ComparisonType op)
            {
                switch (op)
                {
                    case UndertaleInstruction.ComparisonType.LT:
                        return "<";
                    case UndertaleInstruction.ComparisonType.LTE:
                        return "<=";
                    case UndertaleInstruction.ComparisonType.EQ:
                        return "==";
                    case UndertaleInstruction.ComparisonType.NEQ:
                        return "!=";
                    case UndertaleInstruction.ComparisonType.GTE:
                        return ">=";
                    case UndertaleInstruction.ComparisonType.GT:
                        return ">";
                    default:
                        return op.ToString().ToUpper();
                }
            }
        }

        public class ExpressionConstant : Expression
        {
            public object Value;

            public ExpressionConstant(UndertaleInstruction.DataType type, object value)
            {
                Type = type;
                Value = value;
            }

            public override string ToString()
            {
                //return String.Format("{0}({1})", Type.ToString().ToLower(), Value.ToString());
                return (Value as IFormattable)?.ToString(null, CultureInfo.InvariantCulture) ?? Value.ToString();
            }
        }

        public class ExpressionCast : Expression
        {
            public Expression Argument;

            public ExpressionCast(UndertaleInstruction.DataType targetType, Expression argument)
            {
                this.Type = targetType;
                this.Argument = argument;
            }

            public override string ToString()
            {
                //return String.Format("{0}({1})", Type != Argument.Type ? "(" + Type.ToString().ToLower() + ")" : "", Argument.ToString());
                return Argument.ToString();
            }
        }

        public class ExpressionOne : Expression
        {
            public UndertaleInstruction.Opcode Opcode;
            public Expression Argument;

            public ExpressionOne(UndertaleInstruction.Opcode opcode, UndertaleInstruction.DataType targetType, Expression argument)
            {
                this.Opcode = opcode;
                this.Type = targetType;
                this.Argument = argument;
            }

            public override string ToString()
            {
                return String.Format("{0}({1} {2})", Type != Argument.Type ? "(" + Type.ToString().ToLower() + ")" : "", OperationToPrintableString(Opcode), Argument.ToString());
            }
        }
        
        public class ExpressionTwo : Expression
        {
            public UndertaleInstruction.Opcode Opcode;
            public Expression Argument1;
            public Expression Argument2;

            public ExpressionTwo(UndertaleInstruction.Opcode opcode, UndertaleInstruction.DataType targetType, Expression argument1, Expression argument2)
            {
                this.Opcode = opcode;
                this.Type = targetType;
                this.Argument1 = argument1;
                this.Argument2 = argument2;
            }

            public override string ToString()
            {
                // TODO: better condition for casts
                return String.Format("{0}({1} {2} {3})", false && (Type != Argument1.Type || Type != Argument2.Type || Argument1.Type != Argument2.Type) ? "(" + Type.ToString().ToLower() + ")" : "", Argument1.ToString(), OperationToPrintableString(Opcode), Argument2.ToString());
            }
        }

        public class ExpressionCompare : Expression
        {
            public UndertaleInstruction.ComparisonType Opcode;
            public Expression Argument1;
            public Expression Argument2;

            public ExpressionCompare(UndertaleInstruction.ComparisonType opcode, Expression argument1, Expression argument2)
            {
                this.Opcode = opcode;
                this.Type = UndertaleInstruction.DataType.Boolean;
                this.Argument1 = argument1;
                this.Argument2 = argument2;
            }

            public override string ToString()
            {
                return String.Format("({0} {1} {2})", Argument1.ToString(), OperationToPrintableString(Opcode), Argument2.ToString());
            }
        }

        public class OperationStatement : Statement
        {
            public UndertaleInstruction.Opcode Opcode;

            public OperationStatement(UndertaleInstruction.Opcode opcode)
            {
                this.Opcode = opcode;
            }

            public override string ToString()
            {
                return Opcode.ToString().ToUpper();
            }
        }


        public class TempVar
        {
            public string Name;
            public UndertaleInstruction.DataType Type;

            private static int i = 0;
            public TempVar()
            {
                Name = "_temp_local_var_" + (++i);
            }
        }

        public class TempVarReference
        {
            public TempVar Var;

            public TempVarReference(TempVar var)
            {
                Var = var;
            }
        }

        public class TempVarAssigmentStatement : Statement
        {
            public TempVarReference Var;
            public Expression Value;

            public TempVarAssigmentStatement(TempVarReference var, Expression value)
            {
                Var = var;
                Value = value;
            }

            public override string ToString()
            {
                return String.Format("{0} = {1}", Var.Var.Name, Value);
            }
        }

        public class ExpressionTempVar : Expression
        {
            public TempVarReference Var;

            public ExpressionTempVar(TempVarReference var, UndertaleInstruction.DataType targetType)
            {
                this.Var = var;
                this.Type = targetType;
            }

            public override string ToString()
            {
                return String.Format("{0}{1}", Type != Var.Var.Type ? "(" + Type.ToString().ToLower() + ")" : "", Var.Var.Name);
            }
        }

        public class ReturnStatement : Statement
        {
            public Expression Value;

            public ReturnStatement(Expression value)
            {
                Value = value;
            }

            public override string ToString()
            {
                if (Value != null)
                    return "return " + Value.ToString();
                else
                    return "return";
            }
        }

        public class AssignmentStatement : Statement
        {
            public Expression Destination;
            public Expression Value;

            public AssignmentStatement(Expression destination, Expression value)
            {
                Destination = destination;
                Value = value;
            }

            public override string ToString()
            {
                return String.Format("{0} = {1}", Destination.ToString(), Value.ToString());
            }
        }

        public class CommentStatement : Statement
        {
            public string Message;

            public CommentStatement(string message)
            {
                Message = message;
            }

            public override string ToString()
            {
                return "// " + Message;
            }
        }

        public class FunctionCall : Expression
        {
            private UndertaleFunction Function;
            private UndertaleInstruction.DataType ReturnType;
            private List<Expression> Arguments;

            public FunctionCall(UndertaleFunction function, UndertaleInstruction.DataType returnType, List<Expression> args)
            {
                this.Function = function;
                this.ReturnType = returnType;
                this.Arguments = args;

                // get these casts out of my way
                for(int i = 0; i < Arguments.Count; i++)
                {
                    if (Arguments[i] is ExpressionCast && (Arguments[i] as ExpressionCast).Type == UndertaleInstruction.DataType.Variable)
                        Arguments[i] = (Arguments[i] as ExpressionCast).Argument;
                }
            }

            public override string ToString()
            {
                //return String.Format("({0}){1}({2})", ReturnType.ToString().ToLower(), Function.Name.Content, String.Join(", ", Arguments));
                return String.Format("{0}({1})", Function.Name.Content, String.Join(", ", Arguments));
            }
        }

        public class ExpressionVar : Expression
        {
            public UndertaleVariable Var;
            public Expression InstType; // UndertaleInstruction.InstanceType
            public UndertaleInstruction.VariableType VarType;
            public Expression ArrayIndex;
            public Expression InstanceIndex;

            public ExpressionVar(UndertaleVariable var, Expression instType, UndertaleInstruction.VariableType varType)
            {
                Var = var;
                InstType = instType;
                VarType = varType;
            }

            private UndertaleInstruction.InstanceType? TryGetInstType()
            {
                // TODO: this should be handled along with type information propagation
                if (InstType is ExpressionConstant)
                    return (UndertaleInstruction.InstanceType)Convert.ToInt32((InstType as ExpressionConstant).Value);
                else
                    return null;
            }

            public override string ToString()
            {
                //Debug.Assert((ArrayIndex != null) == NeedsArrayParameters);
                //Debug.Assert((InstanceIndex != null) == NeedsInstanceParameters);
                string name = Var.Name.Content;
                if (InstanceIndex != null)
                    name = InstanceIndex.ToString() + "." + name;
                if (ArrayIndex != null)
                    name = name + "[" + ArrayIndex.ToString() + "]";
                var instTypeVal = TryGetInstType();
                if (instTypeVal.HasValue)
                {
                    if (instTypeVal.Value != UndertaleInstruction.InstanceType.Undefined)
                    {
                        name = instTypeVal.Value.ToString().ToLower() + "." + name;
                    }
                }
                else
                {
                    name = InstType.ToString() + "." + name;
                }

                return name;
            }

            public bool NeedsArrayParameters => VarType == UndertaleInstruction.VariableType.Array;
            public bool NeedsInstanceParameters => /*InstType == UndertaleInstruction.InstanceType.StackTopOrGlobal &&*/ VarType == UndertaleInstruction.VariableType.StackTop;
        }

        public class PushEnvStatement : Statement
        {
            public Expression NewEnv;

            public PushEnvStatement(Expression newEnv)
            {
                this.NewEnv = newEnv;
            }

            public override string ToString()
            {
                return "pushenv " + NewEnv;
            }
        }

        public class PopEnvStatement : Statement
        {
            public override string ToString()
            {
                return "popenv";
            }
        }

        internal static void DecompileFromBlock(Block block, List<TempVarReference> tempvars, Stack<Tuple<Block, List<TempVarReference>>> workQueue)
        {
            if (block.TempVarsOnEntry != null && (block.nextBlockTrue != null || block.nextBlockFalse != null)) // TODO: RET breaks it?
            {
                // Reroute tempvars to alias them to our ones
                if (block.TempVarsOnEntry.Count != tempvars.Count)
                {
                    //throw new Exception("Reentered block with different amount of vars on stack");
                    block.Statements.Add(new CommentStatement("Something was wrong with the stack, reentered the block with " + tempvars.Count + " variables instead of " + block.TempVarsOnEntry.Count + ", ignoring"));
                }
                else
                {
                    for (int i = 0; i < tempvars.Count; i++)
                    {
                        tempvars[i].Var = block.TempVarsOnEntry[i].Var;
                    }
                }
            }

            if (block.Statements != null)
                return; // don't decompile again :P

            block.TempVarsOnEntry = tempvars;

            Stack<Expression> stack = new Stack<Expression>();
            foreach (TempVarReference var in tempvars)
                stack.Push(new ExpressionTempVar(var, var.Var.Type));

            List<Statement> statements = new List<Statement>();
            bool end = false;
            foreach(var instr in block.Instructions)
            {
                if (end)
                    throw new Exception("Excepted end of block, but still has instructions");
                switch(instr.Kind)
                {
                    case UndertaleInstruction.Opcode.Neg:
                    case UndertaleInstruction.Opcode.Not:
                        stack.Push(new ExpressionOne(instr.Kind, instr.Type1, stack.Pop()));
                        break;

                    case UndertaleInstruction.Opcode.Dup:
                        if (stack.Peek() is ExpressionConstant || (stack.Peek() is ExpressionCast && (stack.Peek() as ExpressionCast).Argument is ExpressionConstant)) // TODO: do this better
                        {
                            List<Expression> topExpressions = new List<Expression>();
                            for (int i = 0; i < instr.DupExtra + 1; i++)
                                topExpressions.Add(stack.Pop());
                            topExpressions.Reverse();
                            for (int i = 0; i < topExpressions.Count; i++)
                                stack.Push(topExpressions[i]);
                            for (int i = 0; i < topExpressions.Count; i++)
                                stack.Push(topExpressions[i]);
                        }
                        else
                        {
                            List<TempVarReference> topExpressions = new List<TempVarReference>();
                            for (int i = 0; i < instr.DupExtra + 1; i++)
                            {
                                TempVar var = new TempVar();
                                var.Type = stack.Peek().Type;
                                TempVarReference varref = new TempVarReference(var);
                                statements.Add(new TempVarAssigmentStatement(varref, stack.Pop()));
                                topExpressions.Add(varref);
                            }
                            for (int i = 0; i < topExpressions.Count; i++)
                                stack.Push(new ExpressionTempVar(topExpressions[i], topExpressions[i].Var.Type));
                            for (int i = 0; i < topExpressions.Count; i++)
                                stack.Push(new ExpressionTempVar(topExpressions[i], instr.Type1));
                        }
                        break;

                    case UndertaleInstruction.Opcode.Ret:
                    case UndertaleInstruction.Opcode.Exit:
                        ReturnStatement stmt = new ReturnStatement(instr.Kind == UndertaleInstruction.Opcode.Ret ? stack.Pop() : null);
                        foreach (var expr in stack.Reverse())
                            if (!(expr is ExpressionTempVar))
                                statements.Add(expr);
                        stack.Clear();
                        statements.Add(stmt);
                        end = true;
                        break;

                    case UndertaleInstruction.Opcode.Popz:
                        Expression popped = stack.Pop();
                        if (!(popped is ExpressionTempVar))
                            statements.Add(popped);
                        break;

                    case UndertaleInstruction.Opcode.Conv:
                        /*if (instr.Type1 != stack.Peek().Type)
                            stack.Push(new ExpressionCast(instr.Type1, stack.Pop()));*/
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
                        stack.Push(new ExpressionTwo(instr.Kind, instr.Type1, a1, a2)); // TODO: type
                        break;

                    case UndertaleInstruction.Opcode.Cmp:
                        Expression aa2 = stack.Pop();
                        Expression aa1 = stack.Pop();
                        stack.Push(new ExpressionCompare(instr.ComparisonKind, aa1, aa2)); // TODO: type
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
                        statements.Add(new PushEnvStatement(stack.Pop()));
                        end = true;
                        break;

                    case UndertaleInstruction.Opcode.PopEnv:
                        statements.Add(new PopEnvStatement());
                        end = true;
                        break;

                    case UndertaleInstruction.Opcode.Pop:
                        ExpressionVar target = new ExpressionVar(instr.Destination.Target, new ExpressionConstant(UndertaleInstruction.DataType.Int16, instr.TypeInst), instr.Destination.Type);
                        Expression val = null;
                        if (instr.Type1 != UndertaleInstruction.DataType.Int32 && instr.Type1 != UndertaleInstruction.DataType.Variable)
                            throw new Exception("Oh no, what do I do with this POP? OH NOOOOOOOoooooooooo");
                        if (instr.Type1 == UndertaleInstruction.DataType.Int32)
                            val = stack.Pop();
                        if (target.NeedsInstanceParameters)
                            target.InstanceIndex = stack.Pop();
                        if (target.NeedsArrayParameters)
                        {
                            target.ArrayIndex = stack.Pop();
                            target.InstType = stack.Pop();
                        }
                        if (instr.Type1 == UndertaleInstruction.DataType.Variable)
                            val = stack.Pop();
                        Debug.Assert(val != null);
                        statements.Add(new AssignmentStatement(target, val));
                        break;

                    case UndertaleInstruction.Opcode.Push:
                    case UndertaleInstruction.Opcode.PushLoc:
                    case UndertaleInstruction.Opcode.PushGlb:
                    case UndertaleInstruction.Opcode.PushVar:
                    case UndertaleInstruction.Opcode.PushI:
                        if (instr.Value is UndertaleInstruction.Reference<UndertaleVariable>)
                        {
                            ExpressionVar pushTarget = new ExpressionVar((instr.Value as UndertaleInstruction.Reference<UndertaleVariable>).Target, new ExpressionConstant(UndertaleInstruction.DataType.Int16, instr.TypeInst), (instr.Value as UndertaleInstruction.Reference<UndertaleVariable>).Type);
                            if (pushTarget.NeedsInstanceParameters)
                                pushTarget.InstanceIndex = stack.Pop();
                            if (pushTarget.NeedsArrayParameters)
                            {
                                pushTarget.ArrayIndex = stack.Pop();
                                pushTarget.InstType = stack.Pop();
                            }
                            stack.Push(pushTarget);
                        }
                        else
                        {
                            Expression pushTarget = new ExpressionConstant(instr.Type1, instr.Value);
                            stack.Push(pushTarget);
                        }
                        break;

                    case UndertaleInstruction.Opcode.Call:
                        List<Expression> args = new List<Expression>();
                        for (int i = 0; i < instr.ArgumentsCount; i++)
                            args.Add(stack.Pop());
                        stack.Push(new FunctionCall(instr.Function.Target, instr.Type1, args));
                        break;

                    case UndertaleInstruction.Opcode.Break:
                        //statements.Add(new CommentStatement("// TODO: BREAK " + (short)instr.Value));
                        // This is used for checking bounds in 2D arrays
                        // I'm not sure of the specifics but I guess it causes a debug breakpoint if the top of the stack is >= 32000
                        // anyway, that's not important when decompiling to high-level code so just ignore it
                        break;
                }
            }

            // Convert everything that remains on the stack to a temp var
            List<TempVarReference> leftovers = new List<TempVarReference>();
            for(int i = stack.Count-1; i >= 0; i--)
            {
                if (i < tempvars.Count)
                {
                    Expression val = stack.Pop();
                    if (!(val is ExpressionTempVar) || (val as ExpressionTempVar).Var != tempvars[i] )
                        statements.Add(new TempVarAssigmentStatement(tempvars[i], val));
                    leftovers.Add(tempvars[i]);
                }
                else
                {
                    Expression val = stack.Pop();
                    TempVar var = new TempVar();
                    var.Type = val.Type;
                    TempVarReference varref = new TempVarReference(var);
                    statements.Add(new TempVarAssigmentStatement(varref, val));
                    leftovers.Add(varref);
                }
            }
            leftovers.Reverse();

            block.Statements = statements;
            if (block.nextBlockFalse != null)
                workQueue.Push(new Tuple<Block, List<TempVarReference>>(block.nextBlockFalse, leftovers));
            if (block.nextBlockTrue != null)
                workQueue.Push(new Tuple<Block, List<TempVarReference>>(block.nextBlockTrue, leftovers));
        }

        public static void DecompileFromBlock(Block block)
        {
            Stack<Tuple<Block, List<TempVarReference>>> workQueue = new Stack<Tuple<Block, List<TempVarReference>>>();
            workQueue.Push(new Tuple<Block, List<TempVarReference>>(block, new List<TempVarReference>()));
            while(workQueue.Count > 0)
            {
                var item = workQueue.Pop();
                DecompileFromBlock(item.Item1, item.Item2, workQueue);
            }
        }

        public static Dictionary<uint, Block> DecompileFlowGraph(UndertaleCode code)
        {
            Dictionary<uint, Block> blockByAddress = new Dictionary<uint, Block>();
            blockByAddress[0] = new Block(0);
            Block entryBlock = new Block(null);
            Block finalBlock = new Block(code.Length / 4);
            blockByAddress[code.Length / 4] = finalBlock;
            Block currentBlock = entryBlock;

            foreach(var instr in code.Instructions)
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
                            foreach(var block in blockByAddress)
                            {
                                if (block.Key < addr && (blockToSplit == null || block.Key > blockToSplit.Address))
                                    blockToSplit = block.Value;
                            }

                            // Now, split the list of instructions into two
                            List<UndertaleInstruction> instrBefore = new List<UndertaleInstruction>();
                            List<UndertaleInstruction> instrAfter = new List<UndertaleInstruction>();
                            foreach(UndertaleInstruction inst in blockToSplit.Instructions)
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
                    currentBlock.nextBlockTrue = finalBlock;
                    currentBlock.nextBlockFalse = finalBlock;
                    currentBlock = null;
                }
            }
            if (currentBlock != null)
            {
                currentBlock.nextBlockTrue = finalBlock;
                currentBlock.nextBlockFalse = finalBlock;
            }
            foreach(var block in blockByAddress.Values)
            {
                if (block.nextBlockTrue != null && !block.nextBlockTrue.entryPoints.Contains(block))
                    block.nextBlockTrue.entryPoints.Add(block);
                if (block.nextBlockFalse != null && !block.nextBlockFalse.entryPoints.Contains(block))
                    block.nextBlockFalse.entryPoints.Add(block);
            }
            return blockByAddress;
        }

        public abstract class HLStatement : Statement
        {
        };

        public class BlockHLStatement : HLStatement
        {
            public List<Statement> Statements = new List<Statement>();

            public string ToString(bool canSkipBrackets = true)
            {
                if (Statements.Count == 1 && !(Statements[0] is IfHLStatement) && !(Statements[0] is LoopHLStatement) && !(Statements[0] is WithHLStatement) && canSkipBrackets)
                    return "    " + Statements[0].ToString().Replace("\n", "\n    ");
                else
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("{\n");
                    foreach(var stmt in Statements)
                    {
                        sb.Append("    ");
                        sb.Append(stmt.ToString().Replace("\n", "\n    "));
                        sb.Append("\n");
                    }
                    sb.Append("}");
                    return sb.ToString();
                }
            }

            public override string ToString()
            {
                return ToString(true);
            }
        };

        public class IfHLStatement : HLStatement
        {
            public Expression condition;
            public BlockHLStatement trueBlock;
            public BlockHLStatement falseBlock;

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("if " + condition.ToString() + "\n");
                sb.Append(trueBlock.ToString());
                if (falseBlock != null && falseBlock.Statements.Count > 0)
                {
                    sb.Append("\nelse\n");
                    sb.Append(falseBlock.ToString());
                }
                return sb.ToString();
            }
        };

        public class LoopHLStatement : HLStatement
        {
            public BlockHLStatement Block;

            public override string ToString()
            {
                return "while(true)\n" + Block.ToString();
            }
        };

        public class ContinueHLStatement : HLStatement
        {
            public override string ToString()
            {
                return "continue";
            }
        }

        public class BreakHLStatement : HLStatement
        {
            public override string ToString()
            {
                return "break";
            }
        }

        public class WithHLStatement : HLStatement
        {
            public Expression NewEnv;
            public BlockHLStatement Block;

            public override string ToString()
            {
                return "with(" + NewEnv.ToString() + ")\n" + Block.ToString(false);
            }
        }

        // Based on http://www.backerstreet.com/decompiler/loop_analysis.php
        public static Dictionary<Block, List<Block>> ComputeDominators(Dictionary<uint, Block> blocks, Block entryBlock, bool reversed)
        {
            List<Block> blockList = blocks.Values.ToList();
            List<BitArray> dominators = new List<BitArray>();

            for (int i = 0; i < blockList.Count; i++) {
                dominators.Add(new BitArray(blockList.Count));
                dominators[i].SetAll(true);
            }

            var entryBlockId = blockList.IndexOf(entryBlock);
            dominators[entryBlockId].SetAll(false);
            dominators[entryBlockId].Set(entryBlockId, true);

            BitArray temp = new BitArray(blockList.Count);
            bool changed = true;
            do
            {
                changed = false;
                for (int i = 0; i < blockList.Count; i++)
                {
                    if (i == entryBlockId)
                        continue;

                    IEnumerable<Block> e = blockList[i].entryPoints;
                    if (reversed)
                        if (blockList[i].conditionalExit)
                            e = new Block[] { blockList[i].nextBlockTrue, blockList[i].nextBlockFalse };
                        else
                            e = new Block[] { blockList[i].nextBlockTrue };
                    foreach (Block pred in e)
                    {
                        var predId = blockList.IndexOf(pred);
                        Debug.Assert(predId >= 0);
                        temp.SetAll(false);
                        temp.Or(dominators[i]);
                        dominators[i].And(dominators[predId]);
                        dominators[i].Set(i, true);
                        /*if (!dominators[i].SequenceEquals(temp))
                            changed = true;*/
                        for(var j = 0; j < blockList.Count; j++)
                            if (dominators[i][j] != temp[j])
                            {
                                changed = true;
                                break;
                            }
                    }
                }
            } while (changed);

            Dictionary<Block, List<Block>> result = new Dictionary<Block, List<Block>>();
            for(var i = 0; i < blockList.Count; i++)
            {
                result[blockList[i]] = new List<Block>();
                for(var j = 0; j < blockList.Count; j++)
                {
                    if (dominators[i].Get(j))
                        result[blockList[i]].Add(blockList[j]);
                }
            }
            return result;
        }

        private static List<Block> NaturalLoopForEdge(Block header, Block tail)
        {
            Stack<Block> workList = new Stack<Block>();
            List<Block> loopBlocks = new List<Block>();

            loopBlocks.Add(header);
            if (header != tail)
            {
                loopBlocks.Add(tail);
                workList.Push(tail);
            }

            while(workList.Count > 0)
            {
                Block block = workList.Pop();
                foreach(Block pred in block.entryPoints)
                {
                    if(!loopBlocks.Contains(pred))
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
            var dominators = ComputeDominators(blocks, entryBlock, false);
            Dictionary<Block, List<Block>> loopSet = new Dictionary<Block, List<Block>>();

            foreach(var block in blocks.Values)
            {
                // Every successor that dominates its predecessor
                // must be the header of a loop.
                // That is, block -> succ is a back edge.

                if (block.nextBlockTrue != null && !loopSet.ContainsKey(block.nextBlockTrue))
                {
                    if (dominators[block].Contains(block.nextBlockTrue))
                        loopSet.Add(block.nextBlockTrue, NaturalLoopForEdge(block.nextBlockTrue, block));
                }
                if (block.nextBlockFalse != null && block.nextBlockTrue != block.nextBlockFalse && !loopSet.ContainsKey(block.nextBlockFalse))
                {
                    if (dominators[block].Contains(block.nextBlockFalse))
                        loopSet.Add(block.nextBlockFalse, NaturalLoopForEdge(block.nextBlockFalse, block));
                }
            }

            return loopSet;
        }

        public static Block FindFirstMeetPoint(Block ifStart, Dictionary<Block, List<Block>> reverseDominators)
        {
            Debug.Assert(ifStart.conditionalExit);
            var commonDominators = reverseDominators[ifStart.nextBlockTrue].Intersect(reverseDominators[ifStart.nextBlockFalse]);

            // find the closest one of them
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

        /*public class ExpressionCollapsedCondition : Expression
        {
            public Expression left;
            public string op;
            public Expression right;

            public ExpressionCollapsedCondition(Expression left, string op, Expression right)
            {
                this.left = left;
                this.op = op;
                this.right = right;
            }

            public override string ToString()
            {
                return "(" + left.ToString() + " " + op + right.ToString() + ")";
            }
        }

        private static Block HLCollapseMultiIf(Block entryBlock, Expression expr)
        {
            bool? ifTrueThen = null;
            bool? ifFalseThen = null;
            if (!entryBlock.nextBlockTrue.conditionalExit && entryBlock.nextBlockTrue.Statements.Count == 1)
            {
                ifTrueThen = ((short)((entryBlock.nextBlockTrue.Statements[0] as TempVarAssigmentStatement)?.Value as ExpressionConstant).Value) != 0;
            }
            if (!entryBlock.nextBlockFalse.conditionalExit && entryBlock.nextBlockFalse.Statements.Count == 1)
            {
                ifFalseThen = ((short)((entryBlock.nextBlockFalse.Statements[0] as TempVarAssigmentStatement)?.Value as ExpressionConstant).Value) != 0;
            }
        }*/

        private static BlockHLStatement HLDecompileBlocks(ref Block block, Dictionary<uint, Block> blocks, Dictionary<Block, List<Block>> loops, Dictionary<Block, List<Block>> reverseDominators, Block currentLoop = null, bool decompileTheLoop = false, Block stopAt = null)
        {
            BlockHLStatement output = new BlockHLStatement();
            while(block != stopAt && block != null)
            {
                if (loops.ContainsKey(block) && !decompileTheLoop)
                {
                    if (block != currentLoop)
                    {
                        output.Statements.Add(new LoopHLStatement() { Block = HLDecompileBlocks(ref block, blocks, loops, reverseDominators, block, true, null) });
                        continue;
                    }
                    else
                    {
                        // this is a continue statement
                        output.Statements.Add(new ContinueHLStatement());
                        break;
                    }
                }
                else if (currentLoop != null && !loops[currentLoop].Contains(block))
                {
                    // this is a break statement
                    output.Statements.Add(new BreakHLStatement());
                    break;
                }

                //output.Statements.Add(new CommentStatement("At block " + block.Address));
                foreach (var stmt in block.Statements)
                    if (!(stmt is PushEnvStatement) && !(stmt is PopEnvStatement))
                        output.Statements.Add(stmt);

                if (block.Statements.Count > 0 && block.Statements.Last() is PushEnvStatement)
                {
                    Debug.Assert(!block.conditionalExit);
                    PushEnvStatement stmt = (block.Statements.Last() as PushEnvStatement);
                    block = block.nextBlockTrue;
                    output.Statements.Add(new WithHLStatement()
                    {
                        NewEnv = stmt.NewEnv,
                        Block = HLDecompileBlocks(ref block, blocks, loops, reverseDominators, currentLoop, false, stopAt)
                    });
                    if (block == null)
                        break;
                }
                else if (block.Statements.Count > 0 && block.Statements.Last() is PopEnvStatement)
                {
                    Debug.Assert(!block.conditionalExit);
                    break;
                }
                if (block.conditionalExit)
                {
                    Block meetPoint = FindFirstMeetPoint(block, reverseDominators);
                    if (meetPoint == null)
                        throw new Exception("End of if not found");

                    IfHLStatement cond = new IfHLStatement();
                    cond.condition = block.ConditionStatement;
                    Block blTrue = block.nextBlockTrue, blFalse = block.nextBlockFalse;
                    cond.trueBlock = HLDecompileBlocks(ref blTrue, blocks, loops, reverseDominators, currentLoop, false, meetPoint);
                    cond.falseBlock = HLDecompileBlocks(ref blFalse, blocks, loops, reverseDominators, currentLoop, false, meetPoint);
                    output.Statements.Add(cond);

                    block = meetPoint;
                }
                else
                {
                    block = block.nextBlockTrue;
                }
            }
            return output;
        }

        private static List<Statement> HLDecompile(Dictionary<uint, Block> blocks, Block entryPoint, Block rootExitPoint)
        {
            Dictionary<Block, List<Block>> loops = ComputeNaturalLoops(blocks, entryPoint);
            /*foreach(var a in loops)
            {
                Debug.WriteLine("LOOP at " + a.Key.Address + " contains blocks: ");
                foreach (var b in a.Value)
                    Debug.WriteLine("* " + b.Address);
            }*/
            var reverseDominators = ComputeDominators(blocks, rootExitPoint, true);
            Block bl = entryPoint;
            return HLDecompileBlocks(ref bl, blocks, loops, reverseDominators).Statements;
        }

        public static string Decompile(UndertaleCode code)
        {
            code.UpdateAddresses();
            Dictionary<uint, Block> blocks = DecompileFlowGraph(code);

            // Throw away unreachable blocks
            // I guess this is a bug in GM:S compiler, it still generates a path to end of script after exit/return
            // and it's throwing off the loop detector for some reason
            bool changed;
            do
            {
                changed = false;
                foreach (var k in blocks.Where(pair => pair.Key != 0 && pair.Value.entryPoints.Count == 0).Select(pair => pair.Key).ToList())
                {
                    //Debug.WriteLine("Throwing away " + k);
                    foreach (var other in blocks.Values)
                        if (other.entryPoints.Contains(blocks[k]))
                            other.entryPoints.Remove(blocks[k]);
                    blocks.Remove(k);
                    changed = true;
                }
            } while (changed);

            DecompileFromBlock(blocks[0]);
            List<Statement> stmts = HLDecompile(blocks, blocks[0], blocks[code.Length / 4]);
            StringBuilder sb = new StringBuilder();
            foreach (var stmt in stmts)
                sb.Append(stmt.ToString() + "\n");
            return sb.ToString();
        }

        public static string ExportFlowGraph(Dictionary<uint, Block> blocks)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("digraph G {");
            //sb.AppendLine("    graph [splines=polyline];");
            //sb.AppendLine("");
            foreach (var block in blocks)
            {
                sb.Append("    block_" + block.Key + " [label=\"");
                foreach (var instr in block.Value.Instructions)
                    sb.Append(instr.ToString().Replace("\"", "\\\"") + "\\n");
                sb.Append("\"");
                sb.Append(block.Key == 0 ? ", color=\"blue\"" : "");
                sb.AppendLine(", shape=\"box\"];");
            }
            sb.AppendLine("");
            foreach (var block in blocks)
            {
                if (block.Value.conditionalExit)
                {
                    if (block.Value.nextBlockTrue != null)
                        sb.AppendLine("    block_" + block.Key + " -> block_" + block.Value.nextBlockTrue.Address + " [color=\"green\"];"); //, headport=n, tailport=s
                    if (block.Value.nextBlockFalse != null)
                        sb.AppendLine("    block_" + block.Key + " -> block_" + block.Value.nextBlockFalse.Address + " [color=\"red\"];"); // , headport=n, tailport=s
                }
                else
                {
                    if (block.Value.nextBlockTrue != null)
                        sb.AppendLine("    block_" + block.Key + " -> block_" + block.Value.nextBlockTrue.Address + ";"); //  [headport=n, tailport=s]
                }
                /*foreach(var rev in block.Value.entryPoints)
                {
                    if (!rev.Address.HasValue)
                        continue;
                    sb.AppendLine("    block_" + block.Key + " -> block_" + rev.Address + " [color=\"gray\", weight=0]");
                }*/
            }
            sb.AppendLine("}");
            return sb.ToString();
        }
    }
}
