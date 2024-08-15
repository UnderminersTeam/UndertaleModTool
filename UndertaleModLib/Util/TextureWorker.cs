using ImageMagick;
using System;
using System.Collections.Generic;
using System.IO;
using UndertaleModLib.Models;

namespace UndertaleModLib.Util
{
    /// <summary>
    /// Helper class used to manage and cache textures.
    /// </summary>
    public class TextureWorker : IDisposable
    {
        private Dictionary<UndertaleEmbeddedTexture, MagickImage> embeddedDictionary = new();
        private readonly object embeddedDictionaryLock = new();

        /// <summary>
        /// Retrieves an image representing the supplied texture page.
        /// </summary>
        /// <remarks>
        /// The returned image will be cached for this <see cref="TextureWorker"/> instance.
        /// </remarks>
        public MagickImage GetEmbeddedTexture(UndertaleEmbeddedTexture embeddedTexture)
        {
            lock (embeddedDictionaryLock)
            {
                // Try to find cached image
                if (embeddedDictionary.TryGetValue(embeddedTexture, out MagickImage image))
                {
                    return image;
                }

                // Otherwise, create new image
                MagickImage newImage = embeddedTexture.TextureData.Image.GetMagickImage();
                embeddedDictionary[embeddedTexture] = newImage;

                return newImage;
            }
        }

        /// <summary>
        /// Exports the given texture page item to disk, as a PNG, to the supplied path. (With or without padding.)
        /// </summary>
        public void ExportAsPNG(UndertaleTexturePageItem texPageItem, string fullPath, string imageName = null, bool includePadding = false)
        {
            using var image = GetTextureFor(texPageItem, imageName ?? Path.GetFileNameWithoutExtension(fullPath), includePadding);
            SaveImageToFile(image, fullPath);
        }

        /// <summary>
        /// Creates an image representing the sole texture page item supplied, with or without padding.
        /// </summary>
        public IMagickImage<byte> GetTextureFor(UndertaleTexturePageItem texPageItem, string imageName, bool includePadding = false)
        {
            // Get texture page that the item lives on
            MagickImage embeddedImage = GetEmbeddedTexture(texPageItem.TexturePage);

            // Ensure texture is no larger than its bounding box
            int exportWidth = texPageItem.BoundingWidth; // sprite.Width
            int exportHeight = texPageItem.BoundingHeight; // sprite.Height
            if (includePadding && (texPageItem.TargetWidth > exportWidth || texPageItem.TargetHeight > exportHeight))
            {
                throw new InvalidDataException($"{imageName}'s texture is larger than its bounding box!");
            }

            // Create an image cropped from the item's part of the texture page
            IMagickImage<byte> resultImage = null;
            lock (embeddedImage)
            {
                try
                {
                    resultImage = embeddedImage.Clone(texPageItem.SourceX, texPageItem.SourceY, texPageItem.SourceWidth, texPageItem.SourceHeight);
                }
                catch (OutOfMemoryException)
                {
                    throw new OutOfMemoryException($"{imageName}'s entry is abnormal. 'Source Position/Size' boxes on the texture page item may be set incorrectly.");
                }
            }

            // Resize the image, if necessar
            if (texPageItem.SourceWidth != texPageItem.TargetWidth || texPageItem.SourceHeight != texPageItem.TargetHeight)
            {
                IMagickImage<byte> original = resultImage;
                resultImage = ResizeImage(resultImage, texPageItem.TargetWidth, texPageItem.TargetHeight);
                original.Dispose();
            }

            // Put it in the final holder image, if necessary
            IMagickImage<byte> returnImage = resultImage;
            if (includePadding)
            {
                returnImage = new MagickImage(MagickColor.FromRgba(0, 0, 0, 0), exportWidth, exportHeight);
                returnImage.Composite(resultImage, texPageItem.TargetX, texPageItem.TargetY, CompositeOperator.Copy);
                resultImage.Dispose();
            }

            return returnImage;
        }

        /// <summary>
        /// Reads an image from the given file path (of arbitrary format, as supported by <see cref="MagickImage(string, IMagickReadSettings{byte})"/>).
        /// </summary>
        /// <remarks>
        /// Image color format will always be converted to BGRA, with no compression.
        /// </remarks>
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

        /// <summary>
        /// Performs a resize of the given image, if required, using bilinear interpolation. Always returns a new image.
        /// </summary>
        public static IMagickImage<byte> ResizeImage(IMagickImage<byte> image, int width, int height)
        {
            if (image.Width == width && image.Height == height)
            {
                // We already have the correct dimensions, so just make a clone of the image.
                // If we don't clone here, we have potential ownership/disposal issues.
                return image.Clone();
            }

            // Clone and resize using bilinear interpolation
            IMagickImage<byte> newImage = image.Clone();
            newImage.InterpolativeResize(width, height, PixelInterpolateMethod.Bilinear);
            return newImage;
        }

        /// <summary>
        /// Reads collision mask data from the given file path, and required width/height.
        /// </summary>
        /// <exception cref="Exception">If the loaded image dimensions do not match the required width/height</exception>
        public static byte[] ReadMaskData(string filePath, int requiredWidth, int requiredHeight)
        {
            List<byte> bytes;
            using (MagickImage image = ReadBGRAImageFromFile(filePath))
            {
                // Verify width/height match required width/height
                if (image.Width != requiredWidth || image.Height != requiredHeight)
                {
                    throw new Exception($"{filePath} is not the proper size to be imported! The proper dimensions are width: {requiredWidth} px, height: {requiredHeight} px.");
                }

                // Get image pixels, and allocate enough capacity for mask
                IPixelCollection<byte> pixels = image.GetPixels();
                bytes = new((requiredWidth + 7) / 8 * requiredHeight);

                // Get white color, used to represent bits that are set
                IMagickColor<byte> white = MagickColor.FromRgba(255, 255, 255, 255);

                // Read all pixels of image, and set a bit on the mask if a given pixel matches the white color
                for (int y = 0; y < image.Height; y++)
                {
                    for (int xByte = 0; xByte < (image.Width + 7) / 8; xByte++)
                    {
                        byte fullByte = 0x00;
                        int pxStart = (xByte * 8);
                        int pxEnd = Math.Min(pxStart + 8, image.Width);

                        for (int x = pxStart; x < pxEnd; x++)
                        {
                            if (pixels.GetPixel(x, y).ToColor().Equals(white))
                            {
                                fullByte |= (byte)(0b1 << (7 - (x - pxStart)));
                            }
                        }

                        bytes.Add(fullByte);
                    }
                }
            }

            return bytes.ToArray();
        }

        /// <summary>
        /// Generates and returns a black-and-white image representing the given sprite's specified collision mask,
        /// and with the given width/height.
        /// </summary>
        public static IMagickImage<byte> GetCollisionMaskImage(UndertaleSprite sprite, UndertaleSprite.MaskEntry mask, int maskWidth, int maskHeight)
        {
            // Create image to draw on
            MagickImage bitmap = new(MagickColor.FromRgba(0, 0, 0, 255), maskWidth, maskHeight);
            IPixelCollection<byte> pixels = bitmap.GetPixels();

            // Get black/white colors to use for drawing
            ReadOnlySpan<byte> black = MagickColor.FromRgba(0, 0, 0, 255).ToByteArray().AsSpan();
            ReadOnlySpan<byte> white = MagickColor.FromRgba(255, 255, 255, 255).ToByteArray().AsSpan();

            // Draw white pixels if a given bit is set; black pixels otherwise
            byte[] maskData = mask.Data;
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

        /// <summary>
        /// Exports a collision mask entry from the given sprite, as a PNG file, at the specified path, and with the given width/height.
        /// </summary>
        public static void ExportCollisionMaskPNG(UndertaleSprite sprite, UndertaleSprite.MaskEntry mask, string path, int maskWidth, int maskHeight)
        {
            using var image = GetCollisionMaskImage(sprite, mask, maskWidth, maskHeight);
            SaveImageToFile(image, path);
        }

        /// <summary>
        /// Saves the provided image as a PNG file, at the specified path.
        /// </summary>
        public static void SaveImageToFile(IMagickImage<byte> image, string path)
        {
            using var stream = new FileStream(path, FileMode.Create);
            image.Write(stream, MagickFormat.Png32);
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
