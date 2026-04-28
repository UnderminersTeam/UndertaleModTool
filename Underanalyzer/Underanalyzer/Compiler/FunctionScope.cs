/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Underanalyzer.Compiler.Bytecode;
using Underanalyzer.Compiler.Nodes;

namespace Underanalyzer.Compiler;

/// <summary>
/// Structure used to track data at the level of a specific function/event/script scope.
/// </summary>
public sealed class FunctionScope(FunctionScope? parent, bool isFunction)
{
    /// <summary>
    /// Parent scope of this function scope, or <see langword="null"/> if none.
    /// </summary>
    public FunctionScope? Parent { get; } = parent;

    /// <summary>
    /// Whether this scope is for specifically a function, and not a script or event.
    /// </summary>
    public bool IsFunction { get; } = isFunction;

    /// <summary>
    /// Number of locals declared in this scope.
    /// </summary>
    public int LocalCount => _declaredLocals.Count;

    /// <summary>
    /// If not <see langword="null"/>, this is the block used for initializing static variables for this scope.
    /// </summary>
    internal BlockNode? StaticInitializerBlock { get; set; } = null;

    /// <summary>
    /// Stack of control flow contexts used during bytecode generation.
    /// </summary>
    internal Stack<IControlFlowContext>? ControlFlowContexts { get; set; } = null;

    /// <summary>
    /// Whether bytecode is currently being generated for a static block.
    /// </summary>
    internal bool GeneratingStaticBlock { get; set; } = false;

    /// <summary>
    /// Whether bytecode is currently being generated for a function call, where the function being called ends in a dot variable access.
    /// </summary>
    internal bool GeneratingDotVariableCall { get; set; } = false;

    /// <summary>
    /// Whether currently post-processing a statement that requires extra logic for break/continue during code rewriting.
    /// </summary>
    internal bool ProcessingBreakContinueContext { get; set; } = false;

    /// <summary>
    /// If generating code inside of a static variable assignment, this is the name of the
    /// static variable being assigned to.
    /// </summary>
    internal string? StaticVariableName { get; set; } = null;

    /// <summary>
    /// List of nodes to duplicate when exiting early from a finally block.
    /// One node per each try statement.
    /// </summary>
    internal List<IASTNode> TryFinallyNodes { get; set; } = [];

    /// <summary>
    /// Array owner function ID associated with this scope, based on <see cref="BytecodeContext.LastFunctionID"/>.
    /// </summary>
    internal long ArrayOwnerID { get; set; } = 1;

    // Set of local variables declared for this scope
    private readonly HashSet<string> _declaredLocals = new(8);

    // List (in order) of local variables declared for this scope
    private readonly List<string> _localsOrder = new(8);

    // Set of static variables declared for this scope
    private readonly HashSet<string> _declaredStatics = new(8);

    // Lookup of argument names to argument indices
    private readonly Dictionary<string, int> _declaredArguments = new(8);

    // Functions declared in this scope (actual entries are assigned during bytecode generation)
    private readonly Dictionary<string, FunctionEntry?> _declaredFunctions = new(4);

    /// <summary>
    /// Declares a local variable for this function scope.
    /// </summary>
    /// <param name="name">Name of the local variable to be declared.</param>
    /// <returns>True if the local was not yet declared; false otherwise.</returns>
    internal bool DeclareLocal(string name)
    {
        if (_declaredLocals.Add(name))
        {
            _localsOrder.Add(name);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Returns whether or not a local variable is declared for this function scope.
    /// </summary>
    /// <param name="name">Name of the local variable to check.</param>
    /// <returns>True if the local variable has been declared; false otherwise.</returns>
    internal bool IsLocalDeclared(string name)
    {
        return _declaredLocals.Contains(name);
    }

    /// <summary>
    /// Declares a static variable for this function scope.
    /// </summary>
    /// <param name="name">Name of the static variable to be declared.</param>
    /// <returns>True if the static was not yet declared; false otherwise.</returns>
    internal bool DeclareStatic(string name)
    {
        return _declaredStatics.Add(name);
    }

    /// <summary>
    /// Returns whether or not a static variable is declared for this function scope.
    /// </summary>
    /// <param name="name">Name of the static variable to check.</param>
    /// <returns>True if the static variable has been declared; false otherwise.</returns>
    internal bool IsStaticDeclared(string name)
    {
        return _declaredStatics.Contains(name);
    }

    /// <summary>
    /// Declares argument names for this function scope.
    /// </summary>
    /// <param name="argumentNames">List of arguments, in order, to declare.</param>
    internal void DeclareArguments(List<string> argumentNames)
    {
        // Map argument names to corresponding index
        for (int i = 0; i < argumentNames.Count; i++)
        {
            _declaredArguments[argumentNames[i]] = i;
        }
    }

    /// <summary>
    /// Attempts to look up an argument index from a variable name.
    /// </summary>
    /// <param name="name">Name of the variable to look up.</param>
    /// <returns>True if an argument index was found; false otherwise.</returns>
    internal bool TryGetArgumentIndex(string name, out int index)
    {
        return _declaredArguments.TryGetValue(name, out index);
    }

    /// <summary>
    /// Attempts to declare a function in this function scope.
    /// </summary>
    /// <param name="name">Name of the function to declare.</param>
    /// <returns>True if the function was declared; false if there was an existing function declared with the name.</returns>
    internal bool TryDeclareFunction(string name)
    {
        return _declaredFunctions.TryAdd(name, null);
    }

    /// <summary>
    /// Assigns a function entry in this function scope.
    /// </summary>
    /// <param name="name">Name of the function to declare.</param>
    /// <param name="entry">Function entry to be assigned.</param>
    internal void AssignFunctionEntry(string name, FunctionEntry entry)
    {
        _declaredFunctions[name] = entry;
    }

    /// <summary>
    /// Attempts to look up a function entry with the given name in this function scope.
    /// </summary>
    /// <remarks>
    /// If <see cref="IGameContext.UsingNewFunctionResolution"/> is <see langword="true"/>, this will check parent scopes as well.
    /// </remarks>
    /// <param name="name">Name of the function to look up.</param>
    /// <param name="entry">Output function entry, if the lookup is successful.</param>
    /// <returns><see langword="true"/> if a function entry was found; <see langword="false"/> otherwise.</returns>
    public bool TryGetDeclaredFunction(IGameContext context, string name, [NotNullWhen(true)] out FunctionEntry? entry)
    {
        if (_declaredFunctions.TryGetValue(name, out entry))
        {
            if (entry is null)
            {
                return false;
            }
            return true;
        }
        if (context.UsingNewFunctionResolution)
        {
            if (Parent is not null)
            {
                return Parent.TryGetDeclaredFunction(context, name, out entry);
            }
        }
        return false;
    }

    /// <summary>
    /// Returns whether or not a function with the given name is declared in this function scope.
    /// </summary>
    /// <remarks>
    /// If <see cref="IGameContext.UsingNewFunctionResolution"/> is <see langword="true"/>, this will check parent scopes as well.
    /// </remarks>
    public bool IsFunctionDeclared(IGameContext context, string name)
    {
        if (_declaredFunctions.ContainsKey(name))
        {
            return true;
        }
        if (context.UsingNewFunctionResolution)
        {
            if (Parent is not null && Parent.IsFunctionDeclared(context, name))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Returns whether or not a function with the given name is declared within this immediate function scope.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="IsFunctionDeclared(IGameContext, string)"/>, this will never check parent scopes.
    /// </remarks>
    public bool IsFunctionDeclaredImmediately(string name)
    {
        return _declaredFunctions.ContainsKey(name);
    }
}
