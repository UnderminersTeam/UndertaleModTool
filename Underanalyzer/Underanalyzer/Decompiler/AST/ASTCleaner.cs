/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;
using Underanalyzer.Decompiler.GameSpecific;

namespace Underanalyzer.Decompiler.AST;

/// <summary>
/// Manages cleaning/postprocessing of the AST.
/// </summary>
public class ASTCleaner(DecompileContext context)
{
    /// <summary>
    /// The decompilation context this is cleaning for.
    /// </summary>
    public DecompileContext Context { get; } = context;

    /// <summary>
    /// List of arguments passed into a struct fragment.
    /// </summary>
    internal List<IExpressionNode>? StructArguments { get => TopFragmentContext!.StructArguments; set => TopFragmentContext!.StructArguments = value; }

    /// <summary>
    /// Set of all local variables present in the current fragment.
    /// </summary>
    internal HashSet<string> LocalVariableNames { get => TopFragmentContext!.LocalVariableNames; }

    /// <summary>
    /// The stack used to manage fragment contexts.
    /// </summary>
    private Stack<ASTFragmentContext> FragmentContextStack { get; } = new();

    /// <summary>
    /// The current/top fragment context.
    /// </summary>
    internal ASTFragmentContext? TopFragmentContext { get; private set; }

    /// <summary>
    /// Helper to access the global macro resolver used for resolving macro types.
    /// </summary>
    internal IMacroTypeResolver GlobalMacroResolver => Context.GameContext.GameSpecificRegistry.MacroResolver;

    /// <summary>
    /// Helper to access an ID instance and object type union, for resolving macro types.
    /// </summary>
    internal IMacroType? MacroInstanceIdOrObjectAsset
    {
        get
        {
            if (_macroInstanceIdOrObjectAsset is null)
            {
                if (!Context.GameContext.GameSpecificRegistry.TypeExists("Id.Instance") ||
                    !Context.GameContext.GameSpecificRegistry.TypeExists("Asset.Object"))
                {
                    return null;
                }
                _macroInstanceIdOrObjectAsset = Context.GameContext.GameSpecificRegistry.FindTypeUnion(["Id.Instance", "Asset.Object"]);
            }
            return _macroInstanceIdOrObjectAsset;
        }
    }
    private IMacroType? _macroInstanceIdOrObjectAsset = null;

    /// <summary>
    /// Pushes a context onto the fragment context stack.
    /// Each fragment has its own expression stack, struct argument list, etc.
    /// </summary>
    internal void PushFragmentContext(ASTFragmentContext context)
    {
        FragmentContextStack.Push(context);
        TopFragmentContext = context;
    }

    /// <summary>
    /// Pops a fragment off of the fragment context stack.
    /// </summary>
    internal ASTFragmentContext PopFragmentContext()
    {
        ASTFragmentContext popped = FragmentContextStack.Pop();
        if (FragmentContextStack.Count > 0)
        {
            TopFragmentContext = FragmentContextStack.Peek();
        }
        else
        {
            TopFragmentContext = null;
        }
        return popped;
    }

    /// <summary>
    /// Helper function to declare a new enum.
    /// </summary>
    internal void DeclareEnum(GMEnum gmEnum)
    {
        Context.EnumDeclarations.Add(gmEnum);
        Context.NameToEnumDeclaration[gmEnum.Name] = gmEnum;
    }
}
