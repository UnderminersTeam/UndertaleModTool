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

        public static UndertaleInstruction AssembleOne(string source, IList<UndertaleFunction> funcs, IList<UndertaleVariable> vars, IList<UndertaleString> strg, Dictionary<string, UndertaleVariable> localvars = null)
        {
            string label;
            UndertaleInstruction instr = AssembleOne(source, funcs, vars, strg, localvars, out label);
            if (label != null)
                throw new Exception("Cannot use labels in this context");
            return instr;
        }

        public static UndertaleInstruction AssembleOne(string source, IList<UndertaleFunction> funcs, IList<UndertaleVariable> vars, IList<UndertaleString> strg, Dictionary<string, UndertaleVariable> localvars, out string label)
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
                        label = line;
                    }
                    line = "";
                    break;

                case UndertaleInstruction.InstructionType.PopInstruction:
                    UndertaleInstruction.InstanceType inst = instr.TypeInst;
                    instr.Destination = ParseVariableReference(line, vars, localvars, ref inst);
                    instr.TypeInst = inst;
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
                            instr.Value = Int32.Parse(line);
                            break;
                        case UndertaleInstruction.DataType.Int64:
                            instr.Value = Int64.Parse(line);
                            break;
                        case UndertaleInstruction.DataType.Boolean:
                            instr.Value = Boolean.Parse(line);
                            break;
                        case UndertaleInstruction.DataType.Variable:
                            UndertaleInstruction.InstanceType inst2 = instr.TypeInst;
                            instr.Value = ParseVariableReference(line, vars, localvars, ref inst2);
                            instr.TypeInst = inst2;
                            break;
                        case UndertaleInstruction.DataType.String:
                            instr.Value = ParseStringReference(line, strg);
                            break;
                        case UndertaleInstruction.DataType.Int16:
                            instr.Value = Int16.Parse(line);
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
                        throw new Exception("Function not found");
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

        public static List<UndertaleInstruction> Assemble(string source, IList<UndertaleFunction> funcs, IList<UndertaleVariable> vars, IList<UndertaleString> strg)
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
                    label = line.Substring(0, labelEnd).Trim();
                    line = line.Substring(labelEnd + 1);
                    if (String.IsNullOrEmpty(label))
                        throw new Exception("Empty label");
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

                string labelTgt;
                UndertaleInstruction instr = AssembleOne(line, funcs, vars, strg, localvars, out labelTgt);
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
            foreach(var pair in labelTargets)
            {
                pair.Key.JumpOffset = (int)labels[pair.Value] - (int)pair.Key.Address;
            }
            return instructions;
        }
        
        private static UndertaleResourceById<UndertaleString> ParseStringReference(string line, IList<UndertaleString> strg)
        {
            string str = line;
            int at = str.LastIndexOf('@');
            uint? id = null;
            if (at >= 0)
            {
                id = UInt32.Parse(str.Substring(at + 1));
                str = str.Substring(0, at);
            }
            if (!String.IsNullOrEmpty(str))
            {
                if (str[0] != '"' || str[str.Length - 1] != '"')
                    throw new Exception("Bad string format");
                str = str.Substring(1, str.Length - 2);
            }
            UndertaleString strobj = id.HasValue ? strg[(int)id.Value] : null;
            if (strobj != null)
                strobj.Content = str;
            else
                strobj = strg.MakeString(str);
            if (!id.HasValue)
                id = (uint)strg.IndexOf(strobj);
            return new UndertaleResourceById<UndertaleString>("STRG") { Resource = strobj, CachedId = (int)id.Value };
        }

        private static UndertaleInstruction.Reference<UndertaleVariable> ParseVariableReference(string line, IList<UndertaleVariable> vars, Dictionary<string, UndertaleVariable> localvars, ref UndertaleInstruction.InstanceType instance)
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
                instance = UndertaleInstruction.InstanceType.StackTopOrGlobal; // TODO: I think this isn't a thing that exists, 0 would be just object index 0
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

            UndertaleVariable varobj;
            if (instance == UndertaleInstruction.InstanceType.Local)
            {
                varobj = localvars.ContainsKey(str) ? localvars[str] : null;
            }
            else
            {
                UndertaleInstruction.InstanceType i = instance; // ugh
                varobj = vars.Where((x) => x.Name.Content == str && x.InstanceType == i).FirstOrDefault();
            }
            if (varobj == null)
                throw new Exception("Bad variable!");
            return new UndertaleInstruction.Reference<UndertaleVariable>(varobj, type);
        }
    }
}
