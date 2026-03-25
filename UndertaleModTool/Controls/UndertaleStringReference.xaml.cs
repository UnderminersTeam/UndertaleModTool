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
using UndertaleModLib;
using UndertaleModLib.Models;
using UndertaleModTool.Windows;

namespace UndertaleModTool
{
    /// <summary>
    /// Logika interakcji dla klasy UndertaleStringReference.xaml
    /// </summary>
    public partial class UndertaleStringReference : UserControl
    {
        private static readonly MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

        public static DependencyProperty ObjectReferenceProperty =
            DependencyProperty.Register("ObjectReference", typeof(UndertaleString),
                typeof(UndertaleStringReference),
                new FrameworkPropertyMetadata(null,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (sender, e) =>
                    {
                        var inst = sender as UndertaleStringReference;
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

        public UndertaleString ObjectReference
        {
            get { return (UndertaleString)GetValue(ObjectReferenceProperty); }
            set { SetValue(ObjectReferenceProperty, value); }
        }

        public UndertaleStringReference()
        {
            InitializeComponent();
        }

        private void Details_Click(object sender, RoutedEventArgs e)
        {
            mainWindow.ChangeSelection(ObjectReference);
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
                if ((menuItem.Header as string) == UndertaleModTool.Resources.Strings.Find_all_references)
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
                mainWindow.ShowError(UndertaleModTool.Resources.Strings.The_selected_object_is_not_an___UndertaleResource);
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
            ObjectReference = null;
        }

        private void TextBox_DragOver(object sender, DragEventArgs e)
        {
            UndertaleString sourceItem = e.Data.GetData(e.Data.GetFormats()[0]) as UndertaleString;

            e.Effects = e.AllowedEffects.HasFlag(DragDropEffects.Link) && sourceItem != null ? DragDropEffects.Link : DragDropEffects.None;
            e.Handled = true;
        }

        private void TextBox_Drop(object sender, DragEventArgs e)
        {
            UndertaleString sourceItem = e.Data.GetData(e.Data.GetFormats()[0]) as UndertaleString;

            e.Effects = e.AllowedEffects.HasFlag(DragDropEffects.Link) && sourceItem != null ? DragDropEffects.Link : DragDropEffects.None;
            if (e.Effects == DragDropEffects.Link)
            {
                ObjectReference = sourceItem;
            }
            e.Handled = true;
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            var binding = BindingOperations.GetBindingExpression(tb, TextBox.TextProperty);
            if (binding.IsDirty)
            {
                if (ObjectReference != null)
                {
                    StringUpdateWindow dialog = new StringUpdateWindow();
                    dialog.Owner = Window.GetWindow(this);
                    dialog.ShowDialog();
                    switch (dialog.Result)
                    {
                        case StringUpdateWindow.ResultType.ChangeOneValue:
                            ObjectReference = (Application.Current.MainWindow as MainWindow).Data.Strings.MakeString(tb.Text);
                            break;
                        case StringUpdateWindow.ResultType.ChangeReferencedValue:
                            binding.UpdateSource();
                            break;
                        case StringUpdateWindow.ResultType.Cancel:
                            binding.UpdateTarget();
                            break;
                    }
                }
                else
                {
                    ObjectReference = (Application.Current.MainWindow as MainWindow).Data.Strings.MakeString(tb.Text);
                }
            }
        }
    }
}
