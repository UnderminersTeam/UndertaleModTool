/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;
using Underanalyzer.Decompiler.AST;

namespace Underanalyzer.Decompiler.GameSpecific;

/// <summary>
/// Macro type that matches an array of macro types to a function call.
/// </summary>
public class FunctionArgsMacroType(IEnumerable<IMacroType?> types) : IMacroType, IMacroTypeFunctionArgs
{
    private List<IMacroType?> Types { get; } = new(types);

    /// <summary>
    /// Resolves this macro type for a given function call in the AST.
    /// </summary>
    public IFunctionCallNode? Resolve(ASTCleaner cleaner, IFunctionCallNode call)
    {
        int callArgumentsCount = call.Arguments.Count;
        int callArgumentsStart = 0;
        if (call.FunctionName == VMConstants.ScriptExecuteFunction)
        {
            // Special case: arguments are shifted to the right by 1
            callArgumentsCount -= 1;
            callArgumentsStart = 1;
        }

        if (Types.Count != callArgumentsCount)
        {
            return null;
        }

        bool didAnything = false;

        List<IExpressionNode> resolved = new(callArgumentsCount);
        for (int i = callArgumentsStart; i < (callArgumentsStart + callArgumentsCount); i++)
        {
            IMacroType? currentType = Types[i - callArgumentsStart];
            if (currentType is null || call.Arguments[i] is not IMacroResolvableNode node)
            {
                // Current type is not defined, or current argument is not resolvable, so just use existing argument
                resolved.Add(call.Arguments[i]);
                continue;
            }

            if (node.ResolveMacroType(cleaner, currentType) is not IExpressionNode nodeResolved)
            {
                // Failed to resolve current argument's macro type.
                // If the type is a conditional which is required in this scope, then fail this resolution;
                // otherwise, use existing argument.
                if (Types[i - callArgumentsStart] is IMacroTypeConditional conditional && conditional.Required)
                {
                    return null;
                }
                resolved.Add(call.Arguments[i]);
                continue;
            }

            // Add to resolved arguments list
            resolved.Add(nodeResolved);
            didAnything = true;
        }

        if (!didAnything)
        {
            return null;
        }

        // Assign resolved values to arguments
        for (int i = 0; i < callArgumentsCount; i++)
        {
            call.Arguments[i + callArgumentsStart] = resolved[i];
        }

        return call;
    }
}
