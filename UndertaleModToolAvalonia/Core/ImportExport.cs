using System;
using System.IO;
using System.Threading.Tasks;
using SkiaSharp;
using UndertaleModLib.Models;
using UndertaleModLib.Util;

namespace UndertaleModToolAvalonia;

public static class ImportExport
{
    public static async Task ImportEmbeddedAudio(UndertaleEmbeddedAudio embeddedAudio, Stream stream)
    {
        byte[] bytes = new byte[stream.Length];
        await stream.ReadExactlyAsync(bytes);

        embeddedAudio.Data = bytes;
    }

    public static async Task ExportEmbeddedAudio(UndertaleEmbeddedAudio embeddedAudio, Stream stream)
    {
        await stream.WriteAsync(embeddedAudio.Data);
    }

    public static async Task ImportEmbeddedTexture(UndertaleEmbeddedTexture embeddedTexture, Stream stream)
    {
        byte[] bytes = new byte[stream.Length];
        await stream.ReadExactlyAsync(bytes);

        GMImage gmImage = GMImage.FromPng(bytes, verifyHeader: true);
        gmImage.ConvertToFormat(embeddedTexture.TextureData.Image.Format);

        embeddedTexture.TextureData.Image = gmImage;
        embeddedTexture.TextureWidth = gmImage.Width;
        embeddedTexture.TextureHeight = gmImage.Height;
    }

    public static async Task ExportEmbeddedTexture(UndertaleEmbeddedTexture embeddedTexture, Stream stream)
    {
        await stream.WriteAsync(embeddedTexture.TextureData.Image.GetData());
    }

    public static async Task ExportEmbeddedTextureAsPNG(UndertaleEmbeddedTexture embeddedTexture, Stream stream)
    {
        embeddedTexture.TextureData.Image.SavePng(stream);
    }

    public static async Task ExportRoomAsPNG(UndertaleRoom room, Stream stream)
    {
        // NOTE: This is a CPU bitmap, unlike the GPU surface used when rendering in the UI.
        SKBitmap bitmap = new((int)room.Width, (int)room.Height, SKColorType.Bgra8888, SKAlphaType.Unpremul);
        SKCanvas canvas = new(bitmap);

        RoomRenderer renderer = new();
        renderer.RenderCommands(new RoomRenderer.RenderCommandsBuilder(room).RenderCommands, canvas);

        bool result = bitmap.Encode(stream, SKEncodedImageFormat.Png, 100);
        if (!result)
            throw new InvalidOperationException();
    }

    public static async Task ImportSpriteCollisionMaskData(UndertaleSprite sprite, int collisionMaskIndex, Stream stream, MainViewModel mainVM)
    {
        byte[] bytes = new byte[stream.Length];
        await stream.ReadExactlyAsync(bytes);

        (int width, int height) = sprite.CalculateMaskDimensions(mainVM.Data);
        UndertaleSprite.MaskEntry maskEntry = new(bytes, width, height);

        sprite.CollisionMasks[collisionMaskIndex] = maskEntry;
    }

    public static async Task ExportSpriteCollisionMaskData(UndertaleSprite sprite, int collisionMaskIndex, Stream stream)
    {
        await stream.WriteAsync(sprite.CollisionMasks[collisionMaskIndex].Data);
    }

    public static async Task ExportTexturePageItemAsPNG(UndertaleTexturePageItem texturePageItem, Stream stream, MainViewModel mainVM)
    {
        SKBitmap bitmap = new(texturePageItem.BoundingWidth, texturePageItem.BoundingHeight, SKColorType.Bgra8888, SKAlphaType.Unpremul);
        SKCanvas canvas = new(bitmap);

        SKImage? image = mainVM.ImageCache.GetCachedImageFromTexturePageItem(texturePageItem);

        if (image is null)
            throw new InvalidOperationException();

        // TODO: TargetWidth/TargetHeight
        canvas.DrawImage(image, texturePageItem.TargetX, texturePageItem.TargetY);

        bool result = bitmap.Encode(stream, SKEncodedImageFormat.Png, 100);
        if (!result)
            throw new InvalidOperationException();
    }
}
