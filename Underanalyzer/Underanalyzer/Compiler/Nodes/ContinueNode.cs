/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;
using Underanalyzer.Compiler.Bytecode;
using Underanalyzer.Compiler.Lexer;
using Underanalyzer.Compiler.Parser;
using static Underanalyzer.Compiler.Nodes.AssignNode;
using static Underanalyzer.IGMInstruction;

namespace Underanalyzer.Compiler.Nodes;

/// <summary>
/// Represents a "continue" statement in the AST.
/// </summary>
internal sealed class ContinueNode(IToken? token) : IASTNode
{
    /// <inheritdoc/>
    public IToken? NearbyToken { get; } = token;

    /// <inheritdoc/>
    public IASTNode PostProcess(ParseContext context)
    {
        // Throw error if using inside of a try statement's finally block
        if (context.ProcessingFinally)
        {
            context.CompileContext.PushError("Cannot use continue inside of finally block", NearbyToken);
        }

        // If in a try statement, generate extra code (note: slightly buggy behavior being mimicked!)
        if (context.TryStatementContext is TryStatementContext tryContext)
        {
            // Check whether we should generate, based on control flow being used (but always generate in older versions)
            if (!context.CompileContext.GameContext.UsingBetterTryBreakContinue || tryContext.ShouldGenerateBreakContinueCode)
            {
                // Generate block, setting continue variable before continuing
                context.CurrentScope.DeclareLocal(tryContext.ContinueVariableName);
                BlockNode blockNode = BlockNode.CreateEmpty(NearbyToken, 2);
                blockNode.Children.Add(new AssignNode(AssignKind.Normal, new SimpleVariableNode(tryContext.ContinueVariableName, null, InstanceType.Local), new NumberNode(1, NearbyToken)));
                blockNode.Children.Add(this);

                // Let the try statement context know that break/continue variable was used
                tryContext.HasBreakContinueVariable = true;

                return blockNode;
            }
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
        // Need at least one loop outside of this statement
        if (!context.AnyLoopContexts())
        {
            context.CompileContext.PushError($"Continue used outside of any loop", NearbyToken);
            return;
        }

        // Use control flow context's continue branch
        IControlFlowContext topContext = context.GetTopControlFlowContext();
        if (!topContext.CanContinueBeUsed)
        {
            // Can't actually use the continue branch!
            context.CompileContext.PushError($"Continue used in an invalid context", NearbyToken);
            return;
        }
        topContext.UseContinue(context, context.Emit(Opcode.Branch));
    }

    /// <inheritdoc/>
    public IEnumerable<IASTNode> EnumerateChildren()
    {
        return [];
    }
}
