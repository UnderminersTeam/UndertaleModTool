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
/// Represents a "while" loop in the AST.
/// </summary>
internal sealed class WhileLoopNode : IASTNode
{
    /// <summary>
    /// Condition of the while loop node.
    /// </summary>
    public IASTNode Condition { get; private set; }

    /// <summary>
    /// Body of the while loop node.
    /// </summary>
    public IASTNode Body { get; private set; }

    /// <inheritdoc/>
    public IToken? NearbyToken { get; }

    public WhileLoopNode(IToken? token, IASTNode condition, IASTNode body)
    {
        NearbyToken = token;
        Condition = condition;
        Body = body;
    }

    /// <summary>
    /// Creates a while loop node, parsing from the given context's current position.
    /// </summary>
    public static WhileLoopNode? Parse(ParseContext context)
    {
        // Parse "while" keyword
        if (context.EnsureToken(KeywordKind.While) is not TokenKeyword tokenKeyword)
        {
            return null;
        }

        // Parse loop condition
        if (Expressions.ParseExpression(context) is not IASTNode condition)
        {
            return null;
        }

        // Skip "do" keyword, if present
        if (context.IsCurrentToken(KeywordKind.Do))
        {
            context.Position++;
        }

        // Parse loop body
        if (Statements.ParseStatement(context) is not IASTNode body)
        {
            return null;
        }

        // Create final statement
        return new WhileLoopNode(tokenKeyword, condition, body);
    }

    /// <inheritdoc/>
    public IASTNode PostProcess(ParseContext context)
    {
        // Enter while context
        bool previousProcessingSwitch = context.ProcessingSwitch;
        context.ProcessingSwitch = false;
        if (context.TryStatementContext is TryStatementContext tryContext)
        {
            // Entering while loop, so break/continue code should no longer be generated.
            // BUG: This isn't reset when exiting this method, but this mimics official compiler behavior...
            tryContext.ShouldGenerateBreakContinueCode = false;
        }

        // Normal post-processing
        Condition = Condition.PostProcess(context);
        Body = Body.PostProcess(context);

        // Exit while context
        context.ProcessingSwitch = previousProcessingSwitch;

        return this;
    }

    /// <inheritdoc/>
    public IASTNode Duplicate(ParseContext context)
    {
        return new WhileLoopNode(NearbyToken, Condition.Duplicate(context), Body.Duplicate(context));
    }

    /// <inheritdoc/>
    public void GenerateCode(BytecodeContext context)
    {
        // Branch target at the head and tail of the loop
        MultiBackwardBranchPatchTracked headPatch = new(context);
        MultiForwardBranchPatch tailPatch = new();

        // Loop condition
        Condition.GenerateCode(context);
        context.ConvertDataType(DataType.Boolean);

        // Jump based on condition
        tailPatch.AddInstruction(context, context.Emit(Opcode.BranchFalse));

        // Store current last array owner ID
        long lastArrayOwnerID = context.LastArrayOwnerID;

        // Enter loop context, and generate body
        context.PushControlFlowContext(new BasicLoopContext(tailPatch, headPatch));
        Body.GenerateCode(context);
        context.PopControlFlowContext();

        // Loop tail
        headPatch.AddInstruction(context, context.Emit(Opcode.Branch));
        tailPatch.Patch(context);

        // If array owners are currently being generated, reset last array owner ID when it changes or when continue/break are used
        if (context.CanGenerateArrayOwners && (lastArrayOwnerID != context.LastArrayOwnerID || tailPatch.NumberUsed > 1 || headPatch.NumberUsed > 1))
        {
            context.LastArrayOwnerID = -1;
        }
    }

    /// <inheritdoc/>
    public IEnumerable<IASTNode> EnumerateChildren()
    {
        yield return Condition;
        yield return Body;
    }
}
