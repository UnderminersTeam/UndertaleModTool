﻿using System.ComponentModel;

namespace UndertaleModLib.Models;

/// <summary>
/// A global initialization entry in a data file.
/// </summary>
/// <remarks>Never seen in GMS1.4 so uncertain if the structure was the same.</remarks>
public class UndertaleGlobalInit : UndertaleObject, INotifyPropertyChanged
{
    private UndertaleResourceById<UndertaleCode, UndertaleChunkCODE> _Code = new UndertaleResourceById<UndertaleCode, UndertaleChunkCODE>();

    /// <summary>
    /// The <see cref="UndertaleCode"/> object which contains the code.
    /// </summary>
    public UndertaleCode Code { get => _Code.Resource; set { _Code.Resource = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Code))); } }

    /// <inheritdoc />
    public event PropertyChangedEventHandler PropertyChanged;

    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        _Code.Serialize(writer);
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        _Code = new UndertaleResourceById<UndertaleCode, UndertaleChunkCODE>();
        _Code.Unserialize(reader); // TODO: reader.ReadUndertaleObject if one object starts with another one
    }
}