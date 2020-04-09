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

        public bool isGameMaker2 { get => Data != null && Data.IsGameMaker2(); }

        public DecompileContext(UndertaleData data, bool enableStringLabels)
        {
            this.Data = data;
            this.EnableStringLabels = enableStringLabels;
        }

        public void Setup(UndertaleCode code)
        {
            TargetCode = code;
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

        private static BlockTreeNode CreateNode(DecompileTree dTree, AssemblyTreeNode treeNode, AssemblyTreeNode stopAt)
        {
            if (treeNode == null)
                return null; // If we're to create a node for a null assembly tree node, that means the block tree node is also null.

            if (treeNode == stopAt)
                return null; // Stop where required to, when traversing subsections

            BlockTreeNode res = new BlockTreeNode();
            Block block = treeNode.Block;

            // Match the AssemblyTreeNode to its BlockTreeNode
            dTree.ReadNodes[treeNode] = res;

            foreach (UndertaleInstruction instr in block.Instructions)
            {
                switch (instr.Kind)
                {
                    /// Handle control flow
                    case UndertaleInstruction.Opcode.Exit:
                        res.Nodes.Add(new ReturnNode());
                        break;
                    case UndertaleInstruction.Opcode.Ret:
                        res.Nodes.Add(new ReturnNode()); // TODO: pop stack for this parameter, but make sure stack has something on it
                        break;
                    case UndertaleInstruction.Opcode.Bf:
                        if (treeNode.IsLoopHeader)
                        {
                            // TODO do...until loop handling here

                            dTree.Loops.Push(treeNode);
                            res.Nodes.Add(new WhileLoopNode() { Block = CreateNode(dTree, treeNode.ConditionFailNode, treeNode.Next) });
                            dTree.Loops.Pop();

                            // Any code that comes after
                            if (treeNode.Next != null)
                                res.Nodes.AddRange(CreateNode(dTree, treeNode.Next, stopAt).Nodes);
                        } else
                        {
                            // If statement
                            IfNode ifNode = new IfNode();
                            dTree.IfContexts.Push(new IfContext()); // context to propagate BAtEndTarget
                            ifNode.FalseBlock = CreateNode(dTree, treeNode.Next, treeNode.Meetpoint);
                            ifNode.TrueBlock = CreateNode(dTree, treeNode.ConditionFailNode, treeNode.Meetpoint);
                            res.Nodes.Add(ifNode);

                            // Let's test if an else block existed in the source code
                            // (there would be a B instruction emitted by the compiler)
                            if (dTree.IfContexts.Pop().BAtEndTarget == (treeNode.Meetpoint?.Address ?? dTree.AssemblyTree.FinalAddress))
                            {
                                if (ifNode.FalseBlock == null)
                                    ifNode.FalseBlock = new BlockTreeNode(); // Just make an empty else block
                            }
                            else if (ifNode.FalseBlock != null)
                            {
                                // This ISN'T an else, so just move it to after the if statement
                                res.Nodes.AddRange(ifNode.FalseBlock.Nodes);
                                ifNode.FalseBlock = null;
                            }

                            // Any code that comes after
                            if (treeNode.Meetpoint != null && treeNode.Meetpoint != stopAt)
                                res.Nodes.AddRange(CreateNode(dTree, treeNode.Meetpoint, stopAt).Nodes);
                        }
                        break;
                    case UndertaleInstruction.Opcode.Bt:
                        // TODO switch statement and repeat handling here
                        break;
                    case UndertaleInstruction.Opcode.B:
                        if (dTree.Loops.Count != 0)
                        {
                            AssemblyTreeNode loopHead = dTree.Loops.Peek();
                            if (treeNode.Next == loopHead)
                            {
                                // This must be a continue statement, but ones at the end of the loop aren't needed
                                if (treeNode != loopHead.LoopTail)
                                {
                                    res.Nodes.Add(new ContinueNode());
                                }
                                break;
                            }
                            else
                            {
                                // To be a break, it has to leave the loop farther out than the tail
                                if ((uint)(instr.Address + instr.JumpOffset) > loopHead.LoopTail.Address)
                                {
                                    res.Nodes.Add(new BreakNode());
                                    break;
                                }
                            }
                        }
                        if (treeNode.Next != null && stopAt != treeNode.Next && !dTree.ReadNodes.ContainsKey(treeNode.Next))
                        {
                            res.Nodes.AddRange(CreateNode(dTree, treeNode.Next, stopAt).Nodes);
                        } else
                        {
                            if (dTree.IfContexts.Count != 0)
                                dTree.IfContexts.Peek().BAtEndTarget = (int)instr.Address + instr.JumpOffset;
                            else
                                res.Nodes.Add(new SingleLineCommentNode("Error: no if context for B instruction"));
                        }
                        break;
                    case UndertaleInstruction.Opcode.PushEnv:
                        WithNode withNode = new WithNode(null, null); // todo

                        // First, read everything after the meetpoint.
                        BlockTreeNode afterWithBlock = CreateNode(dTree, treeNode.Meetpoint, stopAt);
                        withNode.Block = CreateNode(dTree, treeNode.Next, treeNode.Meetpoint); // create with block.
                        res.Nodes.Add(withNode);
                        if (afterWithBlock != null)
                            res.Nodes.AddRange(afterWithBlock.Nodes);
                        break;

                    /// TODO: Handle all other instructions
                    default:
                        res.Nodes.Add(new InstructionNode(instr));
                        break;
                }
            }

            if (!treeNode.IsConditional && treeNode.Next != null && stopAt != treeNode.Next && !dTree.ReadNodes.ContainsKey(treeNode.Next))
                res.Nodes.AddRange(CreateNode(dTree, treeNode.Next, stopAt).Nodes);

            if (treeNode.Unreachable != null)
                res.Nodes.AddRange(CreateNode(dTree, treeNode.Unreachable, stopAt).Nodes); // TODO: use a different stopAt, like meetpoint?

            return res;
        }

        public class IfContext
        {
            // Used to propagate B instruction uses up to the base IfNode
            public int BAtEndTarget { get; set; }

            public IfContext()
            {
                BAtEndTarget = -1;
            }
        }


        public static string Decompile(UndertaleCode code, DecompileContext context)
        {
            return DecompileBase(code, context).ToString(true);
        }

        public static DecompileTree DecompileBase(UndertaleCode code, DecompileContext context)
        {
            context.Setup(code);
            //TODO: Look into getting rid of the parent in CleanStatement

            AssemblyTree assemblyTree = AssemblyTree.CreateTree(context);
            DecompileTree decompileTree = new DecompileTree() { Context = context, AssemblyTree = assemblyTree };
            decompileTree.Root = CreateNode(decompileTree, assemblyTree.Root, null);
            // TODO: After building the tree, combine blocks which overlap.
            // TODO: Type Propogation.
            // TODO: Cleanup.

            return decompileTree;
        }

        public class DecompileTree
        {
            public DecompileContext Context;
            public AssemblyTree AssemblyTree;
            public BlockTreeNode Root = new BlockTreeNode();
            public Stack<AssemblyTreeNode> Loops = new Stack<AssemblyTreeNode>();
            public Stack<IfContext> IfContexts = new Stack<IfContext>();
            public Dictionary<AssemblyTreeNode, BlockTreeNode> ReadNodes = new Dictionary<AssemblyTreeNode, BlockTreeNode>();

            public override string ToString()
            {
                return ToString(false);
            }

            public string ToString(bool includeLocalVars)
            {
                string codeStr = Root?.ToString(Context) ?? "";
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
        }

        public class InstructionNode : DecompileTreeNode
        {
            public UndertaleInstruction Instruction;

            public InstructionNode(UndertaleInstruction instruction)
            {
                Instruction = instruction;
            }

            public override string ToString(DecompileContext context)
            {
                return "// " + Instruction.ToString(context.TargetCode, context.Data.Variables);
            }
        }

        public class SingleLineCommentNode : DecompileTreeNode
        {
            public String Comment;

            public SingleLineCommentNode(string comment)
            {
                Comment = comment;
            }

            public override string ToString(DecompileContext context)
            {
                return "// " + Comment.Replace("\n", "\\n");
            }
        }

        public class DummyTreeNode : DecompileTreeNode
        {
            public String Data;

            public DummyTreeNode(string data)
            {
                Data = data;
            }

            public override string ToString(DecompileContext context)
            {
                return Data;
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

        public override string ToString(DecompileContext context)
        {
            if (ReturnValue != null)
                return "return " + ReturnValue.ToString(context) + ";";

            return (context.isGameMaker2 ? "return;" : "exit;");
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

        public override string ToString(DecompileContext context)
        {
            return "with (" + ExecutionEnvironment.ToString(context) + ") " + Block.ToString(context, true);
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

        public override string ToString(DecompileContext context)
        {
            StringBuilder sb = new StringBuilder();
            //sb.Append("if " + Condition.ToString(context) + " "); TODO
            sb.Append("if (pop stack)\n");
            sb.Append(TrueBlock.ToString(context, true));

            foreach (ElseIfBlock elseIf in ElseIfBlocks)
            {
                //sb.Append("else if " + elseIf.Condition.ToString(context) + " ");
                sb.Append(" else if (pop stack)\n");
                sb.Append(elseIf.Block.ToString(context, true));
            }

            if (HasElse)
            {
                sb.Append(" else\n");
                sb.Append(FalseBlock.ToString(context, true));
            }
            return sb.ToString();
        }
    }

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

    public class WhileLoopNode : DecompileTreeNode
    {
        public BlockTreeNode Block;
        public DecompileTreeNode Condition;

        public override string ToString(DecompileContext context)
        {
            return "while (pop stack)\n" + Block.ToString(context, true);
        }

        // For Loop.
        public bool IsForLoop { get; set; }
        //public AssignmentStatementNode InitializeStatement;
        //public AssignmentStatementNode StepStatement;

        // Repeat loop
        //public bool IsRepeatLoop { get => RepeatStartValue != null; }
        //public Statement RepeatStartValue;
    }

    public class DoUntilNode : DecompileTreeNode
    {
        public BlockTreeNode Block;
        public DecompileTreeNode Condition;

        public override string ToString(DecompileContext context)
        {
            throw new NotImplementedException();
        }
    }

    public class RepeatLoop : DecompileTreeNode
    {
        public DecompileTreeNode Count;
        public BlockTreeNode Block;

        public override string ToString(DecompileContext context)
        {
            throw new NotImplementedException();
        }
    }

    public class SwitchStatementNode : DecompileTreeNode
    {
        public DecompileTreeNode SwitchExpression;
        public List<SwitchCaseStatement> Cases;

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
    }

    public class KeywordNode : DecompileTreeNode
    {
        public string Keyword;

        public KeywordNode(string keyword)
        {
            Keyword = keyword;
        }
      
        public override string ToString(DecompileContext context)
        {
            return Keyword + ";";
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
