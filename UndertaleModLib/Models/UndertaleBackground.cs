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
        public class TileID : UndertaleObject, INotifyPropertyChanged
        {
            private uint _ID;

            public uint ID { get => _ID; set { _ID = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ID")); } }

            public event PropertyChangedEventHandler PropertyChanged;

            public void Serialize(UndertaleWriter writer)
            {
                writer.Write(ID);
            }

            public void Unserialize(UndertaleReader reader)
            {
                ID = reader.ReadUInt32();
            }
        }

        private UndertaleString _Name;
        private bool _Transparent;
        private bool _Smooth;
        private bool _Preload;
        private UndertaleTexturePageItem _Texture;
        private uint _GMS2UnknownAlways2 = 2;
        private uint _GMS2TileWidth = 32;
        private uint _GMS2TileHeight = 32;
        private uint _GMS2OutputBorderX = 2;
        private uint _GMS2OutputBorderY = 2;
        private uint _GMS2TileColumns = 32;
        private uint _GMS2ItemsPerTileCount = 1;
        private uint _GMS2TileCount = 1024;
        private uint _GMS2UnknownAlwaysZero = 0;
        private long _GMS2FrameLength = 66666; // time for each frame (in microseconds seemingly)
        private List<TileID> _GMS2TileIds = new List<TileID>();

        public UndertaleString Name { get => _Name; set { _Name = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Name")); } }
        public bool Transparent { get => _Transparent; set { _Transparent = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Transparent")); } }
        public bool Smooth { get => _Smooth; set { _Smooth = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Smooth")); } }
        public bool Preload { get => _Preload; set { _Preload = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Preload")); } }
        public UndertaleTexturePageItem Texture { get => _Texture; set { _Texture = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Texture")); } }

        // GMS2 tile data
        public uint GMS2UnknownAlways2 { get => _GMS2UnknownAlways2; set { _GMS2UnknownAlways2 = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("GMS2UnknownAlways2")); } }
        public uint GMS2TileWidth { get => _GMS2TileWidth; set { _GMS2TileWidth = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("GMS2TileWidth")); } }
        public uint GMS2TileHeight { get => _GMS2TileHeight; set { _GMS2TileHeight = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("GMS2TileHeight")); } }
        public uint GMS2OutputBorderX { get => _GMS2OutputBorderX; set { _GMS2OutputBorderX = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("GMS2OutputBorderX")); } }
        public uint GMS2OutputBorderY { get => _GMS2OutputBorderY; set { _GMS2OutputBorderY = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("GMS2OutputBorderY")); } }
        public uint GMS2TileColumns { get => _GMS2TileColumns; set { _GMS2TileColumns = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("GMS2TileColumns")); } }
        public uint GMS2ItemsPerTileCount { get => _GMS2ItemsPerTileCount; set { _GMS2ItemsPerTileCount = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("GMS2ItemsPerTileCount")); } }
        public uint GMS2TileCount { get => _GMS2TileCount; set { _GMS2TileCount = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("GMS2TileCount")); } }
        public uint GMS2UnknownAlwaysZero { get => _GMS2UnknownAlwaysZero; set { _GMS2UnknownAlwaysZero = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("GMS2UnknownAlwaysZero")); } }
        public long GMS2FrameLength { get => _GMS2FrameLength; set { _GMS2FrameLength = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("GMS2FrameLength")); } }
        public List<TileID> GMS2TileIds { get => _GMS2TileIds; set { _GMS2TileIds = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("GMS2TileIds")); } }

        public event PropertyChangedEventHandler PropertyChanged;

        public void Serialize(UndertaleWriter writer)
        {
            writer.WriteUndertaleString(Name);
            writer.Write(Transparent);
            writer.Write(Smooth);
            writer.Write(Preload);
            writer.WriteUndertaleObjectPointer(Texture);
            if (writer.undertaleData.GeneralInfo.Major >= 2)
            {
                writer.Write(GMS2UnknownAlways2);
                writer.Write(GMS2TileWidth);
                writer.Write(GMS2TileHeight);
                writer.Write(GMS2OutputBorderX);
                writer.Write(GMS2OutputBorderY);
                writer.Write(GMS2TileColumns);
                writer.Write(GMS2ItemsPerTileCount);
                writer.Write(GMS2TileCount);
                writer.Write(GMS2UnknownAlwaysZero);
                writer.Write(GMS2FrameLength);
                if (GMS2TileIds.Count != GMS2TileCount * GMS2ItemsPerTileCount)
                    throw new UndertaleSerializationException("Bad tile list length, should be tile count * frame count");
                for (int i = 0; i < GMS2TileCount * GMS2ItemsPerTileCount; i++)
                    GMS2TileIds[i].Serialize(writer);
            }
        }

        public void Unserialize(UndertaleReader reader)
        {
            Name = reader.ReadUndertaleString();
            Transparent = reader.ReadBoolean();
            Smooth = reader.ReadBoolean();
            Preload = reader.ReadBoolean();
            Texture = reader.ReadUndertaleObjectPointer<UndertaleTexturePageItem>();
            if (reader.undertaleData.GeneralInfo.Major >= 2)
            {
                GMS2UnknownAlways2 = reader.ReadUInt32();
                GMS2TileWidth = reader.ReadUInt32();
                GMS2TileHeight = reader.ReadUInt32();
                GMS2OutputBorderX = reader.ReadUInt32();
                GMS2OutputBorderY = reader.ReadUInt32();
                GMS2TileColumns = reader.ReadUInt32();
                GMS2ItemsPerTileCount = reader.ReadUInt32();
                GMS2TileCount = reader.ReadUInt32();
                GMS2UnknownAlwaysZero = reader.ReadUInt32();
                GMS2FrameLength = reader.ReadInt64();
                GMS2TileIds = new List<TileID>((int)GMS2TileCount * (int)GMS2ItemsPerTileCount);
                for (int i = 0; i < GMS2TileCount * GMS2ItemsPerTileCount; i++)
                {
                    TileID id = new TileID();
                    id.Unserialize(reader);
                    GMS2TileIds.Add(id);
                }
            }
        }

        public override string ToString()
        {
            return Name.Content + " (" + GetType().Name + ")";
        }
    }
}
