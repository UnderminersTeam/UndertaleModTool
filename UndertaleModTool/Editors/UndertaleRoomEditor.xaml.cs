using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
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
using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModTool
{
    /// <summary>
    /// Logika interakcji dla klasy UndertaleRoomEditor.xaml
    /// </summary>
    public partial class UndertaleRoomEditor : UserControl
    {
        public static DependencyProperty PreviewPathProperty =
            DependencyProperty.Register("PreviewPath", typeof(UndertalePath),
                typeof(UndertaleRoomEditor),
                new FrameworkPropertyMetadata(null));

        public UndertalePath PreviewPath
        {
            get { return (UndertalePath)GetValue(PreviewPathProperty); }
            set { SetValue(PreviewPathProperty, value); }
        }

        public UndertaleRoomEditor()
        {
            InitializeComponent();

            Loaded += UndertaleRoomEditor_Loaded;
            DataContextChanged += UndertaleRoomEditor_DataContextChanged;
        }

        public void SaveImagePNG(Stream outfile)
        {
            var target = new RenderTargetBitmap((int)RoomGraphics.RenderSize.Width, (int)RoomGraphics.RenderSize.Height, 96, 96, PixelFormats.Pbgra32);
            target.Render(RoomGraphics);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(target));
            encoder.Save(outfile);
        }

        private void ExportAsPNG_Click(object sender, RoutedEventArgs e)
        {
            UndertaleRoom room = this.DataContext as UndertaleRoom;

            SaveFileDialog dlg = new SaveFileDialog();

            dlg.FileName = room.Name.Content + ".png";
            dlg.DefaultExt = ".png";
            dlg.Filter = "PNG files (.png)|*.png|All files|*";

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    using (var file = File.OpenWrite(dlg.FileName))
                    {
                        SaveImagePNG(file);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to export file: " + ex.Message, "Failed to export file", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void UndertaleRoomEditor_Loaded(object sender, RoutedEventArgs e)
        {
            RoomRootItem.IsSelected = true;
        }

        private void UndertaleRoomEditor_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            RoomRootItem.IsSelected = true;
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            // I can't bind it directly because then clicking on the headers makes WPF explode because it tries to attach the header as child of ObjectEditor
            // TODO: find some better workaround
            object sel = e.NewValue;
            if (sel == RoomRootItem)
            {
                ObjectEditor.Content = DataContext;
            }
            else if (sel is UndertaleObject)
            {
                ObjectEditor.Content = sel;
            }
        }

        private UndertaleObject movingObj;
        private double hotpointX, hotpointY;
        
        private void Rectangle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            UndertaleObject clickedObj = (sender as Rectangle).DataContext as UndertaleObject;
            UndertaleObject selectedObj = ObjectEditor.Content as UndertaleObject;

            SelectObject(clickedObj);

            var mousePos = e.GetPosition(RoomGraphics);
            if (selectedObj is UndertaleRoom.GameObject || selectedObj is UndertaleRoom.Tile)
            {
                movingObj = clickedObj;
                if (movingObj is UndertaleRoom.GameObject)
                {
                    hotpointX = mousePos.X - (movingObj as UndertaleRoom.GameObject).X;
                    hotpointY = mousePos.Y - (movingObj as UndertaleRoom.GameObject).Y;
                }
                else if (movingObj is UndertaleRoom.Tile)
                {
                    hotpointX = mousePos.X - (movingObj as UndertaleRoom.Tile).X;
                    hotpointY = mousePos.Y - (movingObj as UndertaleRoom.Tile).Y;
                }
            }
        }

        private void Rectangle_MouseMove(object sender, MouseEventArgs e)
        {
            if (movingObj != null)
            {
                var mousePos = e.GetPosition(RoomGraphics);

                int tgtX = (int)(mousePos.X - hotpointX);
                int tgtY = (int)(mousePos.Y - hotpointY);

                int gridSize = 5;
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                    gridSize = 20;
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                    gridSize = 10;
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                    gridSize = 1;

                // Snap to grid
                tgtX = ((tgtX + gridSize / 2) / gridSize) * gridSize;
                tgtY = ((tgtY + gridSize / 2) / gridSize) * gridSize;

                if (movingObj is UndertaleRoom.GameObject)
                {
                    (movingObj as UndertaleRoom.GameObject).X = tgtX;
                    (movingObj as UndertaleRoom.GameObject).Y = tgtY;
                }
                else if (movingObj is UndertaleRoom.Tile)
                {
                    (movingObj as UndertaleRoom.Tile).X = tgtX;
                    (movingObj as UndertaleRoom.Tile).Y = tgtY;
                }
            }
        }

        private void Rectangle_MouseUp(object sender, MouseButtonEventArgs e)
        {
            movingObj = null;
        }

        private void SelectObject(UndertaleObject obj)
        {
            // TODO: This sometimes fails to open objects in non-expanded tree
            // Note that this prefers to select the object inside a layer than the 'floating' one in GameObjects

            foreach (var child in (RoomObjectsTree.Items[0] as TreeViewItem).Items)
            {
                foreach (var layer in (child as TreeViewItem).Items)
                {
                    var layer_twi = (child as TreeViewItem).ItemContainerGenerator.ContainerFromItem(layer) as TreeViewItem;
                    if (layer_twi == null)
                        continue;
                    var obj_twi = layer_twi.ItemContainerGenerator.ContainerFromItem(obj) as TreeViewItem;
                    if (obj_twi != null)
                    {
                        obj_twi.BringIntoView();
                        obj_twi.Focus();
                        return;
                    }
                }
            }

            foreach (var child in (RoomObjectsTree.Items[0] as TreeViewItem).Items)
            {
                var twi = (child as TreeViewItem).ItemContainerGenerator.ContainerFromItem(obj) as TreeViewItem;
                if (twi != null)
                {
                    twi.BringIntoView();
                    twi.Focus();
                    return;
                }
            }
        }

        private void Canvas_DragOver(object sender, DragEventArgs e)
        {
            UndertaleObject sourceItem = e.Data.GetData(e.Data.GetFormats()[e.Data.GetFormats().Length - 1]) as UndertaleObject; // TODO: make this more reliable

            e.Effects = e.AllowedEffects.HasFlag(DragDropEffects.Link) && sourceItem != null && (sourceItem is UndertaleGameObject || sourceItem is UndertalePath) ? DragDropEffects.Link : DragDropEffects.None;
            e.Handled = true;
        }

        private void Canvas_Drop(object sender, DragEventArgs e)
        {
            UndertaleObject sourceItem = e.Data.GetData(e.Data.GetFormats()[e.Data.GetFormats().Length - 1]) as UndertaleObject;
            
            e.Effects = e.AllowedEffects.HasFlag(DragDropEffects.Link) && sourceItem != null && (sourceItem is UndertaleGameObject || sourceItem is UndertalePath) ? DragDropEffects.Link : DragDropEffects.None;
            if (e.Effects == DragDropEffects.Link)
            {
                if (sourceItem is UndertaleGameObject)
                {
                    UndertaleGameObject droppedObject = sourceItem as UndertaleGameObject;
                    var mousePos = e.GetPosition(RoomGraphics);

                    UndertaleRoom room = this.DataContext as UndertaleRoom;
                    UndertaleRoom.Layer layer = ObjectEditor.Content as UndertaleRoom.Layer;
                    if ((Application.Current.MainWindow as MainWindow).IsGMS2 == Visibility.Visible && layer == null)
                    {
                        MessageBox.Show("Must have a layer selected", "UndertaleModTool", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    if (layer != null && layer.InstancesData == null)
                    {
                        MessageBox.Show("Must be on an instances layer", "UndertaleModTool", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var obj = new UndertaleRoom.GameObject()
                    {
                        X = (int)mousePos.X,
                        Y = (int)mousePos.Y,
                        ObjectDefinition = droppedObject,
                        InstanceID = (Application.Current.MainWindow as MainWindow).Data.GeneralInfo.LastObj++
                    };
                    room.GameObjects.Add(obj);
                    if (layer != null)
                        layer.InstancesData.Instances.Add(obj);

                    SelectObject(obj);
                }

                if (sourceItem is UndertalePath)
                {
                    PreviewPath = sourceItem as UndertalePath;
                }
            }
            e.Handled = true;
        }

        private void RoomObjectsTree_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                UndertaleRoom room = this.DataContext as UndertaleRoom;
                UndertaleObject selectedObj = ObjectEditor.Content as UndertaleObject;
                
                if (selectedObj is UndertaleRoom.Background)
                {
                    UndertaleRoom.Background bg = selectedObj as UndertaleRoom.Background;
                    bg.Enabled = false;
                    bg.BackgroundDefinition = null;
                    ObjectEditor.Content = null;
                }
                else if (selectedObj is UndertaleRoom.View)
                {
                    UndertaleRoom.View view = selectedObj as UndertaleRoom.View;
                    view.Enabled = false;
                    ObjectEditor.Content = null;
                }
                else if (selectedObj is UndertaleRoom.Tile)
                {
                    UndertaleRoom.Tile tile = selectedObj as UndertaleRoom.Tile;
                    if ((Application.Current.MainWindow as MainWindow).IsGMS2 == Visibility.Visible)
                        foreach (var layer in room.Layers)
                            if (layer.AssetsData != null)
                                layer.AssetsData.LegacyTiles.Remove(tile);
                    room.Tiles.Remove(tile);
                    ObjectEditor.Content = null;
                }
                else if (selectedObj is UndertaleRoom.GameObject)
                {
                    UndertaleRoom.GameObject gameObj = selectedObj as UndertaleRoom.GameObject;
                    if ((Application.Current.MainWindow as MainWindow).IsGMS2 == Visibility.Visible)
                        foreach (var layer in room.Layers)
                            if (layer.InstancesData != null)
                                layer.InstancesData.Instances.Remove(gameObj);
                    room.GameObjects.Remove(gameObj);
                    ObjectEditor.Content = null;
                }
                else if (selectedObj is UndertaleRoom.Layer)
                {
                    UndertaleRoom.Layer layer = selectedObj as UndertaleRoom.Layer;
                    if (layer.InstancesData != null)
                        foreach (var go in layer.InstancesData.Instances)
                            room.GameObjects.Remove(go);
                    room.Layers.Remove(layer);
                    ObjectEditor.Content = null;
                }
            }
        }

        private void RoomObjectsTree_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            object sel = (sender as TreeView).SelectedItem;
            if (sel is UndertaleRoom.GameObject)
                (Application.Current.MainWindow as MainWindow).ChangeSelection((sel as UndertaleRoom.GameObject).ObjectDefinition);
            if (sel is UndertaleRoom.Background)
                (Application.Current.MainWindow as MainWindow).ChangeSelection((sel as UndertaleRoom.Background).BackgroundDefinition);
            if (sel is UndertaleRoom.Tile)
                (Application.Current.MainWindow as MainWindow).ChangeSelection((sel as UndertaleRoom.Tile).ObjectDefinition);
            if (sel is UndertaleRoom.SpriteInstance)
                (Application.Current.MainWindow as MainWindow).ChangeSelection((sel as UndertaleRoom.SpriteInstance).Sprite);
        }

        private UndertaleObject copied;

        public void Command_Copy(object sender, ExecutedRoutedEventArgs e)
        {
            UndertaleObject selectedObj = ObjectEditor.Content as UndertaleObject;
            if (selectedObj != null)
            {
                Debug.WriteLine("Copy");
                /*Clipboard.Clear();
                Clipboard.SetDataObject(new DataObject(selectedObj));*/
                copied = selectedObj;
            }
        }

        public void Command_Paste(object sender, ExecutedRoutedEventArgs e)
        {
            /*IDataObject data = Clipboard.GetDataObject();
            UndertaleObject obj = data.GetData(data.GetFormats()[0]) as UndertaleObject;
            if (obj != null)
            {
                Debug.WriteLine("Paste");
                Debug.WriteLine(obj);
            }*/

            if (copied != null)
            {
                UndertaleRoom room = this.DataContext as UndertaleRoom;

                UndertaleRoom.Layer layer = ObjectEditor.Content as UndertaleRoom.Layer;
                if ((Application.Current.MainWindow as MainWindow).IsGMS2 == Visibility.Visible && layer == null)
                {
                    MessageBox.Show("Must paste onto a layer", "UndertaleModTool", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                if (copied is UndertaleRoom.GameObject)
                {
                    if (layer != null && layer.InstancesData == null)
                    {
                        MessageBox.Show("Must be on an instances layer", "UndertaleModTool", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    var other = copied as UndertaleRoom.GameObject;
                    var obj = new UndertaleRoom.GameObject();
                    obj.X = other.X;
                    obj.Y = other.Y;
                    obj.ObjectDefinition = other.ObjectDefinition;
                    obj.InstanceID = (Application.Current.MainWindow as MainWindow).Data.GeneralInfo.LastObj++;
                    obj.CreationCode = other.CreationCode;
                    obj.ScaleX = other.ScaleX;
                    obj.ScaleY = other.ScaleY;
                    obj.Color = other.Color;
                    obj.Rotation = other.Rotation;
                    obj.PreCreateCode = other.PreCreateCode;
                    room.GameObjects.Add(obj);
                    if (layer != null)
                        layer.InstancesData.Instances.Add(obj);
                    SelectObject(obj);
                }
                if (copied is UndertaleRoom.Tile)
                {
                    if (layer != null && layer.AssetsData == null)
                    {
                        MessageBox.Show("Must be on an assets layer", "UndertaleModTool", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    var other = copied as UndertaleRoom.Tile;
                    var obj = new UndertaleRoom.Tile();
                    obj.X = other.X;
                    obj.Y = other.Y;
                    obj._SpriteMode = other._SpriteMode;
                    obj.ObjectDefinition = other.ObjectDefinition;
                    obj.SourceX = other.SourceX;
                    obj.SourceY = other.SourceY;
                    obj.Width = other.Width;
                    obj.Height = other.Height;
                    obj.TileDepth = other.TileDepth;
                    obj.InstanceID = (Application.Current.MainWindow as MainWindow).Data.GeneralInfo.LastTile++;
                    obj.ScaleX = other.ScaleX;
                    obj.ScaleY = other.ScaleY;
                    obj.Color = other.Color;
                    if (layer != null)
                        layer.AssetsData.LegacyTiles.Add(obj);
                    else
                        room.Tiles.Add(obj);
                    SelectObject(obj);
                }
            }
        }

        private void AddLayer<T>(UndertaleRoom.LayerType type, string name) where T : UndertaleRoom.Layer.LayerData, new()
        {
            UndertaleRoom room = this.DataContext as UndertaleRoom;

            UndertaleRoom.Layer layer = new UndertaleRoom.Layer();
            layer.LayerName = (Application.Current.MainWindow as MainWindow).Data.Strings.MakeString(name);
            layer.LayerId = 0; // TODO: find next ID
            layer.LayerType = type;
            layer.Data = new T();
            room.Layers.Add(layer);

            SelectObject(layer);
        }
        
        private void MenuItem_NewLayerInstances_Click(object sender, RoutedEventArgs e)
        {
            AddLayer<UndertaleRoom.Layer.LayerInstancesData>(UndertaleRoom.LayerType.Instances, "NewInstancesLayer");
        }

        private void MenuItem_NewLayerTiles_Click(object sender, RoutedEventArgs e)
        {
            AddLayer<UndertaleRoom.Layer.LayerTilesData>(UndertaleRoom.LayerType.Tiles, "NewTilesLayer");
        }

        private void MenuItem_NewLayerBackground_Click(object sender, RoutedEventArgs e)
        {
            AddLayer<UndertaleRoom.Layer.LayerBackgroundData>(UndertaleRoom.LayerType.Background, "NewBackgroundLayer");
        }

        private void MenuItem_NewLayerAssets_Click(object sender, RoutedEventArgs e)
        {
            AddLayer<UndertaleRoom.Layer.LayerAssetsData>(UndertaleRoom.LayerType.Assets, "NewAssetsLayer");
        }
    }

    [ValueConversion(typeof(UndertaleRoom.Background), typeof(int))]
    public class BackgroundScaleXConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            UndertaleRoomEditor roomEditor = parameter as UndertaleRoomEditor;
            Debug.Assert(roomEditor != null);
            UndertaleRoom room = roomEditor.DataContext as UndertaleRoom;

            // GMS2 backgrounds.
            if (value is UndertaleRoom.Layer.LayerBackgroundData)
            {
                UndertaleRoom.Layer.LayerBackgroundData layerBackground = value as UndertaleRoom.Layer.LayerBackgroundData;
                return layerBackground.Stretch ? (room.Width / layerBackground.Sprite.Width) : 1;
            }

            // GMS1 backgrounds.
            UndertaleRoom.Background background = value as UndertaleRoom.Background;
            if (background == null || !background.Stretch || background.BackgroundDefinition == null)
                return 1;

            //TODO: Object rotation.
            //TODO: Update stretch on checkbox.
            //TODO: Tile mode? Update tile on checkbox. TiledHorizontally, TiledVertically

            return ((room.Width - background.X) / background.BackgroundDefinition.Texture.SourceWidth);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [ValueConversion(typeof(UndertaleRoom.Background), typeof(int))]
    public class BackgroundScaleYConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            UndertaleRoomEditor roomEditor = parameter as UndertaleRoomEditor;
            Debug.Assert(roomEditor != null);
            UndertaleRoom room = roomEditor.DataContext as UndertaleRoom;

            // GMS2 backgrounds.
            if (value is UndertaleRoom.Layer.LayerBackgroundData)
            {
                UndertaleRoom.Layer.LayerBackgroundData layerBackground = value as UndertaleRoom.Layer.LayerBackgroundData;
                return layerBackground.Stretch ? (room.Height / layerBackground.Sprite.Height) : 1;
            }

            // GMS1 backgrounds.
            UndertaleRoom.Background background = value as UndertaleRoom.Background;
            if (background == null || !background.Stretch || background.BackgroundDefinition == null)
                return 1;

            return ((room.Height - background.Y) / background.BackgroundDefinition.Texture.SourceHeight);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [ValueConversion(typeof(UndertaleRoom.Background), typeof(int))]
    public class BackgroundCenterXConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value as UndertaleRoom.Background)?.X ?? 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [ValueConversion(typeof(UndertaleRoom.Background), typeof(int))]
    public class BackgroundCenterYConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value as UndertaleRoom.Background)?.Y ?? 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [ValueConversion(typeof(ObservableCollection<UndertaleRoom.GameObject>), typeof(int))]
    public class ObjCenterXConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            UndertaleRoom.GameObject obj = value as UndertaleRoom.GameObject;
            if (obj == null)
                return 0;
            if (obj.ObjectDefinition == null || obj.ObjectDefinition.Sprite == null)
                return obj.X;
            return (obj.X + (obj.ObjectDefinition.Sprite.OriginX * obj.ScaleX));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [ValueConversion(typeof(ObservableCollection<UndertaleRoom.GameObject>), typeof(int))]
    public class ObjCenterYConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            UndertaleRoom.GameObject obj = value as UndertaleRoom.GameObject;
            if (obj == null)
                return 0;
            if (obj.ObjectDefinition == null || obj.ObjectDefinition.Sprite == null)
                return obj.Y;
            return (obj.Y + (obj.ObjectDefinition.Sprite.OriginY * obj.ScaleY));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class LayerDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate InstancesDataTemplate { get; set; }
        public DataTemplate TilesDataTemplate { get; set; }
        public DataTemplate AssetsDataTemplate { get; set; }
        public DataTemplate BackgroundDataTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            FrameworkElement element = container as FrameworkElement;

            if (element != null && item != null && item is UndertaleRoom.Layer)
            {
                UndertaleRoom.Layer layer = item as UndertaleRoom.Layer;
                
                switch(layer.LayerType)
                {
                    case UndertaleRoom.LayerType.Instances:
                        return InstancesDataTemplate;
                    case UndertaleRoom.LayerType.Tiles:
                        return TilesDataTemplate;
                    case UndertaleRoom.LayerType.Assets:
                        return AssetsDataTemplate;
                    case UndertaleRoom.LayerType.Background:
                        return BackgroundDataTemplate;
                }
            }

            return null;
        }
    }

    public class MultiCollectionBinding : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            CompositeCollection collection = new CompositeCollection();
            foreach(var v in values)
                if (v is IEnumerable)
                    collection.Add(new CollectionContainer() { Collection = (IEnumerable)v });
            return collection;
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
    
    public class LayerFlattenerConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            CompositeCollection collection = new CompositeCollection();
            IList<UndertaleRoom.Layer> layers = value as IList<UndertaleRoom.Layer>;
            foreach (var layer in layers.OrderByDescending((x) => x.LayerDepth))
            {
                switch (layer.LayerType)
                {
                    case UndertaleRoom.LayerType.Background:
                        collection.Add(new CollectionContainer() { Collection = new UndertaleRoom.Layer.LayerBackgroundData[] { layer.BackgroundData } });
                        break;
                    case UndertaleRoom.LayerType.Instances:
                        collection.Add(new CollectionContainer() { Collection = layer.InstancesData.Instances });
                        break;
                    case UndertaleRoom.LayerType.Assets:
                        collection.Add(new CollectionContainer() { Collection = layer.AssetsData.LegacyTiles });
                        collection.Add(new CollectionContainer() { Collection = layer.AssetsData.Sprites }); // TODO: add rendering
                        break;
                    case UndertaleRoom.LayerType.Tiles:
                        // TODO
                        break;
                }
            }
            return collection;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
