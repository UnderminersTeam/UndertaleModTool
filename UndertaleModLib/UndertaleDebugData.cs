using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib.Models;
using UndertaleModLib.ModelsDebug;

namespace UndertaleModLib
{
    public class UndertaleDebugData
    {
        public UndertaleDebugFORM FORM;

        public IList<UndertaleScriptSource> SourceCode => FORM.SCPT.List;
        public IList<UndertaleDebugInfo> DebugInfo => FORM.DBGI.List;
        public IList<UndertaleInstanceVars> InstanceVars => FORM.INST.List;
        public IList<UndertaleCodeLocals> LocalVars => FORM.LOCL.List;
        public IList<UndertaleString> Strings => FORM.STRG.List;

        public static UndertaleDebugData CreateNew()
        {
            UndertaleDebugData data = new UndertaleDebugData();
            data.FORM = new UndertaleDebugFORM();
            data.FORM.Chunks["SCPT"] = new UndertaleDebugChunkSCPT();
            data.FORM.Chunks["DBGI"] = new UndertaleDebugChunkDBGI();
            data.FORM.Chunks["INST"] = new UndertaleDebugChunkINST();
            data.FORM.Chunks["LOCL"] = new UndertaleDebugChunkLOCL();
            data.FORM.Chunks["STRG"] = new UndertaleDebugChunkSTRG();
            return data;
        }
    }
}
