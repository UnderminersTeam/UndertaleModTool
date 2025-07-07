/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;

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

    public bool SemicolonAfter { get => false; }
    public bool EmptyLineBefore => false;
    public bool EmptyLineAfter => false;
    public ASTFragmentContext FragmentContext { get; } = fragmentContext;

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

    // Cleans all child nodes, and performs block cleanup logic
    private void CleanChildren(ASTCleaner cleaner)
    {
        for (int i = 0; i < Children.Count; i++)
        {
            Children[i] = Children[i].Clean(cleaner);
            if (Children[i] is IBlockCleanupNode blockCleanupNode)
            {
                // Clean this node with the additional context of this block
                i = blockCleanupNode.BlockClean(cleaner, this, i);
            }
        }
    }

    // If there are no local variables to be declared, removes the node where they would be declared.
    private void CleanBlockLocalVars(ASTCleaner cleaner)
    {
        if (cleaner.TopFragmentContext!.LocalVariableNamesList.Count > 0)
        {
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
        CleanChildren(cleaner);
        if (newFragment)
        {
            CleanBlockLocalVars(cleaner);
            cleaner.PopFragmentContext();
        }
    }

    public IFragmentNode Clean(ASTCleaner cleaner)
    {
        CleanAll(cleaner);
        return this;
    }

    IStatementNode IASTNode<IStatementNode>.Clean(ASTCleaner cleaner)
    {
        CleanAll(cleaner);
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

    public void Print(ASTPrinter printer)
    {
        printer.PushFragmentContext(FragmentContext);

        if (PartOfSwitch)
        {
            // We're part of a switch statement, and so will do special processing for indentation
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
        else if (printer.StructArguments is not null)
        {
            // We're a struct initialization block
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
        else
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

        printer.PopFragmentContext();
    }

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
        Children.Insert(0, new BlockLocalVarDeclNode()
        {
            EmptyLineAfter = context.Settings.EmptyLineAfterBlockLocals
        });
    }
}
