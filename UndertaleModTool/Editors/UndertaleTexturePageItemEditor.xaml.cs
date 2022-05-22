using Microsoft.Win32;
using System;
using System.Drawing;
using System.Windows;
using System.IO;
using UndertaleModLib.Models;
using UndertaleModLib.Util;

namespace UndertaleModTool
{
    /// <summary>
    /// Logika interakcji dla klasy UndertaleTexturePageItemEditor.xaml
    /// </summary>
    public partial class UndertaleTexturePageItemEditor : DataUserControl
    {
        public UndertaleTexturePageItemEditor()
        {
            InitializeComponent();
        }

        private void Import_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();

            dlg.DefaultExt = ".png";
            dlg.Filter = "PNG files (.png)|*.png|All files|*";

            if (!(dlg.ShowDialog() ?? false))
                return;

            try
            {
                Bitmap image = TextureWorker.ReadImageFromFile(dlg.FileName);
                image.SetResolution(96.0F, 96.0F);
                (this.DataContext as UndertaleTexturePageItem).ReplaceTexture(image);
            }
            catch (Exception ex)
            {
                MessageBox.Show(Application.Current.MainWindow as MainWindow, ex.Message, "Failed to import image", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            
            dlg.DefaultExt = ".png";
            dlg.Filter = "PNG files (.png)|*.png|All files|*";

            if (dlg.ShowDialog() == true)
            {
                TextureWorker worker = new TextureWorker();
                try
                {
                    worker.ExportAsPNG((UndertaleTexturePageItem)this.DataContext, dlg.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(Application.Current.MainWindow as MainWindow, "Failed to export file: " + ex.Message, "Failed to export file", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                worker.Cleanup();
            }
        }
    }
}
