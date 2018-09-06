using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModLib.Models
{
    public class UndertaleGeneralInfo : UndertaleObject
    {
        [Flags]
        public enum InfoFlags : uint
        {
            Fullscreen = 0x0001,
            SyncVertex1 = 0x0002,
            SyncVertex2 = 0x0004,
            Interpolate = 0x0008,
            unknown = 0x0010,
            ShowCursor = 0x0020,
            Sizeable = 0x0040,
            ScreenKey = 0x0080,
            SyncVertex3 = 0x0100,
            StudioVersionB1 = 0x0200,
            StudioVersionB2 = 0x0400,
            StudioVersionB3 = 0x0800,
            StudioVersionMask = 0x0E00, // studioVersion = (infoFlags & InfoFlags.StudioVersionMask) >> 9
            SteamEnabled = 0x1000,
            LocalDataEnabled = 0x2000,
            BorderlessWindow = 0x4000,
        }

        public byte Debug { get; set; }
        public byte BytecodeVersion { get; set; }
        public ushort Unknown { get; set; }
        public UndertaleString Filename { get; set; }
        public UndertaleString Config { get; set; }
        public uint LastObj { get; set; }
        public uint LastTile { get; set; }
        public uint GameID { get; set; }
        public uint Unknown1 { get; set; }
        public uint Unknown2 { get; set; }
        public uint Unknown3 { get; set; }
        public uint Unknown4 { get; set; }
        public UndertaleString Name { get; set; }
        public uint Major { get; set; }
        public uint Minor { get; set; }
        public uint Release { get; set; }
        public uint Build { get; set; }
        public uint DefaultWindowWidth { get; set; }
        public uint DefaultWindowHeight { get; set; }
        public InfoFlags Info { get; set; }
        public byte[] LicenseMD5 { get; set; } = new byte[16];
        public uint LicenseCRC32 { get; set; }
        public uint Timestamp { get; set; }
        public uint ActiveTargets { get; set; }
        public UndertaleString DisplayName { get; set; }
        public uint Unknown5 { get; set; }
        public uint Unknown6 { get; set; }
        public uint Unknown7 { get; set; }
        public uint Unknown8 { get; set; }
        public uint Unknown9 { get; set; }
        public uint Unknown10 { get; set; }
        public uint UnknownPaddingNumbersCount { get; set; }

        public void Serialize(UndertaleWriter writer)
        {
            writer.Write(Debug);
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
            writer.Write(ActiveTargets);
            writer.WriteUndertaleString(DisplayName);
            writer.Write(Unknown5);
            writer.Write(Unknown6);
            writer.Write(Unknown7);
            writer.Write(Unknown8);
            writer.Write(Unknown9);
            writer.Write(Unknown10);
            writer.Write(UnknownPaddingNumbersCount);
            for (uint i = 0; i < UnknownPaddingNumbersCount; i++)
                writer.Write(i);
        }

        public void Unserialize(UndertaleReader reader)
        {
            Debug = reader.ReadByte();
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
            Timestamp = reader.ReadUInt32();
            ActiveTargets = reader.ReadUInt32();
            DisplayName = reader.ReadUndertaleString();
            Unknown5 = reader.ReadUInt32();
            Unknown6 = reader.ReadUInt32();
            Unknown7 = reader.ReadUInt32();
            Unknown8 = reader.ReadUInt32();
            Unknown9 = reader.ReadUInt32();
            Unknown10 = reader.ReadUInt32();
            UnknownPaddingNumbersCount = reader.ReadUInt32();
            for (uint i = 0; i < UnknownPaddingNumbersCount; i++)
                if (reader.ReadUInt32() != i)
                    throw new IOException("GEN8 padding error");
        }

        public override string ToString()
        {
            return DisplayName + " (build " + Build + ")";
        }
    }

    public class UndertaleOptions : UndertaleObject
    {
        public uint Unknown1 { get; set; }
        public uint Unknown2 { get; set; }
        public UndertaleGeneralInfo.InfoFlags Info { get; set; }
        public uint Unknown3 { get; set; }
        public uint Unknown4 { get; set; }
        public uint Unknown5 { get; set; }
        public uint Unknown6 { get; set; }
        public uint Unknown7 { get; set; }
        public uint Unknown8 { get; set; }
        public uint Unknown9 { get; set; }
        public uint Unknown10 { get; set; }
        public uint Unknown11 { get; set; }
        public uint Unknown12 { get; set; }
        public uint Unknown13 { get; set; }
        public uint Unknown14 { get; set; }
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
            writer.Write((uint)Info);
            writer.Write(Unknown3);
            writer.Write(Unknown4);
            writer.Write(Unknown5);
            writer.Write(Unknown6);
            writer.Write(Unknown7);
            writer.Write(Unknown8);
            writer.Write(Unknown9);
            writer.Write(Unknown10);
            writer.Write(Unknown11);
            writer.Write(Unknown12);
            writer.Write(Unknown13);
            writer.Write(Unknown14);
            writer.WriteUndertaleObject(Constants);
        }

        public void Unserialize(UndertaleReader reader)
        {
            Unknown1 = reader.ReadUInt32();
            Unknown2 = reader.ReadUInt32();
            Info = (UndertaleGeneralInfo.InfoFlags)reader.ReadUInt32();
            Unknown3 = reader.ReadUInt32();
            Unknown4 = reader.ReadUInt32();
            Unknown5 = reader.ReadUInt32();
            Unknown6 = reader.ReadUInt32();
            Unknown7 = reader.ReadUInt32();
            Unknown8 = reader.ReadUInt32();
            Unknown9 = reader.ReadUInt32();
            Unknown10 = reader.ReadUInt32();
            Unknown11 = reader.ReadUInt32();
            Unknown12 = reader.ReadUInt32();
            Unknown13 = reader.ReadUInt32();
            Unknown14 = reader.ReadUInt32();
            Constants = reader.ReadUndertaleObject<UndertaleSimpleList<Constant>>();
        }
    }
    
    public class UndertaleLanguage : UndertaleObject
    {
        public uint Unknown1 { get; set; }
        public uint Unknown2 { get; set; }
        public uint Unknown3 { get; set; }

        public void Serialize(UndertaleWriter writer)
        {
            writer.Write(Unknown1);
            writer.Write(Unknown2);
            writer.Write(Unknown3);
        }

        public void Unserialize(UndertaleReader reader)
        {
            Unknown1 = reader.ReadUInt32();
            Unknown2 = reader.ReadUInt32();
            Unknown3 = reader.ReadUInt32();
        }
    }

    public class UndertaleExtension : UndertaleObject
    {
        void UndertaleGlobal()
        {
            throw new NotImplementedException();
        }

        public void Serialize(UndertaleWriter writer)
        {
            throw new NotImplementedException();
        }

        public void Unserialize(UndertaleReader reader)
        {
            throw new NotImplementedException();
        }
    }
}
