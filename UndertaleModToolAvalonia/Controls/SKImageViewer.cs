using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;
using UndertaleModLib.Models;
using UndertaleModToolAvalonia.Views;

namespace UndertaleModToolAvalonia.Controls;

public class SKImageViewer : Control
{
    public static readonly StyledProperty<object?> SKImageProperty =
        AvaloniaProperty.Register<SKImageViewer, object?>(nameof(SKImage));

    public object? SKImage
    {
        get => GetValue(SKImageProperty);
        set => SetValue(SKImageProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SKImageProperty)
        {
            if (change.NewValue is UndertaleTexturePageItem texturePageItem)
            {
                TexturePageItem = texturePageItem;
            }
            else
            {
                TexturePageItem = null;
            }

            InvalidateMeasure();
            InvalidateVisual();
        }
    }

    readonly CustomDrawOperation customDrawOperation;

    public UndertaleTexturePageItem? TexturePageItem;

    public SKImageViewer()
    {
        customDrawOperation = new CustomDrawOperation(this);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (TexturePageItem is not null)
            return new Size(TexturePageItem.BoundingWidth, TexturePageItem.BoundingHeight);

        return new Size(0, 0);
    }

    public override void Render(DrawingContext context)
    {
        customDrawOperation.Bounds = new Rect(0, 0, Bounds.Width, Bounds.Height);

        context.Custom(customDrawOperation);
    }

    class CustomDrawOperation : ICustomDrawOperation
    {
        public Rect Bounds { get; set; }

        public SKImageViewer skImageViewer;

        public CustomDrawOperation(SKImageViewer skImageViewer)
        {
            this.skImageViewer = skImageViewer;
        }

        public void Dispose() { }

        public bool Equals(ICustomDrawOperation? other) => false;

        public bool HitTest(Point p) => Bounds.Contains(p);

        public void Render(ImmediateDrawingContext context)
        {
            try
            {
                var leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
                if (leaseFeature is null)
                    return;

                using var lease = leaseFeature.Lease();
                SKCanvas canvas = lease.SkCanvas;
                canvas.Save();

                if (skImageViewer.TexturePageItem is not null)
                {
                    // TODO: Checkerboard background
                    canvas.DrawRect(SKRect.Create(0, 0, skImageViewer.TexturePageItem.BoundingWidth, skImageViewer.TexturePageItem.BoundingHeight), new SKPaint { Color = SKColors.Gray });

                    var image = App.Services.GetRequiredService<MainViewModel>().ImageCache.GetCachedImageFromTexturePageItem(skImageViewer.TexturePageItem);

                    // TODO: TargetWidth/TargetHeight
                    canvas.DrawImage(image, skImageViewer.TexturePageItem.TargetX, skImageViewer.TexturePageItem.TargetY);
                }

                canvas.Restore();
            }
            catch (Exception)
            {
                Debugger.Break();
                throw;
            }
        }
    }
}

public class UndertaleTexturePageItemUpdaterConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        // Ignore other values, they're just for binding updates.
        if (values[0] is UndertaleTexturePageItem texture)
        {
            return texture;
        }
        return null;
    }
}