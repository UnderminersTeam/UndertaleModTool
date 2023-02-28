using System;

namespace UndertaleModLib.Models;

/// <summary>
/// A texture group info entry in a data file.
/// </summary>
/// <remarks>This is a new chunk since Bytecode 17. It's probably related to performance improvements mentioned in the release notes for that runtime.
/// Here is the specification:
/// <code>
/// TGIN structure - likely stands for Texture Group Info
/// Chunk introduced in 2.2.1 with new texture functions
/// ---
///
/// Int32 - probably chunk format version number, always 1
///
/// PointerList&lt;T&gt; structure. Each item represents a texture group:
///     32-bit string pointer - Name
///     32-bit pointer #1
///     32-bit pointer #2
///     32-bit pointer #3
///     32-bit pointer #4
///     32-bit pointer #5
///
///     #1 leads here:
///     SimpleList&lt;int&gt; of texture page IDs the group has
///
///     #2 leads here:
///     SimpleList&lt;int&gt; of sprite IDs the group has
///
///     #3 leads here:
///     SimpleList&lt;int&gt; of Spine sprite IDs (normal sprite ID, just this has Spine sprites separated) the group has
///
///     #4 leads here:
///     SimpleList&lt;int&gt; of font IDs the group has
///
///     #5 leads here:
///     SimpleList&lt;int&gt; of tileset IDs the group has
/// </code>
/// <see href="https://github.com/krzys-h/UndertaleModTool/wiki/Bytecode-17#tgin-new-chunk"/>.</remarks>
[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertaleTextureGroupInfo : UndertaleNamedResource, IDisposable
{
    /// <summary>
    /// The name of the texture group info entry.
    /// </summary>
    public UndertaleString Name { get; set; }

    /// <summary>
    /// A list of texture pages the group has.
    /// </summary>
    public UndertaleSimpleResourcesList<UndertaleEmbeddedTexture, UndertaleChunkTXTR> TexturePages { get; set; }

    /// <summary>
    /// A list of sprites the group has.
    /// </summary>
    public UndertaleSimpleResourcesList<UndertaleSprite, UndertaleChunkSPRT> Sprites { get; set; }

    /// <summary>
    /// A list of spine sprites the group has.
    /// </summary>
    public UndertaleSimpleResourcesList<UndertaleSprite, UndertaleChunkSPRT> SpineSprites { get; set; }

    /// <summary>
    /// A list of fonts the group has.
    /// </summary>
    public UndertaleSimpleResourcesList<UndertaleFont, UndertaleChunkFONT> Fonts { get; set; }

    /// <summary>
    /// A list of tilesets this group has.
    /// </summary>
    public UndertaleSimpleResourcesList<UndertaleBackground, UndertaleChunkBGND> Tilesets { get; set; }

    /// <summary>
    /// Directory of the texture on disk. 2022.9+ only.
    /// </summary>
    public UndertaleString Directory { get; set; }

    /// <summary>
    /// File extension of the texture on disk. 2022.9+ only.
    /// </summary>
    public UndertaleString Extension { get; set; }

    /// <summary>
    /// The load type of the texture. 2022.9+ only.
    /// </summary>
    public TextureGroupLoadType LoadType { get; set; }

    /// <summary>
    /// The possible load types of a texture in 2022.9 and above. Old versions default to "InFile".
    /// </summary>
    public enum TextureGroupLoadType
    {
        /// <summary>
        /// The texture data is located inside this file.
        /// </summary>
        InFile = 0,
        /// <summary>
        /// The textures of the group this belongs to are located externally
        /// May mean more specifically that textures for one texture group are all in one file.
        /// </summary>
        SeparateGroup = 1,
        /// <summary>
        /// The textures of the group this belongs to are located externally.
        /// May mean more specifically that textures are separated into different files, within the group.
        /// </summary>
        SeparateTextures = 2
    }

    /// <summary>
    /// Initializes a new instance of <see cref="UndertaleTextureGroupInfo"/>.
    /// </summary>
    public UndertaleTextureGroupInfo()
    {
        TexturePages = new UndertaleSimpleResourcesList<UndertaleEmbeddedTexture, UndertaleChunkTXTR>();
        Sprites = new UndertaleSimpleResourcesList<UndertaleSprite, UndertaleChunkSPRT>();
        SpineSprites = new UndertaleSimpleResourcesList<UndertaleSprite, UndertaleChunkSPRT>();
        Fonts = new UndertaleSimpleResourcesList<UndertaleFont, UndertaleChunkFONT>();
        Tilesets = new UndertaleSimpleResourcesList<UndertaleBackground, UndertaleChunkBGND>();
    }

    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        writer.WriteUndertaleString(Name);

        if (writer.undertaleData.IsVersionAtLeast(2022, 9))
        {
            writer.WriteUndertaleString(Directory);
            writer.WriteUndertaleString(Extension);
            writer.Write((int)LoadType);
        }

        writer.WriteUndertaleObjectPointer(TexturePages);
        writer.WriteUndertaleObjectPointer(Sprites);
        writer.WriteUndertaleObjectPointer(SpineSprites);
        writer.WriteUndertaleObjectPointer(Fonts);
        writer.WriteUndertaleObjectPointer(Tilesets);

        writer.WriteUndertaleObject(TexturePages);
        writer.WriteUndertaleObject(Sprites);
        writer.WriteUndertaleObject(SpineSprites);
        writer.WriteUndertaleObject(Fonts);
        writer.WriteUndertaleObject(Tilesets);
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        Name = reader.ReadUndertaleString();

        if (reader.undertaleData.IsVersionAtLeast(2022, 9))
        {
            Directory = reader.ReadUndertaleString();
            Extension = reader.ReadUndertaleString();
            LoadType = (TextureGroupLoadType)reader.ReadInt32();
        }

        // Read the pointers
        TexturePages = reader.ReadUndertaleObjectPointer<UndertaleSimpleResourcesList<UndertaleEmbeddedTexture, UndertaleChunkTXTR>>();
        Sprites = reader.ReadUndertaleObjectPointer<UndertaleSimpleResourcesList<UndertaleSprite, UndertaleChunkSPRT>>();
        SpineSprites = reader.ReadUndertaleObjectPointer<UndertaleSimpleResourcesList<UndertaleSprite, UndertaleChunkSPRT>>();
        Fonts = reader.ReadUndertaleObjectPointer<UndertaleSimpleResourcesList<UndertaleFont, UndertaleChunkFONT>>();
        Tilesets = reader.ReadUndertaleObjectPointer<UndertaleSimpleResourcesList<UndertaleBackground, UndertaleChunkBGND>>();

        // Read the objects, throwing an error if the pointers are invalid
        reader.ReadUndertaleObject(TexturePages);
        reader.ReadUndertaleObject(Sprites);
        reader.ReadUndertaleObject(SpineSprites);
        reader.ReadUndertaleObject(Fonts);
        reader.ReadUndertaleObject(Tilesets);
    }

    /// <inheritdoc cref="UndertaleObject.UnserializeChildObjectCount(UndertaleReader)"/>
    public static uint UnserializeChildObjectCount(UndertaleReader reader)
    {
        uint count = 0;

        reader.Position += 4; // "Name"

        if (reader.undertaleData.IsVersionAtLeast(2022, 9))
            reader.Position += 12;

        uint texPagesPtr = reader.ReadUInt32();
        uint spritesPtr = reader.ReadUInt32();
        uint spineSpritesPtr = reader.ReadUInt32();
        uint fontsPtr = reader.ReadUInt32();
        uint tilesetsPtr = reader.ReadUInt32();

        reader.AbsPosition = texPagesPtr;
        count += 1 + UndertaleSimpleResourcesList<UndertaleEmbeddedTexture, UndertaleChunkTXTR>
                     .UnserializeChildObjectCount(reader);

        reader.AbsPosition = spritesPtr;
        count += 1 + UndertaleSimpleResourcesList<UndertaleSprite, UndertaleChunkSPRT>
                     .UnserializeChildObjectCount(reader);

        reader.AbsPosition = spineSpritesPtr;
        count += 1 + UndertaleSimpleResourcesList<UndertaleSprite, UndertaleChunkSPRT>
                     .UnserializeChildObjectCount(reader);

        reader.AbsPosition = fontsPtr;
        count += 1 + UndertaleSimpleResourcesList<UndertaleFont, UndertaleChunkFONT>
                     .UnserializeChildObjectCount(reader);

        reader.AbsPosition = tilesetsPtr;
        count += 1 + UndertaleSimpleResourcesList<UndertaleBackground, UndertaleChunkBGND>
                     .UnserializeChildObjectCount(reader);

        return count;
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

        Name = null;
        TexturePages = null;
        Sprites = null;
        SpineSprites = null;
        Fonts = null;
        Tilesets = null;
    }
}