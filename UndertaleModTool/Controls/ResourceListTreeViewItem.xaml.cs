using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using UndertaleModLib;
using UndertaleModTool.Windows;

namespace UndertaleModTool
{
    public partial class ResourceListTreeViewItem : TreeViewItem
    {
        public static readonly DependencyProperty DefaultTemplateProperty = DependencyProperty.Register(
            "DefaultTemplate", typeof(DataTemplate), typeof(ResourceListTreeViewItem),
            new FrameworkPropertyMetadata(default(DataTemplate), FrameworkPropertyMetadataOptions.AffectsRender, OnDefaultTemplateChanged));

        public DataTemplate DefaultTemplate
        {
            get { return (DataTemplate)GetValue(DefaultTemplateProperty); }
            set { SetValue(DefaultTemplateProperty, value); }
        }

        public ResourceListTreeViewItem()
        {
            InitializeComponent();

            Binding visibilityBinding = new("ItemsSource")
            {
                Converter = new NullToVisibilityConverter() {
                    nullValue = Visibility.Collapsed,
                    notNullValue = Visibility.Visible
                },
                RelativeSource = RelativeSource.Self,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            SetBinding(VisibilityProperty, visibilityBinding);

            ItemTemplateSelector = new ResourceItemTemplateSelector()
            {
                DefaultTemplate = DefaultTemplate,
                NullTemplate = FindResource("NullResourceItemTemplate") as DataTemplate
            };
        }

        private static void OnDefaultTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ResourceListTreeViewItem item = d as ResourceListTreeViewItem;
            if (item is not null)
                (item.ItemTemplateSelector as ResourceItemTemplateSelector).DefaultTemplate = item.DefaultTemplate;
        }

        private void MenuItem_ContextMenuOpened(object sender, RoutedEventArgs e)
        {
            var menu = sender as ContextMenu;
            foreach (var item in menu.Items)
            {
                var menuItem = item as MenuItem;
                if ((menuItem.Header as string) == "Find all references")
                {
                    menuItem.Visibility = UndertaleResourceReferenceMap.IsTypeReferenceable(menu.DataContext?.GetType())
                                          ? Visibility.Visible : Visibility.Collapsed;

                    break;
                }
            }
        }

        private void MenuItem_OpenInNewTab_Click(object sender, RoutedEventArgs e)
        {
            MainWindow window = (MainWindow)Application.Current.MainWindow;
            window.OpenInTab(window.Highlighted, true);
        }

        private void MenuItem_FindAllReferences_Click(object sender, RoutedEventArgs e)
        {
            MainWindow window = (MainWindow)Application.Current.MainWindow;
            var obj = (sender as FrameworkElement)?.DataContext as UndertaleResource;
            if (obj is null)
            {
                window.ShowError("The selected object is not an \"UndertaleResource\".");
                return;
            }

            FindReferencesTypesDialog dialog = null;
            try
            {
                dialog = new(obj, window.Data);
                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                window.ShowError("An error occured in the object references related window.\n" +
                               $"Please report this on GitHub.\n\n{ex}");
            }
            finally
            {
                dialog?.Close();
            }
        }

        private void MenuItem_CopyName_Click(object sender, RoutedEventArgs e)
        {
            MainWindow window = (MainWindow)Application.Current.MainWindow;
            window.CopyItemName(window.Highlighted);
        }

        private void MenuItem_Delete_Click(object sender, RoutedEventArgs e)
        {
            MainWindow window = (MainWindow)Application.Current.MainWindow;
            if (window.Highlighted is UndertaleObject obj)
                window.DeleteItem(obj);
        }
    }

    public class ResourceItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate DefaultTemplate { get; set; }
        public DataTemplate NullTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is null)
                return NullTemplate;
            return DefaultTemplate;
        }
    }
}