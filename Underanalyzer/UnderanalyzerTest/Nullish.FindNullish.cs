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

public class Nullish_FindNullish
{
    [Fact]
    public void TestBasicExpression()
    {
        GMCode code = TestUtil.GetCode(
            """
            :[0]
            push.v self.a
            isnullish.e
            bf [2]

            :[1]
            popz.v
            push.v self.b

            :[2]
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);
        List<Fragment> fragments = Fragment.FindFragments(ctx);
        List<Nullish> nulls = Nullish.FindNullish(ctx);

        Assert.Single(nulls);
        Assert.Equal(Nullish.NullishType.Expression, nulls[0].NullishKind);
        Assert.Equal([blocks[0]], nulls[0].Predecessors);
        Assert.Equal([blocks[2]], nulls[0].Successors);
        Assert.Equal([nulls[0]], blocks[0].Successors);
        Assert.Equal([nulls[0]], blocks[2].Predecessors);
        Assert.Equal(blocks[1], nulls[0].IfNullish);
        Assert.Empty(blocks[1].Predecessors);
        Assert.Single(blocks[1].Successors);
        Assert.IsType<EmptyNode>(blocks[1].Successors[0]);
        Assert.True(blocks[0].Instructions is [{ Kind: IGMInstruction.Opcode.Push }]);
        Assert.True(blocks[1].Instructions is [{ Kind: IGMInstruction.Opcode.Push }]);
        Assert.Equal(nulls[0], blocks[1].Parent);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(nulls);
    }

    [Fact]
    public void TestBasicAssignment()
    {
        GMCode code = TestUtil.GetCode(
            """
            :[0]
            push.v self.a
            isnullish.e
            bf [2]

            :[1]
            popz.v
            push.v self.b
            pop.v.v self.a
            b [3]

            :[2]
            popz.v

            :[3]
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);
        List<Fragment> fragments = Fragment.FindFragments(ctx);
        List<Nullish> nulls = Nullish.FindNullish(ctx);

        Assert.Single(nulls);
        Assert.Equal(Nullish.NullishType.Assignment, nulls[0].NullishKind);
        Assert.Equal([blocks[0]], nulls[0].Predecessors);
        Assert.Equal([blocks[2]], nulls[0].Successors);
        Assert.Equal([nulls[0]], blocks[0].Successors);
        Assert.Equal([nulls[0]], blocks[2].Predecessors);
        Assert.Equal(blocks[1], nulls[0].IfNullish);
        Assert.Empty(blocks[1].Predecessors);
        Assert.Empty(blocks[1].Successors);
        Assert.True(blocks[0].Instructions is [{ Kind: IGMInstruction.Opcode.Push }]);
        Assert.True(blocks[1].Instructions is [{ Kind: IGMInstruction.Opcode.Push }, { Kind: IGMInstruction.Opcode.Pop }]);
        Assert.True(blocks[2].Instructions is []);
        Assert.Equal([blocks[3]], blocks[2].Successors);
        Assert.Equal(nulls[0], blocks[1].Parent);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(nulls);
    }

    [Fact]
    public void TestChainExpression()
    {
        GMCode code = TestUtil.GetCode(
            """
            :[0]
            push.v self.a
            isnullish.e
            bf [2]

            :[1]
            popz.v
            push.v self.b

            :[2]
            isnullish.e
            bf [4]

            :[3]
            popz.v
            push.v self.c

            :[4]
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);
        List<Fragment> fragments = Fragment.FindFragments(ctx);
        List<Nullish> nulls = Nullish.FindNullish(ctx);

        Nullish firstNull = nulls[1];
        Nullish secondNull = nulls[0];

        Assert.Equal(2, nulls.Count);
        Assert.Equal(Nullish.NullishType.Expression, firstNull.NullishKind);
        Assert.Equal([blocks[0]], firstNull.Predecessors);
        Assert.Equal([blocks[2]], firstNull.Successors);
        Assert.Equal([firstNull], blocks[0].Successors);
        Assert.Equal([firstNull], blocks[2].Predecessors);
        Assert.Equal(blocks[1], firstNull.IfNullish);
        Assert.Empty(blocks[1].Predecessors);
        Assert.Single(blocks[1].Successors);
        Assert.IsType<EmptyNode>(blocks[1].Successors[0]);
        Assert.True(blocks[0].Instructions is [{ Kind: IGMInstruction.Opcode.Push }]);
        Assert.True(blocks[1].Instructions is [{ Kind: IGMInstruction.Opcode.Push }]);
        Assert.Equal(firstNull, blocks[1].Parent);

        Assert.Equal(Nullish.NullishType.Expression, secondNull.NullishKind);
        Assert.Equal([blocks[2]], secondNull.Predecessors);
        Assert.Equal([blocks[4]], secondNull.Successors);
        Assert.Equal([secondNull], blocks[2].Successors);
        Assert.Equal([secondNull], blocks[4].Predecessors);
        Assert.Equal(blocks[3], secondNull.IfNullish);
        Assert.Empty(blocks[3].Predecessors);
        Assert.Single(blocks[3].Successors);
        Assert.IsType<EmptyNode>(blocks[3].Successors[0]);
        Assert.Equal([secondNull], blocks[2].Successors);
        Assert.True(blocks[2].Instructions is []);
        Assert.True(blocks[3].Instructions is [{ Kind: IGMInstruction.Opcode.Push }]);
        Assert.Equal(secondNull, blocks[3].Parent);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(nulls);
    }

    [Fact]
    public void TestCombined()
    {
        GMCode code = TestUtil.GetCode(
            """
            :[0]
            push.v self.a
            isnullish.e
            bf [4]

            :[1]
            popz.v
            push.v self.b
            isnullish.e
            bf [3]

            :[2]
            popz.v
            push.v self.c

            :[3]
            pop.v.v self.a
            b [5]

            :[4]
            popz.v

            :[5]
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);
        List<Fragment> fragments = Fragment.FindFragments(ctx);
        List<Nullish> nulls = Nullish.FindNullish(ctx);

        Nullish firstNull = nulls[1];
        Nullish secondNull = nulls[0];

        Assert.Equal(2, nulls.Count);
        Assert.Equal(Nullish.NullishType.Assignment, firstNull.NullishKind);
        Assert.Equal([blocks[0]], firstNull.Predecessors);
        Assert.Equal([blocks[4]], firstNull.Successors);
        Assert.Equal([firstNull], blocks[0].Successors);
        Assert.Equal([firstNull], blocks[4].Predecessors);
        Assert.Equal(blocks[1], firstNull.IfNullish);
        Assert.Empty(blocks[1].Predecessors);
        Assert.Empty(blocks[3].Successors);
        Assert.True(blocks[0].Instructions is [{ Kind: IGMInstruction.Opcode.Push }]);
        Assert.True(blocks[1].Instructions is [{ Kind: IGMInstruction.Opcode.Push }]);
        Assert.True(blocks[4].Instructions is []);
        Assert.Equal(firstNull, blocks[1].Parent);

        Assert.Equal(Nullish.NullishType.Expression, secondNull.NullishKind);
        Assert.Equal([blocks[1]], secondNull.Predecessors);
        Assert.Equal([blocks[3]], secondNull.Successors);
        Assert.Equal([secondNull], blocks[1].Successors);
        Assert.Equal([secondNull], blocks[3].Predecessors);
        Assert.Equal(blocks[2], secondNull.IfNullish);
        Assert.Empty(blocks[2].Predecessors);
        Assert.Single(blocks[2].Successors);
        Assert.IsType<EmptyNode>(blocks[2].Successors[0]);
        Assert.True(blocks[2].Instructions is [{ Kind: IGMInstruction.Opcode.Push }]);
        Assert.True(blocks[3].Instructions is [{ Kind: IGMInstruction.Opcode.Pop }]);
        Assert.Equal(secondNull, blocks[2].Parent);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(nulls);
    }

    [Fact]
    public void TestInnerBranchAssignment()
    {
        GMCode code = TestUtil.GetCode(
            """
            :[0]
            push.v self.a
            isnullish.e
            bf [5]

            :[1]
            popz.v
            push.v self.b
            conv.v.b
            bf [3]

            :[2]
            push.v self.c
            b [4]

            :[3]
            push.v self.d

            :[4]
            pop.v.v self.a
            b [6]

            :[5]
            popz.v

            :[6]
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);
        List<Fragment> fragments = Fragment.FindFragments(ctx);
        List<Nullish> nulls = Nullish.FindNullish(ctx);

        Assert.Single(nulls);
        Assert.Equal(Nullish.NullishType.Assignment, nulls[0].NullishKind);
        Assert.Equal([blocks[0]], nulls[0].Predecessors);
        Assert.Equal([blocks[5]], nulls[0].Successors);
        Assert.Equal([nulls[0]], blocks[0].Successors);
        Assert.Equal([nulls[0]], blocks[5].Predecessors);
        Assert.Equal(blocks[1], nulls[0].IfNullish);
        Assert.Empty(blocks[1].Predecessors);
        Assert.Empty(blocks[4].Successors);
        Assert.True(blocks[0].Instructions is [{ Kind: IGMInstruction.Opcode.Push }]);
        Assert.True(blocks[5].Instructions is []);
        Assert.Equal(2, blocks[1].Successors.Count);
        Assert.Equal(nulls[0], blocks[1].Parent);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(nulls);
    }
}
