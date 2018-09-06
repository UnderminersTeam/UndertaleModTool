using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModLib.Models
{
    public class UndertaleSound : UndertaleObject
    {
        [Flags]
        public enum AudioEntryFlags : uint
        {
            IsEmbedded = 0x1,
            IsCompressed = 0x2,
            Regular = 0x64,
        }

        public UndertaleString Name { get; set; }
        public AudioEntryFlags Flags { get; set; }
        public UndertaleString Type { get; set; }
        public UndertaleString File { get; set; }
        public uint Unknown { get; set; }
        public float Volume { get; set; }
        public float Pitch { get; set; }
        public uint GroupID { get; set; } // ID of audio group in AGRP, seems to be always 0 and AGRP is totally empty
        public UndertaleResourceById<UndertaleEmbeddedAudio> AudioID { get; } = new UndertaleResourceById<UndertaleEmbeddedAudio>("AUDO");

        public void Serialize(UndertaleWriter writer)
        {
            writer.WriteUndertaleString(Name);
            writer.Write((uint)Flags);
            writer.WriteUndertaleString(Type);
            writer.WriteUndertaleString(File);
            writer.Write(Unknown);
            writer.Write(Volume);
            writer.Write(Pitch);
            writer.Write(GroupID);
            writer.Write(AudioID.Serialize(writer));
        }

        public void Unserialize(UndertaleReader reader)
        {
            Name = reader.ReadUndertaleString();
            Flags = (AudioEntryFlags)reader.ReadUInt32();
            Type = reader.ReadUndertaleString();
            File = reader.ReadUndertaleString();
            Unknown = reader.ReadUInt32();
            Volume = reader.ReadSingle();
            Pitch = reader.ReadSingle();
            GroupID = reader.ReadUInt32();
            AudioID.Unserialize(reader, reader.ReadInt32());
        }

        public override string ToString()
        {
            return Name.Content + " (" + GetType().Name + ")";
        }
    }

    // Unused, but was documented for some reason
    public class UndertaleAudioGroup : UndertaleObject
    {
        public UndertaleString Name { get; set; }

        public void Serialize(UndertaleWriter writer)
        {
            writer.WriteUndertaleString(Name);
        }

        public void Unserialize(UndertaleReader reader)
        {
            Name = reader.ReadUndertaleString();
        }

        public override string ToString()
        {
            return Name.Content + " (" + GetType().Name + ")";
        }
    }
}
