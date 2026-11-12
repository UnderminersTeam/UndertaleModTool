using System;
using System.Collections.Generic;
using ImageMagick;
using SkiaSharp;
using UndertaleModLib.Models;
using UndertaleModLib.Util;

namespace UndertaleModToolAvalonia;

public class ImageCache
{
    interface ImageKey;
    readonly record struct GMImageImageKey(GMImage GMImage) : ImageKey;
    readonly record struct TexturePageItemImageKey(GMImage GMImage, ushort SourceX, ushort SourceY, ushort SourceWidth, ushort SourceHeight,
        ushort TargetX, ushort TargetY, ushort TargetWidth, ushort TargetHeight,
        ushort BoundingWidth, ushort BoundingHeight) : ImageKey;
    readonly record struct TileImageKey(TexturePageItemImageKey TexturePageItemImageKey, int TileSourceX, int TileSourceY, uint Width, uint Height) : ImageKey;
    readonly record struct LayerTileImageKey(GMImage GMImage, ushort SourceX, ushort SourceY, uint TileId,
        uint TileColumns, uint TileWidth, uint TileHeight, uint TileBorderX, uint TileBorderY) : ImageKey;

    readonly Dictionary<ImageKey, WeakReference<SKImage>> imageCache = [];

    public static SKImage GetImageFromByteArray(byte[] data, int width, int height)
    {
        SKImageInfo info = new(width, height, SKColorType.Bgra8888, SKAlphaType.Unpremul);

        SKImage? image = SKImage.FromPixelCopy(info, data);

        if (image is null)
        {
            throw new InvalidOperationException("Could not create image");
        }

        return image;
    }

    public static SKImage GetImageFromMagickImage(IMagickImage magickImage)
    {
        magickImage.Alpha(AlphaOption.Set);
        magickImage.Format = MagickFormat.Bgra;
        magickImage.Depth = 8;
        magickImage.SetCompression(CompressionMethod.NoCompression);
        byte[] data = magickImage.ToByteArray();

        return GetImageFromByteArray(data, (int)magickImage.Width, (int)magickImage.Height);
    }

    public SKImage GetImageFromGMImage(GMImage gmImage)
    {
        byte[] data = gmImage.ConvertToRawBgra().GetData();

        return GetImageFromByteArray(data, gmImage.Width, gmImage.Height);
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

    public SKImage? GetCachedImageFromTexturePageItem(UndertaleTexturePageItem texturePageItem)
    {
        if (texturePageItem.TexturePage is null
            || texturePageItem.TexturePage.TextureData is null
            || texturePageItem.TexturePage.TextureData.Image is null
            || texturePageItem.SourceWidth == 0
            || texturePageItem.SourceHeight == 0
            || texturePageItem.BoundingWidth == 0
            || texturePageItem.BoundingHeight == 0)
            return null;

        TexturePageItemImageKey key = new(
            texturePageItem.TexturePage.TextureData.Image,
            texturePageItem.SourceX,
            texturePageItem.SourceY,
            texturePageItem.SourceWidth,
            texturePageItem.SourceHeight,
            texturePageItem.TargetX,
            texturePageItem.TargetY,
            texturePageItem.TargetWidth,
            texturePageItem.TargetHeight,
            texturePageItem.BoundingWidth,
            texturePageItem.BoundingHeight);

        SKImage? image = null;
        if (imageCache.TryGetValue(key, out var reference))
            if (reference.TryGetTarget(out image))
                return image;

        using (var surface = SKSurface.Create(new SKImageInfo(texturePageItem.BoundingWidth, texturePageItem.BoundingHeight, SKColorType.Bgra8888, SKAlphaType.Unpremul)))
        {
            image = GetCachedImageFromGMImage(texturePageItem.TexturePage.TextureData.Image);

            surface.Canvas.DrawImage(image,
                SKRect.Create(
                    texturePageItem.SourceX,
                    texturePageItem.SourceY,
                    texturePageItem.SourceWidth,
                    texturePageItem.SourceHeight),
                SKRect.Create(
                    texturePageItem.TargetX,
                    texturePageItem.TargetY,
                    texturePageItem.TargetWidth,
                    texturePageItem.TargetHeight),
                SKSamplingOptions.Default);

            image = surface.Snapshot();
        }

        if (image is null)
            return null;

        imageCache[key] = new WeakReference<SKImage>(image);

        return image;
    }

    public SKImage? GetCachedImageFromTile(UndertaleRoom.Tile tile)
    {
        if (tile.Tpag is null
            || tile.Tpag.TexturePage is null
            || tile.Width == 0
            || tile.Height == 0
            || tile.SourceX < 0
            || tile.SourceY < 0
            || tile.SourceX + tile.Width > tile.Tpag.BoundingWidth
            || tile.SourceY + tile.Height > tile.Tpag.BoundingHeight)
            return null;

        TileImageKey key = new(
            new TexturePageItemImageKey(
                tile.Tpag.TexturePage.TextureData.Image,
                tile.Tpag.SourceX,
                tile.Tpag.SourceY,
                tile.Tpag.SourceWidth,
                tile.Tpag.SourceHeight,
                tile.Tpag.TargetX,
                tile.Tpag.TargetY,
                tile.Tpag.TargetWidth,
                tile.Tpag.TargetHeight,
                tile.Tpag.BoundingWidth,
                tile.Tpag.BoundingHeight),
            tile.SourceX,
            tile.SourceY,
            tile.Width,
            tile.Height);

        SKImage? image = null;
        if (imageCache.TryGetValue(key, out var reference))
            if (reference.TryGetTarget(out image))
                return image;

        var texturePageItemImage = GetCachedImageFromTexturePageItem(tile.Tpag);

        if (texturePageItemImage is null)
            return null;

        image = texturePageItemImage
            .Subset(SKRectI.Create(tile.SourceX, tile.SourceY, (int)tile.Width, (int)tile.Height));

        if (image is not null)
            imageCache[key] = new WeakReference<SKImage>(image);

        return image;
    }

    public void Clear()
    {
        foreach (var weakReference in imageCache.Values)
        {
            if (weakReference.TryGetTarget(out SKImage? image))
            {
                image?.Dispose();
            }
        }
        imageCache.Clear();
    }
}