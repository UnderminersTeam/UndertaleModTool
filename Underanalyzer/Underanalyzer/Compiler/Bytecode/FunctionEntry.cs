/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System;

namespace Underanalyzer.Compiler.Bytecode;

/// <summary>
/// Represents a function entry produced during code generation.
/// </summary>
public sealed record FunctionEntry
{
    /// <summary>
    /// Parent function entry, or <see langword="null"/> if none exists.
    /// </summary>
    public FunctionEntry? Parent { get; }

    /// <summary>
    /// Function scope corresponding to this function entry.
    /// </summary>
    public FunctionScope Scope { get; }

    /// <summary>
    /// Byte offset of the function entry in the bytecode.
    /// </summary>
    public int BytecodeOffset { get; }

    /// <summary>
    /// Number of arguments passed into the function entry.
    /// </summary>
    public int ArgumentCount { get; }

    /// <summary>
    /// Whether this function declaration is generated as a constructor function.
    /// </summary>
    /// <remarks>
    /// This is always <see langword="true"/> for struct instantiations.
    /// </remarks>
    public bool IsConstructor { get; }

    /// <summary>
    /// Name of the function, if one exists; <see langword="null"/> otherwise.
    /// </summary>
    public string? FunctionName { get; }

    /// <summary>
    /// Whether the function entry was declared in the root function scope of the code entry.
    /// </summary>
    public bool DeclaredInRootScope { get; }

    /// <summary>
    /// Static variable name being assigned to around the function entry, if one exists; <see langword="null"/> otherwise.
    /// </summary>
    public string? StaticVariableName { get; }

    /// <summary>
    /// Kind of function entry. Useful for generating a final code entry name.
    /// </summary>
    public FunctionEntryKind Kind { get; }

    /// <summary>
    /// Function as found in the game data, when resolved using <see cref="ResolveFunction(IGMFunction)"/>.
    /// </summary>
    public IGMFunction? Function { get; private set; }

    /// <summary>
    /// Name that child functions can reference this function entry as, when resolved using <see cref="ResolveFunction(IGMFunction)"/>.
    /// </summary>
    public string? ChildFunctionName { get; private set; }

    /// <summary>
    /// Name of the struct function, if applicable, and when resolved using <see cref="ResolveStructName(string)"/>.
    /// </summary>
    public string? StructName { get; private set; }

    internal FunctionEntry(FunctionEntry? parent, FunctionScope scope, int bytecodeOffset, int argumentCount, bool isConstructor,
                           string? functionName, bool declaredInRootScope, string? staticVariableName, FunctionEntryKind kind)
    {
        Parent = parent;
        Scope = scope;
        BytecodeOffset = bytecodeOffset;
        ArgumentCount = argumentCount;
        IsConstructor = isConstructor;
        FunctionName = functionName;
        DeclaredInRootScope = declaredInRootScope;
        StaticVariableName = staticVariableName;
        Kind = kind;
    }

    /// <summary>
    /// Resolves the actual function for this function entry. <see cref="Function"/> must be <see langword="null"/> (as initialized).
    /// </summary>
    public void ResolveFunction(IGMFunction function, string childFunctionName)
    {
        // Usage checks
        if (Function is not null)
        {
            throw new InvalidOperationException("Tried to resolve function when it was already resolved");
        }

        Function = function;
        ChildFunctionName = childFunctionName;
    }

    /// <summary>
    /// Resolves the struct name for this function entry. <see cref="Kind"/> must be <see cref="FunctionEntryKind.StructInstantiation"/>, 
    /// and <see cref="StructName"/> must be <see langword="null"/> (as initialized).
    /// </summary>
    public void ResolveStructName(string name)
    {
        // Usage checks
        if (Kind != FunctionEntryKind.StructInstantiation)
        {
            throw new InvalidOperationException("Tried to resolve struct name for non-struct function entry");
        }
        if (StructName is not null)
        {
            throw new InvalidOperationException("Tried to resolve struct name when it was already resolved");
        }

        StructName = name;
    }
}

/// <summary>
/// Kinds of function entries that can exist.
/// </summary>
public enum FunctionEntryKind
{
    /// <summary>
    /// Function entry is for a function declaration, either named or anonymous.
    /// </summary>
    FunctionDeclaration,

    /// <summary>
    /// Function entry is for a struct instantiation.
    /// </summary>
    StructInstantiation
}
