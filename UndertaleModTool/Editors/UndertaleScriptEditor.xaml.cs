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
using System.Windows.Navigation;
using System.Windows.Shapes;
using UndertaleModLib.Models;
using UndertaleModLib;

namespace UndertaleModTool
{
    /// <summary>
    /// Logika interakcji dla klasy UndertaleScriptEditor.xaml
    /// </summary>
    public partial class UndertaleScriptEditor : DataUserControl
    {
        private static readonly MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

        public UndertaleScriptEditor()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            Unloaded += OnUnloaded;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is UndertaleScript oldObj)
            {
                oldObj.PropertyChanged -= OnPropertyChanged;
            }
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is UndertaleScript oldObj)
            {
                oldObj.PropertyChanged -= OnPropertyChanged;
            }
            if (e.NewValue is UndertaleScript newObj)
            {
                newObj.PropertyChanged += OnPropertyChanged;
            }
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnAssetUpdated();
        }

        private void OnAssetUpdated()
        {
            if (mainWindow.Project is null || !mainWindow.IsSelectedProjectExportable)
            {
                return;
            }
            if (DataContext is UndertaleScript obj)
            {
                Dispatcher.BeginInvoke(() =>
                {
                    mainWindow.Project?.MarkAssetForExport(obj);
                });
            }
        }
    }
}
