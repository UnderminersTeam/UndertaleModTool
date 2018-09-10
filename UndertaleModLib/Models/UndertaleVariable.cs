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
        public uint Unknown { get; set; } // some kind of 'parent object' identifier? either 0 or increasing numbers, with the exception of a couple -10
        public uint Occurrences { get; set; }

        // TODO: temporary untill I parse ref chains
        public UndertaleCode FirstAddressCode { get; set; }
        public uint FirstAddressOffset { get; set; }
        public bool FirstAddressOk { get; set; }

        public int UnknownUniqueChainEndingValue { get; set; } // looks like an identifier or counter of some kind. Increases in every variable, but I can't find the pattern

        public void Serialize(UndertaleWriter writer)
        {
            writer.WriteUndertaleString(Name);
            writer.Write(InstanceType);
            writer.Write(Unknown);
            writer.Write(Occurrences);
            if (Occurrences > 0)
                writer.Write((int)(FirstAddressCode._BytecodeAbsoluteAddress + FirstAddressOffset));
            else
                writer.Write((int)-1);
        }

        //private static int id = 0;
        public void Unserialize(UndertaleReader reader)
        {
            Name = reader.ReadUndertaleString();
            InstanceType = reader.ReadUInt32();
            Unknown = reader.ReadUInt32();
            Occurrences = reader.ReadUInt32();
            int FirstAddress = reader.ReadInt32();
            //Debug.WriteLine("Variable " + (id++) + " at " + reader.GetAddressForUndertaleObject(Name).ToString("X8") + " child of " + Unknown);
            if (Occurrences > 0)
            {
                //Debug.WriteLine("* " + FirstAddress.ToString("X8") + " (first)");
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
                    /*if (i < Occurrences - 1)
                        Debug.WriteLine("* " + addr.ToString("X8"));*/
                }
                //Debug.WriteLine("* " + reference.NextOccurrenceOffset.ToString() + " (ending value)");
                UnknownUniqueChainEndingValue = reference.NextOccurrenceOffset;
            }
            else
            {
                Debug.Assert(FirstAddress == -1);
            }
        }

        public override string ToString()
        {
            return Name.Content;
        }
    }
}
