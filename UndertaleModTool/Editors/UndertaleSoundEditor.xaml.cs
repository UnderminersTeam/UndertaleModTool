#pragma warning disable CA1416 // Validate platform compatibility

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using NAudio.Vorbis;
using NAudio.Wave;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModTool
{
    /// <summary>
    /// Logika interakcji dla klasy UndertaleSoundEditor.xaml
    /// </summary>
    public partial class UndertaleSoundEditor : DataUserControl
    {
        private WaveOutEvent waveOut;
        private WaveFileReader wavReader;
        private VorbisWaveReader oggReader;
        private Mp3FileReader mp3Reader;
        private UndertaleData audioGroupData;
        private string loadedPath;

        private static readonly MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

        public UndertaleSoundEditor()
        {
            InitializeComponent();
            Unloaded += OnUnloaded;
            DataContextChanged += OnDataContextChanged;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            waveOut?.Stop();

            if (DataContext is UndertaleSound oldObj)
            {
                oldObj.PropertyChanged -= OnPropertyChanged;
            }
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is UndertaleSound oldObj)
            {
                oldObj.PropertyChanged -= OnPropertyChanged;
            }
            if (e.NewValue is UndertaleSound newObj)
            {
                newObj.PropertyChanged += OnPropertyChanged;
            }
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnAssetUpdated();
        }

        private void OnAssetUpdated()
        {
            if (mainWindow.Project is null || !mainWindow.IsSelectedProjectExportable)
            {
                return;
            }
            Dispatcher.BeginInvoke(() =>
            {
                if (DataContext is UndertaleSound obj)
                {
                    mainWindow.Project?.MarkAssetForExport(obj);
                }
            });
        }

        private void InitAudio()
        {
            if (waveOut == null)
                waveOut = new WaveOutEvent() { DeviceNumber = 0 };
            else if (waveOut.PlaybackState != PlaybackState.Stopped)
                waveOut.Stop();
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            UndertaleSound sound = DataContext as UndertaleSound;

            if ((sound.Flags & UndertaleSound.AudioEntryFlags.IsEmbedded) != UndertaleSound.AudioEntryFlags.IsEmbedded &&
                (sound.Flags & UndertaleSound.AudioEntryFlags.IsCompressed) != UndertaleSound.AudioEntryFlags.IsCompressed)
            {
                try
                {
                    string filename;
                    if (!sound.File.Content.Contains("."))
                        filename = sound.File.Content + ".ogg";
                    else
                        filename = sound.File.Content;
                    string audioPath = Path.Combine(Path.GetDirectoryName((Application.Current.MainWindow as MainWindow).FilePath), filename);
                    if (File.Exists(audioPath))
                    {
                        switch (Path.GetExtension(filename).ToLower())
                        {
                            case ".wav":
                                wavReader = new WaveFileReader(audioPath);
                                InitAudio();
                                waveOut.Init(wavReader);
                                waveOut.Play();
                                break;
                            case ".ogg":
                                oggReader = new VorbisWaveReader(audioPath);
                                InitAudio();
                                waveOut.Init(oggReader);
                                waveOut.Play();
                                break;
                            case ".mp3":
                                mp3Reader = new Mp3FileReader(audioPath);
                                InitAudio();
                                waveOut.Init(mp3Reader);
                                waveOut.Play();
                                break;
                            default:
                                throw new Exception("Unknown file type.");
                        }
                    }
                    else
                        throw new Exception("Failed to find audio file.");
                } catch (Exception ex)
                {
                    waveOut = null;
                    mainWindow.ShowError("Failed to play audio!\r\n" + ex.Message, "Audio failure");
                }
                return;
            }

            UndertaleEmbeddedAudio target;

            if (sound.GroupID != 0 && sound.AudioID != -1)
            {
                try
                {
                    string path = Path.Combine(Path.GetDirectoryName((Application.Current.MainWindow as MainWindow).FilePath), "audiogroup" + sound.GroupID + ".dat");
                    if (File.Exists(path))
                    {
                        if (loadedPath != path)
                        {
                            loadedPath = path;
                            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
                            {
                                audioGroupData = UndertaleIO.Read(stream, warning =>
                                {
                                    throw new Exception(warning);
                                });
                            }
                        }

                        target = audioGroupData.EmbeddedAudio[sound.AudioID];
                    }
                    else
                        throw new Exception("Failed to find audio group file.");
                } catch (Exception ex)
                {
                    waveOut = null;
                    mainWindow.ShowError("Failed to play audio!\r\n" + ex.Message, "Audio failure");
                    return;
                }
            } else
                target = sound.AudioFile;

            if (target != null)
            {
                if (target.Data.Length > 4)
                {
                    try
                    {
                        if (target.Data[0] == 'R' && target.Data[1] == 'I' && target.Data[2] == 'F' && target.Data[3] == 'F')
                        {
                            wavReader = new WaveFileReader(new MemoryStream(target.Data));
                            InitAudio();
                            waveOut.Init(wavReader);
                            waveOut.Play();
                        }
                        else if (target.Data[0] == 'O' && target.Data[1] == 'g' && target.Data[2] == 'g' && target.Data[3] == 'S')
                        {
                            oggReader = new VorbisWaveReader(new MemoryStream(target.Data));
                            InitAudio();
                            waveOut.Init(oggReader);
                            waveOut.Play();
                        }
                        else
                            mainWindow.ShowError("Failed to play audio!\r\nNot a WAV or OGG.", "Audio failure");
                    }
                    catch (Exception ex)
                    {
                        waveOut = null;
                        mainWindow.ShowError("Failed to play audio!\r\n" + ex.Message, "Audio failure");
                    }
                }
            }
            else
                mainWindow.ShowError("Failed to play audio!\r\nNo options for playback worked.", "Audio failure");
        }


        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            if (waveOut != null)
                waveOut.Stop();
        }
    }
}

#pragma warning restore CA1416 // Validate platform compatibility
