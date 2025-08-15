using ImageMagick;
using System;
using System.Collections.Generic;
using System.Linq;
using UndertaleModLib.Models;
using static System.Net.Mime.MediaTypeNames;

namespace UndertaleModLib.Util;

/// <summary>
/// Helper class used to pack images into textures for a single texture group.
/// </summary>
public sealed class TextureGroupPacker
{
    /// <summary>
    /// Maximum allowed texture page size (in terms of width and height) to generate.
    /// </summary>
    public int MaxTextureSize { get; }

    /// <summary>
    /// The amount of extra border pixels each texture entry should be extended by.
    /// </summary>
    public int Border { get; }
    
    /// <summary>
    /// Whether the texture group being packed allows cropping to occur.
    /// </summary>
    public bool AllowCrop { get; }

    /// <summary>
    /// Image format to be used by generated textures.
    /// </summary>
    public GMImage.ImageFormat Format { get; }

    // Next unique ID used for pack images (for determinism when sorting)
    private int _nextPackImageId = 0;

    // List of pages generated for the texture group
    private List<UndertaleEmbeddedTexture> _pages = null;

    // List of texture items generated for the texture group
    private readonly List<UndertaleTexturePageItem> _textureItems = new(128);

    // Lookup of images that are similar to each other (based on a very rough 64-bit hash)
    private readonly Dictionary<long, List<PackImage>> _similarPackImages = new(128);

    // List of images to be packed
    private readonly List<PackImage> _images = new(128);

    // List of duplicate images that don't need to be packed
    private readonly List<PackImageDuplicate> _duplicateImages = new(64);

    /// <summary>
    /// Initializes a new texture group packer with the provided texture group settings.
    /// </summary>
    public TextureGroupPacker(int maxTextureSize = 2048, int border = 2, bool allowCrop = true, GMImage.ImageFormat format = GMImage.ImageFormat.Png)
    {
        // Make sure max texture size is valid
        if (maxTextureSize < 16 || maxTextureSize > GMImage.MaxImageDimension)
        {
            throw new ArgumentOutOfRangeException(nameof(maxTextureSize));
        }
        if ((maxTextureSize & (maxTextureSize - 1)) != 0)
        {
            throw new ArgumentException("Maximum texture size is not a power of two", nameof(maxTextureSize));
        }

        // Make sure border is valid
        if (border < 0 || border >= (maxTextureSize / 2))
        {
            throw new ArgumentOutOfRangeException(nameof(border));
        }

        MaxTextureSize = maxTextureSize;
        Border = border;
        AllowCrop = allowCrop;
        Format = format;
    }

    /// <summary>
    /// Adds an image to be packed by the texture group packer, and returns a texture page item for it.
    /// </summary>
    public UndertaleTexturePageItem AddImage(MagickImage image, BorderFlags borderFlags)
    {
        // Make sure this is used *before* packing only...
        if (_pages is not null)
        {
            throw new InvalidOperationException("Cannot add images after packing");
        }

        // Ensure image is in a valid format
        int uncroppedWidth = (int)image.Width, uncroppedHeight = (int)image.Height;
        image.Format = MagickFormat.Rgba;
        image.SetBitDepth(8);
        image.SetCompression(CompressionMethod.NoCompression);
        image.Alpha(AlphaOption.Set);

        // Crop the image, if allowed
        int minX = 0, minY = 0;
        if (AllowCrop)
        {
            // Scan image for min/max X/Y pixels with nonzero alpha
            minX = uncroppedWidth;
            minY = uncroppedHeight;
            int maxX = 0, maxY = 0;
            ReadOnlySpan<byte> pixels = image.GetPixels().GetReadOnlyArea(0, 0, image.Width, image.Height);
            for (int y = 0; y < uncroppedHeight; y++)
            {
                for (int x = 0; x < uncroppedWidth; x++)
                {
                    if (pixels[(4 * (x + (y * uncroppedWidth))) + 3] != 0)
                    {
                        minX = Math.Min(minX, x);
                        minY = Math.Min(minY, y);
                        maxX = Math.Max(maxX, x);
                        maxY = Math.Max(maxY, y);
                    }
                }
            }

            if (minX > maxX || minY > maxY)
            {
                // Image is literally just empty
                image.Dispose();
                image = new MagickImage(new byte[] { 0, 0, 0, 0 }, MagickFormat.Rgba);
            }
            else
            {
                // Crop image
                image.Crop(new MagickGeometry(minX, minY, (uint)((maxX - minX) + 1), (uint)((maxY - minY) + 1)));
            }
        }

        // Ensure image can actually fix on page
        if (image.Width + (Border * 2) > MaxTextureSize || image.Height + (Border * 2) > MaxTextureSize)
        {
            throw new Exception($"Cannot fit texture item of size {image.Width}x{image.Height} with max texture size {MaxTextureSize}, border {Border}");
        }

        // Create pack image and associated texture page item
        UndertaleTexturePageItem textureItem = new();
        _textureItems.Add(textureItem);
        PackImage packImage = new(image, textureItem, _nextPackImageId++, minX, minY, uncroppedWidth, uncroppedHeight, borderFlags);

        // Check for duplicate, and use original if so
        long hash = packImage.ComputeHash();
        if (_similarPackImages.TryGetValue(hash, out List<PackImage> similarImages))
        {
            // Check all similar images (with the same hash)
            foreach (PackImage similarImage in similarImages)
            {
                if (similarImage.IsDuplicateOf(packImage))
                {
                    // Found a duplicate!
                    PackImageDuplicate duplicateImage = new(packImage.TextureItem, similarImage.TextureItem, 
                                                            packImage.CroppedOffsetX, packImage.CroppedOffsetY, 
                                                            packImage.UncroppedWidth, packImage.UncroppedHeight);
                    packImage.Dispose();
                    _duplicateImages.Add(duplicateImage);
                    return duplicateImage.TextureItem;
                }
            }

            // No duplicates; add another similar image
            similarImages.Add(packImage);
        }
        else
        {
            // No similar images; create new list
            _similarPackImages.Add(hash, [packImage]);
        }

        // Return texture item from this pack image, and add to list
        _images.Add(packImage);
        return packImage.TextureItem;
    }

    /// <summary>
    /// Packs and generates textures for all texture pages for the packer after all images have been added.
    /// </summary>
    public void PackPages()
    {
        // Sort images by area, and then by unique ID
        _images.Sort((a, b) =>
        {
            int area = ((int)a.Width * (int)a.Height) - ((int)b.Width * (int)b.Height);
            if (area != 0)
            {
                return area;
            }
            return b.UniqueID - a.UniqueID;
        });

        // Perform packing algorithm
        _pages = new(4);
        while (_images.Count > 0)
        {
            _pages.Add(PackSinglePage());
        }

        // Resolve all duplicate images
        foreach (PackImageDuplicate duplicateImage in _duplicateImages)
        {
            UndertaleTexturePageItem dupTextureItem = duplicateImage.TextureItem;
            UndertaleTexturePageItem origTextureItem = duplicateImage.OriginalTextureItem;
            dupTextureItem.TexturePage = origTextureItem.TexturePage;
            dupTextureItem.SourceX = origTextureItem.SourceX;
            dupTextureItem.SourceY = origTextureItem.SourceY;
            dupTextureItem.SourceWidth = origTextureItem.SourceWidth;
            dupTextureItem.SourceHeight = origTextureItem.SourceHeight;
            dupTextureItem.TargetX = (ushort)duplicateImage.CroppedOffsetX;
            dupTextureItem.TargetY = (ushort)duplicateImage.CroppedOffsetY;
            dupTextureItem.TargetWidth = origTextureItem.TargetWidth;
            dupTextureItem.TargetHeight = origTextureItem.TargetHeight;
            dupTextureItem.BoundingWidth = (ushort)duplicateImage.UncroppedWidth;
            dupTextureItem.BoundingHeight = (ushort)duplicateImage.UncroppedHeight;
        }
    }

    /// <summary>
    /// Imports all generated texture pages and texture items to the supplied game data.
    /// </summary>
    public void ImportToData(UndertaleData data)
    {
        // Import textures
        int embeddedTextureCount = data.EmbeddedTextures.Count;
        foreach (UndertaleEmbeddedTexture texture in _pages)
        {
            texture.Name = new UndertaleString($"Texture {embeddedTextureCount++}");
            data.EmbeddedTextures.Add(texture);
        }

        // TODO: support for external textures?

        // Import texture items
        int textureItemCount = data.TexturePageItems.Count;
        foreach (UndertaleTexturePageItem textureItem in _textureItems)
        {
            textureItem.Name = new UndertaleString($"PageItem {textureItemCount++}");
            data.TexturePageItems.Add(textureItem);
        }
    }

    /// <summary>
    /// Rectangle used for packing images on a texture.
    /// </summary>
    private readonly record struct TextureRect(int X, int Y, int Width, int Height);

    /// <summary>
    /// Packs and generates a texture for a single texture page, given all remaining images to be packed.
    /// </summary>
    private UndertaleEmbeddedTexture PackSinglePage()
    {
        // Create embedded texture for page
        UndertaleEmbeddedTexture newTex = new();

        // Try to insert images into free rectangles using greedy heuristics (images are sorted by area already)
        int maxWidth = 16, maxHeight = 16;
        List<TextureRect> freeRectangles = new(128)
        {
            new(0, 0, MaxTextureSize, MaxTextureSize)
        };
        List<PackImage> currentPageImages = new(128);
        for (int i = _images.Count - 1; i >= 0; i--)
        {
            // Attempt inserting current image
            PackImage currImage = _images[i];
            if (TryInsertImage(freeRectangles, (int)currImage.Width + (Border * 2), (int)currImage.Height + (Border * 2), out int x, out int y))
            {
                // Successfully placed - remove image from list, and add to list for current page
                _images.RemoveAt(i);
                currentPageImages.Add(currImage);

                // Update max width/height of this page
                maxWidth = Math.Max(maxWidth, x + (int)currImage.Width + (Border * 2));
                maxHeight = Math.Max(maxHeight, y + (int)currImage.Height + (Border * 2));

                // Update texture item for image
                UndertaleTexturePageItem currTextureItem = currImage.TextureItem;
                currTextureItem.TexturePage = newTex;
                currTextureItem.SourceX = (ushort)(x + Border);
                currTextureItem.SourceY = (ushort)(y + Border);
                currTextureItem.SourceWidth = (ushort)currImage.Width;
                currTextureItem.SourceHeight = (ushort)currImage.Height;
                currTextureItem.TargetX = (ushort)currImage.CroppedOffsetX;
                currTextureItem.TargetY = (ushort)currImage.CroppedOffsetY;
                currTextureItem.TargetWidth = (ushort)currImage.Width;
                currTextureItem.TargetHeight = (ushort)currImage.Height;
                currTextureItem.BoundingWidth = (ushort)currImage.UncroppedWidth;
                currTextureItem.BoundingHeight = (ushort)currImage.UncroppedHeight;
            }
        }

        // Crop final page
        uint width = (uint)MaxTextureSize, height = (uint)MaxTextureSize;
        while ((width / 2) >= maxWidth)
        {
            width /= 2;
        }
        while ((height / 2) >= maxHeight)
        {
            height /= 2;
        }

        // Generate texture image
        using MagickImage textureImage = new(MagickColors.Transparent, width, height)
        {
            Format = MagickFormat.Rgba
        };
        textureImage.SetCompression(CompressionMethod.NoCompression);
        textureImage.Alpha(AlphaOption.Set);
        foreach (PackImage image in currentPageImages)
        {
            int x = image.TextureItem.SourceX, y = image.TextureItem.SourceY;
            textureImage.Composite(image.Image, x, y, CompositeOperator.Copy);

            // TODO: generate borders

            image.Dispose();
        }
        newTex.TextureData.Image = GMImage.FromMagickImage(textureImage).ConvertToFormat(Format);

        return newTex;
    }
    
    private static bool TryInsertImage(List<TextureRect> freeRects, int width, int height, out int x, out int y)
    {
        // Find good location to place rectangle, based on how closely the short and long sides of the rectangle fit
        int bestShortSideFit = int.MaxValue;
        int bestLongSideFit = int.MaxValue;
        TextureRect bestRect = default;
        for (int i = 0; i < freeRects.Count; i++)
        {
            TextureRect rect = freeRects[i];
            if (rect.Width >= width && rect.Height >= height)
            {
                int leftoverHoriz = rect.Width - width;
                int leftoverVert = rect.Height - height;
                int shortSideFit = Math.Min(leftoverHoriz, leftoverVert);
                int longSideFit = Math.Max(leftoverHoriz, leftoverVert);

                if (shortSideFit < bestShortSideFit || (shortSideFit == bestShortSideFit && longSideFit < bestLongSideFit))
                {
                    bestRect = rect;
                    bestShortSideFit = shortSideFit;
                    bestLongSideFit = longSideFit;
                }
            }
        }

        // If no best rectangle is found, insertion failed (not enough space)
        if (bestRect == default)
        {
            x = 0;
            y = 0;
            return false;
        }

        // Split all rectangles we intersect with
        int numRectsToProcess = freeRects.Count;
        for (int i = 0; i < numRectsToProcess; i++)
        {
            TextureRect currRect = freeRects[i];

            // If not intersecting, ignore
            if (bestRect.X >= currRect.X + currRect.Width || bestRect.Y >= currRect.Y + currRect.Height)
            {
                continue;
            }
            if (bestRect.X + width <= currRect.X || bestRect.Y + height <= currRect.Y)
            {
                continue;
            }

            // Remove old rect
            freeRects.RemoveAt(i);
            numRectsToProcess--;
            i--;

            if (bestRect.X < currRect.X + currRect.Width && bestRect.X + width > currRect.X)
            {
                // New rectangle at the top side of the used rectangle
                if (bestRect.Y > currRect.Y && bestRect.Y < currRect.Y + currRect.Height)
                {
                    freeRects.Add(new(currRect.X, currRect.Y, currRect.Width, bestRect.Y - currRect.Y));
                }

                // New rectangle at the bottom side of the used rectangle
                if (bestRect.Y + height < currRect.Y + currRect.Height)
                {
                    freeRects.Add(new(currRect.X, bestRect.Y + height, currRect.Width, currRect.Y + currRect.Height - (bestRect.Y + height)));
                }
            }

            if (bestRect.Y < currRect.Y + currRect.Height && bestRect.Y + height > currRect.Y)
            {
                // New rectangle at the left side of the used rectangle
                if (bestRect.X > currRect.X && bestRect.X < currRect.X + currRect.Width)
                {
                    freeRects.Add(new(currRect.X, currRect.Y, bestRect.X - currRect.X, currRect.Height));
                }

                // New rectangle at the right side of the used rectangle
                if (bestRect.X + width < currRect.X + currRect.Width)
                {
                    freeRects.Add(new(bestRect.X + width, currRect.Y, currRect.X + currRect.Width - (bestRect.X + width), currRect.Height));
                }
            }
        }

        // Prune rectangles that are wholly contained within another rectangle
        for (int i = 0; i < freeRects.Count; i++)
        {
            for (int j = i + 1; j < freeRects.Count; j++)
            {
                TextureRect a = freeRects[i];
                TextureRect b = freeRects[j];

                if (a.X >= b.X && a.Y >= b.Y &&
                    a.X + a.Width <= b.X + b.Width &&
                    a.Y + a.Height <= b.Y + b.Height)
                {
                    freeRects.RemoveAt(i);
                    i--;
                    break;
                }

                if (b.X >= a.X && b.Y >= a.Y &&
                    b.X + b.Width <= a.X + a.Width &&
                    b.Y + b.Height <= a.Y + a.Height)
                {
                    freeRects.RemoveAt(j);
                    j--;
                }
            }
        }

        // Return the X/Y coordinates of the best rectangle determined earlier
        x = bestRect.X;
        y = bestRect.Y;
        return true;
    }

    /// <summary>
    /// Flags used for generating borders for a texture page item.
    /// </summary>
    [Flags]
    public enum BorderFlags
    {
        None = 0,

        /// <summary>
        /// Enables border generation for the pack image.
        /// </summary>
        Enabled = 1 << 0,

        /// <summary>
        /// Whether the pack image should tile (wrap around) horizontally when generating its border.
        /// </summary>
        TileHorizontally = 1 << 1,

        /// <summary>
        /// Whether the pack image should tile (wrap around) vertically when generating its border.
        /// </summary>
        TileVertically = 1 << 2,

        /// <summary>
        /// Whether the border of the pack image should be extended by 2 pixels (in older versions) or 1 pixel (in post-GMS2 versions).
        /// </summary>
        ExtraBorder = 1 << 3
    }

    /// <summary>
    /// Represents a single image to be packed as part of a pack/texture page.
    /// </summary>
    private sealed class PackImage(MagickImage croppedImage, UndertaleTexturePageItem textureItem, int uniqueId, int croppedOffsetX, int croppedOffsetY, int uncroppedWidth, int uncroppedHeight, BorderFlags borderFlags) : IDisposable
    {
        /// <summary>
        /// Source (cropped) image data.
        /// </summary>
        public MagickImage Image { get; } = croppedImage;

        /// <summary>
        /// Texture item to be populated with data from this image, during final packing.
        /// </summary>
        public UndertaleTexturePageItem TextureItem { get; } = textureItem;

        /// <summary>
        /// Width of the cropped image data.
        /// </summary>
        public uint Width { get; } = croppedImage.Width;

        /// <summary>
        /// Height of the cropped image data.
        /// </summary>
        public uint Height { get; } = croppedImage.Height;

        /// <summary>
        /// X offset into the image at which the crop was performed. That is, this is non-negative.
        /// </summary>
        public int CroppedOffsetX { get; } = croppedOffsetX;

        /// <summary>
        /// Y offset into the image at which the crop was performed. That is, this is non-negative.
        /// </summary>
        public int CroppedOffsetY { get; } = croppedOffsetY;

        /// <summary>
        /// Full uncropped image width.
        /// </summary>
        public int UncroppedWidth { get; } = uncroppedWidth;

        /// <summary>
        /// Full uncropped image height.
        /// </summary>
        public int UncroppedHeight { get; } = uncroppedHeight;

        /// <summary>
        /// Unique incrementing ID to be used for deterministic sorting.
        /// </summary>
        public int UniqueID { get; } = uniqueId;

        /// <summary>
        /// Flags defining how the border should be generated for the image on a page.
        /// </summary>
        public BorderFlags BorderFlags { get; } = borderFlags;

        /// <summary>
        /// Gets 7 most-significant bits from the color channels of the given pixel.
        /// </summary>
        /// <remarks>
        /// Dedicates 2 bits for MSB of the red channel, 2 bits for MSB of the green channel, 2 bits for MSB of the blue channel, and 1 bit for the MSB of the alpha channel.
        /// </remarks>
        private static long Get7BitsFromPixel(IPixel<byte> pixel)
        {
            IMagickColor<byte> color = pixel.ToColor();
            return (color.R >> 6) | ((color.G >> 6) << 2) | ((color.B >> 6) << 4) | ((color.A >> 7) << 6);
        }

        /// <summary>
        /// Computes a 64-bit integer hash of the pack image, based on width, height, border flags, and some image contents.
        /// </summary>
        /// <remarks>
        /// This is used as a heuristic for de-duplicating images during texture packing.
        /// </remarks>
        public long ComputeHash()
        {
            long value = 0;
            
            // Use lower 29 bits for width/height (and also use lower bits to store part of the border flags).
            value |= (long)BorderFlags | (((long)Width & 0b1111_1111_1111_11) << 1) | (((long)Height & 0b1111_1111_1111_11) << 15);

            // Images should be at least 1x1... verify that
            uint width = Image.Width, height = Image.Height;
            if (width < 1 || height < 1 || width > int.MaxValue || height > int.MaxValue)
            {
                throw new InvalidOperationException();
            }

            // Use 7 bits from 5 pixels (corners and center) for most of the rest
            IUnsafePixelCollection<byte> pixels = Image.GetPixelsUnsafe();
            value |= Get7BitsFromPixel(pixels.GetPixel(0, 0)) << 29;
            value |= Get7BitsFromPixel(pixels.GetPixel((int)width - 1, 0)) << (29 + 7);
            value |= Get7BitsFromPixel(pixels.GetPixel(0, (int)height - 1)) << (29 + (7 * 2));
            value |= Get7BitsFromPixel(pixels.GetPixel((int)width - 1, (int)height - 1)) << (29 + (7 * 3));
            value |= Get7BitsFromPixel(pixels.GetPixel((int)(width >> 1), (int)(height >> 1))) << (29 + (7 * 4));

            return value;
        }

        /// <summary>
        /// Checks whether the pack image is a duplicate of another pack image.
        /// </summary>
        /// <remarks>
        /// This should only be used after <see cref="ComputeHash"/> has been called on both instances, and the results were equal.
        /// </remarks>
        public bool IsDuplicateOf(PackImage other)
        {
            // Simple checks
            uint widthA = Image.Width, heightA = Image.Height;
            uint widthB = other.Image.Width, heightB = other.Image.Height;
            if (widthA != widthB || heightA != heightB)
            {
                return false;
            }
            if (BorderFlags != other.BorderFlags)
            {
                return false;
            }
            if (CroppedOffsetX != other.CroppedOffsetX || CroppedOffsetY != other.CroppedOffsetY)
            {
                return false;
            }

            // Compare all of the image data
            ReadOnlySpan<byte> pixelsA = Image.GetPixels().GetReadOnlyArea(0, 0, widthA, heightA);
            ReadOnlySpan<byte> pixelsB = other.Image.GetPixels().GetReadOnlyArea(0, 0, widthB, heightB);
            return pixelsA.SequenceEqual(pixelsB);
        }

        // Disposal for the image
        private bool disposedValue = false;
        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Image.Dispose();
                }
                disposedValue = true;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Represents a duplicate of a <see cref="PackImage"/>, with its own unique texture item.
    /// </summary>
    private sealed class PackImageDuplicate(UndertaleTexturePageItem textureItem, UndertaleTexturePageItem originalTextureItem, int croppedOffsetX, int croppedOffsetY, int uncroppedWidth, int uncroppedHeight)
    {
        /// <summary>
        /// Texture item to be populated with data from this image, during final packing.
        /// </summary>
        public UndertaleTexturePageItem TextureItem { get; } = textureItem;

        /// <summary>
        /// This is the first/single (non-duplicate) texture item, from which its data can be used to partially populate <see cref="TextureItem"/>.
        /// </summary>
        public UndertaleTexturePageItem OriginalTextureItem { get; } = originalTextureItem;

        /// <summary>
        /// X offset into the image at which the crop was performed. That is, this is non-negative.
        /// </summary>
        public int CroppedOffsetX { get; } = croppedOffsetX;

        /// <summary>
        /// Y offset into the image at which the crop was performed. That is, this is non-negative.
        /// </summary>
        public int CroppedOffsetY { get; } = croppedOffsetY;

        /// <summary>
        /// Full uncropped image width.
        /// </summary>
        public int UncroppedWidth { get; } = uncroppedWidth;

        /// <summary>
        /// Full uncropped image height.
        /// </summary>
        public int UncroppedHeight { get; } = uncroppedHeight;
    }
}
