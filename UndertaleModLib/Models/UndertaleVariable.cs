using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModLib.Models
{
    // TODO: INotifyPropertyChanged
    public class UndertaleVariable : UndertaleResource, ISearchable, UndertaleInstruction.ReferencedObject
    {
        public UndertaleString Name { get; set; }
        public UndertaleInstruction.InstanceType InstanceType { get; set; }
        public int VarID { get; set; }

        public uint Occurrences { get; set; }
        public UndertaleInstruction FirstAddress { get; set; }
        public int UnknownChainEndingValue { get; set; } // looks like an identifier or counter of some kind. Increases in every variable, but I can't find the pattern

        public void Serialize(UndertaleWriter writer)
        {
            writer.WriteUndertaleString(Name);
            writer.Write((int)InstanceType);
            writer.Write(VarID);
            writer.Write(Occurrences);
            if (Occurrences > 0)
                writer.Write(writer.GetAddressForUndertaleObject(FirstAddress));
            else
                writer.Write((int)-1);
        }
        
        public void Unserialize(UndertaleReader reader)
        {
            Name = reader.ReadUndertaleString();
            InstanceType = (UndertaleInstruction.InstanceType)reader.ReadInt32();
            VarID = reader.ReadInt32();
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

        public override string ToString()
        {
            return Name.Content;
        }

        public bool SearchMatches(string filter)
        {
            return Name?.SearchMatches(filter) ?? false;
        }
    }
}
