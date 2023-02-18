using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace UndertaleModLib.Models;

/// <summary>
/// Sound entry in a data file.
/// </summary>
public class UndertaleSound : UndertaleNamedResource, INotifyPropertyChanged, IDisposable
{
    /// <summary>
    /// Audio entry flags a sound entry can use.
    /// </summary>
    [Flags]
    public enum AudioEntryFlags : uint
    {
        /// <summary>
        /// Whether the sound is embedded into the data file.
        /// </summary>
        IsEmbedded = 0x1,
        /// <summary>
        /// Whether the sound is compressed.
        /// </summary>
        IsCompressed = 0x2,
        /// <summary>
        /// Whether the sound is compressed on load.
        /// </summary>
        IsDecompressedOnLoad = 0x3,
        /// <summary>
        /// Whether this sound uses the "new audio system".
        /// </summary>
        /// <remarks>This is default for everything post Game Maker: Studio.
        /// The legacy sound system was used in pre Game Maker 8</remarks>
        Regular = 0x64, // also means "Use New Audio System?" Set by default on GMS 2.
    }

    /// <summary>
    /// The name of the sound entry.
    /// </summary>
    public UndertaleString Name { get; set; }

    /// <summary>
    /// The flags the sound entry uses.
    /// </summary>
    public AudioEntryFlags Flags { get; set; } = AudioEntryFlags.IsEmbedded;

    /// <summary>
    /// The file format of the audio entry.
    /// </summary>
    public UndertaleString Type { get; set; }

    /// <summary>
    /// The file name of the audio entry.
    /// </summary>
    public UndertaleString File { get; set; }

    /// <summary>
    /// A pre- Game Maker: Studio way of having certain effects on a sound effect.
    /// </summary>
    /// <remarks>The exact way this works is unknown. But following values are possible:
    /// <c>Chorus</c>, <c>Echo</c>, <c>Flanger</c>, <c>Reverb</c>, <c>Gargle</c>, all possible to be combined with one another.</remarks>
    public uint Effects { get; set; }

    /// <summary>
    /// The volume the audio entry is played at.
    /// </summary>
    public float Volume { get; set; } = 1;

    /// <summary>
    /// Whether the audio entry should be preloaded.
    /// </summary>
    public bool Preload { get; set; } = true;

    /// <summary>
    /// The pitch change of the audio entry.
    /// </summary>
    public float Pitch { get; set; } = 0;

    private UndertaleResourceById<UndertaleAudioGroup, UndertaleChunkAGRP> _audioGroup = new();
    private UndertaleResourceById<UndertaleEmbeddedAudio, UndertaleChunkAUDO> _audioFile = new();

    /// <summary>
    /// The audio group this audio entry belongs to.
    /// </summary>
    public UndertaleAudioGroup AudioGroup { get => _audioGroup.Resource; set { _audioGroup.Resource = value; OnPropertyChanged(); } }

    /// <summary>
    /// The reference to the <see cref="UndertaleEmbeddedAudio"/> audio file.
    /// </summary>
    public UndertaleEmbeddedAudio AudioFile { get => _audioFile.Resource; set { _audioFile.Resource = value; OnPropertyChanged(); } }

    /// <summary>
    /// The id of <see cref="AudioFile"></see>.
    /// </summary>
    public int AudioID { get => _audioFile.CachedId; set { _audioFile.CachedId = value; OnPropertyChanged(); } }

    /// <summary>
    /// The id of <see cref="AudioGroup"/>.
    /// </summary>
    public int GroupID { get => _audioGroup.CachedId; set { _audioGroup.CachedId = value; OnPropertyChanged(); } }

    /// <inheritdoc />
    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <inheritdoc />
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
            writer.WriteUndertaleObject(_audioGroup);
        else
            writer.Write(Preload);

        if (GroupID == 0)
            writer.WriteUndertaleObject(_audioFile);
        else
            writer.Write(_audioFile.CachedId);
    }

    /// <inheritdoc />
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
            _audioGroup = reader.ReadUndertaleObject<UndertaleResourceById<UndertaleAudioGroup, UndertaleChunkAGRP>>();
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
            _audioFile = reader.ReadUndertaleObject<UndertaleResourceById<UndertaleEmbeddedAudio, UndertaleChunkAUDO>>();
        }
        else
        {
            _audioFile.CachedId = reader.ReadInt32();
        }
    }

    /// <inheritdoc cref="UndertaleObject.UnserializeChildObjectCount(UndertaleReader)"/>
    public static uint UnserializeChildObjectCount(UndertaleReader reader)
    {
        uint count = 0;

        reader.Position += 4;
        AudioEntryFlags flags = (AudioEntryFlags)reader.ReadUInt32();
        reader.Position += 20;

        int audioGroupID;

        if (flags.HasFlag(AudioEntryFlags.Regular) && reader.BytecodeVersion >= 14)
        {
            audioGroupID = reader.ReadInt32();
            count++;
        }
        else
        {
            audioGroupID = reader.BuiltinSoundGroupID;
            reader.Position += 4; // "Preload"
        }

        if (audioGroupID == reader.BuiltinSoundGroupID)
        {
            reader.Position += 4; // "_audioFile"
            count++;
        }
        else
            reader.Position += 4; // "_audioFile.CachedId"

        return count;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Name?.Content + " (" + GetType().Name + ")";
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);

        _audioGroup.Dispose();
        _audioFile.Dispose();
        Name = null;
        Type = null;
        File = null;
    }
}

/// <summary>
/// Audio group entry in a data file.
/// </summary>
[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertaleAudioGroup : UndertaleNamedResource, IStaticChildObjectsSize, IDisposable
{
    /// <inheritdoc cref="IStaticChildObjectsSize.ChildObjectsSize" />
    public static readonly uint ChildObjectsSize = 4;

    /// <summary>
    /// The name of the audio group.
    /// </summary>
    public UndertaleString Name { get; set; }

    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        writer.WriteUndertaleString(Name);
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        Name = reader.ReadUndertaleString();
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Name?.Content + " (" + GetType().Name + ")";
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);

        Name = null;
    }
}