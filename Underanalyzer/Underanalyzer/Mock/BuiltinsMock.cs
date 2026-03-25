/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using Underanalyzer.Compiler;
using System.Collections.Generic;

namespace Underanalyzer.Mock;

/// <summary>
/// A default implementation of <see cref="IBuiltins"/>.
/// </summary>
public class BuiltinsMock : IBuiltins
{
    /// <summary>
    /// Mock list of constant double values.
    /// </summary>
    public Dictionary<string, double> ConstantDoubles = new()
    {
        { "self", -1 },
        { "other", -2 },
        { "all", -3 },
        { "noone", -4 },
        { "global", -5 },
        { "test_constant", 128 }
    };

    /// <summary>
    /// Mock map of builtin functions.
    /// </summary>
    public Dictionary<string, BuiltinFunctionMock> BuiltinFunctions = new()
    {
        { "test_builtin_function", new("test_builtin_function", 0, int.MaxValue) },
        { "string", new("string", 1, 1) },
        { "real", new("real", 1, 1) },
        { "ord", new("ord", 1, 1) },
        { "script_execute", new("script_execute", 1, int.MaxValue) },
        { "array_set", new("array_set", 3, 3) },
        { "array_create", new("array_create", 1, 2) },
        { VMConstants.SelfFunction, new(VMConstants.SelfFunction, 0, 0) },
        { VMConstants.OtherFunction, new(VMConstants.OtherFunction, 0, 0) },
        { VMConstants.GlobalFunction, new(VMConstants.GlobalFunction, 0, 0) },
        { VMConstants.GetInstanceFunction, new(VMConstants.GetInstanceFunction, 1, 1) },
        { VMConstants.MethodFunction, new(VMConstants.MethodFunction, 2, 2) },
        { VMConstants.NullObjectFunction, new(VMConstants.NullObjectFunction, 0, 0) },
        { VMConstants.NewObjectFunction, new(VMConstants.NewObjectFunction, 0, int.MaxValue) },
        { VMConstants.NewArrayFunction, new(VMConstants.NewArrayFunction, 0, int.MaxValue) },
        { VMConstants.SetStaticFunction, new(VMConstants.SetStaticFunction, 0, 0) },
        { VMConstants.CopyStaticFunction, new(VMConstants.CopyStaticFunction, 1, 1) },
        { VMConstants.StaticGetFunction, new(VMConstants.StaticGetFunction, 1, 1) },
        { VMConstants.ThrowFunction, new(VMConstants.ThrowFunction, 1, 1) },
        { VMConstants.TryHookFunction, new(VMConstants.TryHookFunction, 2, 2) },
        { VMConstants.TryUnhookFunction, new(VMConstants.TryUnhookFunction, 0, 0) },
        { VMConstants.FinishCatchFunction, new(VMConstants.FinishCatchFunction, 0, 0) },
        { VMConstants.FinishFinallyFunction, new(VMConstants.FinishFinallyFunction, 0, 0) },
    };

    /// <summary>
    /// Mock map of builtin variables.
    /// </summary>
    public Dictionary<string, BuiltinVariableMock> BuiltinVariables = new()
    {
        { "undefined", new("undefined", false, true) },
        { "sprite_index", new("sprite_index") },
        { "id", new("id", false) },
        { "view_xview", new("view_xview", true, true, true) },
        { "view_camera", new("view_camera", true, true, true) }
    };

    /// <inheritdoc/>
    public IBuiltinFunction? LookupBuiltinFunction(string name)
    {
        return BuiltinFunctions.GetValueOrDefault(name);
    }

    /// <inheritdoc/>
    public IBuiltinVariable? LookupBuiltinVariable(string name)
    {
        return BuiltinVariables.GetValueOrDefault(name);
    }

    /// <inheritdoc/>
    public bool LookupConstantDouble(string name, out double value)
    {
        return ConstantDoubles.TryGetValue(name, out value);
    }
}

public class BuiltinFunctionMock(string name, int minArguments, int maxArguments) : IBuiltinFunction
{
    /// <inheritdoc/>
    public string Name { get; } = name;

    /// <inheritdoc/>
    public int MinArguments { get; } = minArguments;

    /// <inheritdoc/>
    public int MaxArguments { get; } = maxArguments;
}

public class BuiltinVariableMock(string name, bool canSet = true, bool isGlobal = false, bool isAutomaticArray = false) : IBuiltinVariable
{
    /// <inheritdoc/>
    public string Name { get; } = name;

    /// <inheritdoc/>
    public bool CanSet { get; } = canSet;

    /// <inheritdoc/>
    public bool IsGlobal { get; } = isGlobal;

    /// <inheritdoc/>
    public bool IsAutomaticArray { get; } = isAutomaticArray;
}