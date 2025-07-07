using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Underanalyzer;
using static UndertaleModLib.Models.UndertaleGeneralInfo;

namespace UndertaleModLib.Models;

/// <summary>
/// A function entry as it's used in a GameMaker data file.
/// </summary>
[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertaleFunction : UndertaleNamedResource, UndertaleInstruction.IReferencedObject, IStaticChildObjectsSize, IDisposable, IGMFunction
{
    /// <inheritdoc cref="IStaticChildObjectsSize.ChildObjectsSize" />
    public static readonly uint ChildObjectsSize = 12;

    public FunctionClassification Classification { get; set; }

    /// <summary>
    /// The name of the <see cref="UndertaleFunction"/>.
    /// </summary>
    public UndertaleString Name { get; set; }

    /// <summary>
    /// The index of <see cref="Name"/> in <see cref="UndertaleData.Strings"/>.
    /// </summary>
    public int NameStringID { get; set; }

    /// <summary>
    /// How often this <see cref="UndertaleFunction"/> is referenced in code.
    /// </summary>
    public uint Occurrences { get; set; }
    
    /// <summary>
    /// The first instruction in this function.
    /// </summary>
    public UndertaleInstruction FirstAddress { get; set; }

    [Obsolete("This variable has been renamed to NameStringID.")]
    public int UnknownChainEndingValue { get => NameStringID; set => NameStringID = value; }

    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        writer.WriteUndertaleString(Name);
        writer.Write(Occurrences);
        if (Occurrences > 0)
        {
            uint addr = writer.GetAddressForUndertaleObject(FirstAddress);
            if (writer.undertaleData.IsVersionAtLeast(2, 3))
                writer.Write((addr == 0) ? 0 : (addr + 4)); // in GMS 2.3, it points to the actual reference rather than the instruction
            else
                writer.Write(addr);
        }
        else
            writer.Write(-1);
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        Name = reader.ReadUndertaleString();
        Occurrences = reader.ReadUInt32();
        if (Occurrences > 0)
        {
            if (reader.undertaleData.IsVersionAtLeast(2, 3))
                FirstAddress = reader.GetUndertaleObjectAtAddress<UndertaleInstruction>(reader.ReadUInt32() - 4);
            else
                FirstAddress = reader.ReadUndertaleObjectPointer<UndertaleInstruction>();
            UndertaleInstruction.ParseReferenceChain(reader, this);
        }
        else
        {
            if (reader.ReadInt32() != -1)
                throw new Exception("Function with no occurrences, but still has a first occurrence address");
            FirstAddress = null;
        }
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Name?.Content;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);

        Name = null;
        FirstAddress = null;
    }

    // Underanalyzer implementations
    IGMString IGMFunction.Name => Name;
}

// Seems to be unused. You can remove all entries and the game still works normally. TODO: not true, incorrect/missing locals can wreak some weird havoc like random segfaults.
// Maybe the GM:S debugger uses this data?
// TODO: INotifyPropertyChanged
public class UndertaleCodeLocals : UndertaleNamedResource, IDisposable
{
    /// <summary>
    /// The name of the code local entry which corresponds to a code entry.
    /// </summary>
    public UndertaleString Name { get; set; }
    
    /// <summary>
    /// A collection of local variables.
    /// </summary>
    public ObservableCollection<LocalVar> Locals { get; private set; } = new ObservableCollection<LocalVar>();

    /// <summary>
    /// Creates an empty code locals entry with the given name.
    /// </summary>
    /// <param name="data">Data to add the new code to.</param>
    /// <param name="name">Name of the new code locals entry to create.</param>
    /// <returns>The new code locals entry.</returns>
    public static UndertaleCodeLocals CreateEmptyEntry(UndertaleData data, UndertaleString name)
    {
        UndertaleCodeLocals locals = new()
        {
            Name = name
        };
        locals.Locals.Add(new()
        {
            Name = data.Strings.MakeString("arguments", out int argumentsStringId),
            Index = data.IsVersionAtLeast(2, 3) ? (uint)argumentsStringId : 0
        });
        data.CodeLocals.Add(locals);
        return locals;
    }

    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        writer.Write((uint)Locals.Count);
        writer.WriteUndertaleString(Name);
        foreach (LocalVar var in Locals)
        {
            writer.WriteUndertaleObject(var);
        }
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        uint count = reader.ReadUInt32();
        Name = reader.ReadUndertaleString();
        List<LocalVar> newLocals = new((int)count);
        for (uint i = 0; i < count; i++)
            newLocals.Add(reader.ReadUndertaleObject<LocalVar>());
        Locals = new(newLocals);
        Util.DebugUtil.Assert(Locals.Count == count);
    }

    /// <inheritdoc cref="UndertaleObject.UnserializeChildObjectCount(UndertaleReader)"/>
    public static uint UnserializeChildObjectCount(UndertaleReader reader)
    {
        uint count = reader.ReadUInt32();

        reader.Position += 4 + count * LocalVar.ChildObjectsSize;

        return count;
    }

    /// <summary>
    /// Returns whether this code local entry contains any local variables matching a name.
    /// </summary>
    /// <param name="varName">The variable name to search for.</param>
    /// <returns><see langword="true"/> if a variable matching the provided name exists, otherwise <see langword="false"/>.</returns>
    public bool HasLocal(string varName)
    {
        return Locals.Any(local => local.Name.Content == varName);
    }

    // TODO: INotifyPropertyChanged
    /// <summary>
    /// A local variable. TODO: a better description for this.
    /// </summary>
    public class LocalVar : UndertaleObject, IStaticChildObjectsSize, IDisposable
    {
        /// <inheritdoc cref="IStaticChildObjectsSize.ChildObjectsSize" />
        public static readonly uint ChildObjectsSize = 8;

        /// <summary>
        /// TODO: have no idea what this index does.
        /// </summary>
        public uint Index { get; set; }
        
        /// <summary>
        /// The name of the local variable.
        /// </summary>
        public UndertaleString Name { get; set; }

        /// <inheritdoc />
        public void Serialize(UndertaleWriter writer)
        {
            writer.Write(Index);
            writer.WriteUndertaleString(Name);
        }

        /// <inheritdoc />
        public void Unserialize(UndertaleReader reader)
        {
            Index = reader.ReadUInt32();
            Name = reader.ReadUndertaleString();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            GC.SuppressFinalize(this);

            Name = null;
        }
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

        Name = null;
        Locals.Clear();
    }
}