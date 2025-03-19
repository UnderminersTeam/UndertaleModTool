using System;
using System.Collections.Generic;
using System.Globalization;
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
    /// Interaction logic for GMLSettingsWindow.xaml
    /// </summary>
    public partial class GMLSettingsWindow : Window
    {
        public GMLSettingsWindow(Settings settings)
        {
            DataContext = settings;
            InitializeComponent();
        }

        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!IsVisible || IsLoaded)
                return;

            if (Settings.Instance.EnableDarkMode)
                MainWindow.SetDarkTitleBarForWindow(this, true, false);
        }
    }

    [ValueConversion(typeof(DecompilerSettings.IndentStyleKind), typeof(string))]
    public class IndentStyleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch ((DecompilerSettings.IndentStyleKind)value)
            {
                case DecompilerSettings.IndentStyleKind.FourSpaces:
                    return "4 spaces";
                case DecompilerSettings.IndentStyleKind.TwoSpaces:
                    return "2 spaces";
                case DecompilerSettings.IndentStyleKind.Tabs:
                    return "Tabs";
            }
            throw new Exception("Unknown indent style kind");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
