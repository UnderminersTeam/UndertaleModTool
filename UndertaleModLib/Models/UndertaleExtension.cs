using System;

namespace UndertaleModLib.Models;

/// <summary>
/// Details the possible extension kinds for a GameMaker data file.
/// </summary>
public enum UndertaleExtensionKind : uint
{
    /// <summary>
    /// TODO: unknown, needs more research.
    /// </summary>
    [Obsolete("Likely unused")]
    Unknown0 = 0,
    /// <summary>
    /// A DLL extension.
    /// </summary>
    Dll = 1,
    /// <summary>
    /// A GML extension.
    /// </summary>
    GML = 2,
    /// <summary>
    /// TODO: unknown
    /// </summary>
    ActionLib = 3,
    /// <summary>
    /// TODO: unknown
    /// </summary>
    Generic = 4,
    /// <summary>
    /// A JavaScript extension.
    /// </summary>
    Js = 5
}

/// <summary>
/// Details the possible variable types for GameMaker extensions.
/// </summary>
public enum UndertaleExtensionVarType : uint
{
    /// <summary>
    /// A string variable.
    /// </summary>
    String = 1,
    /// <summary>
    /// A double variable.
    /// </summary>
    Double = 2
}

/// <summary>
/// A class representing an argument for <see cref="UndertaleExtensionFunction"/>s.
/// </summary>
[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertaleExtensionFunctionArg : UndertaleObject
{
    /// <summary>
    /// The variable type of this argument.
    /// </summary>
    public UndertaleExtensionVarType Type { get; set; }

    /// <summary>
    /// Initializes a new instance of <see cref="UndertaleExtensionFunctionArg"/>.
    /// </summary>
    public UndertaleExtensionFunctionArg()
    {
        Type = UndertaleExtensionVarType.Double;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="UndertaleExtensionFunctionArg"/> with a specified <see cref="UndertaleExtensionVarType"/>.
    /// </summary>
    /// <param name="type"></param>
    public UndertaleExtensionFunctionArg(UndertaleExtensionVarType type)
    {
        Type = type;
    }

    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        writer.Write((uint)Type);
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        Type = (UndertaleExtensionVarType)reader.ReadUInt32();
    }
}

/// <summary>
/// A function in a <see cref="UndertaleExtension"/>.
/// </summary>
[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertaleExtensionFunction : UndertaleObject, IDisposable
{
    /// <summary>
    /// The name of the function.
    /// </summary>
    public UndertaleString Name { get; set; }

    /// <summary>
    /// An identification number of the function.
    /// </summary>
    public uint ID { get; set; }
    public uint Kind { get; set; }

    /// <summary>
    /// The return type of the function.
    /// </summary>
    public UndertaleExtensionVarType RetType { get; set; }
    public UndertaleString ExtName { get; set; }

    /// <summary>
    /// A list of arguments this function takes.
    /// </summary>
    public UndertaleSimpleList<UndertaleExtensionFunctionArg> Arguments { get; set; } = new UndertaleSimpleList<UndertaleExtensionFunctionArg>();

    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        writer.WriteUndertaleString(Name);
        writer.Write(ID);
        writer.Write(Kind);
        writer.Write((uint)RetType);
        writer.WriteUndertaleString(ExtName);
        writer.WriteUndertaleObject(Arguments);
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        Name = reader.ReadUndertaleString();
        ID = reader.ReadUInt32();
        Kind = reader.ReadUInt32();
        RetType = (UndertaleExtensionVarType)reader.ReadUInt32();
        ExtName = reader.ReadUndertaleString();
        Arguments = reader.ReadUndertaleObject<UndertaleSimpleList<UndertaleExtensionFunctionArg>>();
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Name?.Content + " (" + ExtName?.Content + ")";
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);

        Arguments = new();
        Name = null;
        ExtName = null;
    }
}

[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertaleExtensionFile : UndertaleObject, IDisposable
{
    public UndertaleString Filename { get; set; }
    public UndertaleString CleanupScript { get; set; }
    public UndertaleString InitScript { get; set; }
    public UndertaleExtensionKind Kind { get; set; }
    public UndertalePointerList<UndertaleExtensionFunction> Functions { get; set; } = new UndertalePointerList<UndertaleExtensionFunction>();

    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        writer.WriteUndertaleString(Filename);
        writer.WriteUndertaleString(CleanupScript);
        writer.WriteUndertaleString(InitScript);
        writer.Write((uint)Kind);
        writer.WriteUndertaleObject(Functions);
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        Filename = reader.ReadUndertaleString();
        CleanupScript = reader.ReadUndertaleString();
        InitScript = reader.ReadUndertaleString();
        Kind = (UndertaleExtensionKind)reader.ReadUInt32();
        Functions = reader.ReadUndertaleObject<UndertalePointerList<UndertaleExtensionFunction>>();
    }

    /// <inheritdoc />
    public override string ToString()
    {
        try
        {
            return Filename.Content + " (" + GetType().Name + ")";
        }
        catch
        {
            return "(Unknown extension file)";
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);

        if (Functions is not null)
        {
            foreach (UndertaleExtensionFunction func in Functions)
                func?.Dispose();
         }
        Filename = null;
        CleanupScript = null;
        InitScript = null;
        Functions = new();
    }
}


[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertaleExtensionOption : UndertaleObject, IDisposable
{
    public enum OptionKind : uint
    {
        Boolean = 0,
        Number = 1,
        String = 2
    }

    public UndertaleString Name { get; set; }
    public UndertaleString Value { get; set; }
    public OptionKind Kind { get; set; }

    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        writer.WriteUndertaleString(Name);
        writer.WriteUndertaleString(Value);
        writer.Write((uint)Kind);
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        Name = reader.ReadUndertaleString();
        Value = reader.ReadUndertaleString();
        Kind = (OptionKind)reader.ReadUInt32();
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Name.Content;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);

        Name = null;
        Value = null;
    }
}

/// <summary>
/// An extension entry in a GameMaker data file.
/// </summary>
[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertaleExtension : UndertaleNamedResource, IDisposable
{
    /// <summary>
    /// In which folder the extension is located. TODO: needs more GM8 research.
    /// </summary>
    /// <remarks>This is a remnant from the legacy GameMaker7-8.1 extension editor (aka ExtMaker). <br/>
    /// The runner reads this name, but ignores it. This probably shouldn't be changed anyway.</remarks>
    public UndertaleString FolderName { get; set; }

    /// <summary>
    /// The name of the extension.
    /// </summary>
    public UndertaleString Name { get; set; }
    public UndertaleString ClassName { get; set; }

    public UndertalePointerList<UndertaleExtensionFile> Files { get; set; } = new UndertalePointerList<UndertaleExtensionFile>();
    public UndertalePointerList<UndertaleExtensionOption> Options { get; set; } = new UndertalePointerList<UndertaleExtensionOption>();

    /// <inheritdoc />
    public override string ToString()
    {
        return Name?.Content + " (" + GetType().Name + ")";
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);

        foreach (UndertaleExtensionFile file in Files)
            file?.Dispose();
        Files = new();
        foreach (UndertaleExtensionOption opt in Options)
            opt?.Dispose();
        Options = new();
        FolderName = null;
        Name = null;
        ClassName = null;
    }

    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        writer.WriteUndertaleString(FolderName);
        writer.WriteUndertaleString(Name);
        writer.WriteUndertaleString(ClassName);
        if (writer.undertaleData.IsVersionAtLeast(2022, 6))
        {
            writer.WriteUndertaleObjectPointer(Files);
            writer.WriteUndertaleObjectPointer(Options);
            writer.WriteUndertaleObject(Files);
            writer.WriteUndertaleObject(Options);
        }
        else
        {
            writer.WriteUndertaleObject(Files);
        }
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        FolderName = reader.ReadUndertaleString();
        Name = reader.ReadUndertaleString();
        ClassName = reader.ReadUndertaleString();
        if (reader.undertaleData.IsVersionAtLeast(2022, 6))
        {
            Files = reader.ReadUndertaleObjectPointer<UndertalePointerList<UndertaleExtensionFile>>();
            Options = reader.ReadUndertaleObjectPointer<UndertalePointerList<UndertaleExtensionOption>>();
            reader.ReadUndertaleObject(Files);
            reader.ReadUndertaleObject(Options);
        }
        else
        {
            Files = reader.ReadUndertaleObject<UndertalePointerList<UndertaleExtensionFile>>();
        }
    }
}