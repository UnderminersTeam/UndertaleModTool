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
        return $"{Name?.Content} ({GetType().Name})";
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);

        Name = null;
    }
}