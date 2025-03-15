using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UndertaleModLib.Models;

namespace UndertaleModLib.Decompiler
{
    public static class Assembler
    {
        public static Dictionary<short, string> BreakIDToName = new Dictionary<short, string>()
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
        public static Dictionary<string, short> NameToBreakID = new Dictionary<string, short>()
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

        private static readonly Regex callInstrRegex = new(@"^(.*)\(argc=(.*)\)$", RegexOptions.Compiled);
        private static readonly Regex codeEntryRegex = new(@"^\(locals=(.*)\,\s*argc=(.*)\)$", RegexOptions.Compiled);

        // TODO: Improve the error messages

        public static UndertaleInstruction AssembleOne(string source, IList<UndertaleFunction> funcs, IList<UndertaleVariable> vars, IList<UndertaleString> strg, Dictionary<string, UndertaleVariable> localvars = null, UndertaleData data = null)
        {
            string label;
            UndertaleInstruction instr = AssembleOne(source, funcs, vars, strg, localvars, out label, data);
            if (label != null)
                throw new Exception("Cannot use labels in this context");
            return instr;
        }

        public static UndertaleInstruction AssembleOne(string source, UndertaleData data, Dictionary<string, UndertaleVariable> localvars = null)
        {
            return AssembleOne(source, data.Functions, data.Variables, data.Strings, localvars, data);
        }

        public static UndertaleInstruction AssembleOne(string source, IList<UndertaleFunction> funcs, IList<UndertaleVariable> vars, IList<UndertaleString> strg, Dictionary<string, UndertaleVariable> localvars, out string label, UndertaleData data = null)
        {
            label = null;
            string line = source;
            UndertaleInstruction instr = new UndertaleInstruction();
            line = line.Split(" ;;; ", 2)[0]; // remove comments

            string opcode = line;
            int space = opcode.IndexOf(' ', StringComparison.InvariantCulture);
            if (space >= 0)
            {
                opcode = line.Substring(0, space);
                line = line.Substring(space + 1).Trim();
            }
            else
                line = "";
            string[] types = opcode.Split('.');
            if (types.Length > 3)
                throw new Exception("Too many type parameters");

            string kind = types[0];
            short breakId = 0;
            if (NameToBreakID.TryGetValue(kind.ToLower(CultureInfo.InvariantCulture), out breakId))
                instr.Kind = UndertaleInstruction.Opcode.Break;
            else
                instr.Kind = (UndertaleInstruction.Opcode)Enum.Parse(typeof(UndertaleInstruction.Opcode), kind, true);
            if (types.Length >= 2)
                instr.Type1 = UndertaleInstructionUtil.FromOpcodeParam(types[1]);
            if (types.Length >= 3)
                instr.Type2 = UndertaleInstructionUtil.FromOpcodeParam(types[2]);

            switch (UndertaleInstruction.GetInstructionType(instr.Kind))
            {
                case UndertaleInstruction.InstructionType.SingleTypeInstruction:
                    if (instr.Kind == UndertaleInstruction.Opcode.Dup || instr.Kind == UndertaleInstruction.Opcode.CallV)
                    {
                        if (instr.Kind == UndertaleInstruction.Opcode.Dup)
                        {
                            space = line.IndexOf(' ', StringComparison.InvariantCulture);
                            if (space >= 0)
                            {
                                byte spec = Byte.Parse(line.Substring(space + 1).Trim());
                                instr.ComparisonKind = (UndertaleInstruction.ComparisonType)(spec | 0x80);
                                line = line.Substring(0, space);
                            }
                        }
                        instr.Extra = Byte.Parse(line);
                        line = "";
                    }
                    break;

                case UndertaleInstruction.InstructionType.DoubleTypeInstruction:
                    break;

                case UndertaleInstruction.InstructionType.ComparisonInstruction:
                    instr.ComparisonKind = (UndertaleInstruction.ComparisonType)Enum.Parse(typeof(UndertaleInstruction.ComparisonType), line, true);
                    line = "";
                    break;

                case UndertaleInstruction.InstructionType.GotoInstruction:
                    if (line[0] == '$')
                    {
                        instr.JumpOffset = Int32.Parse(line.Substring(1));
                    }
                    else
                    {
                        if (line == "<drop>")
                        {
                            instr.JumpOffsetPopenvExitMagic = true;
                            instr.JumpOffset = 0xF00000;
                        }
                        else if (line[0] == '[' && line[^1] == ']')
                            label = line.Substring(1, line.Length - 2);
                        else
                            throw new Exception("Unknown goto target");
                    }
                    line = "";
                    break;

                case UndertaleInstruction.InstructionType.PopInstruction:
                    if (instr.Type1 == UndertaleInstruction.DataType.Int16)
                    {
                        // Special scenario - the swap instruction
                        // TODO: Figure out the proper syntax, see #129
                        instr.SwapExtra = Byte.Parse(line);
                    }
                    else
                    {
                        UndertaleInstruction.InstanceType inst = instr.TypeInst;
                        instr.ValueVariable = ParseVariableReference(line, vars, localvars, ref inst, instr, data);
                        instr.TypeInst = inst;
                    }
                    line = "";
                    break;

                case UndertaleInstruction.InstructionType.PushInstruction:
                    switch (instr.Type1)
                    {
                        case UndertaleInstruction.DataType.Double:
                            instr.ValueDouble = double.Parse(line, CultureInfo.InvariantCulture);
                            break;
                        case UndertaleInstruction.DataType.Int32:
                            if (int.TryParse(line, out int ival))
                            {
                                instr.ValueInt = ival;
                            }
                            else
                            {
                                if (line.StartsWith("[variable]", StringComparison.Ordinal))
                                {
                                    line = line["[variable]".Length..];
                                    instr.ValueVariable = new UndertaleInstruction.Reference<UndertaleVariable>(data.Variables.EnsureDefined(
                                        data.Strings.MakeString(line, out int nameStringId), nameStringId,
                                        UndertaleInstruction.InstanceType.Self, false, data));
                                }
                                else if (line.StartsWith("[function]", StringComparison.Ordinal))
                                {
                                    line = line["[function]".Length..];
                                    instr.ValueFunction = new UndertaleInstruction.Reference<UndertaleFunction>(data.Functions.ByName(line));
                                }
                                else
                                {
                                    if (data.Functions.ByName(line) is UndertaleFunction f)
                                    {
                                        instr.ValueFunction = new UndertaleInstruction.Reference<UndertaleFunction>(f);
                                    }
                                    else
                                    {
                                        instr.ValueInt = ParseResourceName(line, data);
                                    }
                                }
                            }
                            break;
                        case UndertaleInstruction.DataType.Int64:
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
                            UndertaleInstruction.InstanceType inst2 = instr.TypeInst;
                            instr.ValueVariable = ParseVariableReference(line, vars, localvars, ref inst2, instr, data);
                            instr.TypeInst = inst2;
                            break;
                        case UndertaleInstruction.DataType.String:
                            instr.ValueString = ParseStringReference(line, strg);
                            break;
                        case UndertaleInstruction.DataType.Int16:
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
                            throw new Exception("Invalid push data type");
                    }
                    line = "";
                    break;

                case UndertaleInstruction.InstructionType.CallInstruction:
                    Match match = callInstrRegex.Match(line);
                    if (!match.Success)
                        throw new Exception("Call instruction format error");

                    UndertaleFunction func = funcs.ByName(match.Groups[1].Value);
                    if (func == null)
                        throw new Exception("Function not found: " + match.Groups[1].Value);
                    instr.ValueFunction = new UndertaleInstruction.Reference<UndertaleFunction>() { Target = func };
                    instr.ArgumentsCount = UInt16.Parse(match.Groups[2].Value);
                    line = "";
                    break;

                case UndertaleInstruction.InstructionType.BreakInstruction:
                    if (breakId != 0)
                    {
                        instr.ExtendedKind = breakId;
                        if (breakId == -11) // pushref
                        {
                            // Parse additional int argument
                            if (Int32.TryParse(line, out int intArgument))
                            {
                                instr.IntArgument = intArgument;
                            }
                            else
                            {
                                // Or alternatively parse function!
                                var f = data.Functions.ByName(line);
                                if (f == null)
                                    throw new Exception("Function in pushref not found: " + line);
                                instr.ValueFunction = new UndertaleInstruction.Reference<UndertaleFunction>(f);
                            }
                        }
                    }
                    else
                        instr.ExtendedKind = Int16.Parse(line);
                    line = "";
                    break;
            }
            if (line != "")
                throw new Exception("Excess parameters");
            return instr;
        }

        private static int ParseResourceName(string line, UndertaleData data)
        {
            if (data != null)
            {
                UndertaleNamedResource byName = data.ByName(line);
                if (byName == null)
                    throw new FormatException("Could not locate resource named '" + line + "'.");

                int id = data.IndexOf(byName);
                if (id >= 0)
                    return id;
            }
            throw new FormatException("Unable to parse " + line + " as number or resource name");
        }

        public static List<UndertaleInstruction> Assemble(string source, IList<UndertaleFunction> funcs, IList<UndertaleVariable> vars, IList<UndertaleString> strg, UndertaleData data = null)
        {
            StringReader strReader = new(source);
            uint addr = 0;
            Dictionary<string, uint> labels = new Dictionary<string, uint>();
            Dictionary<UndertaleInstruction, string> labelTargets = new Dictionary<UndertaleInstruction, string>();
            List<UndertaleInstruction> instructions = new List<UndertaleInstruction>();
            Dictionary<string, UndertaleVariable> localvars = new Dictionary<string, UndertaleVariable>();
            string fullLine;
            while ((fullLine = strReader.ReadLine()) is not null)
            {
                string line = fullLine;
                if (line.Length == 0)
                    continue;

                if (line[0] == ';')
                    continue;
                if (line[0] == '>')
                {
                    // Code entry inside of this one
                    line = line.Substring(2, line.Length - 2).Trim();
                    int space = line.IndexOf(' ', StringComparison.InvariantCulture);
                    string codeName = line.Substring(0, space);
                    var code = data.Code.ByName(codeName);
                    if (code == null)
                        throw new Exception($"Failed to find code entry with name \"{codeName}\".");
                    string info = line.Substring(space + 1);

                    Match match = codeEntryRegex.Match(info);
                    if (!match.Success)
                        throw new Exception("Sub-code entry format error");
                    code.LocalsCount = ushort.Parse(match.Groups[1].Value);
                    code.ArgumentsCount = ushort.Parse(match.Groups[2].Value);
                    code.Offset = addr * 4;
                    continue;
                }
                if (line[0] == ':' && line.Length >= 3)
                {
                    if (line[1] == '[')
                    {
                        string label = line.Substring(2, line.IndexOf(']', StringComparison.InvariantCulture) - 2);

                        if (!string.IsNullOrEmpty(label))
                        {
                            if (labels.ContainsKey(label))
                                throw new Exception("Duplicate label: " + label);
                            labels.Add(label, addr);
                        }

                        continue;
                    }
                }

                if (line[0] == '.')
                {
                    // Assembler directive
                    // TODO: Does not update the CodeLocals block yet!!
                    string[] aaa = line.Split(' ');
                    if (aaa[0] == ".localvar")
                    {
                        if (aaa.Length >= 4)
                        {
                            var varii = vars[Int32.Parse(aaa[3])];
                            if (data?.GeneralInfo?.BytecodeVersion >= 15 && varii.InstanceType != UndertaleInstruction.InstanceType.Local)
                                throw new Exception("Not a local var");
                            if (varii.Name.Content != aaa[2])
                                throw new Exception("Name mismatch");
                            localvars.Add(aaa[2], varii);
                        }
                    }
                    else
                    {
                        throw new Exception("Unknown assembler directive: " + aaa[0]);
                    }
                    continue;
                }

                string labelTgt;
                UndertaleInstruction instr = AssembleOne(line, funcs, vars, strg, localvars, out labelTgt, data);
                instr.Address = addr;
                if (labelTgt != null)
                    labelTargets.Add(instr, labelTgt);

                instructions.Add(instr);

                addr += instr.CalculateInstructionSize();
            }
            foreach (var pair in labelTargets)
            {
                pair.Key.JumpOffset = (int)labels[pair.Value] - (int)pair.Key.Address;
            }
            return instructions;
        }

        public static List<UndertaleInstruction> Assemble(string source, UndertaleData data)
        {
            return Assemble(source, data.Functions, data.Variables, data.Strings, data);
        }

        private static UndertaleResourceById<UndertaleString, UndertaleChunkSTRG> ParseStringReference(string line, IList<UndertaleString> strg)
        {
            string str = line;
            int at = str.LastIndexOf('@');
            uint? id = null;
            if (at >= 0)
            {
                // First make certain that this is actually an ID, not part of the string content
                if ((at - 1) == str.LastIndexOf('"'))
                {
                    if (str.Substring(at + 1) != "-1") // TODO
                        id = UInt32.Parse(str.Substring(at + 1));
                    str = str.Substring(0, at);
                }
            }
            if (!String.IsNullOrEmpty(str))
            {
                if (str[0] != '"' || str[str.Length - 1] != '"')
                    throw new Exception("Bad string format");
                str = UndertaleString.UnescapeText(str.Substring(1, str.Length - 2));
            }
            else
                str = null;
            UndertaleString strobj = id.HasValue ? strg[(int)id.Value] : null;
            if (strobj != null)
            {
                if (str != null) // Retain original value if empty string passed (push.s @300)
                    strobj.Content = str;
            }
            else
                strobj = strg.MakeString(str);
            if (!id.HasValue)
                id = (uint)strg.IndexOf(strobj);
            return new UndertaleResourceById<UndertaleString, UndertaleChunkSTRG>() { Resource = strobj, CachedId = (int)id.Value };
        }

        private static UndertaleInstruction.Reference<UndertaleVariable> ParseVariableReference(string line, IList<UndertaleVariable> vars, Dictionary<string, UndertaleVariable> localvars, ref UndertaleInstruction.InstanceType instance, UndertaleInstruction instr, UndertaleData data = null)
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
                        instance = (UndertaleInstruction.InstanceType)Enum.Parse(typeof(UndertaleInstruction.InstanceType), instanceTypeStr, true);
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
                if (variableTypeEnd >= (strPosition + 1))
                {
                    // Variable type should always be one of the enum values
                    ReadOnlySpan<char> variableTypeStr = str[(strPosition + 1)..variableTypeEnd];
                    type = (UndertaleInstruction.VariableType)Enum.Parse(typeof(UndertaleInstruction.VariableType), variableTypeStr, true);

                    // Parse instance type, if present
                    int instanceTypeDot = str[(variableTypeEnd + 1)..].IndexOf('.') + (variableTypeEnd + 1);
                    if (instanceTypeDot >= (variableTypeEnd + 1))
                    {
                        // This instance type should always be one of the enum values
                        ReadOnlySpan<char> instanceTypeStr = str[(variableTypeEnd + 1)..instanceTypeDot];
                        variInstanceType = (UndertaleInstruction.InstanceType)Enum.Parse(typeof(UndertaleInstruction.InstanceType), instanceTypeStr, true);

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
                else
                {
                    // Invalid formatting, objectively
                    throw new Exception("Missing ']' character in variable reference");
                }
            }

            // In older versions, VARI does not assign instance types properly, so account for that
            if (data?.GeneralInfo?.BytecodeVersion <= 14)
            {
                variInstanceType = UndertaleInstruction.InstanceType.Undefined;
            }

            // Locate variable from either local variables, or VARI chunk
            UndertaleVariable locatedVariable;
            string variableName = str[strPosition..].ToString();
            if (variInstanceType == UndertaleInstruction.InstanceType.Local && data?.CodeLocals is not null)
            {
                locatedVariable = localvars.ContainsKey(variableName) ? localvars[variableName] : null;
            }
            else
            {
                locatedVariable = vars.Where((x) => x.Name.Content == variableName && x.InstanceType == variInstanceType).FirstOrDefault();
            }

            // If nothing is found, throw an error, as we cannot properly assemble it
            if (locatedVariable == null)
            {
                throw new Exception($"Failed to find existing variable: {variInstanceType.ToString().ToLower(CultureInfo.InvariantCulture)}.{variableName}");
            }

            // Return reference to be used in instruction
            return new UndertaleInstruction.Reference<UndertaleVariable>(locatedVariable, type);
        }
    }
}
