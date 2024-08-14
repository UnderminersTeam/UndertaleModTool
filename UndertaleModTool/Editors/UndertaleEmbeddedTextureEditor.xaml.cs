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
using System.Globalization;
using UndertaleModLib;
using UndertaleModTool.Windows;
using System.Windows.Threading;
using ImageMagick;

namespace UndertaleModTool
{
    /// <summary>
    /// Logika interakcji dla klasy UndertaleEmbeddedTextureEditor.xaml
    /// </summary>
    public partial class UndertaleEmbeddedTextureEditor : DataUserControl
    {
        private static readonly MainWindow mainWindow = Application.Current.MainWindow as MainWindow;
        private readonly ContextMenuDark pageContextMenu = new();
        private bool isMenuOpen;
        private UndertaleTexturePageItem[] items;
        private UndertaleTexturePageItem hoveredItem;

        public static (Transform Transform, double Left, double Top) OverriddenPreviewState { get; set; }

        public UndertaleEmbeddedTextureEditor()
        {
            InitializeComponent();

            var newTabItem = new MenuItem()
            {
                Header = "Open in new tab"
            };
            newTabItem.Click += OpenInNewTabItem_Click;
            var referencesItem = new MenuItem()
            {
                Header = "Find all references to this page item"
            };
            referencesItem.Click += FindAllItemReferencesItem_Click;
            pageContextMenu.Items.Add(newTabItem);
            pageContextMenu.Items.Add(referencesItem);

            pageContextMenu.Closed += PageContextMenu_Closed;

            DataContextChanged += ReloadTexture;
            Unloaded += UnloadTexture;
        }

        private void ReloadTexture(object sender, DependencyPropertyChangedEventArgs e)
        {
            UndertaleEmbeddedTexture texture = (DataContext as UndertaleEmbeddedTexture);
            if (texture is null)
                return;

            GMImage image = texture.TextureData.Image;
            BitmapSource bitmap = mainWindow.GetBitmapSourceForImage(image);
            TextureImageView.Source = bitmap;
        }

        private void UnloadTexture(object sender, RoutedEventArgs e)
        {
            TextureImageView.Source = null;
        }

        private void OpenInNewTabItem_Click(object sender, RoutedEventArgs e)
        {
            mainWindow.ChangeSelection((sender as FrameworkElement)?.DataContext, true);
        }
        private void FindAllItemReferencesItem_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not UndertaleTexturePageItem item)
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
        private void PageContextMenu_Closed(object sender, RoutedEventArgs e)
        {
            isMenuOpen = false;
            Grid_MouseLeave(null, null);
        }

        private void ScaleTextureToFit()
        {
            var scrollPres = MainWindow.FindVisualChild<ScrollContentPresenter>(TextureScroll);
            if (scrollPres is null)
                return;

            double initScale = 1;
            if (DataContext is UndertaleEmbeddedTexture texturePage)
            {
                int textureWidth = texturePage.TextureData?.Width ?? 1;
                if (textureWidth < scrollPres.ActualWidth)
                    initScale = scrollPres.ActualWidth / textureWidth;
            }

            Transform t;
            double top, left;
            if (OverriddenPreviewState == default)
            {
                t = new MatrixTransform(initScale, 0, 0, initScale, 0, 0);
                top = 0;
                left = 0;
            }
            else
            {
                t = OverriddenPreviewState.Transform;
                top = OverriddenPreviewState.Top;
                left = OverriddenPreviewState.Left;
            }

            TextureViewbox.LayoutTransform = t;
            TextureViewbox.UpdateLayout();
            TextureScroll.ScrollToVerticalOffset(top);
            TextureScroll.ScrollToHorizontalOffset(left);
        }
        private void DataUserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (DataContext is UndertaleEmbeddedTexture texturePage)
                items = mainWindow.Data.TexturePageItems.Where(x => x.TexturePage == texturePage).ToArray();

            if (!IsLoaded)
                return;
            // "UpdateLayout()" doesn't work here
            _ = Dispatcher.InvokeAsync(ScaleTextureToFit, DispatcherPriority.ContextIdle);
        }
        private void DataUserControl_Loaded(object sender, RoutedEventArgs e)
        {
            ScaleTextureToFit();
        }
        private void DataUserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            OverriddenPreviewState = default;
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
                    GMImage image;
                    if (System.IO.Path.GetExtension(dlg.FileName).Equals(".png", StringComparison.InvariantCultureIgnoreCase))
                    {
                        // Import PNG data verbatim, without attempting to modify it
                        image = GMImage.FromPng(File.ReadAllBytes(dlg.FileName), true).ConvertToFormat(target.TextureData.Image.Format);
                    }
                    else
                    {
                        // Import any file type
                        using var magickImage = new MagickImage(dlg.FileName);
                        magickImage.Format = MagickFormat.Rgba;
                        magickImage.Alpha(AlphaOption.Set);
                        magickImage.SetCompression(CompressionMethod.NoCompression);

                        // Import image
                        image = GMImage.FromMagickImage(magickImage).ConvertToFormat(target.TextureData.Image.Format);
                    }

                    // Check dimensions
                    uint width = (uint)image.Width, height = (uint)image.Height;
                    if ((width & (width - 1)) != 0 || (height & (height - 1)) != 0)
                    {
                        mainWindow.ShowWarning("WARNING: Texture page dimensions are not powers of 2. Sprite blurring is very likely in-game.", "Unexpected texture dimensions");
                    }

                    // Import image
                    target.TextureData.Image = image;

                    // Update image in UI
                    BitmapSource bitmap = mainWindow.GetBitmapSourceForImage(target.TextureData.Image);
                    TextureImageView.Source = bitmap;

                    // Update width/height properties in the UI
                    TexWidth.GetBindingExpression(TextBox.TextProperty)?.UpdateTarget();
                    TexHeight.GetBindingExpression(TextBox.TextProperty)?.UpdateTarget();
                }
                catch (Exception ex)
                {
                    mainWindow.ShowError("Failed to import file: " + ex.Message, "Failed to import file");
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
                    using FileStream fs = new(dlg.FileName, FileMode.Create);
                    fs.Write(target.TextureData.Image.ConvertToPng().ToSpan());
                }
                catch (Exception ex)
                {
                    mainWindow.ShowError("Failed to export file: " + ex.Message, "Failed to export file");
                }
            }
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (hoveredItem is null)
                return;

            if (e.ChangedButton == MouseButton.Right)
            {
                isMenuOpen = true;
                pageContextMenu.DataContext = hoveredItem;
                pageContextMenu.IsOpen = true;
                return;
            }
            
            mainWindow.ChangeSelection(hoveredItem, e.ChangedButton == MouseButton.Middle);
        }

        private void Grid_MouseMove(object sender, MouseEventArgs e)
        {
            var prevItem = hoveredItem;
            hoveredItem = null;

            var pos = e.GetPosition(sender as IInputElement);
            foreach (var item in items)
            {
                if (pos.X > item.SourceX && pos.X < item.SourceX + item.SourceWidth
                    && pos.Y > item.SourceY && pos.Y < item.SourceY + item.SourceHeight)
                {
                    hoveredItem = item;
                    break;
                }
            }

            if (hoveredItem is null)
            {
                PageItemBorder.Width = PageItemBorder.Height = 0;
                return;
            }

            if (prevItem == hoveredItem)
                return;

            PageItemBorder.Width = hoveredItem.SourceWidth;
            PageItemBorder.Height = hoveredItem.SourceHeight;
            Canvas.SetLeft(PageItemBorder, hoveredItem.SourceX);
            Canvas.SetTop(PageItemBorder, hoveredItem.SourceY);
        }
        private void Grid_MouseLeave(object sender, MouseEventArgs e)
        {
            if (isMenuOpen)
                return;

            PageItemBorder.Width = PageItemBorder.Height = 0;
            hoveredItem = null;
        }

        private void TextureScroll_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.ExtentHeightChange != 0 || e.ExtentWidthChange != 0)
            {
                double xMousePositionOnScrollViewer = Mouse.GetPosition(TextureScroll).X;
                double yMousePositionOnScrollViewer = Mouse.GetPosition(TextureScroll).Y;
                double offsetX = e.HorizontalOffset + xMousePositionOnScrollViewer;
                double offsetY = e.VerticalOffset + yMousePositionOnScrollViewer;

                double oldExtentWidth = e.ExtentWidth - e.ExtentWidthChange;
                double oldExtentHeight = e.ExtentHeight - e.ExtentHeightChange;

                double relx = offsetX / oldExtentWidth;
                double rely = offsetY / oldExtentHeight;

                offsetX = Math.Max(relx * e.ExtentWidth - xMousePositionOnScrollViewer, 0);
                offsetY = Math.Max(rely * e.ExtentHeight - yMousePositionOnScrollViewer, 0);

                TextureScroll.ScrollToHorizontalOffset(offsetX);
                TextureScroll.ScrollToVerticalOffset(offsetY);
            }
        }

        private void TextureViewbox_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;
            var mousePos = e.GetPosition(TextureViewbox);
            var transform = TextureViewbox.LayoutTransform as MatrixTransform;
            var matrix = transform.Matrix;
            var scale = e.Delta >= 0 ? 1.1 : (1.0 / 1.1); // choose appropriate scaling factor

            if ((matrix.M11 > 0.2 || (matrix.M11 <= 0.2 && scale > 1)) && (matrix.M11 < 3 || (matrix.M11 >= 3 && scale < 1)))
            {
                matrix.ScaleAtPrepend(scale, scale, mousePos.X, mousePos.Y);
            }
            TextureViewbox.LayoutTransform = new MatrixTransform(matrix);
        }
    }

    public class TextureLoadedWrapper : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Any(v => v == DependencyProperty.UnsetValue))
                return null;

            bool textureLoaded, textureExternal;
            try
            {
                textureLoaded = (bool)values[0];
                textureExternal = (bool)values[1];
            }
            catch
            {
                return null;
            }

            return (textureLoaded || !textureExternal) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
