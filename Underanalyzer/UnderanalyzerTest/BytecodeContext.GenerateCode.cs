/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using Underanalyzer;

namespace UnderanalyzerTest;

public class BytecodeContext_GenerateCode
{
    [Fact]
    public void TestReturn()
    {
        TestUtil.AssertBytecode(
            """
            return 123;
            """,
            """
            pushi.e 123
            conv.i.v
            ret.v
            """
        );
    }

    [Fact]
    public void TestSimpleAssigns()
    {
        TestUtil.AssertBytecode(
            """
            a = 1;
            var b = 2;
            self.c = 3;
            global.d = 4;
            b = 5;
            """,
            """
            pushi.e 1
            pop.v.i self.a
            pushi.e 2
            pop.v.i local.b
            pushi.e 3
            pop.v.i self.c
            pushi.e 4
            pop.v.i global.d
            pushi.e 5
            pop.v.i local.b
            """
        );
    }

    [Fact]
    public void TestInstanceConstants1()
    {
        TestUtil.AssertBytecode(
            """
            a = self;
            b = other;
            c = global;
            d = noone;
            """,
            """
            pushi.e -1
            pop.v.i self.a
            pushi.e -2
            pop.v.i self.b
            pushi.e -5
            pop.v.i self.c
            pushi.e -4
            pop.v.i self.d
            """,
            false,
            new Underanalyzer.Mock.GameContextMock()
            {
                UsingGMLv2 = false
            }
        );
    }

    [Fact]
    public void TestInstanceConstants2()
    {
        TestUtil.AssertBytecode(
            """
            a = self;
            b = other;
            c = global;
            d = noone;
            """,
            """
            call.i @@This@@ 0
            pop.v.v self.a
            call.i @@Other@@ 0
            pop.v.v self.b
            pushi.e -5
            pop.v.i self.c
            pushi.e -4
            pop.v.i self.d
            """,
            false,
            new Underanalyzer.Mock.GameContextMock()
            {
                UsingGMLv2 = true
            }
        );
    }

    [Fact]
    public void TestInstanceConstants3()
    {
        TestUtil.AssertBytecode(
            """
            a = self;
            b = other;
            c = global;
            d = noone;
            """,
            """
            call.i @@This@@ 0
            pop.v.v self.a
            call.i @@Other@@ 0
            pop.v.v self.b
            call.i @@Global@@ 0
            pop.v.v self.c
            pushi.e -4
            pop.v.i self.d
            """,
            false,
            new Underanalyzer.Mock.GameContextMock()
            {
                UsingGMLv2 = true,
                UsingGlobalConstantFunction = true
            }
        );
    }

    [Fact]
    public void TestVariableCalls()
    {
        Underanalyzer.Mock.GameContextMock gameContext = new()
        {
            UsingGMLv2 = true,
            UsingAssetReferences = false
        };
        ((Underanalyzer.Mock.BuiltinsMock)gameContext.Builtins)
            .BuiltinFunctions["show_debug_message"] = new("show_debug_message", 1, 1);
        gameContext.DefineMockAsset(AssetType.Object, 123, "obj_test");
        TestUtil.AssertBytecode(
            """
            var d;
            show_debug_message("a");
            test("a");
            self.test("a");
            a.b("a");
            d("a");
            obj_test.a("a");
            obj_test.a.b("a");
            """,
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
            push.s "a"
            conv.s.v
            call.i @@This@@ 0
            pushloc.v local.d
            callv.v 1
            popz.v
            pushi.e 123
            conv.i.v
            call.i @@GetInstance@@ 1
            push.s "a"
            conv.s.v
            dup.v 1 1
            dup.v 0
            push.v stacktop.a
            callv.v 1
            popz.v
            push.v 123.a
            push.s "a"
            conv.s.v
            dup.v 1 1
            dup.v 0
            push.v stacktop.b
            callv.v 1
            popz.v
            """,
            false,
            gameContext
        );
    }

    [Fact]
    public void TestVariableCallsAssetRefs()
    {
        Underanalyzer.Mock.GameContextMock gameContext = new()
        {
            UsingGMLv2 = true,
            UsingAssetReferences = true,
            UsingSelfToBuiltin = true
        };
        ((Underanalyzer.Mock.BuiltinsMock)gameContext.Builtins)
            .BuiltinFunctions["show_debug_message"] = new("show_debug_message", 1, 1);
        gameContext.DefineMockAsset(AssetType.Object, 123, "obj_test");
        TestUtil.AssertBytecode(
            """
            var d;
            show_debug_message("a");
            test("a");
            self.test("a");
            a.b("a");
            d("a");
            obj_test.a("a");
            obj_test.a.b("a");
            """,
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
            push.v builtin.a
            push.s "a"
            conv.s.v
            dup.v 1 1
            dup.v 0
            push.v stacktop.b
            callv.v 1
            popz.v
            push.s "a"
            conv.s.v
            call.i @@This@@ 0
            pushloc.v local.d
            callv.v 1
            popz.v
            pushref.i 123 Object
            push.s "a"
            conv.s.v
            dup.v 1 1
            dup.v 0
            push.v stacktop.a
            callv.v 1
            popz.v
            pushref.i 123 Object
            pushi.e -9
            push.v [stacktop]self.a
            push.s "a"
            conv.s.v
            dup.v 1 1
            dup.v 0
            push.v stacktop.b
            callv.v 1
            popz.v
            """,
            false,
            gameContext
        );
    }

    [Fact]
    public void TestRepeatVariableCalls()
    {
        // NOTE: as of writing, "a.b(1)(2)(3);" does not compile correctly in GameMaker, but does here
        TestUtil.AssertBytecode(
            """
            a(1)(2)(3)(4)(5);
            a.b(1)(2)(3);
            a(1).b(2);
            a(1).b(2)(3)(4);
            a(1).b(2)(3).c(4);
            """,
            """
            pushi.e 5
            conv.i.v
            call.i @@This@@ 0
            pushi.e 4
            conv.i.v
            call.i @@This@@ 0
            pushi.e 3
            conv.i.v
            call.i @@This@@ 0
            pushi.e 2
            conv.i.v
            call.i @@This@@ 0
            pushi.e 1
            conv.i.v
            call.i @@This@@ 0
            push.v builtin.a
            callv.v 1
            callv.v 1
            callv.v 1
            callv.v 1
            callv.v 1
            popz.v
            pushi.e 3
            conv.i.v
            call.i @@This@@ 0
            pushi.e 2
            conv.i.v
            call.i @@This@@ 0
            push.v self.a
            pushi.e 1
            conv.i.v
            dup.v 1 1
            dup.v 0
            push.v stacktop.b
            callv.v 1
            callv.v 1
            callv.v 1
            popz.v
            pushi.e 2
            conv.i.v
            pushi.e 1
            conv.i.v
            call.i @@This@@ 0
            push.v builtin.a
            callv.v 1
            dup.v 0
            pushi.e -9
            push.v [stacktop]self.b
            callv.v 1
            popz.v
            pushi.e 4
            conv.i.v
            call.i @@This@@ 0
            pushi.e 3
            conv.i.v
            call.i @@This@@ 0
            pushi.e 2
            conv.i.v
            pushi.e 1
            conv.i.v
            call.i @@This@@ 0
            push.v builtin.a
            callv.v 1
            dup.v 0
            pushi.e -9
            push.v [stacktop]self.b
            callv.v 1
            callv.v 1
            callv.v 1
            popz.v
            pushi.e 4
            conv.i.v
            pushi.e 3
            conv.i.v
            call.i @@This@@ 0
            pushi.e 2
            conv.i.v
            pushi.e 1
            conv.i.v
            call.i @@This@@ 0
            push.v builtin.a
            callv.v 1
            dup.v 0
            pushi.e -9
            push.v [stacktop]self.b
            callv.v 1
            callv.v 1
            dup.v 0
            pushi.e -9
            push.v [stacktop]self.c
            callv.v 1
            popz.v
            """
        );
    }

    [Fact]
    public void TestRepeatVariableCalls2()
    {
        Underanalyzer.Mock.GameContextMock gameContext = new()
        {
            UsingGMLv2 = true,
            UsingAssetReferences = false
        };
        gameContext.DefineMockAsset(AssetType.Object, 123, "obj_test");
        TestUtil.AssertBytecode(
            """
            obj_test.a(1).b(2);
            """,
            """
            pushi.e 123
            conv.i.v
            call.i @@GetInstance@@ 1
            pushi.e 1
            conv.i.v
            dup.v 1 1
            dup.v 0
            push.v stacktop.a
            callv.v 1
            pushi.e 2
            conv.i.v
            dup.v 1 1
            dup.v 0
            push.v stacktop.b
            callv.v 1
            popz.v
            """,
            false,
            gameContext
        );
    }

    [Fact]
    public void TestRepeatVariableCalls2AssetRefs()
    {
        Underanalyzer.Mock.GameContextMock gameContext = new()
        {
            UsingGMLv2 = true,
            UsingAssetReferences = true
        };
        gameContext.DefineMockAsset(AssetType.Object, 123, "obj_test");
        TestUtil.AssertBytecode(
            """
            obj_test.a(1).b(2);
            """,
            """
            pushref.i 123 Object
            pushi.e 1
            conv.i.v
            dup.v 1 1
            dup.v 0
            push.v stacktop.a
            callv.v 1
            pushi.e 2
            conv.i.v
            dup.v 1 1
            dup.v 0
            push.v stacktop.b
            callv.v 1
            popz.v
            """,
            false,
            gameContext
        );
    }

    [Fact]
    public void TestRepeatVariableCalls3()
    {
        TestUtil.AssertBytecode(
            """
            self.a(1).b(2);
            """,
            """
            call.i @@This@@ 0
            pushi.e 1
            conv.i.v
            dup.v 1 1
            dup.v 0
            push.v stacktop.a
            callv.v 1
            pushi.e 2
            conv.i.v
            dup.v 1 1
            dup.v 0
            push.v stacktop.b
            callv.v 1
            popz.v
            """
        );
    }

    [Fact]
    public void TestVariableCallsArrays()
    {
        // NOTE: These compile somewhat differently depending on GameMaker version, which this currently ignores.
        //       As of writing this test, the goal is to mainly get compatible code compilation for all versions.
        Underanalyzer.Mock.GameContextMock gameContext = new()
        {
            UsingGMLv2 = true,
            UsingAssetReferences = false
        };
        gameContext.DefineMockAsset(AssetType.Object, 123, "obj_test");
        TestUtil.AssertBytecode(
            """
            a[0](1);
            self.a[0](1);
            global.a[0](1);
            obj_test.a[0](1);
            a[0].b(1);
            a[0].b[1](2);
            a[0](1).b[2](3);
            a.b[0](1).c.d[2](3);
            a.b[0].c(1).d.e[2].f(3);
            """,
            """
            pushi.e 1
            conv.i.v
            call.i @@This@@ 0
            pushi.e -1
            pushi.e 0
            push.v [array]self.a
            callv.v 1
            popz.v
            pushi.e 1
            conv.i.v
            call.i @@This@@ 0
            pushi.e -9
            pushi.e 0
            push.v [array]self.a
            dup.v 0
            callv.v 1
            popz.v
            pushi.e 1
            conv.i.v
            pushi.e -5
            pushi.e 0
            push.v [array]self.a
            dup.v 0
            callv.v 1
            popz.v
            pushi.e 1
            conv.i.v
            pushi.e 123
            pushi.e 0
            push.v [array]self.a
            dup.v 0
            callv.v 1
            popz.v
            pushi.e -1
            pushi.e 0
            push.v [array]self.a
            pushi.e 1
            conv.i.v
            dup.v 1 1
            dup.v 0
            push.v stacktop.b
            callv.v 1
            popz.v
            pushi.e 2
            conv.i.v
            pushi.e -1
            pushi.e 0
            push.v [array]self.a
            pushi.e -9
            pushi.e 1
            push.v [array]self.b
            dup.v 0
            callv.v 1
            popz.v
            pushi.e 3
            conv.i.v
            pushi.e 1
            conv.i.v
            call.i @@This@@ 0
            pushi.e -1
            pushi.e 0
            push.v [array]self.a
            callv.v 1
            pushi.e -9
            pushi.e 2
            push.v [array]self.b
            dup.v 0
            callv.v 1
            popz.v
            pushi.e 3
            conv.i.v
            pushi.e 1
            conv.i.v
            push.v self.a
            pushi.e -9
            pushi.e 0
            push.v [array]self.b
            dup.v 0
            callv.v 1
            pushi.e -9
            push.v [stacktop]self.c
            pushi.e -9
            pushi.e 2
            push.v [array]self.d
            dup.v 0
            callv.v 1
            popz.v
            push.v self.a
            pushi.e -9
            pushi.e 0
            push.v [array]self.b
            pushi.e 1
            conv.i.v
            dup.v 1 1
            dup.v 0
            push.v stacktop.c
            callv.v 1
            pushi.e -9
            push.v [stacktop]self.d
            pushi.e -9
            pushi.e 2
            push.v [array]self.e
            pushi.e 3
            conv.i.v
            dup.v 1 1
            dup.v 0
            push.v stacktop.f
            callv.v 1
            popz.v
            """,
            false,
            gameContext
        );
    }

    [Fact]
    public void TestVariableCallsArraysAssetRefs()
    {
        // NOTE: These compile somewhat differently depending on GameMaker version, which this currently ignores.
        //       As of writing this test, the goal is to mainly get compatible code compilation for all versions.
        Underanalyzer.Mock.GameContextMock gameContext = new()
        {
            UsingGMLv2 = true,
            UsingAssetReferences = true
        };
        gameContext.DefineMockAsset(AssetType.Object, 123, "obj_test");
        TestUtil.AssertBytecode(
            """
            a[0](1);
            self.a[0](1);
            global.a[0](1);
            obj_test.a[0](1);
            a[0].b(1);
            a[0].b[1](2);
            a[0](1).b[2](3);
            a.b[0](1).c.d[2](3);
            a.b[0].c(1).d.e[2].f(3);
            """,
            """
            pushi.e 1
            conv.i.v
            call.i @@This@@ 0
            pushi.e -1
            pushi.e 0
            push.v [array]self.a
            callv.v 1
            popz.v
            pushi.e 1
            conv.i.v
            call.i @@This@@ 0
            pushi.e -9
            pushi.e 0
            push.v [array]self.a
            dup.v 0
            callv.v 1
            popz.v
            pushi.e 1
            conv.i.v
            pushi.e -5
            pushi.e 0
            push.v [array]self.a
            dup.v 0
            callv.v 1
            popz.v
            pushi.e 1
            conv.i.v
            pushref.i 123 Object
            pushi.e -9
            pushi.e 0
            push.v [array]self.a
            dup.v 0
            callv.v 1
            popz.v
            pushi.e -1
            pushi.e 0
            push.v [array]self.a
            pushi.e 1
            conv.i.v
            dup.v 1 1
            dup.v 0
            push.v stacktop.b
            callv.v 1
            popz.v
            pushi.e 2
            conv.i.v
            pushi.e -1
            pushi.e 0
            push.v [array]self.a
            pushi.e -9
            pushi.e 1
            push.v [array]self.b
            dup.v 0
            callv.v 1
            popz.v
            pushi.e 3
            conv.i.v
            pushi.e 1
            conv.i.v
            call.i @@This@@ 0
            pushi.e -1
            pushi.e 0
            push.v [array]self.a
            callv.v 1
            pushi.e -9
            pushi.e 2
            push.v [array]self.b
            dup.v 0
            callv.v 1
            popz.v
            pushi.e 3
            conv.i.v
            pushi.e 1
            conv.i.v
            push.v self.a
            pushi.e -9
            pushi.e 0
            push.v [array]self.b
            dup.v 0
            callv.v 1
            pushi.e -9
            push.v [stacktop]self.c
            pushi.e -9
            pushi.e 2
            push.v [array]self.d
            dup.v 0
            callv.v 1
            popz.v
            push.v self.a
            pushi.e -9
            pushi.e 0
            push.v [array]self.b
            pushi.e 1
            conv.i.v
            dup.v 1 1
            dup.v 0
            push.v stacktop.c
            callv.v 1
            pushi.e -9
            push.v [stacktop]self.d
            pushi.e -9
            pushi.e 2
            push.v [array]self.e
            pushi.e 3
            conv.i.v
            dup.v 1 1
            dup.v 0
            push.v stacktop.f
            callv.v 1
            popz.v
            """,
            false,
            gameContext
        );
    }

    [Fact]
    public void TestVariableCallsArraysMulti()
    {
        // NOTE: These compile somewhat differently depending on GameMaker version, which this currently ignores.
        //       As of writing this test, the goal is to mainly get compatible code compilation for all versions.
        Underanalyzer.Mock.GameContextMock gameContext = new()
        {
            UsingGMLv2 = true,
            UsingAssetReferences = false
        };
        gameContext.DefineMockAsset(AssetType.Object, 123, "obj_test");
        TestUtil.AssertBytecode(
            """
            a[0][1](2);
            self.a[0][1](2);
            global.a[0][1](2);
            obj_test.a[0][1](2);
            a.b[0][1](2);
            a[0][1].b(2);
            a[0][1].b[1][2](3);
            a[0][1](2).b[2][3](4);
            a.b[0][1](2).c.d[3][4](5);
            a.b[0][1].c(2).d.e[3][4].f(5);
            """,
            """
            pushi.e 2
            conv.i.v
            call.i @@This@@ 0
            pushi.e -1
            pushi.e 0
            push.v [multipush]self.a
            pushi.e 1
            pushaf.e
            callv.v 1
            popz.v
            pushi.e 2
            conv.i.v
            call.i @@This@@ 0
            pushi.e -9
            pushi.e 0
            push.v [multipush]self.a
            pushi.e 1
            pushaf.e
            dup.v 0
            callv.v 1
            popz.v
            pushi.e 2
            conv.i.v
            pushi.e -5
            pushi.e 0
            push.v [multipush]self.a
            pushi.e 1
            pushaf.e
            dup.v 0
            callv.v 1
            popz.v
            pushi.e 2
            conv.i.v
            pushi.e 123
            pushi.e 0
            push.v [multipush]self.a
            pushi.e 1
            pushaf.e
            dup.v 0
            callv.v 1
            popz.v
            pushi.e 2
            conv.i.v
            push.v self.a
            pushi.e -9
            pushi.e 0
            push.v [multipush]self.b
            pushi.e 1
            pushaf.e
            dup.v 0
            callv.v 1
            popz.v
            pushi.e -1
            pushi.e 0
            push.v [multipush]self.a
            pushi.e 1
            pushaf.e
            pushi.e 2
            conv.i.v
            dup.v 1 1
            dup.v 0
            push.v stacktop.b
            callv.v 1
            popz.v
            pushi.e 3
            conv.i.v
            pushi.e -1
            pushi.e 0
            push.v [multipush]self.a
            pushi.e 1
            pushaf.e
            pushi.e -9
            pushi.e 1
            push.v [multipush]self.b
            pushi.e 2
            pushaf.e
            dup.v 0
            callv.v 1
            popz.v
            pushi.e 4
            conv.i.v
            pushi.e 2
            conv.i.v
            call.i @@This@@ 0
            pushi.e -1
            pushi.e 0
            push.v [multipush]self.a
            pushi.e 1
            pushaf.e
            callv.v 1
            pushi.e -9
            pushi.e 2
            push.v [multipush]self.b
            pushi.e 3
            pushaf.e
            dup.v 0
            callv.v 1
            popz.v
            pushi.e 5
            conv.i.v
            pushi.e 2
            conv.i.v
            push.v self.a
            pushi.e -9
            pushi.e 0
            push.v [multipush]self.b
            pushi.e 1
            pushaf.e
            dup.v 0
            callv.v 1
            pushi.e -9
            push.v [stacktop]self.c
            pushi.e -9
            pushi.e 3
            push.v [multipush]self.d
            pushi.e 4
            pushaf.e
            dup.v 0
            callv.v 1
            popz.v
            push.v self.a
            pushi.e -9
            pushi.e 0
            push.v [multipush]self.b
            pushi.e 1
            pushaf.e
            pushi.e 2
            conv.i.v
            dup.v 1 1
            dup.v 0
            push.v stacktop.c
            callv.v 1
            pushi.e -9
            push.v [stacktop]self.d
            pushi.e -9
            pushi.e 3
            push.v [multipush]self.e
            pushi.e 4
            pushaf.e
            pushi.e 5
            conv.i.v
            dup.v 1 1
            dup.v 0
            push.v stacktop.f
            callv.v 1
            popz.v
            """,
            false,
            gameContext
        );
    }

    [Fact]
    public void TestFunctionDeclNames()
    {
        TestUtil.AssertBytecode(
            """
            function GlobalScriptMockName()
            {
            }

            function RandomOtherName()
            {
            }
            """,
            """
            :[0]
            b [2]

            > global_func_GlobalScriptMockName (locals=0, args=0)
            :[1]
            exit.i

            :[2]
            push.i [function]global_func_GlobalScriptMockName
            conv.i.v
            pushi.e -1
            conv.i.v
            call.i method 2
            dup.v 0
            pushi.e -1
            pop.v.v [stacktop]self.GlobalScriptMockName
            popz.v
            b [4]

            > global_func_RandomOtherName (locals=0, args=0)
            :[3]
            exit.i

            :[4]
            push.i [function]global_func_RandomOtherName
            conv.i.v
            pushi.e -1
            conv.i.v
            call.i method 2
            dup.v 0
            pushi.e -6
            pop.v.v [stacktop]self.RandomOtherName
            popz.v
            """,
            true
        );
    }

    [Fact]
    public void TestNewObject()
    {
        TestUtil.AssertBytecode(
            """
            function Test() constructor 
            {
            }

            new Test();
            new self.Test();
            new global.Test();
            a = new Test();
            b = new VariableCall();
            b = new self.VariableCall();
            b = new global.VariableCall();
            new Complex.Variable.Call(123, 456);
            """,
            """
            :[0]
            b [2]

            > regular_func_Test (locals=0, args=0)
            :[1]
            exit.i

            :[2]
            push.i [function]regular_func_Test
            conv.i.v
            call.i @@NullObject@@ 0
            call.i method 2
            dup.v 0
            pushi.e -6
            pop.v.v [stacktop]self.Test
            popz.v
            push.i [function]regular_func_Test
            conv.i.v
            call.i @@NewGMLObject@@ 1
            popz.v
            push.i [function]regular_func_Test
            conv.i.v
            call.i @@NewGMLObject@@ 1
            popz.v
            push.i [function]regular_func_Test
            conv.i.v
            call.i @@NewGMLObject@@ 1
            popz.v
            push.i [function]regular_func_Test
            conv.i.v
            call.i @@NewGMLObject@@ 1
            pop.v.v self.a
            push.v builtin.VariableCall
            call.i @@NewGMLObject@@ 1
            pop.v.v self.b
            push.v self.VariableCall
            call.i @@NewGMLObject@@ 1
            pop.v.v self.b
            pushglb.v global.VariableCall
            call.i @@NewGMLObject@@ 1
            pop.v.v self.b
            pushi.e 456
            conv.i.v
            pushi.e 123
            conv.i.v
            push.v self.Complex
            pushi.e -9
            push.v [stacktop]self.Variable
            pushi.e -9
            push.v [stacktop]self.Call
            call.i @@NewGMLObject@@ 3
            popz.v
            """
        );
    }

    [Fact]
    public void TestNonSelfToBuiltin()
    {
        TestUtil.AssertBytecode(
            """
            a = 0;
            self.a = 0;
            a = b;
            a = self.b;
            a += 1;
            self.a += 1;
            a++;
            self.a++;
            a.b = 0;
            a[0] = 1;
            self.a[0] = 1;
            a.b[0] = 1;
            a[0] += 1;
            self.a[0] += 1;
            a.b[0] += 1;
            a = a[0];
            a = self.a[0];
            a = a.b[0];
            global.a = 0;
            global.a[0] = 0;
            self.a.b = 1;
            a.b = 1;
            a = 1;
            other.a[0] = 1;
            global.a[0] = 1;
            a[0].b = 1;
            a = a[0].b;
            a = a[0];
            a[0].b[0]("a");
            a[0].b.c("a");
            a[0].b("a");
            """,
            """
            pushi.e 0
            pop.v.i self.a
            pushi.e 0
            pop.v.i self.a
            push.v self.b
            pop.v.v self.a
            push.v self.b
            pop.v.v self.a
            push.v self.a
            pushi.e 1
            add.i.v
            pop.v.v self.a
            push.v self.a
            pushi.e 1
            add.i.v
            pop.v.v self.a
            push.v self.a
            push.e 1
            add.i.v
            pop.v.v self.a
            push.v self.a
            push.e 1
            add.i.v
            pop.v.v self.a
            pushi.e 0
            push.v self.a
            pushi.e -9
            pop.v.i [stacktop]self.b
            pushi.e 1
            conv.i.v
            pushi.e -1
            pushi.e 0
            pop.v.v [array]self.a
            pushi.e 1
            conv.i.v
            pushi.e -1
            pushi.e 0
            pop.v.v [array]self.a
            pushi.e 1
            conv.i.v
            push.v self.a
            pushi.e -9
            pushi.e 0
            pop.v.v [array]self.b
            pushi.e -1
            pushi.e 0
            dup.i 1
            push.v [array]self.a
            pushi.e 1
            add.i.v
            pop.i.v [array]self.a
            pushi.e -1
            pushi.e 0
            dup.i 1
            push.v [array]self.a
            pushi.e 1
            add.i.v
            pop.i.v [array]self.a
            push.v self.a
            pushi.e -9
            pushi.e 0
            dup.i 5
            push.v [array]self.b
            pushi.e 1
            add.i.v
            pop.i.v [array]self.b
            pushi.e -1
            pushi.e 0
            push.v [array]self.a
            pop.v.v self.a
            pushi.e -1
            pushi.e 0
            push.v [array]self.a
            pop.v.v self.a
            push.v self.a
            pushi.e -9
            pushi.e 0
            push.v [array]self.b
            pop.v.v self.a
            pushi.e 0
            pop.v.i global.a
            pushi.e 0
            conv.i.v
            pushi.e -5
            pushi.e 0
            pop.v.v [array]self.a
            pushi.e 1
            push.v self.a
            pushi.e -9
            pop.v.i [stacktop]self.b
            pushi.e 1
            push.v self.a
            pushi.e -9
            pop.v.i [stacktop]self.b
            pushi.e 1
            pop.v.i self.a
            pushi.e 1
            conv.i.v
            pushi.e -2
            pushi.e 0
            pop.v.v [array]self.a
            pushi.e 1
            conv.i.v
            pushi.e -5
            pushi.e 0
            pop.v.v [array]self.a
            pushi.e 1
            pushi.e -1
            pushi.e 0
            push.v [array]self.a
            pushi.e -9
            pop.v.i [stacktop]self.b
            pushi.e -1
            pushi.e 0
            push.v [array]self.a
            pushi.e -9
            push.v [stacktop]self.b
            pop.v.v self.a
            pushi.e -1
            pushi.e 0
            push.v [array]self.a
            pop.v.v self.a
            push.s "a"
            conv.s.v
            pushi.e -1
            pushi.e 0
            push.v [array]self.a
            pushi.e -9
            pushi.e 0
            push.v [array]self.b
            dup.v 0
            callv.v 1
            popz.v
            pushi.e -1
            pushi.e 0
            push.v [array]self.a
            pushi.e -9
            push.v [stacktop]self.b
            push.s "a"
            conv.s.v
            dup.v 1 1
            dup.v 0
            push.v stacktop.c
            callv.v 1
            popz.v
            pushi.e -1
            pushi.e 0
            push.v [array]self.a
            push.s "a"
            conv.s.v
            dup.v 1 1
            dup.v 0
            push.v stacktop.b
            callv.v 1
            popz.v
            """
        );
    }

    [Fact]
    public void TestSelfToBuiltin()
    {
        Underanalyzer.Mock.GameContextMock gameContext = new()
        {
            UsingSelfToBuiltin = true,
            UsingGlobalConstantFunction = true
        };
        TestUtil.AssertBytecode(
            """
            a = 0;
            self.a = 0;
            a = b;
            a = self.b;
            a += 1;
            self.a += 1;
            a++;
            self.a++;
            a.b = 0;
            a[0] = 1;
            self.a[0] = 1;
            a.b[0] = 1;
            a[0] += 1;
            self.a[0] += 1;
            a.b[0] += 1;
            a = a[0];
            a = self.a[0];
            a = a.b[0];
            global.a = 0;
            global.a[0] = 0;
            self.a.b = 1;
            a.b = 1;
            a = 1;
            other.a[0] = 1;
            global.a[0] = 1;
            a[0].b = 1;
            a = a[0].b;
            a = a[0];
            a[0].b[0]("a");
            a[0].b.c("a");
            a[0].b("a");
            """,
            """
            pushi.e 0
            pop.v.i builtin.a
            pushi.e 0
            pop.v.i self.a
            push.v builtin.b
            pop.v.v builtin.a
            push.v self.b
            pop.v.v builtin.a
            push.v builtin.a
            pushi.e 1
            add.i.v
            pop.v.v builtin.a
            push.v self.a
            pushi.e 1
            add.i.v
            pop.v.v self.a
            push.v builtin.a
            push.e 1
            add.i.v
            pop.v.v builtin.a
            push.v self.a
            push.e 1
            add.i.v
            pop.v.v self.a
            pushi.e 0
            push.v builtin.a
            pushi.e -9
            pop.v.i [stacktop]self.b
            pushi.e 1
            conv.i.v
            pushi.e -6
            pushi.e 0
            pop.v.v [array]self.a
            pushi.e 1
            conv.i.v
            call.i @@This@@ 0
            pushi.e -9
            pushi.e 0
            pop.v.v [array]self.a
            pushi.e 1
            conv.i.v
            push.v builtin.a
            pushi.e -9
            pushi.e 0
            pop.v.v [array]self.b
            pushi.e -6
            pushi.e 0
            dup.i 1
            push.v [array]self.a
            pushi.e 1
            add.i.v
            pop.i.v [array]self.a
            call.i @@This@@ 0
            pushi.e -9
            pushi.e 0
            dup.i 5
            push.v [array]self.a
            pushi.e 1
            add.i.v
            pop.i.v [array]self.a
            push.v builtin.a
            pushi.e -9
            pushi.e 0
            dup.i 5
            push.v [array]self.b
            pushi.e 1
            add.i.v
            pop.i.v [array]self.b
            pushi.e -6
            pushi.e 0
            push.v [array]self.a
            pop.v.v builtin.a
            call.i @@This@@ 0
            pushi.e -9
            pushi.e 0
            push.v [array]self.a
            pop.v.v builtin.a
            push.v builtin.a
            pushi.e -9
            pushi.e 0
            push.v [array]self.b
            pop.v.v builtin.a
            pushi.e 0
            pop.v.i global.a
            pushi.e 0
            conv.i.v
            call.i @@Global@@ 0
            pushi.e -9
            pushi.e 0
            pop.v.v [array]self.a
            pushi.e 1
            push.v self.a
            pushi.e -9
            pop.v.i [stacktop]self.b
            pushi.e 1
            push.v builtin.a
            pushi.e -9
            pop.v.i [stacktop]self.b
            pushi.e 1
            pop.v.i builtin.a
            pushi.e 1
            conv.i.v
            call.i @@Other@@ 0
            pushi.e -9
            pushi.e 0
            pop.v.v [array]self.a
            pushi.e 1
            conv.i.v
            call.i @@Global@@ 0
            pushi.e -9
            pushi.e 0
            pop.v.v [array]self.a
            pushi.e 1
            pushi.e -1
            pushi.e 0
            push.v [array]self.a
            pushi.e -9
            pop.v.i [stacktop]self.b
            pushi.e -1
            pushi.e 0
            push.v [array]self.a
            pushi.e -9
            push.v [stacktop]self.b
            pop.v.v builtin.a
            pushi.e -6
            pushi.e 0
            push.v [array]self.a
            pop.v.v builtin.a
            push.s "a"
            conv.s.v
            pushi.e -1
            pushi.e 0
            push.v [array]self.a
            pushi.e -9
            pushi.e 0
            push.v [array]self.b
            dup.v 0
            callv.v 1
            popz.v
            pushi.e -1
            pushi.e 0
            push.v [array]self.a
            pushi.e -9
            push.v [stacktop]self.b
            push.s "a"
            conv.s.v
            dup.v 1 1
            dup.v 0
            push.v stacktop.c
            callv.v 1
            popz.v
            pushi.e -1
            pushi.e 0
            push.v [array]self.a
            push.s "a"
            conv.s.v
            dup.v 1 1
            dup.v 0
            push.v stacktop.b
            callv.v 1
            popz.v
            """,
            false,
            gameContext
        );
    }

    [Fact]
    public void TestFunctionReferencesModern()
    {
        Underanalyzer.Mock.GameContextMock gameContext = new()
        {
            UsingAssetReferences = true,
            UsingSelfToBuiltin = true,
            UsingNewFunctionVariables = true,
            UsingFunctionScriptReferences = true
        };
        gameContext.DefineMockAsset(AssetType.Script, 124, "AnotherFunction");
        gameContext.DefineMockAsset(AssetType.Script, 125, "global_func_AnotherFunction");
        gameContext.DefineMockAsset(AssetType.Script, 123, "ExampleFunction");
        ((Underanalyzer.Decompiler.GlobalFunctions)gameContext.GlobalFunctions)
            .DefineFunction("ExampleFunction", new Underanalyzer.Mock.GMFunction("ExampleFunction"));
        ((Underanalyzer.Decompiler.GlobalFunctions)gameContext.GlobalFunctions)
            .DefineFunction("AnotherFunction", new Underanalyzer.Mock.GMFunction("global_func_AnotherFunction"));
        ((Underanalyzer.Mock.BuiltinsMock)gameContext.Builtins).BuiltinFunctions["script_execute"] =
            new("script_execute", 0, int.MaxValue);
        TestUtil.AssertBytecode(
            """
            test = ExampleFunction;
            test2 = script_execute;
            test3 = LocalFunction;
            test4 = AnotherFunction;
            ExampleFunction.a += 1;
            LocalFunction.a += 1;
            AnotherFunction.a += 1;
            
            function LocalFunction()
            {
            }
            """,
            """
            pushref.i ExampleFunction
            pop.v.v builtin.test
            push.i [function]script_execute
            pop.v.i builtin.test2
            pushref.i global_func_LocalFunction
            pop.v.v builtin.test3
            pushref.i global_func_AnotherFunction
            pop.v.v builtin.test4
            push.i [function]ExampleFunction
            conv.i.v
            call.i static_get 1
            pushi.e -9
            dup.i 4
            push.v [stacktop]self.a
            pushi.e 1
            add.i.v
            pop.i.v [stacktop]self.a
            push.i [function]global_func_LocalFunction
            conv.i.v
            call.i static_get 1
            pushi.e -9
            dup.i 4
            push.v [stacktop]self.a
            pushi.e 1
            add.i.v
            pop.i.v [stacktop]self.a
            push.i [function]global_func_AnotherFunction
            conv.i.v
            call.i static_get 1
            pushi.e -9
            dup.i 4
            push.v [stacktop]self.a
            pushi.e 1
            add.i.v
            pop.i.v [stacktop]self.a
            b [2]
            
            > global_func_LocalFunction (locals=0, args=0)
            :[1]
            exit.i
            
            :[2]
            push.i [function]global_func_LocalFunction
            conv.i.v
            pushi.e -1
            conv.i.v
            call.i method 2
            dup.v 0
            pushi.e -1
            pop.v.v [stacktop]self.LocalFunction
            popz.v
            """,
            true,
            gameContext
        );
    }

    [Fact]
    public void TestFunctionReferencesRegularModern()
    {
        Underanalyzer.Mock.GameContextMock gameContext = new()
        {
            UsingAssetReferences = true,
            UsingSelfToBuiltin = true,
            UsingNewFunctionVariables = true,
            UsingObjectFunctionForesight = true,
            UsingFunctionScriptReferences = true
        };
        gameContext.DefineMockAsset(AssetType.Script, 124, "AnotherFunction");
        gameContext.DefineMockAsset(AssetType.Script, 125, "global_func_AnotherFunction");
        gameContext.DefineMockAsset(AssetType.Script, 123, "ExampleFunction");
        ((Underanalyzer.Decompiler.GlobalFunctions)gameContext.GlobalFunctions)
            .DefineFunction("ExampleFunction", new Underanalyzer.Mock.GMFunction("ExampleFunction"));
        ((Underanalyzer.Decompiler.GlobalFunctions)gameContext.GlobalFunctions)
            .DefineFunction("AnotherFunction", new Underanalyzer.Mock.GMFunction("global_func_AnotherFunction"));
        ((Underanalyzer.Mock.BuiltinsMock)gameContext.Builtins).BuiltinFunctions["script_execute"] =
            new("script_execute", 0, int.MaxValue);
        TestUtil.AssertBytecode(
            """
            test = ExampleFunction;
            test2 = script_execute;
            test3 = LocalFunction;
            test4 = AnotherFunction;
            ExampleFunction.a += 1;
            LocalFunction.a += 1;
            AnotherFunction.a += 1;
            
            function LocalFunction()
            {
            }
            """,
            """
            pushref.i ExampleFunction
            pop.v.v builtin.test
            push.i [function]script_execute
            pop.v.i builtin.test2
            push.i [function]regular_func_LocalFunction
            pop.v.i builtin.test3
            pushref.i global_func_AnotherFunction
            pop.v.v builtin.test4
            push.i [function]ExampleFunction
            conv.i.v
            call.i static_get 1
            pushi.e -9
            dup.i 4
            push.v [stacktop]self.a
            pushi.e 1
            add.i.v
            pop.i.v [stacktop]self.a
            push.i [function]regular_func_LocalFunction
            conv.i.v
            call.i static_get 1
            pushi.e -9
            dup.i 4
            push.v [stacktop]self.a
            pushi.e 1
            add.i.v
            pop.i.v [stacktop]self.a
            push.i [function]global_func_AnotherFunction
            conv.i.v
            call.i static_get 1
            pushi.e -9
            dup.i 4
            push.v [stacktop]self.a
            pushi.e 1
            add.i.v
            pop.i.v [stacktop]self.a
            b [2]

            > regular_func_LocalFunction (locals=0, args=0)
            :[1]
            exit.i

            :[2]
            push.i [function]regular_func_LocalFunction
            conv.i.v
            pushi.e -1
            conv.i.v
            call.i method 2
            dup.v 0
            pushi.e -1
            pop.v.v [stacktop]self.LocalFunction
            popz.v
            """,
            false,
            gameContext
        );
    }

    [Fact]
    public void TestFunctionReferencesRegularExtraModern()
    {
        Underanalyzer.Mock.GameContextMock gameContext = new()
        {
            UsingAssetReferences = true,
            UsingSelfToBuiltin = true,
            UsingNewFunctionVariables = true,
            UsingObjectFunctionForesight = true,
            UsingFunctionScriptReferences = true,
            UsingNewFunctionResolution = true
        };
        gameContext.DefineMockAsset(AssetType.Script, 124, "AnotherFunction");
        gameContext.DefineMockAsset(AssetType.Script, 125, "global_func_AnotherFunction");
        gameContext.DefineMockAsset(AssetType.Script, 123, "ExampleFunction");
        ((Underanalyzer.Decompiler.GlobalFunctions)gameContext.GlobalFunctions)
            .DefineFunction("ExampleFunction", new Underanalyzer.Mock.GMFunction("ExampleFunction"));
        ((Underanalyzer.Decompiler.GlobalFunctions)gameContext.GlobalFunctions)
            .DefineFunction("AnotherFunction", new Underanalyzer.Mock.GMFunction("global_func_AnotherFunction"));
        ((Underanalyzer.Mock.BuiltinsMock)gameContext.Builtins).BuiltinFunctions["script_execute"] =
            new("script_execute", 0, int.MaxValue);
        TestUtil.AssertBytecode(
            """
            test = ExampleFunction;
            test2 = script_execute;
            test3 = LocalFunction;
            test4 = AnotherFunction;
            ExampleFunction.a += 1;
            LocalFunction.a += 1;
            AnotherFunction.a += 1;
            
            function LocalFunction()
            {
            }
            """,
            """
            pushref.i ExampleFunction
            pop.v.v builtin.test
            push.i [function]script_execute
            pop.v.i builtin.test2
            pushref.i regular_func_LocalFunction
            pop.v.v builtin.test3
            pushref.i global_func_AnotherFunction
            pop.v.v builtin.test4
            push.i [function]ExampleFunction
            conv.i.v
            call.i static_get 1
            pushi.e -9
            dup.i 4
            push.v [stacktop]self.a
            pushi.e 1
            add.i.v
            pop.i.v [stacktop]self.a
            push.i [function]regular_func_LocalFunction
            conv.i.v
            call.i static_get 1
            pushi.e -9
            dup.i 4
            push.v [stacktop]self.a
            pushi.e 1
            add.i.v
            pop.i.v [stacktop]self.a
            push.i [function]global_func_AnotherFunction
            conv.i.v
            call.i static_get 1
            pushi.e -9
            dup.i 4
            push.v [stacktop]self.a
            pushi.e 1
            add.i.v
            pop.i.v [stacktop]self.a
            b [2]

            > regular_func_LocalFunction (locals=0, args=0)
            :[1]
            exit.i

            :[2]
            push.i [function]regular_func_LocalFunction
            conv.i.v
            pushi.e -1
            conv.i.v
            call.i method 2
            dup.v 0
            pushi.e -1
            pop.v.v [stacktop]self.LocalFunction
            popz.v
            """,
            false,
            gameContext
        );
    }

    [Fact]
    public void TestFunctionReferencesEarlyGMLv2()
    {
        Underanalyzer.Mock.GameContextMock gameContext = new()
        {
            UsingAssetReferences = false
        };
        gameContext.DefineMockAsset(AssetType.Script, 124, "AnotherFunction");
        gameContext.DefineMockAsset(AssetType.Script, 125, "global_func_AnotherFunction");
        gameContext.DefineMockAsset(AssetType.Script, 123, "ExampleFunction");
        ((Underanalyzer.Decompiler.GlobalFunctions)gameContext.GlobalFunctions)
            .DefineFunction("ExampleFunction", new Underanalyzer.Mock.GMFunction("ExampleFunction"));
        ((Underanalyzer.Decompiler.GlobalFunctions)gameContext.GlobalFunctions)
            .DefineFunction("AnotherFunction", new Underanalyzer.Mock.GMFunction("global_func_AnotherFunction"));
        ((Underanalyzer.Mock.BuiltinsMock)gameContext.Builtins).BuiltinFunctions["script_execute"] =
            new("script_execute", 0, int.MaxValue);
        TestUtil.AssertBytecode(
            """
            test = ExampleFunction;
            test2 = script_execute;
            test3 = LocalFunction;
            test4 = AnotherFunction;
            
            function LocalFunction()
            {
            }
            """,
            """
            push.i [function]ExampleFunction
            pop.v.i self.test
            push.i [function]script_execute
            pop.v.i self.test2
            push.i [function]global_func_LocalFunction
            pop.v.i self.test3
            push.i [function]global_func_AnotherFunction
            pop.v.i self.test4
            b [2]

            > global_func_LocalFunction (locals=0, args=0)
            :[1]
            exit.i

            :[2]
            push.i [function]global_func_LocalFunction
            conv.i.v
            pushi.e -1
            conv.i.v
            call.i method 2
            dup.v 0
            pushi.e -6
            pop.v.v [stacktop]self.LocalFunction
            popz.v
            """,
            true,
            gameContext
        );
    }

    [Fact]
    public void TestFunctionReferencesRegularEarlyGMLv2()
    {
        Underanalyzer.Mock.GameContextMock gameContext = new()
        {
            UsingAssetReferences = false
        };
        gameContext.DefineMockAsset(AssetType.Script, 124, "AnotherFunction");
        gameContext.DefineMockAsset(AssetType.Script, 125, "global_func_AnotherFunction");
        gameContext.DefineMockAsset(AssetType.Script, 123, "ExampleFunction");
        ((Underanalyzer.Decompiler.GlobalFunctions)gameContext.GlobalFunctions)
            .DefineFunction("ExampleFunction", new Underanalyzer.Mock.GMFunction("ExampleFunction"));
        ((Underanalyzer.Decompiler.GlobalFunctions)gameContext.GlobalFunctions)
            .DefineFunction("AnotherFunction", new Underanalyzer.Mock.GMFunction("global_func_AnotherFunction"));
        ((Underanalyzer.Mock.BuiltinsMock)gameContext.Builtins).BuiltinFunctions["script_execute"] =
            new("script_execute", 0, int.MaxValue);
        TestUtil.AssertBytecode(
            """
            test = ExampleFunction;
            test2 = script_execute;
            test3 = LocalFunction;
            test4 = AnotherFunction;
            
            function LocalFunction()
            {
            }
            """,
            """
            push.i [function]ExampleFunction
            pop.v.i self.test
            push.i [function]script_execute
            pop.v.i self.test2
            push.v self.LocalFunction
            pop.v.v self.test3
            push.i [function]global_func_AnotherFunction
            pop.v.i self.test4
            b [2]

            > regular_func_LocalFunction (locals=0, args=0)
            :[1]
            exit.i

            :[2]
            push.i [function]regular_func_LocalFunction
            conv.i.v
            pushi.e -1
            conv.i.v
            call.i method 2
            dup.v 0
            pushi.e -6
            pop.v.v [stacktop]self.LocalFunction
            popz.v
            """,
            false,
            gameContext
        );
    }

    [Fact]
    public void TestFunctionReferencesPreGMLv2()
    {
        Underanalyzer.Mock.GameContextMock gameContext = new()
        {
            UsingAssetReferences = false,
            UsingGMLv2 = false
        };
        gameContext.DefineMockAsset(AssetType.Script, 123, "ExampleFunction");
        ((Underanalyzer.Decompiler.GlobalFunctions)gameContext.GlobalFunctions)
            .DefineFunction("ExampleFunction", new Underanalyzer.Mock.GMFunction("ExampleFunction"));
        TestUtil.AssertBytecode(
            """
            a = ExampleFunction;
            """,
            """
            pushi.e 123
            pop.v.i self.a
            """,
            false,
            gameContext
        );
    }

    [Fact]
    public void TestFunctionReferencesErrorPreGMLv2()
    {
        Underanalyzer.Mock.GameContextMock gameContext = new()
        {
            UsingAssetReferences = false,
            UsingGMLv2 = false
        };
        ((Underanalyzer.Mock.BuiltinsMock)gameContext.Builtins).BuiltinFunctions["script_execute"] =
            new("script_execute", 0, int.MaxValue);
        Assert.Throws<TestCompileErrorException>(() =>
        {
            TestUtil.AssertBytecode(
                """
                a = script_execute;
                """,
                "",
                false,
                gameContext
            );
        });
    }

    [Fact]
    public void TestTryCatchBasic()
    {
        TestUtil.AssertBytecode(
            """
            try
            {
                throw "Error!";
            }
            catch (ex)
            {
                a = ex;
            }
            """,
            """
            :[0]
            push.i 112
            conv.i.v
            push.i 60
            conv.i.v
            call.i @@try_hook@@ 2
            popz.v
            push.s "Error!"
            conv.s.v
            call.i @@throw@@ 1
            b [2]

            :[1]
            pop.v.v local.ex
            call.i @@try_unhook@@ 0
            popz.v
            pushloc.v local.ex
            pop.v.v self.a
            call.i @@finish_catch@@ 0
            popz.v
            b [3]

            :[2]
            call.i @@try_unhook@@ 0
            popz.v

            :[3]
            """
        );
    }

    [Fact]
    public void TestTryCatchFinally1()
    {
        TestUtil.AssertBytecode(
            """
            try
            {
                throw "Error!";
            }
            catch (ex)
            {
                a = ex;
            }
            finally
            {
                b = 123;
            }
            """,
            """
            :[0]
            push.i 112
            conv.i.v
            push.i 60
            conv.i.v
            call.i @@try_hook@@ 2
            popz.v
            push.s "Error!"
            conv.s.v
            call.i @@throw@@ 1
            b [2]

            :[1]
            pop.v.v local.ex
            call.i @@try_unhook@@ 0
            popz.v
            pushloc.v local.ex
            pop.v.v self.a
            call.i @@finish_catch@@ 0
            popz.v
            b [3]

            :[2]
            call.i @@try_unhook@@ 0
            popz.v

            :[3]
            pushi.e 123
            pop.v.i self.b
            call.i @@finish_finally@@ 0
            popz.v
            b [4]

            :[4]
            """
        );
    }

    [Fact]
    public void TestTryCatchFinally2()
    {
        TestUtil.AssertBytecode(
            """
            try
            {
                throw "Error!";
                return 456;
            }
            catch (ex)
            {
                a = ex;
            }
            finally
            {
                b = 123;
            }
            """,
            """
            :[0]
            push.i 184
            conv.i.v
            push.i 132
            conv.i.v
            call.i @@try_hook@@ 2
            popz.v
            pushi.e 123
            pop.v.i self.b
            push.s "Error!"
            conv.s.v
            call.i @@throw@@ 1
            pushi.e 456
            pop.v.i local.copyVar
            pushi.e 123
            pop.v.i self.b
            pushloc.v local.copyVar
            pop.v.v local.$$$$temp$$$$
            call.i @@try_unhook@@ 0
            push.v local.$$$$temp$$$$
            ret.v

            :[1]
            b [3]

            :[2]
            pop.v.v local.ex
            call.i @@try_unhook@@ 0
            popz.v
            pushloc.v local.ex
            pop.v.v self.a
            call.i @@finish_catch@@ 0
            popz.v
            b [4]

            :[3]
            call.i @@try_unhook@@ 0
            popz.v

            :[4]
            pushi.e 123
            pop.v.i self.b
            call.i @@finish_finally@@ 0
            popz.v
            b [5]

            :[5]
            """
        );
    }

    [Fact]
    public void TestTryFinally()
    {
        TestUtil.AssertBytecode(
            """
            try
            {
                return 123;
            }
            finally
            {
                a = "finally code";
            }
            """,
            """
            push.i 100
            conv.i.v
            push.i -1
            conv.i.v
            call.i @@try_hook@@ 2
            popz.v
            pushi.e 123
            pop.v.i local.copyVar
            push.s "finally code"
            pop.v.s self.a
            pushloc.v local.copyVar
            pop.v.v local.$$$$temp$$$$
            call.i @@try_unhook@@ 0
            push.v local.$$$$temp$$$$
            ret.v

            :[1]
            call.i @@try_unhook@@ 0
            popz.v
            push.s "finally code"
            pop.v.s self.a
            call.i @@finish_finally@@ 0
            popz.v
            b [2]

            :[2]
            """
        );
    }

    [Fact]
    public void TestTryBreakContinue()
    {
        TestUtil.AssertBytecode(
            """
            repeat (123)
            {
                try
                {
                    if (c)
                    {
                        continue;
                    }
                    if (d)
                    {
                        break;
                    }
                }
                catch (ex)
                {
                }
            }
            """,
            """
            :[0]
            pushi.e 123
            dup.i 0
            push.i 0
            cmp.i.i LTE
            bt [19]

            :[1]
            pushi.e 0
            pop.v.i local.__yy_breakEx0
            pushi.e 0
            pop.v.i local.__yy_continueEx0
            push.i 224
            conv.i.v
            push.i 188
            conv.i.v
            call.i @@try_hook@@ 2
            popz.v

            :[2]
            pushi.e 1
            bf [11]

            :[3]
            pushloc.v local.__yy_continueEx0
            conv.v.b
            bf [5]

            :[4]
            b [11]

            :[5]
            push.v self.c
            conv.v.b
            bf [7]

            :[6]
            pushi.e 1
            pop.v.i local.__yy_continueEx0
            b [2]

            :[7]
            push.v self.d
            conv.v.b
            bf [9]

            :[8]
            pushi.e 1
            pop.v.i local.__yy_breakEx0
            b [11]

            :[9]
            b [11]

            :[10]
            b [2]

            :[11]
            b [13]

            :[12]
            pop.v.v local.ex
            call.i @@try_unhook@@ 0
            popz.v
            call.i @@finish_catch@@ 0
            popz.v
            b [14]

            :[13]
            call.i @@try_unhook@@ 0
            popz.v

            :[14]
            pushloc.v local.__yy_continueEx0
            conv.v.b
            bf [16]

            :[15]
            b [18]

            :[16]
            pushloc.v local.__yy_breakEx0
            conv.v.b
            bf [18]

            :[17]
            b [19]

            :[18]
            push.i 1
            sub.i.i
            dup.i 0
            conv.i.b
            bt [1]

            :[19]
            popz.i
            """,
            false,
            new Underanalyzer.Mock.GameContextMock()
            {
                UsingExtraRepeatInstruction = true
            }
        );
    }

    [Fact]
    public void TestTryBreakBusted()
    {
        TestUtil.AssertBytecode(
            """
            try
            {
                while (b)
                {
                    break;
                }
            }
            catch (ex)
            {
            }
            """,
            """
            :[0]
            pushi.e 0
            pop.v.i local.__yy_breakEx0
            pushi.e 0
            pop.v.i local.__yy_continueEx0
            push.i 172
            conv.i.v
            push.i 136
            conv.i.v
            call.i @@try_hook@@ 2
            popz.v

            :[1]
            pushi.e 1
            bf [9]

            :[2]
            pushloc.v local.__yy_continueEx0
            conv.v.b
            bf [4]

            :[3]
            b [9]

            :[4]
            push.v self.b
            conv.v.b
            bf [7]

            :[5]
            pushi.e 1
            pop.v.i local.__yy_breakEx0
            b [7]

            :[6]
            b [4]

            :[7]
            b [9]

            :[8]
            b [1]

            :[9]
            b [11]

            :[10]
            pop.v.v local.ex
            call.i @@try_unhook@@ 0
            popz.v
            call.i @@finish_catch@@ 0
            popz.v
            b [12]

            :[11]
            call.i @@try_unhook@@ 0
            popz.v

            :[12]
            """
        );
    }

    [Fact]
    public void TestTryBreakFixed()
    {
        TestUtil.AssertBytecode(
            """
            try
            {
                while (b)
                {
                    break;
                }
            }
            catch (ex)
            {
            }
            """,
            """
            :[0]
            push.i 100
            conv.i.v
            push.i 64
            conv.i.v
            call.i @@try_hook@@ 2
            popz.v

            :[1]
            push.v builtin.b
            conv.v.b
            bf [4]

            :[2]
            b [4]

            :[3]
            b [1]

            :[4]
            b [6]

            :[5]
            pop.v.v local.ex
            call.i @@try_unhook@@ 0
            popz.v
            call.i @@finish_catch@@ 0
            popz.v
            b [7]

            :[6]
            call.i @@try_unhook@@ 0
            popz.v

            :[7]
            """,
            false,
            new Underanalyzer.Mock.GameContextMock()
            {
                UsingBetterTryBreakContinue = true,
                UsingSelfToBuiltin = true
            }
        );
    }

    [Fact]
    public void TestTryCatchBreakContinue()
    {
        TestUtil.AssertBytecode(
            """
            repeat (123)
            {
                try
            	{
                    continue;
                    break;
                }
                catch (ex)
                {
                    continue;
                    break;
                }
            }
            """,
            """
            :[0]
            pushi.e 123
            dup.i 0
            push.i 0
            cmp.i.i LTE
            bt [25]

            :[1]
            pushi.e 0
            pop.v.i local.__yy_breakEx0
            pushi.e 0
            pop.v.i local.__yy_continueEx0
            push.i 260
            conv.i.v
            push.i 156
            conv.i.v
            call.i @@try_hook@@ 2
            popz.v

            :[2]
            pushi.e 1
            bf [9]

            :[3]
            pushloc.v local.__yy_continueEx0
            conv.v.b
            bf [5]

            :[4]
            b [9]

            :[5]
            pushi.e 1
            pop.v.i local.__yy_continueEx0
            b [2]

            :[6]
            pushi.e 1
            pop.v.i local.__yy_breakEx0
            b [9]

            :[7]
            b [9]

            :[8]
            b [2]

            :[9]
            b [19]

            :[10]
            pop.v.v local.ex
            call.i @@try_unhook@@ 0
            popz.v

            :[11]
            pushi.e 1
            bf [18]

            :[12]
            pushloc.v local.__yy_continueEx0
            conv.v.b
            bf [14]

            :[13]
            b [18]

            :[14]
            pushi.e 1
            pop.v.i local.__yy_continueEx0
            b [11]

            :[15]
            pushi.e 1
            pop.v.i local.__yy_breakEx0
            b [18]

            :[16]
            b [18]

            :[17]
            b [11]

            :[18]
            call.i @@finish_catch@@ 0
            popz.v
            b [20]

            :[19]
            call.i @@try_unhook@@ 0
            popz.v

            :[20]
            pushloc.v local.__yy_continueEx0
            conv.v.b
            bf [22]

            :[21]
            b [24]

            :[22]
            pushloc.v local.__yy_breakEx0
            conv.v.b
            bf [24]

            :[23]
            b [25]

            :[24]
            push.i 1
            sub.i.i
            dup.i 0
            conv.i.b
            bt [1]

            :[25]
            popz.i
            """,
            false,
            new Underanalyzer.Mock.GameContextMock()
            {
                UsingExtraRepeatInstruction = true
            }
        );
    }

    [Fact]
    public void TestTrySwitchBreak()
    {
        TestUtil.AssertBytecode(
            """
            try
            {
                switch (a)
                {
                    default:
                        break;
                }
            }
            catch (ex)
            {
            }
            """,
            """
            :[0]
            push.i 100
            conv.i.v
            push.i 64
            conv.i.v
            call.i @@try_hook@@ 2
            popz.v
            push.v self.a
            b [2]

            :[1]
            b [3]

            :[2]
            b [3]

            :[3]
            popz.v
            b [5]

            :[4]
            pop.v.v local.ex
            call.i @@try_unhook@@ 0
            popz.v
            call.i @@finish_catch@@ 0
            popz.v
            b [6]

            :[5]
            call.i @@try_unhook@@ 0
            popz.v

            :[6]
            """
        );
    }

    [Fact]
    public void TestTryFinallyError()
    {
        Assert.Throws<TestCompileErrorException>(() =>
        {
            TestUtil.AssertBytecode(
                """
                try
                {
                }
                finally
                {
                    exit;
                    return 123;
                    break;
                    continue;
                }
                """,
                ""
            );
        });
    }

    [Fact]
    public void TestBuiltinSelfOther()
    {
        TestUtil.AssertBytecode(
            """
            sprite_index = other.sprite_index;
            image_blend = other.image_blend;
            image_xscale = other.image_xscale;
            image_yscale = other.image_yscale;
            depth = other.depth - 1;
            a *= a;
            """,
            """
            push.v other.sprite_index
            pop.v.v self.sprite_index
            push.v other.image_blend
            pop.v.v self.image_blend
            push.v other.image_xscale
            pop.v.v self.image_xscale
            push.v other.image_yscale
            pop.v.v self.image_yscale
            push.v other.depth
            pushi.e 1
            sub.i.v
            pop.v.v self.depth
            push.v self.a
            push.v self.a
            mul.v.v
            pop.v.v self.a
            """
        );
    }

    [Fact]
    public void TestHexColor()
    {
        TestUtil.AssertBytecode(
            """
            a = #123456;
            """,
            """
            push.i 5649426
            pop.v.i self.a
            """
        );
    }

    [Fact]
    public void TestStringOptimization()
    {
        TestUtil.AssertBytecode(
            """
            a = "b" + "c" + "d";
            """,
            """
            push.s "bcd"
            pop.v.s self.a
            """
        );
    }

    [Fact]
    public void TestAssetArray()
    {
        Underanalyzer.Mock.GameContextMock gameContext = new()
        {
            UsingGMLv2 = false,
            UsingAssetReferences = false
        };
        gameContext.DefineMockAsset(AssetType.Object, 123, "obj_test");
        TestUtil.AssertBytecode(
            """
            obj_test.a[0] = 1;
            """,
            """
            pushi.e 1
            pushi.e 123
            pushi.e 0
            pop.v.i [array]self.a
            """,
            false,
            gameContext
        );
    }

    [Fact]
    public void TestArgumentsOld()
    {
        Underanalyzer.Mock.GameContextMock gameContext = new()
        {
            UsingSelfToBuiltin = false
        };
        TestUtil.AssertBytecode(
            """
            a = argument0;
            b = argument0[1];
            c = argument[1];
            function args(a)
            {
                test = a;
                test2 = a[0];
                test3 = argument0;
                test4 = argument0[0];
                test5 = argument[0];
            }
            """,
            """
            pushbltn.v builtin.argument0
            pop.v.v self.a
            pushi.e -15
            pushi.e 1
            push.v [array]self.argument0
            pop.v.v self.b
            pushi.e -15
            pushi.e 1
            push.v [array]self.argument
            pop.v.v self.c
            b [2]

            > regular_func_args (locals=0, argc=1)
            :[1]
            push.v arg.argument0
            pop.v.v self.test
            pushi.e -15
            pushi.e 0
            push.v [array]self.argument0
            pop.v.v self.test2
            pushbltn.v builtin.argument0
            pop.v.v self.test3
            pushi.e -6
            pushi.e 0
            push.v [array]self.argument0
            pop.v.v self.test4
            pushi.e -15
            pushi.e 0
            push.v [array]self.argument
            pop.v.v self.test5
            exit.i

            :[2]
            push.i [function]regular_func_args
            conv.i.v
            pushi.e -1
            conv.i.v
            call.i method 2
            dup.v 0
            pushi.e -6
            pop.v.v [stacktop]self.args
            popz.v
            """,
            false,
            gameContext
        );
    }

    [Fact]
    public void TestArguments()
    {
        Underanalyzer.Mock.GameContextMock gameContext = new()
        {
            UsingSelfToBuiltin = true
        };
        TestUtil.AssertBytecode(
            """
            a = argument0;
            b = argument0[1];
            c = argument[1];
            function args(a)
            {
                test = a;
                test2 = a[0];
                test3 = argument0;
                test4 = argument0[0];
                test5 = argument[0];
            }
            """,
            """
            push.v arg.argument0
            pop.v.v builtin.a
            pushi.e -15
            pushi.e 1
            push.v [array]self.argument0
            pop.v.v builtin.b
            pushi.e -15
            pushi.e 1
            push.v [array]self.argument
            pop.v.v builtin.c
            b [2]

            > regular_func_args (locals=0, argc=1)
            :[1]
            push.v arg.argument0
            pop.v.v builtin.test
            pushi.e -15
            pushi.e 0
            push.v [array]self.argument0
            pop.v.v builtin.test2
            pushbltn.v builtin.argument0
            pop.v.v builtin.test3
            pushi.e -6
            pushi.e 0
            push.v [array]self.argument0
            pop.v.v builtin.test4
            pushi.e -15
            pushi.e 0
            push.v [array]self.argument
            pop.v.v builtin.test5
            exit.i

            :[2]
            push.i [function]regular_func_args
            conv.i.v
            pushi.e -1
            conv.i.v
            call.i method 2
            dup.v 0
            pushi.e -6
            pop.v.v [stacktop]self.args
            popz.v
            """,
            false,
            gameContext
        );
    }

    [Fact]
    public void TestStruct()
    {
        Underanalyzer.Mock.GameContextMock gameContext = new()
        {
            UsingSelfToBuiltin = true,
            UsingNewFunctionVariables = true
        };
        TestUtil.AssertBytecode(
            """
            a = 
            {
                b: c + d
            };
            """,
            """
            push.v builtin.c
            push.v builtin.d
            add.v.v
            b [2]

            > struct_func___struct__1 (locals=0, argc=0)
            :[1]
            pushi.e -15
            pushi.e 0
            push.v [array]self.argument
            pop.v.v self.b
            exit.i

            :[2]
            push.i [function]struct_func___struct__1
            conv.i.v
            call.i @@NullObject@@ 0
            call.i method 2
            dup.v 0
            pushi.e -5
            pop.v.v [stacktop]global.__struct__1
            call.i @@NewGMLObject@@ 2
            pop.v.v builtin.a
            """,
            false,
            gameContext
        );
    }

    [Fact]
    public void TestEscapeQuotes()
    {
        TestUtil.AssertBytecode(
            """
            a = "This is a \"test!\"";
            """,
            """
            push.s "This is a \"test!\""
            pop.v.s self.a
            """
        );
    }

    [Fact]
    public void TestRoomInstances()
    {
        Underanalyzer.Mock.GameContextMock gameContext = new()
        {
            UsingSelfToBuiltin = true
        };
        TestUtil.AssertBytecode(
            """
            a = (101234).b;
            (101234).c = d;
            """,
            """
            push.v [instance]1234.b
            pop.v.v builtin.a
            push.v builtin.d
            pop.v.v [instance]1234.c
            """,
            false,
            gameContext
        );
    }

    [Fact]
    public void TestStatic()
    {
        TestUtil.AssertBytecode(
            """
            function statictest()
            {
                static a = 123;
                static b = a + 1;
            }
            """,
            """
            b [4]

            > regular_func_statictest (locals=0, args=0)
            :[1]
            isstaticok.e
            bt [3]

            :[2]
            pushi.e 123
            pop.v.i static.a
            push.v static.a
            pushi.e 1
            add.i.v
            pop.v.v static.b

            :[3]
            setstatic.e
            exit.i

            :[4]
            push.i [function]regular_func_statictest
            conv.i.v
            pushi.e -1
            conv.i.v
            call.i method 2
            dup.v 0
            pushi.e -6
            pop.v.v [stacktop]self.statictest
            popz.v
            """
        );
    }

    [Fact]
    public void TestNewRepeatGeneration()
    {
        Underanalyzer.Mock.GameContextMock gameContext = new()
        {
            UsingSelfToBuiltin = true,
            UsingExtraRepeatInstruction = false
        };
        TestUtil.AssertBytecode(
            """
            repeat (123)
            {
            	if (a)
            	{
            		exit;
            	}
            }
            """,
            """
            pushi.e 123
            dup.i 0
            push.i 0
            cmp.i.i LTE
            bt [4]

            :[1]
            push.v builtin.a
            conv.v.b
            bf [3]

            :[2]
            popz.i
            exit.i

            :[3]
            pushi.e 1
            sub.i.i
            dup.i 0
            bt [1]

            :[4]
            popz.i
            """,
            false,
            gameContext
        );
    }

    [Fact]
    public void TestConstantConflicts()
    {
        Underanalyzer.Mock.GameContextMock gameContext = new()
        {
            UsingAssetReferences = false
        };
        gameContext.DefineMockAsset(AssetType.Sprite, 123, "spr_test");
        TestUtil.AssertBytecode(
            """
            a = spr_test;
            self.spr_test = 456;
            global.spr_test = 789;
            b = global.spr_test;
            spr_test();
            global.spr_test();
            
            a = test_constant;
            self.test_constant = 456;
            global.test_constant = 789;
            b = global.test_constant;
            test_constant();
            global.test_constant();
            """,
            """
            pushi.e 123
            pop.v.i self.a
            pushi.e 456
            pop.v.i self.spr_test
            pushi.e 789
            pop.v.i global.spr_test
            pushglb.v global.spr_test
            pop.v.v self.b
            call.i @@This@@ 0
            push.v builtin.spr_test
            callv.v 0
            popz.v
            call.i @@Global@@ 0
            dup.v 0 1
            dup.v 0
            push.v stacktop.spr_test
            callv.v 0
            popz.v
            pushi.e 128
            pop.v.i self.a
            pushi.e 456
            pop.v.i self.test_constant
            pushi.e 789
            pop.v.i global.test_constant
            pushglb.v global.test_constant
            pop.v.v self.b
            call.i @@This@@ 0
            push.v builtin.test_constant
            callv.v 0
            popz.v
            call.i @@Global@@ 0
            dup.v 0 1
            dup.v 0
            push.v stacktop.test_constant
            callv.v 0
            popz.v
            """,
            false,
            gameContext
        );
    }

    [Fact]
    public void TestReadOnlyError()
    {
        Assert.Throws<TestCompileErrorException>(() =>
        {
            TestUtil.AssertBytecode(
                """
                id = 123;
                a.id = 456;
                """,
                "",
                false,
                new Underanalyzer.Mock.GameContextMock()
                {
                    UsingGMLv2 = false
                }
            );
        });
        TestUtil.AssertBytecode(
            """
            id = 123;
            a.id = 456;
            """,
            """
            pushi.e 123
            pop.v.i self.id
            pushi.e 456
            push.v self.a
            pushi.e -9
            pop.v.i [stacktop]self.id
            """,
            false,
            new Underanalyzer.Mock.GameContextMock()
            {
                UsingGMLv2 = true
            }
        );
    }

    [Fact]
    public void TestAutomaticArray()
    {
        TestUtil.AssertBytecode(
            """
            a = view_xview;
            """,
            """
            pushi.e -1
            push.l 0
            conv.l.i
            push.v [array]self.view_xview
            pop.v.v self.a
            """,
            false,
            new Underanalyzer.Mock.GameContextMock()
            {
                UsingGMLv2 = false
            }
        );
    }

    [Fact]
    public void TestAutomaticArrayGMLv2()
    {
        TestUtil.AssertBytecode(
            """
            a = view_camera;
            """,
            """
            pushi.e -6
            push.l 0
            conv.l.i
            push.v [array]self.view_camera
            pop.v.v self.a
            """,
            false,
            new Underanalyzer.Mock.GameContextMock()
            {
                UsingGMLv2 = true
            }
        );
    }

    [Fact]
    public void TestArrayIncrementOld()
    {
        TestUtil.AssertBytecode(
            """
            global.arr[123]++;
            """,
            """
            pushi.e -5
            pushi.e 123
            dup.i 1
            push.v [array]global.arr
            push.e 1
            add.i.v
            pop.i.v [array]global.arr
            """,
            false,
            new Underanalyzer.Mock.GameContextMock()
            {
                Bytecode14OrLower = true,
                UsingGMLv2 = false
            }
        );
    }

    [Fact]
    public void TestArrayIncrementMid()
    {
        TestUtil.AssertBytecode(
            """
            global.arr[123]++;
            """,
            """
            pushi.e -5
            pushi.e 123
            dup.l 0
            push.v [array]global.arr
            push.e 1
            add.i.v
            pop.i.v [array]global.arr
            """,
            false,
            new Underanalyzer.Mock.GameContextMock()
            {
                Bytecode14OrLower = false,
                UsingGMLv2 = false
            }
        );
    }

    [Fact]
    public void TestArrayIncrementNew()
    {
        TestUtil.AssertBytecode(
            """
            global.arr[123]++;
            """,
            """
            pushi.e -5
            pushi.e 123
            dup.i 1
            push.v [array]self.arr
            push.e 1
            add.i.v
            pop.i.v [array]global.arr
            """,
            false,
            new Underanalyzer.Mock.GameContextMock()
            {
                Bytecode14OrLower = false,
                UsingGMLv2 = true
            }
        );
    }

    [Fact]
    public void TestOldVariableCallsError()
    {
        Assert.Throws<TestCompileErrorException>(() =>
        {
            TestUtil.AssertBytecode(
                """
                a();
                a.b.c();
                a.b()();
                """,
                "",
                false,
                new Underanalyzer.Mock.GameContextMock()
                {
                    UsingGMLv2 = false
                }
            );
        });
    }

    [Fact]
    public void TestInstrinsicsOld()
    {
        TestUtil.AssertBytecode(
            """
            a = real(123);
            b = string(123);
            c = string("123");
            d = ord("A");
            """,
            """
            pushi.e 123
            conv.i.v
            call.i real 1
            pop.v.v self.a
            pushi.e 123
            conv.i.v
            call.i string 1
            pop.v.v self.b
            push.s "123"
            conv.s.v
            call.i string 1
            pop.v.v self.c
            pushi.e 65
            pop.v.i self.d
            """,
            false,
            new Underanalyzer.Mock.GameContextMock()
            {
                UsingStringRealOptimizations = false,
                UsingGMLv2 = false
            }
        );
    }

    [Fact]
    public void TestOtherArrayPreGMLv2()
    {
        TestUtil.AssertBytecode(
            """
            a = other.b[123];
            other.c[123] = d;
            """,
            """
            pushi.e -2
            pushi.e 123
            push.v [array]self.b
            pop.v.v self.a
            push.v self.d
            pushi.e -2
            pushi.e 123
            pop.v.v [array]self.c
            """,
            false,
            new Underanalyzer.Mock.GameContextMock()
            {
                UsingGMLv2 = false
            }
        );
    }

    [Fact]
    public void TestCompoundBitwisePre2_3_2()
    {
        TestUtil.AssertBytecode(
            """
            a ^= b;
            """,
            """
            push.v self.a
            push.v self.b
            conv.v.i
            xor.i.v
            pop.v.v self.a
            """,
            false,
            new Underanalyzer.Mock.GameContextMock()
            {
                UsingLongCompoundBitwise = false
            }
        );
    }

    [Fact]
    public void TestCompoundBitwisePost2_3_2()
    {
        TestUtil.AssertBytecode(
            """
            a ^= b;
            """,
            """
            push.v self.a
            push.v self.b
            conv.v.l
            xor.l.v
            pop.v.v self.a
            """,
            false,
            new Underanalyzer.Mock.GameContextMock()
            {
                UsingLongCompoundBitwise = true
            }
        );
    }

    [Fact]
    public void TestExpressionVariableFunctionCalls()
    {
        TestUtil.AssertBytecode(
            """
            (0)(123, 456);

            (function()
            {
            })(123, 456);

            (function()
            {
            } + 1)(123, 456);
            """,
            """
            :[0]
            pushi.e 456
            conv.i.v
            pushi.e 123
            conv.i.v
            call.i @@This@@ 0
            pushi.e 0
            callv.v 2
            popz.v
            pushi.e 456
            conv.i.v
            pushi.e 123
            conv.i.v
            call.i @@This@@ 0
            b [2]

            > anon_func_1 (locals=0, args=0)
            :[1]
            exit.i

            :[2]
            push.i [function]anon_func_1
            conv.i.v
            pushi.e -1
            conv.i.v
            call.i method 2
            callv.v 2
            popz.v
            pushi.e 456
            conv.i.v
            pushi.e 123
            conv.i.v
            call.i @@This@@ 0
            b [4]

            > anon_func_2 (locals=0, args=0)
            :[3]
            exit.i

            :[4]
            push.i [function]anon_func_2
            conv.i.v
            pushi.e -1
            conv.i.v
            call.i method 2
            pushi.e 1
            add.i.v
            callv.v 2
            popz.v
            """
        );
    }

    [Fact]
    public void TestContinueError()
    {
        Assert.Throws<TestCompileErrorException>(() =>
        {
            TestUtil.AssertBytecode(
                """
                for (var i = 0; i < 10; { i++; continue; }) {}
                """,
                ""
            );
        });
    }

    [Fact]
    public void TestNullishErrors()
    {
        Underanalyzer.Mock.GameContextMock gameContext = new()
        {
            UsingNullishOperator = false
        };
        Assert.Throws<TestCompileErrorException>(() =>
        {
            TestUtil.AssertBytecode(
                """
                a = b ?? c;
                """,
                "",
                false,
                gameContext
            );
        });
    }

    [Fact]
    public void TestNullishErrors2()
    {
        Underanalyzer.Mock.GameContextMock gameContext = new()
        {
            UsingNullishOperator = false
        };
        Assert.Throws<TestCompileErrorException>(() =>
        {
            TestUtil.AssertBytecode(
                """
                a ??= b;
                """,
                "",
                false,
                gameContext
            );
        });
    }

    [Fact]
    public void TestMultiDimensionalErrors()
    {
        Underanalyzer.Mock.GameContextMock gameContext = new()
        {
            UsingGMLv2 = false
        };
        Assert.Throws<TestCompileErrorException>(() =>
        {
            TestUtil.AssertBytecode(
                """
                a[b][c] = 123;
                """,
                "",
                false,
                gameContext
            );
        });
    }

    [Fact]
    public void TestStaticAnonymousName()
    {
        TestUtil.AssertBytecode(
            """
            function Test()
            {
                static myStaticFunc = function()
                {
                };
            }
            """,
            """
            :[0]
            b [6]

            > regular_func_Test (locals=0, args=0)
            :[1]
            isstaticok.e
            bt [5]

            :[2]
            b [4]

            > myStaticFunc_anon_func_1 (locals=0, args=0)
            :[3]
            exit.i

            :[4]
            push.i [function]myStaticFunc_anon_func_1
            conv.i.v
            pushi.e -16
            conv.i.v
            call.i method 2
            pop.v.v static.myStaticFunc

            :[5]
            setstatic.e
            exit.i

            :[6]
            push.i [function]regular_func_Test
            conv.i.v
            pushi.e -1
            conv.i.v
            call.i method 2
            dup.v 0
            pushi.e -6
            pop.v.v [stacktop]self.Test
            popz.v
            """
        );
    }

    [Fact]
    public void TestArrayOwners()
    {
        TestUtil.AssertBytecode(
            """
            a[0] = 1;
            a[0] = 1;
            a.b[0] = 1;
            a.b[0] = 1;
            array_set(a(), 0, 1);
            c = [1];
            d = array_create(15);
            e[0]++;
            e[0]++;
            e.f[0]++;
            e.f[0]++;
            ++e[0];
            ++e[0];
            ++e.f[0];
            ++e.f[0];

            function TestFunc()
            {
            	a[0] = 1;
            	a[0] = 1;
            	a.b[0] = 1;
            	a.b[0] = 1;
            	array_set(a(), 0, 1);
            	c = [1];
            	d = array_create(15);
            	e[0]++;
            	e[0]++;
            	e.f[0]++;
            	e.f[0]++;
            	++e[0];
            	++e[0];
            	++e.f[0];
            	++e.f[0];
            }
            """,
            """
            push.i 165536
            setowner.e
            pushi.e 1
            conv.i.v
            pushi.e -1
            pushi.e 0
            pop.v.v [array]self.a
            pushi.e 1
            conv.i.v
            pushi.e -1
            pushi.e 0
            pop.v.v [array]self.a
            push.i 231073
            setowner.e
            pushi.e 1
            conv.i.v
            push.v self.a
            pushi.e -9
            pushi.e 0
            pop.v.v [array]self.b
            pushi.e 1
            conv.i.v
            push.v self.a
            pushi.e -9
            pushi.e 0
            pop.v.v [array]self.b
            push.i 65536
            setowner.e
            pushi.e 1
            conv.i.v
            pushi.e 0
            conv.i.v
            call.i @@This@@ 0
            push.v builtin.a
            callv.v 0
            call.i array_set 3
            popz.v
            push.i 165538
            setowner.e
            pushi.e 1
            conv.i.v
            call.i @@NewGMLArray@@ 1
            pop.v.v self.c
            push.i 65536
            setowner.e
            pushi.e 15
            conv.i.v
            call.i array_create 1
            pop.v.v self.d
            push.i 165540
            setowner.e
            pushi.e -1
            pushi.e 0
            dup.i 1
            push.v [array]self.e
            dup.v 0
            dup.i 4 6
            push.e 1
            add.i.v
            pop.i.v [array]self.e
            popz.v
            pushi.e -1
            pushi.e 0
            push.v [array]self.e
            popz.v
            pushi.e -1
            pushi.e 0
            dup.i 1
            push.v [array]self.e
            dup.v 0
            dup.i 4 6
            push.e 1
            add.i.v
            pop.i.v [array]self.e
            popz.v
            push.i 231077
            setowner.e
            push.v self.e
            pushi.e -9
            pushi.e 0
            dup.i 5
            push.v [array]self.f
            dup.v 0
            dup.i 4 10
            push.e 1
            add.i.v
            pop.i.v [array]self.f
            popz.v
            push.v self.e
            pushi.e -9
            pushi.e 0
            push.v [array]self.f
            popz.v
            push.v self.e
            pushi.e -9
            pushi.e 0
            dup.i 5
            push.v [array]self.f
            dup.v 0
            dup.i 4 10
            push.e 1
            add.i.v
            pop.i.v [array]self.f
            popz.v
            push.i 165540
            setowner.e
            pushi.e -1
            pushi.e 0
            dup.i 1
            push.v [array]self.e
            push.e 1
            add.i.v
            dup.v 0
            dup.i 4 6
            pop.i.v [array]self.e
            popz.v
            pushi.e -1
            pushi.e 0
            push.v [array]self.e
            popz.v
            pushi.e -1
            pushi.e 0
            dup.i 1
            push.v [array]self.e
            push.e 1
            add.i.v
            dup.v 0
            dup.i 4 6
            pop.i.v [array]self.e
            popz.v
            push.i 231077
            setowner.e
            push.v self.e
            pushi.e -9
            pushi.e 0
            dup.i 5
            push.v [array]self.f
            push.e 1
            add.i.v
            dup.v 0
            dup.i 4 10
            pop.i.v [array]self.f
            popz.v
            push.v self.e
            pushi.e -9
            pushi.e 0
            push.v [array]self.f
            popz.v
            push.v self.e
            pushi.e -9
            pushi.e 0
            dup.i 5
            push.v [array]self.f
            push.e 1
            add.i.v
            dup.v 0
            dup.i 4 10
            pop.i.v [array]self.f
            popz.v
            b [2]

            > regular_func_TestFunc (locals=0, args=0)
            :[1]
            push.i 231072
            setowner.e
            pushi.e 1
            conv.i.v
            pushi.e -1
            pushi.e 0
            pop.v.v [array]self.a
            pushi.e 1
            conv.i.v
            pushi.e -1
            pushi.e 0
            pop.v.v [array]self.a
            push.i 362145
            setowner.e
            pushi.e 1
            conv.i.v
            push.v self.a
            pushi.e -9
            pushi.e 0
            pop.v.v [array]self.b
            pushi.e 1
            conv.i.v
            push.v self.a
            pushi.e -9
            pushi.e 0
            pop.v.v [array]self.b
            push.i 131072
            setowner.e
            pushi.e 1
            conv.i.v
            pushi.e 0
            conv.i.v
            call.i @@This@@ 0
            push.v builtin.a
            callv.v 0
            call.i array_set 3
            popz.v
            push.i 231074
            setowner.e
            pushi.e 1
            conv.i.v
            call.i @@NewGMLArray@@ 1
            pop.v.v self.c
            push.i 131072
            setowner.e
            pushi.e 15
            conv.i.v
            call.i array_create 1
            pop.v.v self.d
            push.i 231076
            setowner.e
            pushi.e -1
            pushi.e 0
            dup.i 1
            push.v [array]self.e
            dup.v 0
            dup.i 4 6
            push.e 1
            add.i.v
            pop.i.v [array]self.e
            popz.v
            pushi.e -1
            pushi.e 0
            push.v [array]self.e
            popz.v
            pushi.e -1
            pushi.e 0
            dup.i 1
            push.v [array]self.e
            dup.v 0
            dup.i 4 6
            push.e 1
            add.i.v
            pop.i.v [array]self.e
            popz.v
            push.i 362149
            setowner.e
            push.v self.e
            pushi.e -9
            pushi.e 0
            dup.i 5
            push.v [array]self.f
            dup.v 0
            dup.i 4 10
            push.e 1
            add.i.v
            pop.i.v [array]self.f
            popz.v
            push.v self.e
            pushi.e -9
            pushi.e 0
            push.v [array]self.f
            popz.v
            push.v self.e
            pushi.e -9
            pushi.e 0
            dup.i 5
            push.v [array]self.f
            dup.v 0
            dup.i 4 10
            push.e 1
            add.i.v
            pop.i.v [array]self.f
            popz.v
            push.i 231076
            setowner.e
            pushi.e -1
            pushi.e 0
            dup.i 1
            push.v [array]self.e
            push.e 1
            add.i.v
            dup.v 0
            dup.i 4 6
            pop.i.v [array]self.e
            popz.v
            pushi.e -1
            pushi.e 0
            push.v [array]self.e
            popz.v
            pushi.e -1
            pushi.e 0
            dup.i 1
            push.v [array]self.e
            push.e 1
            add.i.v
            dup.v 0
            dup.i 4 6
            pop.i.v [array]self.e
            popz.v
            push.i 362149
            setowner.e
            push.v self.e
            pushi.e -9
            pushi.e 0
            dup.i 5
            push.v [array]self.f
            push.e 1
            add.i.v
            dup.v 0
            dup.i 4 10
            pop.i.v [array]self.f
            popz.v
            push.v self.e
            pushi.e -9
            pushi.e 0
            push.v [array]self.f
            popz.v
            push.v self.e
            pushi.e -9
            pushi.e 0
            dup.i 5
            push.v [array]self.f
            push.e 1
            add.i.v
            dup.v 0
            dup.i 4 10
            pop.i.v [array]self.f
            popz.v
            exit.i

            :[2]
            push.i [function]regular_func_TestFunc
            conv.i.v
            pushi.e -1
            conv.i.v
            call.i method 2
            dup.v 0
            pushi.e -6
            pop.v.v [stacktop]self.TestFunc
            popz.v
            """,
            false,
            new Underanalyzer.Mock.GameContextMock()
            {
                UsingArrayCopyOnWrite = true,
                UsingNewArrayOwners = true
            }
        );
    }

    [Fact]
    public void TestArrayOwnerInvalidation()
    {
        TestUtil.AssertBytecode(
            """
            a[0] = 1;
            while (a)
            {
            	a[0] = 1;
            }
            a[0] = 1;
            while (a)
            {
            	a[0] = 1;
            	break;
            }
            a[0] = 1;
            """,
            """
            push.i 165536
            setowner.e
            pushi.e 1
            conv.i.v
            pushi.e -1
            pushi.e 0
            pop.v.v [array]self.a

            :[1]
            push.v self.a
            conv.v.b
            bf [3]

            :[2]
            pushi.e 1
            conv.i.v
            pushi.e -1
            pushi.e 0
            pop.v.v [array]self.a
            b [1]

            :[3]
            pushi.e 1
            conv.i.v
            pushi.e -1
            pushi.e 0
            pop.v.v [array]self.a

            :[4]
            push.v self.a
            conv.v.b
            bf [7]

            :[5]
            pushi.e 1
            conv.i.v
            pushi.e -1
            pushi.e 0
            pop.v.v [array]self.a
            b [7]

            :[6]
            b [4]

            :[7]
            push.i 165536
            setowner.e
            pushi.e 1
            conv.i.v
            pushi.e -1
            pushi.e 0
            pop.v.v [array]self.a
            """,
            false,
            new Underanalyzer.Mock.GameContextMock()
            {
                UsingArrayCopyOnWrite = true,
                UsingNewArrayOwners = true
            }
        );
    }

    [Fact]
    public void TestSwitchArrayOwners()
    {
        TestUtil.AssertBytecode(
            """
            a[0] = 0;
            switch (b)
            {
                case 1:
                    a[0] = 1;      
                    break;
                case 2:
            		b[0] = 2;
                    break;
                case 3:
            		a[0] = 3; 
                    break;
            }
            """,
            """
            push.i 165536
            setowner.e
            pushi.e 0
            conv.i.v
            pushi.e -1
            pushi.e 0
            pop.v.v [array]self.a
            push.v self.b
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
            pushi.e 1
            conv.i.v
            pushi.e -1
            pushi.e 0
            pop.v.v [array]self.a
            b [7]

            :[5]
            push.i 165537
            setowner.e
            pushi.e 2
            conv.i.v
            pushi.e -1
            pushi.e 0
            pop.v.v [array]self.b
            b [7]

            :[6]
            pushi.e 3
            conv.i.v
            pushi.e -1
            pushi.e 0
            pop.v.v [array]self.a
            b [7]

            :[7]
            popz.v
            """,
            false,
            new Underanalyzer.Mock.GameContextMock()
            {
                UsingArrayCopyOnWrite = true,
                UsingNewArrayOwners = true
            }
        );
    }

    [Fact]
    public void TestArrayOwnersNewArrayInCall()
    {
        TestUtil.AssertBytecode(
            """
            test_builtin_function([1, 2, 3], 4);
            """,
            """
            pushi.e 4
            conv.i.v
            push.i 65536
            setowner.e
            pushi.e 3
            conv.i.v
            pushi.e 2
            conv.i.v
            pushi.e 1
            conv.i.v
            call.i @@NewGMLArray@@ 3
            call.i test_builtin_function 2
            popz.v
            """,
            false,
            new Underanalyzer.Mock.GameContextMock()
            {
                UsingArrayCopyOnWrite = true,
                UsingNewArrayOwners = true
            }
        );
    }

    [Fact]
    public void TestArrayOwnersNewArrayInVarCall()
    {
        TestUtil.AssertBytecode(
            """
            a([1, 2, 3], 4);
            """,
            """
            pushi.e 4
            conv.i.v
            push.i 65536
            setowner.e
            pushi.e 3
            conv.i.v
            pushi.e 2
            conv.i.v
            pushi.e 1
            conv.i.v
            call.i @@NewGMLArray@@ 3
            call.i @@This@@ 0
            push.v builtin.a
            callv.v 2
            popz.v
            """,
            false,
            new Underanalyzer.Mock.GameContextMock()
            {
                UsingArrayCopyOnWrite = true,
                UsingNewArrayOwners = true
            }
        );
    }

    [Fact]
    public void TestArrayOwnersDots()
    {
        TestUtil.AssertBytecode(
            """
            a[0] = 1;
            b[0].a = 1;
            global.a[0] = 1;
            global.b[0].a = 1;
            """,
            """
            push.i 165536
            setowner.e
            pushi.e 1
            conv.i.v
            pushi.e -1
            pushi.e 0
            pop.v.v [array]self.a
            push.i 231072
            setowner.e
            pushi.e 1
            pushi.e -1
            pushi.e 0
            push.v [array]self.b
            pushi.e -9
            pop.v.i [stacktop]self.a
            pushi.e 1
            conv.i.v
            pushi.e -5
            pushi.e 0
            pop.v.v [array]self.a
            pushi.e 1
            pushi.e -5
            pushi.e 0
            push.v [array]self.b
            pushi.e -9
            pop.v.i [stacktop]self.a
            """,
            false,
            new Underanalyzer.Mock.GameContextMock()
            {
                UsingArrayCopyOnWrite = true,
                UsingNewArrayOwners = true
            }
        );
    }

    [Fact]
    public void TestGlobalAssignIncrement()
    {
        TestUtil.AssertBytecode(
            """
            global.a[0] = 1;
            global.a[0] += 1;
            global.a[0]++;
            """,
            """
            pushi.e 1
            conv.i.v
            pushi.e -5
            pushi.e 0
            pop.v.v [array]self.a
            pushi.e -5
            pushi.e 0
            dup.i 1
            push.v [array]self.a
            pushi.e 1
            add.i.v
            pop.i.v [array]self.a
            pushi.e -5
            pushi.e 0
            dup.i 1
            push.v [array]self.a
            push.e 1
            add.i.v
            pop.i.v [array]global.a
            """
        );
    }

    [Fact]
    public void TestRoomInstanceIdsOld()
    {
        TestUtil.AssertBytecode(
            """
            a = inst_id_101234;
            """,
            """
            push.i 101234
            pop.v.i self.a
            """,
            false,
            new Underanalyzer.Mock.GameContextMock()
            {
                UsingAssetReferences = false,
                UsingRoomInstanceReferences = false
            }
        );
    }

    [Fact]
    public void TestRoomInstanceIdsMid()
    {
        TestUtil.AssertBytecode(
            """
            a = inst_id_101234;
            """,
            """
            push.i 101234
            pop.v.i self.a
            """,
            false,
            new Underanalyzer.Mock.GameContextMock()
            {
                UsingAssetReferences = true,
                UsingRoomInstanceReferences = false
            }
        );
    }

    [Fact]
    public void TestRoomInstanceIdsModern()
    {
        TestUtil.AssertBytecode(
            """
            a = inst_id_101234;
            """,
            """
            pushref.i 101234 RoomInstance
            pop.v.v self.a
            """,
            false,
            new Underanalyzer.Mock.GameContextMock()
            {
                UsingAssetReferences = true,
                UsingRoomInstanceReferences = true
            }
        );
    }

    [Fact]
    public void TestNewSetStatic()
    {
        TestUtil.AssertBytecode(
            """
            function Test(arg0) constructor
            {
                test = arg0;
            }
            """,
            """
            :[0]
            b [2]
            
            > regular_func_Test (locals=0, args=1)
            :[1]
            call.i @@SetStatic@@ 0
            push.v arg.argument0
            pop.v.v builtin.test
            exit.i
            
            :[2]
            push.i [function]regular_func_Test
            conv.i.v
            call.i @@NullObject@@ 0
            call.i method 2
            dup.v 0
            pushi.e -1
            pop.v.v [stacktop]self.Test
            popz.v
            """,
            false,
            new Underanalyzer.Mock.GameContextMock()
            {
                UsingSelfToBuiltin = true,
                UsingNewFunctionVariables = true,
                UsingConstructorSetStatic = true
            }
        );
    }

    [Fact]
    public void TestNonBuiltinDefaultArguments()
    {
        TestUtil.AssertBytecode(
            """
            function test_func(arg0 = 123, arg1 = 456, arg2)
            {
            }
            """,
            """
            b [6]

            > global_func_test_func (locals=0, args=3)
            :[1]
            push.v arg.argument0
            pushbltn.v builtin.undefined
            cmp.v.v EQ
            bf [3]

            :[2]
            pushi.e 123
            pop.v.i arg.argument0

            :[3]
            push.v arg.argument1
            pushbltn.v builtin.undefined
            cmp.v.v EQ
            bf [5]

            :[4]
            pushi.e 456
            pop.v.i arg.argument1

            :[5]
            exit.i

            :[6]
            push.i [function]global_func_test_func
            conv.i.v
            pushi.e -1
            conv.i.v
            call.i method 2
            dup.v 0
            pushi.e -1
            pop.v.v [stacktop]self.test_func
            popz.v
            """,
            true,
            new Underanalyzer.Mock.GameContextMock()
            {
                UsingSelfToBuiltin = true,
                UsingNewFunctionVariables = true,
                UsingConstructorSetStatic = false,
                UsingBuiltinDefaultArguments = false
            }
        );
    }

    [Fact]
    public void TestBuiltinDefaultArguments()
    {
        TestUtil.AssertBytecode(
            """
            function test_func(arg0 = 123, arg1 = 456, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16 = 789)
            {
            }
            """,
            """
            b [8]

            > global_func_test_func (locals=0, args=17)
            :[1]
            pushbltn.v builtin.argument0
            pushbltn.v builtin.undefined
            cmp.v.v EQ
            bf [3]

            :[2]
            pushi.e 123
            pop.v.i builtin.argument0

            :[3]
            pushbltn.v builtin.argument1
            pushbltn.v builtin.undefined
            cmp.v.v EQ
            bf [5]

            :[4]
            pushi.e 456
            pop.v.i builtin.argument1

            :[5]
            pushi.e -15
            pushi.e 16
            push.v [array]self.argument
            pushbltn.v builtin.undefined
            cmp.v.v EQ
            bf [7]

            :[6]
            pushi.e 789
            conv.i.v
            pushi.e -15
            pushi.e 16
            pop.v.v [array]self.argument

            :[7]
            exit.i

            :[8]
            push.i [function]global_func_test_func
            conv.i.v
            pushi.e -1
            conv.i.v
            call.i method 2
            dup.v 0
            pushi.e -1
            pop.v.v [stacktop]self.test_func
            popz.v
            """,
            true,
            new Underanalyzer.Mock.GameContextMock()
            {
                UsingSelfToBuiltin = true,
                UsingNewFunctionVariables = true,
                UsingConstructorSetStatic = true,
                UsingBuiltinDefaultArguments = true
            }
        );
    }

    [Fact]
    public void TestFunctionScriptReferencesGlobalScript()
    {
        Underanalyzer.Mock.GameContextMock gameContextMock = new()
        {
            UsingSelfToBuiltin = true,
            UsingNewFunctionVariables = true,
            UsingConstructorSetStatic = true,
            UsingFunctionScriptReferences = true,
            UsingObjectFunctionForesight = false
        };
        gameContextMock.DefineMockAsset(AssetType.Script, 123, "DifferentScript");
        ((Underanalyzer.Decompiler.GlobalFunctions)gameContextMock.GlobalFunctions).
            DefineFunction("DifferentScript", new Underanalyzer.Mock.GMFunction("DifferentScript"));
        gameContextMock.DefineMockAsset(AssetType.Script, 124, "global_func_DifferentScript_SubFunction");
        ((Underanalyzer.Decompiler.GlobalFunctions)gameContextMock.GlobalFunctions).
            DefineFunction("DifferentScript_SubFunction", new Underanalyzer.Mock.GMFunction("global_func_DifferentScript_SubFunction"));
        TestUtil.AssertBytecode(
            """
            a = SameScript;
            b = new SameScript();
            SameScript();

            function SameScript()
            {
            }

            a = SameScript;
            b = new SameScript();
            SameScript();
            c(new SameScript());
            c.d(new SameScript());
            c.d([new SameScript()]);
            c.d(script_execute(new SameScript()));
            c.d(function()
            {
                e = new SameScript();
            });
            script_execute(new SameScript());
            c(SameScript);
            c.d(SameScript);
            c.d([SameScript]);
            c.d(script_execute(SameScript));
            c.d(function()
            {
                e = SameScript;
            });
            script_execute(SameScript);
            a = DifferentScript;
            b = new DifferentScript();
            DifferentScript();
            c(new DifferentScript());
            c.d(new DifferentScript());
            c.d([new DifferentScript()]);
            c.d(script_execute(new DifferentScript()));
            c.d(function()
            {
                e = new DifferentScript();
            });
            script_execute(new DifferentScript());
            c(DifferentScript);
            c.d(DifferentScript);
            c.d([DifferentScript]);
            c.d(script_execute(DifferentScript));
            c.d(function()
            {
                e = DifferentScript;
            });
            script_execute(DifferentScript);
            a = DifferentScript_SubFunction;
            b = new DifferentScript_SubFunction();
            DifferentScript_SubFunction();
            c(DifferentScript_SubFunction);
            c.d(DifferentScript_SubFunction);
            c.d([DifferentScript_SubFunction]);
            c.d(script_execute(DifferentScript_SubFunction));
            c.d(function()
            {
                e = DifferentScript_SubFunction;
            });
            script_execute(DifferentScript_SubFunction);
            a = string;
            """,
            """
            pushref.i global_func_SameScript
            pop.v.v builtin.a
            push.i [function]global_func_SameScript
            conv.i.v
            call.i @@NewGMLObject@@ 1
            pop.v.v builtin.b
            call.i global_func_SameScript 0
            popz.v
            b [2]

            > global_func_SameScript (locals=0, args=0)
            :[1]
            exit.i

            :[2]
            push.i [function]global_func_SameScript
            conv.i.v
            pushi.e -1
            conv.i.v
            call.i method 2
            dup.v 0
            pushi.e -1
            pop.v.v [stacktop]self.SameScript
            popz.v
            pushref.i global_func_SameScript
            pop.v.v builtin.a
            push.i [function]global_func_SameScript
            conv.i.v
            call.i @@NewGMLObject@@ 1
            pop.v.v builtin.b
            call.i global_func_SameScript 0
            popz.v
            push.i [function]global_func_SameScript
            conv.i.v
            call.i @@NewGMLObject@@ 1
            call.i @@This@@ 0
            push.v builtin.c
            callv.v 1
            popz.v
            push.v builtin.c
            push.i [function]global_func_SameScript
            conv.i.v
            call.i @@NewGMLObject@@ 1
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            push.v builtin.c
            push.i [function]global_func_SameScript
            conv.i.v
            call.i @@NewGMLObject@@ 1
            call.i @@NewGMLArray@@ 1
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            push.v builtin.c
            push.i [function]global_func_SameScript
            conv.i.v
            call.i @@NewGMLObject@@ 1
            call.i script_execute 1
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            push.v builtin.c
            b [4]

            > anon_func_1 (locals=0, args=0)
            :[3]
            push.i [function]global_func_SameScript
            conv.i.v
            call.i @@NewGMLObject@@ 1
            pop.v.v builtin.e
            exit.i

            :[4]
            push.i [function]anon_func_1
            conv.i.v
            pushi.e -1
            conv.i.v
            call.i method 2
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            push.i [function]global_func_SameScript
            conv.i.v
            call.i @@NewGMLObject@@ 1
            call.i script_execute 1
            popz.v
            pushref.i global_func_SameScript
            call.i @@This@@ 0
            push.v builtin.c
            callv.v 1
            popz.v
            push.v builtin.c
            push.i [function]global_func_SameScript
            conv.i.v
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            push.v builtin.c
            push.i [function]global_func_SameScript
            conv.i.v
            call.i @@NewGMLArray@@ 1
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            push.v builtin.c
            push.i [function]global_func_SameScript
            conv.i.v
            call.i script_execute 1
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            push.v builtin.c
            b [6]

            > anon_func_2 (locals=0, args=0)
            :[5]
            pushref.i global_func_SameScript
            pop.v.v builtin.e
            exit.i

            :[6]
            push.i [function]anon_func_2
            conv.i.v
            pushi.e -1
            conv.i.v
            call.i method 2
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            pushref.i global_func_SameScript
            call.i script_execute 1
            popz.v
            pushref.i DifferentScript
            pop.v.v builtin.a
            pushref.i DifferentScript
            call.i @@NewGMLObject@@ 1
            pop.v.v builtin.b
            call.i DifferentScript 0
            popz.v
            pushref.i DifferentScript
            call.i @@NewGMLObject@@ 1
            call.i @@This@@ 0
            push.v builtin.c
            callv.v 1
            popz.v
            push.v builtin.c
            push.i [function]DifferentScript
            conv.i.v
            call.i @@NewGMLObject@@ 1
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            push.v builtin.c
            push.i [function]DifferentScript
            conv.i.v
            call.i @@NewGMLObject@@ 1
            call.i @@NewGMLArray@@ 1
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            push.v builtin.c
            push.i [function]DifferentScript
            conv.i.v
            call.i @@NewGMLObject@@ 1
            call.i script_execute 1
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            push.v builtin.c
            b [8]

            > anon_func_3 (locals=0, args=0)
            :[7]
            pushref.i DifferentScript
            call.i @@NewGMLObject@@ 1
            pop.v.v builtin.e
            exit.i

            :[8]
            push.i [function]anon_func_3
            conv.i.v
            pushi.e -1
            conv.i.v
            call.i method 2
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            pushref.i DifferentScript
            call.i @@NewGMLObject@@ 1
            call.i script_execute 1
            popz.v
            pushref.i DifferentScript
            call.i @@This@@ 0
            push.v builtin.c
            callv.v 1
            popz.v
            push.v builtin.c
            push.i [function]DifferentScript
            conv.i.v
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            push.v builtin.c
            push.i [function]DifferentScript
            conv.i.v
            call.i @@NewGMLArray@@ 1
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            push.v builtin.c
            push.i [function]DifferentScript
            conv.i.v
            call.i script_execute 1
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            push.v builtin.c
            b [10]

            > anon_func_4 (locals=0, args=0)
            :[9]
            pushref.i DifferentScript
            pop.v.v builtin.e
            exit.i

            :[10]
            push.i [function]anon_func_4
            conv.i.v
            pushi.e -1
            conv.i.v
            call.i method 2
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            pushref.i DifferentScript
            call.i script_execute 1
            popz.v
            pushref.i global_func_DifferentScript_SubFunction
            pop.v.v builtin.a
            push.i [function]global_func_DifferentScript_SubFunction
            conv.i.v
            call.i @@NewGMLObject@@ 1
            pop.v.v builtin.b
            call.i global_func_DifferentScript_SubFunction 0
            popz.v
            pushref.i global_func_DifferentScript_SubFunction
            call.i @@This@@ 0
            push.v builtin.c
            callv.v 1
            popz.v
            push.v builtin.c
            push.i [function]global_func_DifferentScript_SubFunction
            conv.i.v
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            push.v builtin.c
            push.i [function]global_func_DifferentScript_SubFunction
            conv.i.v
            call.i @@NewGMLArray@@ 1
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            push.v builtin.c
            push.i [function]global_func_DifferentScript_SubFunction
            conv.i.v
            call.i script_execute 1
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            push.v builtin.c
            b [12]

            > anon_func_5 (locals=0, args=0)
            :[11]
            pushref.i global_func_DifferentScript_SubFunction
            pop.v.v builtin.e
            exit.i

            :[12]
            push.i [function]anon_func_5
            conv.i.v
            pushi.e -1
            conv.i.v
            call.i method 2
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            pushref.i global_func_DifferentScript_SubFunction
            call.i script_execute 1
            popz.v
            push.i [function]string
            pop.v.i builtin.a
            """,
            true,
            gameContextMock
        );
    }

    [Fact]
    public void TestFunctionScriptReferencesObjectEvent()
    {
        Underanalyzer.Mock.GameContextMock gameContextMock = new()
        {
            UsingSelfToBuiltin = true,
            UsingNewFunctionVariables = true,
            UsingConstructorSetStatic = true,
            UsingFunctionScriptReferences = true,
            UsingObjectFunctionForesight = false
        };
        gameContextMock.DefineMockAsset(AssetType.Script, 123, "DifferentScript");
        ((Underanalyzer.Decompiler.GlobalFunctions)gameContextMock.GlobalFunctions).
            DefineFunction("DifferentScript", new Underanalyzer.Mock.GMFunction("DifferentScript"));
        gameContextMock.DefineMockAsset(AssetType.Script, 124, "global_func_DifferentScript_SubFunction");
        ((Underanalyzer.Decompiler.GlobalFunctions)gameContextMock.GlobalFunctions).
            DefineFunction("DifferentScript_SubFunction", new Underanalyzer.Mock.GMFunction("global_func_DifferentScript_SubFunction"));
        TestUtil.AssertBytecode(
            """
            a = SameEvent;
            b = new SameEvent();
            SameEvent();

            function SameEvent()
            {
            }

            a = SameEvent;
            b = new SameEvent();
            SameEvent();
            c(new SameEvent());
            c.d(new SameEvent());
            c.d([new SameEvent()]);
            c.d(script_execute(new SameEvent()));
            c.d(function()
            {
                e = new SameEvent();
            });
            script_execute(new SameEvent());
            c(SameEvent);
            c.d(SameEvent);
            c.d([SameEvent]);
            c.d(script_execute(SameEvent));
            c.d(function()
            {
                e = SameEvent;
            });
            script_execute(SameEvent);
            a = DifferentScript;
            b = new DifferentScript();
            DifferentScript();
            c(new DifferentScript());
            c.d(new DifferentScript());
            c.d([new DifferentScript()]);
            c.d(script_execute(new DifferentScript()));
            c.d(function()
            {
                e = new DifferentScript();
            });
            script_execute(new DifferentScript());
            c(DifferentScript);
            c.d(DifferentScript);
            c.d([DifferentScript]);
            c.d(script_execute(DifferentScript));
            c.d(function()
            {
                e = DifferentScript;
            });
            script_execute(DifferentScript);
            a = DifferentScript_SubFunction;
            b = new DifferentScript_SubFunction();
            DifferentScript_SubFunction();
            c(DifferentScript_SubFunction);
            c.d(DifferentScript_SubFunction);
            c.d([DifferentScript_SubFunction]);
            c.d(script_execute(DifferentScript_SubFunction));
            c.d(function()
            {
                e = DifferentScript_SubFunction;
            });
            script_execute(DifferentScript_SubFunction);
            a = string;
            """,
            """
            push.v builtin.SameEvent
            pop.v.v builtin.a
            push.v builtin.SameEvent
            call.i @@NewGMLObject@@ 1
            pop.v.v builtin.b
            call.i @@This@@ 0
            push.v builtin.SameEvent
            callv.v 0
            popz.v
            b [2]

            > regular_func_SameEvent (locals=0, args=0)
            :[1]
            exit.i

            :[2]
            push.i [function]regular_func_SameEvent
            conv.i.v
            pushi.e -1
            conv.i.v
            call.i method 2
            dup.v 0
            pushi.e -1
            pop.v.v [stacktop]self.SameEvent
            popz.v
            push.i [function]regular_func_SameEvent
            pop.v.i builtin.a
            push.i [function]regular_func_SameEvent
            conv.i.v
            call.i @@NewGMLObject@@ 1
            pop.v.v builtin.b
            call.i regular_func_SameEvent 0
            popz.v
            push.i [function]regular_func_SameEvent
            conv.i.v
            call.i @@NewGMLObject@@ 1
            call.i @@This@@ 0
            push.v builtin.c
            callv.v 1
            popz.v
            push.v builtin.c
            push.i [function]regular_func_SameEvent
            conv.i.v
            call.i @@NewGMLObject@@ 1
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            push.v builtin.c
            push.i [function]regular_func_SameEvent
            conv.i.v
            call.i @@NewGMLObject@@ 1
            call.i @@NewGMLArray@@ 1
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            push.v builtin.c
            push.i [function]regular_func_SameEvent
            conv.i.v
            call.i @@NewGMLObject@@ 1
            call.i script_execute 1
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            push.v builtin.c
            b [4]

            > anon_func_1 (locals=0, args=0)
            :[3]
            push.v builtin.SameEvent
            call.i @@NewGMLObject@@ 1
            pop.v.v builtin.e
            exit.i

            :[4]
            push.i [function]anon_func_1
            conv.i.v
            pushi.e -1
            conv.i.v
            call.i method 2
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            push.i [function]regular_func_SameEvent
            conv.i.v
            call.i @@NewGMLObject@@ 1
            call.i script_execute 1
            popz.v
            push.i [function]regular_func_SameEvent
            conv.i.v
            call.i @@This@@ 0
            push.v builtin.c
            callv.v 1
            popz.v
            push.v builtin.c
            push.i [function]regular_func_SameEvent
            conv.i.v
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            push.v builtin.c
            push.i [function]regular_func_SameEvent
            conv.i.v
            call.i @@NewGMLArray@@ 1
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            push.v builtin.c
            push.i [function]regular_func_SameEvent
            conv.i.v
            call.i script_execute 1
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            push.v builtin.c
            b [6]

            > anon_func_2 (locals=0, args=0)
            :[5]
            push.v builtin.SameEvent
            pop.v.v builtin.e
            exit.i

            :[6]
            push.i [function]anon_func_2
            conv.i.v
            pushi.e -1
            conv.i.v
            call.i method 2
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            push.i [function]regular_func_SameEvent
            conv.i.v
            call.i script_execute 1
            popz.v
            pushref.i DifferentScript
            pop.v.v builtin.a
            pushref.i DifferentScript
            call.i @@NewGMLObject@@ 1
            pop.v.v builtin.b
            call.i DifferentScript 0
            popz.v
            pushref.i DifferentScript
            call.i @@NewGMLObject@@ 1
            call.i @@This@@ 0
            push.v builtin.c
            callv.v 1
            popz.v
            push.v builtin.c
            push.i [function]DifferentScript
            conv.i.v
            call.i @@NewGMLObject@@ 1
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            push.v builtin.c
            push.i [function]DifferentScript
            conv.i.v
            call.i @@NewGMLObject@@ 1
            call.i @@NewGMLArray@@ 1
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            push.v builtin.c
            push.i [function]DifferentScript
            conv.i.v
            call.i @@NewGMLObject@@ 1
            call.i script_execute 1
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            push.v builtin.c
            b [8]

            > anon_func_3 (locals=0, args=0)
            :[7]
            pushref.i DifferentScript
            call.i @@NewGMLObject@@ 1
            pop.v.v builtin.e
            exit.i

            :[8]
            push.i [function]anon_func_3
            conv.i.v
            pushi.e -1
            conv.i.v
            call.i method 2
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            pushref.i DifferentScript
            call.i @@NewGMLObject@@ 1
            call.i script_execute 1
            popz.v
            pushref.i DifferentScript
            call.i @@This@@ 0
            push.v builtin.c
            callv.v 1
            popz.v
            push.v builtin.c
            push.i [function]DifferentScript
            conv.i.v
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            push.v builtin.c
            push.i [function]DifferentScript
            conv.i.v
            call.i @@NewGMLArray@@ 1
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            push.v builtin.c
            push.i [function]DifferentScript
            conv.i.v
            call.i script_execute 1
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            push.v builtin.c
            b [10]

            > anon_func_4 (locals=0, args=0)
            :[9]
            pushref.i DifferentScript
            pop.v.v builtin.e
            exit.i

            :[10]
            push.i [function]anon_func_4
            conv.i.v
            pushi.e -1
            conv.i.v
            call.i method 2
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            pushref.i DifferentScript
            call.i script_execute 1
            popz.v
            pushref.i global_func_DifferentScript_SubFunction
            pop.v.v builtin.a
            push.i [function]global_func_DifferentScript_SubFunction
            conv.i.v
            call.i @@NewGMLObject@@ 1
            pop.v.v builtin.b
            call.i global_func_DifferentScript_SubFunction 0
            popz.v
            pushref.i global_func_DifferentScript_SubFunction
            call.i @@This@@ 0
            push.v builtin.c
            callv.v 1
            popz.v
            push.v builtin.c
            push.i [function]global_func_DifferentScript_SubFunction
            conv.i.v
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            push.v builtin.c
            push.i [function]global_func_DifferentScript_SubFunction
            conv.i.v
            call.i @@NewGMLArray@@ 1
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            push.v builtin.c
            push.i [function]global_func_DifferentScript_SubFunction
            conv.i.v
            call.i script_execute 1
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            push.v builtin.c
            b [12]

            > anon_func_5 (locals=0, args=0)
            :[11]
            pushref.i global_func_DifferentScript_SubFunction
            pop.v.v builtin.e
            exit.i

            :[12]
            push.i [function]anon_func_5
            conv.i.v
            pushi.e -1
            conv.i.v
            call.i method 2
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            pushref.i global_func_DifferentScript_SubFunction
            call.i script_execute 1
            popz.v
            push.i [function]string
            pop.v.i builtin.a
            """,
            false,
            gameContextMock
        );
    }

    [Fact]
    public void TestFunctionScriptReferencesNewGlobalScript()
    {
        Underanalyzer.Mock.GameContextMock gameContextMock = new()
        {
            UsingSelfToBuiltin = true,
            UsingNewFunctionVariables = true,
            UsingConstructorSetStatic = true,
            UsingFunctionScriptReferences = true,
            UsingObjectFunctionForesight = true,
            UsingNewFunctionResolution = true
        };
        gameContextMock.DefineMockAsset(AssetType.Script, 123, "DifferentScript");
        ((Underanalyzer.Decompiler.GlobalFunctions)gameContextMock.GlobalFunctions).
            DefineFunction("DifferentScript", new Underanalyzer.Mock.GMFunction("DifferentScript"));
        gameContextMock.DefineMockAsset(AssetType.Script, 124, "global_func_DifferentScript_SubFunction");
        ((Underanalyzer.Decompiler.GlobalFunctions)gameContextMock.GlobalFunctions).
            DefineFunction("DifferentScript_SubFunction", new Underanalyzer.Mock.GMFunction("global_func_DifferentScript_SubFunction"));
        TestUtil.AssertBytecode(
            """
            a = SameScript;
            b = new SameScript();
            SameScript();

            function SameScript()
            {
            }

            a = SameScript;
            b = new SameScript();
            SameScript();
            c(new SameScript());
            c.d(new SameScript());
            c.d([new SameScript()]);
            c.d(script_execute(new SameScript()));
            c.d(function()
            {
                e = new SameScript();
            });
            script_execute(new SameScript());
            c(SameScript);
            c.d(SameScript);
            c.d([SameScript]);
            c.d(script_execute(SameScript));
            c.d(function()
            {
                e = SameScript;
            });
            script_execute(SameScript);
            a = DifferentScript;
            b = new DifferentScript();
            DifferentScript();
            c(new DifferentScript());
            c.d(new DifferentScript());
            c.d([new DifferentScript()]);
            c.d(script_execute(new DifferentScript()));
            c.d(function()
            {
                e = new DifferentScript();
            });
            script_execute(new DifferentScript());
            c(DifferentScript);
            c.d(DifferentScript);
            c.d([DifferentScript]);
            c.d(script_execute(DifferentScript));
            c.d(function()
            {
                e = DifferentScript;
            });
            script_execute(DifferentScript);
            a = DifferentScript_SubFunction;
            b = new DifferentScript_SubFunction();
            DifferentScript_SubFunction();
            c(DifferentScript_SubFunction);
            c.d(DifferentScript_SubFunction);
            c.d([DifferentScript_SubFunction]);
            c.d(script_execute(DifferentScript_SubFunction));
            c.d(function()
            {
                e = DifferentScript_SubFunction;
            });
            script_execute(DifferentScript_SubFunction);
            a = string;
            """,
            """
            pushref.i global_func_SameScript
            pop.v.v builtin.a
            push.i [function]global_func_SameScript
            conv.i.v
            call.i @@NewGMLObject@@ 1
            pop.v.v builtin.b
            call.i global_func_SameScript 0
            popz.v
            b [2]

            > global_func_SameScript (locals=0, args=0)
            :[1]
            exit.i

            :[2]
            push.i [function]global_func_SameScript
            conv.i.v
            pushi.e -1
            conv.i.v
            call.i method 2
            dup.v 0
            pushi.e -1
            pop.v.v [stacktop]self.SameScript
            popz.v
            pushref.i global_func_SameScript
            pop.v.v builtin.a
            push.i [function]global_func_SameScript
            conv.i.v
            call.i @@NewGMLObject@@ 1
            pop.v.v builtin.b
            call.i global_func_SameScript 0
            popz.v
            push.i [function]global_func_SameScript
            conv.i.v
            call.i @@NewGMLObject@@ 1
            call.i @@This@@ 0
            push.v builtin.c
            callv.v 1
            popz.v
            push.v builtin.c
            push.i [function]global_func_SameScript
            conv.i.v
            call.i @@NewGMLObject@@ 1
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            push.v builtin.c
            push.i [function]global_func_SameScript
            conv.i.v
            call.i @@NewGMLObject@@ 1
            call.i @@NewGMLArray@@ 1
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            push.v builtin.c
            push.i [function]global_func_SameScript
            conv.i.v
            call.i @@NewGMLObject@@ 1
            call.i script_execute 1
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            push.v builtin.c
            b [4]

            > anon_func_1 (locals=0, args=0)
            :[3]
            push.i [function]global_func_SameScript
            conv.i.v
            call.i @@NewGMLObject@@ 1
            pop.v.v builtin.e
            exit.i

            :[4]
            push.i [function]anon_func_1
            conv.i.v
            pushi.e -1
            conv.i.v
            call.i method 2
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            push.i [function]global_func_SameScript
            conv.i.v
            call.i @@NewGMLObject@@ 1
            call.i script_execute 1
            popz.v
            pushref.i global_func_SameScript
            call.i @@This@@ 0
            push.v builtin.c
            callv.v 1
            popz.v
            push.v builtin.c
            push.i [function]global_func_SameScript
            conv.i.v
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            push.v builtin.c
            push.i [function]global_func_SameScript
            conv.i.v
            call.i @@NewGMLArray@@ 1
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            push.v builtin.c
            push.i [function]global_func_SameScript
            conv.i.v
            call.i script_execute 1
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            push.v builtin.c
            b [6]

            > anon_func_2 (locals=0, args=0)
            :[5]
            pushref.i global_func_SameScript
            pop.v.v builtin.e
            exit.i

            :[6]
            push.i [function]anon_func_2
            conv.i.v
            pushi.e -1
            conv.i.v
            call.i method 2
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            pushref.i global_func_SameScript
            call.i script_execute 1
            popz.v
            pushref.i DifferentScript
            pop.v.v builtin.a
            pushref.i DifferentScript
            call.i @@NewGMLObject@@ 1
            pop.v.v builtin.b
            call.i DifferentScript 0
            popz.v
            pushref.i DifferentScript
            call.i @@NewGMLObject@@ 1
            call.i @@This@@ 0
            push.v builtin.c
            callv.v 1
            popz.v
            push.v builtin.c
            push.i [function]DifferentScript
            conv.i.v
            call.i @@NewGMLObject@@ 1
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            push.v builtin.c
            push.i [function]DifferentScript
            conv.i.v
            call.i @@NewGMLObject@@ 1
            call.i @@NewGMLArray@@ 1
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            push.v builtin.c
            push.i [function]DifferentScript
            conv.i.v
            call.i @@NewGMLObject@@ 1
            call.i script_execute 1
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            push.v builtin.c
            b [8]

            > anon_func_3 (locals=0, args=0)
            :[7]
            pushref.i DifferentScript
            call.i @@NewGMLObject@@ 1
            pop.v.v builtin.e
            exit.i

            :[8]
            push.i [function]anon_func_3
            conv.i.v
            pushi.e -1
            conv.i.v
            call.i method 2
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            pushref.i DifferentScript
            call.i @@NewGMLObject@@ 1
            call.i script_execute 1
            popz.v
            pushref.i DifferentScript
            call.i @@This@@ 0
            push.v builtin.c
            callv.v 1
            popz.v
            push.v builtin.c
            pushref.i DifferentScript
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            push.v builtin.c
            pushref.i DifferentScript
            call.i @@NewGMLArray@@ 1
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            push.v builtin.c
            pushref.i DifferentScript
            call.i script_execute 1
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            push.v builtin.c
            b [10]

            > anon_func_4 (locals=0, args=0)
            :[9]
            pushref.i DifferentScript
            pop.v.v builtin.e
            exit.i

            :[10]
            push.i [function]anon_func_4
            conv.i.v
            pushi.e -1
            conv.i.v
            call.i method 2
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            pushref.i DifferentScript
            call.i script_execute 1
            popz.v
            pushref.i global_func_DifferentScript_SubFunction
            pop.v.v builtin.a
            push.i [function]global_func_DifferentScript_SubFunction
            conv.i.v
            call.i @@NewGMLObject@@ 1
            pop.v.v builtin.b
            call.i global_func_DifferentScript_SubFunction 0
            popz.v
            pushref.i global_func_DifferentScript_SubFunction
            call.i @@This@@ 0
            push.v builtin.c
            callv.v 1
            popz.v
            push.v builtin.c
            pushref.i global_func_DifferentScript_SubFunction
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            push.v builtin.c
            pushref.i global_func_DifferentScript_SubFunction
            call.i @@NewGMLArray@@ 1
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            push.v builtin.c
            pushref.i global_func_DifferentScript_SubFunction
            call.i script_execute 1
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            push.v builtin.c
            b [12]

            > anon_func_5 (locals=0, args=0)
            :[11]
            pushref.i global_func_DifferentScript_SubFunction
            pop.v.v builtin.e
            exit.i

            :[12]
            push.i [function]anon_func_5
            conv.i.v
            pushi.e -1
            conv.i.v
            call.i method 2
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            pushref.i global_func_DifferentScript_SubFunction
            call.i script_execute 1
            popz.v
            push.i [function]string
            pop.v.i builtin.a
            """,
            true,
            gameContextMock
        );
    }

    [Fact]
    public void TestFunctionScriptReferencesNewObjectEvent()
    {
        Underanalyzer.Mock.GameContextMock gameContextMock = new()
        {
            UsingSelfToBuiltin = true,
            UsingNewFunctionVariables = true,
            UsingConstructorSetStatic = true,
            UsingFunctionScriptReferences = true,
            UsingObjectFunctionForesight = true,
            UsingNewFunctionResolution = true
        };
        gameContextMock.DefineMockAsset(AssetType.Script, 123, "DifferentScript");
        ((Underanalyzer.Decompiler.GlobalFunctions)gameContextMock.GlobalFunctions).
            DefineFunction("DifferentScript", new Underanalyzer.Mock.GMFunction("DifferentScript"));
        gameContextMock.DefineMockAsset(AssetType.Script, 124, "global_func_DifferentScript_SubFunction");
        ((Underanalyzer.Decompiler.GlobalFunctions)gameContextMock.GlobalFunctions).
            DefineFunction("DifferentScript_SubFunction", new Underanalyzer.Mock.GMFunction("global_func_DifferentScript_SubFunction"));
        TestUtil.AssertBytecode(
            """
            a = SameEvent;
            b = new SameEvent();
            SameEvent();
            
            function SameEvent()
            {
            }
            
            a = SameEvent;
            b = new SameEvent();
            SameEvent();
            c(new SameEvent());
            c.d(new SameEvent());
            c.d([new SameEvent()]);
            c.d(script_execute(new SameEvent()));
            c.d(function()
            {
                e = new SameEvent();
            });
            script_execute(new SameEvent());
            c(SameEvent);
            c.d(SameEvent);
            c.d([SameEvent]);
            c.d(script_execute(SameEvent));
            c.d(function()
            {
                e = SameEvent;
            });
            script_execute(SameEvent);
            a = DifferentScript;
            b = new DifferentScript();
            DifferentScript();
            c(new DifferentScript());
            c.d(new DifferentScript());
            c.d([new DifferentScript()]);
            c.d(script_execute(new DifferentScript()));
            c.d(function()
            {
                e = new DifferentScript();
            });
            script_execute(new DifferentScript());
            c(DifferentScript);
            c.d(DifferentScript);
            c.d([DifferentScript]);
            c.d(script_execute(DifferentScript));
            c.d(function()
            {
                e = DifferentScript;
            });
            script_execute(DifferentScript);
            a = DifferentScript_SubFunction;
            b = new DifferentScript_SubFunction();
            DifferentScript_SubFunction();
            c(DifferentScript_SubFunction);
            c.d(DifferentScript_SubFunction);
            c.d([DifferentScript_SubFunction]);
            c.d(script_execute(DifferentScript_SubFunction));
            c.d(function()
            {
                e = DifferentScript_SubFunction;
            });
            script_execute(DifferentScript_SubFunction);
            a = string;
            """,
            """
            pushref.i regular_func_SameEvent
            pop.v.v builtin.a
            push.i [function]regular_func_SameEvent
            conv.i.v
            call.i @@NewGMLObject@@ 1
            pop.v.v builtin.b
            call.i regular_func_SameEvent 0
            popz.v
            b [2]

            > regular_func_SameEvent (locals=0, args=0)
            :[1]
            exit.i

            :[2]
            push.i [function]regular_func_SameEvent
            conv.i.v
            pushi.e -1
            conv.i.v
            call.i method 2
            dup.v 0
            pushi.e -1
            pop.v.v [stacktop]self.SameEvent
            popz.v
            pushref.i regular_func_SameEvent
            pop.v.v builtin.a
            push.i [function]regular_func_SameEvent
            conv.i.v
            call.i @@NewGMLObject@@ 1
            pop.v.v builtin.b
            call.i regular_func_SameEvent 0
            popz.v
            push.i [function]regular_func_SameEvent
            conv.i.v
            call.i @@NewGMLObject@@ 1
            call.i @@This@@ 0
            push.v builtin.c
            callv.v 1
            popz.v
            push.v builtin.c
            push.i [function]regular_func_SameEvent
            conv.i.v
            call.i @@NewGMLObject@@ 1
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            push.v builtin.c
            push.i [function]regular_func_SameEvent
            conv.i.v
            call.i @@NewGMLObject@@ 1
            call.i @@NewGMLArray@@ 1
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            push.v builtin.c
            push.i [function]regular_func_SameEvent
            conv.i.v
            call.i @@NewGMLObject@@ 1
            call.i script_execute 1
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            push.v builtin.c
            b [4]

            > anon_func_1 (locals=0, args=0)
            :[3]
            pushref.i regular_func_SameEvent
            call.i @@NewGMLObject@@ 1
            pop.v.v builtin.e
            exit.i

            :[4]
            push.i [function]anon_func_1
            conv.i.v
            pushi.e -1
            conv.i.v
            call.i method 2
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            push.i [function]regular_func_SameEvent
            conv.i.v
            call.i @@NewGMLObject@@ 1
            call.i script_execute 1
            popz.v
            pushref.i regular_func_SameEvent
            call.i @@This@@ 0
            push.v builtin.c
            callv.v 1
            popz.v
            push.v builtin.c
            push.i [function]regular_func_SameEvent
            conv.i.v
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            push.v builtin.c
            push.i [function]regular_func_SameEvent
            conv.i.v
            call.i @@NewGMLArray@@ 1
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            push.v builtin.c
            push.i [function]regular_func_SameEvent
            conv.i.v
            call.i script_execute 1
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            push.v builtin.c
            b [6]

            > anon_func_2 (locals=0, args=0)
            :[5]
            pushref.i regular_func_SameEvent
            pop.v.v builtin.e
            exit.i

            :[6]
            push.i [function]anon_func_2
            conv.i.v
            pushi.e -1
            conv.i.v
            call.i method 2
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            pushref.i regular_func_SameEvent
            call.i script_execute 1
            popz.v
            pushref.i DifferentScript
            pop.v.v builtin.a
            pushref.i DifferentScript
            call.i @@NewGMLObject@@ 1
            pop.v.v builtin.b
            call.i DifferentScript 0
            popz.v
            pushref.i DifferentScript
            call.i @@NewGMLObject@@ 1
            call.i @@This@@ 0
            push.v builtin.c
            callv.v 1
            popz.v
            push.v builtin.c
            push.i [function]DifferentScript
            conv.i.v
            call.i @@NewGMLObject@@ 1
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            push.v builtin.c
            push.i [function]DifferentScript
            conv.i.v
            call.i @@NewGMLObject@@ 1
            call.i @@NewGMLArray@@ 1
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            push.v builtin.c
            push.i [function]DifferentScript
            conv.i.v
            call.i @@NewGMLObject@@ 1
            call.i script_execute 1
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            push.v builtin.c
            b [8]

            > anon_func_3 (locals=0, args=0)
            :[7]
            pushref.i DifferentScript
            call.i @@NewGMLObject@@ 1
            pop.v.v builtin.e
            exit.i

            :[8]
            push.i [function]anon_func_3
            conv.i.v
            pushi.e -1
            conv.i.v
            call.i method 2
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            pushref.i DifferentScript
            call.i @@NewGMLObject@@ 1
            call.i script_execute 1
            popz.v
            pushref.i DifferentScript
            call.i @@This@@ 0
            push.v builtin.c
            callv.v 1
            popz.v
            push.v builtin.c
            pushref.i DifferentScript
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            push.v builtin.c
            pushref.i DifferentScript
            call.i @@NewGMLArray@@ 1
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            push.v builtin.c
            pushref.i DifferentScript
            call.i script_execute 1
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            push.v builtin.c
            b [10]

            > anon_func_4 (locals=0, args=0)
            :[9]
            pushref.i DifferentScript
            pop.v.v builtin.e
            exit.i

            :[10]
            push.i [function]anon_func_4
            conv.i.v
            pushi.e -1
            conv.i.v
            call.i method 2
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            pushref.i DifferentScript
            call.i script_execute 1
            popz.v
            pushref.i global_func_DifferentScript_SubFunction
            pop.v.v builtin.a
            push.i [function]global_func_DifferentScript_SubFunction
            conv.i.v
            call.i @@NewGMLObject@@ 1
            pop.v.v builtin.b
            call.i global_func_DifferentScript_SubFunction 0
            popz.v
            pushref.i global_func_DifferentScript_SubFunction
            call.i @@This@@ 0
            push.v builtin.c
            callv.v 1
            popz.v
            push.v builtin.c
            pushref.i global_func_DifferentScript_SubFunction
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            push.v builtin.c
            pushref.i global_func_DifferentScript_SubFunction
            call.i @@NewGMLArray@@ 1
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            push.v builtin.c
            pushref.i global_func_DifferentScript_SubFunction
            call.i script_execute 1
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            push.v builtin.c
            b [12]

            > anon_func_5 (locals=0, args=0)
            :[11]
            pushref.i global_func_DifferentScript_SubFunction
            pop.v.v builtin.e
            exit.i

            :[12]
            push.i [function]anon_func_5
            conv.i.v
            pushi.e -1
            conv.i.v
            call.i method 2
            dup.v 1 1
            dup.v 0
            push.v stacktop.d
            callv.v 1
            popz.v
            pushref.i global_func_DifferentScript_SubFunction
            call.i script_execute 1
            popz.v
            push.i [function]string
            pop.v.i builtin.a
            """,
            false,
            gameContextMock
        );
    }

    [Fact]
    public void TestOldFunctionResolution()
    {
        TestUtil.AssertBytecode(
            """
            function event_function()
            {
            }

            function other_event_function()
            {
                event_function();
            }

            event_function();
            """,
            """
            b [2]

            > regular_func_event_function (locals=0, args=0)
            :[1]
            exit.i

            :[2]
            push.i [function]regular_func_event_function
            conv.i.v
            pushi.e -1
            conv.i.v
            call.i method 2
            dup.v 0
            pushi.e -1
            pop.v.v [stacktop]self.event_function
            popz.v
            b [4]

            > regular_func_other_event_function (locals=0, args=0)
            :[3]
            call.i @@This@@ 0
            push.v builtin.event_function
            callv.v 0
            popz.v
            exit.i

            :[4]
            push.i [function]regular_func_other_event_function
            conv.i.v
            pushi.e -1
            conv.i.v
            call.i method 2
            dup.v 0
            pushi.e -1
            pop.v.v [stacktop]self.other_event_function
            popz.v
            call.i regular_func_event_function 0
            popz.v
            """,
            false,
            new Underanalyzer.Mock.GameContextMock()
            {
                UsingSelfToBuiltin = true,
                UsingNewFunctionVariables = true,
                UsingFunctionScriptReferences = true,
                UsingNewFunctionResolution = false
            }
        );
    }

    [Fact]
    public void TestNewFunctionResolution()
    {
        TestUtil.AssertBytecode(
            """
            function event_function()
            {
            }

            function other_event_function()
            {
                event_function();
            }

            event_function();
            """,
            """
            b [2]

            > regular_func_event_function (locals=0, args=0)
            :[1]
            exit.i

            :[2]
            push.i [function]regular_func_event_function
            conv.i.v
            pushi.e -1
            conv.i.v
            call.i method 2
            dup.v 0
            pushi.e -1
            pop.v.v [stacktop]self.event_function
            popz.v
            b [4]

            > regular_func_other_event_function (locals=0, args=0)
            :[3]
            call.i @@This@@ 0
            pushref.i regular_func_event_function
            callv.v 0
            popz.v
            exit.i

            :[4]
            push.i [function]regular_func_other_event_function
            conv.i.v
            pushi.e -1
            conv.i.v
            call.i method 2
            dup.v 0
            pushi.e -1
            pop.v.v [stacktop]self.other_event_function
            popz.v
            call.i regular_func_event_function 0
            popz.v
            """,
            false,
            new Underanalyzer.Mock.GameContextMock()
            {
                UsingSelfToBuiltin = true,
                UsingNewFunctionVariables = true,
                UsingFunctionScriptReferences = true,
                UsingNewFunctionResolution = true
            }
        );
    }
}