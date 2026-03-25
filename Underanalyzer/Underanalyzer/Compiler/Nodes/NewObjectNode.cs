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
/// Represents a new object node in the AST.
/// </summary>
internal sealed class NewObjectNode : IMaybeStatementASTNode
{
    /// <summary>
    /// Expression being instantiated.
    /// </summary>
    public IASTNode Expression { get; private set; }

    /// <summary>
    /// Arguments being used in constructor call.
    /// </summary>
    public List<IASTNode> Arguments { get; private set; }

    /// <inheritdoc/>
    public bool IsStatement { get; set; } = false;

    /// <inheritdoc/>
    public IToken? NearbyToken { get; }

    private NewObjectNode(IToken? nearbyToken, IASTNode expression, List<IASTNode> arguments)
    {
        Expression = expression;
        Arguments = arguments;
        NearbyToken = nearbyToken;
    }

    /// <summary>
    /// Creates a new object node, parsing from the given context's current position.
    /// </summary>
    public static NewObjectNode? Parse(ParseContext context)
    {
        // Parse "new" keyword
        if (context.EnsureToken(KeywordKind.New) is not TokenKeyword tokenKeyword)
        {
            return null;
        }

        // Parse function/variable/value being instantiated
        IASTNode expression;
        if (!context.EndOfCode && context.Tokens[context.Position] is TokenFunction tokenFunction)
        {
            // Convert function to simple variable node
            expression = new SimpleVariableNode(tokenFunction.Text, null);
            context.Position++;
        }
        else
        {
            // Parse general chain expression
            if (Expressions.ParseChainExpression(context, true) is IASTNode chainExpression)
            {
                expression = chainExpression;
            }
            else
            {
                return null;
            }
        }

        // Parse arguments being used in constructor call
        if (Functions.ParseCallArguments(context, 65534 /* TODO: is this limit correct? */) is not List<IASTNode> arguments)
        {
            return null;
        }

        // Create final expression node
        return new NewObjectNode(tokenKeyword, expression, arguments);
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
        return new NewObjectNode(NearbyToken, Expression.Duplicate(context), newArguments)
        {
            IsStatement = IsStatement
        };
    }

    /// <inheritdoc/>
    public void GenerateCode(BytecodeContext context)
    {
        // Push arguments in reverse order (so they get popped in normal order)
        for (int i = Arguments.Count - 1; i >= 0; i--)
        {
            Arguments[i].GenerateCode(context);
            context.ConvertDataType(DataType.Variable);
        }

        // Push function reference
        if (Expression is SimpleVariableNode simpleVariable)
        {
            string functionName = simpleVariable.VariableName;
            bool isGlobalFunction = 
                context.IsGlobalFunctionName(functionName) || 
                (context.CompileContext.ScriptKind == CompileScriptKind.GlobalScript && context.RootScope.IsFunctionDeclaredImmediately(functionName));
            if (isGlobalFunction || context.IsFunctionDeclaredInCurrentScope(functionName))
            {
                // We can statically resolve the function at compile time, so do that
                IGameContext gameContext = context.CompileContext.GameContext;
                if (gameContext.UsingFunctionScriptReferences && !context.CurrentScope.GeneratingDotVariableCall &&
                    (
                        (gameContext.GetScriptId(functionName, out int _) && !gameContext.GetScriptIdByFunctionName(functionName, out int _)) ||
                        (gameContext.UsingNewFunctionResolution && !isGlobalFunction && !context.CurrentScope.IsFunctionDeclaredImmediately(functionName))
                    ))
                {
                    // If calling a script that doesn't actually have its own function, push a reference to
                    // the script directly (in versions where it does that, at least).
                    context.Emit(ExtendedOpcode.PushReference, new FunctionPatch(context.CurrentScope, functionName));
                }
                else
                {
                    // Use regular function push
                    context.EmitPushFunction(new FunctionPatch(context.CurrentScope, functionName));
                    context.Emit(Opcode.Convert, DataType.Int32, DataType.Variable);
                }
            }
            else
            {
                // Failed to find function, so just push variable.
                // Mark it as a function call variable, if not collapsed from a dot variable, to mimic official compiler quirks.
                if (!simpleVariable.CollapsedFromDot)
                {
                    simpleVariable.IsFunctionCall = true;
                }
                simpleVariable.GenerateCode(context);
                context.ConvertDataType(DataType.Variable);
            }
        }
        else
        {
            // Non-trivial expression, so we can't statically resolve it at all (just generate it normally)
            Expression.GenerateCode(context);
            context.ConvertDataType(DataType.Variable);
        }

        // Call to actually instantiate object
        context.EmitCall(FunctionPatch.FromBuiltin(context, VMConstants.NewObjectFunction), Arguments.Count + 1);

        if (IsStatement)
        {
            // This is a statement, so remove result from the stack
            context.Emit(Opcode.PopDelete, DataType.Variable);
        }
        else
        {
            // This is not a statement, so result is on the stack
            context.PushDataType(DataType.Variable);
        }
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
