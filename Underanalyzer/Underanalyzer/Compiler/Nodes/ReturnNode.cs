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
/// Represents a "return" statement in the AST.
/// </summary>
internal sealed class ReturnNode(IToken? token, IASTNode returnValue) : IASTNode
{
    /// <summary>
    /// Expression being used as a return value for this node.
    /// </summary>
    public IASTNode ReturnValue { get; private set; } = returnValue;

    /// <inheritdoc/>
    public IToken? NearbyToken { get; } = token;

    /// <inheritdoc/>
    public IASTNode PostProcess(ParseContext context)
    {
        ReturnValue = ReturnValue.PostProcess(context);

        // Throw error if using inside of a try statement's finally block
        if (context.ProcessingFinally)
        {
            context.CompileContext.PushError("Cannot use return inside of finally block", NearbyToken);
        }

        // If in a try statement with a finally block, generate extra code
        if (context.TryStatementContext is { HasFinally: true })
        {
            // Reserve block with enough space for all finally nodes
            List<IASTNode> finallyNodes = context.CurrentScope.TryFinallyNodes;
            BlockNode newBlock = BlockNode.CreateEmpty(NearbyToken, 2 + finallyNodes.Count);
            
            // Generate new local variable to store return value temporarily
            context.CurrentScope.DeclareLocal(VMConstants.TryCopyVariable);
            SimpleVariableNode variable = new(VMConstants.TryCopyVariable, null, InstanceType.Local);

            // Store return value, generate finally code, then return with stored value
            newBlock.Children.Add(new AssignNode(AssignNode.AssignKind.Normal, variable, ReturnValue));
            for (int i = finallyNodes.Count - 1; i >= 0; i--)
            {
                newBlock.Children.Add(finallyNodes[i].Duplicate(context));
            }
            newBlock.Children.Add(new ReturnNode(NearbyToken, variable));
            return newBlock;
        }

        return this;
    }

    /// <inheritdoc/>
    public IASTNode Duplicate(ParseContext context)
    {
        return new ReturnNode(NearbyToken, ReturnValue.Duplicate(context));
    }

    /// <inheritdoc/>
    public void GenerateCode(BytecodeContext context)
    {
        // Generate return value, and convert to Variable data type
        ReturnValue.GenerateCode(context);
        context.ConvertDataType(DataType.Variable);

        // If necessary, perform data stack cleanup
        if (context.DoAnyControlFlowRequireCleanup() || context.FunctionCallBeforeExit is not null)
        {
            // Store return value into temporary local variable, perform stack cleanup, and then re-push return value using local
            VariablePatch tempVariable = new(VMConstants.TempReturnVariable, InstanceType.Local);
            context.Emit(Opcode.Pop, tempVariable, DataType.Variable, DataType.Variable);
            context.GenerateControlFlowCleanup();
            if (context.FunctionCallBeforeExit is not null)
            {
                // Call function call before retruning (exactly here, to mimic official compiler behavior)
                context.EmitCall(FunctionPatch.FromBuiltin(context, context.FunctionCallBeforeExit), 0);
            }
            context.Emit(Opcode.Push /* compiler quirk: not local! */, tempVariable, DataType.Variable);
        }

        // Emit actual return
        context.Emit(Opcode.Return, DataType.Variable);
    }

    /// <inheritdoc/>
    public IEnumerable<IASTNode> EnumerateChildren()
    {
        yield return ReturnValue;
    }
}
