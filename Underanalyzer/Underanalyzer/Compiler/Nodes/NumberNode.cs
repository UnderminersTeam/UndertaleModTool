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
/// Represents a constant 64-bit floating point (or integer) number in the AST.
/// </summary>
internal sealed class NumberNode : IConstantASTNode
{
    /// <summary>
    /// Number being used as a value for this node.
    /// </summary>
    public double Value { get; }

    /// <summary>
    /// Name of the constant that created this number, or <see langword="null"/> if none was used.
    /// </summary>
    public string? ConstantName { get; }

    /// <inheritdoc/>
    public IToken? NearbyToken { get; }

    public NumberNode(TokenNumber token, string? constantName)
    {
        Value = token.Value;
        NearbyToken = token;
        ConstantName = constantName;
    }

    public NumberNode(double value, IToken? nearbyToken)
    {
        Value = value;
        NearbyToken = nearbyToken;
    }

    /// <inheritdoc/>
    public IASTNode PostProcess(ParseContext context)
    {
        // No processing to do prior to GMLv2
        if (!context.CompileContext.GameContext.UsingGMLv2)
        {
            return this;
        }

        // Handle special self/other/global cases in GMLv2
        return ConstantName switch
        {
            "self" => new SimpleFunctionCallNode(VMConstants.SelfFunction, null, []),
            "other" => new SimpleFunctionCallNode(VMConstants.OtherFunction, null, []),
            "global" => 
                context.CompileContext.GameContext.UsingGlobalConstantFunction ?
                    new SimpleFunctionCallNode(VMConstants.GlobalFunction, null, []) :
                    this,
            _ => this
        };
    }

    /// <inheritdoc/>
    public IASTNode Duplicate(ParseContext context)
    {
        return this;
    }

    /// <inheritdoc/>
    public void GenerateCode(BytecodeContext context)
    {
        GenerateCode(context, Value);
    }

    /// <summary>
    /// Generates code to push a number, but without any specific node.
    /// </summary>
    public static void GenerateCode(BytecodeContext context, double value)
    {
        if ((long)value == value)
        {
            // Integer value
            long integerValue = (long)value;
            if (integerValue <= int.MaxValue && integerValue >= int.MinValue)
            {
                if (integerValue <= short.MaxValue && integerValue >= short.MinValue)
                {
                    // 16-bit integer
                    context.Emit(Opcode.PushImmediate, (short)integerValue, DataType.Int16);
                    context.PushDataType(DataType.Int32);
                }
                else
                {
                    // 32-bit integer
                    context.Emit(Opcode.Push, (int)integerValue, DataType.Int32);
                    context.PushDataType(DataType.Int32);
                }
            }
            else
            {
                // 64-bit integer
                context.Emit(Opcode.Push, integerValue, DataType.Int64);
                context.PushDataType(DataType.Int64);
            }
        }
        else
        {
            // Double value
            context.Emit(Opcode.Push, value, DataType.Double);
            context.PushDataType(DataType.Double);
        }
    }

    /// <inheritdoc/>
    public IEnumerable<IASTNode> EnumerateChildren()
    {
        return [];
    }
}
