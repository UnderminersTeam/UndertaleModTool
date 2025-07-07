using System;
using System.Collections.Generic;
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
using WpfAnimatedGif;

namespace UndertaleModTool
{
    /// <summary>
    /// Interaction logic for UndertaleTimelineEditor.xaml
    /// </summary>
    public partial class UndertaleTimelineEditor : DataUserControl
    {
        private static MainWindow mainWindow = Application.Current.MainWindow as MainWindow;
        public UndertaleTimelineEditor()
        {
            InitializeComponent();

            ((Image)mainWindow.FindName("Flowey")).Opacity = 0;
            ((Image)mainWindow.FindName("FloweyLeave")).Opacity = 0;
            ((Image)mainWindow.FindName("FloweyBubble")).Opacity = 0;

            ((Label)this.FindName("TimelinesObjectLabel")).Content = ((Label)mainWindow.FindName("ObjectLabel")).Content;
        }

        private void DataGrid_AddingNewItem(object sender, AddingNewItemEventArgs e)
        {
            UndertaleTimeline.UndertaleTimelineMoment obj = new UndertaleTimeline.UndertaleTimelineMoment();

            // find the last timeline moment (which should have the biggest step value)
            var lastMoment = ((sender as DataGrid).ItemsSource as IList<UndertaleTimeline.UndertaleTimelineMoment>).LastOrDefault();

            // the default value is 0 anyway.
            if (lastMoment != null)
                obj.Step = lastMoment.Step + 1;

            // make an empty event with a null code entry.
            obj.Event = new UndertalePointerList<UndertaleGameObject.EventAction>();
            obj.Event.Add(new UndertaleGameObject.EventAction());

            // we're done here.
            e.NewItem = obj;
        }
        private void UndertaleTimelinesEditor_Unloaded(object sender, RoutedEventArgs e)
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
            UndertaleTimeline code = this.DataContext as UndertaleTimeline;

            int foundIndex = code is UndertaleResource res ? mainWindow.Data.IndexOf(res, false) : -1;
            string idString;

            if (foundIndex == -1)
                idString = "None";
            else if (foundIndex == -2)
                idString = "N/A";
            else
                idString = Convert.ToString(foundIndex);

            ((Label)this.FindName("TimelinesObjectLabel")).Content = idString;

            //((Image)mainWindow.FindName("FloweyBubble")).Opacity = 0;
            //((Image)mainWindow.FindName("Flowey")).Opacity = 0;
        }
    }
}
