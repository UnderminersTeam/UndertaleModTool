using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using UndertaleModLib.Models;
using UndertaleModLib.Util;
using UndertaleModTool.Localization;
using static UndertaleModLib.Models.UndertaleSprite;

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
            DataContextChanged += OnDataContextChanged;
            Unloaded += OnUnloaded;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is UndertaleSprite oldObj)
            {
                oldObj.PropertyChanged -= OnPropertyChanged;
            }
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is UndertaleSprite oldObj)
            {
                oldObj.PropertyChanged -= OnPropertyChanged;
            }
            if (e.NewValue is UndertaleSprite newObj)
            {
                newObj.PropertyChanged += OnPropertyChanged;
            }
        }

        private void UndertaleObjectReference_SourceUpdated(object sender, System.Windows.Data.DataTransferEventArgs e)
        {
            OnAssetUpdated();
        }

        private void UndertaleObjectReference_ObjectReferenceChanged(object sender, UndertaleObjectReference.ObjectReferenceChangedEventArgs e)
        {
            OnAssetUpdated();
        }

        private void TextureList_Loaded(object sender, RoutedEventArgs e)
        {
            // Attach to collection changed events
            if (sender is not DataGrid dg || dg.ItemsSource is not ObservableCollection<TextureEntry> collection)
            {
                return;
            }
            collection.CollectionChanged += DataGrid_CollectionChanged;
        }

        private void TextureList_Unloaded(object sender, RoutedEventArgs e)
        {
            // Detach to collection changed events
            if (sender is not DataGrid dg || dg.ItemsSource is not ObservableCollection<TextureEntry> collection)
            {
                return;
            }
            collection.CollectionChanged -= DataGrid_CollectionChanged;
        }

        private void MaskList_Loaded(object sender, RoutedEventArgs e)
        {
            // Attach to collection changed events
            if (sender is not DataGrid dg || dg.ItemsSource is not ObservableCollection<MaskEntry> collection)
            {
                return;
            }
            collection.CollectionChanged += DataGrid_CollectionChanged;
        }

        private void MaskList_Unloaded(object sender, RoutedEventArgs e)
        {
            // Detach to collection changed events
            if (sender is not DataGrid dg || dg.ItemsSource is not ObservableCollection<MaskEntry> collection)
            {
                return;
            }
            collection.CollectionChanged -= DataGrid_CollectionChanged;
        }

        private void DataGrid_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnAssetUpdated();
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnAssetUpdated();
        }

        private void OnAssetUpdated()
        {
            if (mainWindow.Project is null || !mainWindow.IsSelectedProjectExportable)
            {
                return;
            }
            Dispatcher.BeginInvoke(() =>
            {
                if (DataContext is UndertaleSprite obj)
                {
                    mainWindow.Project?.MarkAssetForExport(obj);
                }
            });
        }

        private void ExportAllSpine(SaveFileDialog dlg, UndertaleSprite sprite)
        {
            mainWindow.ShowWarning(LocalizationSource.GetString("Msg_SpineSpriteWarning"), LocalizationSource.GetString("Dialog_SpineWarning"));

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    string dir = Path.GetDirectoryName(dlg.FileName);
                    string name = Path.GetFileNameWithoutExtension(dlg.FileName);
                    string path = Path.Join(dir, name);
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
                                    File.WriteAllBytes(Paths.JoinVerifyWithinDirectory(path, tex.id + ext), tex.tex.TexBlob);
                                } 
                                catch (Exception ex) 
                                {
                                    mainWindow.ShowError(string.Format(LocalizationSource.GetString("Msg_FailedToExportFile"), ex.Message), LocalizationSource.GetString("Dialog_FailedToExportFile"));
                                }
                            }
                        }

                        // json and atlas
                        File.WriteAllText(Path.Join(path, "spine.json"), sprite.SpineJSON);
                        File.WriteAllText(Path.Join(path, "spine.atlas"), sprite.SpineAtlas);
                    }
                }
                catch (Exception ex)
                {
                    mainWindow.ShowError(string.Format(LocalizationSource.GetString("Msg_FailedToExportSprite"), ex.Message), LocalizationSource.GetString("Dialog_FailedToExportSprite"));
                }
            }
        }

        private void ExportAll_Click(object sender, RoutedEventArgs e)
        {
            UndertaleSprite sprite = this.DataContext as UndertaleSprite;

            SaveFileDialog dlg = new SaveFileDialog();

            dlg.FileName = sprite.Name.Content + ".png";
            dlg.DefaultExt = ".png";
            dlg.Filter = LocalizationSource.GetString("FileFilter_PNG") + "|*.png|" + LocalizationSource.GetString("FileFilter_AllFiles") + "|*";

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
                    bool includePadding = (mainWindow.ShowQuestion(LocalizationSource.GetString("Msg_IncludePadding")) == MessageBoxResult.Yes);

                    using TextureWorker worker = new();
                    if (sprite.Textures.Count > 1)
                    {
                        string dir = Path.GetDirectoryName(dlg.FileName);
                        string name = Path.GetFileNameWithoutExtension(dlg.FileName);
                        string path = Path.Join(dir, name);
                        string ext = Path.GetExtension(dlg.FileName);

                        Directory.CreateDirectory(path);
                        foreach (var tex in sprite.Textures.Select((tex, id) => new { id, tex }))
                        {
                            try
                            {
                                worker.ExportAsPNG(tex.tex.Texture, Paths.JoinVerifyWithinDirectory(path, sprite.Name.Content + "_" + tex.id + ext), null, includePadding);
                            }
                            catch (Exception ex)
                            {
                                mainWindow.ShowError(string.Format(LocalizationSource.GetString("Msg_FailedToExportFile"), ex.Message), LocalizationSource.GetString("Dialog_FailedToExportFile"));
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
                            mainWindow.ShowError(string.Format(LocalizationSource.GetString("Msg_FailedToExportFile"), ex.Message), LocalizationSource.GetString("Dialog_FailedToExportFile"));
                        }
                    }
                    else
                    {
                        mainWindow.ShowError(LocalizationSource.GetString("Msg_NoFramesToExport"), LocalizationSource.GetString("Dialog_FailedToExportSprite"));
                    }
                }
                catch (Exception ex)
                {
                    mainWindow.ShowError(string.Format(LocalizationSource.GetString("Msg_FailedToExportSprite"), ex.Message), LocalizationSource.GetString("Dialog_FailedToExportSprite"));
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
            dlg.Filter = LocalizationSource.GetString("FileFilter_PNG") + "|*.png|" + LocalizationSource.GetString("FileFilter_AllFiles") + "|*";

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    (int maskWidth, int maskHeight) = sprite.CalculateMaskDimensions(mainWindow.Data);
                    target.Data = TextureWorker.ReadMaskData(dlg.FileName, maskWidth, maskHeight);
                    target.Width = maskWidth;
                    target.Height = maskHeight;

                    OnAssetUpdated();
                }
                catch (Exception ex)
                {
                    mainWindow.ShowError(string.Format(LocalizationSource.GetString("Msg_FailedToImportFile"), ex.Message), LocalizationSource.GetString("Dialog_FailedToImportFile"));
                }
            }
        }

        private void MaskExport_Click(object sender, RoutedEventArgs e)
        {
            UndertaleSprite sprite = this.DataContext as UndertaleSprite;
            UndertaleSprite.MaskEntry target = (sender as Button).DataContext as UndertaleSprite.MaskEntry;

            SaveFileDialog dlg = new SaveFileDialog();

            dlg.DefaultExt = ".png";
            dlg.Filter = LocalizationSource.GetString("FileFilter_PNG") + "|*.png|" + LocalizationSource.GetString("FileFilter_AllFiles") + "|*";

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    (int maskWidth, int maskHeight) = sprite.CalculateMaskDimensions(mainWindow.Data);
                    TextureWorker.ExportCollisionMaskPNG(target, dlg.FileName, maskWidth, maskHeight);
                }
                catch (Exception ex)
                {
                    mainWindow.ShowError(string.Format(LocalizationSource.GetString("Msg_FailedToExportFile"), ex.Message), LocalizationSource.GetString("Dialog_FailedToExportFile"));
                }
            }
        }

        private void UndertaleObjectReference_Loaded(object sender, RoutedEventArgs e)
        {
            var objRef = sender as UndertaleObjectReference;

            objRef.ClearRemoveClickHandler();
            objRef.RemoveButton.Click += Remove_Click_Override;
            objRef.RemoveButton.ToolTip = LocalizationSource.GetString("Editor_RemoveTextureEntry");
            objRef.RemoveButton.IsEnabled = true;
            objRef.DetailsButton.ToolTip = LocalizationSource.GetString("Editor_OpenTextureEntry");
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
