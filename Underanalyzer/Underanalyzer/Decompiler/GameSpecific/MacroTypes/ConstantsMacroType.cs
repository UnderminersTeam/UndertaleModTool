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
/// A macro type usable for general-purpose constants.
/// </summary>
public class ConstantsMacroType : IMacroTypeInt32
{
    private Dictionary<int, string> ValueToConstantName { get; }

    /// <summary>
    /// Constructs a macro type from a dictionary of constant values, from value to name.
    /// </summary>
    public ConstantsMacroType(Dictionary<int, string> constants)
    {
        ValueToConstantName = new(constants);
    }

    /// <summary>
    /// Constructs a macro type from a dictionary of constant values, from name to value.
    /// </summary>
    public ConstantsMacroType(Dictionary<string, int> constants)
    {
        ValueToConstantName = new(constants.Count);
        foreach ((string name, int value) in constants)
        {
            ValueToConstantName[value] = name;
        }
    }

    /// <summary>
    /// Constructs a macro type from an enum, where value names are the constant names, 
    /// associated with their enum values.
    /// </summary>
    public ConstantsMacroType(Type enumType)
    {
        Array values = Enum.GetValues(enumType);
        ValueToConstantName = new(values.Length);
        foreach (int value in values)
        {
            ValueToConstantName[value] = Enum.GetName(enumType, value) ?? throw new NullReferenceException();
        }
    }

    public IExpressionNode? Resolve(ASTCleaner cleaner, IMacroResolvableNode node, int data)
    {
        if (ValueToConstantName.TryGetValue(data, out string? name))
        {
            return new MacroValueNode(name);
        }
        return null;
    }
}
