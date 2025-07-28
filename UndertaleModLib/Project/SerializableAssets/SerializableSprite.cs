using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using UndertaleModLib.Models;
using UndertaleModLib.Util;

namespace UndertaleModLib.Project.SerializableAssets;

/// <summary>
/// A serializable version of <see cref="UndertaleSprite"/>.
/// </summary>
internal sealed class SerializableSprite : ISerializableTextureProjectAsset
{
    /// <inheritdoc/>
    public string DataName { get; set; }

    /// <inheritdoc cref="UndertaleSprite.Width"/>
    public uint Width { get; set; }

    /// <inheritdoc cref="UndertaleSprite.Height"/>
    public uint Height { get; set; }

    /// <inheritdoc cref="UndertaleSprite.MarginLeft"/>
    public int MarginLeft { get; set; }

    /// <inheritdoc cref="UndertaleSprite.MarginRight"/>
    public int MarginRight { get; set; }

    /// <inheritdoc cref="UndertaleSprite.MarginBottom"/>
    public int MarginBottom { get; set; }

    /// <inheritdoc cref="UndertaleSprite.MarginTop"/>
    public int MarginTop { get; set; }

    /// <inheritdoc cref="UndertaleSprite.Transparent"/>
    public bool Transparent { get; set; }

    /// <inheritdoc cref="UndertaleSprite.Smooth"/>
    public bool Smooth { get; set; }

    /// <inheritdoc cref="UndertaleSprite.Preload"/>
    public bool Preload { get; set; }

    /// <inheritdoc cref="UndertaleSprite.BBoxMode"/>
    public uint BBoxMode { get; set; }

    /// <inheritdoc cref="UndertaleSprite.SepMasks"/>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CollisionKinds CollisionKind { get; set; }

    /// <inheritdoc cref="UndertaleSprite.OriginX"/>
    public int OriginX { get; set; }

    /// <inheritdoc cref="UndertaleSprite.OriginY"/>
    public int OriginY { get; set; }

    /// <summary>
    /// Number of textures for the sprite, to be put on disk.
    /// </summary>
    public int TextureCount { get; set; }

    /// <summary>
    /// Number of collision masks for the sprite, to be put on disk.
    /// </summary>
    public int MaskCount { get; set; }

    /// <summary>
    /// Extra properties, optionally defined.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public ExtraProperties Extra { get; set; }

    /// <summary>
    /// Sequence contained within the sprite, optionally defined.
    /// </summary>
    public SerializableSequence Sequence { get; set; }

    /// <summary>
    /// Nine-slice properties, optionally defined.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public NineSliceProperties NineSlice { get; set; }

    /// <summary>
    /// Extra properties attached to sprites, but not always present in older versions of GameMaker.
    /// </summary>
    public struct ExtraProperties()
    {
        /// <inheritdoc cref="UndertaleSprite.SVersion"/>
        public uint ExtraVersion { get; set; }

        /// <inheritdoc cref="UndertaleSprite.SSpriteType"/>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ExtraSpriteType SpriteType { get; set; }

        /// <inheritdoc cref="UndertaleSprite.GMS2PlaybackSpeed"/>
        public float PlaybackSpeed { get; set; }

        /// <inheritdoc cref="UndertaleSprite.GMS2PlaybackSpeedType"/>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ExtraAnimSpeedType PlaybackSpeedType { get; set; }
    }

    /// <summary>
    /// Nine-slice properties attached to sprites, optionally.
    /// </summary>
    public struct NineSliceProperties()
    {
        /// <inheritdoc cref="UndertaleSprite.NineSlice.Enabled"/>
        public bool Enabled { get; set; }

        /// <inheritdoc cref="UndertaleSprite.NineSlice.Left"/>
        public int Left { get; set; }

        /// <inheritdoc cref="UndertaleSprite.NineSlice.Top"/>
        public int Top { get; set; }

        /// <inheritdoc cref="UndertaleSprite.NineSlice.Right"/>
        public int Right { get; set; }

        /// <inheritdoc cref="UndertaleSprite.NineSlice.Bottom"/>
        public int Bottom { get; set; }

        /// <inheritdoc cref="UndertaleSprite.NineSlice.TileModes"/>
        public List<TileMode> TileModes { get; set; }

        /// <inheritdoc cref="UndertaleSprite.NineSlice.TileMode"/>
        [JsonConverter(typeof(JsonStringEnumConverter<TileMode>))]
        public enum TileMode
        {
            Stretch = 0,
            Repeat = 1,
            Mirror = 2,
            BlankRepeat = 3,
            Hide = 4
        }
    }

    /// <inheritdoc/>
    [JsonIgnore]
    public SerializableAssetType AssetType => SerializableAssetType.Sprite;

    /// <inheritdoc/>
    [JsonIgnore]
    public bool IndividualDirectory => true;

    /// <inheritdoc/>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int OverrideOrder { get; set; }

    // Data asset that was located during pre-import.
    private UndertaleSprite _dataAsset = null;

    // Texture images created for the asset during texture import.
    private List<UndertaleTexturePageItem> _textureImages = null;

    // Mask blobs created for the asset during texture import.
    private List<byte[]> _maskData = null;

    /// <inheritdoc cref="UndertaleSprite.SepMaskType"/>
    public enum CollisionKinds : uint
    {
        AxisAlignedRect = 0,
        Precise = 1,
        RotatedRect = 2
    }

    /// <inheritdoc cref="UndertaleSprite.SpriteType"/>
    public enum ExtraSpriteType : uint
    {
        /// <inheritdoc cref="UndertaleSprite.SpriteType.Normal"/>
        Normal = 0,

        /// <inheritdoc cref="UndertaleSprite.SpriteType.SWF"/>
        SWF = 1,

        /// <inheritdoc cref="UndertaleSprite.SpriteType.Spine"/>
        Spine = 2,

        /// <inheritdoc cref="UndertaleSprite.SpriteType.Vector"/>
        Vector = 3
    }

    /// <inheritdoc cref="AnimSpeedType"/>
    public enum ExtraAnimSpeedType : uint
    {
        FramesPerSecond = 0,
        FramesPerGameFrame = 1
    }

    /// <summary>
    /// Populates this serializable sprite with data from an actual sprite.
    /// </summary>
    internal void PopulateFromData(ProjectContext projectContext, UndertaleSprite spr)
    {
        // Update all main properties
        DataName = spr.Name.Content;
        Width = spr.Width;
        Height = spr.Height;
        MarginLeft = spr.MarginLeft;
        MarginRight = spr.MarginRight;
        MarginBottom = spr.MarginBottom;
        MarginTop = spr.MarginTop;
        Transparent = spr.Transparent;
        Smooth = spr.Smooth;
        Preload = spr.Preload;
        BBoxMode = spr.BBoxMode;
        CollisionKind = (CollisionKinds)spr.SepMasks;
        OriginX = spr.OriginX;
        OriginY = spr.OriginY;
        TextureCount = spr.Textures.Count;
        MaskCount = spr.CollisionMasks.Count;

        // Update extra properties, if they exist
        if (spr.IsSpecialType)
        {
            Extra = new()
            {
                ExtraVersion = spr.SVersion,
                SpriteType = (ExtraSpriteType)spr.SSpriteType,
                PlaybackSpeed = spr.GMS2PlaybackSpeed,
                PlaybackSpeedType = (ExtraAnimSpeedType)spr.GMS2PlaybackSpeedType
            };
        }

        // Update sequence and nine slice properties, if they exist
        if (spr.SVersion >= 2 && spr.V2Sequence is UndertaleSequence sequence)
        {
            Sequence = (SerializableSequence)sequence.GenerateSerializableProjectAsset(projectContext);
        }
        if (spr.SVersion >= 3 && spr.V3NineSlice is UndertaleSprite.NineSlice nineSlice)
        {
            NineSlice = new()
            {
                Enabled = nineSlice.Enabled,
                Left = nineSlice.Left,
                Top = nineSlice.Top,
                Right = nineSlice.Right,
                Bottom = nineSlice.Bottom,
                TileModes = [.. nineSlice.TileModes.Select((mode) => (NineSliceProperties.TileMode)mode)]
            };
        }

        _dataAsset = spr;
    }

    /// <inheritdoc/>
    public void Serialize(ProjectContext projectContext, string destinationFile)
    {
        // Write main JSON
        using FileStream fs = new(destinationFile, FileMode.Create);
        JsonSerializer.Serialize<ISerializableProjectAsset>(fs, this, ProjectContext.JsonOptions);

        // Write images and masks as PNGs
        string directory = Path.GetDirectoryName(destinationFile);
        string baseFilename = Path.GetFileNameWithoutExtension(destinationFile);
        for (int i = 0; i < TextureCount; i++)
        {
            string filename = $"{baseFilename}_{i}.png";
            projectContext.TextureWorker.ExportAsPNG(_dataAsset.Textures[i].Texture, Path.Join(directory, filename), DataName, true);
        }
        (int maskWidth, int maskHeight) = _dataAsset.CalculateMaskDimensions(projectContext.Data);
        for (int i = 0; i < MaskCount; i++)
        {
            string filename = $"{baseFilename}_{i}.mask.png";
            TextureWorker.ExportCollisionMaskPNG(_dataAsset.CollisionMasks[i], Path.Join(directory, filename), maskWidth, maskHeight);
        }
    }

    /// <inheritdoc/>
    public void PreImport(ProjectContext projectContext)
    {
        if (projectContext.Data.Sprites.ByName(DataName) is UndertaleSprite existing)
        {
            // Sprite found
            _dataAsset = existing;
        }
        else
        {
            // No sprite found; create new one
            _dataAsset = new()
            {
                Name = projectContext.MakeString(DataName)
            };
            projectContext.Data.Sprites.Add(_dataAsset);
        }

        UndertaleSprite spr = _dataAsset;

        // Update all main properties
        spr.Width = Width;
        spr.Height = Height;
        spr.MarginLeft = MarginLeft;
        spr.MarginRight = MarginRight;
        spr.MarginBottom = MarginBottom;
        spr.MarginTop = MarginTop;
        spr.Transparent = Transparent;
        spr.Smooth = Smooth;
        spr.Preload = Preload;
        spr.BBoxMode = BBoxMode;
        spr.SepMasks = (UndertaleSprite.SepMaskType)CollisionKind;
        spr.OriginX = OriginX;
        spr.OriginY = OriginY;
    }

    /// <inheritdoc/>
    public IProjectAsset Import(ProjectContext projectContext)
    {
        UndertaleSprite spr = _dataAsset;

        // Update extra properties, if they exist
        if (!Extra.Equals(default(ExtraProperties)))
        {
            spr.IsSpecialType = true;
            spr.SVersion = Extra.ExtraVersion;
            spr.SSpriteType = (UndertaleSprite.SpriteType)Extra.SpriteType;
            spr.GMS2PlaybackSpeed = Extra.PlaybackSpeed;
            spr.GMS2PlaybackSpeedType = (AnimSpeedType)Extra.PlaybackSpeedType;
        }
        else
        {
            spr.IsSpecialType = false;
        }

        // Update sequence and nine slice properties, if they exist
        if (Extra.ExtraVersion >= 2)
        {
            spr.V2Sequence = Sequence?.ImportSubResource(projectContext);
        }
        if (Extra.ExtraVersion >= 3)
        {
            if (NineSlice.Equals(default(NineSliceProperties)))
            {
                spr.V3NineSlice = null;
            }
            else
            {
                spr.V3NineSlice = new()
                {
                    Enabled = NineSlice.Enabled,
                    Left = NineSlice.Left,
                    Top = NineSlice.Top,
                    Right = NineSlice.Right,
                    Bottom = NineSlice.Bottom,
                    TileModes = [.. NineSlice.TileModes.Select((mode) => (UndertaleSprite.NineSlice.TileMode)mode)]
                };
            }
        }

        // Assign texture images and collision masks
        spr.Textures.Clear();
        foreach (UndertaleTexturePageItem textureImage in _textureImages)
        {
            spr.Textures.Add(new() { Texture = textureImage });
        }
        spr.CollisionMasks.Clear();
        foreach (byte[] mask in _maskData)
        {
            UndertaleSprite.MaskEntry maskEntry = spr.NewMaskEntry(projectContext.Data);
            maskEntry.Data = mask;
            spr.CollisionMasks.Add(maskEntry);
        }

        return spr;
    }

    /// <inheritdoc/>
    public void ImportTextures(ProjectContext projectContext, TextureGroupPacker texturePacker)
    {
        // Get JSON filename (of main asset file)
        if (!projectContext.AssetDataNamesToPaths.TryGetValue((DataName, AssetType), out string jsonFilename))
        {
            throw new ProjectException("Failed to get background asset path");
        }

        // TODO: support loading other file types as well
        // TODO: support texture groups (or separate pages)

        string baseFilename = Path.GetFileNameWithoutExtension(jsonFilename);
        string directory = Path.GetDirectoryName(jsonFilename);

        // Load PNGs from disk, to be imported
        _textureImages = new(TextureCount);
        for (int i = 0; i < TextureCount; i++)
        {
            string filename = $"{baseFilename}_{i}.png";
            try
            {
                // Add image to packer
                _textureImages.Add(texturePacker.AddImage(TextureWorker.ReadBGRAImageFromFile(Path.Join(directory, filename)),
                                                          TextureGroupPacker.BorderFlags.Enabled));
            }
            catch (Exception e)
            {
                throw new ProjectException($"Failed to import sprite image file named \"{filename}\": {e.Message}", e);
            }
        }

        // Load collision masks from disk, to be imported
        (int maskWidth, int maskHeight) = _dataAsset.CalculateMaskDimensions(projectContext.Data);
        _maskData = new(MaskCount);
        for (int i = 0; i < MaskCount; i++)
        {
            string filename = $"{baseFilename}_{i}.mask.png";
            try
            {
                // Add image to packer
                _maskData.Add(TextureWorker.ReadMaskData(Path.Join(directory, filename), maskWidth, maskHeight));
            }
            catch (Exception e)
            {
                throw new ProjectException($"Failed to import sprite mask image file named \"{filename}\": {e.Message}", e);
            }
        }
    }
}
