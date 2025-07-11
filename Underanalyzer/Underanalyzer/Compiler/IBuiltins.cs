/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

namespace Underanalyzer.Compiler;

/// <summary>
/// Represents an instance of builtin function/variable/etc. information to use for compilation.
/// </summary>
public interface IBuiltins
{
    /// <summary>
    /// Looks up a builtin function.
    /// </summary>
    /// <param name="name">Name of the function to look up.</param>
    /// <returns>The builtin function corresponding to <paramref name="name"/>, or null if none exists.</returns>
    public IBuiltinFunction? LookupBuiltinFunction(string name);

    /// <summary>
    /// Looks up a builtin variable.
    /// </summary>
    /// <param name="name">Name of the variable to look up.</param>
    /// <returns>The builtin variable corresponding to <paramref name="name"/>, or null if none exists.</returns>
    public IBuiltinVariable? LookupBuiltinVariable(string name);

    /// <summary>
    /// Looks up a builtin constant double value.
    /// </summary>
    /// <param name="name">Name of the constant to look up.</param>
    /// <param name="value">Outputs the value if lookup was successful; otherwise undefined.</param>
    /// <returns><see langword="true"/> if a constant double was found; <see langword="false"/> otherwise.</returns>
    public bool LookupConstantDouble(string name, out double value);
}

/// <summary>
/// Represents a single builtin function to be used during compilation.
/// </summary>
public interface IBuiltinFunction
{
    /// <summary>
    /// Name of the builtin function.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Minimum number of arguments allowed for the builtin function.
    /// </summary>
    public int MinArguments { get; }

    /// <summary>
    /// Maximum number of arguments allowed for the builtin function.
    /// </summary>
    public int MaxArguments { get; }
}

/// <summary>
/// Represents a single builtin variable to be used during compilation.
/// </summary>
public interface IBuiltinVariable
{
    /// <summary>
    /// Name of the builtin variable.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Whether or not the builtin variable is a global variable.
    /// </summary>
    public bool IsGlobal { get; }

    /// <summary>
    /// Whether or not the builtin variable will automatically 
    /// add an array index when compiled, if one is not already present.
    /// </summary>
    public bool IsAutomaticArray { get; }

    /// <summary>
    /// Whether or not the builtin variable can be set.
    /// </summary>
    public bool CanSet { get; }
}
