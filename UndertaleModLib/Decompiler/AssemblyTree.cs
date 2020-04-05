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
        public AssemblyTreeNode ConditionFailNode { get; set; } // This is the code that follows the node (not directly after), if not Next
        public AssemblyTreeNode Unreachable { get; set; } // This is the code following the node if it is unreachable
        public bool IsConditional { get; set; }
        public bool IsConditionSwapped { get; set; }
        public uint Address { get => Block.Address; }
        public bool IsUnreachable { get; set; }

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
                sb.Append("[" + block.ToString());
                if (node.IsUnreachable)
                    sb.Append(", Unreachable");
                if (node.IsConditional)
                    sb.Append(", Conditional");
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
                    sb.Append(instr.ToString().Replace("\\\"", "\\\\\"").Replace("\"", "\\\"") + "\\n");
                sb.Append("\"");
                sb.Append(block.Address == 0 ? ", color=\"blue\"" : "");
                sb.AppendLine(", shape=\"box\"];");
                nodeQueue.Enqueue(node.Next);
                nodeQueue.Enqueue(node.ConditionFailNode);
                nodeQueue.Enqueue(node.Unreachable);
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
                if (node.Unreachable != null)
                    sb.AppendLine("    block_" + block.Address + " -> block_" + node.Unreachable.Address + " [color=\"gray\"];");

                nodeQueue.Enqueue(node.Next);
                nodeQueue.Enqueue(node.ConditionFailNode);
                nodeQueue.Enqueue(node.Unreachable);
            }

            sb.AppendLine("}");
            return sb.ToString();
        }

        public static AssemblyTreeNode ReadNode(AssemblyTree tree, uint startAddress, AssemblyTreeNode parent, bool unreachable = false)
        {
            uint i; // Declare this field up here so it can be reused properly

            // Prevent infinite recursion resulting from loops; read a block only one time
            if (tree.Blocks.ContainsKey(startAddress))
                return tree.Nodes[startAddress];

            // If this is the final block, don't deal with reading nothing
            if (startAddress == tree.FinalAddress)
                return null;

            uint maxAddress;
            if (unreachable)
            {
                // Calculate the end of the unreachable block
                UndertaleInstruction instr = null;
                for (i = startAddress; !tree.BlockStarts.Contains(i) && i <= tree.MaxAddress; i += instr.CalculateInstructionSize())
                {
                    instr = tree.BlockAddresses[i];
                }
                maxAddress = instr?.Address ?? startAddress;

                // Calculate BlockStarts for this range
                tree.CalculateBlockStarts(startAddress, maxAddress);
            }
            else
                maxAddress = tree.MaxAddress; // Go all the way until the end or until a branch is hit

            Block currentBlock = new Block(startAddress);
            bool readUnreachable = false;
            for (i = startAddress; i <= maxAddress;)
            {
                UndertaleInstruction instr = tree.BlockAddresses[i];
                if (tree.Blocks.ContainsKey(instr.Address))
                { 
                    // Using some already existing block, while also having instructions before it
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
                    if (tree.BlockAddresses.ContainsKey(instr.Address + 1) && !tree.BlockStarts.Contains(instr.Address + 1))
                        addNode.Unreachable = ReadNode(tree, instr.Address + 1, null, true);
                    if (unreachable)
                        addNode.IsUnreachable = true;
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
                    if (tree.BlockAddresses.ContainsKey(instr.Address + 1) && !tree.BlockStarts.Contains(instr.Address + 1))
                        addNode.Unreachable = ReadNode(tree, instr.Address + 1, null, true);

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
                    if (unreachable)
                        addNode.IsUnreachable = true;
                    return addNode;
                }
                else if (instr.Kind == UndertaleInstruction.Opcode.PushEnv)
                {
                    AssemblyTreeNode addNode = new AssemblyTreeNode(parent, currentBlock);
                    tree.AddNode(addNode);
                    if (parent != null)
                        parent.Next = addNode;

                    tree.PushEnvNodes.Push(addNode);
                    addNode.Next = ReadNode(tree, (instr.Address + instr.CalculateInstructionSize()), addNode);
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
                    // Set the meetpoint to be the block after the popenv
                    if (instr.JumpOffsetPopenvExitMagic)
                        tree.PushEnvNodes.Peek().Meetpoint = afterWith;
                    else
                        tree.PushEnvNodes.Pop().Meetpoint = afterWith;
                    return addNode;
                }
                else if (instr.Kind == UndertaleInstruction.Opcode.Ret || instr.Kind == UndertaleInstruction.Opcode.Exit)
                {
                    if (tree.BlockAddresses.ContainsKey(instr.Address + 1) && !tree.BlockStarts.Contains(instr.Address + 1))
                        readUnreachable = true;
                    break;
                }

                uint nextAddress = instr.Address + instr.CalculateInstructionSize();
                if (tree.BlockStarts.Contains(nextAddress) && nextAddress <= maxAddress) // The next instruction is the start of a new block; read it as one
                {
                    AssemblyTreeNode newNode = new AssemblyTreeNode(parent, currentBlock);
                    tree.AddNode(newNode);
                    if (parent != null)
                        parent.Next = newNode;
                    newNode.Next = ReadNode(tree, nextAddress, newNode);
                    if (unreachable)
                        newNode.IsUnreachable = true;
                    return newNode;
                }

                // Move to the next instruction
                i = nextAddress;
            }

            // Reached the end of the code
            AssemblyTreeNode lastNode = new AssemblyTreeNode(parent, currentBlock);
            tree.AddNode(lastNode);
            if (unreachable)
            {
                lastNode.IsUnreachable = true;
            } else if (parent != null)
                parent.Next = lastNode;

            if (readUnreachable)
                lastNode.Unreachable = ReadNode(tree, i + 1, null, true);

            return lastNode;
        }

        public void CalculateBlockStarts(uint start, uint end)
        {
            // Calculate the addresses of the start of each block from start to end (including end), using a queue, ignoring unreachable blocks
            Queue<uint> toSearchNext = new Queue<uint>();
            toSearchNext.Enqueue(start);
            do
            {
                uint addr = toSearchNext.Dequeue();
                if (addr < start || addr > end) // Ignore addresses out of bounds
                    continue;

                UndertaleInstruction instr;
                for (uint i = addr; i <= end; i += instr.CalculateInstructionSize())
                {
                    instr = BlockAddresses[i];
                    if (instr.Kind == UndertaleInstruction.Opcode.B)
                    {
                        addr = (uint)(instr.Address + instr.JumpOffset);
                        if (!BlockStarts.Contains(addr))
                        {
                            toSearchNext.Enqueue(addr);
                            BlockStarts.Add(addr);
                        }
                        break;
                    }
                    else if (instr.Kind == UndertaleInstruction.Opcode.Bt || instr.Kind == UndertaleInstruction.Opcode.Bf)
                    {
                        addr = instr.Address + instr.CalculateInstructionSize();
                        if (!BlockStarts.Contains(addr))
                        {
                            toSearchNext.Enqueue(addr);
                            BlockStarts.Add(addr);
                        }
                        addr = (uint)(instr.Address + instr.JumpOffset);
                        if (!BlockStarts.Contains(addr))
                        {
                            toSearchNext.Enqueue(addr);
                            BlockStarts.Add(addr);
                        }
                        break;
                    }
                }
            } while (toSearchNext.Count != 0);
        }

        public void Setup(DecompileContext context)
        {
            // Assign the context to this AssemblyTree instance
            Context = context;

            // Shorthand
            var instructions = Context.TargetCode.Instructions;

            // Map addresses to their corresponding instructions, for easy access
            BlockAddresses.Clear();
            for (int i = 0; i < instructions.Count; i++)
            {
                UndertaleInstruction instr = instructions[i];
                BlockAddresses[instr.Address] = instr;
            }

            if (instructions.Count != 0)
            {
                // Figure out the addresses of the last instruction, and the end of the last instruction
                var lastInstruction = instructions.Last();
                MaxAddress = lastInstruction.Address;
                FinalAddress = MaxAddress + lastInstruction.CalculateInstructionSize();

                // Calculate the block starts
                BlockStarts.Clear();
                CalculateBlockStarts(0, MaxAddress);
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
