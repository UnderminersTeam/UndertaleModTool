/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using Underanalyzer.Decompiler.AST;

namespace Underanalyzer.Decompiler.GameSpecific;

/// <summary>
/// Macro type for booleans, in older GML versions.
/// </summary>
public class BooleanMacroType : IMacroTypeInt32
{
    public IExpressionNode? Resolve(ASTCleaner cleaner, IMacroResolvableNode node, int data)
    {
        // Ensure we don't resolve this on newer GameMaker versions where this is unnecessary
        if (cleaner.Context.GameContext.UsingTypedBooleans)
        {
            return null;
        }

        // Simply check if 0 or 1 exactly...
        return data switch
        {
            0 => new MacroValueNode("false"),
            1 => new MacroValueNode("true"),
            _ => null
        };
    }
}
