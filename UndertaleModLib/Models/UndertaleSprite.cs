using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

namespace UndertaleModLib.Models;

public enum AnimSpeedType : uint
{
    FramesPerSecond = 0,
    FramesPerGameFrame = 1
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
    /// The atlas as raw bytes, can be a GameMaker QOI texture or a PNG file.
    /// </summary>
    public byte[] TexBlob { get; set; }
    
    /// <summary>
    /// Indicates whether <see cref="TexBlob"/> contains a GameMaker QOI texture (the header is qoif reversed).
    /// </summary>
    public bool IsQOI => TexBlob != null && TexBlob.Length > 7 && TexBlob[0] == 102/*f*/ && TexBlob[1] == 105/*i*/ && TexBlob[2] == 111/*o*/ && TexBlob[3] == 113/*q*/;

    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        writer.Write(PageWidth);
        writer.Write(PageHeight);
        writer.Write(TexBlob.Length);
        writer.Write(TexBlob);
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        PageWidth = reader.ReadInt32();
        PageHeight = reader.ReadInt32();
        TexBlob = reader.ReadBytes(reader.ReadInt32());
    }

    /// <inheritdoc cref="UndertaleObject.UnserializeChildObjectCount(UndertaleReader)"/>
    public static uint UnserializeChildObjectCount(UndertaleReader reader)
    {
        reader.Position += 8;                        // Size
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

/// <summary>
/// Sprite entry in the data file.
/// </summary>
public class UndertaleSprite : UndertaleNamedResource, PrePaddedObject, INotifyPropertyChanged
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
    /// A <see cref="OriginX"/> wrapper that also sets <see cref="V2Sequence.OriginX"/> accordingly.
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
    /// A <see cref="OriginY"/> wrapper that also sets <see cref="V2Sequence.OriginY"/> accordingly.
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
    public UndertaleSimpleList<TextureEntry> Textures { get; private set; } = new UndertaleSimpleList<TextureEntry>();

    /// <summary>
    /// The collision masks of the sprite.
    /// </summary>
    public ObservableCollection<MaskEntry> CollisionMasks { get; private set; } = new ObservableCollection<MaskEntry>();

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
    public bool IsYYSWFSprite { get => YYSWF != null; }

    private int _SWFVersion;
    private UndertaleYYSWF _YYSWF;

    public int SWFVersion { get => _SWFVersion; set { _SWFVersion = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SWFVersion))); } }
    public UndertaleYYSWF YYSWF { get => _YYSWF; set { _YYSWF = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(YYSWF))); } }

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
    }

    public MaskEntry NewMaskEntry()
    {
        MaskEntry newEntry = new MaskEntry();
        uint len = (Width + 7) / 8 * Height;
        newEntry.Data = new byte[len];
        return newEntry;
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
        Spine = 2
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

        public MaskEntry()
        {
        }

        public MaskEntry(byte[] data)
        {
            this.Data = data;
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
                    writer.Write((int)0);
                    if (SVersion >= 3)
                    {
                        nineSlicePatchPos = writer.Position;
                        writer.Write((int)0);
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
            }

            // Sequence + nine slice
            if (sequencePatchPos != 0 && V2Sequence != null) // Normal compiler also checks for sprite type to be normal, but whatever!
            {
                uint returnTo = writer.Position;
                writer.Position = sequencePatchPos;
                writer.Write(returnTo);
                writer.Position = returnTo;
                writer.Write((int)1);
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
        Util.DebugUtil.Assert(total == CalculateMaskDataSize(Width, Height, (uint)CollisionMasks.Count), "Invalid mask data for sprite");
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

        reader.Position += 44;

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
                    SkipMaskData(reader, width, height);
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
                            reader.Position += 8 + jsonLength + atlasLength + textures;
                            break;

                        case 2:
                        case 3:
                        {
                            reader.Position += jsonLength + atlasLength;

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
            SkipMaskData(reader, width, height);
        }

        return count;
    }

    private void ReadMaskData(UndertaleReader reader)
    {
        uint maskCount = reader.ReadUInt32();
        uint len = (Width + 7) / 8 * Height;
        List<MaskEntry> newMasks = new((int)maskCount);
        uint total = 0;
        for (uint i = 0; i < maskCount; i++)
        {
            newMasks.Add(new MaskEntry(reader.ReadBytes((int)len)));
            total += len;
        }

        CollisionMasks = new(newMasks);

        while (total % 4 != 0)
        {
            if (reader.ReadByte() != 0)
                throw new IOException("Mask padding");
            total++;
        }
        Util.DebugUtil.Assert(total == CalculateMaskDataSize(Width, Height, maskCount));
    }
    private static void SkipMaskData(UndertaleReader reader, uint width, uint height)
    {
        uint maskCount = reader.ReadUInt32();
        uint len = (width + 7) / 8 * height;

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

    public uint CalculateMaskDataSize(uint width, uint height, uint maskcount)
    {
        uint roundedWidth = (width + 7) / 8 * 8; // round to multiple of 8
        uint dataBits = roundedWidth * height * maskcount;
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

// TODO: make all these classes "IDisposable"
/// <summary>
/// Some dirty hacks to make SWF work, they'll be removed later. TODO:
/// </summary>
public static class UndertaleYYSWFUtils
{
    /// <summary>
    /// Reads an <see cref="UndertaleObject"/> ignoring the <paramref name="reader"/>s object pool.
    /// </summary>
    /// <typeparam name="T"><see cref="UndertaleObject"/>s child.</typeparam>
    /// <param name="reader">An instance of <see cref="UndertaleReader"/>.</param>
    /// <returns>The object</returns>
    public static T ReadUndertaleObjectNoPool<T>(this UndertaleReader reader) where T : UndertaleObject, new()
    {
        T o = new T();
        o.Unserialize(reader);
        return o;
    }
}

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
public class UndertaleYYSWFMatrix33 : UndertaleObject
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
    public UndertaleYYSWFMatrix33 TransformationMatrix { get; set; }
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
        TransformationMatrix = reader.ReadUndertaleObjectNoPool<UndertaleYYSWFMatrix33>();
    }
}

[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertaleYYSWFTimelineFrame : UndertaleObject
{
    public UndertaleSimpleList<UndertaleYYSWFTimelineObject> FrameObjects { get; set; }
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
        FrameObjects = new UndertaleSimpleList<UndertaleYYSWFTimelineObject>();
        for (int i = 0; i < ii; i++)
        {
            var frameObject = reader.ReadUndertaleObjectNoPool<UndertaleYYSWFTimelineObject>();
            FrameObjects.Add(frameObject);
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

public enum UndertaleYYSWFItemType : int
{
    ItemInvalid,
    ItemShape,
    ItemBitmap,
    ItemFont,
    ItemTextField,
    ItemSprite
}

public enum UndertaleYYSWFFillType
{
    FillInvalid,
    FillSolid,
    FillGradient,
    FillBitmap
}

public enum UndertaleYYSWFBitmapFillType
{
    FillRepeat,
    FillClamp,
    FillRepeatPoint,
    FillClampPoint
}

public enum UndertaleYYSWFGradientFillType
{
    FillLinear,
    FillRadial
}

[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertaleYYSWFSolidFillData : UndertaleObject
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
public class UndertaleYYSWFGradientRecord : UndertaleObject
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
public class UndertaleYYSWFGradientFillData : UndertaleObject
{
    public UndertaleYYSWFGradientFillType GradientFillType { get; set; }
    public UndertaleYYSWFMatrix33 TransformationMatrix { get; set; }
    public UndertaleSimpleList<UndertaleYYSWFGradientRecord> Records { get; set; }
    /// <summary>
    /// Unknown purpose. Probably to accomodate for new texture formats.
    /// </summary>
    /// <remarks>
    /// Presumably present in GM 2022.1+.
    /// </remarks>
    public int? TPEIndex { get; set; }

    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        writer.Write((int)GradientFillType);
        if (TPEIndex is not null)
            writer.Write(TPEIndex.Value);
        writer.WriteUndertaleObject(TransformationMatrix);
        writer.WriteUndertaleObject(Records);
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        GradientFillType = (UndertaleYYSWFGradientFillType)reader.ReadInt32();
        if (reader.undertaleData.IsVersionAtLeast(2022, 1))
        {
            TPEIndex = reader.ReadInt32();
        }
        TransformationMatrix = reader.ReadUndertaleObject<UndertaleYYSWFMatrix33>();
        Records = new UndertaleSimpleList<UndertaleYYSWFGradientRecord>();
        int count = reader.ReadInt32();
        for (int i = 0; i < count; i++)
        {
            Records.Add(reader.ReadUndertaleObjectNoPool<UndertaleYYSWFGradientRecord>());
        }
    }
}

[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertaleYYSWFBitmapFillData : UndertaleObject
{
    public UndertaleYYSWFBitmapFillType BitmapFillType { get; set; }
    public int CharID { get; set; }
    public UndertaleYYSWFMatrix33 TransformationMatrix { get; set; }

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
        BitmapFillType = (UndertaleYYSWFBitmapFillType)reader.ReadInt32();
        CharID = reader.ReadInt32();
        TransformationMatrix = reader.ReadUndertaleObjectNoPool<UndertaleYYSWFMatrix33>();
    }
}

[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertaleYYSWFFillData : UndertaleObject
{
    public UndertaleYYSWFFillType Type { get; set; }
    public UndertaleYYSWFBitmapFillData BitmapFillData { get; set; }
    public UndertaleYYSWFGradientFillData GradientFillData { get; set; }
    public UndertaleYYSWFSolidFillData SolidFillData { get; set; }

    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        writer.Write((int)Type);
        switch (Type)
        {
            case UndertaleYYSWFFillType.FillBitmap:
            {
                writer.WriteUndertaleObject(BitmapFillData);
                break;
            }

            case UndertaleYYSWFFillType.FillGradient:
            {
                writer.WriteUndertaleObject(GradientFillData);
                break;
            }

            case UndertaleYYSWFFillType.FillSolid:
            {
                writer.WriteUndertaleObject(SolidFillData);
                break;
            }

            case UndertaleYYSWFFillType.FillInvalid:
            {
                // throw an exception maybe?
                break;
            }
        }
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        Type = (UndertaleYYSWFFillType)reader.ReadInt32();
        switch (Type)
        {
            case UndertaleYYSWFFillType.FillBitmap:
            {
                BitmapFillData = reader.ReadUndertaleObjectNoPool<UndertaleYYSWFBitmapFillData>();
                break;
            }

            case UndertaleYYSWFFillType.FillGradient:
            {
                GradientFillData = reader.ReadUndertaleObjectNoPool<UndertaleYYSWFGradientFillData>();
                break;
            }

            case UndertaleYYSWFFillType.FillSolid:
            {
                SolidFillData = reader.ReadUndertaleObjectNoPool<UndertaleYYSWFSolidFillData>();
                break;
            }

            case UndertaleYYSWFFillType.FillInvalid:
            default:
            {
                reader.SubmitWarning("Tried to read invalid fill data.");
                break;
            }
        }
    }
}

[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertaleYYSWFLineStyleData : UndertaleObject
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
        return $"UndertaleYYSWFLineStyleData ({Red};{Green};{Blue};{Alpha})";
    }
}

[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertaleYYSWFVector2 : UndertaleObject
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
        return $"UndertaleYYSWFVector2 ({X};{Y})";
    }
}

[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertaleYYSWFVector2F : UndertaleObject
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
        return $"UndertaleYYSWFVector2F ({X};{Y})";
    }
}

[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertaleYYSWFSubshapeData : UndertaleObject
{
    public int FillStyleOne { get; set; }
    public int FillStyleTwo { get; set; }
    public int LineStyle { get; set; }

    public UndertaleSimpleList<UndertaleYYSWFVector2F> Points { get; set; }
    public UndertaleSimpleList<UndertaleYYSWFVector2> Lines { get; set; }
    public ObservableCollection<int> Triangles { get; set; }

    public UndertaleSimpleList<UndertaleYYSWFVector2F> LinePoints { get; set; }
    public ObservableCollection<int> LineTriangles { get; set; }

    public UndertaleSimpleList<UndertaleYYSWFVector2> AALines { get; set; }
    public UndertaleSimpleList<UndertaleYYSWFVector2F> AAVectors { get; set; }

    public UndertaleSimpleList<UndertaleYYSWFVector2> LineAALines { get; set; }
    public UndertaleSimpleList<UndertaleYYSWFVector2F> LineAAVectors { get; set; }

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
        Points = new UndertaleSimpleList<UndertaleYYSWFVector2F>();
        Lines = new UndertaleSimpleList<UndertaleYYSWFVector2>();
        Triangles = new ObservableCollection<int>();
        LinePoints = new UndertaleSimpleList<UndertaleYYSWFVector2F>();
        LineTriangles = new ObservableCollection<int>();
        AALines = new UndertaleSimpleList<UndertaleYYSWFVector2>();
        AAVectors = new UndertaleSimpleList<UndertaleYYSWFVector2F>();
        LineAALines = new UndertaleSimpleList<UndertaleYYSWFVector2>();
        LineAAVectors = new UndertaleSimpleList<UndertaleYYSWFVector2F>();

        int points = reader.ReadInt32();
        int lines = reader.ReadInt32();
        int triangles = reader.ReadInt32() * 3;
        int linepoints = reader.ReadInt32();
        int linetriangles = reader.ReadInt32() * 3;
        int aalines = reader.ReadInt32();
        int aavectors = reader.ReadInt32();
        int lineaalines = reader.ReadInt32();
        int lineaavectors = reader.ReadInt32();

        for (int i = 0; i < points; i++)
        {
            Points.Add(reader.ReadUndertaleObjectNoPool<UndertaleYYSWFVector2F>());
        }

        for (int i = 0; i < lines; i++)
        {
            Lines.Add(reader.ReadUndertaleObjectNoPool<UndertaleYYSWFVector2>());
        }

        for (int i = 0; i < triangles; i++)
        {
            Triangles.Add(reader.ReadInt32());
        }

        for (int i = 0; i < linepoints; i++)
        {
            LinePoints.Add(reader.ReadUndertaleObjectNoPool<UndertaleYYSWFVector2F>());
        }

        for (int i = 0; i < linetriangles; i++)
        {
            LineTriangles.Add(reader.ReadInt32());
        }

        for (int i = 0; i < aalines; i++)
        {
            AALines.Add(reader.ReadUndertaleObjectNoPool<UndertaleYYSWFVector2>());
        }

        for (int i = 0; i < aavectors; i++)
        {
            AAVectors.Add(reader.ReadUndertaleObjectNoPool<UndertaleYYSWFVector2F>());
        }

        for (int i = 0; i < lineaalines; i++)
        {
            LineAALines.Add(reader.ReadUndertaleObjectNoPool<UndertaleYYSWFVector2>());
        }

        for (int i = 0; i < lineaavectors; i++)
        {
            LineAAVectors.Add(reader.ReadUndertaleObjectNoPool<UndertaleYYSWFVector2F>());
        }
    }
}

[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertaleYYSWFShapeData : UndertaleObject
{
    public float MinX { get; set; }
    public float MaxX { get; set; }
    public float MinY { get; set; }
    public float MaxY { get; set; }
    public UndertaleSimpleList<UndertaleYYSWFStyleGroup> StyleGroups { get; set; }


    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        writer.Write(MinX);
        writer.Write(MaxX);
        writer.Write(MinY);
        writer.Write(MaxY);
        writer.WriteUndertaleObject(StyleGroups);
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        MinX = reader.ReadSingle();
        MaxX = reader.ReadSingle();
        MinY = reader.ReadSingle();
        MaxY = reader.ReadSingle();
        StyleGroups = new UndertaleSimpleList<UndertaleYYSWFStyleGroup>();
        int s = reader.ReadInt32();
        for (int i = 0; i < s; i++)
        {
            StyleGroups.Add(reader.ReadUndertaleObjectNoPool<UndertaleYYSWFStyleGroup>());
        }
    }
}

[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertaleYYSWFStyleGroup : UndertaleObject
{
    public UndertaleSimpleList<UndertaleYYSWFFillData> FillStyles { get; set; }
    public UndertaleSimpleList<UndertaleYYSWFLineStyleData> LineStyles { get; set; }
    public UndertaleSimpleList<UndertaleYYSWFSubshapeData> Subshapes { get; set; }

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

        FillStyles = new UndertaleSimpleList<UndertaleYYSWFFillData>();
        LineStyles = new UndertaleSimpleList<UndertaleYYSWFLineStyleData>();
        Subshapes = new UndertaleSimpleList<UndertaleYYSWFSubshapeData>();

        for (int i = 0; i < f; i++)
        {
            FillStyles.Add(reader.ReadUndertaleObjectNoPool<UndertaleYYSWFFillData>());
        }

        for (int i = 0; i < l; i++)
        {
            LineStyles.Add(reader.ReadUndertaleObjectNoPool<UndertaleYYSWFLineStyleData>());
        }

        for (int i = 0; i < s; i++)
        {
            Subshapes.Add(reader.ReadUndertaleObjectNoPool<UndertaleYYSWFSubshapeData>());
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
    public UndertaleYYSWFShapeData ShapeData { get; set; }
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
                ShapeData = reader.ReadUndertaleObjectNoPool<UndertaleYYSWFShapeData>();
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
    public UndertaleSimpleList<UndertaleYYSWFTimelineFrame> Frames { get; set; }
    public UndertaleSimpleList<UndertaleYYSWFCollisionMask> CollisionMasks { get; set; }

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
        //UsedItems = reader.ReadUndertaleObject<UndertaleSimpleList<UndertaleYYSWFItem>>();
        UsedItems = new UndertaleSimpleList<UndertaleYYSWFItem>();
        int uc = reader.ReadInt32();
        for (int i = 0; i < uc; i++)
        {
            UsedItems.Add(reader.ReadUndertaleObjectNoPool<UndertaleYYSWFItem>());
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

        Frames = new UndertaleSimpleList<UndertaleYYSWFTimelineFrame>();
        for (int f = 0; f < fc; f++)
        {
            var yyswfFrame = reader.ReadUndertaleObjectNoPool<UndertaleYYSWFTimelineFrame>();
            Frames.Add(yyswfFrame);
        }

        CollisionMasks = new UndertaleSimpleList<UndertaleYYSWFCollisionMask>();
        for (int m = 0; m < mc; m++)
        {
            var yyswfMask = reader.ReadUndertaleObjectNoPool<UndertaleYYSWFCollisionMask>();
            CollisionMasks.Add(yyswfMask);
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
        int jpeglen = reader.ReadInt32() & (~int.MinValue); // the length is ORed with int.MinValue.
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
