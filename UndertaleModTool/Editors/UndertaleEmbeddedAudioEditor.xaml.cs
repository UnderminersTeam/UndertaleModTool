using Microsoft.Win32;
using NAudio.Vorbis;
using NAudio.Wave;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using UndertaleModLib;
using UndertaleModLib.Models;
using WpfAnimatedGif;

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
                    return "Unknown";
                }

                if (IsWav(target.Data))
                {
                    return "WAV";
                }
                if (IsOgg(target.Data))
                {
                    return "OGG";
                }
                return "Unknown";
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

            ((Image)mainWindow.FindName("Flowey")).Opacity = 0;
            ((Image)mainWindow.FindName("FloweyLeave")).Opacity = 0;
            ((Image)mainWindow.FindName("FloweyBubble")).Opacity = 0;

            ((Label)this.FindName("EmbedAudioObjectLabel")).Content = ((Label)mainWindow.FindName("ObjectLabel")).Content;
        }

        private void UndertaleEmbedAudioEditor_Unloaded(object sender, RoutedEventArgs e)
        {
            var floweranim = ((Image)mainWindow.FindName("Flowey"));
            //floweranim.Opacity = 1;

            var controller = ImageBehavior.GetAnimationController(floweranim);
            controller.Pause();
            controller.GotoFrame(controller.FrameCount - 5);
            controller.Play();

            ((Image)mainWindow.FindName("FloweyLeave")).Opacity = 0;
        }
        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            UndertaleEmbeddedAudio code = this.DataContext as UndertaleEmbeddedAudio;

            int foundIndex = code is UndertaleResource res ? mainWindow.Data.IndexOf(res, false) : -1;
            string idString;

            if (foundIndex == -1)
                idString = "None";
            else if (foundIndex == -2)
                idString = "N/A";
            else
                idString = Convert.ToString(foundIndex);

            ((Label)this.FindName("EmbedAudioObjectLabel")).Content = idString;

            //((Image)mainWindow.FindName("FloweyBubble")).Opacity = 0;
            //((Image)mainWindow.FindName("Flowey")).Opacity = 0;
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
                dlg.Filter = "WAV files|*.wav|All files|*";
            } 
            else if (IsOgg(target.Data)) 
            {
                dlg.DefaultExt = ".ogg";
                dlg.Filter = "OGG files|*.ogg|All files|*";
            } 
            else 
            {
                dlg.DefaultExt = "";
                dlg.Filter = "All files|*";
            }

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    byte[] data = File.ReadAllBytes(dlg.FileName);
                    if (!IsWav(data) && !IsOgg(data)) 
                    {
                        if (mainWindow.ShowQuestionWithCancel("Warning: File being imported is not a WAV or OGG. Import anyway?\r\n\r\nThis may corrupt the sound.", MessageBoxImage.Warning, "Unknown format") != MessageBoxResult.Yes)
                        {
                            return;
                        }
                    }
                    else if ((IsWav(target.Data) && IsOgg(data)) || (IsOgg(target.Data) && IsWav(data)))
                    {
                        if (mainWindow.ShowQuestionWithCancel(
                            "Warning: Filetype being imported does not match existing filetype. Import anyway?\r\n\r\n" +
                            "This may corrupt the sound, unless sound asset compression settings are adjusted as well.", MessageBoxImage.Warning, "Format mismatch") != MessageBoxResult.Yes)
                        {
                            return;
                        }
                    } 
                    target.Data = data;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FileType)));
                }
                catch (Exception ex)
                {
                    mainWindow.ShowError("Failed to import file: " + ex.Message, "Failed to import file");
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
                dlg.Filter = "WAV files|*.wav|All files|*";
            } 
            else if (IsOgg(target.Data)) 
            {
                dlg.DefaultExt = ".ogg";
                dlg.Filter = "OGG files|*.ogg|All files|*";
            } 
            else 
            {
                dlg.DefaultExt = "";
                dlg.Filter = "All files|*";
            }

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    File.WriteAllBytes(dlg.FileName, target.Data);
                }
                catch (Exception ex)
                {
                    mainWindow.ShowError("Failed to export file: " + ex.Message, "Failed to export file");
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
                    mainWindow.ShowError("Failed to play audio!\r\nNot a WAV or OGG.", "Audio failure");
                }
            } 
            catch (Exception ex)
            {
                waveOut = null;
                mainWindow.ShowError("Failed to play audio!\r\n" + ex.Message, "Audio failure");
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