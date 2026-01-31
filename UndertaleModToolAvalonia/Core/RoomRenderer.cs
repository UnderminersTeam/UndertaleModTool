using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Skia;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia;

public class RoomRenderer
{
    public UndertaleRoom Room = null!;
    public List<UndertaleRoomEditor.RoomItem> RoomItems = null!;
    public SKCanvas Canvas = null!;

    // Used to keep the images alive while room is open
    List<SKImage> usedImages = [];
    List<SKImage> currentUsedImages = [];

    readonly List<SKPoint> vertices = [];
    readonly List<SKPoint> texs = [];

    readonly MainViewModel mainVM = App.Services.GetRequiredService<MainViewModel>();

    public void RenderRoom()
    {
        RenderBackgroundColor();

        foreach (UndertaleRoomEditor.RoomItem roomItem in RoomItems)
        {
            switch (roomItem.Object)
            {
                case UndertaleRoom.Background roomBackground:
                    RenderBackground(roomBackground);
                    break;
                case UndertaleRoom.GameObject roomGameObject:
                    RenderGameObject(roomGameObject);
                    break;
                case UndertaleRoom.Tile roomTile:
                    RenderTile(roomTile, roomItem.Layer);
                    break;
                case UndertaleRoom.SpriteInstance roomSprite:
                    RenderSprite(roomSprite, roomItem.Layer!);
                    break;
                // layer.AssetsData.Sequences
                // layer.AssetsData.NineSlices
                // layer.AssetsData.ParticleSystems
                // layer.AssetsData.TextItems
                case UndertaleRoom.Layer { LayerType: UndertaleRoom.LayerType.Background } roomLayerBackground:
                    RenderLayerBackground(roomLayerBackground);
                    break;
                case UndertaleRoom.Layer { LayerType: UndertaleRoom.LayerType.Tiles } roomLayerTiles:
                    RenderLayerTiles(roomLayerTiles);
                    break;
            }
        }

        (currentUsedImages, usedImages) = (usedImages, currentUsedImages);
        currentUsedImages.Clear();
    }

    void RenderBackgroundColor()
    {
        if (Room.Flags.HasFlag(UndertaleRoom.RoomEntryFlags.IsGMS2) || Room.Flags.HasFlag(UndertaleRoom.RoomEntryFlags.IsGM2024_13))
        {
        }
        else
        {
            // Fill room background color
            Color color = UndertaleColor.ToColor(Room.BackgroundColor);
            Canvas.DrawRect(0, 0, Room.Width, Room.Height, new SKPaint { Color = color.ToSKColor() });
        }
    }

    void RenderBackground(UndertaleRoom.Background roomBackground)
    {
        if (!roomBackground.Enabled)
            return;

        UndertaleBackground? background = roomBackground.BackgroundDefinition;
        if (background is null)
            return;

        roomBackground.UpdateStretch();

        UndertaleTexturePageItem texture = background.Texture;
        SKImage? image = mainVM.ImageCache.GetCachedImageFromTexturePageItem(texture);
        if (image is null)
            return;

        currentUsedImages.Add(image);

        var w = texture.BoundingWidth * roomBackground.CalcScaleX;
        var h = texture.BoundingHeight * roomBackground.CalcScaleY;

        var startX = roomBackground.TiledHorizontally ? ((roomBackground.X % w) - w) : roomBackground.X;
        var startY = roomBackground.TiledVertically ? ((roomBackground.Y % h) - h) : roomBackground.Y;

        var endX = roomBackground.TiledHorizontally ? roomBackground.ParentRoom.Width : (startX + w);
        var endY = roomBackground.TiledVertically ? roomBackground.ParentRoom.Height : (startY + h);

        for (var x = startX; x < endX; x += w)
        {
            for (var y = startY; y < endY; y += h)
            {
                Canvas.Save();
                if (roomBackground.TiledHorizontally || roomBackground.TiledVertically)
                {
                    // TODO: Only clip in direction of tiling
                    Canvas.ClipRect(new SKRect(0, 0, roomBackground.ParentRoom.Width, roomBackground.ParentRoom.Height));
                }
                Canvas.Translate(x, y);
                Canvas.Translate(texture.TargetX, texture.TargetY);
                Canvas.Scale(roomBackground.CalcScaleX, roomBackground.CalcScaleY);
                Canvas.DrawImage(image, 0, 0);
                Canvas.Restore();
            }
        }
    }

    void RenderLayerBackground(UndertaleRoom.Layer layer)
    {
        UndertaleRoom.Layer.LayerBackgroundData backgroundData = layer.BackgroundData;

        if (!backgroundData.Visible)
            return;

        if (backgroundData.Sprite is null)
        {
            Canvas.DrawRect(0, 0, layer.ParentRoom.Width, layer.ParentRoom.Height, new SKPaint { Color = UndertaleColor.ToColor(backgroundData.Color).ToSKColor() });
            return;
        }

        if (!(backgroundData.FirstFrame >= 0 && backgroundData.FirstFrame < backgroundData.Sprite.Textures.Count))
            return;

        backgroundData.UpdateScale();

        UndertaleTexturePageItem texture = backgroundData.Sprite.Textures[(int)backgroundData.FirstFrame].Texture;

        SKImage? image = mainVM.ImageCache.GetCachedImageFromTexturePageItem(texture);
        if (image is null)
            return;

        currentUsedImages.Add(image);

        var w = backgroundData.Sprite.Width * backgroundData.CalcScaleX;
        var h = backgroundData.Sprite.Height * backgroundData.CalcScaleY;

        var startX = backgroundData.TiledHorizontally ? ((layer.XOffset % w) - w) : layer.XOffset;
        var startY = backgroundData.TiledVertically ? ((layer.YOffset % h) - h) : layer.YOffset;

        var endX = backgroundData.TiledHorizontally ? layer.ParentRoom.Width : (startX + w);
        var endY = backgroundData.TiledVertically ? layer.ParentRoom.Height : (startY + h);

        for (var x = startX; x < endX; x += w)
        {
            for (var y = startY; y < endY; y += h)
            {
                Canvas.Save();
                if (backgroundData.TiledHorizontally || backgroundData.TiledVertically)
                {
                    // TODO: Only clip in direction of tiling
                    Canvas.ClipRect(new SKRect(0, 0, layer.ParentRoom.Width, layer.ParentRoom.Height));
                }
                Canvas.Translate(x, y);
                Canvas.Translate(texture.TargetX, texture.TargetY);
                Canvas.Scale(backgroundData.CalcScaleX, backgroundData.CalcScaleY);
                Canvas.DrawImage(image, 0, 0, new SKPaint()
                {
                    ColorFilter = SKColorFilter.CreateBlendMode(UndertaleColor.ToColor(backgroundData.Color).ToSKColor(), SKBlendMode.Modulate),
                });
                Canvas.Restore();
            }
        }
    }

    void RenderTile(UndertaleRoom.Tile roomTile, UndertaleRoom.Layer? layer = null)
    {
        SKImage? image = mainVM.ImageCache.GetCachedImageFromTile(roomTile);
        if (image is null)
            return;

        currentUsedImages.Add(image);

        Canvas.Save();
        if (layer is not null)
            Canvas.Translate(layer.XOffset, layer.YOffset);

        Canvas.Translate(roomTile.X, roomTile.Y);
        Canvas.Translate(
            -Math.Min(roomTile.SourceX - roomTile.Tpag.TargetX, 0),
            -Math.Min(roomTile.SourceY - roomTile.Tpag.TargetY, 0));
        Canvas.Scale(roomTile.ScaleX, roomTile.ScaleY);
        Canvas.DrawImage(image, 0, 0);
        Canvas.Restore();
    }

    void RenderLayerTiles(UndertaleRoom.Layer layer)
    {
        UndertaleRoom.Layer.LayerTilesData tilesData = layer.TilesData;

        if (tilesData.Background is null)
            return;

        SKImage? image = mainVM.ImageCache.GetCachedImageFromGMImage(tilesData.Background.Texture.TexturePage.TextureData.Image);
        if (image is null)
            return;

        currentUsedImages.Add(image);

        UndertaleBackground background = tilesData.Background;
        UndertaleTexturePageItem texture = background.Texture;

        uint tileColumns = background.GMS2TileColumns;
        uint tileW = background.GMS2TileWidth;
        uint tileH = background.GMS2TileHeight;
        uint borderX = background.GMS2OutputBorderX;
        uint borderY = background.GMS2OutputBorderY;

        ushort targetX = texture.TargetX;
        ushort targetY = texture.TargetY;
        ushort sourceX = texture.SourceX;
        ushort sourceY = texture.SourceY;

        uint[][] tileData = tilesData.TileData;

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

        for (int y = 0; y < tileData.Length; y++)
            for (int x = 0; x < tileData[y].Length; x++)
            {
                uint tile = tileData[y][x];
                uint tileId = tile & UndertaleRoomViewModel.TILE_ID;

                if (tileId != 0)
                {
                    uint tileOrientation = tile >> 28;

                    float posX = (x * tileW) + targetX;
                    float posY = (y * tileH) + targetY;

                    uint tileX = tileId % tileColumns;
                    uint tileY = tileId / tileColumns;

                    float xx = sourceX + (tileX * (tileW + borderX * 2) + borderX);
                    float yy = sourceY + (tileY * (tileH + borderY * 2) + borderY);

                    AddQuad(texs, xx, yy, xx + tileW, yy + tileH, tileOrientation);
                    AddQuad(vertices, posX, posY, posX + tileW, posY + tileH, 0);
                }
            }

        SKShader shader = SKShader.CreateImage(image);

        Canvas.Save();
        Canvas.Translate(layer.XOffset, layer.YOffset);
        Canvas.DrawVertices(SKVertexMode.Triangles, vertices.ToArray(), texs.ToArray(), null, new SKPaint() { Shader = shader });
        Canvas.Restore();
    }

    void RenderSprite(UndertaleRoom.SpriteInstance roomSprite, UndertaleRoom.Layer layer)
    {
        // TODO: roomSprite.AnimationSpeed
        // TODO: roomSprite.AnimationSpeedType

        if (roomSprite.Sprite is null)
            return;
        if (!(roomSprite.FrameIndex >= 0 && roomSprite.FrameIndex < roomSprite.Sprite.Textures.Count))
            return;

        UndertaleTexturePageItem texture = roomSprite.Sprite.Textures[(int)roomSprite.FrameIndex].Texture;

        SKImage? image = mainVM.ImageCache.GetCachedImageFromTexturePageItem(texture);
        if (image is null)
            return;

        currentUsedImages.Add(image);

        Canvas.Save();
        Canvas.Translate(layer.XOffset + texture.TargetX, layer.YOffset + texture.TargetY);
        Canvas.Translate(roomSprite.X, roomSprite.Y);
        Canvas.RotateDegrees(roomSprite.OppositeRotation);
        Canvas.Scale(roomSprite.ScaleX, roomSprite.ScaleY);

        Canvas.DrawImage(image, -roomSprite.Sprite.OriginX, -roomSprite.Sprite.OriginY, new SKPaint()
        {
            ColorFilter = SKColorFilter.CreateBlendMode(UndertaleColor.ToColor(roomSprite.Color).ToSKColor(), SKBlendMode.Modulate),
        });

        Canvas.Restore();
    }

    void RenderGameObject(UndertaleRoom.GameObject roomGameObject)
    {
        // TODO: roomGameObject.ImageSpeed

        UndertaleGameObject? gameObject = roomGameObject.ObjectDefinition;
        if (gameObject is null)
            return;
        if (gameObject.Sprite is null)
            return;
        if (!(roomGameObject.ImageIndex >= 0 && roomGameObject.ImageIndex < gameObject.Sprite.Textures.Count))
            return;

        UndertaleTexturePageItem texture = gameObject.Sprite.Textures[roomGameObject.ImageIndex].Texture;

        SKImage? image = mainVM.ImageCache.GetCachedImageFromTexturePageItem(texture);
        if (image is null)
            return;

        currentUsedImages.Add(image);

        Canvas.Save();
        Canvas.Translate(roomGameObject.X, roomGameObject.Y);
        Canvas.RotateDegrees(roomGameObject.OppositeRotation);
        Canvas.Scale(roomGameObject.ScaleX, roomGameObject.ScaleY);

        Canvas.DrawImage(image, -gameObject.Sprite.OriginX + texture.TargetX, -gameObject.Sprite.OriginY + texture.TargetY, new SKPaint()
        {
            ColorFilter = SKColorFilter.CreateBlendMode(UndertaleColor.ToColor(roomGameObject.Color).ToSKColor(), SKBlendMode.Modulate),
        });

        Canvas.Restore();
    }
}