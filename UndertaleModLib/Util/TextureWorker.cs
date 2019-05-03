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
            Bitmap result = GetTextureFor(texPageItem, imageName != null ? imageName : Path.GetFileNameWithoutExtension(FullPath));

            // Export the image data.
            var stream = new FileStream(FullPath, FileMode.Create);
            result.Save(stream, ImageFormat.Png);
            stream.Close();
            result.Dispose();
        }

        public Bitmap GetTextureFor(UndertaleTexturePageItem texPageItem, string imageName)
        {
            int exportWidth = texPageItem.BoundingWidth; // sprite.Width
            int exportHeight = texPageItem.BoundingHeight; // sprite.Height

            if (!embeddedDictionary.ContainsKey(texPageItem.TexturePage))
                embeddedDictionary[texPageItem.TexturePage] = GetImageFromByteArray(texPageItem.TexturePage.TextureData.TextureBlob);
            Bitmap embeddedImage = embeddedDictionary[texPageItem.TexturePage];

            // Sanity checks.
            if ((texPageItem.TargetWidth > exportWidth) || (texPageItem.TargetHeight > exportHeight))
                throw new InvalidDataException(imageName + " has too large a texture");

            // Create a bitmap representing that part of the texture page.
            Bitmap resultImage = embeddedImage.Clone(new Rectangle(texPageItem.SourceX, texPageItem.SourceY, texPageItem.SourceWidth, texPageItem.SourceHeight), System.Drawing.Imaging.PixelFormat.DontCare);
            
            // Do we need to scale?
            if ((texPageItem.SourceWidth != texPageItem.TargetWidth) || (texPageItem.SourceHeight != texPageItem.TargetHeight))
                resultImage = ResizeImage(resultImage, texPageItem.SourceWidth, texPageItem.SourceHeight);

            // Put it in the final, holder image.
            Bitmap finalImage = new Bitmap(exportWidth, exportHeight);
            Graphics g = Graphics.FromImage(finalImage);
            g.DrawImageUnscaled(resultImage, new Point(texPageItem.TargetX, texPageItem.TargetY));
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

        // Grabbed from https://stackoverflow.com/questions/1922040/how-to-resize-an-image-c-sharp
        public static Bitmap ResizeImage(Image image, int newWidth, int newHeight)
        {
            Bitmap newImage = new Bitmap(newWidth, newHeight);
            Graphics g = Graphics.FromImage((Image) newImage);
            g.InterpolationMode = InterpolationMode.High;
            g.DrawImage(image, 0, 0, newWidth, newHeight);
            g.Dispose();
            return newImage;
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

            return bytes.ToArray();
        }

        public static byte[] ReadTextureBlob(string filePath)
        {
            Image.FromFile(filePath); // Make sure the file is valid PNG
            return File.ReadAllBytes(filePath);
        }

        public static void SaveEmptyPNG(string FullPath, int width, int height)
        {
            var b = new Bitmap(1, 1);
            b.SetPixel(0, 0, Color.Black);
            var result = new Bitmap(b, width, height);
            var stream = new FileStream(FullPath, FileMode.Create);
            result.Save(stream, ImageFormat.Png);
            stream.Close();
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
            Bitmap bitmap = GetCollisionMaskImage(sprite, mask);

            // Export the image data.
            var stream = new FileStream(fullPath, FileMode.Create);
            bitmap.Save(stream, ImageFormat.Png);
            stream.Close();
            bitmap.Dispose();
        }
    }
}
