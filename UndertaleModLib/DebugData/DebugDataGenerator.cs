using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib.ModelsDebug;

namespace UndertaleModLib.DebugData
{
    public static class DebugDataGenerator
    {
        public static UndertaleDebugData GenerateDebugData(UndertaleData data)
        {
            UndertaleDebugData debug = UndertaleDebugData.CreateNew();

            foreach (var code in data.Code)
            {
                Debug.WriteLine(code.Name);
                string output;
                try
                {
                    output = Decompiler.Decompiler.Decompile(code);
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
            foreach(var locals in data.CodeLocals)
            {
                debug.LocalVars.Add(locals);
            }

            return debug;
        }
    }
}
