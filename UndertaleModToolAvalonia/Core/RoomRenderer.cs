using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using Avalonia.Skia;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;
using UndertaleModLib.Models;
using UndertaleModLib.Util;
using static UndertaleModToolAvalonia.RoomRenderer.RenderCommandsBuilder;

namespace UndertaleModToolAvalonia;

public class RoomRenderer
{
    public class RenderCommandsBuilder
    {
        public interface IRenderCommand;

        public readonly record struct BackgroundColorRenderCommand(uint RoomWidth, uint RoomHeight, uint Color)
            : IRenderCommand;
        public readonly record struct BackgroundRenderCommand(SKImage Image,
            ushort SourceX, ushort SourceY, ushort SourceWidth, ushort SourceHeight,
            ushort TargetX, ushort TargetY, ushort TargetWidth, ushort TargetHeight, ushort BoundingWidth, ushort BoundingHeight,
            float X, float Y, float ScaleX, float ScaleY, uint Color, bool TiledHorizontally, bool TiledVertically, uint RoomWidth, uint RoomHeight)
            : IRenderCommand;
        public readonly record struct TileRenderCommand(SKImage Image,
            ushort SourceX, ushort SourceY, ushort SourceWidth, ushort SourceHeight,
            ushort TargetX, ushort TargetY, ushort TargetWidth, ushort TargetHeight,
            int TileSourceX, int TileSourceY, float X, float Y, float ScaleX, float ScaleY)
            : IRenderCommand;
        public readonly record struct GameObjectRenderCommand(SKImage Image,
            ushort SourceX, ushort SourceY, ushort SourceWidth, ushort SourceHeight,
            ushort TargetX, ushort TargetY, ushort TargetWidth, ushort TargetHeight,
            int X, int Y, float ScaleX, float ScaleY, uint Color, float Rotation, int OriginX, int OriginY)
            : IRenderCommand;
        public readonly record struct SpriteRenderCommand(SKImage Image,
            ushort SourceX, ushort SourceY, ushort SourceWidth, ushort SourceHeight,
            ushort TargetX, ushort TargetY, ushort TargetWidth, ushort TargetHeight,
            float X, float Y, float ScaleX, float ScaleY, uint Color, float Rotation, int OriginX, int OriginY)
            : IRenderCommand;
        public readonly record struct LayerTilesRenderCommand(SKImage Image,
            ushort SourceX, ushort SourceY, ushort SourceWidth, ushort SourceHeight,
            ushort TargetX, ushort TargetY, ushort TargetWidth, ushort TargetHeight,
            float X, float Y, uint[][] TileData, uint TileColumns, uint TileW, uint TileH, uint OutputBorderX, uint OutputBorderY)
            : IRenderCommand;

        public readonly UndertaleRoom Room;
        public readonly List<IRenderCommand> RenderCommands = [];

        readonly MainViewModel mainVM = App.Services.GetRequiredService<MainViewModel>();

        public RenderCommandsBuilder(UndertaleRoom room)
        {
            Room = room;

            if (!(Room.Flags.HasFlag(UndertaleRoom.RoomEntryFlags.IsGMS2) || Room.Flags.HasFlag(UndertaleRoom.RoomEntryFlags.IsGM2024_13)))
            {
                AddBackgroundColor(Room.BackgroundColor);
                AddBackgrounds(Room.Backgrounds, foregrounds: false);
                // TODO: Order tiles and game objects by depth
                AddTiles(Room.Tiles);
                AddGameObjects(Room.GameObjects);
                AddBackgrounds(Room.Backgrounds, foregrounds: true);
            }
            else
            {
                IOrderedEnumerable<UndertaleRoom.Layer> layers = Room.Layers.Reverse().OrderByDescending(x => x.LayerDepth);
                foreach (UndertaleRoom.Layer layer in layers)
                {
                    if (!layer.IsVisible)
                        continue;

                    switch (layer.LayerType)
                    {
                        case UndertaleRoom.LayerType.Path:
                        case UndertaleRoom.LayerType.Path2:
                            break;
                        case UndertaleRoom.LayerType.Background:
                            AddLayerBackground(layer);
                            break;
                        case UndertaleRoom.LayerType.Instances:
                            AddGameObjects(layer.InstancesData.Instances);
                            break;
                        case UndertaleRoom.LayerType.Assets:
                            AddTiles(layer.AssetsData.LegacyTiles, layer);
                            AddSprites(layer.AssetsData.Sprites, layer);
                            // layer.AssetsData.Sequences
                            // layer.AssetsData.NineSlices
                            // layer.AssetsData.ParticleSystems
                            // layer.AssetsData.TextItems
                            break;
                        case UndertaleRoom.LayerType.Tiles:
                            AddLayerTiles(layer);
                            break;
                            //case UndertaleRoom.LayerType.Effect:
                            // layer.EffectData
                            //break;
                    }
                }
            }
        }

        void AddBackgroundColor(uint color)
        {
            RenderCommands.Add(new BackgroundColorRenderCommand(
                RoomWidth: Room.Width,
                RoomHeight: Room.Height,
                Color: color
            ));
        }

        void AddBackgrounds(IList<UndertaleRoom.Background> roomBackgrounds, bool foregrounds)
        {
            foreach (UndertaleRoom.Background roomBackground in roomBackgrounds)
            {
                if (roomBackground.Foreground == foregrounds)
                {
                    if (!roomBackground.Enabled)
                        continue;

                    UndertaleTexturePageItem? texture = roomBackground.BackgroundDefinition?.Texture;
                    if (texture is null)
                        continue;

                    SKImage? image = mainVM.ImageCache.GetCachedImageFromTexturePageItem(texture);
                    if (image is null)
                        continue;

                    roomBackground.UpdateStretch();

                    RenderCommands.Add(new BackgroundRenderCommand(
                       Image: image,
                       SourceX: texture.SourceX,
                       SourceY: texture.SourceY,
                       SourceWidth: texture.SourceWidth,
                       SourceHeight: texture.SourceHeight,
                       TargetX: texture.TargetX,
                       TargetY: texture.TargetY,
                       TargetWidth: texture.TargetWidth,
                       TargetHeight: texture.TargetHeight,
                       BoundingWidth: texture.BoundingWidth,
                       BoundingHeight: texture.BoundingHeight,
                       X: roomBackground.X,
                       Y: roomBackground.Y,
                       ScaleX: roomBackground.CalcScaleX,
                       ScaleY: roomBackground.CalcScaleY,
                       Color: 0xFFFFFFFF,
                       TiledHorizontally: roomBackground.TiledHorizontally,
                       TiledVertically: roomBackground.TiledVertically,
                       RoomWidth: Room.Width,
                       RoomHeight: Room.Height
                    ));
                }
            }
        }

        void AddTiles(IList<UndertaleRoom.Tile> roomTiles, UndertaleRoom.Layer? layer = null)
        {
            IOrderedEnumerable<UndertaleRoom.Tile> orderedRoomTiles = roomTiles.OrderByDescending(x => x.TileDepth);
            foreach (UndertaleRoom.Tile roomTile in orderedRoomTiles)
            {
                SKImage? image = mainVM.ImageCache.GetCachedImageFromTile(roomTile);
                if (image is null)
                    continue;

                UndertaleTexturePageItem? texture = roomTile.Tpag;
                if (texture is null)
                    continue;

                RenderCommands.Add(new TileRenderCommand(
                    Image: image,
                    SourceX: texture.SourceX,
                    SourceY: texture.SourceY,
                    SourceWidth: texture.SourceWidth,
                    SourceHeight: texture.SourceHeight,
                    TargetX: texture.TargetX,
                    TargetY: texture.TargetY,
                    TargetWidth: texture.TargetWidth,
                    TargetHeight: texture.TargetHeight,
                    TileSourceX: roomTile.SourceX,
                    TileSourceY: roomTile.SourceY,
                    X: (layer?.XOffset ?? 0) + roomTile.X - Math.Min(roomTile.SourceX - texture.TargetX, 0),
                    Y: (layer?.YOffset ?? 0) + roomTile.Y - Math.Min(roomTile.SourceX - texture.TargetX, 0),
                    ScaleX: roomTile.ScaleX,
                    ScaleY: roomTile.ScaleY
                ));
            }
        }

        void AddGameObjects(IList<UndertaleRoom.GameObject> roomGameObjects)
        {
            foreach (UndertaleRoom.GameObject roomGameObject in roomGameObjects)
            {
                UndertaleTexturePageItem? texture = roomGameObject.ObjectDefinition?.Sprite?.Textures?.ElementAtOrDefault(roomGameObject.ImageIndex)?.Texture;
                if (texture is null)
                    continue;

                SKImage? image = mainVM.ImageCache.GetCachedImageFromTexturePageItem(texture);
                if (image is null)
                    continue;

                // image, source xywh, target xywh, x/y offset, scale x/y, color, rotation, origin x/y
                RenderCommands.Add(new GameObjectRenderCommand(
                    Image: image,
                    SourceX: texture.SourceX,
                    SourceY: texture.SourceY,
                    SourceWidth: texture.SourceWidth,
                    SourceHeight: texture.SourceHeight,
                    TargetX: texture.TargetX,
                    TargetY: texture.TargetY,
                    TargetWidth: texture.TargetWidth,
                    TargetHeight: texture.TargetHeight,
                    X: roomGameObject.X,
                    Y: roomGameObject.Y,
                    ScaleX: roomGameObject.ScaleX,
                    ScaleY: roomGameObject.ScaleY,
                    Color: roomGameObject.Color,
                    Rotation: roomGameObject.OppositeRotation,
                    OriginX: roomGameObject.ObjectDefinition!.Sprite.OriginX,
                    OriginY: roomGameObject.ObjectDefinition!.Sprite.OriginY
                ));
            }
        }

        void AddSprites(IList<UndertaleRoom.SpriteInstance> roomSprites, UndertaleRoom.Layer layer)
        {
            foreach (UndertaleRoom.SpriteInstance roomSprite in roomSprites)
            {
                UndertaleTexturePageItem? texture = roomSprite.Sprite?.Textures?.ElementAtOrDefault((int)roomSprite.FrameIndex)?.Texture;
                if (texture is null)
                    continue;

                SKImage? image = mainVM.ImageCache.GetCachedImageFromTexturePageItem(texture);
                if (image is null)
                    continue;

                RenderCommands.Add(new SpriteRenderCommand(
                    Image: image,
                    SourceX: texture.SourceX,
                    SourceY: texture.SourceY,
                    SourceWidth: texture.SourceWidth,
                    SourceHeight: texture.SourceHeight,
                    TargetX: texture.TargetX,
                    TargetY: texture.TargetY,
                    TargetWidth: texture.TargetWidth,
                    TargetHeight: texture.TargetHeight,
                    X: layer.XOffset + roomSprite.X,
                    Y: layer.YOffset + roomSprite.Y,
                    ScaleX: roomSprite.ScaleX,
                    ScaleY: roomSprite.ScaleY,
                    Color: roomSprite.Color,
                    Rotation: roomSprite.OppositeRotation,
                    OriginX: roomSprite.Sprite!.OriginX,
                    OriginY: roomSprite.Sprite!.OriginY
                ));
            }
        }

        void AddLayerBackground(UndertaleRoom.Layer layer)
        {
            if (!layer.BackgroundData.Visible)
                return;

            if (layer.BackgroundData.Sprite is null)
            {
                AddBackgroundColor(layer.BackgroundData.Color);
                return;
            }

            UndertaleTexturePageItem? texture = layer.BackgroundData.Sprite?.Textures?.ElementAtOrDefault((int)layer.BackgroundData.FirstFrame)?.Texture;
            if (texture is null)
                return;

            SKImage? image = mainVM.ImageCache.GetCachedImageFromTexturePageItem(texture);
            if (image is null)
                return;

            layer.BackgroundData.UpdateScale();

            // image, source xywh, target xywh, x/y offset, scale x/y, color, tile h/v, parent w/h
            RenderCommands.Add(new BackgroundRenderCommand(
                Image: image,
                SourceX: texture.SourceX,
                SourceY: texture.SourceY,
                SourceWidth: texture.SourceWidth,
                SourceHeight: texture.SourceHeight,
                TargetX: texture.TargetX,
                TargetY: texture.TargetY,
                TargetWidth: texture.TargetWidth,
                TargetHeight: texture.TargetHeight,
                BoundingWidth: texture.BoundingWidth,
                BoundingHeight: texture.BoundingHeight,
                X: layer.XOffset,
                Y: layer.YOffset,
                ScaleX: layer.BackgroundData.CalcScaleX,
                ScaleY: layer.BackgroundData.CalcScaleY,
                Color: layer.BackgroundData.Color,
                TiledHorizontally: layer.BackgroundData.TiledHorizontally,
                TiledVertically: layer.BackgroundData.TiledVertically,
                RoomWidth: Room.Width,
                RoomHeight: Room.Height
            ));
        }

        void AddLayerTiles(UndertaleRoom.Layer layer)
        {
            UndertaleTexturePageItem? texture = layer.TilesData.Background?.Texture;
            if (texture is null)
                return;

            GMImage? gmImage = texture.TexturePage?.TextureData?.Image;
            if (gmImage is null)
                return;

            SKImage? image = mainVM.ImageCache.GetCachedImageFromGMImage(gmImage);
            if (image is null)
                return;

            // image, source xywh, target xywh, x/y offset, tilesdata, tile columns, tile w/h, border x/y
            RenderCommands.Add(new LayerTilesRenderCommand(
                Image: image,
                SourceX: texture.SourceX,
                SourceY: texture.SourceY,
                SourceWidth: texture.SourceWidth,
                SourceHeight: texture.SourceHeight,
                TargetX: texture.TargetX,
                TargetY: texture.TargetY,
                TargetWidth: texture.TargetWidth,
                TargetHeight: texture.TargetHeight,
                X: layer.XOffset,
                Y: layer.YOffset,
                TileData: layer.TilesData.TileData.Select(x => x.ToArray()).ToArray(),
                TileColumns: layer.TilesData.Background!.GMS2TileColumns,
                TileW: layer.TilesData.Background!.GMS2TileWidth,
                TileH: layer.TilesData.Background!.GMS2TileHeight,
                OutputBorderX: layer.TilesData.Background!.GMS2OutputBorderX,
                OutputBorderY: layer.TilesData.Background!.GMS2OutputBorderY
            ));
        }
    }

    SKCanvas Canvas = null!;

    // Used to keep the images alive while room is open
    List<SKImage> usedImages = [];
    List<SKImage> currentUsedImages = [];

    readonly List<SKPoint> vertices = [];
    readonly List<SKPoint> texs = [];

    public void RenderCommands(List<RenderCommandsBuilder.IRenderCommand> renderCommands, SKCanvas canvas)
    {
        Canvas = canvas;

        foreach (var renderCommand in renderCommands)
        {
            switch (renderCommand)
            {
                case BackgroundColorRenderCommand c:
                    RenderBackgroundColorRenderCommand(c);
                    break;
                case BackgroundRenderCommand c:
                    RenderBackgroundRenderCommand(c);
                    break;
                case TileRenderCommand c:
                    RenderTileRenderCommand(c);
                    break;
                case GameObjectRenderCommand c:
                    RenderGameObjectRenderCommand(c);
                    break;
                case SpriteRenderCommand c:
                    RenderSpriteRenderCommand(c);
                    break;
                case LayerTilesRenderCommand c:
                    RenderLayerTilesRenderCommand(c);
                    break;
            }
        }

        (currentUsedImages, usedImages) = (usedImages, currentUsedImages);
        currentUsedImages.Clear();
    }

    void RenderBackgroundColorRenderCommand(BackgroundColorRenderCommand c)
    {
        Color color = UndertaleColor.ToColor(c.Color);
        Canvas.DrawRect(0, 0, c.RoomWidth, c.RoomHeight, new SKPaint { Color = color.ToSKColor() });
    }

    void RenderBackgroundRenderCommand(BackgroundRenderCommand c)
    {
        currentUsedImages.Add(c.Image);

        var w = c.BoundingWidth * c.ScaleX;
        var h = c.BoundingHeight * c.ScaleY;

        var startX = c.TiledHorizontally ? ((c.X % w) - w) : c.X;
        var startY = c.TiledVertically ? ((c.Y % h) - h) : c.Y;

        var endX = c.TiledHorizontally ? c.RoomWidth : (startX + w);
        var endY = c.TiledVertically ? c.RoomHeight : (startY + h);

        SKPaint skPaint = new()
        {
            ColorFilter = SKColorFilter.CreateBlendMode(UndertaleColor.ToColor(c.Color).ToSKColor(), SKBlendMode.Modulate),
        };

        for (var x = startX; x < endX; x += w)
        {
            for (var y = startY; y < endY; y += h)
            {
                Canvas.Save();
                if (c.TiledHorizontally || c.TiledVertically)
                {
                    // TODO: Only clip in direction of tiling
                    Canvas.ClipRect(new SKRect(0, 0, c.RoomWidth, c.RoomHeight));
                }
                Canvas.Translate(x + c.TargetX, y + c.TargetY);
                Canvas.Scale(c.ScaleX, c.ScaleY);
                Canvas.DrawImage(c.Image, 0, 0, skPaint);
                Canvas.Restore();
            }
        }
    }

    void RenderTileRenderCommand(TileRenderCommand c)
    {
        currentUsedImages.Add(c.Image);

        Canvas.Save();

        Canvas.Translate(c.X, c.Y);
        Canvas.Scale(c.ScaleX, c.ScaleY);
        Canvas.DrawImage(c.Image, 0, 0);
        Canvas.Restore();
    }

    void RenderGameObjectRenderCommand(GameObjectRenderCommand c)
    {
        // TODO: roomGameObject.ImageSpeed

        currentUsedImages.Add(c.Image);

        Canvas.Save();
        Canvas.Translate(c.X, c.Y);
        Canvas.RotateDegrees(c.Rotation);
        Canvas.Scale(c.ScaleX, c.ScaleY);

        Canvas.DrawImage(c.Image, -c.OriginX + c.TargetX, -c.OriginY + c.TargetY, new SKPaint()
        {
            ColorFilter = SKColorFilter.CreateBlendMode(UndertaleColor.ToColor(c.Color).ToSKColor(), SKBlendMode.Modulate),
        });

        Canvas.Restore();
    }

    void RenderSpriteRenderCommand(SpriteRenderCommand c)
    {
        // TODO: roomSprite.AnimationSpeed
        // TODO: roomSprite.AnimationSpeedType

        currentUsedImages.Add(c.Image);

        Canvas.Save();
        Canvas.Translate(c.X, c.Y);
        Canvas.RotateDegrees(c.Rotation);
        Canvas.Scale(c.ScaleX, c.ScaleY);

        Canvas.DrawImage(c.Image, -c.OriginX + c.TargetX, -c.OriginY + c.TargetY, new SKPaint()
        {
            ColorFilter = SKColorFilter.CreateBlendMode(UndertaleColor.ToColor(c.Color).ToSKColor(), SKBlendMode.Modulate),
        });

        Canvas.Restore();
    }

    void RenderLayerTilesRenderCommand(LayerTilesRenderCommand c)
    {
        currentUsedImages.Add(c.Image);

        vertices.Clear();
        texs.Clear();

        static void AddQuad(List<SKPoint> list, float x1, float y1, float x2, float y2, uint transform)
        {
            // Flip X
            if ((transform & 1) != 0)
                (x1, x2) = (x2, x1);

            // Flip Y
            if (((transform >> 1) & 1) != 0)
                (y1, y2) = (y2, y1);

            SKPoint topLeft;
            SKPoint bottomLeft;
            SKPoint topRight;
            SKPoint bottomRight;

            // Rotate 90 degrees clockwise
            if (((transform >> 2) & 1) == 0)
            {
                topLeft = new SKPoint(x1, y1);
                bottomLeft = new SKPoint(x1, y2);
                topRight = new SKPoint(x2, y1);
                bottomRight = new SKPoint(x2, y2);
            }
            else
            {
                topLeft = new SKPoint(x1, y2);
                bottomLeft = new SKPoint(x2, y2);
                topRight = new SKPoint(x1, y1);
                bottomRight = new SKPoint(x2, y1);
            }

            list.Add(topLeft);
            list.Add(bottomLeft);
            list.Add(topRight);

            list.Add(topRight);
            list.Add(bottomLeft);
            list.Add(bottomRight);
        }

        for (int y = 0; y < c.TileData.Length; y++)
            for (int x = 0; x < c.TileData[y].Length; x++)
            {
                uint tile = c.TileData[y][x];
                uint tileId = tile & UndertaleRoomViewModel.TILE_ID;

                if (tileId != 0)
                {
                    uint tileOrientation = tile >> 28;

                    float posX = x * c.TileW;
                    float posY = y * c.TileH;

                    uint tileX = tileId % c.TileColumns;
                    uint tileY = tileId / c.TileColumns;

                    float xx = c.SourceX + (tileX * (c.TileW + c.OutputBorderX * 2) + c.OutputBorderY);
                    float yy = c.SourceY + (tileY * (c.TileH + c.OutputBorderX * 2) + c.OutputBorderY);

                    AddQuad(texs, xx, yy, xx + c.TileW, yy + c.TileH, tileOrientation);
                    AddQuad(vertices, posX, posY, posX + c.TileW, posY + c.TileH, 0);
                }
            }

        SKShader shader = SKShader.CreateImage(c.Image);

        Canvas.Save();
        Canvas.Translate(c.X, c.Y);
        SKPoint[] verticesArray = vertices.ToArray();
        SKPoint[] texsArray = texs.ToArray();
        Canvas.DrawVertices(SKVertexMode.Triangles, verticesArray, texsArray, null, new SKPaint() { Shader = shader });
        Canvas.Restore();
    }
}