/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using Underanalyzer;
using Underanalyzer.Decompiler;
using Underanalyzer.Decompiler.ControlFlow;
using Underanalyzer.Mock;

namespace UnderanalyzerTest;

internal static class TestUtil
{
    /// <summary>
    /// Utility function to reduce having to split lines in tests.
    /// </summary>
    public static GMCode GetCode(string assembly, IGameContext? context = null)
    {
        string[] lines = assembly.Split('\n');
        return VMAssembly.ParseAssemblyFromLines(lines, context);
    }

    /// <summary>
    /// Asserts that for every predecessor, there is a corresponding successor, and vice versa.
    /// Additionally asserts that for every parent, there is a child (and NOT the other way around).
    /// </summary>
    public static void VerifyFlowDirections(IEnumerable<IControlFlowNode> nodes)
    {
        foreach (var node in nodes)
        {
            foreach (var pred in node.Predecessors)
            {
                Assert.Contains(node, pred.Successors);
            }
            foreach (var succ in node.Successors)
            {
                Assert.Contains(node, succ.Predecessors);
            }
            if (node.Parent is not null)
            {
                Assert.Contains(node, node.Parent.Children);
            }
        }
    }

    /// <summary>
    /// Throws an exception if there's any detected continue/break statements that have yet to be
    /// processed. This indicates continue/break detection and/or processing is broken.
    /// </summary>
    public static void EnsureNoRemainingJumps(DecompileContext ctx)
    {
        List<Block> blocks = ctx.Blocks!;
        List<BinaryBranch> branches = ctx.BinaryBranchNodes!;

        foreach (BinaryBranch bb in branches)
        {
            int startIndex = ((Block)bb.Condition).BlockIndex;
            int endAddress = bb.EndAddress;
            for (int i = startIndex + 1; i < blocks.Count && blocks[i].StartAddress < endAddress; i++)
            {
                Block block = blocks[i];
                if (block.Instructions is [.., { Kind: IGMInstruction.Opcode.Branch }] &&
                    block.Successors.Count >= 1 && block.Successors[0].StartAddress >= endAddress)
                {
                    throw new Exception("Found unprocessed break/continue");
                }
            }
        }
    }

    /// <summary>
    /// Asserts that the decompilation result of the assembly equals the provided GML, as a string.
    /// </summary>
    public static DecompileContext VerifyDecompileResult(string asm, string gml, GameContextMock? gameContext = null, DecompileSettings? decompileSettings = null)
    {
        gameContext ??= new();
        DecompileContext decompilerContext = new(gameContext, GetCode(asm, gameContext), decompileSettings);
        string decompileResult = decompilerContext.DecompileToString().Trim();
        Assert.Equal(gml.Trim().ReplaceLineEndings("\n"), decompileResult);
        return decompilerContext;
    }
}
