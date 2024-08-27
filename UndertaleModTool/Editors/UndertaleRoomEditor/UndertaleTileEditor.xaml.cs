using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using UndertaleModLib.Models;
using static UndertaleModLib.Models.UndertaleRoom;

namespace UndertaleModTool
{
    /// <summary>
    /// Global settings used by the tile editor
    /// </summary>
    [PropertyChanged.AddINotifyPropertyChangedInterface]
    public class TileEditorSettings
    {
        public static TileEditorSettings instance { get; set; } = new();
        public bool BrushTiling { get; set; } = true;
        public bool RoomPreviewBool { get; set; } = true;
        public bool ShowGridBool { get; set; } = true;
    }

    /// <summary>
    /// Interaction logic for UndertaleTileEditor.xaml
    /// </summary>
    [PropertyChanged.AddINotifyPropertyChangedInterface]
    public partial class UndertaleTileEditor : Window
    {
        public static RoutedUICommand MirrorCommand = new("Mirror the brush", "Mirror", typeof(UndertaleTileEditor));
        public static RoutedUICommand FlipCommand = new("Flip the brush", "Flip", typeof(UndertaleTileEditor));
        public static RoutedUICommand RotateCWCommand = new("Rotate the brush 90 degrees clockwise", "RotateCW", typeof(UndertaleTileEditor));
        public static RoutedUICommand RotateCCWCommand = new("Rotate the brush 90 degrees counterclockwise", "RotateCCW", typeof(UndertaleTileEditor));
        public static RoutedUICommand ToggleGridCommand = new("Toggle the tile grid", "ToggleGrid", typeof(UndertaleTileEditor));
        public static RoutedUICommand ToggleBrushTilingCommand = new("Toggle the \"tiling\" behavior on multi-tile brushes", "ToggleBrushTiling", typeof(UndertaleTileEditor));
        public static RoutedUICommand TogglePreviewCommand = new("Toggle the room preview", "TogglePreview", typeof(UndertaleTileEditor));
        public static RoutedUICommand UndoCommand = new("Undoes actions", "Undo", typeof(UndertaleTileEditor));
        public static RoutedUICommand RedoCommand = new("Redoes actions", "Redo", typeof(UndertaleTileEditor));

        public TileEditorSettings settings { get; set; } = TileEditorSettings.instance;

        public bool Modified { get; set; } = false;

        public Layer EditingLayer { get; set; }

        public WriteableBitmap TilesBitmap { get; set; }

        public double EditWidth { get; set; }
        public double EditHeight { get; set; }
        public double PaletteWidth { get; set; }
        public double PaletteHeight { get; set; }

        public List<Dictionary<Tuple<int, int>, uint>> UndoStack { get; set; } = new();
        public List<Dictionary<Tuple<int, int>, uint>> RedoStack { get; set; } = new();
        public bool UndoEnabled { get; set; } = false;
        public bool RedoEnabled { get; set; } = false;

        private const uint TILE_FLIP_H = 0b00010000000000000000000000000000;
        private const uint TILE_FLIP_V = 0b00100000000000000000000000000000;
        private const uint TILE_ROTATE = 0b01000000000000000000000000000000;
        private const uint TILE_INDEX = 0x7ffff;
        private const uint TILE_FLAGS = ~TILE_INDEX;
        // flags shifted 28 bits to the right
        private static Dictionary<uint, uint> ROTATION_CW = new Dictionary<uint, uint>{
            {0b000, 0b100},
            {0b100, 0b011},
            {0b011, 0b111},
            {0b111, 0b000},

            {0b110, 0b001},
            {0b010, 0b110},
            {0b101, 0b010},
            {0b001, 0b101},
        };
        private static Dictionary<uint, uint> ROTATION_CCW = new Dictionary<uint, uint>{
            {0b100, 0b000},
            {0b011, 0b100},
            {0b111, 0b011},
            {0b000, 0b111},

            {0b001, 0b110},
            {0b110, 0b010},
            {0b010, 0b101},
            {0b101, 0b001},
        };

        private uint[][] OldTileData { get; set; }
        public Layer.LayerTilesData TilesData { get; set; }
        public Layer.LayerTilesData PaletteTilesData { get; set; }
        public uint PaletteColumns
        {
            get { return PaletteTilesData.TilesX; }
            set
            {
                PaletteTilesData.TilesX = Math.Max(value, 1);
                SetPaletteColumns(PaletteTilesData.Background, PaletteTilesData.TilesX);
                PopulatePalette();
            }
        }
        public double PaletteCursorX { get; set; }
        public double PaletteCursorY { get; set; }
        public double PaletteCursorWidth { get; set; }
        public double PaletteCursorHeight { get; set; }
        public Visibility PaletteCursorVisibility { get; set; }

        public Layer.LayerTilesData BrushTilesData { get; set; }
        private bool BrushEmpty { get; set; } = true;
        public double BrushWidth { get; set; }
        public double BrushHeight { get; set; }
        public double BrushPreviewX { get; set; } = 0;
        public double BrushPreviewY { get; set; } = 0;
        public Visibility BrushPreviewVisibility { get; set; }
        public Visibility BrushOutlineVisibility { get; set; }
        public Visibility BrushPickVisibility { get; set; }
        public long RefreshBrush { get; set; } = 0;


        public RenderTargetBitmap RoomPreview { get; set; }
        public float RoomPrevOffsetX { get; set; }
        public float RoomPrevOffsetY { get; set; }
        public Point ScrollViewStart { get; set; }
        public Point DrawingStart { get; set; }
        public Point LastMousePos { get; set; }

        private bool apply { get; set; } = false;

        private ScrollViewer FocusedTilesScroll { get; set; }
        private Layer.LayerTilesData FocusedTilesData { get; set; }
        private TileLayerImage FocusedTilesImage { get; set; }

        private enum Painting
        {
            None,
            Draw,
            Erase,
            Pick,
            DragPick,
            Drag,

        }
        private Painting painting { get; set; } = Painting.None;

        private static CachedTileDataLoader loader = new();
        private byte[] emptyTile { get; set; }
        private Dictionary<uint, byte[]> TileCache { get; set; }

        public Rect GridRect { get; set; }
        public Point GridPoint1 { get; set; }
        public Point GridPoint2 { get; set; }

        public string StatusText { get; set; } = "";

        private static List<(WeakReference<UndertaleBackground>, uint)> PaletteColumnsMap { get; set; } = new();
        public static uint GetPaletteColumns(UndertaleBackground background)
        {
            // Look through entire list, clearing out old weak references
            uint paletteColumns = background.GMS2TileColumns;
            for (int i = PaletteColumnsMap.Count - 1; i >= 0; i--)
            {
                (WeakReference<UndertaleBackground> reference, uint thisColumns) = PaletteColumnsMap[i];
                if (reference.TryGetTarget(out UndertaleBackground thisBg))
                {
                    if (thisBg == background)
                    {
                        paletteColumns = thisColumns;
                    }
                }
                else
                {
                    // Clear out old weak reference
                    PaletteColumnsMap.RemoveAt(i);
                }
            }
            return paletteColumns;
        }
        public static void SetPaletteColumns(UndertaleBackground background, uint value)
        {
            // Look through entire list, clearing out old weak references, and possibly set the palette columns value of this background
            bool added = false;
            for (int i = PaletteColumnsMap.Count - 1; i >= 0; i--)
            {
                (WeakReference<UndertaleBackground> reference, uint _) = PaletteColumnsMap[i];
                if (reference.TryGetTarget(out UndertaleBackground thisBg))
                {
                    if (thisBg == background)
                    {
                        // Set the palette columns
                        PaletteColumnsMap[i] = (reference, value);
                        added = true;
                    }
                }
                else
                {
                    // Clear out old weak reference
                    PaletteColumnsMap.RemoveAt(i);
                }
            }
            if (added) return;
            // Add new entry
            PaletteColumnsMap.Add((new WeakReference<UndertaleBackground>(background), value));
        }

        public UndertaleTileEditor(Layer layer)
        {
            EditingLayer = layer;

            RoomPrevOffsetX = -EditingLayer.XOffset;
            RoomPrevOffsetY = -EditingLayer.YOffset;

            OldTileData = CloneTileData(EditingLayer.TilesData.TileData);
            TilesData = EditingLayer.TilesData;
            TileCache = new();

            BrushTilesData = new Layer.LayerTilesData();
            BrushTilesData.TileData = new uint[][] { new uint[] { 0 } };
            BrushTilesData.Background = TilesData.Background;
            BrushTilesData.TilesX = 1;
            BrushTilesData.TilesY = 1;
            UpdateBrush();

            PaletteTilesData = new Layer.LayerTilesData();
            PaletteTilesData.TileData = new uint[][] { new uint[] { 0 } };
            PaletteTilesData.Background = TilesData.Background;
            PaletteColumns = GetPaletteColumns(PaletteTilesData.Background);

            EditWidth = Convert.ToDouble((long)TilesData.TilesX * (long)TilesData.Background.GMS2TileWidth);
            EditHeight = Convert.ToDouble((long)TilesData.TilesY * (long)TilesData.Background.GMS2TileHeight);

            emptyTile = (byte[])Array.CreateInstance(
                typeof(byte), TilesData.Background.GMS2TileWidth * TilesData.Background.GMS2TileHeight * 4
            );
            Array.Fill<byte>(emptyTile, 0);

            GridRect = new(0, 0, TilesData.Background.GMS2TileWidth, TilesData.Background.GMS2TileHeight);
            GridPoint1 = new(TilesData.Background.GMS2TileWidth, 0);
            GridPoint2 = new(0, TilesData.Background.GMS2TileHeight);

            CachedTileDataLoader.Reset();
            TilesBitmap = new(
                (int)EditWidth, (int)EditHeight, 96, 96,
                PixelFormats.Bgra32, null
            );
            DrawTilemap(TilesData, TilesBitmap);

            this.DataContext = this;
            InitializeComponent();
        }

        #region General events
        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!IsVisible || IsLoaded)
                return;

            if (Settings.Instance.EnableDarkMode)
                MainWindow.SetDarkTitleBarForWindow(this, true, false);
        }
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (
                !apply && Modified && this.ShowQuestion(
                    "Cancel changes to the tilemap?", MessageBoxImage.Warning, "Confirmation"
                ) == MessageBoxResult.No
            )
            {
                e.Cancel = true;
                return;
            }
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            PaletteTilesData.Dispose();
            if (apply)
            {
                EditingLayer.TilesData.TileDataUpdated();
            }
            else if (Modified)
                EditingLayer.TilesData.TileData = OldTileData;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            apply = false;
            this.Close();
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            apply = true;
            this.Close();
        }
        #endregion

        #region Brush and tile palette
        private void PopulatePalette()
        {
            PaletteTilesData.TilesY = (uint)Convert.ToInt32(
                Math.Ceiling(
                    (double)PaletteTilesData.Background.GMS2TileCount /
                    PaletteTilesData.TilesX
                )
            );

            PaletteWidth = Convert.ToDouble((long)PaletteTilesData.TilesX * (long)PaletteTilesData.Background.GMS2TileWidth);
            PaletteHeight = Convert.ToDouble((long)PaletteTilesData.TilesY * (long)PaletteTilesData.Background.GMS2TileHeight);

            int i = 0;
            int itemsPerTile = (int)PaletteTilesData.Background.GMS2ItemsPerTileCount;
            int count = (int)PaletteTilesData.Background.GMS2TileCount * itemsPerTile;
            for (int y = 0; y < PaletteTilesData.TilesY; y++)
            {
                for (int x = 0; x < PaletteTilesData.TilesX; x++)
                {
                    if (i >= count)
                        PaletteTilesData.TileData[y][x] = 0;
                    else
                        PaletteTilesData.TileData[y][x] = PaletteTilesData.Background.GMS2TileIds[i].ID;
                    i += itemsPerTile;
                }
            }

            FindPaletteCursor();
        }
        private void UpdateBrush()
        {
            if (painting == Painting.DragPick)
            {
                BrushWidth = Convert.ToDouble(PaletteTilesData.Background.GMS2TileWidth);
                BrushHeight = Convert.ToDouble(PaletteTilesData.Background.GMS2TileHeight);
            }
            else
            {
                BrushWidth = Convert.ToDouble(
                    (long)BrushTilesData.TilesX * (long)BrushTilesData.Background.GMS2TileWidth
                );
                BrushHeight = Convert.ToDouble(
                    (long)BrushTilesData.TilesY * (long)BrushTilesData.Background.GMS2TileHeight
                );
            }
            BrushEmpty = true;
            for (int y = 0; y < BrushTilesData.TilesY; y++)
            {
                for (int x = 0; x < BrushTilesData.TilesX; x++)
                {
                    if ((BrushTilesData.TileData[y][x] & TILE_INDEX) != 0)
                    {
                        BrushEmpty = false;
                        break;
                    }
                }
                if (!BrushEmpty)
                    break;
            }
            UpdateBrushVisibility();
        }
        private void UpdateBrushVisibility()
        {
            bool over = TilesScroll is not null ? TilesScroll.IsMouseOver : false;
            BrushPreviewVisibility = (painting == Painting.None && over) ? Visibility.Visible : Visibility.Hidden;
            BrushOutlineVisibility =
                ((BrushEmpty && (painting == Painting.None || painting == Painting.Draw)) ||
                painting == Painting.Erase) && over ? Visibility.Visible : Visibility.Hidden;
            BrushPickVisibility =
                ((painting == Painting.Pick || (painting == Painting.DragPick &&
                    PositionToTile(LastMousePos, FocusedTilesData, out _, out _)
                )) && FocusedTilesImage == LayerImage) ? Visibility.Visible : Visibility.Hidden;
        }
        #endregion

        #region Tile painting and picking
        // Places the current brush onto a tilemap.
        // ox and oy specify the origin point of multi-tile brushes.
        private void PaintTile(int x, int y, int ox, int oy, Layer.LayerTilesData tilesData, bool erase = false)
        {
            int maxX = (int)Math.Min(x + BrushTilesData.TilesX, tilesData.TilesX);
            int maxY = (int)Math.Min(y + BrushTilesData.TilesY, tilesData.TilesY);
            for (int ty = (int)Math.Max(y, 0); ty < maxY; ty++)
            {
                for (int tx = (int)Math.Max(x, 0); tx < maxX; tx++)
                {
                    if (erase)
                        SetTile(tx, ty, tilesData, 0);
                    else
                        SetBrushTile(tilesData, tx, ty, ox, oy);
                }
            }
        }
        private void PaintLine(Layer.LayerTilesData tilesData, Point pos1, Point pos2, Point start, bool erase = false)
        {
            PositionToTile(pos1, tilesData, out int x1, out int y1);
            PositionToTile(pos2, tilesData, out int x2, out int y2);
            PositionToTile(start, tilesData, out int ox, out int oy);

            Line(tilesData, x1, y1, x2, y2, ox, oy, erase);
        }

        private void SetTile(int x, int y, Layer.LayerTilesData tilesData, uint tileID)
        {
            Modified = true;
            if (tilesData.TileData[y][x] != tileID)
            {
                Tuple<int, int> key = new(x, y);
                UndoStack[UndoStack.Count - 1][key] = tilesData.TileData[y][x];

                tilesData.TileData[y][x] = tileID;
                DrawTile(
                    tilesData.Background, tileID,
                    TilesBitmap, x, y
                );
            }
        }

        // Places one tile of the current brush.
        // ox and oy specify the origin point of multi-tile brushes.
        private void SetBrushTile(Layer.LayerTilesData tilesData, int x, int y, int ox, int oy)
        {
            int tx = mod(x - ox, (int)BrushTilesData.TilesX);
            int ty = mod(y - oy, (int)BrushTilesData.TilesY);
            uint tile = BrushTilesData.TileData[ty][tx];
            if ((tile & TILE_INDEX) != 0 || BrushEmpty)
                SetTile(x, y, tilesData, tile);
        }

        private void Fill(Layer.LayerTilesData tilesData, int x, int y, bool global, bool erase = false)
        {
            uint[][] data = tilesData.TileData;
            uint replace = data[y][x];

            if (global)
            {
                for (int fy = 0; fy < tilesData.TilesY; fy++)
                {
                    for (int fx = 0; fx < tilesData.TilesX; fx++)
                    {
                        if (data[fy][fx] == replace)
                        {
                            if (erase)
                                SetTile(fx, fy, tilesData, 0);
                            else
                                SetBrushTile(tilesData, fx, fy, x, y);
                        }
                    }
                }
                return;
            }

            Stack<Tuple<int, int>> stack = new();
            stack.Push(new(x, y));
            HashSet<Tuple<int, int>> handled = new();
            while (stack.Count > 0)
            {
                Tuple<int, int> tuple = stack.Pop();
                if (handled.Contains(tuple))
                    continue;
                handled.Add(tuple);
                int fx = tuple.Item1;
                int fy = tuple.Item2;
                if (data[fy][fx] == replace)
                {
                    if (erase)
                        SetTile(fx, fy, tilesData, 0);
                    else
                        SetBrushTile(tilesData, fx, fy, x, y);
                    // if this fill just did nothing
                    // (fixes infinite loops)
                    if (data[fy][fx] == replace)
                        continue;
                    if (fx > 0) stack.Push(new(fx - 1, fy));
                    if (fy > 0) stack.Push(new(fx, fy - 1));
                    if (fx < (tilesData.TilesX - 1)) stack.Push(new(fx + 1, fy));
                    if (fy < (tilesData.TilesY - 1)) stack.Push(new(fx, fy + 1));
                }
            }
        }

        private void Line(Layer.LayerTilesData tilesData, int x1, int y1, int x2, int y2, int ox, int oy, bool erase = false)
        {
            int dx = Math.Abs(x2 - x1);
            int sx = x1 < x2 ? 1 : -1;
            int dy = -Math.Abs(y2 - y1);
            int sy = y1 < y2 ? 1 : -1;
            int error = dx + dy;

            while (true)
            {
                PaintTile(x1, y1, settings.BrushTiling ? ox : x1, settings.BrushTiling ? oy : y1, tilesData, erase);

                if (x1 == x2 && y1 == y2)
                    break;

                int e2 = 2 * error;
                if (e2 >= dy)
                {
                    if (x1 == x2)
                        break;
                    error += dy;
                    x1 += sx;
                }
                if (e2 <= dx)
                {
                    if (y1 == y2)
                        break;
                    error += dx;
                    y1 += sy;
                }
            }
        }

        private void Pick(Point pos, Point drawingStart, Layer.LayerTilesData tilesData)
        {
            bool boundsA = PositionToTile(drawingStart, tilesData, out int x1, out int y1);
            bool boundsB = PositionToTile(pos, tilesData, out int x2, out int y2);
            if (!boundsA && !boundsB) return;
            x1 = Math.Clamp(x1, 0, (int)tilesData.TilesX - 1);
            y1 = Math.Clamp(y1, 0, (int)tilesData.TilesY - 1);
            x2 = Math.Clamp(x2, 0, (int)tilesData.TilesX - 1);
            y2 = Math.Clamp(y2, 0, (int)tilesData.TilesY - 1);
            if (x2 < x1)
            {
                (x1, x2) = (x2, x1);
            }
            if (y2 < y1)
            {
                (y1, y2) = (y2, y1);
            }

            BrushTilesData.TilesX = (uint)(Math.Abs(x2 - x1) + 1);
            BrushTilesData.TilesY = (uint)(Math.Abs(y2 - y1) + 1);

            for (int y = 0; y < BrushTilesData.TilesY; y++)
            {
                for (int x = 0; x < BrushTilesData.TilesX; x++)
                {
                    BrushTilesData.TileData[y][x] = tilesData.TileData[y1 + y][x1 + x];
                }
            }

            UpdateBrush();

            if (tilesData == PaletteTilesData)
            {
                MovePaletteCursor(x1, y1);
                ResizePaletteCursor();
                PaletteCursorVisibility = Visibility.Visible;
            }
        }

        private void FindPaletteCursor()
        {
            if (BrushTilesData.TilesX > 1 || BrushTilesData.TilesY > 1)
            {
                PaletteCursorVisibility = Visibility.Hidden;
                return;
            }
            PaletteCursorVisibility = Visibility.Visible;

            uint brushTile = BrushTilesData.TileData[0][0] & TILE_INDEX;
            int index = PaletteTilesData.Background.GMS2TileIds.FindIndex(
                id => id.ID == brushTile
            );
            if (index == -1)
                index = 0;
            MovePaletteCursor((int)(index / PaletteTilesData.Background.GMS2ItemsPerTileCount));
            ResizePaletteCursor();
            if (PaletteCursor is not null)
                PaletteCursor.BringIntoView();
        }
        private void MovePaletteCursor(int index)
        {
            MovePaletteCursor((index % (int)PaletteTilesData.TilesX), (index / (int)PaletteTilesData.TilesX));
        }
        private void MovePaletteCursor(int x, int y)
        {
            PaletteCursorX = x * (int)PaletteTilesData.Background.GMS2TileWidth;
            PaletteCursorY = y * (int)PaletteTilesData.Background.GMS2TileHeight;
        }
        private void ResizePaletteCursor()
        {
            PaletteCursorWidth = BrushTilesData.TilesX * (int)PaletteTilesData.Background.GMS2TileWidth;
            PaletteCursorHeight = BrushTilesData.TilesY * (int)PaletteTilesData.Background.GMS2TileHeight;
        }

        private void Tiles_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender == TilesCanvas)
            {
                FocusedTilesScroll = TilesScroll;
                FocusedTilesImage = LayerImage;
                FocusedTilesData = TilesData;
            }
            else
            {
                FocusedTilesScroll = PaletteScroll;
                FocusedTilesImage = PaletteLayerImage;
                FocusedTilesData = PaletteTilesData;
            }

            DrawingStart = e.GetPosition(FocusedTilesImage);
            LastMousePos = DrawingStart;

            if (e.MiddleButton == MouseButtonState.Pressed)
            {
                painting = Painting.DragPick;
                DrawingStart = e.GetPosition(this as Window);
                ScrollViewStart = new Point(FocusedTilesScroll.HorizontalOffset, FocusedTilesScroll.VerticalOffset);
                UpdateBrush();
            }
            else if (FocusedTilesScroll == PaletteScroll)
            {
                if (PositionToTile(DrawingStart, FocusedTilesData, out _, out _))
                {
                    painting = Painting.Pick;
                    Pick(DrawingStart, DrawingStart, FocusedTilesData);
                }
            }
            else if (e.RightButton == MouseButtonState.Pressed)
            {
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                {
                    if (PositionToTile(DrawingStart, FocusedTilesData, out int x, out int y))
                    {
                        RecordUndo();
                        Fill(FocusedTilesData, x, y, Keyboard.Modifiers.HasFlag(ModifierKeys.Shift), true);
                        painting = Painting.None;
                    }
                }
                else
                {
                    RecordUndo();
                    PositionToTile(DrawingStart, FocusedTilesData, out int x, out int y);
                    PaintTile(x, y, x, y, FocusedTilesData, true);
                    painting = Painting.Erase;
                }
            }
            else if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
                {
                    if (PositionToTile(DrawingStart, FocusedTilesData, out _, out _))
                    {
                        Pick(DrawingStart, DrawingStart, FocusedTilesData);
                        painting = Painting.Pick;
                    }
                }
                else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                {
                    if (PositionToTile(DrawingStart, FocusedTilesData, out int x, out int y))
                    {
                        RecordUndo();
                        Fill(FocusedTilesData, x, y, Keyboard.Modifiers.HasFlag(ModifierKeys.Shift), false);
                        painting = Painting.None;
                    }
                }
                else
                {
                    RecordUndo();
                    PositionToTile(DrawingStart, FocusedTilesData, out int x, out int y);
                    PaintTile(x, y, x, y, FocusedTilesData, false);
                    painting = Painting.Draw;
                }
            }
            UpdateBrushVisibility();
        }
        private void Tiles_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (painting == Painting.DragPick)
            {
                Pick(e.GetPosition(FocusedTilesImage), e.GetPosition(FocusedTilesImage), FocusedTilesData);
                FindPaletteCursor();
                RefreshBrush++;
            }
            EndDrawing(e);
        }
        private void Tiles_MouseMove(object sender, MouseEventArgs e)
        {
            PositionToTile(e.GetPosition(LayerImage as TileLayerImage), TilesData, out int mapX, out int mapY);
            StatusText = $"x: {mapX}  y: {mapY}";

            if (painting != Painting.Pick)
            {
                BrushPreviewX = Convert.ToDouble((long)mapX * (long)TilesData.Background.GMS2TileWidth);
                BrushPreviewY = Convert.ToDouble((long)mapY * (long)TilesData.Background.GMS2TileHeight);
            }
            else
            {
                PositionToTile(
                    DrawingStart, TilesData, out int startX, out int startY
                );
                if (mapX < startX) startX = mapX;
                if (mapY < startY) startY = mapY;
                BrushPreviewX = Convert.ToDouble((long)Math.Max(startX, 0) * (long)TilesData.Background.GMS2TileWidth);
                BrushPreviewY = Convert.ToDouble((long)Math.Max(startY, 0) * (long)TilesData.Background.GMS2TileHeight);
            }

            UpdateBrushVisibility();

            if (FocusedTilesScroll is null)
                return;

            if (painting == Painting.DragPick || painting == Painting.Drag)
            {
                Point pos = e.GetPosition(this as Window);
                if (painting == Painting.DragPick && pos != DrawingStart)
                {
                    painting = Painting.Drag;
                    UpdateBrush();
                }
                FocusedTilesScroll.ScrollToHorizontalOffset(Math.Clamp(
                    ScrollViewStart.X + -(pos.X - DrawingStart.X), 0, FocusedTilesScroll.ScrollableWidth
                ));
                FocusedTilesScroll.ScrollToVerticalOffset(Math.Clamp(
                    ScrollViewStart.Y + -(pos.Y - DrawingStart.Y), 0, FocusedTilesScroll.ScrollableHeight
                ));
                return;
            }

            if (painting == Painting.Draw)
            {
                Point pos = e.GetPosition(FocusedTilesImage);
                PaintLine(FocusedTilesData, LastMousePos, pos, DrawingStart, false);
                LastMousePos = pos;
            }
            else if (painting == Painting.Erase)
            {
                Point pos = e.GetPosition(FocusedTilesImage);
                PaintLine(FocusedTilesData, LastMousePos, pos, DrawingStart, true);
                LastMousePos = pos;
            }
            else if (painting == Painting.Pick)
                Pick(e.GetPosition(FocusedTilesImage), DrawingStart, FocusedTilesData);
        }
        private void Window_MouseLeave(object sender, MouseEventArgs e)
        {
            EndDrawing(e);
        }
        private void EndDrawing()
        {
            if (painting == Painting.None)
                return;
            painting = Painting.None;
            FocusedTilesScroll = null;
            FocusedTilesData = null;
            FocusedTilesImage = null;
            UpdateBrush();
        }
        private void EndDrawing(MouseEventArgs e)
        {
            if (painting == Painting.None)
                return;
            if (painting == Painting.Pick)
            {
                PositionToTile(e.GetPosition(LayerImage as TileLayerImage), TilesData, out int mapX, out int mapY);
                BrushPreviewX = Convert.ToDouble((long)mapX * (long)TilesData.Background.GMS2TileWidth);
                BrushPreviewY = Convert.ToDouble((long)mapY * (long)TilesData.Background.GMS2TileHeight);
                if (FocusedTilesData != PaletteTilesData)
                {
                    FindPaletteCursor();
                }
                RefreshBrush++;
            }
            EndDrawing();
        }

        private void Scroll_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scrollViewer = (ScrollViewer)sender;
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                Canvas canvas = (scrollViewer == TilesScroll ? TilesCanvas : PaletteCanvas) as Canvas;

                e.Handled = true;
                var mousePos = e.GetPosition(canvas);
                var transform = canvas.LayoutTransform as MatrixTransform;
                var matrix = transform.Matrix;
                var scale = e.Delta >= 0 ? 1.1 : (1.0 / 1.1); // choose appropriate scaling factor

                if ((matrix.M11 > 0.2 || (matrix.M11 <= 0.2 && scale > 1)) && (matrix.M11 < 6 || (matrix.M11 >= 6 && scale < 1)))
                {
                    matrix.ScaleAtPrepend(scale, scale, mousePos.X, mousePos.Y);
                }

                double offX = matrix.OffsetX;
                double offY = matrix.OffsetY;
                matrix.OffsetX = 0.0;
                matrix.OffsetY = 0.0;
                canvas.LayoutTransform = new MatrixTransform(matrix);
                // fix scroll
                scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset - offX);
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - offY);
            }
            else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset - e.Delta);
                e.Handled = true;
            }
        }
        #endregion

        #region Tile drawing
        private void DrawTilemap(Layer.LayerTilesData tilesData, WriteableBitmap wBitmap)
        {
            if ((loader.Convert(new object[] { tilesData }, null, "cache", null) as string) == "Error")
                return;

            for (int y = 0; y < tilesData.TilesY; y++)
            {
                for (int x = 0; x < tilesData.TilesX; x++)
                {
                    DrawTile(
                        tilesData.Background, tilesData.TileData[y][x],
                        wBitmap, x, y
                    );
                }
            }
        }

        // assumes a bgra32 writeablebitmap
        private void DrawTile(UndertaleBackground tileset, uint tile, WriteableBitmap wBitmap, int x, int y)
        {
            uint tileID = tile & TILE_INDEX;
            if (tileID == 0)
            {
                ClearToWBitmap(
                    wBitmap, (int)(x * tileset.GMS2TileWidth), (int)(y * tileset.GMS2TileHeight),
                    (int)tileset.GMS2TileWidth, (int)tileset.GMS2TileHeight
                );
                return;
            }

            System.Drawing.Bitmap tileBMP = CachedTileDataLoader.TileCache[new(tileset.Texture.Name.Content, tileID)];

            if ((tile & TILE_FLAGS) == 0)
            {
                if (TileCache.TryGetValue(tileID, out byte[] tileBytes))
                {
                    wBitmap.WritePixels(
                        new Int32Rect(0, 0, (int)tileset.GMS2TileWidth, (int)tileset.GMS2TileHeight),
                        tileBytes, (int)tileset.GMS2TileWidth * 4,
                        (int)(x * tileset.GMS2TileWidth), (int)(y * tileset.GMS2TileHeight)
                    );
                    return;
                }
                DrawBitmapToWBitmap(
                    tileBMP, wBitmap,
                    (int)(x * tileset.GMS2TileWidth), (int)(y * tileset.GMS2TileHeight),
                    tileID
                );
                return;
            }

            using System.Drawing.Bitmap newBMP = (System.Drawing.Bitmap)tileBMP.Clone();

            switch (tile >> 28)
            {
                case 1:
                    newBMP.RotateFlip(System.Drawing.RotateFlipType.RotateNoneFlipX);
                    break;
                case 2:
                    newBMP.RotateFlip(System.Drawing.RotateFlipType.RotateNoneFlipY);
                    break;
                case 3:
                    newBMP.RotateFlip(System.Drawing.RotateFlipType.RotateNoneFlipXY);
                    break;
                case 4:
                    newBMP.RotateFlip(System.Drawing.RotateFlipType.Rotate90FlipNone);
                    break;
                case 5:
                    // axes flipped since flip/mirror is done before rotation
                    newBMP.RotateFlip(System.Drawing.RotateFlipType.Rotate90FlipY);
                    break;
                case 6:
                    newBMP.RotateFlip(System.Drawing.RotateFlipType.Rotate90FlipX);
                    break;
                case 7:
                    newBMP.RotateFlip(System.Drawing.RotateFlipType.Rotate90FlipXY);
                    break;
                default:
                    throw new InvalidDataException($"{tile & TILE_FLAGS} is not a valid tile flag value.");
            }

            DrawBitmapToWBitmap(
                newBMP, wBitmap,
                (int)(x * tileset.GMS2TileWidth), (int)(y * tileset.GMS2TileHeight)
            );
        }
        private void DrawBitmapToWBitmap(System.Drawing.Bitmap bitmap, WriteableBitmap wBitmap, int x, int y, uint? cache = null)
        {
            byte[] arr = (byte[])Array.CreateInstance(typeof(byte), bitmap.Width * bitmap.Height * 4);

            int i = 0;
            for (int by = 0; by < bitmap.Height; by++)
            {
                for (int bx = 0; bx < bitmap.Width; bx++)
                {
                    System.Drawing.Color color = bitmap.GetPixel(bx, by);

                    arr[i] = color.B;
                    arr[i + 1] |= color.G;
                    arr[i + 2] |= color.R;
                    arr[i + 3] |= color.A;
                    i += 4;
                }
            }

            if (cache is uint cacheID)
            {
                TileCache.TryAdd(cacheID, arr);
            }

            wBitmap.WritePixels(new Int32Rect(0, 0, bitmap.Width, bitmap.Height), arr, bitmap.Width * 4, x, y);
        }
        private void ClearToWBitmap(WriteableBitmap wBitmap, int x, int y, int width, int height)
        {
            wBitmap.WritePixels(new Int32Rect(0, 0, width, height), emptyTile, width * 4, x, y);
        }
        #endregion

        #region Commands
        private void Command_Mirror(object sender, RoutedEventArgs e)
        {
            for (int y = 0; y < BrushTilesData.TilesY; y++)
            {
                Array.Reverse(BrushTilesData.TileData[y]);
                for (int x = 0; x < BrushTilesData.TilesX; x++)
                {
                    if ((BrushTilesData.TileData[y][x] & TILE_ROTATE) != 0)
                        BrushTilesData.TileData[y][x] ^= TILE_FLIP_V;
                    else
                        BrushTilesData.TileData[y][x] ^= TILE_FLIP_H;
                }
            }
            RefreshBrush++;
        }
        private void Command_Flip(object sender, RoutedEventArgs e)
        {
            Array.Reverse(BrushTilesData.TileData);
            for (int y = 0; y < BrushTilesData.TilesY; y++)
            {
                for (int x = 0; x < BrushTilesData.TilesX; x++)
                {
                    if ((BrushTilesData.TileData[y][x] & TILE_ROTATE) != 0)
                        BrushTilesData.TileData[y][x] ^= TILE_FLIP_H;
                    else
                        BrushTilesData.TileData[y][x] ^= TILE_FLIP_V;
                }
            }
            RefreshBrush++;
        }
        private void Command_RotateCW(object sender, RoutedEventArgs e)
        {
            uint[][] oldTileData = CloneTileData(BrushTilesData.TileData);
            uint _tilesX = BrushTilesData.TilesX;
            uint _tilesY = BrushTilesData.TilesY;
            BrushTilesData.TilesX = _tilesY;
            BrushTilesData.TilesY = _tilesX;
            for (int y = 0; y < _tilesY; y++)
            {
                for (int x = 0; x < _tilesX; x++)
                {
                    uint tile = oldTileData[y][x];
                    uint flags = ROTATION_CW[(uint)(tile >> 28)] << 28;
                    BrushTilesData.TileData[x][_tilesY - y - 1] = (uint)((tile & TILE_INDEX) | flags);
                }
            }
            UpdateBrush();
            RefreshBrush++;
        }
        private void Command_RotateCCW(object sender, RoutedEventArgs e)
        {
            uint[][] oldTileData = CloneTileData(BrushTilesData.TileData);
            uint _tilesX = BrushTilesData.TilesX;
            uint _tilesY = BrushTilesData.TilesY;
            BrushTilesData.TilesX = _tilesY;
            BrushTilesData.TilesY = _tilesX;
            for (int y = 0; y < _tilesY; y++)
            {
                for (int x = 0; x < _tilesX; x++)
                {
                    uint tile = oldTileData[y][x];
                    uint flags = ROTATION_CCW[(uint)(tile >> 28)] << 28;
                    BrushTilesData.TileData[_tilesX - x - 1][y] = (uint)((tile & TILE_INDEX) | flags);
                }
            }
            UpdateBrush();
            RefreshBrush++;
        }
        private void Command_ToggleGrid(object sender, RoutedEventArgs e)
        {
            settings.ShowGridBool = !settings.ShowGridBool;
        }
        private void Command_ToggleBrushTiling(object sender, RoutedEventArgs e)
        {
            settings.BrushTiling = !settings.BrushTiling;
        }
        private void Command_TogglePreview(object sender, RoutedEventArgs e)
        {
            settings.RoomPreviewBool = !settings.RoomPreviewBool;
        }
        private void Command_Undo(object sender, RoutedEventArgs e)
        {
            if (UndoStack.Count == 0)
                return;
            EndDrawing();
            int index = UndoStack.Count - 1;
            var undoData = UndoStack[index];
            ApplyUndo(undoData);
            UndoStack.RemoveAt(index);
            RedoStack.Add(undoData);
            UndoEnabled = UndoStack.Count > 0;
            RedoEnabled = true;

        }
        private void Command_Redo(object sender, RoutedEventArgs e)
        {
            if (RedoStack.Count == 0)
                return;
            EndDrawing();
            int index = RedoStack.Count - 1;
            var undoData = RedoStack[index];
            ApplyUndo(undoData);
            RedoStack.RemoveAt(index);
            UndoStack.Add(undoData);
            UndoEnabled = true;
            RedoEnabled = RedoStack.Count > 0;
        }
        private void RecordUndo()
        {
            if (UndoStack.Count >= 100)
                UndoStack.RemoveAt(1);
            UndoStack.Add(new());
            RedoStack.Clear();
            UndoEnabled = true;
            RedoEnabled = false;
        }
        // Applies some undo data, and also "swaps" it
        // to instead redo.
        private void ApplyUndo(Dictionary<Tuple<int, int>, uint> data)
        {
            foreach (KeyValuePair<Tuple<int, int>, uint> kvp in data)
            {
                int x = kvp.Key.Item1;
                int y = kvp.Key.Item2;
                uint tile = kvp.Value;
                uint oldTile = TilesData.TileData[y][x];
                TilesData.TileData[y][x] = tile;
                DrawTile(
                    TilesData.Background, tile,
                    TilesBitmap, x, y
                );
                data[kvp.Key] = oldTile;
            }
        }
        #endregion

        #region Utilities
        private int mod(int left, int right)
        {
            int remainder = left % right;
            return remainder < 0 ? remainder + right : remainder;
        }

        private uint[][] CloneTileData(uint[][] tileData)
        {
            uint[][] newTileData = (uint[][])tileData.Clone();
            for (int i = 0; i < tileData.Length; i++)
                newTileData[i] = (uint[])tileData[i].Clone();
            return newTileData;
        }

        private bool PositionToTile(Point p, Layer.LayerTilesData tilesData, out int x, out int y)
        {
            x = Convert.ToInt32(Math.Floor(p.X / tilesData.Background.GMS2TileWidth));
            y = Convert.ToInt32(Math.Floor(p.Y / tilesData.Background.GMS2TileHeight));

            return TileInBounds(x, y, tilesData);
        }

        private bool TileInBounds(int x, int y, Layer.LayerTilesData tilesData)
        {
            if (x < 0 || y < 0) return false;
            if (x >= tilesData.TilesX || y >= tilesData.TilesY) return false;
            return true;
        }
        #endregion
    }
}
