using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
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
    [ValueConversion(typeof(byte[]), typeof(ImageSource))]
    public class UndertaleCachedImageLoader : IValueConverter
    {
        public static Dictionary<byte[], ImageSource> cache = new Dictionary<byte[], ImageSource>(); // TODO: weakref?
        private static readonly ImageSourceConverter imgSrcConverter = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            byte[] data = (byte[])value;
            if (!cache.ContainsKey(data))
            {
                cache.Add(data, imgSrcConverter.ConvertFrom(data) as ImageSource);
            }
            return cache[data];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
