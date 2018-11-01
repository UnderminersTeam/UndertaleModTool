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
        private AudioEntryFlags _Flags = AudioEntryFlags.IsEmbedded;
        private UndertaleString _Type;
        private UndertaleString _File;
        private uint _Unknown = 0;
        private float _Volume = 1;
        private float _Pitch = 0;
        private UndertaleResourceById<UndertaleAudioGroup> _AudioGroup { get; } = new UndertaleResourceById<UndertaleAudioGroup>("AGRP");
        private UndertaleResourceById<UndertaleEmbeddedAudio> _AudioFile { get; } = new UndertaleResourceById<UndertaleEmbeddedAudio>("AUDO");

        public UndertaleString Name { get => _Name; set { _Name = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Name")); } }
        public AudioEntryFlags Flags { get => _Flags; set { _Flags = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Flags")); } }
        public UndertaleString Type { get => _Type; set { _Type = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Type")); } }
        public UndertaleString File { get => _File; set { _File = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("File")); } }
        public uint Unknown { get => _Unknown; set { _Unknown = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Unknown")); } }
        public float Volume { get => _Volume; set { _Volume = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Volume")); } }
        public float Pitch { get => _Pitch; set { _Pitch = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Pitch")); } }
        public UndertaleAudioGroup AudioGroup { get => _AudioGroup.Resource; set { _AudioGroup.Resource = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("AudioGroup")); } }
        public UndertaleEmbeddedAudio AudioFile { get => _AudioFile.Resource; set { _AudioFile.Resource = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("AudioFile")); } }
        public int AudioID { get => _AudioFile.CachedId; set { _AudioFile.CachedId = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("AudioID")); } }
        public int GroupID { get => _AudioGroup.CachedId; set { _AudioGroup.CachedId = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("GroupID")); } }

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
            writer.Write(_AudioGroup.Serialize(writer));
            if (GroupID == 0)
                writer.Write(_AudioFile.Serialize(writer));
            else
                writer.Write(_AudioFile.CachedId);
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
            _AudioGroup.Unserialize(reader, reader.ReadInt32());
            if (GroupID == 0)
                _AudioFile.Unserialize(reader, reader.ReadInt32());
            else
                _AudioFile.CachedId = reader.ReadInt32();
        }

        public override string ToString()
        {
            return Name.Content + " (" + GetType().Name + ")";
        }
    }
    
    public class UndertaleAudioGroup : INotifyPropertyChanged, UndertaleNamedResource
    {
        private UndertaleString _Name;
        public UndertaleString Name { get => _Name; set { _Name = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Name")); } }

        public event PropertyChangedEventHandler PropertyChanged;

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
