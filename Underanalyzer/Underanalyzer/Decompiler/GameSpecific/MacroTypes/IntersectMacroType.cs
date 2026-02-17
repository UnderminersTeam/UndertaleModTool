/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;
using Underanalyzer.Decompiler.AST;

namespace Underanalyzer.Decompiler.GameSpecific;

/// <summary>
/// A macro type that is the intersection of multiple macro types, usually used to add macro types that are conditions.
/// </summary>
public class IntersectMacroType : IMacroTypeInt32, IMacroTypeInt64, IMacroTypeFunctionArgs, IMacroTypeArrayInit, IMacroTypeConditional
{
    private List<IMacroType> Types { get; }

    public bool Required { get; }

    public IntersectMacroType(IEnumerable<IMacroType> types)
    {
        Types = new(types);

        // Set this macro type as required if any sub-macro types are required
        Required = false;
        foreach (IMacroType type in types)
        {
            if (type is IMacroTypeConditional conditional && conditional.Required)
            {
                Required = true;
                break;
            }
        }
    }

    public IExpressionNode? Resolve(ASTCleaner cleaner, IMacroResolvableNode node, int data)
    {
        IExpressionNode? result = null;
        foreach (IMacroType type in Types)
        {
            if (type is not IMacroTypeInt32 type32)
            {
                continue;
            }
            result = type32.Resolve(cleaner, node, data);
            if (result is null)
            {
                return null;
            }
        }
        return result;
    }

    public IExpressionNode? Resolve(ASTCleaner cleaner, IMacroResolvableNode node, long data)
    {
        IExpressionNode? result = null;
        foreach (IMacroType type in Types)
        {
            if (type is not IMacroTypeInt64 type64)
            {
                continue;
            }
            result = type64.Resolve(cleaner, node, data);
            if (result is null)
            {
                return null;
            }
        }
        return result;
    }

    public IFunctionCallNode? Resolve(ASTCleaner cleaner, IFunctionCallNode functionCall)
    {
        IFunctionCallNode? result = null;
        foreach (IMacroType type in Types)
        {
            if (type is not IMacroTypeFunctionArgs typeFuncArgs)
            {
                continue;
            }
            result = typeFuncArgs.Resolve(cleaner, functionCall);
            if (result is null)
            {
                return null;
            }
        }
        return result;
    }

    public ArrayInitNode? Resolve(ASTCleaner cleaner, ArrayInitNode arrayInit)
    {
        ArrayInitNode? result = null;
        foreach (IMacroType type in Types)
        {
            if (type is not IMacroTypeArrayInit typeArrayInit)
            {
                continue;
            }
            result = typeArrayInit.Resolve(cleaner, arrayInit);
            if (result is null)
            {
                return null;
            }
        }
        return result;
    }

    public IExpressionNode? Resolve(ASTCleaner cleaner, IConditionalValueNode node)
    {
        IExpressionNode? result = null;
        foreach (IMacroType type in Types)
        {
            if (type is not IMacroTypeConditional typeConditional)
            {
                continue;
            }
            result = typeConditional.Resolve(cleaner, node);
            if (result is null)
            {
                return null;
            }
        }
        return result;
    }
}
