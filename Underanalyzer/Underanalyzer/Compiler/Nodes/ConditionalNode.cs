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
/// Represents a conditional (ternary) node in the AST.
/// </summary>
internal sealed class ConditionalNode : IASTNode
{
    /// <summary>
    /// Condition of the conditional node.
    /// </summary>
    public IASTNode Condition { get; private set; }

    /// <summary>
    /// True expression of the conditional node.
    /// </summary>
    public IASTNode TrueExpression { get; private set; }

    /// <summary>
    /// False expression of the conditional node.
    /// </summary>
    public IASTNode FalseExpression { get; private set; }

    /// <inheritdoc/>
    public IToken? NearbyToken { get; }

    /// <summary>
    /// Creates a conditional node, given the provided token and expressions for condition, true, and false.
    /// </summary>
    public ConditionalNode(IToken? token, IASTNode condition, IASTNode trueExpression, IASTNode falseExpression)
    {
        Condition = condition;
        TrueExpression = trueExpression;
        FalseExpression = falseExpression;
        NearbyToken = token;
    }

    /// <inheritdoc/>
    public IASTNode PostProcess(ParseContext context)
    {
        Condition = Condition.PostProcess(context);
        TrueExpression = TrueExpression.PostProcess(context);
        FalseExpression = FalseExpression.PostProcess(context);
        return this;
    }

    /// <inheritdoc/>
    public IASTNode Duplicate(ParseContext context)
    {
        return new ConditionalNode(
            NearbyToken,
            Condition.Duplicate(context),
            TrueExpression.Duplicate(context),
            FalseExpression.Duplicate(context)
        );
    }

    /// <inheritdoc/>
    public void GenerateCode(BytecodeContext context)
    {
        // Generate condition, and convert to boolean
        Condition.GenerateCode(context);
        context.ConvertDataType(DataType.Boolean);

        // Jump based on condition
        SingleForwardBranchPatch conditionBranch = new(context, context.Emit(Opcode.BranchFalse));

        // True expression (and convert to variable type)
        TrueExpression.GenerateCode(context);
        context.ConvertDataType(DataType.Variable);
        SingleForwardBranchPatch skipElseBranch = new(context, context.Emit(Opcode.Branch));

        // Store current last array owner ID
        long lastArrayOwnerID = context.LastArrayOwnerID;

        // False expression (and convert to variable type)
        conditionBranch.Patch(context);
        FalseExpression.GenerateCode(context);
        context.ConvertDataType(DataType.Variable);

        // If last array owner ID changed, reset it
        if (lastArrayOwnerID != context.LastArrayOwnerID)
        {
            context.LastArrayOwnerID = -1;
        }

        // Ending (result is always variable type)
        skipElseBranch.Patch(context);
        context.PushDataType(DataType.Variable);
    }

    /// <inheritdoc/>
    public IEnumerable<IASTNode> EnumerateChildren()
    {
        yield return Condition;
        yield return TrueExpression;
        yield return FalseExpression;
    }
}
