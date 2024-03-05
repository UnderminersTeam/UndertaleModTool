using System;
using System.Linq;
using UndertaleModLib.Models;

namespace UndertaleModLib.Decompiler;

public static partial class Decompiler
{
    public class LoopHLStatement : HLStatement
    {
        // Loop block
        public BlockHLStatement Block;

        // While / for condition
        public Statement Condition;

        // While loop
        public bool IsWhileLoop { get => !IsForLoop && !IsRepeatLoop && !IsDoUntilLoop; }
        public bool IsDoUntilLoop;

        // For loop
        public bool IsForLoop { get => InitializeStatement != null && StepStatement != null && Condition != null; }
        public AssignmentStatement InitializeStatement;
        public AssignmentStatement StepStatement;

        // Repeat loop
        public bool IsRepeatLoop { get => RepeatStartValue != null; }
        public Statement RepeatStartValue;

        public override Statement CleanStatement(DecompileContext context, BlockHLStatement block)
        {
            int myIndex = block.Statements.IndexOf(this);
            Block = Block?.CleanBlockStatement(context);
            Condition = Condition?.CleanStatement(context, block);
            InitializeStatement = InitializeStatement?.CleanStatement(context, block) as AssignmentStatement;
            StepStatement = StepStatement?.CleanStatement(context, block) as AssignmentStatement;
            RepeatStartValue = RepeatStartValue?.CleanStatement(context, block);

            if (IsWhileLoop)
            {
                // While loops have conditions.
                if (Block.Statements.Count == 1)
                {
                    Statement firstStatement = Block.Statements[0];

                    if (firstStatement is IfHLStatement)
                    {
                        IfHLStatement ifStatement = (IfHLStatement)firstStatement;
                        if (ifStatement.falseBlock is BlockHLStatement statement && statement.Statements.Count == 0 && !ifStatement.HasElseIf)
                        {
                            Condition = ifStatement.condition;
                            Block.Statements.Remove(firstStatement); // Remove if statement.
                            Block.Statements.InsertRange(0, ifStatement.trueBlock.Statements); // Add if contents.
                        }
                    }
                }

                // If it's been shoved into an if else, take it out of there.
                //TODO: This is disabled until further notice. The problem is that for loops that do this can use continues. The issue there is we don't have an easy way to remove the increment from the decompile at the moment.
                /*if (Block.Statements.Count > 0 && Block.Statements.Last() is IfHLStatement)
                    {
                        IfHLStatement ifStatement = Block.Statements.Last() as IfHLStatement;
                        BlockHLStatement blockStatement = ifStatement.falseBlock as BlockHLStatement;

                        if (blockStatement != null && blockStatement.Statements.Count > 0 && blockStatement.Statements.Last() is ContinueHLStatement)
                        {
                            blockStatement.Statements.Remove(blockStatement.Statements.Last());
                            Block.Statements.AddRange(blockStatement.Statements);
                            ifStatement.falseBlock.Statements.Clear();
                        }
                    }*/

                // Remove redundant continues at the end of the loop.
                if (Block.Statements.Count > 0)
                {
                    Statement lastStatement = Block.Statements.Last();
                    if (lastStatement is ContinueHLStatement)
                        Block.Statements.Remove(lastStatement);
                }

                // Convert into a for loop.
                if (myIndex > 0 && block.Statements[myIndex - 1] is AssignmentStatement assignment
                                && Block.Statements.Count > 0 && Block.Statements.Last() is AssignmentStatement increment
                                && Condition is ExpressionCompare compare)
                {
                    UndertaleVariable variable = assignment.Destination.Var;

                    if (((compare.Argument1 is ExpressionVar exprVar && (exprVar.Var == variable)) || (compare.Argument2 is ExpressionVar exprVar2 && (exprVar2.Var == variable))) && increment.Destination.Var == variable)
                    {
                        block.Statements.Remove(assignment);
                        InitializeStatement = assignment;
                        Block.Statements.Remove(increment);
                        StepStatement = increment;
                    }
                }

                if (Condition == null)
                {
                    if (Block.Statements.Last() is IfHLStatement)
                    {
                        IfHLStatement ifStatement = Block.Statements.Last() as IfHLStatement;
                        if (ifStatement.trueBlock.Statements.Count == 0 && ifStatement.falseBlock.Statements.Count == 1 && ifStatement.falseBlock.Statements[0] is ContinueHLStatement && ifStatement.elseConditions.Count == 0)
                        {
                            IsDoUntilLoop = true;
                            Condition = ifStatement.condition;
                            Block.Statements.Remove(ifStatement);
                        }
                    }
                }
            }

            return this;
        }

        public override string ToString(DecompileContext context)
        {
            if (IsRepeatLoop)
            {
                bool needsParen = RepeatStartValue is ExpressionConstant || RepeatStartValue is ExpressionCompare;
                return "repeat " + (needsParen ? "(" : "") + RepeatStartValue.ToString(context) + (needsParen ? ")" : "") + "\n" + context.Indentation + Block.ToString(context);
            }

            if (IsForLoop)
            {
                string conditionStr = Condition.ToString(context); // Cut off parenthesis for the condition.
                if (conditionStr.StartsWith("(", StringComparison.InvariantCulture) && conditionStr.EndsWith(")", StringComparison.InvariantCulture))
                    conditionStr = conditionStr.Substring(1, conditionStr.Length - 2);

                return "for (" + InitializeStatement.ToString(context) + "; " + conditionStr + "; " + StepStatement.ToString(context) + ")\n" + context.Indentation + Block.ToString(context);
            }

            string cond;
            if (Condition is ExpressionCompare)
                cond = (Condition as ExpressionCompare).ToStringWithParen(context);
            else if (IsDoUntilLoop)
                cond = Condition?.ToString(context) ?? "(false)";
            else
                cond = Condition != null ? Condition.ToString(context) : "(true)";

            if (IsDoUntilLoop)
                return "do\n" + context.Indentation + Block.ToString(context, false) + " until " + cond + ";";

            return "while " + cond + "\n" + context.Indentation + Block.ToString(context);
        }

        internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
        {
            RepeatStartValue?.DoTypePropagation(context, AssetIDType.Other);
            Condition?.DoTypePropagation(context, AssetIDType.Other);
            InitializeStatement?.DoTypePropagation(context, AssetIDType.Other);
            StepStatement?.DoTypePropagation(context, AssetIDType.Other);
            return Block.DoTypePropagation(context, AssetIDType.Other);
        }
    };
}