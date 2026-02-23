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
/// Represents a function declaration in the AST.
/// </summary>
internal sealed class FunctionDeclNode : IMaybeStatementASTNode
{
    /// <summary>
    /// Function scope for this function declaration.
    /// </summary>
    public FunctionScope Scope { get; }

    /// <summary>
    /// Name of the function being declared, or null if anonymous.
    /// </summary>
    public string? FunctionName { get; }

    /// <summary>
    /// List of argument names in the function declaration, in order.
    /// </summary>
    public List<string> ArgumentNames { get; }

    /// <summary>
    /// Block that checks and assigns default argument values, if any are present.
    /// </summary>
    public BlockNode? DefaultValueBlock { get; private set; }

    /// <summary>
    /// Main body of the function declaration.
    /// </summary>
    public BlockNode Body { get; }

    /// <summary>
    /// If not null, this is the call used for inheritance (as a constructor function).
    /// </summary>
    public SimpleFunctionCallNode? InheritanceCall { get; }

    /// <summary>
    /// Whether this function declaration is a struct literal.
    /// </summary>
    public bool IsStruct { get; }

    /// <summary>
    /// Whether this function declaration is a constructor function.
    /// </summary>
    /// <remarks>
    /// This is always <see langword="true"/> when <see cref="IsStruct"/> is <see langword="true"/>.
    /// </remarks>
    public bool IsConstructor { get; }

    /// <inheritdoc/>
    public bool IsStatement { get; set; } = false;

    /// <inheritdoc/>
    public IToken? NearbyToken { get; }

    private FunctionDeclNode(FunctionScope scope, IToken? token, string? functionName, 
                             List<string> argumentNames, BlockNode? defaultValueBlock, BlockNode body,
                             SimpleFunctionCallNode? inheritanceCall,
                             bool isStruct, bool isConstructor)
    {
        Scope = scope;
        NearbyToken = token;
        FunctionName = functionName;
        ArgumentNames = argumentNames;
        DefaultValueBlock = defaultValueBlock;
        Body = body;
        InheritanceCall = inheritanceCall;
        IsStruct = isStruct;
        IsConstructor = isConstructor;
    }

    /// <summary>
    /// Generates a default argument value check and assignment.
    /// </summary>
    private static IfNode GenerateDefaultCheckAndAssign(ParseContext context, int argumentIndex, IASTNode value)
    {
        // Create condition
        SimpleVariableNode undefined = SimpleVariableNode.CreateUndefined(context);
        bool useBuiltinInstanceType = context.CompileContext.GameContext.UsingBuiltinDefaultArguments;
        IAssignableASTNode argumentVar = SimpleVariableNode.CreateArgumentVariable(context, value.NearbyToken, argumentIndex, useBuiltinInstanceType);
        BinaryChainNode condition = new(value.NearbyToken, [argumentVar, undefined], [BinaryChainNode.BinaryOperation.CompareEqual]);

        // Create assignment statement
        argumentVar = SimpleVariableNode.CreateArgumentVariable(context, value.NearbyToken, argumentIndex, useBuiltinInstanceType);
        AssignNode assign = new(AssignNode.AssignKind.Normal, argumentVar, value);

        // Return final if statement check
        return new IfNode(value.NearbyToken, condition, assign, null);
    }

    /// <summary>
    /// Creates a function declaration node, parsing from the given context's current position.
    /// </summary>
    public static FunctionDeclNode? Parse(ParseContext context, TokenKeyword tokenKeyword)
    {
        // Parse function name, if present
        string? functionName = null;
        if (!context.EndOfCode && context.Tokens[context.Position] is TokenFunction tokenFunction)
        {
            functionName = tokenFunction.Text;
            context.Position++;
        }

        // Check for "("
        if (context.EnsureToken(SeparatorKind.GroupOpen) is null)
        {
            return null;
        }

        // Enter a new function scope
        FunctionScope oldScope = context.CurrentScope;
        FunctionScope newScope = new(oldScope, true);
        context.CurrentScope = newScope;

        // Parse arguments and default values
        List<string> argumentNames = new(16);
        BlockNode? defaultValueBlock = null;
        while (!context.EndOfCode && context.Tokens[context.Position] is TokenVariable tokenVariable)
        {
            // Add argument and check for duplicate argument names
            string argumentName = tokenVariable.Text;
            if (argumentNames.Contains(argumentName))
            {
                context.CompileContext.PushError($"Duplicate argument name '{argumentName}'", tokenVariable);
            }
            argumentNames.Add(argumentName);
            context.Position++;

            // Check for default value
            if (context.IsCurrentToken(OperatorKind.Assign) || context.IsCurrentToken(OperatorKind.Assign2))
            {
                context.Position++;

                // Parse default value
                if (Expressions.ParseExpression(context) is IASTNode defaultValueExpr)
                {
                    // Generate code for checking/assigning default value for this argument
                    defaultValueBlock ??= BlockNode.CreateEmpty(tokenKeyword, 16);
                    defaultValueBlock.Children.Add(GenerateDefaultCheckAndAssign(context, argumentNames.Count - 1, defaultValueExpr));
                }
                else
                {
                    // Failed to parse expression, stop parsing arguments
                    break;
                }
            }

            // If at end of code, stop here
            if (context.EndOfCode)
            {
                break;
            }

            // We expect either a comma (separating the arguments), or a group close
            if (context.IsCurrentToken(SeparatorKind.Comma))
            {
                context.Position++;
                continue;
            }

            // Should be a group close at this point
            if (!context.IsCurrentToken(SeparatorKind.GroupClose))
            {
                // Failed to find group end, so give error and stop parsing
                IToken currentToken = context.Tokens[context.Position];
                context.CompileContext.PushError(
                    $"Expected '{TokenSeparator.KindToString(SeparatorKind.Comma)}' or " +
                    $"'{TokenSeparator.KindToString(SeparatorKind.GroupClose)}', " +
                    $"got '{currentToken}'", currentToken);
                break;
            }
        }

        // Check for ")"
        if (context.EnsureToken(SeparatorKind.GroupClose) is null)
        {
            return null;
        }

        // Assign arguments to function scope
        newScope.DeclareArguments(argumentNames);

        // Check for inheritance
        SimpleFunctionCallNode? inheritanceCall = null;
        if (context.IsCurrentToken(SeparatorKind.Colon))
        {
            context.Position++;

            // Parse inheritance call
            if (!context.EndOfCode && context.Tokens[context.Position] is TokenFunction tokenInheritFunction)
            {
                context.Position++;
                inheritanceCall = new SimpleFunctionCallNode(context, tokenInheritFunction);
            }
            else
            {
                // Failed to parse inheritance call
                return null;
            }
        }

        // Check for attributes (like "constructor")
        bool isConstructor = false;
        while (!context.EndOfCode && context.Tokens[context.Position] is TokenVariable tokenAttribute)
        {
            if (tokenAttribute.Text == "constructor")
            {
                isConstructor = true;
            }
            else
            {
                context.CompileContext.PushError($"Unknown function attribute '{tokenAttribute.Text}'", tokenAttribute);
            }

            // Move to next attribute/next token after attributes (and skip commas)
            context.Position++;
            if (context.IsCurrentToken(SeparatorKind.Comma))
            {
                context.Position++;
            }
        }

        // If inheriting, ensure that this function is a constructor as well
        if (inheritanceCall is not null && !isConstructor)
        {
            context.CompileContext.PushError("Only constructor functions can inherit", inheritanceCall.NearbyToken);
        }

        // Parse main body block
        BlockNode body = BlockNode.ParseRegular(context);

        // Exit function scope
        context.CurrentScope = oldScope;

        // Return new function declaration node
        return new FunctionDeclNode(newScope, tokenKeyword, functionName, argumentNames, defaultValueBlock, body, inheritanceCall, false, isConstructor);
    }

    /// <summary>
    /// Hoists a given expression for a struct literal, adding to the argument list, and returning a replacement variable.
    /// </summary>
    private static AccessorNode HoistStructValue(List<IASTNode> args, IASTNode toHoist)
    {
        SimpleVariableNode argumentVariable = new("argument", null);
        argumentVariable.SetExplicitInstanceType(InstanceType.Argument);
        AccessorNode accessor = new(toHoist.NearbyToken, argumentVariable, AccessorNode.AccessorKind.Array, new NumberNode(args.Count - 1, toHoist.NearbyToken));
        args.Add(toHoist);
        return accessor;
    }

    /// <summary>
    /// Creates a struct, as a simple function call node, parsing from the given context's current position.
    /// </summary>
    public static SimpleFunctionCallNode ParseStruct(ParseContext context, IToken tokenOpen)
    {
        // Enter a new function scope
        FunctionScope oldScope = context.CurrentScope;
        FunctionScope newScope = new(oldScope, true);
        context.CurrentScope = newScope;

        // Create new function declaration node
        BlockNode block = BlockNode.CreateEmpty(tokenOpen, 8);
        FunctionDeclNode decl = new(newScope, tokenOpen, null, [], null, block, null, true, true);
        List<IASTNode> args = new(9) { decl };

        // Read assignments from struct literal
        while (!context.EndOfCode && !context.IsCurrentToken(SeparatorKind.BlockClose, KeywordKind.End))
        {
            if (context.Tokens[context.Position] is not TokenVariable variable)
            {
                // Failed to find a variable here... check if a constant/asset reference, string, or keyword
                if (context.Tokens[context.Position] is TokenAssetReference assetReference)
                {
                    variable = new TokenVariable(assetReference);
                }
                else if (context.Tokens[context.Position] is TokenNumber { IsConstant: true } number)
                {
                    variable = new TokenVariable(number);
                }
                else if (context.Tokens[context.Position] is TokenString str)
                {
                    variable = new TokenVariable(str);
                }
                else if (context.Tokens[context.Position] is TokenKeyword keyword)
                {
                    variable = new TokenVariable(keyword);
                }
                else
                {
                    // Nothing matched; stop parsing
                    break;
                }
            }
            context.Position++;

            // If the next token is a comma or end, assume the value is simply the variable name itself.
            // Otherwise, parse actual value to assign.
            IASTNode value;
            if (context.IsCurrentToken(SeparatorKind.Comma) || context.IsCurrentToken(SeparatorKind.BlockClose, KeywordKind.End))
            {
                value = new SimpleVariableNode(variable);
            }
            else
            {
                // Parse ":"
                context.EnsureToken(SeparatorKind.Colon);

                // Parse expression to assign to the value
                if (Expressions.ParseExpression(context) is not IASTNode expr)
                {
                    // Failed to parse expression... stop parsing
                    break;
                }
                value = expr;
            }

            // Non-constant values get hoisted outside of the struct function itself
            if (value is not (IConstantASTNode or FunctionDeclNode))
            {
                if (value is SimpleFunctionCallNode { FunctionName: VMConstants.NewArrayFunction or VMConstants.NewObjectFunction } func)
                {
                    // Hoist individual elements of array/struct, rather than entire array/struct
                    for (int i = 0; i < func.Arguments.Count; i++)
                    {
                        if (func.Arguments[i] is not (IConstantASTNode or FunctionDeclNode))
                        {
                            // Array element is not constant itself, so hoist it
                            func.Arguments[i] = HoistStructValue(args, func.Arguments[i]);
                        }
                    }
                }
                else
                {
                    // Hoist entire value directly
                    value = HoistStructValue(args, value);
                }
            }

            // Create assignment statement
            SimpleVariableNode destination = new(variable)
            {
                StructVariable = true
            };
            block.Children.Add(new AssignNode(AssignNode.AssignKind.Normal, destination, value));

            // Expect "," or "}"
            if (context.IsCurrentToken(SeparatorKind.Comma))
            {
                context.Position++;
            }
            else if (!context.IsCurrentToken(SeparatorKind.BlockClose, KeywordKind.End))
            {
                // Stop parsing if unexpected token
                break;
            }
        }

        // Ensure block close
        context.EnsureToken(SeparatorKind.BlockClose, KeywordKind.End);

        // Exit function scope
        context.CurrentScope = oldScope;

        // Create function call node
        return new SimpleFunctionCallNode(VMConstants.NewObjectFunction, null, args);
    }

    /// <inheritdoc/>
    public IASTNode PostProcess(ParseContext context)
    {
        // Register function name if in the root scope of a global script, and not anonymous
        if (context.CurrentScope == context.RootScope && context.ParseGlobalFunctions is not null && FunctionName is not null)
        {
            if (!context.ParseGlobalFunctions.Add(FunctionName))
            {
                // Can't declare global functions with the same name twice...
                context.CompileContext.PushError($"Global function \"{FunctionName}\" declared more than once", NearbyToken);
            }
        }

        // Enter a new function scope
        FunctionScope oldScope = context.CurrentScope;
        context.CurrentScope = Scope;

        // Declare function in the current scope if named
        if (FunctionName is not null)
        {
            if (!oldScope.TryDeclareFunction(FunctionName))
            {
                context.CompileContext.PushError($"Function name \"{FunctionName}\" already declared in scope", NearbyToken);
            }
        }

        // Normal post-processing
        DefaultValueBlock?.PostProcessChildrenOnly(context);
        InheritanceCall?.PostProcessChildrenOnly(context);
        Scope.StaticInitializerBlock?.PostProcessChildrenOnly(context);
        Body.PostProcessChildrenOnly(context);

        // Exit function scope
        context.CurrentScope = oldScope;

        return this;
    }

    /// <inheritdoc/>
    public IASTNode Duplicate(ParseContext context)
    {
        return new FunctionDeclNode(
            Scope,
            NearbyToken,
            FunctionName,
            new(ArgumentNames),
            (BlockNode?)DefaultValueBlock?.Duplicate(context),
            (BlockNode)Body.Duplicate(context),
            (SimpleFunctionCallNode?)InheritanceCall?.Duplicate(context),
            IsStruct,
            IsConstructor)
        {
            IsStatement = IsStatement
        };
    }

    /// <inheritdoc/>
    public void GenerateCode(BytecodeContext context)
    {
        // Enter scope of this function
        FunctionScope oldScope = context.CurrentScope;
        context.CurrentScope = Scope;

        // Update array owner IDs if necessary
        if (context.CompileContext.GameContext.UsingArrayCopyOnWrite)
        {
            Scope.ArrayOwnerID = ++context.LastFunctionID;
        }

        // Skip past function body
        SingleForwardBranchPatch skipFunctionPatch = new(context, context.Emit(Opcode.Branch));

        // Register FunctionEntry instance
        FunctionEntry? parentEntry = context.CurrentFunctionEntry;
        FunctionEntry entry = new(
            parentEntry,
            Scope,
            context.Position,
            ArgumentNames.Count,
            IsConstructor,
            FunctionName,
            oldScope == context.RootScope,
            oldScope.StaticVariableName,
            IsStruct ? FunctionEntryKind.StructInstantiation : FunctionEntryKind.FunctionDeclaration
        );
        context.CurrentFunctionEntry = entry;
        context.FunctionEntries.Add(entry);

        // Assign function entry in the current scope if named
        if (FunctionName is not null)
        {
            oldScope.AssignFunctionEntry(FunctionName, entry);
        }

        // Create control flow context stack for the scope
        Scope.ControlFlowContexts = new(8);

        // Generate default argument assignments, before main body
        DefaultValueBlock?.GenerateCode(context);

        // Inheritance call, before main body
        if (InheritanceCall is SimpleFunctionCallNode inheritCall)
        {
            // Generate actual call
            if (oldScope.IsFunctionDeclared(context.CompileContext.GameContext, inheritCall.FunctionName))
            {
                // Override scope to be the outer scope for this call (but NOT its arguments), and generate a direct call
                inheritCall.GenerateDirectCode(context, oldScope);
            }
            else
            {
                // General case
                inheritCall.GenerateCode(context);
            }
            context.PopDataType();

            // Copy static variables from parent function (must be statically accessible from this scope)
            context.EmitPushFunction(new FunctionPatch(oldScope, inheritCall.FunctionName, inheritCall.BuiltinFunction));
            context.Emit(Opcode.Convert, DataType.Int32, DataType.Variable);
            context.EmitCall(FunctionPatch.FromBuiltin(context, VMConstants.CopyStaticFunction), 1);
        }

        // If this is a constructor, generate set static function call for certain GameMaker versions
        if (IsConstructor && context.CompileContext.GameContext.UsingConstructorSetStatic)
        {
            context.EmitCall(FunctionPatch.FromBuiltin(context, VMConstants.SetStaticFunction), 0);
        }

        // Static block, before main body
        if (Scope.StaticInitializerBlock is BlockNode staticBlock)
        {
            // If static has already initialized, branch past this block
            context.Emit(ExtendedOpcode.HasStaticInitialized);
            SingleForwardBranchPatch skipStaticPatch = new(context, context.Emit(Opcode.BranchTrue));

            // If not allowing re-entrant static, set the static flag here
            bool allowReentrantStatic = context.CompileContext.GameContext.UsingReentrantStatic;
            if (!allowReentrantStatic)
            {
                context.Emit(ExtendedOpcode.SetStaticInitialized);
            }

            // Compile block (as a static block, specifically)
            staticBlock.GenerateStaticCode(context);

            // Skip static patch ends up here
            skipStaticPatch.Patch(context);

            // If allowing re-entrant static, set the static flag here
            if (allowReentrantStatic)
            {
                context.Emit(ExtendedOpcode.SetStaticInitialized);
            }
        }

        // Generate main body
        Body.GenerateCode(context);
        context.Emit(Opcode.Exit, DataType.Int32);

        // Clean up control flow context stack for this scope (no longer need it)
        Scope.ControlFlowContexts = null;

        // Exit scope of this function
        context.CurrentFunctionEntry = parentEntry;
        context.CurrentScope = oldScope;

        // Skip function patch ends up here
        skipFunctionPatch.Patch(context);

        // Generate code to register function in variable form
        context.EmitPushFunction(new LocalFunctionPatch(entry));
        context.Emit(Opcode.Convert, DataType.Int32, DataType.Variable);
        if (IsConstructor)
        {
            context.EmitCall(FunctionPatch.FromBuiltin(context, VMConstants.NullObjectFunction), 0);
        }
        else
        {
            context.Emit(Opcode.PushImmediate, (short)(oldScope.GeneratingStaticBlock ? InstanceType.Static : InstanceType.Self), DataType.Int16);
            context.Emit(Opcode.Convert, DataType.Int32, DataType.Variable);
        }
        context.EmitCall(FunctionPatch.FromBuiltin(context, VMConstants.MethodFunction), 2);

        // Store this function to a variable, if not anonymous (or if a struct)
        if (FunctionName is not null)
        {
            context.EmitDuplicate(DataType.Variable, 0);
            if (context.CompileContext.GameContext.UsingNewFunctionVariables || FunctionName == context.CompileContext.GlobalScriptName)
            {
                context.Emit(Opcode.PushImmediate, (short)InstanceType.Self, DataType.Int16);
            }
            else
            {
                context.Emit(Opcode.PushImmediate, (short)InstanceType.Builtin, DataType.Int16);
            }
            context.Emit(Opcode.Pop, new VariablePatch(FunctionName, InstanceType.Self, VariableType.StackTop), DataType.Variable, DataType.Variable);
        }
        else if (IsStruct)
        {
            context.EmitDuplicate(DataType.Variable, 0);
            if (context.CompileContext.GameContext.UsingNewFunctionVariables)
            {
                context.Emit(Opcode.PushImmediate, (short)InstanceType.Global, DataType.Int16);
                context.Emit(Opcode.Pop, new StructVariablePatch(entry, InstanceType.Global, VariableType.StackTop), DataType.Variable, DataType.Variable);
            }
            else
            {
                context.Emit(Opcode.PushImmediate, (short)InstanceType.Static, DataType.Int16);
                context.Emit(Opcode.Pop, new StructVariablePatch(entry, InstanceType.Static, VariableType.StackTop), DataType.Variable, DataType.Variable);
            }
        }

        if (IsStatement)
        {
            // This is a statement, so remove method() result from the stack
            context.Emit(Opcode.PopDelete, DataType.Variable);
        }
        else
        {
            // This is not a statement, so result from method() is still on the stack
            context.PushDataType(DataType.Variable);
        }
    }

    /// <inheritdoc/>
    public IEnumerable<IASTNode> EnumerateChildren()
    {
        if (DefaultValueBlock is not null)
        {
            yield return DefaultValueBlock;
        }
        yield return Body;
        if (InheritanceCall is not null)
        {
            yield return InheritanceCall;
        }
    }
}
