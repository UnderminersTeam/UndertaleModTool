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
/// Represents a constant 64-bit integer in the AST.
/// </summary>
internal sealed class Int64Node : IConstantASTNode
{
    /// <summary>
    /// Number being used as a value for this node.
    /// </summary>
    public long Value { get; }

    /// <inheritdoc/>
    public IToken? NearbyToken { get; }

    /// <summary>
    /// Creates a 64-bit integer node from a given 64-bit integer token.
    /// </summary>
    public Int64Node(TokenInt64 token)
    {
        Value = token.Value;
        NearbyToken = token;
    }

    /// <summary>
    /// Creates a 64-bit integer node from a given value and nearby token.
    /// </summary>
    public Int64Node(long value, IToken? nearbyToken)
    {
        Value = value;
        NearbyToken = nearbyToken;
    }

    /// <inheritdoc/>
    public IASTNode PostProcess(ParseContext context)
    {
        return this;
    }

    /// <inheritdoc/>
    public IASTNode Duplicate(ParseContext context)
    {
        return this;
    }

    /// <inheritdoc/>
    public void GenerateCode(BytecodeContext context)
    {
        context.Emit(Opcode.Push, Value, DataType.Int64);
        context.PushDataType(DataType.Int64);
    }

    /// <inheritdoc/>
    public IEnumerable<IASTNode> EnumerateChildren()
    {
        return [];
    }
}
