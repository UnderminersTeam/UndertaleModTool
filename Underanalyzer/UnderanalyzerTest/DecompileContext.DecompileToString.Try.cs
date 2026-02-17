/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

namespace UnderanalyzerTest;

public class DecompileContext_DecompileToString_Try
{
    [Fact]
    public void TestBasicTry()
    {
        TestUtil.VerifyDecompileResult(
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
            """,
            """
            try
            {
                a = 1;
            }
            finally
            {
                b = 2;
            }
            """
        );
    }

    [Fact]
    public void TestBasicTryCatch()
    {
        TestUtil.VerifyDecompileResult(
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
            """,
            """
            try
            {
                a = 1;
            }
            catch (ex)
            {
                show_debug_message(ex);
            }
            """
        );
    }

    [Fact]
    public void TestNestedTryCatch()
    {
        TestUtil.VerifyDecompileResult(

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
            """,
            """
            try
            {
                try
                {
                }
                catch (ex2)
                {
                }
            }
            catch (ex)
            {
                try
                {
                }
                catch (ex2)
                {
                }
            }
            """
        );
    }

    [Fact]
    public void TestTryIf()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            push.i 128
            conv.i.v
            push.i 80
            conv.i.v
            call.i @@try_hook@@ 2
            popz.v

            :[1]
            pushi.e 1
            pop.v.i self.a
            push.v self.b
            conv.v.b
            bf [3]

            :[2]
            pushi.e 1
            pop.v.i self.c

            :[3]
            b [5]

            :[4]
            pop.v.v local.e
            call.i @@try_unhook@@ 0
            popz.v
            pushi.e 1
            pop.v.i self.d
            call.i @@finish_catch@@ 0
            popz.v
            b [6]

            :[5]
            call.i @@try_unhook@@ 0
            popz.v

            :[6]
            """,
            """
            try
            {
                a = 1;
                if (b)
                {
                    c = 1;
                }
            }
            catch (e)
            {
                d = 1;
            }
            """
        );
    }

    [Fact]
    public void TestTryExitFinally()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            push.i 72
            conv.i.v
            push.i -1
            conv.i.v
            call.i @@try_hook@@ 2
            popz.v
            pushi.e 1
            pop.v.i self.a
            pushi.e 2
            pop.v.i self.b
            call.i @@try_unhook@@ 0
            exit.i

            :[1]
            call.i @@try_unhook@@ 0
            popz.v
            pushi.e 2
            pop.v.i self.b
            call.i @@finish_finally@@ 0
            popz.v
            b [2]

            :[2]
            """,
            """
            try
            {
                a = 1;
                exit;
            }
            finally
            {
                b = 2;
            }
            """
        );
    }
}