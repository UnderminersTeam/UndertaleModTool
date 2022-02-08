namespace UndertaleModTool
{
    public partial class DataUserControl : System.Windows.Controls.UserControl
    {
        public DataUserControl()
        {
            DataContextChanged += DataUserControl_DataContextChanged;
        }

        private void DataUserControl_DataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            // prevent WPF binding errors when switching to incompatible data type
            if (e.NewValue is null)
                DataContext = e.OldValue;
        }
    }
}
