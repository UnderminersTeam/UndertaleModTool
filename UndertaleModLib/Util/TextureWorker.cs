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
        /// <param name="embeddedTexture">Texture to get an image from.</param>
        /// <returns><see cref="MagickImage"/> with the contents of the given texture's image.</returns>
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
        /// <param name="texPageItem">Texture page item to export.</param>
        /// <param name="filePath">File path to export to.</param>
        /// <param name="imageName">Image name to be used when throwing exceptions, or null to use the filename from the path.</param>
        /// <param name="includePadding">True if padding should be exported; false otherwise.</param>
        public void ExportAsPNG(UndertaleTexturePageItem texPageItem, string filePath, string imageName = null, bool includePadding = false)
        {
            using var image = GetTextureFor(texPageItem, imageName ?? Path.GetFileNameWithoutExtension(filePath), includePadding);
            SaveImageToFile(image, filePath);
        }

        /// <summary>
        /// Creates an image representing the sole texture page item supplied, with or without padding.
        /// </summary>
        /// <param name="texPageItem">Texture page item to get the image of.</param>
        /// <param name="imageName">Image name to be used when throwing exceptions.</param>
        /// <param name="includePadding">True if padding should be used in the returned image; false otherwise.</param>
        /// <returns>An image with the contents of the given texture page item's portion of its texture page.</returns>
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
            IMagickImage<byte> croppedImage = null;
            lock (embeddedImage)
            {
                croppedImage = embeddedImage.Clone(texPageItem.SourceX, texPageItem.SourceY, texPageItem.SourceWidth, texPageItem.SourceHeight);
            }

            // Resize the image, if necessary
            if (texPageItem.SourceWidth != texPageItem.TargetWidth || texPageItem.SourceHeight != texPageItem.TargetHeight)
            {
                IMagickImage<byte> original = croppedImage;
                croppedImage = ResizeImage(croppedImage, texPageItem.TargetWidth, texPageItem.TargetHeight);
                original.Dispose();
            }

            // Put it in the final holder image, if necessary
            IMagickImage<byte> returnImage = croppedImage;
            if (includePadding)
            {
                returnImage = new MagickImage(MagickColor.FromRgba(0, 0, 0, 0), exportWidth, exportHeight);
                returnImage.Composite(croppedImage, texPageItem.TargetX, texPageItem.TargetY, CompositeOperator.Copy);
                croppedImage.Dispose();
            }

            return returnImage;
        }

        /// <summary>
        /// Reads an image from the given file path (of arbitrary format, as supported by <see cref="MagickImage(string, IMagickReadSettings{byte})"/>).
        /// </summary>
        /// <remarks>
        /// Image color format will always be converted to BGRA, with no compression.
        /// </remarks>
        /// <param name="filePath">File path to read the image from.</param>
        /// <returns>An image, in uncompressed BGRA format, containing the contents of the image file at the given path.</returns>
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
        /// <param name="image">Image to be resized (without being modified).</param>
        /// <param name="width">Desired width to resize to.</param>
        /// <param name="height">Desired height to resize to.</param>
        /// <returns>A copy of the provided image, which is resized to the given dimensions when required.</returns>
        public static IMagickImage<byte> ResizeImage(IMagickImage<byte> image, int width, int height)
        {
            // Clone image
            IMagickImage<byte> newImage = image.Clone();

            // If the image already has the correct dimensions, skip resizing
            if (image.Width == width && image.Height == height)
            {
                return newImage;
            }

            // Resize using bilinear interpolation
            newImage.InterpolativeResize(width, height, PixelInterpolateMethod.Bilinear);
            return newImage;
        }

        /// <summary>
        /// Reads collision mask data from the given file path, and required width/height.
        /// </summary>
        /// <param name="filePath">Image file to read the mask data from (usually a black-and-white PNG).</param>
        /// <param name="requiredWidth">The width that the collision mask must be (e.g., sprite width or bbox width, depending on version).</param>
        /// <param name="requiredHeight">The height that the collision mask must be (e.g., sprite height or bbox height, depending on version).</param>
        /// <returns>A byte array, encoding the collision mask as a 1-bit-per-pixel image.</returns>
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
                IMagickColor<byte> white = MagickColors.White;

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
        /// Generates and returns a black-and-white image representing a given sprite's collision mask,
        /// and with the given width/height.
        /// </summary>
        /// <param name="mask">Mask entry to generate the image from.</param>
        /// <param name="maskWidth">Width of the image to generate (and to interpret the collision mask with).</param>
        /// <param name="maskHeight">Height of the image to generate (and to interpret the collision mask with).</param>
        /// <returns>A new black-and-white image representing the specified collision mask.</returns>
        public static IMagickImage<byte> GetCollisionMaskImage(UndertaleSprite.MaskEntry mask, int maskWidth, int maskHeight)
        {
            // Create image to draw on
            MagickImage image = new(MagickColor.FromRgba(0, 0, 0, 255), maskWidth, maskHeight);
            IPixelCollection<byte> pixels = image.GetPixels();

            // Get black/white colors to use for drawing
            ReadOnlySpan<byte> black = MagickColors.Black.ToByteArray().AsSpan();
            ReadOnlySpan<byte> white = MagickColors.White.ToByteArray().AsSpan();

            // Draw white pixels if a given bit is set; black pixels otherwise
            byte[] maskData = mask.Data;
            for (int y = 0; y < maskHeight; y++)
            {
                int rowStart = y * ((maskWidth + 7) / 8);
                for (int x = 0; x < maskWidth; x++)
                {
                    byte temp = maskData[rowStart + (x / 8)];
                    bool pixelBit = (temp & (0b1 << (7 - (x % 8)))) != 0b0;
                    pixels.SetPixel(x, y, pixelBit ? white : black);
                }
            }

            return image;
        }

        /// <summary>
        /// Exports a collision mask entry from a given sprite's collision mask, as a PNG file, at the specified path, and with the given width/height.
        /// </summary>
        /// <param name="mask">Mask entry to export the image from.</param>
        /// <param name="filePath">File path to export to.</param>
        /// <param name="maskWidth">Width of the image to export (and to interpret the collision mask with).</param>
        /// <param name="maskHeight">Height of the image to export (and to interpret the collision mask with).</param>
        public static void ExportCollisionMaskPNG(UndertaleSprite.MaskEntry mask, string filePath, int maskWidth, int maskHeight)
        {
            using var image = GetCollisionMaskImage(mask, maskWidth, maskHeight);
            SaveImageToFile(image, filePath);
        }

        /// <summary>
        /// Saves the provided image as a PNG file, at the specified path.
        /// </summary>
        /// <param name="image">Image to save.</param>
        /// <param name="filePath">File path to save the image to.</param>
        public static void SaveImageToFile(IMagickImage<byte> image, string filePath)
        {
            using var stream = new FileStream(filePath, FileMode.Create);
            image.Write(stream, MagickFormat.Png32);
        }

        /// <summary>
        /// Returns the width and height of the image stored at the given path, or -1 width/height if the file fails to parse as an image.
        /// </summary>
        /// <param name="filePath">File path to get the image size from.</param>
        /// <returns>Width and height of the image stored at the file path, or -1 for both values if invalid.</returns>
        public static (int width, int height) GetImageSizeFromFile(string filePath)
        {
            try
            {
                MagickImageInfo info = new(filePath);
                return (info.Width, info.Height);
            }
            catch (Exception)
            {
                return (-1, -1);
            }
        }

        /// <inheritdoc/>
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
