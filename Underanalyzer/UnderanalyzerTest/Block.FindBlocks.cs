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

public class Block_FindBlocks
{
    [Fact]
    public void TestEmpty()
    {
        GMCode code = TestUtil.GetCode("");
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);

        Assert.Single(blocks);
        Assert.Equal(0, blocks[0].StartAddress);
        Assert.Equal(0, blocks[0].EndAddress);
        Assert.Empty(blocks[0].Instructions);
    }

    [Fact]
    public void TestSingle()
    {
        GMCode code = TestUtil.GetCode(
            """
            pushi.e 123
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);

        Assert.Equal(2, blocks.Count);
        Assert.Equal(0, blocks[0].StartAddress);
        Assert.Equal(4, blocks[0].EndAddress);
        Assert.Equal(4, blocks[1].StartAddress);
        Assert.Equal(4, blocks[1].EndAddress);
        Assert.Single(blocks[0].Instructions);
        Assert.Equal(IGMInstruction.Opcode.PushImmediate, blocks[0].Instructions[0].Kind);
        Assert.Empty(blocks[1].Instructions);
        Assert.Equal(blocks[1], blocks[0].Successors[0]);
        Assert.Single(blocks[0].Successors);
        Assert.Empty(blocks[0].Predecessors);
        Assert.Equal(blocks[0], blocks[1].Predecessors[0]);
        Assert.Single(blocks[1].Predecessors);
        Assert.Empty(blocks[1].Successors);
    }

    [Fact]
    public void TestIfElse()
    {
        GMCode code = TestUtil.GetCode(
            """
            :[0]
            pushi.e 0
            bf [2]

            :[1]
            pushi.e 1
            b [3]

            :[2]
            pushi.e 2

            :[3]
            pushi.e 3
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);

        Assert.Equal(5, blocks.Count);
        for (int i = 0; i <= 3; i++)
            Assert.Equal(i, blocks[i].Instructions[0].ValueShort);
        Assert.Empty(blocks[4].Instructions);

        Assert.Empty(blocks[0].Predecessors);
        Assert.Equal(2, blocks[0].Successors.Count);
        Assert.Equal(blocks[1], blocks[0].Successors[0]);
        Assert.Equal(blocks[2], blocks[0].Successors[1]);

        Assert.Single(blocks[1].Predecessors);
        Assert.Contains(blocks[0], blocks[1].Predecessors);
        Assert.Single(blocks[1].Successors);
        Assert.Contains(blocks[3], blocks[1].Successors);

        Assert.Single(blocks[2].Predecessors);
        Assert.Contains(blocks[0], blocks[2].Predecessors);
        Assert.Single(blocks[2].Successors);
        Assert.Contains(blocks[3], blocks[2].Successors);

        Assert.Equal(2, blocks[3].Predecessors.Count);
        Assert.Equal(blocks[1], blocks[3].Predecessors[0]);
        Assert.Equal(blocks[2], blocks[3].Predecessors[1]);
        Assert.Single(blocks[3].Successors);
        Assert.Contains(blocks[4], blocks[3].Successors);

        Assert.Single(blocks[4].Predecessors);
        Assert.Contains(blocks[3], blocks[4].Predecessors);
    }

    [Fact]
    public void TestLoop()
    {
        GMCode code = TestUtil.GetCode(
            """
            :[0]
            pushi.e 0
            bf [2]

            :[1]
            pushi.e 1
            b [0]

            :[2]
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);

        Assert.Equal(3, blocks.Count);
        for (int i = 0; i <= 1; i++)
            Assert.Equal(i, blocks[i].Instructions[0].ValueShort);
        Assert.Empty(blocks[2].Instructions);

        Assert.Single(blocks[0].Predecessors);
        Assert.Contains(blocks[1], blocks[0].Predecessors);
        Assert.Equal(2, blocks[0].Successors.Count);
        Assert.Equal(blocks[1], blocks[0].Successors[0]);
        Assert.Equal(blocks[2], blocks[0].Successors[1]);

        Assert.Single(blocks[1].Predecessors);
        Assert.Contains(blocks[0], blocks[1].Predecessors);
        Assert.Single(blocks[1].Successors);
        Assert.Contains(blocks[0], blocks[1].Successors);

        Assert.Single(blocks[2].Predecessors);
        Assert.Contains(blocks[0], blocks[2].Predecessors);
        Assert.Empty(blocks[2].Successors);

        TestUtil.VerifyFlowDirections(blocks);
    }

    [Fact]
    public void TestWith()
    {
        GMCode code = TestUtil.GetCode(
            """
            :[0]
            pushi.e 0
            pushenv [2]

            :[1]
            pushi.e 1

            :[2]
            # Note: This handling is different than other pre-existing tooling.
            # We treat popenv as a branch instruction as well, when it has a destination.
            popenv [1]

            :[3]
            pushi.e 3

            :[4]
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);

        Assert.Equal(5, blocks.Count);
        Assert.Equal(0, blocks[0].Instructions[0].ValueShort);
        Assert.Equal(1, blocks[1].Instructions[0].ValueShort);
        Assert.Equal(3, blocks[3].Instructions[0].ValueShort);
        Assert.Empty(blocks[4].Instructions);

        Assert.Empty(blocks[0].Predecessors);
        Assert.Equal(2, blocks[0].Successors.Count);
        Assert.Equal(blocks[1], blocks[0].Successors[0]);
        Assert.Equal(blocks[2], blocks[0].Successors[1]);

        Assert.Equal(2, blocks[1].Predecessors.Count);
        Assert.Equal(blocks[0], blocks[1].Predecessors[0]);
        Assert.Equal(blocks[2], blocks[1].Predecessors[1]);
        Assert.Single(blocks[1].Successors);
        Assert.Contains(blocks[2], blocks[1].Successors);

        Assert.Equal(2, blocks[2].Predecessors.Count);
        Assert.Equal(blocks[0], blocks[2].Predecessors[0]);
        Assert.Equal(blocks[1], blocks[2].Predecessors[1]);
        Assert.Equal(2, blocks[2].Successors.Count);
        Assert.Equal(blocks[3], blocks[2].Successors[0]);
        Assert.Equal(blocks[1], blocks[2].Successors[1]);

        Assert.Single(blocks[3].Predecessors);
        Assert.Contains(blocks[2], blocks[3].Predecessors);

        Assert.Single(blocks[4].Predecessors);
        Assert.Contains(blocks[3], blocks[4].Predecessors);

        TestUtil.VerifyFlowDirections(blocks);
    }

    [Fact]
    public void TestBreakWith()
    {
        GMCode code = TestUtil.GetCode(
            """
            :[0]
            pushi.e 0
            pushenv [2]

            :[1]
            pushi.e 1
            b [4]

            :[2]
            popenv [1]

            :[3]
            b [5]

            :[4]
            popenv <drop>

            :[5]
            pushi.e 5

            :[6]
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);

        Assert.Equal(7, blocks.Count);
        Assert.Equal(0, blocks[0].Instructions[0].ValueShort);
        Assert.Equal(1, blocks[1].Instructions[0].ValueShort);
        Assert.Equal(5, blocks[5].Instructions[0].ValueShort);
        Assert.Empty(blocks[6].Instructions);

        Assert.Empty(blocks[0].Predecessors);
        Assert.Equal(2, blocks[0].Successors.Count);
        Assert.Equal(blocks[1], blocks[0].Successors[0]);
        Assert.Equal(blocks[2], blocks[0].Successors[1]);

        Assert.Equal(2, blocks[1].Predecessors.Count);
        Assert.Equal(blocks[0], blocks[1].Predecessors[0]);
        Assert.Equal(blocks[2], blocks[1].Predecessors[1]);
        Assert.Single(blocks[1].Successors);
        Assert.Contains(blocks[4], blocks[1].Successors);

        Assert.Single(blocks[2].Predecessors);
        Assert.Equal(blocks[0], blocks[2].Predecessors[0]);
        Assert.Equal(2, blocks[2].Successors.Count);
        Assert.Equal(blocks[3], blocks[2].Successors[0]);
        Assert.Equal(blocks[1], blocks[2].Successors[1]);

        Assert.Single(blocks[3].Predecessors);
        Assert.Equal(blocks[2], blocks[3].Predecessors[0]);
        Assert.Single(blocks[3].Successors);
        Assert.Equal(blocks[5], blocks[3].Successors[0]);

        Assert.Equal([blocks[3], blocks[4]], blocks[5].Predecessors);

        Assert.Single(blocks[6].Predecessors);
        Assert.Contains(blocks[5], blocks[6].Predecessors);

        TestUtil.VerifyFlowDirections(blocks);
    }

    [Fact]
    public void TestUnreachableBranch()
    {
        GMCode code = TestUtil.GetCode(
            """
            :[0]
            pushi.e 0
            b [2]

            :[1]
            pushi.e 1

            :[2]
            pushi.e 2
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);

        Assert.Equal(4, blocks.Count);

        Assert.False(blocks[0].Unreachable);
        Assert.Empty(blocks[0].Predecessors);
        Assert.Equal([blocks[2], blocks[1]], blocks[0].Successors);

        Assert.True(blocks[1].Unreachable);
        Assert.Equal([blocks[0]], blocks[1].Predecessors);

        Assert.False(blocks[2].Unreachable);
        Assert.Equal([blocks[0], blocks[1]], blocks[2].Predecessors);

        Assert.False(blocks[3].Unreachable);
        Assert.Equal([blocks[2]], blocks[3].Predecessors);
        Assert.Empty(blocks[3].Successors);

        TestUtil.VerifyFlowDirections(blocks);
    }

    [Fact]
    public void TestUnreachableExit()
    {
        GMCode code = TestUtil.GetCode(
            """
            :[0]
            pushi.e 0
            exit.i

            :[1]
            pushi.e 1
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);

        Assert.Equal(3, blocks.Count);

        Assert.False(blocks[0].Unreachable);
        Assert.Empty(blocks[0].Predecessors);
        Assert.Equal([blocks[1]], blocks[0].Successors);

        Assert.True(blocks[1].Unreachable);
        Assert.Equal([blocks[0]], blocks[1].Predecessors);

        Assert.False(blocks[2].Unreachable);
        Assert.Equal([blocks[1]], blocks[2].Predecessors);
        Assert.Empty(blocks[2].Successors);

        TestUtil.VerifyFlowDirections(blocks);
    }

    [Fact]
    public void TestTry()
    {
        GMCode code = TestUtil.GetCode(
            """
            :[0]
            push.i 48
            conv.i.v
            push.i -1
            conv.i.v
            call.i @@try_hook@@ 2
            popz.v

            :[1]
            pushi.e 1
            pop.v.i self.a

            :[2]
            call.i @@try_unhook@@ 0
            popz.v
            pushi.e 2
            pop.v.i self.b
            call.i @@finish_finally@@ 0
            popz.v
            b [3]

            :[3]
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);

        Assert.Equal(4, blocks.Count);

        Assert.Equal(6, blocks[0].Instructions.Count);
        Assert.Empty(blocks[0].Predecessors);
        Assert.Equal([blocks[1], blocks[2]], blocks[0].Successors);

        Assert.Equal(2, blocks[1].Instructions.Count);
        Assert.Equal([blocks[0]], blocks[1].Predecessors);
        Assert.Equal([blocks[2]], blocks[1].Successors);

        Assert.Equal(7, blocks[2].Instructions.Count);
        Assert.Equal([blocks[0], blocks[1]], blocks[2].Predecessors);
        Assert.Equal([blocks[3]], blocks[2].Successors);

        TestUtil.VerifyFlowDirections(blocks);
    }

    [Fact]
    public void TestTryCatch()
    {
        GMCode code = TestUtil.GetCode(
            """
            :[0]
            push.i 108
            conv.i.v
            push.i 52
            conv.i.v
            call.i @@try_hook@@ 2
            popz.v

            :[1]
            pushi.e 1
            pop.v.i self.a
            b [3]

            :[2]
            pop.v.v local.ex
            call.i @@try_unhook@@ 0
            popz.v
            pushloc.v local.ex
            call.i show_debug_message 1
            popz.v
            call.i @@finish_catch@@ 0
            popz.v
            b [4]

            :[3]
            call.i @@try_unhook@@ 0
            popz.v

            :[4]
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);

        Assert.Equal(5, blocks.Count);

        Assert.Equal(6, blocks[0].Instructions.Count);
        Assert.Empty(blocks[0].Predecessors);
        Assert.Equal([blocks[1], blocks[3], blocks[2]], blocks[0].Successors); // Note the order: try, end/finally, catch

        Assert.Equal(3, blocks[1].Instructions.Count);
        Assert.Equal([blocks[0]], blocks[1].Predecessors);
        Assert.Equal([blocks[3]], blocks[1].Successors);

        Assert.Equal(9, blocks[2].Instructions.Count);
        Assert.Equal([blocks[0]], blocks[2].Predecessors);
        Assert.Equal([blocks[4]], blocks[2].Successors);

        Assert.Equal(2, blocks[3].Instructions.Count);
        Assert.Equal([blocks[0], blocks[1]], blocks[3].Predecessors);
        Assert.Equal([blocks[4]], blocks[3].Successors);

        TestUtil.VerifyFlowDirections(blocks);
    }
}
