using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace UndertaleModTool
{
    /// <summary>
    /// Interaction logic for FlagsBox.xaml
    /// </summary>
    public partial class FlagsBox : UserControl
    {
        public object Value
        {
            get { return (object)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(object),
                typeof(FlagsBox),
                new FrameworkPropertyMetadata(null,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public FlagsBox()
        {
            InitializeComponent();
        }
    }

    public class EnumToValuesConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null)
            {
                return Array.Empty<string>();
            }
            return Enum.GetValues(value.GetType());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class EnumFlagToBoolConverter : IMultiValueConverter
    {
        dynamic enumValue;
        dynamic flag;

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Any(x => x == DependencyProperty.UnsetValue))
                return DependencyProperty.UnsetValue;

            enumValue = (Enum)values[0];
            flag = (Enum)values[1];

            return enumValue.HasFlag(flag);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            if ((bool)value)
            {
                enumValue |= flag;
            }
            else
            {
                enumValue &= ~flag;
            }
            return new object[] { enumValue, flag };
        }
    }
}
