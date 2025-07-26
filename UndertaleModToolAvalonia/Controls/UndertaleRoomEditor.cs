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

    public record RoomItem(object Object, Rect Bounds, double Rotation, Point Pivot);

    public List<RoomItem> RoomItems = [];
    public RoomItem? HoveredRoomItem;

    public Vector translation = new(0, 0);
    public double scaling = 1;

    public double CustomDrawOperationTime;

    Point mousePosition;
    bool moving = false;
    Point movingStartMousePosition = new(0, 0);

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
                        UpdateGameObjects(layer.InstancesData.Instances);
                        break;
                    case UndertaleRoom.LayerType.Assets:
                        UpdateTiles(layer.AssetsData.LegacyTiles, layer.XOffset, layer.YOffset);
                        UpdateSprites(layer.AssetsData.Sprites, layer.XOffset, layer.YOffset);
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

    void UpdateTiles(IList<UndertaleRoom.Tile> roomTiles, float xOffset=0, float yOffset=0)
    {
        var orderedRoomTiles = roomTiles.OrderByDescending(x => x.TileDepth);
        foreach (UndertaleRoom.Tile roomTile in orderedRoomTiles)
        {
            var x = xOffset + roomTile.X;
            var y = yOffset + roomTile.Y;
            var w = roomTile.Width * roomTile.ScaleX;
            var h = roomTile.Height * roomTile.ScaleY;

            RoomItems.Add(new RoomItem(
                Object: roomTile,
                Bounds: new Rect(x, y, w, h),
                Rotation: 0,
                Pivot: new Point(x, y)));
        }
    }

    void UpdateSprites(IList<UndertaleRoom.SpriteInstance> roomSprites, float xOffset = 0, float yOffset = 0)
    {
        
    }

    void UpdateGameObjects(IList<UndertaleRoom.GameObject> roomGameObjects)
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
                Bounds: new Rect(
                    roomGameObject.X - gameObject.Sprite.OriginX * roomGameObject.ScaleX,
                    roomGameObject.Y - gameObject.Sprite.OriginY * roomGameObject.ScaleY,
                    texture.BoundingWidth * roomGameObject.ScaleX,
                    texture.BoundingHeight * roomGameObject.ScaleY
                ),
                Rotation: roomGameObject.OppositeRotation,
                Pivot: new Point(
                    roomGameObject.X,
                    roomGameObject.Y)));
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
            Point roomMousePosition = ((mousePosition - translation) / scaling);

            context.DrawText(new FormattedText(
                $"mouse: ({mousePosition.X}, {mousePosition.Y}), room: ({Math.Floor(roomMousePosition.X)}, {Math.Floor(roomMousePosition.Y)})\n" +
                $"view: ({-translation.X}, {-translation.Y}, {-translation.X + Bounds.Width}, {-translation.Y + Bounds.Height}), zoom: {scaling}x\n" +
                $"{vm?.Room.Name.Content} ({vm?.Room.Width}, {vm?.Room.Height})\n" +
                $"custom render time: <{CustomDrawOperationTime} ms",
                CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, 12, new SolidColorBrush(Colors.White)),
                new Point(0, 0));
        }

        Dispatcher.UIThread.InvokeAsync(InvalidateVisual, DispatcherPriority.Background);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        mousePosition = e.GetPosition(this);

        if (moving)
        {
            translation = mousePosition - movingStartMousePosition;
        }

        // Find object below cursor
        static bool RectContainsPoint(Rect rect, double rotation, Point pivot, Point point)
        {
            // TODO: Use matrices
            double rotationRadians = rotation * (Math.PI / 180);
            var sin = Math.Sin(-rotationRadians);
            var cos = Math.Cos(-rotationRadians);

            var newPoint = point - pivot;
            newPoint = new Point(newPoint.X * cos - newPoint.Y * sin, newPoint.X * sin + newPoint.Y * cos);
            newPoint = newPoint + pivot;

            return newPoint.X >= rect.Left && newPoint.X <= rect.Right && newPoint.Y >= rect.Top && newPoint.Y <= rect.Bottom;
        }

        HoveredRoomItem = null;

        var roomMousePosition = (mousePosition - translation) / scaling;
        foreach (RoomItem roomItem in RoomItems.Reverse<RoomItem>())
        {
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
            movingStartMousePosition = mousePosition - translation;
            return;
        }

        if (HoveredRoomItem is not null)
        {
            vm!.RoomItemsSelectedItem = HoveredRoomItem.Object;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        moving = false;
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        if (e.Delta.Y > 0)
        {
            translation *= 2;
            translation -= mousePosition;
            scaling *= 2;
        }
        else if (e.Delta.Y < 0)
        {
            translation += mousePosition;
            translation /= 2;
            scaling /= 2;
        }
        translation = new Vector(Math.Round(translation.X), Math.Round(translation.Y));
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Space)
        {
            moving = true;
            movingStartMousePosition = mousePosition - translation;
        }
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        if (e.Key == Key.Space)
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

        MainViewModel mainVM = App.Services.GetRequiredService<MainViewModel>();

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
                canvas.DrawRect((float)editor.translation.X - 1,
                    (float)editor.translation.Y - 1,
                    (float)Math.Ceiling(vm.Room.Width * editor.scaling + 1),
                    (float)Math.Ceiling(vm.Room.Height * editor.scaling + 1),
                    new SKPaint { Color = SKColors.White, Style = SKPaintStyle.Stroke });

                // Transform
                canvas.Translate((float)editor.translation.X, (float)editor.translation.Y);
                canvas.Scale((float)editor.scaling);

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

                object? roomSelectedItem = editor.vm?.RoomItemsSelectedItem;
                if (roomSelectedItem is not null)
                {
                    var roomItems = editor.RoomItems;
                    var foundRoomItem = roomItems.Find(x => x.Object == roomSelectedItem);
                    if (foundRoomItem is not null)
                    {
                        SKRect rect = foundRoomItem.Bounds.ToSKRect();

                        canvas.Save();
                        canvas.RotateDegrees((float)foundRoomItem.Rotation,
                            (float)(foundRoomItem.Pivot.X),
                            (float)(foundRoomItem.Pivot.Y));
                        canvas.DrawRect(rect, new SKPaint { Color = SKColors.Blue.WithAlpha(128), StrokeWidth = 2, Style = SKPaintStyle.Stroke });
                        canvas.Restore();
                    }
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
            var orderedRoomTiles = roomTiles.OrderByDescending(x => x.TileDepth);
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
                for (var x = 0; x < tilesData.TileData[y].Length; x++)
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
            foreach (UndertaleRoom.SpriteInstance roomSprite in roomSprites)
            {
                if (roomSprite.Sprite is null)
                    continue;
                if (!(roomSprite.FrameIndex >= 0 && roomSprite.FrameIndex < roomSprite.Sprite.Textures.Count))
                    continue;

                UndertaleTexturePageItem texture = roomSprite.Sprite.Textures[(int)roomSprite.FrameIndex].Texture;

                SKImage image = mainVM.ImageCache.GetCachedImageFromTexturePageItem(texture);
                currentUsedImages.Add(image);

                // roomSprite.AnimationSpeed
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

                // TODO: all other properties
            }
        }
    }
}