using Microsoft.Win32;
using System;
using System.Windows;
using UndertaleModLib.Models;
using UndertaleModLib.Util;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Data;
using UndertaleModTool.Windows;
using ImageMagick;
using System.Windows.Media.Imaging;
using System.ComponentModel;

namespace UndertaleModTool
{
    /// <summary>
    /// Logika interakcji dla klasy UndertaleTexturePageItemEditor.xaml
    /// </summary>
    public partial class UndertaleTexturePageItemEditor : DataUserControl
    {
        private static readonly MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

        /// <summary>
        /// Handle on the texture page item we're listening for updates from.
        /// </summary>
        private UndertaleTexturePageItem _textureItemContext = null;

        /// <summary>
        /// Handle on the texture data where we're listening for updates from.
        /// </summary>
        private UndertaleEmbeddedTexture.TexData _textureDataContext = null;

        public UndertaleTexturePageItemEditor()
        {
            InitializeComponent();

            DataContextChanged += SwitchDataContext;
            Unloaded += UnloadTexture;
        }

        private void UpdateImages(UndertaleTexturePageItem item)
        {
            if (item.TexturePage?.TextureData?.Image is null)
            {
                ItemTextureBGImage.Source = null;
                ItemTextureImage.Source = null;
                return;
            }

            GMImage image = item.TexturePage.TextureData.Image;
            BitmapSource bitmap = mainWindow.GetBitmapSourceForImage(image);
            ItemTextureBGImage.Source = bitmap;
            ItemTextureImage.Source = bitmap;
        }

        private void SwitchDataContext(object sender, DependencyPropertyChangedEventArgs e)
        {
            UndertaleTexturePageItem item = (DataContext as UndertaleTexturePageItem);
            if (item is null)
                return;

            // Load current image
            UpdateImages(item);

            // Start listening for texture page updates
            if (_textureItemContext is not null)
            {
                _textureItemContext.PropertyChanged -= ReloadTexturePage;
            }
            _textureItemContext = item;
            _textureItemContext.PropertyChanged += ReloadTexturePage;

            // Start listening for texture image updates
            if (_textureDataContext is not null)
            {
                _textureDataContext.PropertyChanged -= ReloadTextureImage;
            }

            if (item.TexturePage?.TextureData is not null)
            {
                _textureDataContext = item.TexturePage.TextureData;
                _textureDataContext.PropertyChanged += ReloadTextureImage;
            }
        }

        private void ReloadTexturePage(object sender, PropertyChangedEventArgs e)
        {
            // Invoke dispatcher to only perform updates on UI thread
            Dispatcher.Invoke(() =>
            {
                UndertaleTexturePageItem item = (DataContext as UndertaleTexturePageItem);
                if (item is null)
                    return;

                if (e.PropertyName != nameof(UndertaleTexturePageItem.TexturePage))
                    return;

                UpdateImages(item);

                // Start listening for (new) texture image updates
                if (_textureDataContext is not null)
                {
                    _textureDataContext.PropertyChanged -= ReloadTextureImage;
                }
                _textureDataContext = item.TexturePage.TextureData;
                _textureDataContext.PropertyChanged += ReloadTextureImage;
            });
        }

        private void ReloadTextureImage(object sender, PropertyChangedEventArgs e)
        {
            // Invoke dispatcher to only perform updates on UI thread
            Dispatcher.Invoke(() =>
            {
                UndertaleTexturePageItem item = (DataContext as UndertaleTexturePageItem);
                if (item is null)
                    return;

                if (e.PropertyName != nameof(UndertaleEmbeddedTexture.TexData.Image))
                    return;

                // If the texture's image was updated, reload it
                UpdateImages(item);
            });
        }

        private void UnloadTexture(object sender, RoutedEventArgs e)
        {
            ItemTextureBGImage.Source = null;
            ItemTextureImage.Source = null;

            // Stop listening for texture page updates
            if (_textureItemContext is not null)
            {
                _textureItemContext.PropertyChanged -= ReloadTexturePage;
                _textureItemContext = null;
            }

            // Stop listening for texture image updates
            if (_textureDataContext is not null)
            {
                _textureDataContext.PropertyChanged -= ReloadTextureImage;
                _textureDataContext = null;
            }
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
                using MagickImage image = TextureWorker.ReadBGRAImageFromFile(dlg.FileName);
                UndertaleTexturePageItem item = DataContext as UndertaleTexturePageItem;
                item.ReplaceTexture(image);

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
                using TextureWorker worker = new();
                try
                {
                    worker.ExportAsPNG((UndertaleTexturePageItem)DataContext, dlg.FileName);
                }
                catch (Exception ex)
                {
                    mainWindow.ShowError("Failed to export file: " + ex.Message, "Failed to export file");
                }
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
