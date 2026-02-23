/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using Underanalyzer;
using Underanalyzer.Compiler.Nodes;
using Underanalyzer.Compiler.Parser;

namespace UnderanalyzerTest;

public class ParseContext_Parse
{
    [Fact]
    public void TestSimpleFunctionCalls()
    {
        ParseContext context = TestUtil.Parse(
            """
            show_message("Hello, world!");
            a(b(), c(456, 789), true, 0.5, , false);
            """
        );

        Assert.Empty(context.CompileContext.Errors);
        Assert.False(context.CompileContext.HasErrors);
        Assert.Collection(((BlockNode)context.Root!).Children,
            (node) =>
            {
                SimpleFunctionCallNode funcCall = (SimpleFunctionCallNode)node;
                Assert.Equal("show_message", funcCall.FunctionName);
                Assert.True(funcCall.IsStatement);
                Assert.Single(funcCall.Arguments);
                Assert.Equal("Hello, world!", ((StringNode)funcCall.Arguments[0]).Value);
            },
            (node) =>
            {
                SimpleFunctionCallNode funcCall = (SimpleFunctionCallNode)node;
                Assert.Equal("a", funcCall.FunctionName);
                Assert.True(funcCall.IsStatement);
                Assert.Collection(funcCall.Arguments,
                    (node) =>
                    {
                        SimpleFunctionCallNode funcCall = (SimpleFunctionCallNode)node;
                        Assert.Equal("b", funcCall.FunctionName);
                        Assert.False(funcCall.IsStatement);
                        Assert.Empty(funcCall.Arguments);
                    },
                    (node) =>
                    {
                        SimpleFunctionCallNode funcCall = (SimpleFunctionCallNode)node;
                        Assert.Equal("c", funcCall.FunctionName);
                        Assert.False(funcCall.IsStatement);
                        Assert.Collection(funcCall.Arguments,
                            (node) => Assert.Equal(456, ((NumberNode)node).Value),
                            (node) => Assert.Equal(789, ((NumberNode)node).Value)
                        );
                    },
                    (node) => Assert.True(((BooleanNode)node).Value),
                    (node) => Assert.Equal(0.5, ((NumberNode)node).Value),
                    (node) => Assert.Equal("undefined", ((SimpleVariableNode)node).VariableName),
                    (node) => Assert.False(((BooleanNode)node).Value)
                );
            }
        );
    }

    [Fact]
    public void TestIf()
    {
        ParseContext context = TestUtil.Parse(
            """
            if (1)
            {
            }
            else
            {
            }

            if (2) call();

            if 3 then begin end else {}
            """
        );

        Assert.Empty(context.CompileContext.Errors);
        Assert.False(context.CompileContext.HasErrors);
        Assert.Collection(((BlockNode)context.Root!).Children,
            (node) =>
            {
                IfNode ifNode = (IfNode)node;
                Assert.Equal(1, ((NumberNode)ifNode.Condition!).Value);
                Assert.Empty(((BlockNode)ifNode.TrueStatement!).Children);
                Assert.Empty(((BlockNode)ifNode.FalseStatement!).Children);
            },
            (node) =>
            {
                IfNode ifNode = (IfNode)node;
                Assert.Equal(2, ((NumberNode)ifNode.Condition!).Value);
                Assert.Empty(((SimpleFunctionCallNode)ifNode.TrueStatement!).Arguments);
                Assert.Null(ifNode.FalseStatement);
            },
            (node) =>
            {
                IfNode ifNode = (IfNode)node;
                Assert.Equal(3, ((NumberNode)ifNode.Condition!).Value);
                Assert.Empty(((BlockNode)ifNode.TrueStatement!).Children);
                Assert.Empty(((BlockNode)ifNode.FalseStatement!).Children);
            }
        );
    }

    [Fact]
    public void TestAssignsAndFunctions()
    {
        ParseContext context = TestUtil.Parse(
            """
            a = 123;
            ++b;
            if (++c) {}
            d();
            if (e()) {}
            f++;
            if (g++) {}
            """
        );

        Assert.Empty(context.CompileContext.Errors);
        Assert.False(context.CompileContext.HasErrors);
        Assert.Collection(((BlockNode)context.Root!).Children,
            (node) =>
            {
                AssignNode assign = (AssignNode)node;
                Assert.Equal(AssignNode.AssignKind.Normal, assign.Kind);
                Assert.Equal("a", ((SimpleVariableNode)assign.Destination!).VariableName);
                Assert.Equal(123, ((NumberNode)assign.Expression!).Value);
            },
            (node) =>
            {
                PrefixNode prefix = (PrefixNode)node;
                Assert.Equal("b", ((SimpleVariableNode)prefix.Expression!).VariableName);
                Assert.True(prefix.IsIncrement);
                Assert.True(prefix.IsStatement);
            },
            (node) =>
            {
                IfNode ifNode = (IfNode)node;
                PrefixNode prefix = (PrefixNode)ifNode.Condition!;
                Assert.Equal("c", ((SimpleVariableNode)prefix.Expression!).VariableName);
                Assert.True(prefix.IsIncrement);
                Assert.False(prefix.IsStatement);
            },
            (node) =>
            {
                SimpleFunctionCallNode funcCall = (SimpleFunctionCallNode)node;
                Assert.Equal("d", funcCall.FunctionName);
                Assert.True(funcCall.IsStatement);
                Assert.Empty(funcCall.Arguments);
            },
            (node) =>
            {
                IfNode ifNode = (IfNode)node;
                SimpleFunctionCallNode funcCall = (SimpleFunctionCallNode)ifNode.Condition!;
                Assert.Equal("e", funcCall.FunctionName);
                Assert.False(funcCall.IsStatement);
                Assert.Empty(funcCall.Arguments);
            },
            (node) =>
            {
                PostfixNode postfix = (PostfixNode)node;
                Assert.Equal("f", ((SimpleVariableNode)postfix.Expression!).VariableName);
                Assert.True(postfix.IsIncrement);
                Assert.True(postfix.IsStatement);
            },
            (node) =>
            {
                IfNode ifNode = (IfNode)node;
                PostfixNode postfix = (PostfixNode)ifNode.Condition!;
                Assert.Equal("g", ((SimpleVariableNode)postfix.Expression!).VariableName);
                Assert.True(postfix.IsIncrement);
                Assert.False(postfix.IsStatement);
            }
        );
    }

    [Fact]
    public void TestAssignTypes()
    {
        ParseContext context = TestUtil.Parse(
            """
            a = 1;
            a := 1;
            a += 1;
            a -= 1;
            a *= 1;
            a /= 1;
            a %= 1;
            a &= 1;
            a |= 1;
            a ^= 1;
            a ??= 1;
            ++a;
            --a;
            a++;
            a--;
            """
        );

        Assert.Empty(context.CompileContext.Errors);
        Assert.False(context.CompileContext.HasErrors);
        Assert.Collection(((BlockNode)context.Root!).Children,
            (node) => Assert.Equal(AssignNode.AssignKind.Normal, ((AssignNode)node).Kind),
            (node) => Assert.Equal(AssignNode.AssignKind.Normal, ((AssignNode)node).Kind),
            (node) => Assert.Equal(AssignNode.AssignKind.CompoundPlus, ((AssignNode)node).Kind),
            (node) => Assert.Equal(AssignNode.AssignKind.CompoundMinus, ((AssignNode)node).Kind),
            (node) => Assert.Equal(AssignNode.AssignKind.CompoundTimes, ((AssignNode)node).Kind),
            (node) => Assert.Equal(AssignNode.AssignKind.CompoundDivide, ((AssignNode)node).Kind),
            (node) => Assert.Equal(AssignNode.AssignKind.CompoundMod, ((AssignNode)node).Kind),
            (node) => Assert.Equal(AssignNode.AssignKind.CompoundBitwiseAnd, ((AssignNode)node).Kind),
            (node) => Assert.Equal(AssignNode.AssignKind.CompoundBitwiseOr, ((AssignNode)node).Kind),
            (node) => Assert.Equal(AssignNode.AssignKind.CompoundBitwiseXor, ((AssignNode)node).Kind),
            (node) => Assert.Equal(AssignNode.AssignKind.CompoundNullishCoalesce, ((AssignNode)node).Kind),
            (node) => Assert.True(((PrefixNode)node).IsIncrement),
            (node) => Assert.False(((PrefixNode)node).IsIncrement),
            (node) => Assert.True(((PostfixNode)node).IsIncrement),
            (node) => Assert.False(((PostfixNode)node).IsIncrement)
        );
    }

    [Fact]
    public void TestUnary()
    {
        ParseContext context = TestUtil.Parse(
            """
            a = !b;
            a = ~b;
            a = +b;
            a = -b;
            a = not b;
            """
        );

        Assert.Empty(context.CompileContext.Errors);
        Assert.False(context.CompileContext.HasErrors);
        Assert.Collection(((BlockNode)context.Root!).Children,
            (node) =>
            {
                AssignNode assign = (AssignNode)node;
                Assert.Equal(AssignNode.AssignKind.Normal, assign.Kind);
                Assert.Equal("a", ((SimpleVariableNode)assign.Destination!).VariableName);
                UnaryNode unary = (UnaryNode)assign.Expression!;
                Assert.Equal(UnaryNode.UnaryKind.BooleanNot, unary.Kind);
                Assert.Equal("b", ((SimpleVariableNode)unary.Expression!).VariableName);
            },
            (node) =>
            {
                AssignNode assign = (AssignNode)node;
                Assert.Equal(AssignNode.AssignKind.Normal, assign.Kind);
                Assert.Equal("a", ((SimpleVariableNode)assign.Destination!).VariableName);
                UnaryNode unary = (UnaryNode)assign.Expression!;
                Assert.Equal(UnaryNode.UnaryKind.BitwiseNegate, unary.Kind);
                Assert.Equal("b", ((SimpleVariableNode)unary.Expression!).VariableName);
            },
            (node) =>
            {
                AssignNode assign = (AssignNode)node;
                Assert.Equal(AssignNode.AssignKind.Normal, assign.Kind);
                Assert.Equal("a", ((SimpleVariableNode)assign.Destination!).VariableName);
                UnaryNode unary = (UnaryNode)assign.Expression!;
                Assert.Equal(UnaryNode.UnaryKind.Positive, unary.Kind);
                Assert.Equal("b", ((SimpleVariableNode)unary.Expression!).VariableName);
            },
            (node) =>
            {
                AssignNode assign = (AssignNode)node;
                Assert.Equal(AssignNode.AssignKind.Normal, assign.Kind);
                Assert.Equal("a", ((SimpleVariableNode)assign.Destination!).VariableName);
                UnaryNode unary = (UnaryNode)assign.Expression!;
                Assert.Equal(UnaryNode.UnaryKind.Negative, unary.Kind);
                Assert.Equal("b", ((SimpleVariableNode)unary.Expression!).VariableName);
            },
            (node) =>
            {
                AssignNode assign = (AssignNode)node;
                Assert.Equal(AssignNode.AssignKind.Normal, assign.Kind);
                Assert.Equal("a", ((SimpleVariableNode)assign.Destination!).VariableName);
                UnaryNode unary = (UnaryNode)assign.Expression!;
                Assert.Equal(UnaryNode.UnaryKind.BooleanNot, unary.Kind);
                Assert.Equal("b", ((SimpleVariableNode)unary.Expression!).VariableName);
            }
        );
    }

    [Fact]
    public void TestArrayLiteral()
    {
        ParseContext context = TestUtil.Parse(
            """
            a = [];
            b = [1];
            c = [1, 2, 3];
            """
        );

        Assert.Empty(context.CompileContext.Errors);
        Assert.False(context.CompileContext.HasErrors);
        Assert.Collection(((BlockNode)context.Root!).Children,
            (node) =>
            {
                AssignNode assign = (AssignNode)node;
                Assert.Equal(AssignNode.AssignKind.Normal, assign.Kind);
                Assert.Equal("a", ((SimpleVariableNode)assign.Destination!).VariableName);
                SimpleFunctionCallNode func = (SimpleFunctionCallNode)assign.Expression!;
                Assert.Equal(VMConstants.NewArrayFunction, func.FunctionName);
                Assert.Empty(func.Arguments);
            },
            (node) =>
            {
                AssignNode assign = (AssignNode)node;
                Assert.Equal(AssignNode.AssignKind.Normal, assign.Kind);
                Assert.Equal("b", ((SimpleVariableNode)assign.Destination!).VariableName);
                SimpleFunctionCallNode func = (SimpleFunctionCallNode)assign.Expression!;
                Assert.Equal(VMConstants.NewArrayFunction, func.FunctionName);
                Assert.Single(func.Arguments);
                Assert.Equal(1, ((NumberNode)func.Arguments[0]).Value);
            },
            (node) =>
            {
                AssignNode assign = (AssignNode)node;
                Assert.Equal(AssignNode.AssignKind.Normal, assign.Kind);
                Assert.Equal("c", ((SimpleVariableNode)assign.Destination!).VariableName);
                SimpleFunctionCallNode func = (SimpleFunctionCallNode)assign.Expression!;
                Assert.Equal(VMConstants.NewArrayFunction, func.FunctionName);
                Assert.Equal(3, func.Arguments.Count);
                Assert.Equal(1, ((NumberNode)func.Arguments[0]).Value);
                Assert.Equal(2, ((NumberNode)func.Arguments[1]).Value);
                Assert.Equal(3, ((NumberNode)func.Arguments[2]).Value);
            }
        );
    }

    [Fact]
    public void TestExitReturnBreakContinue()
    {
        ParseContext context = TestUtil.Parse(
            """
            exit;
            return;
            return 123;
            break;
            continue;
            """
        );

        Assert.Empty(context.CompileContext.Errors);
        Assert.False(context.CompileContext.HasErrors);
        Assert.Collection(((BlockNode)context.Root!).Children,
            (node) => Assert.IsType<ExitNode>(node),
            (node) => Assert.IsType<ExitNode>(node),
            (node) =>
            {
                Assert.IsType<ReturnNode>(node);
                ReturnNode ret = (ReturnNode)node;
                Assert.Equal(123, ((NumberNode)ret.ReturnValue).Value);
            },
            (node) => Assert.IsType<BreakNode>(node),
            (node) => Assert.IsType<ContinueNode>(node)
        );
    }

    [Fact]
    public void TestErrorFreeFloating()
    {
        ParseContext context = TestUtil.Parse(
            """
            1
            """
        );
        Assert.Equal(2, context.CompileContext.Errors.Count);
        Assert.True(context.CompileContext.HasErrors);
    }

    [Fact]
    public void TestErrorEndOfCode1()
    {
        ParseContext context = TestUtil.Parse(
            """
            a =
            """
        );
        Assert.Equal(2, context.CompileContext.Errors.Count);
        Assert.True(context.CompileContext.HasErrors);
    }

    [Fact]
    public void TestErrorEndOfCode2()
    {
        ParseContext context = TestUtil.Parse(
            """
            {
            """
        );
        Assert.Single(context.CompileContext.Errors);
        Assert.True(context.CompileContext.HasErrors);
    }

    [Fact]
    public void TestErrorInvalidExpression()
    {
        ParseContext context = TestUtil.Parse(
            """
            a = ;
            """
        );
        Assert.Equal(2, context.CompileContext.Errors.Count);
        Assert.True(context.CompileContext.HasErrors);
    }

    [Fact]
    public void TestErrorUnclosedCall()
    {
        ParseContext context = TestUtil.Parse(
            """
            a(1, 2
            """
        );
        Assert.Single(context.CompileContext.Errors);
        Assert.True(context.CompileContext.HasErrors);
    }

    [Fact]
    public void TestChain()
    {
        ParseContext context = TestUtil.Parse(
            """
            a[0].b.c()[123][456] = 789;
            """
        );

        Assert.Empty(context.CompileContext.Errors);
        Assert.False(context.CompileContext.HasErrors);
        Assert.Single(((BlockNode)context.Root!).Children);
        AssignNode assign = (AssignNode)((BlockNode)context.Root!).Children[0];
        AccessorNode accessor1 = (AccessorNode)assign.Destination;
        Assert.Equal(456, ((NumberNode)accessor1.AccessorExpression!).Value);
        Assert.Null(accessor1.AccessorExpression2);
        AccessorNode accessor2 = (AccessorNode)accessor1.Expression;
        Assert.Equal(123, ((NumberNode)accessor2.AccessorExpression!).Value);
        Assert.Null(accessor2.AccessorExpression2);
        FunctionCallNode funcCall = (FunctionCallNode)accessor2.Expression;
        Assert.Empty(funcCall.Arguments);
        DotVariableNode dot1 = (DotVariableNode)funcCall.Expression;
        Assert.Equal("c", dot1.VariableName);
        DotVariableNode dot2 = (DotVariableNode)dot1.LeftExpression;
        Assert.Equal("b", dot2.VariableName);
        AccessorNode accessor3 = (AccessorNode)dot2.LeftExpression;
        Assert.Equal(0, ((NumberNode)accessor3.AccessorExpression!).Value);
        Assert.Null(accessor3.AccessorExpression2);
        SimpleVariableNode variable = (SimpleVariableNode)accessor3.Expression;
        Assert.Equal("a", variable.VariableName);
    }

    [Fact]
    public void TestConditional()
    {
        ParseContext context = TestUtil.Parse(
            """
            a = b ? c : d;
            """
        );

        Assert.Empty(context.CompileContext.Errors);
        Assert.False(context.CompileContext.HasErrors);
        Assert.Single(((BlockNode)context.Root!).Children);
        AssignNode assign = (AssignNode)((BlockNode)context.Root!).Children[0];
        ConditionalNode conditional = (ConditionalNode)assign.Expression;
        Assert.Equal("b", ((SimpleVariableNode)conditional.Condition).VariableName);
        Assert.Equal("c", ((SimpleVariableNode)conditional.TrueExpression).VariableName);
        Assert.Equal("d", ((SimpleVariableNode)conditional.FalseExpression).VariableName);
    }

    [Fact]
    public void TestNullish()
    {
        ParseContext context = TestUtil.Parse(
            """
            a = b ?? c;
            """
        );

        Assert.Empty(context.CompileContext.Errors);
        Assert.False(context.CompileContext.HasErrors);
        Assert.Single(((BlockNode)context.Root!).Children);
        AssignNode assign = (AssignNode)((BlockNode)context.Root!).Children[0];
        NullishCoalesceNode conditional = (NullishCoalesceNode)assign.Expression;
        Assert.Equal("b", ((SimpleVariableNode)conditional.Left).VariableName);
        Assert.Equal("c", ((SimpleVariableNode)conditional.Right).VariableName);
    }

    [Fact]
    public void TestBinary()
    {
        ParseContext context = TestUtil.Parse(
            """
            a = (b + c) - (d * e);
            f = g + h - i;
            j = k * l / m % n mod o div p;
            q = (r / s) * t;
            u = v / (w * x);
            y = 1 & 2 | 3 ^ 4;
            z = 1 << 2 >> 3;
            """
        );

        Assert.Empty(context.CompileContext.Errors);
        Assert.False(context.CompileContext.HasErrors);
        Assert.Collection(((BlockNode)context.Root!).Children,
            (node) =>
            {
                BinaryChainNode bin1 = (BinaryChainNode)((AssignNode)node).Expression;
                Assert.Equal([BinaryChainNode.BinaryOperation.Subtract], bin1.Operations);
                BinaryChainNode bin2 = (BinaryChainNode)bin1.Arguments[0];
                Assert.Equal([BinaryChainNode.BinaryOperation.Add], bin2.Operations);
                BinaryChainNode bin3 = (BinaryChainNode)bin1.Arguments[1];
                Assert.Equal([BinaryChainNode.BinaryOperation.Multiply], bin3.Operations);
            },
            (node) =>
            {
                BinaryChainNode bin1 = (BinaryChainNode)((AssignNode)node).Expression;
                Assert.Equal([BinaryChainNode.BinaryOperation.Add, BinaryChainNode.BinaryOperation.Subtract], bin1.Operations);
            },
            (node) =>
            {
                BinaryChainNode bin1 = (BinaryChainNode)((AssignNode)node).Expression;
                Assert.Equal([BinaryChainNode.BinaryOperation.Multiply, BinaryChainNode.BinaryOperation.Divide,
                              BinaryChainNode.BinaryOperation.GMLModulo, BinaryChainNode.BinaryOperation.GMLModulo,
                              BinaryChainNode.BinaryOperation.GMLDivRemainder], 
                             bin1.Operations);
            },
            (node) =>
            {
                BinaryChainNode bin1 = (BinaryChainNode)((AssignNode)node).Expression;
                Assert.Equal([BinaryChainNode.BinaryOperation.Multiply], bin1.Operations);
                BinaryChainNode bin2 = (BinaryChainNode)bin1.Arguments[0];
                Assert.Equal([BinaryChainNode.BinaryOperation.Divide], bin2.Operations);
            },
            (node) =>
            {
                BinaryChainNode bin1 = (BinaryChainNode)((AssignNode)node).Expression;
                Assert.Equal([BinaryChainNode.BinaryOperation.Divide], bin1.Operations);
                BinaryChainNode bin2 = (BinaryChainNode)bin1.Arguments[1];
                Assert.Equal([BinaryChainNode.BinaryOperation.Multiply], bin2.Operations);
            },
            (node) =>
            {
                BinaryChainNode bin1 = (BinaryChainNode)((AssignNode)node).Expression;
                Assert.Equal([BinaryChainNode.BinaryOperation.BitwiseAnd, BinaryChainNode.BinaryOperation.BitwiseOr,
                              BinaryChainNode.BinaryOperation.BitwiseXor],
                             bin1.Operations);
            },
            (node) =>
            {
                BinaryChainNode bin1 = (BinaryChainNode)((AssignNode)node).Expression;
                Assert.Equal([BinaryChainNode.BinaryOperation.BitwiseShiftLeft, BinaryChainNode.BinaryOperation.BitwiseShiftRight],
                             bin1.Operations);
            }
        );
    }

    [Fact]
    public void TestLocalDeclaration()
    {
        ParseContext context = TestUtil.Parse(
            """
            var a, b = 123, c;
            """
        );

        Assert.Empty(context.CompileContext.Errors);
        Assert.False(context.CompileContext.HasErrors);
        Assert.Collection(((BlockNode)context.Root!).Children,
            (node) =>
            {
                LocalVarDeclNode decl = (LocalVarDeclNode)node;
                Assert.Equal(["a", "b", "c"], decl.DeclaredLocals);
                Assert.Equal(3, decl.AssignedValues.Count);
                Assert.Null(decl.AssignedValues[0]);
                Assert.Equal(123, ((NumberNode)decl.AssignedValues[1]!).Value);
                Assert.Null(decl.AssignedValues[2]);
            }
        );
    }

    [Fact]
    public void TestBranchStatements()
    {
        ParseContext context = TestUtil.Parse(
            """
            switch (a)
            {
                case 123:
                    break;
                default:
            }
            while (b) 
                c = 1;
            while b do c = 1
            for (var i = 0; i < 10; i++)
                d = 1;
            repeat (8)
                e = 1;
            with (123)
                f = 1;
            with 123 do f = 1
            do
            {
                test();
            }
            until g;
            do test(); until g;
            for (;;) {}
            """
        );

        Assert.Empty(context.CompileContext.Errors);
        Assert.False(context.CompileContext.HasErrors);
        Assert.Collection(((BlockNode)context.Root!).Children,
            (node) =>
            {
                SwitchNode switchNode = (SwitchNode)node;
                Assert.Equal("a", ((SimpleVariableNode)switchNode.Expression).VariableName);
                Assert.Collection(switchNode.Children,
                    (node) =>
                    {
                        SwitchCaseNode case1 = (SwitchCaseNode)node;
                        Assert.Equal(123, ((NumberNode)case1.Expression!).Value);
                    },
                    (node) => Assert.IsType<BreakNode>(node),
                    (node) =>
                    {
                        SwitchCaseNode case2 = (SwitchCaseNode)node;
                        Assert.Null(case2.Expression);
                    }
                );
            },
            (node) =>
            {
                WhileLoopNode whileLoopNode = (WhileLoopNode)node;
                Assert.Equal("b", ((SimpleVariableNode)whileLoopNode.Condition).VariableName);
                AssignNode assign = (AssignNode)whileLoopNode.Body;
                Assert.Equal("c", ((SimpleVariableNode)assign.Destination).VariableName);
            },
            (node) =>
            {
                WhileLoopNode whileLoopNode = (WhileLoopNode)node;
                Assert.Equal("b", ((SimpleVariableNode)whileLoopNode.Condition).VariableName);
                AssignNode assign = (AssignNode)whileLoopNode.Body;
                Assert.Equal("c", ((SimpleVariableNode)assign.Destination).VariableName);
            },
            (node) =>
            {
                ForLoopNode forLoopNode = (ForLoopNode)node;
                LocalVarDeclNode decl = (LocalVarDeclNode)forLoopNode.Initializer;
                Assert.Equal(["i"], decl.DeclaredLocals);
                Assert.Single(decl.AssignedValues);
                Assert.Equal(0, ((NumberNode)decl.AssignedValues[0]!).Value);
                BinaryChainNode condition = (BinaryChainNode)forLoopNode.Condition;
                Assert.Equal("i", ((SimpleVariableNode)condition.Arguments[0]).VariableName);
                Assert.Equal(10, ((NumberNode)condition.Arguments[1]).Value);
                Assert.Equal([BinaryChainNode.BinaryOperation.CompareLesser], condition.Operations);
                PostfixNode incrementor = (PostfixNode)forLoopNode.Incrementor;
                Assert.Equal("i", ((SimpleVariableNode)incrementor.Expression).VariableName);
                Assert.True(incrementor.IsStatement);
                AssignNode assign = (AssignNode)forLoopNode.Body;
                Assert.Equal("d", ((SimpleVariableNode)assign.Destination).VariableName);
            },
            (node) =>
            {
                RepeatLoopNode repeatLoopNode = (RepeatLoopNode)node;
                Assert.Equal(8, ((NumberNode)repeatLoopNode.TimesToRepeat).Value);
                AssignNode assign = (AssignNode)repeatLoopNode.Body;
                Assert.Equal("e", ((SimpleVariableNode)assign.Destination).VariableName);
            },
            (node) =>
            {
                WithLoopNode withLoopNode = (WithLoopNode)node;
                Assert.Equal(123, ((NumberNode)withLoopNode.Expression).Value);
                AssignNode assign = (AssignNode)withLoopNode.Body;
                Assert.Equal("f", ((SimpleVariableNode)assign.Destination).VariableName);
            },
            (node) =>
            {
                WithLoopNode withLoopNode = (WithLoopNode)node;
                Assert.Equal(123, ((NumberNode)withLoopNode.Expression).Value);
                AssignNode assign = (AssignNode)withLoopNode.Body;
                Assert.Equal("f", ((SimpleVariableNode)assign.Destination).VariableName);
            },
            (node) =>
            {
                DoUntilLoopNode doUntilLoopNode = (DoUntilLoopNode)node;
                BlockNode block = (BlockNode)doUntilLoopNode.Body;
                Assert.Equal("test", ((SimpleFunctionCallNode)block.Children[0]).FunctionName);
                Assert.Equal("g", ((SimpleVariableNode)doUntilLoopNode.Condition).VariableName);
            },
            (node) =>
            {
                DoUntilLoopNode doUntilLoopNode = (DoUntilLoopNode)node;
                Assert.Equal("test", ((SimpleFunctionCallNode)doUntilLoopNode.Body).FunctionName);
                Assert.Equal("g", ((SimpleVariableNode)doUntilLoopNode.Condition).VariableName);
            },
            (node) =>
            {
                ForLoopNode forLoopNode = (ForLoopNode)node;
                Assert.IsType<EmptyNode>(forLoopNode.Initializer);
                Assert.Equal(1, ((Int64Node)forLoopNode.Condition).Value);
                Assert.IsType<EmptyNode>(forLoopNode.Incrementor);
                Assert.Empty(((BlockNode)forLoopNode.Body).Children);
            }
        );
    }

    [Fact]
    public void TestFunctionDeclarations()
    {
        ParseContext context = TestUtil.Parse(
            """
            function test(arg1, arg2 = "default value")
            {
                show_message(arg1);
            }

            function ctor(arg1) constructor {}
            function ctor_inherited(arg1, arg2) : ctor(arg1) constructor {}

            expr = function named() {};
            expr_anon = function() {};
            """
        );

        Assert.Empty(context.CompileContext.Errors);
        Assert.False(context.CompileContext.HasErrors);
        Assert.Collection(((BlockNode)context.Root!).Children,
            (node) =>
            {
                FunctionDeclNode decl = (FunctionDeclNode)node;
                Assert.Equal("test", decl.FunctionName);
                Assert.False(decl.IsStruct);
                Assert.True(decl.IsStatement);
                Assert.False(decl.IsConstructor);
                Assert.Equal(["arg1", "arg2"], decl.ArgumentNames);
                BlockNode defaultBlock = decl.DefaultValueBlock!;
                IfNode ifCheck = (IfNode)defaultBlock.Children[0];
                BinaryChainNode binaryCheck = (BinaryChainNode)ifCheck.Condition;
                Assert.Equal("argument1", ((SimpleVariableNode)binaryCheck.Arguments[0]).VariableName);
                Assert.Equal("undefined", ((SimpleVariableNode)binaryCheck.Arguments[1]).VariableName);
                Assert.Equal([BinaryChainNode.BinaryOperation.CompareEqual], binaryCheck.Operations);
                AssignNode assign = (AssignNode)ifCheck.TrueStatement;
                Assert.Equal("argument1", ((SimpleVariableNode)assign.Destination).VariableName);
                Assert.Equal("default value", ((StringNode)assign.Expression).Value);
                Assert.Equal(AssignNode.AssignKind.Normal, assign.Kind);
                Assert.Null(ifCheck.FalseStatement);
                Assert.Equal("show_message", ((SimpleFunctionCallNode)decl.Body.Children[0]).FunctionName);
                Assert.Null(decl.InheritanceCall);
            },
            (node) =>
            {
                FunctionDeclNode decl = (FunctionDeclNode)node;
                Assert.Equal("ctor", decl.FunctionName);
                Assert.False(decl.IsStruct);
                Assert.True(decl.IsStatement);
                Assert.True(decl.IsConstructor);
                Assert.Equal(["arg1"], decl.ArgumentNames);
                Assert.Empty(decl.Body.Children);
                Assert.Null(decl.InheritanceCall);
            },
            (node) =>
            {
                FunctionDeclNode decl = (FunctionDeclNode)node;
                Assert.Equal("ctor_inherited", decl.FunctionName);
                Assert.False(decl.IsStruct);
                Assert.True(decl.IsStatement);
                Assert.True(decl.IsConstructor);
                Assert.Equal(["arg1", "arg2"], decl.ArgumentNames);
                Assert.Empty(decl.Body.Children);
                SimpleFunctionCallNode inheritance = decl.InheritanceCall!;
                Assert.Equal("ctor", inheritance.FunctionName);
                Assert.Equal("arg1", ((SimpleVariableNode)inheritance.Arguments[0]).VariableName);
            },
            (node) =>
            {
                AssignNode assign = (AssignNode)node;
                FunctionDeclNode decl = (FunctionDeclNode)assign.Expression;
                Assert.Equal("named", decl.FunctionName);
                Assert.False(decl.IsStruct);
                Assert.False(decl.IsStatement);
                Assert.False(decl.IsConstructor);
                Assert.Equal([], decl.ArgumentNames);
                Assert.Empty(decl.Body.Children);
                Assert.Null(decl.InheritanceCall);
            },
            (node) =>
            {
                AssignNode assign = (AssignNode)node;
                FunctionDeclNode decl = (FunctionDeclNode)assign.Expression;
                Assert.Null(decl.FunctionName);
                Assert.False(decl.IsStruct);
                Assert.False(decl.IsStatement);
                Assert.False(decl.IsConstructor);
                Assert.Equal([], decl.ArgumentNames);
                Assert.Empty(decl.Body.Children);
                Assert.Null(decl.InheritanceCall);
            }
        );
    }

    [Fact]
    public void TestStructs()
    {
        ParseContext context = TestUtil.Parse(
            """
            basic = { a: 123, b: 456 };
            reuse_name = { a, b };
            hoist = { a: func(123), b: [1, 2, func(456)] };
            nested =
            {
                a:
                {
                    inner: 123,
                    hoist: func(789)
                },
                b:
                {
                    c:
                    {
                        complex: func(0)
                    }
                }
            };
            """
        );

        Assert.Empty(context.CompileContext.Errors);
        Assert.False(context.CompileContext.HasErrors);
        Assert.Collection(((BlockNode)context.Root!).Children,
            (node) =>
            {
                AssignNode assign = (AssignNode)node;
                SimpleFunctionCallNode call = (SimpleFunctionCallNode)assign.Expression;
                Assert.Equal(VMConstants.NewObjectFunction, call.FunctionName);
                FunctionDeclNode structDecl1 = (FunctionDeclNode)call.Arguments[0];
                Assert.True(structDecl1.IsStruct);
                Assert.False(structDecl1.IsStatement);
                Assert.True(structDecl1.IsConstructor);
                Assert.Equal("a", ((SimpleVariableNode)((AssignNode)structDecl1.Body.Children[0]).Destination).VariableName);
                Assert.Equal(123, ((NumberNode)((AssignNode)structDecl1.Body.Children[0]).Expression).Value);
                Assert.Equal("b", ((SimpleVariableNode)((AssignNode)structDecl1.Body.Children[1]).Destination).VariableName);
                Assert.Equal(456, ((NumberNode)((AssignNode)structDecl1.Body.Children[1]).Expression).Value);
            },
            (node) =>
            {
                AssignNode assign = (AssignNode)node;
                SimpleFunctionCallNode call = (SimpleFunctionCallNode)assign.Expression;
                Assert.Equal(VMConstants.NewObjectFunction, call.FunctionName);
                FunctionDeclNode structDecl1 = (FunctionDeclNode)call.Arguments[0];
                Assert.True(structDecl1.IsStruct);
                Assert.False(structDecl1.IsStatement);
                Assert.True(structDecl1.IsConstructor);
                Assert.Equal("a", ((SimpleVariableNode)((AssignNode)structDecl1.Body.Children[0]).Destination).VariableName);
                Assert.Equal(0, ((NumberNode)((AccessorNode)((AssignNode)structDecl1.Body.Children[0]).Expression).AccessorExpression).Value);
                Assert.Equal("argument", ((SimpleVariableNode)((AccessorNode)((AssignNode)structDecl1.Body.Children[0]).Expression).Expression).VariableName);
                Assert.Equal("b", ((SimpleVariableNode)((AssignNode)structDecl1.Body.Children[1]).Destination).VariableName);
                Assert.Equal(1, ((NumberNode)((AccessorNode)((AssignNode)structDecl1.Body.Children[1]).Expression).AccessorExpression).Value);
                Assert.Equal("argument", ((SimpleVariableNode)((AccessorNode)((AssignNode)structDecl1.Body.Children[1]).Expression).Expression).VariableName);
                Assert.Equal("a", ((SimpleVariableNode)call.Arguments[1]).VariableName);
                Assert.Equal("b", ((SimpleVariableNode)call.Arguments[2]).VariableName);
            },
            (node) =>
            {
                AssignNode assign = (AssignNode)node;
                SimpleFunctionCallNode call = (SimpleFunctionCallNode)assign.Expression;
                Assert.Equal(VMConstants.NewObjectFunction, call.FunctionName);
                FunctionDeclNode structDecl1 = (FunctionDeclNode)call.Arguments[0];
                Assert.True(structDecl1.IsStruct);
                Assert.False(structDecl1.IsStatement);
                Assert.True(structDecl1.IsConstructor);
                Assert.Equal("a", ((SimpleVariableNode)((AssignNode)structDecl1.Body.Children[0]).Destination).VariableName);
                Assert.Equal(0, ((NumberNode)((AccessorNode)((AssignNode)structDecl1.Body.Children[0]).Expression).AccessorExpression).Value);
                Assert.Equal("argument", ((SimpleVariableNode)((AccessorNode)((AssignNode)structDecl1.Body.Children[0]).Expression).Expression).VariableName);
                Assert.Equal("b", ((SimpleVariableNode)((AssignNode)structDecl1.Body.Children[1]).Destination).VariableName);
                SimpleFunctionCallNode arrayCall = (SimpleFunctionCallNode)((AssignNode)structDecl1.Body.Children[1]).Expression;
                Assert.Equal(VMConstants.NewArrayFunction, arrayCall.FunctionName);
                Assert.Equal(1, ((NumberNode)arrayCall.Arguments[0]).Value);
                Assert.Equal(2, ((NumberNode)arrayCall.Arguments[1]).Value);
                Assert.Equal(1, ((NumberNode)((AccessorNode)arrayCall.Arguments[2]).AccessorExpression).Value);
                Assert.Equal("argument", ((SimpleVariableNode)((AccessorNode)arrayCall.Arguments[2]).Expression).VariableName);
                Assert.Equal("func", ((SimpleFunctionCallNode)call.Arguments[1]).FunctionName);
                Assert.Equal(123, ((NumberNode)((SimpleFunctionCallNode)call.Arguments[1]).Arguments[0]).Value);
                Assert.Equal("func", ((SimpleFunctionCallNode)call.Arguments[2]).FunctionName);
                Assert.Equal(456, ((NumberNode)((SimpleFunctionCallNode)call.Arguments[2]).Arguments[0]).Value);
            },
            (node) =>
            {
                // TODO: finish test for this
            }
        );
    }

    [Fact]
    public void TestLocalAndStaticDeclarations()
    {
        ParseContext context = TestUtil.Parse(
            """
            function test()
            {
                static a = 123;
                static b = 456, c = 789;

                var localA = 123;
                var localB = 456, localUnassigned, localC = 789;

                if (d)
                {
                    static e = 0;
                    var localE = 0;
                }
            }
            """
        );

        Assert.Empty(context.CompileContext.Errors);
        Assert.False(context.CompileContext.HasErrors);
        Assert.Single(((BlockNode)context.Root!).Children);
        FunctionDeclNode decl = (FunctionDeclNode)((BlockNode)context.Root!).Children[0];
        Assert.Equal("test", decl.FunctionName);
        Assert.True(decl.Scope.IsStaticDeclared("a"));
        Assert.True(decl.Scope.IsStaticDeclared("b"));
        Assert.True(decl.Scope.IsStaticDeclared("c"));
        Assert.False(decl.Scope.IsStaticDeclared("d"));
        Assert.True(decl.Scope.IsStaticDeclared("e"));
        Assert.True(decl.Scope.IsLocalDeclared("localA"));
        Assert.True(decl.Scope.IsLocalDeclared("localB"));
        Assert.True(decl.Scope.IsLocalDeclared("localUnassigned"));
        Assert.True(decl.Scope.IsLocalDeclared("localC"));
        Assert.False(decl.Scope.IsLocalDeclared("localD"));
        Assert.True(decl.Scope.IsLocalDeclared("localE"));
        Assert.Collection(decl.Scope.StaticInitializerBlock!.Children,
            (node) =>
            {
                AssignNode assign = (AssignNode)node;
                Assert.Equal("a", ((SimpleVariableNode)assign.Destination).VariableName);
                Assert.Equal(123, ((NumberNode)assign.Expression).Value);
            },
            (node) =>
            {
                AssignNode assign = (AssignNode)node;
                Assert.Equal("b", ((SimpleVariableNode)assign.Destination).VariableName);
                Assert.Equal(456, ((NumberNode)assign.Expression).Value);
            },
            (node) =>
            {
                AssignNode assign = (AssignNode)node;
                Assert.Equal("c", ((SimpleVariableNode)assign.Destination).VariableName);
                Assert.Equal(789, ((NumberNode)assign.Expression).Value);
            },
            (node) =>
            {
                AssignNode assign = (AssignNode)node;
                Assert.Equal("e", ((SimpleVariableNode)assign.Destination).VariableName);
                Assert.Equal(0, ((NumberNode)assign.Expression).Value);
            }
        );
    }

    [Fact]
    public void TestTryCatchFinally()
    {
        ParseContext context = TestUtil.Parse(
            """
            try a = 123;
            try { } catch (ex1) { }
            try { } finally { }
            try { } catch (ex2) { } finally { }
            """
        );

        Assert.Empty(context.CompileContext.Errors);
        Assert.False(context.CompileContext.HasErrors);
        Assert.True(context.RootScope.IsLocalDeclared("ex1"));
        Assert.True(context.RootScope.IsLocalDeclared("ex2"));
        Assert.Collection(((BlockNode)context.Root!).Children,
            (node) =>
            {
                Assert.Equal("a", ((SimpleVariableNode)((AssignNode)node).Destination).VariableName);
                Assert.Equal(123, ((NumberNode)((AssignNode)node).Expression).Value);
            },
            (node) =>
            {
                Assert.IsType<BlockNode>(((TryCatchNode)node).Try);
                Assert.IsType<BlockNode>(((TryCatchNode)node).Catch);
                Assert.Equal("ex1", ((TryCatchNode)node).CatchVariableName);
                Assert.Null(((TryCatchNode)node).Finally);
            },
            (node) =>
            {
                Assert.IsType<BlockNode>(((TryCatchNode)node).Try);
                Assert.Null(((TryCatchNode)node).Catch);
                Assert.Null(((TryCatchNode)node).CatchVariableName);
                Assert.IsType<BlockNode>(((TryCatchNode)node).Finally);
            },
            (node) =>
            {
                Assert.IsType<BlockNode>(((TryCatchNode)node).Try);
                Assert.IsType<BlockNode>(((TryCatchNode)node).Catch);
                Assert.Equal("ex2", ((TryCatchNode)node).CatchVariableName);
                Assert.IsType<BlockNode>(((TryCatchNode)node).Finally);
            }
        );
    }

    [Fact]
    public void TestThrow()
    {
        ParseContext context = TestUtil.Parse(
            """
            throw a;
            throw "test";
            """
        );

        Assert.Empty(context.CompileContext.Errors);
        Assert.False(context.CompileContext.HasErrors);
        Assert.Collection(((BlockNode)context.Root!).Children,
            (node) =>
            {
                Assert.Equal("a", ((SimpleVariableNode)((ThrowNode)node).Expression).VariableName);
            },
            (node) =>
            {
                Assert.Equal("test", ((StringNode)((ThrowNode)node).Expression).Value);
            }
        );
    }

    [Fact]
    public void TestNewObject()
    {
        ParseContext context = TestUtil.Parse(
            """
            new A(123);
            b = new A(456);
            c = new global.D(789);
            """
        );

        Assert.Empty(context.CompileContext.Errors);
        Assert.False(context.CompileContext.HasErrors);
        Assert.Collection(((BlockNode)context.Root!).Children,
            (node) =>
            {
                NewObjectNode newObject = (NewObjectNode)node;
                Assert.True(newObject.IsStatement);
                Assert.Equal("A", ((SimpleVariableNode)newObject.Expression).VariableName);
                Assert.Single(newObject.Arguments);
                Assert.Equal(123, ((NumberNode)newObject.Arguments[0]).Value);
            },
            (node) =>
            {
                AssignNode assign = (AssignNode)node;
                Assert.Equal("b", ((SimpleVariableNode)assign.Destination).VariableName);
                NewObjectNode newObject = (NewObjectNode)assign.Expression;
                Assert.False(newObject.IsStatement);
                Assert.Equal("A", ((SimpleVariableNode)newObject.Expression).VariableName);
                Assert.Single(newObject.Arguments);
                Assert.Equal(456, ((NumberNode)newObject.Arguments[0]).Value);
            },
            (node) =>
            {
                AssignNode assign = (AssignNode)node;
                Assert.Equal("c", ((SimpleVariableNode)assign.Destination).VariableName);
                NewObjectNode newObject = (NewObjectNode)assign.Expression;
                Assert.False(newObject.IsStatement);
                Assert.Equal("global", ((NumberNode)((DotVariableNode)newObject.Expression).LeftExpression).ConstantName);
                Assert.Equal("D", ((DotVariableNode)newObject.Expression).VariableName);
                Assert.Single(newObject.Arguments);
                Assert.Equal(789, ((NumberNode)newObject.Arguments[0]).Value);
            }
        );
    }

    [Fact]
    public void TestDelete()
    {
        ParseContext context = TestUtil.Parse(
            """
            delete a;
            delete a.b[0];
            """
        );

        Assert.Empty(context.CompileContext.Errors);
        Assert.False(context.CompileContext.HasErrors);
        Assert.Collection(((BlockNode)context.Root!).Children,
            (node) =>
            {
                AssignNode assign = (AssignNode)node;
                Assert.Equal("a", ((SimpleVariableNode)assign.Destination).VariableName);
                Assert.Equal("undefined", ((SimpleVariableNode)assign.Expression).VariableName);
            },
            (node) =>
            {
                AssignNode assign = (AssignNode)node;
                AccessorNode accessor = (AccessorNode)assign.Destination;
                Assert.Equal(0, ((NumberNode)accessor.AccessorExpression).Value);
                Assert.Equal("b", ((DotVariableNode)accessor.Expression).VariableName);
                Assert.Equal("a", ((SimpleVariableNode)((DotVariableNode)accessor.Expression).LeftExpression).VariableName);
                Assert.Equal("undefined", ((SimpleVariableNode)assign.Expression).VariableName);
            }
        );
    }
}