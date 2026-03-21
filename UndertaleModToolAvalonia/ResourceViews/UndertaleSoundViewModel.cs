using System;
using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using PropertyChanged.SourceGenerator;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia;

public partial class UndertaleSoundViewModel : IUndertaleResourceViewModel
{
    public MainViewModel MainVM;
    public UndertaleResource Resource => Sound;
    public UndertaleSound Sound { get; }

    [Notify]
    private bool _IsBuiltinAudioGroup;

    AudioPlayer? audioPlayer = null;

    public UndertaleSoundViewModel(UndertaleSound sound, IServiceProvider? serviceProvider = null)
    {
        MainVM = (serviceProvider ?? App.Services).GetRequiredService<MainViewModel>();

        Sound = sound;

        UpdateIsBuiltinAudioGroup();
    }

    public void OnAttached()
    {
        Sound.PropertyChanged += OnSoundPropertyChanged;
    }

    public void OnDetached()
    {
        Sound.PropertyChanged -= OnSoundPropertyChanged;
        StopAudio();
    }

    public async void PlayAudio()
    {
        audioPlayer?.Stop();
        if (Sound.AudioFile is not null)
            audioPlayer = new(Sound.AudioFile.Data);
    }

    public async void StopAudio()
    {
        audioPlayer?.Stop();
        audioPlayer = null;
    }

    void OnSoundPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(UndertaleSound.AudioGroup))
        {
            UpdateIsBuiltinAudioGroup();
        }
    }

    void UpdateIsBuiltinAudioGroup()
    {
        IsBuiltinAudioGroup = (MainVM.Data!.AudioGroups.IndexOf(Sound.AudioGroup) == MainVM.Data!.GetBuiltinSoundGroupID());
    }
}
