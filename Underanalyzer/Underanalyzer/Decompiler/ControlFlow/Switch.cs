/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;
using Underanalyzer.Decompiler.AST;

namespace Underanalyzer.Decompiler.ControlFlow;

internal class Switch : IControlFlowNode
{
    /// <summary>
    /// Initial detection data for a switch statement, used to prevent calculations being done twice.
    /// </summary>
    public class SwitchDetectionData(Block endBlock, IControlFlowNode endNode, bool mayBeMisdetected)
    {
        public Block EndBlock { get; set; } = endBlock;
        public IControlFlowNode EndNode { get; set; } = endNode;
        public Block? ContinueBlock { get; set; } = null;
        public Block? ContinueSkipBlock { get; set; } = null;
        public Block? EndOfCaseBlock { get; set; } = null;
        public Block? DefaultBranchBlock { get; set; } = null;
        public bool MayBeMisdetected { get; set; } = mayBeMisdetected;
    }

    public class CaseJumpNode(int address) : IControlFlowNode
    {
        public int StartAddress { get; private set; } = address;

        public int EndAddress { get; private set; } = address;

        public List<IControlFlowNode> Predecessors { get; } = [];

        public List<IControlFlowNode> Successors { get; } = [];

        public IControlFlowNode? Parent { get; set; } = null;

        public List<IControlFlowNode?> Children { get; } = [];

        public bool Unreachable { get; set; } = false;

        public override string ToString()
        {
            return $"{nameof(CaseJumpNode)} (address {StartAddress}, {Predecessors.Count} predecessors, {Successors.Count} successors)";
        }

        public void BuildAST(ASTBuilder builder, List<IStatementNode> output)
        {
            // Queue our expression to be used later, when the case destination is processed
            builder.SwitchCases!.Enqueue(builder.ExpressionStack.Pop());

            // Get rid of duplicated expression
            builder.ExpressionStack.Pop();
        }
    }

    public class CaseDestinationNode(int address) : IControlFlowNode
    {
        public int StartAddress { get; private set; } = address;

        public int EndAddress { get; private set; } = address;

        public List<IControlFlowNode> Predecessors { get; } = [];

        public List<IControlFlowNode> Successors { get; } = [];

        public IControlFlowNode? Parent { get; set; } = null;

        public List<IControlFlowNode?> Children { get; } = [];

        public bool Unreachable { get; set; } = false;

        public bool IsDefault { get; set; } = false;

        public override string ToString()
        {
            return $"{nameof(CaseDestinationNode)} (address {StartAddress}, {Predecessors.Count} predecessors, {Successors.Count} successors)";
        }

        public void BuildAST(ASTBuilder builder, List<IStatementNode> output)
        {
            if (IsDefault)
            {
                // Just a simple default
                output.Add(new SwitchCaseNode(null));
            }
            else
            {
                // Retrieve expression from earlier evaluation
                output.Add(new SwitchCaseNode(builder.SwitchCases!.Dequeue()));
            }
        }
    }

    public int StartAddress { get; private set; }

    public int EndAddress { get; private set; }

    public List<IControlFlowNode> Predecessors { get; } = [];

    public List<IControlFlowNode> Successors { get; } = [];

    public IControlFlowNode? Parent { get; set; } = null;

    public List<IControlFlowNode?> Children { get; } = [null, null, null];

    public bool Unreachable { get; set; } = false;

    /// <summary>
    /// The first block that begins the chain of case conditions. Should always be a Block.
    /// </summary>
    public IControlFlowNode Cases { get => Children[0] ?? throw new System.NullReferenceException(); private set => Children[0] = value; }

    /// <summary>
    /// The first node of the switch statement body. Should always be a CaseDestinationNode, or <see langword="null"/>.
    /// </summary>
    public IControlFlowNode? Body { get => Children[1]; private set => Children[1] = value; }

    /// <summary>
    /// An optional successor chain of case destinations (<see langword="null"/> if none was necessary).
    /// Specifically, those that appear at the very end of the switch statement and have no code.
    /// </summary>
    public IControlFlowNode? EndCaseDestinations { get => Children[2]; private set => Children[2] = value; }

    /// <summary>
    /// The data used to detect this switch statement, used for later verification.
    /// </summary>
    internal SwitchDetectionData DetectionData { get; }

    public Switch(int startAddress, int endAddress, 
        IControlFlowNode cases, IControlFlowNode? body, IControlFlowNode? endCaseDestinations, SwitchDetectionData data)
    {
        StartAddress = startAddress;
        EndAddress = endAddress;
        Cases = cases;
        Body = body;
        EndCaseDestinations = endCaseDestinations;
        DetectionData = data;
    }

    private class BlockIndexComparer : IComparer<Block>
    {
        public static BlockIndexComparer Instance { get; } = new();

        public int Compare(Block? x, Block? y)
        {
            if (x is null || y is null)
            {
                throw new System.NullReferenceException();
            }
            return x.BlockIndex - y.BlockIndex;
        }
    }

    private static void DetectionPass(DecompileContext ctx)
    {
        List<Block> blocks = ctx.Blocks!;
        List<Fragment> fragments = ctx.FragmentNodes!;
        ctx.BlockSurroundingLoops ??= Branches.FindSurroundingLoops(blocks, ctx.BlocksByAddress!, ctx.LoopNodes!);
        ctx.BlockAfterLimits ??= Branches.ComputeBlockAfterLimits(blocks, ctx.BlockSurroundingLoops);

        foreach (Fragment fragment in fragments)
        {
            for (int i = 0; i < fragment.Blocks.Count - 1; i++)
            {
                Block block = fragment.Blocks[i];
                Block? endCaseBlock = null;
                IControlFlowNode? endNode = null;
                Block? endBlock = null;
                bool mayBeMisdetected = false;
                if (block.Instructions is [.., { Kind: IGMInstruction.Opcode.BranchTrue }])
                {
                    // We have the very top of a switch statement.
                    // Now, repeatedly follow successors until we find a Branch instruction,
                    // which is either a default case branch or the "end of cases" branch.
                    IControlFlowNode node = block.Successors[0];
                    while (node is not Block b || b.Instructions is not [.., { Kind: IGMInstruction.Opcode.Branch }])
                    {
                        // Check if this node is a block ending with BranchFalse.
                        if (node is Block { Instructions: [.., { Kind: IGMInstruction.Opcode.BranchFalse }] })
                        {
                            // Go to *jump* successor, so as to avoid taking any "else" branches in conditionals
                            node = node.Successors[1];
                        }
                        else
                        {
                            // Follow immediate (non-branch) successor for all other types of nodes
                            node = node.Successors[0];
                        }

                        // Ensure we don't infinitely loop...
                        if (node.StartAddress <= block.StartAddress)
                        {
                            throw new DecompilerException("Unexpected loop when detecting switch statements");
                        }
                    }

                    // We're now at a Block ending in a Branch instruction. This is either the end of case branch, or the default case branch.
                    // If there exists a default case branch in this switch statement, it would be this one.
                    // To check, we look at the next block (at least one more *should* exist) to see if it's an unreachable Branch block.
                    // We use this to determine endNode.
                    Block firstBranchBlock = node as Block ?? throw new System.NullReferenceException();
                    Block nextBlock = blocks[firstBranchBlock.BlockIndex + 1];
                    if (nextBlock is { Unreachable: true, Instructions: [{ Kind: IGMInstruction.Opcode.Branch }] })
                    {
                        // nextBlock is the end of case block
                        endCaseBlock = nextBlock;
                    }
                    else
                    {
                        // firstBranchBlock is the end of case block
                        endCaseBlock = firstBranchBlock;
                    }
                    endNode = endCaseBlock.Successors[0];
                    endBlock = ctx.BlocksByAddress![endNode.StartAddress];
                }
                else if (block.Instructions is [.., { Kind: IGMInstruction.Opcode.Branch }])
                {
                    // If we have an unreachable Branch block immediately after this block, then this block is a default case.
                    Block nextBlock = blocks[block.BlockIndex + 1];
                    if (nextBlock is { Unreachable: true, Instructions: [{ Kind: IGMInstruction.Opcode.Branch }] })
                    {
                        // nextBlock is the end of case block
                        endCaseBlock = nextBlock;
                        endNode = nextBlock.Successors[0];
                        endBlock = ctx.BlocksByAddress![endNode.StartAddress];

                        // Ensure both branches are forward branches (past these two blocks)
                        if (block.Successors[0].StartAddress < nextBlock.EndAddress ||
                            nextBlock.Successors[0].StartAddress < nextBlock.EndAddress)
                        {
                            continue;
                        }

                        // Ensure end block starts with PopDelete instruction (as a quick elimination)
                        if (endBlock.Instructions is not [{ Kind: IGMInstruction.Opcode.PopDelete }, ..])
                        {
                            continue;
                        }

                        // Perform extra checks only if we're inside of a loop, and endBlock ends with return/exit
                        // (as this pattern can be produced with continue statments in a for loop, with an incrementor that exits)
                        if (ctx.BlockSurroundingLoops.ContainsKey(block) &&
                            endBlock.Instructions is [.., { Kind: IGMInstruction.Opcode.Return or IGMInstruction.Opcode.Exit }])
                        {
                            // Split cases
                            if (block.Successors[0].StartAddress == nextBlock.Successors[0].StartAddress)
                            {
                                // Both branches go to the same block, meaning this switch should be totally empty (with a default case)
                                if (nextBlock.EndAddress != endNode.StartAddress)
                                {
                                    continue;
                                }
                            }
                            else
                            {
                                // Ensure default case branches somewhere before the end block
                                if (block.Successors[0].StartAddress >= endNode.StartAddress)
                                {
                                    continue;
                                }
                            }

                            // Ensure end block is only branched to by internal nodes
                            bool externalPredecessor = false;
                            for (int j = 0; j < endNode.Predecessors.Count; j++)
                            {
                                IControlFlowNode currPred = endNode.Predecessors[j];
                                if (currPred.StartAddress < block.StartAddress || currPred.StartAddress >= endNode.StartAddress)
                                {
                                    externalPredecessor = true;
                                    break;
                                }
                            }
                            if (externalPredecessor)
                            {
                                continue;
                            }

                            // Have to detect whether this actually a switch during AST building, unfortunately
                            mayBeMisdetected = true;
                        }
                    }
                    else
                    {
                        // Check for an empty switch (note: quirk of existing modding tools; not actually valid GML)
                        if (block.Successors.Count == 1 && block.Successors[0] == nextBlock &&
                            nextBlock.Predecessors.Count == 1 && nextBlock.Predecessors[0] == block)
                        {
                            // Ensure next block starts with PopDelete instruction (as a quick elimination)
                            if (nextBlock.Instructions is not [{ Kind: IGMInstruction.Opcode.PopDelete }, ..])
                            {
                                continue;
                            }

                            // Perform extra checks only if we're inside of a loop, and nextBlock ends with return/exit
                            // (as this pattern can be produced with continue statments in a for loop, with an incrementor that exits)
                            if (ctx.BlockSurroundingLoops.ContainsKey(block) && 
                                nextBlock.Instructions is [.., { Kind: IGMInstruction.Opcode.Return or IGMInstruction.Opcode.Exit } ])
                            {
                                mayBeMisdetected = true;
                            }

                            // nextBlock is the end node, but go up to any parent control flow
                            endCaseBlock = block;
                            endNode = nextBlock;
                            endBlock = nextBlock;
                            while (endNode.Parent is not null)
                            {
                                endNode = endNode.Parent;
                            }
                        }
                    }
                }

                if (endNode is not null)
                {
                    if (endBlock is null || endCaseBlock is null)
                    {
                        throw new System.NullReferenceException();
                    }

                    // Check that we're not detecting the same switch twice (e.g. due to a break/continue statement)
                    if (ctx.SwitchEndNodes!.Contains(endNode))
                    {
                        continue;
                    }
                    if (ctx.SwitchContinueBlocks!.Contains(endBlock))
                    {
                        continue;
                    }

                    // We found the end of a switch statement - add it to our list
                    ctx.SwitchEndNodes.Add(endNode);

                    // Create detection data
                    SwitchDetectionData data = new(endBlock, endNode, mayBeMisdetected);
                    ctx.SwitchData!.Add(data);

                    // Update index for next iteration (to be after the end case node's block index).
                    int endCaseBlockIndex =
                        fragment.Blocks.BinarySearch(i, fragment.Blocks.Count - i, endCaseBlock, BlockIndexComparer.Instance);
                    i = endCaseBlockIndex;

                    // Check if we have a continue block immediately preceding this end block
                    if (endBlock.BlockIndex == 0)
                    {
                        continue;
                    }
                    Block previousBlock = blocks[endBlock.BlockIndex - 1];
                    if (previousBlock.Instructions is not
                        [{ Kind: IGMInstruction.Opcode.PopDelete }, { Kind: IGMInstruction.Opcode.Branch }])
                    {
                        continue;
                    }

                    // This block should be a continue block, but additionally check we have a branch around it
                    if (previousBlock.BlockIndex == 0)
                    {
                        continue;
                    }
                    Block previousPreviousBlock = blocks[previousBlock.BlockIndex - 1];
                    if (previousPreviousBlock.Instructions is not [.., { Kind: IGMInstruction.Opcode.Branch }])
                    {
                        continue;
                    }
                    if (previousPreviousBlock.Successors.Count != 1 || previousPreviousBlock.Successors[0] != endBlock)
                    {
                        continue;
                    }

                    // This is definitely a switch continue block
                    ctx.SwitchContinueBlocks.Add(previousBlock);
                    ctx.SwitchIgnoreJumpBlocks!.Add(previousPreviousBlock);
                    data.ContinueBlock = previousBlock;
                    data.ContinueSkipBlock = previousPreviousBlock;
                }
            }
        }
    }

    private static void DetailPass(DecompileContext ctx)
    {
        List<Block> blocks = ctx.Blocks!;

        foreach (SwitchDetectionData data in ctx.SwitchData!)
        {
            // Find first predecessor that ends in Branch (should be the first one that *doesn't* end in BranchTrue)
            Block? firstBranchPredecessor = null;
            foreach (IControlFlowNode pred in data.EndNode.Predecessors)
            {
                if (pred is Block predBlock && predBlock.Instructions is [.., { Kind: IGMInstruction.Opcode.Branch }])
                {
                    firstBranchPredecessor = predBlock;
                    break;
                }
            }
            if (firstBranchPredecessor is null)
            {
                throw new DecompilerException("Failed to find end of switch cases");
            }

            // Need to detect whether or not we have a default case in this switch.
            // If previous block ends with Branch, then:
            //  - If it branches beyond the end of the switch (or backwards), then it can't be the default branch itself.
            //    Also, if the current block is a switch end block, the previous block is also not a default branch...
            //      -> Fall into case where previous block doesn't end with Branch
            //  - If it branches into the switch, then it's clearly the default branch
            // If the previous block doesn't end with Branch, then:
            //  - If the next block is Unreachable, and only contains Branch, then firstBranchPredecessor is the default branch
            //  - Otherwise, there's no default branch
            data.EndOfCaseBlock = firstBranchPredecessor;
            bool prevBlockIsDefaultBranch;
            if (firstBranchPredecessor.BlockIndex >= 1 && !ctx.SwitchEndNodes!.Contains(firstBranchPredecessor))
            {
                Block prevBlock = blocks[firstBranchPredecessor.BlockIndex - 1];
                if (prevBlock.Instructions is not [.., { Kind: IGMInstruction.Opcode.Branch }])
                {
                    prevBlockIsDefaultBranch = false;
                }
                else if (prevBlock.Successors[0].StartAddress > data.EndNode.StartAddress ||
                         prevBlock.Successors[0].StartAddress <= prevBlock.StartAddress)
                {
                    prevBlockIsDefaultBranch = false;
                }
                else
                {
                    prevBlockIsDefaultBranch = true;
                    data.DefaultBranchBlock = prevBlock;
                }
            }
            else
            {
                prevBlockIsDefaultBranch = false;
            }
            if (!prevBlockIsDefaultBranch)
            {
                Block nextBlock = blocks[firstBranchPredecessor.BlockIndex + 1];
                if (nextBlock.Unreachable && nextBlock.Instructions is [{ Kind: IGMInstruction.Opcode.Branch }])
                {
                    data.DefaultBranchBlock = firstBranchPredecessor;
                    data.EndOfCaseBlock = nextBlock;
                }
            }

            // Update list of blocks that we should ignore
            ctx.SwitchIgnoreJumpBlocks!.Add(data.EndOfCaseBlock);
            if (data.DefaultBranchBlock is not null)
            {
                ctx.SwitchIgnoreJumpBlocks.Add(data.DefaultBranchBlock);
            }
        }
    }

    /// <summary>
    /// Scans for all blocks representing the end of a switch statement, the "continue" block of a switch statement,
    /// as well as other important branch blocks that should not be touched by binary branch break/continue detection.
    /// Stores this data for later use when creating/inserting actual switch statements into the graph.
    /// </summary>
    public static void FindSwitchStatements(DecompileContext ctx)
    {
        ctx.SwitchEndNodes = [];
        ctx.SwitchData = [];
        ctx.SwitchContinueBlocks = [];
        ctx.SwitchIgnoreJumpBlocks = [];

        // First pass: simply detect the end blocks of switch statements, as well as continue blocks.
        // We do this first as this requires a special algorithm to prevent false positives/negatives.
        DetectionPass(ctx);

        // Second pass: find details about the remaining important blocks.
        // We do this second as this sometimes requires knowledge of surrounding switch statements.
        DetailPass(ctx);
    }

    /// <summary>
    /// Finds all switch statements in the code entry (given data from all the earlier control flow passes),
    /// and inserts them into the graph accordingly.
    /// </summary>
    public static List<Switch> InsertSwitchStatements(DecompileContext ctx)
    {
        List<Switch> res = [];

        for (int j = 0; j < ctx.SwitchData!.Count; j++)
        {
            SwitchDetectionData data = ctx.SwitchData[j];

            // Find all cases
            IControlFlowNode? currentNode = data.EndOfCaseBlock;
            List<Block> caseBranches = [];
            while (currentNode is not null)
            {
                if (currentNode is Block currentBlock)
                {
                    if (currentBlock.Instructions is [.., { Kind: IGMInstruction.Opcode.BranchTrue }])
                    {
                        // We've found a case!
                        caseBranches.Add(currentBlock);
                    }

                    if (ctx.SwitchEndNodes!.Contains(currentBlock))
                    {
                        // We're at the end of another switch statement - do not continue
                        break;
                    }
                }

                if (currentNode.Predecessors.Count != 1 ||
                    currentNode.StartAddress != currentNode.Predecessors[0].EndAddress)
                {
                    // We have either nowhere left to go, or a nonlinear branch here - do not continue
                    break;
                }

                currentNode = currentNode.Predecessors[0];
            }

            // Update graph for all cases (in reverse; we found them backwards)
            // First pass: update chain of conditions
            IControlFlowNode? startOfBody = null;
            IControlFlowNode? endCaseDestinations = null;
            IControlFlowNode? endCaseDestinationsEnd = null;
            List<IControlFlowNode> caseDestinationNodes = new(caseBranches.Count);
            for (int i = caseBranches.Count - 1; i >= 0; i--)
            {
                Block currentBlock = caseBranches[i];
                caseDestinationNodes.Add(currentBlock.Successors[1]);

                // Clear out the Compare & BranchTrue and replace it with a CaseJumpNode
                if (currentBlock.Instructions[^2].Kind != IGMInstruction.Opcode.Compare)
                {
                    throw new DecompilerException("Expected Compare instruction in switch case");
                }
                currentBlock.Instructions.RemoveRange(currentBlock.Instructions.Count - 2, 2);
                IControlFlowNode.DisconnectSuccessor(currentBlock, 1);
                CaseJumpNode caseJumpNode = new(currentBlock.EndAddress);
                IControlFlowNode.InsertSuccessor(currentBlock, 0, caseJumpNode);
                if (i == 0)
                {
                    // If we're the last case, disconnect the chain here
                    IControlFlowNode.DisconnectSuccessor(caseJumpNode, 0);
                }
            }
            // First pass (part two): also update default case
            IControlFlowNode? defaultDestinationNode = null;
            if (data.DefaultBranchBlock is not null)
            {
                Block defaultBlock = data.DefaultBranchBlock;
                defaultDestinationNode = defaultBlock.Successors[0];

                // Clear out Branch and disconnect successors (multiple successors because unreachable blocks are possible)
                defaultBlock.Instructions.RemoveAt(defaultBlock.Instructions.Count - 1);
                for (int i = defaultBlock.Successors.Count - 1; i >= 0; i--)
                {
                    IControlFlowNode.DisconnectSuccessor(defaultBlock, i);
                }
            }
            // Second pass: update destinations
            foreach (IControlFlowNode caseDestination in caseDestinationNodes)
            {
                // Insert case destination node before destination
                CaseDestinationNode caseDestNode = new(caseDestination.StartAddress);
                if (caseDestination.StartAddress >= (data.ContinueSkipBlock?.StartAddress ?? data.EndNode.StartAddress))
                {
                    // Our destination is at the very end of the switch statement
                    if (endCaseDestinations is null)
                    {
                        endCaseDestinations = caseDestNode;
                        endCaseDestinationsEnd = caseDestNode;
                    }
                    else
                    {
                        endCaseDestinationsEnd!.Successors.Add(caseDestNode);
                        caseDestNode.Predecessors.Add(endCaseDestinationsEnd);
                        endCaseDestinationsEnd = caseDestNode;
                    }
                }
                else
                {
                    IControlFlowNode.InsertPredecessorsAll(caseDestination, caseDestNode);

                    // Update the start of the switch body
                    if (startOfBody is null)
                    {
                        startOfBody = caseDestNode;
                    }
                    else if (caseDestNode.StartAddress < startOfBody.StartAddress)
                    {
                        startOfBody = caseDestNode;
                    }
                }
            }
            // Second pass (part two): update destination for default case
            if (defaultDestinationNode is not null)
            {
                // Insert default case destination node before destination
                CaseDestinationNode caseDestNode = new(defaultDestinationNode.StartAddress)
                {
                    IsDefault = true
                };
                if (defaultDestinationNode.StartAddress >= (data.ContinueSkipBlock?.StartAddress ?? data.EndNode.StartAddress))
                {
                    // Our destination is at the very end of the switch statement
                    if (endCaseDestinations is null)
                    {
                        endCaseDestinations = caseDestNode;
                    }
                    else
                    {
                        endCaseDestinationsEnd!.Successors.Add(caseDestNode);
                        caseDestNode.Predecessors.Add(endCaseDestinationsEnd);
                    }
                }
                else
                {
                    IControlFlowNode.InsertPredecessorsAll(defaultDestinationNode, caseDestNode);

                    // Update the start of the switch body
                    if (startOfBody is null)
                    {
                        startOfBody = caseDestNode;
                    }
                    else if (caseDestNode.StartAddress < startOfBody.StartAddress)
                    {
                        startOfBody = caseDestNode;
                    }
                }
            }

            // Remove branch from end of case block
            if (data.EndOfCaseBlock is not null)
            {
                Block endOfCaseBlock = data.EndOfCaseBlock;

                // Clear out Branch and disconnect successor
                endOfCaseBlock.Instructions.RemoveAt(endOfCaseBlock.Instructions.Count - 1);
                IControlFlowNode.DisconnectSuccessor(endOfCaseBlock, 0);
            }

            // Remove continue block (and branch around it) if it exists
            if (data.ContinueBlock is not null)
            {
                Block continueBlock = data.ContinueBlock;
                continueBlock.Instructions.Clear();
                for (int i = continueBlock.Predecessors.Count - 1; i >= 0; i--)
                {
                    IControlFlowNode.DisconnectPredecessor(continueBlock, i);
                }
                for (int i = continueBlock.Successors.Count - 1; i >= 0; i--)
                {
                    IControlFlowNode.DisconnectSuccessor(continueBlock, i);
                }

                Block skipContinueBlock = data.ContinueSkipBlock!;
                skipContinueBlock.Instructions.RemoveAt(skipContinueBlock.Instructions.Count - 1);
                for (int i = skipContinueBlock.Predecessors.Count - 1; i >= 0; i--)
                {
                    IControlFlowNode.DisconnectPredecessor(skipContinueBlock, i);
                }
                for (int i = skipContinueBlock.Successors.Count - 1; i >= 0; i--)
                {
                    IControlFlowNode.DisconnectSuccessor(skipContinueBlock, i);
                }
            }

            // Disconnect all branches going into the end node, and remove PopDelete
            data.EndBlock.Instructions.RemoveAt(0);
            IControlFlowNode endNode = data.EndNode;
            while (endNode.Parent is not null)
            {
                endNode = endNode.Parent;
            }
            for (int i = endNode.Predecessors.Count - 1; i >= 0; i--)
            {
                IControlFlowNode.DisconnectPredecessor(endNode, i);
            }

            // Construct actual switch node
            Block startOfStatement = (caseBranches.Count > 0) ? caseBranches[^1] : (data.DefaultBranchBlock ?? data.EndOfCaseBlock)!;
            Switch switchNode = 
                new(startOfStatement.StartAddress, endNode.StartAddress, startOfStatement, startOfBody, endCaseDestinations, data);
            IControlFlowNode.InsertStructure(startOfStatement, endNode, switchNode);
            res.Add(switchNode);

            // Update parent status of Cases/Body
            switchNode.Parent = startOfStatement.Parent;
            startOfStatement.Parent = switchNode;
            if (startOfBody is not null)
            {
                startOfBody.Parent = switchNode;
            }
        }

        // Resolve remaining continue statements
        Branches.ResolveRemainingExternalJumps(ctx);

        ctx.SwitchNodes = res;
        return res;
    }

    public void BuildAST(ASTBuilder builder, List<IStatementNode> output)
    {
        // Begin new switch case queue for this statement
        var prevSwitchCases = builder.SwitchCases;
        builder.SwitchCases = new(8);

        // Evaluate case expressions
        builder.BuildArbitrary(Cases, output, 1);

        if (DetectionData.MayBeMisdetected && builder.ExpressionStack.Count == 0)
        {
            // If we have an empty stack at this point, we presume we're a misdetected switch statement.
            // This should only ever happen with an empty switch (possibly with a default branch).
            // We turn into one or two continue statements.
            output.Add(new AST.ContinueNode());
            if (DetectionData.DefaultBranchBlock is not null)
            {
                // We have a default case, which is actually just a second continue.
                output.Add(new AST.ContinueNode());
            }

            // Check if our continue statements go forwards or backwards, based on the end node
            IControlFlowNode endNode = DetectionData.EndNode;
            while (endNode.Parent is not null)
            {
                endNode = endNode.Parent;
            }
            bool forward = (endNode.StartAddress > DetectionData.EndOfCaseBlock!.EndAddress - 4);

            // Now, we check our surrounding loop to see if it needs to be transformed between while/for.
            Loop? loop = builder.TopFragmentContext!.SurroundingLoop;
            if (loop is WhileLoop whileLoop)
            {
                if (forward)
                {
                    // Must be a for loop
                    whileLoop.ForLoopIncrementor = endNode;
                }
                else
                {
                    // Must be a while loop
                    whileLoop.MustBeWhileLoop = true;
                }
            }

            // Don't continue processing this "switch statement" any further - it's not one.
            return;
        }

        // All that's left on stack is the expression we're switching on
        IExpressionNode expression = builder.ExpressionStack.Pop();

        // Evaluate block
        BlockNode body = builder.BuildBlock(Body);
        body.UseBraces = true;
        body.PartOfSwitch = true;

        // Evaluate end case destinations
        body.Children.AddRange(builder.BuildBlock(EndCaseDestinations).Children);

        // Add statement
        output.Add(new SwitchNode(expression, body));

        // Restore previous switch case queue
        builder.SwitchCases = prevSwitchCases;
    }
}
