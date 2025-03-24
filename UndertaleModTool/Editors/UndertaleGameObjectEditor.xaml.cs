using System;
using System.Collections.Generic;
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
using System.Windows.Threading;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModTool
{
    /// <summary>
    /// Logika interakcji dla klasy UndertaleGameObjectEditor.xaml
    /// </summary>
    public partial class UndertaleGameObjectEditor : DataUserControl
    {
        private bool handleMouseScroll = true;

        public UndertaleGameObjectEditor()
        {
            InitializeComponent();
        }

        private void DataGrid_AddingNewItem(object sender, AddingNewItemEventArgs e)
        {
            UndertaleGameObject.Event obj = new UndertaleGameObject.Event();
            obj.Actions.Add(new UndertaleGameObject.EventAction());
            e.NewItem = obj;
        }

        // mouse wheel scrolling fix
        // source - https://stackoverflow.com/a/4342746/12136394
        private void HandlePreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!e.Handled && handleMouseScroll)
            {
                e.Handled = true;
                MouseWheelEventArgs eventArg = new(e.MouseDevice, e.Timestamp, e.Delta);
                eventArg.RoutedEvent = MouseWheelEvent;
                eventArg.Source = sender;
                UIElement parent = ((Control)sender).Parent as UIElement;
                parent.RaiseEvent(eventArg);
            }
        }

        private void ComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            (sender as ComboBox).DropDownOpened -= ComboBox_DropDownOpened;
            (sender as ComboBox).DropDownOpened += ComboBox_DropDownOpened;
            (sender as ComboBox).DropDownClosed -= ComboBox_DropDownClosed;
            (sender as ComboBox).DropDownClosed += ComboBox_DropDownClosed;
        }
        private void ComboBox_DropDownOpened(object sender, EventArgs e)
        {
            handleMouseScroll = false;
        }
        private void ComboBox_DropDownClosed(object sender, EventArgs e)
        {
            handleMouseScroll = true;
        }
        private void UndertaleObjectReference_Loaded(object sender, RoutedEventArgs e)
        {
            var objRef = sender as UndertaleObjectReference;

            objRef.ClearRemoveClickHandler();
            objRef.RemoveButton.Click += Remove_Click_Override;
            objRef.RemoveButton.ToolTip = "Remove action";
            objRef.RemoveButton.IsEnabled = true;
        }
        private void Remove_Click_Override(object sender, RoutedEventArgs e)
        {
            var btn = (ButtonDark)sender;
            var objRef = (UndertaleObjectReference)((Grid)btn.Parent).Parent;

            var obj = (UndertaleGameObject)DataContext;
            var evType = objRef.ObjectEventType;
            var evSubtype = objRef.ObjectEventSubtype;
            var action = (UndertaleGameObject.EventAction)btn.DataContext;
            var evList = ((UndertaleGameObject)DataContext).Events[(int)evType];

            var ev = evList.FirstOrDefault(x => x.EventSubtype == evSubtype);
            if (ev is null) return;
            ev.Actions.Remove(action);
            if (ev.Actions.Count <= 0)
            {
                evList.Remove(ev);
            }
        }
    }
}
