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
/// Represents a function call in the AST, not tied to any specific variable name.
/// </summary>
internal sealed class FunctionCallNode : IMaybeStatementASTNode
{
    /// <summary>
    /// Expression being called.
    /// </summary>
    public IASTNode Expression { get; private set; }

    /// <summary>
    /// Arguments being used for this function call, in order.
    /// </summary>
    public List<IASTNode> Arguments { get; }

    /// <inheritdoc/>
    public bool IsStatement { get; set; } = false;

    /// <inheritdoc/>
    public IToken? NearbyToken { get; }

    /// <summary>
    /// Creates a function call node, parsing from the given context's current position,
    /// and given the provided expression being called.
    /// </summary>
    public FunctionCallNode(ParseContext context, TokenSeparator token, IASTNode expression)
    {
        NearbyToken = token;
        Expression = expression;
        Arguments = Functions.ParseCallArguments(context, 2047 /* TODO: change based on gamemaker version? */);
    }
    
    /// <summary>
    /// Creates a function call node with the given token, expression, and arguments.
    /// </summary>
    public FunctionCallNode(IToken? nearbyToken, IASTNode expression, List<IASTNode> arguments)
    {
        NearbyToken = nearbyToken;
        Expression = expression; 
        Arguments = arguments;
    }

    /// <inheritdoc/>
    public IASTNode PostProcess(ParseContext context)
    {
        Expression = Expression.PostProcess(context);
        for (int i = 0; i < Arguments.Count; i++)
        {
            Arguments[i] = Arguments[i].PostProcess(context);
        }
        return this;
    }

    /// <inheritdoc/>
    public IASTNode Duplicate(ParseContext context)
    {
        List<IASTNode> newArguments = new(Arguments);
        for (int i = 0; i < newArguments.Count; i++)
        {
            newArguments[i] = newArguments[i].Duplicate(context);
        }
        return new FunctionCallNode(NearbyToken, Expression.Duplicate(context), newArguments)
        {
            IsStatement = IsStatement
        };
    }

    /// <summary>
    /// Generates code for pushing arguments to the stack for this function call node.
    /// </summary>
    private void GenerateArguments(BytecodeContext context)
    {
        // Push arguments in reverse order (so they get popped in normal order)
        for (int i = Arguments.Count - 1; i >= 0; i--)
        {
            Arguments[i].GenerateCode(context);
            context.ConvertDataType(DataType.Variable);
        }
    }

    /// <summary>
    /// Generates code for this node, given whether it's in part of (but not the end of) a chain.
    /// </summary>
    private void GenerateCode(BytecodeContext context, bool inChain)
    {
        VariablePatch finalVariable;

        // For some reason, chains of function calls generate arguments before instance.
        // We'll also have an exception for DotVariableNode expressions, which is broken in the original compiler.
        if (Expression is DotVariableNode)
        {
            inChain = false;
        }
        if (inChain)
        {
            // Push arguments to stack
            GenerateArguments(context);
        }

        // Push instance
        if (Expression is SimpleVariableNode simpleVar)
        {
            // Push self/other/global (or get instance from object ID)
            string functionToCall;
            int argsToUse;
            if (simpleVar.ExplicitInstanceType >= 0)
            {
                functionToCall = VMConstants.GetInstanceFunction;
                argsToUse = 1;

                // Generate object ID as number
                NumberNode.GenerateCode(context, (int)simpleVar.ExplicitInstanceType);
                context.ConvertDataType(DataType.Variable);
            }
            else
            {
                functionToCall = simpleVar.ExplicitInstanceType switch
                {
                    InstanceType.Other => VMConstants.OtherFunction,
                    InstanceType.Global => VMConstants.GlobalFunction,
                    _ => VMConstants.SelfFunction
                };
                argsToUse = 0;
            }
            context.EmitCall(FunctionPatch.FromBuiltin(context, functionToCall), argsToUse);

            // If in chain, this value needs to be duplicated still
            if (inChain)
            {
                context.EmitDuplicate(DataType.Variable, 0);
            }

            // Make final variable patch
            finalVariable = new(simpleVar.VariableName, InstanceType.Self, VariableType.Normal, simpleVar.BuiltinVariable is not null)
            {
                InstructionInstanceType = InstanceType.StackTop
            };
        }
        else if (Expression is DotVariableNode dotVar)
        {
            // Handle pretty strange compiler quirk with no dup swaps being performed (if no dot on left side of this dot)
            if (!inChain && dotVar.LeftExpression is 
                    FunctionCallNode { Expression: SimpleVariableNode { CollapsedFromDot: false } or 
                                                   FunctionCallNode or SimpleFunctionCallNode or
                                                   AccessorNode } or 
                    SimpleFunctionCallNode)
            {
                inChain = true;

                // Push arguments to stack
                bool prevGeneratingDotVariableCall = context.CurrentScope.GeneratingDotVariableCall;
                context.CurrentScope.GeneratingDotVariableCall = true;
                GenerateArguments(context);
                context.CurrentScope.GeneratingDotVariableCall = prevGeneratingDotVariableCall;

                // Push left expression
                if (dotVar.LeftExpression is FunctionCallNode funcCall)
                {
                    funcCall.GenerateCode(context, true);
                }
                else
                {
                    dotVar.LeftExpression.GenerateCode(context);
                }

                // Duplicate left expression (it's an instance) and convert to instance ID
                context.EmitDuplicate(DataType.Variable, 0);
                context.ConvertToInstanceId();

                // Make final variable patch
                finalVariable = new(dotVar.VariableName, InstanceType.Self, VariableType.StackTop, dotVar.BuiltinVariable is not null);
            }
            else
            {
                // Push left expression
                dotVar.LeftExpression.GenerateCode(context);

                // If conversion is necessary, run function to get single instance
                if (context.ConvertDataType(DataType.Variable))
                {
                    context.EmitCall(FunctionPatch.FromBuiltin(context, VMConstants.GetInstanceFunction), 1);
                }

                // Make final variable patch
                finalVariable = new(dotVar.VariableName, InstanceType.Self, VariableType.Normal, dotVar.BuiltinVariable is not null)
                {
                    InstructionInstanceType = InstanceType.StackTop
                };
            }
        }
        else if (Expression is FunctionCallNode or SimpleFunctionCallNode or AccessorNode)
        {
            // Only push arguments if not already pushed
            if (!inChain)
            {
                // Push arguments to stack
                GenerateArguments(context);
            }

            // Handle compiler quirk with dot on left side of accessors using the accessor as an instance
            bool dupInstance = false;
            if (Expression is AccessorNode accessor)
            {
                // Get leftmost accessor from here
                AccessorNode leftmostAccessor = accessor;
                while (leftmostAccessor.Expression is AccessorNode furtherLeft)
                {
                    leftmostAccessor = furtherLeft;
                }

                if (leftmostAccessor.Expression is DotVariableNode)
                {
                    // Duplicate instance if dot on left side of leftmost accessor
                    dupInstance = true;
                }
                else if (leftmostAccessor.Expression is SimpleVariableNode { CollapsedFromDot: true } simpleVarCollapsed)
                {
                    // Duplicate instance if a collapsed dot node is on the left side of the leftmost accessor
                    dupInstance = true;

                    // If leftmost accessor expression has "self." on left side (collapsed from dot),
                    // generate code specific for this situation (compiler quirk).
                    if (simpleVarCollapsed.ExplicitInstanceType == InstanceType.Self)
                    {
                        context.EmitCall(FunctionPatch.FromBuiltin(context, VMConstants.SelfFunction), 0);
                        simpleVarCollapsed.SetExplicitInstanceType(InstanceType.StackTop);
                    }

                    // TODO: certain GameMaker versions also do the above for other/global apparently
                }
            }

            // Push self (if not using expression as an instance)
            if (!dupInstance)
            {
                context.EmitCall(FunctionPatch.FromBuiltin(context, VMConstants.SelfFunction), 0);
            }

            // Recurse to earlier function call
            if (Expression is FunctionCallNode funcCall)
            {
                funcCall.GenerateCode(context, true);
            }
            else
            {
                Expression.GenerateCode(context);
            }
            context.PopDataType();

            // Duplicate expression to use as instance, if applicable
            if (dupInstance)
            {
                context.EmitDuplicate(DataType.Variable, 0);
            }

            // Push error if not using GMLv2
            if (!context.CompileContext.GameContext.UsingGMLv2)
            {
                context.CompileContext.PushError("Cannot call variables as functions before GMLv2 (GameMaker 2.3+)", Expression.NearbyToken);
            }

            // Emit actual call
            context.EmitCallVariable(Arguments.Count);
            context.PushDataType(DataType.Variable);

            // If this node is a statement, remove result from stack
            if (IsStatement)
            {
                context.Emit(Opcode.PopDelete, context.PopDataType());
            }
            return;
        }
        else
        {
            // Only push arguments if not already pushed
            if (!inChain)
            {
                // Push arguments to stack
                GenerateArguments(context);
            }

            // General expression - use self instance, and generate expression to call.
            // NOTE: expression is not converted to variable type, which is an official compiler bug/quirk
            context.EmitCall(FunctionPatch.FromBuiltin(context, VMConstants.SelfFunction), 0);
            Expression.GenerateCode(context);
            context.PopDataType();

            // Push error if not using GMLv2
            if (!context.CompileContext.GameContext.UsingGMLv2)
            {
                context.CompileContext.PushError("Cannot call variables as functions before GMLv2 (GameMaker 2.3+)", Expression.NearbyToken);
            }

            // Emit actual call
            context.EmitCallVariable(Arguments.Count);
            context.PushDataType(DataType.Variable);

            // If this node is a statement, remove result from stack
            if (IsStatement)
            {
                context.Emit(Opcode.PopDelete, context.PopDataType());
            }
            return;
        }

        // In the common case, generate arguments after instance and swap/duplicate
        if (!inChain)
        {
            // Push arguments to stack
            if (Expression is DotVariableNode)
            {
                bool prevGeneratingDotVariableCall = context.CurrentScope.GeneratingDotVariableCall;
                context.CurrentScope.GeneratingDotVariableCall = true;
                GenerateArguments(context);
                context.CurrentScope.GeneratingDotVariableCall = prevGeneratingDotVariableCall;
            }
            else
            {
                GenerateArguments(context);
            }

            // Swap instance and arguments around on stack, and duplicate instance
            context.EmitDupSwap(DataType.Variable, (byte)Arguments.Count, 1);
            context.EmitDuplicate(DataType.Variable, 0);
        }

        // Compile final variable
        context.Emit(Opcode.Push, finalVariable, DataType.Variable);

        // Push error if not using GMLv2
        if (!context.CompileContext.GameContext.UsingGMLv2)
        {
            context.CompileContext.PushError("Cannot call variables as functions before GMLv2 (GameMaker 2.3+)", Expression.NearbyToken);
        }

        // Emit actual call
        context.EmitCallVariable(Arguments.Count);
        context.PushDataType(DataType.Variable);

        // If this node is a statement, remove result from stack
        if (IsStatement)
        {
            context.Emit(Opcode.PopDelete, context.PopDataType());
        }
    }

    /// <inheritdoc/>
    public void GenerateCode(BytecodeContext context)
    {
        GenerateCode(context, false);
    }

    /// <inheritdoc/>
    public IEnumerable<IASTNode> EnumerateChildren()
    {
        yield return Expression;
        foreach (IASTNode argument in Arguments)
        {
            yield return argument;
        }
    }
}
