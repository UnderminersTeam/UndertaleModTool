using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModLib.Models
{
    public class UndertaleRoom : UndertaleObject
    {
        public UndertaleString Name { get; set; }
        public UndertaleString Caption { get; set; }
        public uint Width { get; set; }
        public uint Height { get; set; }
        public uint Speed { get; set; }
        public bool Persistent { get; set; }
        public uint Argb { get; set; }
        public bool DrawBGColor { get; set; }
        public int Unknown { get; set; }
        [Flags]
        public enum RoomEntryFlags : uint
        {
            EnableViews = 1,
            ShowColor = 2,
            ClearDisplayBuffer = 4
        }
        public RoomEntryFlags Flags { get; set; }
        //public UndertaleObject BgOffset;
        //public UndertaleObject ViewOffset;
        //public UndertaleObject ObjOffset;
        //public UndertaleObject TileOffset;
        public uint World { get; set; }
        public uint Top { get; set; }
        public uint Left { get; set; }
        public uint Right { get; set; }
        public uint Bottom { get; set; }
        public float GravityX { get; set; }
        public float GravityY { get; set; }
        public float MetersPerPixel { get; set; }
        public UndertalePointerList<Background> Backgrounds { get; private set; } = new UndertalePointerList<Background>();
        public UndertalePointerList<View> Views { get; private set; } = new UndertalePointerList<View>();
        public UndertalePointerList<GameObject> GameObjects { get; private set; } = new UndertalePointerList<GameObject>();
        public UndertalePointerList<Tile> Tiles { get; private set; } = new UndertalePointerList<Tile>();

        public void Serialize(UndertaleWriter writer)
        {
            writer.WriteUndertaleString(Name);
            writer.WriteUndertaleString(Caption);
            writer.Write(Width);
            writer.Write(Height);
            writer.Write(Speed);
            writer.Write(Persistent);
            writer.Write(Argb);
            writer.Write(DrawBGColor);
            writer.Write(Unknown);
            writer.Write((uint)Flags);
            writer.WriteUndertaleObjectPointer(Backgrounds);
            writer.WriteUndertaleObjectPointer(Views);
            writer.WriteUndertaleObjectPointer(GameObjects);
            writer.WriteUndertaleObjectPointer(Tiles);
            writer.Write(World);
            writer.Write(Top);
            writer.Write(Left);
            writer.Write(Right);
            writer.Write(Bottom);
            writer.Write(GravityX);
            writer.Write(GravityY);
            writer.Write(MetersPerPixel);
            writer.WriteUndertaleObject(Backgrounds);
            writer.WriteUndertaleObject(Views);
            writer.WriteUndertaleObject(GameObjects);
            writer.WriteUndertaleObject(Tiles);
        }

        public void Unserialize(UndertaleReader reader)
        {
            Name = reader.ReadUndertaleString();
            Caption = reader.ReadUndertaleString();
            Width = reader.ReadUInt32();
            Height = reader.ReadUInt32();
            Speed = reader.ReadUInt32();
            Persistent = reader.ReadBoolean();
            Argb = reader.ReadUInt32();
            DrawBGColor = reader.ReadBoolean();
            Unknown = reader.ReadInt32();
            Flags = (RoomEntryFlags)reader.ReadUInt32();
            Backgrounds = reader.ReadUndertaleObjectPointer<UndertalePointerList<Background>>();
            Views = reader.ReadUndertaleObjectPointer<UndertalePointerList<View>>();
            GameObjects = reader.ReadUndertaleObjectPointer<UndertalePointerList<GameObject>>();
            Tiles = reader.ReadUndertaleObjectPointer<UndertalePointerList<Tile>>();
            World = reader.ReadUInt32();
            Top = reader.ReadUInt32();
            Left = reader.ReadUInt32();
            Right = reader.ReadUInt32();
            Bottom = reader.ReadUInt32();
            GravityX = reader.ReadSingle();
            GravityY = reader.ReadSingle();
            MetersPerPixel = reader.ReadSingle();
            //reader.ReadUInt32();//
            if (reader.ReadUndertaleObject<UndertalePointerList<Background>>() != Backgrounds)
                throw new IOException();
            if (reader.ReadUndertaleObject<UndertalePointerList<View>>() != Views)
                throw new IOException();
            if (reader.ReadUndertaleObject<UndertalePointerList<GameObject>>() != GameObjects)
                throw new IOException();
            if (reader.ReadUndertaleObject<UndertalePointerList<Tile>>() != Tiles)
                throw new IOException();
        }

        public override string ToString()
        {
            return Name.Content + " (" + GetType().Name + ")";
        }

        public class Background : UndertaleObject
        {
            public bool Enabled { get; set; }
            public bool Foreground { get; set; }
            public UndertaleResourceById<UndertaleBackground> BgDefIndex { get; } = new UndertaleResourceById<UndertaleBackground>("BGND");
            public uint X { get; set; }
            public uint Y { get; set; }
            public uint TileX { get; set; }
            public uint TileY { get; set; }
            public int SpeedX { get; set; }
            public int SpeedY { get; set; }
            public int ObjectId { get; set; } //?

            public void Serialize(UndertaleWriter writer)
            {
                writer.Write(Enabled);
                writer.Write(Foreground);
                writer.Write(BgDefIndex.Serialize(writer));
                writer.Write(X);
                writer.Write(Y);
                writer.Write(TileX);
                writer.Write(TileY);
                writer.Write(SpeedX);
                writer.Write(SpeedY);
                writer.Write(ObjectId);
            }

            public void Unserialize(UndertaleReader reader)
            {
                Enabled = reader.ReadBoolean();
                Foreground = reader.ReadBoolean();
                BgDefIndex.Unserialize(reader, reader.ReadInt32());
                X = reader.ReadUInt32();
                Y = reader.ReadUInt32();
                TileX = reader.ReadUInt32();
                TileY = reader.ReadUInt32();
                SpeedX = reader.ReadInt32();
                SpeedY = reader.ReadInt32();
                ObjectId = reader.ReadInt32();
            }
        }

        public class View : UndertaleObject
        {
            public bool Enabled { get; set; }
            public int ViewX { get; set; }
            public int ViewY { get; set; }
            public int ViewWidth { get; set; }
            public int ViewHeight { get; set; }
            public int PortX { get; set; }
            public int PortY { get; set; }
            public int PortWidth { get; set; }
            public int PortHeight { get; set; }
            public uint BorderX { get; set; }
            public uint BorderY { get; set; }
            public int SpeedX { get; set; }
            public int SpeedY { get; set; }
            public int ObjectId { get; set; } //?

            public void Serialize(UndertaleWriter writer)
            {
                writer.Write(Enabled);
                writer.Write(ViewX);
                writer.Write(ViewY);
                writer.Write(ViewWidth);
                writer.Write(ViewHeight);
                writer.Write(PortX);
                writer.Write(PortY);
                writer.Write(PortWidth);
                writer.Write(PortHeight);
                writer.Write(BorderX);
                writer.Write(BorderY);
                writer.Write(SpeedX);
                writer.Write(SpeedY);
                writer.Write(ObjectId);
            }

            public void Unserialize(UndertaleReader reader)
            {
                Enabled = reader.ReadBoolean();
                ViewX = reader.ReadInt32();
                ViewY = reader.ReadInt32();
                ViewWidth = reader.ReadInt32();
                ViewHeight = reader.ReadInt32();
                PortX = reader.ReadInt32();
                PortY = reader.ReadInt32();
                PortWidth = reader.ReadInt32();
                PortHeight = reader.ReadInt32();
                BorderX = reader.ReadUInt32();
                BorderY = reader.ReadUInt32();
                SpeedX = reader.ReadInt32();
                SpeedY = reader.ReadInt32();
                ObjectId = reader.ReadInt32();
            }
        }

        public class GameObject : UndertaleObject
        {
            public int X { get; set; }
            public int Y { get; set; }
            public UndertaleResourceById<UndertaleGameObject> ObjDefIndex { get; } = new UndertaleResourceById<UndertaleGameObject>("OBJT");
            public uint InstanceID { get; set; }
            public int CreationCodeID { get; set; } // gml_RoomCC_<name>_<CreationCodeID> (TODO: damn, we need a 'by name' thing now)
            public float ScaleX { get; set; }
            public float ScaleY { get; set; }
            public uint ArgbTint { get; set; }
            public float Rotation { get; set; }
            public int Unknown { get; set; }

            public void Serialize(UndertaleWriter writer)
            {
                writer.Write(X);
                writer.Write(Y);
                writer.Write(ObjDefIndex.Serialize(writer));
                writer.Write(InstanceID);
                writer.Write(CreationCodeID);
                writer.Write(ScaleX);
                writer.Write(ScaleY);
                writer.Write(ArgbTint);
                writer.Write(Rotation);
                writer.Write(Unknown);
            }

            public void Unserialize(UndertaleReader reader)
            {
                X = reader.ReadInt32();
                Y = reader.ReadInt32();
                ObjDefIndex.Unserialize(reader, reader.ReadInt32());
                InstanceID = reader.ReadUInt32();
                CreationCodeID = reader.ReadInt32();
                ScaleX = reader.ReadSingle();
                ScaleY = reader.ReadSingle();
                ArgbTint = reader.ReadUInt32();
                Rotation = reader.ReadSingle();
                Unknown = reader.ReadInt32();
            }
        }

        public class Tile : UndertaleObject
        {
            public int X { get; set; }
            public int Y { get; set; }
            public UndertaleResourceById<UndertaleBackground> BgDefIndex { get; } = new UndertaleResourceById<UndertaleBackground>("BGND");
            public uint SourceX { get; set; }
            public uint SourceY { get; set; }
            public uint Width { get; set; }
            public uint Height { get; set; }
            public int TileDepth { get; set; }
            public int InstanceID { get; set; }
            public float ScaleX { get; set; }
            public float ScaleY { get; set; }
            public uint ArgbTint { get; set; }

            public void Serialize(UndertaleWriter writer)
            {
                writer.Write(X);
                writer.Write(Y);
                writer.Write(BgDefIndex.Serialize(writer));
                writer.Write(SourceX);
                writer.Write(SourceY);
                writer.Write(Width);
                writer.Write(Height);
                writer.Write(TileDepth);
                writer.Write(InstanceID);
                writer.Write(ScaleX);
                writer.Write(ScaleY);
                writer.Write(ArgbTint);
            }

            public void Unserialize(UndertaleReader reader)
            {
                X = reader.ReadInt32();
                Y = reader.ReadInt32();
                BgDefIndex.Unserialize(reader, reader.ReadInt32());
                SourceX = reader.ReadUInt32();
                SourceY = reader.ReadUInt32();
                Width = reader.ReadUInt32();
                Height = reader.ReadUInt32();
                TileDepth = reader.ReadInt32();
                InstanceID = reader.ReadInt32();
                ScaleX = reader.ReadSingle();
                ScaleY = reader.ReadSingle();
                ArgbTint = reader.ReadUInt32();
            }
        }
    }
}
