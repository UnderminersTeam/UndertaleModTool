using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using UndertaleModLib.Models;
using UndertaleModLib.Util;
using static UndertaleModLib.Decompiler.Decompiler;

namespace UndertaleModLib.Decompiler
{
    public class DecompileContext
    {
        public UndertaleData Data;
        public UndertaleCode TargetCode;

        // Settings
        public bool EnableStringLabels;

        // Decompilation instance data
        public Stack<Stack<DecompileTreeNode>> Stacks = new Stack<Stack<DecompileTreeNode>>();
        public HashSet<string> LocalVarDefines = new HashSet<string>();
        public Dictionary<UndertaleVariable, AssetIDType> assetTypes = new Dictionary<UndertaleVariable, AssetIDType>();
        public Dictionary<string, AssetIDType[]> scriptArgs = new Dictionary<string, AssetIDType[]>();

        public bool isGameMaker2 { get => Data != null && Data.IsGameMaker2(); }

        public DecompileContext(UndertaleData data, bool enableStringLabels)
        {
            this.Data = data;
            this.EnableStringLabels = enableStringLabels;
        }

        public void ClearScriptArgs()
        {
            // This will not be done automatically, because it would cause significant slowdown having to recalculate this each time, and there's no reason to reset it if it's decompiling a bunch at once.
            // But, since it is possible to invalidate this data, we add this here so we'll be able to invalidate it if we need to.
            scriptArgs.Clear();
        }

        public void Setup(UndertaleCode code)
        {
            TargetCode = code;
            assetTypes.Clear();
            LocalVarDefines.Clear();
            Stacks.Clear();
        }
    }

    public static class Decompiler
    {

        // Helper function to convert opcode operations to "printable" strings.
        public static string OperationToPrintableString(UndertaleInstruction.Opcode op)
        {
            switch (op)
            {
                case UndertaleInstruction.Opcode.Mul:
                    return "*";
                case UndertaleInstruction.Opcode.Div:
                    return "/";
                case UndertaleInstruction.Opcode.Rem:
                    return "div";
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

        // Helper function to convert opcode comparisons to "printable" strings.
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

        // Represents a block node of instructions from GML bytecode (for control flow).
        public class Block
        {
            public uint Address;
            public List<UndertaleInstruction> Instructions = new List<UndertaleInstruction>();

            public Block(uint address)
            {
                Address = address;
            }

            public override string ToString()
            {
                return "Block " + Address;
            }
        }

        private static BlockTreeNode CreateNode(DecompileTree dTree, AssemblyTreeNode treeNode, AssemblyTreeNode stopAt)
        {
            if (treeNode == null)
                return null; // If we're to create a node for a null assembly tree node, that means the block tree node is also null.

            if (dTree.ReadNodes.ContainsKey(treeNode))
                return dTree.ReadNodes[treeNode];

            if (treeNode == stopAt)
                return null; // We've reached our stopping point. TODO: Also do this if a block has already been read.

            //TODO: Go over nodes in an order that will let us
            // Create a stack of stacks, cloning the most recent stack every CreateNode, and popping when the CreateNode finishes.

            DecompileContext context = dTree.Context;

            Stack<Stack<DecompileTreeNode>> allStacks = context.Stacks;
            if (allStacks.Count == 0)
                allStacks.Push(new Stack<DecompileTreeNode>());

            Stack<DecompileTreeNode> stack = new Stack<DecompileTreeNode>(allStacks.Peek());
            allStacks.Push(stack);

            Block block = treeNode.Block;

            // Iterate through all of the instructions.
            bool Finished = false;
            BlockTreeNode newNode = new BlockTreeNode();
            dTree.ReadNodes[treeNode] = newNode;
            for (int i = 0; i < block.Instructions.Count; i++)
            {
                Console.WriteLine(block.Instructions[i] + " -> " + i + ", " + stack.Count);
                var instr = block.Instructions[i];
                switch (instr.Kind)
                {
                    case UndertaleInstruction.Opcode.Neg:
                    case UndertaleInstruction.Opcode.Not:
                        stack.Push(new UnaryTreeNode(instr.Kind, instr.Type1, stack.Pop()));
                        break;

                    case UndertaleInstruction.Opcode.Dup:
                        List<DecompileTreeNode> nodesToAdd = new List<DecompileTreeNode>();
                        // This "count" is necessary because sometimes dup.i 1 is replaced with dup.l 0...
                        // Seemingly have equivalent behavior, so treat it that way.
                        int count = ((instr.DupExtra + 1) * (instr.Type1 == UndertaleInstruction.DataType.Int64 ? 2 : 1));
                        for (int j = 0; j < count; j++)
                        {
                            if ((j % 2) > 0 && stack.Count == 0)
                                continue;

                            Console.WriteLine(instr.ToString());
                            var item = stack.Pop();
                            item.WasDuplicated = true;
                            nodesToAdd.Add(item);
                        }
                        nodesToAdd.Reverse();
                        foreach (var dupNode in nodesToAdd) // First time.
                            stack.Push(dupNode.GetDuplicateNode());
                        foreach (var dupNode in nodesToAdd) // Second time.
                            stack.Push(dupNode.GetDuplicateNode());
                        break;

                    case UndertaleInstruction.Opcode.Exit:
                        newNode.Nodes.Add(new ReturnNode());
                        Finished = true;
                        break;
                    case UndertaleInstruction.Opcode.Ret:
                        //newNode.Nodes.Add(new ReturnNode(stack.Pop()));
                        newNode.Nodes.Add(new ReturnNode());
                        Finished = true;
                        break;

                    case UndertaleInstruction.Opcode.Popz: //TODO: This needs to put the popped value into the code in certain situations. (Are there any besides function calls with ignored return values?)
                        if (stack.Count == 0)
                        { // TODO: Make this an exception later.
                            newNode.Nodes.Add(new SingleLineCommentNode("WARNING: Popz'd an empty stack."));
                        } else
                        {
                            var popzNode = stack.Pop();
                            if (popzNode is FunctionCallNode)
                                newNode.Nodes.Add(popzNode);
                            else
                                newNode.Nodes.Add(new SingleLineCommentNode("popz skip: " + popzNode.ToString(context)));
                        }
                        
                        break;

                    case UndertaleInstruction.Opcode.Conv: //TODO: Maybe this would make more sense to make a cast Node?
                        var convNode = stack.Pop();
                        convNode.Type = instr.Type2;
                        stack.Push(convNode);
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
                        DecompileTreeNode a2 = stack.Pop();
                        DecompileTreeNode a1 = stack.Pop();
                        stack.Push(new BinOpTreeNode(instr.Kind, instr.Type1, a1, a2));
                        break;

                    case UndertaleInstruction.Opcode.Cmp:
                        DecompileTreeNode arg2 = stack.Pop();
                        DecompileTreeNode arg1 = stack.Pop();
                        stack.Push(new CompareTreeNode(instr.ComparisonKind, arg1, arg2));
                        break;

                    case UndertaleInstruction.Opcode.B: //TODO: Cache AssemblyTreeNodes to their BlockNodes, so if one is already in existence it won't be added.
                        Finished = true;
                        newNode.Nodes.AddRange(CreateNode(dTree, treeNode.Next, stopAt).Nodes);
                        break;

                    case UndertaleInstruction.Opcode.Bt:
                    case UndertaleInstruction.Opcode.Bf: //TODO: Make this support loops.
                        Finished = true;
                        DecompileTreeNode branchCondition = stack.Pop();

                        BlockTreeNode afterIfStatement = CreateNode(dTree, treeNode.Meetpoint, stopAt);

                        // Make IF statement. (BAD)
                        BlockTreeNode trueBlock = CreateNode(dTree, treeNode.IsConditionSwapped ? treeNode.ConditionFailNode : treeNode.Next, treeNode.Meetpoint);
                        BlockTreeNode falseBlock = CreateNode(dTree, treeNode.IsConditionSwapped ? treeNode.Next : treeNode.ConditionFailNode, treeNode.Meetpoint);
                        IfNode ifStatement = new IfNode() { Condition = branchCondition, TrueBlock = trueBlock, FalseBlock = falseBlock };


                        newNode.Nodes.Add(ifStatement);
                        if (afterIfStatement != null)
                            newNode.Nodes.AddRange(afterIfStatement.Nodes);
                        break;

                    case UndertaleInstruction.Opcode.PushEnv:
                        Finished = true;
                        WithNode withNode = new WithNode(stack.Pop(), null);

                        // First, read everything after the meetpoint.
                        BlockTreeNode afterWithBlock = CreateNode(dTree, treeNode.Meetpoint, stopAt);
                        withNode.Block = CreateNode(dTree, treeNode.Next, treeNode.Meetpoint); // create with block.
                        newNode.Nodes.Add(withNode);
                        if (afterWithBlock != null)
                            newNode.Nodes.AddRange(afterWithBlock.Nodes);
                        break;

                    //case UndertaleInstruction.Opcode.PopEnv:
                        //if (!instr.JumpOffsetPopenvExitMagic)
                        //    statements.Add(new PopEnvStatement());
                        // For JumpOffsetPopenvExitMagic:
                        //  This is just an instruction to make sure the pushenv/popenv stack is cleared on early function return
                        //  Works kinda like 'break', but doesn't have a high-level representation as it's immediately followed by a 'return'
                        //end = true;
                        //break;

                    case UndertaleInstruction.Opcode.Pop:
                        if (instr.Destination == null)
                        {
                            // pop.e.v 5/6, strange magic stack operation
                            DecompileTreeNode e1 = stack.Pop();
                            DecompileTreeNode e2 = stack.Pop();
                            for (int j = 0; j < instr.SwapExtra - 4; j++)
                                stack.Pop();
                            stack.Push(e2);
                            stack.Push(e1);
                            break;
                        }
                        VariableReferenceNode target = new VariableReferenceNode(instr.Destination.Target, new ConstantNode(UndertaleInstruction.DataType.Int16, instr.TypeInst), instr.Destination.Type);
                        DecompileTreeNode val = null;
                        if (instr.Type1 != UndertaleInstruction.DataType.Int32 && instr.Type1 != UndertaleInstruction.DataType.Variable)
                            throw new Exception("Unrecognized pop instruction, doesn't conform to pop.i.X, pop.v.X, or pop.e.v");
                        if (instr.Type1 == UndertaleInstruction.DataType.Int32)
                            val = stack.Pop();
                        if (target.NeedsInstanceParameters)
                            target.VariableOwner = stack.Pop();
                        if (target.NeedsArrayParameters)
                        {
                            Tuple<DecompileTreeNode, DecompileTreeNode> ind = VariableReferenceNode.Decompile2DArrayIndex(stack.Pop());
                            target.ArrayIndex1 = ind.Item1;
                            target.ArrayIndex2 = ind.Item2;
                            target.VariableOwner = stack.Pop();
                        }
                        if (instr.Type1 == UndertaleInstruction.DataType.Variable)
                            val = stack.Pop();
                        if (val != null)
                        {
                            if ((target.NeedsInstanceParameters || target.NeedsArrayParameters) && target.VariableOwner.WasDuplicated)
                            {
                                // Almost safe to assume that this is a +=, -=, etc.
                                // Need to confirm a few things first. It's not certain, could be ++ even.
                                if (val is BinOpTreeNode)
                                {
                                    var two = (val as BinOpTreeNode);
                                    if (two.Opcode != UndertaleInstruction.Opcode.Rem && // Not possible in GML, but possible in bytecode. Don't deal with these,
                                        two.Opcode != UndertaleInstruction.Opcode.Shl && // frankly we don't care enough.
                                        two.Opcode != UndertaleInstruction.Opcode.Shr)
                                    {
                                        var arg = two.Argument1;
                                        if (arg is VariableReferenceNode)
                                        {
                                            var v = arg as VariableReferenceNode;
                                            if (v.Variable == target.Variable && v.VariableOwner == target.VariableOwner &&
                                                v.ArrayIndex1 == target.ArrayIndex1 && v.ArrayIndex2 == target.ArrayIndex2 && // even if null
                                                (!(two.Argument2 is ConstantNode) || // Also check to make sure it's not a ++ or --
                                                (!((two.Argument2 as ConstantNode).IsPushE && ConstantNode.ConvertToInt((two.Argument2 as ConstantNode).Value) == 1))))
                                            {
                                                newNode.Nodes.Add(new AssignmentStatementNode(target, two.Argument2, two.Opcode));
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                            Debug.Fail("Pop value is null.");
                        newNode.Nodes.Add(new AssignmentStatementNode(target, val, null));
                        break;

                    case UndertaleInstruction.Opcode.Push:
                    case UndertaleInstruction.Opcode.PushLoc:
                    case UndertaleInstruction.Opcode.PushGlb:
                    case UndertaleInstruction.Opcode.PushBltn:
                    case UndertaleInstruction.Opcode.PushI:
                        if (instr.Value is UndertaleInstruction.Reference<UndertaleVariable>)
                        {
                            VariableReferenceNode pushTarget = new VariableReferenceNode((instr.Value as UndertaleInstruction.Reference<UndertaleVariable>).Target, new ConstantNode(UndertaleInstruction.DataType.Int16, instr.TypeInst), (instr.Value as UndertaleInstruction.Reference<UndertaleVariable>).Type);
                            if (pushTarget.NeedsInstanceParameters)
                                pushTarget.VariableOwner = stack.Pop();
                            if (pushTarget.NeedsArrayParameters)
                            {
                                Tuple<DecompileTreeNode, DecompileTreeNode> ind = VariableReferenceNode.Decompile2DArrayIndex(stack.Pop());
                                pushTarget.ArrayIndex1 = ind.Item1;
                                pushTarget.ArrayIndex2 = ind.Item2;
                                pushTarget.VariableOwner = stack.Pop();
                            }
                            stack.Push(pushTarget);
                        }
                        else
                        {
                            bool isPushE = (instr.Kind == UndertaleInstruction.Opcode.Push && instr.Type1 == UndertaleInstruction.DataType.Int16);
                            ConstantNode pushTarget = new ConstantNode(instr.Type1, instr.Value, isPushE);
                            if (isPushE && pushTarget.Type == UndertaleInstruction.DataType.Int16 && Convert.ToInt32((pushTarget as ConstantNode).Value) == 1)
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
                                    stack.Push(new IncrementNode(block.Instructions[i + 1].Kind, stack.Pop(), false));

                                    while (i < block.Instructions.Count && (block.Instructions[i].Kind != UndertaleInstruction.Opcode.Pop || (block.Instructions[i].Type1 == UndertaleInstruction.DataType.Int16 && block.Instructions[i].Type2 == UndertaleInstruction.DataType.Variable)))
                                        i++;
                                }
                                else if (i + 2 < block.Instructions.Count && (block.Instructions[i + 1].Kind == UndertaleInstruction.Opcode.Add || block.Instructions[i + 1].Kind == UndertaleInstruction.Opcode.Sub) &&
                                        block.Instructions[i + 2].Kind == UndertaleInstruction.Opcode.Dup && block.Instructions[i + 2].Type1 == UndertaleInstruction.DataType.Variable)
                                {
                                    // We've detected a pre increment/decrement (i.e., x = ++y)
                                    // Do the magic
                                    stack.Push(new IncrementNode(block.Instructions[i + 1].Kind, stack.Pop(), true));

                                    while (i < block.Instructions.Count && block.Instructions[i].Kind != UndertaleInstruction.Opcode.Pop)
                                        i++;
                                    var _inst = block.Instructions[i];
                                    if (_inst.Type1 == UndertaleInstruction.DataType.Int16 && _inst.Type2 == UndertaleInstruction.DataType.Variable)
                                    {
                                        DecompileTreeNode e = stack.Pop();
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
                        List<DecompileTreeNode> args = new List<DecompileTreeNode>();
                        for (int j = 0; j < instr.ArgumentsCount; j++)
                            args.Add(stack.Pop());
                        stack.Push(new FunctionCallNode(instr.Function.Target, instr.Type1, args));
                        break;

                    case UndertaleInstruction.Opcode.Break:
                        // This is used for checking bounds in 2D arrays
                        // I'm not sure of the specifics but I guess it causes a debug breakpoint if the top of the stack is >= 32000
                        // anyway, that's not important when decompiling to high-level code so just ignore it
                        newNode.Nodes.Add(new SingleLineCommentNode("If you see this message, it means the special break instruction was used. Please report this to a UTMT developer, along with the game/script this appears in.")); // If this gets reported, note the game + version + script, and then handle this appropriately.
                        break;
                    default:
                        newNode.Nodes.Add(new SingleLineCommentNode(instr.ToString()));
                        break;
                }

                if (Finished)
                    break;
            }

            allStacks.Pop();
            return newNode;
        }

        public static string Decompile(UndertaleCode code, DecompileContext context)
        {
            context.Setup(code);
            //TODO: Look into getting rid of the parent in CleanStatement

            AssemblyTree assemblyTree = AssemblyTree.CreateTree(context); //TODO: The DecompileTree should probably manage the AssemlyTree creation.
            DecompileTree decompileTree = new DecompileTree() { Context = context, AssemblyTree = assemblyTree };
            decompileTree.Root = CreateNode(decompileTree, assemblyTree.Root, null);
            // TODO: After building the tree, combine blocks which overlap.
            // TODO: Type Propogation.
            // TODO: Cleanup.

            return decompileTree.ToString(true); //TODO: Return DecompileTree instead.
        }

        public class DecompileTree
        {
            public DecompileContext Context;
            public AssemblyTree AssemblyTree;
            public BlockTreeNode Root = new BlockTreeNode();
            public Dictionary<AssemblyTreeNode, BlockTreeNode> ReadNodes = new Dictionary<AssemblyTreeNode, BlockTreeNode>();

            public override string ToString()
            {
                return ToString(false);
            }

            public string ToString(bool includeLocalVars)
            {
                string codeStr = Root.ToString(Context);
                if (includeLocalVars)
                    codeStr = DeclareLocalVars() + "\n" + codeStr;

                return codeStr;
            }

            public string DeclareLocalVars()
            {
                // Mark local variables as local.
                UndertaleCode code = Context.TargetCode;
                StringBuilder tempBuilder = new StringBuilder();
                UndertaleCodeLocals locals = Context.Data != null ? Context.Data.CodeLocals.For(code) : null;

                HashSet<string> possibleVars = new HashSet<string>();
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
                            if (name != null)
                                possibleVars.Add(name);
                        }
                        else if (inst.Kind == UndertaleInstruction.Opcode.Pop && inst.TypeInst == UndertaleInstruction.InstanceType.Local)
                        {
                            string name = inst.Destination.Target?.Name?.Content;
                            if (name != null)
                                possibleVars.Add(name);
                        }
                    }
                }

                foreach (var possibleName in possibleVars)
                {
                    if (possibleName == "arguments" || possibleName == "$$$$temp$$$$" || Context.LocalVarDefines.Contains(possibleName))
                        continue;

                    if (tempBuilder.Length > 0)
                        tempBuilder.Append(", ");

                    tempBuilder.Append(possibleName);
                }

                return (tempBuilder.Length > 0) ? "var " + tempBuilder.ToString() + ";" : "";
            }
        }

        public abstract class DecompileTreeNode
        {
            public UndertaleInstruction.DataType Type;
            public bool WasDuplicated = false;

            // Used by the DUP instruction.
            public virtual DecompileTreeNode GetDuplicateNode()
            {
                return this;
            }

            public abstract string ToString(DecompileContext context);
            internal abstract AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType);

            public abstract DecompileTreeNode CleanStatement(DecompileContext context, DecompileTreeNode parent);
        }

        public class SingleLineCommentNode : DecompileTreeNode
        {
            public String Comment;

            public SingleLineCommentNode(string comment)
            {
                Comment = comment;
            }

            public override DecompileTreeNode CleanStatement(DecompileContext context, DecompileTreeNode parent)
            {
                return this;
            }

            public override string ToString(DecompileContext context)
            {
                return "// " + Comment.Replace("\n", "\\n");
            }

            internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
            {
                return suggestedType;
            }
        }

        public class DummyTreeNode : DecompileTreeNode
        {
            public String Data;

            public DummyTreeNode(string data)
            {
                Data = data;
            }

            public override DecompileTreeNode CleanStatement(DecompileContext context, DecompileTreeNode parent)
            {
                return this;
            }

            public override string ToString(DecompileContext context)
            {
                return Data;
            }

            internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
            {
                return suggestedType;
            }
        }

        public class BlockTreeNode : DecompileTreeNode
        {
            public List<DecompileTreeNode> Nodes = new List<DecompileTreeNode>();
            //TODO: Need to have behavior to move on.

            public override string ToString(DecompileContext context)
            {
                return ToString(context, false);
            }

            public string ToString(DecompileContext context, bool includeBrackets)
            {
                StringBuilder sb = new StringBuilder();
                if (includeBrackets)
                    sb.Append("{\n");
                foreach (var node in Nodes)
                {
                    string resultStr = node.ToString(context);
                    if (includeBrackets)
                    {
                        sb.Append("    ");
                        resultStr = resultStr.Replace("\n", "\n    ");
                    }
                    sb.Append(resultStr).Append("\n");
                }
                if (includeBrackets)
                    sb.Append("}");
                return sb.ToString().Trim('\n');
            }

            public override DecompileTreeNode CleanStatement(DecompileContext context, DecompileTreeNode parent)
            {
                for (var i = 0; i < Nodes.Count; i++)
                {
                    var count = Nodes.Count;
                    var Result = Nodes[i]?.CleanStatement(context, this); // Yes, this uses "this" and not "block".
                    i -= (count - Nodes.Count); // If removed.
                    Nodes[i] = Result;
                }
                return this;
            }

            internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
            {
                foreach (var node in Nodes)
                    node.DoTypePropagation(context, suggestedType);
                return suggestedType;
            }
        }
    }

    public class ReturnNode : DecompileTreeNode
    {
        public DecompileTreeNode ReturnValue;

        public ReturnNode(DecompileTreeNode value)
        {
            ReturnValue = value;
        }

        public ReturnNode()
        {

        }

        public override DecompileTreeNode CleanStatement(DecompileContext context, DecompileTreeNode parent)
        {
            ReturnValue = ReturnValue?.CleanStatement(context, parent);
            return this;
        }

        public override string ToString(DecompileContext context)
        {
            if (ReturnValue != null)
                return "return " + ReturnValue.ToString(context) + ";";

            return (context.isGameMaker2 ? "return;" : "exit;");
        }

        internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
        {
            return ReturnValue?.DoTypePropagation(context, suggestedType) ?? suggestedType;
        }
    }

    public class UnaryTreeNode : DecompileTreeNode
    {
        public UndertaleInstruction.Opcode Opcode;
        public DecompileTreeNode Value;

        public UnaryTreeNode(UndertaleInstruction.Opcode opcode, UndertaleInstruction.DataType targetType, DecompileTreeNode argument)
        {
            Opcode = opcode;
            Type = targetType;
            Value = argument;
        }

        public override DecompileTreeNode CleanStatement(DecompileContext context, DecompileTreeNode parent)
        {
            Value = Value?.CleanStatement(context, parent);
            return this;
        }

        public override string ToString(DecompileContext context)
        {
            string symbol = (Opcode == UndertaleInstruction.Opcode.Not && Type == UndertaleInstruction.DataType.Boolean ? "!" : OperationToPrintableString(Opcode));
            return symbol + Value.ToString(context);
        }

        internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
        {
            return Value.DoTypePropagation(context, suggestedType);
        }
    }

    // Binary Operation Tree Node.
    public class BinOpTreeNode : DecompileTreeNode
    {
        public UndertaleInstruction.Opcode Opcode;
        public DecompileTreeNode Argument1;
        public DecompileTreeNode Argument2;
        public string OverrideSymbol;

        public BinOpTreeNode(UndertaleInstruction.Opcode opcode, UndertaleInstruction.DataType targetType, DecompileTreeNode argument1, DecompileTreeNode argument2)
        {
            this.Opcode = opcode;
            this.Type = targetType;
            this.Argument1 = argument1;
            this.Argument2 = argument2;
        }

        public override DecompileTreeNode CleanStatement(DecompileContext context, DecompileTreeNode parent)
        {
            Argument1 = Argument1?.CleanStatement(context, parent);
            Argument2 = Argument2?.CleanStatement(context, parent);
            return this;
        }

        public override string ToString(DecompileContext context)
        {
            return "(" + Argument1.ToString(context) + " " + (OverrideSymbol ?? Decompiler.OperationToPrintableString(Opcode)) + " " + Argument2.ToString(context) + ")";
        }

        internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
        {
            AssetIDType t = Argument1.DoTypePropagation(context, suggestedType);
            Argument2.DoTypePropagation(context, AssetIDType.Other); // Most likely it will be var + constant, so we incorrectly assume it is always like this. I'm open to better methods if anyone has one.
            return t;
        }
    }

    public class CompareTreeNode : DecompileTreeNode
    {
        public UndertaleInstruction.ComparisonType Opcode;
        public DecompileTreeNode Argument1;
        public DecompileTreeNode Argument2;

        public CompareTreeNode(UndertaleInstruction.ComparisonType opcode, DecompileTreeNode argument1, DecompileTreeNode argument2)
        {
            this.Opcode = opcode;
            this.Type = UndertaleInstruction.DataType.Boolean;
            this.Argument1 = argument1;
            this.Argument2 = argument2;
        }

        public override DecompileTreeNode CleanStatement(DecompileContext context, DecompileTreeNode parent)
        {
            Argument1 = Argument1?.CleanStatement(context, parent);
            Argument2 = Argument2?.CleanStatement(context, parent);
            return this;
        }

        public override string ToString(DecompileContext context)
        {
            return "(" + Argument1.ToString(context) + " " + OperationToPrintableString(Opcode) + " " + Argument2.ToString(context) + ")";
        }

        internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
        {
            // TODO: This should be probably able to go both ways...
            Argument2.DoTypePropagation(context, Argument1.DoTypePropagation(context, suggestedType));
            return AssetIDType.Other;
        }
    }

    public class FunctionCallNode : DecompileTreeNode
    {
        public UndertaleFunction Function;
        public UndertaleInstruction.DataType ReturnType;
        public List<DecompileTreeNode> Arguments;

        public FunctionCallNode(UndertaleFunction function, UndertaleInstruction.DataType returnType, List<DecompileTreeNode> args)
        {
            this.Function = function;
            this.ReturnType = returnType;
            this.Arguments = args;
        }

        public override DecompileTreeNode CleanStatement(DecompileContext context, DecompileTreeNode parent)
        {
            for (var i = 0; i < Arguments.Count; i++)
                Arguments[i] = Arguments[i]?.CleanStatement(context, parent);
            return this;
        }

        public override string ToString(DecompileContext context)
        {
            StringBuilder argumentString = new StringBuilder();
            foreach (DecompileTreeNode arg in Arguments)
            {
                if (argumentString.Length > 0)
                    argumentString.Append(", ");
                argumentString.Append(arg.ToString(context));
            }

            if (Function.Name.Content == "@@NewGMLArray@@" && context.isGameMaker2) // Special case in GMS2 for creating arrays.
                return "[" + argumentString.ToString() + "]";

            return Function.Name.Content + "(" + argumentString.ToString() + ")";
        }

        internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
        { //TODO: ??
            /*var script_code = context.Data?.Scripts.ByName(Function.Name.Content)?.Code;
            if (script_code != null && !context.scriptArgs.ContainsKey(Function.Name.Content))
            {
                context.scriptArgs.Add(Function.Name.Content, null); // stop the recursion from looping
                var xxx = context.assetTypes;
                context.assetTypes = new Dictionary<UndertaleVariable, AssetIDType>(); // Apply a temporary dictionary which types will be applied to.
                Dictionary<uint, Block> blocks = Decompiler.PrepareDecompileFlow(script_code);
                Decompiler.DecompileFromBlock(context, blocks[0]);
                Decompiler.DoTypePropagation(context, blocks); // TODO: This should probably put suggestedType through the "return" statement at the other end
                context.scriptArgs[Function.Name.Content] = new AssetIDType[15];
                for (int i = 0; i < 15; i++)
                {
                    var v = context.assetTypes.Where((x) => x.Key.Name.Content == "argument" + i);
                    context.scriptArgs[Function.Name.Content][i] = v.Count() > 0 ? v.First().Value : AssetIDType.Other;
                }
                context.assetTypes = xxx; // restore original / proper map.
            }

            AssetIDType[] args = new AssetIDType[Arguments.Count];
            AssetTypeResolver.AnnotateTypesForFunctionCall(Function.Name.Content, args, context.scriptArgs);
            for (var i = 0; i < Arguments.Count; i++)
            {
                Arguments[i].DoTypePropagation(context, args[i]);
            }*/
            return suggestedType; // TODO: maybe we should handle returned values too?
        }
    }

    public class VariableReferenceNode : DecompileTreeNode
    {
        public UndertaleVariable Variable;
        public DecompileTreeNode VariableOwner; // Variable owner, so it could be like: obj_gun.firerange where obj_gun is the VariableOwner.
        public UndertaleInstruction.VariableType VarType;
        public DecompileTreeNode ArrayIndex1;
        public DecompileTreeNode ArrayIndex2;

        public bool NeedsArrayParameters => VarType == UndertaleInstruction.VariableType.Array;
        public bool NeedsInstanceParameters => VarType == UndertaleInstruction.VariableType.StackTop;

        public VariableReferenceNode(UndertaleVariable var, DecompileTreeNode varOwner, UndertaleInstruction.VariableType varType)
        {
            Variable = var;
            VariableOwner = varOwner;
            VarType = varType;
        }

        public override DecompileTreeNode CleanStatement(DecompileContext context, DecompileTreeNode parent)
        {
            //TODO: We need a solution for $$$$temp$$$$. It doesn't need to necessarily go here, but it would make sense.

            VariableOwner = VariableOwner?.CleanStatement(context, parent);
            ArrayIndex1 = ArrayIndex1?.CleanStatement(context, parent);
            ArrayIndex2 = ArrayIndex2?.CleanStatement(context, parent);
            return this;
        }

        public override string ToString(DecompileContext context)
        {
            string name = Variable.Name.Content;
            if (ArrayIndex1 != null && ArrayIndex2 != null)
                name = name + "[" + ArrayIndex1.ToString(context) + ", " + ArrayIndex2.ToString(context) + "]";
            else if (ArrayIndex1 != null)
                name = name + "[" + ArrayIndex1.ToString(context) + "]";

            // NOTE: The "var" prefix is handled in Decompiler.Decompile. 

            if (VariableOwner is ConstantNode) // Only use "global." and "other.", not "self." or "local.". GMS doesn't recognize those.
            {
                string holder = VariableOwner.ToString(context).ToLower();
                string prefix = (holder != "self" && holder != "local" ? holder + "." : ""); //TODO: Can we get a better handler here?

                ConstantNode constant = (ConstantNode)VariableOwner;
                if (!(constant.Value is Int64))
                {
                    int? val = ConstantNode.ConvertToInt(constant.Value);
                    if (val != null)
                    {
                        if (constant.AssetType == AssetIDType.GameObject && val < 0)
                        {
                            UndertaleInstruction.InstanceType instanceType = (UndertaleInstruction.InstanceType)val;
                            prefix = (instanceType == UndertaleInstruction.InstanceType.Global || instanceType == UndertaleInstruction.InstanceType.Other) ? prefix.ToLower() : "";
                        }
                    }
                }
                return prefix + name;
            }

            return VariableOwner.ToString(context) + "." + name;
        }

        internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
        {
            VariableOwner?.DoTypePropagation(context, AssetIDType.GameObject); // TODO: This isn't always a GameObject.
            ArrayIndex1?.DoTypePropagation(context, AssetIDType.Other);
            ArrayIndex2?.DoTypePropagation(context, AssetIDType.Other);

            AssetIDType current = context.assetTypes.ContainsKey(Variable) ? context.assetTypes[Variable] : AssetIDType.Other;
            if (current == AssetIDType.Other && suggestedType != AssetIDType.Other)
                current = suggestedType;
            AssetIDType builtinSuggest = AssetTypeResolver.AnnotateTypeForVariable(Variable.Name.Content);
            if (builtinSuggest != AssetIDType.Other)
                current = builtinSuggest;

            if (VarType != UndertaleInstruction.VariableType.Array) // Arrays can (and do in games like Undertale) have different types of assets in them, so for now we'll skip assigning types to them.
                context.assetTypes[Variable] = current;
            return current;
        }

        public static Tuple<DecompileTreeNode, DecompileTreeNode> Decompile2DArrayIndex(DecompileTreeNode index)
        {
            DecompileTreeNode ind1 = index;
            DecompileTreeNode ind2 = null;
            if (ind1 is BinOpTreeNode && (ind1 as BinOpTreeNode).Opcode == UndertaleInstruction.Opcode.Add)
            {
                var arg1 = (ind1 as BinOpTreeNode).Argument1;
                var arg2 = (ind1 as BinOpTreeNode).Argument2;
                if (arg1 is BinOpTreeNode && (arg1 as BinOpTreeNode).Opcode == UndertaleInstruction.Opcode.Mul)
                {
                    var arg11 = (arg1 as BinOpTreeNode).Argument1;
                    var arg12 = (arg1 as BinOpTreeNode).Argument2;
                    if (arg12 is ConstantNode && (arg12 as ConstantNode).Value.GetType() == typeof(int) && (int)(arg12 as ConstantNode).Value == 32000)
                    {
                        ind1 = arg11;
                        ind2 = arg2;
                    }
                }
            }
            return new Tuple<DecompileTreeNode, DecompileTreeNode>(ind1, ind2);
        }
    }

    public class WithNode : DecompileTreeNode
    {
        public DecompileTreeNode ExecutionEnvironment; // The object to execute under.
        public BlockTreeNode Block;

        public WithNode(DecompileTreeNode ExecuteUnder, BlockTreeNode block)
        {
            ExecutionEnvironment = ExecuteUnder;
            Block = block;
        }

        public override DecompileTreeNode CleanStatement(DecompileContext context, DecompileTreeNode parent)
        {
            ExecutionEnvironment = ExecutionEnvironment?.CleanStatement(context, parent);
            Block = (BlockTreeNode) Block?.CleanStatement(context, parent);
            return this;
        }

        public override string ToString(DecompileContext context)
        {
            return "with (" + ExecutionEnvironment.ToString(context) + ") " + Block.ToString(context, true);
        }

        internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
        {
            return ExecutionEnvironment.DoTypePropagation(context, AssetIDType.GameObject);
        }
    }

    public class IfNode : DecompileTreeNode
    {
        public DecompileTreeNode Condition;
        public BlockTreeNode TrueBlock;
        public BlockTreeNode FalseBlock;
        public List<ElseIfBlock> ElseIfBlocks = new List<ElseIfBlock>();
        public bool HasElseIf { get => ElseIfBlocks != null && ElseIfBlocks.Count > 0; }
        public bool HasElse { get => FalseBlock != null; }


        public override DecompileTreeNode CleanStatement(DecompileContext context, DecompileTreeNode parent)
        {
            // TODO: && + ||
            // TOOO: Ternary.
            // TODO: Repeat Loops.
            // TODO: Clean each condition, block, and else if block + conditions.
            throw new NotImplementedException();
        }

        public override string ToString(DecompileContext context)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("if " + Condition.ToString(context) + " ");
            sb.Append(TrueBlock.ToString(context, true));

            foreach (ElseIfBlock elseIf in ElseIfBlocks)
            {
                sb.Append("else if " + elseIf.Condition.ToString(context) + " ");
                sb.Append(elseIf.Block.ToString(context, true));
            }

            if (HasElse)
            {
                sb.Append("else ");
                sb.Append(FalseBlock.ToString(context, true));
            }
            return sb.ToString();
        }

        internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
        {
            // This is a statement, not an expression, so there's no handling.
            return suggestedType;
        }
    }

    //public class TernaryNode : DecompileTreeNode
    //{
    //
    //}

    public class ElseIfBlock
    {
        public DecompileTreeNode Condition;
        public BlockTreeNode Block;

        public ElseIfBlock(DecompileTreeNode condition, BlockTreeNode block)
        {
            Condition = condition;
            Block = block;
        }
    }

    public class LoopNode : DecompileTreeNode
    {
        public BlockTreeNode Block;
        public DecompileTreeNode Condition;

        //public bool IsWhileLoop { get => !IsForLoop && !IsRepeatLoop && !IsDoUntilLoop; }
        public bool IsDoUntilLoop;

        // TODO
        public override DecompileTreeNode CleanStatement(DecompileContext context, DecompileTreeNode parent)
        {
            throw new NotImplementedException();
        }

        public override string ToString(DecompileContext context)
        {
            throw new NotImplementedException();
        }

        internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
        {
            throw new NotImplementedException();
        }

        // For Loop.
        //public bool IsForLoop { get => InitializeStatement != null && StepStatement != null && Condition != null; }
        //public AssignmentStatement InitializeStatement;
        //public AssignmentStatement StepStatement;

        // Repeat loop
        //public bool IsRepeatLoop { get => RepeatStartValue != null; }
        //public Statement RepeatStartValue;
    }

    public class SwitchStatementNode : DecompileTreeNode
    {
        public DecompileTreeNode SwitchExpression;
        public List<SwitchCaseStatement> Cases;

        public override DecompileTreeNode CleanStatement(DecompileContext context, DecompileTreeNode parent)
        {
            SwitchExpression = SwitchExpression?.CleanStatement(context, parent);
            for (var i = 0; i < Cases.Count; i++)
                Cases[i] = (SwitchCaseStatement) Cases[i]?.CleanStatement(context, parent);

            return this;
        }

        public override string ToString(DecompileContext context)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("switch " + SwitchExpression.ToString(context) + "\n");
            sb.Append("{\n");
            foreach (var casee in Cases)
            {
                sb.Append("    ");
                sb.Append(casee.ToString(context).Replace("\n", "\n    "));
                sb.Append("\n");
            }
            sb.Append("}\n");
            return sb.ToString();
        }

        internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
        {
            SwitchExpression.DoTypePropagation(context, suggestedType);
            foreach (var caseStatement in Cases)
                caseStatement.DoTypePropagation(context, suggestedType);

            return suggestedType;
        }
    }

    public class SwitchCaseStatement : DecompileTreeNode
    {
        public List<DecompileTreeNode> Values;
        public BlockTreeNode Block;

        public SwitchCaseStatement(List<DecompileTreeNode> values, BlockTreeNode block)
        {
            Values = values;
            Block = block;
        }

        public override DecompileTreeNode CleanStatement(DecompileContext context, DecompileTreeNode parent)
        {
            for (var i = 0; i < Values.Count; i++)
                Values[i] = Values[i]?.CleanStatement(context, parent);
            Block = (BlockTreeNode)Block?.CleanStatement(context, null);
            return this;
        }

        public override string ToString(DecompileContext context)
        {
            StringBuilder sb = new StringBuilder();
            foreach (DecompileTreeNode caseValue in Values)
            {
                if (caseValue != null)
                    sb.Append("case " + caseValue.ToString(context) + ":\n");
                else
                    sb.Append("default:\n");
            }

            if (Block != null && Block.Nodes.Count > 0)
            {
                sb.Append("    ");
                sb.Append(Block.ToString(context, false).Replace("\n", "\n        "));
            }
            return sb.ToString();
        }

        internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
        {
            foreach (DecompileTreeNode node in Values)
                node.DoTypePropagation(context, suggestedType);
            Block.DoTypePropagation(context, suggestedType);
            return suggestedType;
        }
    }

    public class ConstantNode : DecompileTreeNode
    {
        public object Value;
        public bool IsPushE;
        internal AssetIDType AssetType = AssetIDType.Other;

        public ConstantNode(UndertaleInstruction.DataType type, object value, bool isPushE = false)
        {
            Type = type;
            Value = value;
            IsPushE = isPushE;
        }
        public bool EqualsNumber(int TestNumber)
        {
            return (Value is Int16 || Value is Int32) && Convert.ToInt32(Value) == TestNumber;
        }

        // Helper function to carefully check if an object is in fact an integer, for asset types.
        public static int? ConvertToInt(object val)
        {
            if (val is int || val is short || val is ushort || val is UndertaleInstruction.InstanceType)
            {
                return Convert.ToInt32(val);
            }
            else if (val is double)
            {
                var v = Convert.ToDouble(val);
                int res = (int)v;
                if (v == res)
                    return res;
            }
            else if (val is float)
            {
                var v = Convert.ToSingle(val);
                int res = (int)v;
                if (v == res)
                    return res;
            }
            return null;
        }

        // Helper function, using the one above, to convert an object into its respective asset type enum, if possible.
        public static string ConvertToEnumStr<T>(object val)
        {
            int? intVal = ConvertToInt(val);
            if (intVal == null)
                return val.ToString();
            return ((T)(object)intVal).ToString();
        }

        public override DecompileTreeNode CleanStatement(DecompileContext context, DecompileTreeNode parent)
        {
            Value = (Value as DecompileTreeNode)?.CleanStatement(context, parent) ?? Value;
            return this;
        }

        public override string ToString(DecompileContext context)
        {
            if (Value is UndertaleResourceById<UndertaleString, UndertaleChunkSTRG>) // Export string.
            {
                UndertaleResourceById<UndertaleString, UndertaleChunkSTRG> resource = (UndertaleResourceById<UndertaleString, UndertaleChunkSTRG>)Value;

                string resultStr = resource.Resource.ToString(context);
                if (context.EnableStringLabels)
                    resultStr += resource.GetMarkerSuffix();
                return resultStr;
            }

            if (AssetType == AssetIDType.GameObject && !(Value is Int64)) // When the value is Int64, an example value is 343434343434. It is unknown what it represents, but it's not an InstanceType.
            {
                int? val = ConvertToInt(Value);
                if (val != null && val < 0)
                    return ((UndertaleInstruction.InstanceType)Value).ToString().ToLower();
            }
            // Need to put else because otherwise it gets terribly unoptimized with GameObject type
            else if (AssetType == AssetIDType.e__VW)
                return "e__VW." + ConvertToEnumStr<e__VW>(Value);
            else if (AssetType == AssetIDType.e__BG)
                return "e__BG." + ConvertToEnumStr<e__BG>(Value);

            else if (AssetType == AssetIDType.Enum_HAlign)
                return ConvertToEnumStr<HAlign>(Value);
            else if (AssetType == AssetIDType.Enum_VAlign)
                return ConvertToEnumStr<VAlign>(Value);
            else if (AssetType == AssetIDType.Enum_OSType)
                return ConvertToEnumStr<OSType>(Value);
            else if (AssetType == AssetIDType.Enum_GamepadButton)
                return ConvertToEnumStr<GamepadButton>(Value);
            else if (AssetType == AssetIDType.Enum_PathEndAction)
                return ConvertToEnumStr<PathEndAction>(Value);
            else if (AssetType == AssetIDType.Enum_BufferKind)
                return ConvertToEnumStr<BufferKind>(Value);
            else if (AssetType == AssetIDType.Enum_BufferType)
                return ConvertToEnumStr<BufferType>(Value);
            else if (AssetType == AssetIDType.Enum_BufferSeek)
                return ConvertToEnumStr<BufferSeek>(Value);
            else if (AssetType == AssetIDType.Boolean)
                return ConvertToEnumStr<Boolean>(Value);

            else if (AssetType == AssetIDType.Color && Value is IFormattable && !(Value is float) && !(Value is double) && !(Value is decimal))
                return (context.isGameMaker2 ? "0x" : "$") + ((IFormattable)Value).ToString("X8", CultureInfo.InvariantCulture);

            else if (AssetType == AssetIDType.KeyboardKey)
            {
                int? tryVal = ConvertToInt(Value);
                if (tryVal != null)
                {
                    int val = tryVal ?? -1;

                    bool isAlphaNumeric = val >= (int)EventSubtypeKey.Digit0 && val <= (int)EventSubtypeKey.Z;
                    if (isAlphaNumeric)
                        return "ord(\"" + (char)val + "\")";

                    if (val >= 0 && Enum.IsDefined(typeof(EventSubtypeKey), (uint)val))
                        return ((EventSubtypeKey)val).ToString(); // Either return the key enum, or the right alpha-numeric key-press.

                    if (!Char.IsControl((char)val) && !Char.IsLower((char)val) && val > 0) // The special keys overlay with the uppercase letters (ugh)
                        return "ord(" + (((char)val) == '\'' ? (context.isGameMaker2 ? "\"\\\"\"" : "'\"'")
                            : (((char)val) == '\\' ? (context.isGameMaker2 ? "\"\\\\\"" : "\"\\\"")
                            : "\"" + (char)val + "\"")) + ")";
                }
            }

            if (context.Data != null && AssetType != AssetIDType.Other)
            {
                IList assetList = null;
                switch (AssetType)
                {
                    case AssetIDType.Sprite:
                        assetList = (IList)context.Data.Sprites;
                        break;
                    case AssetIDType.Background:
                        assetList = (IList)context.Data.Backgrounds;
                        break;
                    case AssetIDType.Sound:
                        assetList = (IList)context.Data.Sounds;
                        break;
                    case AssetIDType.Font:
                        assetList = (IList)context.Data.Fonts;
                        break;
                    case AssetIDType.Path:
                        assetList = (IList)context.Data.Paths;
                        break;
                    case AssetIDType.Timeline:
                        assetList = (IList)context.Data.Timelines;
                        break;
                    case AssetIDType.Room:
                        assetList = (IList)context.Data.Rooms;
                        break;
                    case AssetIDType.GameObject:
                        assetList = (IList)context.Data.GameObjects;
                        break;
                    case AssetIDType.Shader:
                        assetList = (IList)context.Data.Shaders;
                        break;
                    case AssetIDType.Script:
                        assetList = (IList)context.Data.Scripts;
                        break;
                }

                if (!(Value is Int64)) // It is unknown what Int64 data represents, but it's not this.
                {
                    int? tryVal = ConvertToInt(Value);
                    int val;
                    if (tryVal != null)
                    {
                        val = tryVal ?? -1;
                        if (assetList != null && val >= 0 && val < assetList.Count)
                            return ((UndertaleNamedResource)assetList[val]).Name.Content;
                    }
                }
            }

            if (Value is float) // Prevents scientific notation by using high bit number.
                return ((decimal)((float)Value)).ToString(CultureInfo.InvariantCulture);

            if (Value is double) // Prevents scientific notation by using high bit number.
                return ((decimal)((double)Value)).ToString(CultureInfo.InvariantCulture);

            if (Value is DecompileTreeNode)
                return ((DecompileTreeNode)Value).ToString(context);

            return ((Value as IFormattable)?.ToString(null, CultureInfo.InvariantCulture) ?? Value.ToString());
        }

        internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
        {
            if (AssetType == AssetIDType.Other)
                AssetType = suggestedType;
            return AssetType;
        }
    }

    public class AssignmentStatementNode : DecompileTreeNode
    {
        public VariableReferenceNode TargetVariable;
        public DecompileTreeNode NewValue;
        public UndertaleInstruction.Opcode? Opcode;

        public AssignmentStatementNode(VariableReferenceNode destination, DecompileTreeNode value, UndertaleInstruction.Opcode? opcode)
        {
            TargetVariable = destination;
            NewValue = value;
            Opcode = opcode;
        }

        public override DecompileTreeNode CleanStatement(DecompileContext context, DecompileTreeNode parent)
        {
            NewValue = NewValue.CleanStatement(context, parent);
            return this;
        }

        public override string ToString(DecompileContext context)
        {
            string op = Opcode != null ? OperationToPrintableString(Opcode.Value) : "";
            return TargetVariable.ToString(context) + " " + op + "= " + NewValue.ToString(context);
        }

        internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
        {
            return NewValue.DoTypePropagation(context, TargetVariable.DoTypePropagation(context, suggestedType));
        }
    }

    public class IncrementNode : DecompileTreeNode
    {
        public UndertaleInstruction.Opcode Opcode;
        public DecompileTreeNode Variable;
        public bool IsPre;
        public bool IsPost { get => !IsPre; }

        public IncrementNode(UndertaleInstruction.Opcode opcode, DecompileTreeNode variable, bool pre)
        {
            Opcode = opcode;
            Variable = variable;
            IsPre = pre;
        }

        public override DecompileTreeNode CleanStatement(DecompileContext context, DecompileTreeNode parent)
        {
            Variable = Variable?.CleanStatement(context, parent);
            return this;
        }

        public override string ToString(DecompileContext context)
        {
            string op = (Opcode == UndertaleInstruction.Opcode.Add ? "++" : "--");
            return (IsPre ? op : "") + Variable.ToString(context) + (IsPost ? op : "");
        }

        internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
        {
            return Variable.DoTypePropagation(context, suggestedType);
        }
    }

    public class KeywordNode : DecompileTreeNode
    {
        public string Keyword;

        public KeywordNode(string keyword)
        {
            Keyword = keyword;
        }

        public override DecompileTreeNode CleanStatement(DecompileContext context, DecompileTreeNode parent)
        {
            return this;
        }

        public override string ToString(DecompileContext context)
        {
            return Keyword + ";";
        }

        internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
        {
            return suggestedType;
        }
    }

    public class BreakNode : KeywordNode
    {
        public BreakNode() : base("break")
        {
        }
    }

    public class ContinueNode : KeywordNode
    {
        public ContinueNode() : base("continue")
        {
        }
    }
}
