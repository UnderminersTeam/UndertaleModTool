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
        // GMS 2.3+ for the below chunks
        public UndertaleChunkACRV ACRV => Chunks.GetValueOrDefault("ACRV") as UndertaleChunkACRV;
        public UndertaleChunkSEQN SEQN => Chunks.GetValueOrDefault("SEQN") as UndertaleChunkSEQN;
        public UndertaleChunkTAGS TAGS => Chunks.GetValueOrDefault("TAGS") as UndertaleChunkTAGS;
        public UndertaleChunkFEAT FEAT => Chunks.GetValueOrDefault("FEAT") as UndertaleChunkFEAT;
        // GMS 2.3.6+
        public UndertaleChunkFEDS FEDS => Chunks.GetValueOrDefault("FEDS") as UndertaleChunkFEDS;
        // GM 2023.2+
        public UndertaleChunkPSEM PSEM => Chunks.GetValueOrDefault("PSEM") as UndertaleChunkPSEM;
        public UndertaleChunkPSYS PSYS => Chunks.GetValueOrDefault("PSYS") as UndertaleChunkPSYS;

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

                    if (reader.ReadOnlyGEN8 && chunk.Name == "GEN8")
                        return;
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

            if (reader.AllChunkNames[0] == "GEN8")
            {
                UndertaleChunkGEN8 gen8Chunk = new();
                gen8Chunk.UnserializeGeneralData(reader);
                Chunks.Add("GEN8", gen8Chunk);

                reader.Position = startPos;
            }

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

        private static bool checkedFor2022_6, checkedFor2023_4;
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
        private void CheckFor2023_4(UndertaleReader reader)
        {
            if (!reader.undertaleData.IsVersionAtLeast(2022, 6) || reader.undertaleData.IsVersionAtLeast(2023, 4))
            {
                checkedFor2023_4 = true;
                return;
            }

            long returnPosition = reader.Position;

            int extCount = reader.ReadInt32();
            if (extCount > 0)
            {
                // Go to the first extension
                reader.AbsPosition = reader.ReadUInt32();

                // Skip the miminal amount of strings
                reader.Position += 4 * 3;

                uint filesPtr = reader.ReadUInt32();
                uint optionsPtr = reader.ReadUInt32();

                // The file list pointer should be less than the option list pointer.
                // If it's not true, then "filesPtr" is actually a string pointer, so it's GM 2023.4+.
                if (filesPtr > optionsPtr)
                    reader.undertaleData.SetGMS2Version(2023, 4);
            }

            reader.Position = returnPosition;

            checkedFor2023_4 = true;
        }

        internal override void UnserializeChunk(UndertaleReader reader)
        {
            if (!checkedFor2022_6)
                CheckFor2022_6(reader);
            if (!checkedFor2023_4)
                CheckFor2023_4(reader);

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
            CheckFor2023_4(reader);

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
        private static bool checkedFor2023_6;
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

        private void CheckForGM2023_6(UndertaleReader reader)
        {
            // This is basically the same as the 2022.2 check, but adapted for the LineHeight value instead of Ascender.
            
            // We already know whether the version is more or less than 2023.2 due to PSEM. Checking a shorter range narrows possibility of error.
            if (!reader.undertaleData.IsVersionAtLeast(2023, 2) || reader.undertaleData.IsVersionAtLeast(2023, 6))
            {
                checkedFor2023_6 = true;
                return;
            }

            long positionToReturn = reader.Position;
            bool GMS2023_6 = false;

            if (reader.ReadUInt32() > 0) // Font count
            {
                uint firstFontPointer = reader.ReadUInt32();
                reader.AbsPosition = firstFontPointer + 56; // Two more values: SDFSpread and LineHeight. 48 + 4 + 4 = 56.
                uint glyphsLength = reader.ReadUInt32();
                GMS2023_6 = true;
                if ((glyphsLength * 4) > this.Length)
                {
                    GMS2023_6 = false;
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
                            GMS2023_6 = false;
                            break;
                        }

                        reader.Position += 14;
                        ushort kerningLength = reader.ReadUInt16();
                        reader.Position += (uint)4 * kerningLength; // combining read/write would apparently break
                    }
                }

            }
            if (GMS2023_6)
                reader.undertaleData.SetGMS2Version(2023, 6);
            reader.Position = positionToReturn;

            checkedFor2023_6 = true;
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

            if (!checkedFor2023_6)
                CheckForGM2023_6(reader);

            base.UnserializeChunk(reader);

            Padding = reader.ReadBytes(512);
        }

        internal override uint UnserializeObjectCount(UndertaleReader reader)
        {
            checkedFor2022_2 = false;
            checkedFor2023_6 = false;

            CheckForGM2022_2(reader);
            CheckForGM2023_6(reader);

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
                    reader.Position += 12 + vertexCount * 8;
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

            if (!checkedForGMS2_2_2_302)
                CheckForImageSpeed(reader);

            base.UnserializeChunk(reader);
        }

        internal override uint UnserializeObjectCount(UndertaleReader reader)
        {
            checkedFor2022_1 = false;
            checkedForGMS2_2_2_302 = false;

            CheckForEffectData(reader);
            CheckForImageSpeed(reader);

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
            if (!reader.undertaleData.IsVersionAtLeast(2, 3) || reader.undertaleData.IsVersionAtLeast(2022, 1))
            {
                checkedFor2022_1 = true;
                return;
            }
            long returnTo = reader.Position;

            // Iterate over all rooms until a length check is performed
            int roomCount = reader.ReadInt32();
            for (uint roomIndex = 0; roomIndex < roomCount; roomIndex++)
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
                if (layerCount <= 0)
                {
                    // No layers, try the next room
                    continue;
                }
                // Get pointer into the individual layer data (plus 8 bytes) for the first layer in the room
                int jumpOffset = reader.ReadInt32() + 8;

                // Find the offset for the end of this layer
                int nextOffset;
                if (layerCount == 1)
                    nextOffset = seqnPtr;
                else
                    nextOffset = reader.ReadInt32(); // (pointer to next element in the layer list)

                // Actually perform the length checks, depending on layer data
                reader.AbsPosition = jumpOffset;

                LayerType layerType = (LayerType)reader.ReadInt32();
                // This is the only way to repeat the loop, because each successful switch case terminates the loop
                if (!Enum.IsDefined(layerType) || layerType == LayerType.Path)
                    continue;

                switch (layerType)
                {
                    case LayerType.Background:
                        if (nextOffset - reader.AbsPosition > 16 * 4)
                            reader.undertaleData.SetGMS2Version(2022, 1);
                        break;
                    case LayerType.Instances:
                        reader.Position += 6 * 4;
                        int instanceCount = reader.ReadInt32();
                        if (nextOffset - reader.AbsPosition != (instanceCount * 4))
                            reader.undertaleData.SetGMS2Version(2022, 1);
                        break;
                    case LayerType.Assets:
                        reader.Position += 6 * 4;
                        int tileOffset = reader.ReadInt32();
                        if (tileOffset != reader.AbsPosition + 8)
                            reader.undertaleData.SetGMS2Version(2022, 1);
                        break;
                    case LayerType.Tiles:
                        reader.Position += 7 * 4;
                        int tileMapWidth = reader.ReadInt32();
                        int tileMapHeight = reader.ReadInt32();
                        if (nextOffset - reader.AbsPosition != (tileMapWidth * tileMapHeight * 4))
                            reader.undertaleData.SetGMS2Version(2022, 1);
                        break;
                    case LayerType.Effect:
                        reader.Position += 7 * 4;
                        int propertyCount = reader.ReadInt32();
                        if (nextOffset - reader.AbsPosition != (propertyCount * 3 * 4))
                            reader.undertaleData.SetGMS2Version(2022, 1);
                        break;
                }
                // Check complete, found and tested a layer.
                break;
            }

            reader.Position = returnTo;


            checkedFor2022_1 = true;
        }

        private static bool checkedForGMS2_2_2_302;
        private void CheckForImageSpeed(UndertaleReader reader)
        {
            // Check the size of the first GameObject in a room
            if (!reader.undertaleData.IsGameMaker2() || reader.undertaleData.IsVersionAtLeast(2, 2, 2, 302))
            {
                checkedForGMS2_2_2_302 = true;
                return;
            }
            long returnTo = reader.Position;

            // Iterate over all rooms until a length check is performed
            int roomCount = reader.ReadInt32();
            for (uint roomIndex = 0; roomIndex < roomCount; roomIndex++)
            {
                // Advance to room data we're interested in (and grab pointer for next room)
                reader.Position = returnTo + 4 + (4 * roomIndex);
                uint roomPtr = (uint)reader.ReadInt32();
                reader.AbsPosition = roomPtr + (12 * 4);

                // Get the pointer for this room's object list, as well as pointer to tile list
                uint objectListPtr = (uint)reader.ReadInt32();
                int tileListPtr = reader.ReadInt32();
                reader.AbsPosition = objectListPtr;
                int objectCount = reader.ReadInt32();
                if (objectCount <= 0)
                {
                    // No objects, try the next room
                    continue;
                }
                // Compare position of first object to second
                int firstPtr = reader.ReadInt32();
                int secondPtr;
                if (objectCount == 1) // Tile list starts right after, so it works as an alternate
                    secondPtr = tileListPtr;
                else
                    secondPtr = reader.ReadInt32();

                if (secondPtr - firstPtr == 48)
                {
                    reader.undertaleData.SetGMS2Version(2, 2, 2, 302);

                    uint newSize = GameObject.ChildObjectsSize + 8;
                    reader.SetStaticChildObjectsSize(typeof(GameObject), newSize);
                }
                // Check performed, one way or the other
                break;
            }

            reader.Position = returnTo;

            checkedForGMS2_2_2_302 = true;
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
            if (!reader.undertaleData.IsVersionAtLeast(2, 3) || reader.undertaleData.IsVersionAtLeast(2022, 3))
            {
                checkedFor2022_3 = true;
                return;
            }
            long positionToReturn = reader.Position;

            // Check for 2022.3 format
            bool isGM2022_3 = false;
            uint texCount = reader.ReadUInt32();
            if (texCount == 1) // If no textures exist, this could false positive.
            {
                reader.Position += 16; // Jump to either padding or length, depending on version
                if (reader.ReadUInt32() > 0) // Check whether it's padding or length
                    isGM2022_3 = true;
            }
            else if (texCount > 1)
            {
                uint firstTex = reader.ReadUInt32();
                uint secondTex = reader.ReadUInt32();
                if (firstTex + 16 == secondTex)
                    isGM2022_3 = true;
            }

            if (isGM2022_3)
            {
                reader.undertaleData.SetGMS2Version(2022, 3);
                // Also check for 2022.5 format
                reader.Position = positionToReturn + 4;
                for (uint i = 0; i < texCount; i++)
                {
                    // Go to each texture, and then to each texture's data
                    reader.Position = positionToReturn + 4 + (i * 4);
                    reader.AbsPosition = reader.ReadUInt32() + 12; // Go to texture, at an offset
                    reader.AbsPosition = reader.ReadUInt32(); // Go to texture data
                    byte[] header = reader.ReadBytes(4);
                    if (!header.SequenceEqual(UndertaleEmbeddedTexture.TexData.QOIAndBZip2Header))
                    {
                        // Nothing useful, check the next texture
                        continue;
                    }
                    reader.Position += 4; // Skip width/height
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
                        if (reader.ReadUInt24() != 0x594131) // Digits of pi... (block header)
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

            reader.Position = positionToReturn;

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

        // GMS 2.0.6.96 is the oldest available runtime version,
        // so this actually could be some other version between GMS 2.0 - 2.0.6.
        // (the oldest copy of "Zeus-Runtime.rss" on web.archive.org has this version as first one)
        private void CheckForGMS2_0_6(UndertaleReader reader)
        {
            bool atLeastGMS2_0 = reader.undertaleData.IsGameMaker2();
            if (!atLeastGMS2_0 || reader.undertaleData.IsVersionAtLeast(2, 0, 6))
                return;

            long returnPos = reader.Position;
            bool noGeneratedMips = false;

            uint count = reader.ReadUInt32();
            if (count == 0)
            {
                reader.Position = returnPos;
                return;
            }

            if (count >= 2)
            {
                uint firstPtr = reader.ReadUInt32();
                uint secondPtr = reader.ReadUInt32();

                if (atLeastGMS2_0) // 2 < version < 2.2
                {
                    if (secondPtr - firstPtr == 8) // "Scaled" + "_textureData" -> 8
                        noGeneratedMips = true;
                }
            }
            else
            {
                // Go to the first texture pointer (+ minimal texture entry size)
                reader.AbsPosition = reader.ReadUInt32() + 8;

                // If there is a zero instead of texture data pointer (which cannot be zero)
                if (reader.ReadUInt32() == 0)
                    noGeneratedMips = true;
            }

            if (!noGeneratedMips)
                reader.undertaleData.SetGMS2Version(2, 0, 6);

            reader.Position = returnPos;
        }

        internal override void UnserializeChunk(UndertaleReader reader)
        {
            if (!checkedFor2022_3)
                CheckFor2022_3And5(reader);

            CheckForGMS2_0_6(reader);

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
            // (not "AbsPosition" because of "reader.SwitchReaderType(false)")
            while (reader.Position % 4 != 0)
                if (reader.ReadByte() != 0)
                    throw new IOException("Padding error!");
        }

        internal override uint UnserializeObjectCount(UndertaleReader reader)
        {
            checkedFor2022_3 = false;

            CheckFor2022_3And5(reader);

            uint txtrSize = UndertaleEmbeddedTexture.ChildObjectsSize;
            if (reader.undertaleData.IsVersionAtLeast(2, 0, 6))
                txtrSize += 4; // "GeneratedMips"
            if (reader.undertaleData.IsVersionAtLeast(2022, 3))
                txtrSize += 4; // "TextureBlockSize"
            if (reader.undertaleData.IsVersionAtLeast(2022, 9))
                txtrSize += 12;

            if (txtrSize != UndertaleEmbeddedTexture.ChildObjectsSize)
                reader.SetStaticChildObjectsSize(typeof(UndertaleEmbeddedTexture), txtrSize);

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
        private void CheckFor2022_9And2023(UndertaleReader reader)
        {
            if (!reader.undertaleData.IsVersionAtLeast(2, 3)
                || reader.undertaleData.IsVersionAtLeast(2022, 9))
            {
                checkedFor2022_9 = true;
                return;
            }

            // Check for 2022.9
            long returnPosition = reader.AbsPosition;

            bool isGM2022_9 = false;
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
                {
                    isGM2022_9 = true;
                    reader.undertaleData.SetGMS2Version(2022, 9);
                }
            }

            reader.AbsPosition = returnPosition;

            // Check for 2023.1
            if (isGM2022_9)
            {
                reader.Position += 4; // Skip "tginCount"

                uint firstEntryPtr = reader.ReadUInt32();
                // Go to the the 4th list pointer of the first TGIN entry.
                // (either to "Fonts" or "SpineTextures" depending on the version)
                reader.AbsPosition = firstEntryPtr + 16 + (sizeof(uint) * 3); // +16 = "TexturePages" pointer
                uint fourthPtr = reader.ReadUInt32();

                // If there's a "TexturePages" count instead of the 5th list pointer.
                // The count can't be greater than the pointer.
                // (the list could be either "Tilesets" or "Fonts").
                if (reader.ReadUInt32() <= fourthPtr)
                    reader.undertaleData.SetGMS2Version(2023, 1);
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
                CheckFor2022_9And2023(reader);

            base.UnserializeChunk(reader);
        }

        internal override uint UnserializeObjectCount(UndertaleReader reader)
        {
            checkedFor2022_9 = false;

            if (!reader.undertaleData.IsGameMaker2())
                throw new InvalidOperationException();

            if (reader.ReadUInt32() != 1)
                throw new IOException("Expected TGIN version 1");

            CheckFor2022_9And2023(reader);

            return base.UnserializeObjectCount(reader);
        }
    }

    // GMS2.3+ only
    public class UndertaleChunkACRV : UndertaleListChunk<UndertaleAnimationCurve>
    {
        public override string Name => "ACRV";

        private static bool checkedForGMS2_3_1;

        // See also a similar check in UndertaleAnimationCurve.cs, necessary for embedded animation curves.
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

            reader.AbsPosition = reader.ReadUInt32(); // Go to the first "Point"
            reader.Position += 8;

            if (reader.ReadUInt32() != 0) // In 2.3 a int with the value of 0 would be set here,
            {                             // it cannot be version 2.3 if this value isn't 0
                reader.undertaleData.SetGMS2Version(2, 3, 1);
            }
            else
            {
                if (reader.ReadUInt32() == 0)                      // At all points (besides the first one)
                    reader.undertaleData.SetGMS2Version(2, 3, 1);  // if BezierX0 equals to 0 (the above check)
                                                                   // then BezierY0 equals to 0 as well (the current check)
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

    // GM2022.8+ only
    public class UndertaleChunkFEAT : UndertaleSingleChunk<UndertaleFeatureFlags>
    {
        public override string Name => "FEAT";

        internal override void SerializeChunk(UndertaleWriter writer)
        {
            if (!writer.undertaleData.IsGameMaker2())
                throw new InvalidOperationException();

            while (writer.Position % 4 != 0)
                writer.Write((byte)0);

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

    // GM2023.2+ only
    public class UndertaleChunkPSEM : UndertaleListChunk<UndertaleParticleSystemEmitter>
    {
        public override string Name => "PSEM";
        private static bool checkedPsemVersion;

        private void CheckPsemVersion(UndertaleReader reader)
        {
            // Particle system emitters had the good grace to change three times in three versions
            // Three versions which are only detectable by optional features
            // This function checks for 2023.4, 2023.6, and 2023.8
            if (reader.undertaleData.IsVersionAtLeast(2023, 8))
            {
                checkedPsemVersion = true;
                return;
            }

            long positionToReturn = reader.AbsPosition;

            uint count = reader.ReadUInt32();

            if (count < 11) // 2023.2 automatically adds eleven, later versions don't
            {
                if (!reader.undertaleData.IsVersionAtLeast(2023, 4))
                    reader.undertaleData.SetGMS2Version(2023, 4);
            }

            if (count == 0) // Nothing more to do here, unfortunately
            {
                reader.AbsPosition = positionToReturn;
                checkedPsemVersion = true;
                return;
            }
            else if (count == 1) // Special case
            {
                // Fortunately, consistent padding means we need no parsing here
                if (Length == 0xF8)
                {
                    reader.undertaleData.SetGMS2Version(2023, 8);
                }
                else if (Length == 0xD8)
                {
                    // This check is probably unnecessary since there's no 2023.7 so it would, at worst, change from 2023.6 to 2023.6
                    if (!reader.undertaleData.IsVersionAtLeast(2023, 6))
                        reader.undertaleData.SetGMS2Version(2023, 6);
                }
                else if (Length == 0xC8)
                {
                    // This one is necessary, though, as it could already be 2023.6 at this point
                    if (!reader.undertaleData.IsVersionAtLeast(2023, 4))
                        reader.undertaleData.SetGMS2Version(2023, 4);
                }
                else
                {
                    reader.AbsPosition = positionToReturn;
                    throw new IOException("Unrecognized PSEM size with only one element");
                }

                reader.AbsPosition = positionToReturn;
                checkedPsemVersion = true;
                return;
            }

            // More than one emitter
            uint firstPtr = reader.ReadUInt32();
            uint secondPtr = reader.ReadUInt32();
            if (secondPtr - firstPtr == 0xEC)
            {
                reader.undertaleData.SetGMS2Version(2023, 8);
            }
            else if (secondPtr - firstPtr == 0xC0)
            {
                if (!reader.undertaleData.IsVersionAtLeast(2023, 6))
                    reader.undertaleData.SetGMS2Version(2023, 6);
            }
            else if (secondPtr - firstPtr == 0xBC)
            {
                if (!reader.undertaleData.IsVersionAtLeast(2023, 4))
                    reader.undertaleData.SetGMS2Version(2023, 4);
            }
            else if (secondPtr - firstPtr != 0xB0) // 2023.2
            {
                reader.AbsPosition = positionToReturn;
                throw new IOException("Unrecognized PSEM size with " + count + " elements");
            }
            

            reader.AbsPosition = positionToReturn;
            checkedPsemVersion = true;
        }

        internal override void SerializeChunk(UndertaleWriter writer)
        {
            if (!writer.undertaleData.IsVersionAtLeast(2023, 2))
                throw new InvalidOperationException();

            writer.Align(4);

            writer.Write((uint)1); // Version

            base.SerializeChunk(writer);
        }

        internal override void UnserializeChunk(UndertaleReader reader)
        {
            if (!reader.undertaleData.IsVersionAtLeast(2023, 2))
                throw new InvalidOperationException();

            // Padding
            reader.Align(4);

            if (reader.ReadUInt32() != 1)
                throw new IOException("Expected PSEM version 1");

            if (!checkedPsemVersion)
                CheckPsemVersion(reader);

            base.UnserializeChunk(reader);
        }

        internal override uint UnserializeObjectCount(UndertaleReader reader)
        {
            checkedPsemVersion = false;

            if (!reader.undertaleData.IsVersionAtLeast(2023, 2))
                throw new InvalidOperationException();

            // Padding
            reader.Align(4);

            uint version = reader.ReadUInt32();
            if (version != 1)
                throw new IOException("Expected PSEM version 1, got " + version.ToString());

            CheckPsemVersion(reader);

            return base.UnserializeObjectCount(reader);
        }
    }
    public class UndertaleChunkPSYS : UndertaleListChunk<UndertaleParticleSystem>
    {
        public override string Name => "PSYS";

        internal override void SerializeChunk(UndertaleWriter writer)
        {
            if (!writer.undertaleData.IsVersionAtLeast(2023, 2))
                throw new InvalidOperationException();

            writer.Align(4);

            writer.Write((uint)1); // Version

            base.SerializeChunk(writer);
        }

        internal override void UnserializeChunk(UndertaleReader reader)
        {
            if (!reader.undertaleData.IsVersionAtLeast(2023, 2))
                throw new InvalidOperationException();

            // Padding
            reader.Align(4);

            if (reader.ReadUInt32() != 1)
                throw new IOException("Expected PSYS version 1");

            base.UnserializeChunk(reader);
        }

        internal override uint UnserializeObjectCount(UndertaleReader reader)
        {
            if (!reader.undertaleData.IsVersionAtLeast(2023, 2))
                throw new InvalidOperationException();

            // Padding
            reader.Align(4);

            uint version = reader.ReadUInt32();
            if (version != 1)
                throw new IOException("Expected PSYS version 1, got " + version.ToString());

            return base.UnserializeObjectCount(reader);
        }
    }
}
