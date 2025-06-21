using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SkiaSharp;
using UndertaleModLib.Models;
using UndertaleModLib.Util;

namespace UndertaleModToolAvalonia;

public class ImageCache
{
    abstract record ImageKey();
    record GMImageImageKey(GMImage GMImage) : ImageKey;
    record TexturePageItemImageKey(GMImage GMImage, ushort SourceX, ushort SourceY,
        ushort SourceWidth, ushort SourceHeight) : ImageKey;
    record TileImageKey(GMImage GMImage, ushort SourceX, ushort SourceY, ushort TargetX, ushort TargetY,
        int TileSourceX, int TileSourceY, uint Width, uint Height) : ImageKey;
    record LayerTileImageKey(GMImage GMImage, ushort SourceX, ushort SourceY, uint TileId,
        uint TileColumns, uint TileWidth, uint TileHeight, uint TileBorderX, uint TileBorderY) : ImageKey;

    readonly Dictionary<ImageKey, WeakReference<SKImage>> imageCache = [];

    public SKImage GetImageFromGMImage(GMImage gmImage)
    {
        // Faster shortcut
        if (gmImage.Format == GMImage.ImageFormat.Png)
        {
            return SKImage.FromEncodedData(gmImage.GetData());
        }

        byte[] data = gmImage.ConvertToRawBgra().GetData();
        GCHandle gcHandle = GCHandle.Alloc(data, GCHandleType.Pinned);

        SKBitmap bitmap = new();

        SKImageInfo info = new(gmImage.Width, gmImage.Height, SKColorType.Bgra8888, SKAlphaType.Unpremul);
        SKPixmap pixmap = new(info, gcHandle.AddrOfPinnedObject(), info.RowBytes);
        SKImage? image = SKImage.FromPixels(pixmap, delegate
        { gcHandle.Free(); });

        if (image is null)
        {
            gcHandle.Free();
            throw new Exception("Could not create image");
        }

        return image;
    }

    public SKImage GetCachedImageFromGMImage(GMImage gmImage)
    {
        GMImageImageKey key = new(gmImage);

        SKImage? image = null;
        if (imageCache.TryGetValue(key, out var reference))
            reference.TryGetTarget(out image);

        if (image is null)
        {
            image = GetImageFromGMImage(gmImage);
            imageCache[key] = new WeakReference<SKImage>(image);
        }

        return image;
    }

    public SKImage GetCachedImageFromTexturePageItem(UndertaleTexturePageItem texturePageItem)
    {
        TexturePageItemImageKey key = new(
            texturePageItem.TexturePage.TextureData.Image,
            texturePageItem.SourceX,
            texturePageItem.SourceY,
            texturePageItem.SourceWidth,
            texturePageItem.SourceHeight);

        SKImage? image = null;
        if (imageCache.TryGetValue(key, out var reference))
            reference.TryGetTarget(out image);

        if (image is null)
        {
            image = GetCachedImageFromGMImage(texturePageItem.TexturePage.TextureData.Image)
                .Subset(SKRectI.Create(
                    texturePageItem.SourceX,
                    texturePageItem.SourceY,
                    texturePageItem.SourceWidth,
                    texturePageItem.SourceHeight));

            imageCache[key] = new WeakReference<SKImage>(image);
        }

        return image;
    }

    public SKImage GetCachedImageFromTile(UndertaleRoom.Tile tile)
    {
        TileImageKey key = new(
            tile.Tpag.TexturePage.TextureData.Image,
            tile.Tpag.SourceX,
            tile.Tpag.SourceY,
            tile.Tpag.TargetX,
            tile.Tpag.TargetY,
            tile.SourceX,
            tile.SourceY,
            tile.Width,
            tile.Height);

        SKImage? image = null;
        if (imageCache.TryGetValue(key, out var reference))
            reference.TryGetTarget(out image);

        if (image is null)
        {
            // Assuming source and target are in the same scale.
            image = GetCachedImageFromGMImage(tile.Tpag.TexturePage.TextureData.Image)
                .Subset(SKRectI.Create(
                    tile.Tpag.SourceX + tile.SourceX - tile.Tpag.TargetX,
                    tile.Tpag.SourceY + tile.SourceY - tile.Tpag.TargetY,
                    (int)tile.Width,
                    (int)tile.Height));

            imageCache[key] = new WeakReference<SKImage>(image);
        }

        return image;
    }

    public SKImage GetCachedImageFromLayerTile(UndertaleRoom.Layer.LayerTilesData tilesData, uint tileId)
    {
        LayerTileImageKey key = new(
            tilesData.Background.Texture.TexturePage.TextureData.Image,
            tilesData.Background.Texture.SourceX,
            tilesData.Background.Texture.SourceY,
            //texturePageItem.SourceWidth,
            //texturePageItem.SourceHeight,
            tileId,
            tilesData.Background.GMS2TileColumns,
            tilesData.Background.GMS2TileWidth,
            tilesData.Background.GMS2TileHeight,
            tilesData.Background.GMS2OutputBorderX,
            tilesData.Background.GMS2OutputBorderY
        );

        SKImage? image = null;
        if (imageCache.TryGetValue(key, out var reference))
            reference.TryGetTarget(out image);

        if (image is null)
        {
            uint tileX = tileId % tilesData.Background.GMS2TileColumns;
            uint tileY = tileId / tilesData.Background.GMS2TileColumns;

            uint x = tilesData.Background.Texture.SourceX;
            uint y = tilesData.Background.Texture.SourceY;

            x += tileX * (tilesData.Background.GMS2TileWidth + tilesData.Background.GMS2OutputBorderX * 2) + tilesData.Background.GMS2OutputBorderX;
            y += tileY * (tilesData.Background.GMS2TileHeight + tilesData.Background.GMS2OutputBorderY * 2) + tilesData.Background.GMS2OutputBorderY;

            image = GetCachedImageFromGMImage(tilesData.Background.Texture.TexturePage.TextureData.Image)
                .Subset(SKRectI.Create(
                    (int)x,
                    (int)y,
                    (int)tilesData.Background.GMS2TileWidth,
                    (int)tilesData.Background.GMS2TileHeight));

            imageCache[key] = new WeakReference<SKImage>(image);
        }

        return image;
    }
}