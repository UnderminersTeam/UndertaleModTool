using System;
using System.Globalization;
using System.Windows.Data;
using static UndertaleModLib.Models.UndertaleRoom;

namespace UndertaleModTool
{
    public sealed class GameObjectToStringConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is not GameObject gameObject)
            {
                return "(null)";
            }
            return gameObject.ToString();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
