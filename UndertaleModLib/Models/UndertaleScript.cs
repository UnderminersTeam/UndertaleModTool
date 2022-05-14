﻿using System;
using System.ComponentModel;

namespace UndertaleModLib.Models;

/// <summary>
/// A script entry in a data file.
/// </summary>
public class UndertaleScript : UndertaleNamedResource, INotifyPropertyChanged, IDisposable
{
    /// <summary>
    /// The name of the script entry.
    /// </summary>
    public UndertaleString Name { get; set; }
    private UndertaleResourceById<UndertaleCode, UndertaleChunkCODE> _code = new();

    /// <summary>
    /// The <see cref="UndertaleCode"/> object which contains the code.
    /// </summary>
    public UndertaleCode Code { get => _code.Resource; set { _code.Resource = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Code))); } }

    /// <summary>
    /// Whether or not this script is a constructor.
    /// </summary>
    public bool IsConstructor { get; set; }

    /// <inheritdoc />
    public event PropertyChangedEventHandler PropertyChanged;

    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        writer.WriteUndertaleString(Name);
        if (IsConstructor)
            writer.Write((uint)_code.SerializeById(writer) | 2147483648u);
        else
            writer.WriteUndertaleObject(_code);
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        Name = reader.ReadUndertaleString();
        int id = reader.ReadInt32();
        if (id < -1)
        {
            IsConstructor = true;
            id = (int)((uint)id & 2147483647u);
        }
        _code.UnserializeById(reader, id);
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

        _code.Dispose();
    }
}