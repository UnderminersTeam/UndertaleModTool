using ImageMagick;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using UndertaleModLib.Models;

namespace UndertaleModLib.Util
{
    /// <summary>
    /// Helper class used to manage and cache textures.
    /// </summary>
    public class TextureWorker : IDisposable
    {
        private Dictionary<UndertaleEmbeddedTexture, MagickImage> embeddedDictionary = [];

        public MagickImage GetEmbeddedTexture(UndertaleEmbeddedTexture embeddedTexture)
        {
            lock (embeddedDictionary)
            {
                if (!embeddedDictionary.ContainsKey(embeddedTexture))
                {
                    embeddedDictionary[embeddedTexture] = embeddedTexture.TextureData.Image.GetMagickImage();
                }
                return embeddedDictionary[embeddedTexture];
            }
        }

        public void ExportAsPNG(UndertaleTexturePageItem texPageItem, string fullPath, string imageName = null, bool includePadding = false)
        {
            SaveImageToFile(fullPath, GetTextureFor(texPageItem, imageName ?? Path.GetFileNameWithoutExtension(fullPath), includePadding));
        }

        public IMagickImage<byte> GetTextureFor(UndertaleTexturePageItem texPageItem, string imageName, bool includePadding = false)
        {
            int exportWidth = texPageItem.BoundingWidth; // sprite.Width
            int exportHeight = texPageItem.BoundingHeight; // sprite.Height
            MagickImage embeddedImage = GetEmbeddedTexture(texPageItem.TexturePage);

            // Sanity checks.
            if (includePadding && ((texPageItem.TargetWidth > exportWidth) || (texPageItem.TargetHeight > exportHeight)))
                throw new InvalidDataException(imageName + "'s texture is larger than its bounding box!");

            // Create a bitmap representing that part of the texture page.
            IMagickImage<byte> resultImage = null;
            lock (embeddedImage)
            {
                try
                {
                    resultImage = embeddedImage.Clone(texPageItem.SourceX, texPageItem.SourceY, texPageItem.SourceWidth, texPageItem.SourceHeight);
                }
                catch (OutOfMemoryException)
                {
                    throw new OutOfMemoryException(imageName + "'s texture is abnormal. 'Source Position/Size' boxes 3 & 4 on texture page may be bigger than the sprite itself or it's set to '0'.");
                }
            }

            // Resize the image, if necessary.
            if ((texPageItem.SourceWidth != texPageItem.TargetWidth) || (texPageItem.SourceHeight != texPageItem.TargetHeight))
            {
                IMagickImage<byte> original = resultImage;
                resultImage = ResizeImage(resultImage, texPageItem.TargetWidth, texPageItem.TargetHeight);
                original.Dispose();
            }

            // Put it in the final holder image.
            IMagickImage<byte> returnImage = resultImage;
            if (includePadding)
            {
                returnImage = new MagickImage(MagickColor.FromRgba(0, 0, 0, 0), exportWidth, exportHeight);
                returnImage.Composite(resultImage, texPageItem.TargetX, texPageItem.TargetY, CompositeOperator.Copy);
            }

            return returnImage;
        }

        public static MagickImage ReadBGRAImageFromFile(string filePath)
        {
            MagickReadSettings settings = new()
            {
                ColorSpace = ColorSpace.sRGB,
            };
            MagickImage image = new(filePath, settings);
            image.Alpha(AlphaOption.Set);
            image.Format = MagickFormat.Bgra;
            image.SetCompression(CompressionMethod.NoCompression);
            return image;
        }

        // This should perform a high quality resize.
        public static IMagickImage<byte> ResizeImage(IMagickImage<byte> image, int width, int height)
        {
            if (image.Width == width && image.Height == height)
            {
                return image.Clone();
            }

            IMagickImage<byte> newImage = image.Clone();
            newImage.InterpolativeResize(width, height, PixelInterpolateMethod.Bilinear);
            return newImage;
        }

        public static byte[] ReadMaskData(string filePath, int requiredWidth, int requiredHeight)
        {
            MagickImage image = ReadBGRAImageFromFile(filePath);
            if (image.Width != requiredWidth || image.Height != requiredHeight)
            {
                throw new Exception($"{filePath} is not the proper size to be imported! The proper dimensions are width: {requiredWidth} px, height: {requiredHeight} px.");
            }

            IPixelCollection<byte> pixels = image.GetPixels();
            List<byte> bytes = [];

            IMagickColor<byte> enableColor = MagickColor.FromRgba(255, 255, 255, 255);
            for (int y = 0; y < image.Height; y++)
            {
                for (int xByte = 0; xByte < (image.Width + 7) / 8; xByte++)
                {
                    byte fullByte = 0x00;
                    int pxStart = (xByte * 8);
                    int pxEnd = Math.Min(pxStart + 8, image.Width);

                    for (int x = pxStart; x < pxEnd; x++)
                    {
                        if (pixels.GetPixel(x, y).ToColor().Equals(enableColor))
                        {
                            fullByte |= (byte)(0b1 << (7 - (x - pxStart)));
                        }
                    }

                    bytes.Add(fullByte);
                }
            }

            image.Dispose();
            return bytes.ToArray();
        }

        public static IMagickImage<byte> GetCollisionMaskImage(UndertaleSprite sprite, UndertaleSprite.MaskEntry mask)
        {
            byte[] maskData = mask.Data;
            MagickImage bitmap = new MagickImage(MagickColor.FromRgba(0, 0, 0, 255), (int)sprite.Width, (int)sprite.Height);
            IPixelCollection<byte> pixels = bitmap.GetPixels();
            ReadOnlySpan<byte> black = MagickColor.FromRgba(0, 0, 0, 255).ToByteArray().AsSpan();
            ReadOnlySpan<byte> white = MagickColor.FromRgba(255, 255, 255, 255).ToByteArray().AsSpan();

            for (int y = 0; y < sprite.Height; y++)
            {
                int rowStart = y * (int)((sprite.Width + 7) / 8);
                for (int x = 0; x < sprite.Width; x++)
                {
                    byte temp = maskData[rowStart + (x / 8)];
                    bool pixelBit = (temp & (0b1 << (7 - (x % 8)))) != 0b0;
                    pixels.SetPixel(x, y, pixelBit ? white : black);
                }
            }

            return bitmap;
        }

        public static void ExportCollisionMaskPNG(UndertaleSprite sprite, UndertaleSprite.MaskEntry mask, string fullPath)
        {
            using var image = GetCollisionMaskImage(sprite, mask);
            SaveImageToFile(fullPath, image);
        }

        public static void SaveImageToFile(string fullPath, IMagickImage<byte> image, bool disposeImage = true)
        {
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                image.Write(stream, MagickFormat.Png32);
            }
            if (disposeImage)
            {
                image.Dispose();
            }
        }

        /// <summary>
        /// This should be called when the TextureWorker is no longer going to be used; this frees image data.
        /// </summary>
        public void Dispose()
        {
            if (embeddedDictionary is not null)
            {
                foreach (MagickImage img in embeddedDictionary.Values)
                {
                    img.Dispose();
                }
                embeddedDictionary.Clear();
                embeddedDictionary = null;
            }

            GC.SuppressFinalize(this);
        }
    }
}
