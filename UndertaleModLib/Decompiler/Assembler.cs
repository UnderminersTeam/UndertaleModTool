using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UndertaleModLib.Models;

namespace UndertaleModLib.Decompiler
{
    public static class Assembler
    {
        // TODO: Improve the error messages

        public static UndertaleInstruction AssembleOne(string source, IList<UndertaleFunction> funcs, IList<UndertaleVariable> vars, IList<UndertaleString> strg, Dictionary<string, UndertaleVariable> localvars = null, UndertaleData data = null)
        {
            string label;
            UndertaleInstruction instr = AssembleOne(source, funcs, vars, strg, localvars, out label, data, null);
            if (label != null)
                throw new Exception("Cannot use labels in this context");
            return instr;
        }

        public static UndertaleInstruction AssembleOne(string source, UndertaleData data, Dictionary<string, UndertaleVariable> localvars = null)
        {
            return AssembleOne(source, data.Functions, data.Variables, data.Strings, localvars, data);
        }

        public static UndertaleInstruction AssembleOne(string source, IList<UndertaleFunction> funcs, IList<UndertaleVariable> vars, IList<UndertaleString> strg, Dictionary<string, UndertaleVariable> localvars, out string label, UndertaleData data = null, Func<int, UndertaleInstruction.InstanceType?> lookOnStack = null)
        {
            label = null;
            string line = source;
            UndertaleInstruction instr = new UndertaleInstruction();

            string opcode = line;
            int space = opcode.IndexOf(' ');
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

            instr.Kind = (UndertaleInstruction.Opcode)Enum.Parse(typeof(UndertaleInstruction.Opcode), types[0], true);
            if (types.Length >= 2)
                instr.Type1 = UndertaleInstructionUtil.FromOpcodeParam(types[1]);
            if (types.Length >= 3)
                instr.Type2 = UndertaleInstructionUtil.FromOpcodeParam(types[2]);

            switch (UndertaleInstruction.GetInstructionType(instr.Kind))
            {
                case UndertaleInstruction.InstructionType.SingleTypeInstruction:
                    if (instr.Kind == UndertaleInstruction.Opcode.Dup)
                    {
                        instr.DupExtra = Byte.Parse(line);
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
                        if (line == "[drop]")
                        {
                            instr.JumpOffsetPopenvExitMagic = true;
                            if (data?.GeneralInfo?.BytecodeVersion <= 14)
                                instr.JumpOffset = -1048576; // I really don't know at this point. Magic for little endian 00 00 F0
                        }
                        else
                            label = line;
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
                        instr.Destination = ParseVariableReference(line, vars, localvars, ref inst, instr, lookOnStack, data);
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
                                instr.Value = (int)ParseResourceName(line, data);
                            break;
                        case UndertaleInstruction.DataType.Int64:
                            long lval;
                            if (Int64.TryParse(line, out lval))
                                instr.Value = lval;
                            else
                                instr.Value = (long)ParseResourceName(line, data);
                            break;
                        case UndertaleInstruction.DataType.Boolean:
                            instr.Value = Boolean.Parse(line);
                            break;
                        case UndertaleInstruction.DataType.Variable:
                            UndertaleInstruction.InstanceType inst2 = instr.TypeInst;
                            instr.Value = ParseVariableReference(line, vars, localvars, ref inst2, instr, lookOnStack, data);
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
                int id = data.IndexOf(data.ByName(line));
                if (id >= 0)
                    return id;
            }
            throw new FormatException("Unable to parse " + line + " as number or resource name");
        }

        public static List<UndertaleInstruction> Assemble(string source, IList<UndertaleFunction> funcs, IList<UndertaleVariable> vars, IList<UndertaleString> strg, UndertaleData data = null)
        {
            var lines = source.Split('\n');
            uint addr = 0;
            Dictionary<string, uint> labels = new Dictionary<string, uint>();
            Dictionary<UndertaleInstruction, string> labelTargets = new Dictionary<UndertaleInstruction, string>();
            List<UndertaleInstruction> instructions = new List<UndertaleInstruction>();
            Dictionary<string, UndertaleVariable> localvars = new Dictionary<string, UndertaleVariable>();
            foreach (var fullline in lines)
            {
                string line = fullline;
                if (line.Length > 0 && line[0] == ';')
                    continue;
                string label = null;
                int labelEnd = line.IndexOf(':');
                if (labelEnd >= 0)
                {
                    if (line.Take(labelEnd).All(c => Char.IsLetterOrDigit(c) || c.Equals('_')))
                    {
                        label = line.Substring(0, labelEnd).Trim();
                        line = line.Substring(labelEnd + 1);
                        if (String.IsNullOrEmpty(label))
                            throw new Exception("Empty label");
                    }
                }
                line = line.Trim();
                if (String.IsNullOrEmpty(line))
                {
                    if (!String.IsNullOrEmpty(label))
                        throw new Exception("Label with no instruction");
                    else
                        continue;
                }

                if (line.StartsWith("."))
                {
                    // Assembler directive
                    // TODO: Does not update the CodeLocals block yet!!
                    string[] aaa = line.Split(' ');
                    if (aaa[0] == ".localvar")
                    {
                        if (aaa.Length >= 4)
                        {
                            var varii = vars[Int32.Parse(aaa[3])];
                            if (varii.InstanceType != UndertaleInstruction.InstanceType.Local)
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

                // Really ugly hack for compiling array variable references
                // See https://github.com/krzys-h/UndertaleModTool/issues/27#issuecomment-426637438
                Func<int, UndertaleInstruction.InstanceType?> lookOnStack = (int amt) => {
                    int stackCounter = amt;
                    foreach (var i in instructions.Cast<UndertaleInstruction>().Reverse())
                    {
                        if (stackCounter == 1) // This needs to be here because otherwise sth[aaa].another[bbb] doesn't work (damn this workaround is getting crazy, CHAOS, CHAOS)
                        {
                            if (i.Kind == UndertaleInstruction.Opcode.Push)
                                return UndertaleInstruction.InstanceType.Self; // This is probably an instance variable then (e.g. pushi.e 1337; push.v self.someinstance; conv.v.i; pushi.e 0; pop.v.v [array]alarm)
                            else if (i.Kind == UndertaleInstruction.Opcode.PushLoc)
                                return UndertaleInstruction.InstanceType.Local;
                        }
                        //int old = stackCounter;
                        stackCounter -= UndertaleInstruction.CalculateStackDiff(i);
                        //Debug.WriteLine(i.ToString() + "; " + old + " -> " + stackCounter);
                        if (stackCounter == 0)
                        {
                            if (i.Kind == UndertaleInstruction.Opcode.PushI)
                                return (UndertaleInstruction.InstanceType?)(short)i.Value;
                            else if (i.Kind == UndertaleInstruction.Opcode.Dup)
                                stackCounter += 1 + i.DupExtra; // Keep looking for the value that was duplicated
                            else
                                throw new Exception("My workaround still sucks");
                        }
                    }
                    return null;
                };

                string labelTgt;
                UndertaleInstruction instr = AssembleOne(line, funcs, vars, strg, localvars, out labelTgt, data, lookOnStack);
                instr.Address = addr;
                if (labelTgt != null)
                    labelTargets.Add(instr, labelTgt);

                if (!String.IsNullOrEmpty(label))
                {
                    if (labels.ContainsKey(label))
                        throw new Exception("Duplicate label: " + label);
                    labels.Add(label, instr.Address);
                }
                instructions.Add(instr);

                addr += instr.CalculateInstructionSize();
            }
            if (labels.ContainsKey("func_end"))
                throw new Exception("func_end is a reserved label name");
            labels.Add("func_end", addr);
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
                if (str.Substring(at + 1) != "-1") // TODO
                    id = UInt32.Parse(str.Substring(at + 1));
                str = str.Substring(0, at);
            }
            if (!String.IsNullOrEmpty(str))
            {
                if (str[0] != '"' || str[str.Length - 1] != '"')
                    throw new Exception("Bad string format");
                str = UndertaleString.UnescapeText(str.Substring(1, str.Length - 2));
            }
            UndertaleString strobj = id.HasValue ? strg[(int)id.Value] : null;
            if (strobj != null)
                strobj.Content = str;
            else
                strobj = strg.MakeString(str);
            if (!id.HasValue)
                id = (uint)strg.IndexOf(strobj);
            return new UndertaleResourceById<UndertaleString, UndertaleChunkSTRG>() { Resource = strobj, CachedId = (int)id.Value };
        }

        private static UndertaleInstruction.Reference<UndertaleVariable> ParseVariableReference(string line, IList<UndertaleVariable> vars, Dictionary<string, UndertaleVariable> localvars, ref UndertaleInstruction.InstanceType instance, UndertaleInstruction instr, Func<int, UndertaleInstruction.InstanceType?> lookOnStack = null, UndertaleData data = null)
        {
            string str = line;
            string inst = null;
            int instdot = str.IndexOf('.');
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
            UndertaleInstruction.VariableType type = UndertaleInstruction.VariableType.Normal;
            if (str[0] == '[')
            {
                int typeend = str.IndexOf(']');
                if (typeend >= 0)
                {
                    string typestr = str.Substring(1, typeend - 1);
                    str = str.Substring(typeend + 1);
                    type = (UndertaleInstruction.VariableType)Enum.Parse(typeof(UndertaleInstruction.VariableType), typestr, true);
                }
            }

            UndertaleInstruction.InstanceType realinstance = instance;
            // for arrays, the type is on the stack which totally breaks things
            // This is an ugly hack to handle that
            // see https://github.com/krzys-h/UndertaleModTool/issues/27#issuecomment-426637438
            if (type == UndertaleInstruction.VariableType.Array && lookOnStack != null)
            {
                var instTypeOnStack = lookOnStack(instr.Kind == UndertaleInstruction.Opcode.Pop && instr.Type1 == UndertaleInstruction.DataType.Int32 ? 3 : 2);
                if (instTypeOnStack.HasValue)
                    realinstance = instTypeOnStack.Value;
            }
            if (realinstance >= 0)
                realinstance = UndertaleInstruction.InstanceType.Self;
            else if (realinstance == UndertaleInstruction.InstanceType.Other)
                realinstance = UndertaleInstruction.InstanceType.Self;

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
                throw new Exception("Bad variable: " + realinstance.ToString().ToLower() + "." + str);
            return new UndertaleInstruction.Reference<UndertaleVariable>(varobj, type);
        }
    }
}
