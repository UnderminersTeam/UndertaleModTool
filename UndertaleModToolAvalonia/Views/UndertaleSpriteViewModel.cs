using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
using PropertyChanged.SourceGenerator;
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

    IReadOnlyList<FilePickerFileType> binFileTypes = [
        new FilePickerFileType("BIN files (.bin)")
        {
            Patterns = ["*.bin"],
        },
        new FilePickerFileType("All files")
        {
            Patterns = ["*"],
        },
    ];

    public UndertaleSpriteViewModel(UndertaleSprite sprite, IServiceProvider? serviceProvider = null)
    {
        MainVM = (serviceProvider ?? App.Services).GetRequiredService<MainViewModel>();
        Sprite = sprite;
    }

    public void TexturesSelectedChanged(object? item)
    {
        TexturesSelected = (UndertaleSprite.TextureEntry?)item!;
    }
    public void CollisionMasksSelectedChanged(object? item)
    {
        CollisionMasksSelected = (UndertaleSprite.MaskEntry?)item!;
    }

    public async void ImportCollisionMaskData()
    {
        if (CollisionMasksSelected is null)
            return;

        IReadOnlyList<IStorageFile> files = await MainVM.View!.OpenFileDialog(new FilePickerOpenOptions
        {
            Title = "Import collision mask data",
            FileTypeFilter = binFileTypes,
        });

        if (files.Count != 1)
            return;

        using Stream stream = await files[0].OpenReadAsync();
        byte[] bytes = new byte[stream.Length];
        await stream.ReadAsync(bytes);

        (int width, int height) = Sprite.CalculateMaskDimensions(MainVM.Data);
        UndertaleSprite.MaskEntry maskEntry = new(bytes, width, height);

        Sprite.CollisionMasks[Sprite.CollisionMasks.IndexOf(CollisionMasksSelected)] = maskEntry;
    }

    public async void ExportCollisionMaskData()
    {
        if (CollisionMasksSelected is null)
            return;

        IStorageFile? file = await MainVM.View!.SaveFileDialog(new FilePickerSaveOptions()
        {
            Title = "Export collision mask data",
            FileTypeChoices = binFileTypes,
            DefaultExtension = ".bin",
        });

        if (file is null)
            return;

        using Stream stream = await file.OpenWriteAsync();
        stream.Write(CollisionMasksSelected.Data);
    }

    public static UndertaleSprite.TextureEntry CreateTextureEntry() => new();
    public static UndertaleSprite.MaskEntry CreateMaskEntry() => new();
}
