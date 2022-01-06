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
            ClearDisplayBuffer = 4,
            IsGMS2 = 131072,
            IsGMS2_3 = 65536
        }

        public UndertaleString Name { get; set; }
        public UndertaleString Caption { get; set; }
        public uint Width { get; set; } = 320;
        public uint Height { get; set; } = 240;
        public uint Speed { get; set; } = 30;
        public bool Persistent { get; set; } = false;
        public uint BackgroundColor { get; set; } = 0;
        public bool DrawBackgroundColor { get; set; } = true;

        private UndertaleResourceById<UndertaleCode, UndertaleChunkCODE> _CreationCodeId = new UndertaleResourceById<UndertaleCode, UndertaleChunkCODE>();
        public UndertaleCode CreationCodeId { get => _CreationCodeId.Resource; set { _CreationCodeId.Resource = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CreationCodeId))); } }
        public RoomEntryFlags Flags { get; set; } = RoomEntryFlags.EnableViews;
        public bool World { get; set; } = false;
        public uint Top { get; set; } = 0;
        public uint Left { get; set; } = 0;
        public uint Right { get; set; } = 1024;
        public uint Bottom { get; set; } = 768;
        public float GravityX { get; set; } = 0;
        public float GravityY { get; set; } = 10;
        public float MetersPerPixel { get; set; } = 0.1f;
        public double Grid { get; set; } = 16d;
        public double GridThicknessPx { get; set; } = 1d;
        public UndertalePointerList<Background> Backgrounds { get; private set; } = new UndertalePointerList<Background>();
        public UndertalePointerList<View> Views { get; private set; } = new UndertalePointerList<View>();
        public UndertalePointerListLenCheck<GameObject> GameObjects { get; private set; } = new UndertalePointerListLenCheck<GameObject>();
        public UndertalePointerList<Tile> Tiles { get; private set; } = new UndertalePointerList<Tile>();
        public UndertalePointerList<Layer> Layers { get; private set; } = new UndertalePointerList<Layer>();
        public UndertaleSimpleList<UndertaleResourceById<UndertaleSequence, UndertaleChunkSEQN>> Sequences { get; private set; } = new UndertaleSimpleList<UndertaleResourceById<UndertaleSequence, UndertaleChunkSEQN>>();

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
            bool sequences = false;
            if (writer.undertaleData.GeneralInfo.Major >= 2)
            {
                writer.WriteUndertaleObjectPointer(Layers);
                sequences = writer.undertaleData.FORM.Chunks.ContainsKey("SEQN");
                if (sequences)
                    writer.WriteUndertaleObjectPointer(Sequences);
            }
            writer.WriteUndertaleObject(Backgrounds);
            writer.WriteUndertaleObject(Views);
            writer.WriteUndertaleObject(GameObjects);
            writer.WriteUndertaleObject(Tiles);
            if (writer.undertaleData.GeneralInfo.Major >= 2)
            {
                writer.WriteUndertaleObject(Layers);
                
                if (sequences)
                    writer.WriteUndertaleObject(Sequences);
            }
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
            bool sequences = false;
            if (reader.undertaleData.GeneralInfo.Major >= 2)
            {
                Layers = reader.ReadUndertaleObjectPointer<UndertalePointerList<Layer>>();
                sequences = reader.GMS2_3;
                if (sequences)
                    Sequences = reader.ReadUndertaleObjectPointer<UndertaleSimpleList<UndertaleResourceById<UndertaleSequence, UndertaleChunkSEQN>>>();
            }
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
                
                if (sequences)
                    reader.ReadUndertaleObject(Sequences);
            }
        }

        public void SetupRoom()
        {
            foreach (UndertaleRoom.Layer layer in Layers)
            {
                if (layer != null)
                    layer.ParentRoom = this;
            }
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
            public UndertaleRoom ParentRoom { get => _ParentRoom; set { _ParentRoom = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ParentRoom))); UpdateStretch(); } }
            public float CalcScaleX { get; set; } = 1;
            public float CalcScaleY { get; set; } = 1;
            public bool Enabled { get; set; } = false;
            public bool Foreground { get; set; } = false;
            private UndertaleResourceById<UndertaleBackground, UndertaleChunkBGND> _BackgroundDefinition = new UndertaleResourceById<UndertaleBackground, UndertaleChunkBGND>();
            public UndertaleBackground BackgroundDefinition { get => _BackgroundDefinition.Resource; set { _BackgroundDefinition.Resource = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BackgroundDefinition))); } }
            private int _X = 0;
            private int _Y = 0; 
            public int X { get => _X; set { _X = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(X))); UpdateStretch(); } }
            public int Y { get => _Y; set { _Y = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Y))); UpdateStretch(); } }
            public int TileX { get; set; } = 1;
            public int TileY { get; set; } = 1;
            public int SpeedX { get; set; } = 0;
            public int SpeedY { get; set; } = 0;
            private bool _Stretch = false;
            public bool Stretch { get => _Stretch; set { _Stretch = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Stretch))); UpdateStretch(); } }

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
            public bool Enabled { get; set; } = false;
            public int ViewX { get; set; }
            public int ViewY { get; set; }
            public int ViewWidth { get; set; } = 640;
            public int ViewHeight { get; set; } = 480;
            public int PortX { get; set; }
            public int PortY { get; set; }
            public int PortWidth { get; set; } = 640;
            public int PortHeight { get; set; } = 480;
            public uint BorderX { get; set; } = 32;
            public uint BorderY { get; set; } = 32;
            public int SpeedX { get; set; } = -1;
            public int SpeedY { get; set; } = -1;

            private UndertaleResourceById<UndertaleGameObject, UndertaleChunkOBJT> _ObjectId = new UndertaleResourceById<UndertaleGameObject, UndertaleChunkOBJT>();
            public UndertaleGameObject ObjectId { get => _ObjectId.Resource; set { _ObjectId.Resource = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ObjectId))); } }

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
            private UndertaleResourceById<UndertaleGameObject, UndertaleChunkOBJT> _ObjectDefinition = new UndertaleResourceById<UndertaleGameObject, UndertaleChunkOBJT>();
            private UndertaleResourceById<UndertaleCode, UndertaleChunkCODE> _CreationCode = new UndertaleResourceById<UndertaleCode, UndertaleChunkCODE>();
            private UndertaleResourceById<UndertaleCode, UndertaleChunkCODE> _PreCreateCode = new UndertaleResourceById<UndertaleCode, UndertaleChunkCODE>();

            public int X { get; set; }
            public int Y { get; set; }
            public UndertaleGameObject ObjectDefinition { get => _ObjectDefinition.Resource; set { _ObjectDefinition.Resource = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ObjectDefinition))); } }
            public uint InstanceID { get; set; }
            public UndertaleCode CreationCode { get => _CreationCode.Resource; set { _CreationCode.Resource = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CreationCode))); } }
            public float ScaleX { get; set; } = 1;
            public float ScaleY { get; set; } = 1;
            public uint Color { get; set; } = 0xFFFFFFFF;
            public float Rotation { get; set; }
            public UndertaleCode PreCreateCode { get => _PreCreateCode.Resource; set { _PreCreateCode.Resource = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PreCreateCode))); } }
            public float ImageSpeed { get; set; }
            public int ImageIndex { get; set; }

            public event PropertyChangedEventHandler PropertyChanged;
            public float OppositeRotation => 360F - Rotation;
            public int XOffset => ObjectDefinition.Sprite != null ? X - ObjectDefinition.Sprite.OriginX : X;
            public int YOffset => ObjectDefinition.Sprite != null ? Y - ObjectDefinition.Sprite.OriginY : Y;

            public void Serialize(UndertaleWriter writer)
            {
                writer.Write(X);
                writer.Write(Y);
                writer.WriteUndertaleObject(_ObjectDefinition);
                writer.Write(InstanceID);
                writer.WriteUndertaleObject(_CreationCode);
                writer.Write(ScaleX);
                writer.Write(ScaleY);
                if (writer.undertaleData.GMS2_2_2_302)
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
                    reader.undertaleData.GMS2_2_2_302 = true;
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
            public bool _SpriteMode = false;
            private UndertaleResourceById<UndertaleBackground, UndertaleChunkBGND> _BackgroundDefinition = new UndertaleResourceById<UndertaleBackground, UndertaleChunkBGND>();
            private UndertaleResourceById<UndertaleSprite, UndertaleChunkSPRT> _SpriteDefinition = new UndertaleResourceById<UndertaleSprite, UndertaleChunkSPRT>();

            public int X { get; set; }
            public int Y { get; set; }
            public UndertaleBackground BackgroundDefinition { get => _BackgroundDefinition.Resource; set { _BackgroundDefinition.Resource = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BackgroundDefinition))); PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ObjectDefinition))); } }
            public UndertaleSprite SpriteDefinition { get => _SpriteDefinition.Resource; set { _SpriteDefinition.Resource = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SpriteDefinition))); PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ObjectDefinition))); } }
            public UndertaleNamedResource ObjectDefinition { get => _SpriteMode ? SpriteDefinition : BackgroundDefinition; set { if (_SpriteMode) SpriteDefinition = (UndertaleSprite)value; else BackgroundDefinition = (UndertaleBackground)value; } }
            public uint SourceX { get; set; }
            public uint SourceY { get; set; }
            public uint Width { get; set; }
            public uint Height { get; set; }
            public int TileDepth { get; set; }
            public uint InstanceID { get; set; }
            public float ScaleX { get; set; } = 1;
            public float ScaleY { get; set; } = 1;
            public uint Color { get; set; } = 0xFFFFFFFF;

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
            Background = 1,
            Instances = 2,
            Assets = 3,
            Tiles = 4
        }

        public class Layer : UndertaleObject, INotifyPropertyChanged
        {
            public interface LayerData : UndertaleObject
            {
            }

            private UndertaleRoom _ParentRoom;
            public UndertaleRoom ParentRoom { get => _ParentRoom; set { _ParentRoom = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ParentRoom))); UpdateParentRoom(); } }

            public UndertaleString LayerName { get; set; }
            public uint LayerId { get; set; }
            public LayerType LayerType { get; set; }
            public int LayerDepth { get; set; }
            public float XOffset { get; set; }
            public float YOffset { get; set; }
            public float HSpeed { get; set; }
            public float VSpeed { get; set; }
            public bool IsVisible { get; set; }
            public LayerData Data { get; set; }
            public LayerInstancesData InstancesData => Data as LayerInstancesData;
            public LayerTilesData TilesData => Data as LayerTilesData;
            public LayerBackgroundData BackgroundData => Data as LayerBackgroundData;
            public LayerAssetsData AssetsData => Data as LayerAssetsData;

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

                public UndertaleBackground Background { get => _Background.Resource; set { _Background.Resource = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Background))); } }
                public uint TilesX
                {
                    get => _TilesX; set
                    {
                        _TilesX = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TilesX)));
                        if (_TileData != null)
                        {
                            for (var y = 0; y < _TileData.Length; y++)
                            {
                                Array.Resize(ref _TileData[y], (int)value);
                            }
                            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TileData)));
                        }
                    }
                }
                public uint TilesY
                {
                    get => _TilesY; set
                    {
                        _TilesY = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TilesY)));
                        if (_TileData != null)
                        {
                            Array.Resize(ref _TileData, (int)value);
                            for (var y = 0; y < _TileData.Length; y++)
                            {
                                if (_TileData[y] == null)
                                    _TileData[y] = new uint[TilesX];
                            }
                            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TileData)));
                        }
                    }
                }
                public uint[][] TileData { get => _TileData; set { _TileData = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TileData))); } }

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

                private UndertaleResourceById<UndertaleSprite, UndertaleChunkSPRT> _Sprite = new UndertaleResourceById<UndertaleSprite, UndertaleChunkSPRT>(); // Apparently there's a mode where it's a background reference, but probably not necessary
                private bool _TiledHorizontally;
                private bool _TiledVertically;
                private bool _Stretch;

                public Layer ParentLayer { get => _ParentLayer; set { _ParentLayer = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ParentLayer))); UpdateScale(); } }
                public float CalcScaleX { get; set; }
                public float CalcScaleY { get; set; }

                public bool Visible { get; set; }
                public bool Foreground { get; set; }
                public UndertaleSprite Sprite { get => _Sprite.Resource; set { _Sprite.Resource = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Sprite))); } }
                public bool TiledHorizontally { get => _TiledHorizontally; set { _TiledHorizontally = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TiledHorizontally))); UpdateScale(); } }
                public bool TiledVertically { get => _TiledVertically; set { _TiledVertically = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TiledVertically))); UpdateScale(); } }
                public bool Stretch { get => _Stretch; set { _Stretch = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Stretch))); UpdateScale(); } }
                public uint Color { get; set; }
                public float FirstFrame { get; set; }
                public float AnimationSpeed { get; set; }
                public AnimationSpeedType AnimationSpeedType { get; set; }

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

            [PropertyChanged.AddINotifyPropertyChangedInterface]
            public class LayerAssetsData : LayerData
            {
                public UndertalePointerList<Tile> LegacyTiles { get; set; }
                public UndertalePointerList<SpriteInstance> Sprites { get; set; }
                public UndertalePointerList<SequenceInstance> Sequences { get; set; }
                public UndertalePointerList<SpriteInstance> NineSlices { get; set; } // Removed in 2.3.2, before ever used

                public void Serialize(UndertaleWriter writer)
                {
                    writer.WriteUndertaleObjectPointer(LegacyTiles);
                    writer.WriteUndertaleObjectPointer(Sprites);
                    if (writer.undertaleData.GMS2_3)
                    {
                        writer.WriteUndertaleObjectPointer(Sequences);
                        if (!writer.undertaleData.GMS2_3_2)
                            writer.WriteUndertaleObjectPointer(NineSlices);
                    }
                    writer.WriteUndertaleObject(LegacyTiles);
                    writer.WriteUndertaleObject(Sprites);
                    if (writer.undertaleData.GMS2_3)
                    {
                        writer.WriteUndertaleObject(Sequences);
                        if (!writer.undertaleData.GMS2_3_2)
                            writer.WriteUndertaleObject(NineSlices);
                    }
                }

                public void Unserialize(UndertaleReader reader)
                {
                    LegacyTiles = reader.ReadUndertaleObjectPointer<UndertalePointerList<Tile>>();
                    Sprites = reader.ReadUndertaleObjectPointer<UndertalePointerList<SpriteInstance>>();
                    if (reader.GMS2_3)
                    {
                        Sequences = reader.ReadUndertaleObjectPointer<UndertalePointerList<SequenceInstance>>();
                        if (!reader.undertaleData.GMS2_3_2)
                            NineSlices = reader.ReadUndertaleObjectPointer<UndertalePointerList<SpriteInstance>>();
                    }
                    reader.ReadUndertaleObject(LegacyTiles);
                    reader.ReadUndertaleObject(Sprites);
                    if (reader.GMS2_3)
                    {
                        reader.ReadUndertaleObject(Sequences);
                        if (!reader.undertaleData.GMS2_3_2)
                            reader.ReadUndertaleObject(NineSlices);
                    }
                }
            }
        }

        public class SpriteInstance : UndertaleObject, INotifyPropertyChanged
        {
            private UndertaleResourceById<UndertaleSprite, UndertaleChunkSPRT> _Sprite = new UndertaleResourceById<UndertaleSprite, UndertaleChunkSPRT>();

            public UndertaleString Name { get; set; }
            public UndertaleSprite Sprite { get => _Sprite.Resource; set { _Sprite.Resource = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Sprite))); } }
            public int X { get; set; }
            public int Y { get; set; }
            public float ScaleX { get; set; }
            public float ScaleY { get; set; }
            public uint Color { get; set; }
            public float AnimationSpeed { get; set; }
            public AnimationSpeedType AnimationSpeedType { get; set; }
            public float FrameIndex { get; set; }
            public float Rotation { get; set; }

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

        public class SequenceInstance : UndertaleObject, INotifyPropertyChanged
        {
            private UndertaleResourceById<UndertaleSequence, UndertaleChunkSEQN> _Sequence = new UndertaleResourceById<UndertaleSequence, UndertaleChunkSEQN>();

            public UndertaleString Name { get; set; }
            public UndertaleSequence Sequence { get => _Sequence.Resource; set { _Sequence.Resource = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Sequence))); } }
            public int X { get; set; }
            public int Y { get; set; }
            public float ScaleX { get; set; }
            public float ScaleY { get; set; }
            public uint Color { get; set; }
            public float AnimationSpeed { get; set; }
            public AnimationSpeedType AnimationSpeedType { get; set; }
            public float FrameIndex { get; set; }
            public float Rotation { get; set; }

            public event PropertyChangedEventHandler PropertyChanged;

            public void Serialize(UndertaleWriter writer)
            {
                writer.WriteUndertaleString(Name);
                writer.WriteUndertaleObject(_Sequence);
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
                _Sequence = reader.ReadUndertaleObject<UndertaleResourceById<UndertaleSequence, UndertaleChunkSEQN>>();
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
                return "Sequence " + Name?.Content + " of " + (Sequence?.Name?.Content ?? "?") + " (UndertaleRoom+SequenceInstance)";
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
