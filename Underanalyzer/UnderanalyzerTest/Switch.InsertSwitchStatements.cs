/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using Underanalyzer.Decompiler;
using Underanalyzer.Decompiler.ControlFlow;
using Underanalyzer.Mock;

namespace UnderanalyzerTest;

public class Switch_InsertSwitchStatements
{
    [Fact]
    public void TestEmpty()
    {
        // NOTE: GameMaker itself can't generate this, but modding tools can
        GMCode code = TestUtil.GetCode(
            """
            # switch (a) { }

            :[0]
            push.v self.a
            b [1]

            :[1]
            popz.v
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);
        List<Fragment> fragments = Fragment.FindFragments(ctx);
        List<Loop> loops = Loop.FindLoops(ctx);
        Switch.FindSwitchStatements(ctx);
        List<BinaryBranch> branches = BinaryBranch.FindBinaryBranches(ctx);
        List<Switch> switches = Switch.InsertSwitchStatements(ctx);

        Assert.Single(switches);
        Switch s0 = switches[0];

        Assert.Equal(s0, fragments[0].Children[0]);
        Assert.Equal(blocks[0], s0.Cases);
        Assert.Null(s0.Body);
        Assert.Null(s0.EndCaseDestinations);
        Assert.Empty(blocks[0].Predecessors);
        Assert.Empty(blocks[0].Successors);
        Assert.Single(blocks[0].Instructions);
        Assert.Equal([blocks[1]], s0.Successors);
        Assert.Empty(blocks[1].Instructions);
        Assert.Equal([s0], blocks[1].Predecessors);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(loops);
        TestUtil.VerifyFlowDirections(branches);
        TestUtil.VerifyFlowDirections(switches);
        TestUtil.EnsureNoRemainingJumps(ctx);
    }

    [Fact]
    public void TestDefault()
    {
        GMCode code = TestUtil.GetCode(
            """
            # switch (a) { default: }

            :[0]
            push.v self.a
            b [2]

            :[1]
            b [2]

            :[2]
            popz.v
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);
        List<Fragment> fragments = Fragment.FindFragments(ctx);
        List<Loop> loops = Loop.FindLoops(ctx);
        Switch.FindSwitchStatements(ctx);
        List<BinaryBranch> branches = BinaryBranch.FindBinaryBranches(ctx);
        List<Switch> switches = Switch.InsertSwitchStatements(ctx);

        Assert.Single(switches);
        Switch s0 = switches[0];

        Assert.Equal(blocks[0], s0.Cases);
        Assert.Null(s0.Body);
        Assert.IsType<Switch.CaseDestinationNode>(s0.EndCaseDestinations);
        Assert.Empty(blocks[0].Predecessors);
        Assert.Empty(blocks[0].Successors);
        Assert.Single(blocks[0].Instructions);
        Assert.Empty(blocks[1].Instructions);
        Assert.Empty(blocks[1].Predecessors);
        Assert.Empty(blocks[1].Successors);
        Assert.Equal([blocks[2]], s0.Successors);
        Assert.Empty(blocks[2].Instructions);
        Assert.Equal([s0], blocks[2].Predecessors);

        var destNode = (Switch.CaseDestinationNode)s0.EndCaseDestinations;
        Assert.Empty(destNode.Predecessors);
        Assert.Empty(destNode.Successors);
        Assert.True(destNode.IsDefault);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(loops);
        TestUtil.VerifyFlowDirections(branches);
        TestUtil.VerifyFlowDirections(switches);
        TestUtil.EnsureNoRemainingJumps(ctx);
    }

    [Fact]
    public void TestDefaultBreak()
    {
        GMCode code = TestUtil.GetCode(
            """
            # switch (a) { default: break; }

            :[0]
            push.v self.a
            b [2]

            :[1]
            b [3]

            :[2]
            b [3]

            :[3]
            popz.v
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);
        List<Fragment> fragments = Fragment.FindFragments(ctx);
        List<Loop> loops = Loop.FindLoops(ctx);
        Switch.FindSwitchStatements(ctx);
        List<BinaryBranch> branches = BinaryBranch.FindBinaryBranches(ctx);
        List<Switch> switches = Switch.InsertSwitchStatements(ctx);

        Assert.Single(switches);
        Switch s0 = switches[0];

        Assert.Equal(blocks[0], s0.Cases);
        Assert.IsType<Switch.CaseDestinationNode>(s0.Body);
        Assert.Null(s0.EndCaseDestinations);
        Assert.Empty(blocks[0].Predecessors);
        Assert.Empty(blocks[0].Successors);
        Assert.Single(blocks[0].Instructions);
        Assert.Empty(blocks[1].Instructions);
        Assert.Empty(blocks[1].Predecessors);
        Assert.Empty(blocks[1].Successors);
        Assert.Equal([blocks[3]], s0.Successors);
        Assert.Empty(blocks[3].Instructions);
        Assert.Equal([s0], blocks[3].Predecessors);

        var destNode = (Switch.CaseDestinationNode)s0.Body;
        Assert.Empty(destNode.Predecessors);
        Assert.Equal([blocks[2]], destNode.Successors);
        Assert.Empty(blocks[2].Instructions);
        Assert.IsType<BreakNode>(blocks[2].Successors[0]);
        Assert.Empty(blocks[2].Successors[0].Successors);
        Assert.True(destNode.IsDefault);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(loops);
        TestUtil.VerifyFlowDirections(branches);
        TestUtil.VerifyFlowDirections(switches);
        TestUtil.EnsureNoRemainingJumps(ctx);
    }

    [Fact]
    public void TestDefaultContinue()
    {
        GMCode code = TestUtil.GetCode(
            """
            # while (a) { switch (b) { default: continue; } }

            :[0]
            push.v self.a
            conv.v.b
            bf [7]

            :[1]
            push.v self.b
            b [3]

            :[2]
            b [6]

            :[3]
            b [5]

            :[4]
            b [6]

            :[5]
            popz.v
            b [0]

            :[6]
            popz.v
            b [0]

            :[7]
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);
        List<Fragment> fragments = Fragment.FindFragments(ctx);
        List<Loop> loops = Loop.FindLoops(ctx);
        Switch.FindSwitchStatements(ctx);
        List<BinaryBranch> branches = BinaryBranch.FindBinaryBranches(ctx);
        List<Switch> switches = Switch.InsertSwitchStatements(ctx);

        Assert.Single(switches);
        Switch s0 = switches[0];

        Assert.Equal(loops[0], s0.Parent);
        Assert.Equal(blocks[1], s0.Cases);
        Assert.IsType<Switch.CaseDestinationNode>(s0.Body);
        Assert.Null(s0.EndCaseDestinations);
        Assert.Empty(blocks[1].Predecessors);
        Assert.Empty(blocks[1].Successors);
        Assert.Single(blocks[1].Instructions);
        Assert.Empty(blocks[2].Instructions);
        Assert.Empty(blocks[2].Predecessors);
        Assert.Empty(blocks[2].Successors);
        Assert.Equal([blocks[6]], s0.Successors);
        Assert.Empty(blocks[6].Instructions);
        Assert.Equal([s0], blocks[6].Predecessors);

        var destNode = (Switch.CaseDestinationNode)s0.Body;
        Assert.Empty(destNode.Predecessors);
        Assert.Equal([blocks[3]], destNode.Successors);
        Assert.Empty(blocks[3].Instructions);
        Assert.IsType<ContinueNode>(blocks[3].Successors[0]);
        Assert.Empty(blocks[3].Successors[0].Successors);
        Assert.True(destNode.IsDefault);

        for (int i = 4; i <= 5; i++)
        {
            Assert.Empty(blocks[i].Instructions);
            Assert.Empty(blocks[i].Predecessors);
            Assert.Empty(blocks[i].Successors);
        }

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(loops);
        TestUtil.VerifyFlowDirections(branches);
        TestUtil.VerifyFlowDirections(switches);
        TestUtil.EnsureNoRemainingJumps(ctx);
    }

    [Fact]
    public void TestCase()
    {
        GMCode code = TestUtil.GetCode(
            """
            # switch (a) { case 1: }

            :[0]
            push.v self.a
            dup.v 0
            pushi.e 1
            cmp.i.v EQ
            bt [2]

            :[1]
            b [2]

            :[2]
            popz.v
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);
        List<Fragment> fragments = Fragment.FindFragments(ctx);
        List<Loop> loops = Loop.FindLoops(ctx);
        Switch.FindSwitchStatements(ctx);
        List<BinaryBranch> branches = BinaryBranch.FindBinaryBranches(ctx);
        List<Switch> switches = Switch.InsertSwitchStatements(ctx);

        Assert.Single(switches);
        Switch s0 = switches[0];

        Assert.Equal(blocks[0], s0.Cases);
        Assert.Null(s0.Body);
        Assert.IsType<Switch.CaseDestinationNode>(s0.EndCaseDestinations);
        Assert.Empty(blocks[0].Predecessors);
        Assert.Single(blocks[0].Successors);
        Assert.IsType<Switch.CaseJumpNode>(blocks[0].Successors[0]);
        Assert.Empty(blocks[0].Successors[0].Successors);
        Assert.Equal(3, blocks[0].Instructions.Count);
        Assert.Empty(blocks[1].Instructions);
        Assert.Empty(blocks[1].Predecessors);
        Assert.Empty(blocks[1].Successors);
        Assert.Equal([blocks[2]], s0.Successors);
        Assert.Empty(blocks[2].Instructions);
        Assert.Equal([s0], blocks[2].Predecessors);

        var destNode = (Switch.CaseDestinationNode)s0.EndCaseDestinations;
        Assert.Empty(destNode.Predecessors);
        Assert.Empty(destNode.Successors);
        Assert.False(destNode.IsDefault);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(loops);
        TestUtil.VerifyFlowDirections(branches);
        TestUtil.VerifyFlowDirections(switches);
        TestUtil.EnsureNoRemainingJumps(ctx);
    }

    [Fact]
    public void TestCaseBreak()
    {
        GMCode code = TestUtil.GetCode(
            """
            # switch (a) { case 1: break; }

            :[0]
            push.v self.a
            dup.v 0
            pushi.e 1
            cmp.i.v EQ
            bt [2]

            :[1]
            b [3]

            :[2]
            b [3]

            :[3]
            popz.v
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);
        List<Fragment> fragments = Fragment.FindFragments(ctx);
        List<Loop> loops = Loop.FindLoops(ctx);
        Switch.FindSwitchStatements(ctx);
        List<BinaryBranch> branches = BinaryBranch.FindBinaryBranches(ctx);
        List<Switch> switches = Switch.InsertSwitchStatements(ctx);

        Assert.Single(switches);
        Switch s0 = switches[0];

        Assert.Equal(blocks[0], s0.Cases);
        Assert.IsType<Switch.CaseDestinationNode>(s0.Body);
        Assert.Null(s0.EndCaseDestinations);
        Assert.Empty(blocks[0].Predecessors);
        Assert.Single(blocks[0].Successors);
        Assert.IsType<Switch.CaseJumpNode>(blocks[0].Successors[0]);
        Assert.Empty(blocks[0].Successors[0].Successors);
        Assert.Equal(3, blocks[0].Instructions.Count);
        Assert.Empty(blocks[1].Instructions);
        Assert.Empty(blocks[1].Predecessors);
        Assert.Empty(blocks[1].Successors);
        Assert.Equal([blocks[3]], s0.Successors);
        Assert.Empty(blocks[3].Instructions);
        Assert.Equal([s0], blocks[3].Predecessors);

        var destNode = (Switch.CaseDestinationNode)s0.Body;
        Assert.Empty(destNode.Predecessors);
        Assert.Equal([blocks[2]], destNode.Successors);
        Assert.Empty(blocks[2].Instructions);
        Assert.Single(blocks[2].Successors);
        Assert.IsType<BreakNode>(blocks[2].Successors[0]);
        Assert.Empty(blocks[2].Successors[0].Successors);
        Assert.False(destNode.IsDefault);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(loops);
        TestUtil.VerifyFlowDirections(branches);
        TestUtil.VerifyFlowDirections(switches);
        TestUtil.EnsureNoRemainingJumps(ctx);
    }

    [Fact]
    public void TestCaseContinue()
    {
        GMCode code = TestUtil.GetCode(
            """
            # switch (a) { case 1: continue; }

            :[0]
            push.v self.a
            conv.v.b
            bf [7]

            :[1]
            push.v self.b
            dup.v 0
            pushi.e 1
            cmp.i.v EQ
            bt [3]

            :[2]
            b [6]

            :[3]
            b [5]

            :[4]
            b [6]

            :[5]
            popz.v
            b [0]

            :[6]
            popz.v
            b [0]

            :[7]
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);
        List<Fragment> fragments = Fragment.FindFragments(ctx);
        List<Loop> loops = Loop.FindLoops(ctx);
        Switch.FindSwitchStatements(ctx);
        List<BinaryBranch> branches = BinaryBranch.FindBinaryBranches(ctx);
        List<Switch> switches = Switch.InsertSwitchStatements(ctx);

        Assert.Single(switches);
        Switch s0 = switches[0];

        Assert.Equal(loops[0], s0.Parent);
        Assert.Equal(blocks[1], s0.Cases);
        Assert.IsType<Switch.CaseDestinationNode>(s0.Body);
        Assert.Null(s0.EndCaseDestinations);
        Assert.Empty(blocks[1].Predecessors);
        Assert.Single(blocks[1].Successors);
        Assert.IsType<Switch.CaseJumpNode>(blocks[1].Successors[0]);
        Assert.Empty(blocks[1].Successors[0].Successors);
        Assert.Equal(3, blocks[1].Instructions.Count);
        Assert.Empty(blocks[2].Instructions);
        Assert.Empty(blocks[2].Predecessors);
        Assert.Empty(blocks[2].Successors);
        Assert.Equal([blocks[6]], s0.Successors);
        Assert.Empty(blocks[6].Instructions);
        Assert.Equal([s0], blocks[6].Predecessors);

        var destNode = (Switch.CaseDestinationNode)s0.Body;
        Assert.Empty(destNode.Predecessors);
        Assert.Equal([blocks[3]], destNode.Successors);
        Assert.Empty(blocks[3].Instructions);
        Assert.IsType<ContinueNode>(blocks[3].Successors[0]);
        Assert.Empty(blocks[3].Successors[0].Successors);
        Assert.False(destNode.IsDefault);

        for (int i = 4; i <= 5; i++)
        {
            Assert.Empty(blocks[i].Instructions);
            Assert.Empty(blocks[i].Predecessors);
            Assert.Empty(blocks[i].Successors);
        }

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(loops);
        TestUtil.VerifyFlowDirections(branches);
        TestUtil.VerifyFlowDirections(switches);
        TestUtil.EnsureNoRemainingJumps(ctx);
    }

    [Fact]
    public void Test2CasesBreaks()
    {
        GMCode code = TestUtil.GetCode(
            """
            # switch (a)
            # {
            #     case 1:
            #         break;
            #     case 2:
            #         break;
            # }

            :[0]
            push.v self.a
            dup.v 0
            pushi.e 1
            cmp.i.v EQ
            bt [3]

            :[1]
            dup.v 0
            pushi.e 2
            cmp.i.v EQ
            bt [4]

            :[2]
            b [5]

            :[3]
            b [5]

            :[4]
            b [5]

            :[5]
            popz.v
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);
        List<Fragment> fragments = Fragment.FindFragments(ctx);
        List<Loop> loops = Loop.FindLoops(ctx);
        Switch.FindSwitchStatements(ctx);
        List<BinaryBranch> branches = BinaryBranch.FindBinaryBranches(ctx);
        List<Switch> switches = Switch.InsertSwitchStatements(ctx);

        Assert.Single(switches);
        Switch s0 = switches[0];

        Assert.Equal(blocks[0], s0.Cases);
        Assert.IsType<Switch.CaseDestinationNode>(s0.Body);
        Assert.Null(s0.EndCaseDestinations);
        Assert.Empty(blocks[0].Predecessors);
        Assert.Single(blocks[0].Successors);
        Assert.IsType<Switch.CaseJumpNode>(blocks[0].Successors[0]);
        Assert.Equal([blocks[1]], blocks[0].Successors[0].Successors);
        Assert.Equal(3, blocks[0].Instructions.Count);
        Assert.Single(blocks[1].Successors);
        Assert.IsType<Switch.CaseJumpNode>(blocks[1].Successors[0]);
        Assert.Empty(blocks[1].Successors[0].Successors);
        Assert.Equal(2, blocks[1].Instructions.Count);

        Assert.Empty(blocks[2].Instructions);
        Assert.Empty(blocks[2].Predecessors);
        Assert.Empty(blocks[2].Successors);
        Assert.Equal([blocks[5]], s0.Successors);
        Assert.Empty(blocks[5].Instructions);
        Assert.Equal([s0], blocks[5].Predecessors);

        var destNode = (Switch.CaseDestinationNode)s0.Body;
        Assert.Empty(destNode.Predecessors);
        Assert.Equal([blocks[3]], destNode.Successors);
        Assert.Empty(blocks[3].Instructions);
        Assert.Single(blocks[3].Successors);
        Assert.IsType<BreakNode>(blocks[3].Successors[0]);
        Assert.Single(blocks[3].Successors[0].Successors);
        Assert.IsType<Switch.CaseDestinationNode>(blocks[3].Successors[0].Successors[0]);
        Assert.False(destNode.IsDefault);

        destNode = (Switch.CaseDestinationNode)blocks[3].Successors[0].Successors[0];
        Assert.Equal([blocks[4]], destNode.Successors);
        Assert.Empty(blocks[4].Instructions);
        Assert.Single(blocks[4].Successors);
        Assert.IsType<BreakNode>(blocks[4].Successors[0]);
        Assert.Empty(blocks[4].Successors[0].Successors);
        Assert.False(destNode.IsDefault);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(loops);
        TestUtil.VerifyFlowDirections(branches);
        TestUtil.VerifyFlowDirections(switches);
        TestUtil.EnsureNoRemainingJumps(ctx);
    }

    [Fact]
    public void Test3CasesBreaks()
    {
        GMCode code = TestUtil.GetCode(
            """
            # switch (a)
            # {
            #     case 1:
            #         break;
            #     case 2:
            #         break;
            #     case 3:
            #         break;
            # }

            :[0]
            push.v self.a
            dup.v 0
            pushi.e 1
            cmp.i.v EQ
            bt [4]

            :[1]
            dup.v 0
            pushi.e 2
            cmp.i.v EQ
            bt [5]

            :[2]
            dup.v 0
            pushi.e 3
            cmp.i.v EQ
            bt [6]

            :[3]
            b [7]

            :[4]
            b [7]

            :[5]
            b [7]

            :[6]
            b [7]

            :[7]
            popz.v
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);
        List<Fragment> fragments = Fragment.FindFragments(ctx);
        List<Loop> loops = Loop.FindLoops(ctx);
        Switch.FindSwitchStatements(ctx);
        List<BinaryBranch> branches = BinaryBranch.FindBinaryBranches(ctx);
        List<Switch> switches = Switch.InsertSwitchStatements(ctx);

        Assert.Single(switches);
        Switch s0 = switches[0];

        Assert.Equal(blocks[0], s0.Cases);
        Assert.IsType<Switch.CaseDestinationNode>(s0.Body);
        Assert.Null(s0.EndCaseDestinations);
        Assert.Empty(blocks[0].Predecessors);
        Assert.Single(blocks[0].Successors);
        Assert.IsType<Switch.CaseJumpNode>(blocks[0].Successors[0]);
        Assert.Equal([blocks[1]], blocks[0].Successors[0].Successors);
        Assert.Equal(3, blocks[0].Instructions.Count);
        Assert.Single(blocks[1].Successors);
        Assert.IsType<Switch.CaseJumpNode>(blocks[1].Successors[0]);
        Assert.Equal([blocks[2]], blocks[1].Successors[0].Successors);
        Assert.Equal(2, blocks[1].Instructions.Count);
        Assert.Single(blocks[2].Successors);
        Assert.IsType<Switch.CaseJumpNode>(blocks[2].Successors[0]);
        Assert.Empty(blocks[2].Successors[0].Successors);
        Assert.Equal(2, blocks[2].Instructions.Count);

        Assert.Empty(blocks[3].Instructions);
        Assert.Empty(blocks[3].Predecessors);
        Assert.Empty(blocks[3].Successors);
        Assert.Equal([blocks[7]], s0.Successors);
        Assert.Empty(blocks[7].Instructions);
        Assert.Equal([s0], blocks[7].Predecessors);

        var destNode = (Switch.CaseDestinationNode)s0.Body;
        Assert.Empty(destNode.Predecessors);
        Assert.Equal([blocks[4]], destNode.Successors);
        Assert.Empty(blocks[4].Instructions);
        Assert.Single(blocks[4].Successors);
        Assert.IsType<BreakNode>(blocks[4].Successors[0]);
        Assert.Single(blocks[4].Successors[0].Successors);
        Assert.IsType<Switch.CaseDestinationNode>(blocks[4].Successors[0].Successors[0]);
        Assert.False(destNode.IsDefault);

        destNode = (Switch.CaseDestinationNode)blocks[4].Successors[0].Successors[0];
        Assert.Equal([blocks[5]], destNode.Successors);
        Assert.Empty(blocks[5].Instructions);
        Assert.Single(blocks[5].Successors);
        Assert.IsType<BreakNode>(blocks[5].Successors[0]);
        Assert.IsType<Switch.CaseDestinationNode>(blocks[5].Successors[0].Successors[0]);
        Assert.False(destNode.IsDefault);

        destNode = (Switch.CaseDestinationNode)blocks[5].Successors[0].Successors[0];
        Assert.Equal([blocks[6]], destNode.Successors);
        Assert.Empty(blocks[6].Instructions);
        Assert.Single(blocks[6].Successors);
        Assert.IsType<BreakNode>(blocks[6].Successors[0]);
        Assert.Empty(blocks[6].Successors[0].Successors);
        Assert.False(destNode.IsDefault);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(loops);
        TestUtil.VerifyFlowDirections(branches);
        TestUtil.VerifyFlowDirections(switches);
        TestUtil.EnsureNoRemainingJumps(ctx);
    }

    [Fact]
    public void TestCaseBreakCase()
    {
        GMCode code = TestUtil.GetCode(
            """
            # switch (a)
            # {
            #     case 1:
            #         break;
            #     case 2:
            # }

            :[0]
            push.v self.a
            dup.v 0
            pushi.e 1
            cmp.i.v EQ
            bt [3]

            :[1]
            dup.v 0
            pushi.e 2
            cmp.i.v EQ
            bt [4]

            :[2]
            b [4]

            :[3]
            b [4]

            :[4]
            popz.v
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);
        List<Fragment> fragments = Fragment.FindFragments(ctx);
        List<Loop> loops = Loop.FindLoops(ctx);
        Switch.FindSwitchStatements(ctx);
        List<BinaryBranch> branches = BinaryBranch.FindBinaryBranches(ctx);
        List<Switch> switches = Switch.InsertSwitchStatements(ctx);

        Assert.Single(switches);
        Switch s0 = switches[0];

        Assert.Equal(blocks[0], s0.Cases);
        Assert.IsType<Switch.CaseDestinationNode>(s0.Body);
        Assert.IsType<Switch.CaseDestinationNode>(s0.EndCaseDestinations);
        Assert.Empty(blocks[0].Predecessors);
        Assert.Single(blocks[0].Successors);
        Assert.IsType<Switch.CaseJumpNode>(blocks[0].Successors[0]);
        Assert.Equal([blocks[1]], blocks[0].Successors[0].Successors);
        Assert.Equal(3, blocks[0].Instructions.Count);
        Assert.Single(blocks[1].Successors);
        Assert.IsType<Switch.CaseJumpNode>(blocks[1].Successors[0]);
        Assert.Empty(blocks[1].Successors[0].Successors);
        Assert.Equal(2, blocks[1].Instructions.Count);

        Assert.Empty(blocks[2].Instructions);
        Assert.Empty(blocks[2].Predecessors);
        Assert.Empty(blocks[2].Successors);
        Assert.Equal([blocks[4]], s0.Successors);
        Assert.Empty(blocks[4].Instructions);
        Assert.Equal([s0], blocks[4].Predecessors);

        var destNode = (Switch.CaseDestinationNode)s0.Body;
        Assert.Empty(destNode.Predecessors);
        Assert.Equal([blocks[3]], destNode.Successors);
        Assert.Empty(blocks[3].Instructions);
        Assert.Single(blocks[3].Successors);
        Assert.IsType<BreakNode>(blocks[3].Successors[0]);
        Assert.Empty(blocks[3].Successors[0].Successors);
        Assert.False(destNode.IsDefault);

        destNode = (Switch.CaseDestinationNode)s0.EndCaseDestinations;
        Assert.Empty(destNode.Predecessors);
        Assert.Empty(destNode.Successors);
        Assert.False(destNode.IsDefault);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(loops);
        TestUtil.VerifyFlowDirections(branches);
        TestUtil.VerifyFlowDirections(switches);
        TestUtil.EnsureNoRemainingJumps(ctx);
    }

    [Fact]
    public void TestCaseBreakDefault()
    {
        GMCode code = TestUtil.GetCode(
            """
            # switch (a)
            # {
            #     case 1:
            #         break;
            #     default:
            # }

            :[0]
            push.v self.a
            dup.v 0
            pushi.e 1
            cmp.i.v EQ
            bt [3]

            :[1]
            b [4]

            :[2]
            b [4]

            :[3]
            b [4]

            :[4]
            popz.v
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);
        List<Fragment> fragments = Fragment.FindFragments(ctx);
        List<Loop> loops = Loop.FindLoops(ctx);
        Switch.FindSwitchStatements(ctx);
        List<BinaryBranch> branches = BinaryBranch.FindBinaryBranches(ctx);
        List<Switch> switches = Switch.InsertSwitchStatements(ctx);

        Assert.Single(switches);
        Switch s0 = switches[0];

        Assert.Equal(blocks[0], s0.Cases);
        Assert.IsType<Switch.CaseDestinationNode>(s0.Body);
        Assert.IsType<Switch.CaseDestinationNode>(s0.EndCaseDestinations);
        Assert.Empty(blocks[0].Predecessors);
        Assert.Single(blocks[0].Successors);
        Assert.IsType<Switch.CaseJumpNode>(blocks[0].Successors[0]);
        Assert.Empty(blocks[0].Successors[0].Successors);
        Assert.Equal(3, blocks[0].Instructions.Count);
        Assert.Empty(blocks[1].Instructions);
        Assert.Empty(blocks[1].Predecessors);
        Assert.Empty(blocks[1].Successors);

        Assert.Empty(blocks[2].Instructions);
        Assert.Empty(blocks[2].Predecessors);
        Assert.Empty(blocks[2].Successors);
        Assert.Equal([blocks[4]], s0.Successors);
        Assert.Empty(blocks[4].Instructions);
        Assert.Equal([s0], blocks[4].Predecessors);

        var destNode = (Switch.CaseDestinationNode)s0.Body;
        Assert.Empty(destNode.Predecessors);
        Assert.Equal([blocks[3]], destNode.Successors);
        Assert.Empty(blocks[3].Instructions);
        Assert.Single(blocks[3].Successors);
        Assert.IsType<BreakNode>(blocks[3].Successors[0]);
        Assert.Empty(blocks[3].Successors[0].Successors);
        Assert.False(destNode.IsDefault);

        destNode = (Switch.CaseDestinationNode)s0.EndCaseDestinations;
        Assert.Empty(destNode.Predecessors);
        Assert.Empty(destNode.Successors);
        Assert.True(destNode.IsDefault);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(loops);
        TestUtil.VerifyFlowDirections(branches);
        TestUtil.VerifyFlowDirections(switches);
        TestUtil.EnsureNoRemainingJumps(ctx);
    }

    [Fact]
    public void TestCasesDefaultBreaks()
    {
        GMCode code = TestUtil.GetCode(
            """
            # switch (a)
            # {
            #     case 1:
            #         break;
            #     case 2:
            #         break;
            #     default:
            #         break;
            # }

            :[0]
            push.v self.a
            dup.v 0
            pushi.e 1
            cmp.i.v EQ
            bt [4]

            :[1]
            dup.v 0
            pushi.e 2
            cmp.i.v EQ
            bt [5]

            :[2]
            b [6]

            :[3]
            b [7]

            :[4]
            b [7]

            :[5]
            b [7]

            :[6]
            b [7]

            :[7]
            popz.v
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);
        List<Fragment> fragments = Fragment.FindFragments(ctx);
        List<Loop> loops = Loop.FindLoops(ctx);
        Switch.FindSwitchStatements(ctx);
        List<BinaryBranch> branches = BinaryBranch.FindBinaryBranches(ctx);
        List<Switch> switches = Switch.InsertSwitchStatements(ctx);

        Assert.Single(switches);
        Switch s0 = switches[0];

        Assert.Equal(blocks[0], s0.Cases);
        Assert.IsType<Switch.CaseDestinationNode>(s0.Body);
        Assert.Null(s0.EndCaseDestinations);
        Assert.Empty(blocks[0].Predecessors);
        Assert.Single(blocks[0].Successors);
        Assert.IsType<Switch.CaseJumpNode>(blocks[0].Successors[0]);
        Assert.Equal([blocks[1]], blocks[0].Successors[0].Successors);
        Assert.Equal(3, blocks[0].Instructions.Count);
        Assert.Single(blocks[1].Successors);
        Assert.IsType<Switch.CaseJumpNode>(blocks[1].Successors[0]);
        Assert.Empty(blocks[1].Successors[0].Successors);
        Assert.Equal(2, blocks[1].Instructions.Count);
        Assert.Empty(blocks[2].Instructions);
        Assert.Empty(blocks[2].Predecessors);
        Assert.Empty(blocks[2].Successors);

        Assert.Empty(blocks[3].Instructions);
        Assert.Empty(blocks[3].Predecessors);
        Assert.Empty(blocks[3].Successors);
        Assert.Equal([blocks[7]], s0.Successors);
        Assert.Empty(blocks[7].Instructions);
        Assert.Equal([s0], blocks[7].Predecessors);

        var destNode = (Switch.CaseDestinationNode)s0.Body;
        Assert.Empty(destNode.Predecessors);
        Assert.Equal([blocks[4]], destNode.Successors);
        Assert.Empty(blocks[4].Instructions);
        Assert.Single(blocks[4].Successors);
        Assert.IsType<BreakNode>(blocks[4].Successors[0]);
        Assert.Single(blocks[4].Successors[0].Successors);
        Assert.IsType<Switch.CaseDestinationNode>(blocks[4].Successors[0].Successors[0]);
        Assert.False(destNode.IsDefault);

        destNode = (Switch.CaseDestinationNode)blocks[4].Successors[0].Successors[0];
        Assert.Equal([blocks[5]], destNode.Successors);
        Assert.Empty(blocks[5].Instructions);
        Assert.Single(blocks[5].Successors);
        Assert.IsType<BreakNode>(blocks[5].Successors[0]);
        Assert.IsType<Switch.CaseDestinationNode>(blocks[5].Successors[0].Successors[0]);
        Assert.False(destNode.IsDefault);

        destNode = (Switch.CaseDestinationNode)blocks[5].Successors[0].Successors[0];
        Assert.Equal([blocks[6]], destNode.Successors);
        Assert.Empty(blocks[6].Instructions);
        Assert.Single(blocks[6].Successors);
        Assert.IsType<BreakNode>(blocks[6].Successors[0]);
        Assert.Empty(blocks[6].Successors[0].Successors);
        Assert.True(destNode.IsDefault);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(loops);
        TestUtil.VerifyFlowDirections(branches);
        TestUtil.VerifyFlowDirections(switches);
        TestUtil.EnsureNoRemainingJumps(ctx);
    }

    [Fact]
    public void TestCasesDefaultBreaks2()
    {
        GMCode code = TestUtil.GetCode(
            """
            # switch (a)
            # {
            #     case 1:
            #         break;
            #     default:
            #         break;
            #     case 2:
            #         break;
            # }

            :[0]
            push.v self.a
            dup.v 0
            pushi.e 1
            cmp.i.v EQ
            bt [4]

            :[1]
            dup.v 0
            pushi.e 2
            cmp.i.v EQ
            bt [6]

            :[2]
            b [5]

            :[3]
            b [7]

            :[4]
            b [7]

            :[5]
            b [7]

            :[6]
            b [7]

            :[7]
            popz.v
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);
        List<Fragment> fragments = Fragment.FindFragments(ctx);
        List<Loop> loops = Loop.FindLoops(ctx);
        Switch.FindSwitchStatements(ctx);
        List<BinaryBranch> branches = BinaryBranch.FindBinaryBranches(ctx);
        List<Switch> switches = Switch.InsertSwitchStatements(ctx);

        Assert.Single(switches);
        Switch s0 = switches[0];

        Assert.Equal(blocks[0], s0.Cases);
        Assert.IsType<Switch.CaseDestinationNode>(s0.Body);
        Assert.Null(s0.EndCaseDestinations);
        Assert.Empty(blocks[0].Predecessors);
        Assert.Single(blocks[0].Successors);
        Assert.IsType<Switch.CaseJumpNode>(blocks[0].Successors[0]);
        Assert.Equal([blocks[1]], blocks[0].Successors[0].Successors);
        Assert.Equal(3, blocks[0].Instructions.Count);
        Assert.Single(blocks[1].Successors);
        Assert.IsType<Switch.CaseJumpNode>(blocks[1].Successors[0]);
        Assert.Empty(blocks[1].Successors[0].Successors);
        Assert.Equal(2, blocks[1].Instructions.Count);
        Assert.Empty(blocks[2].Instructions);
        Assert.Empty(blocks[2].Predecessors);
        Assert.Empty(blocks[2].Successors);

        Assert.Empty(blocks[3].Instructions);
        Assert.Empty(blocks[3].Predecessors);
        Assert.Empty(blocks[3].Successors);
        Assert.Equal([blocks[7]], s0.Successors);
        Assert.Empty(blocks[7].Instructions);
        Assert.Equal([s0], blocks[7].Predecessors);

        var destNode = (Switch.CaseDestinationNode)s0.Body;
        Assert.Empty(destNode.Predecessors);
        Assert.Equal([blocks[4]], destNode.Successors);
        Assert.Empty(blocks[4].Instructions);
        Assert.Single(blocks[4].Successors);
        Assert.IsType<BreakNode>(blocks[4].Successors[0]);
        Assert.Single(blocks[4].Successors[0].Successors);
        Assert.IsType<Switch.CaseDestinationNode>(blocks[4].Successors[0].Successors[0]);
        Assert.False(destNode.IsDefault);

        destNode = (Switch.CaseDestinationNode)blocks[4].Successors[0].Successors[0];
        Assert.Equal([blocks[5]], destNode.Successors);
        Assert.Empty(blocks[5].Instructions);
        Assert.Single(blocks[5].Successors);
        Assert.IsType<BreakNode>(blocks[5].Successors[0]);
        Assert.IsType<Switch.CaseDestinationNode>(blocks[5].Successors[0].Successors[0]);
        Assert.True(destNode.IsDefault);

        destNode = (Switch.CaseDestinationNode)blocks[5].Successors[0].Successors[0];
        Assert.Equal([blocks[6]], destNode.Successors);
        Assert.Empty(blocks[6].Instructions);
        Assert.Single(blocks[6].Successors);
        Assert.IsType<BreakNode>(blocks[6].Successors[0]);
        Assert.Empty(blocks[6].Successors[0].Successors);
        Assert.False(destNode.IsDefault);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(loops);
        TestUtil.VerifyFlowDirections(branches);
        TestUtil.VerifyFlowDirections(switches);
        TestUtil.EnsureNoRemainingJumps(ctx);
    }

    [Fact]
    public void TestCasesDefaultBreaks3()
    {
        GMCode code = TestUtil.GetCode(
            """
            # switch (a)
            # {
            #     default:
            #         break;
            #     case 1:
            #         break;
            #     case 2:
            #         break;
            # }

            :[0]
            push.v self.a
            dup.v 0
            pushi.e 1
            cmp.i.v EQ
            bt [5]

            :[1]
            dup.v 0
            pushi.e 2
            cmp.i.v EQ
            bt [6]

            :[2]
            b [4]

            :[3]
            b [7]

            :[4]
            b [7]

            :[5]
            b [7]

            :[6]
            b [7]

            :[7]
            popz.v
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);
        List<Fragment> fragments = Fragment.FindFragments(ctx);
        List<Loop> loops = Loop.FindLoops(ctx);
        Switch.FindSwitchStatements(ctx);
        List<BinaryBranch> branches = BinaryBranch.FindBinaryBranches(ctx);
        List<Switch> switches = Switch.InsertSwitchStatements(ctx);

        Assert.Single(switches);
        Switch s0 = switches[0];

        Assert.Equal(blocks[0], s0.Cases);
        Assert.IsType<Switch.CaseDestinationNode>(s0.Body);
        Assert.Null(s0.EndCaseDestinations);
        Assert.Empty(blocks[0].Predecessors);
        Assert.Single(blocks[0].Successors);
        Assert.IsType<Switch.CaseJumpNode>(blocks[0].Successors[0]);
        Assert.Equal([blocks[1]], blocks[0].Successors[0].Successors);
        Assert.Equal(3, blocks[0].Instructions.Count);
        Assert.Single(blocks[1].Successors);
        Assert.IsType<Switch.CaseJumpNode>(blocks[1].Successors[0]);
        Assert.Empty(blocks[1].Successors[0].Successors);
        Assert.Equal(2, blocks[1].Instructions.Count);
        Assert.Empty(blocks[2].Instructions);
        Assert.Empty(blocks[2].Predecessors);
        Assert.Empty(blocks[2].Successors);

        Assert.Empty(blocks[3].Instructions);
        Assert.Empty(blocks[3].Predecessors);
        Assert.Empty(blocks[3].Successors);
        Assert.Equal([blocks[7]], s0.Successors);
        Assert.Empty(blocks[7].Instructions);
        Assert.Equal([s0], blocks[7].Predecessors);

        var destNode = (Switch.CaseDestinationNode)s0.Body;
        Assert.Empty(destNode.Predecessors);
        Assert.Equal([blocks[4]], destNode.Successors);
        Assert.Empty(blocks[4].Instructions);
        Assert.Single(blocks[4].Successors);
        Assert.IsType<BreakNode>(blocks[4].Successors[0]);
        Assert.Single(blocks[4].Successors[0].Successors);
        Assert.IsType<Switch.CaseDestinationNode>(blocks[4].Successors[0].Successors[0]);
        Assert.True(destNode.IsDefault);

        destNode = (Switch.CaseDestinationNode)blocks[4].Successors[0].Successors[0];
        Assert.Equal([blocks[5]], destNode.Successors);
        Assert.Empty(blocks[5].Instructions);
        Assert.Single(blocks[5].Successors);
        Assert.IsType<BreakNode>(blocks[5].Successors[0]);
        Assert.IsType<Switch.CaseDestinationNode>(blocks[5].Successors[0].Successors[0]);
        Assert.False(destNode.IsDefault);

        destNode = (Switch.CaseDestinationNode)blocks[5].Successors[0].Successors[0];
        Assert.Equal([blocks[6]], destNode.Successors);
        Assert.Empty(blocks[6].Instructions);
        Assert.Single(blocks[6].Successors);
        Assert.IsType<BreakNode>(blocks[6].Successors[0]);
        Assert.Empty(blocks[6].Successors[0].Successors);
        Assert.False(destNode.IsDefault);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(loops);
        TestUtil.VerifyFlowDirections(branches);
        TestUtil.VerifyFlowDirections(switches);
        TestUtil.EnsureNoRemainingJumps(ctx);
    }

    [Fact]
    public void TestMultiCases()
    {
        GMCode code = TestUtil.GetCode(
            """
            # switch (a)
            # {
            #     case 1:
            #     case 2:
            # }

            :[0]
            push.v self.a
            dup.v 0
            pushi.e 1
            cmp.i.v EQ
            bt [3]

            :[1]
            dup.v 0
            pushi.e 2
            cmp.i.v EQ
            bt [3]

            :[2]
            b [3]

            :[3]
            popz.v
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);
        List<Fragment> fragments = Fragment.FindFragments(ctx);
        List<Loop> loops = Loop.FindLoops(ctx);
        Switch.FindSwitchStatements(ctx);
        List<BinaryBranch> branches = BinaryBranch.FindBinaryBranches(ctx);
        List<Switch> switches = Switch.InsertSwitchStatements(ctx);

        Assert.Single(switches);
        Switch s0 = switches[0];

        Assert.Equal(blocks[0], s0.Cases);
        Assert.Null(s0.Body);
        Assert.IsType<Switch.CaseDestinationNode>(s0.EndCaseDestinations);
        Assert.Empty(blocks[0].Predecessors);
        Assert.Single(blocks[0].Successors);
        Assert.IsType<Switch.CaseJumpNode>(blocks[0].Successors[0]);
        Assert.Equal([blocks[1]], blocks[0].Successors[0].Successors);
        Assert.Equal(3, blocks[0].Instructions.Count);
        Assert.Single(blocks[1].Successors);
        Assert.IsType<Switch.CaseJumpNode>(blocks[1].Successors[0]);
        Assert.Empty(blocks[1].Successors[0].Successors);
        Assert.Equal(2, blocks[1].Instructions.Count);

        Assert.Empty(blocks[2].Instructions);
        Assert.Empty(blocks[2].Predecessors);
        Assert.Empty(blocks[2].Successors);
        Assert.Equal([blocks[3]], s0.Successors);
        Assert.Empty(blocks[3].Instructions);
        Assert.Equal([s0], blocks[3].Predecessors);

        var destNode = (Switch.CaseDestinationNode)s0.EndCaseDestinations;
        Assert.Empty(destNode.Predecessors);
        Assert.Single(destNode.Successors);
        Assert.IsType<Switch.CaseDestinationNode>(destNode.Successors[0]);
        Assert.False(destNode.IsDefault);

        destNode = (Switch.CaseDestinationNode)destNode.Successors[0];
        Assert.Empty(destNode.Successors);
        Assert.False(destNode.IsDefault);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(loops);
        TestUtil.VerifyFlowDirections(branches);
        TestUtil.VerifyFlowDirections(switches);
        TestUtil.EnsureNoRemainingJumps(ctx);
    }

    [Fact]
    public void TestMultiCasesBreak()
    {
        GMCode code = TestUtil.GetCode(
            """
            # switch (a)
            # {
            #     case 1:
            #     case 2:
            #         break;
            # }

            :[0]
            push.v self.a
            dup.v 0
            pushi.e 1
            cmp.i.v EQ
            bt [3]

            :[1]
            dup.v 0
            pushi.e 2
            cmp.i.v EQ
            bt [3]

            :[2]
            b [4]

            :[3]
            b [4]

            :[4]
            popz.v
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);
        List<Fragment> fragments = Fragment.FindFragments(ctx);
        List<Loop> loops = Loop.FindLoops(ctx);
        Switch.FindSwitchStatements(ctx);
        List<BinaryBranch> branches = BinaryBranch.FindBinaryBranches(ctx);
        List<Switch> switches = Switch.InsertSwitchStatements(ctx);

        Assert.Single(switches);
        Switch s0 = switches[0];

        Assert.Equal(blocks[0], s0.Cases);
        Assert.IsType<Switch.CaseDestinationNode>(s0.Body);
        Assert.Null(s0.EndCaseDestinations);
        Assert.Empty(blocks[0].Predecessors);
        Assert.Single(blocks[0].Successors);
        Assert.IsType<Switch.CaseJumpNode>(blocks[0].Successors[0]);
        Assert.Equal([blocks[1]], blocks[0].Successors[0].Successors);
        Assert.Equal(3, blocks[0].Instructions.Count);
        Assert.Single(blocks[1].Successors);
        Assert.IsType<Switch.CaseJumpNode>(blocks[1].Successors[0]);
        Assert.Empty(blocks[1].Successors[0].Successors);
        Assert.Equal(2, blocks[1].Instructions.Count);
        Assert.Empty(blocks[2].Instructions);
        Assert.Empty(blocks[2].Predecessors);
        Assert.Empty(blocks[2].Successors);

        Assert.Equal([blocks[4]], s0.Successors);
        Assert.Empty(blocks[4].Instructions);
        Assert.Equal([s0], blocks[4].Predecessors);

        var destNode = (Switch.CaseDestinationNode)s0.Body;
        Assert.Empty(destNode.Predecessors);
        Assert.Single(destNode.Successors);
        Assert.IsType<Switch.CaseDestinationNode>(destNode.Successors[0]);
        Assert.False(destNode.IsDefault);

        destNode = (Switch.CaseDestinationNode)destNode.Successors[0];
        Assert.Equal([blocks[3]], destNode.Successors);
        Assert.Empty(blocks[3].Instructions);
        Assert.Single(blocks[3].Successors);
        Assert.IsType<BreakNode>(blocks[3].Successors[0]);
        Assert.Empty(blocks[3].Successors[0].Successors);
        Assert.False(destNode.IsDefault);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(loops);
        TestUtil.VerifyFlowDirections(branches);
        TestUtil.VerifyFlowDirections(switches);
        TestUtil.EnsureNoRemainingJumps(ctx);
    }

    [Fact]
    public void TestMultiCasesDefault()
    {
        GMCode code = TestUtil.GetCode(
            """
            # switch (a)
            # {
            #     case 1:
            #     case 2:
            #     default:
            # }

            :[0]
            push.v self.a
            dup.v 0
            pushi.e 1
            cmp.i.v EQ
            bt [4]

            :[1]
            dup.v 0
            pushi.e 2
            cmp.i.v EQ
            bt [4]

            :[2]
            b [4]

            :[3]
            b [4]

            :[4]
            popz.v
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);
        List<Fragment> fragments = Fragment.FindFragments(ctx);
        List<Loop> loops = Loop.FindLoops(ctx);
        Switch.FindSwitchStatements(ctx);
        List<BinaryBranch> branches = BinaryBranch.FindBinaryBranches(ctx);
        List<Switch> switches = Switch.InsertSwitchStatements(ctx);

        Assert.Single(switches);
        Switch s0 = switches[0];

        Assert.Equal(blocks[0], s0.Cases);
        Assert.Null(s0.Body);
        Assert.IsType<Switch.CaseDestinationNode>(s0.EndCaseDestinations);
        Assert.Empty(blocks[0].Predecessors);
        Assert.Single(blocks[0].Successors);
        Assert.IsType<Switch.CaseJumpNode>(blocks[0].Successors[0]);
        Assert.Equal([blocks[1]], blocks[0].Successors[0].Successors);
        Assert.Equal(3, blocks[0].Instructions.Count);
        Assert.Single(blocks[1].Successors);
        Assert.IsType<Switch.CaseJumpNode>(blocks[1].Successors[0]);
        Assert.Empty(blocks[1].Successors[0].Successors);
        Assert.Equal(2, blocks[1].Instructions.Count);

        Assert.Empty(blocks[2].Instructions);
        Assert.Empty(blocks[2].Predecessors);
        Assert.Empty(blocks[2].Successors);
        Assert.Equal([blocks[4]], s0.Successors);
        Assert.Empty(blocks[4].Instructions);
        Assert.Equal([s0], blocks[4].Predecessors);

        var destNode = (Switch.CaseDestinationNode)s0.EndCaseDestinations;
        Assert.Empty(destNode.Predecessors);
        Assert.Single(destNode.Successors);
        Assert.IsType<Switch.CaseDestinationNode>(destNode.Successors[0]);
        Assert.False(destNode.IsDefault);

        destNode = (Switch.CaseDestinationNode)destNode.Successors[0];
        Assert.Single(destNode.Successors);
        Assert.IsType<Switch.CaseDestinationNode>(destNode.Successors[0]);
        Assert.False(destNode.IsDefault);

        destNode = (Switch.CaseDestinationNode)destNode.Successors[0];
        Assert.Empty(destNode.Successors);
        Assert.True(destNode.IsDefault);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(loops);
        TestUtil.VerifyFlowDirections(branches);
        TestUtil.VerifyFlowDirections(switches);
        TestUtil.EnsureNoRemainingJumps(ctx);
    }

    [Fact]
    public void TestMultiCasesDefaultBreak()
    {
        GMCode code = TestUtil.GetCode(
            """
            # switch (a)
            # {
            #     case 1:
            #     case 2:
            #     default:
            #         break;
            # }

            :[0]
            push.v self.a
            dup.v 0
            pushi.e 1
            cmp.i.v EQ
            bt [4]

            :[1]
            dup.v 0
            pushi.e 2
            cmp.i.v EQ
            bt [4]

            :[2]
            b [4]

            :[3]
            b [5]

            :[4]
            b [5]

            :[5]
            popz.v
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);
        List<Fragment> fragments = Fragment.FindFragments(ctx);
        List<Loop> loops = Loop.FindLoops(ctx);
        Switch.FindSwitchStatements(ctx);
        List<BinaryBranch> branches = BinaryBranch.FindBinaryBranches(ctx);
        List<Switch> switches = Switch.InsertSwitchStatements(ctx);

        Assert.Single(switches);
        Switch s0 = switches[0];

        Assert.Equal(blocks[0], s0.Cases);
        Assert.IsType<Switch.CaseDestinationNode>(s0.Body);
        Assert.Null(s0.EndCaseDestinations);
        Assert.Empty(blocks[0].Predecessors);
        Assert.Single(blocks[0].Successors);
        Assert.IsType<Switch.CaseJumpNode>(blocks[0].Successors[0]);
        Assert.Equal([blocks[1]], blocks[0].Successors[0].Successors);
        Assert.Equal(3, blocks[0].Instructions.Count);
        Assert.Single(blocks[1].Successors);
        Assert.IsType<Switch.CaseJumpNode>(blocks[1].Successors[0]);
        Assert.Empty(blocks[1].Successors[0].Successors);
        Assert.Equal(2, blocks[1].Instructions.Count);
        Assert.Empty(blocks[2].Instructions);
        Assert.Empty(blocks[2].Predecessors);
        Assert.Empty(blocks[2].Successors);

        Assert.Empty(blocks[3].Instructions);
        Assert.Empty(blocks[3].Predecessors);
        Assert.Empty(blocks[3].Successors);
        Assert.Equal([blocks[5]], s0.Successors);
        Assert.Empty(blocks[5].Instructions);
        Assert.Equal([s0], blocks[5].Predecessors);

        var destNode = (Switch.CaseDestinationNode)s0.Body;
        Assert.Empty(destNode.Predecessors);
        Assert.Single(destNode.Successors);
        Assert.IsType<Switch.CaseDestinationNode>(destNode.Successors[0]);
        Assert.False(destNode.IsDefault);

        destNode = (Switch.CaseDestinationNode)destNode.Successors[0];
        Assert.Single(destNode.Successors);
        Assert.IsType<Switch.CaseDestinationNode>(destNode.Successors[0]);
        Assert.False(destNode.IsDefault);

        destNode = (Switch.CaseDestinationNode)destNode.Successors[0];
        Assert.Equal([blocks[4]], destNode.Successors);
        Assert.Empty(blocks[4].Instructions);
        Assert.Single(blocks[4].Successors);
        Assert.IsType<BreakNode>(blocks[4].Successors[0]);
        Assert.Empty(blocks[4].Successors[0].Successors);
        Assert.True(destNode.IsDefault);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(loops);
        TestUtil.VerifyFlowDirections(branches);
        TestUtil.VerifyFlowDirections(switches);
        TestUtil.EnsureNoRemainingJumps(ctx);
    }

    [Fact]
    public void TestMultiCasesDefaultBreak2()
    {
        GMCode code = TestUtil.GetCode(
            """
            # switch (a)
            # {
            #     case 1:
            #     case 2:
            #         break;
            #     case 3:
            #     default:
            #         break;
            # }

            :[0]
            push.v self.a
            dup.v 0
            pushi.e 1
            cmp.i.v EQ
            bt [5]

            :[1]
            dup.v 0
            pushi.e 2
            cmp.i.v EQ
            bt [5]

            :[2]
            dup.v 0
            pushi.e 3
            cmp.i.v EQ
            bt [6]

            :[3]
            b [6]

            :[4]
            b [7]

            :[5]
            b [7]

            :[6]
            b [7]

            :[7]
            popz.v
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);
        List<Fragment> fragments = Fragment.FindFragments(ctx);
        List<Loop> loops = Loop.FindLoops(ctx);
        Switch.FindSwitchStatements(ctx);
        List<BinaryBranch> branches = BinaryBranch.FindBinaryBranches(ctx);
        List<Switch> switches = Switch.InsertSwitchStatements(ctx);

        Assert.Single(switches);
        Switch s0 = switches[0];

        Assert.Equal(blocks[0], s0.Cases);
        Assert.IsType<Switch.CaseDestinationNode>(s0.Body);
        Assert.Null(s0.EndCaseDestinations);
        Assert.Empty(blocks[0].Predecessors);
        Assert.Single(blocks[0].Successors);
        Assert.IsType<Switch.CaseJumpNode>(blocks[0].Successors[0]);
        Assert.Equal([blocks[1]], blocks[0].Successors[0].Successors);
        Assert.Equal(3, blocks[0].Instructions.Count);
        Assert.Single(blocks[1].Successors);
        Assert.IsType<Switch.CaseJumpNode>(blocks[1].Successors[0]);
        Assert.Equal([blocks[2]], blocks[1].Successors[0].Successors);
        Assert.Equal(2, blocks[1].Instructions.Count);
        Assert.Single(blocks[2].Successors);
        Assert.IsType<Switch.CaseJumpNode>(blocks[2].Successors[0]);
        Assert.Empty(blocks[2].Successors[0].Successors);
        Assert.Equal(2, blocks[2].Instructions.Count);
        Assert.Empty(blocks[3].Instructions);
        Assert.Empty(blocks[3].Predecessors);
        Assert.Empty(blocks[3].Successors);

        Assert.Empty(blocks[4].Instructions);
        Assert.Empty(blocks[4].Predecessors);
        Assert.Empty(blocks[4].Successors);
        Assert.Equal([blocks[7]], s0.Successors);
        Assert.Empty(blocks[7].Instructions);
        Assert.Equal([s0], blocks[7].Predecessors);

        var destNode = (Switch.CaseDestinationNode)s0.Body;
        Assert.Empty(destNode.Predecessors);
        Assert.Single(destNode.Successors);
        Assert.IsType<Switch.CaseDestinationNode>(destNode.Successors[0]);
        Assert.False(destNode.IsDefault);

        destNode = (Switch.CaseDestinationNode)destNode.Successors[0];
        Assert.Equal([blocks[5]], destNode.Successors);
        Assert.Empty(blocks[5].Instructions);
        Assert.Single(blocks[5].Successors);
        Assert.IsType<BreakNode>(blocks[5].Successors[0]);
        Assert.IsType<Switch.CaseDestinationNode>(blocks[5].Successors[0].Successors[0]);
        Assert.False(destNode.IsDefault);

        destNode = (Switch.CaseDestinationNode)blocks[5].Successors[0].Successors[0];
        Assert.Single(destNode.Successors);
        Assert.IsType<Switch.CaseDestinationNode>(destNode.Successors[0]);
        Assert.False(destNode.IsDefault);

        destNode = (Switch.CaseDestinationNode)destNode.Successors[0];
        Assert.Equal([blocks[6]], destNode.Successors);
        Assert.Empty(blocks[6].Instructions);
        Assert.Single(blocks[6].Successors);
        Assert.IsType<BreakNode>(blocks[6].Successors[0]);
        Assert.Empty(blocks[6].Successors[0].Successors);
        Assert.True(destNode.IsDefault);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(loops);
        TestUtil.VerifyFlowDirections(branches);
        TestUtil.VerifyFlowDirections(switches);
        TestUtil.EnsureNoRemainingJumps(ctx);
    }

    [Fact]
    public void TestMultiCasesDefaultBreak3()
    {
        GMCode code = TestUtil.GetCode(
            """
            # switch (a)
            # {
            #     case 1:
            #     case 2:
            #         break;
            #     case 3:
            #     default:
            # }

            :[0]
            push.v self.a
            dup.v 0
            pushi.e 1
            cmp.i.v EQ
            bt [5]

            :[1]
            dup.v 0
            pushi.e 2
            cmp.i.v EQ
            bt [5]

            :[2]
            dup.v 0
            pushi.e 3
            cmp.i.v EQ
            bt [6]

            :[3]
            b [6]

            :[4]
            b [6]

            :[5]
            b [6]

            :[6]
            popz.v
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);
        List<Fragment> fragments = Fragment.FindFragments(ctx);
        List<Loop> loops = Loop.FindLoops(ctx);
        Switch.FindSwitchStatements(ctx);
        List<BinaryBranch> branches = BinaryBranch.FindBinaryBranches(ctx);
        List<Switch> switches = Switch.InsertSwitchStatements(ctx);

        Assert.Single(switches);
        Switch s0 = switches[0];

        Assert.Equal(blocks[0], s0.Cases);
        Assert.IsType<Switch.CaseDestinationNode>(s0.Body);
        Assert.IsType<Switch.CaseDestinationNode>(s0.EndCaseDestinations);
        Assert.Empty(blocks[0].Predecessors);
        Assert.Single(blocks[0].Successors);
        Assert.IsType<Switch.CaseJumpNode>(blocks[0].Successors[0]);
        Assert.Equal([blocks[1]], blocks[0].Successors[0].Successors);
        Assert.Equal(3, blocks[0].Instructions.Count);
        Assert.Single(blocks[1].Successors);
        Assert.IsType<Switch.CaseJumpNode>(blocks[1].Successors[0]);
        Assert.Equal([blocks[2]], blocks[1].Successors[0].Successors);
        Assert.Equal(2, blocks[1].Instructions.Count);
        Assert.Single(blocks[2].Successors);
        Assert.IsType<Switch.CaseJumpNode>(blocks[2].Successors[0]);
        Assert.Empty(blocks[2].Successors[0].Successors);
        Assert.Equal(2, blocks[2].Instructions.Count);
        Assert.Empty(blocks[3].Instructions);
        Assert.Empty(blocks[3].Predecessors);
        Assert.Empty(blocks[3].Successors);

        Assert.Empty(blocks[4].Instructions);
        Assert.Empty(blocks[4].Predecessors);
        Assert.Empty(blocks[4].Successors);
        Assert.Equal([blocks[6]], s0.Successors);
        Assert.Empty(blocks[6].Instructions);
        Assert.Equal([s0], blocks[6].Predecessors);

        var destNode = (Switch.CaseDestinationNode)s0.Body;
        Assert.Empty(destNode.Predecessors);
        Assert.Single(destNode.Successors);
        Assert.IsType<Switch.CaseDestinationNode>(destNode.Successors[0]);
        Assert.False(destNode.IsDefault);

        destNode = (Switch.CaseDestinationNode)destNode.Successors[0];
        Assert.Equal([blocks[5]], destNode.Successors);
        Assert.Empty(blocks[5].Instructions);
        Assert.Single(blocks[5].Successors);
        Assert.IsType<BreakNode>(blocks[5].Successors[0]);
        Assert.Empty(blocks[5].Successors[0].Successors);
        Assert.False(destNode.IsDefault);

        destNode = (Switch.CaseDestinationNode)s0.EndCaseDestinations;
        Assert.Single(destNode.Successors);
        Assert.IsType<Switch.CaseDestinationNode>(destNode.Successors[0]);
        Assert.False(destNode.IsDefault);

        destNode = (Switch.CaseDestinationNode)destNode.Successors[0];
        Assert.Empty(destNode.Successors);
        Assert.True(destNode.IsDefault);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(loops);
        TestUtil.VerifyFlowDirections(branches);
        TestUtil.VerifyFlowDirections(switches);
        TestUtil.EnsureNoRemainingJumps(ctx);
    }

    [Fact]
    public void TestCaseNonConstant()
    {
        GMCode code = TestUtil.GetCode(
            """
            # switch (a)
            # {
            #     case a[b ? c : d]:
            #         break;
            # }

            :[0]
            push.v self.a
            dup.v 0
            pushi.e -1
            push.v self.b
            conv.v.b
            bf [2]

            :[1]
            push.v self.c
            b [3]

            :[2]
            push.v self.d

            :[3]
            conv.v.i
            push.v [array]self.a
            cmp.v.v EQ
            bt [5]

            :[4]
            b [6]

            :[5]
            b [6]

            :[6]
            popz.v
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);
        List<Fragment> fragments = Fragment.FindFragments(ctx);
        List<Loop> loops = Loop.FindLoops(ctx);
        Switch.FindSwitchStatements(ctx);
        List<BinaryBranch> branches = BinaryBranch.FindBinaryBranches(ctx);
        List<Switch> switches = Switch.InsertSwitchStatements(ctx);

        Assert.Single(switches);
        Switch s0 = switches[0];

        Assert.Equal(blocks[3], s0.Cases);
        Assert.IsType<Switch.CaseDestinationNode>(s0.Body);
        Assert.Null(s0.EndCaseDestinations);
        Assert.Empty(blocks[3].Predecessors);
        Assert.Single(blocks[3].Successors);
        Assert.IsType<Switch.CaseJumpNode>(blocks[3].Successors[0]);
        Assert.Empty(blocks[3].Successors[0].Successors);
        Assert.Equal(2, blocks[3].Instructions.Count);

        Assert.Empty(blocks[4].Instructions);
        Assert.Empty(blocks[4].Predecessors);
        Assert.Empty(blocks[4].Successors);
        Assert.IsType<BinaryBranch>(s0.Predecessors[0]);
        Assert.Equal([blocks[6]], s0.Successors);
        Assert.Empty(blocks[6].Instructions);
        Assert.Equal([s0], blocks[6].Predecessors);

        var destNode = (Switch.CaseDestinationNode)s0.Body;
        Assert.Empty(destNode.Predecessors);
        Assert.Equal([blocks[5]], destNode.Successors);
        Assert.Empty(blocks[5].Instructions);
        Assert.Single(blocks[5].Successors);
        Assert.IsType<BreakNode>(blocks[5].Successors[0]);
        Assert.Empty(blocks[5].Successors[0].Successors);
        Assert.False(destNode.IsDefault);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(loops);
        TestUtil.VerifyFlowDirections(branches);
        TestUtil.VerifyFlowDirections(switches);
        TestUtil.EnsureNoRemainingJumps(ctx);
    }

    [Fact]
    public void TestCasesNonConstant()
    {
        GMCode code = TestUtil.GetCode(
            """
            # switch (a)
            # {
            #     case a[b ? c : d]:
            #         break;
            #     case e[f ? g : h]:
            #         break;
            # }

            :[0]
            push.v self.a
            dup.v 0
            pushi.e -1
            push.v self.b
            conv.v.b
            bf [2]

            :[1]
            push.v self.c
            b [3]

            :[2]
            push.v self.d

            :[3]
            conv.v.i
            push.v [array]self.a
            cmp.v.v EQ
            bt [9]

            :[4]
            dup.v 0
            pushi.e -1
            push.v self.f
            conv.v.b
            bf [6]

            :[5]
            push.v self.g
            b [7]

            :[6]
            push.v self.h

            :[7]
            conv.v.i
            push.v [array]self.e
            cmp.v.v EQ
            bt [10]

            :[8]
            b [11]

            :[9]
            b [11]

            :[10]
            b [11]

            :[11]
            popz.v
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);
        List<Fragment> fragments = Fragment.FindFragments(ctx);
        List<Loop> loops = Loop.FindLoops(ctx);
        Switch.FindSwitchStatements(ctx);
        List<BinaryBranch> branches = BinaryBranch.FindBinaryBranches(ctx);
        List<Switch> switches = Switch.InsertSwitchStatements(ctx);

        Assert.Single(switches);
        Switch s0 = switches[0];

        Assert.Equal(blocks[3], s0.Cases);
        Assert.IsType<Switch.CaseDestinationNode>(s0.Body);
        Assert.Null(s0.EndCaseDestinations);
        Assert.Empty(blocks[3].Predecessors);
        Assert.Single(blocks[3].Successors);
        Assert.IsType<Switch.CaseJumpNode>(blocks[3].Successors[0]);
        Assert.Equal([branches[1]], blocks[3].Successors[0].Successors);
        Assert.Equal(2, blocks[3].Instructions.Count);
        Assert.Equal([blocks[7]], branches[1].Successors);
        Assert.Single(blocks[7].Successors);
        Assert.IsType<Switch.CaseJumpNode>(blocks[7].Successors[0]);
        Assert.Empty(blocks[7].Successors[0].Successors);
        Assert.Equal(2, blocks[7].Instructions.Count);

        Assert.Empty(blocks[8].Instructions);
        Assert.Empty(blocks[8].Predecessors);
        Assert.Empty(blocks[8].Successors);
        Assert.Equal([blocks[11]], s0.Successors);
        Assert.Empty(blocks[11].Instructions);
        Assert.Equal([s0], blocks[11].Predecessors);

        var destNode = (Switch.CaseDestinationNode)s0.Body;
        Assert.Empty(destNode.Predecessors);
        Assert.Equal([blocks[9]], destNode.Successors);
        Assert.Empty(blocks[9].Instructions);
        Assert.Single(blocks[9].Successors);
        Assert.IsType<BreakNode>(blocks[9].Successors[0]);
        Assert.Single(blocks[9].Successors[0].Successors);
        Assert.IsType<Switch.CaseDestinationNode>(blocks[9].Successors[0].Successors[0]);
        Assert.False(destNode.IsDefault);

        destNode = (Switch.CaseDestinationNode)blocks[9].Successors[0].Successors[0];
        Assert.Equal([blocks[10]], destNode.Successors);
        Assert.Empty(blocks[10].Instructions);
        Assert.Single(blocks[10].Successors);
        Assert.IsType<BreakNode>(blocks[10].Successors[0]);
        Assert.Empty(blocks[10].Successors[0].Successors);
        Assert.False(destNode.IsDefault);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(loops);
        TestUtil.VerifyFlowDirections(branches);
        TestUtil.VerifyFlowDirections(switches);
        TestUtil.EnsureNoRemainingJumps(ctx);
    }

    [Fact]
    public void TestDoubleEmpty()
    {
        // NOTE: GameMaker itself can't generate this, but modding tools can
        GMCode code = TestUtil.GetCode(
            """
            # switch (a) { }
            # switch (b) { }

            :[0]
            push.v self.a
            b [1]

            :[1]
            popz.v
            push.v self.b
            b [2]

            :[2]
            popz.v
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);
        List<Fragment> fragments = Fragment.FindFragments(ctx);
        List<Loop> loops = Loop.FindLoops(ctx);
        Switch.FindSwitchStatements(ctx);
        List<BinaryBranch> branches = BinaryBranch.FindBinaryBranches(ctx);
        List<Switch> switches = Switch.InsertSwitchStatements(ctx);

        Assert.Equal(2, switches.Count);
        Switch s0 = switches[0];
        Switch s1 = switches[1];

        Assert.Equal(s0, fragments[0].Children[0]);
        Assert.Equal(blocks[0], s0.Cases);
        Assert.Null(s0.Body);
        Assert.Null(s0.EndCaseDestinations);
        Assert.Empty(blocks[0].Predecessors);
        Assert.Empty(blocks[0].Successors);
        Assert.Single(blocks[0].Instructions);
        Assert.Equal([s1], s0.Successors);
        Assert.Equal([s0], s1.Predecessors);
        Assert.Equal(blocks[1], s1.Cases);
        Assert.Null(s1.Body);
        Assert.Null(s1.EndCaseDestinations);
        Assert.Empty(blocks[1].Predecessors);
        Assert.Empty(blocks[1].Successors);
        Assert.Single(blocks[1].Instructions);
        Assert.Empty(blocks[2].Instructions);
        Assert.Equal([blocks[2]], s1.Successors);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(loops);
        TestUtil.VerifyFlowDirections(branches);
        TestUtil.VerifyFlowDirections(switches);
        TestUtil.EnsureNoRemainingJumps(ctx);
    }

    [Fact]
    public void TestDoubleDefault()
    {
        GMCode code = TestUtil.GetCode(
            """
            # switch (a) { default: }
            # switch (b) { default: }

            :[0]
            push.v self.a
            b [2]

            :[1]
            b [2]

            :[2]
            popz.v
            push.v self.b
            b [4]

            :[3]
            b [4]

            :[4]
            popz.v
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);
        List<Fragment> fragments = Fragment.FindFragments(ctx);
        List<Loop> loops = Loop.FindLoops(ctx);
        Switch.FindSwitchStatements(ctx);
        List<BinaryBranch> branches = BinaryBranch.FindBinaryBranches(ctx);
        List<Switch> switches = Switch.InsertSwitchStatements(ctx);

        Assert.Equal(2, switches.Count);
        Switch s0 = switches[0];
        Switch s1 = switches[1];

        Assert.Equal(s0, fragments[0].Children[0]);
        Assert.Equal(blocks[0], s0.Cases);
        Assert.Null(s0.Body);
        Assert.IsType<Switch.CaseDestinationNode>(s0.EndCaseDestinations);
        var destNode = (Switch.CaseDestinationNode)s0.EndCaseDestinations;
        Assert.Empty(destNode.Predecessors);
        Assert.Empty(destNode.Successors);
        Assert.True(destNode.IsDefault);
        Assert.Empty(blocks[0].Predecessors);
        Assert.Empty(blocks[0].Successors);
        Assert.Single(blocks[0].Instructions);
        Assert.Empty(blocks[1].Predecessors);
        Assert.Empty(blocks[1].Successors);
        Assert.Empty(blocks[1].Instructions);
        Assert.Equal([s1], s0.Successors);
        Assert.Equal([s0], s1.Predecessors);
        Assert.Equal(blocks[2], s1.Cases);
        Assert.Null(s1.Body);
        Assert.IsType<Switch.CaseDestinationNode>(s1.EndCaseDestinations);
        destNode = (Switch.CaseDestinationNode)s1.EndCaseDestinations;
        Assert.Empty(destNode.Predecessors);
        Assert.Empty(destNode.Successors);
        Assert.True(destNode.IsDefault);
        Assert.Empty(blocks[2].Predecessors);
        Assert.Empty(blocks[2].Successors);
        Assert.Single(blocks[2].Instructions);
        Assert.Empty(blocks[3].Predecessors);
        Assert.Empty(blocks[3].Successors);
        Assert.Empty(blocks[3].Instructions);
        Assert.Empty(blocks[4].Instructions);
        Assert.Equal([blocks[4]], s1.Successors);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(loops);
        TestUtil.VerifyFlowDirections(branches);
        TestUtil.VerifyFlowDirections(switches);
        TestUtil.EnsureNoRemainingJumps(ctx);
    }

    [Fact]
    public void TestDoubleNonConstant()
    {
        GMCode code = TestUtil.GetCode(
            """
            # switch (a) { case b[c ? d : e]: }
            # switch (f) { case g[h ? i : j]: }

            :[0]
            push.v self.a
            dup.v 0
            pushi.e -1
            push.v self.c
            conv.v.b
            bf [2]

            :[1]
            push.v self.d
            b [3]

            :[2]
            push.v self.e

            :[3]
            conv.v.i
            push.v [array]self.b
            cmp.v.v EQ
            bt [5]

            :[4]
            b [5]

            :[5]
            popz.v
            push.v self.f
            dup.v 0
            pushi.e -1
            push.v self.h
            conv.v.b
            bf [7]

            :[6]
            push.v self.i
            b [8]

            :[7]
            push.v self.j

            :[8]
            conv.v.i
            push.v [array]self.g
            cmp.v.v EQ
            bt [10]

            :[9]
            b [10]

            :[10]
            popz.v
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);
        List<Fragment> fragments = Fragment.FindFragments(ctx);
        List<Loop> loops = Loop.FindLoops(ctx);
        Switch.FindSwitchStatements(ctx);
        List<BinaryBranch> branches = BinaryBranch.FindBinaryBranches(ctx);
        List<Switch> switches = Switch.InsertSwitchStatements(ctx);

        Assert.Equal(2, switches.Count);
        Switch s0 = switches[0];
        Switch s1 = switches[1];

        Assert.Equal(branches[0], fragments[0].Children[0]);
        Assert.Equal([s0], branches[0].Successors);
        Assert.Equal(blocks[3], s0.Cases);
        Assert.Null(s0.Body);
        Assert.IsType<Switch.CaseDestinationNode>(s0.EndCaseDestinations);
        var destNode = (Switch.CaseDestinationNode)s0.EndCaseDestinations;
        Assert.Empty(destNode.Predecessors);
        Assert.Empty(destNode.Successors);
        Assert.False(destNode.IsDefault);
        Assert.Empty(blocks[3].Predecessors);
        Assert.Single(blocks[3].Successors);
        Assert.IsType<Switch.CaseJumpNode>(blocks[3].Successors[0]);
        Assert.Empty(blocks[3].Successors[0].Successors);
        Assert.Equal(2, blocks[3].Instructions.Count);
        Assert.Empty(blocks[4].Predecessors);
        Assert.Empty(blocks[4].Successors);
        Assert.Empty(blocks[4].Instructions);
        Assert.Equal([branches[1]], s0.Successors);
        Assert.Equal([branches[1]], s1.Predecessors);
        Assert.Equal(blocks[8], s1.Cases);
        Assert.Null(s1.Body);
        Assert.IsType<Switch.CaseDestinationNode>(s1.EndCaseDestinations);
        destNode = (Switch.CaseDestinationNode)s1.EndCaseDestinations;
        Assert.Empty(destNode.Predecessors);
        Assert.Empty(destNode.Successors);
        Assert.False(destNode.IsDefault);
        Assert.Empty(blocks[8].Predecessors);
        Assert.Single(blocks[8].Successors);
        Assert.IsType<Switch.CaseJumpNode>(blocks[8].Successors[0]);
        Assert.Empty(blocks[8].Successors[0].Successors);
        Assert.Equal(2, blocks[8].Instructions.Count);
        Assert.Empty(blocks[9].Predecessors);
        Assert.Empty(blocks[9].Successors);
        Assert.Empty(blocks[9].Instructions);
        Assert.Empty(blocks[10].Instructions);
        Assert.Equal([blocks[10]], s1.Successors);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(loops);
        TestUtil.VerifyFlowDirections(branches);
        TestUtil.VerifyFlowDirections(switches);
        TestUtil.EnsureNoRemainingJumps(ctx);
    }

    [Fact]
    public void Test2CasesReturns()
    {
        GMCode code = TestUtil.GetCode(
            """
            # switch (a)
            # {
            #     case 1:
            #         return 10;
            #     case 2:
            #         return 20;
            # }

            :[0]
            push.v self.a
            dup.v 0
            pushi.e 1
            cmp.i.v EQ
            bt [3]

            :[1]
            dup.v 0
            pushi.e 2
            cmp.i.v EQ
            bt [4]

            :[2]
            b [5]

            :[3]
            pushi.e 10
            conv.i.v
            pop.v.v local.$$$$temp$$$$
            popz.v
            push.v local.$$$$temp$$$$
            ret.v

            :[4]
            pushi.e 20
            conv.i.v
            pop.v.v local.$$$$temp$$$$
            popz.v
            push.v local.$$$$temp$$$$
            ret.v

            :[5]
            popz.v
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);
        List<Fragment> fragments = Fragment.FindFragments(ctx);
        List<Loop> loops = Loop.FindLoops(ctx);
        Switch.FindSwitchStatements(ctx);
        List<BinaryBranch> branches = BinaryBranch.FindBinaryBranches(ctx);
        List<Switch> switches = Switch.InsertSwitchStatements(ctx);

        Assert.Single(switches);
        Switch s0 = switches[0];

        Assert.Equal(blocks[0], s0.Cases);
        Assert.IsType<Switch.CaseDestinationNode>(s0.Body);
        Assert.Null(s0.EndCaseDestinations);
        Assert.Empty(blocks[0].Predecessors);
        Assert.Single(blocks[0].Successors);
        Assert.IsType<Switch.CaseJumpNode>(blocks[0].Successors[0]);
        Assert.Equal([blocks[1]], blocks[0].Successors[0].Successors);
        Assert.Equal(3, blocks[0].Instructions.Count);
        Assert.Single(blocks[1].Successors);
        Assert.IsType<Switch.CaseJumpNode>(blocks[1].Successors[0]);
        Assert.Empty(blocks[1].Successors[0].Successors);
        Assert.Equal(2, blocks[1].Instructions.Count);

        Assert.Empty(blocks[2].Instructions);
        Assert.Empty(blocks[2].Predecessors);
        Assert.Empty(blocks[2].Successors);
        Assert.Equal([blocks[5]], s0.Successors);
        Assert.Empty(blocks[5].Instructions);
        Assert.Equal([s0], blocks[5].Predecessors);

        var destNode = (Switch.CaseDestinationNode)s0.Body;
        Assert.Empty(destNode.Predecessors);
        Assert.Equal([blocks[3]], destNode.Successors);
        Assert.Equal(5, blocks[3].Instructions.Count);
        Assert.Single(blocks[3].Successors);
        Assert.IsType<ReturnNode>(blocks[3].Successors[0]);
        Assert.Single(blocks[3].Successors[0].Successors);
        Assert.IsType<Switch.CaseDestinationNode>(blocks[3].Successors[0].Successors[0]);
        Assert.False(destNode.IsDefault);

        destNode = (Switch.CaseDestinationNode)blocks[3].Successors[0].Successors[0];
        Assert.Equal([blocks[4]], destNode.Successors);
        Assert.Equal(5, blocks[4].Instructions.Count);
        Assert.Single(blocks[4].Successors);
        Assert.IsType<ReturnNode>(blocks[4].Successors[0]);
        Assert.Empty(blocks[4].Successors[0].Successors);
        Assert.False(destNode.IsDefault);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(loops);
        TestUtil.VerifyFlowDirections(branches);
        TestUtil.VerifyFlowDirections(switches);
        TestUtil.EnsureNoRemainingJumps(ctx);
    }

    [Fact]
    public void TestNested()
    {
        GMCode code = TestUtil.GetCode(
            """
            # switch (a)
            # {
            #     case 0:
            #         switch (b)
            #         {
            #             default:
            #         }
            #         break;
            # }

            :[0]
            push.v self.a
            dup.v 0
            pushi.e 0
            cmp.i.v EQ
            bt [2]

            :[1]
            b [5]

            :[2]
            push.v self.b
            b [4]

            :[3]
            b [4]

            :[4]
            popz.v
            b [5]

            :[5]
            popz.v
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);
        List<Fragment> fragments = Fragment.FindFragments(ctx);
        List<Loop> loops = Loop.FindLoops(ctx);
        Switch.FindSwitchStatements(ctx);
        List<BinaryBranch> branches = BinaryBranch.FindBinaryBranches(ctx);
        List<Switch> switches = Switch.InsertSwitchStatements(ctx);

        Assert.Equal(2, switches.Count);
        Switch s0 = switches[0]; // outer
        Switch s1 = switches[1]; // inner

        Assert.Equal(blocks[0], s0.Cases);
        Assert.IsType<Switch.CaseDestinationNode>(s0.Body);
        Assert.Null(s0.EndCaseDestinations);
        Assert.Empty(blocks[0].Predecessors);
        Assert.Single(blocks[0].Successors);
        Assert.IsType<Switch.CaseJumpNode>(blocks[0].Successors[0]);
        Assert.Empty(blocks[0].Successors[0].Successors);
        Assert.Equal(3, blocks[0].Instructions.Count);

        Assert.Empty(blocks[1].Instructions);
        Assert.Empty(blocks[1].Predecessors);
        Assert.Empty(blocks[1].Successors);
        Assert.Equal([blocks[5]], s0.Successors);
        Assert.Empty(blocks[5].Instructions);
        Assert.Equal([s0], blocks[5].Predecessors);

        var destNode = (Switch.CaseDestinationNode)s0.Body;
        Assert.Empty(destNode.Predecessors);
        Assert.Equal([s1], destNode.Successors);
        Assert.Equal([blocks[4]], s1.Successors);
        Assert.Single(blocks[4].Successors);
        Assert.IsType<BreakNode>(blocks[4].Successors[0]);
        Assert.Empty(blocks[4].Successors[0].Successors);
        Assert.False(destNode.IsDefault);

        Assert.Equal(blocks[2], s1.Cases);
        Assert.Null(s1.Body);
        Assert.IsType<Switch.CaseDestinationNode>(s1.EndCaseDestinations);
        Assert.Empty(blocks[2].Predecessors);
        Assert.Empty(blocks[2].Successors);

        Assert.Empty(blocks[3].Instructions);
        Assert.Empty(blocks[3].Predecessors);
        Assert.Empty(blocks[3].Successors);

        destNode = (Switch.CaseDestinationNode)s1.EndCaseDestinations;
        Assert.Empty(destNode.Predecessors);
        Assert.Empty(destNode.Successors);
        Assert.True(destNode.IsDefault);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(loops);
        TestUtil.VerifyFlowDirections(branches);
        TestUtil.VerifyFlowDirections(switches);
        TestUtil.EnsureNoRemainingJumps(ctx);
    }
}
