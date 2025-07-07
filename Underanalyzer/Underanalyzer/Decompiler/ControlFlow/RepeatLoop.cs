/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;
using Underanalyzer.Decompiler.AST;

namespace Underanalyzer.Decompiler.ControlFlow;

/// <summary>
/// Represents a "repeat" loop in a control flow graph.
/// </summary>
internal class RepeatLoop : Loop
{
    public override List<IControlFlowNode?> Children { get; } = [null, null, null];

    /// <summary>
    /// The top loop point and body of the loop, as written in the source code.
    /// </summary>
    /// <remarks>
    /// Upon being processed, this has its predecessors disconnected.
    /// Of its predecessors, the instructions used to initialize the loop counter are removed.
    /// </remarks>
    public IControlFlowNode Head { get => Children[0]!; private set => Children[0] = value; }

    /// <summary>
    /// The bottom loop point of the loop, where the loop counter is decremented.
    /// </summary>
    /// <remarks>
    /// Upon being processed, all instructions pertaining to the loop counter are removed, and all successors are disconnected.
    /// </remarks>
    public IControlFlowNode Tail { get => Children[1]!; private set => Children[1] = value; }

    /// <summary>
    /// The "sink" location of the loop. The loop counter being falsey or "break" statements will lead to this location.
    /// </summary>
    /// <remarks>
    /// Upon being processed, this becomes a new <see cref="EmptyNode"/>, which is then disconnected from the external graph.
    /// Additionally, a final pop instruction is removed.
    /// </remarks>
    public IControlFlowNode After { get => Children[2]!; private set => Children[2] = value; }

    public RepeatLoop(int startAddress, int endAddress, int index, IControlFlowNode head, IControlFlowNode tail, IControlFlowNode after)
        : base(startAddress, endAddress, index)
    {
        Head = head;
        Tail = tail;
        After = after;
    }

    public override void UpdateFlowGraph()
    {
        // Get rid of branch (and unneeded logic) from branch into Head
        // The (first) predecessor of Head should always be a Block, as it has logic
        Block headPred = Head.Predecessors[0] as Block ?? throw new DecompilerException("Expected first predecessor to be block");
        headPred.Instructions.RemoveRange(headPred.Instructions.Count - 4, 4);
        IControlFlowNode.DisconnectSuccessor(headPred, 1);

        // Get rid of jumps (and unneeded logic) from Tail
        IControlFlowNode.DisconnectSuccessor(Tail, 1);
        IControlFlowNode.DisconnectSuccessor(Tail, 0);
        Block tailBlock = Tail as Block ?? throw new DecompilerException("Expected Tail to be block");
        if (tailBlock.Instructions is
            [.., { Kind: IGMInstruction.Opcode.Convert }, { Kind: IGMInstruction.Opcode.BranchTrue }])
        {
            // We have a Convert instruction before branching at the end (older GML output)
            tailBlock.Instructions.RemoveRange(tailBlock.Instructions.Count - 5, 5);
        }
        else
        {
            // We don't have any Convert instruction before branching at the end (more recent GML output)
            tailBlock.Instructions.RemoveRange(tailBlock.Instructions.Count - 4, 4);
        }

        // Remove unneeded logic from After (should also always be a Block)
        Block afterBlock = After as Block ?? throw new DecompilerException("Expected After to be block");
        afterBlock.Instructions.RemoveAt(0);

        // Add a new node that is branched to at the end, to keep control flow internal
        var oldAfter = After;
        var newAfter = new EmptyNode(After.StartAddress);
        IControlFlowNode.InsertPredecessors(After, newAfter, Head.EndAddress);
        newAfter.Parent = this;
        After = newAfter;

        // Insert structure into graph
        IControlFlowNode.InsertStructure(Head, oldAfter, this);

        // Update parent status of Head, as well as this loop, for later operation
        Parent = Head.Parent;
        Head.Parent = this;
    }

    public override string ToString()
    {
        return $"{nameof(RepeatLoop)} (start address {StartAddress}, end address {EndAddress}, {Predecessors.Count} predecessors, {Successors.Count} successors)";
    }

    public override void BuildAST(ASTBuilder builder, List<IStatementNode> output)
    {
        // The expression used in the loop header should already be on the stack
        IExpressionNode timesToRepeat = builder.ExpressionStack.Pop();

        // Push this loop context
        Loop? prevLoop = builder.TopFragmentContext!.SurroundingLoop;
        builder.TopFragmentContext.SurroundingLoop = this;

        // Build loop body, and create statement
        BlockNode body = builder.BuildBlock(Head);
        output.Add(new RepeatLoopNode(timesToRepeat, body));

        // Pop this loop context
        builder.TopFragmentContext.SurroundingLoop = prevLoop;
    }
}
