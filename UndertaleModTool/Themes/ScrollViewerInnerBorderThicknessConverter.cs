using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Theme.WPF.Themes
{
    public class ScrollViewerInnerBorderThicknessConverter : IMultiValueConverter
    {
        public static ScrollViewerInnerBorderThicknessConverter Instance { get; } = new ScrollViewerInnerBorderThicknessConverter();

        public double Left { get; } = 0.0;
        public double Top { get; } = 0.0;
        public double RightVisible { get; } = 1.0;
        public double RightNotVisible { get; } = 0.0;
        public double BottomVisible { get; } = 1.0;
        public double BottomNotVisible { get; } = 0.0;

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length != 2)
            {
                throw new Exception("Need 2 values for this converter: bottom and right scroll bar visibility values");
            }

            if (!(values[0] is Visibility bottomBar))
                throw new Exception("Bottom bar value is not a visibility: " + values[0]);
            if (!(values[1] is Visibility rightBar))
                throw new Exception("Right bar value is not a visibility: " + values[1]);

            return new Thickness(
                this.Left,
                this.Top,
                rightBar == Visibility.Visible ? this.RightVisible : this.RightNotVisible,
                bottomBar == Visibility.Visible ? this.BottomVisible : this.BottomNotVisible);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new Exception("no");
        }
    }
}