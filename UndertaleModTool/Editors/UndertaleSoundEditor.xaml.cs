using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using NAudio.Vorbis;
using NAudio.Wave;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModTool
{
    /// <summary>
    /// Logika interakcji dla klasy UndertaleSoundEditor.xaml
    /// </summary>
    public partial class UndertaleSoundEditor : UserControl
    {
        private WaveOutEvent waveOut;
        private WaveFileReader wavReader;
        private VorbisWaveReader oggReader;
        private Mp3FileReader mp3Reader;
        private UndertaleData audioGroupData;
        private string loadedPath;

        public UndertaleSoundEditor()
        {
            InitializeComponent();
            this.Unloaded += Unload;
        }

        public void Unload(object sender, RoutedEventArgs e)
        {
            if (waveOut != null)
                waveOut.Stop();
        }

        private void InitAudio()
        {
            if (waveOut == null)
                waveOut = new WaveOutEvent();
            else if (waveOut.PlaybackState != PlaybackState.Stopped)
                waveOut.Stop();
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            UndertaleSound sound = DataContext as UndertaleSound;

            if ((sound.Flags & UndertaleSound.AudioEntryFlags.IsEmbedded) != UndertaleSound.AudioEntryFlags.IsEmbedded)
            {
                try
                {
                    string audioPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName((Application.Current.MainWindow as MainWindow).FilePath), sound.File.Content);
                    if (File.Exists(audioPath))
                    {
                        switch (System.IO.Path.GetExtension(sound.File.Content).ToLower())
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
                    MessageBox.Show("Failed to play audio!\r\n" + ex.Message, "Audio failure", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                return;
            }

            UndertaleEmbeddedAudio target;

            if (sound.GroupID != 0 && sound.AudioID != -1)
            {
                try
                {
                    string path = System.IO.Path.Combine(System.IO.Path.GetDirectoryName((Application.Current.MainWindow as MainWindow).FilePath), "audiogroup" + sound.GroupID + ".dat");
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
                    MessageBox.Show("Failed to play audio!\r\n" + ex.Message, "Audio failure", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                            MessageBox.Show("Failed to play audio!\r\nNot a WAV or OGG.", "Audio failure", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    catch (Exception ex)
                    {
                        waveOut = null;
                        MessageBox.Show("Failed to play audio!\r\n" + ex.Message, "Audio failure", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            else
                MessageBox.Show("Failed to play audio!\r\nNo options for playback worked.", "Audio failure", MessageBoxButton.OK, MessageBoxImage.Warning);
        }


        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            if (waveOut != null)
                waveOut.Stop();
        }
    }
}
