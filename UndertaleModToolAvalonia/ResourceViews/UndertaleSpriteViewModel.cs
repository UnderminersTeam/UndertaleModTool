using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
using PropertyChanged.SourceGenerator;
using SkiaSharp;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia;

public partial class UndertaleSpriteViewModel : IUndertaleResourceViewModel
{
    public MainViewModel MainVM;
    public UndertaleResource Resource => Sprite;
    public UndertaleSprite Sprite { get; set; }

    [Notify]
    private UndertaleSprite.TextureEntry? _TexturesSelected;
    [Notify]
    private UndertaleSprite.MaskEntry? _CollisionMasksSelected;

    public UndertaleSpriteViewModel(UndertaleSprite sprite, IServiceProvider? serviceProvider = null)
    {
        MainVM = (serviceProvider ?? App.Services).GetRequiredService<MainViewModel>();
        Sprite = sprite;

        if (Sprite.Textures.Count > 0)
            TexturesSelected = Sprite.Textures[0];
        if (Sprite.CollisionMasks.Count > 0)
            CollisionMasksSelected = Sprite.CollisionMasks[0];
    }

    public void TexturesSelectedChanged(object? item)
    {
        if (item is null)
        {
            if (Sprite.Textures.Count > 0)
                TexturesSelected = Sprite.Textures[0];
            else
                TexturesSelected = null;
        }
        else
            TexturesSelected = (UndertaleSprite.TextureEntry?)item!;
    }
    public void CollisionMasksSelectedChanged(object? item)
    {
        if (item is null)
        {
            if (Sprite.CollisionMasks.Count > 0)
                CollisionMasksSelected = Sprite.CollisionMasks[0];
            else
                CollisionMasksSelected = null;
        }
        else
            CollisionMasksSelected = (UndertaleSprite.MaskEntry?)item!;
    }

    public async void ExportAllTexturesAsPNGs()
    {
        string GetFileNameOfTexture(int i) => $"{Sprite.Name.Content}_{i}.png";

        IReadOnlyList<IStorageFolder> folders = await MainVM.View!.OpenFolderDialog(new FolderPickerOpenOptions()
        {
            Title = "Export all textures into folder",
        });

        if (folders.Count != 1)
            return;

        IStorageFolder folder = folders[0];

        List<string> filesThatAlreadyExist = [];
        for (int i = 0; i < Sprite.Textures.Count; i++)
        {
            var fileName = GetFileNameOfTexture(i);
            if (await folder.GetFileAsync(fileName) is not null)
            {
                filesThatAlreadyExist.Add(fileName);
            }
        }

        if (filesThatAlreadyExist.Count > 0)
        {
            MessageWindow.Result result = await MainVM.View!.MessageDialog($"The following files already exist. Do you want to replace them?"
                + $"\n\n{string.Join("\n", filesThatAlreadyExist)}", buttons: MessageWindow.Buttons.YesCancel);

            if (result != MessageWindow.Result.Yes)
                return;
        }

        for (int i = 0; i < Sprite.Textures.Count; i++)
        {
            var fileName = GetFileNameOfTexture(i);
            var texture = Sprite.Textures[i].Texture;

            IStorageFile? file = await folder.CreateFileAsync(fileName);
            if (file is null)
            {
                await MainVM.View!.MessageDialog($"Error: Could not create file \"{fileName}\"");
                return;
            }

            using (var stream = await file.OpenWriteAsync())
            {
                await ImportExport.ExportTexturePageItemAsPNG(texture, stream, MainVM);
            }
        }
    }

    public async void ImportCollisionMaskData()
    {
        if (CollisionMasksSelected is null)
            return;

        IReadOnlyList<IStorageFile> files = await MainVM.View!.OpenFileDialog(new FilePickerOpenOptions
        {
            Title = "Import collision mask data",
            FileTypeFilter = FilePickerFileTypes.BIN,
        });

        if (files.Count != 1)
            return;

        using (Stream stream = await files[0].OpenReadAsync())
        {
            await ImportExport.ImportSpriteCollisionMaskData(Sprite, Sprite.CollisionMasks.IndexOf(CollisionMasksSelected), stream, MainVM);
        }
    }

    public async void ExportCollisionMaskData()
    {
        if (CollisionMasksSelected is null)
            return;

        IStorageFile? file = await MainVM.View!.SaveFileDialog(new FilePickerSaveOptions()
        {
            Title = "Export collision mask data",
            FileTypeChoices = FilePickerFileTypes.BIN,
            DefaultExtension = ".bin",
        });

        if (file is null)
            return;

        using (Stream stream = await file.OpenWriteAsync())
        {
            await ImportExport.ExportSpriteCollisionMaskData(Sprite, Sprite.CollisionMasks.IndexOf(CollisionMasksSelected), stream);
        }
    }

    public static UndertaleSprite.TextureEntry CreateTextureEntry() => new();
    public static UndertaleSprite.MaskEntry CreateMaskEntry() => new();
}
