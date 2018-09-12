using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using UndertaleModLib.Models;

namespace UndertaleModTool
{
    [ValueConversion(typeof(uint), typeof(UndertaleGameObject))]
    public class GameObjectByIdConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            uint val = System.Convert.ToUInt32(value);
            return (Application.Current.MainWindow as MainWindow).Data.GameObjects[(int)val];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (uint)(Application.Current.MainWindow as MainWindow).Data.GameObjects.IndexOf((UndertaleGameObject)value);
        }
    }
}
