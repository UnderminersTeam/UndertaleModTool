using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModLib.Models
{
    [PropertyChanged.AddINotifyPropertyChangedInterface]
    public class UndertaleGeneralInfo : UndertaleObject
    {
        [Flags]
        public enum InfoFlags : uint
        {
            Fullscreen = 0x0001,        // Start fullscreen
            SyncVertex1 = 0x0002,       // Use synchronization to avoid tearing
            SyncVertex2 = 0x0004,
            Interpolate = 0x0008,       // Interpolate colours between pixels
            Scale = 0x0010,             // Scaling: Keep aspect
            ShowCursor = 0x0020,        // Display cursor
            Sizeable = 0x0040,          // Allow window resize
            ScreenKey = 0x0080,         // Allow fullscreen switching
            SyncVertex3 = 0x0100,
            StudioVersionB1 = 0x0200,
            StudioVersionB2 = 0x0400,
            StudioVersionB3 = 0x0800,
            StudioVersionMask = 0x0E00, // studioVersion = (infoFlags & InfoFlags.StudioVersionMask) >> 9
            SteamEnabled = 0x1000,      // Enable Steam
            LocalDataEnabled = 0x2000,
            BorderlessWindow = 0x4000,  // Borderless Window
            DefaultCodeKind = 0x8000,
            LicenseExclusions = 0x10000,
        }

        [Flags]
        public enum FunctionClassification : ulong
        {
            None = 0x0UL,
            Internet = 0x1UL,
            Joystick = 0x2UL,
            Gamepad = 0x4UL,
            Immersion = 0x8UL,
            Screengrab = 0x10UL,
            Math = 0x20UL,
            Action = 0x40UL,
            MatrixD3D = 0x80UL,
            D3DModel = 0x100UL,
            DataStructures = 0x200UL,
            File = 0x400UL,
            INI = 0x800UL,
            Filename = 0x1000UL,
            Directory = 0x2000UL,
            Environment = 0x4000UL,
            UNUSED1 = 0x8000UL,
            HTTP = 0x10000UL,
            Encoding = 0x20000UL,
            UIDialog = 0x40000UL,
            MotionPlanning = 0x80000UL,
            ShapeCollision = 0x100000UL,
            Instance = 0x200000UL,
            Room = 0x400000UL,
            Game = 0x800000UL,
            Display = 0x1000000UL,
            Device = 0x2000000UL,
            Window = 0x4000000UL,
            DrawColor = 0x8000000UL,
            Texture = 0x10000000UL,
            Layer = 0x20000000UL,
            String = 0x40000000UL,
            Tiles = 0x80000000UL,
            Surface = 0x100000000UL,
            Skeleton = 0x200000000UL,
            IO = 0x400000000UL,
            Variables = 0x800000000UL,
            Array = 0x1000000000UL,
            ExternalCall = 0x2000000000UL,
            Notification = 0x4000000000UL,
            Date = 0x8000000000UL,
            Particle = 0x10000000000UL,
            Sprite = 0x20000000000UL,
            Clickable = 0x40000000000UL,
            LegacySound = 0x80000000000UL,
            Audio = 0x100000000000UL,
            Event = 0x200000000000UL,
            UNUSED2 = 0x400000000000UL,
            FreeType = 0x800000000000UL,
            Analytics = 0x1000000000000UL,
            UNUSED3 = 0x2000000000000UL,
            UNUSED4 = 0x4000000000000UL,
            Achievement = 0x8000000000000UL,
            CloudSaving = 0x10000000000000UL,
            Ads = 0x20000000000000UL,
            OS = 0x40000000000000UL,
            IAP = 0x80000000000000UL,
            Facebook = 0x100000000000000UL,
            Physics = 0x200000000000000UL,
            FlashAA = 0x400000000000000UL,
            Console = 0x800000000000000UL,
            Buffer = 0x1000000000000000UL,
            Steam = 0x2000000000000000UL,
            UNUSED5 = 2310346608841064448UL,
            Shaders = 0x4000000000000000UL,
            VertexBuffers = 9223372036854775808UL
        }

        public bool DisableDebugger { get; set; } = true;
        public byte BytecodeVersion { get; set; } = 0x10;
        public ushort Unknown { get; set; } = 0;
        public UndertaleString Filename { get; set; }
        public UndertaleString Config { get; set; }
        public uint LastObj { get; set; } = 100000;
        public uint LastTile { get; set; } = 10000000;
        public uint GameID { get; set; } = 13371337;
        public Guid DirectPlayGuid { get; set; } = Guid.Empty; // in Studio it's always empty.
        public UndertaleString Name { get; set; }
        public uint Major { get; set; } = 1;
        public uint Minor { get; set; } = 0;
        public uint Release { get; set; } = 0;
        public uint Build { get; set; } = 1337;
        public uint DefaultWindowWidth { get; set; } = 1024;
        public uint DefaultWindowHeight { get; set; } = 768;
        public InfoFlags Info { get; set; } = InfoFlags.Interpolate | InfoFlags.Scale | InfoFlags.ShowCursor | InfoFlags.ScreenKey | InfoFlags.StudioVersionB3;
        public byte[] LicenseMD5 { get; set; } = new byte[16];
        public uint LicenseCRC32 { get; set; }
        public ulong Timestamp { get; set; } = 0;
        public UndertaleString DisplayName { get; set; }
        public ulong ActiveTargets { get; set; } = 0;
        public FunctionClassification FunctionClassifications { get; set; } = FunctionClassification.None; // Initializing it with None is a very bad idea.
        public int SteamAppID { get; set; } = 0;
        public uint DebuggerPort { get; set; } = 6502;
        public UndertaleSimpleResourcesList<UndertaleRoom, UndertaleChunkROOM> RoomOrder { get; private set; } = new UndertaleSimpleResourcesList<UndertaleRoom, UndertaleChunkROOM>();

        public List<long> GMS2RandomUID { get; set; } = new List<long>(); // Some sort of checksum

        public float GMS2FPS { get; set; } = 30.0f;
        public bool GMS2AllowStatistics { get; set; } = true;
        public byte[] GMS2GameGUID { get; set; } = new byte[16]; // more high entropy data

        public void Serialize(UndertaleWriter writer)
        {
            writer.Write(DisableDebugger ? (byte)1 : (byte)0);
            writer.Write(BytecodeVersion);
            writer.Write(Unknown);
            writer.WriteUndertaleString(Filename);
            writer.WriteUndertaleString(Config);
            writer.Write(LastObj);
            writer.Write(LastTile);
            writer.Write(GameID);
            writer.Write(DirectPlayGuid.ToByteArray());
            writer.WriteUndertaleString(Name);
            writer.Write(Major);
            writer.Write(Minor);
            writer.Write(Release);
            writer.Write(Build);
            writer.Write(DefaultWindowWidth);
            writer.Write(DefaultWindowHeight);
            writer.Write((uint)Info);
            if (LicenseMD5.Length != 16)
                throw new IOException("LicenseMD5 has invalid length");
            writer.Write(LicenseMD5);
            writer.Write(LicenseCRC32);
            writer.Write(Timestamp);
            writer.WriteUndertaleString(DisplayName);

            // Should always be zero.
            writer.Write(ActiveTargets);

            // Very weird value. Decides if Runner should initialize certain subsystems.
            writer.Write((ulong)FunctionClassifications);

            writer.Write(SteamAppID);
            if (BytecodeVersion >= 14)
                writer.Write(DebuggerPort);
            writer.WriteUndertaleObject(RoomOrder);
            if (Major >= 2)
            {
                // Write random UID
                Random random = new Random((int)(Timestamp & 4294967295L));
                long firstRandom = (long)random.Next() << 32 | (long)random.Next();
                long infoNumber = (long)(Timestamp - 1000);
                ulong temp = (ulong)infoNumber;
                temp = ((temp << 56 & 18374686479671623680UL) | (temp >> 8 & 71776119061217280UL) |
                        (temp << 32 & 280375465082880UL) | (temp >> 16 & 1095216660480UL) | (temp << 8 & 4278190080UL) |
                        (temp >> 24 & 16711680UL) | (temp >> 16 & 65280UL) | (temp >> 32 & 255UL));
                infoNumber = (long)temp;
                infoNumber ^= firstRandom;
                infoNumber = ~infoNumber;
                infoNumber ^= ((long)GameID << 32 | (long)GameID);
                infoNumber ^= ((long)(DefaultWindowWidth + (int)Info) << 48 |
                               (long)(DefaultWindowHeight + (int)Info) << 32 |
                               (long)(DefaultWindowHeight + (int)Info) << 16 |
                               (long)(DefaultWindowWidth + (int)Info));
                infoNumber ^= BytecodeVersion;
                int infoLocation = Math.Abs((int)((int)(Timestamp & 65535L) / 7 + (GameID - DefaultWindowWidth) + RoomOrder.Count)) % 4;
                GMS2RandomUID.Clear();
                writer.Write(firstRandom);
                GMS2RandomUID.Add(firstRandom);
                for (int i = 0; i < 4; i++)
                {
                    if (i == infoLocation)
                    {
                        writer.Write(infoNumber);
                        GMS2RandomUID.Add(infoNumber);
                    }
                    else
                    {
                        int first = random.Next();
                        int second = random.Next();
                        writer.Write(first);
                        writer.Write(second);
                        GMS2RandomUID.Add(((long)first << 32) | (long)second);
                    }
                }
                writer.Write(GMS2FPS);
                if (GMS2GameGUID.Length != 16)
                    throw new IOException("GMS2GameGUID has invalid length");
                writer.Write(GMS2AllowStatistics);
                writer.Write(GMS2GameGUID);
            }
        }

        public void Unserialize(UndertaleReader reader)
        {
            DisableDebugger = reader.ReadByte() != 0;
            BytecodeVersion = reader.ReadByte();
            Unknown = reader.ReadUInt16();
            Filename = reader.ReadUndertaleString();
            Config = reader.ReadUndertaleString();
            LastObj = reader.ReadUInt32();
            LastTile = reader.ReadUInt32();
            GameID = reader.ReadUInt32();
            byte[] GuidData = reader.ReadBytes(16);
            DirectPlayGuid = new Guid(GuidData);
            Name = reader.ReadUndertaleString();
            Major = reader.ReadUInt32();
            Minor = reader.ReadUInt32();
            Release = reader.ReadUInt32();
            Build = reader.ReadUInt32();
            DefaultWindowWidth = reader.ReadUInt32();
            DefaultWindowHeight = reader.ReadUInt32();
            Info = (InfoFlags)reader.ReadUInt32();
            LicenseMD5 = reader.ReadBytes(16);
            LicenseCRC32 = reader.ReadUInt32();
            Timestamp = reader.ReadUInt64();
            DisplayName = reader.ReadUndertaleString();
            ActiveTargets = reader.ReadUInt64();
            FunctionClassifications = (FunctionClassification)reader.ReadUInt64();
            SteamAppID = reader.ReadInt32();
            if (BytecodeVersion >= 14)
                DebuggerPort = reader.ReadUInt32();
            RoomOrder = reader.ReadUndertaleObject<UndertaleSimpleResourcesList<UndertaleRoom, UndertaleChunkROOM>>();
            if (Major >= 2)
            {
                // Begin parsing random UID, and verify it based on original algorithm
                GMS2RandomUID = new List<long>();
                Random random = new Random((int)(Timestamp & 4294967295L));
                long firstRandom = (long)random.Next() << 32 | (long)random.Next();
                if (reader.ReadInt64() != firstRandom)
                {
                    //throw new IOException("Unexpected random UID");
                }
                long infoNumber = (long)(Timestamp - 1000);
                ulong temp = (ulong)infoNumber;
                temp = ((temp << 56 & 18374686479671623680UL) | (temp >> 8 & 71776119061217280UL) |
                        (temp << 32 & 280375465082880UL) | (temp >> 16 & 1095216660480UL) | (temp << 8 & 4278190080UL) |
                        (temp >> 24 & 16711680UL) | (temp >> 16 & 65280UL) | (temp >> 32 & 255UL));
                infoNumber = (long)temp;
                infoNumber ^= firstRandom;
                infoNumber = ~infoNumber;
                infoNumber ^= ((long)GameID << 32 | (long)GameID);
                infoNumber ^= ((long)(DefaultWindowWidth + (int)Info) << 48 |
                               (long)(DefaultWindowHeight + (int)Info) << 32 |
                               (long)(DefaultWindowHeight + (int)Info) << 16 |
                               (long)(DefaultWindowWidth + (int)Info));
                infoNumber ^= BytecodeVersion;
                int infoLocation = (int)(Math.Abs((int)(Timestamp & 65535L) / 7 + (GameID - DefaultWindowWidth) + RoomOrder.Count) % 4);
                for (int i = 0; i < 4; i++)
                {
                    if (i == infoLocation)
                    {
                        reader.ReadInt64();
                        GMS2RandomUID.Add(infoNumber);
                    }
                    else
                    {
                        reader.ReadInt64();
                        int first = random.Next();
                        int second = random.Next();
                        GMS2RandomUID.Add(((long)first << 32) | (long)second);
                    }
                }
                GMS2FPS = reader.ReadSingle();
                GMS2AllowStatistics = reader.ReadBoolean();
                GMS2GameGUID = reader.ReadBytes(16);
            }
            reader.undertaleData.UnsupportedBytecodeVersion = BytecodeVersion < 12 || BytecodeVersion > 17;
            reader.Bytecode14OrLower = BytecodeVersion <= 14;
        }

        public override string ToString()
        {
            return DisplayName + " (GMS " + Major + "." + Minor + "." + Release + "." + Build + ", bytecode " + BytecodeVersion + ")";
        }
    }

    [PropertyChanged.AddINotifyPropertyChangedInterface]
    public class UndertaleOptions : UndertaleObject
    {
        [Flags]
        public enum OptionsFlags : ulong
        {
            FullScreen = 0x1,
            InterpolatePixels = 0x2,
            UseNewAudio = 0x4,
            NoBorder = 0x8,
            ShowCursor = 0x10,
            Sizeable = 0x20,
            StayOnTop = 0x40,
            ChangeResolution = 0x80,
            NoButtons = 0x100,
            ScreenKey = 0x200,
            HelpKey = 0x400,
            QuitKey = 0x800,
            SaveKey = 0x1000,
            ScreenShotKey = 0x2000,
            CloseSec = 0x4000,
            Freeze = 0x8000,
            ShowProgress = 0x10000,
            LoadTransparent = 0x20000,
            ScaleProgress = 0x40000,
            DisplayErrors = 0x80000,
            WriteErrors = 0x100000,
            AbortErrors = 0x200000,
            VariableErrors = 0x400000,
            CreationEventOrder = 0x800000,
            UseFrontTouch = 0x1000000,
            UseRearTouch = 0x2000000,
            UseFastCollision = 0x4000000,
            FastCollisionCompatibility = 0x8000000,
            DisableSandbox = 0x10000000
        }

        public uint Unknown1 { get; set; } = 0x80000000;
        public uint Unknown2 { get; set; } = 0x00000002;
        public OptionsFlags Info { get; set; } = OptionsFlags.InterpolatePixels | OptionsFlags.UseNewAudio | OptionsFlags.ShowCursor | OptionsFlags.ScreenKey | OptionsFlags.QuitKey | OptionsFlags.SaveKey | OptionsFlags.ScreenShotKey | OptionsFlags.CloseSec | OptionsFlags.ScaleProgress | OptionsFlags.DisplayErrors | OptionsFlags.VariableErrors | OptionsFlags.CreationEventOrder;
        public int Scale { get; set; } = -1;
        public uint WindowColor { get; set; } = 0;
        public uint ColorDepth { get; set; } = 0;
        public uint Resolution { get; set; } = 0;
        public uint Frequency { get; set; } = 0;
        public uint VertexSync { get; set; } = 0;
        public uint Priority { get; set; } = 0;
        public UndertaleSprite.TextureEntry BackImage { get; set; } = new UndertaleSprite.TextureEntry(); // Apparently these exist, but I can't find any examples of it
        public UndertaleSprite.TextureEntry FrontImage { get; set; } = new UndertaleSprite.TextureEntry();
        public UndertaleSprite.TextureEntry LoadImage { get; set; } = new UndertaleSprite.TextureEntry();
        public uint LoadAlpha { get; set; } = 255;
        public UndertaleSimpleList<Constant> Constants { get; private set; } = new UndertaleSimpleList<Constant>();

        public bool NewFormat { get; set; } = true;

        [PropertyChanged.AddINotifyPropertyChangedInterface]
        public class Constant : UndertaleObject
        {
            public UndertaleString Name { get; set; }
            public UndertaleString Value { get; set; }

            public void Serialize(UndertaleWriter writer)
            {
                writer.WriteUndertaleString(Name);
                writer.WriteUndertaleString(Value);
            }

            public void Unserialize(UndertaleReader reader)
            {
                Name = reader.ReadUndertaleString();
                Value = reader.ReadUndertaleString();
            }
        }

        public void Serialize(UndertaleWriter writer)
        {
            if (NewFormat)
            {
                writer.Write(Unknown1);
                writer.Write(Unknown2);
                writer.Write((ulong)Info);
                writer.Write(Scale);
                writer.Write(WindowColor);
                writer.Write(ColorDepth);
                writer.Write(Resolution);
                writer.Write(Frequency);
                writer.Write(VertexSync);
                writer.Write(Priority);
                writer.WriteUndertaleObject(BackImage);
                writer.WriteUndertaleObject(FrontImage);
                writer.WriteUndertaleObject(LoadImage);
                writer.Write(LoadAlpha);
                writer.WriteUndertaleObject(Constants);
            } else
            {
                writer.Write((Info & OptionsFlags.FullScreen) == OptionsFlags.FullScreen);
                writer.Write((Info & OptionsFlags.InterpolatePixels) == OptionsFlags.InterpolatePixels);
                writer.Write((Info & OptionsFlags.UseNewAudio) == OptionsFlags.UseNewAudio);
                writer.Write((Info & OptionsFlags.NoBorder) == OptionsFlags.NoBorder);
                writer.Write((Info & OptionsFlags.ShowCursor) == OptionsFlags.ShowCursor);
                writer.Write(Scale);
                writer.Write((Info & OptionsFlags.Sizeable) == OptionsFlags.Sizeable);
                writer.Write((Info & OptionsFlags.StayOnTop) == OptionsFlags.StayOnTop);
                writer.Write(WindowColor);
                writer.Write((Info & OptionsFlags.ChangeResolution) == OptionsFlags.ChangeResolution);
                writer.Write(ColorDepth);
                writer.Write(Resolution);
                writer.Write(Frequency);
                writer.Write((Info & OptionsFlags.NoButtons) == OptionsFlags.NoButtons);
                writer.Write(VertexSync);
                writer.Write((Info & OptionsFlags.ScreenKey) == OptionsFlags.ScreenKey);
                writer.Write((Info & OptionsFlags.HelpKey) == OptionsFlags.HelpKey);
                writer.Write((Info & OptionsFlags.QuitKey) == OptionsFlags.QuitKey);
                writer.Write((Info & OptionsFlags.SaveKey) == OptionsFlags.SaveKey);
                writer.Write((Info & OptionsFlags.ScreenShotKey) == OptionsFlags.ScreenShotKey);
                writer.Write((Info & OptionsFlags.CloseSec) == OptionsFlags.CloseSec);
                writer.Write(Priority);
                writer.Write((Info & OptionsFlags.Freeze) == OptionsFlags.Freeze);
                writer.Write((Info & OptionsFlags.ShowProgress) == OptionsFlags.ShowProgress);
                writer.WriteUndertaleObject(BackImage);
                writer.WriteUndertaleObject(FrontImage);
                writer.WriteUndertaleObject(LoadImage);
                writer.Write((Info & OptionsFlags.LoadTransparent) == OptionsFlags.LoadTransparent);
                writer.Write(LoadAlpha);
                writer.Write((Info & OptionsFlags.ScaleProgress) == OptionsFlags.ScaleProgress);
                writer.Write((Info & OptionsFlags.DisplayErrors) == OptionsFlags.DisplayErrors);
                writer.Write((Info & OptionsFlags.WriteErrors) == OptionsFlags.WriteErrors);
                writer.Write((Info & OptionsFlags.AbortErrors) == OptionsFlags.AbortErrors);
                writer.Write((Info & OptionsFlags.VariableErrors) == OptionsFlags.VariableErrors);
                writer.Write((Info & OptionsFlags.CreationEventOrder) == OptionsFlags.CreationEventOrder);
                writer.WriteUndertaleObject(Constants);
            }
        }

        public void Unserialize(UndertaleReader reader)
        {
            NewFormat = reader.ReadInt32() == int.MinValue;
            reader.Position -= 4;
            if (NewFormat)
            {
                Unknown1 = reader.ReadUInt32();
                Unknown2 = reader.ReadUInt32();
                Info = (OptionsFlags)reader.ReadUInt64();
                Scale = reader.ReadInt32();
                WindowColor = reader.ReadUInt32();
                ColorDepth = reader.ReadUInt32();
                Resolution = reader.ReadUInt32();
                Frequency = reader.ReadUInt32();
                VertexSync = reader.ReadUInt32();
                Priority = reader.ReadUInt32();
                BackImage = reader.ReadUndertaleObject<UndertaleSprite.TextureEntry>();
                FrontImage = reader.ReadUndertaleObject<UndertaleSprite.TextureEntry>();
                LoadImage = reader.ReadUndertaleObject<UndertaleSprite.TextureEntry>();
                LoadAlpha = reader.ReadUInt32();
                Constants = reader.ReadUndertaleObject<UndertaleSimpleList<Constant>>();
            }
            else
            {
                Info = 0;
                if (reader.ReadBoolean()) Info |= OptionsFlags.FullScreen;
                if (reader.ReadBoolean()) Info |= OptionsFlags.InterpolatePixels;
                if (reader.ReadBoolean()) Info |= OptionsFlags.UseNewAudio;
                if (reader.ReadBoolean()) Info |= OptionsFlags.NoBorder;
                if (reader.ReadBoolean()) Info |= OptionsFlags.ShowCursor;
                Scale = reader.ReadInt32();
                if (reader.ReadBoolean()) Info |= OptionsFlags.Sizeable;
                if (reader.ReadBoolean()) Info |= OptionsFlags.StayOnTop;
                WindowColor = reader.ReadUInt32();
                if (reader.ReadBoolean()) Info |= OptionsFlags.ChangeResolution;
                ColorDepth = reader.ReadUInt32();
                Resolution = reader.ReadUInt32();
                Frequency = reader.ReadUInt32();
                if (reader.ReadBoolean()) Info |= OptionsFlags.NoButtons;
                VertexSync = reader.ReadUInt32();
                if (reader.ReadBoolean()) Info |= OptionsFlags.ScreenKey;
                if (reader.ReadBoolean()) Info |= OptionsFlags.HelpKey;
                if (reader.ReadBoolean()) Info |= OptionsFlags.QuitKey;
                if (reader.ReadBoolean()) Info |= OptionsFlags.SaveKey;
                if (reader.ReadBoolean()) Info |= OptionsFlags.ScreenShotKey;
                if (reader.ReadBoolean()) Info |= OptionsFlags.CloseSec;
                Priority = reader.ReadUInt32();
                if (reader.ReadBoolean()) Info |= OptionsFlags.Freeze;
                if (reader.ReadBoolean()) Info |= OptionsFlags.ShowProgress;
                BackImage = reader.ReadUndertaleObject<UndertaleSprite.TextureEntry>();
                FrontImage = reader.ReadUndertaleObject<UndertaleSprite.TextureEntry>();
                LoadImage = reader.ReadUndertaleObject<UndertaleSprite.TextureEntry>();
                if (reader.ReadBoolean()) Info |= OptionsFlags.LoadTransparent;
                LoadAlpha = reader.ReadUInt32();
                if (reader.ReadBoolean()) Info |= OptionsFlags.ScaleProgress;
                if (reader.ReadBoolean()) Info |= OptionsFlags.DisplayErrors;
                if (reader.ReadBoolean()) Info |= OptionsFlags.WriteErrors;
                if (reader.ReadBoolean()) Info |= OptionsFlags.AbortErrors;
                if (reader.ReadBoolean()) Info |= OptionsFlags.VariableErrors;
                if (reader.ReadBoolean()) Info |= OptionsFlags.CreationEventOrder;
                Constants = reader.ReadUndertaleObject<UndertaleSimpleList<Constant>>();
            }
        }
    }

    [PropertyChanged.AddINotifyPropertyChangedInterface]
    public class UndertaleLanguage : UndertaleObject
    {
        // Seems to be a list of entry IDs paired to strings for several languages
        public uint Unknown1 { get; set; } = 1;
        public uint LanguageCount { get; set; } = 0;
        public uint EntryCount { get; set; } = 0;

        public List<UndertaleString> EntryIDs { get; set; } = new List<UndertaleString>();
        public List<LanguageData> Languages { get; set; } = new List<LanguageData>();

        public void Serialize(UndertaleWriter writer)
        {
            writer.Write(Unknown1);
            LanguageCount = (uint)Languages.Count;
            writer.Write(LanguageCount);
            EntryCount = (uint)EntryIDs.Count;
            writer.Write(EntryCount);

            foreach (UndertaleString s in EntryIDs)
            {
                writer.WriteUndertaleString(s);
            }

            foreach (LanguageData ld in Languages)
            {
                ld.Serialize(writer);
            }
        }

        public void Unserialize(UndertaleReader reader)
        {
            Unknown1 = reader.ReadUInt32();
            LanguageCount = reader.ReadUInt32();
            EntryCount = reader.ReadUInt32();

            // Read the identifiers for each entry
            for (int i = 0; i < EntryCount; i++)
            {
                EntryIDs.Add(reader.ReadUndertaleString());
            }

            // Read the data for each language
            for (int i = 0; i < LanguageCount; i++)
            {
                LanguageData ld = new LanguageData();
                ld.Unserialize(reader, EntryCount);
                Languages.Add(ld);
            }
        }

        [PropertyChanged.AddINotifyPropertyChangedInterface]
        public class LanguageData
        {
            public UndertaleString Name { get; set; }
            public UndertaleString Region { get; set; }
            public List<UndertaleString> Entries { get; set; } = new List<UndertaleString>();
            // values that correspond to IDs in the main chunk content

            public void Serialize(UndertaleWriter writer)
            {
                writer.WriteUndertaleString(Name);
                writer.WriteUndertaleString(Region);
                foreach (UndertaleString s in Entries)
                {
                    writer.WriteUndertaleString(s);
                }
            }

            public void Unserialize(UndertaleReader reader, uint entryCount)
            {
                Name = reader.ReadUndertaleString();
                Region = reader.ReadUndertaleString();
                for (uint i = 0; i < entryCount; i++)
                {
                    Entries.Add(reader.ReadUndertaleString());
                }
            }
        }
    }
}
