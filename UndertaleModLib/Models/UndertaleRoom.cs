using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModLib.Models
{
    /// <summary>
    /// A room in a data file.
    /// </summary>
    public class UndertaleRoom : UndertaleNamedResource, INotifyPropertyChanged
    {
        /// <summary>
        /// Certain flags a room can have.
        /// </summary>
        [Flags]
        public enum RoomEntryFlags : uint
        {
            /// <summary>
            /// Whether the room has Views enabled.
            /// </summary>
            EnableViews = 1,
            /// <summary>
            /// TODO not exactly sure, probably similar to <see cref="UndertaleRoom.DrawBackgroundColor"/>?
            /// </summary>
            ShowColor = 2,
            /// <summary>
            /// Whether the room should clear the display buffer.
            /// </summary>
            ClearDisplayBuffer = 4,
            /// <summary>
            /// Whether the room was made in Game Maker: Studio 2.
            /// </summary>
            IsGMS2 = 131072,
            /// <summary>
            /// Whether the room was made in Game Maker: Studio 2.3.
            /// </summary>
            IsGMS2_3 = 65536
        }

        /// <summary>
        /// The name of the room.
        /// </summary>
        public UndertaleString Name { get; set; }

        /// <summary>
        /// The caption of the room. Legacy variable that's used in pre- Game Maker: Studio.
        /// </summary>
        public UndertaleString Caption { get; set; }

        /// <summary>
        /// The Width of the room.
        /// </summary>
        public uint Width { get; set; } = 320;

        /// <summary>
        /// The height of the room.
        /// </summary>
        public uint Height { get; set; } = 240;

        /// <summary>
        /// The speed of the current room in steps.
        /// TODO: GMS1 only? IIRC gms2 deals with it differently.
        /// </summary>
        public uint Speed { get; set; } = 30;

        /// <summary>
        /// Whether this room is persistant.
        /// </summary>
        public bool Persistent { get; set; } = false;

        /// <summary>
        /// The background color of this room.
        /// </summary>
        public uint BackgroundColor { get; set; } = 0;

        /// <summary>
        /// Whether the display buffer should be cleared with Window Color.
        /// </summary>
        public bool DrawBackgroundColor { get; set; } = true;

        private UndertaleResourceById<UndertaleCode, UndertaleChunkCODE> _CreationCodeId = new();

        /// <summary>
        /// The creation code of this room.
        /// </summary>
        public UndertaleCode CreationCodeId { get => _CreationCodeId.Resource; set { _CreationCodeId.Resource = value; OnPropertyChanged(); } }

        /// <summary>
        /// The room flags this room has.
        /// </summary>
        public RoomEntryFlags Flags { get; set; } = RoomEntryFlags.EnableViews;

        //TODO
        public bool World { get; set; } = false;
        public uint Top { get; set; } = 0;
        public uint Left { get; set; } = 0;
        public uint Right { get; set; } = 1024;
        public uint Bottom { get; set; } = 768;

        /// <summary>
        /// The gravity towards x axis using room physics in m/s.
        /// </summary>
        public float GravityX { get; set; } = 0;

        /// <summary>
        /// The gravity towards y axis using room physics in m/s.
        /// </summary>
        public float GravityY { get; set; } = 10;

        /// <summary>
        /// The meters per pixel value for room physics.
        /// </summary>
        public float MetersPerPixel { get; set; } = 0.1f;

        /// <summary>
        /// The width of the room grid in pixels.
        /// </summary>
        public double GridWidth { get; set; } = 16d;

        /// <summary>
        /// The height of the room grid in pixels.
        /// </summary>
        public double GridHeight { get; set; } = 16d;

        /// <summary>
        /// The thickness of the room grid in pixels.
        /// </summary>
        public double GridThicknessPx { get; set; } = 1d;
        private UndertalePointerList<Layer> _layers = new();

        /// <summary>
        /// The list of backgrounds this room uses.
        /// </summary>
        public UndertalePointerList<Background> Backgrounds { get; private set; } = new UndertalePointerList<Background>();

        /// <summary>
        /// The list of views this room uses.
        /// </summary>
        public UndertalePointerList<View> Views { get; private set; } = new UndertalePointerList<View>();

        /// <summary>
        /// The list of game objects this room uses.
        /// </summary>
        public UndertalePointerListLenCheck<GameObject> GameObjects { get; private set; } = new UndertalePointerListLenCheck<GameObject>();

        /// <summary>
        /// The list of tiles this room uses.
        /// </summary>
        public UndertalePointerList<Tile> Tiles { get; private set; } = new UndertalePointerList<Tile>();

        /// <summary>
        /// The list of layers this room uses. Used in Game Maker Studio: 2 only, as <see cref="Backgrounds"/> and <see cref="Tiles"/> are empty there.
        /// </summary>
        public UndertalePointerList<Layer> Layers { get => _layers; private set { _layers = value; UpdateBGColorLayer(); OnPropertyChanged(); } }

        /// <summary>
        /// The list of sequences this room uses.
        /// </summary>
        public UndertaleSimpleList<UndertaleResourceById<UndertaleSequence, UndertaleChunkSEQN>> Sequences { get; private set; } = new UndertaleSimpleList<UndertaleResourceById<UndertaleSequence, UndertaleChunkSEQN>>();

        private Layer GetBGColorLayer()
        {
            return _layers?.Where(l => l.LayerType is LayerType.Background
                                    && l.BackgroundData.Sprite is null
                                    && l.BackgroundData.Color != 0)
                           .OrderBy(l => l?.LayerDepth ?? 0)
                           .FirstOrDefault();
        }
        public void UpdateBGColorLayer() => OnPropertyChanged("BGColorLayer");

        /// <summary>
        /// The layer containing the background color.
        /// </summary>
        public Layer BGColorLayer => GetBGColorLayer();

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

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
            writer.Write(BackgroundColor ^ 0xFF000000); // remove alpha (background color doesn't have alpha)
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
            BackgroundColor = 0xFF000000 | reader.ReadUInt32(); // make alpha 255 (background color doesn't have alpha)
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
                            if (GameObjects.ByInstanceID(id) != null)
                                layer.InstancesData.Instances.Add(GameObjects.ByInstanceID(id));
                            else
                            {
                                /* Attempt to resolve null objects.
                                 * Sometimes, the instance ID in GameObjects will end up a duplicate
                                 * of a previous ID, rather than the correct one.
                                 * So, we traverse the object list a little to find the correct one.
                                 * If you can get two broken objects in a row... it'll probably crash.
                                 */
                                int foundIndex = GameObjects.IndexOf(GameObjects.ByInstanceID(id - 1));
                                layer.InstancesData.Instances.Add(GameObjects[foundIndex + 1]);
                            }
                        }
                    }
                }

                if (sequences)
                    reader.ReadUndertaleObject(Sequences);
            }
        }

        public void SetupRoom(bool calculateGrid = true)
        {
            foreach (Layer layer in Layers)
            {
                if (layer != null)
                    layer.ParentRoom = this;
            }
            foreach (UndertaleRoom.Background bgnd in Backgrounds)
                bgnd.ParentRoom = this;

            if (calculateGrid)
            {
                // Automagically set the grid size to whatever most tiles are sized

                Dictionary<Point, uint> tileSizes = new();
                IEnumerable<Tile> tileList;

                if (Layers.Count > 0)
                {
                    tileList = new List<Tile>();
                    foreach (Layer layer in Layers)
                    {
                        if (layer.LayerType == LayerType.Assets)
                            tileList = tileList.Concat(layer.AssetsData.LegacyTiles);
                        else if (layer.LayerType == LayerType.Tiles && layer.TilesData.TileData.Length != 0)
                        {
                            int w = (int)(Width / layer.TilesData.TilesX);
                            int h = (int)(Height / layer.TilesData.TilesY);
                            tileSizes[new(w, h)] = layer.TilesData.TilesX * layer.TilesData.TilesY;
                        }
                    }

                }
                else
                    tileList = Tiles;

                // Loop through each tile and save how many times their sizes are used
                foreach (Tile tile in tileList)
                {
                    Point scale = new((int)tile.Width, (int)tile.Height);
                    if (tileSizes.ContainsKey(scale))
                    {
                        tileSizes[scale]++;
                    }
                    else
                    {
                        tileSizes.Add(scale, 1);
                    }
                }

                // If tiles exist at all, grab the most used tile size and use that as our grid size
                if (tileSizes.Count > 0)
                {
                    var largestKey = tileSizes.Aggregate((x, y) => x.Value > y.Value ? x : y).Key;
                    GridWidth = largestKey.X;
                    GridHeight = largestKey.Y;
                }
            }
        }

        public override string ToString()
        {
            return Name.Content + " (" + GetType().Name + ")";
        }

        /// <summary>
        /// Interface for objects within rooms.
        /// </summary>
        public interface RoomObject
        {
            /// <summary>
            /// X coordinate of the object.
            /// </summary>
            int X { get; }

            /// <summary>
            /// Y coordinate of the object.
            /// </summary>
            int Y { get; }

            /// <summary>
            /// Instance id of the object.
            /// </summary>
            uint InstanceID { get; }
        }

        /// <summary>
        /// A background with properties as it's used in a room.
        /// </summary>
        public class Background : UndertaleObject, INotifyPropertyChanged
        {
            private UndertaleRoom _ParentRoom;

            /// <summary>
            /// The room parent this background belongs to.
            /// </summary>
            public UndertaleRoom ParentRoom { get => _ParentRoom; set { _ParentRoom = value; OnPropertyChanged(); UpdateStretch(); } }

            //TODO:
            public float CalcScaleX { get; set; } = 1;
            public float CalcScaleY { get; set; } = 1;

            /// <summary>
            /// Whether this background is enabled.
            /// </summary>
            public bool Enabled { get; set; } = false;

            /// <summary>
            /// Whether this acts as a foreground.
            /// </summary>
            public bool Foreground { get; set; } = false;
            private UndertaleResourceById<UndertaleBackground, UndertaleChunkBGND> _BackgroundDefinition = new();

            /// <summary>
            /// The background asset this uses.
            /// </summary>
            public UndertaleBackground BackgroundDefinition { get => _BackgroundDefinition.Resource; set { _BackgroundDefinition.Resource = value; OnPropertyChanged(); } }
            private int _X = 0;
            private int _Y = 0;

            /// <summary>
            /// The x coordinate of the background in the room.
            /// </summary>
            public int X { get => _X; set { _X = value; OnPropertyChanged(); UpdateStretch(); } }

            /// <summary>
            /// The y coordinate of the background in the room.
            /// </summary>
            public int Y { get => _Y; set { _Y = value; OnPropertyChanged(); UpdateStretch(); } }
            public int TileX { get; set; } = 1;
            public int TileY { get; set; } = 1;

            /// <summary>
            /// Horizontal speed of the background.
            /// </summary>
            public int SpeedX { get; set; } = 0;

            /// <summary>
            /// Vertical speed of the background.
            /// </summary>
            public int SpeedY { get; set; } = 0;

            private bool _Stretch = false;

            /// <summary>
            /// Whether this background is stretched
            /// </summary>
            public bool Stretch { get => _Stretch; set { _Stretch = value; OnPropertyChanged(); UpdateStretch(); } }

            /// <summary>
            /// Whether this background is tiled horizontally.
            /// </summary>
            public bool TiledHorizontally { get => TileX > 0; set { TileX = value ? 1 : 0; OnPropertyChanged(); } }

            /// <summary>
            /// Whether this background is tiled vertically.
            /// </summary>
            public bool TiledVertically { get => TileY > 0; set { TileY = value ? 1 : 0; OnPropertyChanged(); } }

            /// <summary>
            /// A horizontal offset used for proper background display in the UndertaleModTool room editor.
            /// </summary>
            /// <remarks>
            /// This attribute is UMT-only and does not exist in GameMaker.
            /// </remarks>
            public int XOffset => X + (BackgroundDefinition?.Texture?.TargetX ?? 0);

            /// <summary>
            /// A vertical offset used for proper background display in the UndertaleModTool room editor.
            /// </summary>
            /// <remarks>
            /// This attribute is UMT-only and does not exist in GameMaker.
            /// </remarks>
            public int YOffset => Y + (BackgroundDefinition?.Texture?.TargetY ?? 0);

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged([CallerMemberName] string name = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }

            public void UpdateStretch()
            {
                bool hasRoom = (ParentRoom != null) && (BackgroundDefinition != null);
                CalcScaleX = (hasRoom && Stretch) ? (ParentRoom.Width / (float)BackgroundDefinition.Texture.SourceWidth) : 1;
                CalcScaleY = (hasRoom && Stretch) ? (ParentRoom.Height / (float)BackgroundDefinition.Texture.SourceHeight) : 1;
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

        /// <summary>
        /// A view with properties as it's used in a room.
        /// </summary>
        public class View : UndertaleObject, INotifyPropertyChanged
        {
            /// <summary>
            /// Whether this view is enabled.
            /// </summary>
            public bool Enabled { get; set; } = false;

            /// <summary>
            /// The x coordinate of the view in the room.
            /// </summary>
            public int ViewX { get; set; }

            /// <summary>
            /// The y coordinate of the view in the room.
            /// </summary>
            public int ViewY { get; set; }

            /// <summary>
            /// The width of the view.
            /// </summary>
            public int ViewWidth { get; set; } = 640;

            /// <summary>
            /// The height of the view.
            /// </summary>
            public int ViewHeight { get; set; } = 480;

            /// <summary>
            /// The x coordinate of the viewport on the screen.
            /// </summary>
            public int PortX { get; set; }

            /// <summary>
            /// The y coordinate of the viewport on the screen.
            /// </summary>
            public int PortY { get; set; }

            /// <summary>
            /// The width of the viewport on the screen.
            /// </summary>
            public int PortWidth { get; set; } = 640;

            /// <summary>
            /// The height of the viewport on the screen.
            /// </summary>
            public int PortHeight { get; set; } = 480;

            /// <summary>
            /// The horizontal border of the view for view following.
            /// </summary>
            public uint BorderX { get; set; } = 32;

            /// <summary>
            /// The vertical border of the view for view following.
            /// </summary>
            public uint BorderY { get; set; } = 32;

            /// <summary>
            /// The horizontal movement speed of the view.
            /// </summary>
            public int SpeedX { get; set; } = -1;

            /// <summary>
            /// The vertical movement speed of the view.
            /// </summary>
            public int SpeedY { get; set; } = -1;

            private UndertaleResourceById<UndertaleGameObject, UndertaleChunkOBJT> _ObjectId = new();

            /// <summary>
            /// The object the view should follow.
            /// </summary>
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

        /// <summary>
        /// A game object with properties as it's used in a room.
        /// </summary>
        public class GameObject : UndertaleObjectLenCheck, RoomObject, INotifyPropertyChanged
        {
            private UndertaleResourceById<UndertaleGameObject, UndertaleChunkOBJT> _objectDefinition = new();
            private UndertaleResourceById<UndertaleCode, UndertaleChunkCODE> _creationCode = new();
            private UndertaleResourceById<UndertaleCode, UndertaleChunkCODE> _preCreateCode = new();

            /// <summary>
            /// The x coordinate of this object.
            /// </summary>
            public int X { get; set; }

            /// <summary>
            /// The y coordinate of this object.
            /// </summary>
            public int Y { get; set; }

            /// <summary>
            /// The game object that is used.
            /// </summary>
            public UndertaleGameObject ObjectDefinition { get => _objectDefinition.Resource; set { _objectDefinition.Resource = value; OnPropertyChanged(); OnPropertyChanged("CurrentTexture"); } }

            /// <summary>
            /// The instance id of this object.
            /// </summary>
            public uint InstanceID { get; set; }

            /// <summary>
            /// The creation code for this object.
            /// </summary>
            public UndertaleCode CreationCode { get => _creationCode.Resource; set { _creationCode.Resource = value; OnPropertyChanged(); } }

            /// <summary>
            /// The x scale that's applied for this object.
            /// </summary>
            public float ScaleX { get; set; } = 1;

            /// <summary>
            /// The y scale that's applied for this object.
            /// </summary>
            public float ScaleY { get; set; } = 1;

            //TODO: no idea
            public uint Color { get; set; } = 0xFFFFFFFF;

            /// <summary>
            /// The rotation of this object.
            /// </summary>
            public float Rotation { get; set; }

            /// <summary>
            /// The pre creation code of this object.
            /// </summary>
            public UndertaleCode PreCreateCode { get => _preCreateCode.Resource; set { _preCreateCode.Resource = value; OnPropertyChanged(); } }

            /// <summary>
            /// The image speed of this object. Game Maker: Studio 2 only.
            /// </summary>
            public float ImageSpeed { get; set; }

            /// <summary>
            /// The image index of this object. Game Maker: Studio 2 only.
            /// </summary>
            public int ImageIndex { get; set; }

            /// <summary>
            /// A wrapper for <see cref="ImageIndex"/> that returns the value being wrapped around available frames of the sprite.<br/>
            /// For example, if this sprite has 3 frames, and the index is 5, then this will return 2.
            /// </summary>
            /// <remarks>
            /// This attribute is UMT-only and does not exist in GameMaker.
            /// </remarks>
            public int WrappedImageIndex
            { 
                get
                {
                    if (ObjectDefinition?.Sprite is null)
                        return 0;

                    int count = ObjectDefinition.Sprite.Textures.Count;
                    if (count == 0)
                        return 0;

                    return ((ImageIndex % count) + count) % count;
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged([CallerMemberName] string name = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }

            /// <summary>
            /// The opposite angle of the current rotation.
            /// </summary>
            public float OppositeRotation => 360F - (Rotation % 360);

            /// <summary>
            /// A horizontal offset used for proper game object display in the UndertaleModTool room editor.
            /// </summary>
            /// <remarks>
            /// This attribute is UMT-only and does not exist in GameMaker.
            /// </remarks>
            public int XOffset => ObjectDefinition?.Sprite != null
                                  ? X - ObjectDefinition.Sprite.OriginX + (ObjectDefinition.Sprite.Textures.ElementAtOrDefault(ImageIndex)?.Texture?.TargetX ?? 0)
                                  : X;

            /// <summary>
            /// A vertical offset used for proper game object display in the UndertaleModTool room editor.
            /// </summary>
            /// <remarks>
            /// This attribute is UMT-only and does not exist in GameMaker.
            /// </remarks>
            public int YOffset => ObjectDefinition?.Sprite != null
                                  ? Y - ObjectDefinition.Sprite.OriginY + (ObjectDefinition.Sprite.Textures.ElementAtOrDefault(ImageIndex)?.Texture?.TargetY ?? 0)
                                  : Y;

            public void Serialize(UndertaleWriter writer)
            {
                writer.Write(X);
                writer.Write(Y);
                writer.WriteUndertaleObject(_objectDefinition);
                writer.Write(InstanceID);
                writer.WriteUndertaleObject(_creationCode);
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
                    writer.WriteUndertaleObject(_preCreateCode);         // Note: Appears in GM:S 1.4.9999 as well, so that's probably the closest it gets
            }

            public void Unserialize(UndertaleReader reader)
            {
                Unserialize(reader, -1);
            }

            public void Unserialize(UndertaleReader reader, int length)
            {
                X = reader.ReadInt32();
                Y = reader.ReadInt32();
                _objectDefinition = reader.ReadUndertaleObject<UndertaleResourceById<UndertaleGameObject, UndertaleChunkOBJT>>();
                InstanceID = reader.ReadUInt32();
                _creationCode = reader.ReadUndertaleObject<UndertaleResourceById<UndertaleCode, UndertaleChunkCODE>>();
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
                    _preCreateCode = reader.ReadUndertaleObject<UndertaleResourceById<UndertaleCode, UndertaleChunkCODE>>(); // Note: Appears in GM:S 1.4.9999 as well, so that's probably the closest it gets
            }

            public override string ToString()
            {
                return "Instance " + InstanceID + " of " + (ObjectDefinition?.Name?.Content ?? "?") + " (UndertaleRoom+GameObject)";
            }
        }

        /// <summary>
        /// A tile with properties as it's used in a room.
        /// </summary>
        public class Tile : UndertaleObject, RoomObject, INotifyPropertyChanged
        {
            /// <summary>
            /// Whether this tile is from an asset layer. Game Maker Studio: 2 exclusive.
            /// </summary>
            public bool spriteMode = false;
            private UndertaleResourceById<UndertaleBackground, UndertaleChunkBGND> _backgroundDefinition = new();
            private UndertaleResourceById<UndertaleSprite, UndertaleChunkSPRT> _spriteDefinition = new();

            /// <summary>
            /// The x coordinate of the tile in the room.
            /// </summary>
            public int X { get; set; }

            /// <summary>
            /// The y coordinate of the tile in the room.
            /// </summary>
            public int Y { get; set; }

            /// <summary>
            /// From which tileset / background the tile stems from.
            /// </summary>
            public UndertaleBackground BackgroundDefinition { get => _backgroundDefinition.Resource; set { _backgroundDefinition.Resource = value; OnPropertyChanged(); OnPropertyChanged("ObjectDefinition"); } }

            /// <summary>
            /// From which sprite this tile stems from.
            /// </summary>
            public UndertaleSprite SpriteDefinition { get => _spriteDefinition.Resource; set { _spriteDefinition.Resource = value; OnPropertyChanged(); OnPropertyChanged("ObjectDefinition"); } }

            /// <summary>
            /// From which object this tile stems from.
            /// Will return a <see cref="UndertaleBackground"/> if <see cref="spriteMode"/> is disabled, a <see cref="UndertaleSprite"/> if it's enabled.
            /// </summary>
            public UndertaleNamedResource ObjectDefinition { get => spriteMode ? SpriteDefinition : BackgroundDefinition; set { if (spriteMode) SpriteDefinition = (UndertaleSprite)value; else BackgroundDefinition = (UndertaleBackground)value; } }

            /// <summary>
            /// The x coordinate of the tile in <see cref="ObjectDefinition"/>.
            /// </summary>
            public uint SourceX { get; set; }

            /// <summary>
            /// The y coordinate of the tile in <see cref="ObjectDefinition"/>.
            /// </summary>
            public uint SourceY { get; set; }

            /// <summary>
            /// The width of the tile.
            /// </summary>
            public uint Width { get; set; }

            /// <summary>
            /// The height of the tile.
            /// </summary>
            public uint Height { get; set; }

            /// <summary>
            /// The depth value of this tile.
            /// </summary>
            public int TileDepth { get; set; }

            /// <summary>
            /// The instance id of this tile.
            /// </summary>
            public uint InstanceID { get; set; }

            /// <summary>
            /// The x scale that's applied for this tile.
            /// </summary>
            public float ScaleX { get; set; } = 1;

            /// <summary>
            /// The y scale that's applied for this tile.
            /// </summary>
            public float ScaleY { get; set; } = 1;

            //TODO?
            public uint Color { get; set; } = 0xFFFFFFFF;

            public UndertaleTexturePageItem Tpag => spriteMode ? SpriteDefinition?.Textures.FirstOrDefault()?.Texture : BackgroundDefinition?.Texture; // TODO: what happens on sprites with multiple textures?

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged([CallerMemberName] string name = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }

            public void Serialize(UndertaleWriter writer)
            {
                writer.Write(X);
                writer.Write(Y);
                if (spriteMode != (writer.undertaleData.GeneralInfo.Major >= 2))
                    throw new Exception("Unsupported in GMS" + writer.undertaleData.GeneralInfo.Major);
                if (spriteMode)
                    writer.WriteUndertaleObject(_spriteDefinition);
                else
                    writer.WriteUndertaleObject(_backgroundDefinition);
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
                spriteMode = reader.undertaleData.GeneralInfo.Major >= 2;
                if (spriteMode)
                    _spriteDefinition = reader.ReadUndertaleObject<UndertaleResourceById<UndertaleSprite, UndertaleChunkSPRT>>();
                else
                    _backgroundDefinition = reader.ReadUndertaleObject<UndertaleResourceById<UndertaleBackground, UndertaleChunkBGND>>();
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
        /// <summary>
        /// The layer type for a specific layer. In Game Maker: Studio 2, <see cref="UndertaleRoom.Backgrounds"/>, <see cref="UndertaleRoom.Tiles"/>
        /// are empty and this is used instead.
        /// </summary>
        public enum LayerType
        {
            Background = 1,
            Instances = 2,
            Assets = 3,
            Tiles = 4,
            Effect = 6
        }

        /// <summary>
        /// A layer with properties as it's used in a room. Game Maker: Studio 2 only.
        /// </summary>
        //TODO: everything from here on is mostly gms2 related which i dont have much experience with
        public class Layer : UndertaleObject, INotifyPropertyChanged
        {
            public interface LayerData : UndertaleObject
            {
            }

            private UndertaleRoom _ParentRoom;
            private int _layerDepth;

            /// <summary>
            /// The room this layer belongs to.
            /// </summary>
            public UndertaleRoom ParentRoom { get => _ParentRoom; set { _ParentRoom = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ParentRoom))); UpdateParentRoom(); } }

            /// <summary>
            /// The name of the layer.
            /// </summary>
            public UndertaleString LayerName { get; set; }

            /// <summary>
            /// The id of the layer.
            /// </summary>
            public uint LayerId { get; set; }

            /// <summary>
            /// The type of this layer.
            /// </summary>
            public LayerType LayerType { get; set; }

            /// <summary>
            /// The depth of this layer.
            /// </summary>
            public int LayerDepth { get => _layerDepth; set { _layerDepth = value; ParentRoom?.UpdateBGColorLayer(); } }

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
            public LayerEffectData EffectData => Data as LayerEffectData;

            public event PropertyChangedEventHandler PropertyChanged;

            // GMS 2022.1+
            public bool EffectEnabled { get; set; }
            public UndertaleString EffectType { get; set; }
            public UndertaleSimpleList<EffectProperty> EffectProperties { get; set; }

            public void UpdateParentRoom()
            {
                if (BackgroundData != null)
                    BackgroundData.ParentLayer = this;
                if (TilesData != null)
                    TilesData.ParentLayer = this;
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

                if (writer.undertaleData.GMS2022_1)
                {
                    writer.Write(EffectEnabled);
                    writer.WriteUndertaleString(EffectType);
                    writer.WriteUndertaleObject(EffectProperties);
                }

                switch (LayerType)
                {
                    case LayerType.Instances: writer.WriteUndertaleObject(InstancesData); break;
                    case LayerType.Tiles: writer.WriteUndertaleObject(TilesData); break;
                    case LayerType.Background: writer.WriteUndertaleObject(BackgroundData); break;
                    case LayerType.Assets: writer.WriteUndertaleObject(AssetsData); break;
                    case LayerType.Effect: writer.WriteUndertaleObject(EffectData); break;
                    default: throw new Exception("Unsupported layer type " + LayerType);
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

                if (reader.undertaleData.GMS2022_1)
                {
                    EffectEnabled = reader.ReadBoolean();
                    EffectType = reader.ReadUndertaleString();
                    EffectProperties = reader.ReadUndertaleObject<UndertaleSimpleList<EffectProperty>>();
                }

                switch (LayerType)
                {
                    case LayerType.Instances: Data = reader.ReadUndertaleObject<LayerInstancesData>(); break;
                    case LayerType.Tiles: Data = reader.ReadUndertaleObject<LayerTilesData>(); break;
                    case LayerType.Background: Data = reader.ReadUndertaleObject<LayerBackgroundData>(); break;
                    case LayerType.Assets: Data = reader.ReadUndertaleObject<LayerAssetsData>(); break;
                    case LayerType.Effect: Data = reader.ReadUndertaleObject<LayerEffectData>(); break;
                    default: throw new Exception("Unsupported layer type " + LayerType);
                }
            }

            public class LayerInstancesData : LayerData
            {
                internal uint[] _InstanceIds { get; private set; } // 100000, 100001, 100002, 100003 - instance ids from GameObjects list in the room
                public ObservableCollection<UndertaleRoom.GameObject> Instances { get; private set; } = new();

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
                private UndertaleResourceById<UndertaleBackground, UndertaleChunkBGND> _Background = new(); // In GMS2 backgrounds are just tilesets
                private uint _TilesX;
                private uint _TilesY;
                private uint[][] _TileData; // Each is simply an ID from the tileset/background/sprite

                public Layer ParentLayer { get; set; }
                public UndertaleBackground Background { get => _Background.Resource; set { _Background.Resource = value; OnPropertyChanged(); } }
                public uint TilesX
                {
                    get => _TilesX; set
                    {
                        _TilesX = value; OnPropertyChanged();
                        if (_TileData != null)
                        {
                            for (var y = 0; y < _TileData.Length; y++)
                            {
                                Array.Resize(ref _TileData[y], (int)value);
                            }
                            OnPropertyChanged("TileData");
                        }
                    }
                }
                public uint TilesY
                {
                    get => _TilesY; set
                    {
                        _TilesY = value; OnPropertyChanged();
                        if (_TileData != null)
                        {
                            Array.Resize(ref _TileData, (int)value);
                            for (var y = 0; y < _TileData.Length; y++)
                            {
                                if (_TileData[y] == null)
                                    _TileData[y] = new uint[TilesX];
                            }
                            OnPropertyChanged("TileData");
                        }
                    }
                }
                public uint[][] TileData { get => _TileData; set { _TileData = value; OnPropertyChanged(); } }

                public event PropertyChangedEventHandler PropertyChanged;
                protected void OnPropertyChanged([CallerMemberName] string name = null)
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
                }

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

                private UndertaleResourceById<UndertaleSprite, UndertaleChunkSPRT> _Sprite = new(); // Apparently there's a mode where it's a background reference, but probably not necessary
                private bool _TiledHorizontally;
                private bool _TiledVertically;
                private bool _Stretch;

                public Layer ParentLayer { get => _ParentLayer; set { _ParentLayer = value; OnPropertyChanged(); UpdateScale(); } }
                public float CalcScaleX { get; set; }
                public float CalcScaleY { get; set; }

                public bool Visible { get; set; }
                public bool Foreground { get; set; }
                public UndertaleSprite Sprite { get => _Sprite.Resource; set { _Sprite.Resource = value; OnPropertyChanged(); ParentLayer.ParentRoom.UpdateBGColorLayer(); } }
                public bool TiledHorizontally { get => _TiledHorizontally; set { _TiledHorizontally = value; OnPropertyChanged(); } }
                public bool TiledVertically { get => _TiledVertically; set { _TiledVertically = value; OnPropertyChanged(); } }
                public bool Stretch { get => _Stretch; set { _Stretch = value; OnPropertyChanged(); } }
                public uint Color { get; set; }
                public float FirstFrame { get; set; }
                public float AnimationSpeed { get; set; }
                public AnimationSpeedType AnimationSpeedType { get; set; }

                public float XOffset => (ParentLayer?.XOffset ?? 0) + (Sprite?.Textures.FirstOrDefault()?.Texture?.TargetX ?? 0);
                public float YOffset => (ParentLayer?.YOffset ?? 0) + (Sprite?.Textures.FirstOrDefault()?.Texture?.TargetY ?? 0);

                public event PropertyChangedEventHandler PropertyChanged;
                protected void OnPropertyChanged([CallerMemberName] string name = null)
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
                }

                public void UpdateScale()
                {
                    bool hasRoom = (ParentLayer != null) && (ParentLayer.ParentRoom != null) && (Sprite != null);
                    CalcScaleX = (hasRoom && Stretch) ? (ParentLayer.ParentRoom.Width / (float)Sprite.Width) : 1;
                    CalcScaleY = (hasRoom && Stretch) ? (ParentLayer.ParentRoom.Height / (float)Sprite.Height) : 1;
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

            [PropertyChanged.AddINotifyPropertyChangedInterface]
            public class LayerEffectData : LayerData
            {
                public UndertaleString EffectType;
                public UndertaleSimpleList<EffectProperty> Properties;

                public void Serialize(UndertaleWriter writer)
                {
                    if (writer.undertaleData.GMS2022_1)
                        return;
                    writer.WriteUndertaleString(EffectType);
                    writer.WriteUndertaleObject(Properties);
                }

                public void Unserialize(UndertaleReader reader)
                {
                    if (reader.undertaleData.GMS2022_1)
                        return;
                    EffectType = reader.ReadUndertaleString();
                    Properties = reader.ReadUndertaleObject<UndertaleSimpleList<EffectProperty>>();
                }
            }
        }

        [PropertyChanged.AddINotifyPropertyChangedInterface]
        public class EffectProperty : UndertaleObject
        {
            public enum PropertyType
            {
                Real = 0,
                Color = 1,
                Sampler = 2
            }

            public PropertyType Kind { get; set; }
            public UndertaleString Name { get; set; }
            public UndertaleString Value { get; set; }

            public void Serialize(UndertaleWriter writer)
            {
                writer.Write((int)Kind);
                writer.WriteUndertaleString(Name);
                writer.WriteUndertaleString(Value);
            }

            public void Unserialize(UndertaleReader reader)
            {
                Kind = (PropertyType)reader.ReadInt32();
                Name = reader.ReadUndertaleString();
                Value = reader.ReadUndertaleString();
            }
        }

        public class SpriteInstance : UndertaleObject, INotifyPropertyChanged
        {
            private UndertaleResourceById<UndertaleSprite, UndertaleChunkSPRT> _Sprite = new();

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
            public int WrappedFrameIndex
            {
                get
                {
                    if (Sprite is null)
                        return 0;

                    int count = Sprite.Textures.Count;
                    if (count == 0)
                        return 0;

                    return (((int)FrameIndex % count) + count) % count;
                }
            }
            public float Rotation { get; set; }
            public float OppositeRotation => 360F - Rotation;
            public int XOffset => Sprite is not null
                                  ? X - Sprite.OriginX + (Sprite.Textures.ElementAtOrDefault((int)FrameIndex)?.Texture?.TargetX ?? 0)
                                  : X;
            public int YOffset => Sprite is not null
                                  ? Y - Sprite.OriginY + (Sprite.Textures.ElementAtOrDefault((int)FrameIndex)?.Texture?.TargetY ?? 0)
                                  : Y;

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged([CallerMemberName] string name = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }

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
            private UndertaleResourceById<UndertaleSequence, UndertaleChunkSEQN> _Sequence = new();

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
