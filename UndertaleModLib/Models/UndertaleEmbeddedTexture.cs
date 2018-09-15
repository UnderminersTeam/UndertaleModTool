using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModLib.Models
{
    public class UndertaleEmbeddedTexture : UndertaleResource, INotifyPropertyChanged
    {
        private uint _UnknownAlwaysZero = 0;
        private TexData _TextureData = new TexData();

        public uint UnknownAlwaysZero { get => _UnknownAlwaysZero; set { _UnknownAlwaysZero = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("UnknownAlwaysZero")); } }
        public TexData TextureData { get => _TextureData; set { _TextureData = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TextureData")); } }

        public event PropertyChangedEventHandler PropertyChanged;

        public void Serialize(UndertaleWriter writer)
        {
            if (writer.undertaleData.GeneralInfo.Major >= 2)
            {
                throw new NotImplementedException();
            }
            writer.Write(UnknownAlwaysZero);
            writer.WriteUndertaleObjectPointer(TextureData);
        }

        public void Unserialize(UndertaleReader reader)
        {
            if (reader.undertaleData.GeneralInfo.Major >= 2)
                reader.ReadUInt32(); // TODO
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
            while (reader.Position % 0x80 != 0)
                if (reader.ReadByte() != 0)
                    throw new IOException("Padding error!");

            if (reader.ReadUndertaleObject<TexData>() != TextureData)
                throw new IOException();
        }

        public class TexData : UndertaleObject, INotifyPropertyChanged
        {
            private byte[] _TextureBlob;

            public byte[] TextureBlob { get => _TextureBlob; set { _TextureBlob = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TextureBlob")); } }

            public event PropertyChangedEventHandler PropertyChanged;

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
