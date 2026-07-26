using System;
using System.ComponentModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using UndertaleModLib;
using UndertaleModLib.Models;
using UndertaleModLib.Util;

namespace UndertaleModToolAvalonia;

public partial class UndertaleSoundViewModel : ObservableObject, IUndertaleResourceViewModel
{
    public MainViewModel MainVM;
    public UndertaleResource Resource => Sound;
    public UndertaleSound Sound { get; }

    [ObservableProperty]
    public partial bool IsBuiltinAudioGroup { get; set; }

    [ObservableProperty]
    public partial bool IsExternal { get; set; }

    AudioPlayer? audioPlayer = null;

    public UndertaleSoundViewModel(UndertaleSound sound, IServiceProvider serviceProvider)
    {
        MainVM = serviceProvider.GetRequiredService<MainViewModel>();

        Sound = sound;

        UpdateSoundProperties();
    }

    void ITabContent.OnAttached()
    {
        Sound.PropertyChanged += OnSoundPropertyChanged;
    }

    void ITabContent.OnDetached()
    {
        Sound.PropertyChanged -= OnSoundPropertyChanged;
        StopAudio();
    }

    public async void PlayAudio()
    {
        audioPlayer?.Stop();

        if (!IsExternal)
        {
            if (IsBuiltinAudioGroup)
            {
                if (Sound.AudioFile is not null)
                {
                    audioPlayer = new(Sound.AudioFile.Data);
                }
            }
            else if (Sound.AudioGroup is not null)
            {
                if (GetAudioGroupSoundData() is byte[] data)
                {
                    audioPlayer = new(data);
                }
            }
        }
        else
        {
            // TODO: Play external sound
        }
    }

    public async void StopAudio()
    {
        audioPlayer?.Stop();
        audioPlayer = null;
    }

    void OnSoundPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(UndertaleSound.AudioGroup)
            || e.PropertyName == nameof(UndertaleSound.Flags))
        {
            UpdateSoundProperties();
        }
    }

    void UpdateSoundProperties()
    {
        IsExternal = !Sound.Flags.HasFlag(UndertaleSound.AudioEntryFlags.IsEmbedded);
        IsBuiltinAudioGroup = Sound.Flags.HasFlag(UndertaleSound.AudioEntryFlags.IsEmbedded)
            && (Sound.AudioGroup is null || (MainVM.Data!.AudioGroups.IndexOf(Sound.AudioGroup) == MainVM.Data!.GetBuiltinSoundGroupID()));
    }

    byte[]? GetAudioGroupSoundData()
    {
        // TODO: Cache audio groups somewhere to not load them every time.
        if (Sound.AudioGroup is null)
            return null;

        string relativePath = Sound.AudioGroup.Path?.Content ?? $"audiogroup{Sound.GroupID}.dat";

        string path = Paths.JoinVerifyWithinDirectory(Path.GetDirectoryName(MainVM.DataPath), relativePath);

        if (File.Exists(path))
        {
            using FileStream stream = File.OpenRead(path);

            // TODO: Maybe deal with messages and warnings
            UndertaleData audioGroupData = UndertaleIO.Read(stream);

            if (Sound.AudioID >= audioGroupData.EmbeddedAudio.Count)
                return null;

            UndertaleEmbeddedAudio audioGroupEmbeddedAudio = audioGroupData.EmbeddedAudio[Sound.AudioID];

            return audioGroupEmbeddedAudio.Data;
        }

        return null;
    }
}
