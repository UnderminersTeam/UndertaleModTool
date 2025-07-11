/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Underanalyzer.Compiler.Bytecode;
using Underanalyzer.Compiler.Lexer;
using Underanalyzer.Compiler.Parser;
using static Underanalyzer.IGMInstruction;

namespace Underanalyzer.Compiler.Nodes;

/// <summary>
/// Represents a simple function call in the AST.
/// </summary>
internal sealed class SimpleFunctionCallNode : IMaybeStatementASTNode
{
    /// <summary>
    /// Function name (or variable name) being called.
    /// </summary>
    public string FunctionName { get; }

    /// <summary>
    /// Builtin function corresponding to the function name, or null if none.
    /// </summary>
    public IBuiltinFunction? BuiltinFunction { get; }

    /// <summary>
    /// Arguments being used for this function call, in order.
    /// </summary>
    public List<IASTNode> Arguments { get; }

    /// <inheritdoc/>
    public bool IsStatement { get; set; } = false;

    /// <inheritdoc/>
    public IToken? NearbyToken { get; }

    /// <summary>
    /// Creates a simple function call node, parsing from the given context's current position,
    /// and given the provided function token.
    /// </summary>
    public SimpleFunctionCallNode(ParseContext context, TokenFunction token)
    {
        NearbyToken = token;
        FunctionName = token.Text;
        BuiltinFunction = token.BuiltinFunction;
        Arguments = Functions.ParseCallArguments(context, 65535 /* TODO: change based on gamemaker version? */);
    }

    /// <summary>
    /// Creates an expression function call directly, without any parsing.
    /// </summary>
    public SimpleFunctionCallNode(string functionName, IBuiltinFunction? builtinFunction, List<IASTNode> arguments)
    {
        FunctionName = functionName;
        BuiltinFunction = builtinFunction;
        Arguments = arguments;
    }

    /// <summary>
    /// Parses an array literal from the given context's current position, and returns
    /// a corresponding function call node to create that array.
    /// </summary>
    public static SimpleFunctionCallNode ParseArrayLiteral(ParseContext context)
    {
        List<IASTNode> arguments = new(16);
        SimpleFunctionCallNode result = new(VMConstants.NewArrayFunction, 
                                            context.CompileContext.GameContext.Builtins.LookupBuiltinFunction(VMConstants.NewArrayFunction), 
                                            arguments);

        while (!context.EndOfCode && !context.IsCurrentToken(SeparatorKind.ArrayClose))
        {
            // Parse current expression in array
            if (Expressions.ParseExpression(context) is IASTNode expr)
            {
                arguments.Add(expr);
            }
            else
            {
                // Failed to parse expression; stop parsing array literal
                break;
            }

            // If at end of code, stop here
            if (context.EndOfCode)
            {
                break;
            }

            // We expect either a comma (separating the expressions), or an array close
            if (context.IsCurrentToken(SeparatorKind.Comma))
            {
                context.Position++;
                continue;
            }

            // Should be an array close at this point
            if (!context.IsCurrentToken(SeparatorKind.ArrayClose))
            {
                // Failed to find group end, so give error and stop parsing
                IToken currentToken = context.Tokens[context.Position];
                context.CompileContext.PushError(
                    $"Expected '{TokenSeparator.KindToString(SeparatorKind.Comma)}' or " +
                    $"'{TokenSeparator.KindToString(SeparatorKind.ArrayClose)}', " +
                    $"got '{currentToken}'", currentToken);
                break;
            }
        }
        context.EnsureToken(SeparatorKind.ArrayClose);

        return result;
    }

    /// <inheritdoc/>
    public IASTNode PostProcess(ParseContext context)
    {
        for (int i = 0; i < Arguments.Count; i++)
        {
            Arguments[i] = Arguments[i].PostProcess(context);
        }

        // Perform specific function optimizations
        return FunctionName switch
        {
            "ord" => OptimizeOrd(),
            "chr" => OptimizeChr(context),
            "int64" => OptimizeInt64(),
            "real" => OptimizeReal(context),
            "string" => OptimizeString(context),
            _ => this,
        };
    }

    /// <inheritdoc/>
    public IASTNode Duplicate(ParseContext context)
    {
        List<IASTNode> newArguments = new(Arguments);
        for (int i = 0; i < newArguments.Count; i++)
        {
            newArguments[i] = newArguments[i].Duplicate(context);
        }
        return new SimpleFunctionCallNode(FunctionName, BuiltinFunction, newArguments)
        {
            IsStatement = IsStatement
        };
    }

    /// <summary>
    /// Optimizes an ord() function call if possible, returning an optimized node.
    /// </summary>
    private IASTNode OptimizeOrd()
    {
        // Can only optimize if a string is passed in
        if (Arguments is not [StringNode str])
        {
            return this;
        }

        // Calculate numerical value and return number node
        int value;
        if (str.Value.Length == 0)
        {
            // Empty string defined as 0
            value = 0;
        }
        else
        {
            // Convert string to UTF-8 byte(s)
            Span<byte> bytes = stackalloc byte[4];
            Encoding.UTF8.GetBytes(str.Value, bytes);

            // Calculate value based on UTF-8 spec and GameMaker implementation
            value = bytes[0];
            if ((value & 0x80) != 0)
            {
                // More than 1 byte to parse
                if ((value & 0xF8) != 0xF0)
                {
                    if ((value & 0x20) == 0)
                    {
                        // 2 bytes to parse
                        value = ((value & 0x1F) << 6) | (bytes[1] & 0x3F);
                    }
                    else
                    {
                        // 3 bytes to parse
                        value = ((value & 0xF) << 12) | ((bytes[1] & 0x3F) << 6) | (bytes[2] & 0x3F);
                    }
                }
                else
                {
                    // 4 bytes to parse
                    value = ((value & 0x7) << 18) | ((bytes[1] & 0x3F) << 12) | ((bytes[2] & 0x3F) << 6) | (bytes[3] & 0x3F);
                }
            }
        }
        return new NumberNode(value, NearbyToken);
    }

    /// <summary>
    /// Optimizes a chr() function call if possible, returning an optimized node.
    /// </summary>
    private IASTNode OptimizeChr(ParseContext context)
    {
        // Can only optimize if a number is passed in
        if (Arguments is not [IConstantASTNode constant])
        {
            return this;
        }

        // Get numerical value
        int value;
        if (constant is NumberNode numberNode)
        {
            value = (int)Math.Max(0, Convert.ToInt64(numberNode.Value));
        }
        else if (constant is Int64Node int64Node)
        {
            value = (int)Math.Max(0, int64Node.Value);
        }
        else
        {
            // No valid number
            return this;
        }

        // Convert to a character (as a string node)
        string str;
        try
        {
            str = char.ConvertFromUtf32(value);
        }
        catch (ArgumentOutOfRangeException)
        {
            str = "X";
            // TODO: compiler warning or error?
        }
        return new StringNode(str, NearbyToken);
    }

    /// <summary>
    /// Optimizes an int64() function call if possible, returning an optimized node.
    /// </summary>
    private IASTNode OptimizeInt64()
    {
        // Can only optimize if a number is passed in
        if (Arguments is not [IConstantASTNode constant])
        {
            return this;
        }

        // Get numerical value
        long value;
        if (constant is NumberNode numberNode)
        {
            value = (long)numberNode.Value;
        }
        else if (constant is Int64Node int64Node)
        {
            value = int64Node.Value;
        }
        else
        {
            // No valid number
            return this;
        }

        // Return Int64Node version of number
        return new Int64Node(value, NearbyToken);
    }

    /// <summary>
    /// Optimizes a real() function call if possible, returning an optimized node.
    /// </summary>
    private IASTNode OptimizeReal(ParseContext context)
    {
        // Can only optimize if a number is passed in
        if (Arguments is not [IConstantASTNode constant])
        {
            return this;
        }

        // Can only optimize if enabled
        if (!context.CompileContext.GameContext.UsingStringRealOptimizations)
        {
            return this;
        }

        // Convert to double value
        double value;
        switch (constant)
        {
            case NumberNode numberNode:
                value = numberNode.Value;
                break;

            case Int64Node int64Node:
                value = int64Node.Value;
                break;

            case StringNode { Value: string str } stringNode:
                // Attempt simple parse first
                if (double.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                {
                    break;
                }

                // Remove underscores and trim string
                str = str.Replace("_", "").Trim();

                // Attempt hex literal parse
                if (str.StartsWith("0x", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (long.TryParse(str[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out long hex))
                    {
                        // Value successfully parsed
                        value = hex;
                        break;
                    }
                    
                    // Hex failed to parse
                    context.CompileContext.PushError($"Failed to convert \"{stringNode.Value}\" to real number", NearbyToken);
                    return this;
                }

                // Attempt binary literal parse
                if (str.StartsWith("0b", StringComparison.InvariantCultureIgnoreCase))
                {
                    // Build number one binary digit at a time
                    long binary = 0;
                    for (int i = 2; i < str.Length; i++)
                    {
                        if (str[i] == '0')
                        {
                            binary <<= 1;
                        }
                        else if (str[i] == '1')
                        {
                            binary <<= 1;
                            binary |= 1;
                        }
                        else
                        {
                            // Binary failed to parse
                            context.CompileContext.PushError($"Failed to convert \"{stringNode.Value}\" to real number", NearbyToken);
                            return this;
                        }
                    }

                    // Value successfully parsed
                    value = binary;
                    break;
                }

                // No successful string -> real conversion found
                context.CompileContext.PushError($"Failed to convert \"{stringNode.Value}\" to real number", NearbyToken);
                return this;

            default:
                return this;
        }

        // Return NumberNode version of number
        return new NumberNode(value, NearbyToken);
    }

    /// <summary>
    /// Optimizes a string() function call if possible, returning an optimized node.
    /// </summary>
    private IASTNode OptimizeString(ParseContext context)
    {
        // TODO: handle string interpolation here as well?

        // Can only optimize if a string is passed in
        if (Arguments is not [StringNode str])
        {
            return this;
        }

        // Can only optimize if enabled
        if (!context.CompileContext.GameContext.UsingStringRealOptimizations)
        {
            return this;
        }

        // Simply return the inner string node
        return str;
    }

    /// <summary>
    /// Post-processes this node, only modifying children, rather than potentially returning a new instance.
    /// </summary>
    public void PostProcessChildrenOnly(ParseContext context)
    {
        for (int i = 0; i < Arguments.Count; i++)
        {
            Arguments[i] = Arguments[i].PostProcess(context);
        }
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
    /// Generates code for this function call, using a direct call (not any indirect variables, etc.).
    /// </summary>
    public void GenerateDirectCode(BytecodeContext context, FunctionScope? overrideCallScope = null)
    {
        // Handle array copy-on-write
        if (context.CanGenerateArrayOwners)
        {
            if (ArrayOwners.IsArraySetFunction(this))
            {
                ArrayOwners.GenerateSetArrayOwner(context, this);
            }
        }

        // Push arguments to stack
        GenerateArguments(context);

        // Emit actual call instruction
        FunctionPatch funcPatch = new(overrideCallScope ?? context.CurrentScope, FunctionName, BuiltinFunction);
        context.EmitCall(funcPatch, Arguments.Count);
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
        IGameContext gameContext = context.CompileContext.GameContext;
        bool isGlobalFunction =
            context.IsGlobalFunctionName(FunctionName) ||
            (context.CompileContext.ScriptKind == CompileScriptKind.GlobalScript && context.RootScope.IsFunctionDeclaredImmediately(FunctionName));
        if (isGlobalFunction || context.IsFunctionDeclaredInCurrentScope(FunctionName))
        {
            // Function is in scope to be called directly, so do that
            if (gameContext.UsingNewFunctionResolution && !isGlobalFunction && !context.CurrentScope.IsFunctionDeclaredImmediately(FunctionName))
            {
                // Because we can't have nice things, this is actually an indirect direct call.
                // First, push arguments to the stack.
                GenerateArguments(context);

                // Use current self instance
                context.EmitCall(FunctionPatch.FromBuiltin(context, VMConstants.SelfFunction), 0);

                // Push reference to function
                context.Emit(ExtendedOpcode.PushReference, new LocalFunctionPatch(null, context.CurrentScope, FunctionName));

                // Emit actual call
                context.EmitCallVariable(Arguments.Count);
                context.PushDataType(DataType.Variable);

                // If this node is a statement, remove result from stack
                if (IsStatement)
                {
                    context.Emit(Opcode.PopDelete, context.PopDataType());
                }
            }
            else
            {
                // Regular direct call
                GenerateDirectCode(context);
            }
        }
        else
        {
            // Not a global function name - ensure it's at least GMLv2
            if (!gameContext.UsingGMLv2)
            {
                context.CompileContext.PushError($"Failed to find function \"{FunctionName}\"", NearbyToken);
            }

            // This is a single variable function call - convert to a variable
            SimpleVariableNode varNode = new(FunctionName, gameContext.Builtins.LookupBuiltinVariable(FunctionName));
            IAssignableASTNode assignable = varNode.ResolveStandaloneType(context);

            // If still actually a simple variable node, compile it here, otherwise defer to general function call
            if (assignable is SimpleVariableNode finalVarNode)
            {
                // Push arguments to stack
                GenerateArguments(context);

                // Push instance to stack
                string functionToCall = finalVarNode.ExplicitInstanceType switch
                {
                    InstanceType.Other =>   VMConstants.OtherFunction,
                    InstanceType.Global =>  VMConstants.GlobalFunction,
                    _ =>                    VMConstants.SelfFunction
                };
                context.EmitCall(FunctionPatch.FromBuiltin(context, functionToCall), 0);

                // Compile variable
                finalVarNode.IsFunctionCall = true;
                finalVarNode.GenerateCode(context);
                context.PopDataType();

                // Emit actual call
                context.EmitCallVariable(Arguments.Count);
                context.PushDataType(DataType.Variable);

                // If this node is a statement, remove result from stack
                if (IsStatement)
                {
                    context.Emit(Opcode.PopDelete, context.PopDataType());
                }
            }
            else
            {
                // Convert to a general function call node
                FunctionCallNode funcCall = new(NearbyToken, assignable, Arguments)
                {
                    IsStatement = IsStatement
                };
                funcCall.GenerateCode(context);
            }
        }
    }

    /// <inheritdoc/>
    public IEnumerable<IASTNode> EnumerateChildren()
    {
        foreach (IASTNode argument in Arguments)
        {
            yield return argument;
        }
    }
}
