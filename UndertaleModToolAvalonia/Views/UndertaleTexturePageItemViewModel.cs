using System;
using System.IO;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;
using UndertaleModLib;
using UndertaleModLib.Models;
using UndertaleModToolAvalonia.Controls;

namespace UndertaleModToolAvalonia.Views;

public partial class UndertaleTexturePageItemViewModel : IUndertaleResourceViewModel
{
    public MainViewModel MainVM;
    public UndertaleResource Resource => TexturePageItem;
    public UndertaleTexturePageItem TexturePageItem { get; set; }

    public UndertaleTexturePageItemViewModel(UndertaleTexturePageItem texturePageItem, IServiceProvider? serviceProvider = null)
    {
        MainVM = (serviceProvider ?? App.Services).GetRequiredService<MainViewModel>();

        TexturePageItem = texturePageItem;
    }

    public async void SaveImage()
    {
        IStorageFile? file = await MainVM.View!.SaveFileDialog(new FilePickerSaveOptions()
        {
            Title = "Save image",
            FileTypeChoices = [
                new FilePickerFileType("PNG files (.png)")
                {
                    Patterns = ["*.png"],
                },
                new FilePickerFileType("All files")
                {
                    Patterns = ["*"],
                },
            ],
            DefaultExtension = ".png",
        });

        if (file is null)
            return;

        using Stream stream = await file.OpenWriteAsync();

        var bitmap = new SKBitmap(TexturePageItem.BoundingWidth, TexturePageItem.BoundingHeight, SKColorType.Bgra8888, SKAlphaType.Unpremul);
        var canvas = new SKCanvas(bitmap);

        var op = new SKImageViewer.CustomDrawOperation();
        op.SKImage = TexturePageItem;
        op.RenderImage(canvas);

        var result = bitmap.Encode(stream, SKEncodedImageFormat.Png, 100);
        if (!result)
            throw new InvalidOperationException();
    }
}
