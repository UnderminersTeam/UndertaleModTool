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
/// Represents a nullish coalesce node in the AST.
/// </summary>
internal sealed class NullishCoalesceNode : IASTNode
{
    /// <summary>
    /// Left side of the nullish coalesce node.
    /// </summary>
    public IASTNode Left { get; private set; }

    /// <summary>
    /// Right side of the nullish coalesce node.
    /// </summary>
    public IASTNode Right { get; private set; }

    /// <inheritdoc/>
    public IToken? NearbyToken { get; }

    /// <summary>
    /// Creates a nullish coalesce node, given the provided token and expressions for the left and right sides.
    /// </summary>
    public NullishCoalesceNode(IToken? token, IASTNode left, IASTNode right)
    {
        Left = left;
        Right = right;
        NearbyToken = token;
    }

    /// <inheritdoc/>
    public IASTNode PostProcess(ParseContext context)
    {
        Left = Left.PostProcess(context);
        Right = Right.PostProcess(context);
        return this;
    }

    /// <inheritdoc/>
    public IASTNode Duplicate(ParseContext context)
    {
        return new NullishCoalesceNode(NearbyToken, Left.Duplicate(context), Right.Duplicate(context));
    }

    /// <inheritdoc/>
    public void GenerateCode(BytecodeContext context)
    {
        // Compile left side (which will be checked for nullish)
        Left.GenerateCode(context);
        context.ConvertDataType(DataType.Variable);

        // Check if nullish; branch around right side if not
        context.Emit(ExtendedOpcode.IsNullishValue);
        SingleForwardBranchPatch skipRightSidePatch = new(context, context.Emit(Opcode.BranchFalse));

        // Right side (but remove nullish result from left side first)
        context.Emit(Opcode.PopDelete, DataType.Variable);
        Right.GenerateCode(context);
        context.ConvertDataType(DataType.Variable);

        // Branch destination at end, and push variable type
        skipRightSidePatch.Patch(context);
        context.PushDataType(DataType.Variable);
    }

    /// <inheritdoc/>
    public IEnumerable<IASTNode> EnumerateChildren()
    {
        yield return Left;
        yield return Right;
    }
}
