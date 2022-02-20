using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
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
    public partial class UndertaleRoomEditor : DataUserControl
    {
        public static DependencyProperty PreviewPathProperty =
            DependencyProperty.Register("PreviewPath", typeof(UndertalePath),
                typeof(UndertaleRoomEditor),
                new FrameworkPropertyMetadata(null));

        public static readonly PropertyInfo visualOffProp = typeof(Canvas).GetProperty("VisualOffset", BindingFlags.NonPublic | BindingFlags.Instance);
        private static MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

        public UndertalePath PreviewPath
        {
            get => (UndertalePath)GetValue(PreviewPathProperty);
            set => SetValue(PreviewPathProperty, value);
        }

        private Stack<UndertaleObject> undoStack = new();
        private Dictionary<UndertaleObject, FrameworkElement> roomObjDict = new();
        public Canvas RoomCanvas { get; set; }

        public static readonly DoubleAnimation flashAnim = new(1, 0, TimeSpan.FromSeconds(0.75))
        { 
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseIn }
        };
        private Storyboard currStoryboard;

        public UndertaleRoomEditor()
        {
            InitializeComponent();

            Loaded += UndertaleRoomEditor_Loaded;
            DataContextChanged += UndertaleRoomEditor_DataContextChanged;
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
            (DataContext as UndertaleRoom)?.SetupRoom();
        }

        private void UndertaleRoomEditor_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (IsLoaded)
            {
                RoomRootItem.IsSelected = false;
                RoomRootItem.IsSelected = true;

                ScrollViewer viewer = MainWindow.FindVisualChild<ScrollViewer>(RoomObjectsTree);
                viewer.ScrollToVerticalOffset(0);
                viewer.ScrollToHorizontalOffset(0);
            }

            UndertaleCachedImageLoader.TileCache.Clear();
            UndertaleCachedImageLoader.ImageCache.Clear();

            if (DataContext is UndertaleRoom room)
            {
                GameObjItems.Header = room.Flags.HasFlag(RoomEntryFlags.IsGMS2) 
                                      ? "Game objects (from all layers)"
                                      : "Game objects";
                room.SetupRoom();
                GenerateSpriteCache(DataContext as UndertaleRoom);

                if (RoomCanvas is not null)
                {
                    RoomGraphics.SetCurrentValue(ItemsControl.ItemsSourceProperty, null); // clear "RoomCanvas.Children"
                    _ = Task.Run(() =>
                    {
                        byte c = 0;
                        int count = Dispatcher.Invoke(() => RoomCanvas.Children.Count);
                        while (count == 0)
                        {
                            if (c >= 10)
                                return;

                            Thread.Sleep(50);
                            count = Dispatcher.Invoke(() => RoomCanvas.Children.Count, DispatcherPriority.ContextIdle);

                            c++;
                        }

                        roomObjDict.Clear();
                        Dispatcher.Invoke(() =>
                        {
                            RoomCanvas.UpdateLayout();
                            foreach (ContentPresenter elem in RoomCanvas.Children.Cast<ContentPresenter>())
                                roomObjDict.Add(elem.Content as UndertaleObject, VisualTreeHelper.GetChild(elem, 0) as FrameworkElement);
                        });
                    });
                }
            }
        }

        private void RoomCanvas_Loaded(object sender, RoutedEventArgs e)
        {
            RoomCanvas = sender as Canvas;

            roomObjDict.Clear();
            foreach (ContentPresenter elem in RoomCanvas.Children.Cast<ContentPresenter>())
                roomObjDict.Add(elem.Content as UndertaleObject, VisualTreeHelper.GetChild(elem, 0) as FrameworkElement);
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            // I can't bind it directly because then clicking on the headers makes WPF explode because it tries to attach the header as child of ObjectEditor
            // TODO: find some better workaround
            if (currStoryboard is not null)
            {
                currStoryboard.Stop(this);
                currStoryboard.Remove(this);
            }
            
            if (e.NewValue == RoomRootItem)
            {
                ObjectEditor.Content = DataContext;
            }
            else if (e.NewValue is UndertaleObject obj)
            {
                ObjectEditor.Content = obj;
                try
                {
                    DependencyObject obj1 = null;
                    if (obj is Layer layer)
                    {
                        if (layer.LayerType == LayerType.Background)
                            obj1 = roomObjDict[layer.BackgroundData];
                        else
                            return;
                    }
                    else if (obj is View)
                        return;
                    else
                        obj1 = roomObjDict[obj];

                    Storyboard.SetTarget(flashAnim, obj1);
                    Storyboard.SetTargetProperty(flashAnim, new PropertyPath(OpacityProperty));

                    currStoryboard = new();
                    currStoryboard.Children.Add(flashAnim);
                    currStoryboard.Begin(this, true);
                }
                catch
                {
                    Debug.WriteLine($"Flash animation error - \"{obj}\" is missing from the room object dictionary.");
                }
            }
        }

        private UndertaleObject movingObj;
        private double hotpointX, hotpointY;

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
            Layer layer = ObjectEditor.Content as Layer;
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
            e.Handled = true;
            movingObj = null;
        }

        UndertaleObject selectedObject;
        private void SelectObject(UndertaleObject obj)
        {
            // TODO: enable virtualizing of RoomObjectsTree and make this method work with it

            ScrollViewer mainTreeViewer = MainWindow.FindVisualChild<ScrollViewer>(RoomObjectsTree);
            UndertaleRoom room = DataContext as UndertaleRoom;
            IList resList = null;
            TreeViewItem resListView = null;
            Layer resLayer = null;
            switch (obj)
            {
                case UndertaleRoom.Background:
                    resList = room.Backgrounds;
                    resListView = BGItems;
                    break;

                case View:
                    resList = room.Views;
                    resListView = ViewItems;
                    break;

                case GameObject gameObj:
                    if (room.Flags.HasFlag(RoomEntryFlags.IsGMS2))
                    {
                        resLayer = room.Layers.AsParallel()
                                              .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                                              .FirstOrDefault(l => l.LayerType is LayerType.Instances
                                                  && (l.InstancesData.Instances?.Any(x => x.InstanceID == gameObj.InstanceID) ?? false));
                        resList = resLayer.InstancesData.Instances;
                        resListView = LayerItems.ItemContainerGenerator.ContainerFromItem(resLayer) as TreeViewItem;
                    }
                    else
                    {
                        resList = room.GameObjects;
                        resListView = GameObjItems;
                    }

                    break;

                case Tile tile:
                    if (room.Flags.HasFlag(RoomEntryFlags.IsGMS2))
                    {
                        resLayer = room.Layers.AsParallel()
                                              .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                                              .FirstOrDefault(l => l.LayerType is LayerType.Assets
                                                  && (l.AssetsData.LegacyTiles?.Any(x => x.InstanceID == tile.InstanceID) ?? false));
                        resList = resLayer.AssetsData.LegacyTiles;
                        resListView = LayerItems.ItemContainerGenerator.ContainerFromItem(resLayer) as TreeViewItem;
                    }
                    else
                    {
                        resList = room.Tiles;
                        resListView = TileItems;
                    }

                    break;

                case Layer:
                case Layer.LayerBackgroundData:
                    resList = room.Layers;
                    resListView = LayerItems;
                    break;

                case SpriteInstance spr:
                    resLayer = room.Layers.AsParallel()
                                          .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                                          .FirstOrDefault(l => l.LayerType is LayerType.Assets
                                              && (l.AssetsData.Sprites?.Any(x => x.Name == spr.Name) ?? false));
                    resList = resLayer.AssetsData.Sprites;
                    resListView = LayerItems.ItemContainerGenerator.ContainerFromItem(resLayer) as TreeViewItem;
                    break;
            }

            if (resList is null || resListView is null)
                return;

            resListView.IsExpanded = true;
            resListView.BringIntoView();
            resListView.UpdateLayout();

            StackPanel resPanel = MainWindow.FindVisualChild<StackPanel>(resListView);
            (resPanel.Children[0] as TreeViewItem).BringIntoView();
            mainTreeViewer.UpdateLayout();

            double firstElemOffset = mainTreeViewer.VerticalOffset + (resPanel.Children[0] as TreeViewItem).TransformToAncestor(mainTreeViewer).Transform(new Point(0, 0)).Y;

            mainTreeViewer.ScrollToVerticalOffset(firstElemOffset + ((resList.IndexOf(obj) + 1) * 16) - (mainTreeViewer.ViewportHeight / 2));
            mainTreeViewer.UpdateLayout();

            UndertaleObject obj1 = obj is Layer.LayerBackgroundData bgData ? bgData.ParentLayer : obj;
            if (resListView.ItemContainerGenerator.ContainerFromItem(obj1) is TreeViewItem resItem)
            {
                resItem.IsSelected = true;

                mainTreeViewer.UpdateLayout();
                mainTreeViewer.ScrollToHorizontalOffset(0);
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
                    var mousePos = e.GetPosition(roomCanvas);

                    UndertaleRoom room = this.DataContext as UndertaleRoom;
                    Layer layer = ObjectEditor.Content as Layer;
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
                else if (selectedObj is Layer)
                {
                    Layer layer = selectedObj as Layer;
                    if (layer.InstancesData != null)
                        foreach (var go in layer.InstancesData.Instances)
                            room.GameObjects.Remove(go);
                    room.Layers.Remove(layer);
                    (RoomGraphics.ItemsSource as CompositeCollection).Remove(layer.BackgroundData);
                    ObjectEditor.Content = null;
                    room.UpdateBGColorLayer();
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

                Layer layer = ObjectEditor.Content as Layer;
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
                foreach (Layer Layer in Room.Layers)
                {
                    if (Layer.LayerId > largest_layerid)
                        largest_layerid = Layer.LayerId;
                }
            }

            Layer layer = new();
            layer.LayerName = data.Strings.MakeString(name);
            layer.LayerId = largest_layerid + 1;
            layer.LayerType = type;
            layer.Data = new T();
            room.Layers.Add(layer);
            room.UpdateBGColorLayer();

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

        private void AddGMS2ObjectInstance(Layer layer)
        {
            UndertaleRoom room = this.DataContext as UndertaleRoom;

            GameObject newObject = new() { InstanceID = mainWindow.Data.GeneralInfo.LastObj++ };
            room.GameObjects.Add(newObject);

            if (layer is not null)
            {
                layer.InstancesData.Instances.Add(newObject);
                SelectObject(newObject);
            }
        }

        private void AddLegacyTile(Layer layer)
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

        private void AddSprite(Layer layer)
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
            AddLayer<Layer.LayerTilesData>(UndertaleRoom.LayerType.Tiles, "NewTilesLayer");
        }

        private void MenuItem_NewLayerBackground_Click(object sender, RoutedEventArgs e)
        {
            AddLayer<Layer.LayerBackgroundData>(UndertaleRoom.LayerType.Background, "NewBackgroundLayer");
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

        private void TileCellTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            BindingExpression binding = textBox.GetBindingExpression(TextBox.TextProperty);
            int x = ((int[])textBox.Tag)[0];
            int y = ((int[])textBox.Tag)[1];

            uint tileID;
            if (UInt32.TryParse(textBox.Text, out tileID))
            {
                TileCellConverter.CurrentLayer.TilesData.TileData[y][x] = tileID;
                Validation.ClearInvalid(binding);
            }
            else
            {
                // change value without losing its binding
                textBox.SetCurrentValue(TextBox.TextProperty, TileCellConverter.CurrentLayer.TilesData.TileData[y][x].ToString());

                ValidationError err = new(new DataErrorValidationRule(), binding) { ErrorContent = "Invalid tile ID" };
                Validation.MarkInvalid(binding, err);
                _ = Task.Run(() =>
                {
                    Thread.Sleep(1000);
                    Dispatcher.Invoke(() => Validation.ClearInvalid(binding));
                });
            }
        }
        private void OnLayerDataContentChange(object dataContext)
        {
            if (dataContext is Layer layer && layer.LayerType == LayerType.Tiles)
            {
                TileCellConverter.CurrentLayer = layer;
                TileCellConverter.TilesWidth = layer.TilesData.TilesX;
            }
            else
            {
                TileCellConverter.CurrentLayer = null;
                TileCellConverter.TilesWidth = 0;
            }

            TileCellConverter.CurrentPos = new int[] { 0, 0 };
        }
        private void LayerDataContent_Loaded(object sender, RoutedEventArgs e)
        {
            OnLayerDataContentChange(DataContext);
        }
        private void LayerDataContent_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            OnLayerDataContentChange(e.NewValue);
        }

        private void MenuItem_NewGMS1Tile_Click(object sender, RoutedEventArgs e)
        {
            AddGMS1Tile(this.DataContext as UndertaleRoom);
        }

        private void MenuItem_NewObjectInstance_Click(object sender, RoutedEventArgs e)
        {
            AddObjectInstance(this.DataContext as UndertaleRoom);
        }


        public static void GenerateSpriteCache(UndertaleRoom room)
        {
            if (room is null)
                return;

            ConcurrentDictionary<string, ConcurrentBag<UndertaleTexturePageItem>> textPages = new(); // text. page name - text. page item list
            UndertaleCachedImageLoader loader = new();

            List<Tile> tiles = null;
            List<Tuple<UndertaleTexturePageItem, List<Tuple<uint, uint, uint, uint>>>> tileTextures = null;
            List<object> allObjects = new();
            if (room.Flags.HasFlag(RoomEntryFlags.IsGMS2))
            {
                foreach (Layer layer in room.Layers)
                {
                    switch (layer.LayerType)
                    {
                        case LayerType.Assets:
                            if (tiles is null)
                                tiles = layer.AssetsData.LegacyTiles.ToList();
                            else
                                tiles.AddRange(layer.AssetsData.LegacyTiles);

                            allObjects.AddRange(layer.AssetsData.Sprites);
                            break;

                        case LayerType.Background:
                            allObjects.Add(layer.BackgroundData);
                            break;

                        case LayerType.Instances:
                            allObjects.AddRange(layer.InstancesData.Instances);
                            break;

                        case LayerType.Tiles:
                            // TODO
                            break;
                    }
                }
            }
            else
            {
                tiles = room.Tiles.ToList();

                allObjects.AddRange(room.Backgrounds);
                allObjects.AddRange(room.GameObjects);
            }

            tileTextures = tiles?.AsParallel()
                                 .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                                 .GroupBy(x => x.Tpag?.Name)
                                 .Select(x =>
                                 {
                                     return new Tuple<UndertaleTexturePageItem, List<Tuple<uint, uint, uint, uint>>>(
                                         x.First().Tpag, 
                                         x.Select(tile => new Tuple<uint, uint, uint, uint>(tile.SourceX, tile.SourceY, tile.Width, tile.Height))
                                          .Distinct()
                                          .ToList());
                                 })
                                 .ToList();

            _ = Parallel.ForEach(allObjects, (obj) =>
            {
                UndertaleTexturePageItem texture = obj switch
                {
                    Background bg => bg.BackgroundDefinition?.Texture,
                    GameObject gameObj => gameObj.ObjectDefinition?.Sprite?.Textures[0].Texture,

                    // GMS 2+
                    Layer.LayerBackgroundData bgData => bgData.Sprite?.Textures[0].Texture,
                    SpriteInstance sprite => sprite.Sprite?.Textures[0].Texture,
                    _ => null
                };

                if (texture is not null)
                {
                    string textPageName = texture.TexturePage.Name.Content;
                    _ = textPages.AddOrUpdate(textPageName, new ConcurrentBag<UndertaleTexturePageItem>() { texture }, (_, list) =>
                    {
                        list.Add(texture);
                        return list;
                    });
                }
            });

            foreach (string key in textPages.Keys)
            {
                ConcurrentBag<UndertaleTexturePageItem> bag = textPages[key];
                textPages[key] = new(bag.Distinct());
            }

            List<UndertaleTexturePageItem> list = new();
            foreach (var text in textPages.Values)
                list.AddRange(text);

            if (tileTextures is not null)
                foreach (var tileTexture in tileTextures)
                {
                    loader.Convert(tileTexture.Item1, null, tileTexture.Item2, null); // it's parallel itself
                }
            _ = Parallel.ForEach(list, (texture) =>
            {
                loader.Convert(texture, null, "generate", null);
            });
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
            item = item switch
            {
                Background bg => bg.BackgroundDefinition,
                GameObject obj => obj.ObjectDefinition,
                Tile tile => tile,
                SpriteInstance spr => spr,
                _ => null
            };

            if (item is null)
            {
                (container as ContentPresenter).Content = null;
                return null;
            }

            string resName = null;
            switch (item)
            {
                case UndertaleGameObject obj:
                    if (obj.Sprite is not null)
                        resName = "Obj";
                    break;

                case UndertaleBackground:
                    resName = "BG";
                    break;

                case Tile tile:
                    if (tile.Tpag is not null)
                        resName = "Tile";
                    break;

                case SpriteInstance spr:
                    if (spr.Sprite is not null)
                        resName = "Spr";
                    break;
            }
            if (resName is null)
            {
                (container as ContentPresenter).Content = null;
                return null;
            }

            return (DataTemplate)(container as FrameworkElement).FindResource(resName + "Template");
        }
    }

    public class BGColorConverter : IMultiValueConverter
    {
        private static readonly ColorConverter colorConv = new();

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Any(x => x is null))
                return null;

            bool isGMS2 = ((RoomEntryFlags)values[1]).HasFlag(RoomEntryFlags.IsGMS2);

            (values[0] as GeometryDrawing).Brush = new SolidColorBrush(Colors.Black);
            BindingOperations.SetBinding((values[0] as GeometryDrawing).Brush, SolidColorBrush.ColorProperty, new Binding(isGMS2 ? "BGColorLayer.BackgroundData.Color" : "BackgroundColor")
            {
                Converter = colorConv,
                Mode = BindingMode.OneWay
            });

            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
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
                        collection.Add(layer.BackgroundData);
                        break;
                    case UndertaleRoom.LayerType.Instances:
                        collection.Add(new CollectionContainer() { Collection = layer.InstancesData.Instances });
                        break;
                    case UndertaleRoom.LayerType.Assets:
                        collection.Add(new CollectionContainer() { Collection = layer.AssetsData.LegacyTiles });
                        collection.Add(new CollectionContainer() { Collection = layer.AssetsData.Sprites });
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
            string par = null;
            if (parameter is string)
                par = parameter as string;

            bool invert = par == "invert";
            bool isGMS2 = false;

            if (value is RoomEntryFlags flags)
            {
                if (par == "flags")
                {
                    if ((Application.Current.MainWindow as MainWindow).IsGMS2 == Visibility.Visible)
                    {
                        if (!flags.HasFlag(RoomEntryFlags.IsGMS2))
                        {
                            try
                            {
                                MessageBox.Show("Room flags of the GMS 2+ games must contain \"IsGMS2\" flag, else game would crash on that room.",
                                            "UndertaleModTool", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                            catch {}
                        }

                        flags |= RoomEntryFlags.IsGMS2 | RoomEntryFlags.IsGMS2_3;
                    }

                    return flags;
                }
                else
                    isGMS2 = flags.HasFlag(RoomEntryFlags.IsGMS2) ^ invert;
            }
            else if (value is Visibility vis)
                return vis == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;

            return targetType.Name switch
            {
                "Boolean" => !isGMS2,
                "Object" => !isGMS2,
                "Visibility" => isGMS2 ? Visibility.Collapsed : Visibility.Visible,
                _ => null,
            };
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is string par && par == "flags")
                return value;
            else
                throw new NotSupportedException();
        }
    }

    public class BGViewportConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            Rectangle rect = values[0] as Rectangle;
            bool tiledV = (bool)values[1];
            bool tiledH = (bool)values[2];
            bool stretch = (bool)values[3];

            if (tiledV || tiledH)
                (rect.Fill as ImageBrush).TileMode = TileMode.Tile;
            else
                (rect.Fill as ImageBrush).TileMode = TileMode.None;

            TranslateTransform translateT = (rect.RenderTransform as TransformGroup).Children.FirstOrDefault(t => t is TranslateTransform) as TranslateTransform;

            UndertaleTexturePageItem texture = null;
            double offsetV = 0;
            double offsetH = 0;
            double roomWidth = 0;
            double roomHeight = 0;
            double xOffset = 0;
            double yOffset = 0;

            if (rect.DataContext is Background bg)
            {
                if (bg.BackgroundDefinition?.Texture is null)
                    return new Rect(0, 0, 0, 0);

                texture = bg.BackgroundDefinition.Texture;

                bg.UpdateStretch();
                offsetV = bg.Y * (1 / bg.CalcScaleY);
                offsetH = bg.X * (1 / bg.CalcScaleX);
                xOffset = bg.X;
                yOffset = bg.Y;
                roomWidth = System.Convert.ToDouble(bg.ParentRoom.Width);
                roomHeight = System.Convert.ToDouble(bg.ParentRoom.Height);
            }
            else if (rect.DataContext is Layer.LayerBackgroundData bgData)
            {
                if (bgData.Sprite is null)
                    return new Rect(0, 0, 0, 0);

                Layer bgLayer = bgData.ParentLayer;
                texture = bgLayer.BackgroundData.Sprite.Textures[0].Texture;

                bgData.UpdateScale();
                offsetV = bgLayer.YOffset * (1 / bgData.CalcScaleY);
                offsetH = bgLayer.XOffset * (1 / bgData.CalcScaleX);
                xOffset = bgLayer.XOffset;
                yOffset = bgLayer.YOffset;
                roomWidth = System.Convert.ToDouble(bgLayer.ParentRoom.Width);
                roomHeight = System.Convert.ToDouble(bgLayer.ParentRoom.Height);
            }
            else
                return new Rect(0, 0, 0, 0);

            if (texture is null)
                return new Rect(0, 0, 0, 0);

            if (tiledV)
            {
                if (!stretch)
                    rect.SetCurrentValue(Rectangle.HeightProperty, roomHeight);         // changing value without losing its binding
                else
                    rect.GetBindingExpression(Rectangle.HeightProperty).UpdateTarget(); // restoring value from its source

                translateT.SetCurrentValue(TranslateTransform.YProperty, (double)0);
            }
            else
            {
                if (!stretch)
                    rect.GetBindingExpression(Rectangle.HeightProperty).UpdateTarget();
                else
                    rect.SetCurrentValue(Rectangle.HeightProperty, texture.SourceHeight - offsetV);

                BindingOperations.GetBindingExpression(translateT, TranslateTransform.YProperty).UpdateTarget();
            }

            if (tiledH)
            {
                if (!stretch)
                    rect.SetCurrentValue(Rectangle.WidthProperty, roomWidth);
                else
                    rect.GetBindingExpression(Rectangle.WidthProperty).UpdateTarget();

                translateT.SetCurrentValue(TranslateTransform.XProperty, (double)0);
            }
            else
            {
                if (!stretch)
                    rect.GetBindingExpression(Rectangle.WidthProperty).UpdateTarget();
                else
                    rect.SetCurrentValue(Rectangle.WidthProperty, texture.SourceWidth - offsetH);

                BindingOperations.GetBindingExpression(translateT, TranslateTransform.XProperty).UpdateTarget();
            }

            return new Rect(tiledH ? (stretch ? offsetH : xOffset) : 0,
                            tiledV ? (stretch ? offsetV : yOffset) : 0,
                            texture.SourceWidth, texture.SourceHeight);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [ValueConversion(typeof(LayerType), typeof(Visibility))]
    public class LayerTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is LayerType.Instances ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [ValueConversion(typeof(RoomEntryFlags), typeof(string))]
    public class GridSizeGroupConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool invert = false;
            if (parameter is string par)
                invert = par == "invert";

            bool res = value switch
            {
                RoomEntryFlags flags => !flags.HasFlag(RoomEntryFlags.IsGMS2),
                LayerType type => type is not LayerType.Instances,
                _ => false
            };
            return (res ^ invert) ? "s22" : "s00";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [ValueConversion(typeof(ContentControl), typeof(GridLength))]
    public class GridColumnSizeConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is ContentControl cont)
            {
                cont.UpdateLayout(); // calculate content elements size

                DependencyObject obj = VisualTreeHelper.GetChild(cont, 0);
                if (obj is not null && VisualTreeHelper.GetChild(obj, 0) is Grid childGrid)
                {
                    double columnMaxWidth = (cont.Parent as Grid).Children.Cast<FrameworkElement>()
                                                                          .Where(x => x is TextBlock && Grid.GetColumn(x) == 0)
                                                                          .Select(x => x.DesiredSize.Width)
                                                                          .Max();

                    return new GridLength(Math.Max(columnMaxWidth, childGrid.ColumnDefinitions[0].ActualWidth));
                }
            }

            return GridLength.Auto;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class TileCellConverter : IValueConverter
    {
        public static Layer CurrentLayer { get; set; }
        public static uint TilesWidth { get; set; }
        public static int[] CurrentPos { get; set; } = new int[] { 0, 0 };
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int[] currPos = (int[])CurrentPos.Clone();

            if (CurrentPos[0] >= TilesWidth - 1)
            {
                CurrentPos[0] = 0;
                CurrentPos[1]++;
            }
            else
                CurrentPos[0]++;

            return currPos;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BGLayerVisConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)values[0] && (bool)values[1] ? Visibility.Visible : Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [ValueConversion(typeof(uint), typeof(double))]
    public class ColorToOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((uint)value >> 24) / 255.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
