﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UndertaleModLib.Models;

namespace UndertaleModLib.Decompiler
{
    public static class Disassembler
    {
        private static void AppendLocalVarDefinitionsToStringBuilder(StringBuilder sb, UndertaleCode code, IList<UndertaleVariable> vars, UndertaleCodeLocals locals)
        {
            if (code.WeirdLocalFlag && locals is null)
            {
                return;
            }

            if (locals is null)
            {
                sb.AppendLine("; Missing code locals, possibly due to unsupported bytecode version or brand new code entry.");
                return;
            }
            
            var referenced = code.FindReferencedLocalVars();
            if (locals.Name != code.Name)
                throw new Exception("Name of the locals block does not match name of the code block!");
            foreach (var arg in locals.Locals)
            {
                sb.Append(".localvar ");
                sb.Append(arg.Index);
                sb.Append(' ');
                sb.Append(arg.Name.Content);
                var refVar = referenced.FirstOrDefault(x => x.Name == arg.Name && x.VarID == arg.Index);
                if (refVar is not null)
                {
                    sb.Append(' ');
                    sb.Append(vars.IndexOf(refVar));
                }
                sb.AppendLine();
            }
        }
        
        public static string Disassemble(this UndertaleCode code, IList<UndertaleVariable> vars, UndertaleCodeLocals locals)
        {
            // This StringBuilder is shared with the ToString method of the code instructions.
            // Experimentation has shown that 200 is a good enough starting value for it (now changed to 256).
            // 300 seemed too high and 100 too low. This may change in the future.
            StringBuilder sb = new(256);
            if (locals is null && !code.WeirdLocalFlag)
                sb.AppendLine("; WARNING: Missing code locals, possibly due to unsupported bytecode version or a brand new code entry.");
            else
                AppendLocalVarDefinitionsToStringBuilder(sb, code, vars, locals);

            Dictionary<uint, string> fragments = new(code.ChildEntries.Count);
            foreach (var dup in code.ChildEntries)
            {
                fragments.Add(dup.Offset / 4, $"{(dup.Name?.Content ?? "<null>")} (locals={dup.LocalsCount}, argc={dup.ArgumentsCount})");
            }

            List<uint> blocks = FindBlockAddresses(code);

            uint address = 0;
            foreach (var inst in code.Instructions)
            {
                bool doNewline = true;
                if (fragments.TryGetValue(address, out string entry))
                {
                    sb.AppendLine();
                    sb.AppendLine($"> {entry}");
                    doNewline = false;
                }

                int ind = blocks.IndexOf(address);
                if (ind != -1)
                {
                    if (doNewline)
                        sb.AppendLine();
                    sb.AppendLine($":[{ind}]");
                }

                inst.ToString(sb, code, address, blocks);
                sb.AppendLine();

                address += inst.CalculateInstructionSize();
            }

            sb.AppendLine();
            sb.Append(":[end]");

            return sb.ToString();
        }

        public static List<uint> FindBlockAddresses(UndertaleCode code)
        {
            HashSet<uint> addresses = new HashSet<uint>();

            if (code.Instructions.Count != 0)
                addresses.Add(0);

            uint address = 0;
            foreach (var inst in code.Instructions)
            {
                switch (inst.Kind)
                {
                    case UndertaleInstruction.Opcode.B:
                    case UndertaleInstruction.Opcode.Bf:
                    case UndertaleInstruction.Opcode.Bt:
                    case UndertaleInstruction.Opcode.PushEnv:
                        addresses.Add(address + 1);
                        addresses.Add((uint)(address + inst.JumpOffset));
                        break;
                    case UndertaleInstruction.Opcode.PopEnv:
                        if (!inst.JumpOffsetPopenvExitMagic)
                            addresses.Add((uint)(address + inst.JumpOffset));
                        break;
                    case UndertaleInstruction.Opcode.Exit:
                    case UndertaleInstruction.Opcode.Ret:
                        addresses.Add(address + 1);
                        break;
                }
                address += inst.CalculateInstructionSize();
            }

            List<uint> res = addresses.ToList();
            res.Sort();
            return res;
        }
    }
}
