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
        private UndertaleTexturePage _TPagId;
        private float _ScaleX;
        private float _ScaleY;

        public UndertaleString Name { get => _Name; set { _Name = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Name")); } }
        public UndertaleString DisplayName { get => _DisplayName; set { _DisplayName = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("DisplayName")); } }
        public uint EmSize { get => _EmSize; set { _EmSize = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("EmSize")); } }
        public bool Bold { get => _Bold; set { _Bold = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Bold")); } }
        public bool Italic { get => _Italic; set { _Italic = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Italic")); } }
        public ushort RangeStart { get => _RangeStart; set { _RangeStart = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("RangeStart")); } }
        public byte Charset { get => _Charset; set { _Charset = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Charset")); } }
        public byte AntiAliasing { get => _AntiAliasing; set { _AntiAliasing = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("AntiAliasing")); } }
        public uint RangeEnd { get => _RangeEnd; set { _RangeEnd = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("RangeEnd")); } }
        public UndertaleTexturePage TPagId { get => _TPagId; set { _TPagId = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TPagId")); } }
        public float ScaleX { get => _ScaleX; set { _ScaleX = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ScaleX")); } }
        public float ScaleY { get => _ScaleY; set { _ScaleY = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ScaleY")); } }
        public UndertalePointerList<Glyph> Glyphs { get; private set; } = new UndertalePointerList<Glyph>();

        public event PropertyChangedEventHandler PropertyChanged;

        public class Glyph : UndertaleObject, INotifyPropertyChanged
        {
            private ushort _Character;
            private ushort _SourceX;
            private ushort _SourceY;
            private ushort _SourceWidth;
            private ushort _SourceHeight;
            private ushort _Shift;
            private uint _Offset;

            public ushort Character { get => _Character; set { _Character = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Character")); } }
            public ushort SourceX { get => _SourceX; set { _SourceX = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SourceX")); } }
            public ushort SourceY { get => _SourceY; set { _SourceY = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SourceY")); } }
            public ushort SourceWidth { get => _SourceWidth; set { _SourceWidth = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SourceWidth")); } }
            public ushort SourceHeight { get => _SourceHeight; set { _SourceHeight = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SourceHeight")); } }
            public ushort Shift { get => _Shift; set { _Shift = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Shift")); } }
            public uint Offset { get => _Offset; set { _Offset = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Offset")); } }

            public event PropertyChangedEventHandler PropertyChanged;

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
