/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using Underanalyzer.Decompiler.GameSpecific;

namespace Underanalyzer.Decompiler.AST;

/// <summary>
/// Represents an instance type (<see cref="IGMInstruction.InstanceType"/>) in the AST.
/// </summary>
public class InstanceTypeNode(IGMInstruction.InstanceType instType) : IExpressionNode, IConditionalValueNode
{
    /// <summary>
    /// The instance type for this node.
    /// </summary>
    public IGMInstruction.InstanceType InstanceType { get; } = instType;

    public bool Duplicated { get; set; } = false;
    public bool Group { get; set; } = false;
    public IGMInstruction.DataType StackType { get; set; } = IGMInstruction.DataType.Int32;

    public string ConditionalTypeName => "InstanceType";
    public string ConditionalValue => InstanceType.ToString();

    public IExpressionNode Clean(ASTCleaner cleaner)
    {
        return this;
    }

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
