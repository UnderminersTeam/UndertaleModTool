using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace UndertaleModTool.Windows
{
    /// <summary>
    /// Interaction logic for FindReferencesResults.xaml
    /// </summary>
    public partial class FindReferencesResults : Window
    {
        private readonly UndertaleData data;

        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!IsVisible || IsLoaded)
                return;

            if (Settings.Instance.EnableDarkMode)
                MainWindow.SetDarkTitleBarForWindow(this, true, false);
        }

        public FindReferencesResults(UndertaleResource sourceObj, UndertaleResource[][] results, UndertaleData data)
        {
            InitializeComponent();

            string sourceObjName;
            if (sourceObj is UndertaleNamedResource namedObj)
                sourceObjName = namedObj.Name.Content;
            else
                sourceObjName = sourceObj.GetType().Name;

            Title = $"The references of game object \"{sourceObjName}\"";
            label.Text = $"The search results for the game object\n\"{sourceObjName}\".";

            this.data = data;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            foreach (var child in ResultsTree.Items)
                ((child as TreeViewItem).ItemsSource as ICollectionView)?.Refresh();
        }

        private void DoneButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
