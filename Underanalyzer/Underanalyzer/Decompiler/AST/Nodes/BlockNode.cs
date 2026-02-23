/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;
using System.Reflection;

namespace Underanalyzer.Decompiler.AST;

/// <summary>
/// Represents a single block of code in the AST.
/// Blocks can have an arbitrary number of child nodes.
/// </summary>
public class BlockNode(ASTFragmentContext fragmentContext) : IFragmentNode, IBlockCleanupNode
{
    /// <summary>
    /// Whether or not curly braces are required for this block.
    /// </summary>
    public bool UseBraces { get; set; } = true;

    /// <summary>
    /// Whether this block is the block of a switch statement.
    /// </summary>
    public bool PartOfSwitch { get; set; } = false;

    /// <summary>
    /// All children contained within this block.
    /// </summary>
    public List<IStatementNode> Children { get; internal set; } = [];

    /// <inheritdoc/>
    public bool SemicolonAfter { get => false; }

    /// <inheritdoc/>
    public bool EmptyLineBefore { get => false; set => _ = value; }

    /// <inheritdoc/>
    public bool EmptyLineAfter { get => false; set => _ = value; }

    /// <inheritdoc/>
    public ASTFragmentContext FragmentContext { get; } = fragmentContext;

    /// <inheritdoc/>
    public int BlockClean(ASTCleaner cleaner, BlockNode block, int i)
    {
        // Remove this block if empty
        if (Children.Count == 0)
        {
            block.Children.RemoveAt(i);
            return i - 1;
        }
        return i;
    }

    // If there are no local variables to be declared, removes the node where they would be declared.
    private void CleanBlockLocalVars(ASTCleaner cleaner)
    {
        if (cleaner.Context.Settings.CleanupLocalVarDeclarations)
        {
            // Locals will be manually declared, so no block locals to clean up
            return;
        }
        if (cleaner.TopFragmentContext!.LocalVariableNamesList.Count > 0)
        {
            // We have local(s) still, so don't remove declaration(s)
            return;
        }

        for (int i = 0; i < Children.Count; i++)
        {
            if (Children[i] is BlockLocalVarDeclNode)
            {
                Children.RemoveAt(i);
                break;
            }
        }
    }

    // Performs all cleanup operations for this block
    private void CleanAll(ASTCleaner cleaner)
    {
        bool newFragment = FragmentContext != cleaner.TopFragmentContext;
        if (newFragment)
        {
            cleaner.PushFragmentContext(FragmentContext);
            FragmentContext.RemoveLocal(VMConstants.TempReturnVariable);
        }

        // Clean all child nodes, and perform block cleanup logic
        for (int i = 0; i < Children.Count; i++)
        {
            Children[i] = Children[i].Clean(cleaner);
            if (Children[i] is IBlockCleanupNode blockCleanupNode)
            {
                // Clean this node with the additional context of this block
                i = blockCleanupNode.BlockClean(cleaner, this, i);
            }
        }

        if (newFragment)
        {
            CleanBlockLocalVars(cleaner);
            cleaner.PopFragmentContext();
        }
    }

    // Performs all post-cleanup operations for this block
    private void PostCleanAll(ASTCleaner cleaner, SwitchNode? parentSwitch, bool isStruct)
    {
        bool newFragment = FragmentContext != cleaner.TopFragmentContext;
        if (newFragment)
        {
            if (isStruct && cleaner.Context.Settings.CleanupLocalVarDeclarations)
            {
                // Connect the struct's local scope to the parent local scope tree.
                // This allows struct local variable reads to hoist locals outside of the struct.
                ASTFragmentContext parentContext = cleaner.TopFragmentContext!;
                cleaner.PushFragmentContext(FragmentContext);
                cleaner.TopFragmentContext!.PushLocalScope(cleaner.Context, this, this);
                cleaner.TopFragmentContext!.CurrentLocalScope!.InsertAsChildOf(parentContext.CurrentLocalScope!);
            }
            else
            {
                // Regular push of fragment context/scope.
                cleaner.PushFragmentContext(FragmentContext);
                cleaner.TopFragmentContext!.PushLocalScope(cleaner.Context, this, this);
            }

        }
        BlockNode? prevPostCleanupBlock = cleaner.TopFragmentContext!.CurrentPostCleanupBlock;
        cleaner.TopFragmentContext!.CurrentPostCleanupBlock = this;

        if (parentSwitch is not null)
        {
            // Handle switch statement case local scopes
            bool inCaseScope = false;
            bool endingCaseScope = false;
            for (int i = 0; i < Children.Count; i++)
            {
                if (Children[i] is SwitchCaseNode && (!inCaseScope || endingCaseScope))
                {
                    inCaseScope = true;
                    if (endingCaseScope)
                    {
                        // End scope from last case(s)
                        cleaner.TopFragmentContext!.PopLocalScope(cleaner.Context);
                        endingCaseScope = false;
                    }
                    cleaner.TopFragmentContext!.PushLocalScope(cleaner.Context, prevPostCleanupBlock!, parentSwitch);
                }
                else if (inCaseScope && Children[i] is BreakNode or ContinueNode)
                {
                    // Upon discovering a break/continue statement, prepare to end local scope at next case
                    endingCaseScope = true;
                }

                // Regular post-cleanup
                Children[i] = Children[i].PostClean(cleaner);
            }
            if (inCaseScope)
            {
                cleaner.TopFragmentContext!.PopLocalScope(cleaner.Context);
            }
        }
        else
        {
            // Normal cleanup
            for (int i = 0; i < Children.Count; i++)
            {
                Children[i] = Children[i].PostClean(cleaner);
            }
        }

        cleaner.TopFragmentContext!.CurrentPostCleanupBlock = prevPostCleanupBlock;
        if (newFragment)
        {
            cleaner.TopFragmentContext!.PopLocalScope(cleaner.Context);
            cleaner.PopFragmentContext();
        }
    }

    /// <inheritdoc cref="IASTNode{IStatementNode}.Clean(ASTCleaner)"/>
    public IFragmentNode Clean(ASTCleaner cleaner)
    {
        CleanAll(cleaner);
        return this;
    }

    /// <inheritdoc/>
    IStatementNode IASTNode<IStatementNode>.Clean(ASTCleaner cleaner)
    {
        CleanAll(cleaner);
        return this;
    }

    /// <inheritdoc cref="IASTNode{IStatementNode}.PostClean(ASTCleaner)"/>
    public IFragmentNode PostClean(ASTCleaner cleaner)
    {
        PostCleanAll(cleaner, null, false);
        return this;
    }

    /// <summary>
    /// Same as <see cref="PostClean(ASTCleaner)"/>, but for specifically a struct block.
    /// </summary>
    public IFragmentNode PostCleanStruct(ASTCleaner cleaner)
    {
        PostCleanAll(cleaner, null, true);
        return this;
    }

    /// <summary>
    /// Same as <see cref="PostClean(ASTCleaner)"/>, but for specifically a switch statement block.
    /// </summary>
    public IFragmentNode PostCleanSwitch(ASTCleaner cleaner, SwitchNode parentSwitch)
    {
        PostCleanAll(cleaner, parentSwitch, false);
        return this;
    }

    /// <inheritdoc/>
    IStatementNode IASTNode<IStatementNode>.PostClean(ASTCleaner cleaner)
    {
        PostCleanAll(cleaner, null, false);
        return this;
    }

    /// <summary>
    /// If this block has 0 or 2+ statements, returns this block.
    /// If this block has one statement, returns that statement.
    /// </summary>
    public IStatementNode GetShortestStatement()
    {
        if (Children.Count == 1)
        {
            return Children[0];
        }
        return this;
    }

    /// <summary>
    /// Prints a switch statement block, performing special processing for indentation.
    /// </summary>
    private void PrintSwitch(ASTPrinter printer)
    {
        printer.OpenBlock();

        bool switchCaseIndent = false;
        bool justDidAfterEmptyLine = false;
        for (int i = 0; i < Children.Count; i++)
        {
            printer.StartLine();

            // Print statement
            IStatementNode current = Children[i];
            if (current.EmptyLineBefore && i != 0 && !justDidAfterEmptyLine && Children[i - 1] is not SwitchCaseNode)
            {
                printer.EndLine();
                printer.StartLine();
            }
            current.Print(printer);
            if (current.SemicolonAfter)
            {
                printer.Semicolon();
            }
            if (current.EmptyLineAfter && i != Children.Count - 1 && Children[i + 1] is not SwitchCaseNode)
            {
                printer.EndLine();
                printer.StartLine();
                justDidAfterEmptyLine = true;
            }
            else
            {
                justDidAfterEmptyLine = false;
            }

            // Check if we need to handle indents for switch
            if ((i + 1) < Children.Count)
            {
                if (current is SwitchCaseNode && Children[i + 1] is not SwitchCaseNode)
                {
                    printer.Indent();
                    switchCaseIndent = true;
                }
                else if (switchCaseIndent && current is not SwitchCaseNode && Children[i + 1] is SwitchCaseNode)
                {
                    printer.Dedent();
                    switchCaseIndent = false;
                }
            }

            printer.EndLine();
        }

        if (switchCaseIndent)
        {
            printer.Dedent();
        }

        printer.CloseBlock();
    }

    /// <summary>
    /// Prints a struct initialization block, performing special processing for syntax.
    /// </summary>
    private void PrintStructInitialization(ASTPrinter printer)
    {
        printer.OpenBlock();

        bool justDidAfterEmptyLine = false;
        for (int i = 0; i < Children.Count; i++)
        {
            printer.StartLine();

            // Print statement
            IStatementNode child = Children[i];
            if (child.EmptyLineBefore && i != 0 && !justDidAfterEmptyLine)
            {
                printer.EndLine();
                printer.StartLine();
            }
            child.Print(printer);
            if (i != Children.Count - 1)
            {
                // Write comma after struct members
                printer.Write(',');

                if (child.EmptyLineAfter)
                {
                    printer.EndLine();
                    printer.StartLine();
                    justDidAfterEmptyLine = true;
                }
                else
                {
                    justDidAfterEmptyLine = false;
                }
            }

            printer.EndLine();
        }

        printer.CloseBlock();
    }

    /// <summary>
    /// Prints a regular block.
    /// </summary>
    private void PrintRegular(ASTPrinter printer)
    {
        // Just a normal block
        if (UseBraces)
        {
            printer.OpenBlock();
        }

        bool justDidAfterEmptyLine = false;
        for (int i = 0; i < Children.Count; i++)
        {
            printer.StartLine();

            // Print statement
            IStatementNode child = Children[i];
            if (child.EmptyLineBefore && i != 0 && !justDidAfterEmptyLine)
            {
                printer.EndLine();
                printer.StartLine();
            }
            child.Print(printer);
            if (child.SemicolonAfter)
            {
                printer.Semicolon();
            }
            if (child.EmptyLineAfter && i != Children.Count - 1)
            {
                printer.EndLine();
                printer.StartLine();
                justDidAfterEmptyLine = true;
            }
            else
            {
                justDidAfterEmptyLine = false;
            }

            printer.EndLine();
        }

        if (UseBraces)
        {
            printer.CloseBlock();
        }
    }

    /// <inheritdoc/>
    public void Print(ASTPrinter printer)
    {
        bool newFragment = FragmentContext != printer.TopFragmentContext;
        if (newFragment)
        {
            printer.PushFragmentContext(FragmentContext);
        }

        // Print specific type of block
        if (PartOfSwitch)
        {
            PrintSwitch(printer);
        }
        else if (printer.StructArguments is not null)
        {
            PrintStructInitialization(printer);
        }
        else
        {
            PrintRegular(printer);
        }

        if (newFragment)
        {
            printer.PopFragmentContext();
        }
    }

    /// <inheritdoc/>
    public void PrintSingleLine(ASTPrinter printer)
    {
        printer.PushFragmentContext(FragmentContext);

        printer.EndLine();
        printer.Indent();
        printer.StartLine();
        if (Children.Count != 1)
        {
            throw new DecompilerException("Expected only one child node when printing on single line");
        }
        Children[0].Print(printer);
        if (Children[0].SemicolonAfter)
        {
            printer.Semicolon();
        }
        printer.Dedent();

        printer.PopFragmentContext();
    }

    /// <inheritdoc/>
    public bool RequiresMultipleLines(ASTPrinter printer)
    {
        // If we have more than one child node, or zero child nodes, we need multiple lines
        if (Children.Count > 1 || Children.Count == 0)
        {
            return true;
        }

        // If our single child needs multiple lines, so do we
        if (Children[0].RequiresMultipleLines(printer))
        {
            return true;
        }

        // Other basic cases: all switch statements, and all struct initializations
        return PartOfSwitch || printer.StructArguments is not null;
    }

    /// <summary>
    /// Adds a block-level local variable declaration node to the top of this block.
    /// </summary>
    public void AddBlockLocalVarDecl(DecompileContext context)
    {
        if (context.Settings.CleanupLocalVarDeclarations)
        {
            // Locals will be manually declared
            return;
        }

        Children.Insert(0, new BlockLocalVarDeclNode()
        {
            EmptyLineAfter = context.Settings.EmptyLineAfterBlockLocals
        });
    }

    /// <inheritdoc/>
    public IEnumerable<IBaseASTNode> EnumerateChildren()
    {
        return Children;
    }
}
