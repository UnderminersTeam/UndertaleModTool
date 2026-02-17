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
/// Represents a "do...until" loop in the AST.
/// </summary>
internal sealed class DoUntilLoopNode : IASTNode
{
    /// <summary>
    /// Body of the do...until loop node.
    /// </summary>
    public IASTNode Body { get; private set; }

    /// <summary>
    /// Condition of the do...until loop node.
    /// </summary>
    public IASTNode Condition { get; private set; }

    /// <inheritdoc/>
    public IToken? NearbyToken { get; }

    private DoUntilLoopNode(IToken? token, IASTNode body, IASTNode condition)
    {
        NearbyToken = token;
        Body = body;
        Condition = condition;
    }

    /// <summary>
    /// Creates a do...until loop node, parsing from the given context's current position.
    /// </summary>
    public static DoUntilLoopNode? Parse(ParseContext context)
    {
        // Parse "do" keyword
        if (context.EnsureToken(KeywordKind.Do) is not TokenKeyword tokenKeyword)
        {
            return null;
        }

        // Parse loop body
        if (Statements.ParseStatement(context) is not IASTNode body)
        {
            return null;
        }

        // Parse "until" keyword
        context.SkipSemicolons();
        if (context.EnsureToken(KeywordKind.Until) is not TokenKeyword tokenKeyword2)
        {
            return null;
        }

        // Parse loop condition
        if (Expressions.ParseExpression(context) is not IASTNode condition)
        {
            return null;
        }

        // Create final statement
        return new DoUntilLoopNode(tokenKeyword, body, condition);
    }

    /// <inheritdoc/>
    public IASTNode PostProcess(ParseContext context)
    {
        Body = Body.PostProcess(context);
        Condition = Condition.PostProcess(context);
        return this;
    }

    /// <inheritdoc/>
    public IASTNode Duplicate(ParseContext context)
    {
        return new DoUntilLoopNode(
            NearbyToken, 
            Body.Duplicate(context), 
            Condition.Duplicate(context)
        );
    }

    /// <inheritdoc/>
    public void GenerateCode(BytecodeContext context)
    {
        // Branch target at the head and tail of the loop
        MultiBackwardBranchPatch headPatch = new(context);
        MultiForwardBranchPatch conditionPatch = new();
        MultiForwardBranchPatch tailPatch = new();

        // Enter loop context, and generate body
        context.PushControlFlowContext(new BasicLoopContext(tailPatch, conditionPatch));
        Body.GenerateCode(context);
        context.PopControlFlowContext();

        // Loop condition
        conditionPatch.Patch(context);
        Condition.GenerateCode(context);
        context.ConvertDataType(DataType.Boolean);

        // Jump based on condition
        headPatch.AddInstruction(context, context.Emit(Opcode.BranchFalse));

        // Loop tail
        tailPatch.Patch(context);

        // If array owners are currently being generated, reset last array owner ID when continue/break are used
        if (context.CanGenerateArrayOwners && (tailPatch.Used || conditionPatch.Used))
        {
            context.LastArrayOwnerID = -1;
        }
    }

    /// <inheritdoc/>
    public IEnumerable<IASTNode> EnumerateChildren()
    {
        yield return Body;
        yield return Condition;
    }
}
