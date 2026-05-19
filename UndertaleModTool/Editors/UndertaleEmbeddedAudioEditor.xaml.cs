using Microsoft.Win32;
using NAudio.Vorbis;
using NAudio.Wave;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using UndertaleModLib.Models;
using UndertaleModTool.Localization;

namespace UndertaleModTool
{
    /// <summary>
    /// Logika interakcji dla klasy UndertaleEmbeddedAudioEditor.xaml
    /// </summary>
    public partial class UndertaleEmbeddedAudioEditor : DataUserControl, INotifyPropertyChanged
    {
        private WaveOutEvent waveOut;
        private WaveFileReader wavReader;
        private VorbisWaveReader oggReader;

        private static readonly MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

        public string FileType
        {
            get
            {
                if (DataContext is not UndertaleEmbeddedAudio target)
                {
                    return LocalizationSource.GetString("Common_Unknown");
                }

                if (IsWav(target.Data))
                {
                    return "WAV";
                }
                if (IsOgg(target.Data))
                {
                    return "OGG";
                }
                return LocalizationSource.GetString("Common_Unknown");
            }
        }

        public UndertaleEmbeddedAudioEditor()
        {
            InitializeComponent();
            DataContextChanged += (sender, args) =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FileType)));
            };
            this.Unloaded += Unload;
        }

        public void Unload(object sender, RoutedEventArgs e)
        {
            waveOut?.Stop();
        }

        private void Import_Click(object sender, RoutedEventArgs e)
        {
            UndertaleEmbeddedAudio target = DataContext as UndertaleEmbeddedAudio;

            OpenFileDialog dlg = new();

            if (IsWav(target.Data))
            {
                dlg.DefaultExt = ".wav";
                dlg.Filter = LocalizationSource.GetString("FileFilter_WAV") + "|*.wav|" + LocalizationSource.GetString("FileFilter_AllFiles") + "|*";
            }
            else if (IsOgg(target.Data))
            {
                dlg.DefaultExt = ".ogg";
                dlg.Filter = LocalizationSource.GetString("FileFilter_OGG") + "|*.ogg|" + LocalizationSource.GetString("FileFilter_AllFiles") + "|*";
            }
            else
            {
                dlg.DefaultExt = "";
                dlg.Filter = LocalizationSource.GetString("FileFilter_AllFiles") + "|*";
            }

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    byte[] data = File.ReadAllBytes(dlg.FileName);
                    if (!IsWav(data) && !IsOgg(data))
                    {
                        if (mainWindow.ShowQuestionWithCancel(LocalizationSource.GetString("Msg_ImportNonAudioWarning"), MessageBoxImage.Warning, LocalizationSource.GetString("Dialog_UnknownFormat")) != MessageBoxResult.Yes)
                        {
                            return;
                        }
                    }
                    else if ((IsWav(target.Data) && IsOgg(data)) || (IsOgg(target.Data) && IsWav(data)))
                    {
                        if (mainWindow.ShowQuestionWithCancel(
                            LocalizationSource.GetString("Msg_ImportFormatMismatchWarning"), MessageBoxImage.Warning, LocalizationSource.GetString("Dialog_FormatMismatch")) != MessageBoxResult.Yes)
                        {
                            return;
                        }
                    }
                    target.Data = data;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FileType)));
                }
                catch (Exception ex)
                {
                    mainWindow.ShowError(string.Format(LocalizationSource.GetString("Msg_FailedToImportFile"), ex.Message), LocalizationSource.GetString("Dialog_FailedToImportFile"));
                }
            }
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            UndertaleEmbeddedAudio target = DataContext as UndertaleEmbeddedAudio;

            SaveFileDialog dlg = new SaveFileDialog();

            if (IsWav(target.Data))
            {
                dlg.DefaultExt = ".wav";
                dlg.Filter = LocalizationSource.GetString("FileFilter_WAV") + "|*.wav|" + LocalizationSource.GetString("FileFilter_AllFiles") + "|*";
            }
            else if (IsOgg(target.Data))
            {
                dlg.DefaultExt = ".ogg";
                dlg.Filter = LocalizationSource.GetString("FileFilter_OGG") + "|*.ogg|" + LocalizationSource.GetString("FileFilter_AllFiles") + "|*";
            }
            else
            {
                dlg.DefaultExt = "";
                dlg.Filter = LocalizationSource.GetString("FileFilter_AllFiles") + "|*";
            }

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    File.WriteAllBytes(dlg.FileName, target.Data);
                }
                catch (Exception ex)
                {
                    mainWindow.ShowError(string.Format(LocalizationSource.GetString("Msg_FailedToExportFile"), ex.Message), LocalizationSource.GetString("Dialog_FailedToExportFile"));
                }
            }
        }

        private void InitAudio()
        {
            if (waveOut == null)
            {
                waveOut = new WaveOutEvent() { DeviceNumber = 0 };
            }
            else if (waveOut.PlaybackState != PlaybackState.Stopped)
            {
                waveOut.Stop();
            }
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            UndertaleEmbeddedAudio target = DataContext as UndertaleEmbeddedAudio;

            try
            {
                if (IsWav(target.Data))
                {
                    wavReader = new WaveFileReader(new MemoryStream(target.Data));
                    InitAudio();
                    waveOut.Init(wavReader);
                    waveOut.Play();
                }
                else if (IsOgg(target.Data))
                {
                    oggReader = new VorbisWaveReader(new MemoryStream(target.Data));
                    InitAudio();
                    waveOut.Init(oggReader);
                    waveOut.Play();
                }
                else
                {
                    mainWindow.ShowError(LocalizationSource.GetString("Msg_FailedToPlayAudioNotWavOgg"), LocalizationSource.GetString("Dialog_AudioFailure"));
                }
            } 
            catch (Exception ex)
            {
                waveOut = null;
                mainWindow.ShowError(string.Format(LocalizationSource.GetString("Msg_FailedToPlayAudio"), ex.Message), LocalizationSource.GetString("Dialog_AudioFailure"));
            }
        }


        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            waveOut?.Stop();
        }
        
        private static bool IsWav(byte[] data) 
        {
            return data.Length >= 4 && data[0] == 'R' && data[1] == 'I' && data[2] == 'F' && data[3] == 'F';
        }

        private static bool IsOgg(byte[] data) 
        {
            return data.Length >= 4 && data[0] == 'O' && data[1] == 'g' && data[2] == 'g' && data[3] == 'S';
        }
    }
}