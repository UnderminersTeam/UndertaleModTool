using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using UndertaleModLib.Models;

namespace UndertaleModTool
{
    [ValueConversion(typeof(uint), typeof(string))]
    public class EventNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            uint val = System.Convert.ToUInt32(value);
            return ((EventType)val).ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (uint)(EventType)Enum.Parse(typeof(EventType), (string)value);
        }
    }
}
