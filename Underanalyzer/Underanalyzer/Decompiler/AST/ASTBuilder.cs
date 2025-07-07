/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;
using Underanalyzer.Decompiler.ControlFlow;

namespace Underanalyzer.Decompiler.AST;

/// <summary>
/// Manages the building of a high-level AST from control flow nodes.
/// </summary>
public class ASTBuilder(DecompileContext context)
{
    /// <summary>
    /// The corresponding code context for this AST builder.
    /// </summary>
    public DecompileContext Context { get; } = context;

    /// <summary>
    /// Reusable expression stack for instruction simulation. When non-empty after building a control flow node,
    /// usually signifies data that needs to get processed by the following control flow node.
    /// </summary>
    internal Stack<IExpressionNode> ExpressionStack { get => TopFragmentContext!.ExpressionStack; }

    /// <summary>
    /// The index to start processing instructions for the next ControlFlow.Block we encounter.
    /// Used by code fragments to skip processing instructions twice.
    /// </summary>
    internal int StartBlockInstructionIndex { get; set; } = 0;

    /// <summary>
    /// List of arguments passed into a struct fragment.
    /// </summary>
    internal List<IExpressionNode>? StructArguments { get => TopFragmentContext!.StructArguments; set => TopFragmentContext!.StructArguments = value; }

    /// <summary>
    /// Set of all local variables present in the current fragment.
    /// </summary>
    internal HashSet<string> LocalVariableNames { get => TopFragmentContext!.LocalVariableNames; }

    /// <summary>
    /// Set of all local variables present in the current fragment.
    /// </summary>
    internal List<string> LocalVariableNamesList { get => TopFragmentContext!.LocalVariableNamesList; }

    /// <summary>
    /// The stack used to manage fragment contexts.
    /// </summary>
    private Stack<ASTFragmentContext> FragmentContextStack { get; } = new();

    /// <summary>
    /// The current/top fragment context.
    /// </summary>
    internal ASTFragmentContext? TopFragmentContext { get; private set; }

    /// <summary>
    /// Current queue of switch case expressions.
    /// </summary>
    internal Queue<IExpressionNode>? SwitchCases { get; set; } = null;

    /// <summary>
    /// Builds the AST for an entire code entry, starting from the root fragment node.
    /// </summary>
    public IStatementNode Build()
    {
        List<IStatementNode> output = new(1);
        PushFragmentContext(Context.FragmentNodes![0]);
        Context.FragmentNodes[0].BuildAST(this, output);
        PopFragmentContext();
        return output[0];
    }

    /// <summary>
    /// Returns the control flow node following the given control flow node, in the current block.
    /// </summary>
    private static IControlFlowNode? Follow(IControlFlowNode node)
    {
        // Ensure we follow a linear path
        if (node.Successors.Count > 1)
        {
            throw new DecompilerException("Unexpected branch when building AST");
        }

        if (node.Successors.Count == 1)
        {
            // Ensure we're not jumping backwards (looping)
            if (node.Successors[0] == node ||
                node.Successors[0].StartAddress < node.StartAddress)
            {
                throw new DecompilerException("Unresolved loop when building AST");
            }
            
            // Follow sole successor
            return node.Successors[0];
        }
        
        // We have no more successors to follow
        return null;
    }

    /// <summary>
    /// Builds a block starting from a control flow node, following all of its successors linearly.
    /// </summary>
    internal BlockNode BuildBlock(IControlFlowNode? startNode)
    {
        BlockNode block = new(TopFragmentContext ?? throw new System.NullReferenceException());

        // Advance through all successors, building out this block
        var currentNode = startNode;
        while (currentNode is not null)
        {
            currentNode.BuildAST(this, block.Children);
            currentNode = Follow(currentNode);
        }

        // If this block has more than 1 child, make sure it has curly braces around it
        if (block.Children.Count > 1)
        {
            block.UseBraces = true;
        }

        return block;
    }

    /// <summary>
    /// Builds a block starting from a control flow node, following all of its successors linearly,
    /// before stopping at the for loop incrementor of a WhileLoop control flow node.
    /// </summary>
    internal BlockNode BuildBlockWhile(IControlFlowNode? startNode, WhileLoop whileLoop)
    {
        BlockNode block = new(TopFragmentContext!);

        // Advance through all successors, building out this block
        var currentNode = startNode;
        while (currentNode is not null && currentNode != whileLoop.ForLoopIncrementor)
        {
            currentNode.BuildAST(this, block.Children);
            currentNode = Follow(currentNode);
        }

        // If this block has more than 1 child, make sure it has curly braces around it
        if (block.Children.Count > 1)
        {
            block.UseBraces = true;
        }

        return block;
    }

    // List used for output of expressions, which should never have any statements
    private readonly List<IStatementNode> expressionOutput = [];

    /// <summary>
    /// Builds an expression (of unknown type) starting from a control flow node, 
    /// following all of its successors linearly.
    /// </summary>
    internal IExpressionNode BuildExpression(IControlFlowNode? startNode, List<IStatementNode>? output = null)
    {
        output ??= expressionOutput;
        int stackCountBefore = ExpressionStack.Count;

        // Advance through all successors, building expression
        var currentNode = startNode;
        while (currentNode is not null)
        {
            currentNode.BuildAST(this, output);
            currentNode = Follow(currentNode);
        }

        int stackCountAfter = ExpressionStack.Count;

        // Ensure we didn't produce any statements while evaluating expression
        if (expressionOutput.Count > 0)
        {
            throw new DecompilerException("Unexpected statement found while evaluating expression");
        }

        // Ensure we added exactly 1 expression to the stack while evaluating this expression (if desired)
        if (stackCountAfter != stackCountBefore + 1)
        {
            throw new DecompilerException(
                $"Unexpected change of stack count from {stackCountBefore} to " +
                $"{stackCountAfter} while evaluating expression");
        }

        // Return the expression that was evaluated
        return ExpressionStack.Pop();
    }

    /// <summary>
    /// Builds arbitrary expression AST starting from a control flow node, following all of its successors linearly.
    /// No statements can be created in this context, and at most a defined number of expressions can be created,
    /// or -1 if any number of expressions can be created.
    /// </summary>
    internal void BuildArbitrary(IControlFlowNode? startNode, List<IStatementNode>? output = null, int numAllowedExpressions = 0)
    {
        output ??= expressionOutput;
        int stackCountBefore = ExpressionStack.Count;

        // Advance through all successors, building expression
        var currentNode = startNode;
        while (currentNode is not null)
        {
            currentNode.BuildAST(this, output);
            currentNode = Follow(currentNode);
        }

        int stackCountAfter = ExpressionStack.Count;

        // Ensure we didn't produce any statements while evaluating (if desired)
        if (expressionOutput.Count > 0)
        {
            throw new DecompilerException("Unexpected statement found while evaluating arbitrary AST");
        }

        // Ensure we didn't add too many expressions to the stack
        if (numAllowedExpressions != -1 && stackCountAfter > (stackCountBefore + numAllowedExpressions))
        {
            throw new DecompilerException(
                $"Unexpected change of stack count from {stackCountBefore} to " +
                $"{stackCountAfter} while evaluating arbitrary AST");
        }
    }

    /// <summary>
    /// Pushes a new fragment onto the fragment context stack.
    /// Each fragment has its own expression stack, struct argument list, etc.
    /// </summary>
    internal void PushFragmentContext(Fragment fragment)
    {
        ASTFragmentContext context = new(fragment);
        TopFragmentContext?.Children.Add(context);
        FragmentContextStack.Push(context);
        TopFragmentContext = context;
    }

    /// <summary>
    /// Pops a fragment off of the fragment context stack.
    /// </summary>
    internal void PopFragmentContext()
    {
        ASTFragmentContext context = FragmentContextStack.Pop();
        if (context.ExpressionStack.Count > 0)
        {
            if (Context.Settings.AllowLeftoverDataOnStack)
            {
                // We have leftover data on stack; this is seemingly invalid code that can't be accurately recompiled.
                // Create a new warning for this fragment.
                Context.Warnings.Add(new DecompileDataLeftoverWarning(context.ExpressionStack.Count, context.CodeEntryName ?? "<unknown code entry>"));
            }
            else
            {
                throw new DecompilerException("Data left over on VM stack at end of fragment.");
            }
        }

        // Add sub-function names to lookup, if any exist
        foreach (ASTFragmentContext child in context.Children)
        {
            if (child.FunctionName is not null)
            {
                context.SubFunctionNames[child.CodeEntryName ?? throw new DecompilerException("Missing code entry name")] = child.FunctionName;
            }
        }

        // Update new top
        if (FragmentContextStack.Count > 0)
        {
            TopFragmentContext = FragmentContextStack.Peek();
        }
        else
        {
            TopFragmentContext = null;
        }
    }
}
