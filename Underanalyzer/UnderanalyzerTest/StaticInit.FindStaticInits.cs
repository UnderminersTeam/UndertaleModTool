/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using Underanalyzer.Decompiler;
using Underanalyzer.Decompiler.ControlFlow;
using Underanalyzer.Mock;

namespace UnderanalyzerTest;

public class StaticInit_FindStaticInits
{
    [Fact]
    public void TestBasic()
    {
        GMCode code = TestUtil.GetCode(
            """
            :[0]
            b [4]

            > test_function
            :[1]
            isstaticok.e
            bt [3]

            :[2]
            pushi.e 0
            pop.v.i static.a

            :[3]
            setstatic.e
            exit.i

            :[4]
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);
        List<Fragment> fragments = Fragment.FindFragments(ctx);
        List<StaticInit> sis = StaticInit.FindStaticInits(ctx);

        Assert.Single(sis);
        Assert.Equal([blocks[1]], sis[0].Predecessors);
        Assert.Equal([blocks[3]], sis[0].Successors);
        Assert.Equal(blocks[2], sis[0].Head);
        Assert.Empty(sis[0].Head.Predecessors);
        Assert.Empty(blocks[2].Successors);
        Assert.Empty(blocks[0].Instructions);
        Assert.Empty(blocks[1].Instructions);
        Assert.Empty(blocks[3].Instructions);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(sis);
    }

    [Fact]
    public void TestStruct()
    {
        GMCode code = TestUtil.GetCode(
            """
            :[0]
            b [6]

            > test_function (locals=0, argc=1)
            :[1]
            isstaticok.e
            bt [5]

            :[2]
            push.v self.a
            b [4]

            > test_struct (locals=0, argc=0)
            :[3]
            pushi.e -15
            pushi.e 0
            push.v [array]self.argument
            pop.v.v self.val
            exit.i

            :[4]
            push.i [function]test_struct
            conv.i.v
            call.i @@NullObject@@ 0
            call.i method 2
            dup.v 0
            pushi.e -16
            pop.v.v [stacktop]static.___test_struct___1234
            call.i @@NewGMLObject@@ 2
            pop.v.v static.test

            :[5]
            setstatic.e
            exit.i

            :[6]
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);
        List<Fragment> fragments = Fragment.FindFragments(ctx);
        List<StaticInit> sis = StaticInit.FindStaticInits(ctx);

        Assert.Single(sis);
        Assert.Equal([blocks[1]], sis[0].Predecessors);
        Assert.Equal([blocks[5]], sis[0].Successors);
        Assert.Equal(blocks[2], sis[0].Head);
        Assert.Empty(sis[0].Head.Predecessors);
        Assert.Empty(blocks[4].Successors);
        Assert.Empty(blocks[0].Instructions);
        Assert.Empty(blocks[1].Instructions);
        Assert.Single(blocks[2].Instructions);
        Assert.Equal(4, blocks[3].Instructions.Count);
        Assert.Empty(blocks[5].Instructions);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(sis);
    }
}
