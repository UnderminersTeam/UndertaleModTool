using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
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
        public static ConcurrentDictionary<string, Bitmap> TilePageCache { get; set; } = new(); // is cleared after generating cache

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null)
                return null;

            bool isTile = false;
            bool cacheEnabled = true;
            bool generate = false;

            string par;
            Tuple<uint, uint, uint, uint> tileRect = null;
            if (parameter is string)
            {
                par = parameter as string;

                isTile = par.Contains("tile");
                cacheEnabled = !par.Contains("nocache");
            }
            else if (parameter is Tuple<uint, uint, uint, uint>)
            {
                generate = true;
                tileRect = parameter as Tuple<uint, uint, uint, uint>;
            }

            uint tileX = 0;
            uint tileY = 0;
            uint tileW = 0;
            uint tileH = 0;
            UndertaleRoom.Tile tile = null;
            if (isTile)
            {
                tile = value as UndertaleRoom.Tile;

                tileX = tile.SourceX;
                tileY = tile.SourceY;
                tileW = tile.Width;
                tileH = tile.Height;
            }

            string tileStr = null;
            if (tileRect is not null)
            {
                tileX = tileRect.Item1;
                tileY = tileRect.Item2;
                tileW = tileRect.Item3;
                tileH = tileRect.Item4;
                tileStr = ":" + tileX + ',' + tileY + ',' + tileW + ',' + tileH; // interpolated string (String.Format()) is heavier
            }

            UndertaleTexturePageItem texture = isTile ? tile.Tpag : value as UndertaleTexturePageItem;
            if (texture is null)
                return null;

            string texName = isTile ? (texture.Name.Content + ':' + tileX + ',' + tileY + ',' + tileW + ',' + tileH) : texture.Name.Content + tileStr;

            if (tileRect is not null && !TilePageCache.ContainsKey(texture.Name.Content))
            {
                Rectangle rect = new(texture.SourceX, texture.SourceY, texture.SourceWidth, texture.SourceHeight);
                TilePageCache.TryAdd(texture.Name.Content, CreateSpriteBitmap(in rect, in texture));
            }

            if (!ImageCache.ContainsKey(texName) || !cacheEnabled)
            {
                Rectangle rect;
                if (isTile)
                    rect = new((int)(texture.SourceX + tileX), (int)(texture.SourceY + tileY), (int)tileW, (int)tileH);
                else
                {
                    if (tileRect is not null)
                        rect = new((int)tileX, (int)tileY, (int)tileW, (int)tileH);
                    else
                        rect = new(texture.SourceX, texture.SourceY, texture.SourceWidth, texture.SourceHeight);
                }

                ImageSource spriteSrc;
                if (tileRect is not null)
                    spriteSrc = CreateSpriteSource(in rect, null, true, texture.Name.Content);
                else
                    spriteSrc = CreateSpriteSource(in rect, in texture);

                if (cacheEnabled)
                    ImageCache.TryAdd(texName, spriteSrc);

                if (generate)
                    return null;
                else
                    return spriteSrc;
            }

            return ImageCache[texName];
        }

        private Bitmap CreateSpriteBitmap(in Rectangle rect, in UndertaleTexturePageItem texture, bool isTile = false, string textureName = null)
        {
            MemoryStream stream = null;
            if (!isTile)
                stream = new(texture.TexturePage.TextureData.TextureBlob);

            Bitmap spriteBMP = new(rect.Width, rect.Height);
            
            using (Graphics g = Graphics.FromImage(spriteBMP))
            {
                if (isTile)
                    g.DrawImage(TilePageCache[textureName], new Rectangle(0, 0, rect.Width, rect.Height), rect, GraphicsUnit.Pixel);
                else
                {
                    using Image img = Image.FromStream(stream); // "ImageConverter.ConvertFrom()" does the same, except it doesn't explicitly dispose MemoryStream
                    g.DrawImage(img, new Rectangle(0, 0, rect.Width, rect.Height), rect, GraphicsUnit.Pixel);

                    stream.Dispose();
                }
            }

            return spriteBMP;
        }
        private ImageSource CreateSpriteSource(in Rectangle rect, in UndertaleTexturePageItem texture, bool isTile = false, string textureName = null)
        {
            Bitmap spriteBMP;
            if (isTile)
                spriteBMP = CreateSpriteBitmap(in rect, null, true, textureName);
            else
                spriteBMP = CreateSpriteBitmap(in rect, in texture);

            IntPtr bmpPtr = spriteBMP.GetHbitmap();
            ImageSource spriteSrc = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(bmpPtr, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            DeleteObject(bmpPtr);
            spriteBMP.Dispose();

            spriteSrc.Freeze(); // allow UI thread access

            return spriteSrc;
        }
        

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
