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
    /// Interaction logic for UndertaleExtensionFileEditor.xaml
    /// </summary>
    public partial class UndertaleExtensionFileEditor : DataUserControl
    {
        private static readonly MainWindow mainWindow = Application.Current.MainWindow as MainWindow;
        public UndertaleExtensionFileEditor()
        {
            InitializeComponent();

            ((System.Windows.Controls.Image)mainWindow.FindName("Flowey")).Opacity = 0;
            ((System.Windows.Controls.Image)mainWindow.FindName("FloweyLeave")).Opacity = 0;
            ((System.Windows.Controls.Image)mainWindow.FindName("FloweyBubble")).Opacity = 0;

            ((Label)this.FindName("ExtensionFileObjectLabel")).Content = ((Label)mainWindow.FindName("ObjectLabel")).Content;
        }

        private void DataUserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            UndertaleExtensionFile code = this.DataContext as UndertaleExtensionFile;

            int foundIndex = code is UndertaleResource res ? mainWindow.Data.IndexOf(res, false) : -1;
            string idString;

            if (foundIndex == -1)
                idString = "None";
            else if (foundIndex == -2)
                idString = "N/A";
            else
                idString = Convert.ToString(foundIndex);

            ((Label)this.FindName("ExtensionFileObjectLabel")).Content = idString;
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

        private void DataGrid_AddingNewItem(object sender, AddingNewItemEventArgs e)
        {
            var itemList = (sender as DataGrid).ItemsSource as IList<UndertaleExtensionFunction>;
            int lastItem = itemList.Count;

            UndertaleExtensionFunction obj = new UndertaleExtensionFunction()
            {
                Name = (Application.Current.MainWindow as MainWindow).Data.Strings.MakeString($"new_extension_function_{lastItem}"),
                ExtName = (Application.Current.MainWindow as MainWindow).Data.Strings.MakeString($"new_extension_function_{lastItem}_ext"),
                RetType = UndertaleExtensionVarType.Double,
                Arguments = new UndertaleSimpleList<UndertaleExtensionFunctionArg>(),
                Kind = 11, // ???
                ID = (Application.Current.MainWindow as MainWindow).Data.ExtensionFindLastId()
            };

            e.NewItem = obj;
        }
    }
}
