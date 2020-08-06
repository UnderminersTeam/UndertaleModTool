using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModLib.Models
{
    public class UndertaleEmbeddedAudio : UndertaleResource, PaddedObject, INotifyPropertyChanged
    {
        private byte[] _Data = new byte[0];

        public byte[] Data { get => _Data; set { _Data = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Data")); } }

        public event PropertyChangedEventHandler PropertyChanged;

        public void Serialize(UndertaleWriter writer)
        {
            writer.Write((uint)Data.Length);
            writer.Write(Data);
        }

        public void SerializePadding(UndertaleWriter writer)
        {
            writer.Position += (4 - (writer.Position % 4)) % 4;
        }

        public void Unserialize(UndertaleReader reader)
        {
            uint len = reader.ReadUInt32();
            Data = reader.ReadBytes((int)len);
            Debug.Assert(Data.Length == len);
        }

        public void UnserializePadding(UndertaleReader reader)
        {
            while (reader.Position % 4 != 0)
                if (reader.ReadByte() != 0)
                    throw new IOException("Padding error!");
        }

        public override string ToString()
        {
            return "(" + GetType().Name + ")";
        }
    }
}
