/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;
using Underanalyzer.Compiler.Bytecode;
using Underanalyzer.Compiler.Lexer;
using Underanalyzer.Compiler.Parser;

namespace Underanalyzer.Compiler.Nodes;

/// <summary>
/// Represents a block of code in the AST.
/// </summary>
internal sealed class BlockNode : IASTNode
{
    /// <summary>
    /// List of statements belonging to this block node.
    /// </summary>
    public List<IASTNode> Children { get; }

    /// <inheritdoc/>
    public IToken? NearbyToken { get; set; }

    private BlockNode(List<IASTNode> children, IToken? nearbyToken)
    {
        Children = children;
        NearbyToken = nearbyToken;
    }

    /// <summary>
    /// Creates a new block node, parsing statements as the root block of the code entry.
    /// </summary>
    public static BlockNode ParseRoot(ParseContext context)
    {
        // Parse statements
        List<IASTNode> children = new(32);
        context.SkipSemicolons();
        while (!context.EndOfCode)
        {
            if (Statements.ParseStatement(context) is IASTNode node)
            {
                children.Add(node);
            }
            else
            {
                // Failed to parse statement; stop parsing this block.
                break;
            }
            context.SkipSemicolons();
        }

        // Get nearby token from first child statement
        IToken? nearbyToken = null;
        if (children.Count > 0)
        {
            nearbyToken = children[0].NearbyToken;
        }

        // Create final block node
        return new BlockNode(children, nearbyToken);
    }

    /// <summary>
    /// Creates a new block node, parsing statements as a regular block, which expects opening/closing braces.
    /// </summary>
    public static BlockNode ParseRegular(ParseContext context)
    {
        // Attempt to parse opening
        IToken? tokenOpen = context.EnsureToken(SeparatorKind.BlockOpen, KeywordKind.Begin);

        // Parse statements
        List<IASTNode> children = new(32);
        context.SkipSemicolons();
        while (!context.EndOfCode && !context.IsCurrentToken(SeparatorKind.BlockClose, KeywordKind.End))
        {
            if (Statements.ParseStatement(context) is IASTNode node)
            {
                children.Add(node);
            }
            else
            {
                // Failed to parse statement; stop parsing this block.
                break;
            }
            context.SkipSemicolons();
        }

        // Attempt to parse closing
        context.EnsureToken(SeparatorKind.BlockClose, KeywordKind.End);

        // Create final block node
        return new BlockNode(children, tokenOpen);
    }

    /// <summary>
    /// Creates an empty block node, given a nearby token.
    /// </summary>
    public static BlockNode CreateEmpty(IToken? nearbyToken, int capacity = 4)
    {
        return new BlockNode(new(capacity), nearbyToken);
    }

    /// <inheritdoc/>
    public IASTNode PostProcess(ParseContext context)
    {
        for (int i = 0; i < Children.Count; i++)
        {
            Children[i] = Children[i].PostProcess(context);
        }
        return this;
    }

    /// <summary>
    /// Post-processes this block, only modifying children, rather than potentially returning a new instance.
    /// </summary>
    public void PostProcessChildrenOnly(ParseContext context)
    {
        for (int i = 0; i < Children.Count; i++)
        {
            Children[i] = Children[i].PostProcess(context);
        }
    }

    /// <inheritdoc/>
    public IASTNode Duplicate(ParseContext context)
    {
        List<IASTNode> newChildren = new(Children);
        for (int i = 0; i < newChildren.Count; i++)
        {
            newChildren[i] = newChildren[i].Duplicate(context);
        }
        return new BlockNode(newChildren, NearbyToken);
    }

    /// <inheritdoc/>
    public void GenerateCode(BytecodeContext context)
    {
        foreach (IASTNode statement in Children)
        {
            statement.GenerateCode(context);
        }
    }

    /// <summary>
    /// Same as <see cref="GenerateCode(BytecodeContext)"/>, but for static initializer blocks.
    /// </summary>
    public void GenerateStaticCode(BytecodeContext context)
    {
        context.CurrentScope.GeneratingStaticBlock = true;
        foreach (IASTNode statement in Children)
        {
            if (statement is AssignNode { Destination: SimpleVariableNode { VariableName: string staticName } } assign)
            {
                // Set new static name, used to assign to function entries
                context.CurrentScope.StaticVariableName = staticName;
                assign.GenerateCode(context);
                context.CurrentScope.StaticVariableName = null;
            }
            else
            {
                statement.GenerateCode(context);
            }
        }
        context.CurrentScope.GeneratingStaticBlock = false;
    }

    /// <inheritdoc/>
    public IEnumerable<IASTNode> EnumerateChildren()
    {
        foreach (IASTNode child in Children)
        {
            yield return child;
        }
    }
}
