/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using Underanalyzer.Compiler.Bytecode;
using Underanalyzer.Compiler.Lexer;
using Underanalyzer.Compiler.Parser;
using static Underanalyzer.IGMInstruction;

namespace Underanalyzer.Compiler.Nodes;

/// <summary>
/// Represents a prefix (++/-- on left side) expression or statement in the AST.
/// </summary>
internal sealed class PrefixNode : IMaybeStatementASTNode
{
    /// <summary>
    /// Expression being pre-incremented/pre-decremented.
    /// </summary>
    public IAssignableASTNode Expression { get; private set; }

    /// <summary>
    /// Whether this prefix is an increment (++) or a decrement (--).
    /// </summary>
    public bool IsIncrement { get; }

    /// <inheritdoc/>
    public bool IsStatement { get; set; } = false;

    /// <inheritdoc/>
    public IToken? NearbyToken { get; }

    private PrefixNode(IToken? token, IAssignableASTNode expression, bool isIncrement)
    {
        NearbyToken = token;
        IsIncrement = isIncrement;
        Expression = expression;
    }

    /// <summary>
    /// Creates a prefix node, parsing from the given context's current position,
    /// and given whether or not the prefix is an increment.
    /// </summary>
    public static PrefixNode? Parse(ParseContext context, TokenOperator token, bool isIncrement)
    {
        // Parse expression after ++/--
        if (Expressions.ParseChainExpression(context) is not IASTNode expression)
        {
            return null;
        }

        // Ensure expression is assignable
        if (expression is not IAssignableASTNode assignableExpression)
        {
            return null;
        }

        // Create final node
        return new PrefixNode(token, assignableExpression, isIncrement);
    }

    /// <inheritdoc/>
    public IASTNode PostProcess(ParseContext context)
    {
        Expression = Expression.PostProcess(context) as IAssignableASTNode ?? throw new Exception("Destination no longer assignable");
        return this;
    }

    /// <inheritdoc/>
    public IASTNode Duplicate(ParseContext context)
    {
        return new PrefixNode(
            NearbyToken,
            Expression.Duplicate(context) as IAssignableASTNode ?? throw new Exception("Destination no longer assignable"),
            IsIncrement)
        {
            IsStatement = IsStatement
        };
    }

    /// <inheritdoc/>
    public void GenerateCode(BytecodeContext context)
    {
        // Handle array copy-on-write
        if (context.CanGenerateArrayOwners)
        {
            if (ArrayOwners.ContainsArrayAccessor(Expression) || ArrayOwners.IsArraySetFunctionOrContainsSubLiteral(Expression))
            {
                // Disable array owner generation for expression
                context.CanGenerateArrayOwners = false;

                if (!ArrayOwners.GenerateSetArrayOwner(context, Expression))
                {
                    // Really weird official compiler bug - generate expression an extra time
                    Expression.GenerateCode(context);
                    context.Emit(Opcode.PopDelete, context.PopDataType());
                }

                // Really weird compiler quirk - generate as if this is an expression, always
                Expression.GeneratePrePostAssignCode(context, IsIncrement, true, false);

                // If we're a statement, though, make sure to get rid of the result
                if (IsStatement)
                {
                    context.Emit(Opcode.PopDelete, context.PopDataType());
                }

                // Restore array owner generation
                context.CanGenerateArrayOwners = true;
                return;
            }
        }

        Expression.GeneratePrePostAssignCode(context, IsIncrement, true, IsStatement);
    }

    /// <inheritdoc/>
    public IEnumerable<IASTNode> EnumerateChildren()
    {
        yield return Expression;
    }
}
