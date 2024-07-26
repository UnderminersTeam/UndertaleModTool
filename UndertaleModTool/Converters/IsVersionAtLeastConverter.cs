using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;

namespace UndertaleModTool
{
    public class IsVersionAtLeastConverter : IValueConverter
    {
        private static readonly MainWindow mainWindow = Application.Current.MainWindow as MainWindow;
        private static readonly Regex versionRegex = new(@"(\d+)\.(\d+)(?:\.(\d+))?(?:\.(\d+))?", RegexOptions.Compiled);

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (mainWindow.Data?.GeneralInfo is null
                || parameter is not string verStr
                || verStr.Length == 0)
                return Visibility.Hidden;

            var ver = versionRegex.Match(verStr);
            if (!ver.Success)
                return Visibility.Hidden;
            try
            {
                uint major = uint.Parse(ver.Groups[1].Value);
                uint minor = uint.Parse(ver.Groups[2].Value);
                uint release = 0;
                uint build = 0;
                if (ver.Groups[3].Value != "")
                    release = uint.Parse(ver.Groups[3].Value);
                if (ver.Groups[4].Value != "")
                    release = uint.Parse(ver.Groups[4].Value);

                if (mainWindow.Data.IsVersionAtLeast(major, minor, release, build))
                    return Visibility.Visible;
                else
                    return Visibility.Collapsed;
            }
            catch
            {
                return Visibility.Hidden;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
