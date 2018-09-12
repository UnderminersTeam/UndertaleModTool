using System;
using System.Collections.Generic;
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
using UndertaleModLib.Models;

namespace UndertaleModTool
{
    /// <summary>
    /// Logika interakcji dla klasy UndertalePathEditor.xaml
    /// </summary>
    public partial class UndertalePathEditor : UserControl
    {
        public UndertalePathEditor()
        {
            InitializeComponent();
        }

        public class LineData
        {
            public Point From { get; set; }
            public Point To { get; set; }
        }
    }

    [ValueConversion(typeof(UndertalePath), typeof(List<UndertalePathEditor.LineData>))]
    public class PointsDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            UndertalePath path = value as UndertalePath;
            if (path == null)
                return null;

            List<UndertalePathEditor.LineData> target = new List<UndertalePathEditor.LineData>();

            Point boundingLow = new Point(Double.PositiveInfinity, Double.PositiveInfinity);
            Point boundingHigh = new Point(Double.NegativeInfinity, Double.NegativeInfinity);
            
            foreach(var point in path.Points)
            {
                if (point.X < boundingLow.X)
                    boundingLow.X = point.X;
                if (point.Y < boundingLow.Y)
                    boundingLow.Y = point.Y;
                if (point.X > boundingHigh.X)
                    boundingHigh.X = point.X;
                if (point.Y > boundingHigh.Y)
                    boundingHigh.Y = point.Y;
            }

            for(int i = 0; i < path.Points.Count-1; i++)
            {
                target.Add(new UndertalePathEditor.LineData()
                {
                    From = ConvertPoint(boundingLow, boundingHigh, new Point(path.Points[i].X, path.Points[i].Y)),
                    To = ConvertPoint(boundingLow, boundingHigh, new Point(path.Points[i+1].X, path.Points[i+1].Y))
                });
            }
            if (path.IsClosed && path.Points.Count > 0)
            {
                target.Add(new UndertalePathEditor.LineData()
                {
                    From = ConvertPoint(boundingLow, boundingHigh, new Point(path.Points[path.Points.Count-1].X, path.Points[path.Points.Count - 1].Y)),
                    To = ConvertPoint(boundingLow, boundingHigh, new Point(path.Points[0].X, path.Points[0].Y))
                });
            }

            return target;
        }

        private Point ConvertPoint(Point boundingLow, Point boundingHigh, Point p)
        {
            double scaleX = (boundingHigh.X - boundingLow.X);
            double scaleY = (boundingHigh.Y - boundingLow.Y);
            double scale = Math.Max(scaleX, scaleY);
            return new Point(
                (p.X - boundingLow.X) / scale * 300,
                (p.Y - boundingLow.Y) / scale * 300
                );
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
