using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModLib.Models
{
    public class UndertaleScript : UndertaleObject
    {
        public UndertaleString Name { get; set; }
        public UndertaleResourceById<UndertaleCode> Code { get; } = new UndertaleResourceById<UndertaleCode>("CODE");

        public void Serialize(UndertaleWriter writer)
        {
            writer.WriteUndertaleString(Name);
            writer.Write(Code.Serialize(writer));
        }

        public void Unserialize(UndertaleReader reader)
        {
            Name = reader.ReadUndertaleString();
            Code.Unserialize(reader, reader.ReadInt32());
        }

        public override string ToString()
        {
            return Name.Content + " (" + GetType().Name + ")";
        }
    }
}
