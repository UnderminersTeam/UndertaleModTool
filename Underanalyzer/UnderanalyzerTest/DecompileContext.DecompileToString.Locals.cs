/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

namespace UnderanalyzerTest;

public class DecompileContext_DecompileToString_Locals
{
    [Fact]
    public void TestBasicLocal1()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            pushi.e 123
            pop.v.i local.a
            """,
            """
            var a = 123;
            """
        );
    }

    [Fact]
    public void TestUnassignedLocal1()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            pushloc.v local.a
            call.i show_debug_message 1
            popz.v
            """,
            """
            var a;
            show_debug_message(a);
            """
        );
    }

    [Fact]
    public void TestUnassignedLocal2()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            push.v self.b
            conv.v.b
            bf [2]

            :[1]
            pushloc.v local.a
            call.i show_debug_message 1
            popz.v

            :[2]
            """,
            """
            var a;
            if (b)
            {
                show_debug_message(a);
            }
            """
        );
    }

    [Fact]
    public void TestUnassignedLocal3()
    {
        // TODO: possibly change this case to be more obvious there's a mistake?
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            push.v self.b
            conv.v.b
            bf [2]

            :[1]
            pushloc.v local.a
            call.i show_debug_message 1
            popz.v
            b [3]

            :[2]
            pushi.e 1
            pop.v.i local.a

            :[3]
            """,
            """
            if (b)
            {
                show_debug_message(a);
            }
            else
            {
                var a = 1;
            }
            """
        );
    }

    [Fact]
    public void TestOnlyIncrementLocal()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            push.v local.a
            pushi.e 1
            add.i.v
            pop.v.v local.a
            """,
            """
            var a;
            a += 1;
            """
        );
    }

    [Fact]
    public void TestOnlyIncrementLocal2()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            push.v local.a
            pushi.e 1
            add.i.v
            pop.v.v local.a
            pushi.e -7
            pushi.e 0
            dup.i 1
            push.v [array]local.b
            pushi.e 1
            add.i.v
            pop.i.v [array]local.b
            """,
            """
            var a, b;
            a += 1;
            b[0] += 1;
            """
        );
    }

    [Fact]
    public void TestFunctionLocal1()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            pushi.e 123
            pop.v.i local.a
            b [2]

            > gml_Script_test (locals=2, args=0)
            :[1]
            pushi.e 456
            pop.v.i local.a
            exit.i

            :[2]
            push.i [function]gml_Script_test
            conv.i.v
            pushi.e -1
            conv.i.v
            call.i method 2
            dup.v 0
            pushi.e -6
            pop.v.v [stacktop]self.test
            popz.v
            """,
            """
            var a = 123;

            function test()
            {
                var a = 456;
            }
            """
        );
    }

    [Fact]
    public void TestFunctionLocal2()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            b [2]

            > gml_Script_test (locals=2, args=0)
            :[1]
            pushi.e 456
            pop.v.i local.a
            exit.i

            :[2]
            push.i [function]gml_Script_test
            conv.i.v
            pushi.e -1
            conv.i.v
            call.i method 2
            dup.v 0
            pushi.e -6
            pop.v.v [stacktop]self.test
            popz.v
            """,
            """
            function test()
            {
                var a = 456;
            }
            """
        );
    }

    [Fact]
    public void TestFunctionUnassignedLocal1()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            b [2]

            > gml_Script_test (locals=2, args=0)
            :[1]
            pushloc.v local.a
            call.i show_debug_message 1
            popz.v
            exit.i

            :[2]
            push.i [function]gml_Script_test
            conv.i.v
            pushi.e -1
            conv.i.v
            call.i method 2
            dup.v 0
            pushi.e -6
            pop.v.v [stacktop]self.test
            popz.v
            """,
            """
            function test()
            {
                var a;
                show_debug_message(a);
            }
            """
        );
    }

    [Fact]
    public void TestGeneral()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            pushi.e 123
            pop.v.i local.a
            pushloc.v local.a
            call.i show_debug_message 1
            popz.v
            pushi.e 456
            pop.v.i local.b
            pushloc.v local.b
            call.i show_debug_message 1
            popz.v
            push.v self.c
            conv.v.b
            bf [2]

            :[1]
            pushi.e 789
            pop.v.i local.b

            :[2]
            """,
            """
            var a = 123;
            show_debug_message(a);
            var b = 456;
            show_debug_message(b);
            if (c)
            {
                b = 789;
            }
            """
        );
    }

    [Fact]
    public void TestGeneral2()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            pushi.e 123
            pop.v.i self.a
            pushi.e 0
            pop.v.i local.i

            :[1]
            pushloc.v local.i
            pushi.e 10
            cmp.i.v LT
            bf [3]

            :[2]
            pushloc.v local.i
            call.i show_debug_message 1
            popz.v
            push.v local.i
            push.e 1
            add.i.v
            pop.v.v local.i
            b [1]

            :[3]
            pushi.e 0
            pop.v.i local.j

            :[4]
            pushloc.v local.j
            pushi.e 10
            cmp.i.v LT
            bf [6]

            :[5]
            pushloc.v local.j
            call.i show_debug_message 1
            popz.v
            push.v local.j
            push.e 1
            add.i.v
            pop.v.v local.j
            b [4]

            :[6]
            pushi.e 456
            pop.v.i local.k
            """,
            """
            a = 123;
            for (var i = 0; i < 10; i++)
            {
                show_debug_message(i);
            }
            for (var j = 0; j < 10; j++)
            {
                show_debug_message(j);
            }
            var k = 456;
            """
        );
    }

    [Fact]
    public void TestGeneral3()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            push.v self.a
            conv.v.b
            bf [2]

            :[1]
            pushi.e 0
            pop.v.i local.i
            b [3]

            :[2]
            pushi.e 1
            pop.v.i local.i

            :[3]
            push.v self.b
            conv.v.b
            bf [5]

            :[4]
            pushloc.v local.i
            pop.v.v self.c

            :[5]
            """,
            """
            var i;
            if (a)
            {
                i = 0;
            }
            else
            {
                i = 1;
            }
            if (b)
            {
                c = i;
            }
            """
        );
    }

    [Fact]
    public void TestGeneral4()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            pushi.e 123
            pop.v.i self.a
            pushi.e 0
            pop.v.i local.i

            :[1]
            pushloc.v local.i
            pushi.e 10
            cmp.i.v LT
            bf [3]

            :[2]
            pushloc.v local.i
            call.i show_debug_message 1
            popz.v
            push.v local.i
            push.e 1
            add.i.v
            pop.v.v local.i
            b [1]

            :[3]
            pushi.e 0
            pop.v.i local.j

            :[4]
            pushloc.v local.j
            pushi.e 10
            cmp.i.v LT
            bf [6]

            :[5]
            pushloc.v local.i
            call.i show_debug_message 1
            popz.v
            push.v local.j
            push.e 1
            add.i.v
            pop.v.v local.j
            b [4]

            :[6]
            pushi.e 456
            pop.v.i local.k
            """,
            """
            a = 123;
            var i;
            for (i = 0; i < 10; i++)
            {
                show_debug_message(i);
            }
            for (var j = 0; j < 10; j++)
            {
                show_debug_message(i);
            }
            var k = 456;
            """
        );
    }

    [Fact]
    public void TestMultiple1()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            push.v self.a
            conv.v.b
            bf [2]

            :[1]
            pushi.e 0
            pop.v.i local.i
            pushi.e 2
            pop.v.i local.j
            b [3]

            :[2]
            pushi.e 1
            pop.v.i local.i
            pushi.e 3
            pop.v.i local.j

            :[3]
            push.v self.b
            conv.v.b
            bf [5]

            :[4]
            pushloc.v local.i
            pop.v.v self.c
            pushloc.v local.j
            pop.v.v self.d

            :[5]
            """,
            """
            var i, j;
            if (a)
            {
                i = 0;
                j = 2;
            }
            else
            {
                i = 1;
                j = 3;
            }
            if (b)
            {
                c = i;
                d = j;
            }
            """
        );
    }

    [Fact]
    public void TestMultiple2()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            push.v self.a
            conv.v.b
            bf [2]

            :[1]
            pushi.e 0
            pop.v.i local.i
            pushi.e 2
            pop.v.i local.j
            b [3]

            :[2]
            pushi.e 1
            pop.v.i local.i

            :[3]
            push.v self.b
            conv.v.b
            bf [5]

            :[4]
            pushloc.v local.i
            pop.v.v self.c

            :[5]
            pushloc.v local.j
            pop.v.v self.d
            """,
            """
            var i, j;
            if (a)
            {
                i = 0;
                j = 2;
            }
            else
            {
                i = 1;
            }
            if (b)
            {
                c = i;
            }
            d = j;
            """
        );
    }

    [Fact]
    public void TestDoubleDeclare1()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            pushi.e 0
            pop.v.i local.i

            :[1]
            pushloc.v local.i
            pushi.e 10
            cmp.i.v LT
            bf [3]

            :[2]
            pushloc.v local.i
            call.i show_debug_message 1
            popz.v
            push.v local.i
            push.e 1
            add.i.v
            pop.v.v local.i
            b [1]

            :[3]
            pushi.e 0
            pop.v.i local.i

            :[4]
            pushloc.v local.i
            pushi.e 10
            cmp.i.v LT
            bf [6]

            :[5]
            pushloc.v local.i
            call.i show_debug_message 1
            popz.v
            push.v local.i
            push.e 1
            add.i.v
            pop.v.v local.i
            b [4]

            :[6]
            """,
            """
            for (var i = 0; i < 10; i++)
            {
                show_debug_message(i);
            }
            for (var i = 0; i < 10; i++)
            {
                show_debug_message(i);
            }
            """
        );
    }

    [Fact]
    public void TestDoubleDeclare2()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            push.v self.a
            conv.v.b
            bf [5]

            :[1]
            pushi.e 0
            pop.v.i local.i

            :[2]
            pushloc.v local.i
            pushi.e 10
            cmp.i.v LT
            bf [4]

            :[3]
            pushloc.v local.i
            call.i show_debug_message 1
            popz.v
            push.v local.i
            push.e 1
            add.i.v
            pop.v.v local.i
            b [2]

            :[4]
            b [8]

            :[5]
            pushi.e 0
            pop.v.i local.i

            :[6]
            pushloc.v local.i
            pushi.e 10
            cmp.i.v LT
            bf [8]

            :[7]
            pushloc.v local.i
            call.i show_debug_message 1
            popz.v
            push.v local.i
            push.e 1
            add.i.v
            pop.v.v local.i
            b [6]

            :[8]
            """,
            """
            if (a)
            {
                for (var i = 0; i < 10; i++)
                {
                    show_debug_message(i);
                }
            }
            else
            {
                for (var i = 0; i < 10; i++)
                {
                    show_debug_message(i);
                }
            }
            """
        );
    }

    [Fact]
    public void TestHoist1()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            push.v self.a
            conv.v.b
            bf [5]

            :[1]
            pushi.e 0
            pop.v.i local.i

            :[2]
            pushloc.v local.i
            pushi.e 10
            cmp.i.v LT
            bf [4]

            :[3]
            pushloc.v local.i
            call.i show_debug_message 1
            popz.v
            push.v local.i
            push.e 1
            add.i.v
            pop.v.v local.i
            b [2]

            :[4]
            b [8]

            :[5]
            pushi.e 0
            pop.v.i local.i

            :[6]
            pushloc.v local.i
            pushi.e 10
            cmp.i.v LT
            bf [8]

            :[7]
            pushloc.v local.i
            call.i show_debug_message 1
            popz.v
            push.v local.i
            push.e 1
            add.i.v
            pop.v.v local.i
            b [6]

            :[8]
            pushloc.v local.i
            call.i show_debug_message 1
            popz.v
            """,
            """
            var i;
            if (a)
            {
                for (i = 0; i < 10; i++)
                {
                    show_debug_message(i);
                }
            }
            else
            {
                for (i = 0; i < 10; i++)
                {
                    show_debug_message(i);
                }
            }
            show_debug_message(i);
            """
        );
    }

    [Fact]
    public void TestHoist2()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            pushi.e 123
            pop.v.i self.b
            push.v self.a
            conv.v.b
            bf [5]

            :[1]
            pushi.e 0
            pop.v.i local.i

            :[2]
            pushloc.v local.i
            pushi.e 10
            cmp.i.v LT
            bf [4]

            :[3]
            pushloc.v local.i
            call.i show_debug_message 1
            popz.v
            push.v local.i
            push.e 1
            add.i.v
            pop.v.v local.i
            b [2]

            :[4]
            b [8]

            :[5]
            pushi.e 0
            pop.v.i local.i

            :[6]
            pushloc.v local.i
            pushi.e 10
            cmp.i.v LT
            bf [8]

            :[7]
            pushloc.v local.i
            call.i show_debug_message 1
            popz.v
            push.v local.i
            push.e 1
            add.i.v
            pop.v.v local.i
            b [6]

            :[8]
            pushloc.v local.i
            call.i show_debug_message 1
            popz.v
            """,
            """
            b = 123;
            var i;
            if (a)
            {
                for (i = 0; i < 10; i++)
                {
                    show_debug_message(i);
                }
            }
            else
            {
                for (i = 0; i < 10; i++)
                {
                    show_debug_message(i);
                }
            }
            show_debug_message(i);
            """
        );
    }

    [Fact]
    public void TestHoist3()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            push.v self.c
            conv.v.b
            bf [10]

            :[1]
            pushi.e 123
            pop.v.i self.b
            push.v self.a
            conv.v.b
            bf [6]

            :[2]
            pushi.e 0
            pop.v.i local.i

            :[3]
            pushloc.v local.i
            pushi.e 10
            cmp.i.v LT
            bf [5]

            :[4]
            pushloc.v local.i
            call.i show_debug_message 1
            popz.v
            push.v local.i
            push.e 1
            add.i.v
            pop.v.v local.i
            b [3]

            :[5]
            b [9]

            :[6]
            pushi.e 0
            pop.v.i local.i

            :[7]
            pushloc.v local.i
            pushi.e 10
            cmp.i.v LT
            bf [9]

            :[8]
            pushloc.v local.i
            call.i show_debug_message 1
            popz.v
            push.v local.i
            push.e 1
            add.i.v
            pop.v.v local.i
            b [7]

            :[9]
            pushloc.v local.i
            call.i show_debug_message 1
            popz.v

            :[10]
            """,
            """
            if (c)
            {
                b = 123;
                var i;
                if (a)
                {
                    for (i = 0; i < 10; i++)
                    {
                        show_debug_message(i);
                    }
                }
                else
                {
                    for (i = 0; i < 10; i++)
                    {
                        show_debug_message(i);
                    }
                }
                show_debug_message(i);
            }
            """
        );
    }

    [Fact]
    public void TestHoist4()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            push.v self.c
            conv.v.b
            bf [5]

            :[1]
            pushi.e 123
            pop.v.i self.b
            push.v self.a
            conv.v.b
            bf [3]

            :[2]
            pushi.e 123
            pop.v.i local.i
            b [4]

            :[3]
            pushi.e 456
            pop.v.i local.i

            :[4]
            pushloc.v local.i
            call.i show_debug_message 1
            popz.v

            :[5]
            """,
            """
            if (c)
            {
                b = 123;
                var i;
                if (a)
                {
                    i = 123;
                }
                else
                {
                    i = 456;
                }
                show_debug_message(i);
            }
            """
        );
    }

    [Fact]
    public void TestHoist5()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            push.v self.c
            conv.v.b
            bf [2]

            :[1]
            b [7]

            :[2]
            pushi.e 0
            pop.v.i self.j
            push.v self.a
            conv.v.b
            bf [4]

            :[3]
            pushi.e 1
            pop.v.i local.i
            b [7]

            :[4]
            pushi.e 1
            pop.v.i self.j
            push.v self.b
            conv.v.b
            bf [6]

            :[5]
            b [7]

            :[6]
            pushloc.v local.i
            pop.v.v self.a

            :[7]
            """,
            """
            if (c)
            {
            }
            else
            {
                j = 0;
                var i;
                if (a)
                {
                    i = 1;
                }
                else
                {
                    j = 1;
                    if (b)
                    {
                    }
                    else
                    {
                        a = i;
                    }
                }
            }
            """
        );
    }

    [Fact]
    public void TestHoist6()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            push.v self.c
            conv.v.b
            bf [2]

            :[1]
            b [7]

            :[2]
            push.v self.a
            conv.v.b
            bf [4]

            :[3]
            pushi.e 1
            pop.v.i local.i
            b [7]

            :[4]
            push.v self.b
            conv.v.b
            bf [6]

            :[5]
            b [7]

            :[6]
            pushloc.v local.i
            pop.v.v self.a

            :[7]
            """,
            """
            if (c)
            {
            }
            else
            {
                var i;
                if (a)
                {
                    i = 1;
                }
                else if (b)
                {
                }
                else
                {
                    a = i;
                }
            }
            """
        );
    }

    [Fact]
    public void TestDoubleHoist()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            push.v self.d
            conv.v.b
            bf [2]

            :[1]
            b [10]

            :[2]
            pushi.e -1
            pop.v.i self.j
            push.v self.c
            conv.v.b
            bf [4]

            :[3]
            b [9]

            :[4]
            pushi.e 0
            pop.v.i self.j
            push.v self.a
            conv.v.b
            bf [6]

            :[5]
            pushi.e 1
            pop.v.i local.i
            b [9]

            :[6]
            pushi.e 1
            pop.v.i self.j
            push.v self.b
            conv.v.b
            bf [8]

            :[7]
            b [9]

            :[8]
            pushloc.v local.i
            pop.v.v self.a

            :[9]
            pushloc.v local.i
            pop.v.v self.b

            :[10]
            """,
            """
            if (d)
            {
            }
            else
            {
                j = -1;
                var i;
                
                if (c)
                {
                }
                else
                {
                    j = 0;
                    
                    if (a)
                    {
                        i = 1;
                    }
                    else
                    {
                        j = 1;
                        
                        if (b)
                        {
                        }
                        else
                        {
                            a = i;
                        }
                    }
                }
                
                b = i;
            }
            """,
            null,
            new Underanalyzer.Decompiler.DecompileSettings()
            {
                EmptyLineAroundBranchStatements = true
            }
        );
    }

    [Fact]
    public void TestSwitch1()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            push.v self.a
            dup.v 0
            pushi.e 123
            cmp.i.v EQ
            bt [2]

            :[1]
            b [3]

            :[2]
            pushi.e 456
            pop.v.i local.i
            b [3]

            :[3]
            popz.v
            pushloc.v local.i
            call.i show_debug_message 1
            popz.v
            """,
            """
            var i;
            switch (a)
            {
                case 123:
                    i = 456;
                    break;
            }
            show_debug_message(i);
            """
        );
    }

    [Fact]
    public void TestSwitch2()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            push.v self.b
            conv.v.b
            bf [11]

            :[1]
            push.v self.a
            dup.v 0
            pushi.e 1
            cmp.i.v EQ
            bt [5]

            :[2]
            dup.v 0
            pushi.e 2
            cmp.i.v EQ
            bt [6]

            :[3]
            dup.v 0
            pushi.e 3
            cmp.i.v EQ
            bt [7]

            :[4]
            b [10]

            :[5]
            pushi.e 123
            pop.v.i local.i
            b [10]

            :[6]
            pushi.e 456
            pop.v.i local.i
            b [9]

            :[7]
            pushi.e 789
            pop.v.i local.i
            b [10]

            :[8]
            b [10]

            :[9]
            popz.v
            b [0]

            :[10]
            popz.v
            b [0]

            :[11]
            """,
            """
            while (b)
            {
                switch (a)
                {
                    case 1:
                        var i = 123;
                        break;
                    case 2:
                        var i = 456;
                        continue;
                    case 3:
                        var i = 789;
                        break;
                }
            }
            """
        );
    }

    [Fact]
    public void TestSwitch3()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            push.v self.a
            dup.v 0
            pushi.e 123
            cmp.i.v EQ
            bt [4]

            :[1]
            dup.v 0
            pushi.e 456
            cmp.i.v EQ
            bt [5]

            :[2]
            dup.v 0
            pushi.e 789
            cmp.i.v EQ
            bt [6]

            :[3]
            b [7]

            :[4]
            pushi.e 0
            pop.v.i local.i

            :[5]
            pushi.e 1
            pop.v.i local.i
            b [7]

            :[6]
            pushi.e 2
            pop.v.i local.i

            :[7]
            popz.v
            """,
            """
            switch (a)
            {
                case 123:
                    var i = 0;
                case 456:
                    i = 1;
                    break;
                case 789:
                    var i = 2;
            }
            """
        );
    }

    [Fact]
    public void TestSwitch4()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            pushi.e 0

            pop.v.i local.a
            push.v self.b
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
            pushi.e 1
            pop.v.i local.a
            b [5]

            :[4]
            pushi.e 2
            pop.v.i local.a
            b [5]

            :[5]
            popz.v
            pushloc.v local.a
            call.i show_debug_message 1
            popz.v
            """,
            """
            var a = 0;
            switch (b)
            {
                case 1:
                    a = 1;
                    break;
                case 2:
                    a = 2;
                    break;
            }
            show_debug_message(a);
            """
        );
    }

    [Fact]
    public void TestSwitch5()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            push.v self.c
            dup.v 0
            pushi.e 1
            cmp.i.v EQ
            bt [6]

            :[1]
            dup.v 0
            pushi.e 2
            cmp.i.v EQ
            bt [7]

            :[2]
            dup.v 0
            pushi.e 3
            cmp.i.v EQ
            bt [8]

            :[3]
            dup.v 0
            pushi.e 4
            cmp.i.v EQ
            bt [9]

            :[4]
            b [10]

            :[5]
            b [11]

            :[6]
            pushi.e 1
            pop.v.i local.a
            b [11]

            :[7]
            pushi.e 2
            pop.v.i local.a
            b [11]

            :[8]
            pushi.e 3
            pop.v.i local.a
            b [11]

            :[9]
            pushi.e 4
            pop.v.i local.a
            b [11]

            :[10]
            pushi.e 5
            pop.v.i local.a
            b [11]

            :[11]
            popz.v
            pushloc.v local.a
            pop.v.v self.b
            """,
            """
            var a;
            switch (c)
            {
                case 1:
                    a = 1;
                    break;
                case 2:
                    a = 2;
                    break;
                case 3:
                    a = 3;
                    break;
                case 4:
                    a = 4;
                    break;
                default:
                    a = 5;
                    break;
            }
            b = a;
            """
        );
    }

    [Fact]
    public void TestStruct()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            pushi.e 123
            pop.v.i local.a
            pushloc.v local.a
            b [2]
            
            > gml_Script____struct___Test (locals=0, args=0)
            :[1]
            pushi.e -15
            pushi.e 0
            push.v [array]self.argument
            pop.v.v self.c
            exit.i
            
            :[2]
            push.i [function]gml_Script____struct___Test
            conv.i.v
            call.i @@NullObject@@ 0
            call.i method 2
            dup.v 0
            pushi.e -16
            pop.v.v [stacktop]static.___struct___Test
            call.i @@NewGMLObject@@ 2
            pop.v.v self.b
            """,
            """
            var a = 123;
            b = 
            {
                c: a
            };
            """
        );
    }

    [Fact]
    public void TestStructHoist()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            push.v self.d
            conv.v.b
            bf [2]

            :[1]
            pushi.e 123
            pop.v.i local.a

            :[2]
            pushloc.v local.a
            b [4]

            > gml_Script____struct___Test (locals=2, args=0)
            :[3]
            pushi.e -15
            pushi.e 0
            push.v [array]self.argument
            pop.v.v self.c
            exit.i

            :[4]
            push.i [function]gml_Script____struct___Test
            conv.i.v
            call.i @@NullObject@@ 0
            call.i method 2
            dup.v 0
            pushi.e -16
            pop.v.v [stacktop]static.___struct___Test
            call.i @@NewGMLObject@@ 2
            pop.v.v self.b
            """,
            """
            var a;
            if (d)
            {
                a = 123;
            }
            b = 
            {
                c: a
            };
            """
        );
    }

    [Fact]
    public void TestArray()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            pushi.e 1
            conv.i.v
            pushi.e -7
            pushi.e 0
            pop.v.v [array]self.a
            """,
            """
            var a;
            a[0] = 1;
            """
        );
    }

    // TODO: hoist4 with ideal spacing?
    // TODO: settings alternate versions for all of the above cases
    // TODO: nested struct test
}