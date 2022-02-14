using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using UndertaleModLib.Models;

namespace UndertaleModTool
{
    [ValueConversion(typeof(byte[]), typeof(ImageSource))]
    public class UndertaleCachedImageLoader : IValueConverter
    {
        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteObject([In] IntPtr hObject);

        public static ConcurrentDictionary<string, ImageSource> ImageCache { get; set; } = new();
        public static ConcurrentDictionary<Tuple<string, Tuple<uint, uint, uint, uint>>, ImageSource> TileCache { get; set; } = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null)
                return null;

            bool isTile = false;
            bool cacheEnabled = true;
            bool generate = false;

            string par;
            List<Tuple<uint, uint, uint, uint>> tileRectList = null;
            if (parameter is string)
            {
                par = parameter as string;

                isTile = par.Contains("tile");
                cacheEnabled = !par.Contains("nocache");
                generate = par.Contains("generate");
            }
            else if (parameter is List<Tuple<uint, uint, uint, uint>>)
            {
                generate = true;
                tileRectList = parameter as List<Tuple<uint, uint, uint, uint>>;
            }

            UndertaleRoom.Tile tile = null;
            if (isTile)
                tile = value as UndertaleRoom.Tile;

            UndertaleTexturePageItem texture = isTile ? tile.Tpag : value as UndertaleTexturePageItem;
            if (texture is null)
                return null;

            string texName = texture.Name.Content;

            if (tileRectList is not null)
            {
                Rectangle rect = new(texture.SourceX, texture.SourceY, texture.SourceWidth, texture.SourceHeight);
                ProcessTileSet(texName, CreateSpriteBitmap(rect, in texture), tileRectList);

                return null;
            }

            ImageSource spriteSrc;
            if (isTile)
            {
                if (TileCache.TryGetValue(new(texName, new(tile.SourceX, tile.SourceY, tile.Width, tile.Height)), out spriteSrc))
                    return spriteSrc;
            }

            if (!ImageCache.ContainsKey(texName) || !cacheEnabled)
            {
                Rectangle rect;

                // how many pixels are out of bounds of tile texture page
                int diffW = 0;
                int diffH = 0;

                if (isTile)
                {
                    diffW = (int)(tile.SourceX + tile.Width - texture.BoundingWidth);
                    diffH = (int)(tile.SourceY + tile.Height - texture.BoundingHeight);
                    rect = new((int)(texture.SourceX + tile.SourceX), (int)(texture.SourceY + tile.SourceY), (int)tile.Width, (int)tile.Height);
                }
                else
                    rect = new(texture.SourceX, texture.SourceY, texture.SourceWidth, texture.SourceHeight);

                spriteSrc = CreateSpriteSource(in rect, in texture, diffW, diffH);

                if (cacheEnabled)
                {
                    if (isTile)
                        TileCache.TryAdd(new(texName, new(tile.SourceX, tile.SourceY, tile.Width, tile.Height)), spriteSrc);
                    else
                        ImageCache.TryAdd(texName, spriteSrc);
                }

                if (generate)
                    return null;
                else
                    return spriteSrc;
            }

            return ImageCache[texName];
        }

        private Bitmap CreateSpriteBitmap(Rectangle rect, in UndertaleTexturePageItem texture, int diffW = 0, int diffH = 0)
        {
            using MemoryStream stream = new(texture.TexturePage.TextureData.TextureBlob);
            Bitmap spriteBMP = new(rect.Width, rect.Height);

            rect.Width -= (diffW > 0) ? diffW : 0;
            rect.Height -= (diffH > 0) ? diffH : 0;

            using (Graphics g = Graphics.FromImage(spriteBMP))
            {
                using Image img = Image.FromStream(stream); // "ImageConverter.ConvertFrom()" does the same, except it doesn't explicitly dispose MemoryStream
                g.DrawImage(img, new Rectangle(0, 0, rect.Width, rect.Height), rect, GraphicsUnit.Pixel);
            }

            return spriteBMP;
        }
        private ImageSource CreateSpriteSource(in Rectangle rect, in UndertaleTexturePageItem texture, int diffW = 0, int diffH = 0)
        {
            Bitmap spriteBMP = CreateSpriteBitmap(rect, in texture, diffW, diffH);

            IntPtr bmpPtr = spriteBMP.GetHbitmap();
            ImageSource spriteSrc = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(bmpPtr, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            DeleteObject(bmpPtr);
            spriteBMP.Dispose();
            spriteSrc.Freeze(); // allow UI thread access

            return spriteSrc;
        }
        private void ProcessTileSet(string textureName, Bitmap bmp, List<Tuple<uint, uint, uint, uint>> tileRectList)
        {
            BitmapData data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, bmp.PixelFormat);
            int depth = Image.GetPixelFormatSize(data.PixelFormat) / 8;
            byte[] buffer = new byte[data.Stride * bmp.Height];
            Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);

            _ = Parallel.ForEach(tileRectList, (tileRect) =>
            {
                int x = (int)tileRect.Item1;
                int y = (int)tileRect.Item2;
                int w = (int)tileRect.Item3;
                int h = (int)tileRect.Item4;

                /// Sometimes, tile size can be bigger than texture size
                /// (for example, BG tile of "room_torielroom")
                /// Also, it can be out of texture bounds
                /// (for example, tile 10055649 of "room_fire_core_topright")
                /// (both examples are from Undertale)
                /// This algorithm doesn't support that, so this tile will be processed by "CreateSpriteSource()"
                if (w > data.Width || h > data.Height || x + w > data.Width || y + h > data.Height)
                    return;

                byte[] bufferRes = new byte[w * h * depth];

                // Source - https://stackoverflow.com/a/9691388/12136394
                // There was faster solution, but it uses "unsafe" code
                for (int i = 0; i < h; i++)
                {
                    for (int j = 0; j < w * depth; j += depth)
                    {
                        int origIndex = (y * data.Stride) + (i * data.Stride) + (x * depth) + j;
                        int croppedIndex = (i * w * depth) + j;

                        for (int k = 0; k < depth; k++)
                            bufferRes[croppedIndex + k] = buffer[origIndex + k];
                    }
                }

                Bitmap tileBMP = new(w, h);
                BitmapData dataNew = tileBMP.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, data.PixelFormat);
                Marshal.Copy(bufferRes, 0, dataNew.Scan0, bufferRes.Length);
                tileBMP.UnlockBits(dataNew);

                IntPtr bmpPtr = tileBMP.GetHbitmap();
                ImageSource spriteSrc = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(bmpPtr, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                DeleteObject(bmpPtr);
                tileBMP.Dispose();

                spriteSrc.Freeze(); // allow UI thread access

                Tuple<string, Tuple<uint, uint, uint, uint>> tileKey = new(textureName, new((uint)x, (uint)y, (uint)w, (uint)h));
                TileCache.TryAdd(tileKey, spriteSrc);
            });

            bmp.UnlockBits(data);
            bmp.Dispose();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
