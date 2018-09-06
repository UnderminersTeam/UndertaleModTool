using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModLib.Models
{
    public class UndertaleEmbeddedTexture : UndertaleObject
    {
        public uint UnknownAlwaysZero { get; set; }
        public TexData TextureData { get; set; }

        public void Serialize(UndertaleWriter writer)
        {
            writer.Write(UnknownAlwaysZero);
            writer.WriteUndertaleObjectPointer(TextureData);
        }

        public void Unserialize(UndertaleReader reader)
        {
            UnknownAlwaysZero = reader.ReadUInt32();
            TextureData = reader.ReadUndertaleObjectPointer<TexData>();
        }

        public void SerializeBlob(UndertaleWriter writer)
        {
            // padding
            while (writer.Position % 0x80 != 0)
                writer.Write((byte)0);

            writer.WriteUndertaleObject(TextureData);
        }

        public void UnserializeBlob(UndertaleReader reader)
        {
            // padding
            while (reader.Position % 0x80 != 0)
                if (reader.ReadByte() != 0)
                    throw new IOException("Padding error!");

            if (reader.ReadUndertaleObject<TexData>() != TextureData)
                throw new IOException();
        }

        public class TexData : UndertaleObject
        {
            public byte[] TextureBlob { get; set; }

            public void Serialize(UndertaleWriter writer)
            {
                writer.Write(TextureBlob);
            }

            public void Unserialize(UndertaleReader reader)
            {
                uint startAddress = reader.Position;

                // There is no length anywhere as far as I can see
                // The only thing we can do is parse the image to find the end
                if (!reader.ReadBytes(8).SequenceEqual(new byte[8] { 137, 80, 78, 71, 13, 10, 26, 10 }))
                    throw new IOException("PNG header error");
                while (true)
                {
                    // PNG is big endian and BinaryRead can't handle that (damn)
                    uint len = (uint)reader.ReadByte() << 24 | (uint)reader.ReadByte() << 16 | (uint)reader.ReadByte() << 8 | (uint)reader.ReadByte();
                    string type = Encoding.UTF8.GetString(reader.ReadBytes(4));
                    byte[] data = reader.ReadBytes((int)len);
                    uint crc = (uint)reader.ReadByte() << 24 | (uint)reader.ReadByte() << 16 | (uint)reader.ReadByte() << 8 | (uint)reader.ReadByte();
                    if (type == "IEND")
                        break;
                }

                uint length = reader.Position - startAddress;
                reader.Position = startAddress;
                TextureBlob = reader.ReadBytes((int)length);
            }
        }

        public override string ToString()
        {
            return " (" + GetType().Name + ")";
        }
    }
}
