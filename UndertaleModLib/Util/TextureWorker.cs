using System;
using System.Collections.Generic;
using System.IO;
using SkiaSharp;
using UndertaleModLib.Models;

namespace UndertaleModLib.Util
{
    public class TextureWorker
    {
        private readonly Dictionary<UndertaleEmbeddedTexture, SKBitmap> embeddedDictionary = new();
        private static readonly SKPaint paintBlack = new()
        {
            Color = SKColors.Black,
            BlendMode = SKBlendMode.Src
        };
        private static readonly SKPaint paintWhite = new()
        {
            Color = SKColors.White,
            BlendMode = SKBlendMode.Src
        };

        // Cleans up all the images when usage of this worker is finished.
        // Should be called when a TextureWorker will never be used again.
        public void Cleanup()
        {
            foreach (SKBitmap img in embeddedDictionary.Values)
                img.Dispose();
            embeddedDictionary.Clear();
        }

        public SKBitmap GetEmbeddedTexture(UndertaleEmbeddedTexture embeddedTexture)
        {
            lock (embeddedDictionary)
            {
                if (!embeddedDictionary.ContainsKey(embeddedTexture))
                    embeddedDictionary[embeddedTexture] = GetImageFromByteArray(embeddedTexture.TextureData.TextureBlob);
                return embeddedDictionary[embeddedTexture];
            }
        }

        public void ExportAsPNG(UndertaleTexturePageItem texPageItem, string fullPath, string imageName = null, bool includePadding = false)
        {
            SaveImageToFile(fullPath, GetTextureFor(texPageItem, imageName ?? Path.GetFileNameWithoutExtension(fullPath), includePadding));
        }

        public SKBitmap GetTextureFor(UndertaleTexturePageItem texPageItem, string imageName, bool includePadding = false)
        {
            int exportWidth = texPageItem.BoundingWidth; // sprite.Width
            int exportHeight = texPageItem.BoundingHeight; // sprite.Height
            SKBitmap embeddedImage = GetEmbeddedTexture(texPageItem.TexturePage);

            // Sanity checks.
            if (includePadding && ((texPageItem.TargetWidth > exportWidth) || (texPageItem.TargetHeight > exportHeight)))
                throw new InvalidDataException(imageName + "'s texture is larger than its bounding box!");

            // Create a bitmap representing that part of the texture page.
            SKBitmap resultImage = new SKBitmap();

            lock (embeddedImage)
            {
                try
                {
                    var sourceRect = SKRectI.Create(texPageItem.SourceX, texPageItem.SourceY, texPageItem.SourceWidth, texPageItem.SourceHeight);
                    embeddedImage.ExtractSubset(resultImage, sourceRect);
                    resultImage = resultImage.Copy();
                }
                catch (OutOfMemoryException)
                {
                    throw new OutOfMemoryException(imageName + "'s texture is abnormal. 'Source Position/Size' boxes 3 & 4 on texture page may be bigger than the sprite itself or it's set to '0'.");
                }
            }

            // Resize the image, if necessary.
            if ((texPageItem.SourceWidth != texPageItem.TargetWidth) || (texPageItem.SourceHeight != texPageItem.TargetHeight))
                resultImage = ResizeImage(resultImage, texPageItem.TargetWidth, texPageItem.TargetHeight);

            // Put it in the final holder image.
            SKBitmap returnImage = resultImage;
            if (includePadding)
            {
                returnImage = new SKBitmap(exportWidth, exportHeight);
                using SKCanvas g = new(returnImage);
                g.DrawBitmap(resultImage, SKRect.Create(0, 0, resultImage.Width, resultImage.Height),
                             SKRect.Create(texPageItem.TargetX, texPageItem.TargetY, resultImage.Width, resultImage.Height));
            }

            return returnImage;
        }

        public static SKBitmap ReadImageFromFile(string filePath)
        {
            return GetImageFromByteArray(File.ReadAllBytes(filePath));
        }

        public static SKBitmap GetImageFromByteArray(byte[] byteArray)
        {
            SKBitmap bm = SKBitmap.Decode(byteArray);
            return bm;
        }

        public static SKSizeI GetImageSizeFromFile(string filePath)
        {
            using SKCodec codec = SKCodec.Create(filePath);

            return codec?.Info.Size ?? default;
        }
        public static SKSizeI GetImageSizeFromByteArray(byte[] byteArray)
        {
            using MemoryStream stream = new(byteArray);
            using SKCodec codec = SKCodec.Create(stream);

            return codec?.Info.Size ?? default;
        }

        public static SKBitmap ResizeImage(SKBitmap image, int width, int height, bool useNearestNeighbor = false)
        {
            var destImage = new SKBitmap(width, height);
            image.ScalePixels(destImage, useNearestNeighbor ? SKFilterQuality.None : SKFilterQuality.High);
            return destImage;
        }

        public static byte[] ReadMaskData(string filePath)
        {
            using SKBitmap image = ReadImageFromFile(filePath);
            List<byte> bytes = new();

            for (int y = 0; y < image.Height; y++)
            {
                for (int xByte = 0; xByte < (image.Width + 7) / 8; xByte++)
                {
                    byte fullByte = 0x00;
                    int pxStart = xByte * 8;
                    int pxEnd = Math.Min(pxStart + 8, image.Width);

                    for (int x = pxStart; x < pxEnd; x++)
                        if (image.GetPixel(x, y) == SKColors.White)
                            fullByte |= (byte)(0b1 << (7 - (x - pxStart)));

                    bytes.Add(fullByte);
                }
            }

            return bytes.ToArray();
        }

        public static byte[] ReadTextureBlob(string filePath)
        {
            SKBitmap.Decode(filePath).Dispose(); // Make sure the file is valid image.
            return File.ReadAllBytes(filePath);
        }

        public static void SaveEmptyPNG(string fullPath, int width, int height)
        {
            var blackImage = new SKBitmap(width, height);
            using SKCanvas g = new(blackImage);
            g.Clear(SKColors.Black);
            SaveImageToFile(fullPath, blackImage);
        }

        public static SKBitmap GetCollisionMaskImage(UndertaleSprite sprite, UndertaleSprite.MaskEntry mask)
        {
            byte[] maskData = mask.Data;
            SKBitmap bitmap = new((int)sprite.Width, (int)sprite.Height, SKColorType.Gray8, SKAlphaType.Premul);
            using SKCanvas g = new(bitmap);

            for (int y = 0; y < sprite.Height; y++)
            {
                int rowStart = y * (int)((sprite.Width + 7) / 8);
                for (int x = 0; x < sprite.Width; x++)
                {
                    byte temp = maskData[rowStart + (x / 8)];
                    bool pixelBit = (temp & (0b1 << (7 - (x % 8)))) != 0b0;
                    g.DrawPoint(x, y, pixelBit ? paintWhite : paintBlack);
                }
            }

            return bitmap;
        }

        public static void ExportCollisionMaskPNG(UndertaleSprite sprite, UndertaleSprite.MaskEntry mask, string fullPath)
        {
            SaveImageToFile(fullPath, GetCollisionMaskImage(sprite, mask));
        }

        public static byte[] GetImageBytes(SKBitmap image, bool disposeImage = true)
        {
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            byte[] result = data.ToArray();
            if (disposeImage)
                image.Dispose();
            return result;
        }

        public static void SaveImageToFile(string fullPath, SKBitmap image, bool disposeImage = true)
        {
            using var stream = new FileStream(fullPath, FileMode.Create);
            image.Encode(stream, SKEncodedImageFormat.Png, 100);
            stream.Close();
            if (disposeImage)
                image.Dispose();
        }
    }
}
