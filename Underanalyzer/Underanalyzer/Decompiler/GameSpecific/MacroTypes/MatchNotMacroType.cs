/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using Underanalyzer.Decompiler.AST;

namespace Underanalyzer.Decompiler.GameSpecific;

/// <summary>
/// Conditional for matching an AST node by *not* having a type name and/or value.
/// </summary>
public class MatchNotMacroType(IMacroType? innerType, string? typeName, string? value = null) : ConditionalMacroType(innerType)
{
    /// <summary>
    /// Type name to *not* match, or <see langword="null"/> if none.
    /// </summary>
    public string? ConditionalTypeName { get; } = typeName;

    /// <summary>
    /// Value content to *not* match, or <see langword="null"/> if none.
    /// </summary>
    public string? ConditionalValue { get; } = value;

    public override bool EvaluateCondition(ASTCleaner cleaner, IConditionalValueNode node)
    {
        if (ConditionalValue is not null && node.ConditionalValue != ConditionalValue)
        {
            return true;
        }
        if (ConditionalTypeName is null)
        {
            return false;
        }
        return node.ConditionalTypeName != ConditionalTypeName;
    }
}
