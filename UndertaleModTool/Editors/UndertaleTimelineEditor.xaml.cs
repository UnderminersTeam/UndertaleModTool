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

namespace UndertaleModTool
{
    /// <summary>
    /// Interaction logic for UndertaleTimelineEditor.xaml
    /// </summary>
    public partial class UndertaleTimelineEditor : UserControl
    {
        public UndertaleTimelineEditor()
        {
            InitializeComponent();
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
    }
}
