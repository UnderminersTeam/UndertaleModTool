﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModLib.Models
{
    [PropertyChanged.AddINotifyPropertyChangedInterface]
    public class UndertaleEmbeddedTexture : UndertaleNamedResource
    {
        public UndertaleString Name { get; set; }
        public uint Scaled { get; set; } = 0;
        public uint GeneratedMips { get; set; }
        public TexData TextureData { get; set; } = new TexData();

        public void Serialize(UndertaleWriter writer)
        {
            writer.Write(Scaled);
            if (writer.undertaleData.GeneralInfo.Major >= 2)
                writer.Write(GeneratedMips);
            writer.WriteUndertaleObjectPointer(TextureData);
        }

        public void Unserialize(UndertaleReader reader)
        {
            Scaled = reader.ReadUInt32();
            if (reader.undertaleData.GeneralInfo.Major >= 2)
                GeneratedMips = reader.ReadUInt32();
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

            reader.ReadUndertaleObject(TextureData);
        }

        public override string ToString()
        {
            if (Name != null)
                return Name.Content + " (" + GetType().Name + ")";
            else
                Name = new UndertaleString("Texture Unknown Index");
            return Name.Content + " (" + GetType().Name + ")";
        }

        public class TexData : UndertaleObject, INotifyPropertyChanged
        {
            private byte[] _TextureBlob;

            public byte[] TextureBlob { get => _TextureBlob; set { _TextureBlob = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TextureBlob))); } }

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
                    reader.Position += len + 4;
                    if (type == "IEND")
                        break;
                }

                uint length = reader.Position - startAddress;
                reader.Position = startAddress;
                TextureBlob = reader.ReadBytes((int)length);
            }
        }
    }
}
