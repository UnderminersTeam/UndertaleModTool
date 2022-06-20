using System;
using System.ComponentModel;

namespace UndertaleModLib.Models;

/// <summary>
/// A global initialization entry in a data file.
/// </summary>
/// <remarks>Never seen in GMS1.4 so uncertain if the structure was the same.</remarks>
public class UndertaleGlobalInit : UndertaleObject, INotifyPropertyChanged, IDisposable
{
    private UndertaleResourceById<UndertaleCode, UndertaleChunkCODE> _code = new();

    /// <summary>
    /// The <see cref="UndertaleCode"/> object which contains the code.
    /// </summary>
    public UndertaleCode Code { get => _code.Resource; set { _code.Resource = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Code))); } }

    /// <inheritdoc />
    public event PropertyChangedEventHandler PropertyChanged;

    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        _code.Serialize(writer);
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        _code = new UndertaleResourceById<UndertaleCode, UndertaleChunkCODE>();
        _code.Unserialize(reader); // TODO: reader.ReadUndertaleObject if one object starts with another one
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);

        _code.Dispose();
    }
}