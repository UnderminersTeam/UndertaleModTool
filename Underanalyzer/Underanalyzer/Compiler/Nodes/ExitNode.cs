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
/// Represents an "exit" statement in the AST.
/// </summary>
internal sealed class ExitNode(TokenKeyword token) : IASTNode
{
    /// <inheritdoc/>
    public IToken? NearbyToken { get; } = token;

    /// <inheritdoc/>
    public IASTNode PostProcess(ParseContext context)
    {
        // Throw error if using inside of a try statement's finally block
        if (context.ProcessingFinally)
        {
            context.CompileContext.PushError("Cannot use exit inside of finally block", NearbyToken);
        }

        // If in a try statement with a finally block, generate extra code
        if (context.TryStatementContext is { HasFinally: true })
        {
            // Reserve block with enough space for all finally nodes
            List<IASTNode> finallyNodes = context.CurrentScope.TryFinallyNodes;
            BlockNode newBlock = BlockNode.CreateEmpty(NearbyToken, 1 + finallyNodes.Count);

            // Generate all finally code, then exit
            for (int i = finallyNodes.Count - 1; i >= 0; i--)
            {
                newBlock.Children.Add(finallyNodes[i].Duplicate(context));
            }
            newBlock.Children.Add(this);
            return newBlock;
        }

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
        // Call function call before exiting (BEFORE control flow cleanup, to mimic official compiler behavior)
        if (context.FunctionCallBeforeExit is not null)
        {
            context.EmitCall(FunctionPatch.FromBuiltin(context, context.FunctionCallBeforeExit), 0);
        }

        // Perform stack cleanup if required
        context.GenerateControlFlowCleanup();

        // Emit actual exit
        context.Emit(Opcode.Exit, DataType.Int32);
    }

    /// <inheritdoc/>
    public IEnumerable<IASTNode> EnumerateChildren()
    {
        return [];
    }
}
