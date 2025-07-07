/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using static Underanalyzer.IGMInstruction;

namespace Underanalyzer.Compiler.Bytecode;

/// <summary>
/// Interface for control flow contexts during bytecode generation.
/// </summary>
internal interface IControlFlowContext
{
    /// <summary>
    /// Whether this control flow context requires cleanup upon exiting early.
    /// </summary>
    public bool RequiresCleanup { get; }

    /// <summary>
    /// Whether this control flow context is for a loop.
    /// </summary>
    public bool IsLoop { get; }

    /// <summary>
    /// Whether "continue" is allowed to be used (this can change).
    /// </summary>
    public bool CanContinueBeUsed { get; set; }

    /// <summary>
    /// Generates cleanup code for the control flow context, using the given bytecode context.
    /// </summary>
    public void GenerateCleanupCode(BytecodeContext context);

    /// <summary>
    /// Uses a break statement branch on this control flow context, for the provided instruction.
    /// </summary>
    public void UseBreak(BytecodeContext context, IGMInstruction instruction);

    /// <summary>
    /// Uses a continue statement branch on this control flow context, for the provided instruction.
    /// </summary>
    public void UseContinue(BytecodeContext context, IGMInstruction instruction);
}

/// <summary>
/// Control flow context for all types of loops.
/// </summary>
internal abstract class LoopContext(IMultiBranchPatch breakPatch, IMultiBranchPatch continuePatch) : IControlFlowContext
{
    /// <inheritdoc/>
    public abstract bool RequiresCleanup { get; }

    /// <inheritdoc/>
    public bool IsLoop => true;

    /// <inheritdoc/>
    public bool CanContinueBeUsed { get; set; } = true;

    /// <inheritdoc/>
    public abstract void GenerateCleanupCode(BytecodeContext context);

    /// <inheritdoc/>
    public void UseBreak(BytecodeContext context, IGMInstruction instruction)
    {
        breakPatch.AddInstruction(context, instruction);
    }

    /// <inheritdoc/>
    public void UseContinue(BytecodeContext context, IGMInstruction instruction)
    {
        continuePatch.AddInstruction(context, instruction);
    }
}

/// <summary>
/// Control flow context for basic loops, such as while/for loops (with no cleanup, etc.).
/// </summary>
internal sealed class BasicLoopContext(IMultiBranchPatch breakPatch, IMultiBranchPatch continuePatch) : LoopContext(breakPatch, continuePatch)
{
    /// <inheritdoc/>
    public override bool RequiresCleanup => false;

    /// <inheritdoc/>
    public override void GenerateCleanupCode(BytecodeContext context)
    {
    }
}

/// <summary>
/// Control flow context for with loops.
/// </summary>
internal sealed class WithLoopContext(IMultiBranchPatch breakPatch, IMultiBranchPatch continuePatch) : LoopContext(breakPatch, continuePatch)
{
    /// <inheritdoc/>
    public override bool RequiresCleanup => true;

    /// <inheritdoc/>
    public override void GenerateCleanupCode(BytecodeContext context)
    {
        context.EmitPopWithExit();
    }
}

/// <summary>
/// Control flow context for repeat loops.
/// </summary>
internal sealed class RepeatLoopContext(IMultiBranchPatch breakPatch, IMultiBranchPatch continuePatch) : LoopContext(breakPatch, continuePatch)
{
    /// <inheritdoc/>
    public override bool RequiresCleanup => true;

    /// <inheritdoc/>
    public override void GenerateCleanupCode(BytecodeContext context)
    {
        // Clear loop counter from stack (only in GMLv2, apparently)
        if (context.CompileContext.GameContext.UsingGMLv2)
        {
            context.Emit(Opcode.PopDelete, DataType.Int32);
        }
    }
}

/// <summary>
/// Control flow context for switch statements.
/// </summary>
internal sealed class SwitchContext(DataType expressionType, IMultiBranchPatch breakPatch, IMultiBranchPatch continuePatch) : IControlFlowContext
{
    /// <inheritdoc/>
    public bool RequiresCleanup => true;

    /// <inheritdoc/>
    public bool IsLoop => false;

    /// <inheritdoc/>
    public bool CanContinueBeUsed { get; set; } = true;

    /// <inheritdoc/>
    public void GenerateCleanupCode(BytecodeContext context)
    {
        // Clear original switch expression from stack
        context.Emit(Opcode.PopDelete, expressionType);
    }

    /// <inheritdoc/>
    public void UseBreak(BytecodeContext context, IGMInstruction instruction)
    {
        breakPatch.AddInstruction(context, instruction);
    }

    /// <inheritdoc/>
    public void UseContinue(BytecodeContext context, IGMInstruction instruction)
    {
        continuePatch.AddInstruction(context, instruction);
    }
}
