/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;
using Underanalyzer.Decompiler.ControlFlow;

namespace Underanalyzer.Decompiler.AST;

/// <summary>
/// Represents a single fragment context within the AST.
/// </summary>
public sealed class ASTFragmentContext
{
    /// <summary>
    /// The fragment we belong to.
    /// </summary>
    private Fragment Fragment { get; }

    /// <summary>
    /// Whether this context belongs to the root fragment of the corresponding root code entry.
    /// </summary>
    public bool IsRootFragment { get => Fragment.StartAddress == 0; }

    /// <summary>
    /// The name of the code entry this fragment belongs to.
    /// </summary>
    public string? CodeEntryName { get => Fragment.CodeEntry.Name?.Content; }

    /// <summary>
    /// The name of the function this fragment belongs to, or <see langword="null"/> if none.
    /// </summary>
    public string? FunctionName { get; internal set; } = null;

    /// <summary>
    /// Parent fragment of this fragment, if not the root.
    /// </summary>
    internal ASTFragmentContext? Parent { get; }

    /// <summary>
    /// Children of this fragment, e.g. sub-functions.
    /// </summary>
    internal List<ASTFragmentContext> Children { get; } = [];
    
    /// <summary>
    /// Current working VM expression stack.
    /// </summary>
    internal Stack<IExpressionNode> ExpressionStack { get; } = new();

    /// <summary>
    /// If true, the current AST is within a static variable initialization block (when printing).
    /// </summary>
    internal bool InStaticInitialization { get; set; } = false;

    /// <summary>
    /// If not null, represents the list of arguments getting passed into this fragment (which is a struct).
    /// </summary>
    public List<IExpressionNode>? StructArguments { get; internal set; } = null;

    /// <summary>
    /// Function call to the parent constructor function, if this is a constructor function that inherits
    /// another constructor function, or <see langword="null"/> otherwise.
    /// </summary>
    internal IExpressionNode? BaseParentCall { get; set; } = null;

    /// <summary>
    /// Contains all local variables referenced from within this fragment.
    /// </summary>
    public HashSet<string> LocalVariableNames { get; } = [];

    /// <summary>
    /// Contains all local variables referenced from within this fragment, in order of occurrence.
    /// </summary>
    public List<string> LocalVariableNamesList { get; } = [];

    /// <summary>
    /// The current local variable scope.
    /// </summary>
    internal LocalScope? CurrentLocalScope { get; private set; }

    /// <summary>
    /// The current block being cleaned up during post-cleanup.
    /// </summary>
    internal BlockNode? CurrentPostCleanupBlock { get; set; }

    /// <summary>
    /// Map of code entry names to function names, for all children fragments/sub-functions of this context.
    /// </summary>
    public Dictionary<string, string> SubFunctionNames { get; } = [];

    /// <summary>
    /// The loop surrounding the currently-building position in the AST.
    /// </summary>
    internal Loop? SurroundingLoop { get; set; } = null;

    /// <summary>
    /// Contains local variable names that should be entirely removed from the fragment. 
    /// (For removing compiler-generated code.)
    /// </summary>
    internal HashSet<string> LocalVariablesToPurge { get; } = [];

    /// <summary>
    /// Stack of the number of statements contained in all enveloping try finally blocks.
    /// </summary>
    internal Stack<int> FinallyStatementCount { get; set; } = new();

    /// <summary>
    /// The maximum argument variable referenced within this context, if applicable. -1 means none are referenced.
    /// </summary>
    internal int MaxReferencedArgument { get; set; } = -1;

    /// <summary>
    /// Contains all named argument variables referenced from within this fragment.
    /// </summary>
    internal HashSet<string> NamedArguments { get; set; } = [];

    /// <summary>
    /// Lookup of argument index to argument name, for GMLv2 named arguments.
    /// </summary>
    private Dictionary<int, string> NamedArgumentByIndex { get; set; } = [];

    internal ASTFragmentContext(ASTFragmentContext? parent, Fragment fragment)
    {
        Parent = parent;
        Fragment = fragment;

        // Update max referenced argument, if we have an argument count greater than 0
        if (fragment.CodeEntry.ArgumentCount > 0)
        {
            MaxReferencedArgument = fragment.CodeEntry.ArgumentCount - 1;
        }
    }

    /// <summary>
    /// Removes a local variable's declaration from this fragment.
    /// </summary>
    internal void RemoveLocal(string name)
    {
        if (LocalVariableNames.Remove(name))
        {
            LocalVariableNamesList.Remove(name);
        }
    }

    /// <summary>
    /// Generates and returns the named argument name that the given index should have.
    /// By default, resorts to formatting string from settings.
    /// Returns <see langword="null"/> if prior to GMLv2 (and no named argument should be used).
    /// </summary>
    internal string? GetNamedArgumentName(DecompileContext context, int index)
    {
        // GMLv2 introduced named arguments
        if (!context.GMLv2)
        {
            return null;
        }

        // Look up existing name, and use that, if it exists already
        if (NamedArgumentByIndex.TryGetValue(index, out string? existingName))
        {
            return existingName;
        }

        string? name = null;

        // Resolve name from registry
        string? codeEntryName = CodeEntryName;
        if (codeEntryName is not null)
        {
            name = context.GameContext.GameSpecificRegistry.NamedArgumentResolver.ResolveArgument(codeEntryName, index);
        }

        // If no name exists in the registry, auto-generate one from settings
        name ??= string.Format(context.Settings.UnknownArgumentNamePattern, index);

        // Resolve conflicts with local variable names
        while (LocalVariableNames.Contains(name) || NamedArguments.Contains(name))
        {
            name += "_";
        }

        // Add new named argument, and return it
        NamedArguments.Add(name);
        NamedArgumentByIndex[index] = name;
        return name;
    }

    /// <summary>
    /// Pushes a new local scope for this fragment.
    /// </summary>
    internal void PushLocalScope(DecompileContext context, BlockNode containingBlock, IStatementNode startStatement)
    {
        if (!context.Settings.CleanupLocalVarDeclarations)
        {
            return;
        }

        LocalScope? oldScope = CurrentLocalScope;
        CurrentLocalScope = new LocalScope(oldScope, containingBlock, startStatement);
        oldScope?.Children?.Add(CurrentLocalScope);
    }

    /// <summary>
    /// Pops a local scope for this fragment.
    /// </summary>
    internal void PopLocalScope(DecompileContext context)
    {
        if (!context.Settings.CleanupLocalVarDeclarations)
        {
            return;
        }

        LocalScope oldScope = CurrentLocalScope!;
        CurrentLocalScope = oldScope.Parent;

        if (CurrentLocalScope is null)
        {
            // Perform a pass on local scopes, generating local variable declarations.
            HashSet<string> declaredAnywhere = new(LocalVariableNames.Count);
            oldScope.GenerateDeclarations(declaredAnywhere);

            // Generate local variable declaration for any remaining undeclared locals
            List<string> toDeclareAtTop = new(LocalVariableNames.Count);
            foreach (string local in LocalVariableNamesList)
            {
                if (!declaredAnywhere.Contains(local))
                {
                    toDeclareAtTop.Add(local);
                }
            }
            if (toDeclareAtTop.Count > 0)
            {
                LocalVarDeclNode decl = new();
                decl.Locals.AddRange(toDeclareAtTop);
                oldScope.ContainingBlock.Children.Insert(0, decl);
            }
        }
    }
}
