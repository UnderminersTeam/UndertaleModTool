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
using System.Reflection;
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
using System.Windows.Threading;
using UndertaleModLib;
using UndertaleModLib.Models;
using static UndertaleModLib.Models.UndertaleRoom;

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

        public static readonly PropertyInfo visualOffProp = typeof(Canvas).GetProperty("VisualOffset", BindingFlags.NonPublic | BindingFlags.Instance);
        private static MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

        public UndertalePath PreviewPath
        {
            get { return (UndertalePath)GetValue(PreviewPathProperty); }
            set { SetValue(PreviewPathProperty, value); }
        }

        private Stack<UndertaleObject> undoStack;
        public Canvas RoomCanvas { get; set; }

        public UndertaleRoomEditor()
        {
            InitializeComponent();

            Loaded += UndertaleRoomEditor_Loaded;
            DataContextChanged += UndertaleRoomEditor_DataContextChanged;
            undoStack = new Stack<UndertaleObject>();
        }

        public void SaveImagePNG(Stream outfile)
        {
            if (RoomCanvas is null)
            {
                if (MainWindow.FindVisualChild<Canvas>(RoomGraphics) is Canvas canv && canv.Name == "RoomCanvas")
                    RoomCanvas = canv;
                else
                    throw new Exception("\"RoomCanvas\" not found.");
            }

            object prevOffset = visualOffProp.GetValue(RoomCanvas);
            visualOffProp.SetValue(RoomCanvas, new Vector(0, 0)); // (probably, there is a better way to fix the offset of the rendered picture)

            RenderTargetBitmap target = new((int)RoomCanvas.RenderSize.Width, (int)RoomCanvas.RenderSize.Height, 96, 96, PixelFormats.Pbgra32);

            target.Render(RoomCanvas);

            PngBitmapEncoder encoder = new() { Interlace = PngInterlaceOption.Off };
            encoder.Frames.Add(BitmapFrame.Create(target));
            encoder.Save(outfile);

            visualOffProp.SetValue(RoomCanvas, prevOffset);
        }

        private void ExportAsPNG_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dlg = new();

            dlg.FileName = (DataContext as UndertaleRoom).Name.Content + ".png";
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
            (this.DataContext as UndertaleRoom)?.SetupRoom();
        }

        private void UndertaleRoomEditor_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            RoomRootItem.IsSelected = false;
            RoomRootItem.IsSelected = true;
            (this.DataContext as UndertaleRoom)?.SetupRoom();
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
        private UndertaleRoom.Tile movingTile;
        private double hotpointX, hotpointY, hotpointTileX, hotpointTileY;
        private ScaleTransform canvasSt = new ScaleTransform();

        private void Rectangle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            UndertaleObject clickedObj = (sender as Rectangle).DataContext as UndertaleObject;
            UndertaleRoom room = this.DataContext as UndertaleRoom;
            UndertaleRoom.Layer layer = ObjectEditor.Content as UndertaleRoom.Layer;
            if (clickedObj is UndertaleRoom.GameObject && Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
            {
                if (layer != null && layer.InstancesData == null)
                {
                    MainWindow.ShowError("Must be on an instances layer");
                    return;
                }
                var other = clickedObj as UndertaleRoom.GameObject;
                var newObj = new UndertaleRoom.GameObject
                {
                    X = other.X,
                    Y = other.Y,
                    ObjectDefinition = other.ObjectDefinition,
                    InstanceID = mainWindow.Data.GeneralInfo.LastObj++,
                    CreationCode = other.CreationCode,
                    ScaleX = other.ScaleX,
                    ScaleY = other.ScaleY,
                    Color = other.Color,
                    Rotation = other.Rotation,
                    PreCreateCode = other.PreCreateCode
                };
                var index = room.GameObjects.IndexOf(other);
                room.GameObjects.Insert(index + 1, newObj);
                clickedObj = newObj;
            }
            if (clickedObj is UndertaleRoom.Tile && Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
            {
                if (layer != null && layer.AssetsData == null)
                {
                    MainWindow.ShowError("Must be on an assets layer");
                    return;
                }
                var other = clickedObj as UndertaleRoom.Tile;
                var newObj = new UndertaleRoom.Tile
                {
                    X = other.X,
                    Y = other.Y,
                    _SpriteMode = other._SpriteMode,
                    ObjectDefinition = other.ObjectDefinition,
                    SourceX = other.SourceX,
                    SourceY = other.SourceY,
                    Width = other.Width,
                    Height = other.Height,
                    TileDepth = other.TileDepth,
                    InstanceID = mainWindow.Data.GeneralInfo.LastTile++,
                    ScaleX = other.ScaleX,
                    ScaleY = other.ScaleY,
                    Color = other.Color
                };
                var index = room.Tiles.IndexOf(other);
                room.Tiles.Insert(index + 1, newObj);
                clickedObj = newObj;
            }

            SelectObject(clickedObj);

            var mousePos = e.GetPosition(RoomGraphics);
            if (clickedObj is UndertaleRoom.GameObject || clickedObj is UndertaleRoom.Tile)
            {
                movingObj = clickedObj;
                if (movingObj is UndertaleRoom.GameObject)
                {
                    var other = movingObj as UndertaleRoom.GameObject;
                    var undoObj = new UndertaleRoom.GameObject()
                    {
                        X = other.X,
                        Y = other.Y,
                        ObjectDefinition = other.ObjectDefinition,
                        InstanceID = other.InstanceID,
                        CreationCode = other.CreationCode,
                        ScaleX = other.ScaleX,
                        ScaleY = other.ScaleY,
                        Color = other.Color,
                        Rotation = other.Rotation,
                        PreCreateCode = other.PreCreateCode
                    };
                    undoStack.Push(undoObj);
                    hotpointX = mousePos.X - (movingObj as UndertaleRoom.GameObject).X;
                    hotpointY = mousePos.Y - (movingObj as UndertaleRoom.GameObject).Y;
                }
                else if (movingObj is UndertaleRoom.Tile)
                {
                    var other = movingObj as UndertaleRoom.Tile;
                    var undoObj = new UndertaleRoom.Tile()
                    {
                        X = other.X,
                        Y = other.Y,
                        _SpriteMode = other._SpriteMode,
                        ObjectDefinition = other.ObjectDefinition,
                        SourceX = other.SourceX,
                        SourceY = other.SourceY,
                        Width = other.Width,
                        Height = other.Height,
                        TileDepth = other.TileDepth,
                        InstanceID = other.InstanceID,
                        ScaleX = other.ScaleX,
                        ScaleY = other.ScaleY,
                        Color = other.Color
                    };
                    undoStack.Push(undoObj);
                    hotpointX = mousePos.X - (movingObj as UndertaleRoom.Tile).X;
                    hotpointY = mousePos.Y - (movingObj as UndertaleRoom.Tile).Y;
                }
            }
        }

        private void RectangleTile_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var element = sender as Rectangle;
            UndertaleRoom.Tile clickedObj = (sender as Rectangle).DataContext as UndertaleRoom.Tile;
            var mousePos = e.GetPosition(element.Parent as UIElement);
            movingTile = clickedObj;
            hotpointTileX = mousePos.X - movingTile.SourceX;
            hotpointTileY = mousePos.Y - movingTile.SourceY;
        }

        private void RectangleTile_MouseMove(object sender, MouseEventArgs e)
        {
            if (movingTile != null)
            {
                var element = sender as UIElement;
                UndertaleRoom room = this.DataContext as UndertaleRoom;

                var maxX = movingTile.Tpag.BoundingWidth;
                var maxY = movingTile.Tpag.BoundingHeight;

                var mousePos = e.GetPosition(element);

                int tgtX = (int)Math.Clamp(mousePos.X - hotpointTileX, 0, maxX - movingTile.Width);
                int tgtY = (int)Math.Clamp(mousePos.Y - hotpointTileY, 0, maxY - movingTile.Height);

                int scaleX = (int)Math.Clamp(mousePos.X - movingTile.SourceX, 0, maxX);
                int scaleY = (int)Math.Clamp(mousePos.Y - movingTile.SourceY, 0, maxY);

                int gridSize = Convert.ToInt32(room.Grid);
                if (gridSize <= 0)
                    gridSize = 1;
                else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                    gridSize = gridSize / 2;
                else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                    gridSize = gridSize * 2;

                // Snap to grid
                tgtX = (tgtX + gridSize / 2) / gridSize * gridSize;
                tgtY = (tgtY + gridSize / 2) / gridSize * gridSize;

                scaleX = (scaleX + gridSize / 2) / gridSize * gridSize;
                scaleY = (scaleY + gridSize / 2) / gridSize * gridSize;
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
                {
                    movingTile.Width = (uint)scaleX;
                    movingTile.Height = (uint)scaleY;
                }
                else
                {
                    movingTile.SourceX = (uint)tgtX;
                    movingTile.SourceY = (uint)tgtY;
                }
            }
        }

        private void RectangleTile_MouseUp(object sender, MouseButtonEventArgs e)
        {
            movingTile = null;
        }

        private void Rectangle_MouseMove(object sender, MouseEventArgs e)
        {
            if (movingObj != null)
            {
                UndertaleRoom room = this.DataContext as UndertaleRoom;

                var mousePos = e.GetPosition(RoomGraphics);

                int tgtX = (int)(mousePos.X - hotpointX);
                int tgtY = (int)(mousePos.Y - hotpointY);

                int gridSize = Convert.ToInt32(room.Grid);
                if (gridSize <= 0)
                    gridSize = 1;
                else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                    gridSize = gridSize/2;
                else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                    gridSize = gridSize*2;

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

        private void Canvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;
            var element = sender as ItemsControl;
            var mousePos = e.GetPosition(RoomGraphics);
            var transform = element.LayoutTransform as MatrixTransform;
            var matrix = transform.Matrix;
            var scale = e.Delta >= 0 ? 1.1 : (1.0 / 1.1); // choose appropriate scaling factor

            if ((matrix.M11 > 0.2 || (matrix.M11 <= 0.2 && scale > 1)) && (matrix.M11 < 3 || (matrix.M11 >= 3 && scale < 1)))
            {
                matrix.ScaleAtPrepend(scale, scale, mousePos.X, mousePos.Y);
            }
            element.LayoutTransform = new MatrixTransform(matrix);
        }

        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            ScrollViewer scrollViewer = sender as ScrollViewer;

            if (e.ExtentHeightChange != 0 || e.ExtentWidthChange != 0)
            {
                double xMousePositionOnScrollViewer = Mouse.GetPosition(scrollViewer).X;
                double yMousePositionOnScrollViewer = Mouse.GetPosition(scrollViewer).Y;
                double offsetX = e.HorizontalOffset + xMousePositionOnScrollViewer;
                double offsetY = e.VerticalOffset + yMousePositionOnScrollViewer;

                double oldExtentWidth = e.ExtentWidth - e.ExtentWidthChange;
                double oldExtentHeight = e.ExtentHeight - e.ExtentHeightChange;

                double relx = offsetX / oldExtentWidth;
                double rely = offsetY / oldExtentHeight;

                offsetX = Math.Max(relx * e.ExtentWidth - xMousePositionOnScrollViewer, 0);
                offsetY = Math.Max(rely * e.ExtentHeight - yMousePositionOnScrollViewer, 0);

                ScrollViewer scrollViewerTemp = sender as ScrollViewer;
                scrollViewerTemp.ScrollToHorizontalOffset(offsetX);
                scrollViewerTemp.ScrollToVerticalOffset(offsetY);
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
            UndertaleObject sourceItem = e.Data.GetData(e.Data.GetFormats()[^1]) as UndertaleObject; // TODO: make this more reliable

            e.Effects = e.AllowedEffects.HasFlag(DragDropEffects.Link) && sourceItem != null && (sourceItem is UndertaleGameObject || sourceItem is UndertalePath) ? DragDropEffects.Link : DragDropEffects.None;
            e.Handled = true;
        }

        private void Canvas_Drop(object sender, DragEventArgs e)
        {
            UndertaleObject sourceItem = e.Data.GetData(e.Data.GetFormats()[^1]) as UndertaleObject;

            e.Effects = e.AllowedEffects.HasFlag(DragDropEffects.Link) && sourceItem != null && (sourceItem is UndertaleGameObject || sourceItem is UndertalePath) ? DragDropEffects.Link : DragDropEffects.None;
            if (e.Effects == DragDropEffects.Link)
            {
                if (sourceItem is UndertaleBackground)
                {

                }
                else if (sourceItem is UndertaleGameObject)
                {
                    UndertaleGameObject droppedObject = sourceItem as UndertaleGameObject;
                    var mousePos = e.GetPosition(RoomGraphics);

                    UndertaleRoom room = this.DataContext as UndertaleRoom;
                    UndertaleRoom.Layer layer = ObjectEditor.Content as UndertaleRoom.Layer;
                    if (mainWindow.IsGMS2 == Visibility.Visible && layer == null)
                    {
                        MainWindow.ShowError("Must have a layer selected");
                        return;
                    }
                    if (layer != null && layer.InstancesData == null)
                    {
                        MainWindow.ShowError("Must be on an instances layer");
                        return;
                    }

                    var obj = new UndertaleRoom.GameObject()
                    {
                        X = (int)mousePos.X,
                        Y = (int)mousePos.Y,
                        ObjectDefinition = droppedObject,
                        InstanceID = mainWindow.Data.GeneralInfo.LastObj++
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

            (this.DataContext as UndertaleRoom)?.SetupRoom();
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
                    if (mainWindow.IsGMS2 == Visibility.Visible)
                        foreach (var layer in room.Layers)
                            if (layer.AssetsData != null)
                                layer.AssetsData.LegacyTiles.Remove(tile);
                    room.Tiles.Remove(tile);
                    ObjectEditor.Content = null;
                }
                else if (selectedObj is UndertaleRoom.GameObject)
                {
                    UndertaleRoom.GameObject gameObj = selectedObj as UndertaleRoom.GameObject;
                    if (mainWindow.IsGMS2 == Visibility.Visible)
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
            if (e.Key == Key.OemMinus)
            {
                UndertaleRoom room = this.DataContext as UndertaleRoom;
                UndertaleObject selectedObj = ObjectEditor.Content as UndertaleObject;

                if (selectedObj is UndertaleRoom.Tile)
                {
                    UndertaleRoom.Tile tile = selectedObj as UndertaleRoom.Tile;
                    var index = room.Tiles.IndexOf(tile);
                    if (index > 0)
                    {
                        var prevIndex = index - 1;
                        var prevIndexObj = room.Tiles[prevIndex];
                        room.Tiles[prevIndex] = tile;
                        room.Tiles[index] = prevIndexObj;
                        SelectObject(tile);
                    }
                }
                else if (selectedObj is UndertaleRoom.GameObject)
                {
                    UndertaleRoom.GameObject gameObj = selectedObj as UndertaleRoom.GameObject;
                    var index = room.GameObjects.IndexOf(gameObj);
                    if (index > 0)
                    {
                        var prevIndex = index - 1;
                        var prevIndexObj = room.GameObjects[prevIndex];
                        room.GameObjects[prevIndex] = gameObj;
                        room.GameObjects[index] = prevIndexObj;
                        SelectObject(gameObj);
                    }
                }
            }
            if (e.Key == Key.OemPlus)
            {
                UndertaleRoom room = this.DataContext as UndertaleRoom;
                UndertaleObject selectedObj = ObjectEditor.Content as UndertaleObject;

                if (selectedObj is UndertaleRoom.Tile)
                {
                    UndertaleRoom.Tile tile = selectedObj as UndertaleRoom.Tile;
                    var index = room.Tiles.IndexOf(tile);
                    if (index < room.Tiles.Count - 1)
                    {
                        var prevIndex = index + 1;
                        var prevIndexObj = room.Tiles[prevIndex];
                        room.Tiles[prevIndex] = tile;
                        room.Tiles[index] = prevIndexObj;
                        SelectObject(tile);
                    }
                }
                else if (selectedObj is UndertaleRoom.GameObject)
                {
                    UndertaleRoom.GameObject gameObj = selectedObj as UndertaleRoom.GameObject;
                    var index = room.GameObjects.IndexOf(gameObj);
                    if (index < room.GameObjects.Count - 1)
                    {
                        var prevIndex = index + 1;
                        var prevIndexObj = room.GameObjects[prevIndex];
                        room.GameObjects[prevIndex] = gameObj;
                        room.GameObjects[index] = prevIndexObj;
                        SelectObject(gameObj);
                    }
                }
            }
        }

        private void RoomObjectsTree_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            object sel = (sender as TreeView).SelectedItem;
            if (sel is UndertaleRoom.GameObject)
                mainWindow.ChangeSelection((sel as UndertaleRoom.GameObject).ObjectDefinition);
            if (sel is UndertaleRoom.Background)
                mainWindow.ChangeSelection((sel as UndertaleRoom.Background).BackgroundDefinition);
            if (sel is UndertaleRoom.Tile)
                mainWindow.ChangeSelection((sel as UndertaleRoom.Tile).ObjectDefinition);
            if (sel is UndertaleRoom.SpriteInstance)
                mainWindow.ChangeSelection((sel as UndertaleRoom.SpriteInstance).Sprite);
        }

        private UndertaleObject copied;

        public void Command_Copy(object sender, ExecutedRoutedEventArgs e)
        {
            UndertaleObject selectedObj = ObjectEditor.Content as UndertaleObject;
            if (selectedObj != null)
            {
                /*Clipboard.Clear();
                Clipboard.SetDataObject(new DataObject(selectedObj));*/
                copied = selectedObj;
            }
        }

        public void Command_Undo(object sender, ExecutedRoutedEventArgs e)
        {
            if (undoStack.Any())
            {
                var undoObject = undoStack.Pop();
                if (undoObject is UndertaleRoom.GameObject && ObjectEditor.Content is UndertaleRoom.GameObject)
                {
                    var toChange = ObjectEditor.Content as UndertaleRoom.GameObject;
                    var undoGameObject = undoObject as UndertaleRoom.GameObject;
                    if (toChange.InstanceID == undoGameObject.InstanceID)
                    {
                        toChange.X = undoGameObject.X;
                        toChange.Y = undoGameObject.Y;
                    }
                }
                if (undoObject is UndertaleRoom.Tile && ObjectEditor.Content is UndertaleRoom.Tile)
                {
                    var toChange = ObjectEditor.Content as UndertaleRoom.Tile;
                    var undoGameObject = undoObject as UndertaleRoom.Tile;
                    if (toChange.InstanceID == undoGameObject.InstanceID)
                    {
                        toChange.X = undoGameObject.X;
                        toChange.Y = undoGameObject.Y;
                    }
                }
                (this.DataContext as UndertaleRoom)?.SetupRoom();
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
                if (mainWindow.IsGMS2 == Visibility.Visible && layer == null)
                {
                    MainWindow.ShowError("Must paste onto a layer");
                    return;
                }

                if (copied is UndertaleRoom.GameObject)
                {
                    if (layer != null && layer.InstancesData == null)
                    {
                        MainWindow.ShowError("Must be on an instances layer");
                        return;
                    }
                    var other = copied as UndertaleRoom.GameObject;
                    var obj = new UndertaleRoom.GameObject();
                    obj.X = other.X;
                    obj.Y = other.Y;
                    obj.ObjectDefinition = other.ObjectDefinition;
                    obj.InstanceID = mainWindow.Data.GeneralInfo.LastObj++;
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
                        MainWindow.ShowError("Must be on an assets layer");
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
                    obj.InstanceID = mainWindow.Data.GeneralInfo.LastTile++;
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

            (this.DataContext as UndertaleRoom)?.SetupRoom();
        }

        private void AddLayer<T>(UndertaleRoom.LayerType type, string name) where T : UndertaleRoom.Layer.LayerData, new()
        {
            UndertaleRoom room = this.DataContext as UndertaleRoom;

            var data = mainWindow.Data;
            uint largest_layerid = 0;

            // Find the largest layer id
            // See #355
            foreach (UndertaleRoom Room in data.Rooms)
            {
                foreach (UndertaleRoom.Layer Layer in Room.Layers)
                {
                    if (Layer.LayerId > largest_layerid)
                        largest_layerid = Layer.LayerId;
                }
            }

            UndertaleRoom.Layer layer = new UndertaleRoom.Layer();
            layer.LayerName = data.Strings.MakeString(name);
            layer.LayerId = largest_layerid + 1;
            layer.LayerType = type;
            layer.Data = new T();
            room.Layers.Add(layer);

            if (layer.LayerType == UndertaleRoom.LayerType.Assets)
            {
                // create a new pointer list
                if (layer.AssetsData.LegacyTiles == null)
                    layer.AssetsData.LegacyTiles = new UndertalePointerList<UndertaleRoom.Tile>();
                // create new sprite pointer list
                if (layer.AssetsData.Sprites == null)
                    layer.AssetsData.Sprites = new UndertalePointerList<UndertaleRoom.SpriteInstance>();
            }

            SelectObject(layer);
            (this.DataContext as UndertaleRoom)?.SetupRoom();
        }

        private void AddObjectInstance(UndertaleRoom room)
        {
            var newobject = new UndertaleRoom.GameObject { InstanceID = mainWindow.Data.GeneralInfo.LastObj++ };
            room.GameObjects.Add(newobject);
            SelectObject(newobject);
        }

        private void AddGMS2ObjectInstance(UndertaleRoom.Layer layer)
        {
            UndertaleRoom room = this.DataContext as UndertaleRoom;
            AddObjectInstance(room);

            var gameobject = room.GameObjects.Last();
            layer.InstancesData.Instances.Add(gameobject);
            if (layer != null)
                SelectObject(gameobject);
        }

        private void AddLegacyTile(UndertaleRoom.Layer layer)
        {
            // add pointer list if one doesn't already exist
            if (layer.AssetsData.LegacyTiles == null)
                layer.AssetsData.LegacyTiles = new UndertalePointerList<UndertaleRoom.Tile>();

            // add sprite pointer list if one doesn't already exist
            if (layer.AssetsData.Sprites == null)
                layer.AssetsData.Sprites = new UndertalePointerList<UndertaleRoom.SpriteInstance>();

            // add tile to list
            var tile = new UndertaleRoom.Tile { InstanceID = mainWindow.Data.GeneralInfo.LastTile++ };
            tile._SpriteMode = true;
            layer.AssetsData.LegacyTiles.Add(tile);

            if (layer != null)
                SelectObject(tile);
        }

        private void AddSprite(UndertaleRoom.Layer layer)
        {
            // add pointer list if one doesn't already exist
            if (layer.AssetsData.Sprites == null)
                layer.AssetsData.Sprites = new UndertalePointerList<UndertaleRoom.SpriteInstance>();

            // add tile to list
            var spriteinstance = new UndertaleRoom.SpriteInstance();
            layer.AssetsData.Sprites.Add(spriteinstance);

            if (layer != null)
                SelectObject(spriteinstance);
        }

        private void AddGMS1Tile(UndertaleRoom room)
        {
            // add tile to list
            var tile = new UndertaleRoom.Tile { InstanceID = mainWindow.Data.GeneralInfo.LastTile++ };
            room.Tiles.Add(tile);
            SelectObject(tile);
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

        private void MenuItem_NewGMS2ObjectInstance_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuitem = sender as MenuItem;
            AddGMS2ObjectInstance(menuitem.DataContext as UndertaleRoom.Layer);
        }

        private void MenuItem_NewLegacyTile_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuitem = sender as MenuItem;
            AddLegacyTile(menuitem.DataContext as UndertaleRoom.Layer);
        }

        private void MenuItem_NewSprite_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuitem = sender as MenuItem;
            AddSprite(menuitem.DataContext as UndertaleRoom.Layer);
        }

        private void MenuItem_NewGMS1Tile_Click(object sender, RoutedEventArgs e)
        {
            AddGMS1Tile(this.DataContext as UndertaleRoom);
        }

        private void MenuItem_NewObjectInstance_Click(object sender, RoutedEventArgs e)
        {
            AddObjectInstance(this.DataContext as UndertaleRoom);
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
    public class ObjXOffsetConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            UndertaleRoom.GameObject obj = value as UndertaleRoom.GameObject;
            if (obj == null)
                return 0;
            if (obj.ObjectDefinition == null || obj.ObjectDefinition.Sprite == null)
                return obj.X;
            return obj.X + obj.ObjectDefinition.Sprite.OriginX;
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
            if (container is FrameworkElement && item != null && item is UndertaleRoom.Layer)
            {
                switch ((item as UndertaleRoom.Layer).LayerType)
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

    public class RoomObjectTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is null)
                return null;

            string resName = item switch
            {
                UndertaleGameObject => "Obj",
                UndertaleBackground => "BG",
                _ => null,
            };
            if (resName is not null)
                return (DataTemplate)(container as FrameworkElement).FindResource(resName + "Template");
            else
                return null;
        }
    }

    public class MultiCollectionBinding : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            CompositeCollection collection = new();
            foreach (var v in values)
                if (v is IEnumerable)
                    collection.Add(new CollectionContainer() { Collection = (IEnumerable)v });
            return collection;
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class LayerFlattenerConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            CompositeCollection collection = new();
            IList<UndertaleRoom.Layer> layers = value as IList<UndertaleRoom.Layer>;
            foreach (var layer in layers.OrderByDescending((x) => x?.LayerDepth ?? 0))
            {
                if (layer == null)
                    continue;

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

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class IsGMS2Converter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isGMS2 = ((RoomEntryFlags)value).HasFlag(RoomEntryFlags.IsGMS2);

            return targetType.Name switch
            {
                "Boolean" => !isGMS2,
                "Visibility" => isGMS2 ? Visibility.Collapsed : Visibility.Visible,
                _ => null,
            };
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
