using System;
using System.Diagnostics;
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

    public override void Render(DrawingContext context)
    {
        if (layerTilesData?.Background is not null)
        {
            context.Custom(new CustomDrawOperation()
            {
                Bounds = new Rect(0, 0, Bounds.Width, Bounds.Height),
                Translation = translation,
                Background = layerTilesData.Background,
                SelectedTileData = SelectedTileData,
                VisualColumns = TileSetColumns,
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
        public required UndertaleBackground Background;
        public required uint SelectedTileData;
        public required uint VisualColumns = 0;

        public Rect Bounds { get; set; }

        public void Dispose() { }

        public bool Equals(ICustomDrawOperation? other) => false;

        public bool HitTest(Point p) => Bounds.Contains(p);

        public void Render(ImmediateDrawingContext context)
        {
            Debug.WriteLine(Bounds);

            var leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
            if (leaseFeature is null)
                return;

            using var lease = leaseFeature.Lease();
            SKCanvas canvas = lease.SkCanvas;

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

            canvas.Translate(Translation.ToSKPoint());

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

            uint selectedTileId = SelectedTileData & 0x0FFFFFFF;
            uint selectedTileX = (selectedTileId % VisualColumns) * tileW;
            uint selectedTileY = (selectedTileId / VisualColumns) * tileH;

            if (selectedTileId < tileCount)
            {
                canvas.DrawRect(selectedTileX, selectedTileY, tileW, tileH, new SKPaint() { Style = SKPaintStyle.Stroke, Color = SKColors.Red });
            }
        }
    }
}
