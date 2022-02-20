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

        private Stack<UndertaleObject> undoStack;

        public UndertaleRoomEditor()
        {
            InitializeComponent();

            Loaded += UndertaleRoomEditor_Loaded;
            DataContextChanged += UndertaleRoomEditor_DataContextChanged;
            undoStack = new Stack<UndertaleObject>();
        }

        public void SaveImagePNG(Stream outfile)
        {
            var target = new RenderTargetBitmap((int)RoomGraphics.RenderSize.Width, (int)RoomGraphics.RenderSize.Height, 96, 96, PixelFormats.Pbgra32);
            target.Render(RoomGraphics);
            var encoder = new PngBitmapEncoder() { Interlace = PngInterlaceOption.Off };
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
            (this.DataContext as UndertaleRoom)?.SetupRoom();
        }

        private void UndertaleRoomEditor_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
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
        private double hotpointX, hotpointY;
        private ScaleTransform canvasSt = new ScaleTransform();

        private Point GetGridMouseCoordinates(Point mousePos, UndertaleRoom room)
        {
            int gridWidth = Math.Max(Convert.ToInt32(room.GridWidth), 1);
            int gridHeight = Math.Max(Convert.ToInt32(room.GridHeight), 1);

            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                gridWidth /= 2;
                gridHeight /= 2;
            }
            else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                gridWidth *= 2;
                gridHeight *= 2;
            }

            return new Point(Math.Floor(mousePos.X / gridWidth) * gridWidth, Math.Floor(mousePos.Y / gridHeight) * gridHeight);
        }

        private void Rectangle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            UndertaleObject clickedObj = (sender as Rectangle).DataContext as UndertaleObject;
            UndertaleRoom room = this.DataContext as UndertaleRoom;
            UndertaleRoom.Layer layer = ObjectEditor.Content as UndertaleRoom.Layer;
            if (clickedObj is UndertaleRoom.GameObject && Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
            {
                if (layer != null && layer.InstancesData == null)
                {
                    MessageBox.Show("Must be on an instances layer", "UndertaleModTool", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                var other = clickedObj as UndertaleRoom.GameObject;
                var newObj = new UndertaleRoom.GameObject
                {
                    X = other.X,
                    Y = other.Y,
                    ObjectDefinition = other.ObjectDefinition,
                    InstanceID = (Application.Current.MainWindow as MainWindow).Data.GeneralInfo.LastObj++,
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
                    MessageBox.Show("Must be on an assets layer", "UndertaleModTool", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    InstanceID = (Application.Current.MainWindow as MainWindow).Data.GeneralInfo.LastTile++,
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

        bool placingTiles = false;
        List<Point> placedTiles = new();

        private void PaintObjects(Point gridMouse, UndertaleObject other, UndertaleRoom room)
        {
            if ((Mouse.LeftButton != MouseButtonState.Pressed) || !(Keyboard.Modifiers.HasFlag(ModifierKeys.Alt)))
            {
                placingTiles = false;
                return;
            }
            UndertaleRoom.Layer layer = ObjectEditor.Content as UndertaleRoom.Layer;

            if (layer != null && layer.AssetsData == null)
            {
                return;
            }

            if (placedTiles.Contains(gridMouse)) return;

            placedTiles.Add(gridMouse);
            placingTiles = true;

            if (other is UndertaleRoom.Tile)
            {
                var otherTile = other as UndertaleRoom.Tile;
                var newObj = new UndertaleRoom.Tile
                {
                    X = (int)gridMouse.X,
                    Y = (int)gridMouse.Y,
                    _SpriteMode = otherTile._SpriteMode,
                    ObjectDefinition = otherTile.ObjectDefinition,
                    SpriteDefinition = otherTile.SpriteDefinition,
                    SourceX = otherTile.SourceX,
                    SourceY = otherTile.SourceY,
                    Width = otherTile.Width,
                    Height = otherTile.Height,
                    TileDepth = otherTile.TileDepth,
                    InstanceID = (Application.Current.MainWindow as MainWindow).Data.GeneralInfo.LastTile++,
                    ScaleX = otherTile.ScaleX,
                    ScaleY = otherTile.ScaleY,
                    Color = otherTile.Color
                };
                if (layer != null)
                    layer.AssetsData.LegacyTiles.Add(newObj);
                else
                    room.Tiles.Add(newObj);
                SelectObject(newObj);
            }
            else if (other is UndertaleRoom.GameObject)
            {
                var otherObject = other as UndertaleRoom.GameObject;
                var newObj = new UndertaleRoom.GameObject
                {
                    X = (int)gridMouse.X,
                    Y = (int)gridMouse.Y,
                    ObjectDefinition = otherObject.ObjectDefinition,
                    InstanceID = (Application.Current.MainWindow as MainWindow).Data.GeneralInfo.LastObj++,
                    CreationCode = otherObject.CreationCode,
                    ScaleX = otherObject.ScaleX,
                    ScaleY = otherObject.ScaleY,
                    Color = otherObject.Color,
                    Rotation = otherObject.Rotation,
                    PreCreateCode = otherObject.PreCreateCode
                };
                room.GameObjects.Add(newObj);
                if (layer != null)
                    layer.InstancesData.Instances.Add(newObj);
                SelectObject(newObj);
            }
        }

        Canvas roomCanvas;

        private void Canvas_Loaded(object sender, RoutedEventArgs e)
        {
            roomCanvas = sender as Canvas;
        }

        private void RectangleBackground_MouseDown(object sender, MouseButtonEventArgs e)
        {
            UndertaleRoom room = DataContext as UndertaleRoom;
            var other = selectedObject as UndertaleObject;

            var mousePos = e.GetPosition(roomCanvas);

            placedTiles.Clear();

            PaintObjects(GetGridMouseCoordinates(mousePos, room), other, room);
        }

        private void RectangleBackground_MouseUp(object sender, MouseButtonEventArgs e)
        {
            placingTiles = false;
            placedTiles.Clear();
        }

        private void RectangleBackground_MouseMove(object sender, MouseEventArgs e)
        {
            if (placingTiles)
            {
                UndertaleRoom room = this.DataContext as UndertaleRoom;
                var other = selectedObject as UndertaleObject;

                var mousePos = e.GetPosition(roomCanvas);

                PaintObjects(GetGridMouseCoordinates(mousePos, room), other, room);
                return;
            }
            Rectangle_MouseMove(sender, e);
        }

        private void Rectangle_MouseMove(object sender, MouseEventArgs e)
        {
            if (movingObj != null)
            {
                UndertaleRoom room = this.DataContext as UndertaleRoom;

                var mousePos = e.GetPosition(RoomGraphics);

                int tgtX = (int)(mousePos.X - hotpointX);
                int tgtY = (int)(mousePos.Y - hotpointY);

                int gridWidth  = Math.Max(Convert.ToInt32(room.GridWidth ), 1);
                int gridHeight = Math.Max(Convert.ToInt32(room.GridHeight), 1);

                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                {
                    gridWidth  /= 2;
                    gridHeight /= 2;
                }
                else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                {
                    gridWidth  *= 2;
                    gridHeight *= 2;
                }

                // Snap to grid
                tgtX = ((tgtX + gridWidth  / 2) / gridWidth ) * gridWidth;
                tgtY = ((tgtY + gridHeight / 2) / gridHeight) * gridHeight;

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

        double scaleOriginX, scaleOriginY;
        private void RectangleTile_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var element = sender as Canvas;
            var tileSelector = element.FindName("TileSelector") as Rectangle;
            var mousePos = e.GetPosition(element);
            var clickedTile = tileSelector.DataContext as UndertaleRoom.Tile;
            UndertaleRoom room = this.DataContext as UndertaleRoom;

            Point gridMouseCoordinates = GetGridMouseCoordinates(mousePos, room);
            scaleOriginX = gridMouseCoordinates.X;
            scaleOriginY = gridMouseCoordinates.Y;
            clickedTile.SourceX = (uint)gridMouseCoordinates.X;
            clickedTile.SourceY = (uint)gridMouseCoordinates.Y;
            clickedTile.Width = (uint)room.GridWidth;
            clickedTile.Height = (uint)room.GridHeight;
        }

        private void RectangleTile_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;

            var element = sender as Canvas;
            var tileSelector = element.FindName("TileSelector") as Rectangle;
            var mousePos = e.GetPosition(element);

            var clickedTile = tileSelector.DataContext as UndertaleRoom.Tile;

            UndertaleRoom room = this.DataContext as UndertaleRoom;

            Point gridMouseCoordinates = GetGridMouseCoordinates(mousePos, room);

            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
            {
                double differenceX = gridMouseCoordinates.X - scaleOriginX;
                double differenceY = gridMouseCoordinates.Y - scaleOriginY;
                clickedTile.Width  = (uint)Math.Clamp(Math.Abs(differenceX), 0, clickedTile.Tpag.BoundingWidth ) + (uint)room.GridWidth;
                clickedTile.Height = (uint)Math.Clamp(Math.Abs(differenceY), 0, clickedTile.Tpag.BoundingHeight) + (uint)room.GridHeight;

                if (differenceX < 0)
                    clickedTile.SourceX = (uint)gridMouseCoordinates.X;
                else
                    clickedTile.SourceX = (uint)scaleOriginX;

                if (differenceY < 0)
                    clickedTile.SourceY = (uint)gridMouseCoordinates.Y;
                else
                    clickedTile.SourceY = (uint)scaleOriginY;
            }
            else
            {
                clickedTile.SourceX = (uint)gridMouseCoordinates.X;
                clickedTile.SourceY = (uint)gridMouseCoordinates.Y;
            }
        }

        private void Canvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;
            var element = sender as System.Windows.Controls.ItemsControl;
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
            e.Handled = true;
            movingObj = null;
        }

        UndertaleObject selectedObject;
        private void SelectObject(UndertaleObject obj)
        {
            // TODO: This sometimes fails to open objects in non-expanded tree
            // Note that this prefers to select the object inside a layer than the 'floating' one in GameObjects

            selectedObject = obj;

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
                if (sourceItem is UndertaleBackground)
                {

                }
                else if (sourceItem is UndertaleGameObject)
                {
                    UndertaleGameObject droppedObject = sourceItem as UndertaleGameObject;
                    var mousePos = e.GetPosition(roomCanvas);

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
                /*Clipboard.Clear();
                Clipboard.SetDataObject(new DataObject(selectedObj));*/
                copied = selectedObj;
            }
        }

        public void Command_Undo(object sender, ExecutedRoutedEventArgs e)
        {
            if (undoStack.Any())
            {
                UndertaleRoom room = this.DataContext as UndertaleRoom;
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

            (this.DataContext as UndertaleRoom)?.SetupRoom();
        }

        private void AddLayer<T>(UndertaleRoom.LayerType type, string name) where T : UndertaleRoom.Layer.LayerData, new()
        {
            UndertaleRoom room = this.DataContext as UndertaleRoom;

            var data = (Application.Current.MainWindow as MainWindow).Data;
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
            var newobject = new UndertaleRoom.GameObject { InstanceID = (Application.Current.MainWindow as MainWindow).Data.GeneralInfo.LastObj++ };
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
            var tile = new UndertaleRoom.Tile { InstanceID = (Application.Current.MainWindow as MainWindow).Data.GeneralInfo.LastTile++ };
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
            var tile = new UndertaleRoom.Tile { InstanceID = (Application.Current.MainWindow as MainWindow).Data.GeneralInfo.LastTile++ };
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
            UndertaleRoom.Layer layer = menuitem.DataContext as UndertaleRoom.Layer;
            AddGMS2ObjectInstance(layer);
        }

        private void MenuItem_NewLegacyTile_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuitem = sender as MenuItem;
            UndertaleRoom.Layer layer = menuitem.DataContext as UndertaleRoom.Layer;
            AddLegacyTile(layer);
        }

        private void MenuItem_NewSprite_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuitem = sender as MenuItem;
            UndertaleRoom.Layer layer = menuitem.DataContext as UndertaleRoom.Layer;
            AddSprite(layer);
        }

        private void MenuItem_NewGMS1Tile_Click(object sender, RoutedEventArgs e)
        {
            UndertaleRoom room = this.DataContext as UndertaleRoom;
            AddGMS1Tile(room);
        }

        private void MenuItem_NewObjectInstance_Click(object sender, RoutedEventArgs e)
        {
            UndertaleRoom room = this.DataContext as UndertaleRoom;
            AddObjectInstance(room);
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
            FrameworkElement element = container as FrameworkElement;

            if (element != null && item != null && item is UndertaleRoom.Layer)
            {
                UndertaleRoom.Layer layer = item as UndertaleRoom.Layer;

                switch (layer.LayerType)
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
            foreach (var v in values)
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

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
