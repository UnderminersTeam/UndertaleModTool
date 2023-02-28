using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib.Models;
using UndertaleModLib.Util;
using static UndertaleModLib.Models.UndertaleRoom;

namespace UndertaleModLib
{
    public class UndertaleChunkFORM : UndertaleChunk
    {
        public override string Name => "FORM";

        public Dictionary<string, UndertaleChunk> Chunks = new();
        public Dictionary<Type, UndertaleChunk> ChunksTypeDict = new();

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
        public UndertaleChunkGMEN GMEN => Chunks.GetValueOrDefault("GMEN") as UndertaleChunkGMEN;
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
        public UndertaleChunkFEAT FEAT => Chunks.GetValueOrDefault("FEAT") as UndertaleChunkFEAT;

        internal override void SerializeChunk(UndertaleWriter writer)
        {
            foreach (var chunk in Chunks)
            {
                writer.Write(chunk.Value);
            }
        }

        internal override void UnserializeChunk(UndertaleReader reader)
        {
            if (Chunks.Count != 1 || Chunks.Keys.First() != "GEN8")
                Chunks.Clear();
            ChunksTypeDict.Clear();
            long startPos = reader.Position;

            // First, find the last chunk in the file because of padding changes
            // (also, calculate all present chunks while we're at it)
            reader.AllChunkNames = new List<string>();
            string lastChunk = "";
            while (reader.Position < reader.Length)
            {
                lastChunk = reader.ReadChars(4);
                reader.AllChunkNames.Add(lastChunk);
                uint length = reader.ReadUInt32();
                reader.Position += length;
            }
            reader.LastChunkName = lastChunk;
            reader.Position = startPos;

            // Now, parse the chunks
            while (reader.Position < startPos + Length)
            {
                UndertaleChunk chunk = reader.ReadUndertaleChunk();
                if (chunk != null)
                {
                    if (Chunks.ContainsKey(chunk.Name))
                    {
                        if (Chunks.Count == 1 && chunk.Name == "GEN8")
                            Chunks.Clear();
                        else
                            throw new IOException("Duplicate chunk " + chunk.Name);
                    }

                    Chunks.Add(chunk.Name, chunk);
                    ChunksTypeDict.Add(chunk.GetType(), chunk);
                }
            }
        }

        internal override uint UnserializeObjectCount(UndertaleReader reader)
        {
            uint totalCount = 0;

            long startPos = reader.Position;
            reader.AllChunkNames = new List<string>();
            while (reader.Position < reader.Length)
            {
                string chunkName = reader.ReadChars(4);
                reader.AllChunkNames.Add(chunkName);
                uint length = reader.ReadUInt32();
                reader.Position += length;
            }
            reader.Position = startPos;

            UndertaleChunkGEN8 gen8Chunk = new();
            gen8Chunk.UnserializeGeneralData(reader);
            Chunks.Add("GEN8", gen8Chunk);
            reader.Position = startPos;

            while (reader.Position < startPos + Length)
                totalCount += reader.CountChunkChildObjects();

            return totalCount;
        }
    }

    public class UndertaleChunkGEN8 : UndertaleSingleChunk<UndertaleGeneralInfo>
    {
        public override string Name => "GEN8";

        public void UnserializeGeneralData(UndertaleReader reader)
        {
            Object = new UndertaleGeneralInfo();

            reader.Position += 8; // Chunk name + length

            reader.Position++; // "IsDebuggerDisabled"
            Object.BytecodeVersion = reader.ReadByte();
            reader.undertaleData.UnsupportedBytecodeVersion
                = Object.BytecodeVersion < 13 || Object.BytecodeVersion > 17;
            reader.Bytecode14OrLower = Object.BytecodeVersion <= 14;

            reader.Position += 42;

            Object.Major = reader.ReadUInt32();
            Object.Minor = reader.ReadUInt32();
            Object.Release = reader.ReadUInt32();
            Object.Build = reader.ReadUInt32();

            var readVer = (Object.Major, Object.Minor, Object.Release, Object.Build);
            var detectedVer = UndertaleGeneralInfo.TestForCommonGMSVersions(reader, readVer);
            (Object.Major, Object.Minor, Object.Release, Object.Build) = detectedVer;
        }
    }

    public class UndertaleChunkOPTN : UndertaleSingleChunk<UndertaleOptions>
    {
        public override string Name => "OPTN";
    }

    public class UndertaleChunkLANG : UndertaleSingleChunk<UndertaleLanguage>
    {
        public override string Name => "LANG";

        internal override uint UnserializeObjectCount(UndertaleReader reader)
        {
            return 1;
        }
    }

    public class UndertaleChunkEXTN : UndertaleListChunk<UndertaleExtension>
    {
        public override string Name => "EXTN";
        public List<byte[]> productIdData = new List<byte[]>();

        private static bool checkedFor2022_6;
        private void CheckFor2022_6(UndertaleReader reader)
        {
            if (!reader.undertaleData.IsVersionAtLeast(2, 3) || reader.undertaleData.IsVersionAtLeast(2022, 6))
            {
                checkedFor2022_6 = true;
                return;
            }

            bool definitely2022_6 = true;
            long returnPosition = reader.AbsPosition;

            int extCount = reader.ReadInt32();
            if (extCount > 0)
            {
                uint firstExtPtr = reader.ReadUInt32();
                uint firstExtEndPtr = (extCount >= 2) ? reader.ReadUInt32() /* second ptr */ : (uint)(returnPosition + this.Length);

                reader.AbsPosition = firstExtPtr + 12;
                uint newPointer1 = reader.ReadUInt32();
                uint newPointer2 = reader.ReadUInt32();

                if (newPointer1 != reader.AbsPosition)
                    definitely2022_6 = false; // first pointer mismatch
                else if (newPointer2 <= reader.AbsPosition || newPointer2 >= (returnPosition + this.Length))
                    definitely2022_6 = false; // second pointer out of bounds
                else
                {
                    // Check ending position
                    reader.AbsPosition = newPointer2;
                    uint optionCount = reader.ReadUInt32();
                    if (optionCount > 0)
                    {
                        long newOffsetCheck = reader.AbsPosition + (4 * (optionCount - 1));
                        if (newOffsetCheck >= (returnPosition + this.Length))
                        {
                            // Option count would place us out of bounds
                            definitely2022_6 = false;
                        }
                        else
                        {
                            reader.Position += (4 * (optionCount - 1));
                            newOffsetCheck = reader.ReadUInt32() + 12; // jump past last option
                            if (newOffsetCheck >= (returnPosition + this.Length))
                            {
                                // Pointer list element would place us out of bounds
                                definitely2022_6 = false;
                            }
                            else
                            {
                                reader.AbsPosition = (uint)newOffsetCheck;
                            }
                        }
                    }
                    if (definitely2022_6)
                    {
                        if (extCount == 1)
                        {
                            reader.Position += 16; // skip GUID data (only one of them)
                            if (reader.AbsPosition % 16 != 0)
                                reader.Position += 16 - (reader.AbsPosition % 16); // align to chunk end
                        }
                        if (reader.AbsPosition != firstExtEndPtr)
                            definitely2022_6 = false;
                    }
                }
            }
            else
                definitely2022_6 = false;

            reader.AbsPosition = returnPosition;

            if (definitely2022_6)
                reader.undertaleData.SetGMS2Version(2022, 6);

            checkedFor2022_6 = true;
        }

        internal override void UnserializeChunk(UndertaleReader reader)
        {
            if (!checkedFor2022_6)
                CheckFor2022_6(reader);

            base.UnserializeChunk(reader);

            // Strange data for each extension, some kind of unique identifier based on
            // the product ID for each of them
            productIdData = new List<byte[]>();
            // NOTE: I do not know if 1773 is the earliest version which contains product IDs.
            if (reader.undertaleData.GeneralInfo?.Major >= 2 || (reader.undertaleData.GeneralInfo?.Major == 1 && reader.undertaleData.GeneralInfo?.Build >= 1773) || (reader.undertaleData.GeneralInfo?.Major == 1 && reader.undertaleData.GeneralInfo?.Build == 1539))
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

        internal override uint UnserializeObjectCount(UndertaleReader reader)
        {
            checkedFor2022_6 = false;

            CheckFor2022_6(reader);

            return base.UnserializeObjectCount(reader);
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

    public class UndertaleChunkBGND : UndertaleAlignUpdatedListChunk<UndertaleBackground>
    {
        public override string Name => "BGND";

        internal override void SerializeChunk(UndertaleWriter writer)
        {
            Alignment = 8;
            base.SerializeChunk(writer);
        }

        internal override void UnserializeChunk(UndertaleReader reader)
        {
            Alignment = 8;
            base.UnserializeChunk(reader);
        }
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

    public class UndertaleChunkGMEN : UndertaleSimpleListChunk<UndertaleGlobalInit>
    {
        public override string Name => "GMEN";
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
            long chunkEnd = reader.AbsPosition + chunkLength;

            long beginPosition = reader.Position;

            // Figure out where the starts/ends of each shader object are
            int count = reader.ReadInt32();
            uint[] objectLocations = new uint[count + 1];
            for (int i = 0; i < count; i++)
            {
                objectLocations[i] = (uint)reader.ReadInt32();
            }
            objectLocations[count] = (uint)chunkEnd;

            Dictionary<uint, UndertaleObject> objPool = reader.GetOffsetMap();
            Dictionary<UndertaleObject, uint> objPoolRev = reader.GetOffsetMapRev();

            // Setup base shader objects with boundaries set. Load into object pool
            // so that they don't immediately discard.
            for (int i = 0; i < count; i++)
            {
                UndertaleShader s = new UndertaleShader { EntryEnd = objectLocations[i + 1] };
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
        public byte[] Padding;

        private static bool checkedFor2022_2;
        private void CheckForGM2022_2(UndertaleReader reader)
        {
           /* This code performs four checks to identify GM2022.2.
            * First, as you've seen, is the bytecode version.
            * Second, we assume it is. If there are no Glyphs, we are vindicated by the impossibility of null values there.
            * Third, we check that the Glyph Length is less than the chunk length. If it's going outside the chunk, that means
            * that the length was misinterpreted.
            * Fourth, in case of a terrible fluke causing this to appear valid erroneously, we verify that each pointer leads into the next.
            * And if someone builds their game so the first pointer is absolutely valid length data and the next font is valid glyph data-
            * screw it, call Jacky720 when someone constructs that and you want to mod it.
            * Maybe try..catch on the whole shebang?
            */
            if (reader.undertaleData.GeneralInfo?.BytecodeVersion < 17 || reader.undertaleData.IsVersionAtLeast(2022, 2))
            {
                checkedFor2022_2 = true;
                return;
            }

            long positionToReturn = reader.Position;
            bool GMS2022_2 = false;

            if (reader.ReadUInt32() > 0) // Font count
            {
                uint firstFontPointer = reader.ReadUInt32();
                reader.AbsPosition = firstFontPointer + 48; // There are 48 bytes of existing metadata.
                uint glyphsLength = reader.ReadUInt32();
                GMS2022_2 = true;
                if ((glyphsLength * 4) > this.Length)
                {
                    GMS2022_2 = false;
                }
                else if (glyphsLength != 0)
                {
                    List<uint> glyphPointers = new List<uint>((int)glyphsLength);
                    for (uint i = 0; i < glyphsLength; i++)
                        glyphPointers.Add(reader.ReadUInt32());
                    foreach (uint pointer in glyphPointers)
                    {
                        if (reader.AbsPosition != pointer)
                        {
                            GMS2022_2 = false;
                            break;
                        }

                        reader.Position += 14;
                        ushort kerningLength = reader.ReadUInt16();
                        reader.Position += (uint)4 * kerningLength; // combining read/write would apparently break
                    }
                }

            }
            if (GMS2022_2)
                reader.undertaleData.SetGMS2Version(2022, 2);
            reader.Position = positionToReturn;

            checkedFor2022_2 = true;
        }

        internal override void SerializeChunk(UndertaleWriter writer)
        {
            base.SerializeChunk(writer);

            if (Padding == null)
            {
                for (ushort i = 0; i < 0x80; i++)
                    writer.Write(i);
                for (ushort i = 0; i < 0x80; i++)
                    writer.Write((ushort)0x3f);
            } else
                writer.Write(Padding);
        }

        internal override void UnserializeChunk(UndertaleReader reader)
        {
            if (!checkedFor2022_2)
                CheckForGM2022_2(reader);

            base.UnserializeChunk(reader);

            Padding = reader.ReadBytes(512);
        }

        internal override uint UnserializeObjectCount(UndertaleReader reader)
        {
            checkedFor2022_2 = false;

            CheckForGM2022_2(reader);

            return base.UnserializeObjectCount(reader);
        }
    }

    public class UndertaleChunkTMLN : UndertaleListChunk<UndertaleTimeline>
    {
        public override string Name => "TMLN";
    }

    public class UndertaleChunkOBJT : UndertaleListChunk<UndertaleGameObject>
    {
        public override string Name => "OBJT";

        private static bool checkedFor2022_5;

        // Simple chunk parser to check for 2022.5, assumes old format until shown otherwise
        private void CheckFor2022_5(UndertaleReader reader)
        {
            if (!reader.undertaleData.IsVersionAtLeast(2, 3) || reader.undertaleData.IsVersionAtLeast(2022, 5))
            {
                checkedFor2022_5 = true;
                return;
            }

            long positionToReturn = reader.Position;
            bool GM2022_5 = false;

            if (reader.ReadUInt32() > 0) // Object count
            {
                uint firstObjectPointer = reader.ReadUInt32();
                reader.AbsPosition = firstObjectPointer + 64;
                uint vertexCount = reader.ReadUInt32();

                // If any of these checks fail, it's 2022.5
                GM2022_5 = true;
                // Bounds check on vertex data
                if (reader.Position + 12 + vertexCount * 8 < positionToReturn + this.Length)
                {
                    reader.Position += (uint)(12 + vertexCount * 8);
                    // A pointer list of events
                    if (reader.ReadUInt32() == UndertaleGameObject.EventTypeCount)
                    {
                        uint subEventPointer = reader.ReadUInt32();
                        // Should start right after the list
                        if (reader.AbsPosition + 56 == subEventPointer)
                            GM2022_5 = false;
                    }
                }
            }
            if (GM2022_5)
                reader.undertaleData.SetGMS2Version(2022, 5);

            reader.Position = positionToReturn;

            checkedFor2022_5 = true;
        }

        internal override void SerializeChunk(UndertaleWriter writer)
        {
            base.SerializeChunk(writer);
        }

        internal override void UnserializeChunk(UndertaleReader reader)
        {
            if (!checkedFor2022_5)
                CheckFor2022_5(reader);

            base.UnserializeChunk(reader);
        }

        internal override uint UnserializeObjectCount(UndertaleReader reader)
        {
            checkedFor2022_5 = false;

            CheckFor2022_5(reader);

            return base.UnserializeObjectCount(reader);
        }
    }

    public class UndertaleChunkROOM : UndertaleListChunk<UndertaleRoom>
    {
        public override string Name => "ROOM";

        internal override void UnserializeChunk(UndertaleReader reader)
        {
            if (!checkedFor2022_1)
                CheckForEffectData(reader);

            base.UnserializeChunk(reader);
        }

        internal override uint UnserializeObjectCount(UndertaleReader reader)
        {
            checkedFor2022_1 = false;
            UndertaleRoom.CheckedForGMS2_2_2_302 = false;

            CheckForEffectData(reader);

            if (reader.undertaleData.GeneralInfo?.BytecodeVersion >= 16)
            {
                // "GameObject._preCreateCode"

                Type gameObjType = typeof(GameObject);

                uint newValue = GameObject.ChildObjectCount + 1;
                reader.SetStaticChildCount(gameObjType, newValue);
                newValue = GameObject.ChildObjectsSize + 4;
                reader.SetStaticChildObjectsSize(gameObjType, newValue);
            }

            return base.UnserializeObjectCount(reader);
        }

        private static bool checkedFor2022_1;
        private void CheckForEffectData(UndertaleReader reader)
        {
            // Do a length check on room layers to see if this is 2022.1 or higher
            if (reader.undertaleData.IsVersionAtLeast(2, 3) && !reader.undertaleData.IsVersionAtLeast(2022, 1))
            {
                long returnTo = reader.Position;

                // Iterate over all rooms until a length check is performed
                int roomCount = reader.ReadInt32();
                bool finished = false;
                for (uint roomIndex = 0; roomIndex < roomCount && !finished; roomIndex++)
                {
                    // Advance to room data we're interested in (and grab pointer for next room)
                    reader.Position = returnTo + 4 + (4 * roomIndex);
                    uint roomPtr = (uint)reader.ReadInt32();
                    reader.AbsPosition = roomPtr + (22 * 4);

                    // Get the pointer for this room's layer list, as well as pointer to sequence list
                    uint layerListPtr = (uint)reader.ReadInt32();
                    int seqnPtr = reader.ReadInt32();
                    reader.AbsPosition = layerListPtr;
                    int layerCount = reader.ReadInt32();
                    if (layerCount >= 1)
                    {
                        // Get pointer into the individual layer data (plus 8 bytes) for the first layer in the room
                        uint jumpOffset = (uint)(reader.ReadInt32() + 8);

                        // Find the offset for the end of this layer
                        int nextOffset;
                        if (layerCount == 1)
                            nextOffset = seqnPtr;
                        else
                            nextOffset = reader.ReadInt32(); // (pointer to next element in the layer list)

                        // Actually perform the length checks, depending on layer data
                        reader.AbsPosition = jumpOffset;
                        switch ((LayerType)reader.ReadInt32())
                        {
                            case LayerType.Background:
                                if (nextOffset - reader.AbsPosition > 16 * 4)
                                    reader.undertaleData.SetGMS2Version(2022, 1);
                                finished = true;
                                break;
                            case LayerType.Instances:
                                reader.Position += 6 * 4;
                                int instanceCount = reader.ReadInt32();
                                if (nextOffset - reader.AbsPosition != (instanceCount * 4))
                                    reader.undertaleData.SetGMS2Version(2022, 1);
                                finished = true;
                                break;
                            case LayerType.Assets:
                                reader.Position += 6 * 4;
                                int tileOffset = reader.ReadInt32();
                                if (tileOffset != reader.AbsPosition + 8)
                                    reader.undertaleData.SetGMS2Version(2022, 1);
                                finished = true;
                                break;
                            case LayerType.Tiles:
                                reader.Position += 7 * 4;
                                int tileMapWidth = reader.ReadInt32();
                                int tileMapHeight = reader.ReadInt32();
                                if (nextOffset - reader.AbsPosition != (tileMapWidth * tileMapHeight * 4))
                                    reader.undertaleData.SetGMS2Version(2022, 1);
                                finished = true;
                                break;
                            case LayerType.Effect:
                                reader.Position += 7 * 4;
                                int propertyCount = reader.ReadInt32();
                                if (nextOffset - reader.AbsPosition != (propertyCount * 3 * 4))
                                    reader.undertaleData.SetGMS2Version(2022, 1);
                                finished = true;
                                break;
                        }
                    }
                }

                reader.Position = returnTo;
            }

            checkedFor2022_1 = true;
        }
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

            if (writer.undertaleData.IsTPAG4ByteAligned)
            {
                // padding present in ARM platforms apparently
                while (writer.Position % 0x4 != 0)
                    writer.Write((byte)0);
            }
        }

        internal override void UnserializeChunk(UndertaleReader reader)
        {
            if (Length % 0x4 == 0)
            {
                reader.undertaleData.IsTPAG4ByteAligned = true;
            }

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

            UndertaleCode.CurrCodeIndex = 0;
            base.UnserializeChunk(reader);

            reader.InstructionArraysLengths = null;
        }

        internal override uint UnserializeObjectCount(UndertaleReader reader)
        {
            if (Length == 0)
                return 0;

            if (reader.undertaleData.UnsupportedBytecodeVersion)
                return reader.ReadUInt32();

            int codeCount = (int)reader.ReadUInt32();
            reader.Position -= 4;

            reader.GMS2BytecodeAddresses = new(codeCount);
            reader.InstructionArraysLengths = new int[codeCount];
            UndertaleCode.CurrCodeIndex = 0;

            uint count = base.UnserializeObjectCount(reader);
            reader.GMS2BytecodeAddresses.Clear();

            return count;
        }
    }

    [PropertyChanged.AddINotifyPropertyChangedInterface]
    public class UndertaleChunkVARI : UndertaleChunk
    {
        public override string Name => "VARI";

        public uint VarCount1 { get; set; }

        public uint VarCount2 { get; set; }
        public uint MaxLocalVarCount { get; set; }
        public bool DifferentVarCounts { get; set; }
        public List<UndertaleVariable> List = new List<UndertaleVariable>();

        [Obsolete]
        public uint InstanceVarCount { get => VarCount1; set => VarCount1 = value; }
        [Obsolete]
        public uint InstanceVarCountAgain { get => VarCount2; set => VarCount2 = value; }

        internal override void SerializeChunk(UndertaleWriter writer)
        {
            if (List == null)
                return;

            if (writer.undertaleData.UnsupportedBytecodeVersion)
                return;

            UndertaleInstruction.Reference<UndertaleVariable>.SerializeReferenceChain(writer, writer.undertaleData.Code, List);

            if (!writer.Bytecode14OrLower)
            {
                writer.Write(VarCount1);
                writer.Write(VarCount2);
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
            long startPosition = reader.Position;
            uint varLength;
            if (!reader.Bytecode14OrLower)
            {
                VarCount1 = reader.ReadUInt32();
                VarCount2 = reader.ReadUInt32();
                DifferentVarCounts = VarCount1 != VarCount2;
                MaxLocalVarCount = reader.ReadUInt32();
                varLength = 20;
            }
            else
                varLength = 12;
            List.Clear();
            List.Capacity = (int)(Length / varLength);
            while (reader.Position + varLength <= startPosition + Length)
                List.Add(reader.ReadUndertaleObject<UndertaleVariable>());
        }

        internal override uint UnserializeObjectCount(UndertaleReader reader)
        {
            if (Length == 0)
                return 0;

            if (reader.undertaleData.UnsupportedBytecodeVersion)
                return 0;

            if (!reader.Bytecode14OrLower)
            {
                reader.Position += 12;
                return (Length - 12) / 20;
            }
            else
                return Length / 12;
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
                long startPosition = reader.Position;
                Functions.Clear();
                Functions.SetCapacity(Length / 12);
                while (reader.Position + 12 <= startPosition + Length)
                    Functions.Add(reader.ReadUndertaleObject<UndertaleFunction>());
            }
            else
            {
                Functions = reader.ReadUndertaleObject<UndertaleSimpleList<UndertaleFunction>>();
                CodeLocals = reader.ReadUndertaleObject<UndertaleSimpleList<UndertaleCodeLocals>>();
            }
        }

        internal override uint UnserializeObjectCount(UndertaleReader reader)
        {
            if (Length == 0 && reader.undertaleData.GeneralInfo?.BytecodeVersion > 14)
                return 0;

            uint count = 0;
            
            if (!reader.Bytecode14OrLower)
            {
                count += 1 + UndertaleSimpleList<UndertaleFunction>.UnserializeChildObjectCount(reader);
                count += 1 + UndertaleSimpleList<UndertaleCodeLocals>.UnserializeChildObjectCount(reader);
            }
            else
                count = Length / 12;

            return count;
        }
    }

    public class UndertaleChunkSTRG : UndertaleAlignUpdatedListChunk<UndertaleString>
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
            while (reader.AbsPosition % 0x80 != 0)
                if (reader.ReadByte() != 0)
                    throw new IOException("Padding error in STRG");
        }

        // There's no need to check padding in "UnserializeObjectCount()"
    }

    public class UndertaleChunkTXTR : UndertaleListChunk<UndertaleEmbeddedTexture>
    {
        public override string Name => "TXTR";

        private static bool checkedFor2022_3;
        private void CheckFor2022_3And5(UndertaleReader reader)
        {
            // Detect GM2022.3
            if (reader.undertaleData.IsVersionAtLeast(2, 3) && !reader.undertaleData.IsVersionAtLeast(2022, 3))
            {
                long positionToReturn = reader.Position;

                // Check for 2022.3 format
                uint texCount = reader.ReadUInt32();
                if (texCount == 1) // If no textures exist, this could false positive.
                {
                    reader.Position += 16; // Jump to either padding or length, depending on version
                    if (reader.ReadUInt32() > 0) // Check whether it's padding or length
                        reader.undertaleData.SetGMS2Version(2022, 3);
                }
                else if (texCount > 1)
                {
                    uint firstTex = reader.ReadUInt32();
                    uint secondTex = reader.ReadUInt32();
                    if (firstTex + 16 == secondTex)
                        reader.undertaleData.SetGMS2Version(2022, 3);
                }

                if (reader.undertaleData.IsVersionAtLeast(2022, 3))
                {
                    // Also check for 2022.5 format
                    reader.Position = positionToReturn + 4;
                    for (uint i = 0; i < texCount; i++)
                    {
                        // Go to each texture, and then to each texture's data
                        reader.Position = positionToReturn + 4 + (i * 4);
                        reader.AbsPosition = reader.ReadUInt32() + 12; // go to texture, at an offset
                        reader.AbsPosition = reader.ReadUInt32(); // go to texture data
                        byte[] header = reader.ReadBytes(4);
                        if (header.SequenceEqual(UndertaleEmbeddedTexture.TexData.QOIAndBZip2Header))
                        {
                            reader.Position += 4; // skip width/height
                            bool is2022_5 = false;
                            // Now check the actual BZ2 headers
                            if (reader.ReadByte() != (byte)'B')
                                is2022_5 = true;
                            else if (reader.ReadByte() != (byte)'Z')
                                is2022_5 = true;
                            else if (reader.ReadByte() != (byte)'h')
                                is2022_5 = true;
                            else
                            {
                                reader.Position++;
                                if (reader.ReadUInt24() != 0x594131) // digits of pi... (block header)
                                    is2022_5 = true;
                                else if (reader.ReadUInt24() != 0x595326)
                                    is2022_5 = true;
                            }

                            if (is2022_5)
                                reader.undertaleData.SetGMS2Version(2022, 5);

                            // Checked one QOI+BZ2 texture. No need to check any more
                            break;
                        }
                    }
                }

                reader.Position = positionToReturn;
            }

            checkedFor2022_3 = true;
        }

        internal override void SerializeChunk(UndertaleWriter writer)
        {
            base.SerializeChunk(writer);

            // texture blobs
            if (List.Count > 0)
            {
                // Compressed size can't be bigger than maximum decompressed size
                int maxSize = List.Select(x => x.TextureData.TextureBlob?.Length ?? 0).Max();
                UndertaleEmbeddedTexture.TexData.InitSharedStream(maxSize);

                bool anythingUsesQoi = false;
                foreach (var tex in List)
                {
                    if (tex.TextureExternal && !tex.TextureLoaded)
                        continue; // don't accidentally load everything...
                    if (tex.TextureData.FormatQOI)
                    {
                        anythingUsesQoi = true;
                        break;
                    }
                }
                if (anythingUsesQoi)
                {
                    // Calculate maximum size of QOI converter buffer
                    maxSize = List.Select(x => x.TextureData.Width * x.TextureData.Height).Max()
                              * QoiConverter.MaxChunkSize + QoiConverter.HeaderSize + (writer.undertaleData.IsVersionAtLeast(2022, 3) ? 0 : 4);
                    QoiConverter.InitSharedBuffer(maxSize);
                }
            }
            foreach (UndertaleEmbeddedTexture obj in List)
                obj.SerializeBlob(writer);

            // padding
            // TODO: Maybe the padding is more global and every chunk is padded to 4 byte boundaries?
            while (writer.Position % 4 != 0)
                writer.Write((byte)0);
        }

        internal override void UnserializeChunk(UndertaleReader reader)
        {
            if (!checkedFor2022_3)
                CheckFor2022_3And5(reader);

            base.UnserializeChunk(reader);
            reader.SwitchReaderType(false);

            // texture blobs
            for (int index = 0; index < List.Count; index++)
            {
                UndertaleEmbeddedTexture obj = List[index];

                obj.UnserializeBlob(reader);
                obj.Name = new UndertaleString("Texture " + index.ToString());
            }

            // padding
            while (reader.Position % 4 != 0)
                if (reader.ReadByte() != 0)
                    throw new IOException("Padding error!");
        }

        internal override uint UnserializeObjectCount(UndertaleReader reader)
        {
            checkedFor2022_3 = false;

            CheckFor2022_3And5(reader);

            // Texture blobs are already included in the count
            return base.UnserializeObjectCount(reader);
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

        internal override uint UnserializeObjectCount(UndertaleReader reader)
        {
            // Though "UndertaleEmbeddedAudio" has dynamic child objects size, 
            // there's still no need to unserialize the count for each object.
            return reader.ReadUInt32();
        }
    }

    // GMS2 only
    public class UndertaleChunkEMBI : UndertaleSimpleListChunk<UndertaleEmbeddedImage>
    {
        public override string Name => "EMBI";

        internal override void SerializeChunk(UndertaleWriter writer)
        {
            if (!writer.undertaleData.IsGameMaker2())
                throw new InvalidOperationException();
            writer.Write((uint)1); // apparently hardcoded 1, see https://github.com/krzys-h/UndertaleModTool/issues/4#issuecomment-421844420
            base.SerializeChunk(writer);
        }

        internal override void UnserializeChunk(UndertaleReader reader)
        {
            if (!reader.undertaleData.IsGameMaker2())
                throw new InvalidOperationException();
            if (reader.ReadUInt32() != 1)
                throw new Exception("Expected EMBI version 1");
            base.UnserializeChunk(reader);
        }

        internal override uint UnserializeObjectCount(UndertaleReader reader)
        {
            if (!reader.undertaleData.IsGameMaker2())
                throw new InvalidOperationException();

            if (reader.ReadUInt32() != 1)
                throw new Exception("Expected EMBI version 1");

            return base.UnserializeObjectCount(reader);
        }
    }

    // GMS2.2.1+ only
    public class UndertaleChunkTGIN : UndertaleListChunk<UndertaleTextureGroupInfo>
    {
        public override string Name => "TGIN";

        private static bool checkedFor2022_9;
        private void CheckFor2022_9(UndertaleReader reader)
        {
            if (!reader.undertaleData.IsVersionAtLeast(2, 3) || reader.undertaleData.IsVersionAtLeast(2022, 9))
            {
                checkedFor2022_9 = true;
                return;
            }

            // Check for 2022.9
            long returnPosition = reader.AbsPosition;

            uint tginCount = reader.ReadUInt32();
            if (tginCount > 0)
            {
                uint tginPtr = reader.ReadUInt32();
                uint secondTginPtr = (tginCount >= 2) ? reader.ReadUInt32() : (uint)(returnPosition + this.Length);
                reader.AbsPosition = tginPtr + 4;

                // Check to see if the pointer located at this address points within this object
                // If not, then we know we're using a new format!
                uint ptr = reader.ReadUInt32();
                if (ptr < tginPtr || ptr >= secondTginPtr)
                    reader.undertaleData.SetGMS2Version(2022, 9);
            }

            reader.AbsPosition = returnPosition;

            checkedFor2022_9 = true;
        }

        internal override void SerializeChunk(UndertaleWriter writer)
        {
            if (!writer.undertaleData.IsGameMaker2())
                throw new InvalidOperationException();

            writer.Write((uint)1); // Version

            base.SerializeChunk(writer);
        }

        internal override void UnserializeChunk(UndertaleReader reader)
        {
            if (!reader.undertaleData.IsGameMaker2())
                throw new InvalidOperationException();

            if (reader.ReadUInt32() != 1)
                throw new IOException("Expected TGIN version 1");

            if (!checkedFor2022_9)
                CheckFor2022_9(reader);

            base.UnserializeChunk(reader);
        }

        internal override uint UnserializeObjectCount(UndertaleReader reader)
        {
            checkedFor2022_9 = false;

            if (!reader.undertaleData.IsGameMaker2())
                throw new InvalidOperationException();

            if (reader.ReadUInt32() != 1)
                throw new IOException("Expected TGIN version 1");

            CheckFor2022_9(reader);

            return base.UnserializeObjectCount(reader);
        }
    }

    // GMS2.3+ only
    public class UndertaleChunkACRV : UndertaleListChunk<UndertaleAnimationCurve>
    {
        public override string Name => "ACRV";

        private static bool checkedForGMS2_3_1;
        private void CheckForGMS2_3_1(UndertaleReader reader)
        {
            if (reader.undertaleData.IsVersionAtLeast(2, 3, 1))
            {
                checkedForGMS2_3_1 = true;
                return;
            }

            long returnTo = reader.Position;

            uint count = reader.ReadUInt32();
            if (count == 0)
            {
                reader.Position = returnTo;
                checkedForGMS2_3_1 = true;
                return;
            }

            reader.AbsPosition = reader.ReadUInt32(); // go to the first "Point"
            reader.Position += 8;

            if (reader.ReadUInt32() != 0) // in 2.3 a int with the value of 0 would be set here,
            {                             // it cannot be version 2.3 if this value isn't 0
                reader.undertaleData.SetGMS2Version(2, 3, 1);
                reader.Position -= 4;
            }
            else
            {
                if (reader.ReadUInt32() == 0)                      // At all points (besides the first one)
                    reader.undertaleData.SetGMS2Version(2, 3, 1);  // if BezierX0 equals to 0 (the above check)
                reader.Position -= 8;                              // then BezierY0 equals to 0 as well (the current check)
            }

            reader.Position = returnTo;

            checkedForGMS2_3_1 = true;
        }

        internal override void SerializeChunk(UndertaleWriter writer)
        {
            if (!writer.undertaleData.IsGameMaker2())
                throw new InvalidOperationException();

            while (writer.Position % 4 != 0)
                writer.Write((byte)0);

            writer.Write((uint)1); // Version

            base.SerializeChunk(writer);
        }

        internal override void UnserializeChunk(UndertaleReader reader)
        {
            if (!reader.undertaleData.IsGameMaker2())
                throw new InvalidOperationException();

            // Padding
            while (reader.AbsPosition % 4 != 0)
                if (reader.ReadByte() != 0)
                    throw new IOException("Padding error!");

            if (reader.ReadUInt32() != 1)
                throw new IOException("Expected ACRV version 1");

            if (!checkedForGMS2_3_1)
                CheckForGMS2_3_1(reader);

            base.UnserializeChunk(reader);
        }

        internal override uint UnserializeObjectCount(UndertaleReader reader)
        {
            checkedForGMS2_3_1 = false;

            // Padding
            while (reader.AbsPosition % 4 != 0)
                if (reader.ReadByte() != 0)
                    throw new IOException("Padding error!");

            if (reader.ReadUInt32() != 1)
                throw new IOException("Expected ACRV version 1");

            CheckForGMS2_3_1(reader);

            return base.UnserializeObjectCount(reader);
        }
    }

    // GMS2.3+ only
    public class UndertaleChunkSEQN : UndertaleListChunk<UndertaleSequence>
    {
        public override string Name => "SEQN";

        internal override void SerializeChunk(UndertaleWriter writer)
        {
            if (!writer.undertaleData.IsGameMaker2())
                throw new InvalidOperationException();

            while (writer.Position % 4 != 0)
                writer.Write((byte)0);

            writer.Write((uint)1); // Version

            base.SerializeChunk(writer);
        }

        internal override void UnserializeChunk(UndertaleReader reader)
        {
            if (!reader.undertaleData.IsGameMaker2())
                throw new InvalidOperationException();

            // Apparently SEQN can be empty
            if (Length == 0)
                return;

            // Padding
            while (reader.AbsPosition % 4 != 0)
                if (reader.ReadByte() != 0)
                    throw new IOException("Padding error!");

            uint version = reader.ReadUInt32();
            if (version != 1)
                throw new IOException("Expected SEQN version 1, got " + version.ToString());

            base.UnserializeChunk(reader);
        }

        internal override uint UnserializeObjectCount(UndertaleReader reader)
        {
            if (!reader.undertaleData.IsGameMaker2())
                throw new InvalidOperationException();

            // Apparently SEQN can be empty
            if (Length == 0)
                return 0;

            // Padding
            while (reader.AbsPosition % 4 != 0)
                if (reader.ReadByte() != 0)
                    throw new IOException("Padding error!");

            uint version = reader.ReadUInt32();
            if (version != 1)
                throw new IOException("Expected SEQN version 1, got " + version.ToString());

            return base.UnserializeObjectCount(reader);
        }
    }

    // GMS2.3+ only
    public class UndertaleChunkTAGS : UndertaleSingleChunk<UndertaleTags>
    {
        public override string Name => "TAGS";

        internal override void SerializeChunk(UndertaleWriter writer)
        {
            if (!writer.undertaleData.IsGameMaker2())
                throw new InvalidOperationException();

            while (writer.Position % 4 != 0)
                writer.Write((byte)0);

            writer.Write((uint)1); // Version

            base.SerializeChunk(writer);
        }

        internal override void UnserializeChunk(UndertaleReader reader)
        {
            if (!reader.undertaleData.IsGameMaker2())
                throw new InvalidOperationException();

            // Padding
            while (reader.AbsPosition % 4 != 0)
                if (reader.ReadByte() != 0)
                    throw new IOException("Padding error!");

            if (reader.ReadUInt32() != 1)
                throw new IOException("Expected TAGS version 1");

            base.UnserializeChunk(reader);
        }

        internal override uint UnserializeObjectCount(UndertaleReader reader)
        {
            if (!reader.undertaleData.IsGameMaker2())
                throw new InvalidOperationException();

            // Padding
            while (reader.AbsPosition % 4 != 0)
                if (reader.ReadByte() != 0)
                    throw new IOException("Padding error!");

            if (reader.ReadUInt32() != 1)
                throw new IOException("Expected TAGS version 1");

            return base.UnserializeObjectCount(reader);
        }
    }

    // GMS2.3.6+ only
    public class UndertaleChunkFEDS : UndertaleListChunk<UndertaleFilterEffect>
    {
        public override string Name => "FEDS";

        internal override void SerializeChunk(UndertaleWriter writer)
        {
            if (!writer.undertaleData.IsGameMaker2())
                throw new InvalidOperationException();

            while (writer.Position % 4 != 0)
                writer.Write((byte)0);

            writer.Write((uint)1); // Version

            base.SerializeChunk(writer);
        }

        internal override void UnserializeChunk(UndertaleReader reader)
        {
            if (!reader.undertaleData.IsGameMaker2())
                throw new InvalidOperationException();

            // Padding
            while (reader.AbsPosition % 4 != 0)
                if (reader.ReadByte() != 0)
                    throw new IOException("Padding error!");

            if (reader.ReadUInt32() != 1)
                throw new IOException("Expected FEDS version 1");

            base.UnserializeChunk(reader);
        }

        internal override uint UnserializeObjectCount(UndertaleReader reader)
        {
            if (!reader.undertaleData.IsGameMaker2())
                throw new InvalidOperationException();

            // Padding
            while (reader.AbsPosition % 4 != 0)
                if (reader.ReadByte() != 0)
                    throw new IOException("Padding error!");

            uint version = reader.ReadUInt32();
            if (version != 1)
                throw new IOException("Expected FEDS version 1, got " + version.ToString());

            return base.UnserializeObjectCount(reader);
        }
    }

    // GMS2022.8+ only
    public class UndertaleChunkFEAT : UndertaleSingleChunk<UndertaleFeatureFlags>
    {
        public override string Name => "FEAT";

        internal override void SerializeChunk(UndertaleWriter writer)
        {
            if (writer.undertaleData.GeneralInfo.Major < 2)
                throw new InvalidOperationException();

            while (writer.Position % 4 != 0)
                writer.Write((byte)0);

            base.SerializeChunk(writer);
        }

        internal override void UnserializeChunk(UndertaleReader reader)
        {
            if (reader.undertaleData.GeneralInfo.Major < 2)
                throw new InvalidOperationException();

            // Padding
            while (reader.AbsPosition % 4 != 0)
                if (reader.ReadByte() != 0)
                    throw new IOException("Padding error!");

            base.UnserializeChunk(reader);
        }

        internal override uint UnserializeObjectCount(UndertaleReader reader)
        {
            if (!reader.undertaleData.IsGameMaker2())
                throw new InvalidOperationException();

            // Padding
            while (reader.AbsPosition % 4 != 0)
                if (reader.ReadByte() != 0)
                    throw new IOException("Padding error!");

            return base.UnserializeObjectCount(reader);
        }
    }
}
