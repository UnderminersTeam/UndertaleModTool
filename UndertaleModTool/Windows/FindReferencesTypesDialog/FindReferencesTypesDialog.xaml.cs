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
using UndertaleModLib.Models;
using UndertaleModTool.Localization;

namespace UndertaleModTool.Windows
{
    /// <summary>
    /// Interaction logic for FindReferencesTypesDialog.xaml
    /// </summary>
    public partial class FindReferencesTypesDialog : Window
    {
        private static readonly MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

        private readonly UndertaleResource sourceObj;
        private readonly UndertaleData data;
        private readonly bool dontShowWindow = false;

        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!IsVisible || IsLoaded)
                return;

            if (dontShowWindow)
            {
                Close();
                return;
            }

            if (Settings.Instance.EnableDarkMode)
                MainWindow.SetDarkTitleBarForWindow(this, true, false);
        }

        public FindReferencesTypesDialog(UndertaleResource obj, UndertaleData data)
        {
            InitializeComponent();

            if (data.GeneralInfo is null)
            {
                this.ShowError(LocalizationSource.GetString("Msg_GeneralInfoNull"));
                dontShowWindow = true;
                return;
            }

            (Type, string)[] sourceTypes = UndertaleResourceReferenceMap.GetTypeMapForVersion(obj.GetType(), data);
            if (sourceTypes is null)
            {
                this.ShowError(string.Format(LocalizationSource.GetString("Msg_CannotGetSourceTypes"), obj.GetType()));
                dontShowWindow = true;
                return;
            }

            foreach (var typePair in sourceTypes)
            {
                TypesList.Items.Add(new CheckBox()
                {
                    DataContext = typePair.Item1,
                    Content = typePair.Item2,
                    IsChecked = true
                });
            }

            sourceObj = obj;
            this.data = data;
        }
        public FindReferencesTypesDialog(UndertaleData data)
        {
            InitializeComponent();

            if (data.GeneralInfo is null)
            {
                this.ShowError(LocalizationSource.GetString("Msg_GeneralInfoNull"));
                dontShowWindow = true;
                return;
            }

            var ver = (data.GeneralInfo.Major, data.GeneralInfo.Minor, data.GeneralInfo.Release);
            var sourceTypes = UndertaleResourceReferenceMap.GetReferenceableTypes(ver);

            foreach (var typePair in sourceTypes)
            {
                if (data.Code is null && UndertaleResourceReferenceMap.CodeTypes.Contains(typePair.Key))
                    continue;

                TypesList.Items.Add(new CheckBox()
                {
                    DataContext = typePair.Key,
                    Content = typePair.Value,
                    IsChecked = true
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

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (sourceObj is not null)
            {
                HashSetTypesOverride typesList = new();
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
                    this.ShowError(LocalizationSource.GetString("Msg_AtLeastOneTypeSelected"));
                    return;
                }

                FindReferencesResults dialog = null;
                try
                {
                    var results = UndertaleResourceReferenceMethodsMap.GetReferencesOfObject(sourceObj, data, typesList);
                    dialog = new(sourceObj, data, results);
                    dialog.Show();
                }
                catch (Exception ex)
                {
                    mainWindow.ShowError(string.Format(LocalizationSource.GetString("Msg_ObjectReferencesWindowError"), ex));
                    dialog?.Close();

                }

            }
            else
            {
                Dictionary<Type, string> typesDict = new();
                foreach (var item in TypesList.Items)
                {
                    if (item is CheckBox checkBox && checkBox.IsChecked == true)
                    {
                        if (checkBox.DataContext is Type t)
                            typesDict[t] = checkBox.Content as string;
                    }
                }

                if (typesDict.Count == 0)
                {
                    this.ShowError(LocalizationSource.GetString("Msg_AtLeastOneTypeSelected"));
                    return;
                }

                if (typesDict.Count > 1 && typesDict.ContainsKey(typeof(UndertaleString))
                    && data.Strings.Count > 5000)
                {
                    var res = this.ShowQuestion(LocalizationSource.GetString("Msg_ManyStringsSelectedWarning"));
                    if (res != MessageBoxResult.Yes)
                        return;
                }

                Hide();
                FindReferencesResults dialog = null;
                try
                {
                    var results = await UndertaleResourceReferenceMethodsMap.GetUnreferencedObjects(data, typesDict);
                    dialog = new(data, results);
                    dialog.Show();
                }
                catch (Exception ex)
                {
                    mainWindow.ShowError(string.Format(LocalizationSource.GetString("Msg_ObjectReferencesWindowError"), ex));
                    dialog?.Close();
                }
            }

            Close();
        }
    }
}
