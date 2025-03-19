using System;
using System.Globalization;
using System.Windows.Data;

namespace UndertaleModTool
{
    public sealed class InvertBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is not bool boolean || !boolean;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is not bool boolean || !boolean;
        }
    }
}
