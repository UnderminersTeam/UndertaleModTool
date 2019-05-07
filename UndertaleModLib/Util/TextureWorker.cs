using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using UndertaleModLib.Models;

namespace UndertaleModLib.Util
{
    public class TextureWorker
    {
        private Dictionary<UndertaleEmbeddedTexture, Bitmap> embeddedDictionary = new Dictionary<UndertaleEmbeddedTexture, Bitmap>();
        private static readonly ImageConverter _imageConverter = new ImageConverter();

        public void ExportAsPNG(UndertaleTexturePageItem texPageItem, string FullPath, string imageName = null)
        {
            SaveImageToFile(FullPath, GetTextureFor(texPageItem, imageName != null ? imageName : Path.GetFileNameWithoutExtension(FullPath)));
        }

        public Bitmap GetTextureFor(UndertaleTexturePageItem texPageItem, string imageName)
        {
            int exportWidth = texPageItem.BoundingWidth; // sprite.Width
            int exportHeight = texPageItem.BoundingHeight; // sprite.Height

            Bitmap embeddedImage = null;

            lock (embeddedDictionary)
            {
                if (!embeddedDictionary.ContainsKey(texPageItem.TexturePage))
                    embeddedDictionary[texPageItem.TexturePage] = GetImageFromByteArray(texPageItem.TexturePage.TextureData.TextureBlob);
                embeddedImage = embeddedDictionary[texPageItem.TexturePage];
            }

            // Sanity checks.
            if ((texPageItem.TargetWidth > exportWidth) || (texPageItem.TargetHeight > exportHeight))
                throw new InvalidDataException(imageName + "'s texture is larger than its bounding box!");

            // Create a bitmap representing that part of the texture page.
            Bitmap resultImage = null;
            lock (embeddedImage)
                resultImage = embeddedImage.Clone(new Rectangle(texPageItem.SourceX, texPageItem.SourceY, texPageItem.SourceWidth, texPageItem.SourceHeight), PixelFormat.DontCare);

            // Resize the image, if necessary.
            if ((texPageItem.SourceWidth != texPageItem.TargetWidth) || (texPageItem.SourceHeight != texPageItem.TargetHeight))
                resultImage = ResizeImage(resultImage, texPageItem.TargetWidth, texPageItem.TargetHeight);

            // Put it in the final holder image.
            Bitmap finalImage = new Bitmap(exportWidth, exportHeight);
            Graphics g = Graphics.FromImage(finalImage);
            g.DrawImage(resultImage, new Rectangle(texPageItem.TargetX, texPageItem.TargetY, resultImage.Width, resultImage.Height), new Rectangle(0, 0, resultImage.Width, resultImage.Height), GraphicsUnit.Pixel);
            g.Dispose();

            return finalImage;
        }

        // Grabbed from https://stackoverflow.com/questions/3801275/how-to-convert-image-to-byte-array/16576471#16576471
        public static Bitmap GetImageFromByteArray(byte[] byteArray)
        {
            Bitmap bm = (Bitmap)_imageConverter.ConvertFrom(byteArray);

            if (bm != null && (bm.HorizontalResolution != (int)bm.HorizontalResolution ||
                               bm.VerticalResolution != (int)bm.VerticalResolution))
            {
                // Correct a strange glitch that has been observed in the test program when converting 
                //  from a PNG file image created by CopyImageToByteArray() - the dpi value "drifts" 
                //  slightly away from the nominal integer value
                bm.SetResolution((int)(bm.HorizontalResolution + 0.5f),
                                 (int)(bm.VerticalResolution + 0.5f));
            }

            return bm;
        }

        // This should perform a high quality resize.
        // Grabbed from https://stackoverflow.com/questions/1922040/how-to-resize-an-image-c-sharp
        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

        public static byte[] ReadMaskData(string filePath)
        {
            Bitmap image = GetImageFromByteArray(File.ReadAllBytes(filePath));
            List<byte> bytes = new List<byte>();

            int enableColor = Color.White.ToArgb();
            for (int y = 0; y < image.Height; y++)
            {
                for (int xByte = 0; xByte < (image.Width + 7) / 8; xByte++)
                {
                    byte fullByte = 0x00;
                    int pxStart = (xByte * 8);
                    int pxEnd = Math.Min(pxStart + 8, (int) image.Width);

                    for (int x = pxStart; x < pxEnd; x++)
                        if (image.GetPixel(x, y).ToArgb() == enableColor) // Don't use Color == OtherColor, it doesn't seem to give us the type of equals comparison we want here.
                            fullByte |= (byte)(0b1 << (7 - (x - pxStart)));

                    bytes.Add(fullByte);
                }
            }

            image.Dispose();
            return bytes.ToArray();
        }

        public static byte[] ReadTextureBlob(string filePath)
        {
            Image.FromFile(filePath).Dispose(); // Make sure the file is valid image.
            return File.ReadAllBytes(filePath);
        }

        public static void SaveEmptyPNG(string FullPath, int width, int height)
        {
            var blackImage = new Bitmap(width, height);
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    blackImage.SetPixel(x, y, Color.Black);
            SaveImageToFile(FullPath, blackImage);
        }

        public static Bitmap GetCollisionMaskImage(UndertaleSprite sprite, UndertaleSprite.MaskEntry mask)
        {
            byte[] maskData = mask.Data;
            Bitmap bitmap = new Bitmap((int)sprite.Width, (int)sprite.Height, PixelFormat.Format32bppArgb); // Ugh. I want to use 1bpp, but for some BS reason C# doesn't allow SetPixel in that mode.

            for (int y = 0; y < sprite.Height; y++)
            {
                int rowStart = y * (int)((sprite.Width + 7) / 8);
                for (int x = 0; x < sprite.Width; x++)
                {
                    byte temp = maskData[rowStart + (x / 8)];
                    bool pixelBit = (temp & (0b1 << (7 - (x % 8)))) != 0b0;
                    bitmap.SetPixel(x, y, pixelBit ? Color.White : Color.Black);
                }
            }

            return bitmap;
        }

        public static void ExportCollisionMaskPNG(UndertaleSprite sprite, UndertaleSprite.MaskEntry mask, string fullPath)
        {
            SaveImageToFile(fullPath, GetCollisionMaskImage(sprite, mask));
        }

        public static void SaveImageToFile(string FullPath, Image image, Boolean disposeImage = true)
        {
            var stream = new FileStream(FullPath, FileMode.Create);
            image.Save(stream, ImageFormat.Png);
            stream.Close();
            if (disposeImage)
                image.Dispose();
        }
    }
}
