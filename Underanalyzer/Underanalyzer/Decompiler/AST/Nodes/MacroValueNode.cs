/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;
using Underanalyzer.Decompiler.GameSpecific;

namespace Underanalyzer.Decompiler.AST;

/// <summary>
/// Represents a reference to a single macro value in the AST.
/// This is only generated during AST cleanup, so the stack type is undefined.
/// </summary>
public class MacroValueNode(string valueName) : IExpressionNode, IConditionalValueNode
{
    /// <summary>
    /// The content of the macro value name.
    /// </summary>
    public string ValueName { get; } = valueName;

    /// <inheritdoc/>
    public bool Duplicated { get; set; } = false;

    /// <inheritdoc/>
    public bool Group { get; set; } = false;

    /// <inheritdoc/>
    public IGMInstruction.DataType StackType { get; set; } = IGMInstruction.DataType.Int32;

    /// <inheritdoc/>
    public string ConditionalTypeName => "MacroValue";

    /// <inheritdoc/>
    public string ConditionalValue => ValueName;

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

    /// <inheritdoc/>
    public void Print(ASTPrinter printer)
    {
        if (Group)
        {
            printer.Write('(');
        }
        printer.Write(ValueName);
        if (Group)
        {
            printer.Write(')');
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
