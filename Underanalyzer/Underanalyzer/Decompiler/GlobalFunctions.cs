/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Underanalyzer.Decompiler.ControlFlow;
using static Underanalyzer.IGMInstruction;

namespace Underanalyzer.Decompiler;

/// <summary>
/// Interface that can be used to supply global function information to the compiler/decompiler.
/// </summary>
public interface IGlobalFunctions
{
    /// <summary>
    /// Returns whether a global function name exists.
    /// </summary>
    /// <param name="name">Name to look up.</param>
    /// <returns><see langword="true"/> if the global function name exists; <see langword="false"/> otherwise.</returns>
    public bool FunctionNameExists(string name);

    /// <summary>
    /// Returns whether a global function exists.
    /// </summary>
    /// <param name="function">Function to look up.</param>
    /// <returns><see langword="true"/> if the global function exists; <see langword="false"/> otherwise.</returns>
    public bool FunctionExists(IGMFunction function);

    /// <summary>
    /// Attempts to look up a global function by name.
    /// </summary>
    /// <param name="name">Name to look up.</param>
    /// <param name="function">Output function, if found.</param>
    /// <returns><see langword="true"/> if the global function was successfully looked up; <see langword="false"/> otherwise.</returns>
    public bool TryGetFunction(string name, [NotNullWhen(true)] out IGMFunction? function);

    /// <summary>
    /// Attempts to look up a global function name by function.
    /// </summary>
    /// <param name="function">Function to look up.</param>
    /// <param name="name">Output name, if found.</param>
    /// <returns><see langword="true"/> if the name was successfully looked up; <see langword="false"/> otherwise.</returns>
    public bool TryGetFunctionName(IGMFunction function, [NotNullWhen(true)] out string? name);
}

/// <summary>
/// Provided way to find all global functions in a game, using some components of the decompiler.
/// </summary>
public class GlobalFunctions : IGlobalFunctions
{
    /// <summary>
    /// Lookup of function reference to name. 
    /// <see cref="IGMFunction"/> references should be the same as those input to the compiler/decompiler.
    /// </summary>
    protected Dictionary<IGMFunction, string> FunctionToName { get; }

    /// <summary>
    /// Lookup of function name to reference. 
    /// <see cref="IGMFunction"/> references should be the same as those input to the compiler/decompiler.
    /// </summary>
    protected Dictionary<string, IGMFunction> NameToFunction { get; }

    /// <summary>
    /// Initializes an empty instance of this class. Useful for pre-GMLv2.
    /// </summary>
    public GlobalFunctions()
    {
        FunctionToName = [];
        NameToFunction = [];
    }

    /// <summary>
    /// Given a list of global scripts, initializes this class with all global function information.
    /// Optionally, <see cref="ParallelOptions"/> can be passed in to configure parallelization.
    /// By default, the default settings are used (which has no limits).
    /// </summary>
    public GlobalFunctions(IEnumerable<IGMCode> globalScripts, ParallelOptions? parallelOptions = null)
    {
        Dictionary<IGMFunction, string> functionToName = [];
        Dictionary<string, IGMFunction> nameToFunction = [];
        object _lock = new();

        Parallel.ForEach(globalScripts, parallelOptions ?? new(), script =>
        {
            // Find all fragments in the code entry
            List<Block> blocks = Block.FindBlocks(script, out _);
            List<Fragment> fragments = Fragment.FindFragments(script, blocks);

            // Find names of functions after each fragment
            for (int i = 1; i < fragments.Count; i++)
            {
                Fragment fragment = fragments[i];
                if (!fragment.RootScope)
                {
                    // If the fragment isn't at the root scope, it can't be a global function
                    continue;
                }
                if (fragment.Successors.Count == 0)
                {
                    // If no successors, assume code is corrupt and don't consider it
                    continue;
                }
                if (fragment.Successors[0] is not Block after)
                {
                    // If block after isn't a block, assume code is corrupt as well
                    continue;
                }

                if (GetFunctionNameAfterFragment(after, out string? name, out IGMFunction? function))
                {
                    lock (_lock)
                    {
                        functionToName[function] = name;
                        nameToFunction[name] = function;
                    }
                }
            }
        });

        FunctionToName = functionToName;
        NameToFunction = nameToFunction;
    }

    /// <inheritdoc/>
    public bool FunctionNameExists(string name)
    {
        return NameToFunction.ContainsKey(name);
    }

    /// <inheritdoc/>
    public bool FunctionExists(IGMFunction function)
    {
        return FunctionToName.ContainsKey(function);
    }

    /// <inheritdoc/>
    public bool TryGetFunction(string name, [NotNullWhen(true)] out IGMFunction? function)
    {
        return NameToFunction.TryGetValue(name, out function);
    }

    /// <inheritdoc/>
    public bool TryGetFunctionName(IGMFunction function, [NotNullWhen(true)] out string? name)
    {
        return FunctionToName.TryGetValue(function, out name);
    }

    /// <summary>
    /// Adds an additional function to the lookup.
    /// </summary>
    /// <remarks>
    /// This should not be used during Underanalyzer compilation/decompilation; this method is not thread-safe.
    /// </remarks>
    public void DefineFunction(string functionName, IGMFunction function)
    {
        FunctionToName[function] = functionName;
        NameToFunction[functionName] = function;
    }

    /// <summary>
    /// Removes a function from the lookup.
    /// </summary>
    /// <remarks>
    /// This should not be used during Underanalyzer compilation/decompilation; this method is not thread-safe.
    /// </remarks>
    public void UndefineFunction(string functionName, IGMFunction function)
    {
        FunctionToName.Remove(function);
        NameToFunction.Remove(functionName);
    }

    /// <summary>
    /// Returns the full list of defined functions.
    /// </summary>
    public IEnumerable<IGMFunction> GetFunctions()
    {
        return FunctionToName.Keys;
    }

    /// <summary>
    /// Returns the full list of defined function names.
    /// </summary>
    public IEnumerable<string> GetFunctionNames()
    {
        return NameToFunction.Keys;
    }

    /// <summary>
    /// Gets the name of a global function based on the instructions after a code fragment.
    /// Returns true if one is found, or false if the code is corrupt or one could not be found.
    /// </summary>
    private static bool GetFunctionNameAfterFragment(Block block, [MaybeNullWhen(false)] out string name, [MaybeNullWhen(false)] out IGMFunction foundFunction)
    {
        name = null;
        foundFunction = null;

        // Ensure enough instructions exist
        if (block.Instructions.Count < 3)
        {
            return false;
        }

        // Get function reference for fragment
        if (block.Instructions[0] is not { Kind: Opcode.Push, Type1: DataType.Int32, ResolvedFunction: IGMFunction function } || function is null)
        {
            return false;
        }
        foundFunction = function;

        // Ensure conv instruction exists
        if (block.Instructions[1] is not { Kind: Opcode.Convert, Type1: DataType.Int32, Type2: DataType.Variable })
        {
            return false;
        }

        switch (block.Instructions[2].Kind)
        {
            case Opcode.PushImmediate:
                {
                    // Normal function. Skip past basic instructions.
                    if (block.Instructions is not
                        [
                            _, _,
                        { ValueShort: -1 or -16 },
                        { Kind: Opcode.Convert, Type1: DataType.Int32, Type2: DataType.Variable },
                        { Kind: Opcode.Call, ResolvedFunction.Name.Content: VMConstants.MethodFunction },
                            ..
                        ])
                    {
                        // Failed to match instructions
                        return false;
                    }

                    // Check if we have a name
                    if (block   .Instructions is
                        [
                            _, _, _, _, _,
                        { Kind: Opcode.Duplicate, DuplicationSize2: 0 },
                        { Kind: Opcode.PushImmediate },
                        { Kind: Opcode.Pop, ResolvedVariable.Name.Content: string funcName },
                            ..
                        ])
                    {
                        // We have a name!
                        name = funcName;
                        return true;
                    }
                    break;
                }
            case Opcode.Call:
                {
                    // This is a struct or constructor function
                    if (block.Instructions is not
                        [
                            _, _,
                        { Kind: Opcode.Call, ResolvedFunction.Name.Content: VMConstants.NullObjectFunction },
                        { Kind: Opcode.Call, ResolvedFunction.Name.Content: VMConstants.MethodFunction },
                            ..
                        ])
                    {
                        // Failed to match instructions
                        return false;
                    }

                    // Check if we're a struct or function constructor (named)
                    if (block.Instructions is
                        [
                            _, _, _, _,
                        { Kind: Opcode.Duplicate, DuplicationSize2: 0 },
                        { Kind: Opcode.PushImmediate, ValueShort: short pushVal },
                        { Kind: Opcode.Pop, ResolvedVariable.Name.Content: string funcName },
                            ..
                        ])
                    {
                        // Check if struct or constructor
                        if (pushVal != -16 && pushVal != -5)
                        {
                            // We're a constructor!
                            name = funcName;
                            return true;
                        }
                    }
                    break;
                }
        }

        return false;
    }
}
