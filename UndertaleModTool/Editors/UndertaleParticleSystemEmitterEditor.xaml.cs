using System;
using System.Windows;
using System.Windows.Controls;
using UndertaleModLib;
using UndertaleModLib.Models;
using WpfAnimatedGif;

namespace UndertaleModTool
{
    /// <summary>
    /// Interaction logic for UndertaleParticleSystemEmitterEditor.xaml
    /// </summary>
    public partial class UndertaleParticleSystemEmitterEditor : DataUserControl
    {
        private static readonly MainWindow mainWindow = Application.Current.MainWindow as MainWindow;
        public UndertaleParticleSystemEmitterEditor()
        {
            InitializeComponent();

            ((System.Windows.Controls.Image)mainWindow.FindName("Flowey")).Opacity = 0;
            ((System.Windows.Controls.Image)mainWindow.FindName("FloweyLeave")).Opacity = 0;
            ((System.Windows.Controls.Image)mainWindow.FindName("FloweyBubble")).Opacity = 0;

            ((Label)this.FindName("ParticleEmitObjectLabel")).Content = ((Label)mainWindow.FindName("ObjectLabel")).Content;
        }
        private void DataUserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            UndertaleParticleSystemEmitter code = this.DataContext as UndertaleParticleSystemEmitter;

            int foundIndex = code is UndertaleResource res ? mainWindow.Data.IndexOf(res, false) : -1;
            string idString;

            if (foundIndex == -1)
                idString = "None";
            else if (foundIndex == -2)
                idString = "N/A";
            else
                idString = Convert.ToString(foundIndex);

            ((Label)this.FindName("ParticleEmitObjectLabel")).Content = idString;
        }
        private void DataUserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            var floweranim = ((System.Windows.Controls.Image)mainWindow.FindName("Flowey"));
            //floweranim.Opacity = 1;

            var controller = ImageBehavior.GetAnimationController(floweranim);
            controller.Pause();
            controller.GotoFrame(controller.FrameCount - 5);
            controller.Play();

            ((System.Windows.Controls.Image)mainWindow.FindName("FloweyLeave")).Opacity = 0;
        }
    }
}
