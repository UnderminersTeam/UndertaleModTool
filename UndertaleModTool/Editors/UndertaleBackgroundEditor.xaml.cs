using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using UndertaleModLib.Models;
using UndertaleModTool.Windows;

namespace UndertaleModTool
{
    /// <summary>
    /// Logika interakcji dla klasy UndertaleBackgroundEditor.xaml
    /// </summary>
    public partial class UndertaleBackgroundEditor : DataUserControl
    {
        private static readonly MainWindow mainWindow = Application.Current.MainWindow as MainWindow;
        private readonly ContextMenuDark tileContextMenu = new();

        public UndertaleBackgroundEditor()
        {
            InitializeComponent();

            var item = new MenuItem()
            {
                Header = "Find all references of this tile"
            };
            item.Click += FindAllTileReferencesItem_Click;
            tileContextMenu.Items.Add(item);
            
            DataContextChanged += OnDataContextChanged;
            Unloaded += OnUnloaded;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is UndertaleBackground oldObj)
            {
                oldObj.PropertyChanged -= OnPropertyChanged;
            }
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is UndertaleBackground oldObj)
            {
                oldObj.PropertyChanged -= OnPropertyChanged;
            }
            if (e.NewValue is UndertaleBackground newObj)
            {
                newObj.PropertyChanged += OnPropertyChanged;
            }
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
                if (DataContext is UndertaleBackground obj)
                {
                    mainWindow.Project?.MarkAssetForExport(obj);
                }
            });
        }

        private void DataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            // Attach to collection changed events
            if (sender is not DataGrid dg || dg.ItemsSource is not ObservableCollection<UndertaleBackground.TileID> collection)
            {
                return;
            }
            collection.CollectionChanged += DataGrid_CollectionChanged;
        }

        private void DataGrid_Unloaded(object sender, RoutedEventArgs e)
        {
            // Detach to collection changed events
            if (sender is not DataGrid dg || dg.ItemsSource is not ObservableCollection<UndertaleBackground.TileID> collection)
            {
                return;
            }
            collection.CollectionChanged -= DataGrid_CollectionChanged;
        }

        private void DataGrid_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnAssetUpdated();
        }

        private void TextBoxDark_TileID_SourceUpdated(object sender, System.Windows.Data.DataTransferEventArgs e)
        {
            OnAssetUpdated();
        }

        private void FindAllTileReferencesItem_Click(object sender, RoutedEventArgs e)
        {
            var tileSet = DataContext as UndertaleBackground;
            var selectedID = TileIdList.SelectedItem as UndertaleBackground.TileID;
            if (tileSet is null || selectedID is null)
                return;

            FindReferencesResults dialog = null;
            try
            {
                var typeList = new HashSetTypesOverride() { typeof(UndertaleRoom.Layer) };
                var tuple = (tileSet, selectedID);
                var results = UndertaleResourceReferenceMethodsMap.GetReferencesOfObject(tuple, mainWindow.Data, typeList);
                dialog = new(tuple, mainWindow.Data, results);
                dialog.Show();
            }
            catch (Exception ex)
            {
                mainWindow.ShowError("An error occurred in the object references related window.\n" +
                                    $"Please report this on GitHub.\n\n{ex}");
                dialog?.Close();
            }
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((sender as DataGrid).SelectedItem is UndertaleBackground.TileID tileID)
            {
                UndertaleBackground bg = DataContext as UndertaleBackground;
                uint x = tileID.ID % bg.GMS2TileColumns;
                uint y = tileID.ID / bg.GMS2TileColumns;

                Canvas.SetLeft(TileRectangle, ((x + 1) * bg.GMS2OutputBorderX) + (x * (bg.GMS2TileWidth + bg.GMS2OutputBorderX)));
                Canvas.SetTop(TileRectangle, ((y + 1) * bg.GMS2OutputBorderY) + (y * (bg.GMS2TileHeight + bg.GMS2OutputBorderY)));
            }
        }

        private bool ScrollTileIntoView(int tileIndex)
        {
            ScrollViewer tileListViewer = MainWindow.FindVisualChild<ScrollViewer>(TileIdList);
            if (tileListViewer is null)
            {
                mainWindow.ShowError("Cannot find the tile ID list scroll viewer.");
                return false;
            }
            tileListViewer.ScrollToVerticalOffset(tileIndex + 1 - (tileListViewer.ViewportHeight / 2)); // DataGrid offset is logical
            tileListViewer.UpdateLayout();

            ScrollViewer dataEditorViewer = mainWindow.DataEditor.Parent as ScrollViewer;
            double initOffset = dataEditorViewer?.VerticalOffset ?? 0;

            TileIdList.SelectedIndex = tileIndex;
            (TileIdList.ItemContainerGenerator.ContainerFromIndex(tileIndex) as DataGridRow)?.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));

            dataEditorViewer.UpdateLayout();
            dataEditorViewer.ScrollToVerticalOffset(initOffset);

            return true;
        }
        private bool SelectTileRegion(object sender, MouseButtonEventArgs e)
        {
            if (!TileRectangle.IsVisible) // MainWindow.IsGMS2
                return false;

            Point pos = e.GetPosition((IInputElement)sender);
            UndertaleBackground bg = DataContext as UndertaleBackground;
            int x = (int)((int)pos.X / (bg.GMS2TileWidth + (2 * bg.GMS2OutputBorderX)));
            int y = (int)((int)pos.Y / (bg.GMS2TileHeight + (2 * bg.GMS2OutputBorderY)));
            int tileID = (int)((bg.GMS2TileColumns * y) + x);
            if (tileID > bg.GMS2TileCount - 1)
                return false;

            e.Handled = true;

            int tileIndex = bg.GMS2TileIds.FindIndex(x => x.ID == tileID);
            if (tileIndex == -1)
                return false;

            return ScrollTileIntoView(tileIndex);
        }
        private void BGTexture_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SelectTileRegion(sender, e);
        }
        private void BGTexture_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!SelectTileRegion(sender, e))
                return;

            UpdateLayout();

            tileContextMenu.IsOpen = true;
        }

        private void DataUserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (IsLoaded)
                MainWindow.FindVisualChild<ScrollViewer>(TileIdList)?.ScrollToTop();
        }
    }
}
