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
using System.Text.RegularExpressions;
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
        private static readonly MainWindow mainWindow = Application.Current.MainWindow as MainWindow;
        private static readonly Regex trailingNumberRegex = new(@"\d+$", RegexOptions.Compiled);
        private readonly Type[] movableTypes = { typeof(Layer), typeof(GameObject), typeof(Tile), typeof(SpriteInstance), typeof(SequenceInstance), typeof(ParticleSystemInstance) };

        // used for the flashing animation when a room object is selected
        public static Dictionary<UndertaleObject, FrameworkElement> ObjElemDict { get; } = new();

        public UndertalePath PreviewPath
        {
            get => (UndertalePath)GetValue(PreviewPathProperty);
            set => SetValue(PreviewPathProperty, value);
        }

        private Stack<UndertaleObject> undoStack = new();
        private Canvas roomCanvas;

        public static readonly DoubleAnimation flashAnim = new(1, 0, TimeSpan.FromSeconds(0.75))
        {
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseIn }
        };
        private Storyboard currStoryboard;

        private ConcurrentDictionary<uint, Layer> roomObjDict = new();
        private ConcurrentDictionary<SpriteInstance, Layer> sprInstDict = new();
        private ConcurrentDictionary<ParticleSystemInstance, Layer> partSysInstDict = new();

        public UndertaleRoomEditor()
        {
            InitializeComponent();

            Loaded += UndertaleRoomEditor_Loaded;
            Unloaded += UndertaleRoomEditor_Unloaded;
            DataContextChanged += UndertaleRoomEditor_DataContextChanged;
        }

        public void SaveImagePNG(Stream outfile)
        {
            if (roomCanvas is null)
            {
                if (MainWindow.FindVisualChild<Canvas>(RoomGraphics) is Canvas canv && canv.Name == "RoomCanvas")
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
                    mainWindow.ShowError("Failed to export file: " + ex.Message, "Failed to export file");
                }
            }
        }

        private void UndertaleRoomEditor_Loaded(object sender, RoutedEventArgs e)
        {
            if (ObjectEditor.Content is null)
                RoomRootItem.IsSelected = true;
        }
        private void UndertaleRoomEditor_Unloaded(object sender, RoutedEventArgs e)
        {
            ObjElemDict.Clear();
            ParticleSystemRectConverter.ClearDict();
        }

        private void UndertaleRoomEditor_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // "DataContextChanged" raised before "Loaded"
            if (IsLoaded)
            {
                RoomRootItem.IsSelected = false;
                RoomRootItem.IsSelected = true;

                ScrollViewer viewer = MainWindow.FindVisualChild<ScrollViewer>(RoomObjectsTree);
                if (viewer is not null)
                {
                    viewer.ScrollToTop();
                    viewer.ScrollToLeftEnd();
                }

                RoomGraphics.ClearValue(LayoutTransformProperty);
                _ = Dispatcher.InvokeAsync(() =>
                {
                    RoomGraphicsScroll.ScrollToTop();
                    RoomGraphicsScroll.ScrollToLeftEnd();
                }, DispatcherPriority.ContextIdle);
            }

            UndertaleCachedImageLoader.Reset();
            CachedTileDataLoader.Reset();
            ObjElemDict.Clear();
            roomObjDict.Clear();
            sprInstDict.Clear();
            partSysInstDict.Clear();

            if (DataContext is UndertaleRoom room)
            {
                GameObjItems.Header = room.Flags.HasFlag(RoomEntryFlags.IsGMS2)
                                      ? "Game objects (from all layers)"
                                      : "Game objects";
                SetupRoomWithGrids(room);
                GenerateSpriteCache(DataContext as UndertaleRoom);

                if (room.Layers.Count > 0) // if GMS 2+
                {
                    LayerZIndexConverter.ProcessOnce = true;

                    if (!room.CheckLayersDepthOrder())
                        room.RearrangeLayers();

                    Parallel.ForEach(room.Layers, (layer) =>
                    {
                        if (layer.LayerType == LayerType.Assets)
                        {
                            foreach (SpriteInstance spr in layer.AssetsData.Sprites)
                                sprInstDict.TryAdd(spr, layer);

                            foreach (Tile tile in layer.AssetsData.LegacyTiles)
                                roomObjDict.TryAdd(tile.InstanceID, layer);

                            if ((layer.AssetsData.ParticleSystems?.Count ?? 0) > 0)
                            {
                                foreach (ParticleSystemInstance partSys in layer.AssetsData.ParticleSystems)
                                    partSysInstDict.TryAdd(partSys, layer);
                                    
                                var particleSystems = layer.AssetsData.ParticleSystems.Select(x => x.ParticleSystem);
                                ParticleSystemRectConverter.Initialize(particleSystems);
                            }
                        }
                        else if (layer.LayerType == LayerType.Instances)
                        {
                            if (layer.InstancesData.AreInstancesUnresolved())
                            {
                                _ = mainWindow.Dispatcher.InvokeAsync(() =>
                                {
                                    mainWindow.ShowWarning($"The instances list of layer \"{layer.LayerName.Content}\" is empty, but the layer has the instances ID.");
                                }, DispatcherPriority.ContextIdle);
                            }

                            foreach (GameObject obj in layer.InstancesData.Instances)
                                roomObjDict.TryAdd(obj.InstanceID, layer);
                        }
                    });
                }
            }
        }

        private void RoomCanvas_Loaded(object sender, RoutedEventArgs e)
        {
            roomCanvas = sender as Canvas;
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (currStoryboard is not null)
            {
                currStoryboard.Stop(this);
                currStoryboard.Remove(this);
            }

            bool isMovable = false;

            // I can't bind it directly because then clicking on the headers makes WPF explode because it tries to attach the header as child of ObjectEditor
            // TODO: find some better workaround
            if (e.NewValue == RoomRootItem)
            {
                ObjectEditor.Content = DataContext;
                MoveButtonsPanel.IsEnabled = false;
            }
            else if (e.NewValue is UndertaleObject obj)
            {
                ObjectEditor.Content = obj;

                if (obj is GameObject)
                {
                    var room = DataContext as UndertaleRoom;
                    if (room?.Flags.HasFlag(RoomEntryFlags.IsGMS2) == true)
                    {
                        // Check if the selected game object is in the "Game objects (from all layers)" list
                        var objectItem = GameObjItems.ItemContainerGenerator.ContainerFromItem(obj) as TreeViewItem;
                        if (objectItem?.IsSelected != true)
                            isMovable = true;
                    }
                    else
                        isMovable = true;
                }
                else
                    isMovable = movableTypes.Contains(obj.GetType());

                MoveButtonsPanel.IsEnabled = isMovable;

                try
                {
                    if (obj is View)
                        return;

                    DependencyObject obj1 = null;

                    if (obj is Layer layer)
                    {
                        if (!layer.IsVisible)
                            return;

                        obj1 = ObjElemDict[obj];
                    }
                    else
                        obj1 = VisualTreeHelper.GetChild(ObjElemDict[obj], 0);

                    (obj1 as FrameworkElement).BringIntoView();

                    Storyboard.SetTarget(flashAnim, obj1);
                    Storyboard.SetTargetProperty(flashAnim, new PropertyPath(OpacityProperty));

                    currStoryboard = new();
                    currStoryboard.Children.Add(flashAnim);
                    currStoryboard.Begin(this, true);
                }
                catch (KeyNotFoundException)
                {
                    Debug.WriteLine($"Flash animation error - \"{obj}\" is missing from the room object dictionary.");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Flash animation error - {ex}");
                }
            }
            else if (e.NewValue is null && ObjectEditor.Content is Layer layer)
            {
                // if layers were rearranged and there was a layer selected
                if ((DataContext as UndertaleRoom).Layers.Contains(layer))
                {
                    TreeViewItem layerItem = LayerItems.ItemContainerGenerator.ContainerFromItem(layer) as TreeViewItem;
                    if (layerItem is not null)
                        layerItem.IsSelected = true;
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
                    Tile or GameObject => roomObjDict[(clickedObj as IRoomObject).InstanceID],
                    SpriteInstance spr => sprInstDict[spr],
                    ParticleSystemInstance partSys => partSysInstDict[partSys],
                    _ => null
                };

            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
            {
                IList collection = clickedObj switch
                {
                    GameObject => layer is null ? room.GameObjects : layer.InstancesData.Instances,
                    Tile => layer is null ? room.Tiles : layer.AssetsData.LegacyTiles,
                    SpriteInstance => layer.AssetsData.Sprites,
                    ParticleSystemInstance => layer.AssetsData.ParticleSystems,
                    _ => null
                };
                if (collection is not null)
                {
                    int index = collection.IndexOf(clickedObj) + 1;
                    Point pos = clickedObj switch
                    {
                        IRoomObject roomObj => new(roomObj.X, roomObj.Y),
                        SpriteInstance sprInst => new(sprInst.X, sprInst.Y),
                        ParticleSystemInstance partSysInst => new(partSysInst.X, partSysInst.Y),
                        _ => new()
                    };
                    clickedObj = AddObjectCopy(room, layer, clickedObj, true, index, pos);
                }
            }
            if (clickedObj is null)
                return;

            SelectObject(clickedObj);

            var mousePos = e.GetPosition(roomCanvas);
            if (clickedObj is GameObject || clickedObj is Tile || clickedObj is SpriteInstance || clickedObj is ParticleSystemInstance)
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
                    hotpointX = mousePos.X - other.X;
                    hotpointY = mousePos.Y - other.Y;
                }
                else if (movingObj is Tile)
                {
                    var other = movingObj as Tile;
                    var undoObj = new Tile()
                    {
                        X = other.X,
                        Y = other.Y,
                        spriteMode = other.spriteMode,
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
                    hotpointX = mousePos.X - other.X;
                    hotpointY = mousePos.Y - other.Y;
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
                        Rotation = other.Rotation
                    };
                    undoStack.Push(undoObj);
                    hotpointX = mousePos.X - other.X;
                    hotpointY = mousePos.Y - other.Y;
                }
                else if (movingObj is ParticleSystemInstance)
                {
                    var other = clickedObj as ParticleSystemInstance;
                    var undoObj = new ParticleSystemInstance
                    {
                        Name = other.Name,
                        ParticleSystem = other.ParticleSystem,
                        X = other.X,
                        Y = other.Y,
                        ScaleX = other.ScaleX,
                        ScaleY = other.ScaleY,
                        Color = other.Color,
                        Rotation = other.Rotation
                    };
                    undoStack.Push(undoObj);
                    hotpointX = mousePos.X - other.X;
                    hotpointY = mousePos.Y - other.Y;
                }
            }
        }
        private void Rectangle_MouseUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            movingObj = null;
        }

        bool placingTiles = false;
        List<Point> placedTiles = new();

        // if "insertIndex" equals -1, then append
        private UndertaleObject AddObjectCopy(UndertaleRoom room, Layer layer, UndertaleObject obj, bool showErrors, int insertIndex = -1, Point pos = new())
        {
            if (room is null || obj is null)
                return null;

            UndertaleObject newObj = null;

            if (obj is Tile tile)
            {
                if (layer != null && layer.AssetsData == null)
                {
                    mainWindow.ShowError("Please select an assets layer.");
                    return null;
                }

                var newTile = new Tile
                {
                    X = (int)pos.X,
                    Y = (int)pos.Y,
                    spriteMode = tile.spriteMode,
                    ObjectDefinition = tile.ObjectDefinition,
                    SpriteDefinition = tile.SpriteDefinition,
                    SourceX = tile.SourceX,
                    SourceY = tile.SourceY,
                    Width = tile.Width,
                    Height = tile.Height,
                    TileDepth = tile.TileDepth,
                    InstanceID = mainWindow.Data.GeneralInfo.LastTile++,
                    ScaleX = tile.ScaleX,
                    ScaleY = tile.ScaleY,
                    Color = tile.Color
                };

                newObj = newTile;

                int index;
                if (layer != null)
                {
                    index = insertIndex > -1 ? insertIndex : layer.AssetsData.LegacyTiles.Count;
                    layer.AssetsData.LegacyTiles.Insert(index, newTile);
                    roomObjDict.TryAdd(newTile.InstanceID, layer);
                }
                else
                {
                    index = insertIndex > -1 ? insertIndex : room.Tiles.Count;
                    room.Tiles.Insert(index, newTile);
                }

                // recalculates room grid size
                SetupRoomWithGrids(room);
            }
            else if (obj is GameObject gameObj)
            {
                if (layer != null && layer.InstancesData == null)
                {
                    mainWindow.ShowError("Please select an instance layer.");
                    return null;
                }

                var newGameObj = new GameObject
                {
                    X = (int)pos.X,
                    Y = (int)pos.Y,
                    ObjectDefinition = gameObj.ObjectDefinition,
                    InstanceID = mainWindow.Data.GeneralInfo.LastObj++,
                    CreationCode = gameObj.CreationCode,
                    ScaleX = gameObj.ScaleX,
                    ScaleY = gameObj.ScaleY,
                    Color = gameObj.Color,
                    Rotation = gameObj.Rotation,
                    PreCreateCode = gameObj.PreCreateCode,
                    ImageSpeed = gameObj.ImageSpeed,
                    ImageIndex = gameObj.ImageIndex
                };

                newObj = newGameObj;

                int index = insertIndex > -1 ? insertIndex : room.GameObjects.Count;
                room.GameObjects.Insert(index, newGameObj);
                if (layer != null)
                {
                    index = insertIndex > -1 ? insertIndex : layer.InstancesData.Instances.Count;
                    layer.InstancesData.Instances.Insert(index, newGameObj);
                    roomObjDict.TryAdd(newGameObj.InstanceID, layer);
                }
            }
            else if (obj is SpriteInstance sprInst)
            {
                if (layer != null && layer.AssetsData == null)
                {
                    mainWindow.ShowError("Please select an assets layer.");
                    return null;
                }

                var newSprInst = new SpriteInstance
                {
                    Name = SpriteInstance.GenerateRandomName(mainWindow.Data),
                    Sprite = sprInst.Sprite,
                    X = (int)pos.X,
                    Y = (int)pos.Y,
                    ScaleX = sprInst.ScaleX,
                    ScaleY = sprInst.ScaleY,
                    Color = sprInst.Color,
                    AnimationSpeed = sprInst.AnimationSpeed,
                    AnimationSpeedType = sprInst.AnimationSpeedType,
                    FrameIndex = sprInst.FrameIndex,
                    Rotation = sprInst.Rotation
                };

                newObj = newSprInst;

                if (layer != null)
                {
                    int index = insertIndex > -1 ? insertIndex : layer.AssetsData.Sprites.Count;
                    layer.AssetsData.Sprites.Insert(index, newSprInst);
                    sprInstDict.TryAdd(newSprInst, layer);
                }
            }
            else if (obj is ParticleSystemInstance partSysInst)
            {
                if (layer != null && layer.AssetsData == null)
                {
                    mainWindow.ShowError("Please select an assets layer.");
                    return null;
                }

                var newPartSysInst = new ParticleSystemInstance
                {
                    Name = ParticleSystemInstance.GenerateRandomName(mainWindow.Data),
                    ParticleSystem = partSysInst.ParticleSystem,
                    X = (int)pos.X,
                    Y = (int)pos.Y,
                    ScaleX = partSysInst.ScaleX,
                    ScaleY = partSysInst.ScaleY,
                    Color = partSysInst.Color,
                    Rotation = partSysInst.Rotation
                };

                newObj = newPartSysInst;

                if (layer != null)
                {
                    int index = insertIndex > -1 ? insertIndex : layer.AssetsData.ParticleSystems.Count;
                    layer.AssetsData.ParticleSystems.Insert(index, newPartSysInst);
                    partSysInstDict.TryAdd(newPartSysInst, layer);
                }
            }

            return newObj;
        }
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
                    Tile or GameObject => roomObjDict[(other as IRoomObject).InstanceID],
                    SpriteInstance spr => sprInstDict[spr],
                    ParticleSystemInstance partSys => partSysInstDict[partSys],
                    _ => null
                };

            if (layer is not null && layer.AssetsData is null && layer.InstancesData is null)
            {
                return;
            }

            placedTiles.Add(gridMouse);
            placingTiles = true;

            UndertaleObject newObj = AddObjectCopy(room, layer, other, false, -1, gridMouse);

            if (newObj is not null)
                SelectObject(newObj);
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

            movingObj = null;
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

                var mousePos = e.GetPosition(roomCanvas);

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

                if (movingObj is GameObject gameObj)
                {
                    gameObj.X = tgtX;
                    gameObj.Y = tgtY;
                }
                else if (movingObj is Tile tile)
                {
                    tile.X = tgtX;
                    tile.Y = tgtY;
                }
                else if (movingObj is SpriteInstance spr)
                {
                    spr.X = tgtX;
                    spr.Y = tgtY;
                }
                else if (movingObj is ParticleSystemInstance partSys)
                {
                    partSys.X = tgtX;
                    partSys.Y = tgtY;
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
            var mousePos = e.GetPosition(RoomGraphics);
            var transform = RoomGraphics.LayoutTransform as MatrixTransform;
            var matrix = transform.Matrix;
            var scale = e.Delta >= 0 ? 1.1 : (1.0 / 1.1); // choose appropriate scaling factor

            if ((matrix.M11 > 0.2 || (matrix.M11 <= 0.2 && scale > 1)) && (matrix.M11 < 3 || (matrix.M11 >= 3 && scale < 1)))
            {
                matrix.ScaleAtPrepend(scale, scale, mousePos.X, mousePos.Y);
            }
            RoomGraphics.LayoutTransform = new MatrixTransform(matrix);
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

        private UndertaleObject selectedObject;

        /// <summary>
        /// Selects the given object inside the TreeView.
        /// </summary>
        /// <param name="obj">the object to select.</param>
        /// <param name="focus">whether to focus on the object after selcting it.</param>
        private void SelectObject(UndertaleObject obj, bool focus = true)
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

                    case ParticleSystemInstance partSys:
                        resLayer = room.Layers.AsParallel()
                                              .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                                              .FirstOrDefault(l => l.LayerType is LayerType.Assets
                                                  && (l.AssetsData.ParticleSystems?.Any(x => x.Name == partSys.Name) ?? false));
                        resList = resLayer.AssetsData.ParticleSystems;
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
                    if (focus)
                        resItem.Focus();

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

            e.Effects = e.AllowedEffects.HasFlag(DragDropEffects.Link) && sourceItem != null
                        && ((mainWindow.IsGMS2 == Visibility.Collapsed && sourceItem is UndertaleBackground)
                            || sourceItem is UndertaleGameObject
                            || sourceItem is UndertalePath
                            || (mainWindow.IsGMS2 == Visibility.Visible && sourceItem is UndertaleSprite))
                        ? DragDropEffects.Link
                        : DragDropEffects.None;

            e.Handled = true;
        }

        private void Canvas_Drop(object sender, DragEventArgs e)
        {
            UndertaleObject sourceItem = e.Data.GetData(e.Data.GetFormats()[^1]) as UndertaleObject;

            e.Effects = e.AllowedEffects.HasFlag(DragDropEffects.Link) && sourceItem != null
                        && ((mainWindow.IsGMS2 == Visibility.Collapsed && sourceItem is UndertaleBackground)
                            || sourceItem is UndertaleGameObject
                            || sourceItem is UndertalePath
                            || (mainWindow.IsGMS2 == Visibility.Visible && sourceItem is UndertaleSprite))
                        ? DragDropEffects.Link
                        : DragDropEffects.None;

            if (e.Effects == DragDropEffects.Link)
            {
                UndertaleRoom room = DataContext as UndertaleRoom;
                Layer layer = null;
                if (room.Layers.Count > 0)
                {
                    object utObj = ObjectEditor.Content;
                    layer = utObj switch
                    {
                        GameObject gameObj => roomObjDict[gameObj.InstanceID],
                        SpriteInstance sprInst => sprInstDict[sprInst],
                        Layer l => l,
                        _ => null
                    };
                }

                if (sourceItem is UndertaleBackground droppedBG)
                {
                    Background firstBG = room.Backgrounds.FirstOrDefault(x => x.BackgroundDefinition is null);
                    if (firstBG is not null)
                    {
                        firstBG.Enabled = true;
                        firstBG.BackgroundDefinition = droppedBG;

                        SelectObject(firstBG);
                    }
                    else
                        mainWindow.ShowError("No empty room backgrounds.");
                }
                else if (sourceItem is UndertaleGameObject droppedObj)
                {
                    var mousePos = e.GetPosition(roomCanvas);

                    if (mainWindow.IsGMS2 == Visibility.Visible && layer == null)
                    {
                        mainWindow.ShowError("Please select a layer.");
                        return;
                    }
                    if (layer != null && layer.InstancesData == null)
                    {
                        mainWindow.ShowError("Please select an instances layer.");
                        return;
                    }

                    GameObject obj = new()
                    {
                        X = (int)mousePos.X,
                        Y = (int)mousePos.Y,
                        ObjectDefinition = droppedObj,
                        InstanceID = mainWindow.Data.GeneralInfo.LastObj++
                    };
                    room.GameObjects.Add(obj);
                    roomObjDict.TryAdd(obj.InstanceID, layer);
                    if (layer != null)
                        layer.InstancesData.Instances.Add(obj);

                    SelectObject(obj);
                }
                else if (sourceItem is UndertalePath)
                {
                    PreviewPath = sourceItem as UndertalePath;
                }
                else if (sourceItem is UndertaleSprite droppedSprite)
                {
                    var mousePos = e.GetPosition(roomCanvas);

                    if (mainWindow.IsGMS2 == Visibility.Visible && layer == null)
                    {
                        mainWindow.ShowError("Please select a layer.");
                        return;
                    }
                    if (layer != null && layer.AssetsData == null)
                    {
                        mainWindow.ShowError("Please select an assets layer.");
                        return;
                    }

                    SpriteInstance sprInst = new()
                    {
                        X = (int)mousePos.X,
                        Y = (int)mousePos.Y,
                        Name = SpriteInstance.GenerateRandomName(mainWindow.Data),
                        Sprite = droppedSprite
                    };

                    sprInstDict.TryAdd(sprInst, layer);
                    layer.AssetsData.Sprites.Add(sprInst);

                    SelectObject(sprInst);
                }
            }

            e.Handled = true;
        }

        private void RoomObjectsTree_KeyDown(object sender, KeyEventArgs e)
        {
            UndertaleObject selectedObj = ObjectEditor.Content as UndertaleObject;

            if (e.Key == Key.Delete)
                DeleteItem(selectedObj);
            else if (e.Key == Key.OemMinus)
                MoveItem(selectedObj, -1);
            else if (e.Key == Key.OemPlus)
                MoveItem(selectedObj, 1);
        }

        private void RoomObjectsTree_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            object sel = (sender as TreeView).SelectedItem;
            if (sel is GameObject gameObj)
                mainWindow.ChangeSelection(gameObj.ObjectDefinition);
            if (sel is Background bg)
                mainWindow.ChangeSelection(bg.BackgroundDefinition);
            if (sel is Tile tile)
                mainWindow.ChangeSelection(tile.ObjectDefinition);
            if (sel is SpriteInstance sprInst)
                mainWindow.ChangeSelection(sprInst.Sprite);
            if (sel is ParticleSystemInstance partSys)
                mainWindow.ChangeSelection(partSys.ParticleSystem);
        }
        private async void RoomObjectsTree_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Middle)
                return;

            TreeViewItem treeViewItem = MainWindow.VisualUpwardSearch<TreeViewItem>(e.OriginalSource as DependencyObject);
            treeViewItem?.Focus();

            RoomObjectsTree.UpdateLayout();
            ObjectEditor.UpdateLayout();
            await Task.Run(async () =>
            {
                // Added a little delay in order to see that selection was changed
                await Task.Delay(25);

                Dispatcher.Invoke(() =>
                {
                    object sel = (sender as TreeView).SelectedItem;
                    if (sel is GameObject gameObj)
                        mainWindow.ChangeSelection(gameObj.ObjectDefinition, true);
                    if (sel is Background bg)
                        mainWindow.ChangeSelection(bg.BackgroundDefinition, true);
                    if (sel is Tile tile)
                        mainWindow.ChangeSelection(tile.ObjectDefinition, true);
                    if (sel is SpriteInstance sprInst)
                        mainWindow.ChangeSelection(sprInst.Sprite, true);
                    if (sel is ParticleSystemInstance partSys)
                        mainWindow.ChangeSelection(partSys.ParticleSystem, true);
                });
            });
        }

        private void TreeViewMoveUpButton_Click(object sender, RoutedEventArgs e)
        {
            UndertaleObject selectedObj = ObjectEditor.Content as UndertaleObject;
            // If the button loses focus it cannot be held
            MoveItem(selectedObj, -1, false);
        }

        private void TreeViewMoveDownButton_Click(object sender, RoutedEventArgs e)
        {
            UndertaleObject selectedObj = ObjectEditor.Content as UndertaleObject;
            // If the button loses focus it cannot be held
            MoveItem(selectedObj, 1, false);
        }

        private void TreeViewMoveButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            UndertaleObject selectedObj = ObjectEditor.Content as UndertaleObject;
            SelectObject(selectedObj);
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
            if (!undoStack.Any()) return;

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
            (this.DataContext as UndertaleRoom)?.SetupRoom(false, false);
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
                {
                    object utObj = ObjectEditor.Content;
                    layer = utObj switch
                    {
                        Tile or GameObject => roomObjDict[(utObj as IRoomObject).InstanceID],
                        SpriteInstance spr => sprInstDict[spr],
                        Layer l => l,
                        _ => null
                    };
                }

                if (mainWindow.IsGMS2 == Visibility.Visible && layer == null)
                {
                    mainWindow.ShowError("Please select a layer.");
                    return;
                }

                Point mousePos = roomCanvas.IsMouseOver ? Mouse.GetPosition(roomCanvas) : new();
                UndertaleObject newObj = AddObjectCopy(room, layer, copied, true, -1, mousePos);

                if (newObj is not null)
                    SelectObject(newObj);
            }
        }

        private void AddLayer<T>(LayerType type, string name) where T : Layer.LayerData, new()
        {
            UndertaleRoom room = this.DataContext as UndertaleRoom;
            if (room is null)
            {
                // (not sure if it's possible)
                mainWindow.ShowError("Room is null.");
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

            // "layerDepth" is "long", because otherwise one can't check if the incremented value is larger than "Int32.MaxValue",
            // because then it would overflow.
            long layerDepth = 0;
            if (room.Layers.Count > 0)
            {
                layerDepth = room.Layers.Select(l => l.LayerDepth).Max();
                if (layerDepth + 100 > Int32.MaxValue)
                {
                    if (layerDepth + 1 > Int32.MaxValue)
                    {
                        layerDepth -= 1;
                        mainWindow.ShowWarning("Warning - the maximum layer depth is reached.\nYou probably should change the depth of the new layer.");
                    }
                    else
                        layerDepth += 1;
                }
                else
                    layerDepth += 100;
            }

            string baseName = null;
            int nameNum = 0;
            while (room.Layers.Any(l => l.LayerName.Content == name))
            {
                if (baseName is null)
                {
                    // Get the trailing number from the name ("name123" => "123")
                    Match numMatch = trailingNumberRegex.Match(name);

                    // Name has a trailing number, so we parse the basename and number, increment the number and
                    // set the new name to the basename and incremented number ("name123" -> "name124")
                    if (numMatch.Success)
                    {
                        baseName = name[..^numMatch.Length];
                        nameNum = Int32.Parse(numMatch.Groups[0].Value) + 1;
                    }
                    // Name doesn't have a trailing number, so it's the first duplicate.
                    // Thus we set baseName and nameNum to produce "name1" on the next loop.
                    else
                    {
                        baseName = name;
                        nameNum = 1;
                    }
                }
                // If base name is already extracted, increment "nameNum" and append it to the base name
                else
                    nameNum++;
                // Update name using baseName and nameNum
                name = baseName + nameNum;
            }

            Layer layer = new()
            {
                LayerName = data.Strings.MakeString(name),
                LayerId = largest_layerid + 1,
                LayerType = type,
                LayerDepth = (int)layerDepth,
                Data = new T()
            };
            room.Layers.Add(layer);
            room.UpdateBGColorLayer();

            if (room.Layers.Count > 1)
            {
                LayerZIndexConverter.ProcessOnce = true;
                foreach (Layer l in room.Layers)
                    l.UpdateZIndex();
            }
            layer.ParentRoom = room;

            if (layer.LayerType == LayerType.Assets)
            {
                // "??=" - assign if null
                layer.AssetsData.LegacyTiles ??= new UndertalePointerList<Tile>();
                layer.AssetsData.Sprites ??= new UndertalePointerList<SpriteInstance>();
                layer.AssetsData.Sequences ??= new UndertalePointerList<SequenceInstance>();

                // if it's needed to set "NineSlices"
                if (!data.IsVersionAtLeast(2, 3, 2))
                    layer.AssetsData.NineSlices ??= new UndertalePointerList<SpriteInstance>();
                // likewise
                if (data.IsNonLTSVersionAtLeast(2023, 2))
                    layer.AssetsData.ParticleSystems ??= new UndertalePointerList<ParticleSystemInstance>();
            }
            else if (layer.LayerType == LayerType.Tiles)
            {
                // create new tile data (if null)
                layer.TilesData.TileData ??= Array.Empty<uint[]>();
            }

            SelectObject(layer);
        }

        private void AddObjectInstance(UndertaleRoom room)
        {
            var newObject = new GameObject { InstanceID = mainWindow.Data.GeneralInfo.LastObj++ };
            room.GameObjects.Add(newObject);

            SelectObject(newObject);
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
                tile.spriteMode = true;
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

                SpriteInstance spriteInstance = new()
                {
                    Name = SpriteInstance.GenerateRandomName(mainWindow.Data)
                };

                layer.AssetsData.Sprites.Add(spriteInstance);
                sprInstDict.TryAdd(spriteInstance, layer);

                SelectObject(spriteInstance);
            }
        }

        private void AddGMS1Tile(UndertaleRoom room)
        {
            // add tile to list
            var tile = new Tile { InstanceID = mainWindow.Data.GeneralInfo.LastTile++ };
            room.Tiles.Add(tile);
            SelectObject(tile);
        }

        /// <summary>
        /// Deletes the given object from the room.
        /// </summary>
        /// <param name="obj">The object to delete.</param>
        private void DeleteItem(UndertaleObject obj)
        {
            UndertaleRoom room = this.DataContext as UndertaleRoom;

            // We need to check before deleting the object but can only clear the editor after deleting the object
            bool clearEditor = (obj == (ObjectEditor.Content as UndertaleObject));

            if (obj is Background bg)
            {
                bg.Enabled = false;
                bg.BackgroundDefinition = null;
            }
            else if (obj is View view)
            {
                view.Enabled = false;
            }
            else if (obj is Tile tile)
            {
                if (mainWindow.IsGMS2 == Visibility.Visible)
                {
                    foreach (var layer in room.Layers)
                        if (layer.AssetsData != null)
                            layer.AssetsData.LegacyTiles.Remove(tile);
                    roomObjDict.Remove(tile.InstanceID, out _);
                }

                room.Tiles.Remove(tile);
            }
            else if (obj is GameObject gameObj)
            {
                if (mainWindow.IsGMS2 == Visibility.Visible)
                {
                    foreach (var layer in room.Layers)
                        if (layer.InstancesData != null)
                            layer.InstancesData.Instances.Remove(gameObj);
                    roomObjDict.Remove(gameObj.InstanceID, out _);
                }

                room.GameObjects.Remove(gameObj);
            }
            else if (obj is SpriteInstance sprInst)
            {
                foreach (var layer in room.Layers)
                    if (layer.AssetsData != null)
                        layer.AssetsData.Sprites.Remove(sprInst);

                sprInstDict.Remove(sprInst, out _);
            }
            else if (obj is ParticleSystemInstance partSysInst)
            {
                foreach (var layer in room.Layers)
                    if (layer.AssetsData != null)
                        layer.AssetsData.ParticleSystems.Remove(partSysInst);
            }
            else if (obj is Layer layer)
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
                    room.UpdateBGColorLayer();
            }

            if (clearEditor)
                ObjectEditor.Content = null;
        }

        /// <summary>
        /// Moves the given object up and down the list in the <see cref="TreeView"/>.
        /// </summary>
        /// <param name="obj">The object to move.</param>
        /// <param name="dist">Distance to move it. Positive - down, negative - up.</param>
        /// <param name="focus">Whether to focus on the element after moving it.</param>
        private void MoveItem(UndertaleObject obj, int dist, bool focus = true)
        {
            if (obj is Layer)
            {
                mainWindow.ShowError("Layers don't support this feature currently, change the layer depths instead.");
                return;
            }
            if (obj is View)
            {
                mainWindow.ShowError("Views don't support this feature.");
                return;
            }
            if (obj is Background)
            {
                mainWindow.ShowError("Backgrounds don't support this feature.");
                return;
            }

            if (this.DataContext is not UndertaleRoom room)
                return;

            if (obj is GameObject)
            {
                if (room.Flags.HasFlag(RoomEntryFlags.IsGMS2))
                {
                    // Check if the selected game object is in the "Game objects (from all layers)" list
                    var objectItem = GameObjItems.ItemContainerGenerator.ContainerFromItem(obj) as TreeViewItem;
                    if (objectItem?.IsSelected == true)
                    {
                        mainWindow.ShowError("You should select an object in an instances layer instead.");
                        return;
                    }
                }
            }

            Layer layer = null;
            if (room.Layers.Count > 0)
                layer = obj switch
                {
                    Tile or GameObject => roomObjDict[(obj as IRoomObject).InstanceID],
                    SpriteInstance spr => sprInstDict[spr],
                    ParticleSystemInstance partSys => partSysInstDict[partSys],
                    _ => null
                };

            IList list = obj switch
            {
                Tile => layer is null ? room.Tiles : layer.AssetsData.LegacyTiles,
                GameObject => layer is null ? room.GameObjects : layer.InstancesData.Instances,
                SpriteInstance => layer.AssetsData.Sprites,
                ParticleSystemInstance => layer.AssetsData.ParticleSystems,
                _ => null
            };
            if (list is null)
            {
                mainWindow.ShowError("Can't change the object position - no list for the selected object was found.");
                return;
            }

            int index = list.IndexOf(obj);
            int newIndex = Math.Clamp(index + dist, 0, list.Count - 1);
            if (newIndex != index)
            {
                var prevObj = list[newIndex];
                list[newIndex] = obj;
                list[index] = prevObj;

                if (layer is not null)
                {
                    // swap back objects in "ObjectDict"
                    var rect = ObjElemDict[obj];
                    var rectPrev = ObjElemDict[prevObj as UndertaleObject];
                    ObjElemDict[obj] = rectPrev;
                    ObjElemDict[prevObj as UndertaleObject] = rect;
                }
            }

            SelectObject(obj, focus);
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

        private void MenuItem_NewObjectInstance_Click(object sender, RoutedEventArgs e)
        {
            AddObjectInstance(this.DataContext as UndertaleRoom);
        }
        private void MenuItem_NewGMS1Tile_Click(object sender, RoutedEventArgs e)
        {
            AddGMS1Tile(this.DataContext as UndertaleRoom);
        }
        private void MenuItem_Delete_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuitem = sender as MenuItem;
            DeleteItem(menuitem.DataContext as UndertaleObject);
        }

        private void MenuItem_Copy_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuitem = sender as MenuItem;
            copied = menuitem.DataContext as UndertaleObject;
        }

        private void MenuItem_Paste_Click(object sender, RoutedEventArgs e)
        {
            UndertaleRoom room = this.DataContext as UndertaleRoom;
            MenuItem menuitem = sender as MenuItem;
            Layer layer = mainWindow.IsGMS2 == Visibility.Visible ?
                menuitem.DataContext as Layer : null;

            UndertaleObject newObj = AddObjectCopy(room, layer, copied, true);
            if (newObj is not null)
                SelectObject(newObj);
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
                            var instances = layer.InstancesData.Instances
                                            .Where(g => g.ObjectDefinition?.Sprite?.Textures.Count > 0);
                            allObjects.AddRange(instances);
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
                    GameObject gameObj => gameObj.ObjectDefinition?.Sprite?.Textures[gameObj.WrappedImageIndex].Texture,

                    // GMS 2+
                    Layer.LayerBackgroundData bgData => bgData.Sprite?.Textures[0].Texture,
                    SpriteInstance sprite => sprite.Sprite?.Textures[sprite.WrappedFrameIndex].Texture,
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

        private void TileDataExport_Click(object sender, RoutedEventArgs e)
        {
            if (RoomObjectsTree.SelectedItem is not Layer layer)
                return;

            if (layer.TilesData.TileData.Length == 0)
            {
                mainWindow.ShowError("Tile data is empty.");
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
                mainWindow.ShowError($"Error while saving the file - \"{ex.Message}\".");
                return;
            }

            mainWindow.ShowMessage("Exported file path: " + filePath);
        }
        private void TileDataImport_Click(object sender, RoutedEventArgs e)
        {
            if (RoomObjectsTree.SelectedItem is not Layer layer)
                return;

            if (layer.TilesData.TilesX == 0 || layer.TilesData.TilesY == 0)
            {
                mainWindow.ShowError("Tile data size can't be zero.");
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
                    mainWindow.ShowError($"Error while opening file - \"{ex.Message}\".");
                    return;
                }

                if (tileDataLines?.Length != tileDataNew.Length)
                {
                    mainWindow.ShowError("Error - selected file line count doesn't match tile layer height.");
                    return;
                }

                char delimChar = ';';
                Func<string, uint> numParseFunc = (x) => UInt32.Parse(x);
                if (tileDataLines[0].Count(x => x == ',') > 1)
                {
                    var res = mainWindow.ShowQuestion("Was the data exported from \"Tiled\"?");
                    if (res == MessageBoxResult.Yes)
                    {
                        delimChar = ',';
                        numParseFunc = (x) =>
                        {
                            uint val;
                            unchecked { val = (uint)Int32.Parse(x); }
                            if (val == uint.MaxValue)
                                return 0;

                            uint id = val & 0x0FFFFFFF;
                            uint flags = val & 0xF0000000;
                            flags = flags switch
                            {
                                0 => 0,
                                2147483648 => 1, // RotateNoneFlipX
                                1073741824 => 2, // RotateNoneFlipY
                                3221225472 => 3, // RotateNoneFlipXY
                                2684354560 => 4, // Rotate90FlipNone
                                3758096384 => 5, // Rotate270FlipY
                                536870912 => 6,  // Rotate90FlipY
                                1610612736 => 7, // Rotate270FlipNone
                                _ => throw new InvalidDataException($"{flags} is not a valid tile flag value.")
                            };
                            flags <<= 28;

                            return (id | flags);
                        };
                    }
                    else
                    {
                        mainWindow.ShowError("The file has invalid data.");
                        return;
                    }
                }

                for (int i = 0; i < tileDataLines.Length; i++)
                {
                    uint[] dataRow;
                    try
                    {
                        dataRow = tileDataLines[i].Split(delimChar).Select(numParseFunc).ToArray();
                    }
                    catch (Exception ex)
                    {
                        mainWindow.ShowError($"Error while parsing line {i + 1} - \"{ex.Message}\".");
                        return;
                    }

                    if (dataRow.Length != layer.TilesData.TilesX)
                    {
                        mainWindow.ShowError($"Length of line {i + 1} is not equal to the tile data width.");
                        return;
                    }

                    tileDataNew[i] = dataRow;
                }

                layer.TilesData.TileData = tileDataNew;

                mainWindow.ShowMessage("Imported successfully.");
            }
        }

        private void LayerCanvas_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // raised on layers rearrange
            if (e.NewValue is not Layer layer)
                return;

            LayerCanvas canvas = sender as LayerCanvas;

            canvas.CurrentLayer = layer;
            ObjElemDict[layer] = canvas;
        }
        private void LayerCanvas_Initialized(object sender, EventArgs e)
        {
            LayerCanvas canvas = sender as LayerCanvas;

            if (canvas?.DataContext is not Layer layer)
                return;

            canvas.CurrentLayer = layer;
            ObjElemDict[layer] = canvas;
        }

        private void LayerCanvas_Unloaded(object sender, RoutedEventArgs e)
        {
            LayerCanvas canvas = sender as LayerCanvas;

            if (canvas.CurrentLayer is not null)
                ObjElemDict.Remove(canvas.CurrentLayer);
        }

        public static void SetupRoomWithGrids(UndertaleRoom room)
        {
            if (Settings.Instance.GridWidthEnabled)
                room.GridWidth = Settings.Instance.GlobalGridWidth;
            if (Settings.Instance.GridHeightEnabled)
                room.GridHeight = Settings.Instance.GlobalGridHeight;

            // SetupRoom already overrides GridWidth and GridHeight if the global setting is disabled, but does
            // not do that for the thickness. Hence why we're overriding it here manually to the default value should
            // the setting be disabled.
            if (Settings.Instance.GridThicknessEnabled)
                room.GridThicknessPx = Settings.Instance.GlobalGridThickness;
            else
                room.GridThicknessPx = 1;
            room.SetupRoom(!Settings.Instance.GridWidthEnabled, !Settings.Instance.GridHeightEnabled);
        }
    }

    public partial class RoomCanvas : Canvas
    {
        private readonly bool isGMS2 = (Application.Current.MainWindow as MainWindow).IsGMS2 == Visibility.Visible;

        protected override void OnVisualChildrenChanged(DependencyObject visualAdded, DependencyObject visualRemoved)
        {
            if (!isGMS2)
            {
                if (visualAdded is ContentPresenter presAdded && presAdded.DataContext is UndertaleObject objAdded)
                    UndertaleRoomEditor.ObjElemDict[objAdded] = presAdded;

                if (visualRemoved is ContentPresenter presRemoved && presRemoved.DataContext is UndertaleObject objRemoved)
                    UndertaleRoomEditor.ObjElemDict.Remove(objRemoved);
            }

            base.OnVisualChildrenChanged(visualAdded, visualRemoved);
        }
    }
    public partial class LayerCanvas : Canvas
    {
        // "DataContext" on "Unloaded" event is "{DisconnectedItem}"
        public UndertaleObject CurrentLayer { get; set; }

        protected override void OnVisualChildrenChanged(DependencyObject visualAdded, DependencyObject visualRemoved)
        {
            if (visualAdded is ContentPresenter presAdded && presAdded.DataContext is UndertaleObject objAdded)
                UndertaleRoomEditor.ObjElemDict[objAdded] = presAdded;

            if (visualRemoved is ContentPresenter presRemoved && presRemoved.DataContext is UndertaleObject objRemoved)
                UndertaleRoomEditor.ObjElemDict.Remove(objRemoved);

            base.OnVisualChildrenChanged(visualAdded, visualRemoved);
        }
    }

    public class LayerDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate PathDataTemplate { get; set; }
        public DataTemplate InstancesDataTemplate { get; set; }
        public DataTemplate TilesDataTemplate { get; set; }
        public DataTemplate AssetsDataTemplate { get; set; }
        public DataTemplate BackgroundDataTemplate { get; set; }
        public DataTemplate EffectDataTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (container is FrameworkElement && item != null && item is Layer)
            {
                switch ((item as Layer).LayerType)
                {
                    case LayerType.Path:
                        return PathDataTemplate;
                    case LayerType.Instances:
                        return InstancesDataTemplate;
                    case LayerType.Tiles:
                        return TilesDataTemplate;
                    case LayerType.Assets:
                        return AssetsDataTemplate;
                    case LayerType.Background:
                        return BackgroundDataTemplate;
                    case LayerType.Effect:
                        return EffectDataTemplate;
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
            if (values.Any(x => x is null || x == DependencyProperty.UnsetValue))
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

    public class LayerItemsSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Layer layer)
            {
                return layer.LayerType switch
                {
                    // TODO: implement "LayerType.Effects"
                    LayerType.Assets => new CompositeCollection(3)
                                        {
                                          new CollectionContainer() { Collection = layer.AssetsData.LegacyTiles },
                                          new CollectionContainer() { Collection = layer.AssetsData.Sprites },
                                          new CollectionContainer() { Collection = layer.AssetsData.ParticleSystems }
                                        },
                    LayerType.Background => new List<Layer.LayerBackgroundData>() { layer.BackgroundData },
                    LayerType.Instances => layer.InstancesData.Instances,
                    LayerType.Tiles => new List<Layer.LayerTilesData>() { layer.TilesData },
                    _ => null
                };
            }
            else
                return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class LayerZIndexConverter : IMultiValueConverter
    {
        public static bool ProcessOnce { get; set; }

        private static bool suspended;
        private static int remainingCount = -1;
        private static Layer selectedLayer;
        private static int selectedLayerIndex = -1;

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is Layer layer && layer.ParentRoom is UndertaleRoom room)
            {
                int layerZIndex, layerIndex;
                if (layer == selectedLayer && selectedLayerIndex != -1)
                    layerIndex = selectedLayerIndex;
                else
                    layerIndex = room.Layers.IndexOf(layer);

                layerZIndex = room.Layers.Count - layerIndex - 1;

                if (ProcessOnce)
                {
                    if (remainingCount == -1 && room.Layers.Count > 0)
                        remainingCount = room.Layers.Count - 1;
                    else if (remainingCount > 0)
                    {
                        remainingCount--;

                        if (remainingCount == 0)
                        {
                            ProcessOnce = false;
                            remainingCount = -1;
                        }
                    }
                }
                else if (!suspended && room.Layers.Count > 1)
                {
                    if (!room.CheckLayersDepthOrder())
                    {
                        suspended = true;

                        var roomEditor = MainWindow.FindVisualChild<UndertaleRoomEditor>((Application.Current.MainWindow as MainWindow).DataEditor);
                        selectedLayer = roomEditor?.RoomObjectsTree.SelectedItem as Layer;
                        
                        if (selectedLayer is not null)
                        {
                            Layer[] orderedLayers = room.Layers.OrderBy(l => l.LayerDepth).ToArray();
                            selectedLayerIndex = Array.IndexOf(orderedLayers, selectedLayer);
                            room.RearrangeLayers(new(selectedLayer, orderedLayers, selectedLayerIndex));
                            selectedLayerIndex = -1;
                        }

                        suspended = false;
                    }
                }

                return layerZIndex;
            }
            else
                return -1;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
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
                                Window mainWindow = Application.Current?.MainWindow;
                                mainWindow.ShowError("Room flags of GMS 2+ games must contain the \"IsGMS2\" flag, otherwise the game will crash when loading that room.");
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
                offsetV = bg.YOffset * (1 / bg.CalcScaleY);
                offsetH = bg.XOffset * (1 / bg.CalcScaleX);
                xOffset = bg.XOffset;
                yOffset = bg.YOffset;
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
            bool includeBGLayer = parameter is string par && par == "Canvas";
            return value is LayerType.Instances || (includeBGLayer && value is LayerType.Background) ? Visibility.Collapsed : Visibility.Visible;
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
                                    angle = 270;
                                    scaleY = -1;
                                    break;
                                case 6:
                                    angle = 90;
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
                    Window mainWindow = Application.Current?.MainWindow;
                    mainWindow.ShowError($"An error occured while generating \"Rectangles\" for tile layer {tilesData.ParentLayer.LayerName}.\n\n{ex}");
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

    public class ParentGridHeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double h)
                return h - 22; // "TabController" has predefined height
            else
                return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ParticleSystemRectConverter : IValueConverter
    {
        private static Dictionary<UndertaleParticleSystem, Rect> partSystemsDict;

        public static void Initialize(IEnumerable<UndertaleParticleSystem> particleSystems)
        {
            partSystemsDict = new();

            foreach (var partSys in particleSystems)
            {
                if (partSys is null)
                    continue;

                _ = AddNewSystem(partSys);
            }
        }
        public static void ClearDict() => partSystemsDict = null;

        private static Rect AddNewSystem(UndertaleParticleSystem partSys)
        {
            Rect rect;
            if (partSys.Emitters.Count == 0)
            {
                partSystemsDict[partSys] = rect;
                return rect;
            }

            rect = new();
            var emitters = partSys.Emitters.Select(x => x.Resource);

            float minX = emitters.Select(x => x.RegionX).Min();
            float maxX = emitters.Select(x => x.RegionX + x.RegionWidth).Max();
            rect.Width = Math.Abs(minX - maxX);

            float minY = emitters.Select(x => x.RegionY).Min();
            float maxY = emitters.Select(x => x.RegionY + x.RegionHeight).Max();
            rect.Height = Math.Abs(minY - maxY);

            rect.X = emitters.Select(x => x.RegionX - x.RegionWidth * 0.5f).Min();
            rect.Y = emitters.Select(x => x.RegionY - x.RegionHeight * 0.5f).Min();

            partSystemsDict[partSys] = rect;

            return rect;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not UndertaleParticleSystem partSys)
                return 0;
            if ((partSys.Emitters?.Count ?? 0) == 0)
                return 0;
            if (parameter is not string mode)
                return 0;

            if (partSystemsDict is not null && !partSystemsDict.TryGetValue(partSys, out Rect sysRect))
                sysRect = AddNewSystem(partSys);

            return mode switch
            {
                "width" => sysRect.Width,
                "height" => sysRect.Height,
                "x" => sysRect.X + 8,
                "y" => sysRect.Y + 8,
                _ => 0
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
