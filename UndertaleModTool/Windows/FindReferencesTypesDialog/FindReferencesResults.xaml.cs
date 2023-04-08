using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModTool.Windows
{
    /// <summary>
    /// Interaction logic for FindReferencesResults.xaml
    /// </summary>
    public partial class FindReferencesResults : Window
    {
        private static readonly MainWindow mainWindow = Application.Current.MainWindow as MainWindow;
        private object highlighted;
        private string sourceObjName;
        private readonly UndertaleData data;

        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!IsVisible || IsLoaded)
                return;

            if (Settings.Instance.EnableDarkMode)
                MainWindow.SetDarkTitleBarForWindow(this, true, false);
        }

        public FindReferencesResults(object sourceObj, UndertaleData data, Dictionary<string, List<object>> results)
        {
            InitializeComponent();

            this.data = data;

            string sourceObjName;
            if (sourceObj is UndertaleNamedResource namedObj)
                sourceObjName = namedObj.Name.Content;
            else if (sourceObj is UndertaleString str)
                sourceObjName = str.Content;
            else if (sourceObj is ValueTuple<UndertaleBackground, UndertaleBackground.TileID> tileTuple)
                sourceObjName = $"Tile {tileTuple.Item2} of {tileTuple.Item1.Name.Content}";
            else
                sourceObjName = sourceObj.GetType().Name;
            this.sourceObjName = sourceObjName;

            Title = $"The references of game asset \"{sourceObjName}\"";
            label.Text = $"The search results for the game asset\n\"{sourceObjName}\".";

            if (results is null)
                ResultsTree.Background = new VisualBrush(new Label()
                {
                    Content = "No references found.",
                    FontSize = 16
                }) { Stretch = Stretch.None };
            else
                ProcessResults(results);
        }
        public FindReferencesResults(UndertaleData data, Dictionary<string, List<object>> results)
        {
            InitializeComponent();

            this.data = data;

            Title = "The unreferenced game assets";
            label.Text = "The search results for the unreferenced game assets.";

            if (results is null)
                ResultsTree.Background = new VisualBrush(new Label()
                {
                    Content = "No unreferenced assets found.",
                    FontSize = 16
                })
                { Stretch = Stretch.None };
            else
                ProcessResults(results);
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            foreach (var child in ResultsTree.Items)
                ((child as TreeViewItem)?.ItemsSource as ICollectionView)?.Refresh();
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            ExportResults();
        }
        private void DoneButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ProcessResults(Dictionary<string, List<object>> results)
        {
            var filterConv = new FilteredViewConverter();
            BindingOperations.SetBinding(filterConv, FilteredViewConverter.FilterProperty, new Binding("Text")
            {
                Source = SearchBox,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });

            var namedResTemplate = XamlReader.Parse(
            @"
                <HierarchicalDataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
                    <TextBlock Text='{Binding Name.Content}'/>
                </HierarchicalDataTemplate>
            ") as HierarchicalDataTemplate;

            foreach (var result in results)
            {
                var item = new TreeViewItem()
                {
                    Header = result.Key,
                    DataContext = result.Value
                };
                item.SetBinding(TreeView.ItemsSourceProperty, new Binding(".")
                {
                    Converter = filterConv,
                    Mode = BindingMode.OneWay
                });
                if (result.Value[0] is UndertaleNamedResource)
                    item.ItemTemplate = namedResTemplate;
                else if (result.Value[0] is UndertaleString)
                    item.ItemTemplate = TryFindResource("StringTemplate") as HierarchicalDataTemplate;
                else if (result.Value[0] is GeneralInfoEditor or GlobalInitEditor or GameEndEditor)
                {
                    ResultsTree.Items.Add(new TextBlock()
                    {
                        Text = result.Key,
                        DataContext = result.Value[0],
                        ContextMenu = TryFindResource("StandaloneTabMenu") as ContextMenu
                    });
                    continue;
                }
                else if (result.Value[0] is object[])
                    item.ItemTemplate = TryFindResource("ChildInstTemplate") as HierarchicalDataTemplate;

                ResultsTree.Items.Add(item);

                item.IsExpanded = true;
            }
        }

        private void ExportResults()
        {
            if (data.FORM is null)
            {
                this.ShowError($"The object references are stale - a different game data was loaded.");
                return;
            }

            string initContent = Title + ":\n";
            initContent += new string('-', initContent.Length - 1) + "\n\n";
            StringBuilder sb = new(initContent);

            foreach (var item in ResultsTree.Items)
            {
                if (item is TreeViewItem treeItem)
                {
                    if (treeItem.Items.IsEmpty)
                        continue;

                    sb.AppendLine((treeItem.Header as string) + ':');

                    foreach (var childItem in treeItem.Items)
                    {
                        string itemName;
                        if (childItem is object[] inst)
                            itemName = ChildInstanceNameConverter.Instance.Convert(inst, null, null, null) as string;
                        else if (childItem is UndertaleNamedResource namedRes)
                            itemName = namedRes.Name?.Content;
                        else if (childItem is UndertaleString str)
                            itemName = StringTitleConverter.Instance.Convert(str.Content, null, null, null) as string;
                        else
                            itemName = childItem.ToString();

                        sb.AppendLine($"    {itemName}");
                    }

                    sb.Append("\n");
                }
                else if (item is TextBlock text)
                    sb.AppendLine(text.Text + "\n");
            }

            if (sb.Length == initContent.Length)
            {
                this.ShowError("No results to export.");
                return;
            }
            sb.Remove(sb.Length - 2, 2);

            if (sourceObjName is not null)
            {
                string invalidCharsRegex = '[' + String.Join("", Path.GetInvalidFileNameChars()) + ']';
                sourceObjName = Regex.Replace(sourceObjName, invalidCharsRegex, "_");
            }
                    
            string folderPath = Path.GetDirectoryName(mainWindow.FilePath);
            string filePath = Path.Combine(folderPath, sourceObjName is null
                                                       ? "unreferenced_assets.txt" : $"references_of_asset_{sourceObjName}.txt");
            if (File.Exists(filePath))
                if (this.ShowQuestion($"File \"{filePath}\" exists.\nOverwrite?") == MessageBoxResult.No)
                    return;

            File.WriteAllText(filePath, sb.ToString());
            this.ShowMessage($"The results were successfully saved at path\n\"{filePath}\".");
        }

        private void Open(object obj, bool inNewTab = false)
        {
            if (data.FORM is null)
            {
                this.ShowError($"The object reference is stale - a different game data was loaded.");
                return;
            }

            if (obj is object[] inst)
            {
                if (inst[^1] is UndertaleRoom room)
                {
                    mainWindow.Focus();

                    mainWindow.ChangeSelection(room, inNewTab);
                    mainWindow.CurrentTab.LastContentState = new RoomTabState()
                    {
                        SelectedObject = inst[0]
                    };
                    mainWindow.CurrentTab.RestoreTabContentState();
                }
                else
                {
                    if (!mainWindow.HasEditorForAsset(inst[^1]))
                    {
                        this.ShowError("The type of this object reference doesn't have an editor/viewer.");
                        return;
                    }
                }
            }
            else
            {
                if (!mainWindow.HasEditorForAsset(obj))
                {
                    this.ShowError("The type of this object reference doesn't have an editor/viewer.");
                    return;
                }

                mainWindow.Focus();

                mainWindow.ChangeSelection(obj, inNewTab);
            } 
        }

        private void MenuItem_ContextMenuOpened(object sender, RoutedEventArgs e)
        {
            var menu = sender as ContextMenu;
            foreach (var item in menu.Items)
            {
                var menuItem = item as MenuItem;
                if ((menuItem.Header as string) == "Find all references")
                {
                    Type objType = menu.DataContext is object[] inst
                                   ? inst[^1].GetType() : menu.DataContext.GetType();
                    menuItem.Visibility = UndertaleResourceReferenceMap.IsTypeReferenceable(objType)
                                          ? Visibility.Visible : Visibility.Collapsed;

                    break;
                }
            }
        }
        private void MenuItem_OpenInNewTab_Click(object sender, RoutedEventArgs e)
        {
            Open(highlighted, true);
        }
        private void MenuItem_CopyName_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item && item.DataContext is not null)
            {
                string itemName;
                if (item.DataContext is object[] inst)
                    itemName = ChildInstanceNameConverter.Instance.Convert(inst, null, null, null) as string;
                else if (item.DataContext is UndertaleNamedResource namedRes)
                    itemName = namedRes.Name?.Content;
                else if (item.DataContext is UndertaleString str)
                    itemName = StringTitleConverter.Instance.Convert(str.Content, null, null, null) as string;
                else
                    itemName = item.DataContext.ToString();

                Clipboard.SetText(itemName);
            }
        }
        private void MenuItem_FindAllReferences_Click(object sender, RoutedEventArgs e)
        {
            UndertaleResource res = null;

            var obj = (sender as FrameworkElement)?.DataContext;
            if (obj is UndertaleResource res1)
                res = res1;
            else if (obj is object[] inst && inst[^1] is UndertaleResource res2)
                res = res2;

            if (res is null)
            {
                this.ShowError("The selected object is not an \"UndertaleResource\".");
                return;
            }

            FindReferencesTypesDialog dialog = null;
            try
            {
                dialog = new(res, data);
                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                this.ShowError("An error occured in the object references related window.\n" +
                               $"Please report this on GitHub.\n\n{ex}");
            }
            finally
            {
                dialog?.Close();
            }
        }


        private void ResultsTree_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is not TextBlock textBlock)
                return;
            if (textBlock.DataContext is string)
                return;

            Open(highlighted);
        }
        private void ResultsTree_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                Open(highlighted);
        }
        private void ResultsTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TreeViewItem)
                return;
            if (e.NewValue is TextBlock block)
            {
                if (block.DataContext is GeneralInfoEditor or GlobalInitEditor or GameEndEditor)
                {
                    highlighted = block.DataContext;
                    return;
                }
            }

            highlighted = e.NewValue;
        }
        private void ResultsTree_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed && e.ChangedButton == MouseButton.Middle)
            {
                if (e.OriginalSource is not TextBlock textBlock)
                    return;

                TreeViewItem item = MainWindow.GetNearestParent<TreeViewItem>(textBlock);
                if (item is null)
                    return;

                item.IsSelected = true;

                if (item.DataContext is Array)
                    return;

                Open(highlighted, true);
            }
        }
        private void ResultsTree_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            TreeViewItem treeViewItem = MainWindow.VisualUpwardSearch<TreeViewItem>(e.OriginalSource as DependencyObject);

            if (treeViewItem != null)
            {
                treeViewItem.Focus();
                e.Handled = true;
            }
        }
    }

    public class ChildInstanceNameConverter : IValueConverter
    {
        public static ChildInstanceNameConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is object[] inst)
            {
                StringBuilder sb = new();
                for (int i = 0; i < inst.Length; i++)
                {
                    var link = inst[i];
                    if (link is UndertaleNamedResource namedObj)
                        sb.Append(namedObj.Name);
                    else
                        sb.Append(link.ToString());

                    if (i != inst.Length - 1)
                        sb.Append(" — ");
                }

                return sb.ToString();
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
