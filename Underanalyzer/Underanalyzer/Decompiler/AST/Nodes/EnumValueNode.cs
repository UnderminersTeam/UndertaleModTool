/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;
using Underanalyzer.Decompiler.GameSpecific;

namespace Underanalyzer.Decompiler.AST;

/// <summary>
/// Represents a reference to a single enum value in the AST.
/// </summary>
public class EnumValueNode(string enumName, string enumValueName, long enumValue, bool isUnknownEnum) 
    : IExpressionNode, IMacroResolvableNode, IConditionalValueNode
{
    /// <summary>
    /// The name of the base enum type being referenced.
    /// </summary>
    public string EnumName { get; } = enumName;

    /// <summary>
    /// The name of the value on the enum being referenced.
    /// </summary>
    public string EnumValueName { get; } = enumValueName;

    /// <summary>
    /// The raw value of the enum value.
    /// </summary>
    public long EnumValue { get; } = enumValue;

    /// <summary>
    /// If true, this enum value node references an unknown enum.
    /// </summary>
    public bool IsUnknownEnum { get; } = isUnknownEnum;

    /// <inheritdoc/>
    public bool Duplicated { get; set; } = false;

    /// <inheritdoc/>
    public bool Group { get; set; } = false;

    /// <inheritdoc/>
    public IGMInstruction.DataType StackType { get; set; } = IGMInstruction.DataType.Int64;

    /// <inheritdoc/>
    public string ConditionalTypeName => "EnumValue";

    /// <inheritdoc/>
    public string ConditionalValue => IsUnknownEnum ? EnumValue.ToString() : $"{EnumName}.{EnumValueName}";

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
        printer.Write(EnumName);
        printer.Write('.');
        printer.Write(EnumValueName);
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
        if (type is IMacroTypeInt64 type64)
        {
            string enumNameBefore = EnumName;
            if (type64.Resolve(cleaner, this, EnumValue) is IExpressionNode resolved)
            {
                // Dereference the unknown enum, if applicable
                if (IsUnknownEnum && (resolved is not EnumValueNode enumValueNode || enumValueNode.EnumName != enumNameBefore))
                {
                    cleaner.Context.UnknownEnumReferenceCount--;
                    if (cleaner.Context.UnknownEnumReferenceCount == 0)
                    {
                        // Remove declaration altogether - it's no longer referenced
                        cleaner.Context.NameToEnumDeclaration.Remove(EnumName);
                        cleaner.Context.EnumDeclarations.Remove(cleaner.Context.UnknownEnumDeclaration!);
                        cleaner.Context.UnknownEnumDeclaration = null;
                    }
                }
                return resolved;
            }
        }
        return null;
    }

    /// <inheritdoc/>
    public IEnumerable<IBaseASTNode> EnumerateChildren()
    {
        return [];
    }
}
