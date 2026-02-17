/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;
using Underanalyzer.Compiler.Bytecode;
using Underanalyzer.Compiler.Lexer;
using Underanalyzer.Compiler.Parser;
using static Underanalyzer.IGMInstruction;

namespace Underanalyzer.Compiler.Nodes;

/// <summary>
/// Represents a "repeat" loop in the AST.
/// </summary>
internal sealed class RepeatLoopNode : IASTNode
{
    /// <summary>
    /// Expression used for the number of times this repeat loop node repeats.
    /// </summary>
    public IASTNode TimesToRepeat { get; private set; }

    /// <summary>
    /// Body of the repeat loop node.
    /// </summary>
    public IASTNode Body { get; private set; }

    /// <inheritdoc/>
    public IToken? NearbyToken { get; }

    private RepeatLoopNode(IToken? token, IASTNode timesToRepeat, IASTNode body)
    {
        NearbyToken = token;
        TimesToRepeat = timesToRepeat;
        Body = body;
    }

    /// <summary>
    /// Creates a repeat loop node, parsing from the given context's current position.
    /// </summary>
    public static RepeatLoopNode? Parse(ParseContext context)
    {
        // Parse "repeat" keyword
        if (context.EnsureToken(KeywordKind.Repeat) is not TokenKeyword tokenKeyword)
        {
            return null;
        }

        // Parse loop repeat count expression
        if (Expressions.ParseExpression(context) is not IASTNode timesToRepeat)
        {
            return null;
        }

        // Parse loop body
        if (Statements.ParseStatement(context) is not IASTNode body)
        {
            return null;
        }

        // Create final statement
        return new RepeatLoopNode(tokenKeyword, timesToRepeat, body);
    }

    /// <inheritdoc/>
    public IASTNode PostProcess(ParseContext context)
    {
        // Enter repeat context
        bool previousProcessingSwitch = context.ProcessingSwitch;
        bool previousProcessingBreakContinueContext = context.CurrentScope.ProcessingBreakContinueContext;
        bool previousShouldGenerateBreakContinueCode = true;
        context.ProcessingSwitch = false;
        context.CurrentScope.ProcessingBreakContinueContext = true;
        if (context.TryStatementContext is TryStatementContext tryContext)
        {
            // Entering repeat loop, so break/continue code should no longer be generated.
            previousShouldGenerateBreakContinueCode = tryContext.ShouldGenerateBreakContinueCode;
            tryContext.ShouldGenerateBreakContinueCode = false;
        }

        // Normal post-processing
        TimesToRepeat = TimesToRepeat.PostProcess(context);
        Body = Body.PostProcess(context);

        // Exit repeat context
        context.ProcessingSwitch = previousProcessingSwitch;
        context.CurrentScope.ProcessingBreakContinueContext = previousProcessingBreakContinueContext;
        if (context.TryStatementContext is TryStatementContext tryContext2)
        {
            tryContext2.ShouldGenerateBreakContinueCode = previousShouldGenerateBreakContinueCode;
        }

        return this;
    }

    /// <inheritdoc/>
    public IASTNode Duplicate(ParseContext context)
    {
        return new RepeatLoopNode(NearbyToken, TimesToRepeat.Duplicate(context), Body.Duplicate(context));
    }

    /// <inheritdoc/>
    public void GenerateCode(BytecodeContext context)
    {
        // Initial number of times to repeat
        TimesToRepeat.GenerateCode(context);
        context.ConvertDataType(DataType.Int32);

        // Maintain a loop counter on the stack (rather than through any variable)
        context.Emit(Opcode.Duplicate, DataType.Int32);
        context.Emit(Opcode.Push, (int)0, DataType.Int32);
        context.Emit(Opcode.Compare, ComparisonType.LesserEqualThan, DataType.Int32, DataType.Int32);

        // Branch target at the tail and incrementor of the loop
        MultiForwardBranchPatch tailPatch = new();
        MultiForwardBranchPatch decrementorPatch = new();

        // Jump based on loop counter
        tailPatch.AddInstruction(context, context.Emit(Opcode.BranchTrue));

        // Store current last array owner ID
        long lastArrayOwnerID = context.LastArrayOwnerID;

        // Body
        MultiBackwardBranchPatch bodyPatch = new(context);
        context.PushControlFlowContext(new RepeatLoopContext(tailPatch, decrementorPatch));
        Body.GenerateCode(context);
        context.PopControlFlowContext();

        // If array owners are currently being generated, reset last array owner ID when it changes or when continue/break are used
        if (context.CanGenerateArrayOwners && (lastArrayOwnerID != context.LastArrayOwnerID || tailPatch.NumberUsed > 1 || decrementorPatch.Used))
        {
            context.LastArrayOwnerID = -1;
        }

        // Decrement loop counter
        decrementorPatch.Patch(context);
        if (context.CompileContext.GameContext.UsingExtraRepeatInstruction)
        {
            context.Emit(Opcode.Push, (int)1, DataType.Int32);
        }
        else
        {
            context.Emit(Opcode.PushImmediate, (short)1, DataType.Int16);
        }
        context.Emit(Opcode.Subtract, DataType.Int32, DataType.Int32);
        context.Emit(Opcode.Duplicate, DataType.Int32);
        if (context.CompileContext.GameContext.UsingExtraRepeatInstruction)
        {
            context.Emit(Opcode.Convert, DataType.Int32, DataType.Boolean);
        }
        bodyPatch.AddInstruction(context, context.Emit(Opcode.BranchTrue));

        // Tail (clean up loop counter as well)
        tailPatch.Patch(context);
        context.Emit(Opcode.PopDelete, DataType.Int32);
    }

    /// <inheritdoc/>
    public IEnumerable<IASTNode> EnumerateChildren()
    {
        yield return TimesToRepeat;
        yield return Body;
    }
}
