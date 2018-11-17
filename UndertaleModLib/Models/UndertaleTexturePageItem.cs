using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModLib.Models
{
    /**
     * The way this works is:
     * It renders in a box of size BoundingWidth x BoundingHeight at some position.
     * TargetX/Y/W/H is relative to the bounding box, anything outside of that is just transparent.
     * SourceX/Y/W/H is part of TexturePage that is drawn over TargetX/Y/W/H
     */
    public class UndertaleTexturePageItem : UndertaleResource, INotifyPropertyChanged
    {
        private ushort _SourceX;
        private ushort _SourceY;
        private ushort _SourceWidth;
        private ushort _SourceHeight;
        private ushort _TargetX;
        private ushort _TargetY;
        private ushort _TargetWidth;
        private ushort _TargetHeight;
        private ushort _BoundingWidth;
        private ushort _BoundingHeight;
        private UndertaleResourceById<UndertaleEmbeddedTexture, UndertaleChunkTXTR> _TexturePage = new UndertaleResourceById<UndertaleEmbeddedTexture, UndertaleChunkTXTR>();

        public ushort SourceX { get => _SourceX; set { _SourceX = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SourceX")); } }
        public ushort SourceY { get => _SourceY; set { _SourceY = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SourceY")); } }
        public ushort SourceWidth { get => _SourceWidth; set { _SourceWidth = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SourceWidth")); } }
        public ushort SourceHeight { get => _SourceHeight; set { _SourceHeight = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SourceHeight")); } }
        public ushort TargetX { get => _TargetX; set { _TargetX = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TargetX")); } }
        public ushort TargetY { get => _TargetY; set { _TargetY = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TargetY")); } }
        public ushort TargetWidth { get => _TargetWidth; set { _TargetWidth = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TargetWidth")); } }
        public ushort TargetHeight { get => _TargetHeight; set { _TargetHeight = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TargetHeight")); } }
        public ushort BoundingWidth { get => _BoundingWidth; set { _BoundingWidth = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("BoundingWidth")); } }
        public ushort BoundingHeight { get => _BoundingHeight; set { _BoundingHeight = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("BoundingHeight")); } }
        public UndertaleEmbeddedTexture TexturePage { get => _TexturePage.Resource; set { _TexturePage.Resource = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TexturePage")); } }

        public event PropertyChangedEventHandler PropertyChanged;

        public void Serialize(UndertaleWriter writer)
        {
            writer.Write(SourceX);
            writer.Write(SourceY);
            writer.Write(SourceWidth);
            writer.Write(SourceHeight);
            writer.Write(TargetX);
            writer.Write(TargetY);
            writer.Write(TargetWidth);
            writer.Write(TargetHeight);
            writer.Write(BoundingWidth);
            writer.Write(BoundingHeight);
            writer.Write((short)_TexturePage.SerializeById(writer));
        }

        public void Unserialize(UndertaleReader reader)
        {
            SourceX = reader.ReadUInt16();
            SourceY = reader.ReadUInt16();
            SourceWidth = reader.ReadUInt16();
            SourceHeight = reader.ReadUInt16();
            TargetX = reader.ReadUInt16();
            TargetY = reader.ReadUInt16();
            TargetWidth = reader.ReadUInt16();
            TargetHeight = reader.ReadUInt16();
            BoundingWidth = reader.ReadUInt16();
            BoundingHeight = reader.ReadUInt16();
            _TexturePage = new UndertaleResourceById<UndertaleEmbeddedTexture, UndertaleChunkTXTR>();
            _TexturePage.UnserializeById(reader, reader.ReadInt16()); // This one is special as it uses a short instead of int
        }

        public override string ToString()
        {
            return "(" + GetType().Name + ")";
        }
    }
}
