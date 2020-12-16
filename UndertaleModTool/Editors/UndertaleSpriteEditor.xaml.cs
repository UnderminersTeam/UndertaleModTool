using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
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
using UndertaleModLib.Util;

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

        private void ExportAll_Click(object sender, RoutedEventArgs e)
        {
            UndertaleSprite sprite = this.DataContext as UndertaleSprite;

            SaveFileDialog dlg = new SaveFileDialog();

            dlg.FileName = sprite.Name.Content + ".png";
            dlg.DefaultExt = ".png";
            dlg.Filter = "PNG files (.png)|*.png|All files|*";

            TextureWorker worker = new TextureWorker();

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    if (sprite.Textures.Count > 1)
                    {
                        string dir = System.IO.Path.GetDirectoryName(dlg.FileName);
                        string name = System.IO.Path.GetFileNameWithoutExtension(dlg.FileName);
                        string path = System.IO.Path.Combine(dir, name);
                        string ext = System.IO.Path.GetExtension(dlg.FileName);

                        Directory.CreateDirectory(path);
                        foreach (var tex in sprite.Textures.Select((tex, id) => new { id, tex }))
                        {
                            try
                            {
                                worker.ExportAsPNG(tex.tex.Texture, System.IO.Path.Combine(path, tex.id + ext));
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("Failed to export file: " + ex.Message, "Failed to export file", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                    else if (sprite.Textures.Count == 1)
                    {
                        try
                        {
                            worker.ExportAsPNG(sprite.Textures[0].Texture, dlg.FileName);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Failed to export file: " + ex.Message, "Failed to export file", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    else
                    {
                        MessageBox.Show("No frames to export", "Failed to export sprite", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to export: " + ex.Message, "Failed to export sprite", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            worker.Cleanup();
        }

        private void MaskList_AddingNewItem(object sender, AddingNewItemEventArgs e)
        {
            e.NewItem = (this.DataContext as UndertaleSprite).NewMaskEntry();
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
                    System.Drawing.Image img = System.Drawing.Image.FromFile(dlg.FileName);
                    if ((sprite.Width != (uint)img.Width) || (sprite.Height != (uint)img.Height))
                        throw new System.Exception(dlg.FileName + " is not the proper size to be imported! Please correct this before importing! The proper dimensions are width: " + sprite.Width.ToString() + " px, height: " + sprite.Height.ToString() + " px.");
                    target.Data = TextureWorker.ReadMaskData(dlg.FileName);
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
                    TextureWorker.ExportCollisionMaskPNG(sprite, target, dlg.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to export file: " + ex.Message, "Failed to export file", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
