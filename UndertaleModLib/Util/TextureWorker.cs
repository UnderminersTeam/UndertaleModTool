using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using SkiaSharp;
using UndertaleModLib.Models;

namespace UndertaleModLib.Util
{
    public class TextureWorker
    {
        private readonly Dictionary<UndertaleEmbeddedTexture, SKBitmap> embeddedDictionary = new();

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
            using SKBitmap embeddedImage = GetEmbeddedTexture(texPageItem.TexturePage);

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

        // This should perform a high quality resize.
        public static SKBitmap ResizeImage(SKBitmap image, int width, int height)
        {
            var destImage = new SKBitmap(width, height);
            image.ScalePixels(destImage, SKFilterQuality.High);
            return destImage;
        }

        public static byte[] ReadMaskData(string filePath)
        {
            using SKBitmap image = ReadImageFromFile(filePath);
            List<byte> bytes = new();

            int enableColor = Color.White.ToArgb();
            for (int y = 0; y < image.Height; y++)
            {
                for (int xByte = 0; xByte < (image.Width + 7) / 8; xByte++)
                {
                    byte fullByte = 0x00;
                    int pxStart = xByte * 8;
                    int pxEnd = Math.Min(pxStart + 8, image.Width);

                    for (int x = pxStart; x < pxEnd; x++)
                        if ((uint)image.GetPixel(x, y) == enableColor) // Don't use Color == OtherColor, it doesn't seem to give us the type of equals comparison we want here.
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
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    blackImage.SetPixel(x, y, SKColors.Black);
            SaveImageToFile(fullPath, blackImage);
        }

        public static SKBitmap GetCollisionMaskImage(UndertaleSprite sprite, UndertaleSprite.MaskEntry mask)
        {
            byte[] maskData = mask.Data;
            SKBitmap bitmap = new((int)sprite.Width, (int)sprite.Height); // Ugh. I want to use 1bpp, but for some BS reason C# doesn't allow SetPixel in that mode.

            for (int y = 0; y < sprite.Height; y++)
            {
                int rowStart = y * (int)((sprite.Width + 7) / 8);
                for (int x = 0; x < sprite.Width; x++)
                {
                    byte temp = maskData[rowStart + (x / 8)];
                    bool pixelBit = (temp & (0b1 << (7 - (x % 8)))) != 0b0;
                    bitmap.SetPixel(x, y, pixelBit ? SKColors.White : SKColors.Black);
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
