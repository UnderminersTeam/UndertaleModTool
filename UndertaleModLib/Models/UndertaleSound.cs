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
            IsDecompressedOnLoad = 0x3,
            Regular = 0x64, // also means "Use New Audio System?" Set by default on GMS 2.
        }

        public UndertaleString Name { get; set; }
        public AudioEntryFlags Flags { get; set; } = AudioEntryFlags.IsEmbedded;
        public UndertaleString Type { get; set; }
        public UndertaleString File { get; set; }
        public uint Effects { get; set; } = 0;
        public float Volume { get; set; } = 1;
        public bool Preload { get; set; } = true;
        public float Pitch { get; set; } = 0;

        private UndertaleResourceById<UndertaleAudioGroup, UndertaleChunkAGRP> _AudioGroup = new UndertaleResourceById<UndertaleAudioGroup, UndertaleChunkAGRP>();
        private UndertaleResourceById<UndertaleEmbeddedAudio, UndertaleChunkAUDO> _AudioFile = new UndertaleResourceById<UndertaleEmbeddedAudio, UndertaleChunkAUDO>();
        public UndertaleAudioGroup AudioGroup { get => _AudioGroup.Resource; set { _AudioGroup.Resource = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AudioGroup))); } }
        public UndertaleEmbeddedAudio AudioFile { get => _AudioFile.Resource; set { _AudioFile.Resource = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AudioFile))); } }
        public int AudioID { get => _AudioFile.CachedId; set { _AudioFile.CachedId = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AudioID))); } }
        public int GroupID { get => _AudioGroup.CachedId; set { _AudioGroup.CachedId = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GroupID))); } }

        public event PropertyChangedEventHandler PropertyChanged;

        public void Serialize(UndertaleWriter writer)
        {
            writer.WriteUndertaleString(Name);
            writer.Write((uint)Flags);
            writer.WriteUndertaleString(Type);
            writer.WriteUndertaleString(File);
            writer.Write(Effects);
            writer.Write(Volume);
            writer.Write(Pitch);
            if (Flags.HasFlag(AudioEntryFlags.Regular) && writer.undertaleData.GeneralInfo.BytecodeVersion >= 14)
                writer.WriteUndertaleObject(_AudioGroup);
            else
                writer.Write(Preload);

            if (GroupID == 0)
                writer.WriteUndertaleObject(_AudioFile);
            else
                writer.Write(_AudioFile.CachedId);
        }

        public void Unserialize(UndertaleReader reader)
        {
            Name = reader.ReadUndertaleString();
            Flags = (AudioEntryFlags)reader.ReadUInt32();
            Type = reader.ReadUndertaleString();
            File = reader.ReadUndertaleString();
            Effects = reader.ReadUInt32();
            Volume = reader.ReadSingle();
            Pitch = reader.ReadSingle();

            if (Flags.HasFlag(AudioEntryFlags.Regular) && reader.undertaleData.GeneralInfo.BytecodeVersion >= 14)
            {
                _AudioGroup = reader.ReadUndertaleObject<UndertaleResourceById<UndertaleAudioGroup, UndertaleChunkAGRP>>();
                Preload = true;
            }
            else
            {
                GroupID = reader.undertaleData.GetBuiltinSoundGroupID(); // legacy audio system doesn't have groups
                // instead, the preload flag is stored (always true? remnant from GM8?)
                Preload = reader.ReadBoolean();
            }

            if (GroupID == reader.undertaleData.GetBuiltinSoundGroupID()) 
            {
                _AudioFile = reader.ReadUndertaleObject<UndertaleResourceById<UndertaleEmbeddedAudio, UndertaleChunkAUDO>>();
            }
            else
            {
                _AudioFile.CachedId = reader.ReadInt32();
            }
        }

        public override string ToString()
        {
            return Name.Content + " (" + GetType().Name + ")";
        }
    }

    [PropertyChanged.AddINotifyPropertyChangedInterface]
    public class UndertaleAudioGroup : UndertaleNamedResource
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
