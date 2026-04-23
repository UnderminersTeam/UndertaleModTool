using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Shapes;
using UndertaleModLib;
using UndertaleModLib.Project;

namespace UndertaleModTool
{
    /// <summary>
    /// Interaction logic for ProjectAssetsWindow.xaml
    /// </summary>
    public partial class ProjectAssetsWindow : Window
    {
        private static readonly MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

        private bool _preventUpdateList = false;

        public readonly record struct UnexportedAsset(string Name, string AssetType, IProjectAsset ProjectAsset);

        public ProjectAssetsWindow()
        {
            InitializeComponent();

            if (mainWindow.Project is ProjectContext project)
            {
                UpdateList(project, new());
                project.UnexportedAssetsChanged += UpdateList;
            }
        }

        private void UpdateList(object sender, EventArgs e)
        {
            // If list is temporarily prevented from being updated, don't do anything
            if (_preventUpdateList)
            {
                return;
            }

            // Populate with current project assets
            List<UnexportedAsset> assets = ((ProjectContext)sender)
                .EnumerateUnexportedAssets()
                .Select((IProjectAsset asset) => new UnexportedAsset(asset.ProjectName, asset.ProjectAssetType.ToInterfaceName(), asset))
                .ToList();

            // Sort assets by type and name
            assets.Sort((a, b) =>
            {
                if (a.AssetType.CompareTo(b.AssetType) is int i && i != 0)
                {
                    return i;
                }
                if (a.Name.CompareTo(b.Name) is int j && j != 0)
                {
                    return j;
                }
                return 0;
            });

            // Update list view
            AssetsListView.ItemsSource = null;
            AssetsListView.ItemsSource = assets;
            AssetsListView.UpdateLayout();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (mainWindow.Project is ProjectContext project)
            {
                project.UnexportedAssetsChanged -= UpdateList;
            }
        }

        private void OpenSelectedListViewItem(bool inNewTab = false)
        {
            if (AssetsListView.SelectedItems is [UnexportedAsset asset, ..])
            {
                if (asset.ProjectAsset is not UndertaleObject obj)
                {
                    return;
                }

                if (!mainWindow.HasEditorForAsset(obj))
                {
                    this.ShowError("The type of this object doesn't have an editor/viewer.");
                    return;
                }

                mainWindow.Focus();
                mainWindow.ChangeSelection(obj, inNewTab);
            }
        }

        private void UnmarkSelectedListViewItemsForExport()
        {
            if (mainWindow.Project is ProjectContext projectContext)
            {
                _preventUpdateList = true;
                foreach (UnexportedAsset asset in AssetsListView.SelectedItems)
                {
                    projectContext.UnmarkAssetForExport(asset.ProjectAsset);
                }
                _preventUpdateList = false;
                UpdateList(projectContext, null);
            }
        }

        private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            OpenSelectedListViewItem();
        }

        private void ListViewItem_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                OpenSelectedListViewItem();
                e.Handled = true;
            }
            else if (e.Key == Key.Delete)
            {
                UnmarkSelectedListViewItemsForExport();
                e.Handled = true;
            }
        }

        private void MenuItemOpen_Click(object sender, RoutedEventArgs e)
        {
            OpenSelectedListViewItem();
            e.Handled = true;
        }

        private void MenuItemOpenInNewTab_Click(object sender, RoutedEventArgs e)
        {
            OpenSelectedListViewItem(true);
            e.Handled = true;
        }

        private void MenuItemUnmarkForExport_Click(object sender, RoutedEventArgs e)
        {
            UnmarkSelectedListViewItemsForExport();
            e.Handled = true;
        }

        private void Grid_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(e.Data.GetFormats()[^1]) is IProjectAsset { ProjectExportable: true })
            {
                e.Effects = DragDropEffects.Move;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }

            e.Handled = true;
        }

        private void Grid_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(e.Data.GetFormats()[^1]) is IProjectAsset projectAsset)
            {
                if (mainWindow.Project is ProjectContext project && project.MarkAssetForExport(projectAsset))
                {
                    e.Handled = true;
                }
            }
        }

        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!IsVisible || IsLoaded)
                return;

            if (Settings.Instance.EnableDarkMode)
                MainWindow.SetDarkTitleBarForWindow(this, true, false);
        }
    }
}
