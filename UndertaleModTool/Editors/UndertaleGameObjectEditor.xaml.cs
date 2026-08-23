using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
using System.Windows.Threading;
using UndertaleModLib;
using UndertaleModLib.Models;
using UndertaleModTool.Windows;

namespace UndertaleModTool
{
    /// <summary>
    /// Logika interakcji dla klasy UndertaleGameObjectEditor.xaml
    /// </summary>
    public partial class UndertaleGameObjectEditor : DataUserControl
    {
        private static readonly MainWindow mainWindow = Application.Current.MainWindow as MainWindow;
        private static readonly HashSetTypesOverride gameObjType = new() { typeof(UndertaleGameObject) };
        private bool handleMouseScroll = true;
        private bool showingNoChildrenFoundMessage = false;

        public UndertaleGameObjectEditor()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            Unloaded += OnUnloaded;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is UndertaleGameObject oldObj)
            {
                oldObj.PropertyChanged -= OnPropertyChanged;
                if (oldObj.PhysicsVertices is UndertaleObservableList<UndertaleGameObject.UndertalePhysicsVertex> vertices)
                {
                    vertices.CollectionChanged -= DataGrid_CollectionChanged;
                }
            }
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is UndertaleGameObject oldObj)
            {
                oldObj.PropertyChanged -= OnPropertyChanged;
                if (oldObj.PhysicsVertices is UndertaleObservableList<UndertaleGameObject.UndertalePhysicsVertex> vertices)
                {
                    vertices.CollectionChanged -= DataGrid_CollectionChanged;
                }
            }
            if (e.NewValue is UndertaleGameObject newObj)
            {
                newObj.PropertyChanged += OnPropertyChanged;
                if (newObj.PhysicsVertices is UndertaleObservableList<UndertaleGameObject.UndertalePhysicsVertex> vertices)
                {
                    vertices.CollectionChanged += DataGrid_CollectionChanged;
                }
            }
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
                if (DataContext is UndertaleGameObject obj)
                {
                    mainWindow.Project?.MarkAssetForExport(obj);
                }
            });
        }

        private void DataGrid_AddingNewItem(object sender, AddingNewItemEventArgs e)
        {
            UndertaleGameObject.Event obj = new();
            obj.Actions.Add(new UndertaleGameObject.EventAction());
            e.NewItem = obj;
        }

        private void DataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            // Attach to collection changed events
            if (sender is not DataGrid dg || dg.ItemsSource is not ObservableCollection<UndertaleGameObject.Event> collection)
            {
                return;
            }
            collection.CollectionChanged += DataGrid_CollectionChanged;
        }

        private void DataGrid_Unloaded(object sender, RoutedEventArgs e)
        {
            // Detach to collection changed events
            if (sender is not DataGrid dg || dg.ItemsSource is not ObservableCollection<UndertaleGameObject.Event> collection)
            {
                return;
            }
            collection.CollectionChanged -= DataGrid_CollectionChanged;
        }

        private void DataGrid_Actions_Loaded(object sender, RoutedEventArgs e)
        {
            // Attach to collection changed events
            if (sender is not DataGrid dg || dg.ItemsSource is not ObservableCollection<UndertaleGameObject.EventAction> collection)
            {
                return;
            }
            collection.CollectionChanged += DataGrid_CollectionChanged;
        }

        private void DataGrid_Actions_Unloaded(object sender, RoutedEventArgs e)
        {
            // Detach to collection changed events
            if (sender is not DataGrid dg || dg.ItemsSource is not ObservableCollection<UndertaleGameObject.EventAction> collection)
            {
                return;
            }
            collection.CollectionChanged -= DataGrid_CollectionChanged;
        }

        private void DataGrid_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnAssetUpdated();
        }

        private void PhysicsVertex_ValueUpdated(object sender, DataTransferEventArgs e)
        {
            OnAssetUpdated();
        }

        private void UndertaleObjectReference_ObjectReferenceChanged_ActionCode(object sender, UndertaleObjectReference.ObjectReferenceChangedEventArgs e)
        {
            OnAssetUpdated();
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
            var comboBox = sender as ComboBox;

            comboBox.DropDownOpened -= ComboBox_DropDownOpened;
            comboBox.DropDownOpened += ComboBox_DropDownOpened;
            comboBox.DropDownClosed -= ComboBox_DropDownClosed;
            comboBox.DropDownClosed += ComboBox_DropDownClosed;
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
            var btn = sender as ButtonDark;
            var objRef = (btn?.Parent as Grid)?.Parent as UndertaleObjectReference;
            if (objRef is null)
                return;

            if (DataContext is not UndertaleGameObject obj)
                return;
            var evType = objRef.ObjectEventType;
            var evSubtype = objRef.ObjectEventSubtype;
            var action = (UndertaleGameObject.EventAction)btn.DataContext;
            var evList = obj.Events[(int)evType];

            var ev = evList.FirstOrDefault(x => x.EventSubtype == evSubtype);
            if (ev is null) return;
            ev.Actions.Remove(action);
            if (ev.Actions.Count <= 0)
            {
                evList.Remove(ev);
            }
        }

        private async void ShowChildrenButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not UndertaleGameObject gameObj)
                return;

            var button = sender as ButtonDark;

            FindReferencesTypesDialog dialog = new(mainWindow.Data);
            try
            {
                bool hasChildren = dialog.ShowReferencesFor(gameObj, gameObjType, showIfNoResults: false);
                if (!hasChildren && !showingNoChildrenFoundMessage)
                {
                    var originalCont = button.Content;

                    showingNoChildrenFoundMessage = true;
                    button.Content = "(no children were found)";
                    await Task.Delay(2000);

                    button.Content = originalCont;
                    showingNoChildrenFoundMessage = false;
                }
            }
            catch (Exception ex)
            {
                mainWindow.ShowError("An error occurred in the object references related window.\n" +
                                     $"Please report this on GitHub.\n\n{ex}");
            }
            finally
            {
                dialog?.Close();
            }
        }
    }
}
