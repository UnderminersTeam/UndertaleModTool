using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia;

public class LegacyTilePicker : RoomOrLegacyTilePicker
{
    public static readonly StyledProperty<UndertaleSprite?> SelectedTileSpriteProperty =
        AvaloniaProperty.Register<LegacyTilePicker, UndertaleSprite?>(nameof(SelectedTileSprite),
            defaultBindingMode: BindingMode.OneWay);

    public UndertaleSprite? SelectedTileSprite
    {
        get => GetValue(SelectedTileSpriteProperty);
        set => SetValue(SelectedTileSpriteProperty, value);
    }

    public override UndertaleTexturePageItem? GetTexturePageItem()
    {
        return SelectedTileSprite?.Textures.FirstOrDefault()?.Texture;
    }
}

public class RoomTilePicker : RoomOrLegacyTilePicker
{
    public static readonly StyledProperty<UndertaleBackground?> SelectedTileBackgroundProperty =
        AvaloniaProperty.Register<RoomTilePicker, UndertaleBackground?>(nameof(SelectedTileBackground),
            defaultBindingMode: BindingMode.OneWay);

    public UndertaleBackground? SelectedTileBackground
    {
        get => GetValue(SelectedTileBackgroundProperty);
        set => SetValue(SelectedTileBackgroundProperty, value);
    }

    public override UndertaleTexturePageItem? GetTexturePageItem()
    {
        return SelectedTileBackground?.Texture;
    }
}

public abstract class RoomOrLegacyTilePicker : TilePicker
{
    public static readonly StyledProperty<Rect?> SelectedTileSourceRectProperty =
        AvaloniaProperty.Register<RoomOrLegacyTilePicker, Rect?>(nameof(SelectedTileSourceRect),
            defaultBindingMode: BindingMode.TwoWay);

    public Rect? SelectedTileSourceRect
    {
        get => GetValue(SelectedTileSourceRectProperty);
        set => SetValue(SelectedTileSourceRectProperty, value);
    }

    public static readonly StyledProperty<uint> TileWidthProperty =
        AvaloniaProperty.Register<RoomOrLegacyTilePicker, uint>(nameof(TileWidth),
            defaultBindingMode: BindingMode.OneWay);

    public uint TileWidth
    {
        get => GetValue(TileWidthProperty);
        set => SetValue(TileWidthProperty, value);
    }

    public static readonly StyledProperty<uint> TileHeightProperty =
        AvaloniaProperty.Register<RoomOrLegacyTilePicker, uint>(nameof(TileHeight),
            defaultBindingMode: BindingMode.OneWay);

    public uint TileHeight
    {
        get => GetValue(TileHeightProperty);
        set => SetValue(TileHeightProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        if (GetTexturePageItem() is UndertaleTexturePageItem texturePageItem)
        {
            context.Custom(new CustomDrawOperation()
            {
                Bounds = new Rect(0, 0, Bounds.Width, Bounds.Height),
                Translation = translation,
                Scaling = scaling,
                SelectedColor = selectedColor,
                TexturePageItem = texturePageItem,
                SelectedTileSourceRect = SelectedTileSourceRect,
            });
        }

        base.Render(context);
    }

    public abstract UndertaleTexturePageItem? GetTexturePageItem();

    public override void SelectTileAt(Point point)
    {
        UndertaleTexturePageItem? texturePageItem = GetTexturePageItem();
        if (texturePageItem is null)
            return;

        point -= translation;
        point /= scaling;

        double x = Math.Floor(point.X / TileWidth) * TileWidth;
        double y = Math.Floor(point.Y / TileHeight) * TileHeight;

        if (x < 0 || y < 0 || x + TileWidth > texturePageItem.BoundingWidth || y + TileHeight > texturePageItem.BoundingHeight)
            return;

        SelectedTileSourceRect = new(x, y, TileWidth, TileHeight);
    }

    public override void SelectTileTo(Point point)
    {
        UndertaleTexturePageItem? texturePageItem = GetTexturePageItem();
        if (texturePageItem is null)
            return;

        point -= translation;
        point /= scaling;

        double x = Math.Floor(point.X / TileWidth) * TileWidth;
        double y = Math.Floor(point.Y / TileHeight) * TileHeight;

        if (x < 0 || y < 0 || x + TileWidth > texturePageItem.BoundingWidth || y + TileHeight > texturePageItem.BoundingHeight)
            return;

        if (SelectedTileSourceRect is Rect rect)
        {
            double rectLeft = (x < rect.X) ? x : rect.Left;
            double rectTop = (y < rect.Y) ? y : rect.Top;
            double rectRight = (x >= rect.Right) ? (x + TileWidth) : rect.Right;
            double rectBottom = (y >= rect.Bottom) ? (y + TileHeight) : rect.Bottom;

            SelectedTileSourceRect = new(rectLeft, rectTop, rectRight - rectLeft, rectBottom - rectTop);
        }
        else
        {
            SelectedTileSourceRect = new(x, y, TileWidth, TileHeight);
        }
    }

    public new class CustomDrawOperation : TilePicker.CustomDrawOperation
    {
        public required Rect? SelectedTileSourceRect;

        public override void DrawTiles(SKCanvas canvas, SKImage image)
        {
            if (TexturePageItem is null)
                return;

            canvas.DrawImage(image, SKRect.Create(TexturePageItem.TargetX, TexturePageItem.TargetY, TexturePageItem.TargetWidth, TexturePageItem.TargetHeight), SKSamplingOptions.Default);

            selectedTileRect = SelectedTileSourceRect?.ToSKRect();
        }
    }
}

public class LayerTilePicker : TilePicker
{
    public static readonly StyledProperty<uint> SelectedTileDataProperty =
        AvaloniaProperty.Register<LayerTilePicker, uint>(nameof(SelectedTileData),
            defaultBindingMode: BindingMode.TwoWay);

    public uint SelectedTileData
    {
        get => GetValue(SelectedTileDataProperty);
        set => SetValue(SelectedTileDataProperty, value);
    }

    public static readonly StyledProperty<uint> TileSetColumnsProperty =
        AvaloniaProperty.Register<LayerTilePicker, uint>(nameof(TileSetColumns),
            defaultBindingMode: BindingMode.TwoWay, defaultValue: 0);

    public uint TileSetColumns
    {
        get => GetValue(TileSetColumnsProperty);
        set => SetValue(TileSetColumnsProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        if (DataContext is UndertaleRoom.Layer.LayerTilesData layerTilesData
            && layerTilesData.Background is UndertaleBackground background
            && background.Texture is UndertaleTexturePageItem texturePageItem)
        {
            context.Custom(new CustomDrawOperation()
            {
                Bounds = new Rect(0, 0, Bounds.Width, Bounds.Height),
                Translation = translation,
                Scaling = scaling,
                SelectedColor = selectedColor,
                TexturePageItem = texturePageItem,
                Background = background,
                SelectedTileData = SelectedTileData,
                VisualColumns = TileSetColumns,
            });
        }

        base.Render(context);
    }

    public override void SelectTileAt(Point point)
    {
        if (DataContext is UndertaleRoom.Layer.LayerTilesData layerTilesData)
        {
            if (layerTilesData?.Background?.Texture is not null)
            {
                UndertaleBackground background = layerTilesData.Background;

                point -= translation;
                point /= scaling;

                uint x = (uint)(point.X / background.GMS2TileWidth);
                uint y = (uint)(point.Y / background.GMS2TileHeight);

                uint visualColumns = TileSetColumns != 0 ? TileSetColumns : background.GMS2TileColumns;

                uint id = x + (y * visualColumns);

                if (x >= visualColumns)
                    return;
                if (id >= background.GMS2TileCount)
                    return;

                SelectedTileData = id;
            }
        }
    }

    public override void SelectTileTo(Point point) => SelectTileAt(point); // TODO: Multiple tile select

    public new class CustomDrawOperation : TilePicker.CustomDrawOperation
    {
        public required uint SelectedTileData;
        public required uint VisualColumns = 0;
        public required UndertaleBackground Background;

        public override void DrawTiles(SKCanvas canvas, SKImage image)
        {
            uint tileW = Background.GMS2TileWidth;
            uint tileH = Background.GMS2TileHeight;
            uint borderX = Background.GMS2OutputBorderX;
            uint borderY = Background.GMS2OutputBorderY;
            uint tileColumns = Background.GMS2TileColumns;
            uint tileCount = Background.GMS2TileCount;

            ushort targetX = TexturePageItem.TargetX;
            ushort targetY = TexturePageItem.TargetY;
            ushort sourceX = TexturePageItem.SourceX;
            ushort sourceY = TexturePageItem.SourceY;

            if (VisualColumns == 0)
                VisualColumns = tileColumns;

            var sx = -targetX + borderX;
            var sy = -targetY + borderY;

            uint dx = 0;
            uint dy = 0;

            var tileColumn = 0;
            var destColumn = 0;

            for (uint i = 0; i < tileCount; i++)
            {
                canvas.DrawImage(image, SKRect.Create(sx, sy, tileW, tileH), SKRect.Create(dx, dy, tileW, tileH), SKSamplingOptions.Default);

                tileColumn++;
                if (tileColumn < tileColumns)
                {
                    sx += tileW + borderX * 2;
                }
                else
                {
                    sx = -targetX + borderX;
                    sy += tileH + borderY * 2;
                    tileColumn = 0;
                }

                destColumn++;
                if (destColumn < VisualColumns)
                {
                    dx += tileW;
                }
                else
                {
                    dx = 0;
                    dy += tileH;
                    destColumn = 0;
                }
            }

            uint selectedTileId = SelectedTileData & UndertaleRoomViewModel.TILE_ID;
            float selectedTileX = (selectedTileId % VisualColumns) * tileW;
            float selectedTileY = (selectedTileId / VisualColumns) * tileH;

            if (selectedTileId < tileCount)
            {
                selectedTileRect = SKRect.Create(selectedTileX, selectedTileY, tileW, tileH);
            }
        }
    }
}

public abstract class TilePicker : Control
{
    protected Vector translation;
    protected double scaling = 1;

    protected SKColor selectedColor;

    Point translationMoveOffset;

    public TilePicker()
    {
        ClipToBounds = true;
    }

    protected override void OnInitialized()
    {
        selectedColor = this.GetSolidColorBrushResource("SystemControlHighlightAccentBrush").Color.ToSKColor().WithAlpha(128);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        var pointerPoint = e.GetCurrentPoint(this);
        if (pointerPoint.Properties.IsLeftButtonPressed)
        {
            SelectTileAt(pointerPoint.Position);
        }
        else if (pointerPoint.Properties.IsMiddleButtonPressed)
        {
            TranslationMoveOnPressed(pointerPoint.Position);
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        //
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        var pointerPoint = e.GetCurrentPoint(this);
        if (pointerPoint.Properties.IsLeftButtonPressed)
        {
            if (e.KeyModifiers.HasFlag(KeyModifiers.Alt))
            {
                SelectTileTo(pointerPoint.Position);
            }
            else
            {
                SelectTileAt(pointerPoint.Position);
            }
        }
        else if (pointerPoint.Properties.IsMiddleButtonPressed)
        {
            TranslationMoveOnMoved(pointerPoint.Position);
        }
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            var pointerPosition = e.GetPosition(this);

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

            translation = new(Math.Round(translation.X), Math.Round(translation.Y));
            e.Handled = true;
        }
    }

    public override void Render(DrawingContext context)
    {
        TopLevel topLevel = TopLevel.GetTopLevel(this)!;
        topLevel.RequestAnimationFrame(_ =>
        {
            InvalidateVisual();
        });
    }

    public abstract void SelectTileAt(Point point);
    public abstract void SelectTileTo(Point point);

    void TranslationMoveOnPressed(Point point)
    {
        translationMoveOffset = point - translation;
    }

    void TranslationMoveOnMoved(Point point)
    {
        translation = point - translationMoveOffset;
        InvalidateVisual();
    }

    public abstract class CustomDrawOperation : ICustomDrawOperation
    {
        readonly MainViewModel mainVM = App.Services.GetRequiredService<MainViewModel>();

        public required Vector Translation;
        public required double Scaling;
        public required SKColor SelectedColor;
        public required UndertaleTexturePageItem TexturePageItem;

        protected SKRect? selectedTileRect = null;

        public Rect Bounds { get; set; }

        public void Render(ImmediateDrawingContext context)
        {
            try
            {
                var leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
                if (leaseFeature is null)
                    return;

                using var lease = leaseFeature.Lease();
                SKCanvas canvas = lease.SkCanvas;

                // Checkered background

                int gridSize = 8;
                SKPaint gridColor1 = new() { Color = new SKColor(102, 102, 102) };
                SKPaint gridColor2 = new() { Color = new SKColor(153, 153, 153) };

                canvas.DrawRect(SKRect.Create(0, 0, (float)Bounds.Width, (float)Bounds.Height), gridColor1);

                for (int x = 0; x < Bounds.Width / gridSize; x++)
                    for (int y = 0; y < Bounds.Height / gridSize; y++)
                    {
                        if ((x + y) % 2 != 0)
                            canvas.DrawRect(SKRect.Create(x * gridSize, y * gridSize, gridSize, gridSize), gridColor2);
                    }

                // Tiles

                SKImage? image = mainVM.ImageCache.GetCachedImageFromTexturePageItem(TexturePageItem);

                if (image is null)
                    return;

                selectedTileRect = null;

                canvas.Save();
                canvas.Translate(Translation.ToSKPoint());
                canvas.Scale((float)Scaling);

                DrawTiles(canvas, image);

                if (selectedTileRect is SKRect rect)
                {
                    float s = 1 / (float)Scaling;
                    rect.Right -= s;
                    rect.Bottom -= s;

                    rect.Inflate(s, s);
                    canvas.DrawRect(rect, new SKPaint() { Style = SKPaintStyle.Stroke, Color = SelectedColor });

                    rect.Inflate(s, s);
                    canvas.DrawRect(rect, new SKPaint() { Style = SKPaintStyle.Stroke, Color = SelectedColor });
                }

                canvas.Restore();
            }
            catch (Exception ex)
            {
                Program.HandleException(ex);
            }
        }

        public abstract void DrawTiles(SKCanvas canvas, SKImage image);

        public bool Equals(ICustomDrawOperation? other) => false;
        public bool HitTest(Point p) => Bounds.Contains(p);
        public void Dispose() { }
    }
}
