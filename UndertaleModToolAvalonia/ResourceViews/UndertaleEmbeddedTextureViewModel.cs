using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using UndertaleModLib;
using UndertaleModLib.Models;
using UndertaleModLib.Util;

namespace UndertaleModToolAvalonia;

public partial class UndertaleEmbeddedTextureViewModel : IUndertaleResourceViewModel
{
    public MainViewModel MainVM;
    public UndertaleResource Resource => EmbeddedTexture;
    public UndertaleEmbeddedTexture EmbeddedTexture { get; set; }

    IReadOnlyList<FilePickerFileType> imageFileTypes = [
        new FilePickerFileType("PNG files (.png)")
        {
            Patterns = ["*.png"],
        },
        new FilePickerFileType("All files")
        {
            Patterns = ["*"],
        },
    ];

    public UndertaleEmbeddedTextureViewModel(UndertaleEmbeddedTexture embeddedTexture, IServiceProvider? serviceProvider = null)
    {
        MainVM = (serviceProvider ?? App.Services).GetRequiredService<MainViewModel>();

        EmbeddedTexture = embeddedTexture;
    }

    public async void ImportImage()
    {
        // TODO: Allow formats other than PNG, either directly or to convert it
        IReadOnlyList<IStorageFile> files = await MainVM.View!.OpenFileDialog(new FilePickerOpenOptions
        {
            Title = "Import image",
            FileTypeFilter = imageFileTypes,
        });

        if (files.Count != 1)
            return;

        byte[] bytes;
        using (Stream stream = await files[0].OpenReadAsync())
        {
            bytes = new byte[stream.Length];
            await stream.ReadExactlyAsync(bytes);
        }

        var gmImage = GMImage.FromPng(bytes, verifyHeader: true);
        gmImage.ConvertToFormat(EmbeddedTexture.TextureData.Image.Format);

        EmbeddedTexture.TextureData.Image = gmImage;
        EmbeddedTexture.TextureWidth = gmImage.Width;
        EmbeddedTexture.TextureHeight = gmImage.Height;
    }

    public async void ExportImage()
    {
        string extension = EmbeddedTexture.TextureData.Image.Format switch
        {
            GMImage.ImageFormat.Png => "png",
            GMImage.ImageFormat.Qoi => "qoi",
            GMImage.ImageFormat.Bz2Qoi => "bz2",
            _ => "bin",
        };

        IStorageFile? file = await MainVM.View!.SaveFileDialog(new FilePickerSaveOptions()
        {
            Title = "Export image",
            FileTypeChoices = [
                new FilePickerFileType($"{extension.ToUpperInvariant()} files (.{extension})")
                {
                    Patterns = [$"*.{extension}"],
                },
                new FilePickerFileType("All files")
                {
                    Patterns = ["*"],
                },
            ],
            DefaultExtension = $"*.{extension}",
            SuggestedFileName = $"{EmbeddedTexture.Name.Content}.{extension}",
        });

        if (file is null)
            return;

        using Stream stream = await file.OpenWriteAsync();

        byte[] data = EmbeddedTexture.TextureData.Image.GetData();
        stream.Write(data);
    }

    public async void ExportImageAsPNG()
    {
        IStorageFile? file = await MainVM.View!.SaveFileDialog(new FilePickerSaveOptions()
        {
            Title = "Export image as PNG",
            FileTypeChoices = imageFileTypes,
            DefaultExtension = ".png",
            SuggestedFileName = $"{EmbeddedTexture.Name.Content}.png",
        });

        if (file is null)
            return;

        using Stream stream = await file.OpenWriteAsync();

        EmbeddedTexture.TextureData.Image.SavePng(stream);
    }
}
