using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModLib.Models
{
    public class UndertaleRoom : UndertaleNamedResource, INotifyPropertyChanged
    {
        [Flags]
        public enum RoomEntryFlags : uint
        {
            EnableViews = 1,
            ShowColor = 2,
            ClearDisplayBuffer = 4
        }

        private UndertaleString _Name;
        private UndertaleString _Caption;
        private uint _Width;
        private uint _Height;
        private uint _Speed;
        private bool _Persistent;
        private uint _BackgroundColor;
        private bool _DrawBackgroundColor;
        private int _Unknown;
        private RoomEntryFlags _Flags;
        private uint _World;
        private uint _Top;
        private uint _Left;
        private uint _Right;
        private uint _Bottom;
        private float _GravityX;
        private float _GravityY;
        private float _MetersPerPixel;

        public UndertaleString Name { get => _Name; set { _Name = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Name")); } }
        public UndertaleString Caption { get => _Caption; set { _Caption = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Caption")); } }
        public uint Width { get => _Width; set { _Width = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Width")); } }
        public uint Height { get => _Height; set { _Height = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Height")); } }
        public uint Speed { get => _Speed; set { _Speed = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Speed")); } }
        public bool Persistent { get => _Persistent; set { _Persistent = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Persistent")); } }
        public uint BackgroundColor { get => _BackgroundColor; set { _BackgroundColor = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("BackgroundColor")); } }
        public bool DrawBackgroundColor { get => _DrawBackgroundColor; set { _DrawBackgroundColor = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("DrawBackgroundColor")); } }
        public int Unknown { get => _Unknown; set { _Unknown = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Unknown")); } }
        public RoomEntryFlags Flags { get => _Flags; set { _Flags = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Flags")); } }
        public uint World { get => _World; set { _World = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("World")); } }
        public uint Top { get => _Top; set { _Top = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Top")); } }
        public uint Left { get => _Left; set { _Left = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Left")); } }
        public uint Right { get => _Right; set { _Right = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Right")); } }
        public uint Bottom { get => _Bottom; set { _Bottom = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Bottom")); } }
        public float GravityX { get => _GravityX; set { _GravityX = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("GravityX")); } }
        public float GravityY { get => _GravityY; set { _GravityY = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("GravityY")); } }
        public float MetersPerPixel { get => _MetersPerPixel; set { _MetersPerPixel = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("MetersPerPixel")); } }
        public UndertalePointerList<Background> Backgrounds { get; private set; } = new UndertalePointerList<Background>();
        public UndertalePointerList<View> Views { get; private set; } = new UndertalePointerList<View>();
        public UndertalePointerList<GameObject> GameObjects { get; private set; } = new UndertalePointerList<GameObject>();
        public UndertalePointerList<Tile> Tiles { get; private set; } = new UndertalePointerList<Tile>();

        public event PropertyChangedEventHandler PropertyChanged;

        public void Serialize(UndertaleWriter writer)
        {
            writer.WriteUndertaleString(Name);
            writer.WriteUndertaleString(Caption);
            writer.Write(Width);
            writer.Write(Height);
            writer.Write(Speed);
            writer.Write(Persistent);
            writer.Write(BackgroundColor);
            writer.Write(DrawBackgroundColor);
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
            BackgroundColor = reader.ReadUInt32();
            DrawBackgroundColor = reader.ReadBoolean();
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

        public class Background : UndertaleObject, INotifyPropertyChanged
        {
            private bool _Enabled;
            private bool _Foreground;
            private UndertaleResourceById<UndertaleBackground> _BackgroundDefinition { get; } = new UndertaleResourceById<UndertaleBackground>("BGND");
            private uint _X;
            private uint _Y;
            private uint _TileX;
            private uint _TileY;
            private int _SpeedX;
            private int _SpeedY;
            private UndertaleResourceById<UndertaleGameObject> _ObjectId { get; } = new UndertaleResourceById<UndertaleGameObject>("OBJT");

            public bool Enabled { get => _Enabled; set { _Enabled = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Enabled")); } }
            public bool Foreground { get => _Foreground; set { _Foreground = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Foreground")); } }
            public UndertaleBackground BackgroundDefinition { get => _BackgroundDefinition.Resource; set { _BackgroundDefinition.Resource = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("BackgroundDefinition")); } }
            public uint X { get => _X; set { _X = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("X")); } }
            public uint Y { get => _Y; set { _Y = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Y")); } }
            public uint TileX { get => _TileX; set { _TileX = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TileX")); } }
            public uint TileY { get => _TileY; set { _TileY = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TileY")); } }
            public int SpeedX { get => _SpeedX; set { _SpeedX = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SpeedX")); } }
            public int SpeedY { get => _SpeedY; set { _SpeedY = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SpeedY")); } }
            public UndertaleGameObject ObjectId { get => _ObjectId.Resource; set { _ObjectId.Resource = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ObjectId")); } }

            public event PropertyChangedEventHandler PropertyChanged;

            public void Serialize(UndertaleWriter writer)
            {
                writer.Write(Enabled);
                writer.Write(Foreground);
                writer.Write(_BackgroundDefinition.Serialize(writer));
                writer.Write(X);
                writer.Write(Y);
                writer.Write(TileX);
                writer.Write(TileY);
                writer.Write(SpeedX);
                writer.Write(SpeedY);
                writer.Write(_ObjectId.Serialize(writer));
            }

            public void Unserialize(UndertaleReader reader)
            {
                Enabled = reader.ReadBoolean();
                Foreground = reader.ReadBoolean();
                _BackgroundDefinition.Unserialize(reader, reader.ReadInt32());
                X = reader.ReadUInt32();
                Y = reader.ReadUInt32();
                TileX = reader.ReadUInt32();
                TileY = reader.ReadUInt32();
                SpeedX = reader.ReadInt32();
                SpeedY = reader.ReadInt32();
                _ObjectId.Unserialize(reader, reader.ReadInt32());
            }
        }

        public class View : UndertaleObject, INotifyPropertyChanged
        {
            private bool _Enabled;
            private int _ViewX;
            private int _ViewY;
            private int _ViewWidth;
            private int _ViewHeight;
            private int _PortX;
            private int _PortY;
            private int _PortWidth;
            private int _PortHeight;
            private uint _BorderX;
            private uint _BorderY;
            private int _SpeedX;
            private int _SpeedY;
            private UndertaleResourceById<UndertaleGameObject> _ObjectId { get; } = new UndertaleResourceById<UndertaleGameObject>("OBJT");

            public bool Enabled { get => _Enabled; set { _Enabled = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Enabled")); } }
            public int ViewX { get => _ViewX; set { _ViewX = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ViewX")); } }
            public int ViewY { get => _ViewY; set { _ViewY = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ViewY")); } }
            public int ViewWidth { get => _ViewWidth; set { _ViewWidth = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ViewWidth")); } }
            public int ViewHeight { get => _ViewHeight; set { _ViewHeight = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ViewHeight")); } }
            public int PortX { get => _PortX; set { _PortX = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("PortX")); } }
            public int PortY { get => _PortY; set { _PortY = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("PortY")); } }
            public int PortWidth { get => _PortWidth; set { _PortWidth = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("PortWidth")); } }
            public int PortHeight { get => _PortHeight; set { _PortHeight = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("PortHeight")); } }
            public uint BorderX { get => _BorderX; set { _BorderX = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("BorderX")); } }
            public uint BorderY { get => _BorderY; set { _BorderY = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("BorderY")); } }
            public int SpeedX { get => _SpeedX; set { _SpeedX = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SpeedX")); } }
            public int SpeedY { get => _SpeedY; set { _SpeedY = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SpeedY")); } }
            public UndertaleGameObject ObjectId { get => _ObjectId.Resource; set { _ObjectId.Resource = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ObjectId")); } }

            public event PropertyChangedEventHandler PropertyChanged;

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
                writer.Write(_ObjectId.Serialize(writer));
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
                _ObjectId.Unserialize(reader, reader.ReadInt32());
            }
        }

        public class GameObject : UndertaleObject, INotifyPropertyChanged
        {
            private int _X;
            private int _Y;
            private UndertaleResourceById<UndertaleGameObject> _ObjectDefinition { get; } = new UndertaleResourceById<UndertaleGameObject>("OBJT");
            private uint _InstanceID;
            private UndertaleResourceById<UndertaleCode> _CreationCode { get; } = new UndertaleResourceById<UndertaleCode>("CODE");
            private float _ScaleX;
            private float _ScaleY;
            private uint _Color;
            private float _Rotation;
            private int _Unknown;

            public int X { get => _X; set { _X = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("X")); } }
            public int Y { get => _Y; set { _Y = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Y")); } }
            public UndertaleGameObject ObjectDefinition { get => _ObjectDefinition.Resource; set { _ObjectDefinition.Resource = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ObjectDefinition")); } }
            public uint InstanceID { get => _InstanceID; set { _InstanceID = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("InstanceID")); } }
            public UndertaleCode CreationCode { get => _CreationCode.Resource; set { _CreationCode.Resource = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CreationCode")); } }
            public float ScaleX { get => _ScaleX; set { _ScaleX = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ScaleX")); } }
            public float ScaleY { get => _ScaleY; set { _ScaleY = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ScaleY")); } }
            public uint Color { get => _Color; set { _Color = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Color")); } }
            public float Rotation { get => _Rotation; set { _Rotation = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Rotation")); } }
            public int Unknown { get => _Unknown; set { _Unknown = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Unknown")); } }

            public event PropertyChangedEventHandler PropertyChanged;

            public void Serialize(UndertaleWriter writer)
            {
                writer.Write(X);
                writer.Write(Y);
                writer.Write(_ObjectDefinition.Serialize(writer));
                writer.Write(InstanceID);
                writer.Write(_CreationCode.Serialize(writer));
                writer.Write(ScaleX);
                writer.Write(ScaleY);
                writer.Write(Color);
                writer.Write(Rotation);
                writer.Write(Unknown);
            }

            public void Unserialize(UndertaleReader reader)
            {
                X = reader.ReadInt32();
                Y = reader.ReadInt32();
                _ObjectDefinition.Unserialize(reader, reader.ReadInt32());
                InstanceID = reader.ReadUInt32();
                _CreationCode.Unserialize(reader, reader.ReadInt32());
                ScaleX = reader.ReadSingle();
                ScaleY = reader.ReadSingle();
                Color = reader.ReadUInt32();
                Rotation = reader.ReadSingle();
                Unknown = reader.ReadInt32();
            }
        }

        public class Tile : UndertaleObject, INotifyPropertyChanged
        {
            private int _X;
            private int _Y;
            private UndertaleResourceById<UndertaleBackground> _BackgroundDefinition { get; } = new UndertaleResourceById<UndertaleBackground>("BGND");
            private uint _SourceX;
            private uint _SourceY;
            private uint _Width;
            private uint _Height;
            private int _TileDepth;
            private uint _InstanceID;
            private float _ScaleX;
            private float _ScaleY;
            private uint _Color;

            public int X { get => _X; set { _X = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("X")); } }
            public int Y { get => _Y; set { _Y = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Y")); } }
            public UndertaleBackground BackgroundDefinition { get => _BackgroundDefinition.Resource; set { _BackgroundDefinition.Resource = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("BackgroundDefinition")); } }
            public uint SourceX { get => _SourceX; set { _SourceX = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SourceX")); } }
            public uint SourceY { get => _SourceY; set { _SourceY = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SourceY")); } }
            public uint Width { get => _Width; set { _Width = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Width")); } }
            public uint Height { get => _Height; set { _Height = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Height")); } }
            public int TileDepth { get => _TileDepth; set { _TileDepth = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TileDepth")); } }
            public uint InstanceID { get => _InstanceID; set { _InstanceID = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("InstanceID")); } }
            public float ScaleX { get => _ScaleX; set { _ScaleX = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ScaleX")); } }
            public float ScaleY { get => _ScaleY; set { _ScaleY = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ScaleY")); } }
            public uint Color { get => _Color; set { _Color = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Color")); } }

            public event PropertyChangedEventHandler PropertyChanged;
            
            public void Serialize(UndertaleWriter writer)
            {
                writer.Write(X);
                writer.Write(Y);
                writer.Write(_BackgroundDefinition.Serialize(writer));
                writer.Write(SourceX);
                writer.Write(SourceY);
                writer.Write(Width);
                writer.Write(Height);
                writer.Write(TileDepth);
                writer.Write(InstanceID);
                writer.Write(ScaleX);
                writer.Write(ScaleY);
                writer.Write(Color);
            }

            public void Unserialize(UndertaleReader reader)
            {
                X = reader.ReadInt32();
                Y = reader.ReadInt32();
                _BackgroundDefinition.Unserialize(reader, reader.ReadInt32());
                SourceX = reader.ReadUInt32();
                SourceY = reader.ReadUInt32();
                Width = reader.ReadUInt32();
                Height = reader.ReadUInt32();
                TileDepth = reader.ReadInt32();
                InstanceID = reader.ReadUInt32();
                ScaleX = reader.ReadSingle();
                ScaleY = reader.ReadSingle();
                Color = reader.ReadUInt32();
            }
        }
    }
}
