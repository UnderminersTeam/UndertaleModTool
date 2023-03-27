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
    /// Interaction logic for FindReferencesTypesDialog.xaml
    /// </summary>
    public partial class FindReferencesTypesDialog : Window
    {
        private readonly UndertaleData data;

        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!IsVisible || IsLoaded)
                return;

            if (Settings.Instance.EnableDarkMode)
                MainWindow.SetDarkTitleBarForWindow(this, true, false);
        }

        public FindReferencesTypesDialog(UndertaleResource obj, UndertaleData data)
        {
            InitializeComponent();

            if (data.GeneralInfo is null)
            {
                this.ShowError("Cannot determine GameMaker version - \"General Info\" is null.");
                return;
            }

            var ver = (data.GeneralInfo.Major, data.GeneralInfo.Minor, data.GeneralInfo.Release);
            (Type, string)[] sourceTypes = UndertaleResourceReferenceMap.GetTypeMapForVersion(obj.GetType(), ver);
            if (sourceTypes is null)
            {
                this.ShowError($"Cannot get the source types for object of type \"{obj.GetType()}\".");
                return;
            }

            foreach (var typePair in sourceTypes)
            {
                TypesList.Items.Add(new CheckBox()
                {
                    DataContext = typePair.Item1,
                    Content = typePair.Item2
                });
            }

            this.data = data;
        }

        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in TypesList.Items)
            {
                if (item is CheckBox checkBox)
                    checkBox.IsChecked = true;
            }
        }
        private void DeselectAllButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in TypesList.Items)
            {
                if (item is CheckBox checkBox)
                    checkBox.IsChecked = false;
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            List<Type> typesList = new();
            foreach (var item in TypesList.Items)
            {
                if (item is CheckBox checkBox && checkBox.IsChecked == true)
                {
                    if (checkBox.DataContext is Type t)
                        typesList.Add(t);
                }
            }

            if (typesList.Count == 0)
            {
                this.ShowError("At least one type should be selected.");
                return;
            }



            Close();
        }
    }
}
