using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static UndertaleModLib.Models.UndertaleRoom;

namespace UndertaleModTool
{
    // source - https://stackoverflow.com/a/4801434/12136394
    public class TileLayerImage : Image
    {
        private static readonly DependencyProperty LayerTilesDataProperty =
            DependencyProperty.Register("LayerTilesData", typeof(Layer.LayerTilesData),
                typeof(TileLayerImage),
                new FrameworkPropertyMetadata(null));
        private static readonly DependencyProperty CheckTransparencyProperty =
            DependencyProperty.Register("CheckTransparency", typeof(bool),
                typeof(TileLayerImage),
                new FrameworkPropertyMetadata(false));

        public Layer.LayerTilesData LayerTilesData
        {
            get => (Layer.LayerTilesData)GetValue(LayerTilesDataProperty);
            set => SetValue(LayerTilesDataProperty, value);
        }
        public bool CheckTransparency 
        {
            get => (bool)GetValue(CheckTransparencyProperty);
            set => SetValue(CheckTransparencyProperty, value);
        }

        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            BitmapSource source = (BitmapSource)Source;

            // Get the pixel of the source that was hit
            int x = (int)(hitTestParameters.HitPoint.X / ActualWidth * source.PixelWidth);
            int y = (int)(hitTestParameters.HitPoint.Y / ActualHeight * source.PixelHeight);

            if (CheckTransparency)
            {
                // Copy the single pixel into a new byte array representing RGBA
                byte[] pixel = new byte[4];
                source.CopyPixels(new Int32Rect(x, y, 1, 1), pixel, 4, 0);

                // Check the alpha (transparency) of the pixel
                if (pixel[3] == 0)
                    return null;
            }
            else
            {
                int x1 = x / (int)LayerTilesData.Background.GMS2TileWidth;
                int y1 = y / (int)LayerTilesData.Background.GMS2TileHeight;

                if (x1 < 0 || x1 > LayerTilesData.TilesX - 1 ||
                    y1 < 0 || y1 > LayerTilesData.TilesY - 1 ||
                    LayerTilesData.TileData[y1][x1] == 0)
                    return null;
            }

            return new PointHitTestResult(this, hitTestParameters.HitPoint);
        }
    }
}
