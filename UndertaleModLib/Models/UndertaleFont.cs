using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModLib.Models
{
    /// <summary>
    /// A font entry of a data file.
    /// </summary>
    [PropertyChanged.AddINotifyPropertyChangedInterface]
    public class UndertaleFont : UndertaleNamedResource
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
        /// The font size in Ems
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


        public ushort RangeStart { get; set; }
        public byte Charset { get; set; }
        public byte AntiAliasing { get; set; }
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
        /// The glyphs that this font uses.
        /// </summary>
        public UndertalePointerList<Glyph> Glyphs { get; private set; } = new UndertalePointerList<Glyph>();


        public int AscenderOffset { get; set; }


        /// <summary>
        /// Glyphs that a font can use.
        /// </summary>
        [PropertyChanged.AddINotifyPropertyChangedInterface]
        public class Glyph : UndertaleObject
        {
            /// <summary>
            /// The character for the glyph.
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
            /// The width of the glyph.
            /// </summary>
            public ushort SourceWidth { get; set; }

            /// <summary>
            /// The height of the glyph.
            /// </summary>
            public ushort SourceHeight { get; set; }


            public short Shift { get; set; }
            public short Offset { get; set; }
            public UndertaleSimpleListShort<GlyphKerning> Kerning { get; set; } = new UndertaleSimpleListShort<GlyphKerning>();

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

            public class GlyphKerning : UndertaleObject
            {
                public short Other;
                public short Amount;

                public void Serialize(UndertaleWriter writer)
                {
                    writer.Write(Other);
                    writer.Write(Amount);
                }

                public void Unserialize(UndertaleReader reader)
                {
                    Other = reader.ReadInt16();
                    Amount = reader.ReadInt16();
                }
            }
        }

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
            writer.WriteUndertaleObject(Glyphs);
        }

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
            uint jacksunknownvalue = reader.ReadUInt32();
            if (reader.ReadUInt32() < reader.Position)
            {
                // Might add a 2022.2+ variable with this as detection.
                // Also, should probably figure out what it does.
                reader.Position -= 4;
            }
            else
                reader.Position -= 8;
            Glyphs = reader.ReadUndertaleObject<UndertalePointerList<Glyph>>();
        }

        public override string ToString()
        {
            return Name.Content + " (" + GetType().Name + ")";
        }
    }
}
