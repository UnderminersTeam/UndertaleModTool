using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
using UndertaleModLib.Scripting;
using UndertaleModTool.Windows;

namespace UndertaleModTool
{
    /// <summary>
    /// Logika interakcji dla klasy UndertaleObjectReference.xaml
    /// </summary>
    public partial class UndertaleObjectReference : UserControl
    {
        private static readonly MainWindow mainWindow = Application.Current.MainWindow as MainWindow;
        private static readonly Regex camelCaseRegex = new("(?<=[a-z])([A-Z])", RegexOptions.Compiled);
        private static readonly char[] vowels = { 'a', 'o', 'u', 'e', 'i', 'y' };

        public event EventHandler<ObjectReferenceChangedEventArgs> ObjectReferenceChanged;

        public class ObjectReferenceChangedEventArgs : EventArgs
        {
            private object OldObject { get; }
            private object NewObject { get; }

            public ObjectReferenceChangedEventArgs(object oldObj, object newObj)
            {
                OldObject = oldObj;
                NewObject = newObj;
            }
        }

        public static DependencyProperty ObjectReferenceProperty =
            DependencyProperty.Register("ObjectReference", typeof(object),
                typeof(UndertaleObjectReference),
                new FrameworkPropertyMetadata(null,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (sender, e) =>
                    {
                        var inst = sender as UndertaleObjectReference;
                        if (inst is null)
                            return;

                        if (e.NewValue is not null)
                        {
                            try
                            {
                                if (inst.Resources["contextMenu"] is not ContextMenu menu)
                                    return;

                                menu.DataContext = inst.ObjectReference;
                                inst.ObjectText.ContextMenu = menu;
                            }
                            catch { }
                        }
                        else
                            inst.ObjectText.ContextMenu = null;
                    }));

        public static DependencyProperty ObjectTypeProperty =
            DependencyProperty.Register("ObjectType", typeof(Type),
                typeof(UndertaleObjectReference));

        public static DependencyProperty CanRemoveProperty =
            DependencyProperty.Register("CanRemove", typeof(bool),
                typeof(UndertaleObjectReference),
                new FrameworkPropertyMetadata(true,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
					
        public static DependencyProperty CanChangeProperty =
            DependencyProperty.Register("CanChange", typeof(bool),
                typeof(UndertaleObjectReference),
                new FrameworkPropertyMetadata(true,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
				

        public static readonly DependencyProperty GameObjectProperty =
            DependencyProperty.Register("GameObject", typeof(UndertaleGameObject),
                typeof(UndertaleObjectReference),
                new PropertyMetadata(null));

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

        public static readonly DependencyProperty RoomProperty =
            DependencyProperty.Register("Room", typeof(UndertaleRoom),
                typeof(UndertaleObjectReference),
                new PropertyMetadata(null));

        public static readonly DependencyProperty RoomGameObjectProperty =
           DependencyProperty.Register("RoomGameObject", typeof(UndertaleRoom.GameObject),
               typeof(UndertaleObjectReference),
               new PropertyMetadata(null));

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
            get { return (bool)GetValue(CanRemoveProperty); }
            set { SetValue(CanRemoveProperty, value); }
        }

        public bool CanChange
        {
            get { return (bool)GetValue(CanChangeProperty); }
            set { SetValue(CanChangeProperty, value); }
        }

        public UndertaleGameObject GameObject
        {
            get { return (UndertaleGameObject)GetValue(GameObjectProperty); }
            set { SetValue(GameObjectProperty, value); }
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

        public UndertaleRoom Room
        {
            get { return (UndertaleRoom)GetValue(RoomProperty); }
            set { SetValue(RoomProperty, value); }
        }

        public UndertaleRoom.GameObject RoomGameObject
        {
            get { return (UndertaleRoom.GameObject)GetValue(RoomGameObjectProperty); }
            set { SetValue(RoomGameObjectProperty, value); }
        }

        public bool IsPreCreate { get; set; } = false;

        public UndertaleObjectReference()
        {
            InitializeComponent();
            Loaded += UndertaleObjectReference_Loaded;
        }
        private void UndertaleObjectReference_Loaded(object sender, RoutedEventArgs e)
        {
            if (ObjectType is null)
                return;

            var label = TryFindResource("emptyReferenceLabel") as Label;
            if (label is null)
                return;

            string typeName = ObjectType.ToString();
            string n = "";
            if (typeName.StartsWith("UndertaleModLib.Models.Undertale"))
            {
                // "UndertaleAudioGroup" -> "audio group"
                typeName = typeName["UndertaleModLib.Models.Undertale".Length..];
                typeName = camelCaseRegex.Replace(typeName, " $1").ToLowerInvariant();
            }
            // If the first letter is a vowel
            if (Array.IndexOf(vowels, typeName[0]) != -1)
                n = "n";

            if (CanChange)
                label.Content = $"(drag & drop a{n} {typeName})";
            else
                label.Content = $"(empty {typeName} reference)";
        }

        public void ClearRemoveClickHandler()
        {
            RemoveButton.Click -= Remove_Click;
        }

        private void Details_Click(object sender, RoutedEventArgs e)
        {
            if (ObjectReference is null)
            {
                object oldObj = ObjectReference;

                if (GameObject is not null)
                {
                    ObjectReference = GameObject.EventHandlerFor(ObjectEventType, ObjectEventSubtype, mainWindow.Data);
                }
                else if (Room is not null)
                {
                    if (RoomGameObject is null)
                    {
                        // Generate base name
                        string name = $"gml_Room_{Room.Name.Content}_Create";

                        // If code already exists, use it (otherwise create new code)
                        if (mainWindow.Data.Code.ByName(name) is UndertaleCode existing)
                        {
                            mainWindow.ShowWarning("Code entry for room already exists; reusing it.");
                            ObjectReference = existing;
                        }
                        else
                        {
                            ObjectReference = UndertaleCode.CreateEmptyEntry(mainWindow.Data, name);
                        }
                    }
                    else
                    {
                        // Generate base name
                        string beginning = $"gml_RoomCC_{Room.Name.Content}_{RoomGameObject.InstanceID}";
                        string suffix = !IsPreCreate ? "_Create" : "_PreCreate";
                        string name = beginning + suffix;

                        // Ensure no duplicate names (in case instance IDs change)
                        int i = 0;
                        while (mainWindow.Data.Code.ByName(name) is not null)
                        {
                            name = beginning + "_" + (i++).ToString() + suffix;
                        }

                        ObjectReference = UndertaleCode.CreateEmptyEntry(mainWindow.Data, name);
                    }
                }
                else
                {
                    mainWindow.ShowError("Adding not supported in this situation.");
                }

                if (oldObj != ObjectReference)
                {
                    ObjectReferenceChanged?.Invoke(this, new ObjectReferenceChangedEventArgs(oldObj, ObjectReference));
                }
            }
            else
            {
                mainWindow.ChangeSelection(ObjectReference);
            }
        }

        private void Details_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (ObjectReference is null)
                return;

            if (e.ChangedButton == MouseButton.Middle && e.ButtonState == MouseButtonState.Pressed)
                mainWindow.ChangeSelection(ObjectReference, true);
        }
        private void OpenInNewTabItem_Click(object sender, RoutedEventArgs e)
        {
            mainWindow.ChangeSelection(ObjectReference, true);
        }
        private void MenuItem_ContextMenuOpened(object sender, RoutedEventArgs e)
        {
            var menu = sender as ContextMenu;
            foreach (var item in menu.Items)
            {
                var menuItem = item as MenuItem;
                if ((menuItem.Header as string) == "Find all references")
                {
                    Type objType = menu.DataContext.GetType();
                    menuItem.Visibility = UndertaleResourceReferenceMap.IsTypeReferenceable(objType)
                                          ? Visibility.Visible : Visibility.Collapsed;

                    break;
                }
            }
        }
        private void FindAllReferencesItem_Click(object sender, RoutedEventArgs e)
        {
            var obj = (sender as FrameworkElement)?.DataContext;
            if (obj is not UndertaleResource res)
            {
                mainWindow.ShowError("The selected object is not an \"UndertaleResource\".");
                return;
            }

            FindReferencesTypesDialog dialog = null;
            try
            {
                dialog = new(res, mainWindow.Data);
                dialog.ShowDialog();
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

        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            ObjectReferenceChanged?.Invoke(this, new ObjectReferenceChangedEventArgs(ObjectReference, null));
            ObjectReference = null;
        }

        private void TextBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ObjectReference != null)
            {
                mainWindow.ChangeSelection(ObjectReference);
            }
        }

        private void TextBox_DragOver(object sender, DragEventArgs e)
        {
            UndertaleObject sourceItem = e.Data.GetData(e.Data.GetFormats()[0]) as UndertaleObject;

            e.Effects = e.AllowedEffects.HasFlag(DragDropEffects.Link) && sourceItem != null && CanChange && sourceItem.GetType() == ObjectType ? DragDropEffects.Link : DragDropEffects.None;
            e.Handled = true;
        }

        private void TextBox_Drop(object sender, DragEventArgs e)
        {
            UndertaleObject sourceItem = e.Data.GetData(e.Data.GetFormats()[0]) as UndertaleObject;

            e.Effects = e.AllowedEffects.HasFlag(DragDropEffects.Link) && sourceItem != null && CanChange && sourceItem.GetType() == ObjectType ? DragDropEffects.Link : DragDropEffects.None;
            if (e.Effects == DragDropEffects.Link)
            {
                ObjectReferenceChanged?.Invoke(this, new ObjectReferenceChangedEventArgs(ObjectReference, sourceItem));
                ObjectReference = sourceItem;
            }
            e.Handled = true;
        }
    }
}
