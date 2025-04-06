using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;
using UndertaleModLib.Models;
using UndertaleModLib.Project.Json;
using System;

namespace UndertaleModLib.Project.SerializableAssets;

/// <summary>
/// A serializable version of <see cref="UndertaleRoom"/>.
/// </summary>
internal sealed class SerializableRoom : ISerializableProjectAsset
{
    /// <inheritdoc/>
    public string DataName { get; set; }

    /// <inheritdoc/>
    [JsonIgnore]
    public SerializableAssetType AssetType => SerializableAssetType.Room;

    /// <inheritdoc/>
    [JsonIgnore]
    public bool IndividualDirectory => false;

    /// <inheritdoc cref="UndertaleRoom.Caption"/>
    public string Caption { get; set; }

    /// <inheritdoc cref="UndertaleRoom.Width"/>
    public uint Width { get; set; }

    /// <inheritdoc cref="UndertaleRoom.Height"/>
    public uint Height { get; set; }

    /// <inheritdoc cref="UndertaleRoom.Speed"/>
    public uint Speed { get; set; }

    /// <inheritdoc cref="UndertaleRoom.Persistent"/>
    public bool Persistent { get; set; }

    /// <inheritdoc cref="UndertaleRoom.BackgroundColor"/>
    public uint BackgroundColor { get; set; }

    /// <inheritdoc cref="UndertaleRoom.DrawBackgroundColor"/>
    public bool DrawBackgroundColor { get; set; }

    /// <inheritdoc cref="UndertaleRoom.CreationCodeId"/>
    public string CreationCodeEntry { get; set; }

    /// <inheritdoc cref="UndertaleRoom.Flags"/>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public UndertaleRoom.RoomEntryFlags Flags { get; set; }

    /// <inheritdoc cref="UndertaleRoom.World"/>
    public bool World { get; set; }

    /// <inheritdoc cref="UndertaleRoom.Top"/>
    public uint Top { get; set; }

    /// <inheritdoc cref="UndertaleRoom.Left"/>
    public uint Left { get; set; }

    /// <inheritdoc cref="UndertaleRoom.Right"/>
    public uint Right { get; set; }

    /// <inheritdoc cref="UndertaleRoom.Bottom"/>
    public uint Bottom { get; set; }

    /// <inheritdoc cref="UndertaleRoom.GravityX"/>
    public float GravityX { get; set; }

    /// <inheritdoc cref="UndertaleRoom.GravityY"/>
    public float GravityY { get; set; }

    /// <inheritdoc cref="UndertaleRoom.MetersPerPixel"/>
    public float MetersPerPixel { get; set; }

    /// <inheritdoc cref="UndertaleRoom.Backgrounds"/>
    public List<Background> Backgrounds { get; set; }

    /// <inheritdoc cref="UndertaleRoom.Views"/>
    public List<View> Views { get; set; }

    /// <inheritdoc cref="UndertaleRoom.GameObjects"/>
    public List<GameObject> GameObjects { get; set; }

    /// <inheritdoc cref="UndertaleRoom.Tiles"/>
    [JsonConverter(typeof(NoPrettyPrintJsonConverter<List<Tile>>))]
    public List<Tile> Tiles { get; set; }

    /// <inheritdoc cref="UndertaleRoom.Layers"/>
    public List<Layer> Layers { get; set; }

    /// <inheritdoc cref="UndertaleRoom.Sequences"/>
    public List<string> Sequences { get; set; }

    /// <inheritdoc cref="UndertaleRoom.Background"/>
    public sealed class Background
    {
        /// <inheritdoc cref="UndertaleRoom.Background.Enabled"/>
        public bool Enabled { get; set; }

        /// <inheritdoc cref="UndertaleRoom.Background.Foreground"/>
        public bool Foreground { get; set; }

        /// <inheritdoc cref="UndertaleRoom.Background.BackgroundDefinition"/>
        public string BackgroundAsset { get; set; }

        /// <inheritdoc cref="UndertaleRoom.Background.X"/>
        public int X { get; set; }

        /// <inheritdoc cref="UndertaleRoom.Background.Y"/>
        public int Y { get; set; }

        /// <inheritdoc cref="UndertaleRoom.Background.SpeedX"/>
        public int SpeedX { get; set; }

        /// <inheritdoc cref="UndertaleRoom.Background.SpeedY"/>
        public int SpeedY { get; set; }

        /// <inheritdoc cref="UndertaleRoom.Background.Stretch"/>
        public bool Stretch { get; set; }

        /// <inheritdoc cref="UndertaleRoom.Background.TiledHorizontally"/>
        public bool TiledHorizontally { get; set; }

        /// <inheritdoc cref="UndertaleRoom.Background.TiledVertically"/>
        public bool TiledVertically { get; set; }
    }


    /// <inheritdoc cref="UndertaleRoom.View"/>
    public sealed class View
    {
        /// <inheritdoc cref="UndertaleRoom.View.Enabled"/>
        public bool Enabled { get; set; }

        /// <inheritdoc cref="UndertaleRoom.View.ViewX"/>
        public int ViewX { get; set; }

        /// <inheritdoc cref="UndertaleRoom.View.ViewY"/>
        public int ViewY { get; set; }

        /// <inheritdoc cref="UndertaleRoom.View.ViewWidth"/>
        public int ViewWidth { get; set; }

        /// <inheritdoc cref="UndertaleRoom.View.ViewHeight"/>
        public int ViewHeight { get; set; }

        /// <inheritdoc cref="UndertaleRoom.View.PortX"/>
        public int PortX { get; set; }

        /// <inheritdoc cref="UndertaleRoom.View.PortY"/>
        public int PortY { get; set; }

        /// <inheritdoc cref="UndertaleRoom.View.PortWidth"/>
        public int PortWidth { get; set; }

        /// <inheritdoc cref="UndertaleRoom.View.PortHeight"/>
        public int PortHeight { get; set; }

        /// <inheritdoc cref="UndertaleRoom.View.BorderX"/>
        public uint BorderX { get; set; }

        /// <inheritdoc cref="UndertaleRoom.View.BorderY"/>
        public uint BorderY { get; set; }

        /// <inheritdoc cref="UndertaleRoom.View.SpeedX"/>
        public int SpeedX { get; set; }

        /// <inheritdoc cref="UndertaleRoom.View.SpeedY"/>
        public int SpeedY { get; set; }

        /// <inheritdoc cref="UndertaleRoom.View.ObjectId"/>
        public string FollowsObject { get; set; }
    }

    /// <inheritdoc cref="UndertaleRoom.GameObject"/>
    public sealed class GameObject
    {
        /// <inheritdoc cref="UndertaleRoom.GameObject.ObjectDefinition"/>
        public string ObjectName { get; set; }

        /// <inheritdoc cref="UndertaleRoom.GameObject.X"/>
        public int X { get; set; }

        /// <inheritdoc cref="UndertaleRoom.GameObject.Y"/>
        public int Y { get; set; }

        /// <inheritdoc cref="UndertaleRoom.GameObject.CreationCode"/>
        public string CreationCodeEntry { get; set; }

        /// <inheritdoc cref="UndertaleRoom.GameObject.ScaleX"/>
        public float ScaleX { get; set; }

        /// <inheritdoc cref="UndertaleRoom.GameObject.ScaleY"/>
        public float ScaleY { get; set; }

        /// <inheritdoc cref="UndertaleRoom.GameObject.Color"/>
        public uint Color { get; set; }

        /// <inheritdoc cref="UndertaleRoom.GameObject.Rotation"/>
        public float Rotation { get; set; }

        /// <inheritdoc cref="UndertaleRoom.GameObject.PreCreateCode"/>
        public string PreCreateCodeEntry { get; set; }

        /// <inheritdoc cref="UndertaleRoom.GameObject.ImageSpeed"/>
        public float ImageSpeed { get; set; }

        /// <inheritdoc cref="UndertaleRoom.GameObject.ImageIndex"/>
        public int ImageIndex { get; set; }
    }

    /// <inheritdoc cref="UndertaleRoom.Tile"/>
    public sealed class Tile
    {
        /// <inheritdoc cref="UndertaleRoom.Tile.BackgroundDefinition"/>
        /// <seealso cref="UndertaleRoom.Tile.SpriteDefinition"/>
        public string Asset { get; set; }

        /// <inheritdoc cref="UndertaleRoom.Tile.X"/>
        public int X { get; set; }

        /// <inheritdoc cref="UndertaleRoom.Tile.Y"/>
        public int Y { get; set; }

        /// <inheritdoc cref="UndertaleRoom.Tile.SourceX"/>
        public int SourceX { get; set; }

        /// <inheritdoc cref="UndertaleRoom.Tile.SourceY"/>
        public int SourceY { get; set; }

        /// <inheritdoc cref="UndertaleRoom.Tile.Width"/>
        public uint Width { get; set; }

        /// <inheritdoc cref="UndertaleRoom.Tile.Height"/>
        public uint Height { get; set; }

        /// <inheritdoc cref="UndertaleRoom.Tile.TileDepth"/>
        public int Depth { get; set; }

        /// <inheritdoc cref="UndertaleRoom.Tile.ScaleX"/>
        public float ScaleX { get; set; }

        /// <inheritdoc cref="UndertaleRoom.Tile.ScaleY"/>
        public float ScaleY { get; set; }

        /// <inheritdoc cref="UndertaleRoom.Tile.Color"/>
        public uint Color { get; set; }
    }

    /// <inheritdoc cref="UndertaleRoom.Layer"/>
    [JsonDerivedType(typeof(InstancesLayer), nameof(InstancesLayer))]
    [JsonDerivedType(typeof(TilesLayer), nameof(TilesLayer))]
    [JsonDerivedType(typeof(BackgroundLayer), nameof(BackgroundLayer))]
    [JsonDerivedType(typeof(AssetsLayer), nameof(AssetsLayer))]
    [JsonDerivedType(typeof(EffectLayer), nameof(EffectLayer))]
    public abstract class Layer
    {
        /// <inheritdoc cref="UndertaleRoom.Layer.LayerName"/>
        public string Name { get; set; }

        /// <inheritdoc cref="UndertaleRoom.Layer.LayerId"/>
        public uint ID { get; set; }

        /// <inheritdoc cref="UndertaleRoom.Layer.LayerDepth"/>
        public int Depth { get; set; }

        /// <inheritdoc cref="UndertaleRoom.Layer.XOffset"/>
        public float XOffset { get; set; }

        /// <inheritdoc cref="UndertaleRoom.Layer.YOffset"/>
        public float YOffset { get; set; }

        /// <inheritdoc cref="UndertaleRoom.Layer.HSpeed"/>
        public float HSpeed { get; set; }

        /// <inheritdoc cref="UndertaleRoom.Layer.VSpeed"/>
        public float VSpeed { get; set; }

        /// <inheritdoc cref="UndertaleRoom.Layer.IsVisible"/>
        public bool IsVisible { get; set; }
    }

    /// <inheritdoc cref="UndertaleRoom.Layer.LayerInstancesData"/>
    public sealed class InstancesLayer : Layer
    {
        /// <summary>
        /// List of indices corresponding to <see cref="GameObjects"/>, or -1 if a nonexistent object instance.
        /// </summary>
        public List<int> InstanceIndices { get; set; }
    }

    /// <inheritdoc cref="UndertaleRoom.Layer.LayerTilesData"/>
    public sealed class TilesLayer : Layer
    {
        /// <inheritdoc cref="UndertaleRoom.Layer.LayerTilesData.Background"/>
        public string Background { get; set; }

        /// <inheritdoc cref="UndertaleRoom.Layer.LayerTilesData.TilesX"/>
        public uint TilesX { get; set; }

        /// <inheritdoc cref="UndertaleRoom.Layer.LayerTilesData.TilesY"/>
        public uint TilesY { get; set; }

        /// <inheritdoc cref="UndertaleRoom.Layer.LayerTilesData.TileData"/>
        [JsonConverter(typeof(NoPrettyPrintJsonConverter<List<uint>>))]
        public List<uint> TileData { get; set; }
    }

    /// <inheritdoc cref="UndertaleRoom.Layer.LayerBackgroundData"/>
    public sealed class BackgroundLayer : Layer
    {
        /// <inheritdoc cref="UndertaleRoom.Layer.LayerBackgroundData.Visible"/>
        public bool BackgroundVisible { get; set; }

        /// <inheritdoc cref="UndertaleRoom.Layer.LayerBackgroundData.Foreground"/>
        public bool Foreground { get; set; }

        /// <inheritdoc cref="UndertaleRoom.Layer.LayerBackgroundData.Sprite"/>
        public string Sprite { get; set; }

        /// <inheritdoc cref="UndertaleRoom.Layer.LayerBackgroundData.TiledHorizontally"/>
        public bool TiledHorizontally { get; set; }

        /// <inheritdoc cref="UndertaleRoom.Layer.LayerBackgroundData.TiledVertically"/>
        public bool TiledVertically { get; set; }

        /// <inheritdoc cref="UndertaleRoom.Layer.LayerBackgroundData.Stretch"/>
        public bool Stretch { get; set; }

        /// <inheritdoc cref="UndertaleRoom.Layer.LayerBackgroundData.Color"/>
        public uint Color { get; set; }

        /// <inheritdoc cref="UndertaleRoom.Layer.LayerBackgroundData.FirstFrame"/>
        public float FirstFrame { get; set; }

        /// <inheritdoc cref="UndertaleRoom.Layer.LayerBackgroundData.AnimationSpeed"/>
        public float AnimationSpeed { get; set; }

        /// <inheritdoc cref="UndertaleRoom.Layer.LayerBackgroundData.AnimationSpeedType"/>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public AnimationSpeedType AnimationSpeedType { get; set; }
    }

    /// <inheritdoc cref="UndertaleRoom.Layer.LayerAssetsData"/>
    public sealed class AssetsLayer : Layer
    {
        /// <inheritdoc cref="UndertaleRoom.Layer.LayerAssetsData.LegacyTiles"/>
        [JsonConverter(typeof(NoPrettyPrintJsonConverter<List<Tile>>))]
        public List<Tile> LegacyTiles { get; set; }

        /// <inheritdoc cref="UndertaleRoom.Layer.LayerAssetsData.Sprites"/>
        public List<SpriteAssetInstance> Sprites { get; set; }

        /// <inheritdoc cref="UndertaleRoom.Layer.LayerAssetsData.Sequences"/>
        public List<SequenceAssetInstance> Sequences { get; set; }

        /// <inheritdoc cref="UndertaleRoom.Layer.LayerAssetsData.NineSlices"/>
        public List<SpriteAssetInstance> NineSlices { get; set; }

        /// <inheritdoc cref="UndertaleRoom.Layer.LayerAssetsData.ParticleSystems"/>
        public List<ParticleSystemAssetInstance> ParticleSystems { get; set; }

        /// <inheritdoc cref="UndertaleRoom.Layer.LayerAssetsData.TextItems"/>
        public List<TextItemAssetInstance> TextItems { get; set; }

        /// <inheritdoc cref="UndertaleRoom.SpriteInstance"/>
        public sealed class SpriteAssetInstance
        {
            /// <inheritdoc cref="UndertaleRoom.SpriteInstance.Name"/>
            public string Name { get; set; }

            /// <inheritdoc cref="UndertaleRoom.SpriteInstance.Sprite"/>
            public string Sprite { get; set; }

            /// <inheritdoc cref="UndertaleRoom.SpriteInstance.X"/>
            public int X { get; set; }

            /// <inheritdoc cref="UndertaleRoom.SpriteInstance.Y"/>
            public int Y { get; set; }

            /// <inheritdoc cref="UndertaleRoom.SpriteInstance.ScaleX"/>
            public float ScaleX { get; set; }

            /// <inheritdoc cref="UndertaleRoom.SpriteInstance.ScaleY"/>
            public float ScaleY { get; set; }

            /// <inheritdoc cref="UndertaleRoom.SpriteInstance.Color"/>
            public uint Color { get; set; }

            /// <inheritdoc cref="UndertaleRoom.SpriteInstance.AnimationSpeed"/>
            public float AnimationSpeed { get; set; }

            /// <inheritdoc cref="UndertaleRoom.SpriteInstance.AnimationSpeedType"/>
            [JsonConverter(typeof(JsonStringEnumConverter))]
            public AnimationSpeedType AnimationSpeedType { get; set; }

            /// <inheritdoc cref="UndertaleRoom.SpriteInstance.FrameIndex"/>
            public float FrameIndex { get; set; }

            /// <inheritdoc cref="UndertaleRoom.SpriteInstance.Rotation"/>
            public float Rotation { get; set; }
        }

        /// <inheritdoc cref="UndertaleRoom.SequenceInstance"/>
        public sealed class SequenceAssetInstance
        {
            /// <inheritdoc cref="UndertaleRoom.SequenceInstance.Name"/>
            public string Name { get; set; }

            /// <inheritdoc cref="UndertaleRoom.SequenceInstance.Sequence"/>
            public string Sequence { get; set; }

            /// <inheritdoc cref="UndertaleRoom.SequenceInstance.X"/>
            public int X { get; set; }

            /// <inheritdoc cref="UndertaleRoom.SequenceInstance.Y"/>
            public int Y { get; set; }

            /// <inheritdoc cref="UndertaleRoom.SequenceInstance.ScaleX"/>
            public float ScaleX { get; set; }

            /// <inheritdoc cref="UndertaleRoom.SequenceInstance.ScaleY"/>
            public float ScaleY { get; set; }

            /// <inheritdoc cref="UndertaleRoom.SequenceInstance.Color"/>
            public uint Color { get; set; }

            /// <inheritdoc cref="UndertaleRoom.SequenceInstance.AnimationSpeed"/>
            public float AnimationSpeed { get; set; }

            /// <inheritdoc cref="UndertaleRoom.SequenceInstance.AnimationSpeedType"/>
            [JsonConverter(typeof(JsonStringEnumConverter))]
            public AnimationSpeedType AnimationSpeedType { get; set; }

            /// <inheritdoc cref="UndertaleRoom.SequenceInstance.FrameIndex"/>
            public float FrameIndex { get; set; }

            /// <inheritdoc cref="UndertaleRoom.SequenceInstance.Rotation"/>
            public float Rotation { get; set; }
        }

        /// <inheritdoc cref="UndertaleRoom.ParticleSystemInstance"/>
        public sealed class ParticleSystemAssetInstance
        {
            /// <inheritdoc cref="UndertaleRoom.ParticleSystemInstance.Name"/>
            public string Name { get; set; }

            /// <inheritdoc cref="UndertaleRoom.ParticleSystemInstance.ParticleSystem"/>
            public string ParticleSystem { get; set; }

            /// <inheritdoc cref="UndertaleRoom.ParticleSystemInstance.X"/>
            public int X { get; set; }

            /// <inheritdoc cref="UndertaleRoom.ParticleSystemInstance.Y"/>
            public int Y { get; set; }

            /// <inheritdoc cref="UndertaleRoom.ParticleSystemInstance.ScaleX"/>
            public float ScaleX { get; set; }

            /// <inheritdoc cref="UndertaleRoom.ParticleSystemInstance.ScaleY"/>
            public float ScaleY { get; set; }

            /// <inheritdoc cref="UndertaleRoom.ParticleSystemInstance.Color"/>
            public uint Color { get; set; }

            /// <inheritdoc cref="UndertaleRoom.ParticleSystemInstance.Rotation"/>
            public float Rotation { get; set; }
        }

        /// <inheritdoc cref="UndertaleRoom.TextItemInstance"/>
        public sealed class TextItemAssetInstance
        {
            /// <inheritdoc cref="UndertaleRoom.TextItemInstance.Name"/>
            public string Name { get; set; }

            /// <inheritdoc cref="UndertaleRoom.TextItemInstance.Font"/>
            public string Font { get; set; }

            /// <inheritdoc cref="UndertaleRoom.TextItemInstance.Text"/>
            public string Text { get; set; }

            /// <inheritdoc cref="UndertaleRoom.TextItemInstance.X"/>
            public int X { get; set; }

            /// <inheritdoc cref="UndertaleRoom.TextItemInstance.Y"/>
            public int Y { get; set; }

            /// <inheritdoc cref="UndertaleRoom.TextItemInstance.ScaleX"/>
            public float ScaleX { get; set; }

            /// <inheritdoc cref="UndertaleRoom.TextItemInstance.ScaleY"/>
            public float ScaleY { get; set; }

            /// <inheritdoc cref="UndertaleRoom.TextItemInstance.Color"/>
            public uint Color { get; set; }

            /// <inheritdoc cref="UndertaleRoom.TextItemInstance.Rotation"/>
            public float Rotation { get; set; }

            /// <inheritdoc cref="UndertaleRoom.TextItemInstance.OriginX"/>
            public float OriginX { get; set; }

            /// <inheritdoc cref="UndertaleRoom.TextItemInstance.OriginY"/>
            public float OriginY { get; set; }

            /// <inheritdoc cref="UndertaleRoom.TextItemInstance.Alignment"/>
            public int Alignment { get; set; }

            /// <inheritdoc cref="UndertaleRoom.TextItemInstance.CharSpacing"/>
            public float CharSpacing { get; set; }

            /// <inheritdoc cref="UndertaleRoom.TextItemInstance.LineSpacing"/>
            public float LineSpacing { get; set; }

            /// <inheritdoc cref="UndertaleRoom.TextItemInstance.FrameWidth"/>
            public float FrameWidth { get; set; }

            /// <inheritdoc cref="UndertaleRoom.TextItemInstance.FrameHeight"/>
            public float FrameHeight { get; set; }

            /// <inheritdoc cref="UndertaleRoom.TextItemInstance.Wrap"/>
            public bool Wrap { get; set; }
        }
    }

    /// <inheritdoc cref="UndertaleRoom.Layer.LayerEffectData"/>
    public sealed class EffectLayer : Layer
    {
        /// <inheritdoc cref="UndertaleRoom.Layer.LayerEffectData.EffectType"/>
        public string EffectType { get; set; }

        /// <inheritdoc cref="UndertaleRoom.Layer.LayerEffectData.Properties"/>
        public List<EffectProperty> Properties { get; set; }

        /// <inheritdoc cref="UndertaleRoom.EffectProperty"/>
        public sealed class EffectProperty
        {
            /// <inheritdoc cref="UndertaleRoom.EffectProperty.Kind"/>
            [JsonConverter(typeof(JsonStringEnumConverter))]
            public PropertyType Kind { get; set; }

            /// <inheritdoc cref="UndertaleRoom.EffectProperty.Name"/>
            public string Name { get; set; }

            /// <inheritdoc cref="UndertaleRoom.EffectProperty.Value"/>
            public string Value { get; set; }

            /// <inheritdoc cref="UndertaleRoom.EffectProperty.PropertyType"/>
            public enum PropertyType
            {
                Real = 0,
                Color = 1,
                Sampler = 2
            }
        }
    }

    /// <inheritdoc cref="AnimationSpeedType"/>
    public enum AnimationSpeedType : uint
    {
        FramesPerSecond = 0,
        FramesPerGameFrame = 1
    }

    // Data asset that was located during pre-import.
    private UndertaleRoom _dataAsset = null;

    /// <summary>
    /// Populates this serializable path with data from an actual path.
    /// </summary>
    public void PopulateFromData(ProjectContext projectContext, UndertaleRoom room)
    {
        // Update all main properties
        DataName = room.Name.Content;
        Caption = room.Caption?.Content;
        Width = room.Width;
        Height = room.Height;
        Speed = room.Speed;
        Persistent = room.Persistent;
        BackgroundColor = room.BackgroundColor;
        DrawBackgroundColor = room.DrawBackgroundColor;
        CreationCodeEntry = room.CreationCodeId?.Name?.Content;
        Flags = room.Flags;
        World = room.World;
        Top = room.Top;
        Left = room.Left;
        Right = room.Right;
        Bottom = room.Bottom;
        GravityX = room.GravityX;
        GravityY = room.GravityY;
        MetersPerPixel = room.MetersPerPixel;

        // Update backgrounds
        Backgrounds = new(room.Backgrounds.Count);
        foreach (UndertaleRoom.Background bg in room.Backgrounds)
        {
            Backgrounds.Add(new()
            {
                Enabled = bg.Enabled,
                Foreground = bg.Foreground,
                BackgroundAsset = bg.BackgroundDefinition?.Name?.Content,
                X = bg.X,
                Y = bg.Y,
                SpeedX = bg.SpeedX,
                SpeedY = bg.SpeedY,
                Stretch = bg.Stretch,
                TiledHorizontally = bg.TiledHorizontally,
                TiledVertically = bg.TiledVertically
            });
        }

        // Update views
        Views = new(room.Views.Count);
        foreach (UndertaleRoom.View view in room.Views)
        {
            Views.Add(new()
            {
                Enabled = view.Enabled,
                ViewX = view.ViewX,
                ViewY = view.ViewY,
                ViewWidth = view.ViewWidth,
                ViewHeight = view.ViewHeight,
                PortX = view.PortX,
                PortY = view.PortY,
                PortWidth = view.PortWidth,
                PortHeight = view.PortHeight,
                BorderX = view.BorderX,
                BorderY = view.BorderY,
                SpeedX = view.SpeedX,
                SpeedY = view.SpeedY,
                FollowsObject = view.ObjectId?.Name?.Content
            });
        }

        // Update game objects (and track indices if we have layers)
        Dictionary<UndertaleRoom.GameObject, int> gameObjectIndices =
            room.Layers is not null ? new(room.GameObjects.Count) : null;
        GameObjects = new(room.GameObjects.Count);
        foreach (UndertaleRoom.GameObject obj in room.GameObjects)
        {
            gameObjectIndices?.Add(obj, GameObjects.Count);

            GameObjects.Add(new()
            {
                ObjectName = obj.ObjectDefinition?.Name?.Content,
                X = obj.X,
                Y = obj.Y,
                CreationCodeEntry = obj.CreationCode?.Name?.Content,
                ScaleX = obj.ScaleX,
                ScaleY = obj.ScaleY,
                Color = obj.Color,
                Rotation = obj.Rotation,
                PreCreateCodeEntry = obj.PreCreateCode?.Name?.Content,
                ImageSpeed = obj.ImageSpeed,
                ImageIndex = obj.ImageIndex
            });
        }

        // Update tiles
        Tiles = new(room.Tiles.Count);
        foreach (UndertaleRoom.Tile tile in room.Tiles)
        {
            Tiles.Add(new()
            {
                Asset = tile.ObjectDefinition?.Name?.Content,
                X = tile.X,
                Y = tile.Y,
                SourceX = tile.SourceX,
                SourceY = tile.SourceY,
                Width = tile.Width,
                Height = tile.Height,
                Depth = tile.TileDepth,
                ScaleX = tile.ScaleX,
                ScaleY = tile.ScaleY,
                Color = tile.Color
            });
        }

        // Update layers, if they exist
        if (room.Layers is UndertalePointerList<UndertaleRoom.Layer> layers)
        {
            Layers = new(layers.Count);
            foreach (UndertaleRoom.Layer layer in layers)
            {
                Layer newLayer = layer.LayerType switch
                {
                    UndertaleRoom.LayerType.Instances => new InstancesLayer()
                    {
                        InstanceIndices =
                            layer.InstancesData.Instances.Select((UndertaleRoom.GameObject instance) =>
                            {
                                if (instance.Nonexistent)
                                {
                                    return -1;
                                }
                                if (gameObjectIndices.TryGetValue(instance, out int index))
                                {
                                    return index;
                                }
                                return -1;
                            })
                            .ToList()
                    },
                    UndertaleRoom.LayerType.Tiles => new TilesLayer()
                    {
                        Background = layer.TilesData.Background?.Name?.Content,
                        TilesX = layer.TilesData.TilesX,
                        TilesY = layer.TilesData.TilesY,
                        TileData = ConvertTileDataToList(layer.TilesData.TileData)
                    },
                    UndertaleRoom.LayerType.Background => new BackgroundLayer()
                    {
                        BackgroundVisible = layer.BackgroundData.Visible,
                        Foreground = layer.BackgroundData.Foreground,
                        Sprite = layer.BackgroundData.Sprite?.Name?.Content,
                        TiledHorizontally = layer.BackgroundData.TiledHorizontally,
                        TiledVertically = layer.BackgroundData.TiledVertically,
                        Stretch = layer.BackgroundData.Stretch,
                        Color = layer.BackgroundData.Color,
                        FirstFrame = layer.BackgroundData.FirstFrame,
                        AnimationSpeed = layer.BackgroundData.AnimationSpeed,
                        AnimationSpeedType = (AnimationSpeedType)layer.BackgroundData.AnimationSpeedType
                    },
                    UndertaleRoom.LayerType.Assets => new AssetsLayer()
                    {
                        LegacyTiles = layer.AssetsData.LegacyTiles.Select((UndertaleRoom.Tile tile) =>
                            new Tile()
                            {
                                Asset = tile.ObjectDefinition?.Name?.Content,
                                X = tile.X,
                                Y = tile.Y,
                                SourceX = tile.SourceX,
                                SourceY = tile.SourceY,
                                Width = tile.Width,
                                Height = tile.Height,
                                Depth = tile.TileDepth,
                                ScaleX = tile.ScaleX,
                                ScaleY = tile.ScaleY,
                                Color = tile.Color
                            })
                            .ToList(),
                        Sprites = layer.AssetsData.Sprites.Select((UndertaleRoom.SpriteInstance sprite) =>
                            new AssetsLayer.SpriteAssetInstance()
                            {
                                Name = sprite.Name?.Content,
                                Sprite = sprite.Sprite?.Name?.Content,
                                X = sprite.X,
                                Y = sprite.Y,
                                ScaleX = sprite.ScaleX,
                                ScaleY = sprite.ScaleY,
                                Color = sprite.Color,
                                AnimationSpeed = sprite.AnimationSpeed,
                                AnimationSpeedType = (AnimationSpeedType)sprite.AnimationSpeedType,
                                FrameIndex = sprite.FrameIndex,
                                Rotation = sprite.Rotation
                            })
                            .ToList(),
                        Sequences = layer.AssetsData.Sequences?.Select((UndertaleRoom.SequenceInstance seq) =>
                            new AssetsLayer.SequenceAssetInstance()
                            {
                                Name = seq.Name?.Content,
                                Sequence = seq.Sequence?.Name?.Content,
                                X = seq.X,
                                Y = seq.Y,
                                ScaleX = seq.ScaleX,
                                ScaleY = seq.ScaleY,
                                Color = seq.Color,
                                AnimationSpeed = seq.AnimationSpeed,
                                AnimationSpeedType = (AnimationSpeedType)seq.AnimationSpeedType,
                                FrameIndex = seq.FrameIndex,
                                Rotation = seq.Rotation
                            })
                            .ToList(),
                        NineSlices = layer.AssetsData.NineSlices?.Select((UndertaleRoom.SpriteInstance sprite) =>
                            new AssetsLayer.SpriteAssetInstance()
                            {
                                Name = sprite.Name?.Content,
                                Sprite = sprite.Sprite?.Name?.Content,
                                X = sprite.X,
                                Y = sprite.Y,
                                ScaleX = sprite.ScaleX,
                                ScaleY = sprite.ScaleY,
                                Color = sprite.Color,
                                AnimationSpeed = sprite.AnimationSpeed,
                                AnimationSpeedType = (AnimationSpeedType)sprite.AnimationSpeedType,
                                FrameIndex = sprite.FrameIndex,
                                Rotation = sprite.Rotation
                            })
                            .ToList(),
                        ParticleSystems = layer.AssetsData.ParticleSystems?.Select((UndertaleRoom.ParticleSystemInstance psys) =>
                            new AssetsLayer.ParticleSystemAssetInstance()
                            {
                                Name = psys.Name?.Content,
                                ParticleSystem = psys.ParticleSystem?.Name?.Content,
                                X = psys.X,
                                Y = psys.Y,
                                ScaleX = psys.ScaleX,
                                ScaleY = psys.ScaleY,
                                Color = psys.Color,
                                Rotation = psys.Rotation
                            })
                            .ToList(),
                        TextItems = layer.AssetsData.TextItems?.Select((UndertaleRoom.TextItemInstance text) =>
                            new AssetsLayer.TextItemAssetInstance()
                            {
                                Name = text.Name?.Content,
                                Font = text.Font?.Name?.Content,
                                Text = text.Text?.Content,
                                X = text.X,
                                Y = text.Y,
                                ScaleX = text.ScaleX,
                                ScaleY = text.ScaleY,
                                Color = text.Color,
                                Rotation = text.Rotation,
                                OriginX = text.OriginX,
                                OriginY = text.OriginY,
                                Alignment = text.Alignment,
                                CharSpacing = text.CharSpacing,
                                LineSpacing = text.LineSpacing,
                                FrameWidth = text.FrameWidth,
                                FrameHeight = text.FrameHeight,
                                Wrap = text.Wrap
                            })
                            .ToList()
                    },
                    UndertaleRoom.LayerType.Effect => new EffectLayer()
                    {
                        EffectType = layer.EffectType?.Content,
                        Properties = layer.EffectProperties.Select((UndertaleRoom.EffectProperty prop) =>
                            new EffectLayer.EffectProperty()
                            {
                                Kind = (EffectLayer.EffectProperty.PropertyType)prop.Kind,
                                Name = prop.Name?.Content,
                                Value = prop.Value?.Content
                            })
                            .ToList()
                    },
                    _ => throw new ProjectException($"Failed to find valid layer type for {room.Name.Content}")
                };
                newLayer.Name = layer.LayerName?.Content;
                newLayer.ID = layer.LayerId;
                newLayer.Depth = layer.LayerDepth;
                newLayer.XOffset = layer.XOffset;
                newLayer.YOffset = layer.YOffset;
                newLayer.HSpeed = layer.HSpeed;
                newLayer.VSpeed = layer.VSpeed;
                newLayer.IsVisible = layer.IsVisible;
                Layers.Add(newLayer);
            }
        }

        // Update sequences, if they exist
        if (room.Sequences is not null)
        {
            Sequences = new(room.Sequences.Count);
            foreach (var seq in room.Sequences)
            {
                Sequences.Add(seq.Resource?.Name?.Content);
            }
        }
    }

    /// <inheritdoc/>
    public void Serialize(ProjectContext projectContext, string destinationFile)
    {
        using FileStream fs = new(destinationFile, FileMode.Create);
        JsonSerializer.Serialize<ISerializableProjectAsset>(fs, this, ProjectContext.JsonOptions);
    }

    /// <inheritdoc/>
    public void PreImport(ProjectContext projectContext)
    {
        if (projectContext.Data.Rooms.ByName(DataName) is UndertaleRoom existing)
        {
            // Path found
            _dataAsset = existing;
        }
        else
        {
            // No path found; create new one
            _dataAsset = new()
            {
                Name = projectContext.MakeString(DataName)
            };
            projectContext.Data.Rooms.Add(_dataAsset);
        }
    }

    /// <inheritdoc/>
    public IProjectAsset Import(ProjectContext projectContext)
    {
        UndertaleRoom room = _dataAsset;

        // Update all main properties
        room.Caption = projectContext.MakeString(Caption);
        room.Width = Width;
        room.Height = Height;
        room.Speed = Speed;
        room.Persistent = Persistent;
        room.BackgroundColor = BackgroundColor;
        room.DrawBackgroundColor = DrawBackgroundColor;
        room.CreationCodeId = projectContext.FindCode(CreationCodeEntry, this);
        room.Flags = Flags;
        room.World = World;
        room.Top = Top;
        room.Left = Left;
        room.Right = Right;
        room.Bottom = Bottom;
        room.GravityX = GravityX;
        room.GravityY = GravityY;
        room.MetersPerPixel = MetersPerPixel;

        // Update backgrounds
        room.Backgrounds ??= [];
        room.Backgrounds.Clear();
        room.Backgrounds.SetCapacity(Backgrounds.Count);
        foreach (Background bg in Backgrounds)
        {
            room.Backgrounds.Add(new()
            {
                Enabled = bg.Enabled,
                Foreground = bg.Foreground,
                BackgroundDefinition = projectContext.FindBackground(bg.BackgroundAsset, this),
                X = bg.X,
                Y = bg.Y,
                SpeedX = bg.SpeedX,
                SpeedY = bg.SpeedY,
                Stretch = bg.Stretch,
                TiledHorizontally = bg.TiledHorizontally,
                TiledVertically = bg.TiledVertically
            });
        }

        // Update views
        room.Views ??= [];
        room.Views.Clear();
        room.Views.SetCapacity(Views.Count);
        foreach (View view in Views)
        {
            room.Views.Add(new()
            {
                Enabled = view.Enabled,
                ViewX = view.ViewX,
                ViewY = view.ViewY,
                ViewWidth = view.ViewWidth,
                ViewHeight = view.ViewHeight,
                PortX = view.PortX,
                PortY = view.PortY,
                PortWidth = view.PortWidth,
                PortHeight = view.PortHeight,
                BorderX = view.BorderX,
                BorderY = view.BorderY,
                SpeedX = view.SpeedX,
                SpeedY = view.SpeedY,
                ObjectId = projectContext.FindGameObject(view.FollowsObject, this)
            });
        }

        // Update game objects
        room.GameObjects ??= [];
        room.GameObjects.Clear();
        room.GameObjects.SetCapacity(GameObjects.Count);
        foreach (GameObject obj in GameObjects)
        {
            room.GameObjects.Add(new()
            {
                ObjectDefinition = projectContext.FindGameObject(obj.ObjectName, this),
                X = obj.X,
                Y = obj.Y,
                CreationCode = projectContext.FindCode(obj.CreationCodeEntry, this),
                ScaleX = obj.ScaleX,
                ScaleY = obj.ScaleY,
                Color = obj.Color,
                Rotation = obj.Rotation,
                PreCreateCode = projectContext.FindCode(obj.PreCreateCodeEntry, this),
                ImageSpeed = obj.ImageSpeed,
                ImageIndex = obj.ImageIndex,
                InstanceID = ++projectContext.Data.GeneralInfo.LastObj
            });
        }

        // Update tiles
        room.Tiles ??= [];
        room.Tiles.Clear();
        room.Tiles.SetCapacity(Tiles.Count);
        foreach (Tile tile in Tiles)
        {
            UndertaleRoom.Tile newTile = new()
            {
                X = tile.X,
                Y = tile.Y,
                SourceX = tile.SourceX,
                SourceY = tile.SourceY,
                Width = tile.Width,
                Height = tile.Height,
                TileDepth = tile.Depth,
                ScaleX = tile.ScaleX,
                ScaleY = tile.ScaleY,
                Color = tile.Color
            };
            if (projectContext.Data.IsGameMaker2())
            {
                newTile.spriteMode = true;
                newTile.SpriteDefinition = projectContext.FindSprite(tile.Asset, this);
            }
            else
            {
                newTile.BackgroundDefinition = projectContext.FindBackground(tile.Asset, this);
            }
            room.Tiles.Add(newTile);
        }

        // Update layers, if they exist
        if (Layers is not null)
        {
            room.Layers ??= [];
            room.Layers.Clear();
            room.Layers.SetCapacity(Layers.Count);
            foreach (Layer layer in Layers)
            {
                room.Layers.Add(new()
                {
                    LayerName = projectContext.MakeString(layer.Name),
                    LayerId = layer.ID,
                    LayerDepth = layer.Depth,
                    XOffset = layer.XOffset,
                    YOffset = layer.YOffset,
                    HSpeed = layer.HSpeed,
                    VSpeed = layer.VSpeed,
                    IsVisible = layer.IsVisible,
                    LayerType = layer switch
                    {
                        InstancesLayer => UndertaleRoom.LayerType.Instances,
                        TilesLayer => UndertaleRoom.LayerType.Tiles,
                        BackgroundLayer => UndertaleRoom.LayerType.Background,
                        AssetsLayer => UndertaleRoom.LayerType.Assets,
                        EffectLayer => UndertaleRoom.LayerType.Effect,
                        _ => throw new ProjectException($"Failed to find valid layer type for {room.Name.Content}")
                    },
                    Data = layer switch
                    {
                        InstancesLayer instances => new UndertaleRoom.Layer.LayerInstancesData()
                        {
                            Instances = new(instances.InstanceIndices.Select((int index) =>
                            {
                                if (index < 0 || index >= room.GameObjects.Count)
                                {
                                    return new UndertaleRoom.GameObject()
                                    {
                                        InstanceID = ++projectContext.Data.GeneralInfo.LastObj,
                                        Nonexistent = true
                                    };
                                }
                                return room.GameObjects[index];
                            }))
                        },
                        TilesLayer tiles => new UndertaleRoom.Layer.LayerTilesData()
                        {
                            Background = projectContext.FindBackground(tiles.Background, this),
                            TilesX = tiles.TilesX,
                            TilesY = tiles.TilesY,
                            TileData = ConvertListToTileData(tiles.TilesX, tiles.TilesY, tiles.TileData)
                        },
                        BackgroundLayer bg => new UndertaleRoom.Layer.LayerBackgroundData()
                        {
                            Visible = bg.BackgroundVisible,
                            Foreground = bg.Foreground,
                            Sprite = projectContext.FindSprite(bg.Sprite, this),
                            TiledHorizontally = bg.TiledHorizontally,
                            TiledVertically = bg.TiledVertically,
                            Stretch = bg.Stretch,
                            Color = bg.Color,
                            FirstFrame = bg.FirstFrame,
                            AnimationSpeed = bg.AnimationSpeed,
                            AnimationSpeedType = (Models.AnimationSpeedType)bg.AnimationSpeedType
                        },
                        AssetsLayer assets => new UndertaleRoom.Layer.LayerAssetsData()
                        {
                            LegacyTiles = new(assets.LegacyTiles.Select((Tile tile) =>
                            {
                                UndertaleRoom.Tile newTile = new()
                                {
                                    X = tile.X,
                                    Y = tile.Y,
                                    SourceX = tile.SourceX,
                                    SourceY = tile.SourceY,
                                    Width = tile.Width,
                                    Height = tile.Height,
                                    TileDepth = tile.Depth,
                                    ScaleX = tile.ScaleX,
                                    ScaleY = tile.ScaleY,
                                    Color = tile.Color
                                };
                                if (projectContext.Data.IsGameMaker2())
                                {
                                    newTile.spriteMode = true;
                                    newTile.SpriteDefinition = projectContext.FindSprite(tile.Asset, this);
                                }
                                else
                                {
                                    newTile.BackgroundDefinition = projectContext.FindBackground(tile.Asset, this);
                                }
                                return newTile;
                            })),
                            Sprites = new(assets.Sprites.Select((AssetsLayer.SpriteAssetInstance sprite) =>
                                new UndertaleRoom.SpriteInstance()
                                {
                                    Name = projectContext.MakeString(sprite.Name),
                                    Sprite = projectContext.FindSprite(sprite.Sprite, this),
                                    X = sprite.X,
                                    Y = sprite.Y,
                                    ScaleX = sprite.ScaleX,
                                    ScaleY = sprite.ScaleY,
                                    Color = sprite.Color,
                                    AnimationSpeed = sprite.AnimationSpeed,
                                    AnimationSpeedType = (Models.AnimationSpeedType)sprite.AnimationSpeedType,
                                    FrameIndex = sprite.FrameIndex,
                                    Rotation = sprite.Rotation
                                })),
                            Sequences = assets.Sequences is null ? null :
                                new(assets.Sequences.Select((AssetsLayer.SequenceAssetInstance seq) =>
                                    new UndertaleRoom.SequenceInstance()
                                    {
                                        Name = projectContext.MakeString(seq.Name),
                                        Sequence = projectContext.FindSequence(seq.Sequence, this),
                                        X = seq.X,
                                        Y = seq.Y,
                                        ScaleX = seq.ScaleX,
                                        ScaleY = seq.ScaleY,
                                        Color = seq.Color,
                                        AnimationSpeed = seq.AnimationSpeed,
                                        AnimationSpeedType = (Models.AnimationSpeedType)seq.AnimationSpeedType,
                                        FrameIndex = seq.FrameIndex,
                                        Rotation = seq.Rotation
                                    })),
                            NineSlices = assets.NineSlices is null ? null :
                                new(assets.NineSlices.Select((AssetsLayer.SpriteAssetInstance sprite) =>
                                    new UndertaleRoom.SpriteInstance()
                                    {
                                        Name = projectContext.MakeString(sprite.Name),
                                        Sprite = projectContext.FindSprite(sprite.Sprite, this),
                                        X = sprite.X,
                                        Y = sprite.Y,
                                        ScaleX = sprite.ScaleX,
                                        ScaleY = sprite.ScaleY,
                                        Color = sprite.Color,
                                        AnimationSpeed = sprite.AnimationSpeed,
                                        AnimationSpeedType = (Models.AnimationSpeedType)sprite.AnimationSpeedType,
                                        FrameIndex = sprite.FrameIndex,
                                        Rotation = sprite.Rotation
                                    })),
                            ParticleSystems = assets.ParticleSystems is null ? null :
                                new(assets.ParticleSystems.Select((AssetsLayer.ParticleSystemAssetInstance psys) =>
                                    new UndertaleRoom.ParticleSystemInstance()
                                    {
                                        Name = projectContext.MakeString(psys.Name),
                                        ParticleSystem = projectContext.FindParticleSystem(psys.ParticleSystem, this),
                                        X = psys.X,
                                        Y = psys.Y,
                                        ScaleX = psys.ScaleX,
                                        ScaleY = psys.ScaleY,
                                        Color = psys.Color,
                                        Rotation = psys.Rotation
                                    })),
                            TextItems = assets.TextItems is null ? null :
                                new(assets.TextItems.Select((AssetsLayer.TextItemAssetInstance text) =>
                                    new UndertaleRoom.TextItemInstance()
                                    {
                                        Name = projectContext.MakeString(text.Name),
                                        Font = projectContext.FindFont(text.Font, this),
                                        Text = projectContext.MakeString(text.Text),
                                        X = text.X,
                                        Y = text.Y,
                                        ScaleX = text.ScaleX,
                                        ScaleY = text.ScaleY,
                                        Color = text.Color,
                                        Rotation = text.Rotation,
                                        OriginX = text.OriginX,
                                        OriginY = text.OriginY,
                                        Alignment = text.Alignment,
                                        CharSpacing = text.CharSpacing,
                                        LineSpacing = text.LineSpacing,
                                        FrameWidth = text.FrameWidth,
                                        FrameHeight = text.FrameHeight,
                                        Wrap = text.Wrap
                                    }))
                        },
                        EffectLayer effect => new UndertaleRoom.Layer.LayerEffectData()
                        {
                            EffectType = projectContext.MakeString(effect.EffectType),
                            Properties = new(effect.Properties.Select((EffectLayer.EffectProperty prop) =>
                                new UndertaleRoom.EffectProperty()
                                {
                                    Kind = (UndertaleRoom.EffectProperty.PropertyType)prop.Kind,
                                    Name = projectContext.MakeString(prop.Name),
                                    Value = projectContext.MakeString(prop.Value)
                                }))
                        },
                        _ => throw new ProjectException($"Failed to find valid layer type for {room.Name.Content}")
                    }
                });
            }
        }

        // Update sequences, if they exist
        if (Sequences is not null)
        {
            room.Sequences = new(Sequences.Count);
            foreach (string seq in Sequences)
            {
                room.Sequences.Add(new(projectContext.FindSequence(seq, this)));
            }
        }

        return room;
    }

    /// <summary>
    /// Converts tile data from a 2d array to a 1d list, to be serialized as JSON.
    /// </summary>
    private static List<uint> ConvertTileDataToList(uint[][] tileData)
    {
        if (tileData.Length == 0)
        {
            return [];
        }

        List<uint> tiles = new(tileData.Length * tileData[0].Length);
        foreach (uint[] subarray in tileData)
        {
            tiles.AddRange(subarray);
        }
        return tiles;
    }

    /// <summary>
    /// Converts tile data from a 1d list to a 2d array.
    /// </summary>
    private static uint[][] ConvertListToTileData(uint tilesX, uint tilesY, List<uint> tileData)
    {
        uint[][] newData = new uint[tilesY][];
        int pos = 0;
        int y = 0;
        while (pos < tileData.Count)
        {
            uint[] row = newData[y] = new uint[tilesX];

            int numElemsInRow = Math.Min((int)tilesX, tileData.Count - pos);
            for (int i = 0; i < numElemsInRow; i++)
            {
                row[i] = tileData[pos++];
            }

            y++;
        }
        return newData;
    }
}
