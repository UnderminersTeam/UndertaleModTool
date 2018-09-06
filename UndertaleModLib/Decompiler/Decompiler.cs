using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            public Statement ConditionStatement = null;
            public bool conditionalExit;
            public Block nextBlockTrue;
            public Block nextBlockFalse;
            public List<Block> entryPoints = new List<Block>();
            /*public bool isLoop = false;
            public Block loopExitPoint = null;
            public ExitType nextBlockTrueExitType = ExitType.Jump;
            public ExitType nextBlockFalseExitType = ExitType.Jump;
            public Block nextBlockTrueLoopStart = null;
            public Block nextBlockFalseLoopStart = null;*/
            internal List<TempVarReference> TempVarsOnEntry;

            /*public enum ExitType
            {
                Jump,
                Break,
                Continue,
                Return
            }*/

            public Block(uint? address)
            {
                Address = address;
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
                return Value.ToString();
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

        public class ThrowStatement : Statement
        {
            public ushort Value;

            public ThrowStatement(ushort value)
            {
                Value = value;
            }

            public override string ToString()
            {
                return "throw " + Value;
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
            private UndertaleFunctionDeclaration Function;
            private UndertaleInstruction.DataType ReturnType;
            private List<Expression> Arguments;

            public FunctionCall(UndertaleFunctionDeclaration function, UndertaleInstruction.DataType returnType, List<Expression> args)
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
            public UndertaleInstruction.InstanceType InstType;
            public UndertaleInstruction.VariableType VarType;
            public Expression ArrayIndex;
            public Expression InstanceIndex;

            public ExpressionVar(UndertaleVariable var, UndertaleInstruction.InstanceType instType, UndertaleInstruction.VariableType varType)
            {
                Var = var;
                InstType = instType;
                VarType = varType;
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
                if (InstType != UndertaleInstruction.InstanceType.StackTopOrGlobal)
                    name = InstType.ToString().ToLower() + "." + name;
                return name;
            }

            public bool NeedsArrayParameters => VarType == UndertaleInstruction.VariableType.Array;
            public bool NeedsInstanceParameters => /*InstType == UndertaleInstruction.InstanceType.StackTopOrGlobal &&*/ VarType == UndertaleInstruction.VariableType.StackTop;
        }

        internal static void DecompileFromBlock(Block block, List<TempVarReference> tempvars)
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
                Debug.Assert(!end);
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
                    case UndertaleInstruction.Opcode.PopEnv: // TODO: seems to change the "environment", duh. After PUSHENV self.test, further references to self are actually self.test, so self.a becomes self.test.a etc.
                        if (instr.Kind == UndertaleInstruction.Opcode.PushEnv)
                        {
                            statements.Add(new CommentStatement(instr.Kind.ToString().ToUpper() + "(" + stack.Pop().ToString() + "): Not supported!"));
                        }
                        else
                        {
                            statements.Add(new CommentStatement(instr.Kind.ToString().ToUpper() + ": Not supported!"));
                        }
                        break;

                    case UndertaleInstruction.Opcode.Pop:
                        ExpressionVar target = new ExpressionVar(instr.Destination.Target, instr.TypeInst, instr.Destination.Type);
                        Expression val = null;
                        Debug.Assert((instr.DupExtra & 0xF) == 0x2 || (instr.DupExtra & 0xF) == 0x5);
                        if ((instr.DupExtra&0xF) == 0x2)
                            val = stack.Pop();
                        if (target.NeedsInstanceParameters)
                            target.InstanceIndex = stack.Pop();
                        if (target.NeedsArrayParameters)
                        {
                            target.ArrayIndex = stack.Pop();
                            target.InstType = (UndertaleInstruction.InstanceType)Convert.ToInt32((stack.Pop() as ExpressionConstant).Value); // TODO: may crash
                        }
                        if ((instr.DupExtra & 0xF) == 0x5)
                            val = stack.Pop();
                        Debug.Assert(val != null);
                        statements.Add(new AssignmentStatement(target, val));
                        break;

                    case UndertaleInstruction.Opcode.PushCst:
                    case UndertaleInstruction.Opcode.PushLoc:
                    case UndertaleInstruction.Opcode.PushGlb:
                    case UndertaleInstruction.Opcode.PushVar:
                    case UndertaleInstruction.Opcode.PushI16:
                        if (instr.Value is UndertaleInstruction.Reference<UndertaleVariable>)
                        {
                            ExpressionVar pushTarget = new ExpressionVar((instr.Value as UndertaleInstruction.Reference<UndertaleVariable>).Target, instr.TypeInst, (instr.Value as UndertaleInstruction.Reference<UndertaleVariable>).Type);
                            if (pushTarget.NeedsInstanceParameters)
                                pushTarget.InstanceIndex = stack.Pop();
                            if (pushTarget.NeedsArrayParameters)
                            {
                                pushTarget.ArrayIndex = stack.Pop();
                                pushTarget.InstType = (UndertaleInstruction.InstanceType)Convert.ToInt32((stack.Pop() as ExpressionConstant).Value); // TODO: may crash
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
                        foreach (var expr in stack.Reverse())
                            if (!(expr is ExpressionTempVar))
                                statements.Add(expr);
                        statements.Add(new ThrowStatement((ushort)instr.Value));
                        end = true;
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
            if (block.nextBlockTrue != null)
                DecompileFromBlock(block.nextBlockTrue, leftovers);
            if (block.nextBlockFalse != null)
                DecompileFromBlock(block.nextBlockFalse, leftovers);
        }

        public static void DecompileFromBlock(Block block)
        {
            DecompileFromBlock(block, new List<TempVarReference>());
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
                    // TODO: check unreachable code
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
                    nextBlock.entryPoints.Add(currentBlock);
                    currentBlock = null;
                }
                else if (instr.Kind == UndertaleInstruction.Opcode.Bt || instr.Kind == UndertaleInstruction.Opcode.Bf)
                {
                    Block nextBlockIfMet = GetBlock((uint)(instr.Address + instr.JumpOffset));
                    Block nextBlockIfNotMet = GetBlock(instr.Address + 1);
                    currentBlock.conditionalExit = true;
                    currentBlock.nextBlockTrue = instr.Kind == UndertaleInstruction.Opcode.Bt ? nextBlockIfMet : nextBlockIfNotMet;
                    currentBlock.nextBlockFalse = instr.Kind == UndertaleInstruction.Opcode.Bt ? nextBlockIfNotMet : nextBlockIfMet;
                    nextBlockIfMet.entryPoints.Add(currentBlock);
                    nextBlockIfNotMet.entryPoints.Add(currentBlock);
                    currentBlock = null;
                }
                else if (instr.Kind == UndertaleInstruction.Opcode.Ret || instr.Kind == UndertaleInstruction.Opcode.Exit || instr.Kind == UndertaleInstruction.Opcode.Break)
                {
                    currentBlock.nextBlockTrue = finalBlock;
                    currentBlock.nextBlockFalse = finalBlock;
                    finalBlock.entryPoints.Add(currentBlock);
                    currentBlock = null;
                }
            }
            if (currentBlock != null)
            {
                currentBlock.nextBlockTrue = finalBlock;
                currentBlock.nextBlockFalse = finalBlock;
                finalBlock.entryPoints.Add(currentBlock);
            }
            return blockByAddress;
        }

        /**
         * According to https://pcy.ulyssis.be/undertale/decompilation-corrected "there are no improper loops, only breaks and continues"
         * so I'll assume that because it makes things easier
         */

        /*private static bool DetectLoop(Block entryPoint, Block current, List<Block> visited, List<Block> blocksInLoop)
        {
            if (current == entryPoint)
                return true;
            visited.Add(current);

            if (current.nextBlockTrue != null && !visited.Contains(current.nextBlockTrue) && DetectLoop(entryPoint, current.nextBlockTrue, visited, blocksInLoop))
            {
                blocksInLoop.Add(current);
                return true;
            }
            if (current.nextBlockFalse != null && !visited.Contains(current.nextBlockFalse) && DetectLoop(entryPoint, current.nextBlockFalse, visited, blocksInLoop))
            {
                blocksInLoop.Add(current);
                return true;
            }
            
            return false;
        }

        public static void DetectLoop(Block entryPoint)
        {
            List<Block> blocksInLoop = new List<Block>();
            if (DetectLoop(entryPoint, entryPoint, new List<Block>(), blocksInLoop))
            {
                entryPoint.isLoop = true;
                Block exitPoint = null;
                foreach (Block block in blocksInLoop)
                {
                    // TODO: could be a return as well...
                    if (!blocksInLoop.Contains(block.nextBlockTrue))
                    {
                        if (exitPoint != null && block.nextBlockTrue != exitPoint)
                            throw new Exception("But no improper loops came... or they did?");
                        exitPoint = block.nextBlockTrue;
                        block.nextBlockTrueExitType = Block.ExitType.Break;
                        block.nextBlockTrueLoopStart = entryPoint;
                    }
                    if (!blocksInLoop.Contains(block.nextBlockFalse))
                    {
                        if (exitPoint != null && block.nextBlockFalse != exitPoint)
                            throw new Exception("But no improper loops came... or they did?");
                        exitPoint = block.nextBlockFalse;
                        block.nextBlockFalseExitType = Block.ExitType.Break;
                        block.nextBlockFalseLoopStart = entryPoint;
                    }
                    if (block.nextBlockTrue == entryPoint)
                    {
                        block.nextBlockTrueExitType = Block.ExitType.Continue;
                        block.nextBlockTrueLoopStart = entryPoint;
                    }
                    if (block.nextBlockFalse == entryPoint)
                    {
                        block.nextBlockFalseExitType = Block.ExitType.Continue;
                        block.nextBlockFalseLoopStart = entryPoint;
                    }
                }

                entryPoint.loopExitPoint = exitPoint ?? throw new Exception("Infinite loop detected!");
            }
        }*/

        // Finding ifs

        /*private static bool FindPathFromRoot(Block root, List<Block> path, Block to)
        {
            if (path.Contains(root))
            {
                // loop
                return false;
            }

            path.Add(root);

            if (root == to)
                return true;

            foreach(Block b in root.entryPoints)
            {
                if (FindPathFromRoot(b, path, to))
                    return true;
            }

            path.Remove(root);
            return false;
        }*/

        private static List<Block> FindPathFromRoot(Block from, Block to, Dictionary<uint, Block>.ValueCollection vertices)
        {
            var previous = new Dictionary<Block, Block>();
            var distances = new Dictionary<Block, int>();
            var nodes = new List<Block>();

            foreach (var vertex in vertices)
            {
                if (vertex == from)
                {
                    distances[vertex] = 0;
                }
                else
                {
                    distances[vertex] = int.MaxValue;
                }
                nodes.Add(vertex);
            }
            nodes.Add(vertices.First().entryPoints[0]); // TODO: ugh, big UGH
            distances[vertices.First().entryPoints[0]] = int.MaxValue;

            while (nodes.Count != 0)
            {
                nodes.Sort((x, y) => distances[x] - distances[y]);

                var smallest = nodes[0];
                nodes.Remove(smallest);

                if (smallest == to)
                {
                    List<Block> path = new List<Block>();
                    while (previous.ContainsKey(smallest))
                    {
                        path.Add(smallest);
                        smallest = previous[smallest];
                    }
                    path.Add(from);

                    path.Reverse();
                    return path;
                }

                if (distances[smallest] == int.MaxValue)
                {
                    break;
                }

                foreach (var neighbor in smallest.entryPoints)
                {
                    var alt = distances[smallest] + 1;
                    if (alt < distances[neighbor])
                    {
                        distances[neighbor] = alt;
                        previous[neighbor] = smallest;
                    }
                }
            }
            return null;
        }

        private static Block FindLowestCommonAncestor(Dictionary<uint, Block>.ValueCollection blocks, Block root, Block b1, Block b2)
        {
            /*List<Block> path1 = new List<Block>();
            List<Block> path2 = new List<Block>();
            if (!FindPathFromRoot(root, path1, b1) || !FindPathFromRoot(root, path2, b2))
                throw new Exception("No paths?");*/
            var path1 = FindPathFromRoot(root, b1, blocks);
            var path2 = FindPathFromRoot(root, b2, blocks);
            if (path1 == null || path2 == null)
                throw new Exception("No paths?");
            int i;
            for (i = 0; i < path1.Count && i < path2.Count; i++)
                if (path1[i] != path2[i])
                    break;
            return path1[i - 1];
        }

        public static Block FindFirstMeetPoint(Block entryPoint, Block rootExitPoint, Dictionary<uint, Block>.ValueCollection blocks)
        {
            return FindLowestCommonAncestor(blocks, rootExitPoint, entryPoint.nextBlockTrue, entryPoint.nextBlockFalse);
        }

        private static void Printer(Block block, string indent = "", List<Block> processed = null)
        {
            if (processed == null)
                processed = new List<Block>();

            if (processed.Contains(block))
            {
                Debug.WriteLine(indent + "* (repeat)");
                return;
            }
            foreach (var instr in block.Instructions)
                Debug.WriteLine(indent + "* " + instr.ToString());
            processed.Add(block);
            if (block.conditionalExit)
            {
                if (block.nextBlockTrue != null)
                {
                    Debug.WriteLine(indent + "-> IfTrue");
                    Printer(block.nextBlockTrue, indent + " ", processed);
                }
                if (block.nextBlockFalse != null)
                {
                    Debug.WriteLine(indent + "-> IfFalse");
                    Printer(block.nextBlockFalse, indent + " ", processed);
                }
            }
            else
            {
                if (block.nextBlockTrue != null)
                {
                    Debug.WriteLine(indent + "-> Jump");
                    Printer(block.nextBlockTrue, indent + " ", processed);
                }
            }
        }

        private static void PrinterV2(StringBuilder sb, Block block, Dictionary<uint, Block> blocks, Block rootExitPoint, string indent = "", Block stopAt = null, int level = 0)
        {
            //sb.AppendLine(indent + "// Block " + block.Address);
            if (level > 300)
            {
                throw new Exception("Stack overflow?");
            }
            if (block == stopAt && stopAt != null)
                return;
            foreach (var instr in block.Statements)
                sb.AppendLine(indent + instr.ToString());
            if (block.conditionalExit)
            {
                Debug.Assert(block.nextBlockTrue != null && block.nextBlockFalse != null);

                Block meetPoint = FindFirstMeetPoint(block, rootExitPoint, blocks.Values);
                Debug.Assert(meetPoint != null);
                //sb.AppendLine(indent + "// Meet point from " + block.Address + " is at " + meetPoint.Address);

                sb.AppendLine(indent + "if " + block.ConditionStatement.ToString());
                sb.AppendLine(indent + "{");
                if (block.nextBlockTrue != meetPoint)
                    PrinterV2(sb, block.nextBlockTrue, blocks, rootExitPoint, indent + "    ", meetPoint, level + 1);
                sb.AppendLine(indent + "}");
                if (block.nextBlockFalse != meetPoint)
                {
                    sb.AppendLine(indent + "else");
                    sb.AppendLine(indent + "{");
                    PrinterV2(sb, block.nextBlockFalse, blocks, rootExitPoint, indent + "    ", meetPoint, level + 1);
                    sb.AppendLine(indent + "}");
                }
                if (meetPoint != stopAt)
                    PrinterV2(sb, meetPoint, blocks, rootExitPoint, indent, stopAt, level + 1);
            }
            else
            {
                if (block.nextBlockTrue != null && block.nextBlockTrue != stopAt && block.nextBlockTrue != rootExitPoint)
                {
                    sb.AppendLine(indent + "// jump to " + block.nextBlockTrue.Address);
                    PrinterV2(sb, block.nextBlockTrue, blocks, rootExitPoint, indent, stopAt, level + 1);
                }
            }
        }

        public static string Decompile(UndertaleCode code)
        {
            Dictionary<uint, Block> blocks = DecompileFlowGraph(code);
            DecompileFromBlock(blocks[0]);
            StringBuilder sb = new StringBuilder();
            PrinterV2(sb, blocks[0], blocks, blocks[code.Length / 4]);
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
            }
            sb.AppendLine("}");
            return sb.ToString();
        }
    }
}
