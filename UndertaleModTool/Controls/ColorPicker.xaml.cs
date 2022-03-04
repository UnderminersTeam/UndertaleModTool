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

        public static DependencyProperty HasAlphaProperty =
            DependencyProperty.Register("HasAlpha", typeof(bool),
                typeof(ColorPicker),
                new FrameworkPropertyMetadata(true,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnHasAlphaChanged));

        public uint Color
        {
            get => (uint)GetValue(ColorProperty);
            set => SetValue(ColorProperty, value);
        }
        public bool HasAlpha
        {
            get => (bool)GetValue(HasAlphaProperty);
            set => SetValue(HasAlphaProperty, value); // we can't put here any other logic
        }

        public ColorPicker()
        {
            InitializeComponent();

            Binding binding = new("Color")
            {
                Converter = new ColorTextConverter(),
                ConverterParameter = HasAlpha.ToString(), // HasAlpha
                RelativeSource = new RelativeSource() { AncestorType = typeof(ColorPicker) },
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            ColorText.SetBinding(TextBox.TextProperty, binding);

            ColorText.MaxLength = HasAlpha ? 9 : 7;
            ColorText.ToolTip = $"{(HasAlpha ? "A, " : "")}B, G, R";
        }

        private static void OnHasAlphaChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            bool hasAlpha = (bool)e.NewValue;
            ColorPicker colorPicker = dependencyObject as ColorPicker;

            Binding binding = new("Color")
            {
                Converter = new ColorTextConverter(),
                ConverterParameter = hasAlpha.ToString(), // HasAlpha
                RelativeSource = new RelativeSource() { AncestorType = typeof(ColorPicker) },
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            colorPicker.ColorText.SetBinding(TextBox.TextProperty, binding);

            colorPicker.ColorText.MaxLength = hasAlpha ? 9 : 7;
            colorPicker.ColorText.ToolTip = $"{(hasAlpha ? "A, " : "")}B, G, R";
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
            bool hasAlpha = bool.Parse((string)parameter);
            return "#" + (hasAlpha ? val.ToString("X8") : val.ToString("X8")[2..]);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string val = (string)value;
            bool hasAlpha = bool.Parse((string)parameter);

            if (val[0] != '#')
                return new ValidationResult(false, "Invalid color string");

            val = val[1..];
            if (val.Length != (hasAlpha ? 8 : 6))
                return new ValidationResult(false, "Invalid color string");

            if (!hasAlpha)
                val = "FF" + val; // add alpha (255)

            try
            {
                return System.Convert.ToUInt32(val, 16);
            }
            catch (Exception e)
            {
                return new ValidationResult(false, e.Message);
            }
        }
    }
}
