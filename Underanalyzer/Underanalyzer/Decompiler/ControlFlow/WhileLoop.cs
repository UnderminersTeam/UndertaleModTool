/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using Underanalyzer.Decompiler.AST;
using static Underanalyzer.IGMInstruction;

namespace Underanalyzer.Decompiler.ControlFlow;

/// <summary>
/// Represents a "while" loop in a control flow graph.
/// Can become a "for" loop as needed or desired, depending on the code.
/// </summary>
internal sealed class WhileLoop : Loop
{
    public override List<IControlFlowNode?> Children { get; } = [null, null, null, null, null];

    /// <summary>
    /// The top loop point of the while loop. This is where the loop condition begins to be evaluated.
    /// </summary>
    /// <remarks>
    /// Upon being processed, this becomes disconnected from the rest of the graph.
    /// </remarks>
    public IControlFlowNode Head { get => Children[0]!; private set => Children[0] = value; }

    /// <summary>
    /// The bottom loop point of the while loop. This is where the jump back to the loop head/condition is located.
    /// </summary>
    /// <remarks>
    /// Upon being processed, this becomes disconnected from the rest of the graph.
    /// </remarks>
    public IControlFlowNode Tail { get => Children[1]!; private set => Children[1] = value; }

    /// <summary>
    /// The "sink" location of the loop. The loop condition being false or "break" statements will lead to this location.
    /// </summary>
    /// <remarks>
    /// Upon being processed, this becomes a new <see cref="EmptyNode"/>, which is then disconnected from the external graph.
    /// </remarks>
    public IControlFlowNode After { get => Children[2]!; private set => Children[2] = value; }

    /// <summary>
    /// The start of the body of the loop, as written in the source code. That is, this does not include the loop condition.
    /// </summary>
    /// <remarks>
    /// Upon being processed, this is disconnected from the loop condition (which is otherwise a predecessor).
    /// </remarks>
    public IControlFlowNode? Body { get => Children[3]; private set => Children[3] = value; }

    /// <summary>
    /// If not null, then it was detected that this while loop must be written as a for loop.
    /// This can occur when "continue" statements are used within the loop, which otherwise
    /// could not be written using normal if/else statements.
    /// This points to the start of the "incrementing" code of the for loop.
    /// </summary>
    public IControlFlowNode? ForLoopIncrementor { get => Children[4]; set => Children[4] = value; }

    /// <summary>
    /// If true, this loop was detected to definitively be a while loop.
    /// This currently happens if a "continue" statement is used within the while loop.
    /// </summary>
    public bool MustBeWhileLoop { get; set; } = false;

    public WhileLoop(int startAddress, int endAddress, int index, IControlFlowNode head, IControlFlowNode tail, IControlFlowNode after)
        : base(startAddress, endAddress, index)
    {
        Head = head;
        Tail = tail;
        After = after;
    }

    public override void UpdateFlowGraph()
    {
        // Get rid of jump from tail
        IControlFlowNode.DisconnectSuccessor(Tail, 0);
        Block tailBlock = Tail as Block ?? throw new DecompilerException("Expected tail to be block");
        tailBlock.Instructions.RemoveAt(tailBlock.Instructions.Count - 1);

        // Find first branch location after head
        Block? branchBlock = null;
        for (int i = 0; i < After.Predecessors.Count; i++)
        {
            if (After.Predecessors[i].StartAddress < Head.StartAddress ||
                After.Predecessors[i] is not Block b)
            {
                continue;
            }
            branchBlock = b;
            break;
        }
        if (branchBlock is null)
        {
            throw new DecompilerException("Failed to find first branch location after head");
        }
        if (branchBlock.Instructions[^1].Kind != IGMInstruction.Opcode.BranchFalse)
        {
            throw new DecompilerException("Expected BranchFalse in branch block - misidentified");
        }

        // Identify body node by using branch location's first target (the one that doesn't jump)
        Body = branchBlock.Successors[0];
        Body.Parent = this;

        // Get rid of jumps from branch location
        IControlFlowNode.DisconnectSuccessor(branchBlock, 1);
        IControlFlowNode.DisconnectSuccessor(branchBlock, 0);
        branchBlock.Instructions.RemoveAt(branchBlock.Instructions.Count - 1);

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
        return $"{nameof(WhileLoop)} (start address {StartAddress}, end address {EndAddress}, {Predecessors.Count} predecessors, {Successors.Count} successors)";
    }

    public override void BuildAST(ASTBuilder builder, List<IStatementNode> output)
    {
        // Build loop condition
        IExpressionNode condition = builder.BuildExpression(Head, output);

        // If condition was an int16 that was directly used without conversion, that means it was a boolean
        if (condition is Int16Node { Value: 0 or 1, StackType: not DataType.Boolean } i16)
        {
            condition = new BooleanNode(i16.Value == 1)
            {
                StackType = i16.StackType
            };
        }

        // Push this loop context
        Loop? prevLoop = builder.TopFragmentContext!.SurroundingLoop;
        builder.TopFragmentContext.SurroundingLoop = this;

        // Build body and create a for/while loop statement (defaults to while if unknown)
        BlockNode body = builder.BuildBlockWhile(Body, this);
        if (ForLoopIncrementor is not null)
        {
            // For loop
            BlockNode incrementor = builder.BuildBlock(ForLoopIncrementor);
            output.Add(new ForLoopNode(null, condition, incrementor, body));
        }
        else
        {
            // While loop
            output.Add(new WhileLoopNode(condition, body, MustBeWhileLoop));
        }

        // Pop this loop context
        builder.TopFragmentContext.SurroundingLoop = prevLoop;

        // Sanity check
        if (MustBeWhileLoop && ForLoopIncrementor is not null)
        {
            throw new DecompilerException("Detected while loop as both for and while");
        }
    }
}
