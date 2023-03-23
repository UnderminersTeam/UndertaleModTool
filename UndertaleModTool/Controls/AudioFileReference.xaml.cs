using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModTool
{
    /// <summary>
    /// Logika interakcji dla klasy UndertaleObjectReference.xaml
    /// </summary>
    public partial class AudioFileReference : UserControl
    {
        public static readonly DependencyProperty AudioReferenceProperty =
            DependencyProperty.Register("AudioReference", typeof(UndertaleEmbeddedAudio),
                typeof(AudioFileReference),
                new FrameworkPropertyMetadata(null,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (sender, e) =>
                    {
                        var inst = sender as AudioFileReference;
                        if (inst is null)
                            return;

                        if (e.NewValue is null)
                            inst.ObjectText.ContextMenu = null;
                        else
                        {
                            try
                            {
                                inst.ObjectText.ContextMenu = inst.Resources["contextMenu"] as ContextMenu;
                            }
                            catch { }
                        }
                    }));

        public static readonly DependencyProperty GroupReferenceProperty =
            DependencyProperty.Register("GroupReference", typeof(UndertaleAudioGroup),
                typeof(AudioFileReference),
                new FrameworkPropertyMetadata(null,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty AudioIDReference =
            DependencyProperty.Register("AudioID", typeof(int),
                typeof(AudioFileReference));

        public static readonly DependencyProperty GroupIDProperty =
            DependencyProperty.Register("GroupID", typeof(int),
                typeof(AudioFileReference));

        public UndertaleEmbeddedAudio AudioReference
        {
            get { return (UndertaleEmbeddedAudio)GetValue(AudioReferenceProperty); }
            set { SetValue(AudioReferenceProperty, value); }
        }

        public UndertaleAudioGroup GroupReference
        {
            get { return (UndertaleAudioGroup)GetValue(GroupReferenceProperty); }
            set { SetValue(GroupReferenceProperty, value); }
        }
        
        public int AudioID
        {
            get { return (int)GetValue(AudioIDReference); }
            set { SetValue(AudioIDReference, value); }
        }

        public int GroupID
        {
            get { return (int)GetValue(GroupIDProperty); }
            set { SetValue(GroupIDProperty, value); }
        }

        public AudioFileReference()
        {
            InitializeComponent();
        }

        private void Details_Click(object sender, RoutedEventArgs e)
        {
            OpenReference();
        }
        private void Details_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle && e.ButtonState == MouseButtonState.Pressed)
                OpenReference(true);
        }
        private void OpenInNewTabItem_Click(object sender, RoutedEventArgs e)
        {
            OpenReference(true);
        }

        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            AudioReference = null;
        }

        private void TextBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            OpenReference();
        }

        private void OpenReference(bool inNewTab = false)
        {
            if (GroupID != 0 && AudioID != -1)
            {
                (Application.Current.MainWindow as MainWindow).OpenChildFile("audiogroup" + GroupID + ".dat", "AUDO", AudioID);
                return;
            }

            if (AudioReference == null)
                return;

            (Application.Current.MainWindow as MainWindow).ChangeSelection(AudioReference, inNewTab);
        }

        private void TextBox_DragOver(object sender, DragEventArgs e)
        {
            UndertaleObject sourceItem = e.Data.GetData(e.Data.GetFormats()[0]) as UndertaleObject;

            e.Effects = GroupID == 0 && e.AllowedEffects.HasFlag(DragDropEffects.Link) && sourceItem != null && sourceItem.GetType() == typeof(UndertaleEmbeddedAudio) ? DragDropEffects.Link : DragDropEffects.None;
            e.Handled = true;
        }

        private void TextBox_Drop(object sender, DragEventArgs e)
        {
            UndertaleObject sourceItem = e.Data.GetData(e.Data.GetFormats()[0]) as UndertaleObject;

            e.Effects = GroupID == 0 && e.AllowedEffects.HasFlag(DragDropEffects.Link) && sourceItem != null && sourceItem.GetType() == typeof(UndertaleEmbeddedAudio) ? DragDropEffects.Link : DragDropEffects.None;
            if (e.Effects == DragDropEffects.Link)
            {
                AudioReference = (UndertaleEmbeddedAudio)sourceItem;
            }
            e.Handled = true;
        }
    }
}
