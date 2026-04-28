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

public class TryCatch_FindTryCatch
{
    [Fact]
    public void TestBasicTry()
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
        List<Fragment> fragments = Fragment.FindFragments(ctx);
        List<TryCatch> tryCatch = TryCatch.FindTryCatch(ctx);

        Assert.Single(tryCatch);
        TryCatch tc = tryCatch[0];
        Assert.Equal([blocks[0]], tc.Predecessors);
        Assert.Equal([blocks[2]], tc.Successors);
        Assert.Empty(blocks[0].Instructions);
        Assert.True(blocks[2].Instructions is 
        [
            { Kind: IGMInstruction.Opcode.PushImmediate },
            { Kind: IGMInstruction.Opcode.Pop },
            { Kind: IGMInstruction.Opcode.Call },
            { Kind: IGMInstruction.Opcode.PopDelete }
        ]);
        Assert.Empty(tc.Try.Predecessors);
        Assert.Equal(blocks[1], tc.Try);
        Assert.Single(blocks[1].Successors);
        Assert.IsType<EmptyNode>(blocks[1].Successors[0]);
        Assert.Null(tc.Catch);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(tryCatch);
    }

    [Fact]
    public void TestBasicTryCatch()
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
        List<Fragment> fragments = Fragment.FindFragments(ctx);
        List<TryCatch> tryCatch = TryCatch.FindTryCatch(ctx);

        Assert.Single(tryCatch);
        TryCatch tc = tryCatch[0];
        Assert.Equal([blocks[0]], tc.Predecessors);
        Assert.Equal([blocks[3]], tc.Successors);
        Assert.Empty(blocks[0].Instructions);
        Assert.Equal(2, blocks[1].Instructions.Count);
        Assert.True(blocks[1].Instructions is not [.., { Kind: IGMInstruction.Opcode.Branch }]);
        Assert.Equal(6, blocks[2].Instructions.Count);
        Assert.True(blocks[2].Instructions is not [.., { Kind: IGMInstruction.Opcode.Branch }]);
        Assert.True(blocks[2].Instructions is not 
            [.., { Kind: IGMInstruction.Opcode.Call }, { Kind: IGMInstruction.Opcode.PopDelete },
             { Kind: IGMInstruction.Opcode.Branch }]);
        Assert.Empty(blocks[3].Instructions);
        Assert.Equal(blocks[1], tc.Try);
        Assert.Empty(tc.Try.Predecessors);
        Assert.Single(blocks[1].Successors);
        Assert.IsType<EmptyNode>(blocks[1].Successors[0]);
        Assert.Equal(blocks[2], tc.Catch);
        Assert.Empty(tc.Catch!.Predecessors);
        Assert.Single(blocks[2].Successors);
        Assert.IsType<EmptyNode>(blocks[2].Successors[0]);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(tryCatch);
    }

    [Fact]
    public void TestNestedTryCatch()
    {
        GMCode code = TestUtil.GetCode(
            """
            :[0]
            push.i 252
            conv.i.v
            push.i 128
            conv.i.v
            call.i @@try_hook@@ 2
            popz.v

            :[1]
            push.i 112
            conv.i.v
            push.i 76
            conv.i.v
            call.i @@try_hook@@ 2
            popz.v

            :[2]
            b [4]

            :[3]
            pop.v.v local.ex2
            call.i @@try_unhook@@ 0
            popz.v
            call.i @@finish_catch@@ 0
            popz.v
            b [5]

            :[4]
            call.i @@try_unhook@@ 0
            popz.v

            :[5]
            b [12]

            :[6]
            pop.v.v local.ex
            call.i @@try_unhook@@ 0
            popz.v

            :[7]
            push.i 224
            conv.i.v
            push.i 188
            conv.i.v
            call.i @@try_hook@@ 2
            popz.v

            :[8]
            b [10]

            :[9]
            pop.v.v local.ex2
            call.i @@try_unhook@@ 0
            popz.v
            call.i @@finish_catch@@ 0
            popz.v
            b [11]

            :[10]
            call.i @@try_unhook@@ 0
            popz.v

            :[11]
            call.i @@finish_catch@@ 0
            popz.v
            b [13]

            :[12]
            call.i @@try_unhook@@ 0
            popz.v

            :[13]
            """
        );
        DecompileContext ctx = new(code);
        List<Block> blocks = Block.FindBlocks(ctx);
        List<Fragment> fragments = Fragment.FindFragments(ctx);
        List<TryCatch> tryCatch = TryCatch.FindTryCatch(ctx);

        Assert.Equal(14, blocks.Count);
        Assert.Equal(3, tryCatch.Count);
        TryCatch tc0 = tryCatch[0];
        TryCatch tc1 = tryCatch[1];
        TryCatch tc2 = tryCatch[2];

        Assert.Equal([blocks[0]], tc0.Predecessors);
        Assert.Equal([blocks[12]], tc0.Successors);
        Assert.Equal([tc0], blocks[12].Predecessors);
        Assert.Equal(blocks[1], tc0.Try);
        Assert.Equal(blocks[6], tc0.Catch);
        Assert.Empty(tc0.Try.Predecessors);
        Assert.Empty(tc0.Catch!.Predecessors);
        Assert.Single(blocks[5].Successors);
        Assert.IsType<EmptyNode>(blocks[5].Successors[0]);
        Assert.Single(blocks[11].Successors);
        Assert.IsType<EmptyNode>(blocks[11].Successors[0]);

        Assert.Equal([blocks[1]], tc1.Predecessors);
        Assert.Equal([blocks[4]], tc1.Successors);
        Assert.Equal([tc1], blocks[4].Predecessors);
        Assert.Equal(blocks[2], tc1.Try);
        Assert.Equal(blocks[3], tc1.Catch);
        Assert.Empty(tc1.Try.Predecessors);
        Assert.Empty(tc1.Catch!.Predecessors);
        Assert.Single(blocks[2].Successors);
        Assert.IsType<EmptyNode>(blocks[2].Successors[0]);
        Assert.Single(blocks[3].Successors);
        Assert.IsType<EmptyNode>(blocks[3].Successors[0]);

        Assert.Equal([blocks[7]], tc2.Predecessors);
        Assert.Equal([blocks[10]], tc2.Successors);
        Assert.Equal([tc2], blocks[10].Predecessors);
        Assert.Equal(blocks[8], tc2.Try);
        Assert.Equal(blocks[9], tc2.Catch);
        Assert.Empty(tc2.Try.Predecessors);
        Assert.Empty(tc2.Catch!.Predecessors);
        Assert.Single(blocks[8].Successors);
        Assert.IsType<EmptyNode>(blocks[8].Successors[0]);
        Assert.Single(blocks[9].Successors);
        Assert.IsType<EmptyNode>(blocks[9].Successors[0]);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(tryCatch);
    }
}
