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
using UndertaleModLib.Decompiler;
using UndertaleModLib.Scripting;

namespace UndertaleModTool
{
    /// <summary>
    /// Logika interakcji dla klasy UndertaleObjectReference.xaml
    /// </summary>
    public partial class UndertaleObjectReference : UserControl
    {
        public static DependencyProperty ObjectReferenceProperty =
            DependencyProperty.Register("ObjectReference", typeof(object),
                typeof(UndertaleObjectReference),
                new FrameworkPropertyMetadata(null,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static DependencyProperty ObjectTypeProperty =
            DependencyProperty.Register("ObjectType", typeof(Type),
                typeof(UndertaleObjectReference));

        public static DependencyProperty CanRemoveProperty =
            DependencyProperty.Register("CanRemove", typeof(bool),
                typeof(UndertaleObjectReference),
                new FrameworkPropertyMetadata(true,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static DependencyProperty ObjectEventTypeProperty =
            DependencyProperty.Register("ObjectEventType", typeof(EventType),
                typeof(UndertaleObjectReference),
                new FrameworkPropertyMetadata(EventType.Create,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static DependencyProperty ObjectEventSubtypeProperty =
            DependencyProperty.Register("ObjectEventSubtype", typeof(uint),
                typeof(UndertaleObjectReference),
                new FrameworkPropertyMetadata((uint) 0,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));


        public object ObjectReference
        {
            get { return GetValue(ObjectReferenceProperty); }
            set { SetValue(ObjectReferenceProperty, value); }
        }

        public Type ObjectType
        {
            get { return (Type)GetValue(ObjectTypeProperty); }
            set { SetValue(ObjectTypeProperty, value); }
        }

        public bool CanRemove
        {
            get { return (bool)GetValue(ObjectTypeProperty); }
            set { SetValue(ObjectTypeProperty, value); }
        }

        public EventType ObjectEventType
        {
            get { return (EventType)GetValue(ObjectEventTypeProperty); }
            set { SetValue(ObjectEventTypeProperty, value); }
        }

        public uint ObjectEventSubtype
        {
            get { return (uint)GetValue(ObjectEventSubtypeProperty); }
            set { SetValue(ObjectEventSubtypeProperty, value); }
        }


        public UndertaleObjectReference()
        {
            InitializeComponent();
        }

        public void ClearRemoveClickHandler()
        {
            RemoveButton.Click -= Remove_Click;
        }

        private void Details_Click(object sender, RoutedEventArgs e)
        {
            if (ObjectReference is null)
            {
                OwnedMessageBox.Show("This feature is very WIP, so expect it to be broken.");

                MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

                if (mainWindow.Selected is null)
                {
                    MainWindow.ShowError("Nothing currently selected! This is currently unsupported.");
                    return;
                }
                else if (mainWindow.Selected is UndertaleGameObject gameObject)
                {
                    // Generate the code entry
                    UndertaleCode code = gameObject.EventHandlerFor(ObjectEventType, ObjectEventSubtype, mainWindow.Data.Strings, mainWindow.Data.Code, mainWindow.Data.CodeLocals);

                    ObjectReference = code;
                }
                else
                {
                    MainWindow.ShowError("Adding to non-objects is currently unsupported.");
                    return;
                }
            }
            else
            {
                (Application.Current.MainWindow as MainWindow).ChangeSelection(ObjectReference);
            }
        }

        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            ObjectReference = null;
        }

        private void TextBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ObjectReference != null)
            {
                (Application.Current.MainWindow as MainWindow).ChangeSelection(ObjectReference);
            }
        }

        private void TextBox_DragOver(object sender, DragEventArgs e)
        {
            UndertaleObject sourceItem = e.Data.GetData(e.Data.GetFormats()[0]) as UndertaleObject;

            e.Effects = e.AllowedEffects.HasFlag(DragDropEffects.Link) && sourceItem != null && sourceItem.GetType() == ObjectType ? DragDropEffects.Link : DragDropEffects.None;
            e.Handled = true;
        }

        private void TextBox_Drop(object sender, DragEventArgs e)
        {
            UndertaleObject sourceItem = e.Data.GetData(e.Data.GetFormats()[0]) as UndertaleObject;

            e.Effects = e.AllowedEffects.HasFlag(DragDropEffects.Link) && sourceItem != null && sourceItem.GetType() == ObjectType ? DragDropEffects.Link : DragDropEffects.None;
            if (e.Effects == DragDropEffects.Link)
            {
                ObjectReference = sourceItem;
            }
            e.Handled = true;
        }
    }
}
