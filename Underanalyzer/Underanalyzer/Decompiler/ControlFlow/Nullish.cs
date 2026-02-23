/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;
using Underanalyzer.Decompiler.AST;

namespace Underanalyzer.Decompiler.ControlFlow;

internal sealed class Nullish : IControlFlowNode
{
    public enum NullishType
    {
        Expression,
        Assignment
    }

    public int StartAddress { get; private set; }

    public int EndAddress { get; private set; }

    public List<IControlFlowNode> Predecessors { get; } = [];

    public List<IControlFlowNode> Successors { get; } = [];

    public IControlFlowNode? Parent { get; set; } = null;

    public List<IControlFlowNode?> Children { get; } = [null];

    public bool Unreachable { get; set; } = false;

    public NullishType NullishKind { get; }

    /// <summary>
    /// The node that gets executed if the predecessor has a nullish value on the top of the stack after it.
    /// </summary>
    /// <remarks>
    /// Upon being processed, this has its predecessors disconnected. 
    /// All paths exiting from it are also isolated from the external graph.
    /// </remarks>
    public IControlFlowNode IfNullish { get => Children[0]!; private set => Children[0] = value; }

    public Nullish(int startAddress, int endAddress, NullishType nullishKind, IControlFlowNode ifNullishNode)
    {
        StartAddress = startAddress;
        EndAddress = endAddress;
        NullishKind = nullishKind;
        IfNullish = ifNullishNode;
    }

    /// <summary>
    /// Finds all nullish operations present in a list of blocks, and updates the control flow graph accordingly.
    /// </summary>
    public static List<Nullish> FindNullish(DecompileContext ctx)
    {
        List<Block> blocks = ctx.Blocks!;

        List<Nullish> res = [];

        for (int j = blocks.Count - 1; j >= 0; j--)
        {
            // Search for pattern
            Block block = blocks[j];
            if (block.Instructions is
                [..,
                { Kind: IGMInstruction.Opcode.Extended, ExtKind: IGMInstruction.ExtendedOpcode.IsNullishValue },
                { Kind: IGMInstruction.Opcode.BranchFalse }
                ])
            {
                Block ifNullishBlock = block.Successors[0] as Block ?? throw new DecompilerException("Expected first successor to be block");
                Block afterBlock = block.Successors[1] as Block ?? throw new DecompilerException("Expected second successor to be block");

                // Determine nullish type by using the block "after"
                NullishType nullishKind = NullishType.Expression;
                if (afterBlock.Instructions is [{ Kind: IGMInstruction.Opcode.PopDelete }, ..])
                {
                    nullishKind = NullishType.Assignment;
                }

                Nullish n = new(block.EndAddress, afterBlock.StartAddress, nullishKind, ifNullishBlock);
                res.Add(n);

                // Remove instructions from this block
                block.Instructions.RemoveRange(block.Instructions.Count - 2, 2);

                // Remove pop instruction from "if nullish" block
                ifNullishBlock.Instructions.RemoveAt(0);

                Block? endOfNullishBlock = null;
                if (nullishKind == NullishType.Assignment)
                {
                    // Remove pop instruction from "after" block
                    afterBlock.Instructions.RemoveAt(0);

                    // Our "end of nullish" block is always before the "after" block.
                    // Remove its branch instruction.
                    endOfNullishBlock = blocks[afterBlock.BlockIndex - 1];
                    endOfNullishBlock.Instructions.RemoveAt(endOfNullishBlock.Instructions.Count - 1);
                }

                // Disconnect sections of graph
                IControlFlowNode.DisconnectPredecessor(ifNullishBlock, 0);
                if (nullishKind == NullishType.Expression)
                {
                    EmptyNode emptyDest = new(afterBlock.StartAddress);
                    for (int i = 0; i < afterBlock.Predecessors.Count; i++)
                    {
                        IControlFlowNode afterPred = afterBlock.Predecessors[i];
                        if (afterPred.StartAddress >= block.EndAddress)
                        {
                            // Route all nodes going into after into a unique empty node
                            IControlFlowNode.ReplaceConnections(afterPred.Successors, afterBlock, emptyDest);
                            emptyDest.Predecessors.Add(afterPred);
                            afterBlock.Predecessors.RemoveAt(i);
                            i--;
                        }
                    }
                }
                IControlFlowNode.DisconnectSuccessor(block, 0);
                if (endOfNullishBlock is not null)
                    IControlFlowNode.DisconnectSuccessor(endOfNullishBlock, 0);

                // Insert new node into graph
                block.Successors.Add(n);
                n.Predecessors.Add(block);
                n.Successors.Add(afterBlock);
                afterBlock.Predecessors.Add(n);

                // Update parent status of "if nullish" block
                if (ifNullishBlock.Parent is not null)
                {
                    throw new DecompilerException("Expected IfNullish block to be null");
                }
                ifNullishBlock.Parent = n;
            }
        }

        ctx.NullishNodes = res;
        return res;
    }

    public void BuildAST(ASTBuilder builder, List<IStatementNode> output)
    {
        if (NullishKind == NullishType.Expression)
        {
            IExpressionNode left = builder.ExpressionStack.Pop();
            IExpressionNode right = builder.BuildExpression(IfNullish);
            builder.ExpressionStack.Push(new NullishCoalesceNode(left, right));
        }
        else
        {
            // Pop off existing variable from stack
            builder.ExpressionStack.Pop();

            // Read assignment statement from branch
            BlockNode rightBlock = builder.BuildBlock(IfNullish);
            if (rightBlock.Children is not [AssignNode rightAssign])
            {
                throw new DecompilerException("Expected assignment in nullish-coalescing assignment");
            }

            // Modify and output assignment
            rightAssign.AssignKind = AssignNode.AssignType.NullishCoalesce;
            output.Add(rightAssign);
        }
    }
}
