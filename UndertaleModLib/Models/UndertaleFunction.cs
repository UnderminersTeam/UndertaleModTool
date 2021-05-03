using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static UndertaleModLib.Models.UndertaleGeneralInfo;

namespace UndertaleModLib.Models
{
    public class UndertaleFunction : UndertaleNamedResource, INotifyPropertyChanged, UndertaleInstruction.ReferencedObject
    {
        public FunctionClassification Classification { get; set; }
        public UndertaleString Name { get; set; }
        public int NameStringID { get; set; }

        public uint Occurrences { get; set; }
        public UndertaleInstruction FirstAddress { get; set; }

        public bool GMS2_3 { get; set; } = false;

        [Obsolete("This variable has been renamed to NameStringID.")]
        public int UnknownChainEndingValue { get => NameStringID; set => NameStringID = value; }

        public event PropertyChangedEventHandler PropertyChanged;

        public void Serialize(UndertaleWriter writer)
        {
            writer.WriteUndertaleString(Name);
            writer.Write(Occurrences);
            if (Occurrences > 0)
            {
                uint addr = writer.GetAddressForUndertaleObject(FirstAddress);
                if (GMS2_3)
                    writer.Write((addr == 0) ? 0 : (addr + 4)); // in GMS 2.3, it points to the actual reference rather than the instruction
                else
                    writer.Write(addr);
            }
            else
                writer.Write((int)-1);
        }

        public void Unserialize(UndertaleReader reader)
        {
            Name = reader.ReadUndertaleString();
            Occurrences = reader.ReadUInt32();
            if (Occurrences > 0)
            {
                if (reader.GMS2_3)
                {
                    GMS2_3 = true;
                    FirstAddress = reader.GetUndertaleObjectAtAddress<UndertaleInstruction>(reader.ReadUInt32() - 4);
                }
                else
                    FirstAddress = reader.ReadUndertaleObjectPointer<UndertaleInstruction>();
                UndertaleInstruction.Reference<UndertaleFunction>.ParseReferenceChain(reader, this);
            }
            else
            {
                if (reader.ReadInt32() != -1)
                    throw new Exception("Function with no occurrences, but still has a first occurrence address");
                FirstAddress = null;
            }
        }

        public override string ToString()
        {
            return Name.Content;
        }
    }

    // Seems to be unused. You can remove all entries and the game still works normally.
    // Maybe the GM:S debugger uses this data?
    // TODO: INotifyPropertyChanged
    public class UndertaleCodeLocals : UndertaleNamedResource
    {
        public UndertaleString Name { get; set; }
        public ObservableCollection<LocalVar> Locals { get; } = new ObservableCollection<LocalVar>();

        public void Serialize(UndertaleWriter writer)
        {
            writer.Write((uint)Locals.Count);
            writer.WriteUndertaleString(Name);
            foreach (LocalVar var in Locals)
            {
                writer.WriteUndertaleObject(var);
            }
        }

        public void Unserialize(UndertaleReader reader)
        {
            uint count = reader.ReadUInt32();
            Name = reader.ReadUndertaleString();
            Locals.Clear();
            for (uint i = 0; i < count; i++)
            {
                Locals.Add(reader.ReadUndertaleObject<LocalVar>());
            }
            Debug.Assert(Locals.Count == count);
        }

        public bool HasLocal(string varName)
        {
            return Locals.Any(local=>local.Name.Content == varName);
        }
        
        // TODO: INotifyPropertyChanged
        public class LocalVar : UndertaleObject
        {
            public uint Index { get; set; }
            public UndertaleString Name { get; set; }

            public void Serialize(UndertaleWriter writer)
            {
                writer.Write(Index);
                writer.WriteUndertaleString(Name);
            }

            public void Unserialize(UndertaleReader reader)
            {
                Index = reader.ReadUInt32();
                Name = reader.ReadUndertaleString();
            }
        }

        public override string ToString()
        {
            return Name.Content + " (" + GetType().Name + ")";
        }
    }
}
