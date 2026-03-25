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
/// Represents a "throw" statement in the AST.
/// </summary>
internal sealed class ThrowNode : IASTNode
{
    /// <summary>
    /// The expression being thrown.
    /// </summary>
    public IASTNode Expression { get; private set; }

    /// <inheritdoc/>
    public IToken? NearbyToken { get; private set; }

    private ThrowNode(IToken? nearbyToken, IASTNode expression)
    {
        Expression = expression;
        NearbyToken = nearbyToken;
    }

    /// <summary>
    /// Creates a throw statement node, parsing from the given context's current position.
    /// </summary>
    public static ThrowNode? Parse(ParseContext context)
    {
        // Parse "throw" keyword
        if (context.EnsureToken(KeywordKind.Throw) is not TokenKeyword tokenKeyword)
        {
            return null;
        }

        // Parse expression being thrown
        if (Expressions.ParseExpression(context) is not IASTNode expression)
        {
            return null;
        }

        // Create final statement
        return new ThrowNode(tokenKeyword, expression);
    }

    /// <inheritdoc/>
    public IASTNode PostProcess(ParseContext context)
    {
        Expression = Expression.PostProcess(context);

        // If in a try statement with a finally block, generate extra code (if enabled; this is buggy behavior).
        if (context.TryStatementContext is { HasFinally: true, ThrowFinallyGeneration: true } && 
            context.CompileContext.GameContext.UsingFinallyBeforeThrow)
        {
            // Create block with latest finally node, then this throw node
            BlockNode newBlock = BlockNode.CreateEmpty(NearbyToken, 2);
            newBlock.Children.Add(context.CurrentScope.TryFinallyNodes[^1].Duplicate(context));
            newBlock.Children.Add(this);
            return newBlock;
        }

        return this;
    }

    /// <inheritdoc/>
    public IASTNode Duplicate(ParseContext context)
    {
        return new ThrowNode(NearbyToken, Expression.Duplicate(context));
    }

    /// <inheritdoc/>
    public void GenerateCode(BytecodeContext context)
    {
        // Push expression being thrown
        Expression.GenerateCode(context);

        // Convert to variable, and call built-in throw function (no stack cleanup necessary)
        context.ConvertDataType(DataType.Variable);
        context.EmitCall(FunctionPatch.FromBuiltin(context, VMConstants.ThrowFunction), 1);
    }

    /// <inheritdoc/>
    public IEnumerable<IASTNode> EnumerateChildren()
    {
        yield return Expression;
    }
}