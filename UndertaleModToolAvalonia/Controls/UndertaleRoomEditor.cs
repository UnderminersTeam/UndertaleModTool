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
using UndertaleModToolAvalonia.Helpers;
using UndertaleModToolAvalonia.Views;

namespace UndertaleModToolAvalonia.Controls;

public class UndertaleRoomEditor : Control
{
    readonly CustomDrawOperation customDrawOperation;
    public UndertaleRoomViewModel? vm;

    public record RoomItemProperties(int X, int Y);
    public record RoomItem(object Object, object Category, Rect Bounds, double Rotation, Point Pivot, Func<RoomItemProperties> GetProperties, Action<RoomItemProperties> SetProperties);

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

    public void Update()
    {
        if (vm is null)
            return;

        RoomItems = [];

        if (vm.Room.Flags.HasFlag(UndertaleRoom.RoomEntryFlags.IsGMS2))
        {
            IOrderedEnumerable<UndertaleRoom.Layer> layers = vm.Room.Layers.OrderByDescending(x => x.LayerDepth);
            foreach (UndertaleRoom.Layer layer in layers)
            {
                if (!layer.IsVisible)
                    continue;

                switch (layer.LayerType)
                {
                    case UndertaleRoom.LayerType.Instances:
                        UpdateGameObjects(layer.InstancesData.Instances, layer);
                        break;
                    case UndertaleRoom.LayerType.Assets:
                        UpdateTiles(layer.AssetsData.LegacyTiles, layer);
                        UpdateSprites(layer.AssetsData.Sprites, layer);
                        break;
                    case UndertaleRoom.LayerType.Tiles:
                        //UpdateLayerTiles(layer.TilesData, layer.XOffset, layer.YOffset);
                        break;
                }
            }
        }
        else
        {
            UpdateTiles(vm.Room.Tiles);
            UpdateGameObjects(vm.Room.GameObjects);
        }
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
                Category: layer is not null ? layer : vm!.Room.Tiles,
                Bounds: new Rect(x, y, w, h),
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
            ));
        }
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

            RoomItems.Add(new RoomItem(
                Object: roomSprite,
                Category: layer,
                Bounds: new Rect(
                    layer.XOffset + roomSprite.X - roomSprite.Sprite.OriginX * roomSprite.ScaleX,
                    layer.YOffset + roomSprite.Y - roomSprite.Sprite.OriginY * roomSprite.ScaleY,
                    texture.BoundingWidth * roomSprite.ScaleX,
                    texture.BoundingHeight * roomSprite.ScaleY
                ),
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

            RoomItems.Add(new RoomItem(
                Object: roomGameObject,
                Category: layer is not null ? layer : vm!.Room.GameObjects,
                Bounds: new Rect(
                    roomGameObject.X - gameObject.Sprite.OriginX * roomGameObject.ScaleX,
                    roomGameObject.Y - gameObject.Sprite.OriginY * roomGameObject.ScaleY,
                    texture.BoundingWidth * roomGameObject.ScaleX,
                    texture.BoundingHeight * roomGameObject.ScaleY
                ),
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
            ));
        }
    }

    public RoomItem? GetSelectedRoomItem()
    {
        object? roomSelectedItem = vm?.RoomItemsSelectedItem;
        if (roomSelectedItem is not null)
        {
            return RoomItems.Find(x => x.Object == roomSelectedItem);
        }
        return null;
    }

    public void FocusOnSelectedItem()
    {
        RoomItem? item = GetSelectedRoomItem();
        if (item is not null)
        {
            Translation = new(-item.Bounds.X * Scaling + (Bounds.Width / 2), -item.Bounds.Y * Scaling + (Bounds.Height / 2));
        }
    }

    public override void Render(DrawingContext context)
    {
        if (IsEffectivelyVisible)
        {
            customDrawOperation.Bounds = Bounds;

            Update();
            context.Custom(customDrawOperation);

            // Debug text
            Point roomMousePosition = ((mousePosition - Translation) / Scaling);

            context.DrawText(new FormattedText(
                $"mouse: ({mousePosition.X}, {mousePosition.Y}), room: ({Math.Floor(roomMousePosition.X)}, {Math.Floor(roomMousePosition.Y)})\n" +
                $"view: ({-Translation.X}, {-Translation.Y}, {-Translation.X + Bounds.Width}, {-Translation.Y + Bounds.Height}), zoom: {Scaling}x\n" +
                $"{vm?.Room.Name.Content} ({vm?.Room.Width}, {vm?.Room.Height})\n" +
                $"category: {vm?.CategorySelected}\n" +
                $"custom render time: <{CustomDrawOperationTime} ms",
                CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, 12, new SolidColorBrush(Colors.White)),
                new Point(0, 0));
        }

        Dispatcher.UIThread.InvokeAsync(InvalidateVisual, DispatcherPriority.Background);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        mousePosition = e.GetPosition(this);

        Point roomMousePosition = (mousePosition - Translation) / Scaling;

        if (moving)
        {
            Translation = mousePosition - movingStartMousePosition;
        }

        if (movingItem)
        {
            RoomItem? roomItem = GetSelectedRoomItem();
            if (roomItem is null)
            {
                movingItem = false;
            }
            else
            {
                roomItem.SetProperties(new((int)(roomMousePosition.X - movingItemX), (int)(roomMousePosition.Y - movingItemY)));
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

        HoveredRoomItem = null;

        foreach (RoomItem roomItem in RoomItems.Reverse<RoomItem>())
        {
            if (vm!.CategorySelected is null || roomItem.Category == vm!.CategorySelected)
            if (RectContainsPoint(roomItem.Bounds, roomItem.Rotation, roomItem.Pivot, roomMousePosition))
            {
                HoveredRoomItem = roomItem;
                break;
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
            if (HoveredRoomItem is not null)
            {
                vm!.RoomItemsSelectedItem = HoveredRoomItem.Object;
                movingItem = true;

                Point roomMousePosition = (mousePosition - Translation) / Scaling;
                RoomItemProperties properties = HoveredRoomItem.GetProperties();

                movingItemX = roomMousePosition.X - properties.X;
                movingItemY = roomMousePosition.Y - properties.Y;
            }
            else
            {
                vm!.RoomItemsSelectedItem = null;
            }
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        moving = false;
        movingItem = false;
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

    class CustomDrawOperation : ICustomDrawOperation
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

                if (vm.Room.Flags.HasFlag(UndertaleRoom.RoomEntryFlags.IsGMS2))
                {
                    IOrderedEnumerable<UndertaleRoom.Layer> layers = vm.Room.Layers.OrderByDescending(x => x.LayerDepth);
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
                                RenderLayerBackground(canvas, layer);
                                break;
                            case UndertaleRoom.LayerType.Instances:
                                RenderGameObjects(canvas, layer.InstancesData.Instances);
                                break;
                            case UndertaleRoom.LayerType.Assets:
                                canvas.Save();
                                canvas.Translate(layer.XOffset, layer.YOffset);
                                RenderTiles(canvas, layer.AssetsData.LegacyTiles);
                                RenderSprites(canvas, layer.AssetsData.Sprites);
                                // layer.AssetsData.Sequences
                                // layer.AssetsData.NineSlices
                                // layer.AssetsData.ParticleSystems
                                // layer.AssetsData.TextItems
                                canvas.Restore();
                                break;
                            case UndertaleRoom.LayerType.Tiles:
                                canvas.Save();
                                canvas.Translate(layer.XOffset, layer.YOffset);
                                RenderLayerTiles(canvas, layer.TilesData);
                                canvas.Restore();
                                break;
                            case UndertaleRoom.LayerType.Effect:
                                // layer.EffectData
                                break;
                        }
                    }
                }
                else
                {
                    // Fill room background color
                    Color color = UndertaleColor.ToColor(vm.Room.BackgroundColor);
                    canvas.DrawRect(0, 0, vm.Room.Width, vm.Room.Height, new SKPaint { Color = color.ToSKColor() });

                    // Draw backgrounds
                    RenderBackgrounds(canvas, vm.Room.Backgrounds);

                    // Draw tiles
                    RenderTiles(canvas, vm.Room.Tiles);

                    // Draw game objects
                    RenderGameObjects(canvas, vm.Room.GameObjects);
                }

                RoomItem? selectedRoomItem = editor.GetSelectedRoomItem();
                if (selectedRoomItem is not null)
                {
                    SKRect rect = selectedRoomItem.Bounds.ToSKRect();

                    canvas.Save();
                    canvas.RotateDegrees((float)selectedRoomItem.Rotation,
                        (float)(selectedRoomItem.Pivot.X),
                        (float)(selectedRoomItem.Pivot.Y));
                    canvas.DrawRect(rect, new SKPaint { Color = SKColors.Blue.WithAlpha(128), StrokeWidth = 2, Style = SKPaintStyle.Stroke });
                    canvas.Restore();
                }

                RoomItem? hoveredRoomItem = editor.HoveredRoomItem;
                if (hoveredRoomItem is not null)
                {
                    SKRect rect = hoveredRoomItem.Bounds.ToSKRect();

                    canvas.Save();
                    canvas.RotateDegrees((float)hoveredRoomItem.Rotation,
                            (float)(hoveredRoomItem.Pivot.X),
                            (float)(hoveredRoomItem.Pivot.Y));
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

        void RenderBackgrounds(SKCanvas canvas, IList<UndertaleRoom.Background> roomBackgrounds)
        {
            // TODO: roomBackground.Foreground;
            // TODO: roomBackground.TiledHorizontally;
            // TODO: roomBackground.TiledVertically;
            foreach (UndertaleRoom.Background roomBackground in roomBackgrounds)
            {
                if (!roomBackground.Enabled)
                    continue;

                UndertaleBackground? background = roomBackground.BackgroundDefinition;
                if (background is null)
                    continue;

                roomBackground.UpdateStretch();

                UndertaleTexturePageItem texture = background.Texture;
                SKImage image = mainVM.ImageCache.GetCachedImageFromTexturePageItem(texture);
                currentUsedImages.Add(image);

                canvas.Save();
                canvas.Translate(roomBackground.X, roomBackground.Y);
                canvas.Translate(texture.TargetX, texture.TargetY);
                canvas.Scale(roomBackground.CalcScaleX, roomBackground.CalcScaleY);
                canvas.DrawImage(image, 0, 0);
                canvas.Restore();
            }
        }

        void RenderLayerBackground(SKCanvas canvas, UndertaleRoom.Layer layer)
        {
            UndertaleRoom.Layer.LayerBackgroundData backgroundData = layer.BackgroundData;

            // TODO: backgroundData.Foreground
            // TODO: backgroundData.TiledHorizontally;
            // TODO: backgroundData.TiledVertically;
            if (!backgroundData.Visible)
                return;

            canvas.DrawRect(0, 0, layer.ParentRoom.Width, layer.ParentRoom.Height, new SKPaint { Color = UndertaleColor.ToColor(backgroundData.Color).ToSKColor() });

            if (backgroundData.Sprite is null)
                return;
            if (!(backgroundData.FirstFrame >= 0 && backgroundData.FirstFrame < backgroundData.Sprite.Textures.Count))
                return;

            backgroundData.UpdateScale();

            UndertaleTexturePageItem texture = backgroundData.Sprite.Textures[(int)backgroundData.FirstFrame].Texture;

            SKImage image = mainVM.ImageCache.GetCachedImageFromTexturePageItem(texture);
            currentUsedImages.Add(image);

            canvas.Save();
            canvas.Translate(layer.XOffset, layer.YOffset);
            canvas.Translate(texture.TargetX, texture.TargetY);
            canvas.Scale(backgroundData.CalcScaleX, backgroundData.CalcScaleY);
            canvas.DrawImage(image, -backgroundData.Sprite.OriginX, -backgroundData.Sprite.OriginY);
            canvas.Restore();
        }

        void RenderTiles(SKCanvas canvas, IList<UndertaleRoom.Tile> roomTiles)
        {
            IOrderedEnumerable<UndertaleRoom.Tile> orderedRoomTiles = roomTiles.OrderByDescending(x => x.TileDepth);
            foreach (UndertaleRoom.Tile roomTile in orderedRoomTiles)
            {
                SKImage? image = mainVM.ImageCache.GetCachedImageFromTile(roomTile);
                if (image is null)
                    continue;

                currentUsedImages.Add(image);

                canvas.Save();
                canvas.Translate(roomTile.X, roomTile.Y);
                canvas.Translate(
                    -Math.Min(roomTile.SourceX - roomTile.Tpag.TargetX, 0),
                    -Math.Min(roomTile.SourceY - roomTile.Tpag.TargetY, 0));
                canvas.Scale(roomTile.ScaleX, roomTile.ScaleY);
                canvas.DrawImage(image, 0, 0);
                canvas.Restore();
            }
        }

        void RenderLayerTiles(SKCanvas canvas, UndertaleRoom.Layer.LayerTilesData tilesData)
        {
            if (tilesData.Background is null)
                return;

            for (int y = 0; y < tilesData.TileData.Length; y++)
                for (int x = 0; x < tilesData.TileData[y].Length; x++)
                {
                    uint tile = tilesData.TileData[y][x];
                    uint tileId = tile & 0x0FFFFFFF;
                    uint tileOrientation = tile >> 28;

                    if (tileId != 0)
                    {
                        SKImage? image = mainVM.ImageCache.GetCachedImageFromLayerTile(tilesData, tileId);
                        if (image is null)
                            continue;

                        currentUsedImages.Add(image);

                        canvas.Save();
                        // TODO: tileOrientation
                        canvas.DrawImage(image,
                            (x * tilesData.Background.GMS2TileWidth) - tilesData.Background.Texture.TargetX,
                            (y * tilesData.Background.GMS2TileHeight) - tilesData.Background.Texture.TargetY);
                        canvas.Restore();
                    }
                }
        }

        void RenderSprites(SKCanvas canvas, IList<UndertaleRoom.SpriteInstance> roomSprites)
        {
            // TODO: roomSprite.Color
            // TODO: roomSprite.AnimationSpeed
            // TODO: roomSprite.AnimationSpeedType

            foreach (UndertaleRoom.SpriteInstance roomSprite in roomSprites)
            {
                if (roomSprite.Sprite is null)
                    continue;
                if (!(roomSprite.FrameIndex >= 0 && roomSprite.FrameIndex < roomSprite.Sprite.Textures.Count))
                    continue;

                UndertaleTexturePageItem texture = roomSprite.Sprite.Textures[(int)roomSprite.FrameIndex].Texture;

                SKImage image = mainVM.ImageCache.GetCachedImageFromTexturePageItem(texture);
                currentUsedImages.Add(image);

                canvas.Save();
                canvas.Translate(texture.TargetX, texture.TargetY);
                canvas.Translate(roomSprite.X, roomSprite.Y);
                canvas.RotateDegrees(roomSprite.OppositeRotation);
                canvas.Scale(roomSprite.ScaleX, roomSprite.ScaleY);
                canvas.DrawImage(image, -roomSprite.Sprite.OriginX, -roomSprite.Sprite.OriginY);
                canvas.Restore();
            }
        }

        void RenderGameObjects(SKCanvas canvas, IList<UndertaleRoom.GameObject> roomGameObjects)
        {
            // TODO: roomGameObject.Color
            // TODO: roomGameObject.ImageSpeed

            foreach (UndertaleRoom.GameObject roomGameObject in roomGameObjects)
            {
                UndertaleGameObject? gameObject = roomGameObject.ObjectDefinition;
                if (gameObject is null)
                    continue;
                if (gameObject.Sprite is null)
                    continue;
                if (!(roomGameObject.ImageIndex >= 0 && roomGameObject.ImageIndex < gameObject.Sprite.Textures.Count))
                    continue;

                UndertaleTexturePageItem texture = gameObject.Sprite.Textures[roomGameObject.ImageIndex].Texture;

                SKImage image = mainVM.ImageCache.GetCachedImageFromTexturePageItem(texture);
                currentUsedImages.Add(image);

                canvas.Save();
                canvas.Translate(roomGameObject.X, roomGameObject.Y);
                canvas.RotateDegrees(roomGameObject.OppositeRotation);
                canvas.Scale(roomGameObject.ScaleX, roomGameObject.ScaleY);
                canvas.DrawImage(image, -gameObject.Sprite.OriginX + texture.TargetX, -gameObject.Sprite.OriginY + texture.TargetY);
                canvas.Restore();
            }
        }
    }
}