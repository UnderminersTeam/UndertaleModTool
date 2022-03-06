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
        public UndertaleGameObjectEditor()
        {
            InitializeComponent();
        }

        private void DataGrid_AddingNewItem(object sender, AddingNewItemEventArgs e)
        {
            UndertaleGameObject.Event obj = new UndertaleGameObject.Event();
            obj.Actions.Add(new UndertaleGameObject.EventAction());
            e.NewItem = obj;

            _ = Task.Run(() =>
            {
                Dispatcher.Invoke(() =>
                {
                    // re-focus focused element when grid is updated
                    FrameworkElement elem = Keyboard.FocusedElement as FrameworkElement;
                    (sender as DataGrid).MoveFocus(new TraversalRequest(FocusNavigationDirection.Up));
                    elem?.Focus();
                },
                DispatcherPriority.ContextIdle);
            });
        }

        // mouse wheel scrolling fix
        // source - https://stackoverflow.com/a/4342746/12136394
        private void HandlePreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = true;
                MouseWheelEventArgs eventArg = new(e.MouseDevice, e.Timestamp, e.Delta);
                eventArg.RoutedEvent = MouseWheelEvent;
                eventArg.Source = sender;
                UIElement parent = ((Control)sender).Parent as UIElement;
                parent.RaiseEvent(eventArg);
            }
        }
    }
}
