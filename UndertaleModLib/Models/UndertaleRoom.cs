using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;

namespace UndertaleModLib.Models;

/// <summary>
/// A room in a data file.
/// </summary>
public class UndertaleRoom : UndertaleNamedResource, INotifyPropertyChanged, IDisposable
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

    private UndertaleResourceById<UndertaleCode, UndertaleChunkCODE> _creationCodeId = new();

    /// <summary>
    /// The creation code of this room.
    /// </summary>
    public UndertaleCode CreationCodeId { get => _creationCodeId.Resource; set { _creationCodeId.Resource = value; OnPropertyChanged(); } }

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

    private double _gridWidth = 16.0;
    private double _gridHeight = 16.0;

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
    public double GridWidth { get => _gridWidth; set { if (value >= 0) _gridWidth = value; } }

    /// <summary>
    /// The height of the room grid in pixels.
    /// </summary>
    public double GridHeight { get => _gridHeight; set { if (value >= 0) _gridHeight = value; } }

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
    public UndertalePointerList<GameObject> GameObjects { get; private set; } = new UndertalePointerList<GameObject>();

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

    public static bool CheckedForGMS2_2_2_302;

    /// <summary>
    /// Calls <see cref="OnPropertyChanged(string)"/> for <see cref="BGColorLayer"/> in order to update the room background color.<br/>
    /// Only used for GameMaker: Studio 2 rooms.
    /// </summary>
    public void UpdateBGColorLayer() => OnPropertyChanged("BGColorLayer");

    /// <summary>
    /// Checks whether <see cref="Layers"/> is ordered by <see cref="Layer.LayerDepth"/>.
    /// </summary>
    /// <returns><see langword="true"/> if <see cref="Layers"/> is ordered, and <see langword="false"/> otherwise.</returns>
    public bool CheckLayersDepthOrder()
    {
        for (int i = 0; i < Layers.Count - 1; i++)
        {
            if (Layers[i].LayerDepth > Layers[i + 1].LayerDepth)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Orders <see cref="Layers"/> by depth.
    /// </summary>
    /// <param name="layerProperties">
    /// A <see cref="Tuple"/> that consists of: the selected layer (in the room editor), ordered layers array and selected layer index.
    /// This parameter is used only by <c>LayerZIndexConverter</c> (part of the room editor UI).
    /// </param>
    public void RearrangeLayers(Tuple<Layer, Layer[], int> layerProperties = null)
    {
        if (Layers.Count == 0)
            return;

        Layer[] orderedLayers = null;
        Layer selectedLayer = null;
        int selectedLayerIndex = -1;
        if (layerProperties is not null)
        {
            orderedLayers = layerProperties.Item2;
            selectedLayer = layerProperties.Item1;
            selectedLayerIndex = layerProperties.Item3;
        }
        else
        {
            orderedLayers = Layers.OrderBy(l => l.LayerDepth).ToArray();
            selectedLayerIndex = Array.IndexOf(orderedLayers, selectedLayer);
        }

        // Ensure that room objects tree will have the layer to re-select
        if (selectedLayer is not null)
            Layers[selectedLayerIndex] = selectedLayer;

        for (int i = 0; i < orderedLayers.Length; i++)
        {
            if (Layers[i] != orderedLayers[i])
                Layers[i] = orderedLayers[i];
        }
    }

    /// <summary>
    /// The layer containing the background color.<br/>
    /// </summary>
    /// <remarks>
    /// Used by "BGColorConverter" of the UndertaleModTool room editor.
    /// This attribute is UMT-only and does not exist in GameMaker.
    /// </remarks>
    public Layer BGColorLayer
    {
        get
        {
            return _layers?.Where(l => l.LayerType is LayerType.Background
                                       && l.BackgroundData.Sprite is null
                                       && l.BackgroundData.Color != 0)
                           .OrderBy(l => l.LayerDepth)
                           .FirstOrDefault();
        }
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// Initializes a new instance of <see cref="UndertaleRoom"/>.
    /// </summary>
    public UndertaleRoom()
    {
        Backgrounds.SetCapacity(8);
        Views.SetCapacity(8);
        for (int i = 0; i < 8; i++)
            Backgrounds.InternalAdd(new Background());
        for (int i = 0; i < 8; i++)
            Views.InternalAdd(new View());
        if (Flags.HasFlag(RoomEntryFlags.EnableViews))
            Views[0].Enabled = true;
    }

    private static void CheckForGMS2_2_2_302(UndertaleReader reader)
    {
        if (reader.undertaleData.IsVersionAtLeast(2, 2, 2, 302))
        {
            CheckedForGMS2_2_2_302 = true;

            uint newSize = GameObject.ChildObjectsSize + 8;
            reader.SetStaticChildObjectsSize(typeof(GameObject), newSize);

            return;
        }

        long returnTo = reader.Position;
        reader.Position -= 4;

        uint gameObjPtr = reader.ReadUInt32();
        uint tilePtr = reader.ReadUInt32();

        reader.AbsPosition = gameObjPtr; // "GameObjects"
        uint objCount = reader.ReadUInt32();
        if (objCount > 0)
        {
            uint firstPtr = reader.ReadUInt32();
            uint secondPtr;
            if (objCount == 1)
                secondPtr = tilePtr;
            else
                secondPtr = reader.ReadUInt32();

            if (secondPtr - firstPtr == 48)
            {
                reader.undertaleData.SetGMS2Version(2, 2, 2, 302);

                //"GameObject.ImageSpeed" + "...ImageIndex"
                uint newSize = GameObject.ChildObjectsSize + 8;
                reader.SetStaticChildObjectsSize(typeof(GameObject), newSize);
            }
        }

        reader.Position = returnTo;

        CheckedForGMS2_2_2_302 = true;
    }

    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        if (writer.undertaleData.IsGameMaker2())
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
        writer.WriteUndertaleObject(_creationCodeId);
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
        if (writer.undertaleData.IsGameMaker2())
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
        if (writer.undertaleData.IsGameMaker2())
        {
            writer.WriteUndertaleObject(Layers);

            if (sequences)
                writer.WriteUndertaleObject(Sequences);
        }
    }

    /// <inheritdoc />
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
        _creationCodeId = reader.ReadUndertaleObject<UndertaleResourceById<UndertaleCode, UndertaleChunkCODE>>();
        Flags = (RoomEntryFlags)reader.ReadUInt32();
        Backgrounds = reader.ReadUndertaleObjectPointer<UndertalePointerList<Background>>();
        Views = reader.ReadUndertaleObjectPointer<UndertalePointerList<View>>();
        GameObjects = reader.ReadUndertaleObjectPointer<UndertalePointerList<GameObject>>();

        if (!CheckedForGMS2_2_2_302)
            CheckForGMS2_2_2_302(reader);
        
        Tiles = reader.ReadUndertaleObjectPointer<UndertalePointerList<Tile>>();
        World = reader.ReadBoolean();
        Top = reader.ReadUInt32();
        Left = reader.ReadUInt32();
        Right = reader.ReadUInt32();
        Bottom = reader.ReadUInt32();
        GravityX = reader.ReadSingle();
        GravityY = reader.ReadSingle();
        MetersPerPixel = reader.ReadSingle();
        bool sequences = false;
        if (reader.undertaleData.IsGameMaker2())
        {
            Layers = reader.ReadUndertaleObjectPointer<UndertalePointerList<Layer>>();
            sequences = reader.undertaleData.IsVersionAtLeast(2, 3);
            if (sequences)
                Sequences = reader.ReadUndertaleObjectPointer<UndertaleSimpleList<UndertaleResourceById<UndertaleSequence, UndertaleChunkSEQN>>>();
        }
        reader.ReadUndertaleObject(Backgrounds);
        reader.ReadUndertaleObject(Views);
        reader.ReadUndertaleObject(GameObjects);
        reader.ReadUndertaleObject(Tiles);
        if (reader.undertaleData.IsGameMaker2())
        {
            reader.ReadUndertaleObject(Layers);

            // Resolve the object IDs
            foreach (var layer in Layers)
            {
                if (layer.InstancesData != null)
                {
                    layer.InstancesData.Instances.Clear();
                    foreach (var id in layer.InstancesData.InstanceIds)
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

    /// <inheritdoc cref="UndertaleObject.UnserializeChildObjectCount(UndertaleReader)"/>
    public static uint UnserializeChildObjectCount(UndertaleReader reader)
    {
        uint count = 0;

        reader.Position += 40;
        count += 1; // "_creationCodeId"

        uint backgroundPtr = reader.ReadUInt32();
        uint viewsPtr = reader.ReadUInt32();
        uint gameObjsPtr = reader.ReadUInt32();
        if (!CheckedForGMS2_2_2_302)
            CheckForGMS2_2_2_302(reader);
        uint tilesPtr = reader.ReadUInt32();
        uint layersPtr = 0;
        uint sequencesPtr = 0;

        reader.Position += 32;

        if (reader.undertaleData.IsGameMaker2())
        {
            layersPtr = reader.ReadUInt32();
            if (reader.undertaleData.IsVersionAtLeast(2, 3))
                sequencesPtr = reader.ReadUInt32();
        }

        reader.AbsPosition = backgroundPtr;
        count += 1 + UndertalePointerList<Background>.UnserializeChildObjectCount(reader);
        reader.AbsPosition = viewsPtr;
        count += 1 + UndertalePointerList<View>.UnserializeChildObjectCount(reader);
        reader.AbsPosition = gameObjsPtr;
        count += 1 + UndertalePointerList<GameObject>.UnserializeChildObjectCount(reader);
        reader.AbsPosition = tilesPtr;
        count += 1 + UndertalePointerList<Tile>.UnserializeChildObjectCount(reader);

        if (reader.undertaleData.IsGameMaker2())
        {
            reader.AbsPosition = layersPtr;
            count += 1 + UndertalePointerList<Layer>.UnserializeChildObjectCount(reader);

            if (reader.undertaleData.IsVersionAtLeast(2, 3))
            {
                reader.AbsPosition = sequencesPtr;
                count += 1 + UndertaleSimpleList<UndertaleResourceById<UndertaleSequence, UndertaleChunkSEQN>>
                             .UnserializeChildObjectCount(reader);
            }
        }

        return count;
    }

    /// <summary>
    /// Initialize the room by setting every <see cref="Background.ParentRoom"/> or <see cref="Layer.ParentRoom"/>
    /// (depending on the GameMaker version), and optionally calculate the room grid size.
    /// </summary>
    /// <param name="calculateGridWidth">Whether to calculate the room grid width.</param>
    /// <param name="calculateGridHeight">Whether to calculate the room grid height.</param>
    public void SetupRoom(bool calculateGridWidth = true, bool calculateGridHeight = true)
    {
        foreach (Layer layer in Layers)
        {
            if (layer != null)
                layer.ParentRoom = this;
        }
        foreach (UndertaleRoom.Background bgnd in Backgrounds)
            bgnd.ParentRoom = this;

        if (!(calculateGridWidth || calculateGridHeight)) return;

        // Automatically set the grid size to whatever most tiles are sized

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
                    int w = (int) (Width / layer.TilesData.TilesX);
                    int h = (int) (Height / layer.TilesData.TilesY);
                    tileSizes[new(w, h)] = layer.TilesData.TilesX * layer.TilesData.TilesY;
                }
            }

        }
        else
            tileList = Tiles;

        // Loop through each tile and save how many times their sizes are used
        foreach (Tile tile in tileList)
        {
            Point scale = new((int) tile.Width, (int) tile.Height);
            if (tileSizes.ContainsKey(scale))
                tileSizes[scale]++;
            else
                tileSizes.Add(scale, 1);
        }


        if (tileSizes.Count <= 0)
        {
            if (calculateGridWidth)
                GridWidth = 16;
            if (calculateGridHeight)
                GridHeight = 16;
            return;
        }

        // If tiles exist at all, grab the most used tile size and use that as our grid size
        var largestKey = tileSizes.Aggregate((x, y) => x.Value > y.Value ? x : y).Key;
        if (calculateGridWidth)
            GridWidth = largestKey.X;
        if (calculateGridHeight)
            GridHeight = largestKey.Y;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Name?.Content + " (" + GetType().Name + ")";
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);

        _creationCodeId.Dispose();
        if (Flags.HasFlag(RoomEntryFlags.IsGMS2))
        {
            foreach (Layer layer in _layers)
                layer?.Dispose();
            _layers = null;
            Sequences = new();
        }
        else
        {
            foreach (Background bg in Backgrounds)
                bg?.Dispose();
            foreach (View view in Views)
                view?.Dispose();
            foreach (GameObject obj in GameObjects)
                obj?.Dispose();
            foreach (Tile tile in Tiles)
                tile?.Dispose();
            Backgrounds = new();
            Views = new();
            Tiles = new();
        }
        Name = null;
        Caption = null;
        GameObjects = new();
    }

    /// <summary>
    /// Interface for objects within rooms.
    /// </summary>
    public interface IRoomObject
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
    public class Background : UndertaleObject, INotifyPropertyChanged, IDisposable,
                              IStaticChildObjCount, IStaticChildObjectsSize
    {
        /// <inheritdoc cref="IStaticChildObjCount.ChildObjectCount" />
        public static readonly uint ChildObjectCount = 1;

        /// <inheritdoc cref="IStaticChildObjectsSize.ChildObjectsSize" />
        public static readonly uint ChildObjectsSize = 40;

        private UndertaleRoom _parentRoom;

        /// <summary>
        /// The room parent this <see cref="Background"/> belongs to.
        /// </summary>
        /// <remarks>
        /// This attribute is UMT-only and does not exist in GameMaker.
        /// </remarks>
        public UndertaleRoom ParentRoom { get => _parentRoom; set { _parentRoom = value; OnPropertyChanged(); UpdateStretch(); } }

        /// <summary>
        /// The calculated horizontal render scale for the background texture.
        /// </summary>
        /// <remarks>
        /// Used in the room editor.<br/>
        /// This attribute is UMT-only and does not exist in GameMaker.
        /// </remarks>
        public float CalcScaleX { get; set; } = 1;

        /// <summary>
        /// The calculated vertical render scale for the background texture.
        /// </summary>
        /// <remarks>
        /// Used in the room editor.<br/>
        /// This attribute is UMT-only and does not exist in GameMaker.
        /// </remarks>
        public float CalcScaleY { get; set; } = 1;

        /// <summary>
        /// Whether this <see cref="Background"/> is enabled.
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// Whether this acts as a foreground.
        /// </summary>
        public bool Foreground { get; set; } = false;
        private UndertaleResourceById<UndertaleBackground, UndertaleChunkBGND> _backgroundDefinition = new();

        /// <summary>
        /// The background asset this uses.
        /// </summary>
        public UndertaleBackground BackgroundDefinition { get => _backgroundDefinition.Resource; set { _backgroundDefinition.Resource = value; OnPropertyChanged(); } }
        private int _x = 0;
        private int _y = 0;

        /// <summary>
        /// The x coordinate of the background in the room.
        /// </summary>
        public int X { get => _x; set { _x = value; OnPropertyChanged(); UpdateStretch(); } }

        /// <summary>
        /// The y coordinate of the background in the room.
        /// </summary>
        public int Y { get => _y; set { _y = value; OnPropertyChanged(); UpdateStretch(); } }

        private int tileX = 1;
        private int tileY = 1;

        /// <summary>
        /// Horizontal speed of the background.
        /// </summary>
        public int SpeedX { get; set; } = 0;

        /// <summary>
        /// Vertical speed of the background.
        /// </summary>
        public int SpeedY { get; set; } = 0;

        private bool _stretch = false;

        /// <summary>
        /// Whether this background is stretched
        /// </summary>
        public bool Stretch { get => _stretch; set { _stretch = value; OnPropertyChanged(); UpdateStretch(); } }

        /// <summary>
        /// Indicates whether this <see cref="Background"/> is tiled horizontally.
        /// </summary>
        /// <remarks>
        /// Internally, GameMaker uses an integer value for storing the state, where <c>0</c> acts as <see langword="false"/> and any other number as <see langword="true"/>.
        /// This property is a wrapper for it, which is made for convenience.
        /// </remarks>
        public bool TiledHorizontally { get => tileX != 0; set { tileX = value ? 1 : 0; OnPropertyChanged(); } }

        /// <summary>
        /// Indicates whether this <see cref="Background"/> is tiled vertically.
        /// </summary>
        /// <remarks>
        /// Internally, GameMaker uses an integer value for storing the state, where <c>0</c> acts as <see langword="false"/> and any other number as <see langword="true"/>.
        /// This property is a wrapper for it, which is made for convenience.
        /// </remarks>
        public bool TiledVertically { get => tileY != 0; set { tileY = value ? 1 : 0; OnPropertyChanged(); } }

        /// <summary>
        /// A horizontal offset used for proper background display in the room editor.
        /// </summary>
        /// <remarks>
        /// This attribute is UMT-only and does not exist in GameMaker.
        /// </remarks>
        public int XOffset => X + (BackgroundDefinition?.Texture?.TargetX ?? 0);

        /// <summary>
        /// A vertical offset used for proper background display in the room editor.
        /// </summary>
        /// <remarks>
        /// This attribute is UMT-only and does not exist in GameMaker.
        /// </remarks>
        public int YOffset => Y + (BackgroundDefinition?.Texture?.TargetY ?? 0);

        /// <inheritdoc />
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

        /// <inheritdoc />
        public void Serialize(UndertaleWriter writer)
        {
            writer.Write(Enabled);
            writer.Write(Foreground);
            writer.WriteUndertaleObject(_backgroundDefinition);
            writer.Write(X);
            writer.Write(Y);
            writer.Write(tileX);
            writer.Write(tileY);
            writer.Write(SpeedX);
            writer.Write(SpeedY);
            writer.Write(Stretch);
        }

        /// <inheritdoc />
        public void Unserialize(UndertaleReader reader)
        {
            Enabled = reader.ReadBoolean();
            Foreground = reader.ReadBoolean();
            _backgroundDefinition = reader.ReadUndertaleObject<UndertaleResourceById<UndertaleBackground, UndertaleChunkBGND>>();
            X = reader.ReadInt32();
            Y = reader.ReadInt32();
            tileX = reader.ReadInt32();
            tileY = reader.ReadInt32();
            SpeedX = reader.ReadInt32();
            SpeedY = reader.ReadInt32();
            Stretch = reader.ReadBoolean();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            GC.SuppressFinalize(this);

            _parentRoom = null;
            _backgroundDefinition.Dispose();
        }
    }

    /// <summary>
    /// A view with properties as it's used in a room.
    /// </summary>
    public class View : UndertaleObject, INotifyPropertyChanged, IDisposable,
                        IStaticChildObjCount, IStaticChildObjectsSize
    {
        /// <inheritdoc cref="IStaticChildObjCount.ChildObjectCount" />
        public static readonly uint ChildObjectCount = 1;

        /// <inheritdoc cref="IStaticChildObjectsSize.ChildObjectsSize" />
        public static readonly uint ChildObjectsSize = 56;

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

        private UndertaleResourceById<UndertaleGameObject, UndertaleChunkOBJT> _objectId = new();

        /// <summary>
        /// The object the view should follow.
        /// </summary>
        public UndertaleGameObject ObjectId { get => _objectId.Resource; set { _objectId.Resource = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ObjectId))); } }

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        /// <inheritdoc />
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
            writer.WriteUndertaleObject(_objectId);
        }

        /// <inheritdoc />
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
            _objectId = reader.ReadUndertaleObject<UndertaleResourceById<UndertaleGameObject, UndertaleChunkOBJT>>();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            GC.SuppressFinalize(this);

            _objectId.Dispose();
        }
    }

    /// <summary>
    /// A game object with properties as it's used in a room.
    /// </summary>
    public class GameObject : UndertaleObject, IRoomObject, INotifyPropertyChanged, IDisposable,
                              IStaticChildObjCount, IStaticChildObjectsSize
    {
        /// <inheritdoc cref="IStaticChildObjCount.ChildObjectCount" />
        public static readonly uint ChildObjectCount = 2;

        /// <inheritdoc cref="IStaticChildObjectsSize.ChildObjectsSize" />
        public static readonly uint ChildObjectsSize = 36;

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
        public UndertaleGameObject ObjectDefinition { get => _objectDefinition.Resource; set { _objectDefinition.Resource = value; OnPropertyChanged(); } }

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

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        /// <summary>
        /// The opposite angle of the current rotation.
        /// </summary>
        /// <remarks>
        /// This attribute is UMT-only and does not exist in GameMaker.
        /// </remarks>
        public float OppositeRotation => 360F - (Rotation % 360);

        /// <summary>
        /// A horizontal offset relative to top-left corner of the game object.
        /// </summary>
        /// <remarks>
        /// Used for proper game object rotation display in the room editor and for determining <see cref="XOffset"/>.<br/>
        /// This attribute is UMT-only and does not exist in GameMaker.
        /// </remarks>
        public int SpriteXOffset => ObjectDefinition?.Sprite != null
            ? (-1 * ObjectDefinition.Sprite.OriginXWrapper) + (ObjectDefinition.Sprite.Textures.ElementAtOrDefault(ImageIndex)?.Texture?.TargetX ?? 0)
            : 0;

        /// <summary>
        /// A vertical offset relative to top-left corner of the game object.
        /// </summary>
        /// <remarks>
        /// Used for proper game object rotation display in the room editor and for determining <see cref="YOffset"/>.<br/>
        /// This attribute is UMT-only and does not exist in GameMaker.
        /// </remarks>
        public int SpriteYOffset => ObjectDefinition?.Sprite != null
            ? (-1 * ObjectDefinition.Sprite.OriginYWrapper) + (ObjectDefinition.Sprite.Textures.ElementAtOrDefault(ImageIndex)?.Texture?.TargetY ?? 0)
            : 0;
        /// <summary>
        /// A horizontal offset used for proper game object position display in the room editor.
        /// </summary>
        /// <remarks>
        /// This attribute is UMT-only and does not exist in GameMaker.
        /// </remarks>
        public int XOffset => X + SpriteXOffset;

        /// <summary>
        /// A vertical offset used for proper game object display in the room editor.
        /// </summary>
        /// <remarks>
        /// This attribute is UMT-only and does not exist in GameMaker.
        /// </remarks>
        public int YOffset => Y + SpriteYOffset;

        /// <inheritdoc />
        public void Serialize(UndertaleWriter writer)
        {
            writer.Write(X);
            writer.Write(Y);
            writer.WriteUndertaleObject(_objectDefinition);
            writer.Write(InstanceID);
            writer.WriteUndertaleObject(_creationCode);
            writer.Write(ScaleX);
            writer.Write(ScaleY);
            if (writer.undertaleData.IsVersionAtLeast(2, 2, 2, 302))
            {
                writer.Write(ImageSpeed);
                writer.Write(ImageIndex);
            }
            writer.Write(Color);
            writer.Write(Rotation);
            if (writer.undertaleData.GeneralInfo.BytecodeVersion >= 16) // TODO: is that dependent on bytecode or something else?
                writer.WriteUndertaleObject(_preCreateCode);         // Note: Appears in GM:S 1.4.9999 as well, so that's probably the closest it gets
        }

        /// <inheritdoc />
        public void Unserialize(UndertaleReader reader)
        {
            X = reader.ReadInt32();
            Y = reader.ReadInt32();
            _objectDefinition = reader.ReadUndertaleObject<UndertaleResourceById<UndertaleGameObject, UndertaleChunkOBJT>>();
            InstanceID = reader.ReadUInt32();
            _creationCode = reader.ReadUndertaleObject<UndertaleResourceById<UndertaleCode, UndertaleChunkCODE>>();
            ScaleX = reader.ReadSingle();
            ScaleY = reader.ReadSingle();
            if (reader.undertaleData.IsVersionAtLeast(2, 2, 2, 302))
            {
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

        /// <inheritdoc/>
        public void Dispose()
        {
            GC.SuppressFinalize(this);

            _objectDefinition.Dispose();
            _creationCode.Dispose();
            _preCreateCode.Dispose();
        }
    }

    /// <summary>
    /// A tile with properties as it's used in a room.
    /// </summary>
    public class Tile : UndertaleObject, IRoomObject, INotifyPropertyChanged, IDisposable,
                        IStaticChildObjCount, IStaticChildObjectsSize
    {
        /// <inheritdoc cref="IStaticChildObjCount.ChildObjectCount" />
        public static readonly uint ChildObjectCount = 1;

        /// <inheritdoc cref="IStaticChildObjectsSize.ChildObjectsSize" />
        public static readonly uint ChildObjectsSize = 48;

        /// <summary>
        /// Whether this tile is from an asset layer.<br/>
        /// <see langword="true"/> for GameMaker Studio: 2 games, otherwise <see langword="false"/>.
        /// </summary>
        /// <remarks>
        /// This attribute is UMT-only and does not exist in GameMaker.
        /// </remarks>
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

        //TODO: both should be private and instead accessed via objectDefinition.
        /// <summary>
        /// From which tileset / background the tile stems from.
        /// </summary>
        public UndertaleBackground BackgroundDefinition
        {
            get => _backgroundDefinition.Resource;
            set
            {
                _backgroundDefinition.Resource = value;
                OnPropertyChanged();
                OnPropertyChanged("ObjectDefinition");
            }
        }

        /// <summary>
        /// From which sprite this tile stems from.
        /// </summary>
        public UndertaleSprite SpriteDefinition
        {
            get => _spriteDefinition.Resource;
            set
            {
                _spriteDefinition.Resource = value;
                OnPropertyChanged();
                OnPropertyChanged("ObjectDefinition");
            }
        }

        /// <summary>
        /// From which object this tile stems from.<br/>
        /// Will return a <see cref="UndertaleBackground"/> if <see cref="spriteMode"/> is <see langword="true"/>,
        /// a <see cref="UndertaleSprite"/> if it's <see langword="false"/>.
        /// </summary>
        public UndertaleNamedResource ObjectDefinition
        {
            get => spriteMode ? SpriteDefinition : BackgroundDefinition;
            set
            {
                if (spriteMode) SpriteDefinition = (UndertaleSprite)value;
                else BackgroundDefinition = (UndertaleBackground)value;
            }
        }

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

        /// <summary>
        /// Returns the texture page item of the tile.
        /// </summary>
        /// <remarks>
        /// This attribute is UMT-only and does not exist in GameMaker.
        /// </remarks>
        public UndertaleTexturePageItem Tpag => spriteMode ? SpriteDefinition?.Textures.FirstOrDefault()?.Texture : BackgroundDefinition?.Texture; // TODO: what happens on sprites with multiple textures?

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        /// <inheritdoc />
        public void Serialize(UndertaleWriter writer)
        {
            writer.Write(X);
            writer.Write(Y);
            if (spriteMode != writer.undertaleData.IsGameMaker2())
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

        /// <inheritdoc />
        public void Unserialize(UndertaleReader reader)
        {
            X = reader.ReadInt32();
            Y = reader.ReadInt32();
            spriteMode = reader.undertaleData.IsGameMaker2();
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

        /// <inheritdoc />
        public override string ToString()
        {
            return "Tile " + InstanceID + " of " + (ObjectDefinition?.Name?.Content ?? "?") + " (UndertaleRoom+Tile)";
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            GC.SuppressFinalize(this);

            _backgroundDefinition.Dispose();
            _spriteDefinition.Dispose();
        }
    }

    /// <summary>
    /// The layer type for a specific layer. In Game Maker: Studio 2, <see cref="UndertaleRoom.Backgrounds"/>, <see cref="UndertaleRoom.Tiles"/>
    /// are empty and <see cref="UndertaleRoom.Layers"/> are used instead.
    /// </summary>
    public enum LayerType
    {
        /// <summary>
        /// The layer is a background layer.
        /// </summary>
        Background = 1,
        /// <summary>
        /// The layer is an instances layer.
        /// </summary>
        Instances = 2,
        /// <summary>
        /// The layer is an assets layer.
        /// </summary>
        Assets = 3,
        /// <summary>
        /// The layer is a tiles layer.
        /// </summary>
        Tiles = 4,
        /// <summary>
        /// The layer is an effects layer.
        /// </summary>
        Effect = 6
    }

    /// <summary>
    /// A layer with properties as it's used in a room. Game Maker: Studio 2 only.
    /// </summary>
    //TODO: everything from here on is mostly gms2 related which i dont have much experience with
    public class Layer : UndertaleObject, INotifyPropertyChanged, IDisposable
    {
        public interface LayerData : UndertaleObject, IDisposable
        {
        }

        private UndertaleRoom _parentRoom;
        private int _layerDepth;

        /// <summary>
        /// The room this layer belongs to.
        /// </summary>
        public UndertaleRoom ParentRoom { get => _parentRoom; set { _parentRoom = value; OnPropertyChanged(); UpdateParentRoom(); } }

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
        public bool IsVisible { get; set; } = true;
        public LayerData Data { get; set; }
        public LayerInstancesData InstancesData => Data as LayerInstancesData;
        public LayerTilesData TilesData => Data as LayerTilesData;
        public LayerBackgroundData BackgroundData => Data as LayerBackgroundData;
        public LayerAssetsData AssetsData => Data as LayerAssetsData;
        public LayerEffectData EffectData => Data as LayerEffectData;

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        // GMS 2022.1+
        public bool EffectEnabled { get; set; }
        public UndertaleString EffectType { get; set; }
        public UndertaleSimpleList<EffectProperty> EffectProperties { get; set; } = new();

        public void UpdateParentRoom()
        {
            if (BackgroundData != null)
                BackgroundData.ParentLayer = this;
            if (TilesData != null)
                TilesData.ParentLayer = this;
        }
        public void UpdateZIndex() => OnPropertyChanged("LayerDepth");

        /// <inheritdoc />
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

            if (writer.undertaleData.IsVersionAtLeast(2022, 1))
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

        /// <inheritdoc />
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

            if (reader.undertaleData.IsVersionAtLeast(2022, 1))
            {
                EffectEnabled = reader.ReadBoolean();
                EffectType = reader.ReadUndertaleString();
                EffectProperties = reader.ReadUndertaleObject<UndertaleSimpleList<EffectProperty>>();
            }

            Data = LayerType switch
            {
                LayerType.Instances => reader.ReadUndertaleObject<LayerInstancesData>(),
                LayerType.Tiles => reader.ReadUndertaleObject<LayerTilesData>(),
                LayerType.Background => reader.ReadUndertaleObject<LayerBackgroundData>(),
                LayerType.Assets => reader.ReadUndertaleObject<LayerAssetsData>(),
                LayerType.Effect => // Because effect data is empty in 2022.1+, it would erroneously read the next object.
                                    reader.undertaleData.IsVersionAtLeast(2022, 1)
                                    ? new LayerEffectData() { EffectType = EffectType, Properties = EffectProperties }
                                    : reader.ReadUndertaleObject<LayerEffectData>(),
                _ => throw new Exception("Unsupported layer type " + LayerType)
            };
        }

        /// <inheritdoc cref="UndertaleObject.UnserializeChildObjectCount(UndertaleReader)"/>
        public static uint UnserializeChildObjectCount(UndertaleReader reader)
        {
            uint count = 0;

            reader.Position += 8;
            LayerType layerType = (LayerType)reader.ReadUInt32();
            reader.Position += 24;

            // Effect properties
            if (reader.undertaleData.IsVersionAtLeast(2022, 1))
            {
                reader.Position += 8;
                count += 1 + UndertaleSimpleList<EffectProperty>.UnserializeChildObjectCount(reader);
            }

            count += layerType switch
            {
                LayerType.Instances => 1 + LayerInstancesData.UnserializeChildObjectCount(reader),
                LayerType.Tiles => 1 + LayerTilesData.UnserializeChildObjectCount(reader),
                LayerType.Background => 1 + LayerBackgroundData.UnserializeChildObjectCount(reader),
                LayerType.Assets => 1 + LayerAssetsData.UnserializeChildObjectCount(reader),
                LayerType.Effect => reader.undertaleData.IsVersionAtLeast(2022, 1)
                                    ? 0 : 1 + LayerEffectData.UnserializeChildObjectCount(reader),
                _ => 0
            };

            return count;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return GetType().FullName + " - \"" + LayerName?.Content + '\"';
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            GC.SuppressFinalize(this);

            Data?.Dispose();
            _parentRoom = null;
            LayerName = null;
        }

        public class LayerInstancesData : LayerData
        {
            internal uint[] InstanceIds { get; private set; } // 100000, 100001, 100002, 100003 - instance ids from GameObjects list in the room
            public ObservableCollection<GameObject> Instances { get; private set; } = new();

            /// <inheritdoc />
            public void Serialize(UndertaleWriter writer)
            {
                writer.Write((uint)Instances.Count);
                foreach (var obj in Instances)
                    writer.Write(obj.InstanceID);
            }

            /// <inheritdoc />
            public void Unserialize(UndertaleReader reader)
            {
                uint instanceCount = reader.ReadUInt32();
                InstanceIds = new uint[instanceCount];
                Instances.Clear();
                for (uint i = 0; i < instanceCount; i++)
                    InstanceIds[i] = reader.ReadUInt32();
                // UndertaleRoom.Unserialize resolves these IDs to objects later
            }

            /// <inheritdoc cref="UndertaleObject.UnserializeChildObjectCount(UndertaleReader)"/>
            public static uint UnserializeChildObjectCount(UndertaleReader reader)
            {
                uint instanceCount = reader.ReadUInt32();
                reader.Position += instanceCount * 4;

                return 0;
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                GC.SuppressFinalize(this);

                foreach (GameObject obj in Instances)
                    obj?.Dispose();
                InstanceIds = null;
                Instances = new();
            }
        }

        public class LayerTilesData : LayerData, INotifyPropertyChanged
        {
            private UndertaleResourceById<UndertaleBackground, UndertaleChunkBGND> _background = new(); // In GMS2 backgrounds are just tilesets
            private uint _tilesX;
            private uint _tilesY;
            private uint[][] _tileData; // Each is simply an ID from the tileset/background/sprite

            public Layer ParentLayer { get; set; }
            public UndertaleBackground Background { get => _background.Resource; set { _background.Resource = value; OnPropertyChanged(); } }
            public uint TilesX
            {
                get => _tilesX; set
                {
                    _tilesX = value; OnPropertyChanged();
                    if (_tileData != null)
                    {
                        for (var y = 0; y < _tileData.Length; y++)
                        {
                            Array.Resize(ref _tileData[y], (int)value);
                        }
                        OnPropertyChanged("TileData");
                    }
                }
            }
            public uint TilesY
            {
                get => _tilesY; set
                {
                    _tilesY = value; OnPropertyChanged();
                    if (_tileData != null)
                    {
                        Array.Resize(ref _tileData, (int)value);
                        for (var y = 0; y < _tileData.Length; y++)
                        {
                            if (_tileData[y] == null)
                                _tileData[y] = new uint[TilesX];
                        }
                        OnPropertyChanged("TileData");
                    }
                }
            }
            public uint[][] TileData { get => _tileData; set { _tileData = value; OnPropertyChanged(); } }

            /// <inheritdoc />
            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged([CallerMemberName] string name = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }

            /// <inheritdoc />
            public void Serialize(UndertaleWriter writer)
            {
                _background.Serialize(writer); // see comment below
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

            /// <inheritdoc />
            public void Unserialize(UndertaleReader reader)
            {
                _background = new UndertaleResourceById<UndertaleBackground, UndertaleChunkBGND>(); // see comment in UndertaleGlobalInit.Unserialize
                _background.Unserialize(reader);
                _tileData = null; // prevent unnecessary resizes
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

            /// <inheritdoc cref="UndertaleObject.UnserializeChildObjectCount(UndertaleReader)"/>
            public static uint UnserializeChildObjectCount(UndertaleReader reader)
            {
                uint count = 0;

                reader.Position += 4; // _background

                uint tilesX = reader.ReadUInt32();
                uint tilesY = reader.ReadUInt32();
                reader.Position += tilesX * tilesY * 4;

                return count;
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                GC.SuppressFinalize(this);

                _background.Dispose();
                _tileData = null;
                ParentLayer = null;
            }
        }

        public class LayerBackgroundData : LayerData, IStaticChildObjCount, IStaticChildObjectsSize, INotifyPropertyChanged
        {
            /// <inheritdoc cref="IStaticChildObjCount.ChildObjectCount" />
            public static readonly uint ChildObjectCount = 1;

            /// <inheritdoc cref="IStaticChildObjectsSize.ChildObjectsSize" />
            public static readonly uint ChildObjectsSize = 40;

            private Layer _parentLayer;

            private UndertaleResourceById<UndertaleSprite, UndertaleChunkSPRT> _sprite = new(); // Apparently there's a mode where it's a background reference, but probably not necessary
            private bool _tiledHorizontally;
            private bool _tiledVertically;
            private bool _stretch;

            public Layer ParentLayer { get => _parentLayer; set { _parentLayer = value; OnPropertyChanged(); UpdateScale(); } }
            public float CalcScaleX { get; set; }
            public float CalcScaleY { get; set; }

            public bool Visible { get; set; } = true;
            public bool Foreground { get; set; }
            public UndertaleSprite Sprite { get => _sprite.Resource; set { _sprite.Resource = value; OnPropertyChanged(); ParentLayer.ParentRoom.UpdateBGColorLayer(); } }
            public bool TiledHorizontally { get => _tiledHorizontally; set { _tiledHorizontally = value; OnPropertyChanged(); } }
            public bool TiledVertically { get => _tiledVertically; set { _tiledVertically = value; OnPropertyChanged(); } }
            public bool Stretch { get => _stretch; set { _stretch = value; OnPropertyChanged(); } }
            public uint Color { get; set; } = 0xFF000000;
            public float FirstFrame { get; set; }
            public float AnimationSpeed { get; set; }
            public AnimationSpeedType AnimationSpeedType { get; set; }

            public float XOffset => (ParentLayer?.XOffset ?? 0) +
                                    (Sprite is not null
                                        ? (Sprite.Textures.FirstOrDefault()?.Texture?.TargetX ?? 0) - Sprite.OriginXWrapper
                                        : 0);
            public float YOffset => (ParentLayer?.YOffset ?? 0) +
                                    (Sprite is not null
                                        ? (Sprite.Textures.FirstOrDefault()?.Texture?.TargetY ?? 0) - Sprite.OriginYWrapper
                                        : 0);

            /// <inheritdoc />
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

            /// <inheritdoc />
            public void Serialize(UndertaleWriter writer)
            {
                writer.Write(Visible);
                writer.Write(Foreground);
                writer.WriteUndertaleObject(_sprite);
                writer.Write(TiledHorizontally);
                writer.Write(TiledVertically);
                writer.Write(Stretch);
                writer.Write(Color);
                writer.Write(FirstFrame);
                writer.Write(AnimationSpeed);
                writer.Write((uint)AnimationSpeedType);
            }

            /// <inheritdoc />
            public void Unserialize(UndertaleReader reader)
            {
                Visible = reader.ReadBoolean();
                Foreground = reader.ReadBoolean();
                _sprite = reader.ReadUndertaleObject<UndertaleResourceById<UndertaleSprite, UndertaleChunkSPRT>>();
                TiledHorizontally = reader.ReadBoolean();
                TiledVertically = reader.ReadBoolean();
                Stretch = reader.ReadBoolean();
                Color = reader.ReadUInt32();
                FirstFrame = reader.ReadSingle();
                AnimationSpeed = reader.ReadSingle();
                AnimationSpeedType = (AnimationSpeedType)reader.ReadUInt32();
            }

            /// <inheritdoc cref="UndertaleObject.UnserializeChildObjectCount(UndertaleReader)"/>
            public static uint UnserializeChildObjectCount(UndertaleReader reader)
            {
                reader.Position += ChildObjectsSize;

                return ChildObjectCount;
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                GC.SuppressFinalize(this);

                _parentLayer = null;
                _sprite.Dispose();
            }
        }

        [PropertyChanged.AddINotifyPropertyChangedInterface]
        public class LayerAssetsData : LayerData
        {
            public UndertalePointerList<Tile> LegacyTiles { get; set; }
            public UndertalePointerList<SpriteInstance> Sprites { get; set; }
            public UndertalePointerList<SequenceInstance> Sequences { get; set; }
            public UndertalePointerList<SpriteInstance> NineSlices { get; set; } // Removed in 2.3.2, before never used

            /// <inheritdoc />
            public void Serialize(UndertaleWriter writer)
            {
                writer.WriteUndertaleObjectPointer(LegacyTiles);
                writer.WriteUndertaleObjectPointer(Sprites);
                if (writer.undertaleData.IsVersionAtLeast(2, 3))
                {
                    writer.WriteUndertaleObjectPointer(Sequences);
                    if (!writer.undertaleData.IsVersionAtLeast(2, 3, 2))
                        writer.WriteUndertaleObjectPointer(NineSlices);
                }
                writer.WriteUndertaleObject(LegacyTiles);
                writer.WriteUndertaleObject(Sprites);
                if (writer.undertaleData.IsVersionAtLeast(2, 3))
                {
                    writer.WriteUndertaleObject(Sequences);
                    if (!writer.undertaleData.IsVersionAtLeast(2, 3, 2))
                        writer.WriteUndertaleObject(NineSlices);
                }
            }

            /// <inheritdoc />
            public void Unserialize(UndertaleReader reader)
            {
                LegacyTiles = reader.ReadUndertaleObjectPointer<UndertalePointerList<Tile>>();
                Sprites = reader.ReadUndertaleObjectPointer<UndertalePointerList<SpriteInstance>>();
                if (reader.undertaleData.IsVersionAtLeast(2, 3))
                {
                    Sequences = reader.ReadUndertaleObjectPointer<UndertalePointerList<SequenceInstance>>();
                    if (!reader.undertaleData.IsVersionAtLeast(2, 3, 2))
                        NineSlices = reader.ReadUndertaleObjectPointer<UndertalePointerList<SpriteInstance>>();
                }
                reader.ReadUndertaleObject(LegacyTiles);
                reader.ReadUndertaleObject(Sprites);
                if (reader.undertaleData.IsVersionAtLeast(2, 3))
                {
                    reader.ReadUndertaleObject(Sequences);
                    if (!reader.undertaleData.IsVersionAtLeast(2, 3, 2))
                        reader.ReadUndertaleObject(NineSlices);
                }
            }

            /// <inheritdoc cref="UndertaleObject.UnserializeChildObjectCount(UndertaleReader)"/>
            public static uint UnserializeChildObjectCount(UndertaleReader reader)
            {
                uint count = 0;

                uint legacyTilesPtr = reader.ReadUInt32();
                uint spritesPtr = reader.ReadUInt32();
                uint sequencesPtr = 0;
                uint nineSlicesPtr = 0;
                if (reader.undertaleData.IsVersionAtLeast(2, 3))
                {
                    sequencesPtr = reader.ReadUInt32();
                    if (!reader.undertaleData.IsVersionAtLeast(2, 3, 2))
                        nineSlicesPtr = reader.ReadUInt32();
                }

                reader.AbsPosition = legacyTilesPtr;
                count += 1 + UndertalePointerList<Tile>.UnserializeChildObjectCount(reader);
                reader.AbsPosition = spritesPtr;
                count += 1 + UndertalePointerList<SpriteInstance>.UnserializeChildObjectCount(reader);
                if (reader.undertaleData.IsVersionAtLeast(2, 3))
                {
                    reader.AbsPosition = sequencesPtr;
                    count += 1 + UndertalePointerList<SequenceInstance>.UnserializeChildObjectCount(reader);
                    if (!reader.undertaleData.IsVersionAtLeast(2, 3, 2))
                    {
                        reader.AbsPosition = nineSlicesPtr;
                        count += 1 + UndertalePointerList<SpriteInstance>.UnserializeChildObjectCount(reader);
                    }
                }

                return count;
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                GC.SuppressFinalize(this);

                if (LegacyTiles is not null)
                {
                    foreach (Tile tile in LegacyTiles)
                        tile?.Dispose();
                }
                if (Sprites is not null)
                {
                    foreach (SpriteInstance inst in Sprites)
                        inst?.Dispose();
                }
                if (Sequences is not null)
                {
                    foreach (SequenceInstance inst in Sequences)
                        inst?.Dispose();
                }
                if (NineSlices is not null)
                {
                    foreach (SpriteInstance inst in NineSlices)
                        inst?.Dispose();
                }

                LegacyTiles = null;
                Sprites = null;
                Sequences = null;
                NineSlices = null;
            }
        }

        [PropertyChanged.AddINotifyPropertyChangedInterface]
        public class LayerEffectData : LayerData
        {
            public UndertaleString EffectType;
            public UndertaleSimpleList<EffectProperty> Properties;

            /// <inheritdoc />
            public void Serialize(UndertaleWriter writer)
            {
                if (writer.undertaleData.IsVersionAtLeast(2022, 1))
                    return;
                writer.WriteUndertaleString(EffectType);
                writer.WriteUndertaleObject(Properties);
            }

            /// <inheritdoc />
            public void Unserialize(UndertaleReader reader)
            {
                if (reader.undertaleData.IsVersionAtLeast(2022, 1))
                    return;
                EffectType = reader.ReadUndertaleString();
                Properties = reader.ReadUndertaleObject<UndertaleSimpleList<EffectProperty>>();
            }

            /// <inheritdoc cref="UndertaleObject.UnserializeChildObjectCount(UndertaleReader)"/>
            public static uint UnserializeChildObjectCount(UndertaleReader reader)
            {
                if (reader.undertaleData.IsVersionAtLeast(2022, 1))
                    return 0;

                reader.Position += 4; // "EffectType"

                return 1 + UndertaleSimpleList<EffectProperty>.UnserializeChildObjectCount(reader);
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                GC.SuppressFinalize(this);

                if (Properties is not null)
                {
                    foreach (EffectProperty prop in Properties)
                        prop?.Dispose();
                }

                EffectType = null;
                Properties = null;
            }
        }
    }

    [PropertyChanged.AddINotifyPropertyChangedInterface]
    public class EffectProperty : UndertaleObject, IStaticChildObjectsSize, IDisposable
    {
        /// <inheritdoc cref="IStaticChildObjectsSize.ChildObjectsSize" />
        public static readonly uint ChildObjectsSize = 12;

        public enum PropertyType
        {
            Real = 0,
            Color = 1,
            Sampler = 2
        }

        public PropertyType Kind { get; set; }
        public UndertaleString Name { get; set; }
        public UndertaleString Value { get; set; }

        /// <inheritdoc />
        public void Serialize(UndertaleWriter writer)
        {
            writer.Write((int)Kind);
            writer.WriteUndertaleString(Name);
            writer.WriteUndertaleString(Value);
        }

        /// <inheritdoc />
        public void Unserialize(UndertaleReader reader)
        {
            Kind = (PropertyType)reader.ReadInt32();
            Name = reader.ReadUndertaleString();
            Value = reader.ReadUndertaleString();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            GC.SuppressFinalize(this);

            Name = null;
            Value = null;
        }
    }

    public class SpriteInstance : UndertaleObject, INotifyPropertyChanged, IStaticChildObjCount, IStaticChildObjectsSize, IDisposable
    {
        /// <inheritdoc cref="IStaticChildObjCount.ChildObjectCount" />
        public static readonly uint ChildObjectCount = 1;

        /// <inheritdoc cref="IStaticChildObjectsSize.ChildObjectsSize" />
        public static readonly uint ChildObjectsSize = 44;

        private UndertaleResourceById<UndertaleSprite, UndertaleChunkSPRT> _sprite = new();

        public UndertaleString Name { get; set; }
        public UndertaleSprite Sprite { get => _sprite.Resource; set { _sprite.Resource = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Sprite))); } }
        public int X { get; set; }
        public int Y { get; set; }
        public float ScaleX { get; set; } = 1;
        public float ScaleY { get; set; } = 1;
        public uint Color { get; set; } = 0xFFFFFFFF;
        public float AnimationSpeed { get; set; } = 1;
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

        public int SpriteXOffset => Sprite != null
            ? (-1 * Sprite.OriginXWrapper) + (Sprite.Textures.ElementAtOrDefault(WrappedFrameIndex)?.Texture?.TargetX ?? 0)
            : 0;
        public int SpriteYOffset => Sprite != null
            ? (-1 * Sprite.OriginYWrapper) + (Sprite.Textures.ElementAtOrDefault(WrappedFrameIndex)?.Texture?.TargetY ?? 0)
            : 0;
        public int XOffset => X + SpriteXOffset;
        public int YOffset => Y + SpriteYOffset;

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        /// <inheritdoc />
        public void Serialize(UndertaleWriter writer)
        {
            writer.WriteUndertaleString(Name);
            writer.WriteUndertaleObject(_sprite);
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

        /// <inheritdoc />
        public void Unserialize(UndertaleReader reader)
        {
            Name = reader.ReadUndertaleString();
            _sprite = reader.ReadUndertaleObject<UndertaleResourceById<UndertaleSprite, UndertaleChunkSPRT>>();
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

        //TODO: rework this method slightly.
        public static UndertaleString GenerateRandomName(UndertaleData data)
        {
            // The same format as in "GameMaker Studio: 2".
            return data.Strings.MakeString("graphic_" + ((uint)new Random().Next(-int.MaxValue, int.MaxValue)).ToString("X8"));
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return "Sprite " + Name?.Content + " of " + (Sprite?.Name?.Content ?? "?") + " (UndertaleRoom+SpriteInstance)";
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            GC.SuppressFinalize(this);

            _sprite.Dispose();
            Name = null;
        }
    }

    public class SequenceInstance : UndertaleObject, INotifyPropertyChanged, IStaticChildObjCount, IStaticChildObjectsSize, IDisposable
    {
        /// <inheritdoc cref="IStaticChildObjCount.ChildObjectCount" />
        public static readonly uint ChildObjectCount = 1;

        /// <inheritdoc cref="IStaticChildObjectsSize.ChildObjectsSize" />
        public static readonly uint ChildObjectsSize = 44;

        private UndertaleResourceById<UndertaleSequence, UndertaleChunkSEQN> _sequence = new();

        public UndertaleString Name { get; set; }
        public UndertaleSequence Sequence { get => _sequence.Resource; set { _sequence.Resource = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Sequence))); } }
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

        /// <inheritdoc />
        public void Serialize(UndertaleWriter writer)
        {
            writer.WriteUndertaleString(Name);
            writer.WriteUndertaleObject(_sequence);
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

        /// <inheritdoc />
        public void Unserialize(UndertaleReader reader)
        {
            Name = reader.ReadUndertaleString();
            _sequence = reader.ReadUndertaleObject<UndertaleResourceById<UndertaleSequence, UndertaleChunkSEQN>>();
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

        /// <inheritdoc />
        public override string ToString()
        {
            return "Sequence " + Name?.Content + " of " + (Sequence?.Name?.Content ?? "?") + " (UndertaleRoom+SequenceInstance)";
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            GC.SuppressFinalize(this);

            _sequence.Dispose();
            Name = null;
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
    public static T ByInstanceID<T>(this IList<T> list, uint instance) where T : UndertaleRoom.IRoomObject
    {
        return list.Where((x) => x.InstanceID == instance).FirstOrDefault();
    }
}