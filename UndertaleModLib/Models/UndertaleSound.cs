using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModLib.Models
{
    public class UndertaleSound : UndertaleNamedResource, INotifyPropertyChanged
    {
        [Flags]
        public enum AudioEntryFlags : uint
        {
            IsEmbedded = 0x1,
            IsCompressed = 0x2,
            Regular = 0x64,
        }

        private UndertaleString _Name;
        private AudioEntryFlags _Flags;
        private UndertaleString _Type;
        private UndertaleString _File;
        private uint _Unknown;
        private float _Volume;
        private float _Pitch;
        private uint _GroupID;
        private UndertaleResourceById<UndertaleEmbeddedAudio> _AudioID { get; } = new UndertaleResourceById<UndertaleEmbeddedAudio>("AUDO");

        public UndertaleString Name { get => _Name; set { _Name = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Name")); } }
        public AudioEntryFlags Flags { get => _Flags; set { _Flags = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Flags")); } }
        public UndertaleString Type { get => _Type; set { _Type = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Type")); } }
        public UndertaleString File { get => _File; set { _File = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("File")); } }
        public uint Unknown { get => _Unknown; set { _Unknown = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Unknown")); } }
        public float Volume { get => _Volume; set { _Volume = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Volume")); } }
        public float Pitch { get => _Pitch; set { _Pitch = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Pitch")); } }
        public uint GroupID { get => _GroupID; set { _GroupID = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("GroupID")); } } // ID of audio group in AGRP, seems to be always 0 and AGRP is totally empty
        public UndertaleEmbeddedAudio AudioID { get => _AudioID.Resource; set { _AudioID.Resource = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("AudioID")); } }

        public event PropertyChangedEventHandler PropertyChanged;

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
            writer.Write(_AudioID.Serialize(writer));
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
            _AudioID.Unserialize(reader, reader.ReadInt32());
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
