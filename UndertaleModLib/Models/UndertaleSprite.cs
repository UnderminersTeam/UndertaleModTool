using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using UndertaleModLib.Util;

namespace UndertaleModLib.Models;

public enum AnimSpeedType : uint
{
    FramesPerSecond = 0,
    FramesPerGameFrame = 1
}

/// <summary>
/// Sprite entry in the data file.
/// </summary>
public class UndertaleSprite : UndertaleNamedResource, PrePaddedObject, INotifyPropertyChanged, IDisposable
{
    /// <summary>
    /// The name of the sprite.
    /// </summary>
    public UndertaleString Name { get; set; }

    /// <summary>
    /// The width of the sprite.
    /// </summary>
    public uint Width { get; set; }

    /// <summary>
    /// The height of the sprite.
    /// </summary>
    public uint Height { get; set; }

    /// <summary>
    /// The left margin of the sprite.
    /// </summary>
    public int MarginLeft { get; set; }

    /// <summary>
    /// The right margin of the sprite.
    /// </summary>
    public int MarginRight { get; set; }

    /// <summary>
    /// The bottom margin of the sprite.
    /// </summary>
    public int MarginBottom { get; set; }

    /// <summary>
    /// The top margin of the sprite.
    /// </summary>
    public int MarginTop { get; set; }

    /// <summary>
    /// Whether the sprite should be transparent.
    /// </summary>
    public bool Transparent { get; set; }

    /// <summary>
    /// Whether the sprite should get smoothed.
    /// </summary>
    public bool Smooth { get; set; }

    /// <summary>
    /// Whether the sprite should get preloaded.
    /// </summary>
    public bool Preload { get; set; }

    /// <summary>
    /// The bounding box mode of the sprite. TODO: double check if the possible values here are automatic, full image and manual
    /// </summary>
    public uint BBoxMode { get; set; }

    /// <summary>
    /// The separation mask type this sprite has.
    /// </summary>
    public SepMaskType SepMasks { get; set; }


    /// <summary>
    /// The x-coordinate of the origin of the sprite.
    /// </summary>
    public int OriginX { get; set; }

    /// <summary>
    /// The y-coordinate of the origin of the sprite.
    /// </summary>
    public int OriginY { get; set; }

    /// <summary>
    /// A <see cref="OriginX"/> wrapper that also sets <see cref="V2Sequence"/> accordingly.
    /// </summary>
    /// <remarks>
    /// This attribute is used only in UndertaleModTool and doesn't exist in GameMaker.
    /// </remarks>
    public int OriginXWrapper
    {
        get => OriginX;
        set
        {
            OriginX = value;

            if (IsSpecialType && SVersion > 1 && V2Sequence is not null)
                V2Sequence.OriginX = value;
        }
    }

    /// <summary>
    /// A <see cref="OriginY"/> wrapper that also sets <see cref="V2Sequence"/> accordingly.
    /// </summary>
    /// <remarks>
    /// This attribute is used only in UndertaleModTool and doesn't exist in GameMaker.
    /// </remarks>
    public int OriginYWrapper
    {
        get => OriginY;
        set
        {
            OriginY = value;

            if (IsSpecialType && SVersion > 1 && V2Sequence is not null)
                V2Sequence.OriginY = value;
        }
    }

    /// <summary>
    /// The frames of the sprite.
    /// </summary>
    public UndertaleSimpleList<TextureEntry> Textures { get; set; } = new UndertaleSimpleList<TextureEntry>();

    /// <summary>
    /// The collision masks of the sprite.
    /// </summary>
    public ObservableCollection<MaskEntry> CollisionMasks { get; set; } = new ObservableCollection<MaskEntry>();

    // Special sprite types (always used in GMS2)
    public uint SVersion { get; set; } = 1;
    public SpriteType SSpriteType { get; set; }
    public float GMS2PlaybackSpeed { get; set; } = 15.0f;
    public AnimSpeedType GMS2PlaybackSpeedType { get; set; } = 0;
    public bool IsSpecialType { get; set; } = false;

    public int SpineVersion { get; set; }
    public int SpineCacheVersion { get; set; }
    public string SpineJSON { get; set; }
    public string SpineAtlas { get; set; }
    public UndertaleSimpleList<UndertaleSpineTextureEntry> SpineTextures { get; set; }

    public bool IsSpineSprite { get => SpineJSON != null && SpineAtlas != null && SpineTextures != null; }
    public bool SpineHasTextureData { get; set; } = true;
    public bool IsYYSWFSprite { get => YYSWF != null; }

    private int _SWFVersion;
    private UndertaleYYSWF _YYSWF;

    public int SWFVersion { get => _SWFVersion; set { _SWFVersion = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SWFVersion))); } }
    public UndertaleYYSWF YYSWF { get => _YYSWF; set { _YYSWF = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(YYSWF))); } }

    public int VectorVersion { get; set; }
    public UndertaleSimpleList<UndertaleVectorShapeData> VectorShapes { get; set; }
    public int VectorCollisionMaskWidth { get; set; }
    public int VectorCollisionMaskHeight { get; set; }
    public UndertaleObservableList<byte[]> VectorCollisionMaskRLEData { get; set; }
    public UndertaleObservableList<int> VectorFrameToShapeMap { get; set; }

    public UndertaleSequence V2Sequence;

    public NineSlice V3NineSlice;

    /// <inheritdoc />
    public event PropertyChangedEventHandler PropertyChanged;

    /// <inheritdoc />
    public override string ToString()
    {
        return Name?.Content + " (" + GetType().Name + ")";
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);

        foreach (TextureEntry entry in Textures)
            entry?.Dispose();
        foreach (MaskEntry entry in CollisionMasks)
            entry?.Dispose();
        if (SpineTextures is not null)
        {
            foreach (var spineEntry in SpineTextures)
                spineEntry?.Dispose();
        }

        Textures = new();
        CollisionMasks.Clear();
        SpineTextures = null;
        _YYSWF = null;
        V2Sequence = null;
        V3NineSlice = null;
        // FIXME: verify correctness of this
        VectorShapes = new();
        VectorCollisionMaskRLEData = null;
        VectorFrameToShapeMap = new();
    }

    /// <summary>
    /// Creates a new mask entry for this sprite.
    /// </summary>
    /// <param name="data">Data that this sprite is part of, for checking the GameMaker version.</param>
    /// <returns>The new mask entry.</returns>
    public MaskEntry NewMaskEntry(UndertaleData data = null)
    {
        int width, height;
        if (data is not null)
        {
            // Support for 2024.6+ (modern code path)
            (width, height) = CalculateMaskDimensions(data);
        }
        else
        {
            // Legacy code path (for scripts that haven't been updated to support 2024.6+ yet)
            (width, height) = ((int)Width, (int)Height);
        }

        uint len = (uint)((width + 7) / 8 * height);
        return new MaskEntry(new byte[len], width, height);
    }

    /// <summary>
    /// Different formats a sprite can have.
    /// </summary>
    public enum SpriteType : uint
    {
        /// <summary>
        /// Normal format.
        /// </summary>
        Normal = 0,
        /// <summary>
        /// SWF format.
        /// </summary>
        SWF = 1,
        /// <summary>
        /// Spine format.
        /// </summary>
        Spine = 2,
        /// <summary>
        /// Vector format (e.g., SVGs).
        /// </summary>
        Vector = 3
    }

    /// <summary>
    /// Different Separation mask types a sprite can have.
    /// </summary>
    public enum SepMaskType : uint
    {
        AxisAlignedRect = 0,
        Precise = 1,
        RotatedRect = 2
    }

    [PropertyChanged.AddINotifyPropertyChangedInterface]
    public class TextureEntry : UndertaleObject, IStaticChildObjectsSize, IDisposable
    {
        /// <inheritdoc cref="IStaticChildObjectsSize.ChildObjectsSize" />
        public static readonly uint ChildObjectsSize = 4;

        public UndertaleTexturePageItem Texture { get; set; }

        /// <inheritdoc />
        public void Serialize(UndertaleWriter writer)
        {
            writer.WriteUndertaleObjectPointer(Texture);
        }

        /// <inheritdoc />
        public void Unserialize(UndertaleReader reader)
        {
            Texture = reader.ReadUndertaleObjectPointer<UndertaleTexturePageItem>();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            GC.SuppressFinalize(this);

            Texture = null;
        }
    }

    [PropertyChanged.AddINotifyPropertyChangedInterface]
    public class MaskEntry : IDisposable
    {
        public byte[] Data { get; set; }

        /// <summary>
        /// Width of this sprite mask. UTMT only.
        /// </summary>
        public int Width { get; set; }
        /// <summary>
        /// Height of this sprite mask. UTMT only.
        /// </summary>
        public int Height { get; set; }

        public MaskEntry()
        {
        }

        public MaskEntry(byte[] data, int width, int height)
        {
            this.Data = data;
            this.Width = width;
            this.Height = height;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            GC.SuppressFinalize(this);

            Data = null;
        }
    }

    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        writer.WriteUndertaleString(Name);
        writer.Write(Width);
        writer.Write(Height);
        writer.Write(MarginLeft);
        writer.Write(MarginRight);
        writer.Write(MarginBottom);
        writer.Write(MarginTop);
        writer.Write(Transparent);
        writer.Write(Smooth);
        writer.Write(Preload);
        writer.Write(BBoxMode);
        writer.Write((uint)SepMasks);
        writer.Write(OriginX);
        writer.Write(OriginY);
        if (IsSpecialType)
        {
            uint sequencePatchPos = 0;
            uint nineSlicePatchPos = 0;

            writer.Write(-1);
            writer.Write(SVersion);
            writer.Write((uint)SSpriteType);
            if (writer.undertaleData.GeneralInfo?.Major >= 2)
            {
                writer.Write(GMS2PlaybackSpeed);
                writer.Write((uint)GMS2PlaybackSpeedType);
                if (SVersion >= 2)
                {
                    sequencePatchPos = writer.Position;
                    writer.Write(0);
                    if (SVersion >= 3)
                    {
                        nineSlicePatchPos = writer.Position;
                        writer.Write(0);
                    }
                }
            }

            switch (SSpriteType)
            {
                case SpriteType.Normal:
                    writer.WriteUndertaleObject(Textures);
                    WriteMaskData(writer);
                    break;
                case SpriteType.SWF:
                    writer.Write(SWFVersion);
                    if (SWFVersion == 8) writer.WriteUndertaleObject(Textures);
                    writer.WriteUndertaleObject(YYSWF);
                    break;
                case SpriteType.Spine:
                    writer.Align(4);

                    byte[] encodedJson = EncodeSpineBlob(Encoding.UTF8.GetBytes(SpineJSON));
                    byte[] encodedAtlas = EncodeSpineBlob(Encoding.UTF8.GetBytes(SpineAtlas));

                    if (writer.undertaleData.IsVersionAtLeast(2023, 1))
                        writer.WriteUndertaleObject(Textures);

                    // the header.
                    writer.Write(SpineVersion);
                    if (SpineVersion >= 3) writer.Write(SpineCacheVersion);
                    writer.Write(encodedJson.Length);
                    writer.Write(encodedAtlas.Length);

                    switch (SpineVersion)
                    {
                        case 1:
                        {
                            UndertaleSpineTextureEntry atlas = SpineTextures.First(); // will throw an exception if the list is null, what I want!
                            writer.Write(atlas.TexBlob.Length);
                            writer.Write(atlas.PageWidth);
                            writer.Write(atlas.PageHeight);

                            // the data.
                            writer.Write(encodedJson);
                            writer.Write(encodedAtlas);

                            // the one and only atlas.
                            writer.Write(atlas.TexBlob);

                            break;
                        }
                        case 2:
                        case 3:
                        {
                            writer.Write(SpineTextures.Count);

                            // the data.
                            writer.Write(encodedJson);
                            writer.Write(encodedAtlas);

                            // the length is stored in the header, so we can't use the list's method.
                            foreach (var tex in SpineTextures)
                            {
                                writer.WriteUndertaleObject(tex);
                            }

                            break;
                        }
                    }

                    break;
                case SpriteType.Vector:
                    writer.Write(VectorVersion);

                    writer.WriteUndertaleObject(Textures);

                    if (VectorVersion >= 4)
                    {
                        writer.Write(VectorFrameToShapeMap.Count);
                        foreach (var n in VectorFrameToShapeMap)
                        {
                            writer.Write(n);
                        }
                        writer.WriteUndertaleObject(VectorShapes);
                    }
                    else
                    {
                        DebugUtil.Assert(VectorShapes.Count == 1, "There must be only 1 vector collision mask before vector version 4");
                        writer.WriteUndertaleObject(VectorShapes[0]);
                    }

                    writer.Write(VectorCollisionMaskRLEData.Count);
                    if (VectorCollisionMaskRLEData.Count > 0)
                    {
                        writer.Write(VectorCollisionMaskWidth);
                        writer.Write(VectorCollisionMaskHeight);
                        if (VectorVersion <= 3)
                        {
                            DebugUtil.Assert(VectorCollisionMaskRLEData.Count == 1, "There must be only 1 vector collision mask before vector version 4");
                        }
                        foreach (var data in VectorCollisionMaskRLEData)
                        {
                            writer.Write(data.Length);
                            writer.Write(data);
                            writer.Align(4);
                        }
                    }
                    else
                    {
                        writer.Write(0);
                        writer.Write(0);
                        writer.Write(0);
                    }

                    break;
            }

            // Sequence + nine slice
            if (sequencePatchPos != 0 && V2Sequence != null) // Normal compiler also checks for sprite type to be normal, but whatever!
            {
                uint returnTo = writer.Position;
                writer.Position = sequencePatchPos;
                writer.Write(returnTo);
                writer.Position = returnTo;
                writer.Write(1);
                writer.WriteUndertaleObject(V2Sequence);
            }
            if (nineSlicePatchPos != 0 && V3NineSlice != null)
            {
                uint returnTo = writer.Position;
                writer.Position = nineSlicePatchPos;
                writer.Write(returnTo);
                writer.Position = returnTo;
                writer.WriteUndertaleObject(V3NineSlice);
            }
        }
        else
        {
            writer.WriteUndertaleObject(Textures);
            WriteMaskData(writer);
        }
    }

    private void WriteMaskData(UndertaleWriter writer)
    {
        writer.Write((uint)CollisionMasks.Count);
        uint total = 0;
        foreach (var mask in CollisionMasks)
        {
            writer.Write(mask.Data);
            total += (uint)mask.Data.Length;
        }

        while (total % 4 != 0)
        {
            writer.Write((byte)0);
            total++;
        }

        (int width, int height) = CalculateMaskDimensions(writer.undertaleData);
        Util.DebugUtil.Assert(total == CalculateMaskDataSize(width, height, (uint)CollisionMasks.Count), "Invalid mask data for sprite");
    }

    private static byte[] DecodeSpineBlob(byte[] blob)
    {
        // don't ask.
        uint k = 42;
        for (int i = 0; i < blob.Length; i++)
        {
            blob[i] -= (byte)k;
            k *= k + 1;
        }
        return blob;
    }

    private static byte[] EncodeSpineBlob(byte[] blob)
    {
        // don't ask.
        uint k = 42;
        for (int i = 0; i < blob.Length; i++)
        {
            blob[i] += (byte)k;
            k *= k + 1;
        }
        return blob;
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        Name = reader.ReadUndertaleString();
        Width = reader.ReadUInt32();
        Height = reader.ReadUInt32();
        MarginLeft = reader.ReadInt32();
        MarginRight = reader.ReadInt32();
        MarginBottom = reader.ReadInt32();
        MarginTop = reader.ReadInt32();
        Transparent = reader.ReadBoolean();
        Smooth = reader.ReadBoolean();
        Preload = reader.ReadBoolean();
        BBoxMode = reader.ReadUInt32();
        SepMasks = (SepMaskType)reader.ReadUInt32();
        OriginX = reader.ReadInt32();
        OriginY = reader.ReadInt32();
        if (reader.ReadInt32() == -1) // technically this seems to be able to occur on older versions, for special sprite types
        {
            int sequenceOffset = 0;
            int nineSliceOffset = 0;

            IsSpecialType = true;
            SVersion = reader.ReadUInt32();
            SSpriteType = (SpriteType)reader.ReadUInt32();
            if (reader.undertaleData.IsGameMaker2())
            {
                GMS2PlaybackSpeed = reader.ReadSingle();
                GMS2PlaybackSpeedType = (AnimSpeedType)reader.ReadUInt32();
                if (SVersion >= 2)
                {
                    sequenceOffset = reader.ReadInt32();
                    if (SVersion >= 3)
                    {
                        if (!reader.undertaleData.IsVersionAtLeast(2, 3, 2))
                            reader.undertaleData.SetGMS2Version(2, 3, 2);
                        nineSliceOffset = reader.ReadInt32();
                    }
                }
            }

            switch (SSpriteType)
            {
                case SpriteType.Normal:
                    Textures = reader.ReadUndertaleObject<UndertaleSimpleList<TextureEntry>>();
                    ReadMaskData(reader);
                    break;
                case SpriteType.SWF:
                {
                    //// TODO: This code does not work all the time for some reason. ////

                    SWFVersion = reader.ReadInt32();
                    Util.DebugUtil.Assert(SWFVersion == 8 || SWFVersion == 7, "Invalid SWF sprite format, expected 7 or 8, got " + SWFVersion);

                    if (SWFVersion == 8)
                    {
                        Textures = reader.ReadUndertaleObject<UndertaleSimpleList<TextureEntry>>();
                    }

                    YYSWF = reader.ReadUndertaleObjectNoPool<UndertaleYYSWF>();
                }
                    break;
                case SpriteType.Spine:
                {
                    reader.Align(4);

                    if (reader.undertaleData.IsVersionAtLeast(2023, 1))
                    {
                        Textures = reader.ReadUndertaleObject<UndertaleSimpleList<TextureEntry>>();
                        SpineHasTextureData = false;
                    }

                    SpineVersion = reader.ReadInt32();
                    if (SpineVersion >= 3)
                    {
                        SpineCacheVersion = reader.ReadInt32();
                        Util.DebugUtil.Assert(SpineCacheVersion == 1, "Invalid Spine cache format version number, expected 1, got " + SpineCacheVersion);
                    }
                    Util.DebugUtil.Assert(SpineVersion <= 3 && SpineVersion >= 1,
                                          "Invalid Spine format version number, expected 3, 2 or 1, got " + SpineVersion);
                    int jsonLength = reader.ReadInt32();
                    int atlasLength = reader.ReadInt32();
                    int textures = reader.ReadInt32(); // count in v2(and newer) and size in bytes in v1.
                    SpineTextures = new UndertaleSimpleList<UndertaleSpineTextureEntry>();

                    switch (SpineVersion)
                    {
                        // Version 1 - only one single PNG atlas.
                        // Version 2 - can be multiple atlases.
                        // Version 3 - an atlas can be a QOI blob.
                        case 1:
                        {
                            UndertaleSpineTextureEntry atlas = new UndertaleSpineTextureEntry();
                            int atlasWidth = reader.ReadInt32();
                            int atlasHeight = reader.ReadInt32();
                            SpineJSON = Encoding.UTF8.GetString(DecodeSpineBlob(reader.ReadBytes(jsonLength)));
                            SpineAtlas = Encoding.UTF8.GetString(DecodeSpineBlob(reader.ReadBytes(atlasLength)));

                            atlas.PageWidth = atlasWidth;
                            atlas.PageHeight = atlasHeight;
                            atlas.TexBlob = reader.ReadBytes(textures);
                            SpineTextures.InternalAdd(atlas);
                            break;
                        }
                        case 2:
                        case 3:
                        {
                            SpineJSON = Encoding.UTF8.GetString(DecodeSpineBlob(reader.ReadBytes(jsonLength)));
                            SpineAtlas = Encoding.UTF8.GetString(DecodeSpineBlob(reader.ReadBytes(atlasLength)));

                            SpineTextures.SetCapacity(textures);

                            // the length is stored before json and atlases so we can't use ReadUndertaleObjectList
                            // same goes for serialization.
                            for (int t = 0; t < textures; t++)
                            {
                                SpineTextures.InternalAdd(reader.ReadUndertaleObject<UndertaleSpineTextureEntry>());
                            }

                            break;
                        }
                    }
                }
                    break;
                case SpriteType.Vector:
                {
                    VectorVersion = reader.ReadInt32();
                    DebugUtil.Assert(VectorVersion == 3 || VectorVersion == 4, "Invalid vector format version number, expected 3 or 4, got " + VectorVersion);
                    if (VectorVersion >= 4 && !reader.undertaleData.IsVersionAtLeast(2024, 14))
                        reader.undertaleData.SetGMS2Version(2024, 14);

                    Textures = reader.ReadUndertaleObject<UndertaleSimpleList<TextureEntry>>();

                    if (VectorVersion >= 4)
                    {
                        VectorFrameToShapeMap = new(reader.ReadInt32());
                        for (var i = 0; i < VectorFrameToShapeMap.Count; i++)
                        {
                            VectorFrameToShapeMap.InternalAdd(reader.ReadInt32());
                        }

                        VectorShapes = reader.ReadUndertaleObjectNoPool<UndertaleSimpleList<UndertaleVectorShapeData>>();
                    }
                    else
                    {
                        VectorFrameToShapeMap = new(){ 0 };
                        VectorShapes = new() { reader.ReadUndertaleObjectNoPool<UndertaleVectorShapeData>() };
                    }
                         
                    int collisionMaskCount = VectorVersion >= 4 ? reader.ReadInt32() : (reader.ReadBoolean() ? 1 : 0);
                    VectorCollisionMaskWidth = reader.ReadInt32();
                    VectorCollisionMaskHeight = reader.ReadInt32();
                    VectorCollisionMaskRLEData = new();
                    if (collisionMaskCount > 0)
                    {
                        VectorCollisionMaskRLEData.SetCapacity(collisionMaskCount);
                        for (var i = 0; i < VectorCollisionMaskRLEData.Count; i++)
                        {
                            int dataLength = reader.ReadInt32();
                            VectorCollisionMaskRLEData.Add(reader.ReadBytes(dataLength));
                            reader.Align(4);
                        }
                    }

                    break;
                }
            }

            if (sequenceOffset != 0)
            {
                if (reader.ReadInt32() != 1)
                    throw new UndertaleSerializationException("Sequence data unserialization error - expected 1");
                V2Sequence = reader.ReadUndertaleObject<UndertaleSequence>();
            }

            if (nineSliceOffset != 0)
            {
                V3NineSlice = reader.ReadUndertaleObject<NineSlice>();
            }
        }
        else
        {
            reader.Position -= 4;
            Textures = reader.ReadUndertaleObject<UndertaleSimpleList<TextureEntry>>();
            ReadMaskData(reader);
        }
    }

    /// <inheritdoc cref="UndertaleObject.UnserializeChildObjectCount(UndertaleReader)"/>
    public static uint UnserializeChildObjectCount(UndertaleReader reader)
    {
        reader.Align(4);

        uint count = 0;

        reader.Position += 4; // "Name"
        uint width = reader.ReadUInt32();
        uint height = reader.ReadUInt32();
        int marginLeft = reader.ReadInt32();
        int marginRight = reader.ReadInt32();
        int marginBottom = reader.ReadInt32();
        int marginTop = reader.ReadInt32();

        reader.Position += 28;

        if (reader.ReadInt32() == -1)
        {
            int sequenceOffset = 0;
            int nineSliceOffset = 0;

            uint sVersion = reader.ReadUInt32();
            SpriteType sSpriteType = (SpriteType)reader.ReadUInt32();
            if (reader.undertaleData.IsGameMaker2())
            {
                reader.Position += 8; // playback speed values

                if (sVersion >= 2)
                {
                    sequenceOffset = reader.ReadInt32();
                    if (sVersion >= 3)
                    {
                        if (!reader.undertaleData.IsVersionAtLeast(2, 3, 2))
                            reader.undertaleData.SetGMS2Version(2, 3, 2);
                        nineSliceOffset = reader.ReadInt32();
                    }
                }
            }

            switch (sSpriteType)
            {
                case SpriteType.Normal:
                    count += 1 + UndertaleSimpleList<TextureEntry>.UnserializeChildObjectCount(reader);
                    SkipMaskData(reader, (int)width, (int)height, marginRight, marginLeft, marginBottom, marginTop);
                    break;

                case SpriteType.SWF:
                    int swfVersion = reader.ReadInt32();
                    if (swfVersion == 8)
                        count += 1 + UndertaleSimpleList<TextureEntry>.UnserializeChildObjectCount(reader);

                    // "YYSWF" classes are not in the pool
                    return count;

                case SpriteType.Spine:
                {
                    reader.Align(4);

                    if (reader.undertaleData.IsVersionAtLeast(2023, 1))
                        count += 1 + UndertaleSimpleList<TextureEntry>.UnserializeChildObjectCount(reader);

                    int spineVersion = reader.ReadInt32();
                    if (spineVersion >= 3)
                        reader.Position += 4; // "SpineCacheVersion"
                    Util.DebugUtil.Assert(spineVersion <= 3 && spineVersion >= 1,
                                          "Invalid Spine format version number, expected 3, 2 or 1, got " + spineVersion);

                    int jsonLength = reader.ReadInt32();
                    int atlasLength = reader.ReadInt32();
                    int textures = reader.ReadInt32();

                    switch (spineVersion)
                    {
                        case 1:
                            reader.Position += 8 + (uint)jsonLength + (uint)atlasLength + (uint)textures;
                            break;

                        case 2:
                        case 3:
                        {
                            reader.Position += (uint)jsonLength + (uint)atlasLength;

                            // TODO: make this return count instead if spine sprite
                            // couldn't have sequence or nine slices data.
                            for (int i = 0; i < textures; i++)
                                UndertaleSpineTextureEntry.UnserializeChildObjectCount(reader);

                            count += (uint)textures;
                        }
                            break;
                    }
                }
                    break;

                case SpriteType.Vector:
                    reader.Position += 4; // skip version
                    count += 1 + UndertaleSimpleList<TextureEntry>.UnserializeChildObjectCount(reader);
                    break;
            }

            if (sequenceOffset != 0)
            {
                if (reader.ReadInt32() != 1)
                    throw new UndertaleSerializationException($"Sequence data count unserialization error - expected 1");
                count += 1 + UndertaleSequence.UnserializeChildObjectCount(reader);
            }

            if (nineSliceOffset != 0)
            {
                reader.Position += NineSlice.ChildObjectsSize;
                count++;
            }
        }
        else
        {
            reader.Position -= 4;
            count += 1 + UndertaleSimpleList<TextureEntry>.UnserializeChildObjectCount(reader);
            SkipMaskData(reader, (int)width, (int)height, marginRight, marginLeft, marginBottom, marginTop);
        }

        return count;
    }

    /// <summary>
    /// Returns the width and height of the collision mask for this sprite, which changes depending on GameMaker version.
    /// </summary>
    public (int Width, int Height) CalculateMaskDimensions(UndertaleData data)
    {
        if (data.IsVersionAtLeast(2024, 6))
        {
            return CalculateBboxMaskDimensions(MarginRight, MarginLeft, MarginBottom, MarginTop);
        }
        return CalculateFullMaskDimensions((int)Width, (int)Height);
    }

    /// <summary>
    /// Calculates the width and height of a collision mask from the given margin/bounding box.
    /// This method is used to calculate collision mask dimensions in GameMaker 2024.6 and above.
    /// </summary>
    public static (int Width, int Height) CalculateBboxMaskDimensions(int marginRight, int marginLeft, int marginBottom, int marginTop)
    {
        return (marginRight - marginLeft + 1, marginBottom - marginTop + 1);
    }

    /// <summary>
    /// Calculates the width and height of a collision mask from a given sprite's full width and height.
    /// This method is used to calculate collision mask dimensions prior to GameMaker 2024.6.
    /// </summary>
    /// <remarks>
    /// This simply returns the width and height supplied, but is intended for clarity in the code.
    /// </remarks>
    public static (int Width, int Height) CalculateFullMaskDimensions(int width, int height)
    {
        return (width, height);
    }

    private void ReadMaskData(UndertaleReader reader)
    {
        // Initialize mask list
        uint maskCount = reader.ReadUInt32();
        List<MaskEntry> newMasks = new((int)maskCount);

        // Read in mask data
        (int width, int height) = CalculateMaskDimensions(reader.undertaleData);
        uint len = (uint)((width + 7) / 8 * height);
        uint total = 0;
        for (uint i = 0; i < maskCount; i++)
        {
            newMasks.Add(new MaskEntry(reader.ReadBytes((int)len), width, height));
            total += len;
        }

        while ((total % 4) != 0)
        {
            if (reader.ReadByte() != 0)
            {
                throw new IOException("Mask padding");
            }
            total++;
        }
        if (total != CalculateMaskDataSize(width, height, maskCount))
        {
            throw new IOException("Mask data size incorrect");
        }

        // Assign masks to sprite
        CollisionMasks = new(newMasks);
    }

    private static void SkipMaskData(UndertaleReader reader, int width, int height, int marginRight, int marginLeft, int marginBottom, int marginTop)
    {
        uint maskCount = reader.ReadUInt32();
        if (reader.undertaleData.IsVersionAtLeast(2024, 6))
        {
            (width, height) = CalculateBboxMaskDimensions(marginRight, marginLeft, marginBottom, marginTop);
        }
        else
        {
            (width, height) = CalculateFullMaskDimensions(width, height);
        }
        uint len = (uint)((width + 7) / 8 * height);

        uint total = 0;
        for (uint i = 0; i < maskCount; i++)
        {
            reader.Position += len; // "new MaskEntry()"
            total += len;
        }

        // Skip padding
        int skipSize = 0;
        while (total % 4 != 0)
        {
            skipSize++;
            total++;
        }
        reader.Position += skipSize;
    }

    private uint CalculateMaskDataSize(int width, int height, uint maskCount)
    {
        uint roundedWidth = (uint)((width + 7) / 8 * 8); // round to multiple of 8
        uint dataBits = (uint)(roundedWidth * height * maskCount);
        uint dataBytes = ((dataBits + 31) / 32 * 32) / 8; // round to multiple of 4 bytes
        return dataBytes;
    }

    /// <inheritdoc />
    public void SerializePrePadding(UndertaleWriter writer)
    {
        writer.Align(4);
    }

    /// <inheritdoc />
    public void UnserializePrePadding(UndertaleReader reader)
    {
        // If you are modifying this, you must also modify "UnserializeChildObjectCount()"
        reader.Align(4);
    }

    [PropertyChanged.AddINotifyPropertyChangedInterface]
    public class NineSlice : UndertaleObject, IStaticChildObjectsSize
    {
        /// <inheritdoc cref="IStaticChildObjectsSize.ChildObjectsSize" />
        public static readonly uint ChildObjectsSize = 40;

        public int Left { get; set; }
        public int Top { get; set; }
        public int Right { get; set; }
        public int Bottom { get; set; }
        public bool Enabled { get; set; }
        public TileMode[] TileModes { get; set; } = new TileMode[5];

        public enum TileMode
        {
            Stretch = 0,
            Repeat = 1,
            Mirror = 2,
            BlankRepeat = 3,
            Hide = 4
        }

        /// <inheritdoc />
        public void Serialize(UndertaleWriter writer)
        {
            writer.Write(Left);
            writer.Write(Top);
            writer.Write(Right);
            writer.Write(Bottom);
            writer.Write(Enabled);
            for (int i = 0; i < 5; i++)
                writer.Write((int)TileModes[i]);
        }

        /// <inheritdoc />
        public void Unserialize(UndertaleReader reader)
        {
            Left = reader.ReadInt32();
            Top = reader.ReadInt32();
            Right = reader.ReadInt32();
            Bottom = reader.ReadInt32();
            Enabled = reader.ReadBoolean();
            for (int i = 0; i < 5; i++)
                TileModes[i] = (TileMode)reader.ReadInt32();
        }
    }
}

[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertaleSpineTextureEntry : UndertaleObject, IDisposable
{
    /// <summary>
    /// The width of the Spine atlas in pixels.
    /// </summary>
    public int PageWidth { get; set; }

    /// <summary>
    /// The height of the Spine atlas in pixels.
    /// </summary>
    public int PageHeight { get; set; }

    /// <summary>
    /// The atlas as raw bytes, can be a GameMaker QOI texture or a PNG file. Null for versions >= 2023.1.
    /// </summary>
    public byte[] TexBlob { get; set; }

    /// <summary>
    /// The length of the corresponding sprite texture entry for versions >= 2023.1.
    /// </summary>
    public int TextureEntryLength { get; set; }

    /// <summary>
    /// Indicates whether <see cref="TexBlob"/> contains a GameMaker QOI texture (the header is qoif reversed).
    /// </summary>
    public bool IsQOI => TexBlob != null && TexBlob.Length > 7 && TexBlob[0] == 102/*f*/ && TexBlob[1] == 105/*i*/ && TexBlob[2] == 111/*o*/ && TexBlob[3] == 113/*q*/;

    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        writer.Write(PageWidth);
        writer.Write(PageHeight);
        if (writer.undertaleData.IsVersionAtLeast(2023, 1))
        {
            writer.Write(TextureEntryLength);
        }
        else
        {
            writer.Write(TexBlob.Length);
            writer.Write(TexBlob);
        }
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        PageWidth = reader.ReadInt32();
        PageHeight = reader.ReadInt32();
        if (reader.undertaleData.IsVersionAtLeast(2023, 1))
            TextureEntryLength = reader.ReadInt32();
        else
            TexBlob = reader.ReadBytes(reader.ReadInt32());
    }

    /// <inheritdoc cref="UndertaleObject.UnserializeChildObjectCount(UndertaleReader)"/>
    public static uint UnserializeChildObjectCount(UndertaleReader reader)
    {
        reader.Position += 8;                        // Size
        if (reader.undertaleData.IsVersionAtLeast(2023, 1))
            reader.Position += 4; // "TextureEntryLength"
        else
            reader.Position += (uint)reader.ReadInt32(); // "TexBlob"

        return 0;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"UndertaleSpineTextureEntry ({PageWidth};{PageHeight})";
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);

        TexBlob = null;
    }
}

// TODO: make all these classes "IDisposable"

[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertaleYYSWFMatrixColor : UndertaleObject
{
    private const int MATRIX_SIZE = 4;

    public int[] Additive { get; set; }
    public int[] Multiply { get; set; }

    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        foreach (int v in Additive)
            writer.Write(v);
        foreach (int v in Multiply)
            writer.Write(v);
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        Additive = new int[MATRIX_SIZE];
        Multiply = new int[MATRIX_SIZE];
        for (int i = 0; i < Additive.Length; i++)
            Additive[i] = reader.ReadInt32();
        for (int i = 0; i < Multiply.Length; i++)
            Multiply[i] = reader.ReadInt32();
    }
}

[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertaleVectorMatrix33 : UndertaleObject
{
    private const int MATRIX_SIZE = 9;
    public float[] Values { get; set; }

    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        foreach (float v in Values)
            writer.Write(v);
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        Values = new float[MATRIX_SIZE];
        for (int i = 0; i < Values.Length; i++)
            Values[i] = reader.ReadSingle();
    }
}

[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertaleYYSWFTimelineObject : UndertaleObject
{
    public int CharID { get; set; }
    public int CharIndex { get; set; }
    public int Depth { get; set; }
    public int ClippingDepth { get; set; }
    public UndertaleVectorMatrix33 TransformationMatrix { get; set; }
    public UndertaleYYSWFMatrixColor ColorMatrix { get; set; }
    public float MinX { get; set; }
    public float MaxX { get; set; }
    public float MinY { get; set; }
    public float MaxY { get; set; }

    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        writer.Write(CharID);
        writer.Write(CharIndex);
        writer.Write(Depth);
        writer.Write(ClippingDepth);
        writer.WriteUndertaleObject(ColorMatrix);
        writer.Write(MinX);
        writer.Write(MaxX);
        writer.Write(MinY);
        writer.Write(MaxY);
        writer.WriteUndertaleObject(TransformationMatrix);
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        CharID = reader.ReadInt32();
        CharIndex = reader.ReadInt32();
        Depth = reader.ReadInt32();
        ClippingDepth = reader.ReadInt32();
        ColorMatrix = reader.ReadUndertaleObjectNoPool<UndertaleYYSWFMatrixColor>();
        MinX = reader.ReadSingle();
        MaxX = reader.ReadSingle();
        MinY = reader.ReadSingle();
        MaxY = reader.ReadSingle();
        TransformationMatrix = reader.ReadUndertaleObjectNoPool<UndertaleVectorMatrix33>();
    }
}

[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertaleYYSWFTimelineFrame : UndertaleObject
{
    public UndertaleObservableList<UndertaleYYSWFTimelineObject> FrameObjects { get; set; }
    public float MinX { get; set; }
    public float MaxX { get; set; }
    public float MinY { get; set; }
    public float MaxY { get; set; }

    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        writer.Write(FrameObjects.Count);
        writer.Write(MinX);
        writer.Write(MaxX);
        writer.Write(MinY);
        writer.Write(MaxY);
        foreach (var frameObject in FrameObjects)
        {
            writer.WriteUndertaleObject(frameObject);
        }
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        int ii = reader.ReadInt32();
        MinX = reader.ReadSingle();
        MaxX = reader.ReadSingle();
        MinY = reader.ReadSingle();
        MaxY = reader.ReadSingle();
        FrameObjects = new UndertaleObservableList<UndertaleYYSWFTimelineObject>(ii);
        for (int i = 0; i < ii; i++)
        {
            FrameObjects.InternalAdd(reader.ReadUndertaleObjectNoPool<UndertaleYYSWFTimelineObject>());
        }
    }
}

[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertaleYYSWFCollisionMask : UndertaleObject
{
    public byte[] RLEData { get; set; } // heavily compressed and pre-processed!

    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        /*
         * uhhh, it'd make sense if it wrote zero if the RLE table is empty?????
         * but I guess padding bytes work as zeroes too?!?!?
         * even though it is unreliable??
         * but like, how can you even make a sprite with RLEData==null? so it makes sense??
         * argh.
         */

        if (RLEData != null)
        {
            writer.Write(RLEData.Length);
            writer.Write(RLEData);
        }

        writer.Align(4);
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        int rlelen = reader.ReadInt32();
        if (rlelen > 0)
        {
            RLEData = reader.ReadBytes(rlelen);
        }

        // why it's not aligned before the data is beyond my brain.
        reader.Align(4);
    }
}

public enum UndertaleYYSWFItemType
{
    ItemInvalid,
    ItemShape,
    ItemBitmap,
    ItemFont,
    ItemTextField,
    ItemSprite
}

public enum UndertaleVectorFillType
{
    FillInvalid,
    FillSolid,
    FillGradient,
    FillBitmap
}

public enum UndertaleVectorBitmapFillType
{
    FillRepeat,
    FillClamp,
    FillRepeatPoint,
    FillClampPoint
}

public enum UndertaleVectorGradientFillType
{
    FillLinear,
    FillRadial
}

[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertaleVectorSolidFillData : UndertaleObject
{
    public byte Red { get; set; }
    public byte Green { get; set; }
    public byte Blue { get; set; }
    public byte Alpha { get; set; }

    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        writer.Write(Red);
        writer.Write(Green);
        writer.Write(Blue);
        writer.Write(Alpha);
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        Red = reader.ReadByte();
        Green = reader.ReadByte();
        Blue = reader.ReadByte();
        Alpha = reader.ReadByte();
    }
}

[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertaleVectorGradientRecord : UndertaleObject
{
    public int Ratio { get; set; }
    public byte Red { get; set; }
    public byte Green { get; set; }
    public byte Blue { get; set; }
    public byte Alpha { get; set; }

    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        writer.Write(Ratio);
        writer.Write(Red);
        writer.Write(Green);
        writer.Write(Blue);
        writer.Write(Alpha);
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        Ratio = reader.ReadInt32();
        Red = reader.ReadByte();
        Green = reader.ReadByte();
        Blue = reader.ReadByte();
        Alpha = reader.ReadByte();
    }
}

[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertaleVectorGradientFillData : UndertaleObject
{
    public UndertaleVectorGradientFillType GradientFillType { get; set; }
    public UndertaleVectorMatrix33 TransformationMatrix { get; set; }
    public UndertaleSimpleList<UndertaleVectorGradientRecord> Records { get; set; }

    /// <summary>
    /// Unknown purpose. Probably to accomodate for new texture formats.
    /// </summary>
    /// <remarks>
    /// Presumably present in GM 2022.1+.
    /// </remarks>
    public int TPEIndex { get; set; } = -1;

    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        writer.Write((int)GradientFillType);
        if (writer.undertaleData.IsVersionAtLeast(2022, 1))
        {
            writer.Write(TPEIndex);
        }
        writer.WriteUndertaleObject(TransformationMatrix);
        writer.WriteUndertaleObject(Records);
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        GradientFillType = (UndertaleVectorGradientFillType)reader.ReadInt32();
        if (reader.undertaleData.IsVersionAtLeast(2022, 1))
        {
            TPEIndex = reader.ReadInt32();
        }
        TransformationMatrix = reader.ReadUndertaleObjectNoPool<UndertaleVectorMatrix33>();

        int count = reader.ReadInt32();
        Records = new UndertaleSimpleList<UndertaleVectorGradientRecord>();
        Records.SetCapacity(count);
        for (int i = 0; i < count; i++)
        {
            Records.InternalAdd(reader.ReadUndertaleObjectNoPool<UndertaleVectorGradientRecord>());
        }
    }
}

[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertaleVectorBitmapFillData : UndertaleObject
{
    public UndertaleVectorBitmapFillType BitmapFillType { get; set; }
    public int CharID { get; set; }
    public UndertaleVectorMatrix33 TransformationMatrix { get; set; }

    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        writer.Write((int)BitmapFillType);
        writer.Write(CharID);
        writer.WriteUndertaleObject(TransformationMatrix);
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        BitmapFillType = (UndertaleVectorBitmapFillType)reader.ReadInt32();
        CharID = reader.ReadInt32();
        TransformationMatrix = reader.ReadUndertaleObjectNoPool<UndertaleVectorMatrix33>();
    }
}

[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertaleVectorFillData : UndertaleObject
{
    public UndertaleVectorFillType Type { get; set; }
    public UndertaleVectorBitmapFillData BitmapFillData { get; set; }
    public UndertaleVectorGradientFillData GradientFillData { get; set; }
    public UndertaleVectorSolidFillData SolidFillData { get; set; }

    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        writer.Write((int)Type);
        switch (Type)
        {
            case UndertaleVectorFillType.FillBitmap:
            {
                writer.WriteUndertaleObject(BitmapFillData);
                break;
            }

            case UndertaleVectorFillType.FillGradient:
            {
                writer.WriteUndertaleObject(GradientFillData);
                break;
            }

            case UndertaleVectorFillType.FillSolid:
            {
                writer.WriteUndertaleObject(SolidFillData);
                break;
            }

            case UndertaleVectorFillType.FillInvalid:
            {
                // throw an exception maybe?
                break;
            }
        }
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        Type = (UndertaleVectorFillType)reader.ReadInt32();
        switch (Type)
        {
            case UndertaleVectorFillType.FillBitmap:
            {
                BitmapFillData = reader.ReadUndertaleObjectNoPool<UndertaleVectorBitmapFillData>();
                break;
            }

            case UndertaleVectorFillType.FillGradient:
            {
                GradientFillData = reader.ReadUndertaleObjectNoPool<UndertaleVectorGradientFillData>();
                break;
            }

            case UndertaleVectorFillType.FillSolid:
            {
                SolidFillData = reader.ReadUndertaleObjectNoPool<UndertaleVectorSolidFillData>();
                break;
            }

            case UndertaleVectorFillType.FillInvalid:
            default:
            {
                reader.SubmitWarning("Tried to read invalid fill data.");
                break;
            }
        }
    }
}

[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertaleVectorLineStyleData : UndertaleObject
{
    public byte Red { get; set; }
    public byte Green { get; set; }
    public byte Blue { get; set; }
    public byte Alpha { get; set; }

    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        writer.Write(Red);
        writer.Write(Green);
        writer.Write(Blue);
        writer.Write(Alpha);
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        Red = reader.ReadByte();
        Green = reader.ReadByte();
        Blue = reader.ReadByte();
        Alpha = reader.ReadByte();
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"UndertaleVectorLineStyleData ({Red};{Green};{Blue};{Alpha})";
    }
}

[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertaleVector2 : UndertaleObject
{
    public int X { get; set; }
    public int Y { get; set; }

    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        writer.Write(X);
        writer.Write(Y);
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        X = reader.ReadInt32();
        Y = reader.ReadInt32();
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"UndertaleVector2 ({X};{Y})";
    }
}

[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertaleVector2F : UndertaleObject
{
    public float X { get; set; }
    public float Y { get; set; }

    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        writer.Write(X);
        writer.Write(Y);
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        X = reader.ReadSingle();
        Y = reader.ReadSingle();
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"UndertaleVector2F ({X};{Y})";
    }
}

[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertaleYYSWFSubShapeData : UndertaleObject
{
    public int FillStyleOne { get; set; }
    public int FillStyleTwo { get; set; }
    public int LineStyle { get; set; }

    public UndertaleObservableList<UndertaleVector2F> Points { get; set; }
    public UndertaleObservableList<UndertaleVector2> Lines { get; set; }
    public UndertaleObservableList<int> Triangles { get; set; }

    public UndertaleObservableList<UndertaleVector2F> LinePoints { get; set; }
    public UndertaleObservableList<int> LineTriangles { get; set; }

    public UndertaleObservableList<UndertaleVector2> AALines { get; set; }
    public UndertaleObservableList<UndertaleVector2F> AAVectors { get; set; }

    public UndertaleObservableList<UndertaleVector2> LineAALines { get; set; }
    public UndertaleObservableList<UndertaleVector2F> LineAAVectors { get; set; }

    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        writer.Write(FillStyleOne);
        writer.Write(FillStyleTwo);
        writer.Write(LineStyle);
        writer.Write(Points.Count);
        writer.Write(Lines.Count);
        writer.Write(Triangles.Count / 3);
        writer.Write(LinePoints.Count);
        writer.Write(LineTriangles.Count / 3);
        writer.Write(AALines.Count);
        writer.Write(AAVectors.Count);
        writer.Write(LineAALines.Count);
        writer.Write(LineAAVectors.Count);

        foreach (var vec in Points)
        {
            writer.WriteUndertaleObject(vec);
        }

        foreach (var vec in Lines)
        {
            writer.WriteUndertaleObject(vec);
        }

        foreach (var tri in Triangles)
        {
            writer.Write(tri);
        }

        foreach (var vec in LinePoints)
        {
            writer.WriteUndertaleObject(vec);
        }

        foreach (var tri in LineTriangles)
        {
            writer.Write(tri);
        }

        foreach (var vec in AALines)
        {
            writer.WriteUndertaleObject(vec);
        }

        foreach (var vec in AAVectors)
        {
            writer.WriteUndertaleObject(vec);
        }

        foreach (var vec in LineAALines)
        {
            writer.WriteUndertaleObject(vec);
        }

        foreach (var vec in LineAAVectors)
        {
            writer.WriteUndertaleObject(vec);
        }
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        FillStyleOne = reader.ReadInt32();
        FillStyleTwo = reader.ReadInt32();
        LineStyle = reader.ReadInt32();

        int points = reader.ReadInt32();
        int lines = reader.ReadInt32();
        int triangles = reader.ReadInt32() * 3;
        int linepoints = reader.ReadInt32();
        int linetriangles = reader.ReadInt32() * 3;
        int aalines = reader.ReadInt32();
        int aavectors = reader.ReadInt32();
        int lineaalines = reader.ReadInt32();
        int lineaavectors = reader.ReadInt32();

        Points = new UndertaleObservableList<UndertaleVector2F>(points);
        for (int i = 0; i < points; i++)
        {
            Points.InternalAdd(reader.ReadUndertaleObjectNoPool<UndertaleVector2F>());
        }

        Lines = new UndertaleObservableList<UndertaleVector2>(lines);
        for (int i = 0; i < lines; i++)
        {
            Lines.InternalAdd(reader.ReadUndertaleObjectNoPool<UndertaleVector2>());
        }

        Triangles = new UndertaleObservableList<int>(triangles);
        for (int i = 0; i < triangles; i++)
        {
            Triangles.InternalAdd(reader.ReadInt32());
        }

        LinePoints = new UndertaleObservableList<UndertaleVector2F>(linepoints);
        for (int i = 0; i < linepoints; i++)
        {
            LinePoints.InternalAdd(reader.ReadUndertaleObjectNoPool<UndertaleVector2F>());
        }

        LineTriangles = new UndertaleObservableList<int>(linetriangles);
        for (int i = 0; i < linetriangles; i++)
        {
            LineTriangles.InternalAdd(reader.ReadInt32());
        }

        AALines = new UndertaleObservableList<UndertaleVector2>(aalines);
        for (int i = 0; i < aalines; i++)
        {
            AALines.InternalAdd(reader.ReadUndertaleObjectNoPool<UndertaleVector2>());
        }

        AAVectors = new UndertaleObservableList<UndertaleVector2F>(aavectors);
        for (int i = 0; i < aavectors; i++)
        {
            AAVectors.InternalAdd(reader.ReadUndertaleObjectNoPool<UndertaleVector2F>());
        }

        LineAALines = new UndertaleObservableList<UndertaleVector2>(lineaalines);
        for (int i = 0; i < lineaalines; i++)
        {
            LineAALines.InternalAdd(reader.ReadUndertaleObjectNoPool<UndertaleVector2>());
        }

        LineAAVectors = new UndertaleObservableList<UndertaleVector2F>(lineaavectors);
        for (int i = 0; i < lineaavectors; i++)
        {
            LineAAVectors.InternalAdd(reader.ReadUndertaleObjectNoPool<UndertaleVector2F>());
        }
    }
}

[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertaleVectorSubShapeData : UndertaleObject
{
    public int FillStyleOne { get; set; }
    public int FillStyleTwo { get; set; }
    public int LineStyle { get; set; }

    public UndertaleObservableList<UndertaleVector2F> Points { get; set; }
    public UndertaleObservableList<uint> PointColors { get; set; }
    public UndertaleObservableList<int> Triangles { get; set; }

    public UndertaleObservableList<UndertaleVector2> AALines { get; set; }
    public UndertaleObservableList<UndertaleVector2F> AAVectors { get; set; }

    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        writer.Write(FillStyleOne);
        writer.Write(FillStyleTwo);
        writer.Write(LineStyle);
        writer.Write(Points.Count);
        writer.Write(PointColors.Count);
        writer.Write(0);
        writer.Write(Triangles.Count / 3);
        writer.Write(0);
        writer.Write(0);
        writer.Write(AALines.Count);
        writer.Write(AAVectors.Count);
        writer.Write(0);
        writer.Write(0);

        foreach (var vec in Points)
        {
            writer.WriteUndertaleObject(vec);
        }

        foreach (uint color in PointColors)
        {
            writer.Write(color);
        }

        foreach (int tri in Triangles)
        {
            writer.Write(tri);
        }

        foreach (var vec in AALines)
        {
            writer.WriteUndertaleObject(vec);
        }

        foreach (var vec in AAVectors)
        {
            writer.WriteUndertaleObject(vec);
        }
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        FillStyleOne = reader.ReadInt32();
        FillStyleTwo = reader.ReadInt32();
        LineStyle = reader.ReadInt32();

        int points = reader.ReadInt32();
        int pointcolors = reader.ReadInt32();
        reader.ReadInt32();
        int triangles = reader.ReadInt32() * 3;
        reader.ReadInt32();
        reader.ReadInt32();
        int aalines = reader.ReadInt32();
        int aavectors = reader.ReadInt32();
        reader.ReadInt32();
        reader.ReadInt32();

        Points = new UndertaleObservableList<UndertaleVector2F>(points);
        for (int i = 0; i < points; i++)
        {
            Points.InternalAdd(reader.ReadUndertaleObjectNoPool<UndertaleVector2F>());
        }

        PointColors = new UndertaleObservableList<uint>(pointcolors);
        for (int i = 0; i < pointcolors; i++)
        {
            PointColors.InternalAdd(reader.ReadUInt32());
        }

        Triangles = new UndertaleObservableList<int>(triangles);
        for (int i = 0; i < triangles; i++)
        {
            Triangles.InternalAdd(reader.ReadInt32());
        }

        AALines = new UndertaleObservableList<UndertaleVector2>(aalines);
        for (int i = 0; i < aalines; i++)
        {
            AALines.InternalAdd(reader.ReadUndertaleObjectNoPool<UndertaleVector2>());
        }

        AAVectors = new UndertaleObservableList<UndertaleVector2F>(aavectors);
        for (int i = 0; i < aavectors; i++)
        {
            AAVectors.InternalAdd(reader.ReadUndertaleObjectNoPool<UndertaleVector2F>());
        }
    }
}

[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertaleShapeData<T> : UndertaleObject where T : UndertaleObject, new()
{
    public float MinX { get; set; }
    public float MaxX { get; set; }
    public float MinY { get; set; }
    public float MaxY { get; set; }
    public UndertaleSimpleList<UndertaleStyleGroup<T>> StyleGroups { get; set; }


    /// <inheritdoc />
    public virtual void Serialize(UndertaleWriter writer)
    {
        writer.Write(MinX);
        writer.Write(MaxX);
        writer.Write(MinY);
        writer.Write(MaxY);
        writer.WriteUndertaleObject(StyleGroups);
    }

    /// <inheritdoc />
    public virtual void Unserialize(UndertaleReader reader)
    {
        MinX = reader.ReadSingle();
        MaxX = reader.ReadSingle();
        MinY = reader.ReadSingle();
        MaxY = reader.ReadSingle();
        StyleGroups = new UndertaleSimpleList<UndertaleStyleGroup<T>>();
        int s = reader.ReadInt32();
        for (int i = 0; i < s; i++)
        {
            StyleGroups.InternalAdd(reader.ReadUndertaleObjectNoPool<UndertaleStyleGroup<T>>());
        }
    }
}

[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertaleVectorShapeData : UndertaleShapeData<UndertaleVectorSubShapeData>
{
    public int Version { get; set; }

    public override void Serialize(UndertaleWriter writer)
    {
        writer.Align(4);
        writer.Write(Version);
        base.Serialize(writer);
    }

    public override void Unserialize(UndertaleReader reader)
    {
        reader.Align(4);
        Version = reader.ReadInt32();
        Util.DebugUtil.Assert(Version == 3 || Version == 4, "Invalid shape format version number, expected 3 or 4, got " + Version);
        base.Unserialize(reader);
    }
}

[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertaleStyleGroup<T> : UndertaleObject where T : UndertaleObject, new()
{
    public UndertaleObservableList<UndertaleVectorFillData> FillStyles { get; set; }
    public UndertaleObservableList<UndertaleVectorLineStyleData> LineStyles { get; set; }
    public UndertaleObservableList<T> Subshapes { get; set; }

    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        writer.Write(FillStyles.Count);
        writer.Write(LineStyles.Count);
        writer.Write(Subshapes.Count);

        foreach (var fill in FillStyles)
        {
            writer.WriteUndertaleObject(fill);
        }

        foreach (var line in LineStyles)
        {
            writer.WriteUndertaleObject(line);
        }

        foreach (var shape in Subshapes)
        {
            writer.WriteUndertaleObject(shape);
        }
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        int f = reader.ReadInt32();
        int l = reader.ReadInt32();
        int s = reader.ReadInt32();

        FillStyles = new UndertaleObservableList<UndertaleVectorFillData>(f);
        LineStyles = new UndertaleObservableList<UndertaleVectorLineStyleData>(l);
        Subshapes = new UndertaleObservableList<T>(s);

        for (int i = 0; i < f; i++)
        {
            FillStyles.InternalAdd(reader.ReadUndertaleObjectNoPool<UndertaleVectorFillData>());
        }

        for (int i = 0; i < l; i++)
        {
            LineStyles.InternalAdd(reader.ReadUndertaleObjectNoPool<UndertaleVectorLineStyleData>());
        }

        for (int i = 0; i < s; i++)
        {
            Subshapes.InternalAdd(reader.ReadUndertaleObjectNoPool<T>());
        }
    }
}

public enum UndertaleYYSWFBitmapType
{
    TypeJPEGNoHeader,
    TypeJPEG,
    TypeJPEGWithAlpha,
    TypePNG,
    TypeGIF,
    TypeLossless8bit,
    TypeLossless15bit,
    TypeLossless24bit,
    TypeLossless8bitA,
    TypeLossless32bit
}

[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertaleYYSWFBitmapData : UndertaleObject
{
    public UndertaleYYSWFBitmapType Type { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    /// <summary>
    /// Unknown purpose. Probably to accomodate for new texture formats.
    /// </summary>
    /// <remarks>
    /// Presumably present in GM 2022.1+.
    /// </remarks>
    public int? TPEIndex { get; set; }
    public byte[] ImageData { get; set; }
    public byte[] AlphaData { get; set; }
    public byte[] ColorPaletteData { get; set; }


    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        writer.Write((int)Type);
        writer.Write(Width);
        writer.Write(Height);

        if (TPEIndex is null)
        {
            writer.Write(ImageData is null ? 0 : ImageData.Length);
            writer.Write(AlphaData is null ? 0 : AlphaData.Length);
            writer.Write(ColorPaletteData is null ? 0 : ColorPaletteData.Length);

            if (ImageData != null)
                writer.Write(ImageData);
            if (AlphaData != null)
                writer.Write(AlphaData);
            if (ColorPaletteData != null)
                writer.Write(ColorPaletteData);

            writer.Align(4);
        }
        else
        {
            writer.Write(TPEIndex.Value);
        }
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        Type = (UndertaleYYSWFBitmapType)reader.ReadInt32();
        Width = reader.ReadInt32();
        Height = reader.ReadInt32();

        if (reader.undertaleData.IsVersionAtLeast(2022, 1))
        {
            TPEIndex = reader.ReadInt32();
        }
        else
        {
            int iL = reader.ReadInt32();
            int aL = reader.ReadInt32();
            int cL = reader.ReadInt32();

            if (iL > 0)
                ImageData = reader.ReadBytes(iL);
            if (aL > 0)
                AlphaData = reader.ReadBytes(aL);
            if (cL > 0)
                ColorPaletteData = reader.ReadBytes(cL);

            reader.Align(4);
        }
    }
}

[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertaleYYSWFItem : UndertaleObject
{
    public int ID { get; set; }
    public UndertaleYYSWFItemType ItemType { get; set; }
    public UndertaleShapeData<UndertaleYYSWFSubShapeData> ShapeData { get; set; }
    public UndertaleYYSWFBitmapData BitmapData { get; set; }

    public UndertaleYYSWFItem()
    {
        ItemType = UndertaleYYSWFItemType.ItemInvalid;
        ID = -1;
    }

    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        writer.Write((int)ItemType);
        writer.Write(ID);

        switch (ItemType)
        {
            case UndertaleYYSWFItemType.ItemShape:
            {
                writer.WriteUndertaleObject(ShapeData);
                break;
            }

            case UndertaleYYSWFItemType.ItemBitmap:
            {
                writer.WriteUndertaleObject(BitmapData);
                break;
            }
        }
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        ItemType = (UndertaleYYSWFItemType)reader.ReadInt32();
        ID = reader.ReadInt32();

        // I know, right?
        switch (ItemType)
        {
            case UndertaleYYSWFItemType.ItemShape:
            {
                ShapeData = reader.ReadUndertaleObjectNoPool<UndertaleShapeData<UndertaleYYSWFSubShapeData>>();
                break;
            }

            case UndertaleYYSWFItemType.ItemBitmap:
            {
                BitmapData = reader.ReadUndertaleObjectNoPool<UndertaleYYSWFBitmapData>();
                break;
            }

            case UndertaleYYSWFItemType.ItemFont:
            case UndertaleYYSWFItemType.ItemInvalid:
            case UndertaleYYSWFItemType.ItemTextField:
            case UndertaleYYSWFItemType.ItemSprite:
            default:
            {
                reader.SubmitWarning("Tried to read unknown YYSWFItem, " + ItemType);
                break;
            }
        }
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"UndertaleYYSWFItem ({ItemType}, {ID})";
    }
}

[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertaleYYSWFTimeline : UndertaleObject
{
    public int Framerate { get; set; }
    public float MinX { get; set; }
    public float MaxX { get; set; }
    public float MinY { get; set; }
    public float MaxY { get; set; }
    public int MaskWidth { get; set; }
    public int MaskHeight { get; set; }
    public UndertaleSimpleList<UndertaleYYSWFItem> UsedItems { get; set; }
    public UndertaleObservableList<UndertaleYYSWFTimelineFrame> Frames { get; set; }
    public UndertaleObservableList<UndertaleYYSWFCollisionMask> CollisionMasks { get; set; }

    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        writer.WriteUndertaleObject(UsedItems);
        writer.Write(Framerate);
        writer.Write(Frames.Count);
        writer.Write(MinX);
        writer.Write(MaxX);
        writer.Write(MinY);
        writer.Write(MaxY);
        writer.Write(CollisionMasks.Count);
        writer.Write(MaskWidth);
        writer.Write(MaskHeight);

        foreach (var yyswfFrame in Frames)
        {
            writer.WriteUndertaleObject(yyswfFrame);
        }

        foreach (var yyswfMask in CollisionMasks)
        {
            writer.WriteUndertaleObject(yyswfMask);
        }
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        int uc = reader.ReadInt32();
        UsedItems = new UndertaleSimpleList<UndertaleYYSWFItem>();
        UsedItems.SetCapacity(uc);
        for (int i = 0; i < uc; i++)
        {
            UsedItems.InternalAdd(reader.ReadUndertaleObjectNoPool<UndertaleYYSWFItem>());
        }

        Framerate = reader.ReadInt32();
        int fc = reader.ReadInt32();
        MinX = reader.ReadSingle();
        MaxX = reader.ReadSingle();
        MinY = reader.ReadSingle();
        MaxY = reader.ReadSingle();
        int mc = reader.ReadInt32();
        MaskWidth = reader.ReadInt32();
        MaskHeight = reader.ReadInt32();

        Frames = new UndertaleObservableList<UndertaleYYSWFTimelineFrame>(fc);
        for (int f = 0; f < fc; f++)
        {
            Frames.InternalAdd(reader.ReadUndertaleObjectNoPool<UndertaleYYSWFTimelineFrame>());
        }

        CollisionMasks = new UndertaleObservableList<UndertaleYYSWFCollisionMask>(mc);
        for (int m = 0; m < mc; m++)
        {
            CollisionMasks.InternalAdd(reader.ReadUndertaleObjectNoPool<UndertaleYYSWFCollisionMask>());
        }
    }
}

[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertaleYYSWF : UndertaleObject
{
    public byte[] JPEGTable { get; set; }
    public int Version { get; set; }
    public UndertaleYYSWFTimeline Timeline { get; set; }

    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        writer.Align(4);
        int len = (JPEGTable?.Length ?? 0) | Int32.MinValue;

        writer.Write(len);
        writer.Write(Version);
        if (JPEGTable != null)
        {
            writer.Write(JPEGTable);
        }

        writer.Align(4);
        writer.WriteUndertaleObject(Timeline);
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        reader.Align(4);
        int jpeglen = reader.ReadInt32() & (~Int32.MinValue); // the length is ORed with int.MinValue.
        Version = reader.ReadInt32();
        Util.DebugUtil.Assert(Version == 8 || Version == 7, "Invalid YYSWF version data! Expected 7 or 8, got " + Version);

        if (jpeglen > 0)
        {
            JPEGTable = reader.ReadBytes(jpeglen);
        }

        reader.Align(4);
        Timeline = reader.ReadUndertaleObjectNoPool<UndertaleYYSWFTimeline>();
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"UndertaleYYSWF ({Version})";
    }
}
