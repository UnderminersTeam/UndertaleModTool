using System;

namespace UndertaleModLib.Models;

/// <summary>
/// List of feature flag entries in a GameMaker data file, version 2022.8 and above.
/// </summary>
[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertaleFeatureFlags : UndertaleObject, IDisposable
{
    /// <summary>
    /// The list of feature flags.
    /// </summary>
    public UndertaleSimpleListString List { get; set; }


    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        List.Serialize(writer);
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        List = new UndertaleSimpleListString();
        List.Unserialize(reader);
    }

    /// <inheritdoc cref="UndertaleObject.UnserializeChildObjectCount(UndertaleReader)"/>
    public static uint UnserializeChildObjectCount(UndertaleReader reader)
    {
        return UndertaleSimpleListString.UnserializeChildObjectCount(reader);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);

        List = null;
    }
}
