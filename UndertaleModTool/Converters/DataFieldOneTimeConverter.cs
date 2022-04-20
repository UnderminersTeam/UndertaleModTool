using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using UndertaleModLib;

namespace UndertaleModTool
{
    public class DataFieldOneTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not UndertaleData data || parameter is not string par)
                return null;

            FieldInfo info = data.GetType().GetField(par);
            object resObj = info?.GetValue(data);

            if (resObj is bool res)
                return res ? Visibility.Visible : Visibility.Collapsed;
            else
                return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
