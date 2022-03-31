using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModLib.Models
{
    [PropertyChanged.AddINotifyPropertyChangedInterface]
    public class UndertaleFont : UndertaleNamedResource
    {
        public UndertaleString Name { get; set; }
        public UndertaleString DisplayName { get; set; }
        public bool EmSizeIsFloat { get; set; }
        public uint EmSize { get; set; }
        public bool Bold { get; set; }
        public bool Italic { get; set; }
        public ushort RangeStart { get; set; }
        public byte Charset { get; set; }
        public byte AntiAliasing { get; set; }
        public uint RangeEnd { get; set; }
        public UndertaleTexturePageItem Texture { get; set; }
        public float ScaleX { get; set; }
        public float ScaleY { get; set; }
        public uint Ascender { get; set; }
        public UndertalePointerList<Glyph> Glyphs { get; private set; } = new UndertalePointerList<Glyph>();
        public int AscenderOffset { get; set; }

        [PropertyChanged.AddINotifyPropertyChangedInterface]
        public class Glyph : UndertaleObject
        {
            public ushort Character { get; set; }
            public ushort SourceX { get; set; }
            public ushort SourceY { get; set; }
            public ushort SourceWidth { get; set; }
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
            if (writer.undertaleData.GMS2022_2)
                writer.Write(Ascender);
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
            {
                AscenderOffset = reader.ReadInt32();
                // TODO: Add a second check so it doesn't iterate over every font twice (just the first should do)
                if (!reader.undertaleData.GMS2022_2)
                {
                    /* This code performs three checks to identify GM2022.2.
                     * First, as you've seen, is the bytecode version.
                     * Second, we assume it is. If there are no Glyphs, we are vindicated by the impossibility of null values there.
                     * Third, in case of a terrible fluke causing this to appear valid erroneously, we verify that each pointer leads into the next.
                     * And if someone builds their game so the first pointer is absolutely valid length data and the next font is valid glyph data-
                     * screw it, call Jacky720 when someone constructs that and you want to mod it.
                     * Maybe try..catch on the whole shebang?
                     */
                    uint positionToReturn = reader.Position;
                    reader.ReadUInt32(); // We assume this is the ascender
                    uint glyphsLength = reader.ReadUInt32();
                    reader.undertaleData.GMS2022_2 = true;
                    if (glyphsLength != 0)
                    {
                        List<uint> glyphPointers = new List<uint>();
                        for (uint i = 0; i < glyphsLength; i++)
                            glyphPointers.Add(reader.ReadUInt32());
                        foreach (uint pointer in glyphPointers)
                        {
                            if (reader.Position != pointer)
                            {
                                reader.undertaleData.GMS2022_2 = false;
                                break;
                            }
                            reader.Position += 14;
                            ushort kerningLength = reader.ReadUInt16();
                            reader.Position += (uint)4 * kerningLength; // combining read/write would apparently break
                        }
                    }
                    reader.Position = positionToReturn;
                }
            }
            if (reader.undertaleData.GMS2022_2)
                Ascender = reader.ReadUInt32();
            Glyphs = reader.ReadUndertaleObject<UndertalePointerList<Glyph>>();
        }

        public override string ToString()
        {
            return Name.Content + " (" + GetType().Name + ")";
        }
    }
}
