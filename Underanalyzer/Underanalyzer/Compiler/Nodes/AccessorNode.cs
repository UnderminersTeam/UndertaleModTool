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
using static Underanalyzer.Compiler.Bytecode.BytecodeContext;
using static Underanalyzer.IGMInstruction;

namespace Underanalyzer.Compiler.Nodes;

/// <summary>
/// Represents an accessor/array index in the AST.
/// </summary>
internal sealed class AccessorNode : IAssignableASTNode
{
    /// <summary>
    /// Expression being accessed/indexed by this accessor.
    /// </summary>
    public IASTNode Expression { get; private set; }

    /// <summary>
    /// Kind of accessor.
    /// </summary>
    public AccessorKind Kind { get; }

    /// <summary>
    /// Expression inside of the accessor itself.
    /// </summary>
    public IASTNode AccessorExpression { get; private set; }

    /// <summary>
    /// Second expression inside of the accessor itself, if applicable.
    /// </summary>
    public IASTNode? AccessorExpression2 { get; private set; }

    /// <inheritdoc/>
    public IToken? NearbyToken { get; }

    /// <summary>
    /// Whether this is an accessor node currently on the leftmost side of a <see cref="DotVariableNode"/>.
    /// </summary>
    public bool LeftmostSideOfDot { get; set; } = false;

    /// <summary>
    /// Kinds of accessors.
    /// </summary>
    public enum AccessorKind
    {
        Array,
        ArrayDirect,
        List,
        Map,
        Grid,
        Struct
    }

    public AccessorNode(IToken? nearbyToken, IASTNode expression, AccessorKind kind, IASTNode accessorExpression, IASTNode? accessorExpression2 = null)
    {
        NearbyToken = nearbyToken;
        Expression = expression;
        Kind = kind;
        AccessorExpression = accessorExpression;
        AccessorExpression2 = accessorExpression2;
    }

    /// <summary>
    /// Creates and parses an accessor node, given the provided expression and accessor kind.
    /// </summary>
    public static AccessorNode? Parse(ParseContext context, TokenSeparator token, IASTNode expression, AccessorKind kind)
    {
        // Strings are not allowed for these specific accessor kinds
        bool disallowStrings = kind is AccessorKind.Array or AccessorKind.ArrayDirect or
                                       AccessorKind.List or AccessorKind.Grid;

        // Parse the main accessor expression
        if (Expressions.ParseExpression(context) is not IASTNode accessorExpression)
        {
            return null;
        }
        if (disallowStrings && accessorExpression is StringNode)
        {
            context.CompileContext.PushError("String used in accessor that does not support strings", accessorExpression.NearbyToken);
        }

        // Parse 2D array / grid secondary accessor expression
        IASTNode? accessorExpression2 = null;
        if (kind is AccessorKind.Array or AccessorKind.Grid && context.IsCurrentToken(SeparatorKind.Comma))
        {
            context.Position++;
            accessorExpression2 = Expressions.ParseExpression(context);
            if (accessorExpression2 is null)
            {
                return null;
            }
            if (disallowStrings && accessorExpression2 is StringNode)
            {
                context.CompileContext.PushError("String used in accessor that does not support strings", accessorExpression2.NearbyToken);
            }
        }
        else if (kind is AccessorKind.Grid)
        {
            context.CompileContext.PushError("Expected two arguments to grid accessor", token);
        }

        // All accessors end in "]"
        context.EnsureToken(SeparatorKind.ArrayClose);

        // Create final node
        return new AccessorNode(token, expression, kind, accessorExpression, accessorExpression2);
    }

    /// <summary>
    /// If this accessor node is for a 2D array, this converts the comma syntax to two separate accessors.
    /// </summary>
    public AccessorNode Convert2DArrayToTwoAccessors()
    {
        if (Kind is AccessorKind.Array && AccessorExpression2 is IASTNode expression2)
        {
            AccessorExpression2 = null;
            return new AccessorNode(NearbyToken, this, Kind, expression2);
        }
        return this;
    }

    /// <inheritdoc/>
    public IASTNode PostProcess(ParseContext context)
    {
        // Compiler quirk with rewriting constants in dot nodes earlier in some cases
        if (Expression is DotVariableNode { LeftExpression: NumberNode numberNode } dotVariableNode &&
            (context.CompileContext.GameContext.UsingSelfToBuiltin || numberNode.ConstantName == "global"))
        {
            dotVariableNode.LeftExpression = numberNode.PostProcess(context);
        }

        // Another strange compiler quirk with arguments in root scope...
        if (Expression is SimpleVariableNode { CollapsedFromDot: false, HasExplicitInstanceType: false } simpleVariableNode &&
            SimpleVariableNode.BuiltinArgumentVariables.Contains(simpleVariableNode.VariableName) &&
            context.CurrentScope == context.RootScope &&
            !context.CompileContext.GameContext.UsingSelfToBuiltin)
        {
            simpleVariableNode.SetExplicitInstanceType(InstanceType.Argument);
        }

        Expression = Expression.PostProcess(context);
        AccessorExpression = AccessorExpression.PostProcess(context);
        AccessorExpression2 = AccessorExpression2?.PostProcess(context);

        // TODO: perform post-processing to convert all non-Array accessors to function calls

        return this;
    }

    /// <inheritdoc/>
    public IASTNode Duplicate(ParseContext context)
    {
        return new AccessorNode(
            NearbyToken,
            Expression.Duplicate(context),
            Kind,
            AccessorExpression.Duplicate(context),
            AccessorExpression2?.Duplicate(context)
        );
    }

    /// <summary>
    /// Generates common code for generating array accessors on <see cref="IVariableASTNode"/> expressions.
    /// </summary>
    /// <returns>
    /// The <see cref="InstanceType"/> to use for the corresponding <see cref="Opcode.Push"/> or <see cref="Opcode.Pop"/>,
    /// as well as the <see cref="InstanceConversionType"/> used to convert the instance to an instance ID.
    /// </returns>
    private (InstanceType, InstanceConversionType) GenerateVariableCode(BytecodeContext context, IVariableASTNode variable)
    {
        // Generate instance code, and determine instance type to use for pushing/popping
        InstanceType instanceType;
        InstanceConversionType instanceConversionType;
        if (variable is SimpleVariableNode simpleVariable)
        {
            // Check if instance type should be transformed on simple variable (compiler quirk)
            InstanceType stackInstanceType = simpleVariable.ExplicitInstanceType;
            if (stackInstanceType == InstanceType.Self)
            {
                // Change instance type to builtin (weird compiler quirk), when either a function call,
                // or in newer GML versions when not on the RHS of a dot variable.
                if (simpleVariable.IsFunctionCall || (!LeftmostSideOfDot && !simpleVariable.CollapsedFromDot && context.CompileContext.GameContext.UsingSelfToBuiltin))
                {
                    stackInstanceType = InstanceType.Builtin;
                }
            }

            // Generate instance type
            NumberNode.GenerateCode(context, (int)stackInstanceType);
            instanceConversionType = context.ConvertToInstanceId();

            // Use variable's instance type
            instanceType = simpleVariable.ExplicitInstanceType;
            
            // Prior to GMLv2, Other becomes Self for arrays in particular
            if (instanceType == InstanceType.Other && !context.CompileContext.GameContext.UsingGMLv2)
            {
                instanceType = InstanceType.Self;
            }
        }
        else if (variable is DotVariableNode dotVariable)
        {
            // Generate instance on left side of dot, and convert to instance ID
            dotVariable.LeftExpression.GenerateCode(context);
            instanceConversionType = context.ConvertToInstanceId();

            // Self instance type is always used for stacktop
            instanceType = InstanceType.Self;
        }
        else
        {
            throw new InvalidOperationException();
        }

        // Generate array index
        AccessorExpression.GenerateCode(context);
        context.ConvertDataType(DataType.Int32);

        // Generate 2D array indices, if present
        if (AccessorExpression2 is IASTNode secondIndex)
        {
            context.Emit(ExtendedOpcode.CheckArrayIndex);
            context.Emit(Opcode.Push, (int)32000, DataType.Int32);
            context.Emit(Opcode.Multiply, DataType.Int32, DataType.Int32);
            secondIndex.GenerateCode(context);
            context.ConvertDataType(DataType.Int32);
            context.Emit(ExtendedOpcode.CheckArrayIndex);
            context.Emit(Opcode.Add, DataType.Int32, DataType.Int32);
        }

        return (instanceType, instanceConversionType);
    }

    /// <summary>
    /// Generates code for this node, as part of a chain of accessors.
    /// </summary>
    private void GenerateChainedCode(BytecodeContext context, bool isPop)
    {
        if (Expression is AccessorNode accessor)
        {
            // If expression is another accessor node, continue down chain
            accessor.GenerateChainedCode(context, isPop);

            // Generate current accessor
            AccessorExpression.GenerateCode(context);
            context.ConvertDataType(DataType.Int32);
            context.Emit(ExtendedOpcode.PushArrayContainer);
        }
        else if (Expression is IVariableASTNode variable)
        {
            // Generate common code to prepare for push
            (InstanceType pushInstanceType, _) = GenerateVariableCode(context, variable);

            // Simple variable push
            VariableType variableType = isPop ? VariableType.MultiPushPop : VariableType.MultiPush;
            VariablePatch varPatch = new(variable.VariableName, pushInstanceType, variableType, variable.BuiltinVariable is not null);
            context.Emit(Opcode.Push, varPatch, DataType.Variable);
        }
        else
        {
            throw new Exception("Invalid expression on accessor");
        }
    }

    /// <inheritdoc/>
    public void GenerateCode(BytecodeContext context)
    {
        // Generate differently depending on expression
        if (Expression is IVariableASTNode variable)
        {
            // Generate common code to prepare for push
            (InstanceType pushInstanceType, _) = GenerateVariableCode(context, variable);

            // Simple variable push
            VariablePatch varPatch = new(variable.VariableName, pushInstanceType, VariableType.Array, variable.BuiltinVariable is not null);
            context.Emit(Opcode.Push, varPatch, DataType.Variable);
            context.PushDataType(DataType.Variable);
        }
        else if (Expression is AccessorNode accessor)
        {
            // Multiple chained accessors. Generate chained code first
            accessor.GenerateChainedCode(context, false);

            // This accessor is the final one
            AccessorExpression.GenerateCode(context);
            context.ConvertDataType(DataType.Int32);
            context.Emit(ExtendedOpcode.PushArrayFinal);
            context.PushDataType(DataType.Variable);
        }
        else
        {
            throw new Exception("Invalid expression on accessor");
        }
    }

    /// <inheritdoc/>
    public void GenerateAssignCode(BytecodeContext context)
    {
        // In GMLv2, expression being assigned is converted to a variable type
        DataType storeType = context.PopDataType();
        if (storeType != DataType.Variable && context.CompileContext.GameContext.UsingGMLv2)
        {
            context.Emit(Opcode.Convert, storeType, DataType.Variable);
            storeType = DataType.Variable;
        }

        // Generate differently depending on expression
        if (Expression is IVariableASTNode variable)
        {
            // Generate common code to prepare for pop
            (InstanceType popInstanceType, _) = GenerateVariableCode(context, variable);

            // Simple variable store
            VariablePatch varPatch = new(variable.VariableName, popInstanceType, VariableType.Array, variable.BuiltinVariable is not null);
            context.Emit(Opcode.Pop, varPatch, DataType.Variable, storeType);
        }
        else if (Expression is AccessorNode accessor)
        {
            // Multiple chained accessors. Generate chained code first
            accessor.GenerateChainedCode(context, true);

            // This accessor is the final one
            AccessorExpression.GenerateCode(context);
            context.ConvertDataType(DataType.Int32);
            context.Emit(ExtendedOpcode.PopArrayFinal);
        }
        else
        {
            throw new Exception("Invalid expression on accessor");
        }
    }

    /// <inheritdoc/>
    public void GenerateCompoundAssignCode(BytecodeContext context, IASTNode expression, Opcode operationOpcode)
    {
        // Generate differently depending on expression
        if (Expression is IVariableASTNode variable)
        {
            // Generate common code to prepare for push and pop
            (InstanceType instanceType, InstanceConversionType instanceConversionType) = GenerateVariableCode(context, variable);

            // Duplicate instance ID and array index, so it can be stored back to later
            if (instanceConversionType == InstanceConversionType.StacktopId)
            {
                // 32-bit integer ID, 32-bit integer array index, AND actual instance (as variable type).
                // 8 bytes for two int32s, plus 16 bytes for RValue.
                context.EmitDuplicate(DataType.Int32, 5);
            }
            else
            {
                // Just two 32-bit integers to duplicate
                context.EmitDuplicate(DataType.Int32, 1);
            }

            // Simple variable push
            VariablePatch varPatch = new(variable.VariableName, instanceType, VariableType.Array, variable.BuiltinVariable is not null);
            context.Emit(Opcode.Push, varPatch, DataType.Variable);

            // Push the expression
            expression.GenerateCode(context);

            // Perform operation
            AssignNode.PerformCompoundOperation(context, operationOpcode);

            // Simple variable store, but denote pop order using data types
            context.Emit(Opcode.Pop, varPatch, DataType.Int32, DataType.Variable);
        }
        else if (Expression is AccessorNode accessor)
        {
            // Multiple chained accessors. Generate chained code first
            accessor.GenerateChainedCode(context, true);

            // This accessor is the final one
            AccessorExpression.GenerateCode(context);
            context.ConvertDataType(DataType.Int32);

            // Duplicate array reference
            context.EmitDuplicate(DataType.Int32, 4);

            // Save array reference (in case the expression uses arrays itself, probably)
            context.Emit(ExtendedOpcode.SaveArrayReference);

            // Push value from array
            context.Emit(ExtendedOpcode.PushArrayFinal);

            // Push the expression
            expression.GenerateCode(context);

            // Perform operation
            AssignNode.PerformCompoundOperation(context, operationOpcode);

            // Restore array reference
            context.Emit(ExtendedOpcode.RestoreArrayReference);

            // Swap stack around again, and store to array
            context.EmitDupSwap(DataType.Int32, 4, 5);
            context.Emit(ExtendedOpcode.PopArrayFinal);
        }
        else
        {
            throw new Exception("Invalid expression on accessor");
        }
    }

    /// <summary>
    /// Helper function to duplicate a pre/post-increment/decrement value, and swap around the stack.
    /// </summary>
    private static void PrePostDuplicateAndSwap(BytecodeContext context, InstanceConversionType conversionType)
    {
        // Duplicate value
        context.EmitDuplicate(DataType.Variable, 0);
        context.PushDataType(DataType.Variable);

        // Swap around stack to prepare for pop
        if (context.CompileContext.GameContext.UsingGMLv2)
        {
            if (conversionType == InstanceConversionType.StacktopId)
            {
                // Extra 16 bytes for RValue being referenced
                context.EmitDupSwap(DataType.Int32, 4, 10);
            }
            else
            {
                // No stacktop RValue (just an instance ID)
                context.EmitDupSwap(DataType.Int32, 4, 6);
            }
        }
        else
        {
            // Pre-GMLv2 swap operation
            context.EmitPopSwap(6);
        }
    }

    /// <summary>
    /// Helper function to duplicate a pre/post-increment/decrement value, and swap around the stack.
    /// For multi-dimensional arrays.
    /// </summary>
    private static void MultiArrayPrePostDuplicateAndSwap(BytecodeContext context)
    {
        // Duplicate value
        context.EmitDuplicate(DataType.Variable, 0);
        context.PushDataType(DataType.Variable);

        // Swap around stack to prepare for pop
        context.EmitDupSwap(DataType.Int32, 4, 9);
    }

    /// <inheritdoc/>
    public void GeneratePrePostAssignCode(BytecodeContext context, bool isIncrement, bool isPre, bool isStatement)
    {
        // Generate differently depending on expression
        if (Expression is IVariableASTNode variable)
        {
            // Generate common code to prepare for push and pop
            (InstanceType instanceType, InstanceConversionType instanceConversionType) = GenerateVariableCode(context, variable);

            // Duplicate instance ID and array index, so it can be stored back to later
            if (instanceConversionType == InstanceConversionType.StacktopId)
            {
                // 32-bit integer ID, 32-bit integer array index, AND actual instance (as variable type).
                // 8 bytes for two int32s, plus 16 bytes for RValue.
                context.EmitDuplicate(DataType.Int32, 5);
            }
            else
            {
                // Just two 32-bit integers to duplicate
                if (context.CompileContext.GameContext.UsingGMLv2 ||
                    context.CompileContext.GameContext.Bytecode14OrLower)
                {
                    // New versions (and *really* old versions) output this
                    context.EmitDuplicate(DataType.Int32, 1);
                }
                else
                {
                    // Old versions (but not *really* old versions) output this
                    context.EmitDuplicate(DataType.Int64, 0);
                }
            }

            // Simple variable push
            VariablePatch varPatch = new(variable.VariableName, instanceType, VariableType.Array, variable.BuiltinVariable is not null);
            context.Emit(Opcode.Push, varPatch, DataType.Variable);

            // Postfix expression: duplicate original value, and swap stack around for pop
            if (!isStatement && !isPre)
            {
                PrePostDuplicateAndSwap(context, instanceConversionType);
            }

            // Push the expression
            context.Emit(Opcode.Push, (short)1, DataType.Int16);

            // Perform operation
            context.Emit(isIncrement ? Opcode.Add : Opcode.Subtract, DataType.Int32, DataType.Variable);

            // Prefix expression: duplicate new value, and swap stack around for pop
            if (!isStatement && isPre)
            {
                PrePostDuplicateAndSwap(context, instanceConversionType);
            }

            // Odd quirk in GMLv2, where the instance type is maintained for the pop only
            if (context.CompileContext.GameContext.UsingGMLv2)
            {
                varPatch.KeepInstanceType = true;
            }

            // Simple variable store, but denote pop order using data types
            context.Emit(Opcode.Pop, varPatch, DataType.Int32, DataType.Variable);
        }
        else if (Expression is AccessorNode accessor)
        {
            // Multiple chained accessors. Generate chained code first
            accessor.GenerateChainedCode(context, true);

            // This accessor is the final one
            AccessorExpression.GenerateCode(context);
            context.ConvertDataType(DataType.Int32);

            // Duplicate array reference
            context.EmitDuplicate(DataType.Int32, 4);

            // Push value from array
            context.Emit(ExtendedOpcode.PushArrayFinal);

            // Postfix expression: duplicate old value, and swap stack around for pop
            if (!isStatement && !isPre)
            {
                MultiArrayPrePostDuplicateAndSwap(context);
            }

            // Push the expression
            context.Emit(Opcode.Push, (short)1, DataType.Int16);

            // Perform operation
            context.Emit(isIncrement ? Opcode.Add : Opcode.Subtract, DataType.Int32, DataType.Variable);

            // Prefix expression: duplicate new value, and swap stack around for pop
            if (!isStatement && isPre)
            {
                MultiArrayPrePostDuplicateAndSwap(context);
            }

            // Swap stack around again, and store to array
            context.EmitDupSwap(DataType.Int32, 4, 5);
            context.Emit(ExtendedOpcode.PopArrayFinal);
        }
        else
        {
            throw new Exception("Invalid expression on accessor");
        }
    }

    /// <inheritdoc/>
    public IEnumerable<IASTNode> EnumerateChildren()
    {
        yield return Expression;
        yield return AccessorExpression;
        if (AccessorExpression2 is not null)
        {
            yield return AccessorExpression2;
        }
    }
}
