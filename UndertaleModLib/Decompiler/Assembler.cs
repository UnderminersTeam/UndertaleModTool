using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UndertaleModLib.Models;

namespace UndertaleModLib.Decompiler;

public static partial class Assembler
{
    /// <summary>
    /// Lookup of extended (break) instruction IDs to their mnemonics.
    /// </summary>
    internal static readonly Dictionary<short, string> ExtendedIDToName = new()
    {
        { -1,  "chkindex" },
        { -2,  "pushaf" },
        { -3,  "popaf" },
        { -4,  "pushac" },
        { -5,  "setowner" },
        { -6,  "isstaticok" },
        { -7,  "setstatic" },
        { -8,  "savearef" },
        { -9,  "restorearef" },
        { -10, "chknullish" },
        { -11, "pushref" }
    };

    /// <summary>
    /// Lookup of extended (break) instruction mnemonics to their IDs.
    /// </summary>
    internal static readonly Dictionary<string, short> NameToExtendedID = new()
    {
        { "chkindex", -1 },
        { "pushaf", -2 },
        { "popaf", -3 },
        { "pushac", -4 },
        { "setowner", -5 },
        { "isstaticok", -6 },
        { "setstatic", -7 },
        { "savearef", -8 },
        { "restorearef", -9 },
        { "chknullish", -10 },
        { "pushref", -11 }
    };

    // Regex for parsing call instruction names and argument counts
    [GeneratedRegex(@"^(.+)\(argc=([0-9]+)\)$", RegexOptions.Compiled)]
    private static partial Regex callInstrRegex();

    // Regex for parsing code entry local/argument counts
    [GeneratedRegex(@"^\(locals=([0-9]+)\,\s*argc=([0-9]+)\)$", RegexOptions.Compiled)]
    private static partial Regex codeEntryRegex();

    /// <summary>
    /// Assembles a single <see cref="UndertaleInstruction"/>, with the provided data, and possibly local variables. Labels are not allowed.
    /// </summary>
    public static UndertaleInstruction AssembleOne(string source, UndertaleData data, Dictionary<string, UndertaleVariable> localvars = null)
    {
        UndertaleInstruction instr = AssembleOne(source, data, localvars, out string label);
        if (label is not null)
        {
            throw new Exception("Cannot use labels in this context");
        }
        return instr;
    }

    /// <summary>
    /// Assembles a single <see cref="UndertaleInstruction"/>, with the provided data, and possibly local variables. Labels are not allowed.
    /// </summary>
    public static UndertaleInstruction AssembleOne(string source, UndertaleData data, Dictionary<string, UndertaleVariable> localvars, out string label)
    {
        // Default label output to null
        label = null;

        // Remove comments from end of line
        string line = source.Split(" ;;; ", 2)[0];

        // Split apart opcode and data types
        string opcode = line;
        int space = opcode.IndexOf(' ', StringComparison.InvariantCulture);
        if (space >= 0)
        {
            opcode = line[..space];
            line = line[(space + 1)..].Trim();
        }
        else
        {
            line = "";
        }
        string[] types = opcode.Split('.');
        if (types.Length > 3)
        {
            throw new Exception("Too many type parameters");
        }

        // Start creating instruction with opcode and types
        string kind = types[0];
        UndertaleInstruction instr = new();
        if (NameToExtendedID.TryGetValue(kind.ToLower(CultureInfo.InvariantCulture), out short extendedId))
        {
            // Extended opcode, which uses Break under the hood
            instr.Kind = UndertaleInstruction.Opcode.Break;
        }
        else
        {
            // Regular opcode, just use the normal enum
            instr.Kind = Enum.Parse<UndertaleInstruction.Opcode>(kind, true);
        }
        if (types.Length >= 2)
        {
            // Parse first type
            instr.Type1 = UndertaleInstructionUtil.FromOpcodeParam(types[1]);
        }
        if (types.Length >= 3)
        {
            // Parse second type
            instr.Type2 = UndertaleInstructionUtil.FromOpcodeParam(types[2]);
        }

        // Parse depending on overarching instruction type
        switch (UndertaleInstruction.GetInstructionType(instr.Kind))
        {
            case UndertaleInstruction.InstructionType.SingleTypeInstruction:
                // Single type instructions generally don't have anything, except for dup and callv
                if (instr.Kind is UndertaleInstruction.Opcode.Dup or UndertaleInstruction.Opcode.CallV)
                {
                    // Dup instructions have the additional possibility of being in "dup swap" mode, which has an extra parameter
                    if (instr.Kind is UndertaleInstruction.Opcode.Dup)
                    {
                        space = line.IndexOf(' ', StringComparison.InvariantCulture);
                        if (space >= 0)
                        {
                            byte spec = byte.Parse(line[(space + 1)..].Trim());
                            instr.ComparisonKind = (UndertaleInstruction.ComparisonType)(spec | 0x80);
                            line = line[..space];
                        }
                    }   

                    // Parse regular (first) parameter
                    instr.Extra = byte.Parse(line);
                    line = "";
                }
                break;

            case UndertaleInstruction.InstructionType.DoubleTypeInstruction:
                // Double type instructions never have anything extra to parse
                break;

            case UndertaleInstruction.InstructionType.ComparisonInstruction:
                // Comparison instructions always have a comparison kind to parse
                instr.ComparisonKind = Enum.Parse<UndertaleInstruction.ComparisonType>(line, true);
                line = "";
                break;

            case UndertaleInstruction.InstructionType.GotoInstruction:
                // Goto (jump/branch) instructions have a few formatting options...
                if (line[0] == '$')
                {
                    // Old-style formatting, using a direct offset
                    instr.JumpOffset = int.Parse(line[1..]);
                }
                else if (line == "<drop>")
                {
                    // Special case for popenv instructions, signifying a break out of a with() loop
                    instr.JumpOffsetPopenvExitMagic = true;
                    instr.JumpOffset = 0xF00000;
                }
                else if (line[0] == '[' && line[^1] == ']')
                {
                    // New-style formatting, using a block label (extract the name)
                    label = line[1..^1];
                }
                else
                {
                    // Nothing was valid
                    throw new Exception($"Unknown goto target \"{line}\"");
                }
                
                line = "";
                break;

            case UndertaleInstruction.InstructionType.PopInstruction:
                // Pop instruction, which has one special case for pop swap
                if (instr.Type1 == UndertaleInstruction.DataType.Int16)
                {
                    // Pop swap instruction (see #129)
                    instr.SwapExtra = byte.Parse(line);
                }
                else
                {
                    // Regular variable pop; parse reference
                    UndertaleInstruction.InstanceType inst = instr.TypeInst;
                    (instr.ValueVariable, instr.ReferenceType) = ParseVariableReference(line, data, localvars, ref inst);
                    instr.TypeInst = inst;
                }

                line = "";
                break;

            case UndertaleInstruction.InstructionType.PushInstruction:
                // Push instruction, parsing depends on data type being pushed
                switch (instr.Type1)
                {
                    case UndertaleInstruction.DataType.Double:
                        // Simple 64-bit float
                        instr.ValueDouble = double.Parse(line, CultureInfo.InvariantCulture);
                        break;
                    case UndertaleInstruction.DataType.Int32:
                        // Either a simple 32-bit integer, or a reference to something...
                        if (int.TryParse(line, out int ival))
                        {
                            // Simple integer
                            instr.ValueInt = ival;
                        }
                        else if (line.StartsWith("[variable]", StringComparison.Ordinal))
                        {
                            // Variable reference
                            line = line["[variable]".Length..];
                            instr.ValueVariable = data.Variables.EnsureDefined(
                                data.Strings.MakeString(line, out int nameStringId), nameStringId,
                                UndertaleInstruction.InstanceType.Self, false, data);
                        }
                        else if (line.StartsWith("[function]", StringComparison.Ordinal))
                        {
                            // Function reference
                            line = line["[function]".Length..];
                            instr.ValueFunction = data.Functions.ByName(line);
                        }
                        else if (data.Functions.ByName(line) is UndertaleFunction f)
                        {
                            // Function reference (old-style syntax)
                            instr.ValueFunction = f;
                        }
                        else
                        {
                            // Resource name (for convenience in assembly editing older GM games that don't use pushref)
                            instr.ValueInt = ParseResourceName(line, data);
                        }
                        break;
                    case UndertaleInstruction.DataType.Int64:
                        // Either a simple 64-bit integer, or a resource name for convenience
                        if (long.TryParse(line, out long lval))
                        {
                            instr.ValueLong = lval;
                        }
                        else
                        {
                            instr.ValueLong = ParseResourceName(line, data);
                        }
                        break;
                    case UndertaleInstruction.DataType.Variable:
                        // Simple variable reference
                        UndertaleInstruction.InstanceType inst2 = instr.TypeInst;
                        (instr.ValueVariable, instr.ReferenceType) = ParseVariableReference(line, data, localvars, ref inst2);
                        instr.TypeInst = inst2;
                        break;
                    case UndertaleInstruction.DataType.String:
                        // Simple string reference
                        instr.ValueString = ParseStringReference(line, data.Strings);
                        break;
                    case UndertaleInstruction.DataType.Int16:
                        // Simple 16-bit integer, or a resource name for convenience
                        if (short.TryParse(line, out short sval))
                        {
                            instr.ValueShort = sval;
                        }
                        else
                        {
                            instr.ValueShort = (short)ParseResourceName(line, data);
                        }
                        break;
                    default:
                        // Invalid (or unused) data type
                        throw new Exception($"Invalid push data type {instr.Type1}");
                }

                line = "";
                break;

            case UndertaleInstruction.InstructionType.CallInstruction:
                // Call instructions - match function name and argument count using a regular expression
                Match match = callInstrRegex().Match(line);
                if (!match.Success)
                {
                    throw new Exception("Call instruction format error; should be formatted like my_func_name(argc=3)");
                }

                // Find function being referenced
                string funcName = match.Groups[1].Value;
                UndertaleFunction func = data.Functions.ByName(funcName) ?? 
                    throw new Exception($"Could not find function with name \"{funcName}\"");
                instr.ValueFunction = func;

                // Parse argument count
                instr.ArgumentsCount = ushort.Parse(match.Groups[2].Value);

                line = "";
                break;

            case UndertaleInstruction.InstructionType.BreakInstruction:
                // Break instruction - could be a regular break (with an additional extended kind), or an extended opcode that we already determined
                if (extendedId != 0)
                {
                    // Use predetermined ID
                    instr.ExtendedKind = extendedId;

                    // For pushref (push reference) instructions, additional data is needed for its referenced asset
                    if (extendedId == -11)
                    {
                        // Parse additional int argument
                        if (int.TryParse(line, out int intArgument))
                        {
                            instr.IntArgument = intArgument;
                        }
                        else
                        {
                            // Or alternatively parse function!
                            UndertaleFunction extFunc = data.Functions.ByName(line) ?? 
                                throw new Exception($"Could not find function specified by extended pushref instruction: \"{line}\"");
                            instr.ValueFunction = extFunc;
                        }
                    }
                }
                else
                {
                    // Old-style break syntax (not recommended to be used, especially for pushref)
                    instr.ExtendedKind = short.Parse(line);
                }

                line = "";
                break;
        }

        // Make sure there's no remaining line
        if (line != "")
        {
            throw new Exception($"Expected end of line; found remaining string: \"{line}\"");
        }

        return instr;
    }

    /// <summary>
    /// Parses a resource name, using the given data.
    /// </summary>
    private static int ParseResourceName(string line, UndertaleData data)
    {
        // TODO: have the option of building lookup maps instead of performing this linear search...
        int id = data.IndexOfByName(line);
        if (id < 0)
        {
            throw new FormatException($"Unable to parse \"{line}\" as a number or resource name");
        }
        return id;
    }

    /// <summary>
    /// Assembles many instructions, separated by newlines, using the provided data.
    /// </summary>
    public static List<UndertaleInstruction> Assemble(string source, UndertaleData data)
    {
        // Initialize structures
        Dictionary<string, uint> labels = new();
        List<(UndertaleInstruction Instruction, uint InstructionAddress, string Label)> labelTargets = new();
        List<UndertaleInstruction> instructions = new(16);
        Dictionary<string, UndertaleVariable> localvars = new();

        // Start reading instructions
        uint address = 0;
        StringReader strReader = new(source);
        string fullLine;
        while ((fullLine = strReader.ReadLine()) is not null)
        {
            // Trim line, and skip if it's empty or a comment
            string line = fullLine.Trim();
            if (line.Length == 0 || line[0] == ';')
            {
                continue;
            }

            // Handle sub-code entries
            if (line[0] == '>')
            {
                // Parse sub-code entry name, and make sure it exists
                line = line[2..].Trim();
                int space = line.IndexOf(' ', StringComparison.InvariantCulture);
                string codeName = line[..space];
                UndertaleCode code = data.Code.ByName(codeName) ?? 
                    throw new Exception($"Failed to find code entry with name \"{codeName}\"");

                // Parse additional info (local/argument count), using a regular expression
                string info = line[(space + 1)..];
                Match match = codeEntryRegex().Match(info);
                if (!match.Success)
                {
                    throw new Exception("Sub-code entry format error; should be formatted like \"> gml_Script_some_script (locals=6, argc=7)\"");
                }

                // Update info on the code entry
                code.LocalsCount = ushort.Parse(match.Groups[1].Value);
                code.ArgumentsCount = ushort.Parse(match.Groups[2].Value);
                code.Offset = address * 4;
                continue;
            }

            // Handle block labels
            if (line[0] == ':' && line.Length >= 3 && line[1] == '[')
            {
                // Extract label name
                string label = line[2..line.IndexOf(']', StringComparison.InvariantCulture)];

                // Make sure label name isn't invalid or a duplicate
                if (string.IsNullOrEmpty(label))
                {
                    throw new Exception("Invalid block label syntax");
                }
                if (labels.ContainsKey(label))
                {
                    throw new Exception($"Duplicate label: \"label\"");
                }

                // Register label for later resolving and parsing
                labels.Add(label, address);
                continue;
            }

            // Handle assembler directives
            if (line[0] == '.')
            {
                string[] parts = line.Split(' ');
                if (parts[0] == ".localvar")
                {
                    // Local variable definition
                    if (parts.Length >= 4)
                    {
                        // Find variable using its ID, verify it's a local variable, and add it to structure
                        UndertaleVariable variable = data.Variables[int.Parse(parts[3])];
                        if (data.GeneralInfo?.BytecodeVersion >= 15 && variable.InstanceType != UndertaleInstruction.InstanceType.Local)
                        {
                            throw new Exception($"Variable with index {parts[3]} actually has instance type {variable.InstanceType} instead of Local");
                        }
                        if (variable.Name.Content != parts[2])
                        {
                            throw new Exception($"Variable with index {parts[3]} actually has name {variable.Name} instead of the specified name \"{parts[2]}\"");
                        }
                        localvars.Add(parts[2], variable);

                        // TODO: this does not update the CodeLocals entry!
                    }
                }
                else
                {
                    throw new Exception($"Unknown assembler directive: \"{parts[0]}\"");
                }
                continue;
            }

            // Assemble individual instruction
            UndertaleInstruction instr = AssembleOne(line, data, localvars, out string labelTarget);
            if (labelTarget is not null)
            {
                // Instruction references a label. Track it for later resolving
                labelTargets.Add((instr, address, labelTarget));
            }

            // Add instruction to list
            instructions.Add(instr);
            address += instr.CalculateInstructionSize();
        }
        
        // Resolve jump offsets for instructions that reference labels
        foreach ((UndertaleInstruction instr, uint instrAddress, string label) in labelTargets)
        {
            instr.JumpOffset = (int)labels[label] - (int)instrAddress;
        }

        return instructions;
    }

    /// <summary>
    /// Parses a string reference in assembly, using the given string list (may create new strings).
    /// </summary>
    private static UndertaleResourceById<UndertaleString, UndertaleChunkSTRG> ParseStringReference(string line, IList<UndertaleString> strg)
    {
        // Parse ID at the end of the string, if given
        string str = line;
        int at = str.LastIndexOf('@');
        int id = -1;
        if (at >= 0)
        {
            // First make certain that this is actually an ID, not part of the string content
            if ((at - 1) == str.LastIndexOf('"'))
            {
                id = int.Parse(str[(at + 1)..]);
                str = str[..at];
            }
        }

        // Parse string contents
        if (!string.IsNullOrEmpty(str))
        {
            if (str[0] != '"' || str[^1] != '"')
            {
                throw new Exception("Bad string format");
            }
            str = UndertaleString.UnescapeText(str[1..^1]);
        }
        else
        {
            str = null;
        }

        // Get existing string object using ID
        UndertaleString strobj = (id >= 0) ? strg[id] : null;
        if (strobj is not null)
        {
            // Update string contents, or retain original value if empty string passed (e.g. "push.s @300")
            if (str is not null)
            {
                strobj.Content = str;
            }
        }
        else
        {
            // New string needs to be created
            strobj = strg.MakeString(str, out int newId);
            id = newId;
        }

        return new UndertaleResourceById<UndertaleString, UndertaleChunkSTRG>() { Resource = strobj, CachedId = id };
    }

    private static (UndertaleVariable Variable, UndertaleInstruction.VariableType ReferenceType) ParseVariableReference(
        string line, UndertaleData data, Dictionary<string, UndertaleVariable> localvars, ref UndertaleInstruction.InstanceType instance)
    {
        ReadOnlySpan<char> str = line.AsSpan();
        int strPosition = 0;

        // Variable type, and instance type as stored in VARI chunk, adjusted based on context
        UndertaleInstruction.VariableType type = UndertaleInstruction.VariableType.Normal;
        UndertaleInstruction.InstanceType variInstanceType = instance;

        // Parse instance type, if at the beginning
        if (str[strPosition] != '[')
        {
            // Read up until first dot character
            int instanceTypeDot = str.IndexOf('.');
            if (instanceTypeDot >= 0)
            {
                ReadOnlySpan<char> instanceTypeStr = str[..instanceTypeDot];
                if (short.TryParse(instanceTypeStr, out short instNum))
                {
                    // This is a valid 16-bit integer, probably an object or room instance ID
                    instance = (UndertaleInstruction.InstanceType)instNum;
                }
                else
                {
                    // Otherwise, this should always be one of the valid instance type enum values
                    instance = Enum.Parse<UndertaleInstruction.InstanceType>(instanceTypeStr, true);
                }
            }
            else
            {
                // Instance type is missing seemingly, so just use undefined
                instance = UndertaleInstruction.InstanceType.Undefined;
            }

            // Adjust VARI instance type based on existing type
            variInstanceType = instance switch
            {
                >= 0                                        => UndertaleInstruction.InstanceType.Self,
                UndertaleInstruction.InstanceType.Other     => UndertaleInstruction.InstanceType.Self,
                UndertaleInstruction.InstanceType.Arg       => UndertaleInstruction.InstanceType.Builtin,
                UndertaleInstruction.InstanceType.Builtin   => UndertaleInstruction.InstanceType.Self,      // used with @@This@@
                UndertaleInstruction.InstanceType.Stacktop  => UndertaleInstruction.InstanceType.Self,      // used with @@GetInstance@@
                _                                           => instance
            };

            // Set up for parsing after the dot
            strPosition = instanceTypeDot + 1;
        }
        
        // Parse variable type, if present here, as well as the alternate location of the instance type, if present (directly after it)
        if (strPosition < str.Length && str[strPosition] == '[')
        {
            // Read up until closing bracket character
            int variableTypeEnd = str[(strPosition + 1)..].IndexOf(']') + (strPosition + 1);
            if (variableTypeEnd < (strPosition + 1))
            {
                // Invalid formatting, objectively
                throw new Exception("Missing ']' character in variable reference");
            }

            // Variable type should always be one of the enum values
            ReadOnlySpan<char> variableTypeStr = str[(strPosition + 1)..variableTypeEnd];
            type = Enum.Parse<UndertaleInstruction.VariableType>(variableTypeStr, true);

            // Parse instance type, if present
            int instanceTypeDot = str[(variableTypeEnd + 1)..].IndexOf('.') + (variableTypeEnd + 1);
            if (instanceTypeDot >= (variableTypeEnd + 1))
            {
                // This instance type should always be one of the enum values
                ReadOnlySpan<char> instanceTypeStr = str[(variableTypeEnd + 1)..instanceTypeDot];
                variInstanceType = Enum.Parse<UndertaleInstruction.InstanceType>(instanceTypeStr, true);

                // Set up parsing after the dot
                strPosition = instanceTypeDot + 1;
            }
            else
            {
                // Older versions of the assembly syntax did not print out instance types for array/stacktop references, which loses info in GMS 2.3+
                if (type == UndertaleInstruction.VariableType.Array ||
                    type == UndertaleInstruction.VariableType.StackTop)
                {
                    throw new Exception("Old instruction format is incompatible (missing instance type in array or stacktop)");
                }

                // Adjust VARI instance type based on existing type
                if (variInstanceType >= 0)
                {
                    variInstanceType = UndertaleInstruction.InstanceType.Self;
                }
                else if (variInstanceType == UndertaleInstruction.InstanceType.Other)
                {
                    variInstanceType = UndertaleInstruction.InstanceType.Self;
                }

                // Set up parsing after the variable type's closing bracket
                strPosition = variableTypeEnd + 1;
            }
        }

        // In older versions, VARI does not assign instance types properly, so account for that
        if (data.GeneralInfo?.BytecodeVersion <= 14)
        {
            variInstanceType = UndertaleInstruction.InstanceType.Undefined;
        }

        // Locate variable from either local variables, or VARI chunk
        UndertaleVariable locatedVariable;
        string variableName = str[strPosition..].ToString();
        if (variInstanceType == UndertaleInstruction.InstanceType.Local && data.CodeLocals is not null)
        {
            locatedVariable = localvars.GetValueOrDefault(variableName);
        }
        else
        {
            locatedVariable = data.Variables.FirstOrDefault(var => var.Name.Content == variableName && var.InstanceType == variInstanceType);
        }

        // If nothing is found, throw an error, as we cannot properly assemble it
        if (locatedVariable is null)
        {
            throw new Exception($"Failed to find existing variable: {variInstanceType.ToString().ToLower(CultureInfo.InvariantCulture)}.{variableName}");
        }

        // Return reference to be used in instruction
        return (locatedVariable, type);
    }
}
