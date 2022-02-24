using System;
using System.Linq;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace UndertaleModTool
{
    public class GridConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Any(x => x is not double))
                return new Rect();

            return new Rect(0, 0, (double)values[0], (double)values[1]);
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
