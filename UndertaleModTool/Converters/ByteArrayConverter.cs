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
    [ValueConversion(typeof(byte[]), typeof(string))]
    public sealed class ByteArrayConverter : IValueConverter
    {
        public byte[] loaded_for_edit;
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            loaded_for_edit = (byte[])value;
            return BitConverter.ToString((byte[])value).Replace("-", " ");
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                String hex = (String)value;
                String[] hex_values = hex.Split(" ");
                byte[] bytes = new byte[hex_values.Length];
                for (int i = 0; i < hex_values.Length; i += 1)
                    bytes[i] = System.Convert.ToByte(hex_values[i], 16);
                return bytes;
            }
            catch
            {
                return loaded_for_edit;
            }
        }
    }
}
