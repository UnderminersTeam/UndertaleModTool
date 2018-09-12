using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModLib.Models
{
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
                Debug.Assert(reader.ReadInt32() == -1);
                FirstAddress = null;
            }
        }

        public override string ToString()
        {
            return Name.Content;
        }
    }

    public class UndertaleAction : UndertaleNamedResource
    {
        public uint ArgumentsCount => (uint)Arguments.Count;
        public UndertaleString Name { get; set; }
        public List<Argument> Arguments { get; private set; } = new List<Argument>();

        public void Serialize(UndertaleWriter writer)
        {
            writer.Write((uint)Arguments.Count);
            writer.WriteUndertaleString(Name);
            foreach (Argument var in Arguments)
            {
                writer.WriteUndertaleObject(var);
            }
        }

        public void Unserialize(UndertaleReader reader)
        {
            uint count = reader.ReadUInt32();
            Name = reader.ReadUndertaleString();
            Arguments.Clear();
            for (uint i = 0; i < count; i++)
            {
                Arguments.Add(reader.ReadUndertaleObject<Argument>());
            }
            Debug.Assert(Arguments.Count == count);
        }

        public class Argument : UndertaleObject
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
