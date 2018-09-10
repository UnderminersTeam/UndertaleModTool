using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModLib.Models
{
    public class UndertaleVariable : UndertaleResource
    {
        public UndertaleString Name { get; set; }
        public uint InstanceType { get; set; }
        public uint Unknown { get; set; }
        public uint Occurrences { get; set; }

        // TODO: temporary untill I parse ref chains
        public UndertaleCode FirstAddressCode { get; set; }
        public uint FirstAddressOffset { get; set; }
        public bool FirstAddressOk { get; set; }

        public int UnknownUniqueChainEndingValue { get; set; } // looks like an identifier of some kind...

        public void Serialize(UndertaleWriter writer)
        {
            writer.WriteUndertaleString(Name);
            writer.Write(InstanceType);
            writer.Write(Unknown);
            writer.Write(Occurrences);
            int FirstAddress;
            if (Occurrences > 0)
                FirstAddress = (int)(FirstAddressCode._BytecodeAbsoluteAddress + FirstAddressOffset);
            else
                FirstAddress = UnknownUniqueChainEndingValue;
            writer.Write(FirstAddress);
        }

        public void Unserialize(UndertaleReader reader)
        {
            Name = reader.ReadUndertaleString();
            InstanceType = reader.ReadUInt32();
            Unknown = reader.ReadUInt32();
            Occurrences = reader.ReadUInt32();
            int FirstAddress = reader.ReadInt32();
            if (Occurrences > 0)
            {
                foreach (UndertaleCode code in reader.undertaleData.Code) // should be already parsed
                {
                    if (code._BytecodeAbsoluteAddress <= FirstAddress && (FirstAddressCode == null || code._BytecodeAbsoluteAddress > FirstAddressCode._BytecodeAbsoluteAddress))
                        FirstAddressCode = code;
                }
                FirstAddressOffset = (uint)(FirstAddress - FirstAddressCode._BytecodeAbsoluteAddress);

                // Parse the chain of references
                UndertaleInstruction.Reference<UndertaleVariable> reference = null;
                uint addr = (uint)FirstAddress;
                for (int i = 0; i < Occurrences; i++)
                {
                    UndertaleInstruction instr = reader.GetUndertaleObjectAtAddress<UndertaleInstruction>(addr);
                    reference = null;
                    if (instr.Value is UndertaleInstruction.Reference<UndertaleVariable>)
                        reference = (instr.Value as UndertaleInstruction.Reference<UndertaleVariable>);
                    if (instr.Destination != null)
                        reference = instr.Destination;
                    if (reference == null)
                        throw new IOException("Failed to find reference at " + addr);
                    reference.Target = this;
                    addr += (uint)reference.NextOccurrenceOffset;
                }
                UnknownUniqueChainEndingValue = reference.NextOccurrenceOffset;
            }
            else
            {
                UnknownUniqueChainEndingValue = FirstAddress;
            }
        }

        public override string ToString()
        {
            return Name.Content;
        }
    }
}
