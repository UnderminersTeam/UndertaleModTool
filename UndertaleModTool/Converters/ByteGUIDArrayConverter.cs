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
    public sealed class ByteGUIDArrayConverter : IValueConverter
    {
        public byte[] loaded_for_edit = new byte[16];
        public byte[] reversedForDisplay = new byte[16];
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            loaded_for_edit = (byte[])value;
            reversedForDisplay[0] = loaded_for_edit[3];
            reversedForDisplay[1] = loaded_for_edit[2];
            reversedForDisplay[2] = loaded_for_edit[1];
            reversedForDisplay[3] = loaded_for_edit[0];
            reversedForDisplay[4] = loaded_for_edit[5];
            reversedForDisplay[5] = loaded_for_edit[4];
            reversedForDisplay[6] = loaded_for_edit[7];
            reversedForDisplay[7] = loaded_for_edit[6];
            for (var i = 8; i < 16; i++)
            {
                reversedForDisplay[i] = loaded_for_edit[i];
            }
            return BitConverter.ToString(reversedForDisplay).Replace("-", " ");
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                String hex = (String)value;
                String[] hex_values = hex.Split(" ");
                if (hex_values.Length != 16)
                    return loaded_for_edit;
                String[] hex_values_reversed = new string[16];
                hex_values_reversed[0] = hex_values[3];
                hex_values_reversed[1] = hex_values[2];
                hex_values_reversed[2] = hex_values[1];
                hex_values_reversed[3] = hex_values[0];
                hex_values_reversed[4] = hex_values[5];
                hex_values_reversed[5] = hex_values[4];
                hex_values_reversed[6] = hex_values[7];
                hex_values_reversed[7] = hex_values[6];
                for (var i = 8; i < 16; i++)
                {
                    hex_values_reversed[i] = hex_values[i];
                }
                byte[] bytes = new byte[hex_values_reversed.Length];
                for (int i = 0; i < hex_values_reversed.Length; i += 1)
                    bytes[i] = System.Convert.ToByte(hex_values_reversed[i], 16);
                return bytes;
            }
            catch
            {
                return loaded_for_edit;
            }
        }
    }
}
