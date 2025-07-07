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
        /// <remarks>This should ideally be used for sound effects, but not for music.<br/>
        /// The GameMaker documentation also calls this "not streamed" (or "from memory") for when the flag is present,
        /// or "streamed" when it isn't.</remarks>
        IsEmbedded = 0x1,
        /// <summary>
        /// Whether the sound is compressed.
        /// </summary>
        /// <remarks>When a sound is compressed it will take smaller memory/disk space.
        /// However, this is at the cost of needing to decompress it when it needs to be played,
        /// which means slightly higher CPU usage.</remarks>
        // TODO: where exactly is this used? for non-embedded compressed files, this flag doesnt seem to be set.
        IsCompressed = 0x2,
        /// <summary>
        /// Whether the sound is decompressed on game load.
        /// </summary>
        /// <remarks>When a sound is played, it must be loaded into memory first, which would usually be done when the sound is first used.
        /// If you preload it, the sound will be loaded into memory at the start of the game.</remarks>
        // TODO: some predecessor/continuation of Preload? Also why is this flag the combination of both compressed and embedded?
        IsDecompressedOnLoad = 0x3,
        /// <summary>
        /// Whether this sound uses the "new audio system".
        /// </summary>
        /// <remarks>This is default for everything post GameMaker Studio.
        /// The legacy sound system was used in pre Game Maker 8.</remarks>
        Regular = 0x64,
    }

    /// <summary>
    /// The name of the sound entry.
    /// </summary>
    /// <remarks>This name is used when referencing this entry from code.</remarks>
    public UndertaleString Name { get; set; }

    /// <summary>
    /// The flags the sound entry uses.
    /// </summary>
    /// <remarks>These effectively control different options of this sound.</remarks>
    /// <seealso cref="AudioEntryFlags"/>
    public AudioEntryFlags Flags { get; set; } = AudioEntryFlags.IsEmbedded & AudioEntryFlags.Regular;

    /// <summary>
    /// The file format of the audio entry.
    /// </summary>
    /// <remarks>This includes the <c>.</c> from the file extension. Possible values are:
    /// <list type="bullet">
    /// <item><c>.wav</c>
    /// </item>
    /// <item><c>.mp3</c>
    /// </item>
    /// <item><c>.ogg</c>
    /// </item>
    /// </list>
    /// </remarks>
    // TODO: is .midi valid as well? it was used in legacy GM. Also I assume that this is the extension to
    // know what the runner must do, and not just the extension of the filename.
    public UndertaleString Type { get; set; }

    /// <summary>
    /// The original file name of the audio entry.
    /// </summary>
    /// <remarks>This is the full filename how it was loaded in the project.
    /// This will be used if the sound effect is streamed from disk to find the sound file.
    /// <seealso cref="AudioEntryFlags.IsEmbedded"/>
    /// </remarks>
    public UndertaleString File { get; set; }

    /// <summary>
    /// A pre-GameMaker Studio way of having certain effects on a sound effect.
    /// </summary>
    /// <remarks>The exact way this works is unknown. But following values are possible:
    /// <c>Chorus</c>, <c>Echo</c>, <c>Flanger</c>, <c>Reverb</c>, <c>Gargle</c>, all possible to be combined with one another.</remarks>
    // TODO: some research has been done, but not a lot. these don't *seem* to be bitflags, as both "nothing" and "flanger" have the same value of 1.
    // https://discord.com/channels/566861759210586112/568950566122946580/957318910066196500
    public uint Effects { get; set; }

    /// <summary>
    /// The volume the audio entry is played at.
    /// </summary>
    /// <remarks>The volume is a number between <c>0</c> and <c>1</c>,
    /// which mean "completely silent" and "full volume" respectively.</remarks>
    public float Volume { get; set; } = 1;

    /// <summary>
    /// Whether the sound is decompressed on game load.
    /// </summary>
    /// <inheritdoc cref="AudioEntryFlags.IsDecompressedOnLoad"/>
    public bool Preload { get; set; } = true;

    /// <summary>
    /// The pitch change of the audio entry.
    /// </summary>
    // TODO: is this really pitch? I can't see pitch being referenced anywhere in any manual. This feels like it's panning from legacy GMS.
    public float Pitch { get; set; }

    private UndertaleResourceById<UndertaleAudioGroup, UndertaleChunkAGRP> _audioGroup = new();
    private UndertaleResourceById<UndertaleEmbeddedAudio, UndertaleChunkAUDO> _audioFile = new();

    /// <summary>
    /// The audio group this audio entry belongs to.
    /// </summary>
    /// <remarks>These can only be used with the regular audio system.</remarks>
    /// <seealso cref="AudioEntryFlags.Regular"/>
    /// <seealso cref="AudioGroup"/>
    public UndertaleAudioGroup AudioGroup { get => _audioGroup.Resource; set { _audioGroup.Resource = value; OnPropertyChanged(); } }

    /// <summary>
    /// The reference to the <see cref="UndertaleEmbeddedAudio"/> audio file.
    /// </summary>
    /// <remarks>This is a UTMT-specific attribute. <br/>
    /// All sound entries always have to have a reference to an <see cref="UndertaleEmbeddedAudio"/> entry.
    /// Even if the sound entry is not embedded, it is still necessary. For that case, you can just link to any embedded sound.
    /// </remarks>
    /// <seealso cref="AudioEntryFlags.IsEmbedded"/>
    /// <seealso cref="UndertaleEmbeddedAudio"/>
    public UndertaleEmbeddedAudio AudioFile { get => _audioFile.Resource; set { _audioFile.Resource = value; OnPropertyChanged(); } }

    /// <summary>
    /// The id of <see cref="AudioFile"/>.
    /// </summary>
    public int AudioID { get => _audioFile.CachedId; set { _audioFile.CachedId = value; OnPropertyChanged(); } }

    /// <summary>
    /// The id of <see cref="AudioGroup"/>.
    /// </summary>
    public int GroupID { get => _audioGroup.CachedId; set { _audioGroup.CachedId = value; OnPropertyChanged(); } }

    /// <summary>
    /// The precomputed length of the sound's audio data.
    /// </summary>
    /// <remarks>Introduced in GameMaker 2024.6.</remarks>
    public float AudioLength { get; set; }

    /// <inheritdoc />
    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// Invoked whenever the effective value of any dependency property has been updated.
    /// </summary>
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

        if (writer.undertaleData.IsVersionAtLeast(2024, 6))
            writer.Write(AudioLength);
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

        if (reader.undertaleData.IsVersionAtLeast(2024, 6))
            AudioLength = reader.ReadSingle();
    }

    /// <inheritdoc cref="UndertaleObject.UnserializeChildObjectCount(UndertaleReader)"/>
    public static uint UnserializeChildObjectCount(UndertaleReader reader)
    {
        uint count = 0;

        reader.Position += 4;
        AudioEntryFlags flags = (AudioEntryFlags)reader.ReadUInt32();
        reader.Position += 20;

        int audioGroupID;

        if (flags.HasFlag(AudioEntryFlags.Regular) && reader.undertaleData.GeneralInfo?.BytecodeVersion >= 14)
        {
            audioGroupID = reader.ReadInt32();
            count++;
        }
        else
        {
            audioGroupID = reader.undertaleData.GetBuiltinSoundGroupID();
            reader.Position += 4; // "Preload"
        }

        if (audioGroupID == reader.undertaleData.GetBuiltinSoundGroupID())
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
        return $"{Name?.Content} ({GetType().Name})";
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