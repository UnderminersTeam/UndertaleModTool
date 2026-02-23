/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;

namespace Underanalyzer.Compiler.Bytecode;

/// <summary>
/// Interface for patching branch offsets into an arbitrary number of instructions.
/// </summary>
internal interface IMultiBranchPatch
{
    /// <summary>
    /// Adds an instruction to be patched, <b>relative to the current bytecode position minus the size of the instruction</b>.
    /// </summary>
    public void AddInstruction(BytecodeContext context, IGMInstruction instruction);
}

/// <summary>
/// Branch patch for an arbitrary number of forward branches.
/// </summary>
internal readonly struct MultiForwardBranchPatch() : IMultiBranchPatch
{
    // Address that will be branched to by all patched instructions
    private readonly List<(IGMInstruction Instruction, int Address)> _instructions = new(4);

    /// <summary>
    /// Whether this branch patch has been used by any instructions.
    /// </summary>
    public bool Used => _instructions.Count > 0;

    /// <summary>
    /// Number of instructions that have used this patch.
    /// </summary>
    public int NumberUsed => _instructions.Count;

    /// <inheritdoc/>
    public void AddInstruction(BytecodeContext context, IGMInstruction instruction)
    {
        _instructions.Add((instruction, context.Position - IGMInstruction.GetSize(instruction)));
    }

    /// <summary>
    /// Patches all added instructions, based on the current bytecode position.
    /// </summary>
    public void Patch(BytecodeContext context)
    {
        int destAddress = context.Position;
        foreach ((IGMInstruction instruction, int address) in _instructions)
        {
            context.PatchBranch(instruction, destAddress - address);
        }
    }
}

/// <summary>
/// Branch patch for an arbitrary number of backward branches.
/// </summary>
internal readonly struct MultiBackwardBranchPatch(BytecodeContext context) : IMultiBranchPatch
{
    // Address that will be branched to by all patched instructions
    private readonly int _destAddress = context.Position;

    /// <inheritdoc/>
    public void AddInstruction(BytecodeContext context, IGMInstruction instruction)
    {
        context.PatchBranch(instruction, _destAddress - (context.Position - IGMInstruction.GetSize(instruction)));
    }
}

/// <summary>
/// Branch patch for an arbitrary number of backward branches, but also tracked.
/// </summary>
internal class MultiBackwardBranchPatchTracked(BytecodeContext context) : IMultiBranchPatch
{
    // Address that will be branched to by all patched instructions
    private readonly int _destAddress = context.Position;

    /// <summary>
    /// Whether this branch patch has been used by any instructions.
    /// </summary>
    public bool Used => NumberUsed > 0;

    /// <summary>
    /// Number of instructions that have used this patch.
    /// </summary>
    public int NumberUsed { get; private set; } = 0;

    /// <inheritdoc/>
    public void AddInstruction(BytecodeContext context, IGMInstruction instruction)
    {
        NumberUsed++;
        context.PatchBranch(instruction, _destAddress - (context.Position - IGMInstruction.GetSize(instruction)));
    }
}

/// <summary>
/// Helper struct to make a single forward branch, <b>relative to the current bytecode position minus the size of the instruction</b>.
/// </summary>
internal readonly ref struct SingleForwardBranchPatch(BytecodeContext context, IGMInstruction instruction)
{
    // Address that is being branched from
    private readonly int _startAddress = context.Position - IGMInstruction.GetSize(instruction);

    /// <summary>
    /// Patches the single instruction, based on the current bytecode position.
    /// </summary>
    public void Patch(BytecodeContext context)
    {
        context.PatchBranch(instruction, context.Position - _startAddress);
    }
}
