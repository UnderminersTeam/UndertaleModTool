/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;
using System.Xml.Linq;
using Underanalyzer.Compiler.Bytecode;
using Underanalyzer.Compiler.Lexer;
using Underanalyzer.Compiler.Parser;
using static Underanalyzer.IGMInstruction;

namespace Underanalyzer.Compiler.Nodes;

/// <summary>
/// Represents a simple variable reference in the AST.
/// </summary>
internal sealed class SimpleVariableNode : IAssignableASTNode, IVariableASTNode
{
    /// <inheritdoc/>
    public string VariableName { get; }

    /// <inheritdoc/>
    public IBuiltinVariable? BuiltinVariable { get; }

    /// <summary>
    /// Whether this variable node has an explicit instance type set on it.
    /// </summary>
    public bool HasExplicitInstanceType { get; private set; } = false;

    /// <summary>
    /// The explicit instance type set on this variable node, if <see cref="HasExplicitInstanceType"/> is <see langword="true"/>.
    /// </summary>
    public InstanceType ExplicitInstanceType { get; private set; }

    /// <summary>
    /// Whether this is a variable node as used in a function call.
    /// </summary>
    public bool IsFunctionCall { get; set; } = false;

    /// <summary>
    /// Whether this is a variable node that was collapsed from a <see cref="DotVariableNode"/>.
    /// </summary>
    public bool CollapsedFromDot { get; set; } = false;

    /// <summary>
    /// Whether this is a variable node currently on the leftmost side of a <see cref="DotVariableNode"/>.
    /// </summary>
    public bool LeftmostSideOfDot { get; set; } = false;

    /// <summary>
    /// Whether this is a variable node that is assigned to as part of a struct instantiation.
    /// </summary>
    public bool StructVariable { get; set; } = false;

    /// <summary>
    /// Whether this is a variable node that was collapsed from a <see cref="DotVariableNode"/> with a room instance ID on the left side.
    /// </summary>
    public bool RoomInstanceVariable { get; set; } = false;

    /// <inheritdoc/>
    public IToken? NearbyToken { get; init; }

    // Set of built-in argument variables
    public static readonly HashSet<string> BuiltinArgumentVariables =
    [
        "argument0", "argument1", "argument2", "argument3",
        "argument4", "argument5", "argument6", "argument7",
        "argument8", "argument9", "argument10", "argument11",
        "argument12", "argument13", "argument14", "argument15"
    ];

    /// <summary>
    /// Creates a simple variable reference node, given the provided variable token.
    /// </summary>
    public SimpleVariableNode(TokenVariable token)
    {
        NearbyToken = token;
        VariableName = token.Text;
        BuiltinVariable = token.BuiltinVariable;
    }

    /// <summary>
    /// Creates a simple variable reference node, given the provided name and builtin variable.
    /// </summary>
    public SimpleVariableNode(string variableName, IBuiltinVariable? builtinVariable)
    {
        VariableName = variableName;
        BuiltinVariable = builtinVariable;
    }

    /// <summary>
    /// Creates a simple variable reference node, given the provided name, builtin variable, and instance type.
    /// </summary>
    public SimpleVariableNode(string variableName, IBuiltinVariable? builtinVariable, InstanceType explicitInstanceType)
    {
        VariableName = variableName;
        BuiltinVariable = builtinVariable;
        SetExplicitInstanceType(explicitInstanceType);
    }

    /// <summary>
    /// Sets an explicit instance type on this variable node.
    /// </summary>
    public void SetExplicitInstanceType(InstanceType instanceType)
    {
        ExplicitInstanceType = instanceType;
        HasExplicitInstanceType = true;
    }

    /// <inheritdoc/>
    public IASTNode PostProcess(ParseContext context)
    {
        return ResolveStandaloneType(context);
    }

    /// <inheritdoc/>
    public IASTNode Duplicate(ParseContext context)
    {
        return new SimpleVariableNode(VariableName, BuiltinVariable)
        {
            ExplicitInstanceType = ExplicitInstanceType,
            HasExplicitInstanceType = HasExplicitInstanceType,
            IsFunctionCall = IsFunctionCall,
            CollapsedFromDot = CollapsedFromDot,
            LeftmostSideOfDot = LeftmostSideOfDot,
            NearbyToken = NearbyToken
        };
    }

    /// <summary>
    /// Creates a variable patch for this simple variable node.
    /// </summary>
    private VariablePatch CreateVariablePatch(BytecodeContext context)
    {
        VariablePatch varPatch = new(
            VariableName, 
            ExplicitInstanceType, 
            RoomInstanceVariable ? VariableType.Instance : VariableType.Normal, 
            BuiltinVariable is not null
        );

        if (ExplicitInstanceType == InstanceType.Self && !StructVariable)
        {
            // Change instruction encoding to builtin (weird compiler quirk), when either a function call,
            // or in newer GML versions when not on the RHS of a dot variable.
            if (IsFunctionCall || (!CollapsedFromDot && context.CompileContext.GameContext.UsingSelfToBuiltin))
            {
                varPatch.InstructionInstanceType = InstanceType.Builtin;
            }
        }

        return varPatch;
    }

    /// <inheritdoc/>
    public void GenerateCode(BytecodeContext context)
    {
        // Check if this is a function and generate code accordingly
        IGameContext gameContext = context.CompileContext.GameContext;
        bool isGlobalFunction = context.IsGlobalFunctionName(VariableName);
        bool isLocalGlobalFunction = context.CompileContext.ScriptKind == CompileScriptKind.GlobalScript && context.RootScope.IsFunctionDeclaredImmediately(VariableName);
        if (ExplicitInstanceType == InstanceType.Self && !CollapsedFromDot && (isGlobalFunction || isLocalGlobalFunction || context.IsFunctionDeclaredInCurrentScope(VariableName)))
        {
            if (!LeftmostSideOfDot && gameContext.UsingFunctionScriptReferences)
            {
                if (isLocalGlobalFunction)
                {
                    // Push reference for local functions inside of global scope
                    if (!context.CurrentScope.GeneratingDotVariableCall)
                    {
                        context.Emit(ExtendedOpcode.PushReference, new LocalFunctionPatch(null, context.RootScope, VariableName));
                        context.PushDataType(DataType.Variable);
                    }
                    else
                    {
                        context.EmitPushFunction(new LocalFunctionPatch(null, context.RootScope, VariableName));
                        context.PushDataType(DataType.Int32);
                    }
                }
                else if (gameContext.UsingNewFunctionResolution && context.CurrentScope.IsFunctionDeclared(gameContext, VariableName))
                {
                    // With new function resolution, push references to local functions, even for non-global scope
                    if (!context.CurrentScope.GeneratingDotVariableCall)
                    {
                        context.Emit(ExtendedOpcode.PushReference, new LocalFunctionPatch(null, context.CurrentScope, VariableName));
                        context.PushDataType(DataType.Variable);
                    }
                    else
                    {
                        context.EmitPushFunction(new FunctionPatch(context.CurrentScope, VariableName, null));
                        context.PushDataType(DataType.Int32);
                    }
                }
                else if (gameContext.GetScriptIdByFunctionName(VariableName, out int _) || gameContext.GetScriptId(VariableName, out int _))
                {
                    // Push reference for all global functions defined in global scripts
                    if (!context.CurrentScope.GeneratingDotVariableCall || gameContext.UsingNewFunctionResolution)
                    {
                        context.Emit(ExtendedOpcode.PushReference, new FunctionPatch(context.CurrentScope, VariableName));
                        context.PushDataType(DataType.Variable);
                    }
                    else
                    {
                        context.EmitPushFunction(new FunctionPatch(context.CurrentScope, VariableName, null));
                        context.PushDataType(DataType.Int32);
                    }
                }
                else
                {
                    // Push a regular function reference, for local functions (prior to new function resolution),
                    // as well as any other global functions, like builtin functions and extension functions.
                    context.EmitPushFunction(new FunctionPatch(context.CurrentScope, VariableName, gameContext.Builtins.LookupBuiltinFunction(VariableName)));
                    context.PushDataType(DataType.Int32);
                }
            }
            else if (gameContext.UsingGMLv2)
            {
                // Push function reference
                context.EmitPushFunction(new FunctionPatch(context.CurrentScope, VariableName, gameContext.Builtins.LookupBuiltinFunction(VariableName)));
                context.PushDataType(DataType.Int32);
            }
            else
            {
                // Push script ID
                if (gameContext.GetScriptId(VariableName, out int scriptId))
                {
                    NumberNode.GenerateCode(context, scriptId);
                }
                else
                {
                    context.CompileContext.PushError($"Failed to find script with name \"{VariableName}\" (note: cannot use built-in functions directly in this GameMaker version)", NearbyToken);
                    context.PushDataType(DataType.Int32);
                }
            }

            // If leftmost side of dot, generate static_get call
            if (gameContext.UsingGMLv2 && LeftmostSideOfDot)
            {
                context.ConvertDataType(DataType.Variable);
                context.EmitCall(FunctionPatch.FromBuiltin(context, VMConstants.StaticGetFunction), 1);
                context.PushDataType(DataType.Variable);
            }
            return;
        }

        // Get correct opcode to generate
        Opcode opcode = ExplicitInstanceType switch
        {
            InstanceType.Local =>   Opcode.PushLocal,
            InstanceType.Global =>  Opcode.PushGlobal,
            InstanceType.Builtin => Opcode.PushBuiltin,
            InstanceType.Argument => Opcode.Push,
            _ => (BuiltinVariable is null || !BuiltinVariable.IsGlobal) ? Opcode.Push : Opcode.PushBuiltin,
        };

        // Emit instruction to push (and push data type)
        context.Emit(opcode, CreateVariablePatch(context), DataType.Variable);
        context.PushDataType(DataType.Variable);
    }

    /// <inheritdoc/>
    public void GenerateAssignCode(BytecodeContext context)
    {
        // Simple variable store
        context.Emit(Opcode.Pop, CreateVariablePatch(context), DataType.Variable, context.PopDataType());
    }

    /// <inheritdoc/>
    public void GenerateCompoundAssignCode(BytecodeContext context, IASTNode expression, Opcode operationOpcode)
    {
        // Push this variable
        VariablePatch varPatch = CreateVariablePatch(context);
        context.Emit(Opcode.Push, varPatch, DataType.Variable);

        // Push the expression
        expression.GenerateCode(context);

        // Perform operation
        AssignNode.PerformCompoundOperation(context, operationOpcode);

        // Normal assign
        context.Emit(Opcode.Pop, varPatch, DataType.Variable, DataType.Variable);
    }

    /// <inheritdoc/>
    public void GeneratePrePostAssignCode(BytecodeContext context, bool isIncrement, bool isPre, bool isStatement)
    {
        // Push this variable
        VariablePatch varPatch = CreateVariablePatch(context);
        context.Emit(Opcode.Push, varPatch, DataType.Variable);

        // Postfix expression: duplicate original value
        if (!isStatement && !isPre)
        {
            context.EmitDuplicate(DataType.Variable, 0);
            context.PushDataType(DataType.Variable);
        }

        // Push the expression
        context.Emit(Opcode.Push, (short)1, DataType.Int16);

        // Perform operation
        context.Emit(isIncrement ? Opcode.Add : Opcode.Subtract, DataType.Int32, DataType.Variable);

        // Prefix expression: duplicate new value
        if (!isStatement && isPre)
        {
            context.EmitDuplicate(DataType.Variable, 0);
            context.PushDataType(DataType.Variable);
        }

        // Normal assign
        context.Emit(Opcode.Pop, varPatch, DataType.Variable, DataType.Variable);
    }

    /// <inheritdoc/>
    public IEnumerable<IASTNode> EnumerateChildren()
    {
        return [];
    }

    /// <summary>
    /// Creates an argument variable for the given argument index.
    /// </summary>
    public static IAssignableASTNode CreateArgumentVariable(ISubCompileContext context, IToken? nearbyToken, int argumentIndex, bool useBuiltinInstanceType = false)
    {
        if (argumentIndex < 16)
        {
            // Arguments 0 through 15 have unique variable names
            string argName = $"argument{argumentIndex}";
            SimpleVariableNode argVar = new(argName, context.CompileContext.GameContext.Builtins.LookupBuiltinVariable(argName));
            argVar.SetExplicitInstanceType(useBuiltinInstanceType ? InstanceType.Builtin : InstanceType.Argument);
            return argVar;
        }
        else
        {
            // Arguments 16 and above use array accessors
            const string argName = "argument";
            SimpleVariableNode argVar = new(argName, context.CompileContext.GameContext.Builtins.LookupBuiltinVariable(argName));
            argVar.SetExplicitInstanceType(InstanceType.Argument /* Note: always use Argument here, apparently... */);
            NumberNode argNumberNode = new(argumentIndex, nearbyToken);
            AccessorNode accessorArgVar = new(nearbyToken, argVar, AccessorNode.AccessorKind.Array, argNumberNode);
            return accessorArgVar;
        }
    }

    /// <summary>
    /// Resolves the final variable type (and scope in general) for a variable, given the current context, 
    /// the variable's name, and builtin variable information.
    /// </summary>
    public IAssignableASTNode ResolveStandaloneType(ISubCompileContext context)
    {
        // If an explicit instance type has already been defined, don't do anything else
        if (HasExplicitInstanceType)
        {
            return this;
        }

        // Resolve local variables (overrides everything else)
        if (context.CurrentScope.IsLocalDeclared(VariableName))
        {
            SetExplicitInstanceType(InstanceType.Local);
            return this;
        }

        // GMLv2 has other instance types to be resolved
        if (context.CompileContext.GameContext.UsingGMLv2)
        {
            // Resolve static variables
            if (context.CurrentScope.IsStaticDeclared(VariableName))
            {
                SetExplicitInstanceType(InstanceType.Static);
                return this;
            }

            // Resolve argument names
            if (context.CurrentScope.TryGetArgumentIndex(VariableName, out int argumentIndex))
            {
                // Create new variable node altogether in this case
                return CreateArgumentVariable(context, NearbyToken, argumentIndex);
            }

            // Resolve old builtin argument variables
            if (BuiltinArgumentVariables.Contains(VariableName))
            {
                SetExplicitInstanceType(
                    context.CompileContext.GameContext.UsingSelfToBuiltin && context.CurrentScope == context.RootScope ? 
                    InstanceType.Argument : 
                    InstanceType.Builtin);
                return this;
            }

            // Resolve argument array
            if (VariableName == "argument")
            {
                SetExplicitInstanceType(InstanceType.Argument);
                return this;
            }

            // Resolve builtin variables
            if (BuiltinVariable is not null)
            {
                SetExplicitInstanceType(BuiltinVariable.IsGlobal ? InstanceType.Builtin : InstanceType.Self);
                return this;
            }
        }

        // If nothing matched, default to self
        SetExplicitInstanceType(InstanceType.Self);
        return this;
    }

    /// <summary>
    /// Creates a simple variable node for the "undefined" GML keyword, which is implemented as a variable.
    /// </summary>
    public static SimpleVariableNode CreateUndefined(ParseContext context)
    {
        return new SimpleVariableNode("undefined", context.CompileContext.GameContext.Builtins.LookupBuiltinVariable("undefined"));
    }
}
