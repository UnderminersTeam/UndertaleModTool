using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using UndertaleModLib.Models;
using UndertaleModLib.Util;

namespace UndertaleModTool
{
    /// <summary>
    /// Logika interakcji dla klasy UndertaleSpriteEditor.xaml
    /// </summary>
    public partial class UndertaleSpriteEditor : DataUserControl
    {
        private static readonly MainWindow mainWindow = Application.Current.MainWindow as MainWindow;
        private double currentZoom = 1.0;
        private bool isDragging = false;
        private Point lastMousePosition;
        private double defaultScrollViewerHeight = 200;
        private double userResizedHeight = 200;

        public UndertaleSpriteEditor()
        {
            InitializeComponent();
            UpdateZoomDisplay();
            
            SpriteTextureScrollViewer.Height = defaultScrollViewerHeight;
            SpriteTextureScrollViewer.MaxHeight = double.PositiveInfinity;
            userResizedHeight = defaultScrollViewerHeight;

            SpriteTextureDisplay.DataContextChanged += (s, e) => UpdateScrollViewerSize();
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

        private void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            currentZoom = Math.Min(currentZoom * 1.2, 10.0); // Max 10x zoom
            ApplyZoom();
        }

        private void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            currentZoom = Math.Max(currentZoom / 1.2, 0.1); // Min 0.1x zoom
            ApplyZoom();
        }

        private void FitToView_Click(object sender, RoutedEventArgs e)
        {
            if (SpriteTextureDisplay.DataContext == null) return;

            var scrollViewer = SpriteTextureScrollViewer;
            var display = SpriteTextureDisplay;
            
            display.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var actualSize = display.DesiredSize;
            
            if (actualSize.Width > 0 && actualSize.Height > 0)
            {
                var availableSize = new Size(Math.Max(300, scrollViewer.ActualWidth - 20), Math.Max(200, scrollViewer.ActualHeight - 20));
                var scaleX = availableSize.Width / actualSize.Width;
                var scaleY = availableSize.Height / actualSize.Height;
                currentZoom = Math.Min(scaleX, scaleY);
                currentZoom = Math.Max(0.1, Math.Min(currentZoom, 10.0));
                ApplyZoom();
            }
        }

        private void ActualSize_Click(object sender, RoutedEventArgs e)
        {
            currentZoom = 1.0;
            ApplyZoom();
        }

        private void ApplyZoom()
        {
            var scaleTransform = SpriteTextureContainer.RenderTransform as ScaleTransform;
            if (scaleTransform != null)
            {
                scaleTransform.ScaleX = currentZoom;
                scaleTransform.ScaleY = currentZoom;
                
                UpdateScrollViewerSize();
            }
            UpdateZoomDisplay();
        }

        private void UpdateScrollViewerSize()
        {
            if (SpriteTextureDisplay.DataContext != null)
            {
                SpriteTextureDisplay.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                var textureSize = SpriteTextureDisplay.DesiredSize;
                
                if (textureSize.Width > 0 && textureSize.Height > 0)
                {
                    var scaledHeight = textureSize.Height * currentZoom;
                    var paddingBuffer = 40;
                    
                    var neededHeight = scaledHeight + paddingBuffer;
                    
                    var targetHeight = Math.Max(userResizedHeight, Math.Min(800, neededHeight));
                    
                    if (Math.Abs(SpriteTextureScrollViewer.Height - targetHeight) > 10 || double.IsNaN(SpriteTextureScrollViewer.Height))
                    {
                        SpriteTextureScrollViewer.Height = targetHeight;
                        SpriteTextureScrollViewer.MaxHeight = double.PositiveInfinity;
                    }
                }
            }
            else
            {
                SpriteTextureScrollViewer.Height = userResizedHeight;
                SpriteTextureScrollViewer.MaxHeight = double.PositiveInfinity;
            }
        }

        private void UpdateZoomDisplay()
        {
            SpriteTextureContainer.Tag = $"{currentZoom:P0}";
        }

        private void SpriteTextureScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (e.Delta > 0)
                {
                    currentZoom = Math.Min(currentZoom * 1.1, 10.0);
                }
                else
                {
                    currentZoom = Math.Max(currentZoom / 1.1, 0.1);
                }
                ApplyZoom();
                e.Handled = true;
            }
        }

        private void SpriteTextureContainer_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                isDragging = true;
                lastMousePosition = e.GetPosition(SpriteTextureScrollViewer);
                SpriteTextureContainer.CaptureMouse();
                SpriteTextureContainer.Cursor = Cursors.SizeAll;
                e.Handled = true;
            }
        }

        private void SpriteTextureContainer_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                var currentPosition = e.GetPosition(SpriteTextureScrollViewer);
                var deltaY = currentPosition.Y - lastMousePosition.Y;
                
                var zoomChange = deltaY * 0.01;
                var newZoom = Math.Max(0.1, Math.Min(currentZoom + zoomChange, 10.0));
                
                if (Math.Abs(newZoom - currentZoom) > 0.01)
                {
                    currentZoom = newZoom;
                    ApplyZoom();
                    lastMousePosition = currentPosition;
                }
                e.Handled = true;
            }
            else if (!isDragging)
            {
                SpriteTextureContainer.Cursor = Cursors.SizeNWSE;
            }
        }

        private void SpriteTextureContainer_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (isDragging)
            {
                isDragging = false;
                SpriteTextureContainer.ReleaseMouseCapture();
                SpriteTextureContainer.Cursor = Cursors.SizeNWSE;
                e.Handled = true;
            }
        }

        private void SpriteTextureContainer_MouseLeave(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                isDragging = false;
                SpriteTextureContainer.ReleaseMouseCapture();
            }
            SpriteTextureContainer.Cursor = Cursors.Hand;
        }
    }
}
