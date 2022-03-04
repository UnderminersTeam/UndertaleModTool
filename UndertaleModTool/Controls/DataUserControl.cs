using System.Windows;

namespace UndertaleModTool
{
    public partial class DataUserControl : System.Windows.Controls.UserControl
    {
        public DataUserControl() 
        {
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            // prevent WPF binding errors (and unnessecary "DataContextChanged" firing) when switching to incompatible data type
            if (e.NewValue is null && e.Property == DataContextProperty)
                return;

            base.OnPropertyChanged(e);
        }
    }
}
