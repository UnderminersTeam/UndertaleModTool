/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;
using static Underanalyzer.IGMInstruction;

namespace Underanalyzer.Compiler.Bytecode;

/// <summary>
/// Struct containing references to lists of instruction patches, to be easily copied around.
/// </summary>
internal readonly struct InstructionPatches
{
    /// <summary>
    /// List of variable patches generated during code generation.
    /// </summary>
    public List<VariablePatch>? VariablePatches { get; init; }

    /// <summary>
    /// List of function patches generated during code generation.
    /// </summary>
    public List<FunctionPatch>? FunctionPatches { get; init; }

    /// <summary>
    /// List of local function patches generated during code generation.
    /// </summary>
    public List<LocalFunctionPatch>? LocalFunctionPatches { get; init; }

    /// <summary>
    /// List of struct variable patches generated during code generation.
    /// </summary>
    public List<StructVariablePatch>? StructVariablePatches { get; init; }

    /// <summary>
    /// List of string patches generated during code generation.
    /// </summary>
    public List<StringPatch>? StringPatches { get; init; }

    /// <summary>
    /// Creates an instruction patch struct, initialized with list capacity for patches.
    /// </summary>
    public static InstructionPatches Create()
    {
        return new InstructionPatches()
        {
            VariablePatches = new(32),
            FunctionPatches = new(32),
            LocalFunctionPatches = new(4),
            StructVariablePatches = new(4),
            StringPatches = new(16)
        };
    }
}

/// <summary>
/// Instruction patch base interface.
/// </summary>
internal interface IInstructionPatch
{
    /// <summary>
    /// Associated instruction to patch, or <see langword="null"/> if none is yet assigned.
    /// </summary>
    public IGMInstruction? Instruction { get; set; }
}

/// <summary>
/// A variable patch used during bytecode generation, to assign variables to instructions.
/// </summary>
internal record struct VariablePatch(string Name, InstanceType InstanceType, VariableType VariableType = VariableType.Normal, bool IsBuiltin = false) : IInstructionPatch
{
    /// <inheritdoc/>
    public IGMInstruction? Instruction { get; set; }

    /// <summary>
    /// Instance type to use for instruction. Sometimes differs from <see cref="InstanceType"/>, due to compiler quirks.
    /// </summary>
    public InstanceType InstructionInstanceType { get; set; } = InstanceType;

    /// <summary>
    /// For cases where the variable type being not <see cref="VariableType.Normal"/> would cause instance type to be ignored, this can be set to disable that behavior.
    /// </summary>
    public bool KeepInstanceType { get; set; } = false;
}

/// <summary>
/// A variable patch used during bytecode generation, to assign struct name variables to instructions.
/// </summary>
internal record struct StructVariablePatch(FunctionEntry FunctionEntry, InstanceType InstanceType, VariableType VariableType = VariableType.Normal) : IInstructionPatch
{
    /// <inheritdoc/>
    public IGMInstruction? Instruction { get; set; }

    /// <summary>
    /// Instance type to use for instruction. Sometimes differs from <see cref="InstanceType"/>, due to compiler quirks.
    /// </summary>
    public InstanceType InstructionInstanceType { get; set; } = InstanceType;
}

/// <summary>
/// A function patch used during bytecode generation, to assign functions to instructions.
/// </summary>
internal record struct FunctionPatch(FunctionScope Scope, string Name, IBuiltinFunction? BuiltinFunction = null) : IInstructionPatch
{
    /// <inheritdoc/>
    public IGMInstruction? Instruction { get; set; }

    /// <summary>
    /// Creates a function patch from a builtin function, and the compile context.
    /// </summary>
    /// <param name="builtinName">Builtin function name to use.</param>
    public static FunctionPatch FromBuiltin(ISubCompileContext context, string builtinName)
    {
        return new FunctionPatch(context.CurrentScope, builtinName, context.CompileContext.GameContext.Builtins.LookupBuiltinFunction(builtinName));
    }
}

/// <summary>
/// A local function patch used during bytecode generation, to assign local functions to instructions.
/// </summary>
internal record struct LocalFunctionPatch(FunctionEntry? FunctionEntry, FunctionScope? FunctionScope = null, string? FunctionName = null) : IInstructionPatch
{
    /// <inheritdoc/>
    public IGMInstruction? Instruction { get; set; }
}

/// <summary>
/// A string patch used during bytecode generation, to link to strings.
/// </summary>
internal record struct StringPatch(string Content) : IInstructionPatch
{
    /// <inheritdoc/>
    public IGMInstruction? Instruction { get; set; }
}

