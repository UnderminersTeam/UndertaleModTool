using System;
using System.Globalization;
using System.Windows.Data;

namespace UndertaleModTool
{
    public class NegateNumberConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double num;
            try
            {
                num = System.Convert.ToDouble(value);
            }
            catch
            {
                return null;
            }

            if (parameter is string par)
            {
                object res = null;

                try
                {
                    res = par switch
                    {
                        "sbyte" => (sbyte)num,
                        "short" => (short)num,
                        "int" => (int)num,
                        "long" => (long)num,
                        "float" => (float)num,
                        "decimal" => (decimal)num,
                        _ => null
                    };
                }
                catch { }

                return res; 
            }
            else
                return num;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
