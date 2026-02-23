/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;
using Underanalyzer.Decompiler.AST;
using static Underanalyzer.IGMInstruction;

namespace Underanalyzer.Decompiler.ControlFlow;

/// <summary>
/// Represents a static initialization block for a function.
/// </summary>
internal sealed class StaticInit : IControlFlowNode
{
    public int StartAddress { get; private set; }

    public int EndAddress { get; private set; }

    public List<IControlFlowNode> Predecessors { get; } = [];

    public List<IControlFlowNode> Successors { get; } = [];

    public IControlFlowNode? Parent { get; set; } = null;

    public List<IControlFlowNode?> Children { get; } = [null];

    public bool Unreachable { get; set; } = false;

    /// <summary>
    /// The top of the static initialization block.
    /// </summary>
    /// <remarks>
    /// Upon being processed, this has its predecessors disconnected.
    /// </remarks>
    public IControlFlowNode Head { get => Children[0]!; private set => Children[0] = value; }

    public StaticInit(int startAddress, int endAddress, IControlFlowNode head)
    {
        StartAddress = startAddress;
        EndAddress = endAddress;
        Head = head;
    }

    /// <summary>
    /// Finds all static initialization blocks present in a list of blocks, and updates the control flow graph accordingly.
    /// </summary>
    public static List<StaticInit> FindStaticInits(DecompileContext ctx)
    {
        List<Block> blocks = ctx.Blocks!;

        List<StaticInit> res = [];

        foreach (var block in blocks)
        {
            // Check for pattern
            if (block.Instructions is [..,
                { Kind: Opcode.Extended, ExtKind: ExtendedOpcode.HasStaticInitialized },
                { Kind: Opcode.BranchTrue }])
            {
                StaticInit si = new(block.EndAddress, block.Successors[1].StartAddress, block.Successors[0]);
                res.Add(si);

                // Remove instructions from this block
                block.Instructions.RemoveRange(block.Instructions.Count - 2, 2);

                // Remove instruction from ending block, if it's the right one (changes depending on version)
                IControlFlowNode afterNode = block.Successors[1];
                if (afterNode is Block { Instructions: [{ Kind: Opcode.Extended, ExtKind: ExtendedOpcode.SetStaticInitialized }, ..] } afterBlock)
                {
                    afterBlock.Instructions.RemoveAt(0);
                }

                // Disconnect predecessors of the head and our after block
                IControlFlowNode.DisconnectPredecessor(si.Head, 0);
                IControlFlowNode.DisconnectPredecessor(afterNode, 1);
                IControlFlowNode.DisconnectPredecessor(afterNode, 0);

                // Insert into control flow graph (done manually, here)
                block.Successors.Add(si);
                si.Predecessors.Add(block);
                si.Successors.Add(afterNode);
                afterNode.Predecessors.Add(si);

                // Update parent status of head and this structure
                si.Parent = si.Head.Parent;
                si.Head.Parent = si;
            }
        }

        ctx.StaticInitNodes = res;
        return res;
    }

    public void BuildAST(ASTBuilder builder, List<IStatementNode> output)
    {
        output.Add(new StaticInitNode(builder.BuildBlock(Head)));
    }
}
