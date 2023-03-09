using System.Windows;
using System.Windows.Controls.Primitives;

namespace UndertaleModTool
{
    /// <summary>
    /// A standard data grid which compatible with the dark mode.
    /// </summary>
    public partial class DataGridDark : System.Windows.Controls.DataGrid
    {
        /// <summary>Initializes a new instance of the data grid.</summary>
        public DataGridDark()
        {
            Loaded += DataGrid_Loaded;
        }

        private void DataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            var pres = MainWindow.FindVisualChild<DataGridColumnHeadersPresenter>(this);
            if (pres is null)
                return;

            pres.SetResourceReference(ForegroundProperty, "CustomTextBrush");
        }
    }
}
