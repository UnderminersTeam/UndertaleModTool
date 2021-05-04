using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModLib.Models
{
    [PropertyChanged.AddINotifyPropertyChangedInterface]
    public class UndertaleBackground : UndertaleNamedResource
    {
        public class TileID : UndertaleObject, INotifyPropertyChanged
        {
            private uint _ID;

            public uint ID { get => _ID; set { _ID = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ID))); } }

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

        public UndertaleString Name { get; set; }
        public bool Transparent { get; set; }
        public bool Smooth { get; set; }
        public bool Preload { get; set; }
        public UndertaleTexturePageItem Texture { get; set; }
        public uint GMS2UnknownAlways2 { get; set; } = 2;
        public uint GMS2TileWidth { get; set; } = 32;
        public uint GMS2TileHeight { get; set; } = 32;
        public uint GMS2OutputBorderX { get; set; } = 2;
        public uint GMS2OutputBorderY { get; set; } = 2;
        public uint GMS2TileColumns { get; set; } = 32;
        public uint GMS2ItemsPerTileCount { get; set; } = 1;
        public uint GMS2TileCount { get; set; } = 1024;
        public uint GMS2UnknownAlwaysZero { get; set; } = 0;
        public long GMS2FrameLength { get; set; } = 66666; // time for each frame (in microseconds seemingly)
        public List<TileID> GMS2TileIds { get; set; } = new List<TileID>();

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
