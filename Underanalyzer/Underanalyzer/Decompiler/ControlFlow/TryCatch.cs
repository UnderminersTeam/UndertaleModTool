/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;
using Underanalyzer.Decompiler.AST;

namespace Underanalyzer.Decompiler.ControlFlow;

/// <summary>
/// Represents a try..catch statement in GML code.
/// Notably, this does NOT include the "finally" block, which is detected later on in the process.
/// </summary>
internal sealed class TryCatch : IControlFlowNode
{
    public int StartAddress { get; private set; }

    public int EndAddress { get; private set; }

    public List<IControlFlowNode> Predecessors { get; } = [];

    public List<IControlFlowNode> Successors { get; } = [];

    public IControlFlowNode? Parent { get; set; } = null;

    public List<IControlFlowNode?> Children { get; } = [null, null];

    public bool Unreachable { get; set; } = false;

    /// <summary>
    /// The "try" block of the try statement.
    /// </summary>
    /// <remarks>
    /// Upon being processed, this has its predecessors disconnected. 
    /// All paths exiting from it are also isolated from the external graph.
    /// </remarks>
    public IControlFlowNode Try { get => Children[0]!; private set => Children[0] = value; }

    /// <summary>
    /// The "catch" block of the try statement, or <see langword="null"/> if none exists.
    /// </summary>
    /// <remarks>
    /// Upon being processed, this has its predecessors disconnected.
    /// All paths exiting from it are also isolated from the external graph.
    /// </remarks>
    public IControlFlowNode? Catch { get => Children[1]; private set => Children[1] = value; }

    public TryCatch(int startAddress, int endAddress, IControlFlowNode tryNode, IControlFlowNode? catchNode)
    {
        StartAddress = startAddress;
        EndAddress = endAddress;
        Try = tryNode;
        Catch = catchNode;
    }

    /// <summary>
    /// Finds all try/catch statements present in a list of blocks, and updates the control flow graph accordingly.
    /// </summary>
    public static List<TryCatch> FindTryCatch(DecompileContext ctx)
    {
        List<Block> blocks = ctx.Blocks!;

        List<TryCatch> res = [];

        foreach (var block in blocks)
        {
            // Check for start block
            if (block.Instructions.Count == 6)
            {
                IGMInstruction call = block.Instructions[^2];
                if (call.Kind == IGMInstruction.Opcode.Call &&
                    call.TryFindFunction(ctx.GameContext)?.Name?.Content == VMConstants.TryHookFunction)
                {
                    // Get components of our try..catch statement
                    IControlFlowNode tryNode = block.Successors[0];
                    IControlFlowNode? catchNode = block.Successors.Count >= 3 ? block.Successors[2] : null;
                    Block endBlock = block.Successors[1] as Block ?? throw new DecompilerException("Expected second successor to be block");

                    TryCatch tc = new(block.StartAddress, endBlock.StartAddress, tryNode, catchNode);
                    res.Add(tc);

                    // Remove predecessor of try node
                    IControlFlowNode.DisconnectPredecessor(tryNode, 0);

                    if (catchNode is not null)
                    {
                        // Remove predecessor of catch node
                        IControlFlowNode.DisconnectPredecessor(catchNode, 0);

                        // Remove branch instruction from end node's second predecessor, i.e.
                        // the end of the try block
                        Block tryEndBlock = endBlock.Predecessors[1] as Block ?? throw new DecompilerException("Expected second predecessor to be block");
                        if (tryEndBlock == block || tryEndBlock.StartAddress >= catchNode.StartAddress)
                        {
                            throw new DecompilerException("Failed to find end of try block");
                        }
                        if (tryEndBlock.Instructions.Count < 1 ||
                            tryEndBlock.Instructions[^1].Kind != IGMInstruction.Opcode.Branch)
                        {
                            throw new DecompilerException("Expected Branch at end of try block");
                        }
                        tryEndBlock.Instructions.RemoveAt(tryEndBlock.Instructions.Count - 1);

                        // Remove instructions from the end of the catch block
                        Block catchEndBlock = blocks[endBlock.BlockIndex - 1];
                        if (catchEndBlock.Instructions is not
                            [.., { Kind: IGMInstruction.Opcode.Call }, { Kind: IGMInstruction.Opcode.PopDelete },
                            { Kind: IGMInstruction.Opcode.Branch }])
                        {
                            throw new DecompilerException("Expected finish catch and Branch at end of catch block");
                        }
                        catchEndBlock.Instructions.RemoveRange(catchEndBlock.Instructions.Count - 3, 3);

                        // Reroute end of the catch block into our end node (temporarily)
                        IControlFlowNode.DisconnectSuccessor(catchEndBlock, 0);
                        catchEndBlock.Successors.Add(endBlock);
                        endBlock.Predecessors.Add(catchEndBlock);
                    }

                    // Disconnect start node from end node.
                    // Try to search for the start node as a predecessor (as a safe fix for with loops inside of a try statement)
                    int startNodePredecessorIndex = endBlock.Predecessors.IndexOf(block);
                    IControlFlowNode.DisconnectPredecessor(endBlock, startNodePredecessorIndex >= 0 ? startNodePredecessorIndex : 0);

                    // Add new empty node to act as a meet point for both try and catch blocks
                    EmptyNode empty = new(endBlock.StartAddress);
                    IControlFlowNode.InsertPredecessors(endBlock, empty, tc.StartAddress);

                    // Disconnect new empty node from the end node
                    IControlFlowNode.DisconnectSuccessor(empty, 0);

                    // Remove instructions from initial block
                    block.Instructions.Clear();

                    // Remove try unhook instructions from end node
                    if (endBlock.Instructions.Count == 0 ||
                        endBlock.Instructions[0].Kind != IGMInstruction.Opcode.Call ||
                        endBlock.Instructions[0].TryFindFunction(ctx.GameContext)?.Name?.Content != VMConstants.TryUnhookFunction)
                    {
                        throw new DecompilerException("Expected try unhook in end node");
                    }
                    endBlock.Instructions.RemoveRange(0, 2);

                    // Insert into graph (manually, here)
                    if (block.Successors.Count != 0)
                    {
                        throw new DecompilerException("Expected no successors for try start block");
                    }
                    block.Successors.Add(tc);
                    tc.Predecessors.Add(block);
                    if (endBlock.Predecessors.Count != 0)
                    {
                        throw new DecompilerException("Expected no predecessors for try end block");
                    }
                    tc.Successors.Add(endBlock);
                    endBlock.Predecessors.Add(tc);

                    continue;
                }
            }

            // Check for finally block
            if (block.Instructions.Count >= 3)
            {
                IGMInstruction call = block.Instructions[^3];
                if (call.Kind == IGMInstruction.Opcode.Call &&
                    call.TryFindFunction(ctx.GameContext)?.Name?.Content == VMConstants.FinishFinallyFunction)
                {
                    // Remove redundant branch instruction for later operation.
                    // We leave final blocks for post-processing on the syntax tree due to complexity.
                    if (block.Instructions[^1].Kind != IGMInstruction.Opcode.Branch)
                    {
                        throw new DecompilerException("Expected Branch after finally block");
                    }
                    block.Instructions.RemoveAt(block.Instructions.Count - 1);
                }
            }
        }

        ctx.TryCatchNodes = res;
        return res;
    }

    /// <summary>
    /// Removes all branches to any try statement's immediate successor that is not the try statement itself.
    /// This can occur when certain control flow is placed at the end of try block, with try/finally (without catch).
    /// This should be called after all branches have been resolved.
    /// </summary>
    public static void CleanTryEndBranches(DecompileContext ctx)
    {
        foreach (TryCatch tc in ctx.TryCatchNodes!)
        {
            // Only process if we have 1 successor (and more than 1 is an error at this point)
            if (tc.Successors.Count == 0)
            {
                continue;
            }
            if (tc.Successors.Count > 1)
            {
                throw new DecompilerException("Unexpected branch");
            }

            // Remove predecessors from the successor that are not this node
            IControlFlowNode succ = tc.Successors[0];
            for (int i = succ.Predecessors.Count - 1; i >= 0; i--)
            {
                if (succ.Predecessors[i] != tc)
                {
                    IControlFlowNode.DisconnectPredecessor(succ, i);
                }
            }
        }
    }

    public void BuildAST(ASTBuilder builder, List<IStatementNode> output)
    {
        // Build try block - but first follow all parents (e.g., in case a binary branch shows up at the start)
        IControlFlowNode tryNode = Try;
        while (tryNode.Parent is not null)
        {
            tryNode = tryNode.Parent;
        }
        BlockNode tryBlock = builder.BuildBlock(tryNode);

        // Handle catch block, if it exists
        BlockNode? catchBlock = null;
        VariableNode? catchVariable = null;
        if (Catch is not null)
        {
            // Get variable from start of catch's initial block
            Block catchInstrBlock = builder.Context.BlocksByAddress![Catch.StartAddress];
            if (catchInstrBlock.Instructions is not [{ Kind: IGMInstruction.Opcode.Pop} popInstr, ..] ||
                popInstr.TryFindVariable(builder.Context.GameContext) is not IGMVariable { Name.Content: string variableName } variable)
            {
                throw new DecompilerException("Expected first instruction of catch block to store to variable");
            }
            catchVariable = new VariableNode(variable, IGMInstruction.VariableType.Normal, new InstanceTypeNode(IGMInstruction.InstanceType.Local));
            catchInstrBlock.Instructions.RemoveAt(0);

            // Register this as a local variable, but not to local variable declaration list
            builder.LocalVariableNames.Add(variableName);

            // Build actual catch body - also follow any parents, as needed
            IControlFlowNode catchNode = Catch;
            while (catchNode.Parent is not null)
            {
                catchNode = catchNode.Parent;
            }
            catchBlock = builder.BuildBlock(catchNode);
        }

        output.Add(new TryCatchNode(tryBlock, catchBlock, catchVariable));
    }
}
