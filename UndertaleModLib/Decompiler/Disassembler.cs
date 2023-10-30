using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib.Models;

namespace UndertaleModLib.Decompiler
{
    public static class Disassembler
    {
        public static string GenerateLocalVarDefinitions(this UndertaleCode code, IList<UndertaleVariable> vars, UndertaleCodeLocals locals)
        {
            if (code.WeirdLocalFlag)
                return "";
            if (locals == null)
                return "; Missing code locals- possibly due to unsupported bytecode version or brand new code entry.\n";

            StringBuilder sb = new StringBuilder();

            var referenced = code.FindReferencedLocalVars();
            if (locals.Name != code.Name)
                throw new Exception("Name of the locals block does not match name of the code block");
            foreach (var arg in locals.Locals)
            {
                sb.Append(".localvar " + arg.Index + " " + arg.Name.Content);
                var refvar = referenced.Where((x) => x.Name == arg.Name && x.VarID == arg.Index).FirstOrDefault();
                if (refvar != null)
                {
                    sb.Append(" " + vars.IndexOf(refvar));
                }
                sb.Append('\n');
            }

            return sb.ToString();
        }

        public static string Disassemble(this UndertaleCode code, IList<UndertaleVariable> vars, UndertaleCodeLocals locals)
        {
            StringBuilder sb = new StringBuilder();
            if (locals == null && !code.WeirdLocalFlag)
                sb.Append("; WARNING: Missing code locals, possibly due to unsupported bytecode version or a brand new code entry.\n");
            else
                sb.Append(code.GenerateLocalVarDefinitions(vars, locals));

            Dictionary<uint, string> fragments = new Dictionary<uint, string>();
            foreach (var dup in code.ChildEntries)
                fragments.TryAdd(dup.Offset / 4, (dup.Name?.Content ?? "<null>") + $" (locals={dup.LocalsCount}, argc={dup.ArgumentsCount})");
            List<uint> blocks = FindBlockAddresses(code);

            foreach (var inst in code.Instructions)
            {
                bool doNewline = true;
                if (fragments.TryGetValue(inst.Address, out string entry))
                {
                    sb.AppendLine();
                    sb.AppendLine($"> {entry}");
                    doNewline = false;
                }

                int ind = blocks.IndexOf(inst.Address);
                if (ind != -1)
                {
                    if (doNewline)
                        sb.AppendLine();
                    sb.AppendLine($":[{ind}]");
                }

                sb.AppendLine(inst.ToString(code, blocks));
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

            foreach (var inst in code.Instructions)
            {
                switch (inst.Kind)
                {
                    case UndertaleInstruction.Opcode.B:
                    case UndertaleInstruction.Opcode.Bf:
                    case UndertaleInstruction.Opcode.Bt:
                    case UndertaleInstruction.Opcode.PushEnv:
                        addresses.Add(inst.Address + 1);
                        addresses.Add((uint)(inst.Address + inst.JumpOffset));
                        break;
                    case UndertaleInstruction.Opcode.PopEnv:
                        if (!inst.JumpOffsetPopenvExitMagic)
                            addresses.Add((uint)(inst.Address + inst.JumpOffset));
                        break;
                    case UndertaleInstruction.Opcode.Exit:
                    case UndertaleInstruction.Opcode.Ret:
                        addresses.Add(inst.Address + 1);
                        break;
                }
            }

            List<uint> res = addresses.ToList();
            res.Sort();
            return res;
        }
    }
}
