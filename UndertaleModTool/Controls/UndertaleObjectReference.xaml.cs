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

        public static DependencyProperty EventSuffixProperty =
            DependencyProperty.Register("EventSuffix", typeof(string),
                typeof(UndertaleObjectReference),
                new FrameworkPropertyMetadata("",
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

        public string EventSuffix
        {
            get { return (string)GetValue(EventSuffixProperty); }
            set { SetValue(EventSuffixProperty, value); }
        }

        public UndertaleObjectReference()
        {
            InitializeComponent();
        }

        private void Details_Click(object sender, RoutedEventArgs e)
        {
            if (ObjectReference is null)
            {
                MessageBox.Show("This feature is very WIP, so expect it to be broken.");

                MainWindow mainWindow = (Application.Current.MainWindow as MainWindow);

                if (mainWindow.Selected is null)
                {
                    MessageBox.Show("Nothing currently selected! This is currently unsupported.");
                    return;
                }
                string name = (mainWindow.Selected as UndertaleNamedResource).Name.Content;

                string prefix = "gml_";

                switch (mainWindow.Selected)
                {
                    case UndertaleGameObject: prefix += "Object"; break;
                    case UndertaleRoom:       prefix += "RoomCC"; break;
                    case UndertaleScript:     prefix += "Script"; break;
                }

                prefix += "_";

                //MessageBox.Show(mainWindow.Selected.GetType().Name);


                // TEMPORARILY use ImportGMLString... this sucks.
                //mainWindow.ImportGMLString(prefix + name + "_" + EventSuffix + "_0", "", true, false);
                //UndertaleCode code = mainWindow.Data.Code.ByName(prefix + name + "_" + EventSuffix + "_0");

                //if (code is null)
                //{
                //    MessageBox.Show("This should never happen.");
                //    return;
                //}


                // Okay, INSTEAD of using ImportGMLString, let's do it ourselves!
                UndertaleCode code = new UndertaleCode();
                code.Name = mainWindow.Data.Strings.MakeString(prefix + name + "_" + EventSuffix + "_0");
                mainWindow.Data.Code.Add(code);
                if (mainWindow.Data?.GeneralInfo.BytecodeVersion > 14)
                {
                    UndertaleCodeLocals locals = new UndertaleCodeLocals();
                    locals.Name = code.Name;
                    UndertaleCodeLocals.LocalVar argsLocal = new UndertaleCodeLocals.LocalVar();
                    argsLocal.Name = mainWindow.Data.Strings.MakeString("arguments");
                    argsLocal.Index = 0;
                    locals.Locals.Add(argsLocal);
                    code.LocalsCount = 1;
                    code.GenerateLocalVarDefinitions(code.FindReferencedLocalVars(), locals);
                    mainWindow.Data.CodeLocals.Add(locals);
                }

                // TODO: This doesn't persist somehow?
                ObjectReference = code;
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
