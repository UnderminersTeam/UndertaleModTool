using System;

namespace UndertaleModLib.Models;

/// <summary>
/// A font entry of a data file.
/// </summary>
[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertaleFont : UndertaleNamedResource, IDisposable
{
    /// <summary>
    /// The name of the font.
    /// </summary>
    public UndertaleString Name { get; set; }

    /// <summary>
    /// The display name of the font.
    /// </summary>
    public UndertaleString DisplayName { get; set; }

    /// <summary>
    /// Whether the Em size is a float.
    /// </summary>
    public bool EmSizeIsFloat { get; set; }

    /// <summary>
    /// The font size in Ems. In Game Maker: Studio 2.3 and above, this is a float instead.
    /// </summary>
    public uint EmSize { get; set; }

    /// <summary>
    /// Whether to display the font in bold.
    /// </summary>
    public bool Bold { get; set; }

    /// <summary>
    /// Whether to display the font in italics
    /// </summary>
    public bool Italic { get; set; }

    /// <summary>
    /// The start of the character range for this font.
    /// </summary>
    public ushort RangeStart { get; set; }

    /// <summary>
    /// TODO: Currently unknown value. Possibly related to ranges? (aka normal, ascii, digits, letters)
    /// </summary>
    public byte Charset { get; set; }

    /// <summary>
    /// The level of anti-aliasing that is applied. 0 for none, Game Maker: Studio 2 has 1 for <c>on</c>, while
    /// Game Maker Studio: 1 and earlier have values 1-3 for different anti-aliasing levels.
    /// </summary>
    public byte AntiAliasing { get; set; }

    /// <summary>
    /// The end of the character range for this font.
    /// </summary>
    public uint RangeEnd { get; set; }

    /// <summary>
    /// The <see cref="UndertaleTexturePageItem"/> object that contains the texture for this font.
    /// </summary>
    public UndertaleTexturePageItem Texture { get; set; }

    /// <summary>
    /// The x scale this font uses.
    /// </summary>
    public float ScaleX { get; set; }

    /// <summary>
    /// The y scale this font uses.
    /// </summary>
    public float ScaleY { get; set; }

    /// <summary>
    /// TODO: currently unknown, needs investigation. GMS2022.2 specific?
    /// </summary>
    public uint Ascender { get; set; }

    /// <summary>
    /// The glyphs that this font uses.
    /// </summary>
    public UndertalePointerList<Glyph> Glyphs { get; private set; } = new UndertalePointerList<Glyph>();

    /// <summary>
    /// TODO: currently unknown, needs investigation. Exists since bytecode 17, but seems to be only get checked since 2022.2+.
    /// </summary>
    public int AscenderOffset { get; set; }


    /// <summary>
    /// Glyphs that a font can use.
    /// </summary>
    [PropertyChanged.AddINotifyPropertyChangedInterface]
    public class Glyph : UndertaleObject, IDisposable
    {
        /// <summary>
        /// The code point of character for the glyph.
        /// </summary>
        public ushort Character { get; set; }

        /// <summary>
        /// The x position in the <see cref="UndertaleFont.Texture"/> where the glyph can be found.
        /// </summary>
        public ushort SourceX { get; set; }

        /// <summary>
        /// The y position in the <see cref="UndertaleFont.Texture"/> where the glyph can be found.
        /// </summary>
        public ushort SourceY { get; set; }

        /// <summary>
        /// The width of the glyph in pixels.
        /// </summary>
        public ushort SourceWidth { get; set; }

        /// <summary>
        /// The height of the glyph in pixels.
        /// </summary>
        public ushort SourceHeight { get; set; }


        /// <summary>
        /// TODO: something kerning related
        /// </summary>
        public short Shift { get; set; }

        /// <summary>
        /// TODO: something kerning related.
        /// </summary>
        public short Offset { get; set; }

        /// <summary>
        /// The kerning for each glyph.
        /// </summary>
        public UndertaleSimpleListShort<GlyphKerning> Kerning { get; set; } = new UndertaleSimpleListShort<GlyphKerning>();

        /// <inheritdoc />
        public void Serialize(UndertaleWriter writer)
        {
            writer.Write(Character);
            writer.Write(SourceX);
            writer.Write(SourceY);
            writer.Write(SourceWidth);
            writer.Write(SourceHeight);
            writer.Write(Shift);
            writer.Write(Offset);
            writer.WriteUndertaleObject(Kerning);
        }

        /// <inheritdoc />
        public void Unserialize(UndertaleReader reader)
        {
            Character = reader.ReadUInt16();
            SourceX = reader.ReadUInt16();
            SourceY = reader.ReadUInt16();
            SourceWidth = reader.ReadUInt16();
            SourceHeight = reader.ReadUInt16();
            Shift = reader.ReadInt16();
            Offset = reader.ReadInt16(); // potential assumption, see the conversation at https://github.com/krzys-h/UndertaleModTool/issues/40#issuecomment-440208912
            Kerning = reader.ReadUndertaleObject<UndertaleSimpleListShort<GlyphKerning>>();
        }

        /// <inheritdoc cref="UndertaleObject.UnserializeChildObjectCount(UndertaleReader)"/>
        public static uint UnserializeChildObjectCount(UndertaleReader reader)
        {
            reader.Position += 14;

            return 1 + UndertaleSimpleListShort<GlyphKerning>.UnserializeChildObjectCount(reader);
        }

        /// <summary>
        /// A class representing kerning for a glyph.
        /// </summary>
        public class GlyphKerning : UndertaleObject, IStaticChildObjectsSize
        {
            /// <inheritdoc cref="IStaticChildObjectsSize.ChildObjectsSize" />
            public static readonly uint ChildObjectsSize = 4;

            /// <summary>
            /// TODO: unknown?
            /// </summary>
            public short Other;

            /// <summary>
            /// TODO: unknown?
            /// </summary>
            public short Amount;

            /// <inheritdoc />
            public void Serialize(UndertaleWriter writer)
            {
                writer.Write(Other);
                writer.Write(Amount);
            }

            /// <inheritdoc />
            public void Unserialize(UndertaleReader reader)
            {
                Other = reader.ReadInt16();
                Amount = reader.ReadInt16();
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            GC.SuppressFinalize(this);

            Kerning = new();
        }
    }

    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        writer.WriteUndertaleString(Name);
        writer.WriteUndertaleString(DisplayName);
        if (EmSizeIsFloat)
        {
            // cast to a float and negate.
            writer.Write(0.0f - EmSize);
        }
        else
        {
            // pre-GMS2.3
            writer.Write(EmSize);
        }

        writer.Write(Bold);
        writer.Write(Italic);
        writer.Write(RangeStart);
        writer.Write(Charset);
        writer.Write(AntiAliasing);
        writer.Write(RangeEnd);
        writer.WriteUndertaleObjectPointer(Texture);
        writer.Write(ScaleX);
        writer.Write(ScaleY);
        if (writer.undertaleData.GeneralInfo?.BytecodeVersion >= 17)
            writer.Write(AscenderOffset);
        if (writer.undertaleData.IsVersionAtLeast(2022, 2))
            writer.Write(Ascender);
        writer.WriteUndertaleObject(Glyphs);
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        Name = reader.ReadUndertaleString();
        DisplayName = reader.ReadUndertaleString();
        EmSize = reader.ReadUInt32();
        EmSizeIsFloat = false;

        // since the float is always written negated, it has the first bit set.
        if ((EmSize & (1 << 31)) != 0)
        {
            float fsize = -BitConverter.ToSingle(BitConverter.GetBytes(EmSize), 0);
            EmSize = (uint)fsize;
            EmSizeIsFloat = true;
        }

        Bold = reader.ReadBoolean();
        Italic = reader.ReadBoolean();
        RangeStart = reader.ReadUInt16();
        Charset = reader.ReadByte();
        AntiAliasing = reader.ReadByte();
        RangeEnd = reader.ReadUInt32();
        Texture = reader.ReadUndertaleObjectPointer<UndertaleTexturePageItem>();
        ScaleX = reader.ReadSingle();
        ScaleY = reader.ReadSingle();
        if (reader.undertaleData.GeneralInfo?.BytecodeVersion >= 17)
            AscenderOffset = reader.ReadInt32();
        if (reader.undertaleData.IsVersionAtLeast(2022, 2))
            Ascender = reader.ReadUInt32();
        Glyphs = reader.ReadUndertaleObject<UndertalePointerList<Glyph>>();
    }

    /// <inheritdoc cref="UndertaleObject.UnserializeChildObjectCount(UndertaleReader)"/>
    public static uint UnserializeChildObjectCount(UndertaleReader reader)
    {
        int skipSize = 40;
        if (reader.undertaleData.GeneralInfo?.BytecodeVersion >= 17)
            skipSize += 4; // AscenderOffset
        if (reader.undertaleData.IsVersionAtLeast(2022, 2))
            skipSize += 4; // Ascender

        reader.Position += skipSize;

        return 1 + UndertalePointerList<Glyph>.UnserializeChildObjectCount(reader);
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

        foreach (Glyph glyph in Glyphs)
            glyph?.Dispose();
        Name = null;
        DisplayName = null;
        Texture = null;
        Glyphs = new();
    }
}