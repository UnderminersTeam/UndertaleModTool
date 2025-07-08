using System;
using System.IO;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;
using UndertaleModLib;
using UndertaleModLib.Models;
using UndertaleModToolAvalonia.Controls;

namespace UndertaleModToolAvalonia.Views;

public partial class UndertaleEmbeddedTextureViewModel : IUndertaleResourceViewModel
{
    public MainViewModel MainVM;
    public UndertaleResource Resource => EmbeddedTexture;
    public UndertaleEmbeddedTexture EmbeddedTexture { get; set; }

    public UndertaleEmbeddedTextureViewModel(UndertaleEmbeddedTexture embeddedTexture, IServiceProvider? serviceProvider = null)
    {
        MainVM = (serviceProvider ?? App.Services).GetRequiredService<MainViewModel>();

        EmbeddedTexture = embeddedTexture;
    }

    public async void SaveImage()
    {
        IStorageFile? file = await MainVM.SaveFileDialog!(new FilePickerSaveOptions()
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

        var bitmap = new SKBitmap(EmbeddedTexture.TextureData.Image.Width, EmbeddedTexture.TextureData.Image.Height, SKColorType.Bgra8888, SKAlphaType.Unpremul);
        var canvas = new SKCanvas(bitmap);

        var op = new SKImageViewer.CustomDrawOperation();
        op.SKImage = EmbeddedTexture.TextureData.Image;
        op.RenderImage(canvas);

        var result = bitmap.Encode(stream, SKEncodedImageFormat.Png, 100);
        if (!result)
            throw new InvalidOperationException();
    }
}
