using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using UndertaleModLib;
using UndertaleModLib.Models;
using UndertaleModLib.Util;
using WpfAnimatedGif;

namespace UndertaleModTool
{
    /// <summary>
    /// Logika interakcji dla klasy UndertaleSpriteEditor.xaml
    /// </summary>
    public partial class UndertaleSpriteEditor : DataUserControl
    {
        private static readonly MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

        public UndertaleSpriteEditor()
        {
            InitializeComponent();

            ((System.Windows.Controls.Image)mainWindow.FindName("Flowey")).Opacity = 0;
            ((System.Windows.Controls.Image)mainWindow.FindName("FloweyLeave")).Opacity = 0;
            ((System.Windows.Controls.Image)mainWindow.FindName("FloweyBubble")).Opacity = 0;

            ((Label)this.FindName("SpritesObjectLabel")).Content = ((Label)mainWindow.FindName("ObjectLabel")).Content;
        }
        private void UndertaleSpritesEditor_Unloaded(object sender, RoutedEventArgs e)
        {
            var floweranim = ((System.Windows.Controls.Image)mainWindow.FindName("Flowey"));
            //floweranim.Opacity = 1;

            var controller = ImageBehavior.GetAnimationController(floweranim);
            controller.Pause();
            controller.GotoFrame(controller.FrameCount - 5);
            controller.Play();

            ((System.Windows.Controls.Image)mainWindow.FindName("FloweyLeave")).Opacity = 0;
        }
        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            UndertaleSprite code = this.DataContext as UndertaleSprite;

            int foundIndex = code is UndertaleResource res ? mainWindow.Data.IndexOf(res, false) : -1;
            string idString;

            if (foundIndex == -1)
                idString = "None";
            else if (foundIndex == -2)
                idString = "N/A";
            else
                idString = Convert.ToString(foundIndex);

            ((Label)this.FindName("SpritesObjectLabel")).Content = idString;

            //((Image)mainWindow.FindName("FloweyBubble")).Opacity = 0;
            //((Image)mainWindow.FindName("Flowey")).Opacity = 0;
        }

        private void ExportAllSpine(SaveFileDialog dlg, UndertaleSprite sprite)
        {
            mainWindow.ShowWarning("This seems to be a Spine sprite, .json and .atlas files will be exported together with the frames. " +
                                 "PLEASE EDIT THEM CAREFULLY! SOME MANUAL EDITING OF THE JSON MAY BE REQUIRED! THE DATA IS EXPORTED AS-IS.", "Spine warning");

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    string dir = Path.GetDirectoryName(dlg.FileName);
                    string name = Path.GetFileNameWithoutExtension(dlg.FileName);
                    string path = Path.Combine(dir, name);
                    string ext = Path.GetExtension(dlg.FileName);

                    if (sprite.SpineTextures.Count > 0)
                    {
                        Directory.CreateDirectory(path);

                        // textures
                        if (sprite.SpineHasTextureData)
                        {
                            foreach (var tex in sprite.SpineTextures.Select((tex, id) => new { id, tex }))
                            {
                                try
                                {
                                    File.WriteAllBytes(Path.Combine(path, tex.id + ext), tex.tex.TexBlob);
                                } 
                                catch (Exception ex) 
                                {
                                    mainWindow.ShowError("Failed to export file: " + ex.Message, "Failed to export file");
                                }
                            }
                        }

                        // json and atlas
                        File.WriteAllText(Path.Combine(path, "spine.json"), sprite.SpineJSON);
                        File.WriteAllText(Path.Combine(path, "spine.atlas"), sprite.SpineAtlas);
                    }
                }
                catch (Exception ex)
                {
                    mainWindow.ShowError("Failed to export: " + ex.Message, "Failed to export sprite");
                }
            }
        }

        private void ExportAll_Click(object sender, RoutedEventArgs e)
        {
            UndertaleSprite sprite = this.DataContext as UndertaleSprite;

            SaveFileDialog dlg = new SaveFileDialog();

            dlg.FileName = sprite.Name.Content + ".png";
            dlg.DefaultExt = ".png";
            dlg.Filter = "PNG files (.png)|*.png|All files|*";

            if (sprite.IsSpineSprite)
            {
                ExportAllSpine(dlg, sprite);
                if (sprite.SpineHasTextureData)
                    return;
            }

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    bool includePadding = (mainWindow.ShowQuestion("Include padding?") == MessageBoxResult.Yes);

                    using TextureWorker worker = new();
                    if (sprite.Textures.Count > 1)
                    {
                        string dir = Path.GetDirectoryName(dlg.FileName);
                        string name = Path.GetFileNameWithoutExtension(dlg.FileName);
                        string path = Path.Combine(dir, name);
                        string ext = Path.GetExtension(dlg.FileName);

                        Directory.CreateDirectory(path);
                        foreach (var tex in sprite.Textures.Select((tex, id) => new { id, tex }))
                        {
                            try
                            {
                                worker.ExportAsPNG(tex.tex.Texture, Path.Combine(path, sprite.Name.Content + "_" + tex.id + ext), null, includePadding);
                            }
                            catch (Exception ex)
                            {
                                mainWindow.ShowError("Failed to export file: " + ex.Message, "Failed to export file");
                            }
                        }
                    }
                    else if (sprite.Textures.Count == 1)
                    {
                        try
                        {
                            worker.ExportAsPNG(sprite.Textures[0].Texture, dlg.FileName, null, includePadding);
                        }
                        catch (Exception ex)
                        {
                            mainWindow.ShowError("Failed to export file: " + ex.Message, "Failed to export file");
                        }
                    }
                    else
                    {
                        mainWindow.ShowError("No frames to export", "Failed to export sprite");
                    }
                }
                catch (Exception ex)
                {
                    mainWindow.ShowError("Failed to export: " + ex.Message, "Failed to export sprite");
                }
            }
        }

        private void MaskList_AddingNewItem(object sender, AddingNewItemEventArgs e)
        {
            e.NewItem = (this.DataContext as UndertaleSprite).NewMaskEntry(mainWindow.Data);
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
                    (int maskWidth, int maskHeight) = sprite.CalculateMaskDimensions(mainWindow.Data);
                    target.Data = TextureWorker.ReadMaskData(dlg.FileName, maskWidth, maskHeight);
                    target.Width = maskWidth;
                    target.Height = maskHeight;
                }
                catch (Exception ex)
                {
                    mainWindow.ShowError("Failed to import file: " + ex.Message, "Failed to import file");
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
                    (int maskWidth, int maskHeight) = sprite.CalculateMaskDimensions(mainWindow.Data);
                    TextureWorker.ExportCollisionMaskPNG(target, dlg.FileName, maskWidth, maskHeight);
                }
                catch (Exception ex)
                {
                    mainWindow.ShowError("Failed to export file: " + ex.Message, "Failed to export file");
                }
            }
        }

        private void UndertaleObjectReference_Loaded(object sender, RoutedEventArgs e)
        {
            var objRef = sender as UndertaleObjectReference;

            objRef.ClearRemoveClickHandler();
            objRef.RemoveButton.Click += Remove_Click_Override;
            objRef.RemoveButton.ToolTip = "Remove texture entry";
            objRef.RemoveButton.IsEnabled = true;
            objRef.DetailsButton.ToolTip = "Open texture entry";
            objRef.ObjectText.PreviewKeyDown += ObjectText_PreviewKeyDown;
        }
        private void ObjectText_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
                Remove_Click_Override(sender, null);
        }
        private void Remove_Click_Override(object sender, RoutedEventArgs e)
        {
            if (DataContext is UndertaleSprite sprite && (sender as FrameworkElement).DataContext is UndertaleSprite.TextureEntry entry)
                sprite.Textures.Remove(entry);
        }
        private void RemoveMask_Clicked(object sender, RoutedEventArgs e)
        {
            if (DataContext is UndertaleSprite sprite && (sender as FrameworkElement).DataContext is UndertaleSprite.MaskEntry entry)
                sprite.CollisionMasks.Remove(entry);
        }
    }
}
