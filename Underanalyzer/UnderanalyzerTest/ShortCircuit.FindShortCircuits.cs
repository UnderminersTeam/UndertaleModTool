/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using Underanalyzer.Decompiler;
using Underanalyzer.Decompiler.ControlFlow;
using Underanalyzer.Mock;

namespace UnderanalyzerTest;

public class ShortCircuit_FindShortCircuits
{
    [Fact]
    public void TestBasicAnd()
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
            b [3]

            :[2]
            push.e 0
            
            :[3]
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);
        List<Fragment> fragments = Fragment.FindFragments(ctx);
        ShortCircuit.FindShortCircuits(ctx);
        List<ShortCircuit> scs = ShortCircuit.InsertShortCircuits(ctx);

        Assert.Single(scs);
        Assert.Equal(ShortCircuitType.And, scs[0].LogicKind);
        Assert.Equal(fragments[0], scs[0].Parent);
        Assert.Equal([blocks[0], blocks[1]], scs[0].Children);
        Assert.Equal(2, blocks[0].Instructions.Count);
        Assert.Equal(2, blocks[1].Instructions.Count);
        Assert.Empty(blocks[0].Predecessors);
        Assert.Empty(blocks[0].Successors);
        Assert.Empty(blocks[1].Predecessors);
        Assert.Empty(blocks[1].Successors);
        Assert.Empty(blocks[2].Instructions);
        Assert.Empty(blocks[2].Predecessors);
        Assert.Empty(blocks[2].Successors);
        Assert.Empty(scs[0].Predecessors);
        Assert.Equal([blocks[3]], scs[0].Successors);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(scs);
    }

    [Fact]
    public void TestBasicOr()
    {
        GMCode code = TestUtil.GetCode(
            """
            :[0]
            push.v self.a
            conv.v.b
            bt [2]

            :[1]
            push.v self.b
            conv.v.b
            b [3]

            :[2]
            push.e 1

            :[3]
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);
        List<Fragment> fragments = Fragment.FindFragments(ctx);
        ShortCircuit.FindShortCircuits(ctx);
        List<ShortCircuit> scs = ShortCircuit.InsertShortCircuits(ctx);

        Assert.Single(scs);
        Assert.Equal(ShortCircuitType.Or, scs[0].LogicKind);
        Assert.Equal(fragments[0], scs[0].Parent);
        Assert.Equal([blocks[0], blocks[1]], scs[0].Children);
        Assert.Equal(2, blocks[0].Instructions.Count);
        Assert.Equal(2, blocks[1].Instructions.Count);
        Assert.Empty(blocks[0].Predecessors);
        Assert.Empty(blocks[0].Successors);
        Assert.Empty(blocks[1].Predecessors);
        Assert.Empty(blocks[1].Successors);
        Assert.Empty(blocks[2].Instructions);
        Assert.Empty(blocks[2].Predecessors);
        Assert.Empty(blocks[2].Successors);
        Assert.Empty(scs[0].Predecessors);
        Assert.Equal([blocks[3]], scs[0].Successors);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(scs);
    }

    [Fact]
    public void TestTripleAnd()
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
            push.v self.c
            conv.v.b
            b [4]

            :[3]
            push.e 0

            :[4]
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);
        List<Fragment> fragments = Fragment.FindFragments(ctx);
        ShortCircuit.FindShortCircuits(ctx);
        List<ShortCircuit> scs = ShortCircuit.InsertShortCircuits(ctx);

        Assert.Single(scs);
        Assert.Equal(ShortCircuitType.And, scs[0].LogicKind);
        Assert.Equal(fragments[0], scs[0].Parent);
        Assert.Equal([blocks[0], blocks[1], blocks[2]], scs[0].Children);
        Assert.Equal(2, blocks[0].Instructions.Count);
        Assert.Equal(2, blocks[1].Instructions.Count);
        Assert.Equal(2, blocks[2].Instructions.Count);
        Assert.Empty(blocks[0].Predecessors);
        Assert.Empty(blocks[0].Successors);
        Assert.Empty(blocks[1].Predecessors);
        Assert.Empty(blocks[1].Successors);
        Assert.Empty(blocks[3].Instructions);
        Assert.Empty(blocks[3].Predecessors);
        Assert.Empty(blocks[3].Successors);
        Assert.Empty(scs[0].Predecessors);
        Assert.Equal([blocks[4]], scs[0].Successors);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(scs);
    }

    [Fact]
    public void TestNestedStart()
    {
        GMCode code = TestUtil.GetCode(
            """
            :[0]
            push.v self.a
            conv.v.b
            bt [2]

            :[1]
            push.v self.b
            conv.v.b
            b [3]

            :[2]
            push.e 1

            :[3]
            bf [6]

            :[4]
            push.v self.c
            conv.v.b
            bf [6]

            :[5]
            push.v self.d
            conv.v.b
            b [7]

            :[6]
            push.e 0

            :[7]
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);
        List<Fragment> fragments = Fragment.FindFragments(ctx);
        ShortCircuit.FindShortCircuits(ctx);
        List<ShortCircuit> scs = ShortCircuit.InsertShortCircuits(ctx);

        Assert.Equal(2, scs.Count);
        Assert.Equal(ShortCircuitType.Or, scs[0].LogicKind);
        Assert.Equal(ShortCircuitType.And, scs[1].LogicKind);
        Assert.Equal(fragments[0], scs[0].Parent);
        Assert.Equal([blocks[0], blocks[1]], scs[0].Children);
        Assert.Equal([blocks[3], blocks[4], blocks[5]], scs[1].Children);
        Assert.Equal(2, blocks[0].Instructions.Count);
        Assert.Equal(2, blocks[1].Instructions.Count);
        Assert.Empty(blocks[2].Instructions);
        Assert.Empty(blocks[3].Instructions);
        Assert.Equal(2, blocks[4].Instructions.Count);
        Assert.Equal(2, blocks[5].Instructions.Count);
        Assert.Empty(blocks[6].Instructions);
        for (int i = 0; i <= 6; i++)
        {
            Assert.Empty(blocks[i].Predecessors);
            Assert.Empty(blocks[i].Successors);
        }
        Assert.Empty(scs[0].Predecessors);
        Assert.Equal([scs[1]], scs[0].Successors);
        Assert.Equal([blocks[7]], scs[1].Successors);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(scs);
    }

    [Fact]
    public void TestNestedMiddle()
    {
        GMCode code = TestUtil.GetCode(
            """
            :[0]
            push.v self.a
            conv.v.b
            bf [6]

            :[1]
            push.v self.b
            conv.v.b
            bt [3]

            :[2]
            push.v self.c
            conv.v.b
            b [4]

            :[3]
            push.e 1

            :[4]
            bf [6]

            :[5]
            push.v self.d
            conv.v.b
            b [7]

            :[6]
            push.e 0

            :[7]
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);
        List<Fragment> fragments = Fragment.FindFragments(ctx);
        ShortCircuit.FindShortCircuits(ctx);
        List<ShortCircuit> scs = ShortCircuit.InsertShortCircuits(ctx);

        Assert.Equal(2, scs.Count);
        Assert.Equal(ShortCircuitType.Or, scs[0].LogicKind);
        Assert.Equal(ShortCircuitType.And, scs[1].LogicKind);
        Assert.Equal(scs[1], scs[0].Parent);
        Assert.Equal(fragments[0], scs[1].Parent);
        Assert.Equal([blocks[1], blocks[2]], scs[0].Children);
        Assert.Equal([blocks[0], scs[0], blocks[5]], scs[1].Children);
        Assert.Equal(2, blocks[0].Instructions.Count);
        Assert.Equal(2, blocks[1].Instructions.Count);
        Assert.Equal(2, blocks[2].Instructions.Count);
        Assert.Empty(blocks[3].Instructions);
        Assert.Empty(blocks[4].Instructions);
        Assert.Equal(2, blocks[5].Instructions.Count);
        Assert.Empty(blocks[6].Instructions);
        for (int i = 0; i <= 6; i++)
        {
            if (i != 4)
            {
                Assert.Empty(blocks[i].Predecessors);
            }
            Assert.Empty(blocks[i].Successors);
        }
        Assert.Empty(scs[0].Predecessors);
        Assert.Equal([blocks[4]], scs[0].Successors);
        Assert.Empty(scs[1].Predecessors);
        Assert.Equal([blocks[7]], scs[1].Successors);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(scs);
    }

    [Fact]
    public void TestNestedEnd()
    {
        GMCode code = TestUtil.GetCode(
            """
            :[0]
            push.v self.a
            conv.v.b
            bf [6]

            :[1]
            push.v self.b
            conv.v.b
            bf [6]

            :[2]
            push.v self.c
            conv.v.b
            bt [4]

            :[3]
            push.v self.d
            conv.v.b
            b [5]

            :[4]
            push.e 1

            :[5]
            b [7]

            :[6]
            push.e 0

            :[7]
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);
        List<Fragment> fragments = Fragment.FindFragments(ctx);
        ShortCircuit.FindShortCircuits(ctx);
        List<ShortCircuit> scs = ShortCircuit.InsertShortCircuits(ctx);

        Assert.Equal(2, scs.Count);
        Assert.Equal(ShortCircuitType.Or, scs[0].LogicKind);
        Assert.Equal(ShortCircuitType.And, scs[1].LogicKind);
        Assert.Equal(scs[1], scs[0].Parent);
        Assert.Equal(fragments[0], scs[1].Parent);
        Assert.Equal([blocks[2], blocks[3]], scs[0].Children);
        Assert.Equal([blocks[0], blocks[1], scs[0]], scs[1].Children);
        Assert.Equal(2, blocks[0].Instructions.Count);
        Assert.Equal(2, blocks[1].Instructions.Count);
        Assert.Equal(2, blocks[2].Instructions.Count);
        Assert.Equal(2, blocks[3].Instructions.Count);
        Assert.Empty(blocks[4].Instructions);
        Assert.Empty(blocks[5].Instructions);
        Assert.Empty(blocks[6].Instructions);
        for (int i = 0; i <= 6; i++)
        {
            if (i != 5)
            {
                Assert.Empty(blocks[i].Predecessors);
            }
            Assert.Empty(blocks[i].Successors);
        }
        Assert.Empty(scs[0].Predecessors);
        Assert.Equal([blocks[5]], scs[0].Successors);
        Assert.Empty(scs[1].Predecessors);
        Assert.Equal([blocks[7]], scs[1].Successors);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(scs);
    }

    [Fact]
    public void TestNestedAll()
    {
        GMCode code = TestUtil.GetCode(
            """
            :[0]
            push.v self.a
            conv.v.b
            bt [2]

            :[1]
            push.v self.b
            conv.v.b
            b [3]

            :[2]
            push.e 1

            :[3]
            bf [12]

            :[4]
            push.v self.c
            conv.v.b
            bt [6]

            :[5]
            push.v self.d
            conv.v.b
            b [7]

            :[6]
            push.e 1

            :[7]
            bf [12]

            :[8]
            push.v self.e
            conv.v.b
            bt [10]

            :[9]
            push.v self.f
            conv.v.b
            b [11]

            :[10]
            push.e 1

            :[11]
            b [13]

            :[12]
            push.e 0

            :[13]
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);
        List<Fragment> fragments = Fragment.FindFragments(ctx);
        ShortCircuit.FindShortCircuits(ctx);
        List<ShortCircuit> scs = ShortCircuit.InsertShortCircuits(ctx);

        Assert.Equal(4, scs.Count);
        Assert.Equal(ShortCircuitType.Or, scs[0].LogicKind);
        Assert.Equal(ShortCircuitType.Or, scs[1].LogicKind);
        Assert.Equal(ShortCircuitType.Or, scs[2].LogicKind);
        Assert.Equal(ShortCircuitType.And, scs[3].LogicKind);
        Assert.Equal(fragments[0], scs[0].Parent);
        Assert.Equal(scs[3], scs[1].Parent);
        Assert.Equal(scs[3], scs[2].Parent);
        Assert.Equal([blocks[0], blocks[1]], scs[0].Children);
        Assert.Equal([blocks[4], blocks[5]], scs[1].Children);
        Assert.Equal([blocks[8], blocks[9]], scs[2].Children);
        Assert.Equal([blocks[3], scs[1], scs[2]], scs[3].Children);
        Assert.Equal(2, blocks[0].Instructions.Count);
        Assert.Equal(2, blocks[1].Instructions.Count);
        Assert.Empty(blocks[2].Instructions);
        Assert.Empty(blocks[3].Instructions);
        Assert.Equal(2, blocks[4].Instructions.Count);
        Assert.Equal(2, blocks[5].Instructions.Count);
        Assert.Empty(blocks[6].Instructions);
        Assert.Empty(blocks[7].Instructions);
        Assert.Equal(2, blocks[8].Instructions.Count);
        Assert.Equal(2, blocks[9].Instructions.Count);
        Assert.Empty(blocks[10].Instructions);
        Assert.Empty(blocks[11].Instructions);
        Assert.Empty(blocks[12].Instructions);
        for (int i = 0; i <= 12; i++)
        {
            if (i != 7 && i != 11)
            {
                Assert.Empty(blocks[i].Predecessors);
            }
            Assert.Empty(blocks[i].Successors);
        }
        Assert.Empty(scs[0].Predecessors);
        Assert.Empty(scs[1].Predecessors);
        Assert.Empty(scs[2].Predecessors);
        Assert.Equal([scs[3]], scs[0].Successors);
        Assert.Equal([blocks[7]], scs[1].Successors);
        Assert.Equal([blocks[11]], scs[2].Successors);
        Assert.Equal([scs[0]], scs[3].Predecessors);
        Assert.Equal([blocks[13]], scs[3].Successors);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(scs);
    }
}
