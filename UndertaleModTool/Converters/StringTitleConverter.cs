using System;
using System.Globalization;
using System.Windows.Data;

namespace UndertaleModTool
{
    public class StringTitleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string str)
                return null;

            if (str.Length > 256)
                str = str[..256] + "...";
            str = str.Replace('\n', ' ');

            return str;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
