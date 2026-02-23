/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;
using Underanalyzer.Decompiler.GameSpecific;

namespace Underanalyzer.Decompiler.AST;

/// <summary>
/// Represents an instance type (<see cref="IGMInstruction.InstanceType"/>) in the AST.
/// </summary>
public class InstanceTypeNode(IGMInstruction.InstanceType instType, bool fromBuiltinFunction = false) : IExpressionNode, IConditionalValueNode
{
    /// <summary>
    /// The instance type for this node.
    /// </summary>
    public IGMInstruction.InstanceType InstanceType { get; } = instType;

    /// <summary>
    /// Whether this node was created from a builtin function, such as <see cref="VMConstants.SelfFunction"/>.
    /// </summary>
    public bool FromBuiltinFunction { get; } = fromBuiltinFunction;

    /// <inheritdoc/>
    public bool Duplicated { get; set; } = false;

    /// <inheritdoc/>
    public bool Group { get; set; } = false;

    /// <inheritdoc/>
    public IGMInstruction.DataType StackType { get; set; } = IGMInstruction.DataType.Int32;

    /// <inheritdoc/>
    public string ConditionalTypeName => "InstanceType";

    /// <inheritdoc/>
    public string ConditionalValue => InstanceType.ToString();

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
        printer.Write(InstanceType switch
        {
            IGMInstruction.InstanceType.Self => "self",
            IGMInstruction.InstanceType.Other => "other",
            IGMInstruction.InstanceType.All => "all",
            IGMInstruction.InstanceType.Noone => "noone",
            IGMInstruction.InstanceType.Global => "global",
            _ => throw new DecompilerException($"Printing unknown instance type {InstanceType}")
        });
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
