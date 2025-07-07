/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using Underanalyzer.Decompiler;
using Underanalyzer.Mock;

namespace UnderanalyzerTest;

public class DecompileContext_DecompileToString
{
    [Fact]
    public void TestBasic()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            pushi.e 123
            pop.v.i self.a
            push.v self.b
            conv.v.b
            bf [2]

            :[1]
            push.s "B is true"
            pop.v.s self.msg
            b [7]

            :[2]
            push.v self.c
            conv.v.b
            bf [4]

            :[3]
            push.v self.d
            conv.v.b
            b [5]

            :[4]
            push.e 0

            :[5]
            bf [7]

            :[6]
            push.s "C and D are both true"
            pop.v.s self.msg

            :[7]
            """,
            """
            a = 123;
            if (b)
            {
                msg = "B is true";
            }
            else if (c && d)
            {
                msg = "C and D are both true";
            }
            """
        );
    }

    [Fact]
    public void TestWhileIfElseEmpty()
    {
        TestUtil.VerifyDecompileResult(
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
            b [0]

            :[4]
            """,
            """
            while (a)
            {
                if (b)
                {
                }
                else
                {
                }
            }
            """
        );
    }

    [Fact]
    public void TestNestedDoUntil()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            push.v self.c
            push.v self.d
            add.v.v
            pushi.e 2
            conv.i.d
            div.d.v
            pop.v.v self.b
            push.v self.b
            pushi.e 200
            cmp.i.v GT
            bf [0]

            :[1]
            push.v self.a
            pushi.e 1
            add.i.v
            pop.v.v self.a
            push.v self.a
            pushi.e 100
            cmp.i.v GT
            bf [0]
            """,
            """
            do
            {
                do
                {
                    b = (c + d) / 2;
                }
                until (b > 200);
                a += 1;
            }
            until (a > 100);
            """
        );
    }

    [Fact]
    public void TestBasicSwitch()
    {
        TestUtil.VerifyDecompileResult(
            """
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
            bt [7]

            :[2]
            dup.v 0
            pushi.e 3
            cmp.i.v EQ
            bt [7]

            :[3]
            b [6]

            :[4]
            b [8]

            :[5]
            push.s "Case 1"
            pop.v.s self.msg
            b [8]

            :[6]
            push.s "Default"
            pop.v.s self.msg
            b [8]

            :[7]
            push.s "Case 2 and 3"
            pop.v.s self.msg
            b [8]

            :[8]
            popz.v
            """,
            """
            switch (a)
            {
                case 1:
                    msg = "Case 1";
                    break;
                default:
                    msg = "Default";
                    break;
                case 2:
                case 3:
                    msg = "Case 2 and 3";
                    break;
            }
            """
        );
    }

    [Fact]
    public void TestPrePostfix()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            push.v self.b
            dup.v 0
            push.e 1
            add.i.v
            pop.v.v self.b
            pop.v.v self.a
            push.v self.b
            push.e 1
            add.i.v
            dup.v 0
            pop.v.v self.b
            pop.v.v self.a
            push.v self.b
            conv.v.i
            push.v [stacktop]self.c
            conv.v.i
            dup.i 0
            push.v [stacktop]self.d
            dup.v 0
            pop.e.v 5
            push.e 1
            add.i.v
            pop.i.v [stacktop]self.d
            pop.v.v self.a
            push.v self.b
            conv.v.i
            push.v [stacktop]self.c
            conv.v.i
            dup.i 0
            push.v [stacktop]self.d
            push.e 1
            add.i.v
            dup.v 0
            pop.e.v 5
            pop.i.v [stacktop]self.d
            pop.v.v self.a
            pushi.e -1
            pushi.e 0
            dup.l 0
            push.v [array]self.b
            dup.v 0
            pop.e.v 6
            push.e 1
            add.i.v
            pop.i.v [array]self.b
            pop.v.v self.a
            pushi.e -1
            pushi.e 0
            dup.l 0
            push.v [array]self.b
            push.e 1
            add.i.v
            dup.v 0
            pop.e.v 6
            pop.i.v [array]self.b
            pop.v.v self.a
            push.v self.a
            conv.v.i
            push.v [stacktop]self.b
            conv.v.i
            pushi.e 0
            dup.l 0
            push.v [array]self.c
            dup.v 0
            pop.e.v 6
            push.e 1
            add.i.v
            pop.i.v [array]self.c
            pop.v.v self.a
            push.v self.a
            conv.v.i
            push.v [stacktop]self.b
            conv.v.i
            pushi.e 0
            dup.l 0
            push.v [array]self.c
            push.e 1
            add.i.v
            dup.v 0
            pop.e.v 6
            pop.i.v [array]self.c
            pop.v.v self.a
            push.v self.a
            conv.v.i
            pushi.e 0
            push.v [array]self.b
            conv.v.i
            dup.i 0
            push.v [stacktop]self.c
            dup.v 0
            pop.e.v 5
            push.e 1
            add.i.v
            pop.i.v [stacktop]self.c
            pop.v.v self.a
            push.v self.a
            conv.v.i
            pushi.e 0
            push.v [array]self.b
            conv.v.i
            dup.i 0
            push.v [stacktop]self.c
            push.e 1
            add.i.v
            dup.v 0
            pop.e.v 5
            pop.i.v [stacktop]self.c
            pop.v.v self.a
            push.v self.a
            conv.v.i
            pushi.e 0
            push.v [array]self.b
            conv.v.i
            pushi.e 0
            dup.l 0
            push.v [array]self.c
            dup.v 0
            pop.e.v 6
            push.e 1
            add.i.v
            pop.i.v [array]self.c
            pop.v.v self.a
            push.v self.a
            conv.v.i
            pushi.e 0
            push.v [array]self.b
            conv.v.i
            pushi.e 0
            dup.l 0
            push.v [array]self.c
            push.e 1
            add.i.v
            dup.v 0
            pop.e.v 6
            pop.i.v [array]self.c
            pop.v.v self.a
            pushi.e -1
            push.v self.c
            dup.v 0
            push.e 1
            add.i.v
            pop.v.v self.c
            conv.v.i
            dup.l 0
            push.v [array]self.b
            dup.v 0
            pop.e.v 6
            push.e 1
            add.i.v
            pop.i.v [array]self.b
            pop.v.v self.a
            pushi.e -1
            push.v self.c
            push.e 1
            add.i.v
            dup.v 0
            pop.v.v self.c
            conv.v.i
            dup.l 0
            push.v [array]self.b
            dup.v 0
            pop.e.v 6
            push.e 1
            add.i.v
            pop.i.v [array]self.b
            pop.v.v self.a
            pushi.e -1
            push.v self.c
            push.e 1
            add.i.v
            dup.v 0
            pop.v.v self.c
            conv.v.i
            dup.l 0
            push.v [array]self.b
            push.e 1
            add.i.v
            dup.v 0
            pop.e.v 6
            pop.i.v [array]self.b
            pop.v.v self.a
            pushi.e -1
            push.v self.c
            dup.v 0
            push.e 1
            add.i.v
            pop.v.v self.c
            conv.v.i
            dup.l 0
            push.v [array]self.b
            push.e 1
            add.i.v
            dup.v 0
            pop.e.v 6
            pop.i.v [array]self.b
            pop.v.v self.a
            pushi.e -1
            pushi.e -1
            pushi.e 0
            dup.l 0
            push.v [array]self.c
            dup.v 0
            pop.e.v 6
            push.e 1
            add.i.v
            pop.i.v [array]self.c
            conv.v.i
            dup.l 0
            push.v [array]self.b
            dup.v 0
            pop.e.v 6
            push.e 1
            add.i.v
            pop.i.v [array]self.b
            pop.v.v self.a
            pushi.e -1
            pushi.e -1
            pushi.e 0
            dup.l 0
            push.v [array]self.c
            push.e 1
            add.i.v
            dup.v 0
            pop.e.v 6
            pop.i.v [array]self.c
            conv.v.i
            dup.l 0
            push.v [array]self.b
            dup.v 0
            pop.e.v 6
            push.e 1
            add.i.v
            pop.i.v [array]self.b
            pop.v.v self.a
            pushi.e -1
            pushi.e -1
            pushi.e 0
            dup.l 0
            push.v [array]self.c
            push.e 1
            add.i.v
            dup.v 0
            pop.e.v 6
            pop.i.v [array]self.c
            conv.v.i
            dup.l 0
            push.v [array]self.b
            push.e 1
            add.i.v
            dup.v 0
            pop.e.v 6
            pop.i.v [array]self.b
            pop.v.v self.a
            pushi.e -1
            pushi.e -1
            pushi.e 0
            dup.l 0
            push.v [array]self.c
            dup.v 0
            pop.e.v 6
            push.e 1
            add.i.v
            pop.i.v [array]self.c
            conv.v.i
            dup.l 0
            push.v [array]self.b
            push.e 1
            add.i.v
            dup.v 0
            pop.e.v 6
            pop.i.v [array]self.b
            pop.v.v self.a
            """,
            """
            a = b++;
            a = ++b;
            a = b.c.d++;
            a = ++b.c.d;
            a = b[0]++;
            a = ++b[0];
            a = a.b.c[0]++;
            a = ++a.b.c[0];
            a = a.b[0].c++;
            a = ++a.b[0].c;
            a = a.b[0].c[0]++;
            a = ++a.b[0].c[0];
            a = b[c++]++;
            a = b[++c]++;
            a = ++b[++c];
            a = ++b[c++];
            a = b[c[0]++]++;
            a = b[++c[0]]++;
            a = ++b[++c[0]];
            a = ++b[c[0]++];
            """
        );
    }

    [Fact]
    public void TestPrePostfix_GMLv2()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            push.v self.b
            dup.v 0
            push.e 1
            add.i.v
            pop.v.v self.b
            pop.v.v self.a
            push.v self.b
            push.e 1
            add.i.v
            dup.v 0
            pop.v.v self.b
            pop.v.v self.a
            push.v self.b
            pushi.e -9
            push.v [stacktop]self.c
            pushi.e -9
            dup.i 4
            push.v [stacktop]self.d
            dup.v 0
            dup.i 4 9
            push.e 1
            add.i.v
            pop.i.v [stacktop]self.d
            pop.v.v self.a
            push.v self.b
            pushi.e -9
            push.v [stacktop]self.c
            pushi.e -9
            dup.i 4
            push.v [stacktop]self.d
            push.e 1
            add.i.v
            dup.v 0
            dup.i 4 9
            pop.i.v [stacktop]self.d
            pop.v.v self.a
            pushi.e -1
            pushi.e 0
            dup.i 1
            push.v [array]self.b
            dup.v 0
            dup.i 4 6
            push.e 1
            add.i.v
            pop.i.v [array]self.b
            pop.v.v self.a
            pushi.e -1
            pushi.e 0
            dup.i 1
            push.v [array]self.b
            push.e 1
            add.i.v
            dup.v 0
            dup.i 4 6
            pop.i.v [array]self.b
            pop.v.v self.a
            push.v self.a
            pushi.e -9
            push.v [stacktop]self.b
            pushi.e -9
            pushi.e 0
            dup.i 5
            push.v [array]self.c
            dup.v 0
            dup.i 4 10
            push.e 1
            add.i.v
            pop.i.v [array]self.c
            pop.v.v self.a
            push.v self.a
            pushi.e -9
            push.v [stacktop]self.b
            pushi.e -9
            pushi.e 0
            dup.i 5
            push.v [array]self.c
            push.e 1
            add.i.v
            dup.v 0
            dup.i 4 10
            pop.i.v [array]self.c
            pop.v.v self.a
            push.v self.a
            pushi.e -9
            pushi.e 0
            push.v [array]self.b
            pushi.e -9
            dup.i 4
            push.v [stacktop]self.c
            dup.v 0
            dup.i 4 9
            push.e 1
            add.i.v
            pop.i.v [stacktop]self.c
            pop.v.v self.a
            push.v self.a
            pushi.e -9
            pushi.e 0
            push.v [array]self.b
            pushi.e -9
            dup.i 4
            push.v [stacktop]self.c
            push.e 1
            add.i.v
            dup.v 0
            dup.i 4 9
            pop.i.v [stacktop]self.c
            pop.v.v self.a
            push.v self.a
            pushi.e -9
            pushi.e 0
            push.v [array]self.b
            pushi.e -9
            pushi.e 0
            dup.i 5
            push.v [array]self.c
            dup.v 0
            dup.i 4 10
            push.e 1
            add.i.v
            pop.i.v [array]self.c
            pop.v.v self.a
            push.v self.a
            pushi.e -9
            pushi.e 0
            push.v [array]self.b
            pushi.e -9
            pushi.e 0
            dup.i 5
            push.v [array]self.c
            push.e 1
            add.i.v
            dup.v 0
            dup.i 4 10
            pop.i.v [array]self.c
            pop.v.v self.a
            pushi.e -1
            push.v self.c
            dup.v 0
            push.e 1
            add.i.v
            pop.v.v self.c
            conv.v.i
            dup.i 1
            push.v [array]self.b
            dup.v 0
            dup.i 4 6
            push.e 1
            add.i.v
            pop.i.v [array]self.b
            pop.v.v self.a
            pushi.e -1
            push.v self.c
            push.e 1
            add.i.v
            dup.v 0
            pop.v.v self.c
            conv.v.i
            dup.i 1
            push.v [array]self.b
            dup.v 0
            dup.i 4 6
            push.e 1
            add.i.v
            pop.i.v [array]self.b
            pop.v.v self.a
            pushi.e -1
            push.v self.c
            push.e 1
            add.i.v
            dup.v 0
            pop.v.v self.c
            conv.v.i
            dup.i 1
            push.v [array]self.b
            push.e 1
            add.i.v
            dup.v 0
            dup.i 4 6
            pop.i.v [array]self.b
            pop.v.v self.a
            pushi.e -1
            push.v self.c
            dup.v 0
            push.e 1
            add.i.v
            pop.v.v self.c
            conv.v.i
            dup.i 1
            push.v [array]self.b
            push.e 1
            add.i.v
            dup.v 0
            dup.i 4 6
            pop.i.v [array]self.b
            pop.v.v self.a
            pushi.e -1
            pushi.e -1
            pushi.e 0
            dup.i 1
            push.v [array]self.c
            dup.v 0
            dup.i 4 6
            push.e 1
            add.i.v
            pop.i.v [array]self.c
            conv.v.i
            dup.i 1
            push.v [array]self.b
            dup.v 0
            dup.i 4 6
            push.e 1
            add.i.v
            pop.i.v [array]self.b
            pop.v.v self.a
            pushi.e -1
            pushi.e -1
            pushi.e 0
            dup.i 1
            push.v [array]self.c
            push.e 1
            add.i.v
            dup.v 0
            dup.i 4 6
            pop.i.v [array]self.c
            conv.v.i
            dup.i 1
            push.v [array]self.b
            dup.v 0
            dup.i 4 6
            push.e 1
            add.i.v
            pop.i.v [array]self.b
            pop.v.v self.a
            pushi.e -1
            pushi.e -1
            pushi.e 0
            dup.i 1
            push.v [array]self.c
            push.e 1
            add.i.v
            dup.v 0
            dup.i 4 6
            pop.i.v [array]self.c
            conv.v.i
            dup.i 1
            push.v [array]self.b
            push.e 1
            add.i.v
            dup.v 0
            dup.i 4 6
            pop.i.v [array]self.b
            pop.v.v self.a
            pushi.e -1
            pushi.e -1
            pushi.e 0
            dup.i 1
            push.v [array]self.c
            dup.v 0
            dup.i 4 6
            push.e 1
            add.i.v
            pop.i.v [array]self.c
            conv.v.i
            dup.i 1
            push.v [array]self.b
            push.e 1
            add.i.v
            dup.v 0
            dup.i 4 6
            pop.i.v [array]self.b
            pop.v.v self.a
            """,
            """
            a = b++;
            a = ++b;
            a = b.c.d++;
            a = ++b.c.d;
            a = b[0]++;
            a = ++b[0];
            a = a.b.c[0]++;
            a = ++a.b.c[0];
            a = a.b[0].c++;
            a = ++a.b[0].c;
            a = a.b[0].c[0]++;
            a = ++a.b[0].c[0];
            a = b[c++]++;
            a = b[++c]++;
            a = ++b[++c];
            a = ++b[c++];
            a = b[c[0]++]++;
            a = b[++c[0]]++;
            a = ++b[++c[0]];
            a = ++b[c[0]++];
            """
        );
    }

    [Fact]
    public void TestNullishTernary()
    {
        TestUtil.VerifyDecompileResult(
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
            """,
            """
            a ??= b ? c : d;
            """
        );
    }

    [Fact]
    public void TestMultiWithBreak()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            push.v self.a
            conv.v.b
            bf [10]

            :[1]
            push.v self.b
            pushi.e -9
            pushenv [6]

            :[2]
            push.v self.c
            pushi.e -9
            pushenv [4]

            :[3]
            pushi.e 1
            pop.v.i self.d

            :[4]
            popenv [3]

            :[5]
            pushi.e 1
            pop.v.i self.e
            b [8]

            :[6]
            popenv [2]

            :[7]
            b [9]

            :[8]
            popenv <drop>

            :[9]
            b [12]

            :[10]
            push.v self.f
            pushi.e -9
            pushenv [11]

            :[11]
            popenv [11]

            :[12]
            """,
            """
            if (a)
            {
                with (b)
                {
                    with (c)
                    {
                        d = 1;
                    }
                    e = 1;
                    break;
                }
            }
            else
            {
                with (f)
                {
                }
            }
            """
        );
    }

    [Fact]
    public void TestMultiWithBreak2()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            push.v self.a
            conv.v.b
            bf [10]

            :[1]
            push.v self.b
            pushi.e -9
            pushenv [6]

            :[2]
            push.v self.c
            pushi.e -9
            pushenv [4]

            :[3]
            pushi.e 1
            pop.v.i self.d

            :[4]
            popenv [3]

            :[5]
            pushi.e 1
            pop.v.i self.e
            b [8]

            :[6]
            popenv [2]
            
            :[7]
            b [9]

            :[8]
            popenv <drop>

            :[9]
            b [18]

            :[10]
            push.v self.b
            pushi.e -9
            pushenv [15]

            :[11]
            push.v self.c
            pushi.e -9
            pushenv [13]

            :[12]
            pushi.e 1
            pop.v.i self.d

            :[13]
            popenv [12]
            
            :[14]
            pushi.e 1
            pop.v.i self.e
            b [17]

            :[15]
            popenv [11]
            
            :[16]
            b [18]

            :[17]
            popenv <drop>

            :[18]
            """,
            """
            if (a)
            {
                with (b)
                {
                    with (c)
                    {
                        d = 1;
                    }
                    e = 1;
                    break;
                }
            }
            else
            {
                with (b)
                {
                    with (c)
                    {
                        d = 1;
                    }
                    e = 1;
                    break;
                }
            }
            """
        );
    }

    [Fact]
    public void TestIfElseWithBreak()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            push.v self.a
            conv.v.b
            bf [2]

            :[1]
            b [7]

            :[2]
            push.v self.b
            pushi.e -9
            pushenv [4]

            :[3]
            b [6]

            :[4]
            popenv [3]

            :[5]
            b [7]

            :[6]
            popenv <drop>

            :[7]
            """,
            """
            if (a)
            {
            }
            else
            {
                with (b)
                {
                    break;
                }
            }
            """
        );
    }

    [Fact]
    public void TestSwitchIfShortCircuit()
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
            b [3]

            :[2]
            b [3]

            :[3]
            popz.v
            push.v self.b
            conv.v.b
            bf [5]

            :[4]
            push.v self.c
            conv.v.b
            b [6]

            :[5]
            push.e 0

            :[6]
            bf [7]

            :[7]
            """,
            """
            switch (a)
            {
                case 1:
                    break;
            }
            if (b && c)
            {
            }
            """
        );
    }

    [Fact]
    public void TestMultiArrays()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            exit.i

            :[1]
            pushi.e -1
            pushi.e 0
            push.v [multipush]self.array
            pushi.e 1
            pushac.e
            pushi.e 2
            pushaf.e
            pop.v.v self.basic_push_multi
            exit.i

            :[2]
            pushi.e 3
            conv.i.v
            pushi.e -1
            pushi.e 0
            push.v [multipushpop]self.basic_pop_multi
            pushi.e 1
            pushac.e
            pushi.e 2
            popaf.e
            exit.i

            :[3]
            pushi.e -1
            pushi.e 0
            push.v [multipushpop]self.prefix_multi
            pushi.e 1
            pushac.e
            pushi.e 2
            dup.i 4
            pushaf.e
            push.e 1
            add.i.v
            dup.i 4 5
            popaf.e
            exit.i

            :[4]
            pushi.e -1
            pushi.e 0
            push.v [multipushpop]self.postfix_multi
            pushi.e 1
            pushac.e
            pushi.e 2
            dup.i 4
            pushaf.e
            push.e 1
            add.i.v
            dup.i 4 5

            popaf.e
            exit.i

            :[5]
            pushi.e -1
            pushi.e 0
            push.v [multipushpop]self.compound_multi
            pushi.e 1
            pushac.e
            pushi.e 2
            dup.i 4
            savearef.e
            pushaf.e
            pushi.e 3
            add.i.v
            restorearef.e
            dup.i 4 5
            popaf.e
            exit.i

            :[6]
            pushi.e -1
            pushi.e 0
            push.v [multipushpop]self.prefix_multi
            pushi.e 1
            pushac.e
            pushi.e 2
            dup.i 4
            pushaf.e
            push.e 1
            add.i.v
            dup.v 0
            dup.i 4 9
            dup.i 4 5
            popaf.e
            pop.v.v self.a
            exit.i

            :[7]
            pushi.e -1
            pushi.e 0
            push.v [multipushpop]self.postfix_multi
            pushi.e 1
            pushac.e
            pushi.e 2
            dup.i 4
            pushaf.e
            dup.v 0
            dup.i 4 9
            push.e 1
            add.i.v
            dup.i 4 5
            popaf.e
            pop.v.v self.a
            exit.i
            """,
            """
            exit;
            basic_push_multi = array[0][1][2];
            exit;
            basic_pop_multi[0][1][2] = 3;
            exit;
            prefix_multi[0][1][2]++;
            exit;
            postfix_multi[0][1][2]++;
            exit;
            compound_multi[0][1][2] += 3;
            exit;
            a = ++prefix_multi[0][1][2];
            exit;
            a = postfix_multi[0][1][2]++;
            exit;
            """
        );
    }

    [Fact]
    public void TestSwitchReturn()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            push.v self.a
            dup.v 0
            pushi.e 0
            cmp.i.v EQ
            bt [2]

            :[1]
            b [3]

            :[2]
            b [3]

            :[3]
            popz.v
            call.i game_end 0
            popz.v
            push.v self.b
            ret.v
            """,
            """
            switch (a)
            {
                case 0:
                    break;
            }
            game_end();
            return b;
            """
        );
    }

    [Fact]
    public void TestNestedSwitchThenExit()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            push.v self.a
            dup.v 0
            pushi.e 0
            cmp.i.v EQ
            bt [2]

            :[1]
            b [8]

            :[2]
            push.v self.b
            conv.v.b
            bf [7]

            :[3]
            push.v self.c
            dup.v 0
            pushi.e 0
            cmp.i.v EQ
            bt [5]

            :[4]
            b [6]

            :[5]
            b [6]

            :[6]
            popz.v
            pushi.e 0
            pop.v.i self.d
            popz.v
            exit.i

            :[7]
            b [8]

            :[8]
            popz.v
            """,
            """
            switch (a)
            {
                case 0:
                    if (b)
                    {
                        switch (c)
                        {
                            case 0:
                                break;
                        }
                        d = 0;
                        exit;
                    }
                    break;
            }
            """
        );
    }

    [Fact]
    public void TestNestedSwitchExitInFragment()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            push.v self.a
            dup.v 0
            pushi.e 0
            cmp.i.v EQ
            bt [2]

            :[1]
            b [9]

            :[2]
            b [8]

            > inner_fragment (locals=0, args=0)
            :[3]
            push.v self.b
            dup.v 0
            pushi.e 0
            cmp.i.v EQ
            bt [5]

            :[4]
            b [6]

            :[5]
            b [6]

            :[6]
            popz.v
            exit.i

            :[7]
            exit.i

            :[8]
            push.i [function]inner_fragment
            conv.i.v
            pushi.e -1
            conv.i.v
            call.i method 2
            pop.v.v self.func
            b [9]

            :[9]
            popz.v
            """,
            """
            switch (a)
            {
                case 0:
                    func = function()
                    {
                        switch (b)
                        {
                            case 0:
                                break;
                        }
                        exit;
                    };
                    
                    break;
            }
            """
        );
    }

    [Fact]
    public void TestWithNestedWhile()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            push.v self.a
            pushi.e -9
            pushenv [3]

            :[1]
            push.v self.b
            conv.v.b
            bf [3]

            :[2]
            b [1]

            :[3]
            popenv [1]
            """,
            """
            with (a)
            {
                while (b)
                {
                }
            }
            """
        );
    }

    [Fact]
    public void TestRoomInstanceReference()
    {
        TestUtil.VerifyDecompileResult(
            """
            pushref.i 123123 RoomInstance
            pushi.e -9
            push.v [stacktop]self.a
            pop.v.v self.b
            pushref.i 456456 RoomInstance
            pop.v.v self.c
            """,
            """
            b = inst_id_123123.a;
            c = inst_id_456456;
            """
        );
    }

    [Fact]
    public void TestWithContinue()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            pushi.e 123
            pushenv [2]
            
            :[1]
            b [2]
            
            :[2]
            popenv [1]
            """,
            """
            with (123)
            {
                continue;
            }
            """
        );
    }

    [Fact]
    public void TestWithBreakContinue()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            pushi.e 123
            pushenv [5]
            
            :[1]
            push.v self.a
            conv.v.b
            bf [3]
            
            :[2]
            b [7]
            
            :[3]
            push.v self.b
            conv.v.b
            bf [5]
            
            :[4]
            b [5]
            
            :[5]
            popenv [1]
            
            :[6]
            b [8]
            
            :[7]
            popenv <drop>
            
            :[8]
            """,
            """
            with (123)
            {
                if (a)
                {
                    break;
                }
                if (b)
                {
                    continue;
                }
            }
            """
        );
    }

    [Fact]
    public void TestConditionalNestedNullish()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            push.v self.b
            conv.v.b
            bf [2]

            :[1]
            push.v self.c
            b [4]

            :[2]
            push.v self.d
            isnullish.e
            bf [4]

            :[3]
            popz.v
            push.v self.e

            :[4]
            pop.v.v self.a
            """,
            """
            a = b ? c : (d ?? e);
            """
        );
    }

    [Fact]
    public void TestConditionalNestedNullish2()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            push.v self.b
            conv.v.b
            bf [2]

            :[1]
            push.v self.c
            b [6]

            :[2]
            push.v self.d
            isnullish.e
            bf [6]

            :[3]
            popz.v
            push.v self.e
            conv.v.b
            bf [5]

            :[4]
            push.v self.f
            b [6]

            :[5]
            push.v self.g

            :[6]
            pop.v.v self.a
            """,
            """
            a = b ? c : (d ?? (e ? f : g));
            """
        );
    }

    [Fact]
    public void TestConditionalNestedNullish3()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            push.v self.b
            conv.v.b
            bf [2]

            :[1]
            push.v self.c
            b [5]

            :[2]
            push.v self.d
            isnullish.e
            bf [5]

            :[3]
            popz.v
            push.v self.e
            isnullish.e
            bf [5]

            :[4]
            popz.v
            push.v self.f

            :[5]
            pop.v.v self.a
            """,
            """
            a = b ? c : (d ?? (e ?? f));
            """
        );
    }

    [Fact]
    public void TestDefaultArgumentValues()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            b [8]

            > gml_Script_default_args (locals=0, args=3)
            :[1]
            push.v arg.argument1
            pushbltn.v builtin.undefined
            cmp.v.v EQ
            bf [3]

            :[2]
            pushi.e 123
            pop.v.i arg.argument1

            :[3]
            push.v arg.argument2
            pushbltn.v builtin.undefined
            cmp.v.v EQ
            bf [5]

            :[4]
            pushglb.v global.test
            pop.v.v arg.argument2

            :[5]
            push.v arg.argument0
            pushbltn.v builtin.undefined
            cmp.v.v EQ
            bf [7]

            :[6]
            pushi.e 123
            pop.v.i arg.argument0

            :[7]
            exit.i

            :[8]
            push.i [function]gml_Script_default_args
            conv.i.v
            pushi.e -1
            conv.i.v
            call.i method 2
            dup.v 0
            pushi.e -1
            pop.v.v [stacktop]self.default_args
            popz.v
            """,
            """
            function default_args(arg0, arg1 = 123, arg2 = global.test)
            {
                if (arg0 == undefined)
                {
                    arg0 = 123;
                }
            }
            """
        );
    }

    [Fact]
    public void TestArgumentsConflict()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            b [2]

            > gml_Script_args_conflict (locals=1, args=1)
            :[1]
            pushi.e 123
            pop.v.i local.arg0
            exit.i

            :[2]
            push.i [function]gml_Script_args_conflict
            conv.i.v
            pushi.e -1
            conv.i.v
            call.i method 2
            dup.v 0
            pushi.e -1
            pop.v.v [stacktop]self.args_conflict
            popz.v
            """,
            """
            function args_conflict(arg0_)
            {
                var arg0 = 123;
            }
            """
        );
    }

    [Fact]
    public void TestArgumentsLocal()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            b [4]

            > gml_Script_default_args_locals (locals=1, args=1)
            :[1]
            push.v arg.argument0
            pushbltn.v builtin.undefined
            cmp.v.v EQ
            bf [3]

            :[2]
            pushi.e 123
            pop.v.i arg.argument0

            :[3]
            pushi.e 123
            pop.v.i local.local
            exit.i

            :[4]
            push.i [function]gml_Script_default_args_locals
            conv.i.v
            pushi.e -1
            conv.i.v
            call.i method 2
            dup.v 0
            pushi.e -1
            pop.v.v [stacktop]self.default_args_locals
            popz.v
            """,
            """
            function default_args_locals(arg0 = 123)
            {
                var local = 123;
            }
            """
        );
    }

    [Fact]
    public void TestArgumentsSelf()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            b [4]

            > gml_Script_default_arg_color (locals=0, args=1)
            :[1]
            push.v arg.argument0
            pushbltn.v builtin.undefined
            cmp.v.v EQ
            bf [3]

            :[2]
            push.i 1
            pop.v.i arg.argument0

            :[3]
            pushi.e 123
            pop.v.i builtin.arg0
            exit.i

            :[4]
            push.i [function]gml_Script_default_arg_color
            conv.i.v
            pushi.e -1
            conv.i.v
            call.i method 2
            dup.v 0
            pushi.e -1
            pop.v.v [stacktop]self.default_arg_color
            popz.v
            """,
            """
            function default_arg_color(arg0 = 1)
            {
                self.arg0 = 123;
            }
            """
        );
    }

    [Fact]
    public void TestPredefinedDoubles()
    {
        TestUtil.VerifyDecompileResult(
            """
            push.d 3.141592653589793
            pop.v.d self.a
            push.d 6.283185307179586
            push.v self.c
            add.v.d
            pop.v.v self.b
            push.d 6.283185307179586
            pop.v.v self.d
            """,
            """
            a = pi;
            b = (2 * pi) + c;
            d = 2 * pi;
            """
        );
    }

    [Fact]
    public void TestComparisonPrecedence()
    {
        TestUtil.VerifyDecompileResult(
            """
            push.v self.b
            push.v self.c
            cmp.v.v LT
            push.v self.d
            cmp.v.b LT
            pop.v.b self.a
            push.v self.b
            push.v self.c
            push.v self.d
            cmp.v.v LT
            cmp.b.v LT
            pop.v.b self.a
            """,
            """
            a = (b < c) < d;
            a = b < (c < d);
            """
        );
    }

    [Fact]
    public void TestInheritance()
    {
        var context = new Underanalyzer.Mock.GameContextMock();
        var func = new Underanalyzer.Mock.GMFunction("gml_Script_TestA");
        ((GlobalFunctions)context.GlobalFunctions).DefineFunction("TestA", func);

        TestUtil.VerifyDecompileResult(
            """
            :[0]
            b [2]

            > gml_Script_TestA (locals=0, args=1)
            :[1]
            push.v arg.argument0
            pop.v.v builtin.test
            exit.i

            :[2]
            push.i [function]gml_Script_TestA
            conv.i.v
            call.i @@NullObject@@ 0
            call.i method 2
            dup.v 0
            pushi.e -1
            pop.v.v [stacktop]self.TestA
            popz.v
            b [4]

            > gml_Script_TestB (locals=0, args=2)
            :[3]
            push.v arg.argument0
            pushi.e 2
            mul.i.v
            call.i gml_Script_TestA 1
            push.i [function]gml_Script_TestA
            conv.i.v
            call.i @@CopyStatic@@ 1
            push.v arg.argument1
            pop.v.v builtin.test2
            exit.i

            :[4]
            push.i [function]gml_Script_TestB
            conv.i.v
            call.i @@NullObject@@ 0
            call.i method 2
            dup.v 0
            pushi.e -1
            pop.v.v [stacktop]self.TestB
            popz.v
            """,
            """
            function TestA(arg0) constructor
            {
                test = arg0;
            }

            function TestB(arg0, arg1) : TestA(arg0 * 2) constructor
            {
                test2 = arg1;
            }
            """,
            context
        );
    }

    [Fact]
    public void TestBooleans()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            push.v builtin.a
            pushi.e 1
            cmp.b.v EQ
            bf [2]

            :[1]
            push.v builtin.a
            pushi.e 0
            cmp.b.v EQ
            b [3]

            :[2]
            push.e 0

            :[3]
            bf [5]

            :[4]
            pushi.e 0
            pop.v.b builtin.a
            pushi.e 1
            pop.v.b builtin.a

            :[5]
            push.v builtin.a
            pushi.e 1
            cmp.i.v EQ
            bf [7]

            :[6]
            push.v builtin.a
            pushi.e 0
            cmp.i.v EQ
            b [8]

            :[7]
            push.e 0

            :[8]
            bf [10]

            :[9]
            pushi.e 0
            pop.v.i builtin.a
            pushi.e 1
            pop.v.i builtin.a

            :[10]
            pushi.e 1
            push.v builtin.a
            cmp.v.b EQ
            bf [12]

            :[11]
            pushi.e 0
            push.v builtin.a
            cmp.v.b EQ
            b [13]

            :[12]
            push.e 0

            :[13]
            bf [end]

            :[end]
            """,
            """
            if (a == true && a == false)
            {
                a = false;
                a = true;
            }
            if (a == 1 && a == 0)
            {
                a = 0;
                a = 1;
            }
            if (true == a && false == a)
            {
            }
            """
        );
    }

    [Fact]
    public void TestBooleanSwitch()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            push.v builtin.a
            push.v builtin.b
            cmp.v.v LT
            dup.b 0
            pushi.e 0
            cmp.i.b EQ
            bt [2]

            :[1]
            b [3]

            :[2]
            b [3]

            :[3]
            popz.b
            """,
            """
            switch (a < b)
            {
                case 0:
                    break;
            }
            """
        );
    }

    [Fact]
    public void TestWithIfBreakElse()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            push.v builtin.a
            pushi.e -9
            pushenv [5]

            :[1]
            push.v builtin.b
            conv.v.b
            bf [4]

            :[2]
            pushi.e 1
            pop.v.i builtin.c
            b [7]

            :[3]
            b [5]

            :[4]
            pushi.e 2
            pop.v.i builtin.d

            :[5]
            popenv [1]

            :[6]
            b [8]

            :[7]
            popenv <drop>

            :[8]
            """,
            """
            with (a)
            {
                if (b)
                {
                    c = 1;
                    break;
                }
                else
                {
                    d = 2;
                }
            }
            """
        );
    }

    [Fact]
    public void TestFunctionDeclCall()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            call.i @@This@@ 0
            b [2]

            > gml_Script_anon@1@A (locals=0, args=0)
            :[1]
            pushi.e 1
            pop.v.i builtin.a
            exit.i

            :[2]
            push.i [function]gml_Script_anon@1@A
            conv.i.v
            pushi.e -1
            conv.i.v
            call.i method 2
            callv.v 0
            popz.v
            push.v builtin.a
            push.v builtin.c
            pushi.e -9
            push.v [stacktop]self.d
            dup.v 1 1
            dup.v 0
            push.v stacktop.b
            callv.v 1
            popz.v
            pushi.e 123
            conv.i.v
            call.i @@This@@ 0
            push.v builtin.a
            conv.v.b
            bf [4]

            :[3]
            push.v builtin.b
            b [5]

            :[4]
            push.v builtin.c

            :[5]
            callv.v 1
            popz.v
            """,
            """
            (function()
            {
                a = 1;
            })();
            a.b(c.d);
            (a ? b : c)(123);
            """
        );
    }

    [Fact]
    public void TestWithWhileIfBreak()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            push.v builtin.a
            pushi.e -9
            pushenv [5]

            :[1]
            push.v builtin.b
            conv.v.b
            bf [5]

            :[2]
            push.v builtin.c
            conv.v.b
            bf [4]

            :[3]
            b [5]

            :[4]
            b [1]

            :[5]
            popenv [1]
            """,
            """
            with (a)
            {
                while (b)
                {
                    if (c)
                    {
                        break;
                    }
                }
            }
            """
        );
    }

    [Fact]
    public void TestWhileWhileIfBreak()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            push.v builtin.a
            conv.v.b
            bf [6]

            :[1]
            push.v builtin.b
            conv.v.b
            bf [5]

            :[2]
            push.v builtin.c
            conv.v.b
            bf [4]

            :[3]
            b [5]

            :[4]
            b [1]

            :[5]
            b [0]

            :[6]
            """,
            """
            while (a)
            {
                while (b)
                {
                    if (c)
                    {
                        break;
                    }
                }
            }
            """
        );
    }

    [Fact]
    public void TestNoAccessorBuiltinArray()
    {
        TestUtil.VerifyDecompileResult(
            """
            pushi.e -1
            push.l 0
            conv.l.i
            push.v [array]self.view_xview
            pop.v.v self.a
            pushi.e -1
            pushi.e 0
            push.v [array]self.view_xview
            pop.v.v self.a
            """,
            """
            a = view_xview;
            a = view_xview[0];
            """
        );
    }

    [Fact]
    public void TestOldInstanceIds()
    {
        TestUtil.VerifyDecompileResult(
            """
            pushi.e 123
            pop.v.i [instance]-456.a
            """,
            """
            (165080).a = 123;
            """
        );
    }

    [Fact]
    public void TestBinaryOrderOfOperations()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            push.v self.b
            push.v self.c
            push.v self.d
            add.v.v
            add.v.v
            pop.v.v self.a
            exit.i

            :[1]
            push.v self.b
            push.v self.c
            add.v.v
            push.v self.d
            add.v.v
            pop.v.v self.a
            """,
            """
            a = b + (c + d);
            exit;
            a = b + c + d;
            """
        );
    }

    [Fact]
    public void TestEmpty()
    {
        GameContextMock gameContext = new();
        DecompileContext decompilerContext = new(gameContext, TestUtil.GetCode("", gameContext));
        string decompileResult = decompilerContext.DecompileToString();
        Assert.Equal("", decompileResult);
    }

    [Fact]
    public void TestCompoundOperators()
    {
        TestUtil.VerifyDecompileResult(
            """
            push.v self.a
            pushi.e 2
            add.i.v
            pop.v.v self.a
            push.v self.a
            pushi.e 2
            sub.i.v
            pop.v.v self.a
            push.v self.a
            pushi.e 2
            mul.i.v
            pop.v.v self.a
            push.v self.a
            pushi.e 2
            div.i.v
            pop.v.v self.a
            push.v self.a
            pushi.e 2
            and.i.v
            pop.v.v self.a
            push.v self.a
            pushi.e 2
            or.i.v
            pop.v.v self.a
            push.v self.a
            pushi.e 2
            xor.i.v
            pop.v.v self.a
            push.v self.a
            pushi.e 2
            mod.i.v
            pop.v.v self.a
            push.v self.a
            pushi.e 2
            rem.i.v
            pop.v.v self.a
            push.v self.a
            pushi.e 2
            cmp.i.v EQ
            pop.v.b self.a
            push.v self.a
            pushi.e 2
            cmp.i.v NEQ
            pop.v.b self.a
            push.v self.a
            pushi.e 2
            cmp.i.v GT
            pop.v.b self.a
            push.v self.a
            pushi.e 2
            cmp.i.v LT
            pop.v.b self.a
            push.v self.a
            pushi.e 2
            cmp.i.v LTE
            pop.v.b self.a
            push.v self.a
            pushi.e 2
            cmp.i.v GTE
            pop.v.b self.a
            push.v self.a
            conv.v.l
            pushi.e 2
            conv.i.l
            shl.l.l
            pop.v.l self.a
            push.v self.a
            conv.v.l
            pushi.e 2
            conv.i.l
            shr.l.l
            pop.v.l self.a
            """,
            """
            a += 2;
            a -= 2;
            a *= 2;
            a /= 2;
            a &= 2;
            a |= 2;
            a ^= 2;
            a %= 2;
            a = a div 2;
            a = a == 2;
            a = a != 2;
            a = a > 2;
            a = a < 2;
            a = a <= 2;
            a = a >= 2;
            a = a << 2;
            a = a >> 2;
            """
        );
    }

    [Fact]
    public void TestComplexCompoundOperators()
    {
        TestUtil.VerifyDecompileResult(
            """
            push.v self.a
            conv.v.i
            dup.i 0
            push.v [stacktop]self.b
            push.e 1
            add.i.v
            pop.i.v [stacktop]self.b
            push.v self.a
            conv.v.i
            dup.i 0
            push.v [stacktop]self.b
            pushi.e 1
            add.i.v
            pop.i.v [stacktop]self.b
            push.v self.a
            conv.v.i
            push.v [stacktop]self.b
            pushi.e 1
            add.i.v
            push.v self.a
            conv.v.i
            pop.v.v [stacktop]self.b
            pushi.e -6
            pushi.e 0
            push.v [multipushpop]self.a
            pushi.e 1
            pushac.e
            pushi.e 2
            dup.i 4
            savearef.e
            pushaf.e
            pushi.e 1
            add.i.v
            restorearef.e
            dup.i 4 5
            popaf.e
            pushi.e -6
            pushi.e 0
            push.v [multipush]self.a
            pushi.e 1
            pushac.e
            pushi.e 2
            pushaf.e
            pushi.e 1
            add.i.v
            pushi.e -6
            pushi.e 0
            push.v [multipushpop]self.a
            pushi.e 1
            pushac.e
            pushi.e 2
            popaf.e
            call.i @@This@@ 0
            push.v builtin.a
            callv.v 0
            pushi.e -9
            dup.i 4
            push.v [stacktop]self.b
            push.e 1
            add.i.v
            pop.i.v [stacktop]self.b
            call.i @@This@@ 0
            push.v builtin.a
            callv.v 0
            pushi.e -9
            dup.i 4
            push.v [stacktop]self.b
            pushi.e 1
            add.i.v
            pop.i.v [stacktop]self.b
            call.i @@This@@ 0
            push.v builtin.a
            callv.v 0
            pushi.e -9
            push.v [stacktop]self.b
            pushi.e 1
            add.i.v
            call.i @@This@@ 0
            push.v builtin.a
            callv.v 0
            pushi.e -9
            pop.v.v [stacktop]self.b
            """,
            """
            a.b++;
            a.b += 1;
            a.b = a.b + 1;
            a[0][1][2] += 1;
            a[0][1][2] = a[0][1][2] + 1;
            a().b++;
            a().b += 1;
            a().b = a().b + 1;
            """
        );
    }

    [Fact]
    public void TestOver16NamedArguments()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            b [2]

            > gml_Script_First (locals=0, args=17)
            :[1]
            push.v arg.argument15
            pop.v.v builtin.a
            pushi.e -15
            pushi.e 16
            push.v [array]self.argument
            pop.v.v builtin.b
            exit.i

            :[2]
            push.i [function]gml_Script_First
            conv.i.v
            pushi.e -1
            conv.i.v
            call.i method 2
            dup.v 0
            pushi.e -1
            pop.v.v [stacktop]self.First
            popz.v
            b [4]

            > gml_Script_Second (locals=0, args=18)
            :[3]
            push.v arg.argument15
            pop.v.v builtin.a
            pushi.e -15
            pushi.e 16
            push.v [array]self.argument
            pop.v.v builtin.b
            exit.i

            :[4]
            push.i [function]gml_Script_Second
            conv.i.v
            pushi.e -1
            conv.i.v
            call.i method 2
            dup.v 0
            pushi.e -1
            pop.v.v [stacktop]self.Second
            popz.v
            b [6]

            > gml_Script_Third (locals=0, args=16)
            :[5]
            push.v arg.argument15
            pop.v.v builtin.a
            pushi.e -15
            pushi.e 16
            push.v [array]self.argument
            pop.v.v builtin.b
            exit.i

            :[6]
            push.i [function]gml_Script_Third
            conv.i.v
            pushi.e -1
            conv.i.v
            call.i method 2
            dup.v 0
            pushi.e -1
            pop.v.v [stacktop]self.Third
            popz.v
            b [8]

            > gml_Script_Fourth (locals=0, args=17)
            :[7]
            pushi.e -15
            pushi.e 16
            push.v [multipush]self.argument
            pushi.e 123
            pushaf.e
            pop.v.v builtin.a
            exit.i

            :[8]
            push.i [function]gml_Script_Fourth
            conv.i.v
            pushi.e -1
            conv.i.v
            call.i method 2
            dup.v 0
            pushi.e -1
            pop.v.v [stacktop]self.Fourth
            popz.v
            """,
            """
            function First(_0, _1, _2, _3, _4, _5, _6, _7, _8, _9, _10, _11, _12, _13, _14, _15, _16)
            {
                a = _15;
                b = _16;
            }

            function Second(_0, _1, _2, _3, _4, _5, _6, _7, _8, _9, _10, _11, _12, _13, _14, _15, _16, _17)
            {
                a = _15;
                b = _16;
            }

            function Third(_0, _1, _2, _3, _4, _5, _6, _7, _8, _9, _10, _11, _12, _13, _14, _15)
            {
                a = _15;
                b = argument[16];
            }

            function Fourth(_0, _1, _2, _3, _4, _5, _6, _7, _8, _9, _10, _11, _12, _13, _14, _15, _16)
            {
                a = _16[123];
            }
            """,
            null,
            new DecompileSettings()
            {
                UnknownArgumentNamePattern = "_{0}"
            }
        );
    }

    [Fact]
    public void TestStructNotCompound()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            push.v self.b
            pushi.e 1
            add.i.v
            b [2]

            > gml_Script____struct___test__0 (locals=1, args=0)
            :[1]
            pushi.e -15
            pushi.e 0
            push.v [array]self.argument
            pop.v.v self.b
            exit.i

            :[2]
            push.i [function]gml_Script____struct___test__0
            conv.i.v
            call.i @@NullObject@@ 0
            call.i method 2
            dup.v 0
            pushi.e -16
            pop.v.v [stacktop]static.gml_Script____struct___test__0
            call.i @@NewGMLObject@@ 2
            pop.v.v self.a
            """,
            """
            a = 
            {
                b: b + 1
            };
            """
        );
    }

    [Fact]
    public void TestCallVariable()
    {
        // TODO: make this test with asset references included
        TestUtil.VerifyDecompileResult(
            """
            push.s "a"
            conv.s.v
            call.i show_debug_message 1
            popz.v
            push.s "a"
            conv.s.v
            call.i @@This@@ 0
            push.v builtin.test
            callv.v 1
            popz.v
            call.i @@This@@ 0
            push.s "a"
            conv.s.v
            dup.v 1 1
            dup.v 0
            push.v stacktop.test
            callv.v 1
            popz.v
            push.v self.a
            push.s "a"
            conv.s.v
            dup.v 1 1
            dup.v 0
            push.v stacktop.b
            callv.v 1
            popz.v
            """,
            """
            show_debug_message("a");
            test("a");
            self.test("a");
            a.b("a");
            """
        );
    }

    [Fact]
    public void TestNewConstructorSetStatic()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            b [2]

            > gml_Script_Test (locals=0, args=1)
            :[1]
            call.i @@SetStatic@@ 0
            push.v arg.argument0
            pop.v.v builtin.test
            exit.i

            :[2]
            push.i [function]gml_Script_Test
            conv.i.v
            call.i @@NullObject@@ 0
            call.i method 2
            dup.v 0
            pushi.e -1
            pop.v.v [stacktop]self.Test
            popz.v
            """,
            """
            function Test(arg0) constructor
            {
                test = arg0;
            }
            """
        );
    }

    [Fact]
    public void TestStructSelfArgument()
    {
        // Note about this test case: -1 (self) values are generally -15 (arguments), but seems like either
        // different GameMaker versions or mod tooling(?) generates code that uses self...
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            call.i @@NewGMLArray@@ 0
            call.i @@NewGMLArray@@ 0
            b [2]

            > test_struct (locals=0, args=0)
            :[1]
            pushi.e -1
            pushi.e 0
            push.v [array]self.argument
            pop.v.v self.a
            pushi.e -1
            pushi.e 1
            push.v [array]self.argument
            pop.v.v self.b
            exit.i

            :[2]
            push.i [function]test_struct
            conv.i.v
            call.i @@NullObject@@ 0
            call.i method 2
            dup.v 0
            pushi.e -16
            pop.v.v [stacktop]static.test_struct
            call.i @@NewGMLObject@@ 3
            pop.v.v self.c
            """,
            """
            c = 
            {
                a: [],
                b: []
            };
            """
        );
    }

    [Fact]
    public void TestTryWithFinally()
    {
        TestUtil.VerifyDecompileResult(
            """
            :[0]
            pushi.e 1
            pop.v.i builtin.a

            :[1]
            push.i 80
            conv.i.v
            push.i -1
            conv.i.v
            call.i @@try_hook@@ 2
            popz.v
            
            :[2]
            push.v builtin.b
            pushi.e -9
            pushenv [4]

            :[3]
            pushi.e 3
            pop.v.i builtin.c

            :[4]
            popenv [3]
            
            :[5]
            call.i @@try_unhook@@ 0
            popz.v
            pushi.e 4
            pop.v.i builtin.d
            call.i @@finish_finally@@ 0
            popz.v
            b [6]

            :[6]
            """,
            """
            a = 1;
            try
            {
                with (b)
                {
                    c = 3;
                }
            }
            finally
            {
                d = 4;
            }
            """
        );
    }
}
