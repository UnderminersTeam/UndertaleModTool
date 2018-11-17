using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModLib.Models
{
    public class UndertaleFont : UndertaleNamedResource, INotifyPropertyChanged
    {
        private UndertaleString _Name;
        private UndertaleString _DisplayName;
        private uint _EmSize;
        private bool _Bold;
        private bool _Italic;
        private ushort _RangeStart;
        private byte _Charset;
        private byte _AntiAliasing;
        private uint _RangeEnd;
        private UndertaleTexturePageItem _Texture;
        private float _ScaleX;
        private float _ScaleY;
        private int _AscenderOffset;

        public UndertaleString Name { get => _Name; set { _Name = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Name")); } }
        public UndertaleString DisplayName { get => _DisplayName; set { _DisplayName = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("DisplayName")); } }
        public uint EmSize { get => _EmSize; set { _EmSize = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("EmSize")); } }
        public bool Bold { get => _Bold; set { _Bold = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Bold")); } }
        public bool Italic { get => _Italic; set { _Italic = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Italic")); } }
        public ushort RangeStart { get => _RangeStart; set { _RangeStart = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("RangeStart")); } }
        public byte Charset { get => _Charset; set { _Charset = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Charset")); } }
        public byte AntiAliasing { get => _AntiAliasing; set { _AntiAliasing = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("AntiAliasing")); } }
        public uint RangeEnd { get => _RangeEnd; set { _RangeEnd = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("RangeEnd")); } }
        public UndertaleTexturePageItem Texture { get => _Texture; set { _Texture = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Texture")); } }
        public float ScaleX { get => _ScaleX; set { _ScaleX = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ScaleX")); } }
        public float ScaleY { get => _ScaleY; set { _ScaleY = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ScaleY")); } }
        public UndertalePointerList<Glyph> Glyphs { get; private set; } = new UndertalePointerList<Glyph>();
        public int AscenderOffset { get => _AscenderOffset; set { _AscenderOffset = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("AscenderOffset")); } }

        public event PropertyChangedEventHandler PropertyChanged;

        public class Glyph : UndertaleObject, INotifyPropertyChanged
        {
            private ushort _Character;
            private ushort _SourceX;
            private ushort _SourceY;
            private ushort _SourceWidth;
            private ushort _SourceHeight;
            private short _Shift;
            private int _Offset;
            private UndertaleSimpleListShort<GlyphKerning> _Kerning;

            public ushort Character { get => _Character; set { _Character = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Character")); } }
            public ushort SourceX { get => _SourceX; set { _SourceX = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SourceX")); } }
            public ushort SourceY { get => _SourceY; set { _SourceY = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SourceY")); } }
            public ushort SourceWidth { get => _SourceWidth; set { _SourceWidth = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SourceWidth")); } }
            public ushort SourceHeight { get => _SourceHeight; set { _SourceHeight = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SourceHeight")); } }
            public short Shift { get => _Shift; set { _Shift = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Shift")); } }
            public int Offset { get => _Offset; set { _Offset = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Offset")); } }
            public UndertaleSimpleListShort<GlyphKerning> Kerning { get => _Kerning; set { _Kerning = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Kerning")); } }

            public event PropertyChangedEventHandler PropertyChanged;

            public void Serialize(UndertaleWriter writer)
            {
                writer.Write(Character);
                writer.Write(SourceX);
                writer.Write(SourceY);
                writer.Write(SourceWidth);
                writer.Write(SourceHeight);
                writer.Write(Shift);
                if (writer.undertaleData.GeneralInfo?.Major >= 2 || (writer.undertaleData.GeneralInfo?.Major == 1 && writer.undertaleData.GeneralInfo?.Build == 9999))
                {
                    writer.Write((short)Offset);
                    writer.WriteUndertaleObject(Kerning);
                } else
                {
                    writer.Write(Offset);
                }
            }

            public void Unserialize(UndertaleReader reader)
            {
                Character = reader.ReadUInt16();
                SourceX = reader.ReadUInt16();
                SourceY = reader.ReadUInt16();
                SourceWidth = reader.ReadUInt16();
                SourceHeight = reader.ReadUInt16();
                Shift = reader.ReadInt16();
                if (reader.undertaleData.GeneralInfo?.Major >= 2 || (reader.undertaleData.GeneralInfo?.Major == 1 && reader.undertaleData.GeneralInfo?.Build == 9999))
                {
                    Offset = reader.ReadInt16();
                    Kerning = reader.ReadUndertaleObject<UndertaleSimpleListShort<GlyphKerning>>();
                } else
                {
                    Offset = reader.ReadInt32(); // Maybe? I don't really know, but this definitely used to work
                }
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
            writer.Write(EmSize);
            writer.Write(Bold);
            writer.Write(Italic);
            writer.Write(RangeStart);
            writer.Write(Charset);
            writer.Write(AntiAliasing);
            writer.Write(RangeEnd);
            writer.WriteUndertaleObjectPointer(Texture);
            writer.Write(ScaleX);
            writer.Write(ScaleY);
            if (writer.undertaleData.GeneralInfo?.Major >= 2 || (writer.undertaleData.GeneralInfo?.Major == 1 && writer.undertaleData.GeneralInfo?.Build == 9999))
                writer.Write(AscenderOffset);
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
            Texture = reader.ReadUndertaleObjectPointer<UndertaleTexturePageItem>();
            ScaleX = reader.ReadSingle();
            ScaleY = reader.ReadSingle();
            if (reader.undertaleData.GeneralInfo?.Major >= 2 && reader.undertaleData.GeneralInfo?.BytecodeVersion >= 17)
                AscenderOffset = reader.ReadInt32();
            Glyphs = reader.ReadUndertaleObject<UndertalePointerList<Glyph>>();
        }

        public override string ToString()
        {
            return Name.Content + " (" + GetType().Name + ")";
        }
    }
}
