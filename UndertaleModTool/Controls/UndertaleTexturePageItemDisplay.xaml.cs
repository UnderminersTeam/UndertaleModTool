using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace UndertaleModTool
{
    /// <summary>
    /// Logika interakcji dla klasy UndertaleTexturePageItemDisplay.xaml
    /// </summary>
    public partial class UndertaleTexturePageItemDisplay : UserControl
    {
        public UndertaleTexturePageItemDisplay()
        {
            InitializeComponent();
        }

        public void SaveImagePNG(Stream outfile)
        {
            // Hide the render area border when saving to an image
            Thickness oldRenderAreaBorder = RenderAreaBorder.BorderThickness;
            RenderAreaBorder.BorderThickness = new Thickness(0);
            // https://stackoverflow.com/questions/2557183/drawing-a-wpf-usercontrol-with-databinding-to-an-image/2596035#2596035
            Dispatcher.Invoke(DispatcherPriority.Loaded, new Action(() => { }));

            // Render the canvas
            var target = new RenderTargetBitmap((int)RenderSize.Width, (int)RenderSize.Height, 96, 96, PixelFormats.Pbgra32);
            target.Render(this);

            // Encode to a file
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(target));
            encoder.Save(outfile);

            // Restore the border
            RenderAreaBorder.BorderThickness = oldRenderAreaBorder;
        }
    }
}
