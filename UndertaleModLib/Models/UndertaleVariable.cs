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
        public int InstanceType { get; set; }
        public uint Unknown { get; set; } // some kind of 'parent object' identifier? either 0 or increasing numbers, with the exception of a couple -10
        internal uint Occurrences;

        internal UndertaleInstruction FirstAddress;
        public int UnknownChainEndingValue { get; set; } // looks like an identifier or counter of some kind. Increases in every variable, but I can't find the pattern

        public void Serialize(UndertaleWriter writer)
        {
            writer.WriteUndertaleString(Name);
            writer.Write(InstanceType);
            writer.Write(Unknown);
            writer.Write(Occurrences);
            if (Occurrences > 0)
                writer.Write(writer.GetAddressForUndertaleObject(FirstAddress));
            else
                writer.Write((int)-1);
        }

        //private static int id = 0;
        public void Unserialize(UndertaleReader reader)
        {
            Name = reader.ReadUndertaleString();
            InstanceType = reader.ReadInt32();
            Unknown = reader.ReadUInt32();
            Occurrences = reader.ReadUInt32();
            //Debug.WriteLine("Variable " + (id++) + " at " + reader.GetAddressForUndertaleObject(Name).ToString("X8") + " child of " + Unknown);
            if (Occurrences > 0)
            {
                FirstAddress = reader.ReadUndertaleObjectPointer<UndertaleInstruction>();

                // Parse the chain of references
                UndertaleInstruction.Reference<UndertaleVariable> reference = null;
                uint addr = reader.GetAddressForUndertaleObject(FirstAddress);
                for (int i = 0; i < Occurrences; i++)
                {
                    reference = reader.GetUndertaleObjectAtAddress<UndertaleInstruction>(addr).GetReference<UndertaleVariable>();
                    if (reference == null)
                        throw new IOException("Failed to find reference at " + addr);
                    reference.Target = this;
                    // Debug.WriteLine("* " + addr.ToString("X8"));
                    addr += (uint)reference.NextOccurrenceOffset;
                }
                //Debug.WriteLine("* " + reference.NextOccurrenceOffset.ToString() + " (ending value)");
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
}
