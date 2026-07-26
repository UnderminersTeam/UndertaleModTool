using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia;

public partial class UndertaleSpriteViewModel : ObservableObject, IUndertaleResourceViewModel
{
    public MainViewModel MainVM;
    public UndertaleResource Resource => Sprite;
    public UndertaleSprite Sprite { get; }

    [ObservableProperty]
    public partial UndertaleSprite.TextureEntry? TexturesSelected { get; set; }

    [ObservableProperty]
    public partial UndertaleSprite.MaskEntry? CollisionMasksSelected { get; set; }

    [ObservableProperty]
    public partial bool ShowNineSlice { get; set; }

    [ObservableProperty]
    public partial bool EnableNineSlice { get; set; }

    public UndertaleSpriteViewModel(UndertaleSprite sprite, IServiceProvider serviceProvider)
    {
        MainVM = serviceProvider.GetRequiredService<MainViewModel>();

        Sprite = sprite;

        if (Sprite.Textures.Count > 0)
            TexturesSelected = Sprite.Textures[0];
        if (Sprite.CollisionMasks.Count > 0)
            CollisionMasksSelected = Sprite.CollisionMasks[0];

        UpdateSpriteProperties();
    }

    void ITabContent.OnAttached()
    {
        Sprite.PropertyChanged += OnSpritePropertyChanged;
    }

    void ITabContent.OnDetached()
    {
        Sprite.PropertyChanged -= OnSpritePropertyChanged;
    }

    void OnSpritePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(UndertaleSprite.SVersion):
            case nameof(UndertaleSprite.V3NineSlice):
                UpdateSpriteProperties();
                break;
        }
    }

    void UpdateSpriteProperties()
    {
        if (Sprite.SVersion >= 2)
        {
            // TODO: sequences
        }
        if (Sprite.SVersion >= 3)
        {
            ShowNineSlice = true;
            EnableNineSlice = Sprite.V3NineSlice is not null;
        }
        else
        {
            ShowNineSlice = false;
            EnableNineSlice = false;
        }
    }

    partial void OnEnableNineSliceChanged(bool value)
    {
        if (value)
            Sprite.V3NineSlice ??= new();
        else
            Sprite.V3NineSlice = null;
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
