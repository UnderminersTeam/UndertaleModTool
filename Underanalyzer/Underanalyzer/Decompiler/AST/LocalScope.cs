/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;

namespace Underanalyzer.Decompiler.AST;

/// <summary>
/// Represents a scope within the AST for which local variables can be declared.
/// </summary>
internal sealed class LocalScope(LocalScope? parent, BlockNode containingBlock, IStatementNode startStatement)
{
    /// <summary>
    /// The block that contains this local scope, directly.
    /// This block must directly contain <see cref="StartStatement"/>.
    /// </summary>
    public BlockNode ContainingBlock { get; } = containingBlock;

    /// <summary>
    /// The first statement that is within this local scope.
    /// For example, with an if statement's true/false (else) blocks, this 
    /// would be the enveloping overall <see cref="IfNode"/>.
    /// </summary>
    /// <remarks>
    /// Specifically for blocks at the root of their fragment, 
    /// this is the same as <see cref="ContainingBlock"/>.
    /// </remarks>
    public IStatementNode StartStatement { get; } = startStatement;

    /// <summary>
    /// Scopes contained within this local scope.
    /// </summary>
    public List<LocalScope> Children { get; } = new(4);

    /// <summary>
    /// Scope containing this local scope, if one exists; null otherwise.
    /// </summary>
    public LocalScope? Parent { get; private set; } = parent;

    /// <summary>
    /// Local variable names that have been denoted as "declared" for this local scope.
    /// This means that, each local variable, if not declared already, should be declared
    /// before its first usage inside of the scope.
    /// </summary>
    public HashSet<string> DeclaredLocals { get; } = new(4);

    /// <summary>
    /// For local variables that are denoted as "declared" for this scope,
    /// this is a map of their names to their first <see cref="AssignNode"/>,
    /// for use in generating declarations.
    /// </summary>
    public Dictionary<string, AssignNode> FirstLocalAssignments { get; } = new(4);

    /// <summary>
    /// Local variable names that were specifically hoisted to be declared just 
    /// *before* this local scope's <see cref="StartStatement"/>.
    /// </summary>
    public List<string> HoistedLocals { get; } = new(4);

    // Prevents scope from being considered too many times during certain search operations
    private bool _ignoreInSearch = false;

    /// <summary>
    /// Inserts this local scope as a child of another existing local scope.
    /// </summary>
    public void InsertAsChildOf(LocalScope parent)
    {
        Parent = parent;
        parent.Children.Add(this);
    }

    /// <summary>
    /// Returns whether a local variable has already been denoted as "declared" in this 
    /// local scope, or any of its parent local scopes.
    /// </summary>
    public bool LocalDeclaredInAnyParentOrSelf(string localName)
    {
        if (DeclaredLocals.Contains(localName))
        {
            return true;
        }
        if (Parent is not null)
        {
            return Parent.LocalDeclaredInAnyParentOrSelf(localName);
        }
        return false;
    }

    /// <summary>
    /// Enumerates all children scopes in order, finding the first one that either itself
    /// declares the given local variable name, or one of its own children declares the given
    /// local variable name. If no such child exists, returns null.
    /// </summary>
    public LocalScope? FindLocalDeclaredInAnyChild(string localName)
    {
        foreach (LocalScope child in Children)
        {
            if (child._ignoreInSearch)
            {
                // Already considered and ruled out this child, no need to do it again
                continue;
            }
            if (child.DeclaredLocals.Contains(localName))
            {
                return child;
            }
            if (child.FindLocalDeclaredInAnyChild(localName) is not null)
            {
                return child;
            }
        }
        return null;
    }

    /// <summary>
    /// In the situation that a local variable is not marked as declared in a scope, 
    /// nor its parents (see <see cref="LocalDeclaredInAnyParentOrSelf(string)"/>),
    /// this can be used to return the best local scope to hoist an earlier local variable
    /// declaration to. Specifically, it should be hoisted to just before the scope.
    /// 
    /// Returns null if no suitable scope is found (i.e., the local is never declared anywhere).
    /// </summary>
    public LocalScope? FindBestHoistLocation(string localName)
    {
        // Look for locals in any immediate child scopes first
        if (FindLocalDeclaredInAnyChild(localName) is LocalScope result)
        {
            return result;
        }

        // Since none were found here, search the parent, if one exists
        if (Parent is not null)
        {
            // Prevent this scope from being searched for locals (we already considered it)
            _ignoreInSearch = true;

            LocalScope? parentResult = Parent.FindBestHoistLocation(localName);

            _ignoreInSearch = false;
            return parentResult;
        }

        // No parent, so no result
        return null;
    }

    /// <summary>
    /// Generates local variable declarations where necessary.
    /// Recursively performs this operation across all local scopes in the fragment.
    /// </summary>
    public void GenerateDeclarations(HashSet<string> declaredAnywhere)
    {
        // First, hoist local variables from child scopes.
        HashSet<string> hoistedLocals = new(8);
        foreach (LocalScope child in Children)
        {
            if (child.HoistedLocals.Count > 0)
            {
                // This child scope has hoisted locals declared before its scope.

                // Generate a local var declaration, and add those locals to it.
                LocalVarDeclNode? localDecl = null;
                bool anyNew = false;
                foreach (string hoistedLocal in child.HoistedLocals)
                {
                    if (Parent is null || !Parent.LocalDeclaredInAnyParentOrSelf(hoistedLocal))
                    {
                        anyNew = true;
                        localDecl ??= new();
                        localDecl.Locals.Add(hoistedLocal);
                        declaredAnywhere.Add(hoistedLocal);
                        hoistedLocals.Add(hoistedLocal);
                    }
                }

                // Ensure there's at least one local being hoisted
                if (!anyNew)
                {
                    continue;
                }

                // Find start statement and insert just before it
                List<IStatementNode> blockChildren = child.ContainingBlock.Children;
                int index = blockChildren.IndexOf(child.StartStatement);
                if (index == -1)
                {
                    // Failsafe; start at 0. This usually shouldn't happen, though.
                    index = 0;
                }
                blockChildren.Insert(index, localDecl!);

                /*
                // TODO? perhaps add this as a setting, but for now, will disable this to keep code style consistent
                // If immediately before a node that has an empty line before it, take it over.
                if (index < blockChildren.Count - 1 && blockChildren[index + 1].EmptyLineBefore)
                {
                    blockChildren[index + 1].EmptyLineBefore = false;
                    localDecl!.EmptyLineBefore = true;
                }
                */
            }
        }

        // For remaining locals to be declared in this scope, declare them on their first assignments
        foreach (string local in DeclaredLocals)
        {
            if (hoistedLocals.Contains(local))
            {
                continue;
            }
            if (Parent is null || !Parent.LocalDeclaredInAnyParentOrSelf(local))
            {
                FirstLocalAssignments[local].DeclareLocalVar = true;
                declaredAnywhere.Add(local);
            }
        }

        // Generate declarations for child scopes
        foreach (LocalScope child in Children)
        {
            child.GenerateDeclarations(declaredAnywhere);
        }
    }
}