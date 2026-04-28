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
/// Represents a constant string in the AST.
/// </summary>
internal sealed class StringNode : IConstantASTNode
{
    /// <summary>
    /// String being used as a value for this node.
    /// </summary>
    public string Value { get; }

    /// <inheritdoc/>
    public IToken? NearbyToken { get; }

    public StringNode(TokenString token)
    {
        Value = token.Value;
        NearbyToken = token;
    }

    public StringNode(string value, IToken? nearbyToken)
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
        context.Emit(Opcode.Push, new StringPatch(Value), DataType.String);
        context.PushDataType(DataType.String);
    }

    /// <inheritdoc/>
    public IEnumerable<IASTNode> EnumerateChildren()
    {
        return [];
    }
}
