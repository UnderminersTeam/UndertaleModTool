/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using Underanalyzer.Decompiler;
using Underanalyzer.Decompiler.ControlFlow;
using Underanalyzer.Mock;

namespace UnderanalyzerTest;

public class BinaryBranch_FindBinaryBranches
{
    [Fact]
    public void TestSingleIfEmpty()
    {
        GMCode code = TestUtil.GetCode(
            """
            :[0]
            push.v self.a
            conv.v.b
            bf [1]

            :[1]
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);
        List<Fragment> fragments = Fragment.FindFragments(ctx);
        List<Loop> loops = Loop.FindLoops(ctx);
        Switch.FindSwitchStatements(ctx);
        List<BinaryBranch> branches = BinaryBranch.FindBinaryBranches(ctx);

        Assert.Single(branches);
        BinaryBranch b = branches[0];
        Assert.Equal([], b.Predecessors);
        Assert.Equal([blocks[1]], b.Successors);
        Assert.Equal(blocks[0], b.Condition);
        Assert.IsType<EmptyNode>(b.True);
        Assert.Equal(blocks[1], b.False);
        Assert.Null(b.Else);
        Assert.Empty(b.True.Predecessors);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(loops);
        TestUtil.VerifyFlowDirections(branches);
    }

    [Fact]
    public void TestSingleIf()
    {
        GMCode code = TestUtil.GetCode(
            """
            :[0]
            push.v self.a
            conv.v.b
            bf [2]

            :[1]
            pushi.e 1
            pop.v.i self.b

            :[2]
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);
        List<Fragment> fragments = Fragment.FindFragments(ctx);
        List<Loop> loops = Loop.FindLoops(ctx);
        Switch.FindSwitchStatements(ctx);
        List<BinaryBranch> branches = BinaryBranch.FindBinaryBranches(ctx);

        Assert.Single(branches);
        BinaryBranch b = branches[0];
        Assert.Equal([], b.Predecessors);
        Assert.Equal([blocks[2]], b.Successors);
        Assert.Equal(blocks[0], b.Condition);
        Assert.Equal(blocks[1], b.True);
        Assert.Equal(blocks[2], b.False);
        Assert.Null(b.Else);
        Assert.Empty(b.True.Predecessors);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(loops);
        TestUtil.VerifyFlowDirections(branches);
        TestUtil.EnsureNoRemainingJumps(ctx);
    }

    [Fact]
    public void TestSingleIfElseEmpty()
    {
        GMCode code = TestUtil.GetCode(
            """
            :[0]
            push.v self.a
            conv.v.b
            bf [2]

            :[1]
            b [2]

            :[2]
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);
        List<Fragment> fragments = Fragment.FindFragments(ctx);
        List<Loop> loops = Loop.FindLoops(ctx);
        Switch.FindSwitchStatements(ctx);
        List<BinaryBranch> branches = BinaryBranch.FindBinaryBranches(ctx);

        Assert.Single(branches);
        BinaryBranch b = branches[0];
        Assert.Equal([], b.Predecessors);
        Assert.Equal([blocks[2]], b.Successors);
        Assert.Equal(blocks[0], b.Condition);
        Assert.Equal(blocks[1], b.True);
        Assert.Empty(blocks[1].Successors);
        Assert.Equal(blocks[2], b.False);
        Assert.IsType<EmptyNode>(b.Else);
        Assert.Empty(b.True.Predecessors);
        Assert.Empty(b.Else.Predecessors);

        Assert.Empty(blocks[1].Instructions);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(loops);
        TestUtil.VerifyFlowDirections(branches);
        TestUtil.EnsureNoRemainingJumps(ctx);
    }

    [Fact]
    public void TestSingleIfElse()
    {
        GMCode code = TestUtil.GetCode(
            """
            :[0]
            push.v self.a
            conv.v.b
            bf [2]

            :[1]
            pushi.e 1
            pop.v.i self.b
            b [3]

            :[2]
            pushi.e 2
            pop.v.i self.c

            :[3]
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);
        List<Fragment> fragments = Fragment.FindFragments(ctx);
        List<Loop> loops = Loop.FindLoops(ctx);
        Switch.FindSwitchStatements(ctx);
        List<BinaryBranch> branches = BinaryBranch.FindBinaryBranches(ctx);

        Assert.Single(branches);
        BinaryBranch b = branches[0];
        Assert.Equal([], b.Predecessors);
        Assert.Equal([blocks[3]], b.Successors);
        Assert.Equal(blocks[0], b.Condition);
        Assert.Equal(blocks[1], b.True);
        Assert.Empty(blocks[1].Successors);
        Assert.Equal(blocks[2], b.False);
        Assert.Equal(blocks[2], b.Else);
        Assert.Empty(b.True.Predecessors);
        Assert.Empty(b.Else!.Predecessors);

        Assert.Equal(2, blocks[1].Instructions.Count);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(loops);
        TestUtil.VerifyFlowDirections(branches);
        TestUtil.EnsureNoRemainingJumps(ctx);
    }

    [Fact]
    public void TestNestedIfEmpty()
    {
        GMCode code = TestUtil.GetCode(
            """
            :[0]
            push.v self.a
            conv.v.b
            bf [2]

            :[1]
            push.v self.b
            conv.v.b
            bf [2]

            :[2]
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);
        List<Fragment> fragments = Fragment.FindFragments(ctx);
        List<Loop> loops = Loop.FindLoops(ctx);
        Switch.FindSwitchStatements(ctx);
        List<BinaryBranch> branches = BinaryBranch.FindBinaryBranches(ctx);

        Assert.Equal(2, branches.Count);
        BinaryBranch b0 = branches[0];
        BinaryBranch b1 = branches[1];

        Assert.Equal([], b0.Predecessors);
        Assert.Equal([blocks[2]], b0.Successors);
        Assert.Equal(blocks[0], b0.Condition);
        Assert.Equal(b1, b0.True);
        Assert.Equal(blocks[2], b0.False);
        Assert.Null(b0.Else);
        Assert.Empty(b0.True.Predecessors);

        Assert.Equal([], b1.Predecessors);
        Assert.Equal([], b1.Successors);
        Assert.Equal(blocks[1], b1.Condition);
        Assert.IsType<EmptyNode>(b1.True);
        Assert.Equal(blocks[2], b1.False);
        Assert.Null(b1.Else);
        Assert.Empty(b1.True.Predecessors);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(loops);
        TestUtil.VerifyFlowDirections(branches);
        TestUtil.EnsureNoRemainingJumps(ctx);
    }

    [Fact]
    public void TestNestedIf()
    {
        GMCode code = TestUtil.GetCode(
            """
            :[0]
            push.v self.a
            conv.v.b
            bf [3]

            :[1]
            push.v self.b
            conv.v.b
            bf [3]

            :[2]
            pushi.e 1
            pop.v.i self.c

            :[3]
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);
        List<Fragment> fragments = Fragment.FindFragments(ctx);
        List<Loop> loops = Loop.FindLoops(ctx);
        Switch.FindSwitchStatements(ctx);
        List<BinaryBranch> branches = BinaryBranch.FindBinaryBranches(ctx);

        Assert.Equal(2, branches.Count);
        BinaryBranch b0 = branches[0];
        BinaryBranch b1 = branches[1];

        Assert.Equal([], b0.Predecessors);
        Assert.Equal([blocks[3]], b0.Successors);
        Assert.Equal(blocks[0], b0.Condition);
        Assert.Equal(b1, b0.True);
        Assert.Equal(blocks[3], b0.False);
        Assert.Null(b0.Else);
        Assert.Empty(b0.True.Predecessors);

        Assert.Equal([], b1.Predecessors);
        Assert.Equal([], b1.Successors);
        Assert.Equal(blocks[1], b1.Condition);
        Assert.Equal(blocks[2], b1.True);
        Assert.Equal(blocks[3], b1.False);
        Assert.Null(b1.Else);
        Assert.Empty(b1.True.Predecessors);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(loops);
        TestUtil.VerifyFlowDirections(branches);
        TestUtil.EnsureNoRemainingJumps(ctx);
    }

    [Fact]
    public void TestDoubleIf()
    {
        GMCode code = TestUtil.GetCode(
            """
            :[0]
            push.v self.a
            conv.v.b
            bf [2]

            :[1]
            pushi.e 1
            pop.v.i self.b

            :[2]
            push.v self.c
            conv.v.b
            bf [4]

            :[3]
            pushi.e 1
            pop.v.i self.d

            :[4]
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);
        List<Fragment> fragments = Fragment.FindFragments(ctx);
        List<Loop> loops = Loop.FindLoops(ctx);
        Switch.FindSwitchStatements(ctx);
        List<BinaryBranch> branches = BinaryBranch.FindBinaryBranches(ctx);

        Assert.Equal(2, branches.Count);
        BinaryBranch b0 = branches[0];
        BinaryBranch b1 = branches[1];

        Assert.Equal([], b0.Predecessors);
        Assert.Equal([b1], b0.Successors);
        Assert.Equal(blocks[0], b0.Condition);
        Assert.Equal(blocks[1], b0.True);
        Assert.Equal(b1, b0.False);
        Assert.Null(b0.Else);
        Assert.Empty(b0.True.Predecessors);

        Assert.Equal([b0], b1.Predecessors);
        Assert.Equal([blocks[4]], b1.Successors);
        Assert.Equal(blocks[2], b1.Condition);
        Assert.Equal(blocks[3], b1.True);
        Assert.Equal(blocks[4], b1.False);
        Assert.Null(b1.Else);
        Assert.Empty(b1.True.Predecessors);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(loops);
        TestUtil.VerifyFlowDirections(branches);
        TestUtil.EnsureNoRemainingJumps(ctx);
    }

    [Fact]
    public void TestNestedIfElse()
    {
        GMCode code = TestUtil.GetCode(
            """
            :[0]
            push.v self.a
            conv.v.b
            bf [5]

            :[1]
            push.v self.b
            conv.v.b
            bf [3]

            :[2]
            pushi.e 1
            pop.v.i self.c
            b [4]

            :[3]
            pushi.e 1
            pop.v.i self.d

            :[4]
            b [8]

            :[5]
            push.v self.e
            conv.v.b
            bf [7]

            :[6]
            pushi.e 1
            pop.v.i self.f
            b [8]

            :[7]
            pushi.e 1
            pop.v.i self.g

            :[8]
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);
        List<Fragment> fragments = Fragment.FindFragments(ctx);
        List<Loop> loops = Loop.FindLoops(ctx);
        Switch.FindSwitchStatements(ctx);
        List<BinaryBranch> branches = BinaryBranch.FindBinaryBranches(ctx);

        Assert.Equal(3, branches.Count);
        BinaryBranch b0 = branches[0];
        BinaryBranch b1 = branches[1];
        BinaryBranch b2 = branches[2];

        Assert.Equal([], b0.Predecessors);
        Assert.Equal([blocks[8]], b0.Successors);
        Assert.Equal(blocks[0], b0.Condition);
        Assert.Equal(b1, b0.True);
        Assert.Equal(b2, b0.False);
        Assert.Equal(b2, b0.Else);
        Assert.Empty(b0.True.Predecessors);
        Assert.Empty(b0.Else!.Predecessors);

        Assert.Equal([], b1.Predecessors);
        Assert.Equal([blocks[4]], b1.Successors);
        Assert.Equal(blocks[1], b1.Condition);
        Assert.Equal(blocks[2], b1.True);
        Assert.Equal(blocks[3], b1.False);
        Assert.Equal(blocks[3], b1.Else);
        Assert.Empty(b1.True.Predecessors);
        Assert.Empty(b1.Else!.Predecessors);

        Assert.Equal([], b2.Predecessors);
        Assert.Equal([], b2.Successors);
        Assert.Equal(blocks[5], b2.Condition);
        Assert.Equal(blocks[6], b2.True);
        Assert.Equal(blocks[7], b2.False);
        Assert.Equal(blocks[7], b2.Else);
        Assert.Empty(b2.True.Predecessors);
        Assert.Empty(b2.Else!.Predecessors);

        Assert.Equal(2, blocks[2].Instructions.Count);
        Assert.Empty(blocks[4].Instructions);
        Assert.Equal(2, blocks[6].Instructions.Count);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(loops);
        TestUtil.VerifyFlowDirections(branches);
        TestUtil.EnsureNoRemainingJumps(ctx);
    }

    [Fact]
    public void TestNestedIfElseEmpty1()
    {
        GMCode code = TestUtil.GetCode(
            """
            :[0]
            push.v self.a
            conv.v.b
            bf [3]

            :[1]
            push.v self.b
            conv.v.b
            bf [3]

            :[2]
            b [3]

            :[3]
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);
        List<Fragment> fragments = Fragment.FindFragments(ctx);
        List<Loop> loops = Loop.FindLoops(ctx);
        Switch.FindSwitchStatements(ctx);
        List<BinaryBranch> branches = BinaryBranch.FindBinaryBranches(ctx);

        Assert.Equal(2, branches.Count);
        BinaryBranch b0 = branches[0];
        BinaryBranch b1 = branches[1];

        Assert.Equal([], b0.Predecessors);
        Assert.Equal([blocks[3]], b0.Successors);
        Assert.Equal(blocks[0], b0.Condition);
        Assert.Equal(b1, b0.True);
        Assert.Equal(blocks[3], b0.False);
        Assert.Null(b0.Else);
        Assert.Empty(b0.True.Predecessors);

        Assert.Equal([], b1.Predecessors);
        Assert.Equal([], b1.Successors);
        Assert.Equal(blocks[1], b1.Condition);
        Assert.Equal(blocks[2], b1.True);
        Assert.Equal(blocks[3], b1.False);
        Assert.IsType<EmptyNode>(b1.Else);
        Assert.Empty(b1.True.Predecessors);
        Assert.Empty(b1.Else.Predecessors);

        Assert.Empty(blocks[2].Instructions);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(loops);
        TestUtil.VerifyFlowDirections(branches);
        TestUtil.EnsureNoRemainingJumps(ctx);
    }

    [Fact]
    public void TestNestedIfElseEmpty2()
    {
        GMCode code = TestUtil.GetCode(
            """
            :[0]
            push.v self.a
            conv.v.b
            bf [4]

            :[1]
            push.v self.b
            conv.v.b
            bf [3]

            :[2]
            b [3]

            :[3]
            b [6]

            :[4]
            push.v self.c
            conv.v.b
            bf [6]

            :[5]
            b [6]

            :[6]
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);
        List<Fragment> fragments = Fragment.FindFragments(ctx);
        List<Loop> loops = Loop.FindLoops(ctx);
        Switch.FindSwitchStatements(ctx);
        List<BinaryBranch> branches = BinaryBranch.FindBinaryBranches(ctx);

        Assert.Equal(3, branches.Count);
        BinaryBranch b0 = branches[0];
        BinaryBranch b1 = branches[1];
        BinaryBranch b2 = branches[2];

        Assert.Equal([], b0.Predecessors);
        Assert.Equal([blocks[6]], b0.Successors);
        Assert.Equal(blocks[0], b0.Condition);
        Assert.Equal(b1, b0.True);
        Assert.Equal(b2, b0.False);
        Assert.Equal(b2, b0.Else);
        Assert.Empty(b0.True.Predecessors);
        Assert.Empty(b0.Else!.Predecessors);

        Assert.Equal([], b1.Predecessors);
        Assert.Equal([blocks[3]], b1.Successors);
        Assert.Empty(blocks[3].Successors);
        Assert.Equal(blocks[1], b1.Condition);
        Assert.Equal(blocks[2], b1.True);
        Assert.Equal(blocks[3], b1.False);
        Assert.IsType<EmptyNode>(b1.Else);
        Assert.Empty(b1.True.Predecessors);
        Assert.Empty(b1.Else!.Predecessors);

        Assert.Equal([], b2.Predecessors);
        Assert.Equal([], b2.Successors);
        Assert.Equal(blocks[4], b2.Condition);
        Assert.Equal(blocks[5], b2.True);
        Assert.Equal(blocks[6], b2.False);
        Assert.IsType<EmptyNode>(b2.Else);
        Assert.Empty(b2.True.Predecessors);
        Assert.Empty(b2.Else!.Predecessors);

        Assert.Empty(blocks[2].Instructions);
        Assert.Empty(blocks[3].Instructions);
        Assert.Empty(blocks[5].Instructions);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(loops);
        TestUtil.VerifyFlowDirections(branches);
        TestUtil.EnsureNoRemainingJumps(ctx);
    }

    [Fact]
    public void TestNestedIfElseEmpty3()
    {
        GMCode code = TestUtil.GetCode(
            """
            :[0]
            push.v self.a
            conv.v.b
            bf [2]

            :[1]
            b [3]

            :[2]
            push.v self.b
            conv.v.b
            bf [3]

            :[3]
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);
        List<Fragment> fragments = Fragment.FindFragments(ctx);
        List<Loop> loops = Loop.FindLoops(ctx);
        Switch.FindSwitchStatements(ctx);
        List<BinaryBranch> branches = BinaryBranch.FindBinaryBranches(ctx);

        Assert.Equal(2, branches.Count);
        BinaryBranch b0 = branches[0];
        BinaryBranch b1 = branches[1];

        Assert.Equal([], b0.Predecessors);
        Assert.Equal([blocks[3]], b0.Successors);
        Assert.Equal(blocks[0], b0.Condition);
        Assert.Equal(blocks[1], b0.True);
        Assert.Empty(blocks[1].Successors);
        Assert.Equal(b1, b0.False);
        Assert.Equal(b1, b0.Else);
        Assert.Empty(b0.True.Predecessors);
        Assert.Empty(b0.Else!.Predecessors);

        Assert.Equal([], b1.Predecessors);
        Assert.Equal([], b1.Successors);
        Assert.Equal(blocks[2], b1.Condition);
        Assert.IsType<EmptyNode>(b1.True);
        Assert.Equal(blocks[3], b1.False);
        Assert.Null(b1.Else);
        Assert.Empty(b1.True.Predecessors);

        Assert.Empty(blocks[1].Instructions);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(loops);
        TestUtil.VerifyFlowDirections(branches);
        TestUtil.EnsureNoRemainingJumps(ctx);
    }

    [Fact]
    public void TestNestedIfElseEmpty4()
    {
        GMCode code = TestUtil.GetCode(
            """
            :[0]
            push.v self.a
            conv.v.b
            bf [4]

            :[1]
            push.v self.b
            conv.v.b
            bf [3]

            :[2]
            pushi.e 1
            pop.v.i self.c

            :[3]
            b [4]

            :[4]
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);
        List<Fragment> fragments = Fragment.FindFragments(ctx);
        List<Loop> loops = Loop.FindLoops(ctx);
        Switch.FindSwitchStatements(ctx);
        List<BinaryBranch> branches = BinaryBranch.FindBinaryBranches(ctx);

        Assert.Equal(2, branches.Count);
        BinaryBranch b0 = branches[0];
        BinaryBranch b1 = branches[1];

        Assert.Equal([], b0.Predecessors);
        Assert.Equal([blocks[4]], b0.Successors);
        Assert.Equal(blocks[0], b0.Condition);
        Assert.Equal(b1, b0.True);
        Assert.Equal(blocks[4], b0.False);
        Assert.IsType<EmptyNode>(b0.Else);
        Assert.Empty(b0.True.Predecessors);
        Assert.Empty(b0.Else.Predecessors);

        Assert.Equal([], b1.Predecessors);
        Assert.Equal([blocks[3]], b1.Successors);
        Assert.Empty(blocks[3].Successors);
        Assert.Equal(blocks[1], b1.Condition);
        Assert.Equal(blocks[2], b1.True);
        Assert.Equal(blocks[3], b1.False);
        Assert.Null(b1.Else);
        Assert.Empty(b1.True.Predecessors);

        Assert.Empty(blocks[3].Instructions);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(loops);
        TestUtil.VerifyFlowDirections(branches);
        TestUtil.EnsureNoRemainingJumps(ctx);
    }

    [Fact]
    public void TestIfExit()
    {
        GMCode code = TestUtil.GetCode(
            """
            :[0]
            push.v self.a
            conv.v.b
            bf [2]

            :[1]
            exit.i

            :[2]
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);
        List<Fragment> fragments = Fragment.FindFragments(ctx);
        List<Loop> loops = Loop.FindLoops(ctx);
        Switch.FindSwitchStatements(ctx);
        List<BinaryBranch> branches = BinaryBranch.FindBinaryBranches(ctx);

        Assert.Single(branches);
        BinaryBranch b0 = branches[0];

        Assert.Equal([], b0.Predecessors);
        Assert.Equal([blocks[2]], b0.Successors);
        Assert.Equal(blocks[0], b0.Condition);
        Assert.Equal(blocks[1], b0.True);
        Assert.Single(blocks[1].Successors);
        Assert.IsType<ExitNode>(blocks[1].Successors[0]);
        Assert.Empty(blocks[1].Successors[0].Successors);
        Assert.Equal(blocks[2], b0.False);
        Assert.Null(b0.Else);
        Assert.Empty(b0.True.Predecessors);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(loops);
        TestUtil.VerifyFlowDirections(branches);
        TestUtil.EnsureNoRemainingJumps(ctx);
    }

    [Fact]
    public void TestIfExitUnreachable()
    {
        GMCode code = TestUtil.GetCode(
            """
            :[0]
            push.v self.a
            conv.v.b
            bf [3]

            :[1]
            exit.i

            :[2]
            exit.i

            :[3]
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);
        List<Fragment> fragments = Fragment.FindFragments(ctx);
        List<Loop> loops = Loop.FindLoops(ctx);
        Switch.FindSwitchStatements(ctx);
        List<BinaryBranch> branches = BinaryBranch.FindBinaryBranches(ctx);

        Assert.Single(branches);
        BinaryBranch b0 = branches[0];

        Assert.Equal([], b0.Predecessors);
        Assert.Equal([blocks[3]], b0.Successors);
        Assert.Equal(blocks[0], b0.Condition);
        Assert.Equal(blocks[1], b0.True);
        Assert.Single(blocks[1].Successors);
        Assert.IsType<ExitNode>(blocks[1].Successors[0]);
        Assert.Equal([blocks[2]], blocks[1].Successors[0].Successors);
        Assert.Single(blocks[2].Successors);
        Assert.IsType<ExitNode>(blocks[2].Successors[0]);
        Assert.Empty(blocks[2].Successors[0].Successors);
        Assert.Equal(blocks[3], b0.False);
        Assert.Null(b0.Else);
        Assert.Empty(b0.True.Predecessors);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(loops);
        TestUtil.VerifyFlowDirections(branches);
        TestUtil.EnsureNoRemainingJumps(ctx);
    }

    [Fact]
    public void TestIfElseExit()
    {
        GMCode code = TestUtil.GetCode(
            """
            :[0]
            push.v self.a
            conv.v.b
            bf [3]

            :[1]
            exit.i

            :[2]
            b [4]

            :[3]
            exit.i

            :[4]
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);
        List<Fragment> fragments = Fragment.FindFragments(ctx);
        List<Loop> loops = Loop.FindLoops(ctx);
        Switch.FindSwitchStatements(ctx);
        List<BinaryBranch> branches = BinaryBranch.FindBinaryBranches(ctx);

        Assert.Single(branches);
        BinaryBranch b0 = branches[0];

        Assert.Equal([], b0.Predecessors);
        Assert.Equal([blocks[4]], b0.Successors);
        Assert.Equal(blocks[0], b0.Condition);
        Assert.Equal(blocks[1], b0.True);
        Assert.Single(blocks[1].Successors);
        Assert.IsType<ExitNode>(blocks[1].Successors[0]);
        Assert.Equal([blocks[2]], blocks[1].Successors[0].Successors);
        Assert.Empty(blocks[2].Successors);
        Assert.Equal(blocks[3], b0.Else);
        Assert.Equal(blocks[3], b0.False);
        Assert.Empty(b0.True.Predecessors);
        Assert.Empty(b0.Else!.Predecessors);

        Assert.Empty(blocks[2].Instructions);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(loops);
        TestUtil.VerifyFlowDirections(branches);
        TestUtil.EnsureNoRemainingJumps(ctx);
    }

    [Fact]
    public void TestIfWith()
    {
        GMCode code = TestUtil.GetCode(
            """
            :[0]
            pushi.e 1
            bf [4]

            :[1]
            pushi.e 2
            pushenv [3]

            :[2]
            pushi.e 3

            :[3]
            popenv [2]

            :[4]
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);
        List<Fragment> fragments = Fragment.FindFragments(ctx);
        List<Loop> loops = Loop.FindLoops(ctx);
        Switch.FindSwitchStatements(ctx);
        List<BinaryBranch> branches = BinaryBranch.FindBinaryBranches(ctx);

        Assert.Single(loops);
        WithLoop loop0 = (WithLoop)loops[0];

        Assert.Single(branches);
        BinaryBranch b0 = branches[0];

        Assert.Equal([], b0.Predecessors);
        Assert.Equal([blocks[4]], b0.Successors);
        Assert.Equal(blocks[0], b0.Condition);
        Assert.Equal(blocks[1], b0.True);
        Assert.Equal([loop0], blocks[1].Successors);
        Assert.Empty(loop0.Successors);
        Assert.Null(b0.Else);
        Assert.Equal(blocks[4], b0.False);
        Assert.Empty(b0.True.Predecessors);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(loops);
        TestUtil.VerifyFlowDirections(branches);
        TestUtil.EnsureNoRemainingJumps(ctx);
    }

    [Fact]
    public void TestIfNestedIfThenExit()
    {
        GMCode code = TestUtil.GetCode(
            """
            :[0]
            push.v self.b
            conv.v.b
            bf [end]

            :[1]
            push.v self.c
            conv.v.b
            bf [2]

            :[2]
            exit.i

            :[end]
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);
        List<Fragment> fragments = Fragment.FindFragments(ctx);
        List<Loop> loops = Loop.FindLoops(ctx);
        Switch.FindSwitchStatements(ctx);
        List<BinaryBranch> branches = BinaryBranch.FindBinaryBranches(ctx);

        Assert.Equal(2, branches.Count);
        BinaryBranch b0 = branches[0];
        BinaryBranch b1 = branches[1];

        Assert.Equal([], b0.Predecessors);
        Assert.Equal([blocks[3]], b0.Successors);
        Assert.Equal(blocks[0], b0.Condition);
        Assert.Equal(b1, b0.True);
        Assert.Equal([blocks[2]], b1.Successors);
        Assert.Single(blocks[2].Successors);
        Assert.IsType<ExitNode>(blocks[2].Successors[0]);
        Assert.Empty(blocks[2].Successors[0].Successors);
        Assert.Null(b0.Else);
        Assert.Equal(blocks[3], b0.False);
        Assert.Empty(b0.True.Predecessors);

        Assert.IsType<EmptyNode>(b1.True);
        Assert.Null(b1.Else);
        Assert.Empty(b1.Predecessors);
        Assert.Empty(b1.True.Predecessors);
        Assert.Empty(b1.True.Successors);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(loops);
        TestUtil.VerifyFlowDirections(branches);
        TestUtil.EnsureNoRemainingJumps(ctx);
    }
}
