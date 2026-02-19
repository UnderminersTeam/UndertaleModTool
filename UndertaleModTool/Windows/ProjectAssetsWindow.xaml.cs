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
using UndertaleModLib.Project;

namespace UndertaleModTool
{
    /// <summary>
    /// Interaction logic for ProjectAssetsWindow.xaml
    /// </summary>
    public partial class ProjectAssetsWindow : Window
    {
        private static readonly MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

        public readonly record struct UnexportedAsset(string Name, string AssetType);

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
            // Populate with current project assets
            List<UnexportedAsset> assets = ((ProjectContext)sender)
                .EnumerateUnexportedAssets()
                .Select((IProjectAsset asset) => new UnexportedAsset(asset.ProjectName, asset.ProjectAssetType.ToInterfaceName()))
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

        void OpenSelectedListViewItem(bool inNewTab = false)
        {
            foreach (UnexportedAsset asset in AssetsListView.SelectedItems)
            {
                // TODO
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
        }

        private void MenuItemOpen_Click(object sender, RoutedEventArgs e)
        {
            OpenSelectedListViewItem();
        }

        private void MenuItemOpenInNewTab_Click(object sender, RoutedEventArgs e)
        {
            OpenSelectedListViewItem(true);
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
