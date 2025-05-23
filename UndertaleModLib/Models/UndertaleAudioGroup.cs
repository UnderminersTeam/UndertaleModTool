using System;

namespace UndertaleModLib.Models;

/// <summary>
/// Audio group entry in a data file.
/// </summary>
/// <remarks>Audio Groups allow you to manage a set sound entries easier.
/// You can use these for memory management, volume control and more. <br/>
/// Audio Groups are only available to use in the regular audio system</remarks>
/// <seealso cref="UndertaleSound.AudioEntryFlags.Regular"/>
/// <seealso cref="UndertaleSound"/>
[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertaleAudioGroup : UndertaleNamedResource, IStaticChildObjectsSize, IDisposable
{
    /// <inheritdoc cref="IStaticChildObjectsSize.ChildObjectsSize" />
    public static readonly uint ChildObjectsSize = 4;

    /// <summary>
    /// The name of the audio group.
    /// </summary>
    /// <remarks>This is how the audio group is referenced from code.</remarks>
    public UndertaleString Name { get; set; }

    /// <summary>
    /// Relative path (from the main data file) to the audio group file, in GameMaker 2024.14 and above.
    /// </summary>
    /// <remarks>
    /// Prior to 2024.14, audio groups were all numerically assigned filenames and all in the root directory.
    /// </remarks>
    public UndertaleString Path { get; set; } = null;

    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        writer.WriteUndertaleString(Name);
        if (writer.undertaleData.IsVersionAtLeast(2024, 14))
        {
            writer.WriteUndertaleString(Path);
        }
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        Name = reader.ReadUndertaleString();
        if (reader.undertaleData.IsVersionAtLeast(2024, 14))
        {
            Path = reader.ReadUndertaleString();
        }
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

        Name = null;
        Path = null;
    }
}