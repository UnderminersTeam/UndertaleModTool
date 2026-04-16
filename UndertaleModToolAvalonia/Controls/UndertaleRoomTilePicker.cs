using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia;

public class UndertaleRoomTilePicker : Control
{
    public static readonly StyledProperty<uint> SelectedTileDataProperty =
        AvaloniaProperty.Register<UndertaleRoomTilePicker, uint>(nameof(SelectedTileData),
            defaultBindingMode: BindingMode.TwoWay);

    public uint SelectedTileData
    {
        get => GetValue(SelectedTileDataProperty);
        set => SetValue(SelectedTileDataProperty, value);
    }

    public static readonly StyledProperty<uint> TileSetColumnsProperty =
        AvaloniaProperty.Register<UndertaleRoomTilePicker, uint>(nameof(TileSetColumns),
            defaultBindingMode: BindingMode.TwoWay);

    public uint TileSetColumns
    {
        get => GetValue(TileSetColumnsProperty);
        set => SetValue(TileSetColumnsProperty, value);
    }

    UndertaleRoom.Layer.LayerTilesData? layerTilesData;

    Vector translation;
    double scaling = 1;

    Point translationMoveOffset;

    public UndertaleRoomTilePicker()
    {
        ClipToBounds = true;
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        layerTilesData = DataContext as UndertaleRoom.Layer.LayerTilesData;
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
            SelectTileAt(pointerPoint.Position);
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
        if (layerTilesData?.Background is not null)
        {
            context.Custom(new CustomDrawOperation()
            {
                Bounds = new Rect(0, 0, Bounds.Width, Bounds.Height),
                Translation = translation,
                Scaling = scaling,
                Background = layerTilesData.Background,
                SelectedTileData = SelectedTileData,
                VisualColumns = TileSetColumns,
                SelectedColor = this.GetSolidColorBrushResource("SystemControlHighlightAccentBrush").Color.ToSKColor().WithAlpha(128),
            });
        }

        TopLevel topLevel = TopLevel.GetTopLevel(this)!;
        topLevel.RequestAnimationFrame(_ =>
        {
            InvalidateVisual();
        });
    }

    void SelectTileAt(Point point)
    {
        if (layerTilesData?.Background is null)
            return;

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

    void TranslationMoveOnPressed(Point point)
    {
        translationMoveOffset = point - translation;
    }

    void TranslationMoveOnMoved(Point point)
    {
        translation = point - translationMoveOffset;
        InvalidateVisual();
    }

    public class CustomDrawOperation : ICustomDrawOperation
    {
        readonly MainViewModel mainVM = App.Services.GetRequiredService<MainViewModel>();

        public required Vector Translation;
        public required double Scaling;
        public required UndertaleBackground Background;
        public required uint SelectedTileData;
        public required uint VisualColumns = 0;

        public required SKColor SelectedColor;

        public Rect Bounds { get; set; }

        public void Dispose() { }

        public bool Equals(ICustomDrawOperation? other) => false;

        public bool HitTest(Point p) => Bounds.Contains(p);

        public void Render(ImmediateDrawingContext context)
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

            var texturePageItem = Background.Texture;

            SKImage? image = mainVM.ImageCache.GetCachedImageFromTexturePageItem(texturePageItem);

            if (image is null)
                return;

            UndertaleTexturePageItem texture = Background.Texture;

            uint tileW = Background.GMS2TileWidth;
            uint tileH = Background.GMS2TileHeight;
            uint borderX = Background.GMS2OutputBorderX;
            uint borderY = Background.GMS2OutputBorderY;
            uint tileColumns = Background.GMS2TileColumns;
            uint tileCount = Background.GMS2TileCount;

            if (VisualColumns == 0)
                VisualColumns = tileColumns;

            ushort targetX = texture.TargetX;
            ushort targetY = texture.TargetY;
            ushort sourceX = texture.SourceX;
            ushort sourceY = texture.SourceY;

            var sx = -targetX + borderX;
            var sy = -targetY + borderY;

            uint dx = 0;
            uint dy = 0;

            var tileColumn = 0;
            var destColumn = 0;

            canvas.Save();
            canvas.Translate(Translation.ToSKPoint());
            canvas.Scale((float)Scaling);

            for (uint i = 0; i < tileCount; i++)
            {
                canvas.DrawImage(image, SKRect.Create(sx, sy, tileW, tileH), SKRect.Create(dx, dy, tileW, tileH));

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
                float s = 1 / (float)Scaling;
                SKRect rect = SKRect.Create(selectedTileX - s, selectedTileY - s, tileW + s, tileH + s);

                canvas.DrawRect(rect, new SKPaint() { Style = SKPaintStyle.Stroke, Color = SelectedColor });
            }

            canvas.Restore();
        }
    }
}
