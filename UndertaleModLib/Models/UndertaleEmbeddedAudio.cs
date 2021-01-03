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
    public class UndertaleEmbeddedAudio : UndertaleNamedResource, PaddedObject, INotifyPropertyChanged
    {
        private UndertaleString _Name;
        private byte[] _Data = new byte[0];

        public UndertaleString Name { get => _Name; set { _Name = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Name")); } }
        public byte[] Data { get => _Data; set { _Data = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Data")); } }

        public event PropertyChangedEventHandler PropertyChanged;

        public void Serialize(UndertaleWriter writer)
        {
            writer.Write((uint)Data.Length);
            writer.Write(Data);
        }

        public void SerializePadding(UndertaleWriter writer)
        {
            while (writer.Position % 4 != 0)
                writer.Write((byte)0);
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
            return Name.Content + " (" + GetType().Name + ")";
        }
    }
}
