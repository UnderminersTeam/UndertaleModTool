using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib.Models;
using UndertaleModLib.ModelsDebug;

namespace UndertaleModLib
{
    public class UndertaleDebugFORM : UndertaleChunk
    {
        public override string Name => "FORM";

        public Dictionary<string, UndertaleChunk> Chunks = new Dictionary<string, UndertaleChunk>();

        public UndertaleDebugChunkSCPT SCPT => Chunks["SCPT"] as UndertaleDebugChunkSCPT;
        public UndertaleDebugChunkDBGI DBGI => Chunks["DBGI"] as UndertaleDebugChunkDBGI;
        public UndertaleDebugChunkINST INST => Chunks["INST"] as UndertaleDebugChunkINST;
        public UndertaleDebugChunkLOCL LOCL => Chunks["LOCL"] as UndertaleDebugChunkLOCL;
        public UndertaleDebugChunkSTRG STRG => Chunks["STRG"] as UndertaleDebugChunkSTRG;

        internal override void SerializeChunk(UndertaleWriter writer)
        {
            foreach (var chunk in Chunks)
            {
                writer.Write(chunk.Value);
            }
        }

        internal override void UnserializeChunk(UndertaleReader reader)
        {
            Chunks.Clear();
            uint startPos = reader.Position;
            while (reader.Position < startPos + Length)
            {
                UndertaleChunk chunk = reader.ReadUndertaleChunk();
                if (chunk != null)
                {
                    if (Chunks.ContainsKey(chunk.Name))
                        throw new IOException("Duplicate chunk " + chunk.Name);
                    Chunks.Add(chunk.Name, chunk);
                }
            }
        }
    }

    public class UndertaleDebugChunkSCPT : UndertaleListChunk<UndertaleScriptSource>
    {
        public override string Name => "SCPT";
    }

    public class UndertaleDebugChunkDBGI : UndertaleListChunk<UndertaleDebugInfo>
    {
        public override string Name => "DBGI";
    }

    public class UndertaleDebugChunkINST : UndertaleListChunk<UndertaleInstanceVars>
    {
        public override string Name => "INST";
    }

    public class UndertaleDebugChunkLOCL : UndertaleListChunk<UndertaleCodeLocals>
    {
        public override string Name => "LOCL";
    }

    public class UndertaleDebugChunkSTRG : UndertaleListChunk<UndertaleString>
    {
        public override string Name => "STRG";
    }
}
