using Microsoft.Win32;
using System;
using System.Buffers;
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
using System.Runtime.InteropServices;
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
        private CanvasWithDict roomCanvas;

        public static readonly DoubleAnimation flashAnim = new(1, 0, TimeSpan.FromSeconds(0.75))
        {
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseIn }
        };
        private Storyboard currStoryboard;

        private ConcurrentDictionary<uint, Layer> roomObjDict = new();
        private ConcurrentDictionary<SpriteInstance, Layer> sprInstDict = new();

        public UndertaleRoomEditor()
        {
            InitializeComponent();

            Loaded += UndertaleRoomEditor_Loaded;
            DataContextChanged += UndertaleRoomEditor_DataContextChanged;
        }

        public void SaveImagePNG(Stream outfile)
        {
            if (roomCanvas is null)
            {
                if (MainWindow.FindVisualChild<CanvasWithDict>(RoomGraphics) is CanvasWithDict canv && canv.Name == "RoomCanvas")
                    roomCanvas = canv;
                else
                    throw new Exception("\"RoomCanvas\" not found.");
            }

            object prevOffset = visualOffProp.GetValue(roomCanvas);
            visualOffProp.SetValue(roomCanvas, new Vector(0, 0)); // (probably, there is a better way to fix the offset of the rendered picture)

            RenderTargetBitmap target = new((int)roomCanvas.RenderSize.Width, (int)roomCanvas.RenderSize.Height, 96, 96, PixelFormats.Pbgra32);

            target.Render(roomCanvas);

            PngBitmapEncoder encoder = new() { Interlace = PngInterlaceOption.Off };
            encoder.Frames.Add(BitmapFrame.Create(target));
            encoder.Save(outfile);

            visualOffProp.SetValue(roomCanvas, prevOffset);
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
        }

        private void UndertaleRoomEditor_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // "DataContextChanged" raised before "Loaded"
            if (IsLoaded)
            {
                RoomRootItem.IsSelected = false;
                RoomRootItem.IsSelected = true;

                ScrollViewer viewer = MainWindow.FindVisualChild<ScrollViewer>(RoomObjectsTree);
                viewer.ScrollToVerticalOffset(0);
                viewer.ScrollToHorizontalOffset(0);
            }

            UndertaleCachedImageLoader.Reset();
            CachedTileDataLoader.Reset();
            roomCanvas?.ObjectDict.Clear();
            roomObjDict.Clear();
            sprInstDict.Clear();

            if (DataContext is UndertaleRoom room)
            {
                GameObjItems.Header = room.Flags.HasFlag(RoomEntryFlags.IsGMS2)
                                      ? "Game objects (from all layers)"
                                      : "Game objects";
                room.SetupRoom();
                GenerateSpriteCache(DataContext as UndertaleRoom);

                if (room.Layers.Count > 0) // if GMS 2+
                {
                    Parallel.ForEach(room.Layers, (layer) =>
                    {
                        if (layer.LayerType == LayerType.Assets)
                        {
                            foreach (SpriteInstance spr in layer.AssetsData.Sprites)
                                sprInstDict.TryAdd(spr, layer);

                            foreach (Tile tile in layer.AssetsData.LegacyTiles)
                                roomObjDict.TryAdd(tile.InstanceID, layer);
                        }
                        else if (layer.LayerType == LayerType.Instances)
                        {
                            foreach (GameObject obj in layer.InstancesData.Instances)
                                roomObjDict.TryAdd(obj.InstanceID, layer);
                        }
                    });
                }
            }
        }

        private void RoomCanvas_Loaded(object sender, RoutedEventArgs e)
        {
            roomCanvas = sender as CanvasWithDict;
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (currStoryboard is not null)
            {
                currStoryboard.Stop(this);
                currStoryboard.Remove(this);
            }

            // I can't bind it directly because then clicking on the headers makes WPF explode because it tries to attach the header as child of ObjectEditor
            // TODO: find some better workaround
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
                            obj1 = VisualTreeHelper.GetChild(roomCanvas.ObjectDict[layer.BackgroundData], 0);
                        else if (layer.LayerType == LayerType.Tiles)
                            obj1 = VisualTreeHelper.GetChild(roomCanvas.ObjectDict[layer.TilesData], 0);
                        else
                            return;
                    }
                    else if (obj is View)
                        return;
                    else
                        obj1 = VisualTreeHelper.GetChild(roomCanvas.ObjectDict[obj], 0);

                    (obj1 as FrameworkElement).BringIntoView();

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
            UndertaleObject clickedObj = (sender as FrameworkElement).DataContext as UndertaleObject;
            UndertaleRoom room = this.DataContext as UndertaleRoom;
            Layer layer = null;
            if (room.Layers.Count > 0)
                layer = clickedObj switch
                {
                    Tile or GameObject => roomObjDict[(clickedObj as RoomObject).InstanceID],
                    SpriteInstance spr => sprInstDict[spr],
                    _ => null
                };

            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
            {
                if (clickedObj is GameObject)
                {
                    if (layer != null && layer.InstancesData == null)
                    {
                        MainWindow.ShowError("Must be on an instances layer");
                        return;
                    }
                    var other = clickedObj as GameObject;
                    var newObj = new GameObject
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

                    if (layer != null)
                    {
                        int index = layer.InstancesData.Instances.IndexOf(other);
                        layer.InstancesData.Instances.Insert(index + 1, newObj);
                        roomObjDict.TryAdd(newObj.InstanceID, layer);
                    }
                    else
                    {
                        int index = room.GameObjects.IndexOf(other);
                        room.GameObjects.Insert(index + 1, newObj);
                    }

                    clickedObj = newObj;
                }
                else if (clickedObj is Tile)
                {
                    if (layer != null && layer.AssetsData == null)
                    {
                        MainWindow.ShowError("Must be on an assets layer");
                        return;
                    }
                    var other = clickedObj as Tile;
                    var newObj = new Tile
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

                    if (layer != null)
                    {
                        int index = layer.AssetsData.LegacyTiles.IndexOf(other);
                        layer.AssetsData.LegacyTiles.Insert(index + 1, newObj);
                        roomObjDict.TryAdd(newObj.InstanceID, layer);
                    }
                    else
                    {
                        int index = room.Tiles.IndexOf(other);
                        room.Tiles.Insert(index + 1, newObj);
                    }

                    clickedObj = newObj;
                }
                else if (clickedObj is SpriteInstance)
                {
                    if (layer != null && layer.AssetsData == null)
                    {
                        MainWindow.ShowError("Must be on an assets layer");
                        return;
                    }

                    string graphicName = "graphic_" + ((uint)new Random().Next(-int.MaxValue, int.MaxValue)).ToString("X8");
                    var other = clickedObj as SpriteInstance;
                    var newObj = new SpriteInstance
                    {
                        Name = new UndertaleString(graphicName),
                        Sprite = other.Sprite,
                        X = other.X,
                        Y = other.Y,
                        ScaleX = other.ScaleX,
                        ScaleY = other.ScaleY,
                        Color = other.Color,
                        AnimationSpeed = other.AnimationSpeed,
                        AnimationSpeedType = other.AnimationSpeedType,
                        FrameIndex = other.FrameIndex,
                        Rotation = other.Rotation,
                    };

                    int index = layer.AssetsData.Sprites.IndexOf(other);
                    layer.AssetsData.Sprites.Insert(index + 1, newObj);
                    sprInstDict.TryAdd(newObj, layer);

                    clickedObj = newObj;
                }
            }

            SelectObject(clickedObj);

            var mousePos = e.GetPosition(RoomGraphics);
            if (clickedObj is GameObject || clickedObj is Tile || clickedObj is SpriteInstance)
            {
                movingObj = clickedObj;
                if (movingObj is GameObject)
                {
                    var other = movingObj as GameObject;
                    var undoObj = new GameObject()
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
                    hotpointX = mousePos.X - (movingObj as GameObject).X;
                    hotpointY = mousePos.Y - (movingObj as GameObject).Y;
                }
                else if (movingObj is Tile)
                {
                    var other = movingObj as Tile;
                    var undoObj = new Tile()
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
                    hotpointX = mousePos.X - (movingObj as Tile).X;
                    hotpointY = mousePos.Y - (movingObj as Tile).Y;
                }
                else if (movingObj is SpriteInstance)
                {
                    var other = clickedObj as SpriteInstance;
                    var undoObj = new SpriteInstance
                    {
                        Name = other.Name,
                        Sprite = other.Sprite,
                        X = other.X,
                        Y = other.Y,
                        ScaleX = other.ScaleX,
                        ScaleY = other.ScaleY,
                        Color = other.Color,
                        AnimationSpeed = other.AnimationSpeed,
                        AnimationSpeedType = other.AnimationSpeedType,
                        FrameIndex = other.FrameIndex,
                        Rotation = other.Rotation,
                    };
                    undoStack.Push(undoObj);
                    hotpointX = mousePos.X - (movingObj as SpriteInstance).X;
                    hotpointY = mousePos.Y - (movingObj as SpriteInstance).Y;
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

            if (placedTiles.Contains(gridMouse))
                return;

            Layer layer = null;
            if (room.Layers.Count > 0)
                layer = other switch
                {
                    Tile or GameObject => roomObjDict[(other as RoomObject).InstanceID],
                    SpriteInstance spr => sprInstDict[spr],
                    _ => null
                };

            if (layer is not null && layer.AssetsData is null && layer.InstancesData is null)
            {
                return;
            }

            placedTiles.Add(gridMouse);
            placingTiles = true;

            if (other is Tile)
            {
                var otherTile = other as Tile;
                var newObj = new Tile
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
                    InstanceID = mainWindow.Data.GeneralInfo.LastTile++,
                    ScaleX = otherTile.ScaleX,
                    ScaleY = otherTile.ScaleY,
                    Color = otherTile.Color
                };

                if (layer != null)
                {
                    layer.AssetsData.LegacyTiles.Add(newObj);
                    roomObjDict.TryAdd(newObj.InstanceID, layer);
                }
                else
                    room.Tiles.Add(newObj);

                SelectObject(newObj);
            }
            else if (other is GameObject)
            {
                var otherObject = other as GameObject;
                var newObj = new GameObject
                {
                    X = (int)gridMouse.X,
                    Y = (int)gridMouse.Y,
                    ObjectDefinition = otherObject.ObjectDefinition,
                    InstanceID = mainWindow.Data.GeneralInfo.LastObj++,
                    CreationCode = otherObject.CreationCode,
                    ScaleX = otherObject.ScaleX,
                    ScaleY = otherObject.ScaleY,
                    Color = otherObject.Color,
                    Rotation = otherObject.Rotation,
                    PreCreateCode = otherObject.PreCreateCode
                };

                room.GameObjects.Add(newObj);
                if (layer != null)
                {
                    layer.InstancesData.Instances.Add(newObj);
                    roomObjDict.TryAdd(newObj.InstanceID, layer);
                }

                SelectObject(newObj);
            }
            else if (other is SpriteInstance)
            {
                string graphicName = "graphic_" + ((uint)new Random().Next(-int.MaxValue, int.MaxValue)).ToString("X8");

                var otherObject = other as SpriteInstance;
                var newObj = new SpriteInstance
                {
                    Name = new UndertaleString(graphicName),
                    Sprite = otherObject.Sprite,
                    X = (int)gridMouse.X,
                    Y = (int)gridMouse.Y,
                    ScaleX = otherObject.ScaleX,
                    ScaleY = otherObject.ScaleY,
                    Color = otherObject.Color,
                    AnimationSpeed = otherObject.AnimationSpeed,
                    AnimationSpeedType = otherObject.AnimationSpeedType,
                    FrameIndex = otherObject.FrameIndex,
                    Rotation = otherObject.Rotation,
                };

                if (layer != null)
                {
                    layer.AssetsData.Sprites.Add(newObj);
                    sprInstDict.TryAdd(newObj, layer);
                }

                SelectObject(newObj);
            }
        }

        private void RectangleBackground_MouseDown(object sender, MouseButtonEventArgs e)
        {
            UndertaleRoom room = DataContext as UndertaleRoom;
            var other = selectedObject;

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

                if (movingObj is GameObject)
                {
                    (movingObj as GameObject).X = tgtX;
                    (movingObj as GameObject).Y = tgtY;
                }
                else if (movingObj is Tile)
                {
                    (movingObj as Tile).X = tgtX;
                    (movingObj as Tile).Y = tgtY;
                }
                else if (movingObj is SpriteInstance)
                {
                    (movingObj as SpriteInstance).X = tgtX;
                    (movingObj as SpriteInstance).Y = tgtY;
                }
            }
        }

        double scaleOriginX, scaleOriginY;
        private void RectangleTile_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var element = sender as Canvas;
            var tileSelector = element.FindName("TileSelector") as Rectangle;
            var mousePos = e.GetPosition(element);
            var clickedTile = tileSelector.DataContext as Tile;
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

            var clickedTile = tileSelector.DataContext as Tile;

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

        private UndertaleObject selectedObject;
        private void SelectObject(UndertaleObject obj)
        {
            // TODO: enable virtualizing of RoomObjectsTree and make this method work with it

            selectedObject = obj;

            try
            {
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
                    case Layer.LayerTilesData:
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

                UndertaleObject obj1;
                obj1 = obj switch
                {
                    Layer.LayerBackgroundData bgData => bgData.ParentLayer,
                    Layer.LayerTilesData tileData => tileData.ParentLayer,
                    _ => obj
                };
                if (resListView.ItemContainerGenerator.ContainerFromItem(obj1) is TreeViewItem resItem)
                {
                    resItem.IsSelected = true;

                    mainTreeViewer.UpdateLayout();
                    mainTreeViewer.ScrollToHorizontalOffset(0);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("SelectObject() error - " + ex.Message);
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

                    var obj = new GameObject()
                    {
                        X = (int)mousePos.X,
                        Y = (int)mousePos.Y,
                        ObjectDefinition = droppedObject,
                        InstanceID = mainWindow.Data.GeneralInfo.LastObj++
                    };
                    room.GameObjects.Add(obj);
                    roomObjDict.TryAdd(obj.InstanceID, layer);
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

            (this.DataContext as UndertaleRoom)?.SetupRoom(false);
        }

        private void RoomObjectsTree_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                UndertaleRoom room = this.DataContext as UndertaleRoom;
                UndertaleObject selectedObj = ObjectEditor.Content as UndertaleObject;

                if (selectedObj is Background bg)
                {
                    bg.Enabled = false;
                    bg.BackgroundDefinition = null;

                    ObjectEditor.Content = null;
                }
                else if (selectedObj is View view)
                {
                    view.Enabled = false;

                    ObjectEditor.Content = null;
                }
                else if (selectedObj is Tile tile)
                {
                    if (mainWindow.IsGMS2 == Visibility.Visible)
                    {
                        foreach (var layer in room.Layers)
                            if (layer.AssetsData != null)
                                layer.AssetsData.LegacyTiles.Remove(tile);
                        roomObjDict.Remove(tile.InstanceID, out _);
                    }

                    room.Tiles.Remove(tile);

                    ObjectEditor.Content = null;
                }
                else if (selectedObj is GameObject gameObj)
                {
                    if (mainWindow.IsGMS2 == Visibility.Visible)
                    {
                        foreach (var layer in room.Layers)
                            if (layer.InstancesData != null)
                                layer.InstancesData.Instances.Remove(gameObj);
                        roomObjDict.Remove(gameObj.InstanceID, out _);
                    }

                    room.GameObjects.Remove(gameObj);

                    ObjectEditor.Content = null;
                }
                else if (selectedObj is SpriteInstance sprInst)
                {
                    foreach (var layer in room.Layers)
                        if (layer.AssetsData != null)
                            layer.AssetsData.Sprites.Remove(sprInst);

                    sprInstDict.Remove(sprInst, out _);

                    ObjectEditor.Content = null;
                }
                else if (selectedObj is Layer layer)
                {
                    if (layer.InstancesData != null)
                        foreach (var go in layer.InstancesData.Instances)
                            room.GameObjects.Remove(go);

                    foreach (var pair in roomObjDict)
                        if (pair.Value == layer)
                            roomObjDict.Remove(pair.Key, out _);

                    foreach (var pair in sprInstDict)
                        if (pair.Value == layer)
                            sprInstDict.Remove(pair.Key, out _);

                    room.Layers.Remove(layer);

                    if (layer.LayerType == LayerType.Background)
                    {
                        (RoomGraphics.ItemsSource as CompositeCollection).Remove(layer.BackgroundData);
                        room.UpdateBGColorLayer();
                    }
                    else if (layer.LayerType == LayerType.Tiles)
                        (RoomGraphics.ItemsSource as CompositeCollection).Remove(layer.TilesData);

                    ObjectEditor.Content = null;
                }
            }

            int dir = 0;
            if (e.Key == Key.OemMinus)
                dir = -1;
            else if (e.Key == Key.OemPlus)
                dir = 1;

            if (dir != 0)
            {
                UndertaleRoom room = this.DataContext as UndertaleRoom;
                UndertaleObject selectedObj = ObjectEditor.Content as UndertaleObject;
                Layer layer = null;
                if (room.Layers.Count > 0)
                    layer = selectedObj switch
                    {
                        Tile or GameObject => roomObjDict[(selectedObj as RoomObject).InstanceID],
                        SpriteInstance spr => sprInstDict[spr],
                        _ => null
                    };

                IList list = selectedObj switch
                {
                    Tile tile => layer is null ? room.Tiles : layer.AssetsData.LegacyTiles,
                    GameObject gameObj => layer is null ? room.GameObjects : layer.InstancesData.Instances,
                    SpriteInstance sprInst => layer.AssetsData.Sprites,
                    _ => null
                };
                if (list is null)
                {
                    Debug.WriteLine($"Can't change object position - list of selected object not found.");
                    return;
                }

                int index = list.IndexOf(selectedObj);
                if ((dir == -1 && index > 0) || (dir == 1 && index < list.Count - 1))
                {
                    int prevIndex = index + dir;
                    var prevIndexObj = list[prevIndex];
                    list[prevIndex] = selectedObj;
                    list[index] = prevIndexObj;

                    if (layer is not null)
                    {
                        // swap back objects in "ObjectDict"
                        var rect = roomCanvas.ObjectDict[selectedObj];
                        var rectPrev = roomCanvas.ObjectDict[prevIndexObj as UndertaleObject];
                        roomCanvas.ObjectDict[selectedObj] = rectPrev;
                        roomCanvas.ObjectDict[prevIndexObj as UndertaleObject] = rect;
                    }
                }

                SelectObject(selectedObj);
            }
        }

        private void RoomObjectsTree_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            object sel = (sender as TreeView).SelectedItem;
            if (sel is GameObject)
                mainWindow.ChangeSelection((sel as GameObject).ObjectDefinition);
            if (sel is Background)
                mainWindow.ChangeSelection((sel as Background).BackgroundDefinition);
            if (sel is Tile)
                mainWindow.ChangeSelection((sel as Tile).ObjectDefinition);
            if (sel is SpriteInstance)
                mainWindow.ChangeSelection((sel as SpriteInstance).Sprite);
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
                if (undoObject is GameObject && ObjectEditor.Content is GameObject)
                {
                    var toChange = ObjectEditor.Content as GameObject;
                    var undoGameObject = undoObject as GameObject;
                    if (toChange.InstanceID == undoGameObject.InstanceID)
                    {
                        toChange.X = undoGameObject.X;
                        toChange.Y = undoGameObject.Y;
                    }
                }
                if (undoObject is Tile && ObjectEditor.Content is Tile)
                {
                    var toChange = ObjectEditor.Content as Tile;
                    var undoGameObject = undoObject as Tile;
                    if (toChange.InstanceID == undoGameObject.InstanceID)
                    {
                        toChange.X = undoGameObject.X;
                        toChange.Y = undoGameObject.Y;
                    }
                }
                (this.DataContext as UndertaleRoom)?.SetupRoom(false);
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

                Layer layer = null;
                if (room.Layers.Count > 0)
                    layer = copied switch
                    {
                        Tile or GameObject => roomObjDict[(copied as RoomObject).InstanceID],
                        SpriteInstance spr => sprInstDict[spr],
                        _ => null
                    };

                if (mainWindow.IsGMS2 == Visibility.Visible && layer == null)
                {
                    MainWindow.ShowError("Must paste onto a layer");
                    return;
                }

                if (copied is GameObject)
                {
                    if (layer != null && layer.InstancesData == null)
                    {
                        MainWindow.ShowError("Must be on an instances layer");
                        return;
                    }
                    var other = copied as GameObject;
                    var obj = new GameObject();
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
                    {
                        roomObjDict.TryAdd(obj.InstanceID, layer);
                        layer.InstancesData.Instances.Add(obj);
                    }

                    SelectObject(obj);
                }
                if (copied is Tile)
                {
                    if (layer != null && layer.AssetsData == null)
                    {
                        MainWindow.ShowError("Must be on an assets layer");
                        return;
                    }
                    var other = copied as Tile;
                    var obj = new Tile();
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
                    {
                        roomObjDict.TryAdd(obj.InstanceID, layer);
                        layer.AssetsData.LegacyTiles.Add(obj);
                    }
                    else
                        room.Tiles.Add(obj);
                    SelectObject(obj);
                }
            }

            (this.DataContext as UndertaleRoom)?.SetupRoom(false);
        }

        private void AddLayer<T>(LayerType type, string name) where T : Layer.LayerData, new()
        {
            UndertaleRoom room = this.DataContext as UndertaleRoom;
            if (room is null)
            {
                // (not sure if it's possible)
                MainWindow.ShowError("Room is null.");
                return;
            }

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

            if (layer.LayerType == LayerType.Assets)
            {
                // create a new pointer list (if null)
                layer.AssetsData.LegacyTiles ??= new UndertalePointerList<Tile>();
                // create new sprite pointer list (if null)
                layer.AssetsData.Sprites ??= new UndertalePointerList<SpriteInstance>();
                // create new sequence pointer list (if null)
                layer.AssetsData.Sequences ??= new UndertalePointerList<SequenceInstance>();
            }
            else if (layer.LayerType == LayerType.Tiles)
            {
                // create new tile data (if null)
                layer.TilesData.TileData ??= Array.Empty<uint[]>();
            }

            SelectObject(layer);
            room.SetupRoom(false);
        }

        private void AddObjectInstance(UndertaleRoom room)
        {
            var newobject = new GameObject { InstanceID = mainWindow.Data.GeneralInfo.LastObj++ };
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
                roomObjDict.TryAdd(newObject.InstanceID, layer);
                SelectObject(newObject);
            }
        }

        private void AddLegacyTile(Layer layer)
        {
            if (layer is not null)
            {
                // add pointer list if one doesn't already exist
                layer.AssetsData.LegacyTiles ??= new UndertalePointerList<Tile>();

                // add sprite pointer list if one doesn't already exist
                layer.AssetsData.Sprites ??= new UndertalePointerList<SpriteInstance>();

                // add tile to list
                var tile = new Tile { InstanceID = mainWindow.Data.GeneralInfo.LastTile++ };
                tile._SpriteMode = true;
                layer.AssetsData.LegacyTiles.Add(tile);
                roomObjDict.TryAdd(tile.InstanceID, layer);

                SelectObject(tile);
            }
        }

        private void AddSprite(Layer layer)
        {
            if (layer is not null)
            {
                // add pointer list if one doesn't already exist
                layer.AssetsData.Sprites ??= new UndertalePointerList<SpriteInstance>();

                // add tile to list
                var spriteinstance = new SpriteInstance();
                layer.AssetsData.Sprites.Add(spriteinstance);
                sprInstDict.TryAdd(spriteinstance, layer);

                SelectObject(spriteinstance);
            }
        }

        private void AddGMS1Tile(UndertaleRoom room)
        {
            // add tile to list
            var tile = new Tile { InstanceID = mainWindow.Data.GeneralInfo.LastTile++ };
            room.Tiles.Add(tile);
            SelectObject(tile);
        }

        private void MenuItem_NewLayerInstances_Click(object sender, RoutedEventArgs e)
        {
            AddLayer<Layer.LayerInstancesData>(LayerType.Instances, "NewInstancesLayer");
        }

        private void MenuItem_NewLayerTiles_Click(object sender, RoutedEventArgs e)
        {
            AddLayer<Layer.LayerTilesData>(LayerType.Tiles, "NewTilesLayer");
        }

        private void MenuItem_NewLayerBackground_Click(object sender, RoutedEventArgs e)
        {
            AddLayer<Layer.LayerBackgroundData>(LayerType.Background, "NewBackgroundLayer");
        }

        private void MenuItem_NewLayerAssets_Click(object sender, RoutedEventArgs e)
        {
            AddLayer<Layer.LayerAssetsData>(LayerType.Assets, "NewAssetsLayer");
        }

        private void MenuItem_NewGMS2ObjectInstance_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuitem = sender as MenuItem;
            AddGMS2ObjectInstance(menuitem.DataContext as Layer);
        }

        private void MenuItem_NewLegacyTile_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuitem = sender as MenuItem;
            AddLegacyTile(menuitem.DataContext as Layer);
        }

        private void MenuItem_NewSprite_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuitem = sender as MenuItem;
            AddSprite(menuitem.DataContext as Layer);
        }

        private void MenuItem_NewGMS1Tile_Click(object sender, RoutedEventArgs e)
        {
            AddGMS1Tile(this.DataContext as UndertaleRoom);
        }

        private void TileDataExport_Click(object sender, RoutedEventArgs e)
        {
            if (RoomObjectsTree.SelectedItem is Layer layer)
            {
                if (layer.TilesData.TileData.Length == 0)
                {
                    MainWindow.ShowError("Tile data is empty.");
                    return;
                }

                StringBuilder sb = new();
                foreach (uint[] dataRow in layer.TilesData.TileData)
                    sb.AppendLine(String.Join(";", dataRow.Select(x => x.ToString())));

                string dataFolder = System.IO.Path.GetDirectoryName((Application.Current.MainWindow as MainWindow).FilePath);
                string filePath = System.IO.Path.Combine(dataFolder, $"{layer.LayerName.Content}_tiledata.csv");

                try
                {
                    File.WriteAllText(filePath, sb.ToString());
                }
                catch (Exception ex)
                {
                    MainWindow.ShowError($"Error while saving the file - \"{ex.Message}\".");
                    return;
                }

                MainWindow.ShowMessage("Exported file path: " + filePath);
            }
        }
        private void TileDataImport_Click(object sender, RoutedEventArgs e)
        {
            if (RoomObjectsTree.SelectedItem is Layer layer)
            {
                if (layer.TilesData.TilesX == 0 || layer.TilesData.TilesY == 0)
                {
                    MainWindow.ShowError("Tile data size can't be zero.");
                    return;
                }

                OpenFileDialog dlg = new()
                {
                    DefaultExt = "csv",
                    Filter = "CSV table|*.csv|All files|*"
                };

                if (dlg.ShowDialog() == true)
                {
                    uint[][] tileDataNew = (uint[][])layer.TilesData.TileData.Clone();
                    string[] tileDataLines = null;
                    try
                    {
                        tileDataLines = File.ReadAllLines(dlg.FileName);
                    }
                    catch (Exception ex)
                    {
                        MainWindow.ShowError($"Error while opening file - \"{ex.Message}\".");
                        return;
                    }

                    if (tileDataLines?.Length != tileDataNew.Length)
                    {
                        MainWindow.ShowError("Error - selected file line count doesn't match tile layer height.");
                        return;
                    }

                    for (int i = 0; i < tileDataLines.Length; i++)
                    {
                        uint[] dataRow;
                        try
                        {
                            dataRow = tileDataLines[i].Split(';').Select(x => UInt32.Parse(x)).ToArray();
                        }
                        catch (Exception ex)
                        {
                            MainWindow.ShowError($"Error while parsing line {i + 1} - \"{ex.Message}\".");
                            return;
                        }

                        if (dataRow.Length != layer.TilesData.TilesX)
                        {
                            MainWindow.ShowError($"Length of line {i + 1} is not equal to the tile data width.");
                            return;
                        }

                        tileDataNew[i] = dataRow;
                    }

                    layer.TilesData.TileData = tileDataNew;

                    MainWindow.ShowMessage("Imported successfully.");
                }
            }
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

                            allObjects.AddRange(layer.AssetsData.Sprites.Where(s => s.Sprite?.Textures.Count > 0));
                            break;

                        case LayerType.Background:
                            if (layer.BackgroundData.Sprite?.Textures.Count > 0)
                                allObjects.Add(layer.BackgroundData);
                            break;

                        case LayerType.Instances:
                            allObjects.AddRange(layer.InstancesData.Instances);
                            break;
                    }
                }
            }
            else
            {
                tiles = room.Tiles.ToList();

                allObjects.AddRange(room.Backgrounds);
                allObjects.AddRange(room.GameObjects.Where(g => g.ObjectDefinition?.Sprite?.Textures.Count > 0));
            }

            tileTextures = tiles?.AsParallel()
                                 .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                                 .GroupBy(x => x.Tpag?.Name?.Content ?? (mainWindow.Data.TexturePageItems.IndexOf(x.Tpag) + 1).ToString())
                                 .Where(x => x.Key != "0")
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

                if (texture is not null && texture.TexturePage is not null)
                {
                    string textPageName = texture.TexturePage.Name?.Content;
                    if (textPageName is null)
                    {
                        textPageName = (mainWindow.Data.TexturePageItems.IndexOf(texture) + 1).ToString();
                        if (textPageName == "0")
                            return;
                    }

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


    public partial class CanvasWithDict : Canvas
    {
        public Dictionary<UndertaleObject, FrameworkElement> ObjectDict { get; } = new();

        protected override void OnVisualChildrenChanged(DependencyObject visualAdded, DependencyObject visualRemoved)
        {
            if (visualAdded is ContentPresenter presAdded && presAdded.DataContext is UndertaleObject objAdded)
                ObjectDict[objAdded] = visualAdded as FrameworkElement;

            if (visualRemoved is ContentPresenter presRemoved && presRemoved.DataContext is UndertaleObject objRemoved)
                ObjectDict.Remove(objRemoved);
        }
    }


    [ValueConversion(typeof(ObservableCollection<GameObject>), typeof(int))]
    public class ObjCenterXConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            GameObject obj = value as GameObject;
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

    [ValueConversion(typeof(ObservableCollection<GameObject>), typeof(int))]
    public class ObjXOffsetConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            GameObject obj = value as GameObject;
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

    [ValueConversion(typeof(ObservableCollection<GameObject>), typeof(int))]
    public class ObjCenterYConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            GameObject obj = value as GameObject;
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
            if (container is FrameworkElement && item != null && item is Layer)
            {
                switch ((item as Layer).LayerType)
                {
                    case LayerType.Instances:
                        return InstancesDataTemplate;
                    case LayerType.Tiles:
                        return TilesDataTemplate;
                    case LayerType.Assets:
                        return AssetsDataTemplate;
                    case LayerType.Background:
                        return BackgroundDataTemplate;
                }
            }

            return null;
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
            IList<Layer> layers = value as IList<Layer>;
            foreach (var layer in layers.OrderByDescending((x) => x?.LayerDepth ?? 0))
            {
                if (layer == null)
                    continue;

                switch (layer.LayerType)
                {
                    case LayerType.Background:
                        collection.Add(layer.BackgroundData);
                        break;
                    case LayerType.Instances:
                        collection.Add(new CollectionContainer() { Collection = layer.InstancesData.Instances });
                        break;
                    case LayerType.Assets:
                        collection.Add(new CollectionContainer() { Collection = layer.AssetsData.LegacyTiles });
                        collection.Add(new CollectionContainer() { Collection = layer.AssetsData.Sprites });
                        break;
                    case LayerType.Tiles:
                        collection.Add(layer.TilesData);
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
                                MainWindow.ShowError("Room flags of GMS 2+ games must contain the \"IsGMS2\" flag, otherwise the game will crash when loading that room.", false);
                            }
                            catch {}
                        }

                        flags |= RoomEntryFlags.IsGMS2;
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
                    double childColumnWidth = childGrid.ColumnDefinitions[0].ActualWidth;
                    if (childColumnWidth < columnMaxWidth)
                    {
                        childGrid.ColumnDefinitions[0].Width = new GridLength(columnMaxWidth);

                        return new GridLength(columnMaxWidth);
                    }
                    else
                        return new GridLength(childColumnWidth);
                }
            }

            return GridLength.Auto;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
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

    public class TileLayerTemplateSelector : DataTemplateSelector
    {
        public static byte ForcedMode { get; set; }
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            Layer.LayerTilesData tilesData = item as Layer.LayerTilesData;

            int count = 0;
            foreach (uint[] row in tilesData.TileData)
                foreach (uint tileID in row)
                    if (tileID != 0)
                        count++;

            string name;
            if (ForcedMode == 0)
                name = count > 1000 ? "TileLayerImage" : "TileLayerRectangles";
            else
                name = ForcedMode == 1 ? "TileLayerImage" : "TileLayerRectangles";

            return (DataTemplate)(container as FrameworkElement).FindResource(name + "Template");
        }
    }

    public class TileRectangle
    {
        public ImageSource ImageSrc { get; set; }
        public uint X { get; set; }
        public uint Y { get; set; }
        public uint Width { get; set; }
        public uint Height { get; set; }
        public double ScaleX { get; set; }
        public double ScaleY { get; set; }
        public double Angle { get; set; }

        public TileRectangle(ImageSource imageSrc, uint x, uint y, uint width, uint height, double scaleX, double scaleY, double angle)
        {
            ImageSrc = imageSrc;
            X = x;
            Y = y;
            Width = width;
            Height = height;
            ScaleX = scaleX;
            ScaleY = scaleY;
            Angle = angle;
        }
    }
    public class TileRectanglesConverter : IMultiValueConverter
    {
        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteObject([In] IntPtr hObject);

        public static ConcurrentDictionary<Tuple<string, uint>, ImageSource> TileCache { get; set; } = new();
        private static CachedTileDataLoader loader = new();

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is Layer.LayerTilesData tilesData)
            {
                UndertaleBackground tilesBG = tilesData.Background;

                if (tilesBG is null)
                    return null;

                if ((loader.Convert(new object[] { tilesData }, null, "cache", null) as string) == "Error")
                    return null;

                try
                {
                    HashSet<uint> usedIDs = new();
                    List<Tuple<uint, uint, uint>> tileList = new();
                    for (uint y = 0; y < tilesData.TilesY; y++)
                        for (uint x = 0; x < tilesData.TilesX; x++)
                        {
                            uint tileID = tilesData.TileData[y][x];
                            if (tileID != 0)
                                tileList.Add(new(tileID, x, y));

                            usedIDs.Add(tileID & 0x0FFFFFFF); // removed tile flag
                        }

                    // convert Bitmaps to ImageSources (only used IDs) 
                    _ = Parallel.ForEach(usedIDs, (id) =>
                    {
                        Tuple<string, uint> tileKey = new(tilesBG.Texture.Name.Content, id);

                        IntPtr bmpPtr = CachedTileDataLoader.TileCache[tileKey].GetHbitmap();
                        ImageSource spriteSrc = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(bmpPtr, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                        DeleteObject(bmpPtr);
                        spriteSrc.Freeze(); // allow UI thread access

                        TileCache.TryAdd(tileKey, spriteSrc);
                    });

                    var tileArr = new TileRectangle[tileList.Count];
                    uint w = tilesBG.GMS2TileWidth;
                    uint h = tilesBG.GMS2TileHeight;
                    uint maxID = tilesData.Background.GMS2TileIds.Select(x => x.ID).Max();
                    _ = Parallel.For(0, tileList.Count, (i) =>
                    {
                        var tile = tileList[i];
                        uint id = tile.Item1;
                        uint realID;
                        double scaleX = 1;
                        double scaleY = 1;
                        double angle = 0;

                        if (id > maxID)
                        {
                            realID = id & 0x0FFFFFFF; // remove tile flag
                            if (realID > maxID)
                            {
                                Debug.WriteLine("Tileset \"" + tilesData.Background.Name.Content + "\" doesn't contain tile ID " + realID);
                                return;
                            }

                            switch (id >> 28)
                            {
                                case 1:
                                    scaleX = -1;
                                    break;
                                case 2:
                                    scaleY = -1;
                                    break;
                                case 3:
                                    scaleX = scaleY = -1;
                                    break;
                                case 4:
                                    angle = 90;
                                    break;
                                case 5:
                                    angle = 90;
                                    scaleY = -1;
                                    break;
                                case 6:
                                    angle = 270;
                                    scaleY = -1;
                                    break;
                                case 7:
                                    angle = 270;
                                    break;

                                default:
                                    Debug.WriteLine("Tile of " + tilesData.ParentLayer.LayerName + " located at (" + tile.Item2 + ", " + tile.Item3 + ") has unknown flag.");
                                    break;
                            }
                        }
                        else
                            realID = id;

                        tileArr[i] = new(TileCache[new(tilesBG.Texture.Name.Content, realID)], tile.Item2 * w, tile.Item3 * h, w, h, scaleX, scaleY, angle);
                    });

                    return tileArr;
                }
                catch (Exception ex)
                {
                    MainWindow.ShowError($"An error occured while generating \"Rectangles\" for tile layer {tilesData.ParentLayer.LayerName}.\n\n{ex}");
                    return null;
                }
            }
            else
                return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
