using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using UndertaleModLib.Models;
using UndertaleModLib.Project.Json;
using UndertaleModLib.Util;

namespace UndertaleModLib.Project.SerializableAssets;

/// <summary>
/// A serializable version of <see cref="UndertaleFont"/>.
/// </summary>
internal sealed class SerializableFont : ISerializableTextureProjectAsset
{
    /// <inheritdoc/>
    public string DataName { get; set; }

    /// <inheritdoc cref="UndertaleFont.DisplayName"/>
    public string DisplayName { get; set; }

    /// <inheritdoc cref="UndertaleFont.EmSizeIsFloat"/>
    public bool EmSizeIsFloat { get; set; }

    /// <inheritdoc cref="UndertaleFont.EmSize"/>
    public float EmSize { get; set; }

    /// <inheritdoc cref="UndertaleFont.Bold"/>
    public bool Bold { get; set; }

    /// <inheritdoc cref="UndertaleFont.Italic"/>
    public bool Italic { get; set; }

    /// <inheritdoc cref="UndertaleFont.RangeStart"/>
    public ushort RangeStart { get; set; }

    /// <inheritdoc cref="UndertaleFont.Charset"/>
    public byte Charset { get; set; }

    /// <inheritdoc cref="UndertaleFont.AntiAliasing"/>
    public byte AntiAliasing { get; set; }

    /// <inheritdoc cref="UndertaleFont.RangeEnd"/>
    public uint RangeEnd { get; set; }

    /// <inheritdoc cref="UndertaleFont.ScaleX"/>
    public float ScaleX { get; set; }

    /// <inheritdoc cref="UndertaleFont.ScaleY"/>
    public float ScaleY { get; set; }

    /// <inheritdoc cref="UndertaleFont.Ascender"/>
    public uint Ascender { get; set; }

    /// <inheritdoc cref="UndertaleFont.SDFSpread"/>
    public uint SDFSpread { get; set; }

    /// <inheritdoc cref="UndertaleFont.LineHeight"/>
    public uint LineHeight { get; set; }

    /// <inheritdoc cref="UndertaleFont.AscenderOffset"/>
    public int AscenderOffset { get; set; }

    /// <inheritdoc cref="UndertaleFont.Glyphs"/>
    [JsonConverter(typeof(NoPrettyPrintJsonConverter<List<Glyph>>))]
    public List<Glyph> Glyphs { get; set; }

    /// <inheritdoc cref="UndertaleFont.Glyph"/>
    public sealed class Glyph
    {
        /// <inheritdoc cref="UndertaleFont.Glyph.Character"/>
        [JsonPropertyName("C")]
        public ushort Character { get; set; }

        /// <inheritdoc cref="UndertaleFont.Glyph.SourceX"/>
        [JsonPropertyName("X")]
        public ushort SourceX { get; set; }

        /// <inheritdoc cref="UndertaleFont.Glyph.SourceY"/>
        [JsonPropertyName("Y")]
        public ushort SourceY { get; set; }

        /// <inheritdoc cref="UndertaleFont.Glyph.SourceWidth"/>
        [JsonPropertyName("W")]
        public ushort SourceWidth { get; set; }

        /// <inheritdoc cref="UndertaleFont.Glyph.SourceHeight"/>
        [JsonPropertyName("H")]
        public ushort SourceHeight { get; set; }

        /// <inheritdoc cref="UndertaleFont.Glyph.Shift"/>
        [JsonPropertyName("S")]
        public short Shift { get; set; }

        /// <inheritdoc cref="UndertaleFont.Glyph.Offset"/>
        [JsonPropertyName("O")]
        public short Offset { get; set; }

        /// <inheritdoc cref="UndertaleFont.Glyph.Kerning"/>
        [JsonPropertyName("K")]
        public List<GlyphKerning> Kerning { get; set; }
    }

    /// <inheritdoc cref="UndertaleFont.Glyph.GlyphKerning"/>
    public sealed class GlyphKerning
    {
        /// <inheritdoc cref="UndertaleFont.Glyph.GlyphKerning.Character"/>
        [JsonPropertyName("C")]
        public short Character { get; set; }

        /// <inheritdoc cref="UndertaleFont.Glyph.GlyphKerning.ShiftModifier"/>
        [JsonPropertyName("S")]
        public short ShiftModifier { get; set; }
    }

    /// <inheritdoc/>
    [JsonIgnore]
    public SerializableAssetType AssetType => SerializableAssetType.Font;

    /// <inheritdoc/>
    [JsonIgnore]
    public bool IndividualDirectory => true;

    /// <inheritdoc/>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int OverrideOrder { get; set; }

    // Data asset that was located during pre-import.
    private UndertaleFont _dataAsset = null;

    // Texture image created for the asset during texture import.
    private UndertaleTexturePageItem _textureImage = null;

    /// <summary>
    /// Populates this serializable font with data from an actual font.
    /// </summary>
    internal void PopulateFromData(ProjectContext projectContext, UndertaleFont fnt)
    {
        // Update all main properties
        DataName = fnt.Name.Content;
        DisplayName = fnt.DisplayName?.Content;
        EmSizeIsFloat = fnt.EmSizeIsFloat;
        EmSize = fnt.EmSize;
        Bold = fnt.Bold;
        Italic = fnt.Italic;
        RangeStart = fnt.RangeStart;
        Charset = fnt.Charset;
        AntiAliasing = fnt.AntiAliasing;
        RangeEnd = fnt.RangeEnd;
        ScaleX = fnt.ScaleX;
        ScaleY = fnt.ScaleY;
        Ascender = fnt.Ascender;
        SDFSpread = fnt.SDFSpread;
        LineHeight = fnt.LineHeight;
        AscenderOffset = fnt.AscenderOffset;

        // Update glyphs
        Glyphs = new(fnt.Glyphs.Count);
        foreach (UndertaleFont.Glyph glyph in fnt.Glyphs)
        {
            Glyph newGlyph = new()
            {
                Character = glyph.Character,
                SourceX = glyph.SourceX,
                SourceY = glyph.SourceY,
                SourceWidth = glyph.SourceWidth,
                SourceHeight = glyph.SourceHeight,
                Shift = glyph.Shift,
                Offset = glyph.Offset,
                Kerning = new(glyph.Kerning.Count)
            };
            foreach (UndertaleFont.Glyph.GlyphKerning kerning in glyph.Kerning)
            {
                newGlyph.Kerning.Add(new()
                {
                    Character = kerning.Character,
                    ShiftModifier = kerning.ShiftModifier
                });
            }
            Glyphs.Add(newGlyph);
        }

        _dataAsset = fnt;
    }

    /// <inheritdoc/>
    public void Serialize(ProjectContext projectContext, string destinationFile)
    {
        // Write main JSON
        using FileStream fs = new(destinationFile, FileMode.Create);
        JsonSerializer.Serialize<ISerializableProjectAsset>(fs, this, ProjectContext.JsonOptions);

        // Write image as a PNG
        string filename = $"{Path.GetFileNameWithoutExtension(destinationFile)}.png";
        string directory = Path.GetDirectoryName(destinationFile);
        projectContext.TextureWorker.ExportAsPNG(_dataAsset.Texture, Path.Join(directory, filename), DataName, true);
    }

    /// <inheritdoc/>
    public void PreImport(ProjectContext projectContext)
    {
        if (projectContext.Data.Fonts.ByName(DataName) is UndertaleFont existing)
        {
            // Font found
            _dataAsset = existing;
        }
        else
        {
            // No font found; create new one
            _dataAsset = new()
            {
                Name = projectContext.MakeString(DataName)
            };
            projectContext.Data.Fonts.Add(_dataAsset);
        }
    }

    /// <inheritdoc/>
    public IProjectAsset Import(ProjectContext projectContext)
    {
        UndertaleFont fnt = _dataAsset;

        // Update all main properties
        fnt.DisplayName = projectContext.MakeString(DisplayName);
        fnt.EmSizeIsFloat = EmSizeIsFloat;
        fnt.EmSize = EmSize;
        fnt.Bold = Bold;
        fnt.Italic = Italic;
        fnt.RangeStart = RangeStart;
        fnt.Charset = Charset;
        fnt.AntiAliasing = AntiAliasing;
        fnt.RangeEnd = RangeEnd;
        fnt.ScaleX = ScaleX;
        fnt.ScaleY = ScaleY;
        fnt.Ascender = Ascender;
        fnt.SDFSpread = SDFSpread;
        fnt.LineHeight = LineHeight;
        fnt.AscenderOffset = AscenderOffset;

        // Update glyphs
        fnt.Glyphs = new(Glyphs.Count);
        foreach (Glyph glyph in Glyphs)
        {
            UndertaleFont.Glyph newGlyph = new()
            {
                Character = glyph.Character,
                SourceX = glyph.SourceX,
                SourceY = glyph.SourceY,
                SourceWidth = glyph.SourceWidth,
                SourceHeight = glyph.SourceHeight,
                Shift = glyph.Shift,
                Offset = glyph.Offset,
                Kerning = new(glyph.Kerning.Count)
            };
            foreach (GlyphKerning kerning in glyph.Kerning)
            {
                newGlyph.Kerning.Add(new()
                {
                    Character = kerning.Character,
                    ShiftModifier = kerning.ShiftModifier
                });
            }
            fnt.Glyphs.Add(newGlyph);
        }

        // Assign texture image
        fnt.Texture = _textureImage;

        return fnt;
    }

    /// <inheritdoc/>
    public void ImportTextures(ProjectContext projectContext, TextureGroupPacker texturePacker)
    {
        // Get JSON filename (of main asset file)
        if (!projectContext.AssetDataNamesToPaths.TryGetValue((DataName, AssetType), out string jsonFilename))
        {
            throw new ProjectException("Failed to get font asset path");
        }

        // TODO: support loading other file types as well
        // TODO: support texture groups (or separate pages)

        // Load PNG from disk, to be imported
        string filename = $"{Path.GetFileNameWithoutExtension(jsonFilename)}.png";
        string directory = Path.GetDirectoryName(jsonFilename);
        try
        {
            // Add image to packer
            _textureImage = texturePacker.AddImage(TextureWorker.ReadBGRAImageFromFile(Path.Join(directory, filename)), 
                                                   TextureGroupPacker.BorderFlags.Enabled | TextureGroupPacker.BorderFlags.ExtraBorder);
        }
        catch (Exception e)
        {
            throw new ProjectException($"Failed to import font PNG image file named \"{filename}\": {e.Message}", e);
        }
    }
}
