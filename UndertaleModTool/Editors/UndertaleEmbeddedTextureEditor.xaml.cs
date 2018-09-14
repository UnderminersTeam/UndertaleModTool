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
using UndertaleModLib.Models;

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
                    byte[] data = File.ReadAllBytes(dlg.FileName);

                    // Make sure the file is valid PNG
                    // TODO: does that work?
                    using (var stream = new MemoryStream(data))
                    {
                        PngBitmapDecoder decoder = new PngBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
                    }

                    target.TextureData.TextureBlob = data;
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
