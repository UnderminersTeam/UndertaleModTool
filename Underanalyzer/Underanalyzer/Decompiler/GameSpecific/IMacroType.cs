/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using Underanalyzer.Decompiler.AST;

namespace Underanalyzer.Decompiler.GameSpecific;

/// <summary>
/// Base interface for all macro types.
/// </summary>
public interface IMacroType
{
}

/// <summary>
/// Base interface for all macro types that resolve a 32-bit integer.
/// </summary>
public interface IMacroTypeInt32 : IMacroType
{
    /// <summary>
    /// Resolves the macro type with the given 32-bit int value, or <see langword="null"/> if there is no resolution.
    /// </summary>
    public IExpressionNode? Resolve(ASTCleaner cleaner, IMacroResolvableNode node, int data);
}

/// <summary>
/// Base interface for all macro types that resolve a 64-bit integer.
/// </summary>
public interface IMacroTypeInt64 : IMacroType
{
    /// <summary>
    /// Resolves the macro type with the given 64-bit int value, or <see langword="null"/> if there is no resolution.
    /// </summary>
    public IExpressionNode? Resolve(ASTCleaner cleaner, IMacroResolvableNode node, long data);
}

/// <summary>
/// Base interface for all macro types that resolve function arguments.
/// </summary>
public interface IMacroTypeFunctionArgs : IMacroType
{
    /// <summary>
    /// Resolves the macro type with the given AST function, or <see langword="null"/> if there is no resolution.
    /// </summary>
    public IFunctionCallNode? Resolve(ASTCleaner cleaner, IFunctionCallNode call);
}

/// <summary>
/// Base interface for all macro types that resolve an array initialization.
/// </summary>
public interface IMacroTypeArrayInit : IMacroType
{
    /// <summary>
    /// Resolves the macro type with the given AST array initialization, or <see langword="null"/> if there is no resolution.
    /// </summary>
    public ArrayInitNode? Resolve(ASTCleaner cleaner, ArrayInitNode arrayInit);
}


/// <summary>
/// Base interface for all macro types that resolve a condition.
/// </summary>
public interface IMacroTypeConditional : IMacroType
{
    /// <summary>
    /// True if this macro type is required to be resolved in the context of a larger scope, such as function arguments.
    /// </summary>
    public bool Required { get; }

    /// <summary>
    /// Resolves the macro type with the given AST array initialization, or <see langword="null"/> if there is no resolution.
    /// </summary>
    public IExpressionNode? Resolve(ASTCleaner cleaner, IConditionalValueNode node);
}
