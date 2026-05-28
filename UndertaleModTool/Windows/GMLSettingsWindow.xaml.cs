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
using UndertaleModTool.Localization;

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

        private void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            Settings.Instance.DecompilerSettings.RestoreDefaults();
            Settings.Instance.InstanceIdPrefix = Settings.DefaultInstanceIdPrefix;

            // Force all bindings to be updated
            DataContext = null;
            DataContext = Settings.Instance;
        }
    }

    [ValueConversion(typeof(DecompilerSettings.IndentStyleKind), typeof(string))]
    public class IndentStyleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not DecompilerSettings.IndentStyleKind kind)
            {
                return null;
            }
            return kind switch
            {
                DecompilerSettings.IndentStyleKind.FourSpaces => LocalizationSource.GetString("Editor_4Spaces"),
                DecompilerSettings.IndentStyleKind.TwoSpaces => LocalizationSource.GetString("Editor_2Spaces"),
                DecompilerSettings.IndentStyleKind.Tabs => LocalizationSource.GetString("Editor_Tabs"),
                _ => throw new Exception(LocalizationSource.GetString("Msg_UnknownIndentStyle"))
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
