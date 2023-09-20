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
                            if (data?.GeneralInfo?.BytecodeVersion <= 14)
                                instr.JumpOffset = -1048576; // I really don't know at this point. Magic for little endian 00 00 F0
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
                        instr.Destination = ParseVariableReference(line, vars, localvars, ref inst, instr, data);
                        instr.TypeInst = inst;
                    }
                    line = "";
                    break;

                case UndertaleInstruction.InstructionType.PushInstruction:
                    switch (instr.Type1)
                    {
                        case UndertaleInstruction.DataType.Double:
                            instr.Value = Double.Parse(line, CultureInfo.InvariantCulture);
                            break;
                        case UndertaleInstruction.DataType.Float:
                            instr.Value = Single.Parse(line, CultureInfo.InvariantCulture);
                            break;
                        case UndertaleInstruction.DataType.Int32:
                            int ival;
                            if (Int32.TryParse(line, out ival))
                                instr.Value = ival;
                            else
                            {
                                var f = data.Functions.ByName(line);
                                if (f == null)
                                    instr.Value = (int)ParseResourceName(line, data);
                                else
                                    instr.Value = new UndertaleInstruction.Reference<UndertaleFunction>(f);
                            }
                            break;
                        case UndertaleInstruction.DataType.Int64:
                            long lval;
                            if (Int64.TryParse(line, out lval))
                                instr.Value = lval;
                            else
                                instr.Value = (long)ParseResourceName(line, data);
                            break;
                        case UndertaleInstruction.DataType.Boolean:
                            instr.Value = bool.Parse(line);
                            break;
                        case UndertaleInstruction.DataType.Variable:
                            UndertaleInstruction.InstanceType inst2 = instr.TypeInst;
                            instr.Value = ParseVariableReference(line, vars, localvars, ref inst2, instr, data);
                            instr.TypeInst = inst2;
                            break;
                        case UndertaleInstruction.DataType.String:
                            instr.Value = ParseStringReference(line, strg);
                            break;
                        case UndertaleInstruction.DataType.Int16:
                            short sval;
                            if (Int16.TryParse(line, out sval))
                                instr.Value = sval;
                            else
                                instr.Value = (short)ParseResourceName(line, data);
                            break;
                    }
                    line = "";
                    break;

                case UndertaleInstruction.InstructionType.CallInstruction:
                    Match match = Regex.Match(line, @"^(.*)\(argc=(.*)\)$");
                    if (!match.Success)
                        throw new Exception("Call instruction format error");

                    UndertaleFunction func = funcs.ByName(match.Groups[1].Value);
                    if (func == null)
                        throw new Exception("Function not found: " + match.Groups[1].Value);
                    instr.Function = new UndertaleInstruction.Reference<UndertaleFunction>() { Target = func };
                    instr.ArgumentsCount = UInt16.Parse(match.Groups[2].Value);
                    line = "";
                    break;

                case UndertaleInstruction.InstructionType.BreakInstruction:
                    if (breakId != 0)
                    {
                        instr.Value = breakId;
                        if (breakId == -11) // pushref
                        {
                            // parse additional int argument
                            instr.IntArgument = Int32.Parse(line);
                        }
                    }
                    else
                        instr.Value = Int16.Parse(line);
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
            var strReader = new StringReader(source);
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

                    Match match = Regex.Match(info, @"^\(locals=(.*)\,\s*argc=(.*)\)$");
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
            string str = line;
            UndertaleInstruction.VariableType type = UndertaleInstruction.VariableType.Normal;
            UndertaleInstruction.InstanceType realinstance = instance;
            if (str[0] != '[')
            {
                string inst = null;
                int instdot = str.IndexOf('.', StringComparison.InvariantCulture);
                if (instdot >= 0)
                {
                    inst = str.Substring(0, instdot);
                    str = str.Substring(instdot + 1);
                    if (inst == "")
                        throw new Exception("Whoops?");
                }
                if (inst != null)
                {
                    short instnum;
                    if (Int16.TryParse(inst, out instnum))
                    {
                        instance = (UndertaleInstruction.InstanceType)instnum;
                    }
                    else
                    {
                        instance = (UndertaleInstruction.InstanceType)Enum.Parse(typeof(UndertaleInstruction.InstanceType), inst, true);
                    }
                }
                else
                {
                    instance = UndertaleInstruction.InstanceType.Undefined;
                }

                realinstance = instance;
                if (realinstance >= 0)
                    realinstance = UndertaleInstruction.InstanceType.Self;
                else if (realinstance == UndertaleInstruction.InstanceType.Other)
                    realinstance = UndertaleInstruction.InstanceType.Self;
                else if (realinstance == UndertaleInstruction.InstanceType.Arg)
                    realinstance = UndertaleInstruction.InstanceType.Builtin;
                else if (realinstance == UndertaleInstruction.InstanceType.Builtin)
                    realinstance = UndertaleInstruction.InstanceType.Self; // used with @@This@@
                else if (realinstance == UndertaleInstruction.InstanceType.Stacktop)
                    realinstance = UndertaleInstruction.InstanceType.Self; // used with @@GetInstance@@
            }
            else
            {
                int typeend = str.IndexOf(']', StringComparison.InvariantCulture);
                if (typeend >= 0)
                {
                    string typestr = str.Substring(1, typeend - 1);
                    str = str.Substring(typeend + 1);
                    type = (UndertaleInstruction.VariableType)Enum.Parse(typeof(UndertaleInstruction.VariableType), typestr, true);

                    int instanceEnd = str.IndexOf('.', StringComparison.InvariantCulture);
                    if (instanceEnd >= 0)
                    {
                        string instancestr = str.Substring(0, instanceEnd);
                        str = str.Substring(instanceEnd + 1);
                        realinstance = (UndertaleInstruction.InstanceType)Enum.Parse(typeof(UndertaleInstruction.InstanceType), instancestr, true);
                    } else
                    {
                        if (type == UndertaleInstruction.VariableType.Array || 
                            type == UndertaleInstruction.VariableType.StackTop)
                        {
                            throw new Exception("Old instruction format is incompatible (missing instance type in array or stacktop)");
                        }

                        if (realinstance >= 0)
                            realinstance = UndertaleInstruction.InstanceType.Self;
                        else if (realinstance == UndertaleInstruction.InstanceType.Other)
                            realinstance = UndertaleInstruction.InstanceType.Self;
                    }
                }
                else
                    throw new Exception("Missing ']' character in variable reference");
            }

            if (data?.GeneralInfo?.BytecodeVersion <= 14)
                realinstance = UndertaleInstruction.InstanceType.Undefined;

            UndertaleVariable varobj;
            if (realinstance == UndertaleInstruction.InstanceType.Local)
            {
                varobj = localvars.ContainsKey(str) ? localvars[str] : null;
            }
            else
            {
                varobj = vars.Where((x) => x.Name.Content == str && x.InstanceType == realinstance).FirstOrDefault();
            }
            if (varobj == null)
                throw new Exception("Bad variable: " + realinstance.ToString().ToLower(CultureInfo.InvariantCulture) + "." + str);
            return new UndertaleInstruction.Reference<UndertaleVariable>(varobj, type);
        }
    }
}
