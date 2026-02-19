using System;
using System.Collections.Generic;
using System.ComponentModel;
using UndertaleModLib.Project.SerializableAssets;
using UndertaleModLib.Project;

namespace UndertaleModLib.Models;

/// <summary>
/// A background or tileset entry in a data file.
/// </summary>
/// <remarks>For GameMaker Studio 2, this will only ever be a tileset. For GameMaker: Studio 1, this is usually a background,
/// but is sometimes repurposed as use for a tileset as well.</remarks>
[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertaleBackground : UndertaleNamedResource, IProjectAsset, INotifyPropertyChanged, IDisposable
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
    /// Version number representing GMS2 format; always 2. Game Maker Studio 2 only.
    /// </summary>
    public uint GMS2VersionNumber { get; set; } = 2;

    /// <summary>
    /// The width of a tile in this tileset. Game Maker Studio 2 only.
    /// </summary>
    public uint GMS2TileWidth { get; set; } = 32;

    /// <summary>
    /// The height of a tile in this. Game Maker Studio 2 only.
    /// </summary>
    public uint GMS2TileHeight { get; set; } = 32;

    /// <summary>
    /// The amount of extra empty pixels left and right next to a tile in this tileset. Game Maker Studio 2 only.
    /// </summary>
    public uint GMS2OutputBorderX { get; set; } = 2;

    /// <summary>
    /// The amount of extra empty pixels above and below a tile in this tileset. Game Maker Studio 2 only.
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

    private UndertaleResourceById<UndertaleSprite, UndertaleChunkSPRT> _gms2ExportedSprite = null;

    /// <summary>
    /// Index of the exported sprite, if one was exported, in GameMaker 2023.8 and above.
    /// </summary>
    public UndertaleSprite GMS2ExportedSprite { get => _gms2ExportedSprite?.Resource; set { (_gms2ExportedSprite ??= new()).Resource = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GMS2ExportedSprite))); } }

    /// <summary>
    /// The time for each frame in microseconds. Game Maker Studio 2 only.
    /// </summary>
    public long GMS2FrameLength { get; set; } = 66666;

    /// <summary>
    /// All tile ids of this tileset. Game Maker Studio 2 only.
    /// </summary>
    public UndertaleObservableList<TileID> GMS2TileIds { get; set; } = new(32);

    /// <inheritdoc />
    public event PropertyChangedEventHandler PropertyChanged;

    /// <remarks>
    /// GM 2024.14.1+ only.
    /// </remarks>
    public uint GMS2TileSeparationX { get; set; } = 0;

    /// <remarks>
    /// GM 2024.14.1+ only.
    /// </remarks>
    public uint GMS2TileSeparationY { get; set; } = 0;

    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        writer.WriteUndertaleString(Name);
        writer.Write(Transparent);
        writer.Write(Smooth);
        writer.Write(Preload);
        writer.WriteUndertaleObjectPointer(Texture);
        if (writer.undertaleData.IsGameMaker2())
        {
            writer.Write(GMS2VersionNumber);
            writer.Write(GMS2TileWidth);
            writer.Write(GMS2TileHeight);
            if (writer.undertaleData.IsVersionAtLeast(2024, 14, 1))
            {
                writer.Write(GMS2TileSeparationX);
                writer.Write(GMS2TileSeparationY);
            }
            writer.Write(GMS2OutputBorderX);
            writer.Write(GMS2OutputBorderY);
            writer.Write(GMS2TileColumns);
            writer.Write(GMS2ItemsPerTileCount);
            writer.Write(GMS2TileCount);
            if (writer.undertaleData.IsVersionAtLeast(2023, 8))
            {
                (_gms2ExportedSprite ?? new()).Serialize(writer);
            }
            else
            {
                writer.Write(0);
            }
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
        if (reader.undertaleData.IsGameMaker2())
        {
            GMS2VersionNumber = reader.ReadUInt32();
            GMS2TileWidth = reader.ReadUInt32();
            GMS2TileHeight = reader.ReadUInt32();
            if (reader.undertaleData.IsVersionAtLeast(2024, 14, 1))
            {
                GMS2TileSeparationX = reader.ReadUInt32();
                GMS2TileSeparationY = reader.ReadUInt32();
            }
            GMS2OutputBorderX = reader.ReadUInt32();
            GMS2OutputBorderY = reader.ReadUInt32();
            GMS2TileColumns = reader.ReadUInt32();
            GMS2ItemsPerTileCount = reader.ReadUInt32();
            GMS2TileCount = reader.ReadUInt32();
            if (reader.undertaleData.IsVersionAtLeast(2023, 8))
            {
                (_gms2ExportedSprite = new()).Unserialize(reader);
            }
            else
            {
                int id = reader.ReadInt32();
                if (id != 0)
                {
                    reader.undertaleData.SetGMS2Version(2023, 8);
                    (_gms2ExportedSprite = new()).UnserializeById(reader, id);
                }
            }
            GMS2FrameLength = reader.ReadInt64();
            GMS2TileIds = new((int)GMS2TileCount * (int)GMS2ItemsPerTileCount);
            for (int i = 0; i < GMS2TileCount * GMS2ItemsPerTileCount; i++)
            {
                TileID id = new();
                id.Unserialize(reader);
                GMS2TileIds.InternalAdd(id);
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

    /// <inheritdoc/>
    ISerializableProjectAsset IProjectAsset.GenerateSerializableProjectAsset(ProjectContext projectContext)
    {
        SerializableBackground serializable = new();
        serializable.PopulateFromData(projectContext, this);
        return serializable;
    }

    /// <inheritdoc/>
    public string ProjectName => Name?.Content ?? "<unknown name>";

    /// <inheritdoc/>
    public SerializableAssetType ProjectAssetType => SerializableAssetType.Background;

    /// <inheritdoc/>
    public bool ProjectExportable => Name?.Content is not null && Texture is not null;
}