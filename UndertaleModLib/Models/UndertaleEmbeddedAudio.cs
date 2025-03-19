using System;
using System.IO;

namespace UndertaleModLib.Models;

/// <summary>
/// An embedded audio entry in a data file.
/// </summary>
[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertaleEmbeddedAudio : UndertaleNamedResource, PaddedObject, IDisposable
{
    /// <summary>
    /// The name of the embedded audio entry.
    /// </summary>
    /// <remarks>This is a UTMT only attribute. GameMaker does not store names for them.</remarks>
    public UndertaleString Name { get; set; }

    /// <summary>
    /// The audio data of the embedded audio entry.
    /// </summary>
    public byte[] Data { get; set; } = Array.Empty<byte>();

    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        writer.Write((uint)Data.Length);
        writer.Write(Data);
    }

    /// <inheritdoc />
    public void SerializePadding(UndertaleWriter writer)
    {
        while (writer.Position % 4 != 0)
            writer.Write((byte)0);
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        uint len = reader.ReadUInt32();
        Data = reader.ReadBytes((int)len);
        Util.DebugUtil.Assert(Data.Length == len);
    }

    /// <inheritdoc />
    public void UnserializePadding(UndertaleReader reader)
    {
        while (reader.AbsPosition % 4 != 0)
            if (reader.ReadByte() != 0)
                throw new IOException("Padding error!");
    }

    /// <inheritdoc />
    public override string ToString()
    {
        try
        {
            // TODO: Does only the GUI set this?
            return $"{Name.Content} ({GetType().Name})";
        }
        catch
        {
            Name = new UndertaleString("EmbeddedSound Unknown Index");
        }
        return $"{Name.Content} ({GetType().Name})";
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);

        Name = null;
        Data = Array.Empty<byte>();
    }
}