/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;
using Underanalyzer.Decompiler.GameSpecific;

namespace Underanalyzer.Decompiler.AST;

/// <summary>
/// Represents a 64-bit signed integer constant in the AST.
/// </summary>
public class Int64Node(long value) : IConstantNode<long>, IMacroResolvableNode, IConditionalValueNode
{
    /// <inheritdoc/>
    public long Value { get; } = value;

    /// <inheritdoc/>
    public bool Duplicated { get; set; } = false;

    /// <inheritdoc/>
    public bool Group { get; set; } = false;

    /// <inheritdoc/>
    public IGMInstruction.DataType StackType { get; set; } = IGMInstruction.DataType.Int64;

    /// <inheritdoc/>
    public string ConditionalTypeName => "Integer";

    /// <inheritdoc/>
    public string ConditionalValue => Value.ToString();

    /// <inheritdoc/>
    public IExpressionNode Clean(ASTCleaner cleaner)
    {
        // If we aren't detected as an enum yet, and we're within signed 32-bit range, we assume this is an unknown enum
        if (Value >= int.MinValue && Value <= int.MaxValue)
        {
            // Check if we have an unknown enum name to use (if null, we don't generate/use one at all)
            string unknownEnumName = cleaner.Context.Settings.UnknownEnumName;
            if (unknownEnumName is not null)
            {
                string enumValueName;
                if (cleaner.Context.UnknownEnumDeclaration is null)
                {
                    // Create a new unknown enum declaration, populated with this enum value
                    enumValueName = string.Format(cleaner.Context.Settings.UnknownEnumValuePattern, Value.ToString().Replace("-", "m"));
                    cleaner.Context.UnknownEnumDeclaration = new GMEnum(unknownEnumName, [new(enumValueName, Value)]);
                    cleaner.DeclareEnum(cleaner.Context.UnknownEnumDeclaration);
                }
                else
                {
                    // If the enum doesn't already contain this value, add this new one
                    if (cleaner.Context.UnknownEnumDeclaration.FindValue(Value) is not GMEnumValue gmEnumValue)
                    {
                        enumValueName = string.Format(cleaner.Context.Settings.UnknownEnumValuePattern, Value.ToString().Replace("-", "m"));
                        cleaner.Context.UnknownEnumDeclaration.AddValue(enumValueName, Value);
                    }
                    else
                    {
                        // We have an existing name already on the enum declaration; use it
                        enumValueName = gmEnumValue.Name;
                    }
                }

                // Turn into reference to this enum
                cleaner.Context.UnknownEnumReferenceCount++;
                return new EnumValueNode(unknownEnumName, enumValueName, Value, true);
            }
        }

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
        printer.Write(Value);
    }

    /// <inheritdoc/>
    public bool RequiresMultipleLines(ASTPrinter printer)
    {
        return false;
    }

    /// <inheritdoc/>
    public IExpressionNode? ResolveMacroType(ASTCleaner cleaner, IMacroType type)
    {
        if (type is IMacroTypeInt64 type64)
        {
            return type64.Resolve(cleaner, this, Value);
        }
        return null;
    }

    /// <inheritdoc/>
    public IEnumerable<IBaseASTNode> EnumerateChildren()
    {
        return [];
    }
}
