/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using Underanalyzer.Decompiler.GameSpecific;

namespace Underanalyzer.Decompiler.AST;

/// <summary>
/// Represents a 16-bit signed integer constant in the AST.
/// </summary>
public class Int16Node(short value, bool regularPush) : IConstantNode<short>, IMacroResolvableNode, IConditionalValueNode
{
    public short Value { get; } = value;

    /// <summary>
    /// If true, this number was pushed with a normal Push instruction opcode,
    /// rather than the usual PushImmediate.
    /// </summary>
    public bool RegularPush { get; } = regularPush;

    public bool Duplicated { get; set; } = false;
    public bool Group { get; set; } = false;
    public IGMInstruction.DataType StackType { get; set; } = IGMInstruction.DataType.Int16;

    public string ConditionalTypeName => "Integer";
    public string ConditionalValue => Value.ToString();

    public IExpressionNode Clean(ASTCleaner cleaner)
    {
        return this;
    }

    public void Print(ASTPrinter printer)
    {
        printer.Write(Value);
    }

    public bool RequiresMultipleLines(ASTPrinter printer)
    {
        return false;
    }

    public IExpressionNode? ResolveMacroType(ASTCleaner cleaner, IMacroType type)
    {
        if (type is IMacroTypeInt32 type32)
        {
            return type32.Resolve(cleaner, this, Value);
        }
        return null;
    }
}
