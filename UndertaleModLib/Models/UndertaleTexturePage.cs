using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModLib.Models
{
    /**
     * The way this works is:
     * It renders in a box of size BoundingWidth x BoundingHeight at some position.
     * TargetX/Y/W/H is relative to the bounding box, anything outside of that is just transparent.
     * SourceX/Y/W/H is part of SpritesheetId that is drawn over TargetX/Y/W/H
     */
    public class UndertaleTexturePage : UndertaleObject
    {
        public ushort SourceX { get; set; }
        public ushort SourceY { get; set; }
        public ushort SourceWidth { get; set; }
        public ushort SourceHeight { get; set; }
        public ushort TargetX { get; set; }
        public ushort TargetY { get; set; }
        public ushort TargetWidth { get; set; }
        public ushort TargetHeight { get; set; }
        public ushort BoundingWidth { get; set; }
        public ushort BoundingHeight { get; set; }
        public UndertaleResourceById<UndertaleEmbeddedTexture> SpritesheetId { get; } = new UndertaleResourceById<UndertaleEmbeddedTexture>("TXTR");

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
            writer.Write((short)SpritesheetId.Serialize(writer));
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
            SpritesheetId.Unserialize(reader, reader.ReadInt16());
        }

        public override string ToString()
        {
            return "(" + GetType().Name + ")";
        }
    }
}
