using System;

namespace UndertaleModLib.Models;

// TODO: INotifyPropertyChanged
public class UndertaleVariable : UndertaleNamedResource, ISearchable, UndertaleInstruction.ReferencedObject, IDisposable
{
    /// <inheritdoc />
    public UndertaleString Name { get; set; }
    public UndertaleInstruction.InstanceType InstanceType { get; set; }
    public int VarID { get; set; }

    /// <inheritdoc />
    public uint Occurrences { get; set; }

    /// <inheritdoc />
    public UndertaleInstruction FirstAddress { get; set; }

    /// <inheritdoc />
    public int NameStringID { get; set; }

    /// <summary>
    /// OBSOLETE. This variable is now located at <see cref="NameStringID"/>.
    /// </summary>
    [Obsolete("This variable has been renamed to NameStringID.")]
    public int UnknownChainEndingValue { get => NameStringID; set => NameStringID = value; }


    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        writer.WriteUndertaleString(Name);
        if (writer.undertaleData.GeneralInfo?.BytecodeVersion >= 15)
        {
            writer.Write((int)InstanceType);
            writer.Write(VarID);
        }
        writer.Write(Occurrences);
        if (Occurrences > 0)
            writer.Write(writer.GetAddressForUndertaleObject(FirstAddress));
        else
            writer.Write(-1);
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        Name = reader.ReadUndertaleString();
        if (reader.undertaleData.GeneralInfo?.BytecodeVersion >= 15)
        {
            InstanceType = (UndertaleInstruction.InstanceType)reader.ReadInt32();
            VarID = reader.ReadInt32();
        }
        Occurrences = reader.ReadUInt32();
        if (Occurrences > 0)
        {
            FirstAddress = reader.ReadUndertaleObjectPointer<UndertaleInstruction>();
            UndertaleInstruction.Reference<UndertaleVariable>.ParseReferenceChain(reader, this);
        }
        else
        {
            if (reader.ReadInt32() != -1)
                throw new Exception("Variable with no occurrences, but still has a first occurrence address");
            FirstAddress = null;
        }
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Name?.Content != null ? Name.Content : "<NULL_VAR_NAME>";
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);

        Name = null;
        FirstAddress = null;
    }

    /// <inheritdoc />
    public bool SearchMatches(string filter)
    {
        return Name?.SearchMatches(filter) ?? false;
    }
}