using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
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
        private uint _Width = 320;
        private uint _Height = 240;
        private uint _Speed = 30;
        private bool _Persistent = false;
        private uint _BackgroundColor = 0x00000000;
        private bool _DrawBackgroundColor = true;
        private UndertaleResourceById<UndertaleCode, UndertaleChunkCODE> _CreationCodeId = new UndertaleResourceById<UndertaleCode, UndertaleChunkCODE>();
        private RoomEntryFlags _Flags = RoomEntryFlags.EnableViews;
        private bool _World = false;
        private uint _Top = 0;
        private uint _Left = 0;
        private uint _Right = 1024;
        private uint _Bottom = 768;
        private float _GravityX = 0;
        private float _GravityY = 10;
        private float _MetersPerPixel = 0.1f;

        public UndertaleString Name { get => _Name; set { _Name = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Name")); } }
        public UndertaleString Caption { get => _Caption; set { _Caption = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Caption")); } }
        public uint Width { get => _Width; set { _Width = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Width")); } }
        public uint Height { get => _Height; set { _Height = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Height")); } }
        public uint Speed { get => _Speed; set { _Speed = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Speed")); } }
        public bool Persistent { get => _Persistent; set { _Persistent = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Persistent")); } }
        public uint BackgroundColor { get => _BackgroundColor; set { _BackgroundColor = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("BackgroundColor")); } }
        public bool DrawBackgroundColor { get => _DrawBackgroundColor; set { _DrawBackgroundColor = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("DrawBackgroundColor")); } }
        public UndertaleCode CreationCodeId { get => _CreationCodeId.Resource; set { _CreationCodeId.Resource = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CreationCodeId")); } }
        public RoomEntryFlags Flags { get => _Flags; set { _Flags = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Flags")); } }
        public bool World { get => _World; set { _World = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("World")); } }
        public uint Top { get => _Top; set { _Top = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Top")); } }
        public uint Left { get => _Left; set { _Left = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Left")); } }
        public uint Right { get => _Right; set { _Right = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Right")); } }
        public uint Bottom { get => _Bottom; set { _Bottom = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Bottom")); } }
        public float GravityX { get => _GravityX; set { _GravityX = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("GravityX")); } }
        public float GravityY { get => _GravityY; set { _GravityY = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("GravityY")); } }
        public float MetersPerPixel { get => _MetersPerPixel; set { _MetersPerPixel = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("MetersPerPixel")); } }
        public UndertalePointerList<Background> Backgrounds { get; private set; } = new UndertalePointerList<Background>();
        public UndertalePointerList<View> Views { get; private set; } = new UndertalePointerList<View>();
        public UndertalePointerListLenCheck<GameObject> GameObjects { get; private set; } = new UndertalePointerListLenCheck<GameObject>();
        public UndertalePointerList<Tile> Tiles { get; private set; } = new UndertalePointerList<Tile>();
        public UndertalePointerList<Layer> Layers { get; private set; } = new UndertalePointerList<Layer>();

        public event PropertyChangedEventHandler PropertyChanged;

        public UndertaleRoom()
        {
            for (int i = 0; i < 8; i++)
                Backgrounds.Add(new Background());
            for (int i = 0; i < 8; i++)
                Views.Add(new View());
            if (Flags.HasFlag(RoomEntryFlags.EnableViews))
                Views[0].Enabled = true;
        }

        public void Serialize(UndertaleWriter writer)
        {
            if (writer.undertaleData.GeneralInfo.Major >= 2)
            {
                foreach (var layer in Layers)
                {
                    if (layer.InstancesData != null)
                    {
                        foreach (var inst in layer.InstancesData.Instances)
                        {
                            if (!GameObjects.Contains(inst))
                                throw new Exception("Nonexistent instance " + inst.InstanceID);
                        }
                    }
                }
            }
            writer.WriteUndertaleString(Name);
            writer.WriteUndertaleString(Caption);
            writer.Write(Width);
            writer.Write(Height);
            writer.Write(Speed);
            writer.Write(Persistent);
            writer.Write(BackgroundColor);
            writer.Write(DrawBackgroundColor);
            writer.WriteUndertaleObject(_CreationCodeId);
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
            if (writer.undertaleData.GeneralInfo.Major >= 2)
                writer.WriteUndertaleObjectPointer(Layers);
            writer.WriteUndertaleObject(Backgrounds);
            writer.WriteUndertaleObject(Views);
            writer.WriteUndertaleObject(GameObjects);
            writer.WriteUndertaleObject(Tiles);
            if (writer.undertaleData.GeneralInfo.Major >= 2)
                writer.WriteUndertaleObject(Layers);
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
            _CreationCodeId = reader.ReadUndertaleObject<UndertaleResourceById<UndertaleCode, UndertaleChunkCODE>>();
            Flags = (RoomEntryFlags)reader.ReadUInt32();
            Backgrounds = reader.ReadUndertaleObjectPointer<UndertalePointerList<Background>>();
            Views = reader.ReadUndertaleObjectPointer<UndertalePointerList<View>>();
            GameObjects = reader.ReadUndertaleObjectPointer<UndertalePointerListLenCheck<GameObject>>();
            uint tilePtr = reader.ReadUInt32();
            Tiles = reader.GetUndertaleObjectAtAddress<UndertalePointerList<Tile>>(tilePtr);
            World = reader.ReadBoolean();
            Top = reader.ReadUInt32();
            Left = reader.ReadUInt32();
            Right = reader.ReadUInt32();
            Bottom = reader.ReadUInt32();
            GravityX = reader.ReadSingle();
            GravityY = reader.ReadSingle();
            MetersPerPixel = reader.ReadSingle();
            if (reader.undertaleData.GeneralInfo.Major >= 2)
                Layers = reader.ReadUndertaleObjectPointer<UndertalePointerList<Layer>>();
            reader.ReadUndertaleObject(Backgrounds);
            reader.ReadUndertaleObject(Views);
            reader.ReadUndertaleObject(GameObjects, tilePtr);
            reader.ReadUndertaleObject(Tiles);
            if (reader.undertaleData.GeneralInfo.Major >= 2)
            {
                reader.ReadUndertaleObject(Layers);

                // Resolve the object IDs
                foreach (var layer in Layers)
                {
                    if (layer.InstancesData != null)
                    {
                        layer.InstancesData.Instances.Clear();
                        foreach (var id in layer.InstancesData._InstanceIds)
                        {
                            layer.InstancesData.Instances.Add(GameObjects.ByInstanceID(id));
                        }
                    }
                }
            }
        }

        public void SetupRoom()
        {
            foreach (UndertaleRoom.Layer layer in Layers)
                layer.ParentRoom = this;

            foreach (UndertaleRoom.Background bgnd in Backgrounds)
                bgnd.ParentRoom = this;
        }

        public override string ToString()
        {
            return Name.Content + " (" + GetType().Name + ")";
        }

        public interface RoomObject
        {
            int X { get; }
            int Y { get; }
            uint InstanceID { get; }
        }

        public class Background : UndertaleObject, INotifyPropertyChanged
        {
            private UndertaleRoom _ParentRoom;
            private float _CalcScaleX = 1;
            private float _CalcScaleY = 1;

            private bool _Enabled = false;
            private bool _Foreground = false;
            private UndertaleResourceById<UndertaleBackground, UndertaleChunkBGND> _BackgroundDefinition = new UndertaleResourceById<UndertaleBackground, UndertaleChunkBGND>();
            private int _X = 0;
            private int _Y = 0;
            private int _TileX = 1;
            private int _TileY = 1;
            private int _SpeedX = 0;
            private int _SpeedY = 0;
            private bool _Stretch = false;

            public UndertaleRoom ParentRoom { get => _ParentRoom; set { _ParentRoom = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ParentRoom")); UpdateStretch(); } }
            public float CalcScaleX { get => _CalcScaleX; set { _CalcScaleX = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CalcScaleX")); } }
            public float CalcScaleY { get => _CalcScaleY; set { _CalcScaleY = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CalcScaleY")); } }

            public bool Enabled { get => _Enabled; set { _Enabled = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Enabled")); } }
            public bool Foreground { get => _Foreground; set { _Foreground = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Foreground")); } }
            public UndertaleBackground BackgroundDefinition { get => _BackgroundDefinition.Resource; set { _BackgroundDefinition.Resource = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("BackgroundDefinition")); } }
            public int X { get => _X; set { _X = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("X")); UpdateStretch(); } }
            public int Y { get => _Y; set { _Y = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Y")); UpdateStretch(); } }
            public int TileX { get => _TileX; set { _TileX = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TileX")); } }
            public int TileY { get => _TileY; set { _TileY = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TileY")); } }
            public int SpeedX { get => _SpeedX; set { _SpeedX = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SpeedX")); } }
            public int SpeedY { get => _SpeedY; set { _SpeedY = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SpeedY")); } }
            public bool Stretch { get => _Stretch; set { _Stretch = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Stretch")); UpdateStretch(); } }

            public event PropertyChangedEventHandler PropertyChanged;

            public void UpdateStretch()
            {
                if (ParentRoom == null || BackgroundDefinition == null)
                    return;

                if (!Stretch)
                {
                    CalcScaleX = 1;
                    CalcScaleY = 1;
                    return;
                }

                CalcScaleX = ((ParentRoom.Width - X) / BackgroundDefinition.Texture.SourceWidth);
                CalcScaleY = ((ParentRoom.Height - Y) / BackgroundDefinition.Texture.SourceHeight);
            }

            public void Serialize(UndertaleWriter writer)
            {
                writer.Write(Enabled);
                writer.Write(Foreground);
                writer.WriteUndertaleObject(_BackgroundDefinition);
                writer.Write(X);
                writer.Write(Y);
                writer.Write(TileX);
                writer.Write(TileY);
                writer.Write(SpeedX);
                writer.Write(SpeedY);
                writer.Write(Stretch);
            }

            public void Unserialize(UndertaleReader reader)
            {
                Enabled = reader.ReadBoolean();
                Foreground = reader.ReadBoolean();
                _BackgroundDefinition = reader.ReadUndertaleObject<UndertaleResourceById<UndertaleBackground, UndertaleChunkBGND>>();
                X = reader.ReadInt32();
                Y = reader.ReadInt32();
                TileX = reader.ReadInt32();
                TileY = reader.ReadInt32();
                SpeedX = reader.ReadInt32();
                SpeedY = reader.ReadInt32();
                Stretch = reader.ReadBoolean();
            }
        }

        public class View : UndertaleObject, INotifyPropertyChanged
        {
            private bool _Enabled = false;
            private int _ViewX = 0;
            private int _ViewY = 0;
            private int _ViewWidth = 640;
            private int _ViewHeight = 480;
            private int _PortX = 0;
            private int _PortY = 0;
            private int _PortWidth = 640;
            private int _PortHeight = 480;
            private uint _BorderX = 32;
            private uint _BorderY = 32;
            private int _SpeedX = -1;
            private int _SpeedY = -1;
            private UndertaleResourceById<UndertaleGameObject, UndertaleChunkOBJT> _ObjectId = new UndertaleResourceById<UndertaleGameObject, UndertaleChunkOBJT>();

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
                writer.WriteUndertaleObject(_ObjectId);
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
                _ObjectId = reader.ReadUndertaleObject<UndertaleResourceById<UndertaleGameObject, UndertaleChunkOBJT>>();
            }
        }

        public class GameObject : UndertaleObjectLenCheck, RoomObject, INotifyPropertyChanged
        {
            private int _X;
            private int _Y;
            private UndertaleResourceById<UndertaleGameObject, UndertaleChunkOBJT> _ObjectDefinition = new UndertaleResourceById<UndertaleGameObject, UndertaleChunkOBJT>();
            private uint _InstanceID;
            private UndertaleResourceById<UndertaleCode, UndertaleChunkCODE> _CreationCode = new UndertaleResourceById<UndertaleCode, UndertaleChunkCODE>();
            private float _ScaleX = 1;
            private float _ScaleY = 1;
            private uint _Color = 0xFFFFFFFF;
            private float _Rotation = 0;
            private UndertaleResourceById<UndertaleCode, UndertaleChunkCODE> _PreCreateCode = new UndertaleResourceById<UndertaleCode, UndertaleChunkCODE>();
            private bool _GMS2_2_2 = false;
            private float _ImageSpeed = 0;
            private int _ImageIndex = 0;

            public int X { get => _X; set { _X = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("X")); } }
            public int Y { get => _Y; set { _Y = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Y")); } }
            public UndertaleGameObject ObjectDefinition { get => _ObjectDefinition.Resource; set { _ObjectDefinition.Resource = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ObjectDefinition")); } }
            public uint InstanceID { get => _InstanceID; set { _InstanceID = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("InstanceID")); } }
            public UndertaleCode CreationCode { get => _CreationCode.Resource; set { _CreationCode.Resource = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CreationCode")); } }
            public float ScaleX { get => _ScaleX; set { _ScaleX = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ScaleX")); } }
            public float ScaleY { get => _ScaleY; set { _ScaleY = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ScaleY")); } }
            public uint Color { get => _Color; set { _Color = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Color")); } }
            public float Rotation { get => _Rotation; set { _Rotation = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Rotation")); PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("OppositeRotation")); } }
            public UndertaleCode PreCreateCode { get => _PreCreateCode.Resource; set { _PreCreateCode.Resource = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("PreCreateCode")); } }
            public bool GMS2_2_2 { get => _GMS2_2_2; set { _GMS2_2_2 = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("GMS2_2_2")); } }
            public float ImageSpeed { get => _ImageSpeed; set { _ImageSpeed = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ImageSpeed")); } }
            public int ImageIndex { get => _ImageIndex; set { _ImageIndex = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ImageIndex")); } }

            public event PropertyChangedEventHandler PropertyChanged;
            public float OppositeRotation { get => 360F - Rotation; }

            public void Serialize(UndertaleWriter writer)
            {
                writer.Write(X);
                writer.Write(Y);
                writer.WriteUndertaleObject(_ObjectDefinition);
                writer.Write(InstanceID);
                writer.WriteUndertaleObject(_CreationCode);
                writer.Write(ScaleX);
                writer.Write(ScaleY);
                if (GMS2_2_2)
                {
                    writer.Write(ImageSpeed);
                    writer.Write(ImageIndex);
                }
                writer.Write(Color);
                writer.Write(Rotation);
                if (writer.undertaleData.GeneralInfo.BytecodeVersion >= 16) // TODO: is that dependent on bytecode or something else?
                    writer.WriteUndertaleObject(_PreCreateCode);         // Note: Appears in GM:S 1.4.9999 as well, so that's probably the closest it gets
            }

            public void Unserialize(UndertaleReader reader)
            {
                Unserialize(reader, -1);
            }

            public void Unserialize(UndertaleReader reader, int length)
            {
                X = reader.ReadInt32();
                Y = reader.ReadInt32();
                _ObjectDefinition = reader.ReadUndertaleObject<UndertaleResourceById<UndertaleGameObject, UndertaleChunkOBJT>>();
                InstanceID = reader.ReadUInt32();
                _CreationCode = reader.ReadUndertaleObject<UndertaleResourceById<UndertaleCode, UndertaleChunkCODE>>();
                ScaleX = reader.ReadSingle();
                ScaleY = reader.ReadSingle();
                if (length == 48)
                {
                    GMS2_2_2 = true;
                    ImageSpeed = reader.ReadSingle();
                    ImageIndex = reader.ReadInt32();
                }
                Color = reader.ReadUInt32();
                Rotation = reader.ReadSingle();
                if (reader.undertaleData.GeneralInfo.BytecodeVersion >= 16) // TODO: is that dependent on bytecode or something else?
                    _PreCreateCode = reader.ReadUndertaleObject<UndertaleResourceById<UndertaleCode, UndertaleChunkCODE>>(); // Note: Appears in GM:S 1.4.9999 as well, so that's probably the closest it gets
            }

            public override string ToString()
            {
                return "Instance " + InstanceID + " of " + (ObjectDefinition?.Name?.Content ?? "?") + " (UndertaleRoom+GameObject)";
            }
        }

        public class Tile : UndertaleObject, RoomObject, INotifyPropertyChanged
        {
            private int _X;
            private int _Y;
            public bool _SpriteMode = false;
            private UndertaleResourceById<UndertaleBackground, UndertaleChunkBGND> _BackgroundDefinition = new UndertaleResourceById<UndertaleBackground, UndertaleChunkBGND>();
            private UndertaleResourceById<UndertaleSprite, UndertaleChunkSPRT> _SpriteDefinition = new UndertaleResourceById<UndertaleSprite, UndertaleChunkSPRT>();
            private uint _SourceX;
            private uint _SourceY;
            private uint _Width;
            private uint _Height;
            private int _TileDepth = 0;
            private uint _InstanceID;
            private float _ScaleX = 1;
            private float _ScaleY = 1;
            private uint _Color = 0xFFFFFFFF;

            public int X { get => _X; set { _X = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("X")); } }
            public int Y { get => _Y; set { _Y = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Y")); } }
            public UndertaleBackground BackgroundDefinition { get => _BackgroundDefinition.Resource; set { _BackgroundDefinition.Resource = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("BackgroundDefinition")); PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ObjectDefinition")); } }
            public UndertaleSprite SpriteDefinition { get => _SpriteDefinition.Resource; set { _SpriteDefinition.Resource = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SpriteDefinition")); PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ObjectDefinition")); } }
            public UndertaleNamedResource ObjectDefinition { get => _SpriteMode ? (UndertaleNamedResource)SpriteDefinition : (UndertaleNamedResource)BackgroundDefinition; set { if (_SpriteMode) SpriteDefinition = (UndertaleSprite)value; else BackgroundDefinition = (UndertaleBackground)value; } }
            public uint SourceX { get => _SourceX; set { _SourceX = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SourceX")); } }
            public uint SourceY { get => _SourceY; set { _SourceY = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SourceY")); } }
            public uint Width { get => _Width; set { _Width = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Width")); } }
            public uint Height { get => _Height; set { _Height = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Height")); } }
            public int TileDepth { get => _TileDepth; set { _TileDepth = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TileDepth")); } }
            public uint InstanceID { get => _InstanceID; set { _InstanceID = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("InstanceID")); } }
            public float ScaleX { get => _ScaleX; set { _ScaleX = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ScaleX")); } }
            public float ScaleY { get => _ScaleY; set { _ScaleY = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ScaleY")); } }
            public uint Color { get => _Color; set { _Color = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Color")); } }

            public UndertaleTexturePageItem Tpag => _SpriteMode ? SpriteDefinition?.Textures?.FirstOrDefault()?.Texture : BackgroundDefinition?.Texture; // TODO: what happens on sprites with multiple textures?

            public event PropertyChangedEventHandler PropertyChanged;

            public void Serialize(UndertaleWriter writer)
            {
                writer.Write(X);
                writer.Write(Y);
                if (_SpriteMode != (writer.undertaleData.GeneralInfo.Major >= 2))
                    throw new Exception("Unsupported in GMS" + writer.undertaleData.GeneralInfo.Major);
                if (_SpriteMode)
                    writer.WriteUndertaleObject(_SpriteDefinition);
                else
                    writer.WriteUndertaleObject(_BackgroundDefinition);
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
                _SpriteMode = reader.undertaleData.GeneralInfo.Major >= 2;
                if (_SpriteMode)
                    _SpriteDefinition = reader.ReadUndertaleObject<UndertaleResourceById<UndertaleSprite, UndertaleChunkSPRT>>();
                else
                    _BackgroundDefinition = reader.ReadUndertaleObject<UndertaleResourceById<UndertaleBackground, UndertaleChunkBGND>>();
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

            public override string ToString()
            {
                return "Tile " + InstanceID + " of " + (ObjectDefinition?.Name?.Content ?? "?") + " (UndertaleRoom+Tile)";
            }
        }

        // For GMS2, Backgrounds and Tiles are empty and this is used instead
        public enum LayerType
        {
            Instances = 2,
            Tiles = 4,
            Background = 1,
            Assets = 3
        }

        public class Layer : UndertaleObject, INotifyPropertyChanged
        {
            public interface LayerData : UndertaleObject
            {
            }

            private UndertaleRoom _ParentRoom;

            private UndertaleString _LayerName;
            private uint _LayerId;
            private LayerType _LayerType;
            private int _LayerDepth;
            private float _XOffset;
            private float _YOffset;
            private float _HSpeed;
            private float _VSpeed;
            private bool _IsVisible;
            private LayerData _Data;

            public UndertaleRoom ParentRoom { get => _ParentRoom; set { _ParentRoom = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ParentRoom")); UpdateParentRoom(); } }

            public UndertaleString LayerName { get => _LayerName; set { _LayerName = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("LayerName")); } }
            public uint LayerId { get => _LayerId; set { _LayerId = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("LayerId")); } }
            public LayerType LayerType { get => _LayerType; set { _LayerType = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("LayerType")); } }
            public int LayerDepth { get => _LayerDepth; set { _LayerDepth = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("LayerDepth")); } }
            public float XOffset { get => _XOffset; set { _XOffset = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("XOffset")); } }
            public float YOffset { get => _YOffset; set { _YOffset = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("YOffset")); } }
            public float HSpeed { get => _HSpeed; set { _HSpeed = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("HSpeed")); } }
            public float VSpeed { get => _VSpeed; set { _VSpeed = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("VSpeed")); } }
            public bool IsVisible { get => _IsVisible; set { _IsVisible = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsVisible")); } }
            public LayerData Data { get => _Data; set { _Data = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Data")); } }
            public LayerInstancesData InstancesData => _Data as LayerInstancesData;
            public LayerTilesData TilesData => _Data as LayerTilesData;
            public LayerBackgroundData BackgroundData => _Data as LayerBackgroundData;
            public LayerAssetsData AssetsData => _Data as LayerAssetsData;

            public event PropertyChangedEventHandler PropertyChanged;

            public void UpdateParentRoom()
            {
                if (BackgroundData != null)
                    BackgroundData.ParentLayer = this;
            }

            public void Serialize(UndertaleWriter writer)
            {
                writer.WriteUndertaleString(LayerName);
                writer.Write(LayerId);
                writer.Write((uint)LayerType);
                writer.Write(LayerDepth);
                writer.Write(XOffset);
                writer.Write(YOffset);
                writer.Write(HSpeed);
                writer.Write(VSpeed);
                writer.Write(IsVisible);
                if (LayerType == LayerType.Instances)
                {
                    writer.WriteUndertaleObject(InstancesData);
                }
                else if (LayerType == LayerType.Tiles)
                {
                    writer.WriteUndertaleObject(TilesData);
                }
                else if (LayerType == LayerType.Background)
                {
                    writer.WriteUndertaleObject(BackgroundData);
                }
                else if (LayerType == LayerType.Assets)
                {
                    writer.WriteUndertaleObject(AssetsData);
                }
                else
                {
                    throw new Exception("Unsupported layer type " + LayerType);
                }
            }

            public void Unserialize(UndertaleReader reader)
            {
                LayerName = reader.ReadUndertaleString();
                LayerId = reader.ReadUInt32();
                LayerType = (LayerType)reader.ReadUInt32();
                LayerDepth = reader.ReadInt32();
                XOffset = reader.ReadSingle();
                YOffset = reader.ReadSingle();
                HSpeed = reader.ReadSingle();
                VSpeed = reader.ReadSingle();
                IsVisible = reader.ReadBoolean();
                if (LayerType == LayerType.Instances)
                {
                    Data = reader.ReadUndertaleObject<LayerInstancesData>();
                }
                else if (LayerType == LayerType.Tiles)
                {
                    Data = reader.ReadUndertaleObject<LayerTilesData>();
                }
                else if (LayerType == LayerType.Background)
                {
                    Data = reader.ReadUndertaleObject<LayerBackgroundData>();
                }
                else if (LayerType == LayerType.Assets)
                {
                    Data = reader.ReadUndertaleObject<LayerAssetsData>();
                }
                else
                {
                    throw new Exception("Unsupported layer type " + LayerType);
                }
            }

            public class LayerInstancesData : LayerData
            {
                internal uint[] _InstanceIds { get; private set; } // 100000, 100001, 100002, 100003 - instance ids from GameObjects list in the room
                public ObservableCollection<UndertaleRoom.GameObject> Instances { get; private set; } = new ObservableCollection<UndertaleRoom.GameObject>();

                public void Serialize(UndertaleWriter writer)
                {
                    writer.Write((uint)Instances.Count);
                    foreach (var obj in Instances)
                        writer.Write(obj.InstanceID);
                }

                public void Unserialize(UndertaleReader reader)
                {
                    uint InstanceCount = reader.ReadUInt32();
                    _InstanceIds = new uint[InstanceCount];
                    Instances.Clear();
                    for (uint i = 0; i < InstanceCount; i++)
                        _InstanceIds[i] = reader.ReadUInt32();
                    // UndertaleRoom.Unserialize resolves these IDs to objects later
                }
            }

            public class LayerTilesData : LayerData, INotifyPropertyChanged
            {
                private UndertaleResourceById<UndertaleBackground, UndertaleChunkBGND> _Background = new UndertaleResourceById<UndertaleBackground, UndertaleChunkBGND>(); // In GMS2 backgrounds are just tilesets
                private uint _TilesX;
                private uint _TilesY;
                private uint[][] _TileData; // Each is simply an ID from the tileset/background/sprite

                public UndertaleBackground Background { get => _Background.Resource; set { _Background.Resource = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Background")); } }
                public uint TilesX
                {
                    get => _TilesX; set
                    {
                        _TilesX = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TilesX"));
                        if (_TileData != null)
                        {
                            for (var y = 0; y < _TileData.Length; y++)
                            {
                                Array.Resize(ref _TileData[y], (int)value);
                            }
                            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TileData"));
                        }
                    }
                }
                public uint TilesY
                {
                    get => _TilesY; set
                    {
                        _TilesY = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TilesY"));
                        if (_TileData != null)
                        {
                            Array.Resize(ref _TileData, (int)value);
                            for (var y = 0; y < _TileData.Length; y++)
                            {
                                if (_TileData[y] == null)
                                    _TileData[y] = new uint[TilesX];
                            }
                            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TileData"));
                        }
                    }
                }
                public uint[][] TileData { get => _TileData; set { _TileData = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TileData")); } }

                public event PropertyChangedEventHandler PropertyChanged;

                public void Serialize(UndertaleWriter writer)
                {
                    _Background.Serialize(writer); // see comment below
                    writer.Write(TilesX);
                    writer.Write(TilesY);
                    if (TileData.Length != TilesY)
                        throw new Exception("Invalid TileData row length");
                    foreach (var row in TileData)
                    {
                        if (row.Length != TilesX)
                            throw new Exception("Invalid TileData column length");
                        foreach (var tile in row)
                            writer.Write(tile);
                    }
                }

                public void Unserialize(UndertaleReader reader)
                {
                    _Background = new UndertaleResourceById<UndertaleBackground, UndertaleChunkBGND>(); // see comment in UndertaleGlobalInit.Unserialize
                    _Background.Unserialize(reader);
                    _TileData = null; // prevent unnecessary resizes
                    TilesX = reader.ReadUInt32();
                    TilesY = reader.ReadUInt32();
                    TileData = new uint[TilesY][];
                    for (uint y = 0; y < TilesY; y++)
                    {
                        TileData[y] = new uint[TilesX];
                        for (uint x = 0; x < TilesX; x++)
                        {
                            TileData[y][x] = reader.ReadUInt32();
                        }
                    }
                }
            }

            public class LayerBackgroundData : LayerData, INotifyPropertyChanged
            {
                private Layer _ParentLayer;
                private float _CalcScaleX = 1;
                private float _CalcScaleY = 1;

                private bool _Visible;
                private bool _Foreground;
                private UndertaleResourceById<UndertaleSprite, UndertaleChunkSPRT> _Sprite = new UndertaleResourceById<UndertaleSprite, UndertaleChunkSPRT>(); // Apparently there's a mode where it's a background reference, but probably not necessary
                private bool _TiledHorizontally;
                private bool _TiledVertically;
                private bool _Stretch;
                private uint _Color; // includes alpha channel
                private float _FirstFrame;
                private float _AnimationSpeed;
                private AnimationSpeedType _AnimationSpeedType; // 0 means it's in FPS, 1 means it's in "frames per game frame", I believe

                public Layer ParentLayer { get => _ParentLayer; set { _ParentLayer = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ParentLayer")); UpdateScale(); } }
                public float CalcScaleX { get => _CalcScaleX; set { _CalcScaleX = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CalcScaleX")); } }
                public float CalcScaleY { get => _CalcScaleY; set { _CalcScaleY = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CalcScaleY")); } }

                public bool Visible { get => _Visible; set { _Visible = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Visible")); } }
                public bool Foreground { get => _Foreground; set { _Foreground = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Foreground")); } }
                public UndertaleSprite Sprite { get => _Sprite.Resource; set { _Sprite.Resource = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Sprite")); } }
                public bool TiledHorizontally { get => _TiledHorizontally; set { _TiledHorizontally = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TiledHorizontally")); UpdateScale(); } }
                public bool TiledVertically { get => _TiledVertically; set { _TiledVertically = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TiledVertically")); UpdateScale(); } }
                public bool Stretch { get => _Stretch; set { _Stretch = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Stretch")); UpdateScale(); } }
                public uint Color { get => _Color; set { _Color = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Color")); } }
                public float FirstFrame { get => _FirstFrame; set { _FirstFrame = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("FirstFrame")); } }
                public float AnimationSpeed { get => _AnimationSpeed; set { _AnimationSpeed = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("AnimationSpeed")); } }
                public AnimationSpeedType AnimationSpeedType { get => _AnimationSpeedType; set { _AnimationSpeedType = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("AnimationSpeedType")); } }

                public event PropertyChangedEventHandler PropertyChanged;

                public void UpdateScale()
                {
                    bool HasRoom = (ParentLayer != null) && (ParentLayer.ParentRoom != null) && (Sprite != null);
                    CalcScaleX = (HasRoom && (Stretch || TiledHorizontally)) ? (ParentLayer.ParentRoom.Width / Sprite.Width) : 1;
                    CalcScaleY = (HasRoom && (Stretch || TiledVertically)) ? (ParentLayer.ParentRoom.Height / Sprite.Height) : 1;
                }

                public void Serialize(UndertaleWriter writer)
                {
                    writer.Write(Visible);
                    writer.Write(Foreground);
                    writer.WriteUndertaleObject(_Sprite);
                    writer.Write(TiledHorizontally);
                    writer.Write(TiledVertically);
                    writer.Write(Stretch);
                    writer.Write(Color);
                    writer.Write(FirstFrame);
                    writer.Write(AnimationSpeed);
                    writer.Write((uint)AnimationSpeedType);
                }

                public void Unserialize(UndertaleReader reader)
                {
                    Visible = reader.ReadBoolean();
                    Foreground = reader.ReadBoolean();
                    _Sprite = reader.ReadUndertaleObject<UndertaleResourceById<UndertaleSprite, UndertaleChunkSPRT>>();
                    TiledHorizontally = reader.ReadBoolean();
                    TiledVertically = reader.ReadBoolean();
                    Stretch = reader.ReadBoolean();
                    Color = reader.ReadUInt32();
                    FirstFrame = reader.ReadSingle();
                    AnimationSpeed = reader.ReadSingle();
                    AnimationSpeedType = (AnimationSpeedType)reader.ReadUInt32();
                }
            }

            public class LayerAssetsData : LayerData, INotifyPropertyChanged
            {
                private UndertalePointerList<Tile> _LegacyTiles;
                private UndertalePointerList<SpriteInstance> _Sprites;

                public UndertalePointerList<Tile> LegacyTiles { get => _LegacyTiles; set { _LegacyTiles = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("LegacyTiles")); } }
                public UndertalePointerList<SpriteInstance> Sprites { get => _Sprites; set { _Sprites = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Sprites")); } }

                public event PropertyChangedEventHandler PropertyChanged;

                public void Serialize(UndertaleWriter writer)
                {
                    writer.WriteUndertaleObjectPointer(LegacyTiles);
                    writer.WriteUndertaleObjectPointer(Sprites);
                    writer.WriteUndertaleObject(LegacyTiles);
                    writer.WriteUndertaleObject(Sprites);
                }

                public void Unserialize(UndertaleReader reader)
                {
                    LegacyTiles = reader.ReadUndertaleObjectPointer<UndertalePointerList<Tile>>();
                    Sprites = reader.ReadUndertaleObjectPointer<UndertalePointerList<SpriteInstance>>();
                    reader.ReadUndertaleObject(LegacyTiles);
                    reader.ReadUndertaleObject(Sprites);
                }
            }
        }

        public class SpriteInstance : UndertaleObject, INotifyPropertyChanged
        {
            private UndertaleString _Name;
            private UndertaleResourceById<UndertaleSprite, UndertaleChunkSPRT> _Sprite = new UndertaleResourceById<UndertaleSprite, UndertaleChunkSPRT>();
            private int _X;
            private int _Y;
            private float _ScaleX;
            private float _ScaleY;
            private uint _Color;
            private float _AnimationSpeed;
            private AnimationSpeedType _AnimationSpeedType;
            private float _FrameIndex;
            private float _Rotation;

            public UndertaleString Name { get => _Name; set { _Name = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Name")); } }
            public UndertaleSprite Sprite { get => _Sprite.Resource; set { _Sprite.Resource = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Sprite")); } }
            public int X { get => _X; set { _X = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("X")); } }
            public int Y { get => _Y; set { _Y = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Y")); } }
            public float ScaleX { get => _ScaleX; set { _ScaleX = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ScaleX")); } }
            public float ScaleY { get => _ScaleY; set { _ScaleY = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ScaleY")); } }
            public uint Color { get => _Color; set { _Color = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Color")); } }
            public float AnimationSpeed { get => _AnimationSpeed; set { _AnimationSpeed = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("AnimationSpeed")); } }
            public AnimationSpeedType AnimationSpeedType { get => _AnimationSpeedType; set { _AnimationSpeedType = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("AnimationSpeedType")); } }
            public float FrameIndex { get => _FrameIndex; set { _FrameIndex = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("FrameIndex")); } }
            public float Rotation { get => _Rotation; set { _Rotation = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Rotation")); } }

            public event PropertyChangedEventHandler PropertyChanged;

            public void Serialize(UndertaleWriter writer)
            {
                writer.WriteUndertaleString(Name);
                writer.WriteUndertaleObject(_Sprite);
                writer.Write(X);
                writer.Write(Y);
                writer.Write(ScaleX);
                writer.Write(ScaleY);
                writer.Write(Color);
                writer.Write(AnimationSpeed);
                writer.Write((uint)AnimationSpeedType);
                writer.Write(FrameIndex);
                writer.Write(Rotation);
            }

            public void Unserialize(UndertaleReader reader)
            {
                Name = reader.ReadUndertaleString();
                _Sprite = reader.ReadUndertaleObject<UndertaleResourceById<UndertaleSprite, UndertaleChunkSPRT>>();
                X = reader.ReadInt32();
                Y = reader.ReadInt32();
                ScaleX = reader.ReadSingle();
                ScaleY = reader.ReadSingle();
                Color = reader.ReadUInt32();
                AnimationSpeed = reader.ReadSingle();
                AnimationSpeedType = (AnimationSpeedType)reader.ReadUInt32();
                FrameIndex = reader.ReadSingle();
                Rotation = reader.ReadSingle();
            }

            public override string ToString()
            {
                return "Sprite " + Name?.Content + " of " + (Sprite?.Name?.Content ?? "?") + " (UndertaleRoom+SpriteInstance)";
            }
        }
    }

    public enum AnimationSpeedType : uint
    {
        FPS = 0,
        FramesPerGameFrame = 1,
    }

    public static class UndertaleRoomExtensions
    {
        public static T ByInstanceID<T>(this IList<T> list, uint instance) where T : UndertaleRoom.RoomObject
        {
            return list.Where((x) => x.InstanceID == instance).FirstOrDefault();
        }
    }
}
