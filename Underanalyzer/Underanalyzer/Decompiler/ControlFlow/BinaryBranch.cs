/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;
using Underanalyzer.Decompiler.AST;

namespace Underanalyzer.Decompiler.ControlFlow;

/// <summary>
/// Represents a general binary branch operation. Specifically, only either an if statement or a ternary/conditional operator.
/// </summary>
internal class BinaryBranch : IControlFlowNode
{
    public int StartAddress { get; private set; }

    public int EndAddress { get; private set; }

    public List<IControlFlowNode> Predecessors { get; } = [];

    public List<IControlFlowNode> Successors { get; } = [];

    public IControlFlowNode? Parent { get; set; } = null;

    public List<IControlFlowNode?> Children { get; } = [null, null, null, null];

    public bool Unreachable { get; set; } = false;

    /// <summary>
    /// The "condition" block of the if statement.
    /// </summary>
    public IControlFlowNode Condition { get => Children[0]!; private set => Children[0] = value; }

    /// <summary>
    /// The "true" block of the if statement.
    /// </summary>
    public IControlFlowNode True { get => Children[1]!; private set => Children[1] = value; }

    /// <summary>
    /// The "false" block of the if statement.
    /// </summary>
    public IControlFlowNode False { get => Children[2]!; private set => Children[2] = value; }

    /// <summary>
    /// The "else" block of the if statement, or <see langword="null"/> if none exists.
    /// </summary>
    public IControlFlowNode? Else { get => Children[3]; private set => Children[3] = value; }

    public BinaryBranch(int startAddress, int endAddress, IControlFlowNode condition, IControlFlowNode initialTrue, IControlFlowNode initialFalse)
    {
        StartAddress = startAddress;
        EndAddress = endAddress;
        Condition = condition;
        True = initialTrue;
        False = initialFalse;
    }

    /// <summary>
    /// Visits all nodes that are candidates for the meeting point of the if statement branch, along one path.
    /// Marks off in "visited" all such nodes.
    /// </summary>
    private static void VisitAll(IControlFlowNode start, HashSet<IControlFlowNode> visited)
    {
        Stack<IControlFlowNode> work = new();
        work.Push(start);

        while (work.Count > 0)
        {
            IControlFlowNode node = work.Pop();
            visited.Add(node);

            foreach (IControlFlowNode successor in node.Successors)
            {
                if (successor.StartAddress < node.StartAddress || successor == node)
                {
                    throw new DecompilerException("Unresolved loop when following binary branches");
                }
                if (!visited.Contains(successor))
                {
                    work.Push(successor);
                }
            }
        }
    }


    /// <summary>
    /// Visits all nodes that are candidates for the meeting point of the if statement branch, along a second path.
    /// Upon finding a node that was visited along the first path (through VisitAll), returns that node.
    /// </summary>
    private static IControlFlowNode FindMeetpoint(IControlFlowNode start, IControlFlowNode mustBeAfter, HashSet<IControlFlowNode> visited)
    {
        Stack<IControlFlowNode> work = new();
        work.Push(start);

        while (work.Count > 0)
        {
            IControlFlowNode node = work.Pop();

            if (!visited.Add(node) && node.StartAddress >= mustBeAfter.StartAddress)
            {
                // We found our meetpoint!
                return node;
            }

            foreach (IControlFlowNode successor in node.Successors)
            {
                if (successor.StartAddress < node.StartAddress || successor == node)
                {
                    throw new DecompilerException("Unresolved loop when following binary branches");
                }
                work.Push(successor);
            }
        }

        throw new DecompilerException("Failed to find binary branch meetpoint");
    }

    /// <summary>
    /// Removes any branches coming from inside the given BinaryBranch, and exiting into "after".
    /// </summary>
    private static void CleanupAfterPredecessors(BinaryBranch bb, IControlFlowNode after)
    {
        bool removedElseBranch = false;

        // All branches going into "after" from this branch should come from this branch *only*
        for (int j = after.Predecessors.Count - 1; j >= 0; j--)
        {
            IControlFlowNode curr = after.Predecessors[j];

            // Don't accidentally remove this BinaryBranch going into "after" itself.
            if (curr == bb)
            {
                continue;
            }

            // Check that we're within the bounds of this BinaryBranch.
            if (curr.StartAddress >= bb.StartAddress && curr.EndAddress <= bb.EndAddress)
            {
                // Here, we will additionally remove any "else" branch instructions.
                if (bb.Else is not null && curr.EndAddress == bb.Else.StartAddress && curr is Block b)
                {
                    if (b.Instructions is not [.., { Kind: IGMInstruction.Opcode.Branch }])
                    {
                        throw new DecompilerException("Expected branch to skip past else block");
                    }
                    b.Instructions.RemoveAt(b.Instructions.Count - 1);
                    removedElseBranch = true;
                }

                // Get rid of this connection to "after" from this internal node.
                curr.Successors.RemoveAll(a => a == after);
                after.Predecessors.RemoveAt(j);
            }
        }

        // Sanity check
        if (bb.Else is not null && !removedElseBranch)
        {
            throw new DecompilerException("Failed to remove else branch");
        }
    }

    public static List<BinaryBranch> FindBinaryBranches(DecompileContext ctx)
    {
        List<Block> blocks = ctx.Blocks!;
        List<Loop> loops = ctx.LoopNodes!;
        ctx.BlockSurroundingLoops ??= Branches.FindSurroundingLoops(blocks, ctx.BlocksByAddress!, loops);
        ctx.BlockAfterLimits ??= Branches.ComputeBlockAfterLimits(blocks, ctx.BlockSurroundingLoops);

        // Resolve all relevant continue/break statements
        Branches.ResolveExternalJumps(ctx);

        // Iterate over blocks in reverse, as the compiler generates them in the order we want
        List<BinaryBranch> res = [];
        HashSet<IControlFlowNode> visited = [];
        for (int i = blocks.Count - 1; i >= 0; i--)
        {
            Block block = blocks[i];
            if (block.Instructions is [.., { Kind: IGMInstruction.Opcode.BranchFalse }])
            {
                // Follow "jump" path first, marking off all visited blocks
                VisitAll(block.Successors[1], visited);

                // Locate meetpoint, by following the non-jump path
                IControlFlowNode after = FindMeetpoint(block.Successors[0], block.Successors[1], visited);

                // Insert new node!
                BinaryBranch bb = new(block.StartAddress, after.StartAddress, block, block.Successors[0], block.Successors[1]);
                res.Add(bb);

                // Assign else block if we can immediately detect it
                if (bb.False != after)
                {
                    bb.Else = bb.False;
                }

                // Rewire graph
                if (bb.True == after)
                {
                    // If our true block is the same as the after node, then we have an empty if statement
                    bb.True = new EmptyNode(bb.True.StartAddress);
                }
                else
                {
                    // Disconnect start of "true" block from the condition
                    IControlFlowNode.DisconnectPredecessor(bb.True, 0);
                }
                if (bb.Else is not null)
                {
                    // Disconnect start of "else" block from the condition
                    IControlFlowNode.DisconnectPredecessor(bb.Else, 0);
                }
                else
                {
                    // Check if we have an empty else block
                    for (int j = 0; j < after.Predecessors.Count; j++)
                    {
                        IControlFlowNode curr = after.Predecessors[j];
                        if (curr.StartAddress >= bb.StartAddress && curr.EndAddress <= bb.EndAddress &&
                            curr is Block currBlock && currBlock.Instructions is [.., { Kind: IGMInstruction.Opcode.Branch }])
                        {
                            // We've found the leftovers of an empty else block...
                            bb.Else = new EmptyNode(after.StartAddress);
                        }
                    }
                }

                // If the condition block is unreachable, then so is the branch
                if (block.Unreachable)
                {
                    bb.Unreachable = true;
                    block.Unreachable = false;
                }

                // Reroute all nodes going into condition to instead go into the branch
                for (int j = 0; j < block.Predecessors.Count; j++)
                {
                    bb.Predecessors.Add(block.Predecessors[j]);
                    IControlFlowNode.ReplaceConnections(block.Predecessors[j].Successors, block, bb);
                }
                if (block.Parent is not null)
                {
                    IControlFlowNode.ReplaceConnectionsNullable(block.Parent.Children, block, bb);
                    bb.Parent = block.Parent;
                }
                block.Predecessors.Clear();
                for (int j = 0; j < block.Successors.Count; j++)
                {
                    IControlFlowNode.DisconnectSuccessor(block, j);
                }
                bb.Successors.Add(after);

                // Reroute all predecessors to "after" to come from this branch
                CleanupAfterPredecessors(bb, after);
                after.Predecessors.Add(bb);

                // Update parent status of nodes
                block.Parent = bb;
                bb.True.Parent = bb;
                if (bb.Else is not null)
                {
                    bb.Else.Parent = bb;
                }
            }
        }

        // Sort in order from start to finish
        res.Reverse();

        ctx.BinaryBranchNodes = res;
        return res;
    }

    public void BuildAST(ASTBuilder builder, List<IStatementNode> output)
    {
        // Evaluate condition block
        BlockNode conditionBlock = builder.BuildBlock(Condition);
        conditionBlock.UseBraces = false;
        output.AddRange(conditionBlock.Children);

        IExpressionNode condition = builder.ExpressionStack.Pop();

        // Evaluate true block
        int initialStackCount = builder.ExpressionStack.Count;
        BlockNode trueBlock = builder.BuildBlock(True);
        int postTrueStackCount = builder.ExpressionStack.Count;

        if (Else is not null)
        {
            // Evaluate else block
            BlockNode elseBlock = builder.BuildBlock(Else);
            int postElseStackCount = builder.ExpressionStack.Count;

            if (postTrueStackCount == initialStackCount + 1 && postElseStackCount == postTrueStackCount + 1)
            {
                // We're actually a conditional (ternary) expression
                IExpressionNode falseExpr = builder.ExpressionStack.Pop();
                IExpressionNode trueExpr = builder.ExpressionStack.Pop();
                builder.ExpressionStack.Push(new ConditionalNode(condition, trueExpr, falseExpr));
            }
            else
            {
                // We're an if statement with an else block attached
                output.Add(new IfNode(condition, trueBlock, elseBlock));
            }
        }
        else
        {
            // We're just a simple if statement
            output.Add(new IfNode(condition, trueBlock));
        }
    }
}
