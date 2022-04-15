using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace UndertaleModTool
{
    public class CompareNumbersConverter : IMultiValueConverter
    {
        // these could be overriden on declaration
        public object TrueValue { get; set; } = Visibility.Visible;
        public object FalseValue { get; set; } = Visibility.Collapsed;

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            double a, b;
            try
            {
                a = (double)values[0];
                b = (double)values[1];
            }
            catch
            {
                return null;
            }

            if (parameter is string par)
            {
                int r;
                if (par == ">")      // greater than
                    r = 1;
                else if (par == "<") // less than
                    r = -1;
                else
                    return null;

                bool res = a.CompareTo(b) == r;
                return res ? TrueValue : FalseValue;
            }

            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
