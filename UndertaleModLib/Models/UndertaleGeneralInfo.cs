using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModLib.Models
{
    // TODO: INotifyPropertyChanged
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

        public bool DisableDebugger { get; set; } = true;
        public byte BytecodeVersion { get; set; } = 0x10;
        public ushort Unknown { get; set; } = 0;
        public UndertaleString Filename { get; set; }
        public UndertaleString Config { get; set; }
        public uint LastObj { get; set; } = 100000;
        public uint LastTile { get; set; } = 10000000;
        public uint GameID { get; set; } = 13371337;
        public uint Unknown1 { get; set; } = 0;
        public uint Unknown2 { get; set; } = 0;
        public uint Unknown3 { get; set; } = 0;
        public uint Unknown4 { get; set; } = 0;
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
        public uint ActiveTargets1 { get; set; } = 0;
        public uint ActiveTargets2 { get; set; } = 0;
        public uint FunctionClassifications1 { get; set; } = 0;
        public uint FunctionClassifications2 { get; set; } = 0;
        public int SteamAppID { get; set; } = 0;
        public uint DebuggerPort { get; set; } = 6502;
        public UndertaleSimpleList<RoomOrderEntry> RoomOrder { get; private set; } = new UndertaleSimpleList<RoomOrderEntry>();

        public byte[] GMS2RandomUID { get; set; } = new byte[40]; // License data or encrypted something? Has quite high entropy
        public float GMS2FPS { get; set; } = 30.0f;
        public bool GMS2AllowStatistics { get; set; } = true;
        public byte[] GMS2GameGUID { get; set; } = new byte[16]; // more high entropy data

        public class RoomOrderEntry : UndertaleObject, INotifyPropertyChanged
        {
            private UndertaleResourceById<UndertaleRoom> _Room = new UndertaleResourceById<UndertaleRoom>("ROOM");
            public UndertaleRoom Room { get => _Room.Resource; set { _Room.Resource = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Room")); } }

            public event PropertyChangedEventHandler PropertyChanged;

            public void Serialize(UndertaleWriter writer)
            {
                writer.Write(_Room.Serialize(writer));
            }

            public void Unserialize(UndertaleReader reader)
            {
                _Room.Unserialize(reader, reader.ReadInt32());
            }
        }

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
            writer.Write(Unknown1);
            writer.Write(Unknown2);
            writer.Write(Unknown3);
            writer.Write(Unknown4);
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
            writer.Write(ActiveTargets1);
            writer.Write(ActiveTargets2);
            writer.Write(FunctionClassifications1);
            writer.Write(FunctionClassifications2);
            writer.Write(SteamAppID);
            writer.Write(DebuggerPort);
            writer.WriteUndertaleObject(RoomOrder);
            if (Major >= 2)
            {
                if (GMS2RandomUID.Length != 40)
                    throw new IOException("GMS2RandomUID has invalid length");
                writer.Write(GMS2RandomUID);
                writer.Write(GMS2FPS);
                if (GMS2GameGUID.Length != 16)
                    throw new IOException("GMS2GameGUID has invalid length");
                writer.Write(GMS2AllowStatistics);
                writer.Write(GMS2GameGUID);
            }
        }

        public void Unserialize(UndertaleReader reader)
        {
            DisableDebugger = reader.ReadByte() != 0 ? true : false;
            BytecodeVersion = reader.ReadByte();
            Unknown = reader.ReadUInt16();
            Filename = reader.ReadUndertaleString();
            Config = reader.ReadUndertaleString();
            LastObj = reader.ReadUInt32();
            LastTile = reader.ReadUInt32();
            GameID = reader.ReadUInt32();
            Unknown1 = reader.ReadUInt32();
            Unknown2 = reader.ReadUInt32();
            Unknown3 = reader.ReadUInt32();
            Unknown4 = reader.ReadUInt32();
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
            ActiveTargets1 = reader.ReadUInt32();
            ActiveTargets2 = reader.ReadUInt32();
            FunctionClassifications1 = reader.ReadUInt32();
            FunctionClassifications2 = reader.ReadUInt32();
            SteamAppID = reader.ReadInt32();
            DebuggerPort = reader.ReadUInt32();
            RoomOrder = reader.ReadUndertaleObject<UndertaleSimpleList<RoomOrderEntry>>();
            if (Major >= 2)
            {
                GMS2RandomUID = reader.ReadBytes(40);
                GMS2FPS = reader.ReadSingle();
                GMS2AllowStatistics = reader.ReadBoolean();
                GMS2GameGUID = reader.ReadBytes(16);
            }
            reader.undertaleData.UnsupportedBytecodeVersion = (BytecodeVersion != 15 && BytecodeVersion != 16);
        }

        public override string ToString()
        {
            return "General info for " + DisplayName + " (GMS version " + Major + "." + Minor + "." + Release + "." + Build + ")";
        }
    }

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
        public UndertaleSprite.TextureEntry BackImage { get; set; } = null; // Apparently these exist, but I can't find any examples of it
        public UndertaleSprite.TextureEntry FrontImage { get; set; } = null;
        public UndertaleSprite.TextureEntry LoadImage { get; set; } = null;
        public uint LoadAlpha { get; set; } = 255;
        public UndertaleSimpleList<Constant> Constants { get; private set; } = new UndertaleSimpleList<Constant>();

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
        }

        public void Unserialize(UndertaleReader reader)
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
    }

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
