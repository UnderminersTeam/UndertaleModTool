using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModLib.Models
{
    public class UndertaleBackground : UndertaleNamedResource, INotifyPropertyChanged
    {
        private UndertaleString _Name;
        private uint _Unknown1 = 0;
        private uint _Unknown2 = 0;
        private uint _Unknown3 = 0;
        private UndertaleTexturePageItem _Texture;
        private uint _GMS2Unknown1 = 2;
        private uint _GMS2TileWidth = 32;
        private uint _GMS2TileHeight = 32;
        private uint _GMS2OutputBorderX = 2;
        private uint _GMS2OutputBorderY = 2;
        private uint _GMS2Unknown6 = 32;
        private uint _GMS2ItemsPerTileCount = 1;
        private uint _GMS2TileCount = 1024;
        private uint _GMS2Unknown9 = 0;
        private uint _GMS2Unknown10 = 66666;
        private uint _GMS2Unknown11 = 0;
        private uint[] _GMS2TileIds;

        public UndertaleString Name { get => _Name; set { _Name = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Name")); } }
        public uint Unknown1 { get => _Unknown1; set { _Unknown1 = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Unknown1")); } }
        public uint Unknown2 { get => _Unknown2; set { _Unknown2 = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Unknown2")); } }
        public uint Unknown3 { get => _Unknown3; set { _Unknown3 = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Unknown3")); } }
        public UndertaleTexturePageItem Texture { get => _Texture; set { _Texture = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Texture")); } }

        // GMS2 tile data
        public uint GMS2Unknown1 { get => _GMS2Unknown1; set { _GMS2Unknown1 = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("GMS2Unknown1")); } }
        public uint GMS2TileWidth { get => _GMS2TileWidth; set { _GMS2TileWidth = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("GMS2TileWidth")); } }
        public uint GMS2TileHeight { get => _GMS2TileHeight; set { _GMS2TileHeight = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("GMS2TileHeight")); } }
        public uint GMS2OutputBorderX { get => _GMS2OutputBorderX; set { _GMS2OutputBorderX = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("GMS2OutputBorderX")); } }
        public uint GMS2OutputBorderY { get => _GMS2OutputBorderY; set { _GMS2OutputBorderY = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("GMS2OutputBorderY")); } }
        public uint GMS2Unknown6 { get => _GMS2Unknown6; set { _GMS2Unknown6 = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("GMS2Unknown6")); } }
        public uint GMS2ItemsPerTileCount { get => _GMS2ItemsPerTileCount; set { _GMS2ItemsPerTileCount = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("GMS2ItemsPerTileCount")); } }
        public uint GMS2TileCount { get => _GMS2TileCount; set { _GMS2TileCount = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("GMS2TileCount")); } }
        public uint GMS2Unknown9 { get => _GMS2Unknown9; set { _GMS2Unknown9 = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("GMS2Unknown9")); } }
        public uint GMS2Unknown10 { get => _GMS2Unknown10; set { _GMS2Unknown10 = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("GMS2Unknown10")); } }
        public uint GMS2Unknown11 { get => _GMS2Unknown11; set { _GMS2Unknown11 = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("GMS2Unknown11")); } }
        public uint[] GMS2TileIds { get => _GMS2TileIds; set { _GMS2TileIds = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("GMS2TileIds")); } }

        public event PropertyChangedEventHandler PropertyChanged;

        public void Serialize(UndertaleWriter writer)
        {
            writer.WriteUndertaleString(Name);
            writer.Write(Unknown1);
            writer.Write(Unknown2);
            writer.Write(Unknown3);
            writer.WriteUndertaleObjectPointer(Texture);
            if (writer.undertaleData.GeneralInfo.Major >= 2)
            {
                writer.Write(GMS2Unknown1);
                writer.Write(GMS2TileWidth);
                writer.Write(GMS2TileHeight);
                writer.Write(GMS2OutputBorderX);
                writer.Write(GMS2OutputBorderY);
                writer.Write(GMS2Unknown6);
                writer.Write(GMS2ItemsPerTileCount);
                writer.Write(GMS2TileCount);
                writer.Write(GMS2Unknown9);
                writer.Write(GMS2Unknown10);
                writer.Write(GMS2Unknown11);
                if (GMS2TileIds.Length != GMS2TileCount * GMS2ItemsPerTileCount)
                    throw new Exception("Bad tile list length");
                for (int i = 0; i < GMS2TileCount * GMS2ItemsPerTileCount; i++)
                    writer.Write(GMS2TileIds[i]);
            }
        }

        public void Unserialize(UndertaleReader reader)
        {
            Name = reader.ReadUndertaleString();
            Unknown1 = reader.ReadUInt32();
            Unknown2 = reader.ReadUInt32();
            Unknown3 = reader.ReadUInt32();
            Texture = reader.ReadUndertaleObjectPointer<UndertaleTexturePageItem>();
            if (reader.undertaleData.GeneralInfo.Major >= 2)
            {
                GMS2Unknown1 = reader.ReadUInt32();
                GMS2TileWidth = reader.ReadUInt32();
                GMS2TileHeight = reader.ReadUInt32();
                GMS2OutputBorderX = reader.ReadUInt32();
                GMS2OutputBorderY = reader.ReadUInt32();
                GMS2Unknown6 = reader.ReadUInt32();
                GMS2ItemsPerTileCount = reader.ReadUInt32();
                GMS2TileCount = reader.ReadUInt32();
                GMS2Unknown9 = reader.ReadUInt32();
                GMS2Unknown10 = reader.ReadUInt32();
                GMS2Unknown11 = reader.ReadUInt32();
                GMS2TileIds = new uint[GMS2TileCount * GMS2ItemsPerTileCount];
                for (int i = 0; i < GMS2TileCount * GMS2ItemsPerTileCount; i++)
                    GMS2TileIds[i] = reader.ReadUInt32();
            }
        }

        public override string ToString()
        {
            return Name.Content + " (" + GetType().Name + ")";
        }
    }
}
