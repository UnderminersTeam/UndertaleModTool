/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;
using Underanalyzer.Decompiler.GameSpecific;

namespace Underanalyzer;

/// <summary>
/// Represents an enum, composed of a varying number of defined values.
/// </summary>
public sealed class GMEnum
{
    /// <summary>
    /// Name of the enum. Should be a valid GML enum name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The values of this enum.
    /// </summary>
    public IEnumerable<GMEnumValue> Values { get => _values; }

    // Internal list of values used for this enum
    private readonly List<GMEnumValue> _values;

    // Internal lookup dictionaries used for this enum
    private readonly Dictionary<long, GMEnumValue> _valueLookupByValue;
    private readonly Dictionary<string, GMEnumValue> _valueLookupByName;

    /// <summary>
    /// Makes a new enum, using the existing list (does not copy it - it should not be modified).
    /// </summary>
    public GMEnum(string name, List<GMEnumValue> values)
    {
        Name = name;
        _values = values;

        // Construct lookup dictionaries
        _valueLookupByValue = new(_values.Count);
        _valueLookupByName = new(_values.Count);
        foreach (var value in _values)
        {
            _valueLookupByValue[value.Value] = value;
            _valueLookupByName[value.Name] = value;
        }
    }

    /// <summary>
    /// Makes a new enum, using an enum macro type from the decompiler.
    /// </summary>
    public GMEnum(EnumMacroType enumMacroType)
    {
        Name = enumMacroType.Name;

        // Construct list and lookup dictionaries
        _values = new(enumMacroType.ValueToValueName.Count);
        _valueLookupByValue = new(enumMacroType.ValueToValueName.Count);
        _valueLookupByName = new(enumMacroType.ValueToValueName.Count);
        foreach ((long value, string valueName) in enumMacroType.ValueToValueName)
        {
            GMEnumValue newValue = new(valueName, value);
            _values.Add(newValue);
            _valueLookupByValue[value] = newValue;
            _valueLookupByName[valueName] = newValue;
        }
    }

    /// <summary>
    /// Makes a copy of an existing enum, so that new values can be added without affecting the original.
    /// </summary>
    public GMEnum(GMEnum existing)
    {
        Name = existing.Name;
        _values = new(existing._values);
        _valueLookupByValue = new(existing._valueLookupByValue);
        _valueLookupByName = new(existing._valueLookupByName);
    }

    /// <summary>
    /// Adds all values in the other enum that do not exist in this enum.
    /// Useful for merging multiple enum declarations into a global enum declaration, across code entries.
    /// </summary>
    public void AddNewValuesFrom(GMEnum other)
    {
        foreach (GMEnumValue value in other.Values)
        {
            if (!_valueLookupByName.ContainsKey(value.Name))
            {
                // The name doesn't exist in this enum, so add it
                _values.Add(value);
                _valueLookupByValue[value.Value] = value;
                _valueLookupByName[value.Name] = value;
            }
        }
    }

    /// <summary>
    /// Looks up the value entry for the given string name, on this enum.
    /// </summary>
    /// <returns><see langword="true"/> if contained in the enum; <see langword="false"/> otherwise.</returns>
    public bool ContainsValue(string name)
    {
        return _valueLookupByName.ContainsKey(name);
    }

    /// <summary>
    /// Looks up and returns the value for the given string name, on this enum.
    /// </summary>
    public bool TryGetValue(string name, out long value)
    {
        if (_valueLookupByName.TryGetValue(name, out GMEnumValue? enumValue))
        {
            value = enumValue.Value;
            return true;
        }
        value = default;
        return false;
    }

    /// <summary>
    /// Looks up the value entry for the given integer value, on this enum, or <see langword="null"/> if none exists.
    /// </summary>
    public GMEnumValue? FindValue(long value)
    {
        if (_valueLookupByValue.TryGetValue(value, out GMEnumValue? result))
        {
            return result;
        }
        return null;
    }

    /// <summary>
    /// Adds a new value entry to the enum. Assumes that the value does not already exist.
    /// </summary>
    public void AddValue(string name, long value)
    {
        GMEnumValue entry = new(name, value);
        _values.Add(entry);
        _valueLookupByValue[value] = entry;
        _valueLookupByName[name] = entry;
    }
}

/// <summary>
/// Represents a single enum value.
/// </summary>
public sealed class GMEnumValue(string name, long value)
{
    /// <summary>
    /// Name of the enum value. Should be a valid GML value name.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// The actual value of this value name, as a 64-bit integer.
    /// </summary>
    public long Value { get; } = value;
}