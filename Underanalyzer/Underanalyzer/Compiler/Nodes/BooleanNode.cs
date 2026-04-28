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
/// Represents a constant boolean in the AST.
/// </summary>
internal sealed class BooleanNode : IConstantASTNode
{
    /// <summary>
    /// Boolean being used as a value for this node.
    /// </summary>
    public bool Value { get; }

    /// <inheritdoc/>
    public IToken? NearbyToken { get; }

    public BooleanNode(TokenBoolean token)
    {
        Value = token.Value;
        NearbyToken = token;
    }
    
    public BooleanNode(bool value, IToken? nearbyToken)
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
        context.Emit(Opcode.PushImmediate, (short)(Value ? 1 : 0), DataType.Int16);
        context.PushDataType(context.CompileContext.GameContext.UsingTypedBooleans ? DataType.Boolean : DataType.Int32);
    }

    /// <inheritdoc/>
    public IEnumerable<IASTNode> EnumerateChildren()
    {
        return [];
    }
}
