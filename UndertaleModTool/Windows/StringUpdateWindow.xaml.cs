using System;
using System.Collections.Generic;
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

namespace UndertaleModTool
{
    /// <summary>
    /// Logika interakcji dla klasy StringUpdateWindow.xaml
    /// </summary>
    public partial class StringUpdateWindow : Window
    {
        public enum ResultType
        {
            Cancel = 0,
            ChangeOneValue,
            ChangeReferencedValue
        }

        public ResultType Result { get; private set; } = ResultType.Cancel;

        public StringUpdateWindow()
        {
            InitializeComponent();
        }
        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!IsVisible || IsLoaded)
                return;

            if (Settings.Instance.EnableDarkMode)
                MainWindow.SetDarkTitleBarForWindow(this, true, false);
        }

        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            Result = ResultType.ChangeOneValue;
            Close();
        }

        private void Button2_Click(object sender, RoutedEventArgs e)
        {
            Result = ResultType.ChangeReferencedValue;
            Close();
        }

        private void Button3_Click(object sender, RoutedEventArgs e)
        {
            Result = ResultType.Cancel;
            Close();
        }
    }
}
