using System;
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
        
        public static string Disassemble(this UndertaleCode code, IList<UndertaleVariable> vars, UndertaleCodeLocals locals, bool ignoreMissingCodeLocals = false)
        {
            // This StringBuilder is shared with the ToString method of the code instructions.
            // Experimentation has shown that 200 is a good enough starting value for it (now changed to 256).
            // 300 seemed too high and 100 too low. This may change in the future.
            StringBuilder sb = new(256);

            // Print locals
            bool printedLocalVars = true;
            if (locals is null && !code.WeirdLocalFlag)
            {
                if (ignoreMissingCodeLocals)
                {
                    printedLocalVars = false;
                }
                else
                {
                    sb.AppendLine("; WARNING: Missing code locals, possibly due to unsupported bytecode version or a brand new code entry.");
                }
            }
            else if (locals is null && code.WeirdLocalFlag)
            {
                printedLocalVars = false;
            }
            else
            {
                AppendLocalVarDefinitionsToStringBuilder(sb, code, vars, locals);
            }

            // Collect fragments (sub-functions/child code entries)
            Dictionary<uint, string> fragments = new(code.ChildEntries.Count);
            foreach (var dup in code.ChildEntries)
            {
                fragments.Add(dup.Offset / 4, $"{(dup.Name?.Content ?? "<null>")} (locals={dup.LocalsCount}, argc={dup.ArgumentsCount})");
            }

            // Find addresses of all blocks
            Dictionary<uint, uint> blocks = FindBlockAddresses(code);

            // Print actual instructions
            uint address = 0;
            foreach (UndertaleInstruction inst in code.Instructions)
            {
                // Print an extra newline for fragments/blocks if local vars were printed at the beginning,
                // or if any instructions have already been printed.
                bool doNewline = printedLocalVars || address > 0;

                // Print fragment at current address
                if (fragments.TryGetValue(address, out string entry))
                {
                    sb.AppendLine();
                    sb.AppendLine($"> {entry}");

                    // No need for a second newline before blocks
                    doNewline = false;
                }

                // Print block at current address
                if (blocks.TryGetValue(address, out uint ind))
                {
                    if (doNewline)
                    {
                        sb.AppendLine();
                    }
                    sb.AppendLine($":[{ind}]");
                }

                // Print actual instruction at current address
                inst.ToString(sb, code, address, blocks);
                sb.AppendLine();

                // Advance address to next instruction
                address += inst.CalculateInstructionSize();
            }

            // Print ending block
            if (printedLocalVars || address > 0)
            {
                sb.AppendLine();
            }
            sb.Append(":[end]");

            return sb.ToString();
        }

        public static Dictionary<uint, uint> FindBlockAddresses(UndertaleCode code)
        {
            // Use a sorted set, so that block indices can be calculated
            SortedSet<uint> addresses = new();

            // Add initial block, if any instructions exist
            if (code.Instructions.Count > 0)
            {
                addresses.Add(0);
            }

            // Add all other blocks based 
            uint currentAddress = 0;
            foreach (var inst in code.Instructions)
            {
                switch (inst.Kind)
                {
                    case UndertaleInstruction.Opcode.B:
                    case UndertaleInstruction.Opcode.Bf:
                    case UndertaleInstruction.Opcode.Bt:
                    case UndertaleInstruction.Opcode.PushEnv:
                        addresses.Add(currentAddress + 1);
                        addresses.Add((uint)(currentAddress + inst.JumpOffset));
                        break;
                    case UndertaleInstruction.Opcode.PopEnv:
                        if (!inst.JumpOffsetPopenvExitMagic)
                        {
                            addresses.Add((uint)(currentAddress + inst.JumpOffset));
                        }
                        break;
                    case UndertaleInstruction.Opcode.Exit:
                    case UndertaleInstruction.Opcode.Ret:
                        addresses.Add(currentAddress + 1);
                        break;
                }
                currentAddress += inst.CalculateInstructionSize();
            }

            // Convert to an index lookup
            Dictionary<uint, uint> blockIndicesByAddress = new(addresses.Count);
            uint blockIndex = 0;
            foreach (uint address in addresses)
            {
                blockIndicesByAddress.Add(address, blockIndex);
                blockIndex++;
            }
            return blockIndicesByAddress;
        }
    }
}
