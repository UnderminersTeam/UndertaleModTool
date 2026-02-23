/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;

namespace Underanalyzer.Decompiler.ControlFlow;

internal static class Branches
{
    /// <summary>
    /// Creates a mapping of all blocks to the innermost loop they are contained within.
    /// </summary>
    public static Dictionary<Block, Loop> FindSurroundingLoops(
        List<Block> blocks, Dictionary<int, Block> blockByAddress, List<Loop> loops)
    {
        // Assign blocks to loops.
        // We iterate over loops so that inner loops are processed after outer loops.
        Dictionary<Block, Loop> surroundingLoops = [];
        foreach (Loop l in loops)
        {
            Block startBlock = blockByAddress[l.StartAddress];
            Block endBlock = blockByAddress[l.EndAddress];
            for (int blockIndex = startBlock.BlockIndex; blockIndex < endBlock.BlockIndex; blockIndex++)
            {
                surroundingLoops[blocks[blockIndex]] = l;
            }
        }

        return surroundingLoops;
    }

    private readonly struct LimitEntry(int limit, bool fromBinary)
    {
        public int Limit { get; } = limit;
        public bool FromBinary { get; } = fromBinary;
    }

    /// <summary>
    /// Computes the largest possible address any given if statement can have its "after" node, or successor,
    /// by constraining it based on previous branches and loops. Computes for each branch block.
    /// </summary>
    public static Dictionary<Block, int> ComputeBlockAfterLimits(List<Block> blocks, Dictionary<Block, Loop> surroundingLoops)
    {
        Dictionary<Block, int> blockToAfterLimit = [];

        List<LimitEntry> limitStack = [new(blocks[^1].EndAddress, false)];

        foreach (Block b in blocks)
        {
            // If we've passed the address of our smallest limit, remove it.
            while (b.StartAddress >= limitStack[^1].Limit)
            {
                limitStack.RemoveAt(limitStack.Count - 1);
                if (limitStack.Count == 0)
                {
                    break;
                }
            }
            if (limitStack.Count == 0)
            {
                break;
            }

            // Find the limit for this block
            int thisLimit;
            if (b.Instructions is [.., { Kind: IGMInstruction.Opcode.Branch }])
            {
                if (limitStack[^1].FromBinary)
                {
                    // Most recent limit is from a binary branch - look one further
                    thisLimit = limitStack[^2].Limit;
                }
                else
                {
                    // Most recent limit is from a direct jump - use that directly
                    thisLimit = limitStack[^1].Limit;
                }
            }
            else
            {
                // If we're not Branch, nor BranchFalse/True, we don't really care about determining the limit
                if (b.Instructions is not [.., { Kind: IGMInstruction.Opcode.BranchFalse }] &&
                    b.Instructions is not [.., { Kind: IGMInstruction.Opcode.BranchTrue }]) // TODO: not sure if necessary, but may be for switch statements
                {
                    continue;
                }

                thisLimit = limitStack[^1].Limit;
            }

            // If we have a loop surrounding this block, we can also use that
            if (surroundingLoops.TryGetValue(b, out Loop? loop))
            {
                if (loop.EndAddress < thisLimit)
                {
                    thisLimit = loop.EndAddress;
                }
            }

            // Set resulting limit
            blockToAfterLimit[b] = thisLimit;

            // Update limit stack based on this block
            if (b.Instructions is [.., { Kind: IGMInstruction.Opcode.Branch }])
            {
                // We're in a Branch block.
                // If we have a smaller limit based on our jump destination address, push that to the stack.
                int newLimit = b.Successors[0].StartAddress;
                if (newLimit <= limitStack[^1].Limit)
                {
                    limitStack.Add(new(newLimit, false));
                }
                else
                {
                    // If our limit is larger, but we have an identical limit already on the stack,
                    // from a BranchTrue/False, we mark it as no longer from a binary block.
                    // If there's a slot between two other limits available, use that as well.
                    for (int i = limitStack.Count - 2; i >= 0; i--)
                    {
                        if (limitStack[i].Limit == newLimit)
                        {
                            limitStack[i] = new(newLimit, false);
                            break;
                        }
                        else if (limitStack[i].Limit > newLimit)
                        {
                            limitStack.Insert(i + 1, new(newLimit, false));
                            break;
                        }
                    }
                }
            }
            else
            {
                // We're in a BranchFalse/BranchTrue block.
                // If we have a smaller limit based on our jump destination address, push that to the stack.
                int newLimit = b.Successors[1].StartAddress;
                if (newLimit <= limitStack[^1].Limit)
                {
                    limitStack.Add(new(newLimit, true));
                }
            }
        }

        return blockToAfterLimit;
    }

    /// <summary>
    /// Helper function to insert a continue/break/returne/exit node into the graph.
    /// </summary>
    private static void InsertExternalJumpNode(IControlFlowNode node, Block block, List<Block> blocks, bool disconnectExistingSuccessor = true)
    {
        // Remove branch instruction
        block.Instructions.RemoveAt(block.Instructions.Count - 1);

        // Reroute into a unique node
        if (disconnectExistingSuccessor)
        {
            IControlFlowNode.DisconnectSuccessor(block, 0);
        }
        if (block.Successors.Count == 0)
        {
            block.Successors.Add(node);
            node.Predecessors.Add(block);

            // Now, we want to connect to the following block.
            // However, we may have some other structure there, so we need to follow the parent(s) of the block.
            if (block.BlockIndex + 1 >= blocks.Count)
            {
                throw new DecompilerException("Expected following block after external jump");
            }
            IControlFlowNode following = blocks[block.BlockIndex + 1];
            while (following.Parent is not null)
            {
                following = following.Parent;
            }
            node.Successors.Add(following);
            following.Predecessors.Add(node);
        }
        else
        {
            // We already have a node after us - it's an unreachable node.
            // Just insert this break/continue statement between this block and that node.
            if (block.Successors.Count != 1 || !block.Successors[0].Unreachable)
            {
                throw new DecompilerException("Expected unreachable block after external jump");
            }
            IControlFlowNode.InsertSuccessor(block, 0, node);
        }
    }

    /// <summary>
    /// Resolves (relevant) continue statements, break statements, and return/exit statements.
    /// These are relatively trivial to find on a linear pass, especially with "after limits."
    /// </summary>
    public static void ResolveExternalJumps(DecompileContext ctx)
    {
        Dictionary<Block, Loop> surroundingLoops = ctx.BlockSurroundingLoops!;
        Dictionary<Block, int> blockAfterLimits = ctx.BlockAfterLimits!;
        foreach (Block block in ctx.Blocks!)
        {
            if (block.Instructions is [.., { Kind: IGMInstruction.Opcode.Branch }] && block.Successors.Count >= 1)
            {
                IControlFlowNode? node = null;

                // Check that we're not supposed to be ignored
                if (ctx.SwitchIgnoreJumpBlocks!.Contains(block))
                {
                    continue;
                }

                // Look for a trivial branch to top or end of surrounding loop
                if (surroundingLoops.TryGetValue(block, out Loop? loop))
                {
                    if (block.Successors[0] == loop)
                    {
                        // Detected trivial continue
                        node = new ContinueNode(block.EndAddress - 4);

                        // If enclosing loop is a while loop, it must definitively be a while loop,
                        // as we branch to the very top condition (and not to a for loop incrementor).
                        if (loop is WhileLoop whileLoop)
                        {
                            whileLoop.MustBeWhileLoop = true;
                        }
                    }
                    else if (block.Successors[0].StartAddress >= loop.EndAddress)
                    {
                        if (loop is WithLoop withLoop)
                        {
                            // In a with loop specifically, we only track specifically greater than here
                            // (as our loop end address is technically a little bit earlier than other loops)
                            if (block.Successors[0].StartAddress > withLoop.EndAddress)
                            {
                                if (withLoop.BreakBlock is null)
                                {
                                    throw new DecompilerException("Expected break block on with loop");
                                }

                                // Detected trivial break
                                node = new BreakNode(block.EndAddress - 4);
                            }
                        }
                        else
                        {
                            // Detected trivial break
                            node = new BreakNode(block.EndAddress - 4);
                        }
                    }
                }

                if (node is null)
                {
                    // Check if we're breaking/continuing from inside of a switch statement.
                    IControlFlowNode succNode = block.Successors[0];
                    Block? succBlock = succNode as Block;
                    if (ctx.SwitchEndNodes!.Contains(succNode))
                    {
                        // This is a break from inside of a switch
                        node = new BreakNode(block.EndAddress - 4);
                    }
                    else if (succBlock is not null && ctx.SwitchContinueBlocks!.Contains(succBlock))
                    {
                        // This is a continue from inside of a switch
                        node = new ContinueNode(block.EndAddress - 4);
                    }
                }

                if (node is null && loop is not null)
                {
                    // Look at after limits and see if we can deduce anything there.
                    int afterLimit = blockAfterLimits[block];
                    if (block.Successors[0].StartAddress > afterLimit)
                    {
                        // Detected continue
                        node = new ContinueNode(block.EndAddress - 4);

                        // If enclosing loop is a while loop, it must actually be a for loop - otherwise we would
                        // be branching to the top of the loop, which would have been detected by now.
                        if (loop is WhileLoop whileLoop)
                        {
                            whileLoop.ForLoopIncrementor = block.Successors[0];
                        }
                    }
                }

                if (node is null)
                {
                    // Didn't find anything.
                    continue;
                }

                // Update control flow graph
                InsertExternalJumpNode(node, block, ctx.Blocks);
            }
            else if (block.Instructions is [.., { Kind: IGMInstruction.Opcode.Exit }])
            {
                // We have an exit statement; update control flow graph
                InsertExternalJumpNode(new ExitNode(block.EndAddress - 4), block, ctx.Blocks, false);
            }
            else if (block.Instructions is [.., { Kind: IGMInstruction.Opcode.Return }])
            {
                // We have a return statement; update control flow graph
                InsertExternalJumpNode(new ReturnNode(block.EndAddress - 4), block, ctx.Blocks, false);
            }
        }
    }

    /// <summary>
    /// Helper function to insert a final continue node into the graph.
    /// </summary>
    private static void InsertRemainingExternalJumpNode(IControlFlowNode node, Block block, List<Block> blocks)
    {
        // Remove branch instruction
        block.Instructions.RemoveAt(block.Instructions.Count - 1);

        // Reroute into a unique node
        if (block.Successors.Count > 0)
        {
            IControlFlowNode.DisconnectSuccessor(block, 0);
        }
        if (block.Successors.Count == 0)
        {
            block.Successors.Add(node);
            node.Predecessors.Add(block);

            // Now, we want to connect to the following block, *only* if it has no existing predecessors.
            // However, we may have some other structure there, so we need to follow the parent(s) of the block.
            if (block.BlockIndex + 1 >= blocks.Count)
            {
                throw new DecompilerException("Expected following block after external jump");
            }
            IControlFlowNode following = blocks[block.BlockIndex + 1];
            while (following.Parent is not null)
            {
                following = following.Parent;
            }
            if (following.Predecessors.Count == 0)
            {
                node.Successors.Add(following);
                following.Predecessors.Add(node);
            }
        }
        else
        {
            // We already have a node after us - it's an unreachable node.
            // Just insert this continue statement between this block and that node.
            if (block.Successors.Count != 1 || !block.Successors[0].Unreachable)
            {
                throw new DecompilerException("Expected unreachable block after external jump");
            }
            IControlFlowNode.InsertSuccessor(block, 0, node);
        }
    }

    /// <summary>
    /// Resolves any remaining continue statements.
    /// </summary>
    public static void ResolveRemainingExternalJumps(DecompileContext ctx)
    {
        foreach (Block block in ctx.Blocks!)
        {
            if (block.Instructions is [.., { Kind: IGMInstruction.Opcode.Branch, BranchOffset: int branchOffset }])
            {
                if (!ctx.BlockSurroundingLoops!.TryGetValue(block, out Loop? loop))
                {
                    throw new DecompilerException("Expected loop to be around unresolved branch");
                }

                if (loop is WhileLoop whileLoop)
                {
                    // Manually check instruction branch offset (as we're not guaranteed any successors at this stage)
                    if (branchOffset > 0)
                    {
                        // This is probably a for loop now.
                        // We need to find the original successor, and set that as the incrementor.
                        IControlFlowNode succ = ctx.BlocksByAddress![(block.EndAddress - 4) + branchOffset];
                        while (succ.Parent is not null)
                        {
                            succ = succ.Parent;
                        }
                        whileLoop.ForLoopIncrementor = succ;
                    }
                    else
                    {
                        // This is probably a while loop now
                        whileLoop.MustBeWhileLoop = true;
                    }
                }

                // Update control flow graph
                InsertRemainingExternalJumpNode(new ContinueNode(block.EndAddress - 4), block, ctx.Blocks);
            }
        }
    }
}
