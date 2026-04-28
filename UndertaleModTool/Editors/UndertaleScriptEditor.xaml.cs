using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using UndertaleModLib.Models;
using UndertaleModLib;
using WpfAnimatedGif;

namespace UndertaleModTool
{
    /// <summary>
    /// Logika interakcji dla klasy UndertaleScriptEditor.xaml
    /// </summary>
    public partial class UndertaleScriptEditor : DataUserControl
    {
        private static readonly MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

        public UndertaleScriptEditor()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            Unloaded += OnUnloaded;

            ((System.Windows.Controls.Image)mainWindow.FindName("Flowey")).Opacity = 0;
            ((System.Windows.Controls.Image)mainWindow.FindName("FloweyLeave")).Opacity = 0;
            ((System.Windows.Controls.Image)mainWindow.FindName("FloweyBubble")).Opacity = 0;

            ((Label)this.FindName("ScriptObjectLabel")).Content = ((Label)mainWindow.FindName("ObjectLabel")).Content;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is UndertaleScript oldObj)
            {
                oldObj.PropertyChanged -= OnPropertyChanged;
            }
            var floweranim = ((System.Windows.Controls.Image)mainWindow.FindName("Flowey"));
            //floweranim.Opacity = 1;

            var controller = ImageBehavior.GetAnimationController(floweranim);
            controller.Pause();
            controller.GotoFrame(controller.FrameCount - 5);
            controller.Play();

            ((System.Windows.Controls.Image)mainWindow.FindName("FloweyLeave")).Opacity = 0;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is UndertaleScript oldObj)
            {
                oldObj.PropertyChanged -= OnPropertyChanged;
            }
            if (e.NewValue is UndertaleScript newObj)
            {
                newObj.PropertyChanged += OnPropertyChanged;
            }

            UndertaleScript code = this.DataContext as UndertaleScript;

            int foundIndex = code is UndertaleResource res ? mainWindow.Data.IndexOf(res, false) : -1;
            string idString;

            if (foundIndex == -1)
                idString = "None";
            else if (foundIndex == -2)
                idString = "N/A";
            else
                idString = Convert.ToString(foundIndex);

            ((Label)this.FindName("ScriptObjectLabel")).Content = idString;
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
                if (DataContext is UndertaleScript obj)
                {
                    mainWindow.Project?.MarkAssetForExport(obj);
                }
            });
        }
    }
}
