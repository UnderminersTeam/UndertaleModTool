using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using UndertaleModLib.Models;

namespace UndertaleModTool
{
    public class MaskImageConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values.Any(e => e == DependencyProperty.UnsetValue))
            {
                return null;
            }
            
            int width = (int)values[0];
            int height = (int)values[1];
            byte[] data = (byte[])values[2];
            if (data == null || data.Length != (width + 7) / 8 * height || width <= 0 || height <= 0)
                return null;
            return BitmapSource.Create(width, height, 96, 96, PixelFormats.BlackWhite, null, data, ((width + 7) / 8));
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
