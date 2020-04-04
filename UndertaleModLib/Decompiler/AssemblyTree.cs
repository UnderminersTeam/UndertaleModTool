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
        public AssemblyTreeNode Meetpoint { get; set; } // This is where code execution resumes after any branching.
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
        public Stack<AssemblyTreeNode> PushEnvNodes = new Stack<AssemblyTreeNode>();
        public uint MaxAddress = 0;
        public uint FinalAddress = 0;

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
                if (node.Meetpoint != null)
                    sb.Append(", MP: " + node.Meetpoint.Address);

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
            // Prevent infinite recursion resulting from loops; read a block only one time
            if (tree.Blocks.ContainsKey(startAddress))
                return tree.Nodes[startAddress];

            // If this is the final block, don't deal with reading nothing
            if (startAddress == tree.FinalAddress)
                return null;

            Block currentBlock = new Block(startAddress);
            for (uint i = startAddress; i <= tree.MaxAddress;)
            {
                UndertaleInstruction instr = tree.BlockAddresses[i];
                if (tree.Blocks.ContainsKey(instr.Address))
                { 
                    // Using some already existing block, while also having instructions before it.
                    AssemblyTreeNode newNode = new AssemblyTreeNode(parent, currentBlock);
                    tree.AddNode(newNode);
                    if (parent != null)
                        parent.Next = newNode;
                    newNode.Next = tree.Nodes[instr.Address];
                    return newNode;
                }

                // Add the instruction to the block for this node
                currentBlock.Instructions.Add(instr);

                // Handle reading branching nodes recursively
                if (instr.Kind == UndertaleInstruction.Opcode.B)
                {
                    AssemblyTreeNode addNode = new AssemblyTreeNode(parent, currentBlock);
                    tree.AddNode(addNode);
                    if (parent != null)
                        parent.Next = addNode;

                    addNode.Next = ReadNode(tree, (uint)(instr.Address + instr.JumpOffset), addNode);
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
                    addNode.ConditionFailNode = ReadNode(tree, instr.Address + instr.CalculateInstructionSize(), addNode); // The "fail" adjacent block is just after this instruction
                    addNode.Next = ReadNode(tree, (uint)(instr.Address + instr.JumpOffset), addNode); // The next block is where it jumps to


                    // Calculate the meetpoint where the two paths will come together again
                    HashSet<AssemblyTreeNode> failNodes = new HashSet<AssemblyTreeNode>();

                    Queue<AssemblyTreeNode> queue = new Queue<AssemblyTreeNode>();
                    queue.Enqueue(addNode.ConditionFailNode);
                    while (queue.Count > 0)
                    {
                        AssemblyTreeNode node = queue.Dequeue();
                        if (node == null || !failNodes.Add(node))
                            continue;

                        queue.Enqueue(node.Next);
                        queue.Enqueue(node.ConditionFailNode);
                    }

                    // Search the other branch for the first shared node
                    HashSet<AssemblyTreeNode> nextNodes = new HashSet<AssemblyTreeNode>();
                    AssemblyTreeNode bestMeetpoint = null; // The best node is the node whose Block is the earliest
                    queue.Enqueue(addNode.Next);
                    while (queue.Count > 0)
                    {
                        AssemblyTreeNode node = queue.Dequeue();
                        if (node == null || !nextNodes.Add(node))
                            continue;

                        if (failNodes.Contains(node) && (bestMeetpoint == null || bestMeetpoint.Block.Address > node.Block.Address))
                            bestMeetpoint = node; // If the node is shared on both branches, then check if it's better than the best node we've found so far.

                        queue.Enqueue(node.Next);
                        queue.Enqueue(node.ConditionFailNode);
                    }

                    addNode.Meetpoint = bestMeetpoint;
                    return addNode;
                }
                else if (instr.Kind == UndertaleInstruction.Opcode.PushEnv)
                {
                    AssemblyTreeNode addNode = new AssemblyTreeNode(parent, currentBlock);
                    tree.AddNode(addNode);
                    if (parent != null)
                        parent.Next = addNode;

                    addNode.Next = ReadNode(tree, (instr.Address + instr.CalculateInstructionSize()), addNode);
                    tree.PushEnvNodes.Push(addNode);
                    return addNode;
                }
                else if (instr.Kind == UndertaleInstruction.Opcode.PopEnv)
                {
                    AssemblyTreeNode addNode = new AssemblyTreeNode(parent, currentBlock);
                    tree.AddNode(addNode);
                    if (parent != null)
                        parent.Next = addNode;

                    AssemblyTreeNode afterWith = ReadNode(tree, (instr.Address + instr.CalculateInstructionSize()), addNode);
                    addNode.Next = afterWith;
                    tree.PushEnvNodes.Pop().Meetpoint = afterWith; // Set the meetpoint to be the block after the popenv
                    return addNode;
                }
                else if (instr.Kind == UndertaleInstruction.Opcode.Ret || instr.Kind == UndertaleInstruction.Opcode.Exit)
                {
                    break;
                }

                uint nextAddress = instr.Address + instr.CalculateInstructionSize();
                if (tree.BlockStarts.Contains(nextAddress)) // The next instruction is the start of a new block; read it as one
                {
                    AssemblyTreeNode newNode = new AssemblyTreeNode(parent, currentBlock);
                    tree.AddNode(newNode);
                    if (parent != null)
                        parent.Next = newNode;
                    newNode.Next = ReadNode(tree, nextAddress, newNode);
                    return newNode;
                }

                // Move to the next instruction
                i = nextAddress;
            }

            // Reached the end of the code
            AssemblyTreeNode lastNode = new AssemblyTreeNode(parent, currentBlock);
            tree.AddNode(lastNode);
            if (parent != null)
                parent.Next = lastNode;
            return lastNode;
        }

        public void Setup(DecompileContext context)
        {
            // Assign the context to this AssemblyTree instance
            Context = context;

            // Shorthand
            var instructions = Context.TargetCode.Instructions;

            // Calculate the addresses of the start of each block
            BlockStarts.Clear();
            for (int i = 0; i < instructions.Count; i++)
            {
                UndertaleInstruction instr = instructions[i];

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

            // Map addresses to their corresponding instructions, for easy access
            BlockAddresses.Clear();
            for (int i = 0; i < instructions.Count; i++)
            {
                UndertaleInstruction instr = instructions[i];
                BlockAddresses[instr.Address] = instr;
            }

            // Figure out the addresses of the last instruction, and the end of the last instruction
            if (instructions.Count != 0)
            {
                var lastInstruction = instructions.Last();
                MaxAddress = lastInstruction.Address;
                FinalAddress = MaxAddress + lastInstruction.CalculateInstructionSize();
            }
        }

        public static AssemblyTree CreateTree(DecompileContext context)
        {
            AssemblyTree newTree = new AssemblyTree();
            newTree.Setup(context);
            newTree.Root = ReadNode(newTree, 0, null);
            return newTree;
        }
    }
}
