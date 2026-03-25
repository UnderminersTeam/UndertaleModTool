/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

namespace UnderanalyzerTest;

public class DecompileContext_DecompileToString_SwitchContinue
{
    [Fact]
    public void TestForDoubleContinue()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            pushi.e 0
            pop.v.i self.i

            :[1]
            push.v self.i
            pushi.e 10
            cmp.i.v LT
            bf [5]

            :[2]
            b [4]

            :[3]
            b [4]

            :[4]
            push.v self.i
            push.e 1
            add.i.v
            pop.v.v self.i
            b [1]

            :[5]
            """,
            """
            for (i = 0; i < 10; i++)
            {
                continue;
                continue;
            }
            """
        );
    }

    [Fact]
    public void TestForDoubleContinueInSwitch()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            push.v self.a
            dup.v 0
            pushi.e 1
            cmp.i.v EQ
            bt [2]

            :[1]
            b [8]

            :[2]
            pushi.e 0
            pop.v.i self.i

            :[3]
            push.v self.i
            pushi.e 10
            cmp.i.v LT
            bf [8]

            :[4]
            b [6]

            :[5]
            b [6]

            :[6]
            popz.v
            exit.i

            :[7]
            b [3]

            :[8]
            popz.v
            """,
            """
            switch (a)
            {
                case 1:
                    for (i = 0; i < 10; exit)
                    {
                        continue;
                        continue;
                    }
            }
            """
        );
    }

    [Fact]
    public void TestSwitchContinueEdgeCase1()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            push.v self.a
            dup.v 0
            pushi.e 1
            cmp.i.v EQ
            bt [2]

            :[1]
            b [9]

            :[2]
            pushi.e 0
            pop.v.i self.i

            :[3]
            push.v self.i
            pushi.e 10
            cmp.i.v LT
            bf [9]

            :[4]
            push.v self.a
            conv.v.b
            bf [6]

            :[5]
            b [7]

            :[6]
            b [7]

            :[7]
            popz.v
            exit.i

            :[8]
            b [3]

            :[9]
            popz.v
            """,
            """
            switch (a)
            {
                case 1:
                    for (i = 0; i < 10; exit)
                    {
                        if (a)
                        {
                        }
                        else
                        {
                            continue;
                        }
                    }
            }
            """,
            null,
            new Underanalyzer.Decompiler.DecompileSettings()
            {
                CleanupElseToContinue = false
            }
        );
    }

    [Fact]
    public void TestSwitchContinueEdgeCase2()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            push.v self.a
            dup.v 0
            pushi.e 1
            cmp.i.v EQ
            bt [2]

            :[1]
            b [10]

            :[2]
            pushi.e 0
            pop.v.i self.i

            :[3]
            push.v self.i
            pushi.e 10
            cmp.i.v LT
            bf [10]

            :[4]
            push.v self.a
            conv.v.b
            bf [6]

            :[5]
            b [8]

            :[6]
            b [8]

            :[7]
            b [8]

            :[8]
            popz.v
            exit.i

            :[9]
            b [3]

            :[10]
            popz.v
            """,
            """
            switch (a)
            {
                case 1:
                    for (i = 0; i < 10; exit)
                    {
                        if (a)
                        {
                        }
                        else
                        {
                            continue;
                            continue;
                        }
                    }
            }
            """,
            null,
            new Underanalyzer.Decompiler.DecompileSettings()
            {
                CleanupElseToContinue = false
            }
        );
    }

    [Fact]
    public void TestSwitchContinueEdgeCase3()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            push.v self.a
            dup.v 0
            pushi.e 1
            cmp.i.v EQ
            bt [2]

            :[1]
            b [9]

            :[2]
            pushi.e 0
            pop.v.i self.i

            :[3]
            push.v self.i
            pushi.e 10
            cmp.i.v LT
            bf [9]

            :[4]
            push.v self.a
            conv.v.b
            bf [7]

            :[5]
            b [7]

            :[6]
            b [7]

            :[7]
            popz.v
            exit.i

            :[8]
            b [3]

            :[9]
            popz.v
            """,
            """
            switch (a)
            {
                case 1:
                    for (i = 0; i < 10; exit)
                    {
                        if (a)
                        {
                            continue;
                        }
                        else
                        {
                        }
                    }
            }
            """
        );
    }

    [Fact]
    public void TestSwitchContinueEdgeCase4()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            push.v self.a
            conv.v.b
            bf [9]

            :[1]
            pushi.e 0
            pop.v.i self.i

            :[2]
            push.v self.i
            pushi.e 10
            cmp.i.v LT
            bf [9]

            :[3]
            push.v self.b
            conv.v.b
            bf [8]

            :[4]
            push.v self.c
            conv.v.b
            bf [7]

            :[5]
            pushi.e 123
            pop.v.i self.d
            b [9]

            :[6]
            b [8]

            :[7]
            pushi.e 456
            pop.v.i self.d

            :[8]
            push.v self.i
            push.e 1
            add.i.v
            pop.v.v self.i
            b [2]

            :[9]
            """,
            """
            if (a)
            {
                for (i = 0; i < 10; i++)
                {
                    if (b)
                    {
                        if (c)
                        {
                            d = 123;
                            break;
                        }
                        else
                        {
                            d = 456;
                        }
                    }
                }
            }
            """
        );
    }

    [Fact]
    public void TestSwitchContinueEdgeCase5()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            push.v self.a
            dup.v 0
            pushi.e 1
            cmp.i.v EQ
            bt [2]

            :[1]
            b [8]

            :[2]
            pushi.e 0
            pop.v.i self.i

            :[3]
            push.v self.i
            pushi.e 10
            cmp.i.v LT
            bf [8]

            :[4]
            b [6]

            :[5]
            push.v self.a
            conv.v.b
            bf [6]

            :[6]
            popz.v
            exit.i

            :[7]
            b [3]

            :[8]
            popz.v
            """,
            """
            switch (a)
            {
                case 1:
                    for (i = 0; i < 10; exit)
                    {
                        continue;
                        if (a)
                        {
                        }
                    }
            }
            """
        );
    }

    [Fact]
    public void TestSwitchContinueEdgeCase6()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            push.v self.a
            dup.v 0
            pushi.e 1
            cmp.i.v EQ
            bt [2]

            :[1]
            b [7]

            :[2]
            pushi.e 0
            pop.v.i self.i

            :[3]
            push.v self.i
            pushi.e 10
            cmp.i.v LT
            bf [7]

            :[4]
            b [5]

            :[5]
            popz.v
            exit.i

            :[6]
            b [3]

            :[7]
            popz.v
            """,
            """
            switch (a)
            {
                case 1:
                    for (i = 0; i < 10; exit)
                    {
                        continue;
                    }
            }
            """
        );
    }

    [Fact]
    public void TestSwitchContinueEdgeCase7()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            push.v self.a
            dup.v 0
            pushi.e 1
            cmp.i.v EQ
            bt [2]

            :[1]
            b [7]

            :[2]
            pushi.e 0
            pop.v.i self.i

            :[3]
            push.v self.i
            pushi.e 10
            cmp.i.v LT
            bf [7]

            :[4]
            b [5]

            :[5]
            popz.v
            exit.i

            :[6]
            b [3]

            :[7]
            popz.v
            """,
            """
            switch (a)
            {
                case 1:
                    for (i = 0; i < 10; exit)
                    {
                        continue;
                    }
            }
            """
        );
    }

    [Fact]
    public void TestSwitchContinueEdgeCase8()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            push.v self.a
            dup.v 0
            pushi.e 1
            cmp.i.v EQ
            bt [2]

            :[1]
            b [11]

            :[2]
            pushi.e 0
            pop.v.i self.i

            :[3]
            push.v self.i
            pushi.e 10
            cmp.i.v LT
            bf [11]

            :[4]
            push.v self.a
            conv.v.b
            bf [6]

            :[5]
            b [8]

            :[6]
            b [9]

            :[7]
            b [9]

            :[8]
            push.v self.b
            conv.v.b
            bf [9]

            :[9]
            popz.v
            exit.i

            :[10]
            b [3]

            :[11]
            popz.v
            """,
            """
            switch (a)
            {
                case 1:
                    for (i = 0; i < 10; exit)
                    {
                        if (a)
                        {
                        }
                        else
                        {
                            continue;
                            continue;
                        }
                        if (b)
                        {
                        }
                    }
            }
            """
        );
    }

    [Fact]
    public void TestSwitchContinueEdgeCase9()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            pushi.e 1
            dup.i 0
            push.i 0
            cmp.i.i LTE
            bt [5]

            :[1]
            pushi.e 1
            b [3]

            :[2]
            b [3]

            :[3]
            popz.i
            exit.i

            :[4]
            push.i 1
            sub.i.i
            dup.i 0
            conv.i.b
            bt [1]

            :[5]
            popz.i
            """,
            """
            repeat (1)
            {
                switch (1)
                {
                    default:
                }
                exit;
            }
            """);
    }

    [Fact]
    public void TestSwitchContinueEdgeCase10()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            pushi.e 1
            dup.i 0
            push.i 0
            cmp.i.i LTE
            bt [4]

            :[1]
            pushi.e 1
            b [2]

            :[2]
            popz.i
            exit.i

            :[3]
            push.i 1
            sub.i.i
            dup.i 0
            conv.i.b
            bt [1]

            :[4]
            popz.i
            """,
            """
            repeat (1)
            {
                switch (1)
                {
                }
                exit;
            }
            """);
    }
}
