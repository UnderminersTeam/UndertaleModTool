using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib.Models;
using UndertaleModLib.Util;

namespace UndertaleModLib
{
    public class UndertaleChunkFORM : UndertaleChunk
    {
        public override string Name => "FORM";

        public Dictionary<string, UndertaleChunk> Chunks = new Dictionary<string, UndertaleChunk>();

        public UndertaleChunkGEN8 GEN8 => Chunks.GetValueOrDefault("GEN8") as UndertaleChunkGEN8;
        public UndertaleChunkOPTN OPTN => Chunks.GetValueOrDefault("OPTN") as UndertaleChunkOPTN;
        public UndertaleChunkLANG LANG => Chunks.GetValueOrDefault("LANG") as UndertaleChunkLANG;
        public UndertaleChunkEXTN EXTN => Chunks.GetValueOrDefault("EXTN") as UndertaleChunkEXTN;
        public UndertaleChunkSOND SOND => Chunks.GetValueOrDefault("SOND") as UndertaleChunkSOND;
        public UndertaleChunkAGRP AGRP => Chunks.GetValueOrDefault("AGRP") as UndertaleChunkAGRP;
        public UndertaleChunkSPRT SPRT => Chunks.GetValueOrDefault("SPRT") as UndertaleChunkSPRT;
        public UndertaleChunkBGND BGND => Chunks.GetValueOrDefault("BGND") as UndertaleChunkBGND;
        public UndertaleChunkPATH PATH => Chunks.GetValueOrDefault("PATH") as UndertaleChunkPATH;
        public UndertaleChunkSCPT SCPT => Chunks.GetValueOrDefault("SCPT") as UndertaleChunkSCPT;
        public UndertaleChunkGLOB GLOB => Chunks.GetValueOrDefault("GLOB") as UndertaleChunkGLOB;
        public UndertaleChunkSHDR SHDR => Chunks.GetValueOrDefault("SHDR") as UndertaleChunkSHDR;
        public UndertaleChunkFONT FONT => Chunks.GetValueOrDefault("FONT") as UndertaleChunkFONT;
        public UndertaleChunkTMLN TMLN => Chunks.GetValueOrDefault("TMLN") as UndertaleChunkTMLN;
        public UndertaleChunkOBJT OBJT => Chunks.GetValueOrDefault("OBJT") as UndertaleChunkOBJT;
        public UndertaleChunkROOM ROOM => Chunks.GetValueOrDefault("ROOM") as UndertaleChunkROOM;
        public UndertaleChunkDAFL DAFL => Chunks.GetValueOrDefault("DAFL") as UndertaleChunkDAFL;
        public UndertaleChunkEMBI EMBI => Chunks.GetValueOrDefault("EMBI") as UndertaleChunkEMBI;
        public UndertaleChunkTPAG TPAG => Chunks.GetValueOrDefault("TPAG") as UndertaleChunkTPAG;
        public UndertaleChunkTGIN TGIN => Chunks.GetValueOrDefault("TGIN") as UndertaleChunkTGIN;
        public UndertaleChunkCODE CODE => Chunks.GetValueOrDefault("CODE") as UndertaleChunkCODE;
        public UndertaleChunkVARI VARI => Chunks.GetValueOrDefault("VARI") as UndertaleChunkVARI;
        public UndertaleChunkFUNC FUNC => Chunks.GetValueOrDefault("FUNC") as UndertaleChunkFUNC;
        public UndertaleChunkSTRG STRG => Chunks.GetValueOrDefault("STRG") as UndertaleChunkSTRG;
        public UndertaleChunkTXTR TXTR => Chunks.GetValueOrDefault("TXTR") as UndertaleChunkTXTR;
        public UndertaleChunkAUDO AUDO => Chunks.GetValueOrDefault("AUDO") as UndertaleChunkAUDO;
        // GMS2.3+ for the below chunks
        public UndertaleChunkACRV ACRV => Chunks.GetValueOrDefault("ACRV") as UndertaleChunkACRV;
        public UndertaleChunkSEQN SEQN => Chunks.GetValueOrDefault("SEQN") as UndertaleChunkSEQN;
        public UndertaleChunkTAGS TAGS => Chunks.GetValueOrDefault("TAGS") as UndertaleChunkTAGS;

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

            // First, find the last chunk in the file because of padding changes
            // (also, calculate all present chunks while we're at it)
            reader.AllChunkNames = new List<string>();
            string lastChunk = "";
            while (reader.Position < reader.Length)
            {
                lastChunk = reader.ReadChars(4);
                reader.AllChunkNames.Add(lastChunk);
                uint length = reader.ReadUInt32();
                reader.SmallReadAt(reader.Position + length, 8);
            }
            reader.LastChunkName = lastChunk;
            reader.Position = startPos;

            reader.GMS2_3 = reader.AllChunkNames.Contains("SEQN");
            reader.undertaleData.GMS2_3 = reader.GMS2_3;

            // Now, parse the chunks
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
        public List<byte[]> productIdData = new List<byte[]>();

        internal override void UnserializeChunk(UndertaleReader reader)
        {
            base.UnserializeChunk(reader);

            // Strange data for each extension, some kind of unique identifier based on
            // the product ID for each of them
            productIdData = new List<byte[]>();
            //NOTE: I do not know if 1773 is the youngest version which contains product IDs.
            if (reader.undertaleData.GeneralInfo?.Major >= 2 || (reader.undertaleData.GeneralInfo?.Major == 1 && reader.undertaleData.GeneralInfo?.Build >= 1773))
            {
                for (int i = 0; i < List.Count; i++)
                {
                    productIdData.Add(reader.ReadBytes(16));
                }
            }
        }

        internal override void SerializeChunk(UndertaleWriter writer)
        {
            base.SerializeChunk(writer);

            // (read above comment)
            foreach (byte[] data in productIdData)
            {
                int Len = data.Length;
                if (Len != 16)
                {
                    throw new IOException("Can't write EXTN product id data of invalid length, expected 16, got " + Len);
                }

                writer.Write(data);
            }
        }
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

    public class UndertaleChunkGLOB : UndertaleSimpleListChunk<UndertaleGlobalInit>
    {
        public override string Name => "GLOB";
    }

    public class UndertaleChunkSHDR : UndertaleListChunk<UndertaleShader>
    {
        public override string Name => "SHDR";

        internal override void SerializeChunk(UndertaleWriter writer)
        {
            base.SerializeChunk(writer);
        }

        internal override void UnserializeChunk(UndertaleReader reader)
        {
            reader.Position -= 4;
            int chunkLength = reader.ReadInt32();
            uint chunkEnd = reader.Position + (uint)chunkLength;

            uint beginPosition = reader.Position;

            // Figure out where the starts/ends of each shader object are
            int count = reader.ReadInt32();
            uint[] objectLocations = new uint[count + 1];
            for (int i = 0; i < count; i++)
            {
                objectLocations[i] = (uint)reader.ReadInt32();
            }
            objectLocations[count] = chunkEnd;

            Dictionary<uint, UndertaleObject> objPool = reader.GetOffsetMap();
            Dictionary<UndertaleObject, uint> objPoolRev = reader.GetOffsetMapRev();

            // Setup base shader objects with boundaries set. Load into object pool
            // so that they don't immediately discard.
            for (int i = 0; i < count; i++)
            {
                UndertaleShader s = new UndertaleShader { _EntryEnd = objectLocations[i + 1] };
                objPool.Add(objectLocations[i], s);
                objPoolRev.Add(s, objectLocations[i]);
            }

            reader.Position = beginPosition;
            base.UnserializeChunk(reader);
        }
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

        internal override void UnserializeChunk(UndertaleReader reader)
        {
            base.UnserializeChunk(reader);

            for (int index = 0; index < List.Count; index++)
            {
                List[index].Name = new UndertaleString("PageItem " + index.ToString()); // not Data.MakeString
            }
        }
    }

    public class UndertaleChunkCODE : UndertaleListChunk<UndertaleCode>
    {
        public override string Name => "CODE";

        internal override void SerializeChunk(UndertaleWriter writer)
        {
            if (List == null)
                return;
            base.SerializeChunk(writer);
        }

        internal override void UnserializeChunk(UndertaleReader reader)
        {
            if (Length == 0) // YYC, bytecode <= 16, chunk is empty but exists
            {
                List = null;
                return;
            }
            base.UnserializeChunk(reader);
        }
    }

    // TODO: INotifyPropertyChanged
    public class UndertaleChunkVARI : UndertaleChunk
    {
        public override string Name => "VARI";

        public uint InstanceVarCount { get; set; }
        public uint InstanceVarCountAgain { get; set; }
        public uint MaxLocalVarCount { get; set; }
        public List<UndertaleVariable> List = new List<UndertaleVariable>();

        internal override void SerializeChunk(UndertaleWriter writer)
        {
            if (List == null)
                return;

            if (writer.undertaleData.UnsupportedBytecodeVersion)
                return;

            UndertaleInstruction.Reference<UndertaleVariable>.SerializeReferenceChain(writer, writer.undertaleData.Code, List);

            if (!writer.Bytecode14OrLower)
            {
                writer.Write(InstanceVarCount);
                writer.Write(InstanceVarCountAgain);
                writer.Write(MaxLocalVarCount);
            }
            foreach (UndertaleVariable var in List)
                writer.WriteUndertaleObject(var);
        }

        internal override void UnserializeChunk(UndertaleReader reader)
        {
            if (Length == 0) // YYC, bytecode <= 16, chunk is empty but exists
            {
                List = null;
                return;
            }

            if (reader.undertaleData.UnsupportedBytecodeVersion)
                return;
            uint startPosition = reader.Position;
            uint varLength;
            if (!reader.Bytecode14OrLower)
            {
                InstanceVarCount = reader.ReadUInt32();
                InstanceVarCountAgain = reader.ReadUInt32();
                MaxLocalVarCount = reader.ReadUInt32();
                varLength = 20;
            }
            else
                varLength = 12;
            List.Clear();
            while (reader.Position + varLength <= startPosition + Length)
                List.Add(reader.ReadUndertaleObject<UndertaleVariable>());
        }
    }

    public class UndertaleChunkFUNC : UndertaleChunk
    {
        public override string Name => "FUNC";

        public UndertaleSimpleList<UndertaleFunction> Functions = new UndertaleSimpleList<UndertaleFunction>();
        public UndertaleSimpleList<UndertaleCodeLocals> CodeLocals = new UndertaleSimpleList<UndertaleCodeLocals>();

        internal override void SerializeChunk(UndertaleWriter writer)
        {
            if (Functions == null && CodeLocals == null)
                return;

            UndertaleInstruction.Reference<UndertaleFunction>.SerializeReferenceChain(writer, writer.undertaleData.Code, Functions);

            if (writer.Bytecode14OrLower)
            {
                foreach (UndertaleFunction f in Functions)
                    writer.WriteUndertaleObject(f);
            }
            else
            {
                writer.WriteUndertaleObject(Functions);
                writer.WriteUndertaleObject(CodeLocals);
            }
        }

        internal override void UnserializeChunk(UndertaleReader reader)
        {
            if (Length == 0 && reader.undertaleData.GeneralInfo?.BytecodeVersion > 14) // YYC, 14 < bytecode <= 16, chunk is empty but exists
            {
                Functions = null;
                CodeLocals = null;
                return;
            }

            if (reader.undertaleData.UnsupportedBytecodeVersion)
                return;
            if (reader.Bytecode14OrLower)
            {
                uint startPosition = reader.Position;
                Functions.Clear();
                while (reader.Position + 12 <= startPosition + Length)
                    Functions.Add(reader.ReadUndertaleObject<UndertaleFunction>());
            }
            else
            {
                Functions = reader.ReadUndertaleObject<UndertaleSimpleList<UndertaleFunction>>();
                CodeLocals = reader.ReadUndertaleObject<UndertaleSimpleList<UndertaleCodeLocals>>();
            }
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
            {
                obj.UnserializeBlob(reader);
                obj.Name = new UndertaleString("Texture " + List.IndexOf(obj).ToString());
            }
            
            // padding
            while (reader.Position % 4 != 0)
                if (reader.ReadByte() != 0)
                    throw new IOException("Padding error!");
        }
    }

    public class UndertaleChunkAUDO : UndertaleListChunk<UndertaleEmbeddedAudio>
    {
        public override string Name => "AUDO";

        internal override void SerializeChunk(UndertaleWriter writer)
        {
            base.SerializeChunk(writer);
        }

        internal override void UnserializeChunk(UndertaleReader reader)
        {
            base.UnserializeChunk(reader);

            for (int index = 0; index < List.Count; index++)
            {
                List[index].Name = new UndertaleString("EmbeddedSound " + index.ToString());
            }
        }
    }

    // GMS2 only
    public class UndertaleChunkEMBI : UndertaleSimpleListChunk<UndertaleEmbeddedImage>
    {
        public override string Name => "EMBI";

        internal override void SerializeChunk(UndertaleWriter writer)
        {
            if (writer.undertaleData.GeneralInfo.Major < 2)
                throw new InvalidOperationException();
            writer.Write((uint)1); // apparently hardcoded 1, see https://github.com/krzys-h/UndertaleModTool/issues/4#issuecomment-421844420
            base.SerializeChunk(writer);
        }

        internal override void UnserializeChunk(UndertaleReader reader)
        {
            if (reader.undertaleData.GeneralInfo.Major < 2)
                throw new InvalidOperationException();
            if (reader.ReadUInt32() != 1)
                throw new Exception("Expected EMBI version 1");
            base.UnserializeChunk(reader);
        }
    }

    // GMS2.2.1+ only
    public class UndertaleChunkTGIN : UndertaleListChunk<UndertaleTextureGroupInfo>
    {
        public override string Name => "TGIN";

        internal override void SerializeChunk(UndertaleWriter writer)
        {
            if (writer.undertaleData.GeneralInfo.Major < 2)
                throw new InvalidOperationException();
            writer.Write((uint)1); // Version
            base.SerializeChunk(writer);
        }

        internal override void UnserializeChunk(UndertaleReader reader)
        {
            if (reader.undertaleData.GeneralInfo.Major < 2)
                throw new InvalidOperationException();
            if (reader.ReadUInt32() != 1)
                throw new IOException("Expected TGIN version 1");
            base.UnserializeChunk(reader);
        }
    }

    // Unknown if possible yet, GMS2.3+ only
    public class UndertaleChunkNINE : UndertaleChunk
    {
        public override string Name => "NINE";

        internal override void SerializeChunk(UndertaleWriter writer)
        {
            throw new NotImplementedException();
        }

        internal override void UnserializeChunk(UndertaleReader reader)
        {
            throw new NotImplementedException();
        }
    }

    // Unknown if possible yet, GMS2.3+ only
    public class UndertaleChunkGMEN : UndertaleChunk
    {
        public override string Name => "GMEN";

        internal override void SerializeChunk(UndertaleWriter writer)
        {
            throw new NotImplementedException();
        }

        internal override void UnserializeChunk(UndertaleReader reader)
        {
            throw new NotImplementedException();
        }
    }

    // GMS2.3+ only
    public class UndertaleChunkACRV : UndertaleListChunk<UndertaleAnimationCurve>
    {
        public override string Name => "ACRV";

        internal override void SerializeChunk(UndertaleWriter writer)
        {
            if (writer.undertaleData.GeneralInfo.Major < 2)
                throw new InvalidOperationException();

            while (writer.Position % 4 != 0)
                writer.Write((byte)0);

            writer.Write((uint)1); // Version

            base.SerializeChunk(writer);
        }

        internal override void UnserializeChunk(UndertaleReader reader)
        {
            if (reader.undertaleData.GeneralInfo.Major < 2)
                throw new InvalidOperationException();

            // Padding
            while (reader.Position % 4 != 0)
                if (reader.ReadByte() != 0)
                    throw new IOException("Padding error!");

            if (reader.ReadUInt32() != 1)
                throw new IOException("Expected ACRV version 1");

            base.UnserializeChunk(reader);
        }
    }

    // GMS2.3+ only
    public class UndertaleChunkSEQN : UndertaleListChunk<UndertaleSequence>
    {
        public override string Name => "SEQN";

        internal override void SerializeChunk(UndertaleWriter writer)
        {
            if (writer.undertaleData.GeneralInfo.Major < 2)
                throw new InvalidOperationException();

            while (writer.Position % 4 != 0)
                writer.Write((byte)0);

            writer.Write((uint)1); // Version

            base.SerializeChunk(writer);
        }

        internal override void UnserializeChunk(UndertaleReader reader)
        {
            if (reader.undertaleData.GeneralInfo.Major < 2)
                throw new InvalidOperationException();

            // Padding
            while (reader.Position % 4 != 0)
                if (reader.ReadByte() != 0)
                    throw new IOException("Padding error!");

            if (reader.ReadUInt32() != 1)
                throw new IOException("Expected SEQN version 1");

            base.UnserializeChunk(reader);
        }
    }

    // GMS2.3+ only
    public class UndertaleChunkTAGS : UndertaleSingleChunk<UndertaleTags>
    {
        public override string Name => "TAGS";

        internal override void SerializeChunk(UndertaleWriter writer)
        {
            if (writer.undertaleData.GeneralInfo.Major < 2)
                throw new InvalidOperationException();

            while (writer.Position % 4 != 0)
                writer.Write((byte)0);

            writer.Write((uint)1); // Version

            base.SerializeChunk(writer);
        }

        internal override void UnserializeChunk(UndertaleReader reader)
        {
            if (reader.undertaleData.GeneralInfo.Major < 2)
                throw new InvalidOperationException();

            // Padding
            while (reader.Position % 4 != 0)
                if (reader.ReadByte() != 0)
                    throw new IOException("Padding error!");

            if (reader.ReadUInt32() != 1)
                throw new IOException("Expected TAGS version 1");

            base.UnserializeChunk(reader);
        }
    }
}
