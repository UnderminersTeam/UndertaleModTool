using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// Logika interakcji dla klasy UndertaleSpriteEditor.xaml
    /// </summary>
    public partial class UndertaleSpriteEditor : UserControl
    {
        public UndertaleSpriteEditor()
        {
            InitializeComponent();
        }

        private void MaskList_AddingNewItem(object sender, AddingNewItemEventArgs e)
        {
            UndertaleSprite.MaskEntry obj = new UndertaleSprite.MaskEntry();
            UndertaleSprite sprite = this.DataContext as UndertaleSprite;
            uint len = (sprite.Width + 7) / 8 * sprite.Height;
            obj.Data = new byte[len];
            e.NewItem = obj;
        }

        private void MaskImport_Click(object sender, RoutedEventArgs e)
        {
            UndertaleSprite sprite = this.DataContext as UndertaleSprite;
            UndertaleSprite.MaskEntry target = (sender as Button).DataContext as UndertaleSprite.MaskEntry;

            OpenFileDialog dlg = new OpenFileDialog();

            dlg.DefaultExt = ".png";
            dlg.Filter = "PNG files (.png)|*.png|All files|*";

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    using (FileStream stream = new FileStream(dlg.FileName, FileMode.Open))
                    {
                        PngBitmapDecoder decoder = new PngBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
                        BitmapSource source = decoder.Frames[0];
                        if (source.Format.BitsPerPixel != 1)
                            throw new Exception("Must be a 1 bit-per-pixel image");
                        if (source.PixelWidth != sprite.Width || source.PixelHeight != sprite.Height)
                            throw new Exception("Mask size doesn't match sprite size");
                        int stride = (int)((sprite.Width + 7) / 8);
                        byte[] data = new byte[source.PixelHeight * stride];
                        source.CopyPixels(data, stride, 0);
                        target.Data = data;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to import file: " + ex.Message, "Failed to import file", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void MaskExport_Click(object sender, RoutedEventArgs e)
        {
            UndertaleSprite sprite = this.DataContext as UndertaleSprite;
            UndertaleSprite.MaskEntry target = (sender as Button).DataContext as UndertaleSprite.MaskEntry;

            SaveFileDialog dlg = new SaveFileDialog();

            dlg.DefaultExt = ".png";
            dlg.Filter = "PNG files (.png)|*.png|All files|*";

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    BitmapSource source = BitmapSource.Create((int)sprite.Width, (int)sprite.Height, 96, 96, PixelFormats.BlackWhite, null, target.Data, (int)((sprite.Width + 7) / 8));
                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(source));
                    using (FileStream stream = new FileStream(dlg.FileName, FileMode.Create))
                    {
                        encoder.Save(stream);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to export file: " + ex.Message, "Failed to export file", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
