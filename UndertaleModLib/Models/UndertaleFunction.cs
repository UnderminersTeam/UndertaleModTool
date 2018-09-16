using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModLib.Models
{
    // TODO: INotifyPropertyChanged
    public class UndertaleFunction : UndertaleNamedResource
    {
        public UndertaleString Name { get; set; }
        public int UnknownChainEndingValue { get; set; }

        internal uint Occurrences;
        internal UndertaleInstruction FirstAddress;

        public void Serialize(UndertaleWriter writer)
        {
            writer.WriteUndertaleString(Name);
            writer.Write(Occurrences);
            if (Occurrences > 0)
                writer.Write(writer.GetAddressForUndertaleObject(FirstAddress));
            else
                writer.Write((int)-1);
        }

        public void Unserialize(UndertaleReader reader)
        {
            Name = reader.ReadUndertaleString();
            Occurrences = reader.ReadUInt32();
            if (Occurrences > 0)
            {
                FirstAddress = reader.ReadUndertaleObjectPointer<UndertaleInstruction>();

                // Parse the chain of references
                UndertaleInstruction.Reference<UndertaleFunction> reference = null;
                uint addr = reader.GetAddressForUndertaleObject(FirstAddress);
                for (int i = 0; i < Occurrences; i++)
                {
                    reference = reader.GetUndertaleObjectAtAddress<UndertaleInstruction>(addr).GetReference<UndertaleFunction>();
                    if (reference == null)
                        throw new IOException("Failed to find reference at " + addr);
                    reference.Target = this;
                    addr += (uint)reference.NextOccurrenceOffset;
                }
                UnknownChainEndingValue = reference.NextOccurrenceOffset;
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
