using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace UndertaleModTool
{
    /// <summary>
    /// Logika interakcji dla klasy ColorPicker.xaml
    /// </summary>
    public partial class ColorPicker : UserControl
    {
        public static DependencyProperty ColorProperty =
            DependencyProperty.Register("Color", typeof(uint),
                typeof(ColorPicker),
                new FrameworkPropertyMetadata(0xFFFFFFFF,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public uint Color
        {
            get { return (uint)GetValue(ColorProperty); }
            set { SetValue(ColorProperty, value); }
        }

        public ColorPicker()
        {
            InitializeComponent();
        }
    }

    [ValueConversion(typeof(uint), typeof(Color))]
    public class ColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            uint val = System.Convert.ToUInt32(value);
            return Color.FromArgb((byte)((val >> 24) & 0xff), (byte)(val & 0xff), (byte)((val >> 8) & 0xff), (byte)((val >> 16) & 0xff));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Color val = (Color)value;
            return (val.A << 24) | (val.B << 16) | (val.G << 8) | val.R;
        }
    }

    [ValueConversion(typeof(uint), typeof(string))]
    public class ColorTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            uint val = System.Convert.ToUInt32(value);
            return "#" + val.ToString("X8");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string val = (string)value;
            if (val[0] != '#')
                return new ValidationResult(false, "Invalid color string");
            val = val.Substring(1);
            if (val.Length != 8)
                return new ValidationResult(false, "Invalid color string");

            try
            {
                return System.Convert.ToUInt32(val, 16);
            }
            catch(Exception e)
            {
                return new ValidationResult(false, e.Message);
            }
        }
    }
}
