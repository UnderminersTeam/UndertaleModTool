using System;
using System.IO;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia.Views;

public partial class UndertaleEmbeddedAudioViewModel : IUndertaleResourceViewModel
{
    public MainViewModel MainVM;
    public UndertaleResource Resource => EmbeddedAudio;
    public UndertaleEmbeddedAudio EmbeddedAudio { get; set; }

    public UndertaleEmbeddedAudioViewModel(UndertaleEmbeddedAudio embeddedAudio, IServiceProvider? serviceProvider = null)
    {
        MainVM = (serviceProvider ?? App.Services).GetRequiredService<MainViewModel>();

        EmbeddedAudio = embeddedAudio;
    }

    public async void SaveAudio()
    {
        IStorageFile? file = await MainVM.View!.SaveFileDialog(new FilePickerSaveOptions()
        {
            Title = "Save audio",
            FileTypeChoices = [
                new FilePickerFileType("WAV files (.wav)")
                {
                    Patterns = ["*.wav"],
                },
                new FilePickerFileType("All files")
                {
                    Patterns = ["*"],
                },
            ],
            DefaultExtension = ".wav",
        });

        if (file is null)
            return;

        using Stream stream = await file.OpenWriteAsync();
        stream.Write(EmbeddedAudio.Data);
    }
}
