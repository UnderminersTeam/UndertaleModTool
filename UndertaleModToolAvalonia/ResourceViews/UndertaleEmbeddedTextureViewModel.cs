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
    public UndertaleEmbeddedTexture EmbeddedTexture { get; }

    public UndertaleEmbeddedTextureViewModel(UndertaleEmbeddedTexture embeddedTexture, IServiceProvider serviceProvider)
    {
        MainVM = serviceProvider.GetRequiredService<MainViewModel>();

        EmbeddedTexture = embeddedTexture;
    }

    /// <summary>
    /// Returns the texture page item drawn at this pixel of the texture, if any.
    /// </summary>
    public UndertaleTexturePageItem? FindItemAt(double x, double y)
    {
        // Looked up every time rather than cached, since scripts or imports can add items or move them between textures.
        foreach (UndertaleTexturePageItem item in MainVM.Data!.TexturePageItems)
        {
            if (item.TexturePage != EmbeddedTexture)
                continue;

            bool isInsideX = x >= item.SourceX && x < item.SourceX + item.SourceWidth;
            bool isInsideY = y >= item.SourceY && y < item.SourceY + item.SourceHeight;
            if (isInsideX && isInsideY)
                return item;
        }
        return null;
    }

    public async void OpenItem(UndertaleTexturePageItem item, bool inNewTab)
    {
        await MainVM.Tabs.TabOpen(item, inNewTab);
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
            SuggestedFileName = $"{EmbeddedTexture.Name?.Content ?? "image"}.{type.extension}",
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
            SuggestedFileName = $"{EmbeddedTexture.Name?.Content ?? "image"}.png",
        });

        if (file is null)
            return;

        using (Stream stream = await file.OpenWriteAsync())
        {
            await ImportExport.ExportEmbeddedTextureAsPNG(EmbeddedTexture, stream);
        }
    }
}
