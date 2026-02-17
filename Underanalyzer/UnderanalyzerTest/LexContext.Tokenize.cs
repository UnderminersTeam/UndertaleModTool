/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using Underanalyzer;
using Underanalyzer.Compiler.Lexer;
using Underanalyzer.Mock;

namespace UnderanalyzerTest;

public class LexContext_Tokenize
{
    [Fact]
    public void TestBasic()
    {
        LexContext context = TestUtil.Lex(
            """
            a = 123;
            if (b)
            {
                c = "Test string!";
            }
            """
        );

        Assert.Empty(context.CompileContext.Errors);
        TestUtil.AssertTokens([
            ("a", typeof(TokenVariable)),
            ("=", typeof(TokenOperator)),
            ("123", typeof(TokenNumber)),
            (";", typeof(TokenSeparator)),
            ("if", typeof(TokenKeyword)),
            ("(", typeof(TokenSeparator)),
            ("b", typeof(TokenVariable)),
            (")", typeof(TokenSeparator)),
            ("{", typeof(TokenSeparator)),
            ("c", typeof(TokenVariable)),
            ("=", typeof(TokenOperator)),
            ("\"Test string!\"", typeof(TokenString)),
            (";", typeof(TokenSeparator)),
            ("}", typeof(TokenSeparator)),
        ], context.Tokens);

        Assert.Equal("a", ((TokenVariable)context.Tokens[0]).Text);
        Assert.Equal(OperatorKind.Assign, ((TokenOperator)context.Tokens[1]).Kind);
        Assert.Equal(123, ((TokenNumber)context.Tokens[2]).Value);
        Assert.Equal(SeparatorKind.Semicolon, ((TokenSeparator)context.Tokens[3]).Kind);
        Assert.Equal(KeywordKind.If, ((TokenKeyword)context.Tokens[4]).Kind);
        Assert.Equal("Test string!", ((TokenString)context.Tokens[11]).Value);
    }

    [Fact]
    public void TestInvalidToken()
    {
        LexContext context = TestUtil.Lex(
            """
            `
            """
        );

        Assert.Single(context.CompileContext.Errors);
        Assert.True(context.CompileContext.HasErrors);
    }

    [Fact]
    public void TestMacrosBasic()
    {
        LexContext context = TestUtil.Lex(
             """
            #macro TEST_A a
            #macro TEST_B 123
            #macro TEST_FUNC func_name

            TEST_FUNC(TEST_A + TEST_B);
            """
        );

        Assert.Empty(context.CompileContext.Errors);
        Assert.False(context.CompileContext.HasErrors);
        TestUtil.AssertTokens([
            ("func_name", typeof(TokenFunction)),
            ("(", typeof(TokenSeparator)),
            ("a", typeof(TokenVariable)),
            ("+", typeof(TokenOperator)),
            ("123", typeof(TokenNumber)),
            (")", typeof(TokenSeparator)),
            (";", typeof(TokenSeparator)),
        ], context.Tokens);
    }

    [Fact]
    public void TestMacrosComplex()
    {
        LexContext context = TestUtil.Lex(
            """
            #macro TEST_A TEST_B
            #macro TEST_B TEST_C
            #macro TEST_C TEST_D
            #macro TEST_D 123
            TEST_A
            TEST_B
            TEST_C
            TEST_D
            """
        );

        Assert.Empty(context.CompileContext.Errors);
        Assert.False(context.CompileContext.HasErrors);
        TestUtil.AssertTokens([
            ("123", typeof(TokenNumber)),
            ("123", typeof(TokenNumber)),
            ("123", typeof(TokenNumber)),
            ("123", typeof(TokenNumber)),
        ], context.Tokens);
    }

    [Fact]
    public void TestMacrosFailure1()
    {
        LexContext context = TestUtil.Lex(
            """
            #macro TEST_A TEST_B
            #macro TEST_B TEST_A
            TEST_A
            """
        );

        Assert.Single(context.CompileContext.Errors);
        Assert.True(context.CompileContext.HasErrors);
    }

    [Fact]
    public void TestMacrosFailure2()
    {
        LexContext context = TestUtil.Lex(
            """
            #macro TEST_C 123
            #macro TEST_A TEST_C TEST_B TEST_C
            #macro TEST_B TEST_C TEST_A TEST_C
            TEST_A
            """
        );

        Assert.Single(context.CompileContext.Errors);
        Assert.True(context.CompileContext.HasErrors);
    }

    [Fact]
    public void TestMacrosFailure3()
    {
        LexContext context = TestUtil.Lex(
            """
            #macro TEST_A TEST_B
            #macro TEST_B TEST_C
            #macro TEST_C TEST_A
            TEST_A
            """
        );

        Assert.Single(context.CompileContext.Errors);
        Assert.True(context.CompileContext.HasErrors);
    }

    [Fact]
    public void TestMacrosMultiline()
    {
        LexContext context = TestUtil.Lex(
            """
            #macro TEST_A a = \
                          123;
            TEST_A
            """
        );

        Assert.Empty(context.CompileContext.Errors);
        Assert.False(context.CompileContext.HasErrors);
        TestUtil.AssertTokens([
            ("a", typeof(TokenVariable)),
            ("=", typeof(TokenOperator)),
            ("123", typeof(TokenNumber)),
            (";", typeof(TokenSeparator)),
        ], context.Tokens);
    }

    [Fact]
    public void TestStringUnenclosed()
    {
        LexContext context = TestUtil.Lex(
            """
            "
            """
        );

        Assert.Single(context.CompileContext.Errors);
        Assert.True(context.CompileContext.HasErrors);
    }

    [Fact]
    public void TestStringUnenclosed2()
    {
        LexContext context = TestUtil.Lex(
            """
            @"
            """
        );

        Assert.Single(context.CompileContext.Errors);
        Assert.True(context.CompileContext.HasErrors);
    }

    [Fact]
    public void TestStringNewlines()
    {
        LexContext context = TestUtil.Lex(
            """
            "
            a
            b
            "
            """
        );

        Assert.Single(context.CompileContext.Errors);
        Assert.True(context.CompileContext.HasErrors);
    }

    [Fact]
    public void TestStringEscapes()
    {
        LexContext context = TestUtil.Lex(
            """
            "\\\a\b\f\n\r\t\v\u00e2\u61\x41\101"
            """
        );

        Assert.Empty(context.CompileContext.Errors);
        Assert.False(context.CompileContext.HasErrors);
        TestUtil.AssertTokens([
            ("\"\\\\\\a\\b\\f\\n\\r\\t\\v\\u00e2\\u61\\x41\\101\"", typeof(TokenString)),
        ], context.Tokens);
        Assert.Equal("\\\a\b\f\n\r\t\v\u00e2\u0061AA", ((TokenString)context.Tokens[0]).Value);
    }

    [Fact]
    public void TestStringEscapesFailure()
    {
        LexContext context = TestUtil.Lex(
            """
            "\uFFFFFF"
            "\x"
            "\1"
            """
        );

        Assert.Equal(3, context.CompileContext.Errors.Count);
        Assert.True(context.CompileContext.HasErrors);
    }

    [Fact]
    public void TestStringVerbatim()
    {
        LexContext context = TestUtil.Lex(
            """
            "this is a
            'multiline' string!"
            'this is "another"
            string!'
            """,
            new GameContextMock()
            {
                UsingGMS2OrLater = false
            }
        );

        Assert.Empty(context.CompileContext.Errors);
        Assert.False(context.CompileContext.HasErrors);
        TestUtil.AssertTokens([
            ("\"this is a\n'multiline' string!\"", typeof(TokenString)),
            ("'this is \"another\"\nstring!'", typeof(TokenString)),
        ], context.Tokens);
        Assert.Equal("this is a\n'multiline' string!", ((TokenString)context.Tokens[0]).Value.ReplaceLineEndings("\n"));
        Assert.Equal("this is \"another\"\nstring!", ((TokenString)context.Tokens[1]).Value.ReplaceLineEndings("\n"));
    }

    [Fact]
    public void TestStringVerbatim2()
    {
        LexContext context = TestUtil.Lex(
            """
            @"this is a
            'multiline' string!"
            @'this is "another"
            string!'
            """
        );

        Assert.Empty(context.CompileContext.Errors);
        Assert.False(context.CompileContext.HasErrors);
        TestUtil.AssertTokens([
            ("@\"this is a\n'multiline' string!\"", typeof(TokenString)),
            ("@'this is \"another\"\nstring!'", typeof(TokenString)),
        ], context.Tokens);
        Assert.Equal("this is a\n'multiline' string!", ((TokenString)context.Tokens[0]).Value.ReplaceLineEndings("\n"));
        Assert.Equal("this is \"another\"\nstring!", ((TokenString)context.Tokens[1]).Value.ReplaceLineEndings("\n"));
    }

    [Fact]
    public void TestHex()
    {
        LexContext context = TestUtil.Lex(
            """
            0x1234abcd
            $4567abcd
            #FFEEDD
            """
        );

        Assert.Empty(context.CompileContext.Errors);
        Assert.False(context.CompileContext.HasErrors);
        TestUtil.AssertTokens([
            ("0x1234abcd", typeof(TokenNumber)),
            ("$4567abcd", typeof(TokenNumber)),
            ("#FFEEDD", typeof(TokenNumber)),
        ], context.Tokens);
        Assert.Equal(0x1234abcd, ((TokenNumber)context.Tokens[0]).Value);
        Assert.Equal(0x4567abcd, ((TokenNumber)context.Tokens[1]).Value);
        Assert.Equal(0xDDEEFF, ((TokenNumber)context.Tokens[2]).Value);
    }

    [Fact]
    public void TestHex2()
    {
        LexContext context = TestUtil.Lex(
            """
            0xFFFFFFFFFF
            #FFFFFFFF
            """
        );

        Assert.Empty(context.CompileContext.Errors);
        Assert.False(context.CompileContext.HasErrors);
        TestUtil.AssertTokens([
            ("0xFFFFFFFFFF", typeof(TokenInt64)),
            ("#FFFFFFFF", typeof(TokenInt64)),
        ], context.Tokens);
        Assert.Equal(0xFFFFFFFFFF, ((TokenInt64)context.Tokens[0]).Value);
        Assert.Equal(0xFFFFFFFF, ((TokenInt64)context.Tokens[1]).Value);
    }

    [Fact]
    public void TestNumbers()
    {
        LexContext context = TestUtil.Lex(
            """
            123
            .1
            0.1
            1.1
            -1
            2.3.4.
            999999999999999999
            """
        );

        Assert.Empty(context.CompileContext.Errors);
        Assert.False(context.CompileContext.HasErrors);
        TestUtil.AssertTokens([
            ("123", typeof(TokenNumber)),
            (".1", typeof(TokenNumber)),
            ("0.1", typeof(TokenNumber)),
            ("1.1", typeof(TokenNumber)),
            ("-", typeof(TokenOperator)),
            ("1", typeof(TokenNumber)),
            ("2.3", typeof(TokenNumber)),
            (".4", typeof(TokenNumber)),
            (".", typeof(TokenSeparator)),
            ("999999999999999999", typeof(TokenInt64)),
        ], context.Tokens);
        Assert.Equal(123, ((TokenNumber)context.Tokens[0]).Value);
        Assert.Equal(.1, ((TokenNumber)context.Tokens[1]).Value);
        Assert.Equal(0.1, ((TokenNumber)context.Tokens[2]).Value);
        Assert.Equal(1.1, ((TokenNumber)context.Tokens[3]).Value);
        Assert.Equal(1, ((TokenNumber)context.Tokens[5]).Value);
        Assert.Equal(2.3, ((TokenNumber)context.Tokens[6]).Value);
        Assert.Equal(.4, ((TokenNumber)context.Tokens[7]).Value);
        Assert.Equal(999999999999999999, ((TokenInt64)context.Tokens[9]).Value);
    }

    [Fact]
    public void TestComments()
    {
        LexContext context = TestUtil.Lex(
            """
            // Test comment!
            a
            /* Test
               Multiline
               Comment
            */
            /** Single-line multi-line test **/
            /** 
                Another multi-line test 
            **/
                /** 
                    Another multi-line test 
                **/
            b
            """
        );

        Assert.Empty(context.CompileContext.Errors);
        Assert.False(context.CompileContext.HasErrors);
        TestUtil.AssertTokens([
            ("a", typeof(TokenVariable)),
            ("b", typeof(TokenVariable)),
        ], context.Tokens);
    }

    [Fact]
    public void TestBadMacro()
    {
        LexContext context = TestUtil.Lex(
            """
            #macro TEST #macro BAD
            """
        );

        Assert.Single(context.CompileContext.Errors);
        Assert.True(context.CompileContext.HasErrors);
    }

    [Fact]
    public void TestBackslashInMacro()
    {
        LexContext context = TestUtil.Lex(
            """
            #macro TEST "test\nstring"
            TEST
            """
        );

        Assert.Empty(context.CompileContext.Errors);
        Assert.False(context.CompileContext.HasErrors);
        TestUtil.AssertTokens([
            ("\"test\\nstring\"", typeof(TokenString)),
        ], context.Tokens);
        Assert.Equal("test\nstring", ((TokenString)context.Tokens[0]).Value.ReplaceLineEndings("\n"));
    }

    [Fact]
    public void TestBackslashInMacro2()
    {
        LexContext context = TestUtil.Lex(
            """
            #macro TEST "test\nstring\\ " \
                        second_line
            TEST
            """
        );

        Assert.Empty(context.CompileContext.Errors);
        Assert.False(context.CompileContext.HasErrors);
        TestUtil.AssertTokens([
            ("\"test\\nstring\\\\ \"", typeof(TokenString)),
            ("second_line", typeof(TokenVariable)),
        ], context.Tokens);
        Assert.Equal("test\nstring\\ ", ((TokenString)context.Tokens[0]).Value.ReplaceLineEndings("\n"));
    }

    [Fact]
    public void TestDuplicateMacro()
    {
        LexContext context = TestUtil.Lex(
            """
            #macro TEST a
            #macro TEST b
            """
        );

        Assert.Single(context.CompileContext.Errors);
        Assert.True(context.CompileContext.HasErrors);
    }

    [Fact]
    public void TestRegions()
    {
        LexContext context = TestUtil.Lex(
            """
            #region Test
            a
            #endregion
            """
        );

        Assert.Empty(context.CompileContext.Errors);
        Assert.False(context.CompileContext.HasErrors);
        TestUtil.AssertTokens([
            ("a", typeof(TokenVariable)),
        ], context.Tokens);
    }

    [Fact]
    public void TestInvalidTags()
    {
        LexContext context = TestUtil.Lex(
            """
            #boof
            #unknown
            #aabbgg
            """
        );

        Assert.Equal(3, context.CompileContext.Errors.Count);
        Assert.True(context.CompileContext.HasErrors);
    }

    [Fact]
    public void TestAssets()
    {
        GameContextMock mock = new();
        mock.DefineMockAsset(AssetType.Sprite, 8, "spr_test");
        LexContext context = TestUtil.Lex(
            """
            sprite = spr_test;
            notSprite = spr_test_nonexistent;
            """,
            mock
        );

        Assert.Empty(context.CompileContext.Errors);
        Assert.False(context.CompileContext.HasErrors);
        TestUtil.AssertTokens([
            ("sprite", typeof(TokenVariable)),
            ("=", typeof(TokenOperator)),
            ("spr_test", typeof(TokenAssetReference)),
            (";", typeof(TokenSeparator)),
            ("notSprite", typeof(TokenVariable)),
            ("=", typeof(TokenOperator)),
            ("spr_test_nonexistent", typeof(TokenVariable)),
            (";", typeof(TokenSeparator)),
        ], context.Tokens);

        Assert.True(mock.GetAssetId("spr_test", out int assetId));
        Assert.Equal(assetId, ((TokenAssetReference)context.Tokens[2]).AssetId);
    }
}
