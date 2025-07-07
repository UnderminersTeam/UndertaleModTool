/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System;
using Underanalyzer.Decompiler.GameSpecific;

namespace Underanalyzer.Decompiler.AST;

/// <summary>
/// Represents a string constant in the AST.
/// </summary>
public class StringNode(IGMString value) : IConstantNode<IGMString>, IConditionalValueNode
{
    public IGMString Value { get; } = value;

    public bool Duplicated { get; set; } = false;
    public bool Group { get; set; } = false;
    public IGMInstruction.DataType StackType { get; set; } = IGMInstruction.DataType.String;

    public string ConditionalTypeName => "String";
    public string ConditionalValue => Value.Content;

    public IExpressionNode Clean(ASTCleaner cleaner)
    {
        return this;
    }

    public void Print(ASTPrinter printer)
    {
        ReadOnlySpan<char> content = Value.Content;
        if (printer.Context.GameContext.UsingGMS2OrLater)
        {
            // Handle string escaping.
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

    public bool RequiresMultipleLines(ASTPrinter printer)
    {
        return false;
    }

    public IExpressionNode? ResolveMacroType(ASTCleaner cleaner, IMacroType type)
    {
        if (type is IMacroTypeConditional conditional)
        {
            return conditional.Resolve(cleaner, this);
        }
        return null;
    }
}
