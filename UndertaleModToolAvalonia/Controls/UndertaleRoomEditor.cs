using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia;

public class UndertaleRoomEditor : Control
{
    public record RoomItem(
        object Object,
        UndertaleRoom.Layer? Layer = null,
        RoomItemSelectable? Selectable = null
    );
    public record RoomItemProperties(int X, int Y);
    public record RoomItemSelectable(
        object Category,
        Rect Bounds,
        double Rotation,
        Point Pivot,
        Func<RoomItemProperties> GetProperties,
        Action<RoomItemProperties> SetProperties
    );

    enum InteractionMode
    {
        Items,
        Tiles,
    }

    UndertaleRoomViewModel? vm;

    readonly Updater updaterInstance = new();
    readonly Renderer rendererInstance = new();

    double customDrawOperationTime;

    // Room controls
    Vector translation = new(0, 0);
    double scaling = 1;

    bool translationMoving = false;
    bool translationHasMoved = false;
    Point translationMoveOffset = new(0, 0);

    Point pointerPosition;
    Point pointerPositionInRoom;

    Point itemMoveOffset = new(0, 0);

    RoomItem? hoveredRoomItem;

    uint? hoveredTile = null;

    public UndertaleRoomEditor()
    {
        ClipToBounds = true;
        Focusable = true;
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        vm = (DataContext as UndertaleRoomViewModel)!;
        vm?.Room.SetupRoom();

        translation = new(0, 0);
        scaling = 1;

        updaterInstance.Room = vm?.Room;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        PointerPoint pointerPoint = e.GetCurrentPoint(this);
        InteractionMode interactionMode = GetInteractionMode();

        if (pointerPoint.Properties.IsMiddleButtonPressed)
        {
            TranslationMoveOnPressed();
        }

        if (interactionMode == InteractionMode.Items)
        {
            if (pointerPoint.Properties.IsLeftButtonPressed)
            {
                ItemMoveOnPressed();
            }
        }
        else if (interactionMode == InteractionMode.Tiles)
        {
            UndertaleRoom.Layer? tilesLayer = GetSelectedTilesLayer();
            if (tilesLayer is not null)
            {
                if (pointerPoint.Properties.IsLeftButtonPressed)
                {
                    SetLayerTileAtPointer(tilesLayer, vm!.SelectedTileData);
                }
                else if (pointerPoint.Properties.IsRightButtonPressed)
                {
                    SetLayerTileAtPointer(tilesLayer, 0);
                }
            }
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        PointerPoint pointerPoint = e.GetCurrentPoint(this);
        InteractionMode interactionMode = GetInteractionMode();
        UndertaleRoom.Layer? tilesLayer = GetSelectedTilesLayer();

        pointerPosition = e.GetPosition(this);
        pointerPositionInRoom = (pointerPosition - translation) / scaling;

        TranslationMoveOnMoved();

        if (interactionMode == InteractionMode.Items)
        {
            if (pointerPoint.Properties.IsLeftButtonPressed)
            {
                ItemMoveOnMoved();
            }
        }
        else if (interactionMode == InteractionMode.Tiles)
        {
            if (tilesLayer is not null)
            {
                if (pointerPoint.Properties.IsLeftButtonPressed)
                {
                    SetLayerTileAtPointer(tilesLayer, vm!.SelectedTileData);
                }
                else if (pointerPoint.Properties.IsRightButtonPressed)
                {
                    SetLayerTileAtPointer( tilesLayer, 0);
                }
            }
        }

        hoveredRoomItem = null;
        hoveredTile = null;

        if (tilesLayer is not null)
        {
            hoveredTile = GetLayerTileAtPointer(tilesLayer);
        }
        else
        {
            ItemHoverOnMoved();
        }

        vm!.StatusText = $"({Math.Floor(pointerPositionInRoom.X)}, {Math.Floor(pointerPositionInRoom.Y)})";
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        InteractionMode interactionMode = GetInteractionMode();
        UndertaleRoom.Layer? tilesLayer = GetSelectedTilesLayer();

        if (interactionMode == InteractionMode.Tiles)
        {
            if (tilesLayer is not null)
            {
                if (e.InitialPressMouseButton == MouseButton.Middle)
                {
                    if (!translationHasMoved)
                    {
                        uint? tile = GetLayerTileAtPointer(tilesLayer);
                        if (tile is not null)
                            vm!.SelectedTileData = (uint)tile;
                    }
                }    
            }
        }

        TranslationMoveOnReleased();
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        if (e.Delta.Y > 0)
        {
            translation *= 2;
            translation -= pointerPosition;
            scaling *= 2;
        }
        else if (e.Delta.Y < 0)
        {
            scaling /= 2;
            translation += pointerPosition;
            translation /= 2;
        }

        translation = new Vector(Math.Round(translation.X), Math.Round(translation.Y));

        vm!.Zoom = scaling;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.PhysicalKey == PhysicalKey.Space)
        {
            TranslationMoveOnPressed();
        }
        else if (e.PhysicalKey == PhysicalKey.F)
        {
            FocusOnSelectedItem();
        }
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        if (e.PhysicalKey == PhysicalKey.Space)
        {
            TranslationMoveOnReleased();
        }
    }

    public override void Render(DrawingContext context)
    {
        if (IsEffectivelyVisible)
        {
            scaling = vm?.Zoom ?? 1;

            updaterInstance.Update();

            context.Custom(new CustomDrawOperation(this));

#if DEBUG
            RenderDebugText(context);
#endif
        }

        TopLevel topLevel = TopLevel.GetTopLevel(this)!;
        topLevel.RequestAnimationFrame(_ =>
        {
            InvalidateVisual();
        });
    }

    InteractionMode GetInteractionMode()
    {
        if (GetSelectedTilesLayer() is not null)
        {
            return InteractionMode.Tiles;
        }
        else
        {
            return InteractionMode.Items;
        }
    }

    UndertaleRoom.Layer? GetSelectedTilesLayer()
    {
        if (vm!.RoomTreeItemsSelectedItem is UndertaleRoom.Layer { LayerType: UndertaleRoom.LayerType.Tiles } tilesLayer)
        {
            return tilesLayer;
        }
        return null;
    }

    void TranslationMoveOnPressed()
    {
        Focus();
        translationMoving = true;
        translationMoveOffset = pointerPosition - translation;
    }

    void TranslationMoveOnMoved()
    {
        if (translationMoving)
        {
            translationHasMoved = true;
            translation = pointerPosition - translationMoveOffset;
        }
    }

    void TranslationMoveOnReleased()
    {
        translationMoving = false;
        translationHasMoved = false;
    }

    void ItemHoverOnMoved()
    {
        foreach (RoomItem roomItem in updaterInstance.RoomItems.Reverse<RoomItem>())
        {
            if (roomItem.Selectable is null)
                continue;

            if (vm!.IsSelectAnyLayerEnabled || vm!.CategorySelected is null || roomItem.Selectable.Category == vm!.CategorySelected)
                if (RectContainsPoint(roomItem.Selectable.Bounds, roomItem.Selectable.Rotation, roomItem.Selectable.Pivot, pointerPositionInRoom))
                {
                    hoveredRoomItem = roomItem;
                    break;
                }
        }
    }

    void ItemMoveOnPressed()
    {
        if (hoveredRoomItem is not null && hoveredRoomItem.Selectable is not null)
        {
            RoomItemProperties properties = hoveredRoomItem.Selectable.GetProperties();
            itemMoveOffset = new(pointerPositionInRoom.X - properties.X, pointerPositionInRoom.Y - properties.Y);

            vm!.RoomTreeItemsSelectedItem = hoveredRoomItem.Object;
        }
        else
        {
            if (!vm!.IsSelectAnyLayerEnabled)
                vm!.RoomTreeItemsSelectedItem = vm.FindItemFromCategory(vm!.CategorySelected);
            else
                vm!.RoomTreeItemsSelectedItem = null;
        }
    }

    void ItemMoveOnMoved()
    {
        RoomItem? roomItem = GetSelectedRoomItem();
        if (roomItem is not null && roomItem.Selectable is not null)
        {
            double x = pointerPositionInRoom.X - itemMoveOffset.X;
            double y = pointerPositionInRoom.Y - itemMoveOffset.Y;

            if (vm!.IsGridEnabled)
            {
                x = (Math.Floor(pointerPositionInRoom.X / vm.GridWidth) * vm.GridWidth)
                    - (Math.Floor(itemMoveOffset.X / vm.GridWidth) * vm.GridWidth);
                y = (Math.Floor(pointerPositionInRoom.Y / vm.GridHeight) * vm.GridHeight)
                    - (Math.Floor(itemMoveOffset.Y / vm.GridHeight) * vm.GridHeight);
            }

            roomItem.Selectable.SetProperties(new((int)x, (int)y));
            updaterInstance.Update();
        }
    }

    RoomItem? GetSelectedRoomItem()
    {
        object? roomSelectedItem = vm?.RoomTreeItemsSelectedItem;
        if (roomSelectedItem is not null)
        {
            return updaterInstance.RoomItems.Find(x => x.Selectable is not null && x.Object == roomSelectedItem);
        }
        return null;
    }

    void FocusOnSelectedItem()
    {
        RoomItem? item = GetSelectedRoomItem();
        if (item is not null && item.Selectable is not null)
        {
            translation = new(-item.Selectable.Bounds.X * scaling + (Bounds.Width / 2), -item.Selectable.Bounds.Y * scaling + (Bounds.Height / 2));
        }
    }

    bool GetLayerTileIndexesAtPointer(UndertaleRoom.Layer tilesLayer, out (int x, int y) point)
    {
        point = default;

        if (tilesLayer.TilesData.Background is null)
            return false;

        int x = (int)Math.Floor((pointerPositionInRoom.X - tilesLayer.XOffset) / tilesLayer.TilesData.Background.GMS2TileWidth);
        int y = (int)Math.Floor((pointerPositionInRoom.Y - tilesLayer.YOffset) / tilesLayer.TilesData.Background.GMS2TileHeight);

        if (y >= 0 && x >= 0
            && y < tilesLayer.TilesData.TileData.Length
            && x < tilesLayer.TilesData.TileData[y].Length)
        {
            point = (x, y);
            return true;
        }

        return false;
    }

    uint? GetLayerTileAtPointer(UndertaleRoom.Layer tilesLayer)
    {
        if (GetLayerTileIndexesAtPointer(tilesLayer, out (int x, int y) point))
            return tilesLayer.TilesData.TileData[point.y][point.x];

        return null;
    }

    void SetLayerTileAtPointer(UndertaleRoom.Layer tilesLayer, uint tileData)
    {
        if (GetLayerTileIndexesAtPointer(tilesLayer, out (int x, int y) point))
        {
            if ((tileData & UndertaleRoomViewModel.TILE_ID) < tilesLayer.TilesData.Background.GMS2TileCount)
                tilesLayer.TilesData.TileData[point.y][point.x] = tileData;
        }
    }

    void RenderDebugText(DrawingContext context)
    {
        // Debug text
        Point roomMousePosition = ((pointerPosition - translation) / scaling);

        static string GetTileInfo(uint? tile)
        {
            if (tile is uint tileNN)
            {
                uint tileId = tileNN & UndertaleRoomViewModel.TILE_ID;
                uint tileOrientation = tileNN >> 28;

                float scaleX = (((tileOrientation >> 0) & 1) == 0) ? 1 : -1;
                float scaleY = (((tileOrientation >> 1) & 1) == 0) ? 1 : -1;
                float rotate = (((tileOrientation >> 2) & 1) == 0) ? 0 : 90;
                return $"id: {tileId} xs: {scaleX} ys: {scaleY} r: {rotate}";
            }

            return "";
        }

        context.DrawText(new FormattedText(
            $"mouse: ({pointerPosition.X}, {pointerPosition.Y})\n" +
            $"view: ({-translation.X}, {-translation.Y}, {-translation.X + Bounds.Width}, {-translation.Y + Bounds.Height})\n" +
            $"category: {vm?.CategorySelected}\n" +
            $"custom render time: <{customDrawOperationTime} ms\n" +
            $"hovered room item: {hoveredRoomItem?.Object}\n" +
            $"hovered tile: {GetTileInfo(hoveredTile)}\n" +
            $"selected tile: {GetTileInfo(vm?.SelectedTileData)}",
            CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, 12, new SolidColorBrush(Colors.White)),
            new Point(0, 0));
    }

    static bool RectContainsPoint(Rect rect, double rotation, Point pivot, Point point)
    {
        // TODO: Use matrices
        double rotationRadians = rotation * (Math.PI / 180);
        double sin = Math.Sin(-rotationRadians);
        double cos = Math.Cos(-rotationRadians);

        Point newPoint = point - pivot;
        newPoint = new Point(newPoint.X * cos - newPoint.Y * sin, newPoint.X * sin + newPoint.Y * cos);
        newPoint += pivot;

        return newPoint.X >= rect.Left && newPoint.X <= rect.Right && newPoint.Y >= rect.Top && newPoint.Y <= rect.Bottom;
    }

    class CustomDrawOperation : ICustomDrawOperation
    {
        public Rect Bounds { get; set; }

        readonly UndertaleRoomEditor editor;
        readonly UndertaleRoomViewModel vm;
        readonly Vector translation;
        readonly double scaling;

        readonly List<RoomItem> roomItems;

        readonly RoomItem? selectedRoomItem;
        readonly RoomItem? hoveredRoomItem;

        public CustomDrawOperation(UndertaleRoomEditor editor)
        {
            this.editor = editor;
            Bounds = new(0, 0, editor.Bounds.Width, editor.Bounds.Height);

            vm = editor.vm!;
            translation = editor.translation;
            scaling = editor.scaling;

            // TODO: Actually copy items?
            roomItems = new(editor.updaterInstance.RoomItems);

            selectedRoomItem = editor.GetSelectedRoomItem();
            hoveredRoomItem = editor.hoveredRoomItem;
        }

        public void Dispose() { }

        public bool Equals(ICustomDrawOperation? other) => false;

        public bool HitTest(Point p) => Bounds.Contains(p);

        public void Render(ImmediateDrawingContext context)
        {
            try
            {
                Stopwatch stopWatch = new();
                stopWatch.Start();

                //

                var leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
                if (leaseFeature is null)
                    return;

                using var lease = leaseFeature.Lease();
                SKCanvas canvas = lease.SkCanvas;
                canvas.Save();

                //

                // Fill background of entire control
                canvas.DrawRect(0, 0, (float)Bounds.Width, (float)Bounds.Height, new SKPaint { Color = SKColors.Gray });

                // Draw room outline
                canvas.DrawRect((float)translation.X - 1,
                    (float)translation.Y - 1,
                    (float)Math.Ceiling(vm.Room.Width * scaling + 1),
                    (float)Math.Ceiling(vm.Room.Height * scaling + 1),
                    new SKPaint { Color = SKColors.White, Style = SKPaintStyle.Stroke });

                // Transform
                canvas.Translate((float)translation.X, (float)translation.Y);
                canvas.Scale((float)scaling);

                editor.rendererInstance.Room = vm.Room;
                editor.rendererInstance.RoomItems = roomItems;
                editor.rendererInstance.Canvas = canvas;
                editor.rendererInstance.RenderRoom();

                if (vm.IsGridEnabled)
                {
                    if (vm.GridWidth * scaling >= 2)
                        for (uint x = 0; x < vm.Room.Width; x += vm.GridWidth)
                        {
                            canvas.DrawLine(x, 0, x, vm.Room.Height, new SKPaint { Color = SKColors.White.WithAlpha(64), BlendMode = SKBlendMode.Difference });
                        }

                    if (vm.GridHeight * scaling >= 2)
                        for (uint y = 0; y < vm.Room.Height; y += vm.GridHeight)
                        {
                            canvas.DrawLine(0, y, vm.Room.Width, y, new SKPaint { Color = SKColors.White.WithAlpha(64), BlendMode = SKBlendMode.Difference });
                        }
                }

                if (selectedRoomItem is not null && selectedRoomItem.Selectable is not null)
                {
                    SKRect rect = selectedRoomItem.Selectable.Bounds.ToSKRect();

                    canvas.Save();
                    canvas.RotateDegrees((float)selectedRoomItem.Selectable.Rotation,
                        (float)(selectedRoomItem.Selectable.Pivot.X),
                        (float)(selectedRoomItem.Selectable.Pivot.Y));
                    canvas.DrawRect(rect, new SKPaint { Color = SKColors.Blue.WithAlpha(128), StrokeWidth = 2, Style = SKPaintStyle.Stroke });
                    canvas.Restore();
                }

                if (hoveredRoomItem is not null && hoveredRoomItem.Selectable is not null)
                {
                    SKRect rect = hoveredRoomItem.Selectable.Bounds.ToSKRect();

                    canvas.Save();
                    canvas.RotateDegrees((float)hoveredRoomItem.Selectable.Rotation,
                            (float)(hoveredRoomItem.Selectable.Pivot.X),
                            (float)(hoveredRoomItem.Selectable.Pivot.Y));
                    canvas.DrawRect(rect, new SKPaint { Color = SKColors.Blue.WithAlpha(128), Style = SKPaintStyle.Stroke });
                    canvas.Restore();
                }

                canvas.Restore();

                stopWatch.Stop();
                editor.customDrawOperationTime = Math.Ceiling(stopWatch.Elapsed.TotalMilliseconds);
            }
            catch (Exception e)
            {
                Debugger.Break();
                throw;
            }
        }
    }

    public class Updater()
    {
        public UndertaleRoom? Room = null;
        public readonly List<RoomItem> RoomItems = [];

        public void Update()
        {
            RoomItems.Clear();

            if (Room is null)
                return;

            if (Room.Flags.HasFlag(UndertaleRoom.RoomEntryFlags.IsGMS2) || Room.Flags.HasFlag(UndertaleRoom.RoomEntryFlags.IsGM2024_13))
            {
                IOrderedEnumerable<UndertaleRoom.Layer> layers = Room.Layers.Reverse().OrderByDescending(x => x.LayerDepth);
                foreach (UndertaleRoom.Layer layer in layers)
                {
                    if (!layer.IsVisible)
                        continue;

                    switch (layer.LayerType)
                    {
                        case UndertaleRoom.LayerType.Path:
                        case UndertaleRoom.LayerType.Path2:
                            break;
                        case UndertaleRoom.LayerType.Background:
                            UpdateLayerBackground(layer);
                            break;
                        case UndertaleRoom.LayerType.Instances:
                            UpdateGameObjects(layer.InstancesData.Instances, layer);
                            break;
                        case UndertaleRoom.LayerType.Assets:
                            UpdateTiles(layer.AssetsData.LegacyTiles, layer);
                            UpdateSprites(layer.AssetsData.Sprites, layer);
                            // layer.AssetsData.Sequences
                            // layer.AssetsData.NineSlices
                            // layer.AssetsData.ParticleSystems
                            // layer.AssetsData.TextItems
                            break;
                        case UndertaleRoom.LayerType.Tiles:
                            UpdateLayerTiles(layer);
                            break;
                        case UndertaleRoom.LayerType.Effect:
                            // layer.EffectData
                            break;
                    }
                }
            }
            else
            {
                UpdateBackgrounds(Room.Backgrounds, foregrounds: false);
                UpdateTiles(Room.Tiles);
                UpdateGameObjects(Room.GameObjects);
                UpdateBackgrounds(Room.Backgrounds, foregrounds: true);
            }
        }

        void UpdateBackgrounds(IList<UndertaleRoom.Background> backgrounds, bool foregrounds)
        {
            foreach (var background in backgrounds)
            {
                if (background.Foreground == foregrounds)
                {
                    RoomItems.Add(new(
                       Object: background
                    ));
                }
            }
        }

        void UpdateLayerBackground(UndertaleRoom.Layer layer)
        {
            RoomItems.Add(new RoomItem(
                Object: layer
            ));
        }

        void UpdateTiles(IList<UndertaleRoom.Tile> roomTiles, UndertaleRoom.Layer? layer = null)
        {
            IOrderedEnumerable<UndertaleRoom.Tile> orderedRoomTiles = roomTiles.OrderByDescending(x => x.TileDepth);
            foreach (UndertaleRoom.Tile roomTile in orderedRoomTiles)
            {
                float x = (layer?.XOffset ?? 0) + roomTile.X;
                float y = (layer?.YOffset ?? 0) + roomTile.Y;
                float w = roomTile.Width * roomTile.ScaleX;
                float h = roomTile.Height * roomTile.ScaleY;

                RoomItems.Add(new RoomItem(
                    Object: roomTile,
                    Layer: layer,
                    Selectable: new(
                        Category: layer is not null ? layer : "Tiles",
                        Bounds: new Rect(x, y, w, h).Normalize(),
                        Rotation: 0,
                        Pivot: new Point(x, y),
                        GetProperties: () =>
                        {
                            return new(roomTile.X, roomTile.Y);
                        },
                        SetProperties: (properties) =>
                        {
                            roomTile.X = properties.X;
                            roomTile.Y = properties.Y;
                        }
                    )
                ));
            }
        }

        void UpdateLayerTiles(UndertaleRoom.Layer layer)
        {
            RoomItems.Add(new RoomItem(
                Object: layer
            ));
        }

        void UpdateSprites(IList<UndertaleRoom.SpriteInstance> roomSprites, UndertaleRoom.Layer layer)
        {
            foreach (UndertaleRoom.SpriteInstance roomSprite in roomSprites)
            {
                if (roomSprite.Sprite is null)
                    continue;
                if (!(roomSprite.FrameIndex >= 0 && roomSprite.FrameIndex < roomSprite.Sprite.Textures.Count))
                    continue;

                UndertaleTexturePageItem texture = roomSprite.Sprite.Textures[(int)roomSprite.FrameIndex].Texture;

                RoomItems.Add(new(
                    Object: roomSprite,
                    Layer: layer,
                    Selectable: new(
                        Category: layer,
                        Bounds: new Rect(
                            layer.XOffset + roomSprite.X - roomSprite.Sprite.OriginX * roomSprite.ScaleX,
                            layer.YOffset + roomSprite.Y - roomSprite.Sprite.OriginY * roomSprite.ScaleY,
                            texture.BoundingWidth * roomSprite.ScaleX,
                            texture.BoundingHeight * roomSprite.ScaleY
                        ).Normalize(),
                        Rotation: roomSprite.OppositeRotation,
                        Pivot: new Point(layer.XOffset + roomSprite.X, layer.YOffset + roomSprite.Y),
                        GetProperties: () =>
                        {
                            return new(roomSprite.X, roomSprite.Y);
                        },
                        SetProperties: (properties) =>
                        {
                            roomSprite.X = properties.X;
                            roomSprite.Y = properties.Y;
                        }
                    )
                ));
            }
        }

        void UpdateGameObjects(IList<UndertaleRoom.GameObject> roomGameObjects, UndertaleRoom.Layer? layer = null)
        {
            foreach (UndertaleRoom.GameObject roomGameObject in roomGameObjects)
            {
                UndertaleGameObject? gameObject = roomGameObject.ObjectDefinition;
                if (gameObject is null ||
                    gameObject.Sprite is null ||
                    !(roomGameObject.ImageIndex >= 0 && roomGameObject.ImageIndex < gameObject.Sprite.Textures.Count))
                    continue;

                UndertaleTexturePageItem texture = gameObject.Sprite.Textures[roomGameObject.ImageIndex].Texture;

                RoomItems.Add(new(
                    Object: roomGameObject,
                    Selectable: new(
                        Category: layer is not null ? layer : "GameObjects",
                        Bounds: new Rect(
                            roomGameObject.X - gameObject.Sprite.OriginX * roomGameObject.ScaleX,
                            roomGameObject.Y - gameObject.Sprite.OriginY * roomGameObject.ScaleY,
                            texture.BoundingWidth * roomGameObject.ScaleX,
                            texture.BoundingHeight * roomGameObject.ScaleY
                        ).Normalize(),
                        Rotation: roomGameObject.OppositeRotation,
                        Pivot: new Point(
                            roomGameObject.X,
                            roomGameObject.Y),
                        GetProperties: () =>
                        {
                            return new(roomGameObject.X, roomGameObject.Y);
                        },
                        SetProperties: (properties) =>
                        {
                            roomGameObject.X = properties.X;
                            roomGameObject.Y = properties.Y;
                        }
                    )
                ));
            }
        }
    }

    public class Renderer
    {
        public UndertaleRoom Room = null!;
        public List<RoomItem> RoomItems = null!;
        public SKCanvas Canvas = null!;

        // Used to keep the images alive while room is open
        List<SKImage> usedImages = [];
        List<SKImage> currentUsedImages = [];

        readonly List<SKPoint> vertices = [];
        readonly List<SKPoint> texs = [];

        readonly MainViewModel mainVM = App.Services.GetRequiredService<MainViewModel>();

        public void RenderRoom()
        {
            RenderBackgroundColor();

            foreach (RoomItem roomItem in RoomItems)
            {
                switch (roomItem.Object)
                {
                    case UndertaleRoom.Background roomBackground:
                        RenderBackground(roomBackground);
                        break;
                    case UndertaleRoom.GameObject roomGameObject:
                        RenderGameObject(roomGameObject);
                        break;
                    case UndertaleRoom.Tile roomTile:
                        RenderTile(roomTile, roomItem.Layer);
                        break;
                    case UndertaleRoom.SpriteInstance roomSprite:
                        RenderSprite(roomSprite, roomItem.Layer!);
                        break;
                    // layer.AssetsData.Sequences
                    // layer.AssetsData.NineSlices
                    // layer.AssetsData.ParticleSystems
                    // layer.AssetsData.TextItems
                    case UndertaleRoom.Layer { LayerType: UndertaleRoom.LayerType.Background } roomLayerBackground:
                        RenderLayerBackground(roomLayerBackground);
                        break;
                    case UndertaleRoom.Layer { LayerType: UndertaleRoom.LayerType.Tiles } roomLayerTiles:
                        RenderLayerTiles(roomLayerTiles);
                        break;
                }
            }

            (currentUsedImages, usedImages) = (usedImages, currentUsedImages);
            currentUsedImages.Clear();
        }

        void RenderBackgroundColor()
        {
            if (Room.Flags.HasFlag(UndertaleRoom.RoomEntryFlags.IsGMS2) || Room.Flags.HasFlag(UndertaleRoom.RoomEntryFlags.IsGM2024_13))
            {
            }
            else
            {
                // Fill room background color
                Color color = UndertaleColor.ToColor(Room.BackgroundColor);
                Canvas.DrawRect(0, 0, Room.Width, Room.Height, new SKPaint { Color = color.ToSKColor() });
            }
        }

        void RenderBackground(UndertaleRoom.Background roomBackground)
        {
            if (!roomBackground.Enabled)
                return;

            UndertaleBackground? background = roomBackground.BackgroundDefinition;
            if (background is null)
                return;

            roomBackground.UpdateStretch();

            UndertaleTexturePageItem texture = background.Texture;
            SKImage? image = mainVM.ImageCache.GetCachedImageFromTexturePageItem(texture);
            if (image is null)
                return;

            currentUsedImages.Add(image);

            var w = texture.BoundingWidth * roomBackground.CalcScaleX;
            var h = texture.BoundingHeight * roomBackground.CalcScaleY;

            var startX = roomBackground.TiledHorizontally ? ((roomBackground.X % w) - w) : roomBackground.X;
            var startY = roomBackground.TiledVertically ? ((roomBackground.Y % h) - h) : roomBackground.Y;

            var endX = roomBackground.TiledHorizontally ? roomBackground.ParentRoom.Width : (startX + w);
            var endY = roomBackground.TiledVertically ? roomBackground.ParentRoom.Height : (startY + h);

            for (var x = startX; x < endX; x += w)
            {
                for (var y = startY; y < endY; y += h)
                {
                    Canvas.Save();
                    if (roomBackground.TiledHorizontally || roomBackground.TiledVertically)
                    {
                        // TODO: Only clip in direction of tiling
                        Canvas.ClipRect(new SKRect(0, 0, roomBackground.ParentRoom.Width, roomBackground.ParentRoom.Height));
                    }
                    Canvas.Translate(x, y);
                    Canvas.Translate(texture.TargetX, texture.TargetY);
                    Canvas.Scale(roomBackground.CalcScaleX, roomBackground.CalcScaleY);
                    Canvas.DrawImage(image, 0, 0);
                    Canvas.Restore();
                }
            }
        }

        void RenderLayerBackground(UndertaleRoom.Layer layer)
        {
            UndertaleRoom.Layer.LayerBackgroundData backgroundData = layer.BackgroundData;

            if (!backgroundData.Visible)
                return;

            if (backgroundData.Sprite is null)
            {
                Canvas.DrawRect(0, 0, layer.ParentRoom.Width, layer.ParentRoom.Height, new SKPaint { Color = UndertaleColor.ToColor(backgroundData.Color).ToSKColor() });
                return;
            }

            if (!(backgroundData.FirstFrame >= 0 && backgroundData.FirstFrame < backgroundData.Sprite.Textures.Count))
                return;

            backgroundData.UpdateScale();

            UndertaleTexturePageItem texture = backgroundData.Sprite.Textures[(int)backgroundData.FirstFrame].Texture;

            SKImage? image = mainVM.ImageCache.GetCachedImageFromTexturePageItem(texture);
            if (image is null)
                return;

            currentUsedImages.Add(image);

            var w = backgroundData.Sprite.Width * backgroundData.CalcScaleX;
            var h = backgroundData.Sprite.Height * backgroundData.CalcScaleY;

            var startX = backgroundData.TiledHorizontally ? ((layer.XOffset % w) - w) : layer.XOffset;
            var startY = backgroundData.TiledVertically ? ((layer.YOffset % h) - h) : layer.YOffset;

            var endX = backgroundData.TiledHorizontally ? layer.ParentRoom.Width : (startX + w);
            var endY = backgroundData.TiledVertically ? layer.ParentRoom.Height : (startY + h);

            for (var x = startX; x < endX; x += w)
            {
                for (var y = startY; y < endY; y += h)
                {
                    Canvas.Save();
                    if (backgroundData.TiledHorizontally || backgroundData.TiledVertically)
                    {
                        // TODO: Only clip in direction of tiling
                        Canvas.ClipRect(new SKRect(0, 0, layer.ParentRoom.Width, layer.ParentRoom.Height));
                    }
                    Canvas.Translate(x, y);
                    Canvas.Translate(texture.TargetX, texture.TargetY);
                    Canvas.Scale(backgroundData.CalcScaleX, backgroundData.CalcScaleY);
                    Canvas.DrawImage(image, 0, 0, new SKPaint()
                    {
                        ColorFilter = SKColorFilter.CreateBlendMode(UndertaleColor.ToColor(backgroundData.Color).ToSKColor(), SKBlendMode.Modulate),
                    });
                    Canvas.Restore();
                }
            }
        }

        void RenderTile(UndertaleRoom.Tile roomTile, UndertaleRoom.Layer? layer = null)
        {
            SKImage? image = mainVM.ImageCache.GetCachedImageFromTile(roomTile);
            if (image is null)
                return;

            currentUsedImages.Add(image);

            Canvas.Save();
            if (layer is not null)
                Canvas.Translate(layer.XOffset, layer.YOffset);

            Canvas.Translate(roomTile.X, roomTile.Y);
            Canvas.Translate(
                -Math.Min(roomTile.SourceX - roomTile.Tpag.TargetX, 0),
                -Math.Min(roomTile.SourceY - roomTile.Tpag.TargetY, 0));
            Canvas.Scale(roomTile.ScaleX, roomTile.ScaleY);
            Canvas.DrawImage(image, 0, 0);
            Canvas.Restore();
        }

        void RenderLayerTiles(UndertaleRoom.Layer layer)
        {
            UndertaleRoom.Layer.LayerTilesData tilesData = layer.TilesData;

            if (tilesData.Background is null)
                return;

            SKImage? image = mainVM.ImageCache.GetCachedImageFromGMImage(tilesData.Background.Texture.TexturePage.TextureData.Image);
            if (image is null)
                return;

            currentUsedImages.Add(image);

            UndertaleBackground background = tilesData.Background;
            UndertaleTexturePageItem texture = background.Texture;

            uint tileColumns = background.GMS2TileColumns;
            uint tileW = background.GMS2TileWidth;
            uint tileH = background.GMS2TileHeight;
            uint borderX = background.GMS2OutputBorderX;
            uint borderY = background.GMS2OutputBorderY;

            ushort targetX = texture.TargetX;
            ushort targetY = texture.TargetY;
            ushort sourceX = texture.SourceX;
            ushort sourceY = texture.SourceY;

            uint[][] tileData = tilesData.TileData;

            vertices.Clear();
            texs.Clear();

            static void AddQuad(List<SKPoint> list, float x1, float y1, float x2, float y2, uint transform)
            {
                // Flip X
                if ((transform & 1) != 0)
                    (x1, x2) = (x2, x1);

                // Flip Y
                if (((transform >> 1) & 1) != 0)
                    (y1, y2) = (y2, y1);

                SKPoint topLeft;
                SKPoint bottomLeft;
                SKPoint topRight;
                SKPoint bottomRight;

                // Rotate 90 degrees clockwise
                if (((transform >> 2) & 1) == 0)
                {
                    topLeft = new SKPoint(x1, y1);
                    bottomLeft = new SKPoint(x1, y2);
                    topRight = new SKPoint(x2, y1);
                    bottomRight = new SKPoint(x2, y2);
                }
                else
                {
                    topLeft = new SKPoint(x1, y2);
                    bottomLeft = new SKPoint(x2, y2);
                    topRight = new SKPoint(x1, y1);
                    bottomRight = new SKPoint(x2, y1);
                }

                list.Add(topLeft);
                list.Add(bottomLeft);
                list.Add(topRight);

                list.Add(topRight);
                list.Add(bottomLeft);
                list.Add(bottomRight);
            }

            for (int y = 0; y < tileData.Length; y++)
                for (int x = 0; x < tileData[y].Length; x++)
                {
                    uint tile = tileData[y][x];
                    uint tileId = tile & UndertaleRoomViewModel.TILE_ID;

                    if (tileId != 0)
                    {
                        uint tileOrientation = tile >> 28;

                        float posX = (x * tileW) + targetX;
                        float posY = (y * tileH) + targetY;

                        uint tileX = tileId % tileColumns;
                        uint tileY = tileId / tileColumns;

                        float xx = sourceX + (tileX * (tileW + borderX * 2) + borderX);
                        float yy = sourceY + (tileY * (tileH + borderY * 2) + borderY);

                        AddQuad(texs, xx, yy, xx + tileW, yy + tileH, tileOrientation);
                        AddQuad(vertices, posX, posY, posX + tileW, posY + tileH, 0);
                    }
                }

            SKShader shader = SKShader.CreateImage(image);

            Canvas.Save();
            Canvas.Translate(layer.XOffset, layer.YOffset);
            Canvas.DrawVertices(SKVertexMode.Triangles, vertices.ToArray(), texs.ToArray(), null, new SKPaint() { Shader = shader });
            Canvas.Restore();
        }

        void RenderSprite(UndertaleRoom.SpriteInstance roomSprite, UndertaleRoom.Layer layer)
        {
            // TODO: roomSprite.AnimationSpeed
            // TODO: roomSprite.AnimationSpeedType

            if (roomSprite.Sprite is null)
                return;
            if (!(roomSprite.FrameIndex >= 0 && roomSprite.FrameIndex < roomSprite.Sprite.Textures.Count))
                return;

            UndertaleTexturePageItem texture = roomSprite.Sprite.Textures[(int)roomSprite.FrameIndex].Texture;

            SKImage? image = mainVM.ImageCache.GetCachedImageFromTexturePageItem(texture);
            if (image is null)
                return;

            currentUsedImages.Add(image);

            Canvas.Save();
            Canvas.Translate(layer.XOffset + texture.TargetX, layer.YOffset + texture.TargetY);
            Canvas.Translate(roomSprite.X, roomSprite.Y);
            Canvas.RotateDegrees(roomSprite.OppositeRotation);
            Canvas.Scale(roomSprite.ScaleX, roomSprite.ScaleY);

            Canvas.DrawImage(image, -roomSprite.Sprite.OriginX, -roomSprite.Sprite.OriginY, new SKPaint()
            {
                ColorFilter = SKColorFilter.CreateBlendMode(UndertaleColor.ToColor(roomSprite.Color).ToSKColor(), SKBlendMode.Modulate),
            });

            Canvas.Restore();
        }

        void RenderGameObject(UndertaleRoom.GameObject roomGameObject)
        {
            // TODO: roomGameObject.ImageSpeed

            UndertaleGameObject? gameObject = roomGameObject.ObjectDefinition;
            if (gameObject is null)
                return;
            if (gameObject.Sprite is null)
                return;
            if (!(roomGameObject.ImageIndex >= 0 && roomGameObject.ImageIndex < gameObject.Sprite.Textures.Count))
                return;

            UndertaleTexturePageItem texture = gameObject.Sprite.Textures[roomGameObject.ImageIndex].Texture;

            SKImage? image = mainVM.ImageCache.GetCachedImageFromTexturePageItem(texture);
            if (image is null)
                return;

            currentUsedImages.Add(image);

            Canvas.Save();
            Canvas.Translate(roomGameObject.X, roomGameObject.Y);
            Canvas.RotateDegrees(roomGameObject.OppositeRotation);
            Canvas.Scale(roomGameObject.ScaleX, roomGameObject.ScaleY);

            Canvas.DrawImage(image, -gameObject.Sprite.OriginX + texture.TargetX, -gameObject.Sprite.OriginY + texture.TargetY, new SKPaint()
            {
                ColorFilter = SKColorFilter.CreateBlendMode(UndertaleColor.ToColor(roomGameObject.Color).ToSKColor(), SKBlendMode.Modulate),
            });

            Canvas.Restore();
        }
    }
}