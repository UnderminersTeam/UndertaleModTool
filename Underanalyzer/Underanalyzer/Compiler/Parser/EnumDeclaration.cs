/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;
using Underanalyzer.Compiler.Lexer;
using Underanalyzer.Compiler.Parser;

namespace Underanalyzer.Compiler.Nodes;

/// <summary>
/// Represents a single enum declaration in code, before being converted to a <see cref="GMEnum"/>.
/// </summary>
internal sealed class EnumDeclaration
{
    /// <summary>
    /// Name of the enum being declared.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// List of value names, in order as declared in the enum.
    /// </summary>
    public List<string> ValueNames { get; init; }

    /// <summary>
    /// Map of value names to value expressions (or null if no expression given).
    /// </summary>
    public Dictionary<string, IASTNode?> Values { get; init; }

    /// <summary>
    /// Map of value names to integer values (for ones that have been resolved).
    /// </summary>
    public Dictionary<string, long> IntegerValues { get; init; }

    private EnumDeclaration(string name, List<string> valueNames, Dictionary<string, IASTNode?> values)
    {
        Name = name;
        ValueNames = valueNames;
        Values = values;
        IntegerValues = new(values.Count);
    }

    /// <summary>
    /// Parses an enum declaration from the current position of the given context,
    /// adding to the context's map of parse enums.
    /// </summary>
    public static void Parse(ParseContext context)
    {
        // Parse "enum" keyword
        if (context.EnsureToken(KeywordKind.Enum) is null)
        {
            return;
        }

        // Parse enum name
        if (context.EndOfCode)
        {
            context.CompileContext.PushError("Unexpected end of code (expected name for enum)");
            return;
        }
        IToken tokenEnumName = context.Tokens[context.Position++];
        if (tokenEnumName is not TokenVariable tokenEnumNameVar)
        {
            context.CompileContext.PushError($"Expected name for enum, got '{tokenEnumName}'", tokenEnumName);
            return;
        }

        // Check if overriding a builtin variable
        string enumNameStr = tokenEnumNameVar.Text;
        if (tokenEnumNameVar.BuiltinVariable is not null)
        {
            context.CompileContext.PushError($"Declaring enum name over builtin variable '{enumNameStr}'", tokenEnumNameVar);
            return;
        }

        // Check if declared already
        bool shouldAdd = true;
        if (context.CompileContext.Enums.ContainsKey(enumNameStr) || context.ParseEnums.ContainsKey(enumNameStr))
        {
            context.CompileContext.PushError($"Enum name '{enumNameStr}' declared more than once", tokenEnumNameVar);
            shouldAdd = false;
        }

        // Parse "{"
        if (context.EnsureToken(SeparatorKind.BlockOpen, KeywordKind.Begin) is null)
        {
            return;
        }

        // Create new enum entry, and parse enum values
        List<string> valueNames = new(16);
        Dictionary<string, IASTNode?> values = new(16);
        while (!context.EndOfCode && !context.IsCurrentToken(SeparatorKind.BlockClose))
        {
            // Parse name of enum value
            IToken valueName = context.Tokens[context.Position++];
            if (valueName is not TokenVariable valueNameVar)
            {
                context.CompileContext.PushError($"Expected name for enum value, got '{valueName}'", valueName);
                return;
            }

            // Check for duplicates
            bool shouldAddValue = true;
            string valueNameStr = valueNameVar.Text;
            if (values.ContainsKey(valueNameStr))
            {
                context.CompileContext.PushError($"Duplicate enum value name '{valueNameStr}'", valueNameVar);
                shouldAddValue = false;
            }

            // Parse value, if one exists
            IASTNode? value = null;
            if (context.IsCurrentToken(OperatorKind.Assign) || context.IsCurrentToken(OperatorKind.Assign2))
            {
                context.Position++;
                value = Expressions.ParseExpression(context);
            }

            // Add new enum value entry
            if (shouldAddValue)
            {
                valueNames.Add(valueNameStr);
                values.Add(valueNameStr, value);
            }

            // Expect "," or "}"
            if (context.IsCurrentToken(SeparatorKind.Comma))
            {
                context.Position++;
            }
            else if (!context.IsCurrentToken(SeparatorKind.BlockClose, KeywordKind.End))
            {
                // Stop parsing if unexpected token
                break;
            }
        }

        // Parse "}"
        if (context.EnsureToken(SeparatorKind.BlockClose) is null)
        {
            return;
        }

        // Add new enum entry
        if (shouldAdd)
        {
            context.ParseEnums.Add(enumNameStr, new(enumNameStr, valueNames, values));
        }
    }

    /// <summary>
    /// Given a parse context, this will resolve all parsed enums to <see cref="GMEnum"/> instances,
    /// for the broader compile context.
    /// </summary>
    public static void ResolveValues(ParseContext context)
    {
        // First pass: resolve all direct values
        foreach (EnumDeclaration decl in context.ParseEnums.Values)
        {
            long currentValue = 0;
            bool currentValueValid = true;
            foreach (string name in decl.ValueNames)
            {
                // Process expression, and resolve constant if possible
                if (decl.Values[name]?.PostProcess(context) is IASTNode processed)
                {
                    decl.Values[name] = processed;

                    (currentValue, currentValueValid) = processed switch
                    {
                        NumberNode { Value: double number } => ((long)number, true),
                        Int64Node { Value: long number } => (number, true),
                        _ => (0, false)
                    };
                }

                // If we have a valid value, assign it here, and increment it
                if (currentValueValid)
                {
                    decl.IntegerValues[name] = currentValue;
                    currentValue++;
                }
            }
        }

        // Second pass: resolve all indirect values
        foreach (EnumDeclaration decl in context.ParseEnums.Values)
        {
            long currentValue = 0;
            bool currentValueValid = true;
            foreach (string name in decl.ValueNames)
            {
                // If resolved in first pass, use that value and increment it
                if (decl.IntegerValues.TryGetValue(name, out long alreadyResolved))
                {
                    currentValue = alreadyResolved + 1;
                    currentValueValid = true;
                    continue;
                }

                // Process expression, and resolve constant if possible
                if (decl.Values[name]?.PostProcess(context) is IASTNode processed)
                {
                    decl.Values[name] = processed;

                    (currentValue, currentValueValid) = processed switch
                    {
                        NumberNode { Value: double number } => ((long)number, true),
                        Int64Node { Value: long number } => (number, true),
                        _ => (0, false)
                    };
                }

                // If we have a valid value, assign it here, and increment it
                if (currentValueValid)
                {
                    decl.IntegerValues[name] = currentValue;
                    currentValue++;
                }
                else
                {
                    // No valid value at the end, so report error
                    context.CompileContext.PushError($"Failed to resolve enum value '{decl.Name}.{name}' to a constant integer value", null);

                    // Use placeholder value for any following values, if needed
                    currentValue = 0;
                    currentValueValid = true;
                }
            }
        }

        // Convert parse enums to GMEnum form
        foreach (EnumDeclaration decl in context.ParseEnums.Values)
        {
            List<GMEnumValue> values = new(decl.ValueNames.Count);
            foreach (string valueName in decl.ValueNames)
            {
                decl.IntegerValues.TryGetValue(valueName, out long value);
                values.Add(new(valueName, value));
            }
            context.CompileContext.Enums.Add(decl.Name, new(decl.Name, values));
        }
        context.ParseEnums.Clear();
    }
}
