using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Data;

namespace UndertaleModTool
{
    public class StringTitleConverter : IValueConverter
    {
        public static readonly Regex NewLineRegex = new(@"\r\n?|\n", RegexOptions.Compiled);
        public static StringTitleConverter Instance { get; } = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string str)
                return null;

            if (str.Length == 0)
                return "(empty string)";

            if (str.Length > 256)
                str = str[..256] + "...";
            str = NewLineRegex.Replace(str, " ");

            return str;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
