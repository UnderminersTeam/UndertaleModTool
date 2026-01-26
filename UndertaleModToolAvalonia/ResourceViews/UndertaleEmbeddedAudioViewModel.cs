using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia;

public partial class UndertaleEmbeddedAudioViewModel : IUndertaleResourceViewModel
{
    public MainViewModel MainVM;
    public UndertaleResource Resource => EmbeddedAudio;
    public UndertaleEmbeddedAudio EmbeddedAudio { get; set; }

    IReadOnlyList<FilePickerFileType> audioFileTypes = [
        new FilePickerFileType("WAV files (.wav)")
        {
            Patterns = ["*.wav"],
        },
        new FilePickerFileType("All files")
        {
            Patterns = ["*"],
        },
    ];

    public UndertaleEmbeddedAudioViewModel(UndertaleEmbeddedAudio embeddedAudio, IServiceProvider? serviceProvider = null)
    {
        MainVM = (serviceProvider ?? App.Services).GetRequiredService<MainViewModel>();

        EmbeddedAudio = embeddedAudio;
    }

    public async void ImportAudio()
    {
        IReadOnlyList<IStorageFile> files = await MainVM.View!.OpenFileDialog(new FilePickerOpenOptions
        {
            Title = "Import audio",
            FileTypeFilter = audioFileTypes,
        });

        if (files.Count != 1)
            return;

        byte[] bytes;
        using (Stream stream = await files[0].OpenReadAsync())
        {
            bytes = new byte[stream.Length];
            await stream.ReadExactlyAsync(bytes);
        }

        EmbeddedAudio.Data = bytes;
    }

    public async void ExportAudio()
    {
        IStorageFile? file = await MainVM.View!.SaveFileDialog(new FilePickerSaveOptions()
        {
            Title = "Export audio",
            FileTypeChoices = audioFileTypes,
            DefaultExtension = ".wav",
            SuggestedFileName = $"{EmbeddedAudio.Name.Content}.wav",
        });

        if (file is null)
            return;

        using Stream stream = await file.OpenWriteAsync();
        stream.Write(EmbeddedAudio.Data);
    }
}
