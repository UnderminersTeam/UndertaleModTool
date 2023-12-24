using Microsoft.Win32;
using System;
using System.Drawing;
using System.Windows;
using System.IO;
using UndertaleModLib.Models;
using UndertaleModLib.Util;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Data;
using UndertaleModTool.Windows;

namespace UndertaleModTool
{
    /// <summary>
    /// Logika interakcji dla klasy UndertaleTexturePageItemEditor.xaml
    /// </summary>
    public partial class UndertaleTexturePageItemEditor : DataUserControl
    {
        private static readonly MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

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
                SkiaSharp.SKBitmap image = TextureWorker.ReadImageFromFile(dlg.FileName);
                (this.DataContext as UndertaleTexturePageItem).ReplaceTexture(image);

                // Refresh the image of "ItemDisplay"
                if (ItemDisplay.FindName("RenderAreaBorder") is not Border border)
                    return;
                if (border.Background is not ImageBrush brush)
                    return;
                BindingOperations.GetBindingExpression(brush, ImageBrush.ImageSourceProperty)?.UpdateTarget();
            }
            catch (Exception ex)
            {
                mainWindow.ShowError(ex.Message, "Failed to import image");
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
                    mainWindow.ShowError("Failed to export file: " + ex.Message, "Failed to export file");
                }
                worker.Cleanup();
            }
        }

        private void FindReferencesButton_Click(object sender, RoutedEventArgs e)
        {
            var obj = (sender as FrameworkElement)?.DataContext;
            if (obj is not UndertaleTexturePageItem item)
                return;

            FindReferencesTypesDialog dialog = null;
            try
            {
                dialog = new(item, mainWindow.Data);
                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                mainWindow.ShowError("An error occured in the object references related window.\n" +
                                     $"Please report this on GitHub.\n\n{ex}");
            }
            finally
            {
                dialog?.Close();
            }
        }
    }
}
