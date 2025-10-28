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
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia;

public class UndertaleRoomEditor : Control
{
    readonly CustomDrawOperation customDrawOperation;
    public UndertaleRoomViewModel? vm;

    public record RoomItemSelectable(
        object Category,
        Rect Bounds,
        double Rotation,
        Point Pivot,
        Func<RoomItemProperties> GetProperties,
        Action<RoomItemProperties> SetProperties
    );
    public record RoomItemProperties(int X, int Y);
    public record RoomItem(
        object Object,
        UndertaleRoom.Layer? Layer = null,
        RoomItemSelectable? Selectable = null
    );

    public List<RoomItem> RoomItems = [];
    public RoomItem? HoveredRoomItem;

    public Vector Translation = new(0, 0);
    public double Scaling = 1;

    public double CustomDrawOperationTime;

    private Point mousePosition;
    private bool moving = false;
    private Point movingStartMousePosition = new(0, 0);

    private bool movingItem = false;
    private double movingItemX;
    private double movingItemY;

    private bool settingTiles = false;
    private uint? hoveredTile = null;

    // Used when updating RoomItems so it's not changed when rendering
    private readonly object updateLock = new();

    public UndertaleRoomEditor()
    {
        customDrawOperation = new CustomDrawOperation(this);
        ClipToBounds = true;
        Focusable = true;
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        vm = (DataContext as UndertaleRoomViewModel)!;
        vm?.Room.SetupRoom();

        Translation = new(0, 0);
        Scaling = 1;
    }

    public override void Render(DrawingContext context)
    {
        if (IsEffectivelyVisible)
        {
            customDrawOperation.Bounds = Bounds;

            lock (updateLock)
            {
                Update();
            }

            context.Custom(customDrawOperation);

#if DEBUG
            // Debug text
            Point roomMousePosition = ((mousePosition - Translation) / Scaling);
            context.DrawText(new FormattedText(
                $"mouse: ({mousePosition.X}, {mousePosition.Y}), room: ({Math.Floor(roomMousePosition.X)}, {Math.Floor(roomMousePosition.Y)})\n" +
                $"view: ({-Translation.X}, {-Translation.Y}, {-Translation.X + Bounds.Width}, {-Translation.Y + Bounds.Height}), zoom: {Scaling}x\n" +
                $"{vm?.Room.Name.Content} ({vm?.Room.Width}, {vm?.Room.Height})\n" +
                $"category: {vm?.CategorySelected}\n" +
                $"custom render time: <{CustomDrawOperationTime} ms\n" +
                $"hovered tile: {hoveredTile}",
                CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, 12, new SolidColorBrush(Colors.White)),
                new Point(0, 0));
#endif
        }

        TopLevel topLevel = TopLevel.GetTopLevel(this)!;
        topLevel.RequestAnimationFrame(_ =>
        {
            Dispatcher.UIThread.InvokeAsync(InvalidateVisual, DispatcherPriority.Background);
        });
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        mousePosition = e.GetPosition(this);

        if (moving)
        {
            Translation = mousePosition - movingStartMousePosition;
        }

        Point roomMousePosition = (mousePosition - Translation) / Scaling;
        vm!.StatusText = $"({Math.Floor(roomMousePosition.X)}, {Math.Floor(roomMousePosition.Y)})";

        HoveredRoomItem = null;
        hoveredTile = null;

        if (vm!.RoomItemsSelectedItem is UndertaleRoom.Layer { LayerType: UndertaleRoom.LayerType.Tiles } tilesLayer)
        {
            hoveredTile = GetLayerTile(roomMousePosition, tilesLayer);
            if (settingTiles)
            {
                SetLayerTile(roomMousePosition, tilesLayer, vm.SelectedTileData);
            }
        }
        else
        {
            if (movingItem)
            {
                RoomItem? roomItem = GetSelectedRoomItem();
                if (roomItem is not null && roomItem.Selectable is not null)
                {
                    double x = (roomMousePosition.X - movingItemX);
                    double y = (roomMousePosition.Y - movingItemY);

                    if (vm.IsGridEnabled)
                    {
                        x = Math.Floor(roomMousePosition.X / vm.GridWidth) * vm.GridWidth;
                        y = Math.Floor(roomMousePosition.Y / vm.GridHeight) * vm.GridHeight;

                        x -= Math.Floor(movingItemX / vm.GridWidth) * vm.GridWidth;
                        y -= Math.Floor(movingItemY / vm.GridHeight) * vm.GridHeight;
                    }

                    roomItem.Selectable.SetProperties(new((int)x, (int)y));
                }
                else
                {
                    movingItem = false;
                }
            }

            // Find object below cursor
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

            foreach (RoomItem roomItem in RoomItems.Reverse<RoomItem>())
            {
                if (roomItem.Selectable is null)
                    continue;

                if (vm!.CategorySelected is null || roomItem.Selectable.Category == vm!.CategorySelected)
                    if (RectContainsPoint(roomItem.Selectable.Bounds, roomItem.Selectable.Rotation, roomItem.Selectable.Pivot, roomMousePosition))
                    {
                        HoveredRoomItem = roomItem;
                        break;
                    }
            }
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsMiddleButtonPressed)
        {
            this.Focus();
            moving = true;
            movingStartMousePosition = mousePosition - Translation;
        }
        else if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            Point roomMousePosition = (mousePosition - Translation) / Scaling;

            if (vm!.RoomItemsSelectedItem is UndertaleRoom.Layer { LayerType: UndertaleRoom.LayerType.Tiles } tilesLayer)
            {
                settingTiles = true;
                SetLayerTile(roomMousePosition, tilesLayer, vm.SelectedTileData);
            }
            else
            {
                if (HoveredRoomItem is not null && HoveredRoomItem.Selectable is not null)
                {
                    vm!.RoomItemsSelectedItem = HoveredRoomItem.Object;
                    movingItem = true;

                    RoomItemProperties properties = HoveredRoomItem.Selectable.GetProperties();

                    movingItemX = roomMousePosition.X - properties.X;
                    movingItemY = roomMousePosition.Y - properties.Y;
                }
                else
                {
                    vm!.RoomItemsSelectedItem = vm!.CategorySelected;
                }
            }
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        moving = false;
        movingItem = false;
        settingTiles = false;
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        if (e.Delta.Y > 0)
        {
            Translation *= 2;
            Translation -= mousePosition;
            Scaling *= 2;
        }
        else if (e.Delta.Y < 0)
        {
            Translation += mousePosition;
            Translation /= 2;
            Scaling /= 2;
        }
        Translation = new Vector(Math.Round(Translation.X), Math.Round(Translation.Y));

        vm!.Zoom = Scaling;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.PhysicalKey == PhysicalKey.Space)
        {
            moving = true;
            movingStartMousePosition = mousePosition - Translation;
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
            moving = false;
        }
    }

    public void Update()
    {
        if (vm is null)
            return;

        Scaling = vm.Zoom;

        RoomItems = [];

        if (vm.Room.Flags.HasFlag(UndertaleRoom.RoomEntryFlags.IsGMS2) || vm.Room.Flags.HasFlag(UndertaleRoom.RoomEntryFlags.IsGM2024_13))
        {
            IOrderedEnumerable<UndertaleRoom.Layer> layers = vm.Room.Layers.Reverse().OrderByDescending(x => x.LayerDepth);
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
            UpdateBackgrounds(vm.Room.Backgrounds, foregrounds: false);
            UpdateTiles(vm.Room.Tiles);
            UpdateGameObjects(vm.Room.GameObjects);
            UpdateBackgrounds(vm.Room.Backgrounds, foregrounds: true);
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
                    Category: layer is not null ? layer : vm!.RoomItems.First(x => x.Tag == "Tiles"),
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
                    Category: layer is not null ? layer : vm!.RoomItems.First(x => x.Tag == "GameObjects"),
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

    public RoomItem? GetSelectedRoomItem()
    {
        object? roomSelectedItem = vm?.RoomItemsSelectedItem;
        if (roomSelectedItem is not null)
        {
            return RoomItems.Find(x => x.Selectable is not null && x.Object == roomSelectedItem);
        }
        return null;
    }

    public void FocusOnSelectedItem()
    {
        RoomItem? item = GetSelectedRoomItem();
        if (item is not null && item.Selectable is not null)
        {
            Translation = new(-item.Selectable.Bounds.X * Scaling + (Bounds.Width / 2), -item.Selectable.Bounds.Y * Scaling + (Bounds.Height / 2));
        }
    }

    uint? GetLayerTile(Point roomMousePosition, UndertaleRoom.Layer tilesLayer)
    {
        // Find x/y position
        if (tilesLayer.TilesData.Background is null)
            return null;

        int x = (int)Math.Floor((roomMousePosition.X - tilesLayer.XOffset) / tilesLayer.TilesData.Background.GMS2TileWidth);
        int y = (int)Math.Floor((roomMousePosition.Y - tilesLayer.YOffset) / tilesLayer.TilesData.Background.GMS2TileHeight);

        if (y >= 0 && x >= 0
            && y < tilesLayer.TilesData.TileData.Length
            && x < tilesLayer.TilesData.TileData[y].Length)
        {
            return tilesLayer.TilesData.TileData[y][x];
        }
        return null;
    }

    void SetLayerTile(Point roomMousePosition, UndertaleRoom.Layer tilesLayer, uint tileData)
    {
        // Find x/y position
        int x = (int)Math.Floor((roomMousePosition.X - tilesLayer.XOffset) / tilesLayer.TilesData.Background.GMS2TileWidth);
        int y = (int)Math.Floor((roomMousePosition.Y - tilesLayer.YOffset) / tilesLayer.TilesData.Background.GMS2TileHeight);

        if (y >= 0 && x >= 0
            && y < tilesLayer.TilesData.TileData.Length
            && x < tilesLayer.TilesData.TileData[y].Length)
        {
            tilesLayer.TilesData.TileData[y][x] = tileData;
        }
    }

    public class CustomDrawOperation : ICustomDrawOperation
    {
        public Rect Bounds { get; set; }

        readonly UndertaleRoomEditor editor;

        // Used to keep the images alive while room is open
        List<SKImage> usedImages = [];
        List<SKImage> currentUsedImages = [];

        readonly MainViewModel mainVM = App.Services.GetRequiredService<MainViewModel>();

        public CustomDrawOperation(UndertaleRoomEditor editor)
        {
            this.editor = editor;
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

                UndertaleRoomViewModel? vm = editor.vm;
                if (vm is null)
                    return;

                //

                var leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
                if (leaseFeature is null)
                    return;

                using var lease = leaseFeature.Lease();
                SKCanvas canvas = lease.SkCanvas;
                canvas.Save();

                //

                // Fill background of entire control
                canvas.DrawRect(0, 0, (float)editor.Bounds.Width, (float)editor.Bounds.Height, new SKPaint { Color = SKColors.Gray });

                // Draw room outline
                canvas.DrawRect((float)editor.Translation.X - 1,
                    (float)editor.Translation.Y - 1,
                    (float)Math.Ceiling(vm.Room.Width * editor.Scaling + 1),
                    (float)Math.Ceiling(vm.Room.Height * editor.Scaling + 1),
                    new SKPaint { Color = SKColors.White, Style = SKPaintStyle.Stroke });

                // Transform
                canvas.Translate((float)editor.Translation.X, (float)editor.Translation.Y);
                canvas.Scale((float)editor.Scaling);

                if (vm.Room.Flags.HasFlag(UndertaleRoom.RoomEntryFlags.IsGMS2) || vm.Room.Flags.HasFlag(UndertaleRoom.RoomEntryFlags.IsGM2024_13))
                {
                }
                else
                {
                    // Fill room background color
                    Color color = UndertaleColor.ToColor(vm.Room.BackgroundColor);
                    canvas.DrawRect(0, 0, vm.Room.Width, vm.Room.Height, new SKPaint { Color = color.ToSKColor() });
                }

                lock (editor.updateLock)
                {
                    RenderRoom(canvas);
                }

                if (vm.IsGridEnabled)
                {
                    if (vm.GridWidth > 0)
                    for (uint x = 0; x < vm.Room.Width; x += vm.GridWidth)
                    {
                        canvas.DrawLine(x, 0, x, vm.Room.Height, new SKPaint { Color = SKColors.White.WithAlpha(64), BlendMode = SKBlendMode.Difference });
                    }

                    if (vm.GridHeight > 0)
                    for (uint y = 0; y < vm.Room.Height; y += vm.GridHeight)
                    {
                        canvas.DrawLine(0, y, vm.Room.Width, y, new SKPaint { Color = SKColors.White.WithAlpha(64), BlendMode = SKBlendMode.Difference });
                    }
                }

                RoomItem? selectedRoomItem = editor.GetSelectedRoomItem();
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

                RoomItem? hoveredRoomItem = editor.HoveredRoomItem;
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

                usedImages = currentUsedImages;
                currentUsedImages = [];

                canvas.Restore();

                stopWatch.Stop();
                editor.CustomDrawOperationTime = Math.Ceiling(stopWatch.Elapsed.TotalMilliseconds);
            }
            catch (Exception)
            {
                Debugger.Break();
                throw;
            }
        }

        public void RenderRoom(SKCanvas canvas)
        {
            foreach (RoomItem roomItem in editor.RoomItems)
            {
                switch (roomItem.Object)
                {
                    case UndertaleRoom.Background roomBackground:
                        RenderBackground(canvas, roomBackground);
                        break;
                    case UndertaleRoom.GameObject roomGameObject:
                        RenderGameObject(canvas, roomGameObject);
                        break;
                    case UndertaleRoom.Tile roomTile:
                        RenderTile(canvas, roomTile, roomItem.Layer);
                        break;
                    case UndertaleRoom.SpriteInstance roomSprite:
                        RenderSprite(canvas, roomSprite, roomItem.Layer!);
                        break;
                    // layer.AssetsData.Sequences
                    // layer.AssetsData.NineSlices
                    // layer.AssetsData.ParticleSystems
                    // layer.AssetsData.TextItems
                    case UndertaleRoom.Layer { LayerType: UndertaleRoom.LayerType.Background } roomLayerBackground:
                        RenderLayerBackground(canvas, roomLayerBackground);
                        break;
                    case UndertaleRoom.Layer { LayerType: UndertaleRoom.LayerType.Tiles } roomLayerTiles:
                        RenderLayerTiles(canvas, roomLayerTiles);
                        break;
                }
            }
        }

        void RenderBackground(SKCanvas canvas, UndertaleRoom.Background roomBackground)
        {
            if (!roomBackground.Enabled)
                return;

            UndertaleBackground? background = roomBackground.BackgroundDefinition;
            if (background is null)
                return;

            roomBackground.UpdateStretch();

            UndertaleTexturePageItem texture = background.Texture;
            SKImage image = mainVM.ImageCache.GetCachedImageFromTexturePageItem(texture);
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
                    canvas.Save();
                    if (roomBackground.TiledHorizontally || roomBackground.TiledVertically)
                    {
                        // TODO: Only clip in direction of tiling
                        canvas.ClipRect(new SKRect(0, 0, roomBackground.ParentRoom.Width, roomBackground.ParentRoom.Height));
                    }
                    canvas.Translate(x, y);
                    canvas.Translate(texture.TargetX, texture.TargetY);
                    canvas.Scale(roomBackground.CalcScaleX, roomBackground.CalcScaleY);
                    canvas.DrawImage(image, 0, 0);
                    canvas.Restore();
                }
            }
        }

        void RenderLayerBackground(SKCanvas canvas, UndertaleRoom.Layer layer)
        {
            UndertaleRoom.Layer.LayerBackgroundData backgroundData = layer.BackgroundData;

            if (!backgroundData.Visible)
                return;

            if (backgroundData.Sprite is null)
            {
                canvas.DrawRect(0, 0, layer.ParentRoom.Width, layer.ParentRoom.Height, new SKPaint { Color = UndertaleColor.ToColor(backgroundData.Color).ToSKColor() });
                return;
            }

            if (!(backgroundData.FirstFrame >= 0 && backgroundData.FirstFrame < backgroundData.Sprite.Textures.Count))
                return;

            backgroundData.UpdateScale();

            UndertaleTexturePageItem texture = backgroundData.Sprite.Textures[(int)backgroundData.FirstFrame].Texture;

            SKImage image = mainVM.ImageCache.GetCachedImageFromTexturePageItem(texture);
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
                    canvas.Save();
                    if (backgroundData.TiledHorizontally || backgroundData.TiledVertically)
                    {
                        // TODO: Only clip in direction of tiling
                        canvas.ClipRect(new SKRect(0, 0, layer.ParentRoom.Width, layer.ParentRoom.Height));
                    }
                    canvas.Translate(x, y);
                    canvas.Translate(texture.TargetX, texture.TargetY);
                    canvas.Scale(backgroundData.CalcScaleX, backgroundData.CalcScaleY);
                    canvas.DrawImage(image, 0, 0, new SKPaint()
                    {
                        ColorFilter = SKColorFilter.CreateBlendMode(UndertaleColor.ToColor(backgroundData.Color).ToSKColor(), SKBlendMode.Modulate),
                    });
                    canvas.Restore();
                }
            }
        }

        void RenderTile(SKCanvas canvas, UndertaleRoom.Tile roomTile, UndertaleRoom.Layer? layer = null)
        {
            SKImage? image = mainVM.ImageCache.GetCachedImageFromTile(roomTile);
            if (image is null)
                return;

            currentUsedImages.Add(image);

            canvas.Save();
            if (layer is not null)
                canvas.Translate(layer.XOffset, layer.YOffset);

            canvas.Translate(roomTile.X, roomTile.Y);
            canvas.Translate(
                -Math.Min(roomTile.SourceX - roomTile.Tpag.TargetX, 0),
                -Math.Min(roomTile.SourceY - roomTile.Tpag.TargetY, 0));
            canvas.Scale(roomTile.ScaleX, roomTile.ScaleY);
            canvas.DrawImage(image, 0, 0);
            canvas.Restore();
        }

        void RenderLayerTiles(SKCanvas canvas, UndertaleRoom.Layer layer)
        {
            UndertaleRoom.Layer.LayerTilesData tilesData = layer.TilesData;

            if (tilesData.Background is null)
                return;

            for (int y = 0; y < tilesData.TileData.Length; y++)
                for (int x = 0; x < tilesData.TileData[y].Length; x++)
                {
                    uint tile = tilesData.TileData[y][x];
                    uint tileId = tile & 0x0FFFFFFF;
                    uint tileOrientation = tile >> 28;

                    float scaleX = (((tileOrientation >> 0) & 1) == 0) ? 1 : -1;
                    float scaleY = (((tileOrientation >> 1) & 1) == 0) ? 1 : -1;
                    float rotate = (((tileOrientation >> 2) & 1) == 0) ? 0 : 90;

                    float posX = (x * tilesData.Background.GMS2TileWidth) + tilesData.Background.Texture.TargetX;
                    float posY = (y * tilesData.Background.GMS2TileHeight) + tilesData.Background.Texture.TargetY;
                    float centerX = posX + ((float)tilesData.Background.GMS2TileWidth / 2);
                    float centerY = posY + ((float)tilesData.Background.GMS2TileHeight / 2);

                    if (tileId != 0)
                    {
                        SKImage? image = mainVM.ImageCache.GetCachedImageFromLayerTile(tilesData, tileId);
                        if (image is null)
                            continue;

                        currentUsedImages.Add(image);

                        canvas.Save();
                        canvas.Translate(layer.XOffset, layer.YOffset);
                        canvas.RotateDegrees(rotate, centerX, centerY);
                        canvas.Scale(scaleX, scaleY, centerX, centerY);
                        canvas.DrawImage(image, posX, posY);
                        canvas.Restore();
                    }
                }
        }

        void RenderSprite(SKCanvas canvas, UndertaleRoom.SpriteInstance roomSprite, UndertaleRoom.Layer layer)
        {
            // TODO: roomSprite.AnimationSpeed
            // TODO: roomSprite.AnimationSpeedType

            if (roomSprite.Sprite is null)
                return;
            if (!(roomSprite.FrameIndex >= 0 && roomSprite.FrameIndex < roomSprite.Sprite.Textures.Count))
                return;

            UndertaleTexturePageItem texture = roomSprite.Sprite.Textures[(int)roomSprite.FrameIndex].Texture;

            SKImage image = mainVM.ImageCache.GetCachedImageFromTexturePageItem(texture);
            currentUsedImages.Add(image);

            canvas.Save();
            canvas.Translate(layer.XOffset + texture.TargetX, layer.YOffset + texture.TargetY);
            canvas.Translate(roomSprite.X, roomSprite.Y);
            canvas.RotateDegrees(roomSprite.OppositeRotation);
            canvas.Scale(roomSprite.ScaleX, roomSprite.ScaleY);

            canvas.DrawImage(image, -roomSprite.Sprite.OriginX, -roomSprite.Sprite.OriginY, new SKPaint()
            {
                ColorFilter = SKColorFilter.CreateBlendMode(UndertaleColor.ToColor(roomSprite.Color).ToSKColor(), SKBlendMode.Modulate),
            });

            canvas.Restore();
        }

        void RenderGameObject(SKCanvas canvas, UndertaleRoom.GameObject roomGameObject)
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

            SKImage image = mainVM.ImageCache.GetCachedImageFromTexturePageItem(texture);
            currentUsedImages.Add(image);

            canvas.Save();
            canvas.Translate(roomGameObject.X, roomGameObject.Y);
            canvas.RotateDegrees(roomGameObject.OppositeRotation);
            canvas.Scale(roomGameObject.ScaleX, roomGameObject.ScaleY);

            canvas.DrawImage(image, -gameObject.Sprite.OriginX + texture.TargetX, -gameObject.Sprite.OriginY + texture.TargetY, new SKPaint()
            {
                ColorFilter = SKColorFilter.CreateBlendMode(UndertaleColor.ToColor(roomGameObject.Color).ToSKColor(), SKBlendMode.Modulate),
            });

            canvas.Restore();
        }
    }
}