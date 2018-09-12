using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib.Models;

namespace UndertaleModLib
{
    public class UndertaleChunkFORM : UndertaleChunk
    {
        public override string Name => "FORM";

        public Dictionary<string, UndertaleChunk> Chunks = new Dictionary<string, UndertaleChunk>();

        public UndertaleChunkGEN8 GEN8 => Chunks["GEN8"] as UndertaleChunkGEN8;
        public UndertaleChunkOPTN OPTN => Chunks["OPTN"] as UndertaleChunkOPTN;
        public UndertaleChunkLANG LANG => Chunks["LANG"] as UndertaleChunkLANG;
        public UndertaleChunkEXTN EXTN => Chunks["EXTN"] as UndertaleChunkEXTN;
        public UndertaleChunkSOND SOND => Chunks["SOND"] as UndertaleChunkSOND;
        public UndertaleChunkAGRP AGRP => Chunks["AGRP"] as UndertaleChunkAGRP;
        public UndertaleChunkSPRT SPRT => Chunks["SPRT"] as UndertaleChunkSPRT;
        public UndertaleChunkBGND BGND => Chunks["BGND"] as UndertaleChunkBGND;
        public UndertaleChunkPATH PATH => Chunks["PATH"] as UndertaleChunkPATH;
        public UndertaleChunkSCPT SCPT => Chunks["SCPT"] as UndertaleChunkSCPT;
        public UndertaleChunkGLOB GLOB => Chunks["GLOB"] as UndertaleChunkGLOB;
        public UndertaleChunkSHDR SHDR => Chunks["SHDR"] as UndertaleChunkSHDR;
        public UndertaleChunkFONT FONT => Chunks["FONT"] as UndertaleChunkFONT;
        public UndertaleChunkTMLN TMLN => Chunks["TMLN"] as UndertaleChunkTMLN;
        public UndertaleChunkOBJT OBJT => Chunks["OBJT"] as UndertaleChunkOBJT;
        public UndertaleChunkROOM ROOM => Chunks["ROOM"] as UndertaleChunkROOM;
        public UndertaleChunkDAFL DAFL => Chunks["DAFL"] as UndertaleChunkDAFL;
        public UndertaleChunkTPAG TPAG => Chunks["TPAG"] as UndertaleChunkTPAG;
        public UndertaleChunkCODE CODE => Chunks["CODE"] as UndertaleChunkCODE;
        public UndertaleChunkVARI VARI => Chunks["VARI"] as UndertaleChunkVARI;
        public UndertaleChunkFUNC FUNC => Chunks["FUNC"] as UndertaleChunkFUNC;
        public UndertaleChunkSTRG STRG => Chunks["STRG"] as UndertaleChunkSTRG;
        public UndertaleChunkTXTR TXTR => Chunks["TXTR"] as UndertaleChunkTXTR;
        public UndertaleChunkAUDO AUDO => Chunks["AUDO"] as UndertaleChunkAUDO;

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

    public class UndertaleChunkGEN8 : UndertaleSingleChunk<UndertaleGeneralInfo>
    {
        public override string Name => "GEN8";
    }

    public class UndertaleChunkOPTN : UndertaleSingleChunk<UndertaleOptions>
    {
        public override string Name => "OPTN";
    }

    public class UndertaleChunkLANG : UndertaleSingleChunk<UndertaleLanguage>
    {
        public override string Name => "LANG";
    }

    public class UndertaleChunkEXTN : UndertaleListChunk<UndertaleExtension>
    {
        public override string Name => "EXTN";
    }

    public class UndertaleChunkSOND : UndertaleListChunk<UndertaleSound>
    {
        public override string Name => "SOND";
    }

    public class UndertaleChunkAGRP : UndertaleListChunk<UndertaleAudioGroup>
    {
        public override string Name => "AGRP";
    }

    public class UndertaleChunkSPRT : UndertaleListChunk<UndertaleSprite>
    {
        public override string Name => "SPRT";
    }

    public class UndertaleChunkBGND : UndertaleListChunk<UndertaleBackground>
    {
        public override string Name => "BGND";
    }

    public class UndertaleChunkPATH : UndertaleListChunk<UndertalePath>
    {
        public override string Name => "PATH";
    }

    public class UndertaleChunkSCPT : UndertaleListChunk<UndertaleScript>
    {
        public override string Name => "SCPT";
    }
    
    public class UndertaleChunkGLOB : UndertaleListChunk<UndertaleGlobal>
    {
        public override string Name => "GLOB";
    }

    public class UndertaleChunkSHDR : UndertaleListChunk<UndertaleShader>
    {
        public override string Name => "SHDR";
    }

    public class UndertaleChunkFONT : UndertaleListChunk<UndertaleFont>
    {
        public override string Name => "FONT";

        internal override void SerializeChunk(UndertaleWriter writer)
        {
            base.SerializeChunk(writer);

            // padding?
            for (ushort i = 0; i < 0x80; i++)
                writer.Write(i);
            for (ushort i = 0; i < 0x80; i++)
                writer.Write((ushort)0x3f);
        }

        internal override void UnserializeChunk(UndertaleReader reader)
        {
            base.UnserializeChunk(reader);

            // padding?
            for (ushort i = 0; i < 0x80; i++)
                if (reader.ReadUInt16() != i)
                    throw new IOException("Incorrect padding in FONT, expected " + i);
            for (ushort i = 0; i < 0x80; i++)
                if (reader.ReadUInt16() != 0x3f)
                    throw new IOException("Incorrect padding in FONT");
        }
    }

    public class UndertaleChunkTMLN : UndertaleListChunk<UndertaleTimeline>
    {
        public override string Name => "TMLN";
    }

    public class UndertaleChunkOBJT : UndertaleListChunk<UndertaleGameObject>
    {
        public override string Name => "OBJT";
    }

    public class UndertaleChunkROOM : UndertaleListChunk<UndertaleRoom>
    {
        public override string Name => "ROOM";
    }

    public class UndertaleChunkDAFL : UndertaleEmptyChunk // DataFiles
    {
        public override string Name => "DAFL";
    }

    public class UndertaleChunkTPAG : UndertaleListChunk<UndertaleTexturePageItem>
    {
        public override string Name => "TPAG";

        internal override void SerializeChunk(UndertaleWriter writer)
        {
            base.SerializeChunk(writer);
        }
    }

    public class UndertaleChunkCODE : UndertaleListChunk<UndertaleCode>
    {
        public override string Name => "CODE";
    }

    public class UndertaleChunkVARI : UndertaleChunk
    {
        public override string Name => "VARI";

        public uint Unknown1; // max value of UndertaleVariable.Unknown
        public uint Unknown1Again;
        public uint Unknown2;
        public List<UndertaleVariable> List = new List<UndertaleVariable>();

        internal override void SerializeChunk(UndertaleWriter writer)
        {
            // Update references
            Dictionary<UndertaleVariable, List<UndertaleInstruction>> references = UndertaleInstruction.Reference<UndertaleVariable>.CollectReferences(writer.undertaleData.Code);
            uint pos = writer.Position;
            foreach(UndertaleVariable var in List)
            {
                var.Occurrences = references.ContainsKey(var) ? (uint)references[var].Count : 0;
                if (var.Occurrences > 0)
                {
                    var.FirstAddress = references[var][0];
                    for (int i = 0; i < references[var].Count; i++)
                    {
                        uint thisAddr = writer.GetAddressForUndertaleObject(references[var][i]);
                        int addrDiff;
                        if (i < references[var].Count - 1)
                        {
                            uint nextAddr = writer.GetAddressForUndertaleObject(references[var][i + 1]);
                            addrDiff = (int)(nextAddr - thisAddr);
                        }
                        else
                            addrDiff = var.UnknownChainEndingValue;
                        // references[var][i].GetReference<UndertaleVariable>().NextOccurrenceOffset = addrDiff;
                        /*if (addrDiff != references[var][i].GetReference<UndertaleVariable>().NextOccurrenceOffset)
                            Debug.WriteLine("VARI Changes at " + writer.GetAddressForUndertaleObject(references[var][i].GetReference<UndertaleVariable>()) + ": " + references[var][i].GetReference<UndertaleVariable>().NextOccurrenceOffset + " to " + addrDiff);*/
                        writer.Position = writer.GetAddressForUndertaleObject(references[var][i].GetReference<UndertaleVariable>());
                        writer.WriteInt24(addrDiff);
                    }
                }
                else
                {
                    var.FirstAddress = null;
                }
            }
            writer.Position = pos;

            writer.Write(Unknown1);
            writer.Write(Unknown1Again);
            writer.Write(Unknown2);
            foreach (UndertaleVariable var in List)
                writer.WriteUndertaleObject(var);
        }

        internal override void UnserializeChunk(UndertaleReader reader)
        {
            uint startPosition = reader.Position;
            Unknown1 = reader.ReadUInt32();
            Unknown1Again = reader.ReadUInt32();
            Unknown2 = reader.ReadUInt32();
            List.Clear();
            while (reader.Position + 20 <= startPosition + Length)
                List.Add(reader.ReadUndertaleObject<UndertaleVariable>());
        }
    }

    public class UndertaleChunkFUNC : UndertaleChunk
    {
        public override string Name => "FUNC";

        public UndertaleSimpleList<UndertaleFunction> Functions = new UndertaleSimpleList<UndertaleFunction>();
        public UndertaleSimpleList<UndertaleAction> Actions = new UndertaleSimpleList<UndertaleAction>();

        internal override void SerializeChunk(UndertaleWriter writer)
        {
            // Update references
            Dictionary<UndertaleFunction, List<UndertaleInstruction>> references = UndertaleInstruction.Reference<UndertaleFunction>.CollectReferences(writer.undertaleData.Code);
            uint pos = writer.Position;
            // TODO: don't repeat the code from VARI, I spent 6 hours debugging the fact that I didn't copy one change from 0 to 1 between them :P
            foreach (UndertaleFunction var in Functions)
            {
                var.Occurrences = references.ContainsKey(var) ? (uint)references[var].Count : 0;
                if (var.Occurrences > 0)
                {
                    var.FirstAddress = references[var][0];
                    for (int i = 0; i < references[var].Count; i++)
                    {
                        uint thisAddr = writer.GetAddressForUndertaleObject(references[var][i]);
                        int addrDiff;
                        if (i < references[var].Count - 1)
                        {
                            uint nextAddr = writer.GetAddressForUndertaleObject(references[var][i + 1]);
                            addrDiff = (int)(nextAddr - thisAddr);
                        }
                        else
                            addrDiff = var.UnknownChainEndingValue;
                        // references[var][i].GetReference<UndertaleFunction>().NextOccurrenceOffset = addrDiff;
                        /*if (addrDiff != references[var][i].GetReference<UndertaleFunction>().NextOccurrenceOffset)
                            Debug.WriteLine("FUNC Changes at " + writer.GetAddressForUndertaleObject(references[var][i].GetReference<UndertaleFunction>()) + ": " + references[var][i].GetReference<UndertaleFunction>().NextOccurrenceOffset + " to " + addrDiff);*/
                        writer.Position = writer.GetAddressForUndertaleObject(references[var][i].GetReference<UndertaleFunction>());
                        writer.WriteInt24(addrDiff);
                    }
                }
                else
                {
                    var.FirstAddress = null;
                }
            }
            writer.Position = pos;

            writer.WriteUndertaleObject(Functions);
            writer.WriteUndertaleObject(Actions);
        }

        internal override void UnserializeChunk(UndertaleReader reader)
        {
            Functions = reader.ReadUndertaleObject<UndertaleSimpleList<UndertaleFunction>>();
            Actions = reader.ReadUndertaleObject<UndertaleSimpleList<UndertaleAction>>();
        }
    }

    public class UndertaleChunkSTRG : UndertaleListChunk<UndertaleString>
    {
        public override string Name => "STRG";

        internal override void SerializeChunk(UndertaleWriter writer)
        {
            base.SerializeChunk(writer);

            // padding
            while (writer.Position % 0x80 != 0)
                writer.Write((byte)0);
        }

        internal override void UnserializeChunk(UndertaleReader reader)
        {
            base.UnserializeChunk(reader);

            // padding
            while (reader.Position % 0x80 != 0)
                if (reader.ReadByte() != 0)
                    throw new IOException("Padding error in STRG");
        }
    }

    public class UndertaleChunkTXTR : UndertaleListChunk<UndertaleEmbeddedTexture>
    {
        public override string Name => "TXTR";

        internal override void SerializeChunk(UndertaleWriter writer)
        {
            base.SerializeChunk(writer);

            // texture blobs
            foreach (UndertaleEmbeddedTexture obj in List)
                obj.SerializeBlob(writer);

            // padding
            // TODO: Maybe the padding is more global and every chunk is padded to 4 byte boundaries?
            while (writer.Position % 4 != 0)
                writer.Write((byte)0);
        }

        internal override void UnserializeChunk(UndertaleReader reader)
        {
            base.UnserializeChunk(reader);

            // texture blobs
            foreach (UndertaleEmbeddedTexture obj in List)
                obj.UnserializeBlob(reader);

            // padding
            while (reader.Position % 4 != 0)
                if (reader.ReadByte() != 0)
                    throw new IOException("Padding error!");
        }
    }

    public class UndertaleChunkAUDO : UndertaleListChunk<UndertaleEmbeddedAudio>
    {
        public override string Name => "AUDO";
    }
}
