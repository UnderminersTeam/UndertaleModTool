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
    DLL = 1,
    /// <summary>
    /// A GML extension.
    /// </summary>
    GML = 2,
    ActionLib = 3,
    Generic = 4,
    /// <summary>
    /// A JavaScript extension.
    /// </summary>
    JS = 5
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
public class UndertaleExtensionFunction : UndertaleObject
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
        return Name.Content + " (" + ExtName.Content + ")";
    }
}

[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertaleExtensionFile : UndertaleObject
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
}

/// <summary>
/// An extension entry in a GameMaker data file.
/// </summary>
[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertaleExtension : UndertaleNamedResource
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

    /// <inheritdoc />
    public override string ToString()
    {
        return Name.Content + " (" + GetType().Name + ")";
    }

    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        writer.WriteUndertaleString(FolderName);
        writer.WriteUndertaleString(Name);
        writer.WriteUndertaleString(ClassName);
        writer.WriteUndertaleObject(Files);
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        FolderName = reader.ReadUndertaleString();
        Name = reader.ReadUndertaleString();
        ClassName = reader.ReadUndertaleString();
        Files = reader.ReadUndertaleObject<UndertalePointerList<UndertaleExtensionFile>>();
    }
}