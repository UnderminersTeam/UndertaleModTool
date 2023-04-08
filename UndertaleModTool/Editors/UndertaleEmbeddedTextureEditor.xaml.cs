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
                Header = "Find all references of this page item"
            };
            referencesItem.Click += FindAllItemReferencesItem_Click;
            pageContextMenu.Items.Add(newTabItem);
            pageContextMenu.Items.Add(referencesItem);

            pageContextMenu.Closed += PageContextMenu_Closed;
        }

        private void OpenInNewTabItem_Click(object sender, RoutedEventArgs e)
        {
            mainWindow.ChangeSelection(hoveredItem, true);
        }
        private void FindAllItemReferencesItem_Click(object sender, RoutedEventArgs e)
        {
            if (hoveredItem is null)
                return;

            FindReferencesTypesDialog dialog = null;
            try
            {
                dialog = new(hoveredItem, mainWindow.Data);
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

        private void DataUserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (DataContext is not UndertaleEmbeddedTexture texturePage)
                return;

            items = mainWindow.Data.TexturePageItems.Where(x => x.TexturePage == texturePage).ToArray();
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
                        mainWindow.ShowWarning("WARNING: texture page dimensions are not powers of 2. Sprite blurring is very likely in game.", "Unexpected texture dimensions");
                    }

                    using (var stream = new MemoryStream())
                    {
                        bmp.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                        target.TextureData.TextureBlob = stream.ToArray();

                        TexWidth.GetBindingExpression(TextBox.TextProperty)?.UpdateTarget();
                        TexHeight.GetBindingExpression(TextBox.TextProperty)?.UpdateTarget();
                    }
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
                    File.WriteAllBytes(dlg.FileName, target.TextureData.TextureBlob);
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

    public class GeneratedMipsWrapper : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not UndertaleData data)
                return Visibility.Collapsed;

            return data.IsVersionAtLeast(2, 0, 6) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
