using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModLib.Models
{
    public class UndertaleFont : UndertaleObject
    {
        public UndertaleString Name { get; set; }
        public UndertaleString DisplayName { get; set; }
        public uint EmSize { get; set; }
        public bool Bold { get; set; }
        public bool Italic { get; set; }
        public ushort RangeStart { get; set; }
        public byte Charset { get; set; }
        public byte AntiAliasing { get; set; }
        public uint RangeEnd { get; set; }
        public UndertaleTexturePage TPagId { get; set; }
        public float ScaleX { get; set; }
        public float ScaleY { get; set; }
        public UndertalePointerList<Glyph> Glyphs { get; private set; } = new UndertalePointerList<Glyph>();

        public class Glyph : UndertaleObject
        {
            public ushort Character { get; set; }
            public ushort SourceX { get; set; }
            public ushort SourceY { get; set; }
            public ushort SourceWidth { get; set; }
            public ushort SourceHeight { get; set; }
            public ushort Shift { get; set; }
            public uint Offset { get; set; }

            public void Serialize(UndertaleWriter writer)
            {
                writer.Write(Character);
                writer.Write(SourceX);
                writer.Write(SourceY);
                writer.Write(SourceWidth);
                writer.Write(SourceHeight);
                writer.Write(Shift);
                writer.Write(Offset);
            }

            public void Unserialize(UndertaleReader reader)
            {
                Character = reader.ReadUInt16();
                SourceX = reader.ReadUInt16();
                SourceY = reader.ReadUInt16();
                SourceWidth = reader.ReadUInt16();
                SourceHeight = reader.ReadUInt16();
                Shift = reader.ReadUInt16();
                Offset = reader.ReadUInt32();
            }
        }

        public void Serialize(UndertaleWriter writer)
        {
            writer.WriteUndertaleString(Name);
            writer.WriteUndertaleString(DisplayName);
            writer.Write(EmSize);
            writer.Write(Bold);
            writer.Write(Italic);
            writer.Write(RangeStart);
            writer.Write(Charset);
            writer.Write(AntiAliasing);
            writer.Write(RangeEnd);
            writer.WriteUndertaleObjectPointer(TPagId);
            writer.Write(ScaleX);
            writer.Write(ScaleY);
            writer.WriteUndertaleObject(Glyphs);
        }

        public void Unserialize(UndertaleReader reader)
        {
            Name = reader.ReadUndertaleString();
            DisplayName = reader.ReadUndertaleString();
            EmSize = reader.ReadUInt32();
            Bold = reader.ReadBoolean();
            Italic = reader.ReadBoolean();
            RangeStart = reader.ReadUInt16();
            Charset = reader.ReadByte();
            AntiAliasing = reader.ReadByte();
            RangeEnd = reader.ReadUInt32();
            TPagId = reader.ReadUndertaleObjectPointer<UndertaleTexturePage>();
            ScaleX = reader.ReadSingle();
            ScaleY = reader.ReadSingle();
            Glyphs = reader.ReadUndertaleObject<UndertalePointerList<Glyph>>();
        }

        public override string ToString()
        {
            return Name.Content + " (" + GetType().Name + ")";
        }
    }
}
