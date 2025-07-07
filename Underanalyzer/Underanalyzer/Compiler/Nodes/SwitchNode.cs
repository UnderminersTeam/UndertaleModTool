/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;
using Underanalyzer.Compiler.Bytecode;
using Underanalyzer.Compiler.Lexer;
using Underanalyzer.Compiler.Parser;
using static Underanalyzer.IGMInstruction;

namespace Underanalyzer.Compiler.Nodes;

/// <summary>
/// Represents a "switch" statement in the AST.
/// </summary>
internal sealed class SwitchNode : IASTNode
{
    /// <summary>
    /// Expression to be used for matching in the switch statement node.
    /// </summary>
    public IASTNode Expression { get; private set; }

    /// <summary>
    /// Contents of the switch statement.
    /// </summary>
    public List<IASTNode> Children { get; }

    /// <inheritdoc/>
    public IToken? NearbyToken { get; }

    private SwitchNode(IToken? token, IASTNode expression, List<IASTNode> children)
    {
        NearbyToken = token;
        Expression = expression;
        Children = children;
    }

    /// <summary>
    /// Creates a switch statement node, parsing from the given context's current position.
    /// </summary>
    public static SwitchNode? Parse(ParseContext context)
    {
        // Parse "switch" keyword
        if (context.EnsureToken(KeywordKind.Switch) is not TokenKeyword tokenKeyword)
        {
            return null;
        }

        // Parse expression being matched
        if (Expressions.ParseExpression(context) is not IASTNode expression)
        {
            return null;
        }
        
        // Parse main block
        List<IASTNode> children = new(32);
        context.EnsureToken(SeparatorKind.BlockOpen, KeywordKind.Begin);
        context.SkipSemicolons();
        while (!context.EndOfCode && !context.IsCurrentToken(SeparatorKind.BlockClose, KeywordKind.End))
        {
            // Parse statements, but particularly "case" and "default" labels
            IToken currentToken = context.Tokens[context.Position];
            if (currentToken is TokenKeyword { Kind: KeywordKind.Case } tokenCase)
            {
                // "case" label: parse expression
                context.Position++;
                if (Expressions.ParseExpression(context) is IASTNode caseExpr)
                {
                    SwitchCaseNode caseNode = new(tokenCase, caseExpr);
                    context.EnsureToken(SeparatorKind.Colon);
                    children.Add(caseNode);
                }
            }
            else if (currentToken is TokenKeyword { Kind: KeywordKind.Default } tokenDefault)
            {
                // "default" label: no expression to parse
                context.Position++;
                SwitchCaseNode defaultNode = new(tokenDefault, null);
                context.EnsureToken(SeparatorKind.Colon);
                children.Add(defaultNode);
            }
            else if (Statements.ParseStatement(context) is IASTNode statement)
            {
                // Regular statement
                children.Add(statement);
            }
            else
            {
                // Failed to parse statement; stop parsing this block.
                break;
            }
            context.SkipSemicolons();
        }
        context.EnsureToken(SeparatorKind.BlockClose, KeywordKind.End);

        // Create final statement
        return new SwitchNode(tokenKeyword, expression, children);
    }

    /// <inheritdoc/>
    public IASTNode PostProcess(ParseContext context)
    {
        // Enter switch context
        bool previousProcessingSwitch = context.ProcessingSwitch;
        context.ProcessingSwitch = true;

        // Normal post-processing
        Expression = Expression.PostProcess(context);
        if (Children.Count == 0)
        {
            context.CompileContext.PushError("Switch statement is empty", NearbyToken);
        }
        else
        {
            if (Children[0] is not SwitchCaseNode)
            {
                context.CompileContext.PushError("Switch statement body must begin with \"case\" or \"default\"", NearbyToken);
            }
            for (int i = 0; i < Children.Count; i++)
            {
                Children[i] = Children[i].PostProcess(context);
            }

            // TODO: possibly check for duplicate cases here
        }

        // Exit switch context
        context.ProcessingSwitch = previousProcessingSwitch;

        return this;
    }

    /// <inheritdoc/>
    public IASTNode Duplicate(ParseContext context)
    {
        List<IASTNode> newChildren = new(Children);
        for (int i = 0; i < newChildren.Count; i++)
        {
            newChildren[i] = newChildren[i].Duplicate(context);
        }
        return new SwitchNode(NearbyToken, Expression.Duplicate(context), newChildren);
    }

    // Helper struct for generating switch case code
    private readonly record struct SwitchCase(MultiForwardBranchPatch Branch, int ChildIndex);

    /// <inheritdoc/>
    public void GenerateCode(BytecodeContext context)
    {
        // Expression being switched upon
        Expression.GenerateCode(context);
        DataType expressionType = context.PopDataType();

        // Store current last array owner ID
        long lastArrayOwnerID = context.LastArrayOwnerID;
        bool arrayOwnerChanged = false;

        // Generate comparison and branch logic for all cases
        MultiForwardBranchPatch tailPatch = new();
        MultiForwardBranchPatch continuePatch = new();
        MultiForwardBranchPatch defaultPatch = new();
        bool defaultCaseExists = false;
        List<SwitchCase> cases = new(16);
        for (int i = 0; i < Children.Count; i++)
        {
            if (Children[i] is SwitchCaseNode caseNode)
            {
                if (caseNode.Expression is IASTNode caseExpression)
                {
                    // Set array owner to initial one
                    context.LastArrayOwnerID = lastArrayOwnerID;

                    // Normal case. Duplicate original expression, and compare to this expression
                    context.Emit(Opcode.Duplicate, expressionType);
                    caseExpression.GenerateCode(context);
                    context.Emit(Opcode.Compare, ComparisonType.EqualTo, context.PopDataType(), expressionType);

                    // Branch to actual case code
                    MultiForwardBranchPatch casePatch = new();
                    casePatch.AddInstruction(context, context.Emit(Opcode.BranchTrue));
                    cases.Add(new SwitchCase(casePatch, i));

                    // If array owner ID has changed, keep track of it
                    arrayOwnerChanged |= (context.LastArrayOwnerID != lastArrayOwnerID);
                }
                else
                {
                    // Default case
                    defaultCaseExists = true;
                    cases.Add(new SwitchCase(defaultPatch, i));
                }
            }
        }

        // At the end of the case branches, emit potential default branch, as well as general skip branch
        if (defaultCaseExists)
        {
            defaultPatch.AddInstruction(context, context.Emit(Opcode.Branch));
        }
        tailPatch.AddInstruction(context, context.Emit(Opcode.Branch));

        // Enter switch context, and generate actual body code
        context.PushControlFlowContext(new SwitchContext(expressionType, tailPatch, continuePatch));
        for (int i = 0; i < cases.Count; i++)
        {
            // Get range of children nodes to generate code for
            SwitchCase currentCase = cases[i];
            int startIndex = currentCase.ChildIndex + 1;
            int endIndexExclusive = (i + 1 < cases.Count) ? cases[i + 1].ChildIndex : Children.Count;

            // Set array owner to initial one
            context.LastArrayOwnerID = lastArrayOwnerID;

            // Generate code for case
            currentCase.Branch.Patch(context);
            for (int j = startIndex; j < endIndexExclusive; j++)
            {
                Children[j].GenerateCode(context);
            }

            // If array owner ID has changed, keep track of it
            arrayOwnerChanged |= (context.LastArrayOwnerID != lastArrayOwnerID);
        }
        context.PopControlFlowContext();

        // Generate continue block, if used (applies to a surrounding loop - only if one exists)
        if (continuePatch.Used)
        {
            // Branch to skip past this block if not taking continue path
            tailPatch.AddInstruction(context, context.Emit(Opcode.Branch));

            // Clean up stack, and branch to surrounding loop's continue destination
            continuePatch.Patch(context);
            context.Emit(Opcode.PopDelete, expressionType);
            context.GetTopControlFlowContext().UseContinue(context, context.Emit(Opcode.Branch));
        }

        // Tail of statement, and clean up expression from stack
        tailPatch.Patch(context);
        context.Emit(Opcode.PopDelete, expressionType);

        // Reset array owner ID if it changed
        if (arrayOwnerChanged)
        {
            context.LastArrayOwnerID = -1;
        }
    }

    /// <inheritdoc/>
    public IEnumerable<IASTNode> EnumerateChildren()
    {
        yield return Expression;
        foreach (IASTNode child in Children)
        {
            yield return child;
        }
    }
}
