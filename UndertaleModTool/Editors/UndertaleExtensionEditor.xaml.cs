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
using System.Windows.Navigation;
using System.Windows.Shapes;
using UndertaleModLib;
using UndertaleModLib.Models;
using WpfAnimatedGif;
using static UndertaleModLib.Models.UndertaleExtensionOption;

namespace UndertaleModTool
{
    /// <summary>
    /// Interaction logic for UndertaleExtensionEditor.xaml
    /// </summary>
    public partial class UndertaleExtensionEditor : DataUserControl
    {
        private static readonly MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

        public int MyIndex
        {
            get
            {
                if (DataContext is not UndertaleExtension ext)
                    return -1;
                
                return mainWindow.Data.Extensions.IndexOf(ext);
            }
        }
        // God this is so ugly, if there's a better way, please, put in a pull request
        public byte[] ProductIdData { get => ((((mainWindow.Data?.GeneralInfo?.Major ?? 0) >= 2) || (((mainWindow.Data?.GeneralInfo?.Major ?? 0) == 1) && (((mainWindow.Data?.GeneralInfo?.Build ?? 0) >= 1773) || ((mainWindow.Data?.GeneralInfo?.Build ?? 0) == 1539)))) ? mainWindow.Data.FORM.EXTN.productIdData[MyIndex] : null); set => mainWindow.Data.FORM.EXTN.productIdData[MyIndex] = value; }

        public UndertaleExtensionEditor()
        {
            InitializeComponent();

            ((Image)mainWindow.FindName("Flowey")).Opacity = 0;
            ((Image)mainWindow.FindName("FloweyLeave")).Opacity = 0;
            ((Image)mainWindow.FindName("FloweyBubble")).Opacity = 0;

            ((Label)this.FindName("ExtensionsObjectLabel")).Content = ((Label)mainWindow.FindName("ObjectLabel")).Content;
        }
        private void UndertaleExtensionsEditor_Unloaded(object sender, RoutedEventArgs e)
        {
            var floweranim = ((Image)mainWindow.FindName("Flowey"));
            //floweranim.Opacity = 1;

            var controller = ImageBehavior.GetAnimationController(floweranim);
            controller.Pause();
            controller.GotoFrame(controller.FrameCount - 5);
            controller.Play();

            ((Image)mainWindow.FindName("FloweyLeave")).Opacity = 0;
        }
        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            UndertaleExtension code = this.DataContext as UndertaleExtension;

            int foundIndex = code is UndertaleResource res ? mainWindow.Data.IndexOf(res, false) : -1;
            string idString;

            if (foundIndex == -1)
                idString = "None";
            else if (foundIndex == -2)
                idString = "N/A";
            else
                idString = Convert.ToString(foundIndex);

            ((Label)this.FindName("ExtensionsObjectLabel")).Content = idString;

            //((Image)mainWindow.FindName("FloweyBubble")).Opacity = 0;
            //((Image)mainWindow.FindName("Flowey")).Opacity = 0;
        }

        private void NewFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not UndertaleExtension extension)
                return;
            
            int lastItem = extension.Files.Count;

            UndertaleExtensionFile obj = new()
            {
                Kind = UndertaleExtensionKind.Dll,
                Filename = mainWindow.Data.Strings.MakeString($"NewExtensionFile{lastItem}.dll"),
                Functions = new UndertalePointerList<UndertaleExtensionFunction>()
            };
            extension.Files.Add(obj);
        }
        private void NewOptionButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not UndertaleExtension extension)
                return;
            
            int lastItem = extension.Options.Count;

            UndertaleExtensionOption obj = new()
            {
                Name = mainWindow.Data.Strings.MakeString($"extensionOption{lastItem}"),
                Value = mainWindow.Data.Strings.MakeString("", true)
            };
            extension.Options.Add(obj);
        }

        private void KindComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            var option = comboBox?.DataContext as UndertaleExtensionOption;
            if (option?.Value is null)
                return;
            
            switch (comboBox.SelectedItem)
            {
                case OptionKind.String:
                case null:
                    break;
                
                case OptionKind.Boolean:
                    if (option.Value.Content.ToLowerInvariant() == "true")
                        option.Value.Content = "True";
                    else
                        option.Value.Content = "False";
                    break;
                
                case OptionKind.Number:
                    if (!Double.TryParse(option.Value.Content, NumberStyles.Any, CultureInfo.InvariantCulture, out double _))
                        option.Value.Content = "0";
                    break;
            };
        }
    }

    public class OptionValueTemplateSelector : DataTemplateSelector
    {
        public DataTemplate StringTemplate { get; set; }
        public DataTemplate BooleanTemplate { get; set; }
        public DataTemplate NumberTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is not OptionKind kind)
                return null;

            return kind switch
            {
                OptionKind.String => StringTemplate,
                OptionKind.Boolean => BooleanTemplate,
                OptionKind.Number => NumberTemplate,
                _ => null
            };
        }
    }

    public class OptionValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string str)
                return null;
            
            switch (parameter)
            {
                case "boolean":
                    return str.ToLowerInvariant() == "true";
                
                case "number":
                    if (Double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out double _))
                        return str;
                    return "0";
                
                default:
                    return str;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is not string par)
                return null;
            
            switch (par)
            {
                case "boolean":
                    if (value is not bool b)
                        return new ValidationResult(false, "Invalid boolean value");
                    return (b ? "True" : "False");
                
                case "number":
                    if (value is string s && Double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out double _))
                        return s;
                    return new ValidationResult(false, "Invalid number string");
                
                default:
                    return null;
            }
        }
    }
}
