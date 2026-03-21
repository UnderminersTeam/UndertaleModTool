using System;
using System.IO;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia;

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
            FileTypeChoices = FilePickerFileTypes.PNG,
            DefaultExtension = ".png",
        });

        if (file is null)
            return;

        using (Stream stream = await file.OpenWriteAsync())
        {
            await ImportExport.ExportTexturePageItemAsPNG(TexturePageItem, stream, MainVM);
        }
    }
}
