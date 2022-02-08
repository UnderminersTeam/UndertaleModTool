using System;
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

        public static Dictionary<string, ImageSource> ImageCache { get; set; } = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null)
                return null;

            bool isTile = false;
            bool cacheEnabled = true;

            string par = null;
            if (parameter is string)
            {
                par = parameter as string;
                isTile = par.Contains("tile");
                cacheEnabled = !par.Contains("nocache");
            }

            UndertaleRoom.Tile tile = null;
            if (isTile)
                tile = value as UndertaleRoom.Tile;

            UndertaleTexturePageItem texture = isTile ? tile.Tpag : value as UndertaleTexturePageItem;
            string texName = isTile ? $"{texture.Name.Content}:{tile.SourceX},{tile.SourceY},{tile.Width},{tile.Height}" : texture.Name.Content;
            if (!ImageCache.ContainsKey(texName) || !cacheEnabled)
            {
                Rectangle rect;
                if (isTile)
                    rect = new((int)(texture.SourceX + tile.SourceX), (int)(texture.SourceY + tile.SourceY), (int)tile.Width, (int)tile.Height);
                else
                    rect = new(texture.SourceX, texture.SourceY, texture.SourceWidth, texture.SourceHeight);

                using MemoryStream stream = new(texture.TexturePage.TextureData.TextureBlob);
                Bitmap spriteBMP = new(rect.Width, rect.Height);
                using (Graphics g = Graphics.FromImage(spriteBMP))
                {
                    using Image img = Image.FromStream(stream);
                    g.DrawImage(img, new Rectangle(0, 0, rect.Width, rect.Height), rect, GraphicsUnit.Pixel);
                }

                IntPtr bmpPtr = spriteBMP.GetHbitmap();
                ImageSource spriteSrc = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(bmpPtr, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                DeleteObject(bmpPtr);

                if (cacheEnabled)
                    ImageCache[texName] = spriteSrc;

                return spriteSrc;
            }

            return ImageCache[texName];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
