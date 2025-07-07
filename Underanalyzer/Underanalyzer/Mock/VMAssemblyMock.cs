/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Underanalyzer.Mock;

/// <summary>
/// Utility class to parse mock VM assembly, for testing purposes.
/// </summary>
public static class VMAssembly
{
    private static readonly Dictionary<string, IGMInstruction.Opcode> StringToOpcode = [];
    private static readonly Dictionary<string, IGMInstruction.ExtendedOpcode> StringToExtOpcode = [];
    private static readonly Dictionary<char, IGMInstruction.DataType> CharToDataType = [];
    private static readonly Dictionary<string, AssetType> StringToAssetType = [];

    /// <summary>
    /// Initializes precomputed data for parsing VM assembly
    /// </summary>
    static VMAssembly()
    {
        // Normal opcodes
        Type typeOpcode = typeof(IGMInstruction.Opcode);
        foreach (IGMInstruction.Opcode opcode in Enum.GetValues(typeOpcode))
        {
            var field = typeOpcode.GetField(Enum.GetName(typeOpcode, opcode)!)!;
            var info = field.GetCustomAttribute<IGMInstruction.OpcodeInfo>()!;
            StringToOpcode[info.Mnemonic] = opcode;
        }

        // Extended opcodes
        Type typeExtType = typeof(IGMInstruction.ExtendedOpcode);
        foreach (IGMInstruction.ExtendedOpcode opcode in Enum.GetValues(typeExtType))
        {
            var field = typeExtType.GetField(Enum.GetName(typeExtType, opcode)!)!;
            var info = field.GetCustomAttribute<IGMInstruction.OpcodeInfo>()!;
            StringToOpcode[info.Mnemonic] = IGMInstruction.Opcode.Extended;
            StringToExtOpcode[info.Mnemonic] = opcode;
        }

        // Data types
        Type typeDataType = typeof(IGMInstruction.DataType);
        foreach (IGMInstruction.DataType dataType in Enum.GetValues(typeDataType))
        {
            var field = typeDataType.GetField(Enum.GetName(typeDataType, dataType)!)!;
            var info = field.GetCustomAttribute<IGMInstruction.DataTypeInfo>()!;
            CharToDataType[info.Mnemonic] = dataType;
        }

        // Asset types
        Type typeAssetType = typeof(AssetType);
        foreach (AssetType assetType in Enum.GetValues(typeAssetType))
        {
            StringToAssetType[Enum.GetName(typeAssetType, assetType)!] = assetType;
        }
    }

    /// <summary>
    /// Parses VM assembly into mock instruction data.
    /// </summary>
    public static GMCode ParseAssemblyFromLines(IEnumerable<string> lines, IGameContext? context, string name = "root")
    {
        List<GMInstruction> instructions = [];
        GMCode root = new(name, instructions);

        Dictionary<string, int> branchLabelAddresses = [];
        List<(string Label, GMInstruction Instr)> branchTargets = [];

        HashSet<GMVariable> variables = new(new GMVariableComparer());
        HashSet<GMFunction> functions = new(new GMFunctionComparer());
        if (context is not null)
        {
            foreach (GMFunction func in context.GlobalFunctions.FunctionToName.Keys.Cast<GMFunction>())
            {
                functions.Add(func);
            }
        }

        int address = 0;
        foreach (string line in lines)
        {
            // Totally empty line; ignore
            if (line.Length == 0)
            {
                continue;
            }

            // Ignore comment lines
            if (line[0] == '#')
            {
                continue;
            }

            // Branch label
            if (line[0] == ':')
            {
                string label = line[1..].Trim();
                if (label.Length < 3 || label[0] != '[' || label[^1] != ']')
                {
                    throw new Exception("Invalid branch header");
                }
                branchLabelAddresses[label[1..^1]] = address;
                continue;
            }

            // Sub-entry label
            if (line[0] == '>')
            {
                string decl = line[1..].Trim();

                // Split into label and parameters
                int space = decl.IndexOf(' ');
                string label, parameters;
                if (space == -1)
                {
                    label = decl;
                    parameters = string.Empty;
                }
                else
                {
                    label = decl[..space];
                    parameters = decl[(space + 1)..];
                }

                // Parse (optional) parameters
                int? localCount = ParseOptionalParameter(parameters, "locals");
                int? argCount = ParseOptionalParameter(parameters, "args");

                if (address == 0 && label == name)
                {
                    // If we're at the start and are the same name as the root, then just perform an update
                    if (localCount is not null)
                    {
                        root.LocalCount = localCount.Value;
                    }
                    if (argCount is not null)
                    {
                        root.ArgumentCount = argCount.Value;
                    }
                    continue;
                }

                // Add normal sub-entry as a child of the root
                root.Children.Add(new(label, instructions)
                {
                    Parent = root,
                    StartOffset = address,
                    LocalCount = localCount ?? 0,
                    ArgumentCount = argCount ?? 0
                });
                continue;
            }

            // Split line into constituent parts
            string[] parts = line.Trim().Split(' ');

            // Empty line; ignore
            if (parts.Length == 0 || string.IsNullOrEmpty(parts[0]))
            {
                continue;
            }

            string[] opcodeParts = parts[0].Split('.');

            // Parse opcode
            string opcodeStr = opcodeParts[0].ToLowerInvariant();
            if (!StringToOpcode.TryGetValue(opcodeStr, out IGMInstruction.Opcode opcode))
            {
                throw new Exception($"Unexpected opcode \"{opcodeStr}\"");
            }

            // Parse data types
            IGMInstruction.DataType type1 = 0;
            IGMInstruction.DataType type2 = 0;
            if (opcodeParts.Length >= 2)
            {
                if (opcodeParts[1].Length != 1)
                {
                    throw new Exception("Expected single character for data type 1");
                }
                char c = opcodeParts[1].ToLowerInvariant()[0];
                if (!CharToDataType.TryGetValue(c, out type1))
                {
                    throw new Exception($"Unexpected data type \"{c}\"");
                }
            }
            if (opcodeParts.Length >= 3)
            {
                if (opcodeParts[2].Length != 1)
                {
                    throw new Exception("Expected single character for data type 2");
                }
                char c = opcodeParts[2].ToLowerInvariant()[0];
                if (!CharToDataType.TryGetValue(c, out type2))
                {
                    throw new Exception($"Unexpected data type \"{c}\"");
                }
            }
            if (opcodeParts.Length >= 4)
            {
                throw new Exception("Too many data types");
            }

            // Construct mock instruction
            GMInstruction instr = new()
            {
                Address = address,
                Kind = opcode,
                Type1 = type1,
                Type2 = type2
            };

            // Parse additional data
            switch (opcode)
            {
                case IGMInstruction.Opcode.Compare:
                    {
                        // Parse comparison kind
                        if (parts.Length < 2)
                        {
                            throw new Exception("Compare needs comparison kind parameter");
                        }
                        instr.ComparisonKind = parts[1].ToLowerInvariant() switch
                        {
                            "lt" =>             IGMInstruction.ComparisonType.LesserThan,
                            "leq" or "lte" =>   IGMInstruction.ComparisonType.LesserEqualThan,
                            "eq" =>             IGMInstruction.ComparisonType.EqualTo,
                            "neq" =>            IGMInstruction.ComparisonType.NotEqualTo,
                            "geq" or "gte" =>   IGMInstruction.ComparisonType.GreaterEqualThan,
                            "gt" =>             IGMInstruction.ComparisonType.GreaterThan,
                            _ => throw new Exception("Unknown comparison kind")
                        };
                    }
                    break;
                case IGMInstruction.Opcode.Pop:
                    {
                        if (parts.Length < 2)
                        {
                            throw new Exception("Pop needs parameter");
                        }

                        // Parse swap variant
                        if (instr.Type1 == IGMInstruction.DataType.Int16)
                        {
                            if (!byte.TryParse(parts[1], out byte popSwapSize) || popSwapSize < 5 || popSwapSize > 6)
                            {
                                throw new Exception("Unexpected pop swap size");
                            }
                            instr.PopSwapSize = popSwapSize;
                            break;
                        }

                        // Parse variable destination
                        if (!ParseVariableFromString(parts[1], variables, out var variable, out var varType, out var instType))
                        {
                            throw new Exception($"Failed to parse variable {parts[1]}");
                        }
                        instr.Variable = variable;
                        instr.ReferenceVarType = varType;
                        instr.InstType = instType;
                    }
                    break;
                case IGMInstruction.Opcode.Duplicate:
                    {
                        // Parse normal dup size
                        if (parts.Length < 2)
                        {
                            throw new Exception("Duplicate needs size parameter");
                        }
                        if (!byte.TryParse(parts[1], out byte dupSize))
                        {
                            throw new Exception("Failed to parse parameter");
                        }
                        instr.DuplicationSize = dupSize;

                        // Parse "swap" mode size, if parameter exists
                        if (parts.Length >= 3)
                        {
                            if (!byte.TryParse(parts[2], out byte dupSize2))
                            {
                                throw new Exception("Failed to parse parameter");
                            }
                            instr.DuplicationSize2 = dupSize2;
                        }
                    }
                    break;
                case IGMInstruction.Opcode.CallVariable:
                    {
                        // Parse argument count
                        if (parts.Length < 2)
                        {
                            throw new Exception("CallVariable needs size parameter");
                        }
                        if (!int.TryParse(parts[1], out int argCount))
                        {
                            throw new Exception("Failed to parse parameter");
                        }
                        instr.ArgumentCount = argCount;
                    }
                    break;
                case IGMInstruction.Opcode.Branch:
                case IGMInstruction.Opcode.BranchTrue:
                case IGMInstruction.Opcode.BranchFalse:
                case IGMInstruction.Opcode.PushWithContext:
                case IGMInstruction.Opcode.PopWithContext:
                    {
                        if (parts.Length < 2)
                        {
                            throw new Exception("Branch instruction needs target");
                        }
                        string target = parts[1];

                        // PopWithContext has an exception to this branch target
                        if (opcode == IGMInstruction.Opcode.PopWithContext && target == "<drop>")
                        {
                            instr.PopWithContextExit = true;
                            break;
                        }

                        // Parse normal target
                        if (target.Length < 3 || target[0] != '[' || target[^1] != ']')
                        {
                            throw new Exception("Invalid branch target format");
                        }

                        // Store this target for later
                        branchTargets.Add((target[1..^1], instr));
                    }
                    break;
                case IGMInstruction.Opcode.Push:
                case IGMInstruction.Opcode.PushLocal:
                case IGMInstruction.Opcode.PushGlobal:
                case IGMInstruction.Opcode.PushBuiltin:
                case IGMInstruction.Opcode.PushImmediate:
                    {
                        if (parts.Length < 2)
                        {
                            throw new Exception("Push instruction needs data");
                        }
                        string data = parts[1];

                        switch (type1)
                        {
                            case IGMInstruction.DataType.Double:
                                if (!double.TryParse(data, out double dataDouble))
                                {
                                    throw new Exception("Invalid double");
                                }
                                instr.ValueDouble = dataDouble;
                                break;
                            case IGMInstruction.DataType.Int32:
                                if (!int.TryParse(data, out int dataInt32))
                                {
                                    if (data.StartsWith("[function]"))
                                    {
                                        // We're pushing a function index instead
                                        GMFunction function = new(data["[function]".Length..]);
                                        if (functions.TryGetValue(function, out GMFunction? existingFunction))
                                        {
                                            // We found a function that was already created
                                            instr.Function = existingFunction;
                                        }
                                        else
                                        {
                                            instr.Function = function;
                                        }
                                        break;
                                    }
                                    if (data.StartsWith("[variable]"))
                                    {
                                        // We're pushing a variable hash instead
                                        instr.Variable = new GMVariable(new GMString(data["[variable]".Length..]));
                                        break;
                                    }
                                    throw new Exception("Unknown push.i value");
                                }
                                instr.ValueInt = dataInt32;
                                break;
                            case IGMInstruction.DataType.Int64:
                                if (!long.TryParse(data, out long dataInt64))
                                {
                                    throw new Exception("Invalid int64");
                                }
                                instr.ValueLong = dataInt64;
                                break;
                            case IGMInstruction.DataType.Boolean:
                                if (!bool.TryParse(data, out bool dataBool))
                                {
                                    throw new Exception("Invalid boolean");
                                }
                                instr.ValueBool = dataBool;
                                break;
                            case IGMInstruction.DataType.Variable:
                                if (!ParseVariableFromString(data, variables, out var variable, out var varType, out var instType))
                                {
                                    throw new Exception($"Failed to parse variable {parts[1]}");
                                }
                                instr.Variable = variable;
                                instr.ReferenceVarType = varType;
                                instr.InstType = instType;
                                break;
                            case IGMInstruction.DataType.String:
                                string contents = line[(line.IndexOf('"') + 1)..line.LastIndexOf('"')];
                                instr.ValueString = new GMString(UnescapeStringContents(contents));
                                break;
                            case IGMInstruction.DataType.Int16:
                                if (!short.TryParse(data, out short dataInt16))
                                {
                                    throw new Exception("Invalid int16");
                                }
                                instr.ValueShort = dataInt16;
                                break;
                        }
                    }
                    break;
                case IGMInstruction.Opcode.Call:
                    {
                        if (parts.Length < 3)
                        {
                            throw new Exception("Call needs function and argument count");
                        }

                        GMFunction function = new(parts[1]);
                        if (functions.TryGetValue(function, out GMFunction? existingFunction))
                        {
                            // We found a function that was already created
                            instr.Function = existingFunction;
                        }
                        else
                        {
                            // This is a brand new function
                            instr.Function = function;
                            functions.Add(function);
                        }

                        if (!int.TryParse(parts[2], out int argCount))
                        {
                            throw new Exception("Failed to parse argument count");
                        }
                        instr.ArgumentCount = argCount;
                    }
                    break;
                case IGMInstruction.Opcode.Extended:
                    {
                        // Parse extended opcode type
                        if (!StringToExtOpcode.TryGetValue(opcodeStr, out IGMInstruction.ExtendedOpcode extOpcode))
                        {
                            throw new Exception($"Unexpected extended opcode \"{opcodeStr}\"");
                        }
                        instr.ExtKind = extOpcode;

                        // Parse additional arguments
                        if (extOpcode == IGMInstruction.ExtendedOpcode.PushReference)
                        {
                            if (parts.Length < 2)
                            {
                                throw new Exception("PushReference needs reference ID (or function)");
                            }
                            if (!int.TryParse(parts[1], out int referenceID))
                            {
                                // Not a reference ID. Instead, a function reference
                                instr.Function = new GMFunction(parts[1]);
                                break;
                            }
                            if (parts.Length < 3)
                            {
                                throw new Exception("PushReference needs reference ID and type parameters");
                            }
                            instr.AssetReferenceId = referenceID;
                            instr.AssetReferenceType = StringToAssetType[parts[2]];
                        }
                    }
                    break;
            }

            address += IGMInstruction.GetSize(instr);

            instructions.Add(instr);
        }

        // Resolve branch targets
        foreach ((string targetLabel, GMInstruction targetInstr) in branchTargets)
        {
            // Look up label
            if (!branchLabelAddresses.TryGetValue(targetLabel, out int labelAddress))
            {
                throw new Exception($"Did not find matching label for \"{targetLabel}\"");
            }

            targetInstr.BranchOffset = labelAddress - targetInstr.Address;
        }

        // Update code lengths
        root.Length = address;
        foreach (var child in root.Children)
        {
            child.Length = address;
        }

        return root;
    }

    private static bool ParseVariableFromString(
        string str, HashSet<GMVariable> variables, [MaybeNullWhen(false)] out GMVariable variable, 
        out IGMInstruction.VariableType varType, out IGMInstruction.InstanceType instType)
    {
        // Default data
        varType = IGMInstruction.VariableType.Normal;
        instType = IGMInstruction.InstanceType.Self;
        variable = null;

        // If too small, exit early
        if (str.Length < 2)
        {
            return false;
        }

        // Parse variable type
        if (str[0] == '[')
        {
            int closingBracket = str.IndexOf(']');
            if (closingBracket == -1)
            {
                return false;
            }

            string varTypeStr = str[1..closingBracket].ToLowerInvariant();
            varType = varTypeStr switch
            {
                "array" => IGMInstruction.VariableType.Array,
                "stacktop" => IGMInstruction.VariableType.StackTop,
                "normal" => IGMInstruction.VariableType.Normal,
                "instance" => IGMInstruction.VariableType.Instance,
                "multipush" => IGMInstruction.VariableType.MultiPush,
                "multipushpop" => IGMInstruction.VariableType.MultiPushPop,
                _ => throw new Exception("Unknown variable type")
            };

            // Get rid of this part of the variable string
            str = str[(closingBracket + 1)..];
        }

        // Parse instance type
        int dot = str.IndexOf('.');
        if (dot == -1)
        {
            return false;
        }
        string instTypeStr = str[..dot];
        if (short.TryParse(instTypeStr, out short instTypeObjectId))
        {
            instType = (IGMInstruction.InstanceType)instTypeObjectId;
        }
        else
        {
            instType = instTypeStr.ToLowerInvariant() switch
            {
                "self" => IGMInstruction.InstanceType.Self,
                "other" => IGMInstruction.InstanceType.Other,
                "all" => IGMInstruction.InstanceType.All,
                "global" => IGMInstruction.InstanceType.Global,
                "builtin" => IGMInstruction.InstanceType.Builtin,
                "local" => IGMInstruction.InstanceType.Local,
                "stacktop" => IGMInstruction.InstanceType.StackTop,
                "arg" or "argument" => IGMInstruction.InstanceType.Argument,
                "static" => IGMInstruction.InstanceType.Static,
                _ => throw new Exception("Unknown instance type")
            };
        }

        // Get actual variable name
        str = str[(dot + 1)..];

        // Update variable
        variable = new GMVariable(new GMString(str))
        {
            InstanceType = instType,    // TODO: does this match actual game behavior?
            VariableID = 0              // TODO: do we want to make actual IDs? probably not?
        };

        // Check existing variables - if this already exists, return existing variable
        if (variables.TryGetValue(variable, out GMVariable? existingVariable))
        {
            variable = existingVariable;
        }
        else
        {
            // This is a brand new variable
            variables.Add(variable);
        }

        return true;
    }

    private static string UnescapeStringContents(string str)
    {
        StringBuilder sb = new();

        bool escapeActive = false;
        foreach (char c in str)
        {
            if (escapeActive)
            {
                sb.Append(c switch
                {
                    // TODO: do we want any more escape sequences for tests?
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    '"' => '"',
                    '\\' => '\\',
                    _ => throw new Exception("Unrecognized escape sequence")
                });
                escapeActive = false;
                continue;
            }

            if (c == '\\')
            {
                escapeActive = true;
                continue;
            }

            sb.Append(c);
        }

        if (escapeActive)
        {
            throw new Exception("Invalid escape at end of string");
        }

        return sb.ToString();
    }

    private static int? ParseOptionalParameter(string str, string paramName)
    {
        int paramPos = str.IndexOf($"{paramName}=");
        if (paramPos != -1)
        {
            int digitStart = paramPos + $"{paramName}=".Length;
            int digitEnd = digitStart;
            while (digitEnd < str.Length)
            {
                if (!char.IsDigit(str[digitEnd]))
                {
                    break;
                }
                digitEnd++;
            }
            if (int.TryParse(str[digitStart..digitEnd], out int paramValue))
            {
                return paramValue;
            }
        }
        return null;
    }
}
