﻿using NAudio.Wave;
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
using System.Xml.Linq;
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

        public static DependencyProperty ObjectEventTypeProperty =
            DependencyProperty.Register("ObjectEventType", typeof(EventType),
                typeof(UndertaleObjectReference),
                new FrameworkPropertyMetadata(EventType.Create,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static DependencyProperty ObjectEventSubtypeProperty =
            DependencyProperty.Register("ObjectEventSubtype", typeof(uint),
                typeof(UndertaleObjectReference),
                new FrameworkPropertyMetadata((uint)0,
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

            label.Content = $"(drag & drop a{n} {typeName})";
        }

        public void ClearRemoveClickHandler()
        {
            RemoveButton.Click -= Remove_Click;
        }

        private void Details_Click(object sender, RoutedEventArgs e)
        {
            if (ObjectReference is null)
            {
                if (mainWindow.Selected is null)
                {
                    mainWindow.ShowError("Nothing currently selected! This is currently unsupported.");
                    return;
                }
                else if (mainWindow.Selected is UndertaleGameObject gameObject)
                {
                    // Generate the code entry
                    UndertaleCode code = gameObject.EventHandlerFor(ObjectEventType, ObjectEventSubtype, mainWindow.Data.Strings, mainWindow.Data.Code, mainWindow.Data.CodeLocals);

                    ObjectReference = code;
                }

                else if (mainWindow.Selected is UndertaleRoom roomWindow)
                {
                    //get the name of the room and remove surrounding quotes
                    var roomName = roomWindow.Name.ToString();
                    roomName = roomName.Substring(1, roomName.Length - 2);
                    //grab the ObjectEditor
                    var roomEditor = MainWindow.FindVisualChild<UndertaleRoomEditor>((Application.Current.MainWindow as MainWindow).DataEditor);
                    var objectEditor = roomEditor?.ObjectEditor;
                    if (objectEditor is not null)
                    {
                        if (objectEditor.Content is UndertaleRoom room)
                        {
                            if (room != null)
                            {
                                AddRoomCreationCode(room, roomName, EventType.Create);
                            }
                        }
                        else
                        {
                            if (objectEditor.Content is not null)
                            {
                                UndertaleRoom.GameObject gameObj = objectEditor.Content as UndertaleRoom.GameObject;
                                if (ObjectEventType is EventType.Create)
                                {
                                    AddRoomCreationCode(gameObj, roomName, EventType.Create);
                                }
                                else if (ObjectEventType is EventType.PreCreate)
                                {
                                    AddRoomCreationCode(gameObj, roomName, EventType.PreCreate);
                                }
                            }
                            else
                                mainWindow.ShowError("Content is of unknown type.");
                        }
                    }
                    else
                        mainWindow.ShowError("Null Object Editor");
                }

                else
                {
                    mainWindow.ShowError("Adding to non-objects is currently unsupported.");
                    return;
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
                mainWindow.ShowError("An error occured in the object references related window.\n" +
                                     $"Please report this on GitHub.\n\n{ex}");
            }
            finally
            {
                dialog?.Close();
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
                mainWindow.ChangeSelection(ObjectReference);
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
        private void AddRoomCreationCode(object sender, string nameInsert, EventType eventType)
        {
            var code = new UndertaleCode()
            {
                LocalsCount = 1
            };
            mainWindow.Data.Code.Add(code);
            UndertaleCodeLocals locals = new UndertaleCodeLocals();
            UndertaleCodeLocals.LocalVar argsLocal = new UndertaleCodeLocals.LocalVar();
            argsLocal.Name = mainWindow.Data.Strings.MakeString("arguments");
            argsLocal.Index = 0;
            locals.Locals.Add(argsLocal);
            mainWindow.Data.CodeLocals.Add(locals);
            if (sender is UndertaleRoom room)
            {
                code.Name = mainWindow.Data.Strings.MakeString("gml_RoomCC_" + nameInsert + "_Create");
                locals.Name = code.Name;
                room.CreationCodeId = code;
            }
            else if (sender is UndertaleRoom.GameObject gameObject)
            {
                if (eventType == EventType.Create)
                {
                    code.Name = mainWindow.Data.Strings.MakeString("gml_RoomCC_" + nameInsert + "_" + gameObject.InstanceID.ToString() + "_Create");
                    locals.Name = code.Name;
                    gameObject.CreationCode = code;
                }
                //this could probably be handled in one case with a ternary operator
                else if (eventType == EventType.PreCreate)
                {
                    code.Name = mainWindow.Data.Strings.MakeString("gml_RoomCC_" + nameInsert + "_" + gameObject.InstanceID.ToString() + "_PreCreate");
                    locals.Name = code.Name;
                    gameObject.PreCreateCode = code;
                }
            }
            else
            {
                mainWindow.ShowError("Adding creation to this object is currently unsupported.");
                return;
            }
            
        }
    }
}
