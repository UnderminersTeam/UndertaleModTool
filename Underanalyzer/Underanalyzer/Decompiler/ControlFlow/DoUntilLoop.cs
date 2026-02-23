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
/// Represents a "do...until" loop node in a control flow graph.
/// </summary>
internal sealed class DoUntilLoop : Loop
{
    public override List<IControlFlowNode?> Children { get; } = [null, null, null];

    /// <summary>
    /// The top loop point and start of the loop body, as written in the source code.
    /// </summary>
    /// <remarks>
    /// Upon being processed, this is disconnected from its predecessors.
    /// </remarks>
    public IControlFlowNode Head { get => Children[0]!; private set => Children[0] = value; }

    /// <summary>
    /// The bottom loop point of the loop. This is where the loop condition and branch to the loop head is located.
    /// </summary>
    /// <remarks>
    /// Upon being processed, this is disconnected from its successors.
    /// </remarks>
    public IControlFlowNode Tail { get => Children[1]!; private set => Children[1] = value; }

    /// <summary>
    /// The "sink" location of the loop. The loop condition being false or "break" statements will lead to this location.
    /// </summary>
    /// <remarks>
    /// Upon being processed, this becomes a new <see cref="EmptyNode"/>, which is then disconnected from the external graph.
    /// </remarks>
    public IControlFlowNode After { get => Children[2]!; private set => Children[2] = value; }

    public DoUntilLoop(int startAddress, int endAddress, int index, IControlFlowNode head, IControlFlowNode tail, IControlFlowNode after)
        : base(startAddress, endAddress, index)
    {
        Head = head;
        Tail = tail;
        After = after;
    }

    public override void UpdateFlowGraph()
    {
        // Get rid of jumps from tail
        IControlFlowNode.DisconnectSuccessor(Tail, 1);
        IControlFlowNode.DisconnectSuccessor(Tail, 0);
        Block tailBlock = (Tail as Block)!;
        tailBlock.Instructions.RemoveAt(tailBlock.Instructions.Count - 1);

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
        return $"{nameof(DoUntilLoop)} (start address {StartAddress}, end address {EndAddress}, {Predecessors.Count} predecessors, {Successors.Count} successors)";
    }

    public override void BuildAST(ASTBuilder builder, List<IStatementNode> output)
    {
        // Push this loop context
        Loop? prevLoop = builder.TopFragmentContext!.SurroundingLoop;
        builder.TopFragmentContext.SurroundingLoop = this;

        // Build body
        int stackCountBefore = builder.ExpressionStack.Count;
        BlockNode body = builder.BuildBlock(Head);
        int stackCountAfter = builder.ExpressionStack.Count;

        // Verify that we created a loop condition while simulating the body
        if (stackCountAfter != stackCountBefore + 1)
        {
            throw new DecompilerException(
                $"Expected condition after do..until loop. Stack count: {stackCountBefore} -> {stackCountAfter}");
        }

        // Use newly-created condition from stack, and create statement
        IExpressionNode condition = builder.ExpressionStack.Pop();

        // If condition was an int16 that was directly used without conversion, that means it was a boolean
        if (condition is Int16Node { Value: 0 or 1, StackType: not DataType.Boolean } i16)
        {
            condition = new BooleanNode(i16.Value == 1)
            {
                StackType = i16.StackType
            };
        }

        // Add loop node to output
        output.Add(new DoUntilLoopNode(body, condition));

        // Pop this loop context
        builder.TopFragmentContext.SurroundingLoop = prevLoop;
    }
}
