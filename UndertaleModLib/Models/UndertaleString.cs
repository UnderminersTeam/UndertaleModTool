using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModLib.Models
{
    public class UndertaleString : UndertaleObject
    {
        public string Content { get; set; }

        public UndertaleString()
        {
        }

        public UndertaleString(string Content)
        {
            this.Content = Content;
        }

        public void Serialize(UndertaleWriter writer)
        {
            byte[] chars = Encoding.UTF8.GetBytes(Content);
            writer.Write((uint)chars.Length);
            writer.Write(chars);
            writer.Write((byte)0);
        }

        public void Unserialize(UndertaleReader reader)
        {
            uint length = reader.ReadUInt32();
            byte[] chars = reader.ReadBytes((int)length);
            Content = Encoding.UTF8.GetString(chars);
            if (reader.ReadByte() != 0)
                throw new IOException("The string was not null terminated!");
        }

        public override string ToString()
        {
            return "\"" + Content + "\"";
        }
    }
}
