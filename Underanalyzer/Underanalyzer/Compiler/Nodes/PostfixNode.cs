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
/// Represents a post (++/-- on right side) expression in the AST.
/// </summary>
internal sealed class PostfixNode : IMaybeStatementASTNode
{
    /// <summary>
    /// Expression being post-incremented/post-decremented.
    /// </summary>
    public IAssignableASTNode Expression { get; private set; }

    /// <summary>
    /// Whether this postfix is an increment (++) or a decrement (--).
    /// </summary>
    public bool IsIncrement { get; }

    /// <inheritdoc/>
    public bool IsStatement { get; set; } = false;

    /// <inheritdoc/>
    public IToken? NearbyToken { get; }

    /// <summary>
    /// Creates a postfix node, given whether or not the postfix is an increment.
    /// </summary>
    public PostfixNode(IToken? token, IAssignableASTNode expression, bool isIncrement)
    {
        NearbyToken = token;
        Expression = expression;
        IsIncrement = isIncrement;
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
        return new PostfixNode(
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
                Expression.GeneratePrePostAssignCode(context, IsIncrement, false, false);

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

        Expression.GeneratePrePostAssignCode(context, IsIncrement, false, IsStatement);
    }

    /// <inheritdoc/>
    public IEnumerable<IASTNode> EnumerateChildren()
    {
        yield return Expression;
    }
}
