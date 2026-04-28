using System;
using System.Windows;
using System.Windows.Controls;
using UndertaleModLib;
using UndertaleModLib.Models;
using WpfAnimatedGif;

namespace UndertaleModTool
{
    /// <summary>
    /// Logika interakcji dla klasy UndertaleEmbeddedImageEditor.xaml
    /// </summary>
    public partial class UndertaleEmbeddedImageEditor : DataUserControl
    {
        private static MainWindow mainWindow = Application.Current.MainWindow as MainWindow;
        public UndertaleEmbeddedImageEditor()
        {
            InitializeComponent();

            ((Image)mainWindow.FindName("Flowey")).Opacity = 0;
            ((Image)mainWindow.FindName("FloweyLeave")).Opacity = 0;
            ((Image)mainWindow.FindName("FloweyBubble")).Opacity = 0;

            ((Label)this.FindName("EmbedImageObjectLabel")).Content = ((Label)mainWindow.FindName("ObjectLabel")).Content;
        }
        private void UndertaleEmbedImageEditor_Unloaded(object sender, RoutedEventArgs e)
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
            UndertaleEmbeddedImage code = this.DataContext as UndertaleEmbeddedImage;

            int foundIndex = code is UndertaleResource res ? mainWindow.Data.IndexOf(res, false) : -1;
            string idString;

            if (foundIndex == -1)
                idString = "None";
            else if (foundIndex == -2)
                idString = "N/A";
            else
                idString = Convert.ToString(foundIndex);

            ((Label)this.FindName("EmbedImageObjectLabel")).Content = idString;

            //((Image)mainWindow.FindName("FloweyBubble")).Opacity = 0;
            //((Image)mainWindow.FindName("Flowey")).Opacity = 0;
        }
    }
}
