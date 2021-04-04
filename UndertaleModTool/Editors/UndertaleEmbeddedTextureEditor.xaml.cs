using Microsoft.Win32;
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
using System.Drawing;
using UndertaleModLib.Models;
using UndertaleModLib.Util;

namespace UndertaleModTool
{
    /// <summary>
    /// Logika interakcji dla klasy UndertaleEmbeddedTextureEditor.xaml
    /// </summary>
    public partial class UndertaleEmbeddedTextureEditor : UserControl
    {
        public UndertaleEmbeddedTextureEditor()
        {
            InitializeComponent();
        }

        private void Import_Click(object sender, RoutedEventArgs e)
        {
            UndertaleEmbeddedTexture target = DataContext as UndertaleEmbeddedTexture;

            OpenFileDialog dlg = new OpenFileDialog();

            dlg.DefaultExt = ".png";
            dlg.Filter = "PNG files (.png)|*.png|All files|*";

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    Bitmap bmp;
                    using (var ms = new MemoryStream(TextureWorker.ReadTextureBlob(dlg.FileName)))
                    {
                        bmp = new Bitmap(ms);
                    }
					bmp.SetResolution(96.0F, 96.0F);

                    var width = (uint)bmp.Width;
                    var height = (uint)bmp.Height;
                    
                    if ((width & (width - 1)) != 0 || (height & (height - 1)) != 0)
                    {
                        MessageBox.Show("WARNING: texture page dimensions are not powers of 2. Sprite blurring is very likely in game.", "Unexpected texture dimensions", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }

                    using (var stream = new MemoryStream())
                    {
                        bmp.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                        target.TextureData.TextureBlob = stream.ToArray();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to import file: " + ex.Message, "Failed to import file", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            UndertaleEmbeddedTexture target = DataContext as UndertaleEmbeddedTexture;

            SaveFileDialog dlg = new SaveFileDialog();

            dlg.DefaultExt = ".png";
            dlg.Filter = "PNG files (.png)|*.png|All files|*";

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    File.WriteAllBytes(dlg.FileName, target.TextureData.TextureBlob);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to export file: " + ex.Message, "Failed to export file", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var pos = e.GetPosition(sender as IInputElement);
            var tex = this.DataContext as UndertaleEmbeddedTexture;
            var tpag = (Application.Current.MainWindow as MainWindow).Data.TexturePageItems.Where((x) =>
            {
                if (x.TexturePage != tex)
                    return false;
                return pos.X > x.SourceX && pos.X < x.SourceX + x.SourceWidth && pos.Y > x.SourceY && pos.Y < x.SourceY + x.SourceHeight;
            }).FirstOrDefault();
            if (tpag != null)
                (Application.Current.MainWindow as MainWindow).ChangeSelection(tpag);
        }
    }
}
