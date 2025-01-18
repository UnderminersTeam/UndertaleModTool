using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using UndertaleModLib;
using UndertaleModTool.Windows;

namespace UndertaleModTool
{
    public partial class ResourceListTreeViewItem : TreeViewItem
    {
        // <summary>
        // TreeViewItem for representing UndertaleResources in the MainWindow data hirearchy
        // </summary>

        // We can't just use the stock ItemTemplate property, else our Selector will not run
        public static readonly DependencyProperty DefaultItemTemplateProperty = DependencyProperty.Register(
            "DefaultItemTemplate", typeof(DataTemplate), typeof(ResourceListTreeViewItem),
            new FrameworkPropertyMetadata(default(DataTemplate), FrameworkPropertyMetadataOptions.AffectsRender, OnDefaultItemTemplateChanged));

        // <summary>
        // Template to use if a resource is not null
        // </summary>
        [Bindable(true), Category("Content")]
        public DataTemplate DefaultItemTemplate
        {
            get { return (DataTemplate)GetValue(DefaultItemTemplateProperty); }
            set { SetValue(DefaultItemTemplateProperty, value); }
        }

        public static readonly DependencyProperty NullItemTemplateProperty = DependencyProperty.Register(
            "NullItemTemplate", typeof(DataTemplate), typeof(ResourceListTreeViewItem),
            new FrameworkPropertyMetadata(default(DataTemplate), FrameworkPropertyMetadataOptions.AffectsRender, OnNullItemTemplateChanged));

        private DataTemplate _defaultNullItemTemplateCache;

        // <summary>
        // Template to use if a resource is null
        // </summary>
        [Bindable(true), Category("Content")]
        public DataTemplate NullItemTemplate
        {
            get
            {
                // HACK: I don't want to implement the default template in code, so I'm getting it from
                // the resource dict; unfortunately it means I have to do this, since I can't just
                // specify it in the metadata
                _defaultNullItemTemplateCache ??= FindResource("DefaultNullItemTemplate") as DataTemplate;
                return (DataTemplate)GetValue(NullItemTemplateProperty) ?? _defaultNullItemTemplateCache;
            }
            set { SetValue(NullItemTemplateProperty, value); }
        }

        public ResourceListTreeViewItem()
        {
            InitializeComponent();

            ItemTemplateSelector = new NullConditionalDataTemplateSelector()
            {
                NonNullTemplate = DefaultItemTemplate,
                NullTemplate = NullItemTemplate
            };
        }

        private static void OnDefaultItemTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ResourceListTreeViewItem item = d as ResourceListTreeViewItem;
            if (item is not null)
                (item.ItemTemplateSelector as NullConditionalDataTemplateSelector).NonNullTemplate = item.DefaultItemTemplate;
        }

        private static void OnNullItemTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ResourceListTreeViewItem item = d as ResourceListTreeViewItem;
            if (item is not null)
                (item.ItemTemplateSelector as NullConditionalDataTemplateSelector).NullTemplate = item.NullItemTemplate;
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
}