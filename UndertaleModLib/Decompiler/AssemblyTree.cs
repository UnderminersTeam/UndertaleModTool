using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;
using static UndertaleModLib.Decompiler.Decompiler;

namespace UndertaleModLib.Decompiler
{

    public class AssemblyTreeNode
    {
        public Block Block { get; set;  } // The code for this node.
        public AssemblyTreeNode Parent { get; set; } // This is the code that preceeds this. In the case of this being a new block, this is the last block. In the case of this continuing after, it's the last one.
        public AssemblyTreeNode Next { get; set;  } // This is the code that follows this node. (FOLLOWING, NOT CHILD BLOCKS) Child blocks are set in sub-classes.
        public bool IsNextWithBlock { get; set; }
        public AssemblyTreeNode ConditionFailNode { get; set; }
        public bool IsConditional { get; set; }
        public bool IsConditionSwapped { get; set; }
        public uint Address { get => Block.Address; }

        public AssemblyTreeNode(AssemblyTreeNode parent, Block code)
        {
            Parent = Parent;
            Block = code;
        }
    }

    // This tree holds the parsed assembly data.
    public class AssemblyTree
    {
        public DecompileContext Context { get; set; }
        public AssemblyTreeNode Root { get; set; }
        public Dictionary<uint, Block> Blocks { get; set; } = new Dictionary<uint, Block>();
        public Dictionary<uint, AssemblyTreeNode> Nodes { get; set; } = new Dictionary<uint, AssemblyTreeNode>();
        public List<uint> BlockStarts = new List<uint>();
        public Dictionary<uint, UndertaleInstruction> BlockAddresses = new Dictionary<uint, UndertaleInstruction>();
        public uint MaxAddress;
        public uint FinalAddress;

        public void AddNode(AssemblyTreeNode newNode)
        {
            Blocks[newNode.Block.Address] = newNode.Block;
            Nodes[newNode.Block.Address] = newNode;
        }

        public override string ToString()
        {
            Queue<AssemblyTreeNode> nodeQueue = new Queue<AssemblyTreeNode>();
            nodeQueue.Enqueue(Root);

            StringBuilder builder = new StringBuilder();
            HashSet<uint> set = new HashSet<uint>();
            while (nodeQueue.Count > 0)
            {
                AssemblyTreeNode handleNode = nodeQueue.Dequeue();
                if (handleNode == null || !set.Add(handleNode.Block.Address))
                    continue;

                foreach (var instr in handleNode.Block.Instructions)
                    builder.Append(instr.ToString() + "\n");

                nodeQueue.Enqueue(handleNode.ConditionFailNode); // The jump will always jump below the jump fail code.
                nodeQueue.Enqueue(handleNode.Next);
            }

            return builder.ToString();
        }

        public string ExportFlowGraph()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("digraph G {");

            Queue<AssemblyTreeNode> nodeQueue = new Queue<AssemblyTreeNode>();
            nodeQueue.Enqueue(Root);
            HashSet<uint> alreadyHandled = new HashSet<uint>();
            while (nodeQueue.Count > 0)
            {
                AssemblyTreeNode node = nodeQueue.Dequeue();
                if (node == null || !alreadyHandled.Add(node.Block.Address))
                    continue; // Skip null and already handled options.

                var block = node.Block;
                sb.Append("    block_" + block.Address + " [label=\"");
                bool SwapCondition = node.IsConditional && node.IsConditionSwapped;
                sb.Append("[" + block.ToString() + (node.IsConditional ? ", Conditional" : ""));

                AssemblyTreeNode trueNode = SwapCondition ? node.ConditionFailNode : node.Next;
                AssemblyTreeNode falseNode = SwapCondition ? node.Next : node.ConditionFailNode;
                if (trueNode != null)
                    sb.Append(", T: " + trueNode.Address);
                if (falseNode != null)
                    sb.Append(", F: " + falseNode.Address);

                sb.Append("]\n");
                foreach (var instr in block.Instructions)
                    sb.Append(instr.ToString().Replace("\"", "\\\"") + "\\n");
                sb.Append("\"");
                sb.Append(block.Address == 0 ? ", color=\"blue\"" : "");
                sb.AppendLine(", shape=\"box\"];");
                nodeQueue.Enqueue(node.Next);
                nodeQueue.Enqueue(node.ConditionFailNode);
            }

            sb.AppendLine("");

            nodeQueue.Enqueue(Root);
            alreadyHandled.Clear();
            while (nodeQueue.Count > 0)
            {
                AssemblyTreeNode node = nodeQueue.Dequeue();
                if (node == null || !alreadyHandled.Add(node.Block.Address))
                    continue; // Skip null and already handled options.

                var block = node.Block;
                if (node.IsConditional)
                {
                    bool SwapCondition = node.IsConditional && node.IsConditionSwapped;
                    if (node.Next != null)
                        sb.AppendLine("    block_" + block.Address + " -> block_" + node.Next.Address + " [color=\"" + (SwapCondition ? "red" : "green") + "\"];");
                    if (node.ConditionFailNode != null)
                        sb.AppendLine("    block_" + block.Address + " -> block_" + node.ConditionFailNode.Address + " [color=\"" + (SwapCondition ? "green" : "red") + "\"];");
                }
                else if (node.Next != null)
                        sb.AppendLine("    block_" + block.Address + " -> block_" + node.Next.Address + ";");

                nodeQueue.Enqueue(node.Next);
                nodeQueue.Enqueue(node.ConditionFailNode);
            }

            sb.AppendLine("}");
            return sb.ToString();
        }

        public static AssemblyTreeNode ReadNode(AssemblyTree tree, uint startAddress, AssemblyTreeNode parent)
        {
            if (tree.Blocks.ContainsKey(startAddress))
                return tree.Nodes[startAddress]; // Prevent reading multiple times.

            if (startAddress == tree.FinalAddress)
                return null;

            Block currentBlock = new Block(startAddress);
            for (uint i = startAddress; i <= tree.MaxAddress;)
            {
                UndertaleInstruction instr = tree.BlockAddresses[i];
                if (tree.Blocks.ContainsKey(instr.Address)) { // Using some already existing block, while also having instructions before it.
                    AssemblyTreeNode newNode = new AssemblyTreeNode(parent, currentBlock);
                    tree.AddNode(newNode);
                    if (parent != null)
                        parent.Next = newNode;
                    newNode.Next = tree.Nodes[instr.Address];
                    return newNode;
                }

                currentBlock.Instructions.Add(instr); // Add instruction.

                if (instr.Kind == UndertaleInstruction.Opcode.B)
                {
                    AssemblyTreeNode addNode = new AssemblyTreeNode(parent, currentBlock);
                    tree.AddNode(addNode);
                    if (parent != null)
                        parent.Next = addNode;

                    addNode.Next = ReadNode(tree, (uint) (instr.Address + instr.JumpOffset), addNode);
                    return addNode;
                }
                else if (instr.Kind == UndertaleInstruction.Opcode.Bt || instr.Kind == UndertaleInstruction.Opcode.Bf)
                {
                    AssemblyTreeNode addNode = new AssemblyTreeNode(parent, currentBlock);
                    tree.AddNode(addNode);
                    if (parent != null)
                        parent.Next = addNode;

                    addNode.IsConditional = true;
                    addNode.IsConditionSwapped = (instr.Kind == UndertaleInstruction.Opcode.Bf);
                    addNode.ConditionFailNode = ReadNode(tree, instr.Address + 1, addNode); // The next block is just after this instruction.
                    addNode.Next = ReadNode(tree, (uint)(instr.Address + instr.JumpOffset), addNode); // The next block is where it jumps to.
                    return addNode;
                }
                else if (instr.Kind == UndertaleInstruction.Opcode.PushEnv || instr.Kind == UndertaleInstruction.Opcode.PopEnv)
                {
                    AssemblyTreeNode addNode = new AssemblyTreeNode(parent, currentBlock);
                    tree.AddNode(addNode);
                    if (parent != null)
                        parent.Next = addNode;

                    addNode.Next = ReadNode(tree, (instr.Address + 1), addNode);
                    addNode.IsNextWithBlock = true;
                    return addNode;
                }
                else if (instr.Kind == UndertaleInstruction.Opcode.Ret || instr.Kind == UndertaleInstruction.Opcode.Exit)
                {
                    break;
                }

                uint nextAddress = instr.Address + instr.CalculateInstructionSize();
                if (tree.BlockStarts.Contains(nextAddress)) // The next instruction is the start of a new block, read it as one.
                {
                    AssemblyTreeNode newNode = new AssemblyTreeNode(parent, currentBlock);
                    tree.AddNode(newNode);
                    if (parent != null)
                        parent.Next = newNode;
                    newNode.Next = ReadNode(tree, nextAddress, newNode);
                    return newNode;
                }

                i = nextAddress; // Move to the next instruction.
            }

            AssemblyTreeNode lastNode = new AssemblyTreeNode(parent, currentBlock); // Reached the end of the code.
            if (parent != null)
                parent.Next = lastNode;
            tree.AddNode(lastNode);
            return lastNode;
        }

        public void Setup(DecompileContext context)
        {
            Context = context;

            // Calculate BlockStarts.
            BlockStarts.Clear();
            for (uint i = 0; i < Context.TargetCode.Instructions.Count; i++)
            {
                UndertaleInstruction instr = Context.TargetCode.Instructions[(int)i];

                if (instr.Kind == UndertaleInstruction.Opcode.B)
                {
                    BlockStarts.Add((uint)(instr.Address + instr.JumpOffset));
                }
                else if (instr.Kind == UndertaleInstruction.Opcode.Bt || instr.Kind == UndertaleInstruction.Opcode.Bf)
                {
                    BlockStarts.Add(instr.Address + instr.CalculateInstructionSize());
                    BlockStarts.Add((uint)(instr.Address + instr.JumpOffset));
                }
            }

            // Setup instruction address map.
            BlockAddresses.Clear();
            for (int i = 0; i < context.TargetCode.Instructions.Count; i++)
            {
                UndertaleInstruction instr = context.TargetCode.Instructions[i];
                BlockAddresses[instr.Address] = instr;
            }

            MaxAddress = context.TargetCode.Instructions.Last().Address;
            FinalAddress = MaxAddress + context.TargetCode.Instructions.Last().CalculateInstructionSize();
        }

        public static AssemblyTree CreateTree(DecompileContext context)
        {
            AssemblyTree newTree = new AssemblyTree();
            newTree.Setup(context);
            newTree.Root = ReadNode(newTree, 0, null);
            return newTree;
        }
    }

    

    // with,
    // switch,
    // if,
    // for,
    // while,
    // repeat,
    // do until.
    // Operation.
}
