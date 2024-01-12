using System;
using System.Collections.Generic;
using System.Text;
using UndertaleModLib.Models;

namespace UndertaleModLib.Decompiler;

public static partial class Decompiler
{
    public class IfHLStatement : HLStatement
    {
        public Expression condition;
        public BlockHLStatement trueBlock;
        public List<ValueTuple<Expression, BlockHLStatement>> elseConditions = new List<ValueTuple<Expression, BlockHLStatement>>();
        public BlockHLStatement falseBlock;

        public bool HasElseIf { get => elseConditions != null && elseConditions.Count > 0; }
        public bool HasElse { get => falseBlock != null && falseBlock.Statements.Count > 0; }

        public override Statement CleanStatement(DecompileContext context, BlockHLStatement block)
        {
            condition = condition?.CleanExpression(context, block);
            trueBlock = trueBlock?.CleanBlockStatement(context);
            falseBlock = falseBlock?.CleanBlockStatement(context);

            int myIndex = block.Statements.IndexOf(this);

            // Prevents if (tempvar - 1), when it should be if (tempvar)
            if ((condition as ExpressionCast)?.Argument is ExpressionTwo && myIndex > 0)
            {
                ExpressionTwo conditionExpression = ((ExpressionCast)condition).Argument as ExpressionTwo;
                Statement lastStatement = block.Statements[myIndex - 1];

                if (conditionExpression.Argument1 is ExpressionTempVar tempVar && lastStatement is TempVarAssignmentStatement statement && conditionExpression.Argument2 is ExpressionConstant
                    && tempVar.Var.Var == statement.Var.Var)
                    condition = conditionExpression.Argument1;
            }

            // Use if -> else if, instead of nesting ifs.
            while (falseBlock.Statements.Count == 1 && falseBlock.Statements[0] is IfHLStatement nestedIf) // The condition of one if statement.
            {
                elseConditions.Add(new ValueTuple<Expression, BlockHLStatement>(nestedIf.condition, nestedIf.trueBlock));
                elseConditions.AddRange(nestedIf.elseConditions);
                falseBlock = nestedIf.falseBlock;
            }

            // Collapse conditions into && + || + ternary.
            if (HasElse && !HasElseIf && trueBlock.Statements.Count == 1 && falseBlock.Statements.Count == 1)
            {
                TempVarAssignmentStatement trueAssign = trueBlock.Statements[0] as TempVarAssignmentStatement;
                TempVarAssignmentStatement falseAssign = falseBlock.Statements[0] as TempVarAssignmentStatement;

                if (trueAssign != null && falseAssign != null && trueAssign.Var.Var == falseAssign.Var.Var)
                {
                    TempVarAssignmentStatement newAssign;
                    if (TestNumber(trueAssign.Value, 1) && (falseAssign.Var.Var.Type == UndertaleInstruction.DataType.Boolean || falseAssign.Value.Type == UndertaleInstruction.DataType.Boolean))
                        newAssign = new TempVarAssignmentStatement(trueAssign.Var, new ExpressionTwoSymbol("||", UndertaleInstruction.DataType.Boolean, condition, falseAssign.Value));
                    else if (TestNumber(falseAssign.Value, 0) && (trueAssign.Var.Var.Type == UndertaleInstruction.DataType.Boolean || trueAssign.Value.Type == UndertaleInstruction.DataType.Boolean))
                        newAssign = new TempVarAssignmentStatement(trueAssign.Var, new ExpressionTwoSymbol("&&", UndertaleInstruction.DataType.Boolean, condition, trueAssign.Value));
                    else
                        newAssign = new TempVarAssignmentStatement(trueAssign.Var, new ExpressionTernary(trueAssign.Value.Type, condition, trueAssign.Value, falseAssign.Value));

                    context.TempVarMap[newAssign.Var.Var.Name] = newAssign;
                    return newAssign;
                }
            }

            // Create repeat loops.
            if (HasElse && !HasElseIf && trueBlock.Statements.Count == 0 && falseBlock.Statements.Count == 1 && falseBlock.Statements[0] is LoopHLStatement
                && condition is ExpressionCompare && myIndex > 0 && block.Statements[myIndex - 1] is TempVarAssignmentStatement)
            {
                ExpressionCompare compareCondition = condition as ExpressionCompare;
                LoopHLStatement loop = falseBlock.Statements[0] as LoopHLStatement;
                TempVarAssignmentStatement priorAssignment = block.Statements[myIndex - 1] as TempVarAssignmentStatement;
                Expression startValue = priorAssignment.Value;

                List<Statement> loopCode = loop.Block.Statements;
                if (priorAssignment != null &&
                    loop.IsWhileLoop &&
                    loop.Condition == null &&
                    //loopCode.Count > 2 &&
                    compareCondition.Opcode == UndertaleInstruction.ComparisonType.LTE &&
                    TestNumber(compareCondition.Argument2, 0) &&
                    compareCondition.Argument1.ToString(context) == startValue.ToString(context))
                {
                    TempVarAssignmentStatement repeatAssignment = null;
                    IfHLStatement loopCheckStatement = null;
                    bool hasBreak = false;
                    List<Statement> insideElseBlock = null;

                    if (loopCode.Count > 2)
                    {
                        repeatAssignment = loopCode[loopCode.Count - 2] as TempVarAssignmentStatement;
                        loopCheckStatement = loopCode[loopCode.Count - 1] as IfHLStatement;
                    }

                    if ((repeatAssignment == null || loopCheckStatement == null) &&
                        loopCode[loopCode.Count - 1] is IfHLStatement wrapperIfStatement &&
                        wrapperIfStatement.HasElse
                       ) // single-level break detection
                    {
                        insideElseBlock = wrapperIfStatement.falseBlock.Statements;
                        wrapperIfStatement.trueBlock.Statements.Add(new BreakHLStatement());
                        if (insideElseBlock.Count > 2)
                        {
                            repeatAssignment = insideElseBlock[insideElseBlock.Count - 2] as TempVarAssignmentStatement;
                            loopCheckStatement = insideElseBlock[insideElseBlock.Count - 1] as IfHLStatement;
                            hasBreak = true;
                        }
                    }

                    if (repeatAssignment != null && loopCheckStatement != null)
                    { // tempVar = (tempVar -1); -> if (tempVar) continue -> break

                        // if (tempVar) {continue} else {empty}

                        if (loopCheckStatement.trueBlock.Statements.Count == 1
                            && !loopCheckStatement.HasElse
                            && !loopCheckStatement.HasElseIf
                            && loopCheckStatement.trueBlock.Statements[0] is ContinueHLStatement
                            && loopCheckStatement.condition.ToString(context) == repeatAssignment.Value.ToString(context))
                        {
                            (hasBreak ? insideElseBlock : loopCode).Remove(repeatAssignment);
                            (hasBreak ? insideElseBlock : loopCode).Remove(loopCheckStatement);
                            block.Statements.Remove(priorAssignment);

                            loop.RepeatStartValue = startValue;
                            return loop;
                        }
                    }
                }
            }

            for (int i = 0; i < elseConditions.Count; i++)
            {
                var pair = elseConditions[i];
                pair.Item1 = pair.Item1?.CleanExpression(context, block);
                pair.Item2 = pair.Item2?.CleanBlockStatement(context);
            }


            return this;
        }

        public override string ToString(DecompileContext context)
        {
            StringBuilder sb = new StringBuilder();
            string cond;
            if (condition is ExpressionCompare)
                cond = (condition as ExpressionCompare).ToStringWithParen(context);
            else
                cond = condition.ToString(context);
            sb.Append("if " + cond + "\n");
            sb.Append(context.Indentation + trueBlock.ToString(context));

            foreach (ValueTuple<Expression, BlockHLStatement> tuple in elseConditions)
            {
                if (tuple.Item1 is ExpressionCompare)
                    cond = (tuple.Item1 as ExpressionCompare).ToStringWithParen(context);
                else
                    cond = tuple.Item1.ToString(context);
                sb.Append("\n" + context.Indentation + "else if " + cond + "\n");
                sb.Append(context.Indentation + tuple.Item2.ToString(context));
            }

            if (HasElse)
            {
                sb.Append("\n" + context.Indentation + "else\n");
                sb.Append(context.Indentation + falseBlock.ToString(context));
            }
            return sb.ToString();
        }
    };
}