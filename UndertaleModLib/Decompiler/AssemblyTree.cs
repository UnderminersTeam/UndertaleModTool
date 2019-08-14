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

                nodeQueue.Enqueue(handleNode.Next);
                nodeQueue.Enqueue(handleNode.ConditionFailNode);
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
                sb.Append("[" + block.ToString() + (node.IsConditional ? ", Conditional" : "") + (node.Next != null ? ", T: " + node.Next.Address : "") + (node.ConditionFailNode != null ? ", F: " + node.ConditionFailNode.Address : "") + "]\n");
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
                    if (node.Next != null)
                        sb.AppendLine("    block_" + block.Address + " -> block_" + node.Next.Address + " [color=\"green\"];");
                    if (node.ConditionFailNode != null)
                        sb.AppendLine("    block_" + block.Address + " -> block_" + node.ConditionFailNode.Address + " [color=\"red\"];");
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

            UndertaleCode code = tree.Context.TargetCode;
            Block currentBlock = new Block(startAddress);
            for (uint i = startAddress; i < code.Instructions.Count; i++)
            {
                UndertaleInstruction instr = code.Instructions[(int) i];
                currentBlock.Instructions.Add(instr); // Add instruction.

                if (instr.Kind == UndertaleInstruction.Opcode.B)
                {
                    AssemblyTreeNode addNode = new AssemblyTreeNode(parent, currentBlock);
                    if (parent != null)
                        parent.Next = addNode;
                    tree.AddNode(addNode);

                    addNode.Next = ReadNode(tree, (uint) (instr.Address + instr.JumpOffset), addNode);
                    return addNode;
                }
                else if (instr.Kind == UndertaleInstruction.Opcode.Bt || instr.Kind == UndertaleInstruction.Opcode.Bf)
                {
                    AssemblyTreeNode addNode = new AssemblyTreeNode(parent, currentBlock);
                    if (parent != null)
                        parent.Next = addNode;
                    tree.AddNode(addNode);


                    AssemblyTreeNode stayNode = ReadNode(tree, instr.Address + 1, addNode);
                    AssemblyTreeNode jumpNode = ReadNode(tree, (uint)(instr.Address + instr.JumpOffset), addNode);
                    addNode.IsConditional = true;
                    addNode.ConditionFailNode = (instr.Kind == UndertaleInstruction.Opcode.Bt ? jumpNode : stayNode);
                    addNode.ConditionFailNode = (instr.Kind == UndertaleInstruction.Opcode.Bt ? stayNode : jumpNode);
                    //TODO: addNode.next should go where these meet up.
                    return addNode;
                }
                else if (instr.Kind == UndertaleInstruction.Opcode.PushEnv || instr.Kind == UndertaleInstruction.Opcode.PopEnv)
                {
                    AssemblyTreeNode addNode = new AssemblyTreeNode(parent, currentBlock);
                    if (parent != null)
                        parent.Next = addNode;
                    tree.AddNode(addNode);

                    addNode.Next = ReadNode(tree, (instr.Address + 1), addNode);
                    addNode.IsNextWithBlock = true;
                    return addNode;
                }
                else if (instr.Kind == UndertaleInstruction.Opcode.Ret || instr.Kind == UndertaleInstruction.Opcode.Exit)
                {
                    AssemblyTreeNode addNode = new AssemblyTreeNode(parent, currentBlock);
                    if (parent != null)
                        parent.Next = addNode;
                    tree.AddNode(addNode);
                    return addNode;
                }
            }

            AssemblyTreeNode lastNode = new AssemblyTreeNode(parent, currentBlock); // Reached the end of the code.
            if (parent != null)
                parent.Next = lastNode;
            tree.AddNode(lastNode);
            return lastNode;
        }

        public static AssemblyTree CreateTree(DecompileContext context)
        {
            AssemblyTree newTree = new AssemblyTree();
            newTree.Context = context;
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
