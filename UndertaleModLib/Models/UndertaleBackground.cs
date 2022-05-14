﻿using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace UndertaleModLib.Models;

/// <summary>
/// A background or tileset entry in a data file.
/// </summary>
/// <remarks>For Game Maker Studio: 2, this will only ever be a tileset. For Game Maker Studio: 1, this is usually a background,
/// but is sometimes repurposed as use for a tileset as well.</remarks>
[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertaleBackground : UndertaleNamedResource, IDisposable
{
    /// <summary>
    /// A tile id, which can be used for referencing specific tiles in a tileset. Game Maker Studio 2 only.
    /// </summary>
    public class TileID : UndertaleObject, INotifyPropertyChanged
    {
        private uint _id;

        /// <summary>
        /// The id of a specific tile.
        /// </summary>
        public uint ID { get => _id; set { _id = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ID))); } }

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        /// <inheritdoc />
        public void Serialize(UndertaleWriter writer)
        {
            writer.Write(ID);
        }

        /// <inheritdoc />
        public void Unserialize(UndertaleReader reader)
        {
            ID = reader.ReadUInt32();
        }
    }

    /// <summary>
    /// The name of the background.
    /// </summary>
    public UndertaleString Name { get; set; }

    /// <summary>
    /// Whether the background should be transparent.
    /// </summary>
    public bool Transparent { get; set; }

    /// <summary>
    /// Whether the background should get smoothed.
    /// </summary>
    public bool Smooth { get; set; }

    /// <summary>
    /// Whether to preload the background.
    /// </summary>
    public bool Preload { get; set; }

    /// <summary>
    /// The <see cref="UndertaleTexturePageItem"/> this background uses.
    /// </summary>
    public UndertaleTexturePageItem Texture { get; set; }


    /// <summary>
    /// TODO: Functionality currently unknown.
    /// </summary>
    public uint GMS2UnknownAlways2 { get; set; } = 2;

    /// <summary>
    /// The tile width of the tileset. Game Maker Studio 2 only.
    /// </summary>
    public uint GMS2TileWidth { get; set; } = 32;

    /// <summary>
    /// The tile height of the tileset. Game Maker Studio 2 only.
    /// </summary>
    public uint GMS2TileHeight { get; set; } = 32;

    /// <summary>
    /// The output border X of the tileset. Game Maker Studio 2 only.
    /// </summary>
    public uint GMS2OutputBorderX { get; set; } = 2;

    /// <summary>
    /// The output Border Y of the tileset. Game Maker Studio 2 only.
    /// </summary>
    public uint GMS2OutputBorderY { get; set; } = 2;

    /// <summary>
    /// The amount of columns this tileset has.
    /// </summary>
    public uint GMS2TileColumns { get; set; } = 32;

    /// <summary>
    /// The number of frames of the tileset animation.
    /// </summary>
    public uint GMS2ItemsPerTileCount { get; set; } = 1;

    /// <summary>
    /// The amount of tiles this tileset has.
    /// </summary>
    public uint GMS2TileCount { get; set; } = 1024;

    /// <summary>
    /// TODO: Functionality currently unknown.
    /// </summary>
    public uint GMS2UnknownAlwaysZero { get; set; } = 0;

    /// <summary>
    /// The time for each frame in microseconds.
    /// </summary>
    public long GMS2FrameLength { get; set; } = 66666;

    /// <summary>
    /// All tile ids of this tileset. Game Maker Studio 2 only.
    /// </summary>
    public List<TileID> GMS2TileIds { get; set; } = new List<TileID>();

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
    public override string ToString()
    {
        return Name?.Content + " (" + GetType().Name + ")";
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);

        GMS2TileIds = new();
        Name = null;
        Texture = null;
    }
}