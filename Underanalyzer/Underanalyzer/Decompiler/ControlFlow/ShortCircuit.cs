/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;
using Underanalyzer.Decompiler.AST;

namespace Underanalyzer.Decompiler.ControlFlow;

public enum ShortCircuitType
{
    And,
    Or
}

internal class ShortCircuit(int startAddress, int endAddress, ShortCircuitType logicKind, List<IControlFlowNode> children) 
    : IControlFlowNode
{
    public int StartAddress { get; private set; } = startAddress;

    public int EndAddress { get; private set; } = endAddress;

    public List<IControlFlowNode> Predecessors { get; } = [];

    public List<IControlFlowNode> Successors { get; } = [];

    public IControlFlowNode? Parent { get; set; } = null;

    public List<IControlFlowNode?> Children { get; } = children!;

    public bool Unreachable { get; set; } = false;

    public ShortCircuitType LogicKind { get; } = logicKind;

    /// <summary>
    /// Locates all blocks where a short circuit "ends", storing them on the context for later processing.
    /// </summary>
    public static void FindShortCircuits(DecompileContext ctx)
    {
        List<Block> blocks = ctx.Blocks!;
        bool oldBytecodeVersion = ctx.OlderThanBytecode15;

        ctx.ShortCircuitBlocks = [];

        // Identify and restructure short circuits
        foreach (var block in blocks)
        {
            // Match push.e (or on old versions, pushi.e) instruction, standalone in a block
            if (oldBytecodeVersion &&
                    block is
                    {
                        Instructions: [
                        {
                            Kind: IGMInstruction.Opcode.PushImmediate,
                            Type1: IGMInstruction.DataType.Int16
                        }]
                    }
                    ||
                !oldBytecodeVersion &&
                    block is
                    {
                        Instructions: [
                        {
                            Kind: IGMInstruction.Opcode.Push,
                            Type1: IGMInstruction.DataType.Int16
                        }]
                    })
            {
                ctx.ShortCircuitBlocks.Add(block);
            }
        }
    }

    /// <summary>
    /// Inserts all short-circuit operations contained within a list of blocks, updating the control flow graph accordingly.
    /// </summary>
    public static List<ShortCircuit> InsertShortCircuits(DecompileContext ctx)
    {
        List<ShortCircuit> shortCircuits = [];

        // Identify and restructure short circuits
        foreach (var block in ctx.ShortCircuitBlocks!)
        {
            // Add child nodes
            List<IControlFlowNode> children = [block.Predecessors[0]];
            for (int i = 0; i < block.Predecessors.Count; i++)
            {
                // Connect to the next condition (the non-branch path from the previous condition)
                children.Add(block.Predecessors[i].Successors[0]);
            }

            // Create actual node
            ShortCircuitType logicKind = block.Instructions[0].ValueShort == 0 ? ShortCircuitType.And : ShortCircuitType.Or;
            ShortCircuit sc = new(children[0].StartAddress, block.EndAddress, logicKind, children);
            shortCircuits.Add(sc);

            // Remove branches and connections from previous blocks (not necessarily children!)
            for (int i = block.Predecessors.Count - 1; i >= 0; i--)
            {
                Block pred = block.Predecessors[i] as Block ?? throw new DecompilerException("Expected predecessor to be block");
                pred.Instructions.RemoveAt(pred.Instructions.Count - 1);
                IControlFlowNode.DisconnectSuccessor(pred, 1);
                IControlFlowNode.DisconnectSuccessor(pred, 0);
            }
            Block finalBlock = ctx.Blocks![block.BlockIndex - 1];
            finalBlock.Instructions.RemoveAt(finalBlock.Instructions.Count - 1);
            IControlFlowNode.DisconnectSuccessor(finalBlock, 0);

            // Remove original push instruction that was detected
            block.Instructions.RemoveAt(0);

            // Update overarching control flow
            IControlFlowNode.InsertStructure(children[0], block.Successors[0], sc);

            // Update parent status of the first child, as well as this loop, for later operation
            sc.Parent = children[0].Parent;
            children[0].Parent = sc;

            // Update parent status of remaining children, so they can be later updated if necessary
            for (int i = 1; i < children.Count; i++)
                children[i].Parent = sc;
        }

        ctx.ShortCircuitNodes = shortCircuits;
        return shortCircuits;
    }

    public override string ToString()
    {
        return $"{nameof(ShortCircuit)} (start address {StartAddress}, end address {EndAddress}, {Predecessors.Count} predecessors, {Successors.Count} successors)";
    }

    public void BuildAST(ASTBuilder builder, List<IStatementNode> output)
    {
        List<IExpressionNode> conditions = new(Children.Count);

        // Build first condition (it may already be on expression stack, though)
        builder.BuildArbitrary(Children[0], output, -1);
        conditions.Add(builder.ExpressionStack.Pop());

        // Build the rest of the conditions
        for (int i = 1; i < Children.Count; i++)
        {
            conditions.Add(builder.BuildExpression(Children[i]));
        }

        builder.ExpressionStack.Push(new ShortCircuitNode(conditions, LogicKind));
    }
}
