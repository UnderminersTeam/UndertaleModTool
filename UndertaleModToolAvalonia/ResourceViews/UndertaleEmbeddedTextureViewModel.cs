using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
using UndertaleModLib;
using UndertaleModLib.Models;
using UndertaleModLib.Util;

namespace UndertaleModToolAvalonia;

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

    public async void ImportImage()
    {
        // TODO: Allow formats other than PNG, either directly or to convert it
        IReadOnlyList<IStorageFile> files = await MainVM.View!.OpenFileDialog(new FilePickerOpenOptions
        {
            Title = "Import image",
            FileTypeFilter = FilePickerFileTypes.Image,
        });

        if (files.Count != 1)
            return;

        using (Stream stream = await files[0].OpenReadAsync())
        {
            await ImportExport.ImportEmbeddedTexture(EmbeddedTexture, stream);
        }
    }

    public async void ExportImage()
    {
        (IReadOnlyList<FilePickerFileType> filePickerFileTypeList, string extension) type = EmbeddedTexture.TextureData.Image.Format switch
        {
            GMImage.ImageFormat.Png => (FilePickerFileTypes.PNG, "png"),
            GMImage.ImageFormat.Qoi => (FilePickerFileTypes.QOI, "qoi"),
            GMImage.ImageFormat.Bz2Qoi => (FilePickerFileTypes.BZ2, "bz2"),
            _ => (FilePickerFileTypes.BIN, "bin"),
        };

        IStorageFile? file = await MainVM.View!.SaveFileDialog(new FilePickerSaveOptions()
        {
            Title = "Export image",
            FileTypeChoices = type.filePickerFileTypeList,
            DefaultExtension = $"*.{type.extension}",
            SuggestedFileName = $"{EmbeddedTexture.Name.Content}.{type.extension}",
        });

        if (file is null)
            return;

        using (Stream stream = await file.OpenWriteAsync())
        {
            await ImportExport.ExportEmbeddedTexture(EmbeddedTexture, stream);
        }
    }

    public async void ExportImageAsPNG()
    {
        IStorageFile? file = await MainVM.View!.SaveFileDialog(new FilePickerSaveOptions()
        {
            Title = "Export image as PNG",
            FileTypeChoices = FilePickerFileTypes.PNG,
            DefaultExtension = ".png",
            SuggestedFileName = $"{EmbeddedTexture.Name.Content}.png",
        });

        if (file is null)
            return;

        using (Stream stream = await file.OpenWriteAsync())
        {
            await ImportExport.ExportEmbeddedTextureAsPNG(EmbeddedTexture, stream);
        }
    }
}
