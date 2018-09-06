using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using UndertaleModLib.Models;

namespace UndertaleModTool
{
    [ValueConversion(typeof(UndertaleRoom), typeof(ImageSource))]
    public class UndertaleRoomRendererConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            UndertaleRoom room = value as UndertaleRoom;
            if (room == null)
                return null;

            /*DrawingGroup drawing = new DrawingGroup();
            foreach(UndertaleRoom.Tile tile in room.Tiles)
            {
                ImageBrush brush = new ImageBrush();
                brush.ImageSource = new ImageSourceConverter().ConvertFrom(tile.BgDefIndex.Resource.Texture.SpritesheetId.Resource.TextureData.TextureBlob) as ImageSource;
                brush.Viewbox = new Rect(tile.BgDefIndex.Resource.Texture.SourceX + tile.SourceX, tile.BgDefIndex.Resource.Texture.SourceY + tile.SourceY, tile.Width, tile.Height);
                drawing.Children.Add(new GeometryDrawing(brush, new Pen(), new RectangleGeometry(new Rect(tile.X, tile.Y, tile.Width, tile.Height))));
            }
            drawing.Freeze();
            Debug.WriteLine(drawing);
            return drawing;*/

            /*Dictionary<UndertaleEmbeddedTexture, ImageSource> cache = new Dictionary<UndertaleEmbeddedTexture, ImageSource>();

            DrawingVisual dv = new DrawingVisual();
            using (DrawingContext dc = dv.RenderOpen())
            {
                foreach (UndertaleRoom.Tile tile in room.Tiles)
                {
                    UndertaleEmbeddedTexture tex = tile.BgDefIndex.Resource.Texture.SpritesheetId.Resource;
                    if (!cache.ContainsKey(tex))
                    {
                        cache.Add(tex, new ImageSourceConverter().ConvertFrom(tile.BgDefIndex.Resource.Texture.SpritesheetId.Resource.TextureData.TextureBlob) as ImageSource);
                    }
                    ImageBrush brush = new ImageBrush();
                    brush.ImageSource = cache[tex];
                    brush.Viewbox = new Rect(tile.BgDefIndex.Resource.Texture.SourceX + tile.SourceX, tile.BgDefIndex.Resource.Texture.SourceY + tile.SourceY, tile.Width, tile.Height);
                    dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(102, 181, 243, 20)),
        new Pen(Brushes.Black, 4), new Rect(tile.X, tile.Y, tile.Width, tile.Height));
                }
            }

            RenderTargetBitmap rtb = new RenderTargetBitmap((int)room.Width, (int)room.Height, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(dv);

            PngBitmapEncoder png = new PngBitmapEncoder();
            png.Frames.Add(BitmapFrame.Create(rtb));
            using (Stream stm = File.Create("new.png"))
            {
                png.Save(stm);
            }

            return rtb;*/
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [ValueConversion(typeof(byte[]), typeof(ImageSource))]
    public class UndertaleCachedImageLoader : IValueConverter
    {
        public static Dictionary<byte[], ImageSource> cache = new Dictionary<byte[], ImageSource>(); // TODO: weakref?

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            byte[] data = (byte[])value;
            if (!cache.ContainsKey(data))
            {
                cache.Add(data, new ImageSourceConverter().ConvertFrom(data) as ImageSource);
            }
            return cache[data];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
