using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModLib.Models
{
    // TODO: I'm not fully sure about the naming of these...

    public class UndertaleFunctionDeclaration : UndertaleObject
    {
        public UndertaleString Name { get; set; }
        public uint Occurrences { get; set; }

        // TODO: temporary untill I parse ref chains
        public UndertaleCode FirstAddressCode { get; set; }
        public uint FirstAddressOffset { get; set; }
        public bool FirstAddressOk { get; set; }

        public void Serialize(UndertaleWriter writer)
        {
            writer.WriteUndertaleString(Name);
            writer.Write(Occurrences);
            int FirstAddress = FirstAddressOk ? (int)(FirstAddressCode._BytecodeAbsoluteAddress + FirstAddressOffset) : -1;
            writer.Write(FirstAddress);
        }

        public void Unserialize(UndertaleReader reader)
        {
            Name = reader.ReadUndertaleString();
            Occurrences = reader.ReadUInt32();
            int FirstAddress = reader.ReadInt32();
            FirstAddressOk = (FirstAddress > 0);
            if (FirstAddressOk)
            {
                foreach (UndertaleCode code in reader.undertaleData.Code) // should be already parsed
                {
                    if (code._BytecodeAbsoluteAddress <= FirstAddress && (FirstAddressCode == null || code._BytecodeAbsoluteAddress > FirstAddressCode._BytecodeAbsoluteAddress))
                        FirstAddressCode = code;
                }
                FirstAddressOffset = (uint)(FirstAddress - FirstAddressCode._BytecodeAbsoluteAddress);
            }

            uint addr = (uint)FirstAddress;
            for (int i = 0; i < Occurrences; i++)
            {
                UndertaleInstruction instr = reader.GetUndertaleObjectAtAddress<UndertaleInstruction>(addr);
                UndertaleInstruction.Reference<UndertaleFunctionDeclaration> reference = null;
                if (instr.Function != null)
                    reference = instr.Function;
                if (reference == null)
                    throw new IOException("Failed to find reference at " + addr);
                reference.Target = this;
                addr += (uint)reference.NextOccurrenceOffset;
            }
        }

        public override string ToString()
        {
            return Name.Content;
        }
    }

    public class UndertaleFunctionDefinition : UndertaleObject
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
