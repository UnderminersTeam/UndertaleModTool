/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using Underanalyzer.Decompiler;
using Underanalyzer.Decompiler.ControlFlow;
using Underanalyzer.Mock;

namespace UnderanalyzerTest;

public class Fragment_FindFragments
{
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
        List<Fragment> fragments = Fragment.FindFragments(ctx);

        Assert.Single(fragments);
        Assert.Equal(2, fragments[0].Children.Count);
        Assert.Equal(blocks[0], fragments[0].Children[0]);
        Assert.Equal(blocks[1], fragments[0].Children[1]);
        Assert.Empty(fragments[0].Predecessors);
        Assert.Empty(fragments[0].Successors);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
    }

    [Fact]
    public void TestDouble()
    {
        GMCode code = TestUtil.GetCode(
            """
            :[0]
            pushi.e 0
            b [2]

            > child_entry
            :[1]
            pushi.e 1
            exit.i

            :[2]
            pushi.e 2

            :[3]
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);
        List<Fragment> fragments = Fragment.FindFragments(ctx);

        Assert.Equal(2, fragments.Count);

        Assert.Equal(3, fragments[0].Children.Count);
        Assert.Equal(blocks[0], fragments[0].Children[0]);
        Assert.Equal(blocks[2], fragments[0].Children[1]);
        Assert.Equal(blocks[3], fragments[0].Children[2]);
        Assert.Equal(fragments[1], blocks[0].Successors[0]);
        Assert.Equal(fragments[1], blocks[2].Predecessors[0]);
        Assert.Empty(fragments[0].Predecessors);
        Assert.Empty(fragments[0].Successors);

        Assert.Single(blocks[0].Instructions);
        Assert.Single(fragments[1].Children);
        Assert.Equal(blocks[1], fragments[1].Children[0]);
        Assert.Single(blocks[1].Instructions);
        Assert.Equal(1, blocks[1].Instructions[0].ValueShort);
        Assert.Equal(blocks[0], fragments[1].Predecessors[0]);
        Assert.Equal(blocks[2], fragments[1].Successors[0]);
        Assert.Empty(blocks[1].Successors);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
    }

    [Fact]
    public void TestNested()
    {
        GMCode code = TestUtil.GetCode(
            """
            :[0]
            pushi.e 0
            b [6]

            > child_entry
            :[1]
            pushi.e 1
            b [3]

            > child_child_entry_1
            :[2]
            pushi.e 2
            exit.i

            :[3]
            pushi.e 3
            b [5]

            > child_child_entry_2
            :[4]
            pushi.e 4
            exit.i

            :[5]
            # Test an empty block here
            exit.i

            :[6]
            pushi.e 6

            :[7]
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);
        List<Fragment> fragments = Fragment.FindFragments(ctx);

        Assert.Equal(4, fragments.Count);

        Assert.Equal("root", fragments[0].CodeEntry.Name.Content);
        Assert.Equal([blocks[0], blocks[6], blocks[7]], fragments[0].Children);
        Assert.Equal([], fragments[0].Predecessors);
        Assert.Equal([], fragments[0].Successors);

        Assert.Equal("child_entry", fragments[1].CodeEntry.Name.Content);
        Assert.Single(blocks[0].Instructions);
        Assert.Equal([blocks[1], blocks[3], blocks[5]], fragments[1].Children);
        Assert.Empty(blocks[5].Instructions);
        Assert.Equal([blocks[0]], fragments[1].Predecessors);
        Assert.Equal([blocks[6]], fragments[1].Successors);
        Assert.Empty(blocks[1].Predecessors);
        Assert.Empty(blocks[5].Successors);

        Assert.Equal("child_child_entry_1", fragments[2].CodeEntry.Name.Content);
        Assert.Single(blocks[1].Instructions);
        Assert.Equal([blocks[2]], fragments[2].Children);
        Assert.Equal([blocks[1]], fragments[2].Predecessors);
        Assert.Equal([blocks[3]], fragments[2].Successors);
        Assert.Empty(blocks[2].Predecessors);
        Assert.Empty(blocks[2].Successors);

        Assert.Equal("child_child_entry_2", fragments[3].CodeEntry.Name.Content);
        Assert.Single(blocks[3].Instructions);
        Assert.Equal([blocks[4]], fragments[3].Children);
        Assert.Equal([blocks[3]], fragments[3].Predecessors);
        Assert.Equal([blocks[5]], fragments[3].Successors);
        Assert.Empty(blocks[4].Predecessors);
        Assert.Empty(blocks[4].Successors);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
    }
}
