﻿namespace UndertaleModLib.Models;

/// <summary>
/// A filter effect as it's used in a GameMaker data file. These are GameMaker: Studio 2.3.6+ only.
/// </summary>
[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertaleFilterEffect : UndertaleNamedResource
{
    /// <summary>
    /// The name of the <see cref="UndertaleFilterEffect"/>.
    /// </summary>
    public UndertaleString Name { get; set; }

    /// <summary>
    /// TODO: fill this out please
    /// </summary>
    public UndertaleString Value { get; set; }

    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        writer.WriteUndertaleString(Name);
        writer.WriteUndertaleString(Value);
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        Name = reader.ReadUndertaleString();
        Value = reader.ReadUndertaleString();
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Name.Content + " (" + GetType().Name + ")";
    }
}