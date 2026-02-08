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

    readonly RoomRenderer rendererInstance = new();

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

    object? hoveredItem;

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
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        PointerPoint pointerPoint = e.GetCurrentPoint(this);
        InteractionMode interactionMode = GetInteractionMode();

        var roomItems = Updater.MakeRoomItems(vm!.Room);

        if (pointerPoint.Properties.IsMiddleButtonPressed)
        {
            TranslationMoveOnPressed();
        }

        if (interactionMode == InteractionMode.Items)
        {
            if (pointerPoint.Properties.IsLeftButtonPressed)
            {
                ItemMoveOnPressed(roomItems);
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

        var roomItems = Updater.MakeRoomItems(vm!.Room);

        pointerPosition = e.GetPosition(this);
        pointerPositionInRoom = (pointerPosition - translation) / scaling;

        TranslationMoveOnMoved();

        if (interactionMode == InteractionMode.Items)
        {
            if (pointerPoint.Properties.IsLeftButtonPressed)
            {
                ItemMoveOnMoved(roomItems);
                roomItems = Updater.MakeRoomItems(vm!.Room);
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
                    SetLayerTileAtPointer(tilesLayer, 0);
                }
            }
        }

        hoveredItem = null;
        hoveredTile = null;

        if (tilesLayer is not null)
        {
            hoveredTile = GetLayerTileAtPointer(tilesLayer);
        }
        else
        {
            ItemHoverOnMoved(roomItems);
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
            var roomItems = Updater.MakeRoomItems(vm!.Room);
            FocusOnSelectedItem(roomItems);
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

    void ItemHoverOnMoved(List<RoomItem> roomItems)
    {
        foreach (RoomItem roomItem in roomItems.Reverse<RoomItem>())
        {
            if (roomItem.Selectable is null)
                continue;

            if (vm!.IsSelectAnyLayerEnabled || vm!.CategorySelected is null || roomItem.Selectable.Category == vm!.CategorySelected)
                if (RectContainsPoint(roomItem.Selectable.Bounds, roomItem.Selectable.Rotation, roomItem.Selectable.Pivot, pointerPositionInRoom))
                {
                    hoveredItem = roomItem.Object;
                    break;
                }
        }
    }

    void ItemMoveOnPressed(List<RoomItem> roomItems)
    {
        RoomItem? hoveredRoomItem = GetRoomItemOfItem(roomItems, hoveredItem);

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

    void ItemMoveOnMoved(List<RoomItem> roomItems)
    {
        RoomItem? roomItem = GetSelectedRoomItem(roomItems);
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
        }
    }

    RoomItem? GetRoomItemOfItem(List<RoomItem> roomItems, object? item)
    {
        if (item is null)
            return null;
        return roomItems.Find(x => x.Object == item);
    }

    RoomItem? GetSelectedRoomItem(List<RoomItem> roomItems)
    {
        RoomItem? res = GetRoomItemOfItem(roomItems, vm?.RoomTreeItemsSelectedItem);

        if (res is not null && res.Selectable is not null)
            return res;
        return null;
    }

    void FocusOnSelectedItem(List<RoomItem> roomItems)
    {
        RoomItem? item = GetSelectedRoomItem(roomItems);
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
            $"hovered item: {hoveredItem}\n" +
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

        readonly UndertaleRoom room;
        readonly List<RoomItem> roomItems;

        readonly List<RoomRenderer.RenderCommandsBuilder.IRenderCommand> renderCommands;

        readonly uint roomWidth;
        readonly uint roomHeight;

        readonly RoomItem? selectedRoomItem;
        readonly RoomItem? hoveredRoomItem;

        readonly bool isGridEnabled;
        readonly uint gridWidth;
        readonly uint gridHeight;

        public CustomDrawOperation(UndertaleRoomEditor editor)
        {
            this.editor = editor;
            Bounds = new(0, 0, editor.Bounds.Width, editor.Bounds.Height);

            vm = editor.vm!;
            translation = editor.translation;
            scaling = editor.scaling;

            room = vm.Room;

            // TODO: Remove this
            roomItems = Updater.MakeRoomItems(room);

            renderCommands = new RoomRenderer.RenderCommandsBuilder(room).RenderCommands;

            roomWidth = room.Width;
            roomHeight = room.Height;

            selectedRoomItem = editor.GetSelectedRoomItem(roomItems);
            hoveredRoomItem = editor.GetRoomItemOfItem(roomItems, editor.hoveredItem);

            isGridEnabled = vm.IsGridEnabled;
            gridWidth = vm.GridWidth;
            gridHeight = vm.GridHeight;
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
                    (float)Math.Ceiling(roomWidth * scaling + 1),
                    (float)Math.Ceiling(roomHeight * scaling + 1),
                    new SKPaint { Color = SKColors.White, Style = SKPaintStyle.Stroke });

                // Transform
                canvas.Translate((float)translation.X, (float)translation.Y);
                canvas.Scale((float)scaling);

                editor.rendererInstance.RenderCommands(renderCommands, canvas);

                if (isGridEnabled)
                {
                    if (gridWidth * scaling >= 2)
                        for (uint x = 0; x < roomWidth; x += gridWidth)
                        {
                            canvas.DrawLine(x, 0, x, roomHeight, new SKPaint { Color = SKColors.White.WithAlpha(64), BlendMode = SKBlendMode.Difference });
                        }

                    if (gridHeight * scaling >= 2)
                        for (uint y = 0; y < roomHeight; y += gridHeight)
                        {
                            canvas.DrawLine(0, y, roomWidth, y, new SKPaint { Color = SKColors.White.WithAlpha(64), BlendMode = SKBlendMode.Difference });
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

        public static List<RoomItem> MakeRoomItems(UndertaleRoom room)
        {
            var updater = new Updater()
            {
                Room = room,
            };
            updater.Update();
            return updater.RoomItems;
        }

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
}