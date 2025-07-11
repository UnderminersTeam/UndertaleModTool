/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using Underanalyzer.Decompiler.AST;

namespace Underanalyzer.Decompiler.GameSpecific;

/// <summary>
/// Base interface for all macro type resolvers.
/// Different types of macro type resolvers work in different contexts.
/// </summary>
public interface IMacroTypeResolver
{
    /// <summary>
    /// Resolves a macro type for a variable name on this resolver, or <see langword="null"/> if none is found.
    /// </summary>
    public IMacroType? ResolveVariableType(ASTCleaner cleaner, string? variableName);

    /// <summary>
    /// Resolves a macro type for a function's arguments on this resolver, or <see langword="null"/> if none is found.
    /// </summary>
    public IMacroType? ResolveFunctionArgumentTypes(ASTCleaner cleaner, string? functionName);

    /// <summary>
    /// Resolves a macro type for a function's return value on this resolver, or <see langword="null"/> if none is found.
    /// </summary>
    public IMacroType? ResolveReturnValueType(ASTCleaner cleaner, string? functionName);
}
