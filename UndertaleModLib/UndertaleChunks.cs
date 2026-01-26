using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using UndertaleModLib.Models;
using UndertaleModLib.Util;
using static UndertaleModLib.Models.UndertaleRoom;

namespace UndertaleModLib
{
    public class UndertaleChunkFORM : UndertaleChunk
    {
        public override string Name => "FORM";

        /// <summary>
        /// Lookup from a chunk name to its loaded instance.
        /// </summary>
        public Dictionary<string, UndertaleChunk> Chunks = new();

        /// <summary>
        /// Lookup from a chunk type to its loaded instance.
        /// </summary>
        public Dictionary<Type, UndertaleChunk> ChunksTypeDict = new();

        /// <summary>
        /// Constructors for all chunk types.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, Func<UndertaleChunk>> ChunkConstructors = new Dictionary<string, Func<UndertaleChunk>>()
        {
            { "GEN8", () => new UndertaleChunkGEN8() },
            { "OPTN", () => new UndertaleChunkOPTN() },
            { "LANG", () => new UndertaleChunkLANG() },
            { "EXTN", () => new UndertaleChunkEXTN() },
            { "SOND", () => new UndertaleChunkSOND() },
            { "AGRP", () => new UndertaleChunkAGRP() },
            { "SPRT", () => new UndertaleChunkSPRT() },
            { "BGND", () => new UndertaleChunkBGND() },
            { "PATH", () => new UndertaleChunkPATH() },
            { "SCPT", () => new UndertaleChunkSCPT() },
            { "GLOB", () => new UndertaleChunkGLOB() },
            { "GMEN", () => new UndertaleChunkGMEN() },
            { "SHDR", () => new UndertaleChunkSHDR() },
            { "FONT", () => new UndertaleChunkFONT() },
            { "TMLN", () => new UndertaleChunkTMLN() },
            { "OBJT", () => new UndertaleChunkOBJT() },
            { "ROOM", () => new UndertaleChunkROOM() },
            { "UILR", () => new UndertaleChunkUILR() },
            { "DAFL", () => new UndertaleChunkDAFL() },
            { "EMBI", () => new UndertaleChunkEMBI() },
            { "TPAG", () => new UndertaleChunkTPAG() },
            { "TGIN", () => new UndertaleChunkTGIN() },
            { "CODE", () => new UndertaleChunkCODE() },
            { "VARI", () => new UndertaleChunkVARI() },
            { "FUNC", () => new UndertaleChunkFUNC() },
            { "STRG", () => new UndertaleChunkSTRG() },
            { "TXTR", () => new UndertaleChunkTXTR() },
            { "AUDO", () => new UndertaleChunkAUDO() },
            { "ACRV", () => new UndertaleChunkACRV() },
            { "SEQN", () => new UndertaleChunkSEQN() },
            { "TAGS", () => new UndertaleChunkTAGS() },
            { "FEAT", () => new UndertaleChunkFEAT() },
            { "FEDS", () => new UndertaleChunkFEDS() },
            { "PSEM", () => new UndertaleChunkPSEM() },
            { "PSYS", () => new UndertaleChunkPSYS() },
        };

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
        public UndertaleChunkUILR UILR => Chunks.GetValueOrDefault("UILR") as UndertaleChunkUILR;
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
                if (chunk is not null)
                {
                    if (!Chunks.ContainsKey(chunk.Name))
                    {
                        throw new IOException($"Missed chunk on object count pass \"{chunk.Name}\"");
                    }

                    if (reader.ReadOnlyGEN8 && chunk.Name == "GEN8")
                    {
                        return;
                    }
                }
            }

            if (reader.undertaleData.IsVersionAtLeast(2023, 1) &&
                reader.undertaleData.GeneralInfo.Branch == UndertaleGeneralInfo.BranchType.Pre2022_0)
            {
                reader.undertaleData.SetLTS(true);
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

            // Read some basic data from GEN8 for version info, etc.
            if (reader.AllChunkNames[0] == "GEN8")
            {
                UndertaleChunkGEN8 gen8Chunk = new();
                gen8Chunk.UnserializeGeneralData(reader);
                Chunks.Add(gen8Chunk.Name, gen8Chunk);
                ChunksTypeDict.Add(gen8Chunk.GetType(), gen8Chunk);

                reader.Position = startPos;
            }

            // Read object counts for all chunks
            while (reader.Position < startPos + Length)
            {
                (uint count, UndertaleChunk chunk) = reader.CountChunkChildObjects();
                totalCount += count;

                // Don't register a new chunk for GEN8 specifically
                if (chunk.Name != "GEN8")
                {
                    Chunks.Add(chunk.Name, chunk);
                    ChunksTypeDict.Add(chunk.GetType(), chunk);
                }
            }

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

            var readVer = (Object.Major, Object.Minor, Object.Release, Object.Build, Object.Branch);
            var detectedVer = UndertaleGeneralInfo.TestForCommonGMSVersions(reader, readVer);
            (Object.Major, Object.Minor, Object.Release, Object.Build, Object.Branch) = detectedVer;
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

        private bool checkedFor2022_6 = false;
        private bool checkedFor2023_4 = false;
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

                // Skip the minimal amount of strings
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
            if (UndertaleExtension.ProductDataEligible(reader.undertaleData))
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

        private bool checkedFor2024_6 = false;
        private void CheckForGM2024_6(UndertaleReader reader)
        {
            if (!reader.undertaleData.IsNonLTSVersionAtLeast(2023, 2) || reader.undertaleData.IsVersionAtLeast(2024, 6))
            {
                checkedFor2024_6 = true;
                return;
            }

            long returnTo = reader.Position;

            uint possibleSoundCount = reader.ReadUInt32();
            List<uint> soundPtrs = new();
            if (possibleSoundCount > 0)
            {
                soundPtrs.Capacity = (int)possibleSoundCount;
                for (int i = 0; i < possibleSoundCount; i++)
                {
                    uint soundPtr = reader.ReadUInt32();
                    if (soundPtr == 0)
                        continue;
                    soundPtrs.Add(soundPtr);
                }
            }
            if (soundPtrs.Count >= 2)
            {
                // If first sound's theoretical (old) end offset is below the start offset of
                // the next sound by exactly 4 bytes, then this is 2024.6.
                if ((soundPtrs[0] + (4 * 9)) == (soundPtrs[1] - 4))
                {
                    reader.undertaleData.SetGMS2Version(2024, 6);
                }
            }
            else if (soundPtrs.Count == 1)
            {
                // If there's a nonzero value where padding should be at the
                // end of the sound, then this is 2024.6.
                reader.AbsPosition = soundPtrs[0] + (4 * 9);
                if ((reader.AbsPosition % 16) != 4)
                {
                    // If this occurs, then something weird has happened at the start of the chunk?
                    throw new IOException("Expected to be on specific alignment at this point");
                }
                if (reader.ReadUInt32() != 0)
                {
                    reader.undertaleData.SetGMS2Version(2024, 6);
                }
            }

            reader.Position = returnTo;
            checkedFor2024_6 = true;
        }

        internal override void UnserializeChunk(UndertaleReader reader)
        {
            if (!checkedFor2024_6)
                CheckForGM2024_6(reader);

            base.UnserializeChunk(reader);
        }

        internal override uint UnserializeObjectCount(UndertaleReader reader)
        {
            checkedFor2024_6 = false;

            CheckForGM2024_6(reader);

            return base.UnserializeObjectCount(reader);
        }
    }

    public class UndertaleChunkAGRP : UndertaleListChunk<UndertaleAudioGroup>
    {
        public override string Name => "AGRP";

        private bool checkedFor2024_14 = false;
        private void CheckForGM2024_14(UndertaleReader reader)
        {
            checkedFor2024_14 = true;

            // Only perform check if at least 2024.13 (trivially detected by now), and if not already detected
            if (!reader.undertaleData.IsVersionAtLeast(2024, 13) || reader.undertaleData.IsVersionAtLeast(2024, 14))
            {
                return;
            }

            // Check for new field added in 2024.14
            long returnTo = reader.Position;
            long chunkEndPos = reader.AbsPosition + Length;

            uint agrpCount = reader.ReadUInt32();
            if (agrpCount == 0)
            {
                // No way to check when there's no audio groups... abort
                reader.Position = returnTo;
                return;
            }

            // Scan for up to two valid audio group pointers
            uint firstGroupPosition = 0, secondGroupPosition = 0;

            // Scan until we find a non-null first pointer...
            uint i = 0;
            while (i < agrpCount)
            {
                firstGroupPosition = reader.ReadUInt32();
                i++;

                if (firstGroupPosition != 0)
                {
                    break;
                }
            }

            // Scan until we find a non-null second pointer...
            while (i < agrpCount)
            {
                secondGroupPosition = reader.ReadUInt32();
                i++;

                if (secondGroupPosition != 0)
                {
                    break;
                }    
            }

            // Handle 0 audio groups (can't check anything)
            if (firstGroupPosition == 0)
            {
                reader.Position = returnTo;
                return;
            }

            // Separately handle the case with 1 audio group only, and cases with at least 2
            if (secondGroupPosition == 0)
            {
                // Look for non-null bytes in the 4 bytes after the audio group name (and within bounds of the chunk)
                reader.AbsPosition = firstGroupPosition + 4;

                // Make sure the new field can fit in the remaining chunk space
                if ((reader.AbsPosition + 4) > chunkEndPos)
                {
                    reader.Position = returnTo;
                    return;
                }

                // If the field data is zero, it's not 2024.14
                uint pathPtr = reader.ReadUInt32();
                if (pathPtr == 0)
                {
                    reader.Position = returnTo;
                    return;
                }
            }
            else
            {
                // Compare offsets of two audio groups. If the difference is 4, then it's not 2024.14.
                // Otherwise, it is at least 2024.14.
                if ((secondGroupPosition - firstGroupPosition) == 4)
                {
                    reader.Position = returnTo;
                    return;
                }
            }

            // 2024.14 detected
            reader.Position = returnTo;
            reader.undertaleData.SetGMS2Version(2024, 14);
            uint newSize = UndertaleAudioGroup.ChildObjectsSize + 4;
            reader.SetStaticChildObjectsSize(typeof(UndertaleAudioGroup), newSize);
        }

        internal override void UnserializeChunk(UndertaleReader reader)
        {
            if (!checkedFor2024_14)
                CheckForGM2024_14(reader);

            base.UnserializeChunk(reader);
        }

        internal override uint UnserializeObjectCount(UndertaleReader reader)
        {
            checkedFor2024_14 = false;
            CheckForGM2024_14(reader);

            return base.UnserializeObjectCount(reader);
        }
    }

    public class UndertaleChunkSPRT : UndertaleListChunk<UndertaleSprite>
    {
        public override string Name => "SPRT";

        private bool checkedFor2024_6 = false;
        private void CheckForGM2024_6(UndertaleReader reader)
        {
            if (!reader.undertaleData.IsNonLTSVersionAtLeast(2023, 2) || reader.undertaleData.IsVersionAtLeast(2024, 6))
            {
                checkedFor2024_6 = true;
                return;
            }

            long returnTo = reader.Position;
            long chunkStartPos = reader.AbsPosition;

            // Calculate the expected end position of the first sprite where the bbox size differs from width/height
            uint spriteCount = reader.ReadUInt32();
            for (int i = 0; i < spriteCount; i++)
            {
                // Go to sprite's start position
                reader.Position = returnTo + 4 + (4 * i);
                uint spritePtr = reader.ReadUInt32();
                if (spritePtr == 0)
                    continue;
                uint nextSpritePtr = 0;
                int j = i;
                while (nextSpritePtr == 0 && (++j) < spriteCount)
                    nextSpritePtr = reader.ReadUInt32();
                reader.AbsPosition = spritePtr + 4; // Skip past "Name"

                // Check if bbox size differs from width/height
                uint width = reader.ReadUInt32();
                uint height = reader.ReadUInt32();
                int marginLeft = reader.ReadInt32();
                int marginRight = reader.ReadInt32();
                int marginBottom = reader.ReadInt32();
                int marginTop = reader.ReadInt32();
                (int bboxWidth, int bboxHeight) = UndertaleSprite.CalculateBboxMaskDimensions(marginRight, marginLeft, marginBottom, marginTop);
                (int normalWidth, int normalHeight) = UndertaleSprite.CalculateFullMaskDimensions((int)width, (int)height);
                if (bboxWidth == normalWidth && bboxHeight == normalHeight)
                {
                    // We can't determine anything from this sprite
                    continue;
                }
                
                reader.Position += 28;

                if (reader.ReadInt32() != -1)
                {
                    throw new IOException("Expected special sprite type");
                }

                uint sVersion = reader.ReadUInt32();
                UndertaleSprite.SpriteType sSpriteType = (UndertaleSprite.SpriteType)reader.ReadUInt32();

                if (sSpriteType != UndertaleSprite.SpriteType.Normal)
                {
                    // We can't determine anything from this sprite
                    continue;
                }

                reader.Position += 8; // Playback speed values

                if (sVersion != 3)
                {
                    throw new IOException("Expected sprite version 3");
                }
                uint sequenceOffset = reader.ReadUInt32();
                uint nineSliceOffset = reader.ReadUInt32();

                // Skip past texture pointers
                uint textureCount = reader.ReadUInt32();
                reader.Position += textureCount * 4;

                // Calculate how much space the "full" and "bbox" mask data take up
                uint maskCount = reader.ReadUInt32();
                if (maskCount == 0)
                {
                    // We can't determine anything from this sprite
                    continue;
                }
                uint fullLength = (uint)((normalWidth + 7) / 8 * normalHeight);
                fullLength *= maskCount;
                if ((fullLength % 4) != 0)
                    fullLength += (4 - (fullLength % 4));
                uint bboxLength = (uint)((bboxWidth + 7) / 8 * bboxHeight);
                bboxLength *= maskCount;
                if ((bboxLength % 4) != 0)
                    bboxLength += (4 - (bboxLength % 4));

                // Calculate expected end offset
                long expectedEndOffset;
                bool endOffsetLenient = false;
                if (sequenceOffset != 0)
                {
                    expectedEndOffset = sequenceOffset;
                }
                else if (nineSliceOffset != 0)
                {
                    expectedEndOffset = nineSliceOffset;
                }
                else if (nextSpritePtr != 0)
                {
                    expectedEndOffset = nextSpritePtr;
                }
                else
                {
                    // Use chunk length, and be lenient with it (due to chunk padding)
                    endOffsetLenient = true;
                    expectedEndOffset = chunkStartPos + Length;
                }

                // If the "full" mask data runs past the expected end offset, and the "bbox" mask data does not, then this is 2024.6.
                // Otherwise, stop processing and assume this is not 2024.6.
                long fullEndPos = (reader.AbsPosition + fullLength);
                if (fullEndPos == expectedEndOffset)
                {
                    // "Full" mask data is valid
                    break;
                }
                if (endOffsetLenient && (fullEndPos % 16) != 0 && fullEndPos + (16 - (fullEndPos % 16)) == expectedEndOffset)
                {
                    // "Full" mask data doesn't exactly line up, but works if rounded up to the next chunk padding
                    break;
                }

                long bboxEndPos = (reader.AbsPosition + bboxLength);
                if (bboxEndPos == expectedEndOffset)
                {
                    // "Bbox" mask data is valid
                    reader.undertaleData.SetGMS2Version(2024, 6);
                    break;
                }
                if (endOffsetLenient && (bboxEndPos % 16) != 0 && bboxEndPos + (16 - (bboxEndPos % 16)) == expectedEndOffset)
                {
                    // "Bbox" mask data doesn't exactly line up, but works if rounded up to the next chunk padding
                    reader.undertaleData.SetGMS2Version(2024, 6);
                    break;
                }

                // Neither option seems to have worked...
                throw new IOException("Failed to detect mask type in 2024.6 detection");
            }

            reader.Position = returnTo;
            checkedFor2024_6 = true;
        }

        internal override void UnserializeChunk(UndertaleReader reader)
        {
            if (!checkedFor2024_6)
                CheckForGM2024_6(reader);

            base.UnserializeChunk(reader);
        }

        internal override uint UnserializeObjectCount(UndertaleReader reader)
        {
            checkedFor2024_6 = false;

            CheckForGM2024_6(reader);

            return base.UnserializeObjectCount(reader);
        }
    }

    public class UndertaleChunkBGND : UndertaleAlignUpdatedListChunk<UndertaleBackground>
    {
        public override string Name => "BGND";

        private bool checkedFor2024_14_1 = false;
        private void CheckForGM2024_14_1(UndertaleReader reader)
        {
            checkedFor2024_14_1 = true;

            if (!reader.undertaleData.IsVersionAtLeast(2024, 13) || reader.undertaleData.IsVersionAtLeast(2024, 14, 1))
            {
                return;
            }

            long returnTo = reader.Position;
            long chunkStartPos = reader.AbsPosition;

            // Go through each background, and check to see if it ends at the expected position. If not, this is probably 2024.14.1.
            uint bgCount = reader.ReadUInt32();
            for (int i = 0; i < bgCount; i++)
            {
                // Find background's start position, and calculate next background position (if available).
                reader.Position = returnTo + 4 + (4 * i);
                uint bgPtr = reader.ReadUInt32();
                if (bgPtr == 0)
                {
                    // Removed asset
                    continue;
                }
                uint nextBgPtr = 0;
                int j = i;
                while (nextBgPtr == 0 && (++j) < bgCount)
                {
                    // Try next pointer in list
                    nextBgPtr = reader.ReadUInt32();
                }

                // Skip all the way to "GMS2ItemsPerTileCount" (at its pre-2024.14.1 location), which is what we actually care about.
                reader.AbsPosition = bgPtr + (11 * 4);
                uint itemsPerTileCount = reader.ReadUInt32();
                uint tileCount = reader.ReadUInt32();

                // Calculate the theoretical end position given the above info, and compare to the actual end position (with padding).
                uint theoreticalEndPos = bgPtr + (15 * 4) + (itemsPerTileCount * tileCount * 4);
                if (nextBgPtr == 0)
                {
                    // Align to 16 bytes, and compare against chunk end position
                    if ((theoreticalEndPos % 16) != 0)
                    {
                        theoreticalEndPos += 16 - (theoreticalEndPos % 16);
                    }
                    uint chunkEndPos = (uint)chunkStartPos + Length;
                    if (theoreticalEndPos != chunkEndPos)
                    {
                        // Probably 2024.14.1!
                        reader.undertaleData.SetGMS2Version(2024, 14, 1);
                        break;
                    }
                }
                else
                {
                    // Align to 8 bytes, and compare against next background start position
                    if ((theoreticalEndPos % 8) != 0)
                    {
                        theoreticalEndPos += 8 - (theoreticalEndPos % 8);
                    }
                    if (theoreticalEndPos != nextBgPtr)
                    {
                        // Probably 2024.14.1!
                        reader.undertaleData.SetGMS2Version(2024, 14, 1);
                        break;
                    }
                }
            }

            reader.Position = returnTo;
        }

        internal override void SerializeChunk(UndertaleWriter writer)
        {
            Alignment = 8;
            base.SerializeChunk(writer);
        }

        internal override void UnserializeChunk(UndertaleReader reader)
        {
            Alignment = 8;

            if (!checkedFor2024_14_1)
            {
                CheckForGM2024_14_1(reader);
            }

            base.UnserializeChunk(reader);
        }

        internal override uint UnserializeObjectCount(UndertaleReader reader)
        {
            checkedFor2024_14_1 = false;

            CheckForGM2024_14_1(reader);

            return base.UnserializeObjectCount(reader);
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

        internal override void UnserializeChunk(UndertaleReader reader)
        {
            long chunkEnd = reader.AbsPosition + Length;

            long beginPosition = reader.Position;

            // Figure out where the starts/ends of each shader object are
            int count = reader.ReadInt32();
            uint[] objectLocations = new uint[count + 1];
            for (int i = 0; i < count; i++)
            {
                uint objectLocation = reader.ReadUInt32();
                if (objectLocation == 0)
                {
                    i--;
                    count--;
                    continue;
                }
                objectLocations[i] = objectLocation;
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

        private bool checkedFor2022_2 = false;
        private bool checkedFor2023_6And2024_11 = false;
        private bool checkedFor2024_14 = false;
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

            uint possibleFontCount = reader.ReadUInt32();
            if (possibleFontCount > 0)
            {
                uint firstFontPointer = 0;
                for (int i = 0; i < possibleFontCount; i++)
                {
                    uint fontPointer = reader.ReadUInt32();
                    if (fontPointer != 0)
                    {
                        firstFontPointer = fontPointer;
                        break;
                    }
                }
                if (firstFontPointer != 0)
                {
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
                        {
                            uint glyphPointer = reader.ReadUInt32();
                            if (glyphPointer == 0)
                                throw new IOException("One of the glyph pointers is null?");
                            glyphPointers.Add(glyphPointer);
                        }
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
            }
            if (GMS2022_2)
                reader.undertaleData.SetGMS2Version(2022, 2);
            reader.Position = positionToReturn;

            checkedFor2022_2 = true;
        }

        private void CheckForGM2023_6AndGM2024_11(UndertaleReader reader)
        {
            /*
                We already know whether the version is more or less than 2022.8 due to the FEAT chunk being present.
                Taking advantage of that, this is basically the same as the 2022.2 check, but it:
                - Checks for the LineHeight value instead of Ascender (added in 2023.6)
                    PSEM (2023.2) is not used, as it would return a false negative on LTS (2022.9+ equivalent with no particles).
                - Checks for UnknownAlwaysZero in Glyphs (added in 2024.11)
                    It's possible for the null pointer check planted in UTPointerList deserialisation to not be triggered:
                    for example, if SDF is enabled for any fonts, the shaders related to SDF will not be stripped;
                    it's also possible to prevent audiogroup_default from being stripped by doing
                        audio_group_name(audiogroup_default)
                    So we check for the presence of UnknownAlwaysZero as a last resort.
            */
            if (!reader.undertaleData.IsVersionAtLeast(2022, 8) ||
                (reader.undertaleData.IsVersionAtLeast(2023, 6) && !reader.undertaleData.IsVersionAtLeast(2024, 6)) ||
                reader.undertaleData.IsVersionAtLeast(2024, 11))
            {
                checkedFor2023_6And2024_11 = true;
                return;
            }

            long positionToReturn = reader.AbsPosition;
            bool GMS2023_6 = true;
            bool GMS2024_11 = false;

            uint possibleFontCount = reader.ReadUInt32();
            if (possibleFontCount <= 0)
            {
                // No way to know anything
                reader.AbsPosition = positionToReturn;
                checkedFor2023_6And2024_11 = true;
                return;
            }

            List<long> firstAndNextFontPointers = new(2);
            for (int i = 0; i < possibleFontCount; i++)
            {
                uint fontPointer = reader.ReadUInt32();
                if (fontPointer != 0)
                {
                    firstAndNextFontPointers.Add(fontPointer);
                    if (firstAndNextFontPointers.Count == 2)
                        break;
                }
            }

            if (firstAndNextFontPointers.Count == 0)
            {
                // No way to know anything
                reader.AbsPosition = positionToReturn;
                checkedFor2023_6And2024_11 = true;
                return;
            }

            if (firstAndNextFontPointers.Count == 1)
            {
                // Add in the position of the padding i.e. the end of the font list
                firstAndNextFontPointers.Add(positionToReturn + Length - 512);
            }

            reader.AbsPosition = firstAndNextFontPointers[0] + 52;      // Also the LineHeight value. 48 + 4 = 52.
            if (reader.undertaleData.IsNonLTSVersionAtLeast(2023, 2))   // SDFSpread is present from 2023.2 non-LTS onward
                reader.AbsPosition += 4;                                // (detected by PSEM/PSYS chunk existence)

            uint glyphsLength = reader.ReadUInt32();
            if (glyphsLength * 4 > firstAndNextFontPointers[1] - reader.AbsPosition)
            {
                GMS2023_6 = false;
            }
            else if (glyphsLength != 0)
            {
                List<uint> glyphPointers = new((int)glyphsLength);
                for (uint i = 0; i < glyphsLength; i++)
                {
                    uint glyphPointer = reader.ReadUInt32();
                    if (glyphPointer == 0)
                        throw new IOException("One of the glyph pointers is null?");
                    glyphPointers.Add(glyphPointer);
                }

                // When this is set to true the detection logic will not run again
                bool GMS2024_11_Failed = false;
                for (int i = 0; i < glyphPointers.Count; i++)
                {
                    if (reader.AbsPosition != glyphPointers[i])
                    {
                        GMS2023_6 = false;
                        GMS2024_11 = false;
                        break;
                    }

                    reader.Position += 14;
                    ushort kerningLength = reader.ReadUInt16();
                    if (!GMS2024_11_Failed)
                    {
                        if (!GMS2024_11)
                        {
                            // Hopefully the last thing in a UTFont is the glyph list
                            long pointerNextGlyph = i < (glyphPointers.Count - 1) ? glyphPointers[i + 1] : firstAndNextFontPointers[1];
                            // And hopefully the last thing in a glyph is the kerning list
                            // Note that we're actually skipping all items of the Glyph.Kerning SimpleList here;
                            // 4 is supposed to be the size of a GlyphKerning object
                            long pointerAfterKerningList = reader.AbsPosition + 4 * kerningLength;
                            // If we don't land on the next glyph/font after skipping the Kerning list,
                            // kerningLength is probably bogus and UnknownAlwaysZero may be present
                            if (pointerAfterKerningList != pointerNextGlyph)
                            {
                                // Discard last read, which would be of UnknownAlwaysZero
                                kerningLength = reader.ReadUInt16();
                                pointerAfterKerningList = reader.AbsPosition + 4 * kerningLength;
                                if (pointerAfterKerningList != pointerNextGlyph)
                                    reader.SubmitWarning("There appears to be more/less values than UnknownAlwaysZero before " +
                                                            "the kerning list in a UTFont.Glyph - potential data loss");
                                GMS2024_11 = true;
                            }
                            else
                            {
                                GMS2024_11_Failed = true;
                            }
                        }
                        else
                        {
                            // Discard last read, which would be of UnknownAlwaysZero
                            kerningLength = reader.ReadUInt16();
                        }
                    }
                    reader.Position += 4 * kerningLength; // combining read/write would apparently break
                }
            }

            if (GMS2024_11)
            {
                reader.undertaleData.SetGMS2Version(2024, 11);
            }
            else if (GMS2023_6)
            {
                if (!reader.undertaleData.IsVersionAtLeast(2023, 6))
                    reader.undertaleData.SetGMS2Version(2023, 6);
            }

            reader.AbsPosition = positionToReturn;
            checkedFor2023_6And2024_11 = true;
        }

        private void CheckForGM2024_14(UndertaleReader reader)
        {
            checkedFor2024_14 = true;

            if (!reader.undertaleData.IsVersionAtLeast(2024, 13) || reader.undertaleData.IsVersionAtLeast(2024, 14))
            {
                return;
            }

            // Check for new padding added (and final chunk "padding" removed) in 2024.14
            long returnTo = reader.Position;
            long chunkEndPos = reader.AbsPosition + Length;

            // Scan for up to two valid font pointers
            uint fontCount = reader.ReadUInt32();
            uint lastFontPosition = 0;

            // Scan to find the last non-null pointer...
            for (uint i = 0; i < fontCount; i++)
            {
                uint ptr = reader.ReadUInt32();
                if (ptr != 0)
                {
                    lastFontPosition = ptr;
                }
            }

            // If we have a last font, advance to the end of its data (ignoring the new alignment added in 2024.14)
            if (lastFontPosition != 0)
            {
                reader.AbsPosition = lastFontPosition + 56;

                // Advance to last glyph in pointer list
                uint glyphCount = reader.ReadUInt32();
                reader.Position += (glyphCount - 1) * 4;
                reader.AbsPosition = reader.ReadUInt32() + 16;

                // Advance past kerning
                ushort kerningCount = reader.ReadUInt16();
                reader.Position += kerningCount * 4;
            }

            // Check for the final chunk padding being missing
            if ((reader.AbsPosition + 512) > chunkEndPos)
            {
                // No padding can fit, so this is 2024.14
                reader.undertaleData.SetGMS2Version(2024, 14);
            }

            reader.Position = returnTo;
        }

        internal override void SerializeChunk(UndertaleWriter writer)
        {
            base.SerializeChunk(writer);

            if (!writer.undertaleData.IsVersionAtLeast(2024, 14))
            {
                if (Padding == null)
                {
                    for (ushort i = 0; i < 0x80; i++)
                        writer.Write(i);
                    for (ushort i = 0; i < 0x80; i++)
                        writer.Write((ushort)0x3f);
                }
                else
                {
                    writer.Write(Padding);
                }
            }
        }

        internal override void UnserializeChunk(UndertaleReader reader)
        {
            if (!checkedFor2022_2)
                CheckForGM2022_2(reader);
            if (!checkedFor2023_6And2024_11)
                CheckForGM2023_6AndGM2024_11(reader);
            if (!checkedFor2024_14)
                CheckForGM2024_14(reader);

            base.UnserializeChunk(reader);

            if (!reader.undertaleData.IsVersionAtLeast(2024, 14))
            {
                Padding = reader.ReadBytes(512);
            }
        }

        internal override uint UnserializeObjectCount(UndertaleReader reader)
        {
            checkedFor2022_2 = false;
            checkedFor2023_6And2024_11 = false;
            checkedFor2024_14 = false;

            CheckForGM2022_2(reader);
            CheckForGM2023_6AndGM2024_11(reader);
            CheckForGM2024_14(reader);

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

        private bool checkedFor2022_5 = false;

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
            if (!checkedFor2024_2)
                CheckForTileCompression(reader);

            if (!checkedFor2022_1)
                CheckForEffectData(reader);

            if (!checkedForGMS2_2_2_302)
                CheckForImageSpeed(reader);

            base.UnserializeChunk(reader);
        }

        internal override uint UnserializeObjectCount(UndertaleReader reader)
        {
            checkedFor2022_1 = false;
            checkedFor2024_2 = false;
            checkedFor2024_4 = false;
            checkedForGMS2_2_2_302 = false;

            CheckForTileCompression(reader);
            CheckForEffectData(reader);
            CheckForImageSpeed(reader);

            if (reader.undertaleData.GeneralInfo?.BytecodeVersion >= 16)
            {
                // "GameObject._preCreateCode"

                Type gameObjType = typeof(GameObject);

                uint newValue = GameObject.ChildObjectsSize + 4;
                reader.SetStaticChildObjectsSize(gameObjType, newValue);
            }

            return base.UnserializeObjectCount(reader);
        }

        private bool checkedFor2022_1 = false;
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
                if (!Enum.IsDefined(layerType) || layerType is LayerType.Path or LayerType.Path2)
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
                        if (tileOffset != reader.AbsPosition + 8 && tileOffset != reader.AbsPosition + 12)
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

        private bool checkedForGMS2_2_2_302 = false;
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

        private bool checkedFor2024_2 = false;
        private bool checkedFor2024_4 = false;
        private void CheckForTileCompression(UndertaleReader reader)
        {
            if (!reader.undertaleData.IsVersionAtLeast(2023, 2) || reader.undertaleData.IsVersionAtLeast(2024, 4))
            {
                checkedFor2024_2 = true;
                checkedFor2024_4 = true;
                return;
            }
            if (reader.undertaleData.IsVersionAtLeast(2024, 2))
            {
                checkedFor2024_2 = true;
            }

            // Do a length check on room layers to see if this is 2024.2 or higher
            long returnTo = reader.Position;

            // Iterate over all rooms
            int roomCount = reader.ReadInt32();
            bool foundAnyNonAlignedLayers = false;
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

                bool checkNextLayerOffset = false;
                for (int layerNum = 0; layerNum < layerCount; layerNum++)
                {
                    long layerPtr = layerListPtr + (4 * layerNum);
                    if (checkNextLayerOffset && layerPtr % 4 != 0)
                    {
                        foundAnyNonAlignedLayers = true;
                    }

                    reader.AbsPosition = layerPtr + 4;

                    // Get pointer into the individual layer data (plus 8 bytes)
                    int jumpOffset = reader.ReadInt32() + 8;

                    // Find the offset for the end of this layer
                    int nextOffset;
                    if (layerNum == layerCount - 1)
                        nextOffset = seqnPtr;
                    else
                        nextOffset = reader.ReadInt32(); // (pointer to next element in the layer list)

                    // Actually perform the length checks
                    reader.AbsPosition = jumpOffset;

                    LayerType layerType = (LayerType)reader.ReadInt32();
                    if (layerType != LayerType.Tiles)
                    {
                        checkNextLayerOffset = false;
                        continue;
                    }
                    checkNextLayerOffset = true;

                    reader.Position += 32;
                    int effectCount = reader.ReadInt32();
                    reader.Position += (uint)effectCount * 12 + 4;

                    int tileMapWidth = reader.ReadInt32();
                    int tileMapHeight = reader.ReadInt32();
                    if (!checkedFor2024_2 && nextOffset - reader.AbsPosition != (tileMapWidth * tileMapHeight * 4))
                    {
                        // Check complete, found and tested a layer.
                        reader.undertaleData.SetGMS2Version(2024, 2);
                        checkedFor2024_2 = true;
                    }
                }
            }

            if (!checkedFor2024_4 && reader.undertaleData.IsVersionAtLeast(2024, 2) && !foundAnyNonAlignedLayers)
            {
                // We found no layer that suggests we're not using 2024.4
                // This can rarely lead to false positives, though (in which case it's just 2024.2)
                reader.undertaleData.SetGMS2Version(2024, 4);
            }

            reader.Position = returnTo;
            checkedFor2024_2 = true;
            checkedFor2024_4 = true;
        }
    }

    public class UndertaleChunkUILR : UndertaleListChunk<UndertaleUIRootNode>
    {
        public override string Name => "UILR";
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

            if (reader.undertaleData.IsTPAG4ByteAligned)
            {
                while ((reader.Position % 4) != 0)
                {
                    if (reader.ReadByte() != 0)
                    {
                        reader.SubmitWarning("Missing expected TPAG padding");
                        reader.Position--;
                        break;
                    }
                }
            }
        }
    }

    public class UndertaleChunkCODE : UndertaleListChunk<UndertaleCode>
    {
        public override string Name => "CODE";

        /// <summary>
        /// Code count as determined during object counting.
        /// </summary>
        internal int CodeCount { get; private set; } = -1;

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
            reader.BytecodeAddresses = null;
        }

        internal override uint UnserializeObjectCount(UndertaleReader reader)
        {
            if (Length == 0) // YYC, bytecode <= 16, chunk is empty but exists
            {
                return 0;
            }

            if (reader.undertaleData.UnsupportedBytecodeVersion)
            {
                // In unsupported bytecode versions, there's no instructions parsed (so count is equivalent to the code count)
                return (uint)(CodeCount = (int)reader.ReadUInt32());
            }

            CodeCount = (int)reader.ReadUInt32();
            reader.Position -= 4;

            reader.BytecodeAddresses = new(CodeCount);
            uint count = base.UnserializeObjectCount(reader);

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
        public ObservableCollection<UndertaleVariable> List = new ObservableCollection<UndertaleVariable>();

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

            UndertaleInstruction.SerializeReferenceChain(writer, writer.undertaleData.Code, List);

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
            //List.Capacity = (int)(Length / varLength);
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

        private bool checkedFor2024_8 = false;

        private void CheckFor2024_8(UndertaleReader reader)
        {
            if (reader.undertaleData.IsVersionAtLeast(2024, 8)
                || reader.Bytecode14OrLower || Length == 0) // Irrelevant or non-deductible
            {
                checkedFor2024_8 = true;
                return;
            }

            long returnPos = reader.Position;
            long chunkEndPos = returnPos + Length;

            // The CodeLocals list was removed in 2024.8, so we check if Functions
            // is the only thing in here.
            uint funcCount = reader.ReadUInt32();
            // Skip over the (Simple)List
            // (3*4 is the size of an UndertaleFunction object)
            reader.Position += 3 * 4 * funcCount;
            if (reader.Position == chunkEndPos)
            {
                // Directly reached the end of the chunk after the function list, so code locals are *definitely* missing
                reader.undertaleData.SetGMS2Version(2024, 8);
                reader.Position = returnPos;
                checkedFor2024_8 = true;
                return;
            }

            // Then align the position
            int specAlign = reader.undertaleData.PaddingAlignException;
            int paddingBytesRead = 0;
            while ((reader.AbsPosition & ((specAlign == -1 ? 16 : specAlign) - 1)) != 0)
            {
                if (reader.Position >= chunkEndPos || reader.ReadByte() != 0)
                {
                    // If we hit a non-zero byte (or exceed chunk boundaries), it can't be padding
                    reader.Position = returnPos;
                    checkedFor2024_8 = true;
                    return;
                }
                paddingBytesRead++;
            }

            // If we're at the end of the chunk after aligning padding, code locals are either empty
            // or do not exist altogether. If we read at least 4 padding bytes, we don't know for sure
            // unless we have at least one code entry.
            if (reader.Position == chunkEndPos && (paddingBytesRead < 4 || reader.undertaleData.FORM.CODE.CodeCount > 0))
            {
                reader.undertaleData.SetGMS2Version(2024, 8);
            }

            reader.Position = returnPos;
            checkedFor2024_8 = true;
        }

        internal override void SerializeChunk(UndertaleWriter writer)
        {
            if (Functions is null && CodeLocals is null)
                return;

            UndertaleInstruction.SerializeReferenceChain(writer, writer.undertaleData.Code, Functions);

            if (writer.Bytecode14OrLower)
            {
                foreach (UndertaleFunction f in Functions)
                    writer.WriteUndertaleObject(f);
            }
            else
            {
                writer.WriteUndertaleObject(Functions);
                if (!writer.undertaleData.IsVersionAtLeast(2024, 8))
                    writer.WriteUndertaleObject(CodeLocals);
            }
        }

        internal override void UnserializeChunk(UndertaleReader reader)
        {
            if (!checkedFor2024_8)
                CheckFor2024_8(reader);

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
                CodeLocals = null;
            }
            else
            {
                Functions = reader.ReadUndertaleObject<UndertaleSimpleList<UndertaleFunction>>();
                if (!reader.undertaleData.IsVersionAtLeast(2024, 8))
                    CodeLocals = reader.ReadUndertaleObject<UndertaleSimpleList<UndertaleCodeLocals>>();
                else
                    CodeLocals = null;
            }
        }

        internal override uint UnserializeObjectCount(UndertaleReader reader)
        {
            checkedFor2024_8 = false;
            CheckFor2024_8(reader);

            if (Length == 0 && reader.undertaleData.GeneralInfo?.BytecodeVersion > 14)
                return 0;

            uint count = 0;
            
            if (!reader.Bytecode14OrLower)
            {
                count += 1 + UndertaleSimpleList<UndertaleFunction>.UnserializeChildObjectCount(reader);
                if (!reader.undertaleData.IsVersionAtLeast(2024, 8))
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

        private bool checkedFor2022_3 = false;
        private bool checkedFor2_0_6 = false;

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
                    ReadOnlySpan<byte> header = reader.ReadBytes(4);
                    if (!header.SequenceEqual(GMImage.MagicBz2Qoi))
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
            {
                checkedFor2_0_6 = true;
                return;
            }

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
            checkedFor2_0_6 = true;
        }

        internal override void UnserializeChunk(UndertaleReader reader)
        {
            long startPosition = reader.AbsPosition;

            if (!checkedFor2022_3)
                CheckFor2022_3And5(reader);

            if (!checkedFor2_0_6)
                CheckForGMS2_0_6(reader);

            base.UnserializeChunk(reader);
            reader.SwitchReaderType(false);

            // texture blobs
            for (int index = 0; index < List.Count; index++)
            {
                UndertaleEmbeddedTexture obj = List[index];
                
                // Figure out max end of stream position for the texture, if it's embedded in the file
                if (!obj.TextureExternal)
                {
                    uint recordedSize = obj.GetTextureBlockSize();
                    if (recordedSize > 0)
                    {
                        // The size is stored in the file (in modern GM versions), so use it
                        long startPositionOfTextureData = reader.GetOffsetMapRev()[obj.TextureData];
                        obj.TextureData.SetMaxEndOfStreamPosition(startPositionOfTextureData + recordedSize);
                    }
                    else
                    {
                        // Calculate maximum end stream position for this blob
                        int searchIndex = index + 1;
                        long maxEndOfStreamPosition = -1;
                        while (searchIndex < List.Count)
                        {
                            UndertaleEmbeddedTexture searchObj = List[searchIndex];

                            if (searchObj.TextureExternal)
                            {
                                // Skip this texture, as it's external
                                searchIndex++;
                                continue;
                            }

                            // Use start address of this blob
                            maxEndOfStreamPosition = reader.GetOffsetMapRev()[searchObj.TextureData];
                            break;
                        }

                        if (maxEndOfStreamPosition == -1)
                        {
                            // At end of list, so just use the end of the chunk
                            maxEndOfStreamPosition = startPosition + Length;
                        }
                        obj.TextureData.SetMaxEndOfStreamPosition(maxEndOfStreamPosition);
                    }
                }

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
            checkedFor2_0_6 = false;

            CheckFor2022_3And5(reader);
            CheckForGMS2_0_6(reader);

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
            writer.Write((uint)1); // Apparently hardcoded 1, see https://github.com/UnderminersTeam/UndertaleModTool/issues/4#issuecomment-421844420
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

        private bool checkedFor2022_9 = false;
        private void CheckFor2022_9And2023(UndertaleReader reader)
        {
            if (!reader.undertaleData.IsVersionAtLeast(2, 3)
                || reader.undertaleData.IsNonLTSVersionAtLeast(2023, 1))
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
                    if (!reader.undertaleData.IsVersionAtLeast(2022, 9))
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
                {
                    reader.undertaleData.SetGMS2Version(2023, 1, 0, 0, false);
                }
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

        private bool checkedForGMS2_3_1 = false;

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

            if (reader.ReadUInt32() != 0) // In 2.3 an int with the value of 0 would be set here,
            {                             // It cannot be version 2.3 if this value isn't 0
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
        private bool checkedPsemVersion = false;

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
                    if (!reader.undertaleData.IsVersionAtLeast(2023, 8))
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
                if (!reader.undertaleData.IsVersionAtLeast(2023, 8))
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
