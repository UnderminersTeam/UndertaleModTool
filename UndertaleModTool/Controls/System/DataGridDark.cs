using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;

namespace UndertaleModTool
{
    /// <summary>
    /// A standard data grid which compatible with the dark mode.
    /// </summary>
    public partial class DataGridDark : DataGrid
    {
        /// <summary>Initializes a new instance of the data grid.</summary>
        public DataGridDark()
        {
            Loaded += DataGrid_Loaded;
            AddingNewItem += DataGrid_AddingNewItem;
        }

        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] != DependencyProperty.UnsetValue)
                base.OnSelectionChanged(e);
            else
                System.Diagnostics.Debug.WriteLine("DataGridDark.OnSelectionChanged() - e.AddedItems[0] is \"UnsetValue\", skipping event handling.");
        }

        private void DataGrid_AddingNewItem(object sender, AddingNewItemEventArgs e)
        {
            _ = Task.Run(() =>
            {
                Dispatcher.Invoke(() =>
                {
                    UpdateLayout();
                    CommitEdit(DataGridEditingUnit.Row, true);
                });
            });
        }

        private void DataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            var pres = MainWindow.FindVisualChild<DataGridColumnHeadersPresenter>(this);
            if (pres is null)
                return;

            pres.SetResourceReference(ForegroundProperty, "CustomTextBrush");
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == VisibilityProperty)
            {
                if ((Visibility)e.NewValue == Visibility.Visible)
                {
                    base.OnPropertyChanged(e);
                    UpdateLayout();

                    var pres = MainWindow.FindVisualChild<DataGridColumnHeadersPresenter>(this);
                    pres?.SetResourceReference(ForegroundProperty, "CustomTextBrush");

                    return;
                }
            }

            base.OnPropertyChanged(e);
        }
    }
}
