/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using Underanalyzer.Decompiler.GameSpecific;

namespace Underanalyzer.Decompiler.AST;

/// <summary>
/// Represents a string constant in the AST.
/// </summary>
public class StringNode(IGMString value) : IConstantNode<IGMString>, IConditionalValueNode
{
    /// <inheritdoc/>
    public IGMString Value { get; } = value;

    /// <inheritdoc/>
    public bool Duplicated { get; set; } = false;

    /// <inheritdoc/>
    public bool Group { get; set; } = false;

    /// <inheritdoc/>
    public IGMInstruction.DataType StackType { get; set; } = IGMInstruction.DataType.String;

    /// <inheritdoc/>
    public string ConditionalTypeName => "String";

    /// <inheritdoc/>
    public string ConditionalValue => Value.Content;

    /// <inheritdoc/>
    public IExpressionNode Clean(ASTCleaner cleaner)
    {
        return this;
    }

    /// <inheritdoc/>
    public IExpressionNode PostClean(ASTCleaner cleaner)
    {
        return this;
    }

    /// <summary>
    /// Helper to print a GMS2-style escaped string.
    /// </summary>
    public static void PrintGMS2String(ASTPrinter printer, ReadOnlySpan<char> content)
    {
        printer.Write('"');
        foreach (char c in content)
        {
            switch (c)
            {
                case '\n':
                    printer.Write("\\n");
                    break;
                case '\r':
                    printer.Write("\\r");
                    break;
                case '\b':
                    printer.Write("\\b");
                    break;
                case '\f':
                    printer.Write("\\f");
                    break;
                case '\t':
                    printer.Write("\\t");
                    break;
                case '\v':
                    printer.Write("\\v");
                    break;
                case '\a':
                    printer.Write("\\a");
                    break;
                case '\\':
                    printer.Write("\\\\");
                    break;
                case '\"':
                    printer.Write("\\\"");
                    break;
                default:
                    printer.Write(c);
                    break;
            }
        }
        printer.Write('"');
    }

    /// <inheritdoc/>
    public void Print(ASTPrinter printer)
    {
        ReadOnlySpan<char> content = Value.Content;
        if (printer.Context.GameContext.UsingGMS2OrLater)
        {
            // Handle string escaping.
            PrintGMS2String(printer, content);
        }
        else
        {
            // We don't have any way of escaping strings - must concatenate multiple parts.
            // We also have the choice between ' and ", so use whichever results in less splits.
            int numDoubleQuotes = 0, numSingleQuotes = 0;
            foreach (char c in content)
            {
                if (c == '"')
                {
                    numDoubleQuotes++;
                }
                else if (c == '\'')
                {
                    numSingleQuotes++;
                }
            }
            char quoteChar = (numDoubleQuotes > numSingleQuotes) ? '\'' : '"';
            char splitChar = (numDoubleQuotes > numSingleQuotes) ? '"' : '\'';

            printer.Write(quoteChar);
            foreach (char c in content)
            {
                if (c == quoteChar)
                {
                    printer.Write(quoteChar);
                    printer.Write(" + ");
                    printer.Write(splitChar);
                    printer.Write(quoteChar);
                    printer.Write(splitChar);
                    printer.Write(" + ");
                    printer.Write(quoteChar);
                }
                else
                {
                    printer.Write(c);
                }
            }
            printer.Write(quoteChar);
        }
    }

    /// <inheritdoc/>
    public bool RequiresMultipleLines(ASTPrinter printer)
    {
        return false;
    }

    /// <inheritdoc/>
    public IExpressionNode? ResolveMacroType(ASTCleaner cleaner, IMacroType type)
    {
        if (type is IMacroTypeConditional conditional)
        {
            return conditional.Resolve(cleaner, this);
        }
        return null;
    }

    /// <inheritdoc/>
    public IEnumerable<IBaseASTNode> EnumerateChildren()
    {
        return [];
    }
}
