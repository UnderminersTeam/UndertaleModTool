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

namespace Underanalyzer.Compiler.Nodes;

/// <summary>
/// Represents a switch "case" or "default" label in the AST.
/// </summary>
internal sealed class SwitchCaseNode(IToken? token, IASTNode? expression) : IASTNode
{
    /// <summary>
    /// Expression for the case, or null if none (for default).
    /// </summary>
    public IASTNode? Expression { get; private set; } = expression;

    /// <inheritdoc/>
    public IToken? NearbyToken { get; } = token;

    /// <inheritdoc/>
    public IASTNode PostProcess(ParseContext context)
    {
        Expression = Expression?.PostProcess(context);

        // Note: Official compiler logic here seems wrong, but probably designed to support legacy projects
        if (Expression is not null && Expression is not (IConstantASTNode or SimpleVariableNode or DotVariableNode or AccessorNode))
        {
            context.CompileContext.PushError("Failed to resolve switch case to a constant value or variable", Expression?.NearbyToken);
        }

        return this;
    }

    /// <inheritdoc/>
    public IASTNode Duplicate(ParseContext context)
    {
        return new SwitchCaseNode(NearbyToken, Expression?.Duplicate(context));
    }

    /// <inheritdoc/>
    public void GenerateCode(BytecodeContext context)
    {
        throw new InvalidOperationException();
    }

    /// <inheritdoc/>
    public IEnumerable<IASTNode> EnumerateChildren()
    {
        if (Expression is not null)
        {
            yield return Expression;
        }
    }
}
