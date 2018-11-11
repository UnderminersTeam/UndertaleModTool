using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib.Models;
using UndertaleModLib.ModelsDebug;

namespace UndertaleModLib.DebugData
{
    public enum DebugDataMode
    {
        FullAssembler,
        PartialAssembler,
        Decompiled,
        NoDebug
    }

    public static class DebugDataGenerator
    {
        public static UndertaleDebugData GenerateDebugData(UndertaleData data, DebugDataMode mode)
        {
            if (mode == DebugDataMode.NoDebug)
                return null;

            UndertaleDebugData debug = UndertaleDebugData.CreateNew();

            foreach (var code in data.Code)
            {
                if (mode == DebugDataMode.Decompiled)
                {
                    Debug.WriteLine("Decompiling " + code.Name.Content);
                    string output;
                    try
                    {
                        output = Decompiler.Decompiler.Decompile(code, data);
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.Message);
                        output = "/*\nEXCEPTION!\n" + e.ToString() + "\n*/";
                    }
                    debug.SourceCode.Add(new UndertaleScriptSource() { SourceCode = debug.Strings.MakeString(output) });

                    UndertaleDebugInfo debugInfo = new UndertaleDebugInfo();
                    debugInfo.Add(new UndertaleDebugInfo.DebugInfoPair() { SourceCodeOffset = 0, BytecodeOffset = 0 }); // TODO: generate this too! :D
                    debug.DebugInfo.Add(debugInfo);
                }
                else
                {
                    StringBuilder sb = new StringBuilder();
                    UndertaleDebugInfo debugInfo = new UndertaleDebugInfo();

                    foreach (var instr in code.Instructions)
                    {
                        if (mode == DebugDataMode.FullAssembler || instr.Kind == UndertaleInstruction.Opcode.Pop || instr.Kind == UndertaleInstruction.Opcode.Popz || instr.Kind == UndertaleInstruction.Opcode.B || instr.Kind == UndertaleInstruction.Opcode.Bt || instr.Kind == UndertaleInstruction.Opcode.Bf || instr.Kind == UndertaleInstruction.Opcode.Ret || instr.Kind == UndertaleInstruction.Opcode.Exit)
                            debugInfo.Add(new UndertaleDebugInfo.DebugInfoPair() { SourceCodeOffset = (uint)sb.Length, BytecodeOffset = instr.Address*4 });
                        sb.Append(instr.ToString(code, data.Variables));
                        sb.Append("\n");
                    }

                    debug.SourceCode.Add(new UndertaleScriptSource() { SourceCode = debug.Strings.MakeString(sb.ToString()) });
                    debug.DebugInfo.Add(debugInfo);
                }
            }
            foreach(var locals in data.CodeLocals)
            {
                debug.LocalVars.Add(locals);
            }

            return debug;
        }
    }
}
