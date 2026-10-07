using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Media;
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
            if (Image is UndertaleTexturePageItem texturePageItem)
            {
                // Bind these values to a property so we can get updates when they change.
                // Yeah, I know this sucks.
                IList<BindingBase> bindings =
                [
                    CompiledBinding.Create<UndertaleTexturePageItem, GMImage>(source: texturePageItem, expression: x => x.TexturePage.TextureData.Image),
                    CompiledBinding.Create<UndertaleTexturePageItem, ushort>(source: texturePageItem, expression: x => x.SourceX),
                    CompiledBinding.Create<UndertaleTexturePageItem, ushort>(source: texturePageItem, expression: x => x.SourceY),
                    CompiledBinding.Create<UndertaleTexturePageItem, ushort>(source: texturePageItem, expression: x => x.SourceWidth),
                    CompiledBinding.Create<UndertaleTexturePageItem, ushort>(source: texturePageItem, expression: x => x.SourceHeight),
                    CompiledBinding.Create<UndertaleTexturePageItem, ushort>(source: texturePageItem, expression: x => x.TargetX),
                    CompiledBinding.Create<UndertaleTexturePageItem, ushort>(source: texturePageItem, expression: x => x.TargetY),
                    CompiledBinding.Create<UndertaleTexturePageItem, ushort>(source: texturePageItem, expression: x => x.TargetWidth),
                    CompiledBinding.Create<UndertaleTexturePageItem, ushort>(source: texturePageItem, expression: x => x.TargetHeight),
                    CompiledBinding.Create<UndertaleTexturePageItem, ushort>(source: texturePageItem, expression: x => x.BoundingWidth),
                    CompiledBinding.Create<UndertaleTexturePageItem, ushort>(source: texturePageItem, expression: x => x.BoundingHeight),
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
                ClearValue(BindingsProperty);
                Invalidate();
            }
        }
        else if (change.Property == BindingsProperty)
        {
            // Only invalidate if not in transitional null state. If new value is actually null, it'll invalidate above in the type check.
            if (change.NewValue is not null && Image is not null)
                Invalidate();
        }
    }

    readonly MainViewModel mainVM = App.Services.GetRequiredService<MainViewModel>();
    SKImageDrawOperation? skImageDrawOperation;
    double scaling = 1;

    public SKImageViewer()
    {
        ClipToBounds = true;
    }

    void Invalidate()
    {
        SKImageDrawOperation? GetSKImageDrawOperation()
        {
            if (Image is UndertaleTexturePageItem texturePageItem)
            {
                if (texturePageItem.TexturePage is not null)
                {
                    SKImage? image = mainVM.ImageCache.GetCachedImageFromTexturePageItem(texturePageItem);

                    if (image is not null)
                    {
                        return new SKImageDrawOperation(image,
                            SKRect.Create(texturePageItem.TargetX, texturePageItem.TargetY, texturePageItem.TargetWidth, texturePageItem.TargetHeight));
                    }
                }
            }
            else if (Image is GMImage gmImage)
            {
                SKImage image = mainVM.ImageCache.GetCachedImageFromGMImage(gmImage);
                return new SKImageDrawOperation(image);
            }
            else if (Image is UndertaleSprite.MaskEntry maskEntry)
            {
                int maskSize = maskEntry.Width * maskEntry.Height;
                byte[] pixels = new byte[maskSize];

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

                SKImage image = SKImage.FromPixelCopy(new SKImageInfo(maskEntry.Width, maskEntry.Height, SKColorType.Gray8), pixels);
                return new SKImageDrawOperation(image);
            }
            return null;
        }

        Size size = GetSize();
        Width = size.Width;
        Height = size.Height;

        skImageDrawOperation = GetSKImageDrawOperation();

        InvalidateMeasure();
        InvalidateVisual();
    }

    Size GetSize()
    {
        if (Image is UndertaleTexturePageItem texturePageItem)
            return new Size(texturePageItem.BoundingWidth, texturePageItem.BoundingHeight) * scaling;
        else if (Image is GMImage gmImage)
            return new Size(gmImage.Width, gmImage.Height) * scaling;
        else if (Image is UndertaleSprite.MaskEntry maskEntry)
            return new Size(maskEntry.Width, maskEntry.Height) * scaling;

        return new Size(0, 0);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        return GetSize();
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            var pointerPosition = e.GetPosition(this);

            if (e.Delta.Y > 0)
            {
                scaling *= 2;
            }
            else if (e.Delta.Y < 0)
            {
                scaling /= 2;
            }

            Invalidate();
            e.Handled = true;
        }
    }

    public override void Render(DrawingContext context)
    {
        Size size = GetSize();

        using (context.PushTransform(Matrix.CreateScale(scaling, scaling)))
        {
            var gridSize = 8;
            var brush1 = new SolidColorBrush(new Color(255, 102, 102, 102));
            var brush2 = new SolidColorBrush(new Color(255, 153, 153, 153));

            context.DrawRectangle(brush1, null, new Rect(0, 0, size.Width / scaling, size.Height / scaling));

            for (int x = 0; x < Bounds.Width / scaling / gridSize; x++)
                for (int y = 0; y < Bounds.Height / scaling / gridSize; y++)
                {
                    if ((x + y) % 2 != 0)
                        context.DrawRectangle(brush2, null, new Rect(x * gridSize, y * gridSize, gridSize, gridSize));
                }

            if (skImageDrawOperation is not null)
                context.Custom(skImageDrawOperation);
        }
    }

    class SKImageDrawOperation : ICustomDrawOperation
    {
        bool enableRender = true;
        SKImage image;
        SKRect? dest;

        public Rect Bounds { get; }

        public SKImageDrawOperation(SKImage image, SKRect? dest = null)
        {
            this.image = image;
            this.dest = dest;

            Bounds = dest?.ToAvaloniaRect() ?? new Rect(0, 0, image.Width, image.Height);
        }

        public void Render(ImmediateDrawingContext context)
        {
            if (!enableRender)
                return;

            try
            {
                var leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
                if (leaseFeature is null)
                    return;

                using var lease = leaseFeature.Lease();
                SKCanvas canvas = lease.SkCanvas;

                if (dest is SKRect destSKRect)
                {
                    canvas.DrawImage(image, destSKRect, SKSamplingOptions.Default);
                }
                else
                {
                    canvas.DrawImage(image, 0, 0, SKSamplingOptions.Default);
                }
            }
            catch (Exception ex)
            {
                enableRender = false;
                Program.HandleException(ex);
            }
        }

        public bool Equals(ICustomDrawOperation? other) => false;
        public bool HitTest(Point p) => Bounds.Contains(p);
        public void Dispose() { }
    }
}