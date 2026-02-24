using System;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;
using UndertaleModLib.Models;
using UndertaleModLib.Util;

namespace UndertaleModToolAvalonia;

public class SKImageViewer : Control
{
    public static readonly StyledProperty<object?> ImageProperty =
        AvaloniaProperty.Register<SKImageViewer, object?>(nameof(Image));

    public object? Image
    {
        get => GetValue(ImageProperty);
        set => SetValue(ImageProperty, value);
    }

    public static readonly StyledProperty<IList<object?>> BindingsProperty =
        AvaloniaProperty.Register<SKImageViewer, IList<object?>>(nameof(Bindings));

    public IList<object?> Bindings
    {
        get => GetValue(BindingsProperty);
        set => SetValue(BindingsProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ImageProperty)
        {
            if (Image is UndertaleTexturePageItem)
            {
                // Bind these values to a property so we can get updates when they change.
                IList<IBinding> bindings =
                [
                    new Binding("Image.TexturePage.TextureData.Image")
                        {Source = this},
                    new Binding("Image.SourceX")
                        {Source = this},
                    new Binding("Image.SourceY")
                        {Source = this},
                    new Binding("Image.SourceWidth")
                        {Source = this},
                    new Binding("Image.SourceHeight")
                        {Source = this},
                    new Binding("Image.TargetX")
                        {Source = this},
                    new Binding("Image.TargetY")
                        {Source = this},
                    new Binding("Image.TargetWidth")
                        {Source = this},
                    new Binding("Image.TargetHeight")
                        {Source = this},
                    new Binding("Image.BoundingWidth")
                        {Source = this},
                    new Binding("Image.BoundingHeight")
                        {Source = this},
                ];

                MultiBinding multiBinding = new()
                {
                    Bindings = bindings,
                    Converter = new FuncMultiValueConverter<object?, IList<object?>>(x => new List<object?>(x))
                };

                Bind(BindingsProperty, multiBinding);
            }
            else
            {
                // NOTE: Unbind?
            }

            Invalidate();
        }
        else if (change.Property == BindingsProperty)
        {
            Invalidate();
        }
    }

    readonly CustomDrawOperation customDrawOperation;

    public SKImageViewer()
    {
        ClipToBounds = true;
        customDrawOperation = new CustomDrawOperation();
    }

    void Invalidate()
    {
        Size size = GetSize();
        Width = size.Width;
        Height = size.Height;

        InvalidateMeasure();
        InvalidateVisual();
    }

    Size GetSize()
    {
        if (Image is UndertaleTexturePageItem texturePageItem)
            return new Size(texturePageItem.BoundingWidth, texturePageItem.BoundingHeight);
        else if (Image is GMImage gmImage)
            return new Size(gmImage.Width, gmImage.Height);
        else if (Image is UndertaleSprite.MaskEntry maskEntry)
            return new Size(maskEntry.Width, maskEntry.Height);

        return new Size(0, 0);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        return GetSize();
    }

    public override void Render(DrawingContext context)
    {
        Size size = GetSize();
        customDrawOperation.Bounds = new Rect(0, 0, size.Width, size.Height);
        customDrawOperation.Image = Image;

        context.Custom(customDrawOperation);
    }

    public class CustomDrawOperation : ICustomDrawOperation
    {
        public Rect Bounds { get; set; }

        public object? Image;

        readonly MainViewModel mainVM = App.Services.GetRequiredService<MainViewModel>();

        public CustomDrawOperation()
        {
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

                // Checkered background
                int gridSize = 8;
                SKPaint gridColor1 = new SKPaint { Color = new SKColor(102, 102, 102) };
                SKPaint gridColor2 = new SKPaint { Color = new SKColor(153, 153, 153) };

                canvas.DrawRect(SKRect.Create(0, 0, (float)Bounds.Width, (float)Bounds.Height), gridColor1);

                for (int x = 0; x < Bounds.Width / gridSize; x++)
                    for (int y = 0; y < Bounds.Height / gridSize; y++)
                    {
                        if ((x + y) % 2 != 0)
                            canvas.DrawRect(SKRect.Create(x * gridSize, y * gridSize, gridSize, gridSize), gridColor2);
                    }

                // Image
                RenderImage(canvas);

                canvas.Restore();
            }
            catch (Exception e)
            {
                Debugger.Break();
                throw;
            }
        }

        public void RenderImage(SKCanvas canvas)
        {
            if (Image is UndertaleTexturePageItem texturePageItem)
            {
                if (texturePageItem.TexturePage is not null)
                {
                    SKImage? image = mainVM.ImageCache.GetCachedImageFromTexturePageItem(texturePageItem);

                    if (image is not null)
                    {
                        // TODO: TargetWidth/TargetHeight
                        canvas.DrawImage(image, texturePageItem.TargetX, texturePageItem.TargetY);
                    }
                }
            }
            else if (Image is GMImage gmImage)
            {
                SKImage image = mainVM.ImageCache.GetCachedImageFromGMImage(gmImage);
                canvas.DrawImage(image, 0, 0);
            }
            else if (Image is UndertaleSprite.MaskEntry maskEntry)
            {
                int size = maskEntry.Width * maskEntry.Height;
                byte[] pixels = new byte[size];

                for (int y = 0; y < maskEntry.Height; y++)
                {
                    int rowWidth = (maskEntry.Width + 7) / 8;
                    int byteRowIndex = y * rowWidth;

                    for (int x = 0; x < maskEntry.Width; x++)
                    {
                        int i = y * maskEntry.Width + x;
                        int byteIndex = byteRowIndex + (x / 8);
                        int bitIndex = x % 8;

                        pixels[i] = (maskEntry.Data[byteIndex] & (1 << (7 - bitIndex))) != 0 ? (byte)255 : (byte)0;
                    }
                }

                SKImage image = SkiaSharp.SKImage.FromPixelCopy(new SKImageInfo(maskEntry.Width, maskEntry.Height, SKColorType.Gray8), pixels);
                canvas.DrawImage(image, 0, 0);
            }
        }
    }
}