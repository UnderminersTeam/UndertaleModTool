/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using Underanalyzer.Decompiler.AST;

namespace Underanalyzer.Decompiler.GameSpecific;

/// <summary>
/// A macro type usable for GML enums.
/// </summary>
public class EnumMacroType : IMacroTypeInt64
{
    public string Name { get; }
    internal Dictionary<long, string> ValueToValueName { get; }

    /// <summary>
    /// Constructs an enum macro type with a given name and values, mapped from value to value name.
    /// </summary>
    public EnumMacroType(string name, Dictionary<long, string> values)
    {
        Name = name;
        ValueToValueName = new(values);
    }

    /// <summary>
    /// Constructs an enum macro type with a given name and values, mapped from value name to value.
    /// </summary>
    public EnumMacroType(string name, Dictionary<string, long> values)
    {
        Name = name;
        ValueToValueName = new(values.Count);
        foreach ((string valueName, long value) in values)
        {
            ValueToValueName[value] = valueName;
        }
    }

    /// <summary>
    /// Constructs a macro type from an enum, where value names are the constant names, 
    /// associated with their enum values.
    /// </summary>
    public EnumMacroType(Type enumType, string? name = null)
    {
        Name = name ?? enumType.Name;
        Array values = Enum.GetValues(enumType);
        ValueToValueName = new(values.Length);
        foreach (long value in values)
        {
            ValueToValueName[value] = Enum.GetName(enumType, value) ?? throw new NullReferenceException();
        }
    }

    public IExpressionNode? Resolve(ASTCleaner cleaner, IMacroResolvableNode node, long data)
    {
        if (ValueToValueName.TryGetValue(data, out string? valueName))
        {
            // Use enum node, and declare new enum if one doesn't exist already
            if (!cleaner.Context.NameToEnumDeclaration.ContainsKey(Name))
            {
                cleaner.DeclareEnum(new(this));
            }
            return new EnumValueNode(Name, valueName, data, false);
        }
        return null;
    }
}
