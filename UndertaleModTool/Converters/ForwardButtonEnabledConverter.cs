using System;
using System.Globalization;
using System.Windows.Data;

namespace UndertaleModTool
{
    public class ForwardButtonEnabledConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is not int pos || values[1] is not int count)
                return false;

            return pos != count - 1;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
