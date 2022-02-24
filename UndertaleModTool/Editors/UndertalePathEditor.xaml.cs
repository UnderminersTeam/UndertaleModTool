using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using UndertaleModLib.Models;

namespace UndertaleModTool
{
    /// <summary>
    /// Logika interakcji dla klasy UndertalePathEditor.xaml
    /// </summary>
    public partial class UndertalePathEditor : DataUserControl
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
    public class SimplePointsDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            UndertalePath path = value as UndertalePath;
            if (path == null)
                return null;

            List<UndertalePathEditor.LineData> target = new List<UndertalePathEditor.LineData>();

            for (int i = 0; i < path.Points.Count - 1; i++)
            {
                target.Add(new UndertalePathEditor.LineData()
                {
                    From = new Point(path.Points[i].X, path.Points[i].Y),
                    To = new Point(path.Points[i + 1].X, path.Points[i + 1].Y)
                });
            }
            if (path.IsClosed && path.Points.Count > 0)
            {
                target.Add(new UndertalePathEditor.LineData()
                {
                    From = new Point(path.Points[^1].X, path.Points[^1].Y),
                    To = new Point(path.Points[0].X, path.Points[0].Y)
                });
            }

            return target;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class PointConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Any(e => e == DependencyProperty.UnsetValue))
            {
                return null;
            }
            return new Point((float)values[0], (float)values[1]);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
    [ValueConversion(typeof(ObservableCollection<UndertalePath.PathPoint>), typeof(PointCollection))]
    public class PointListConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ObservableCollection<UndertalePath.PathPoint> coll = value as ObservableCollection<UndertalePath.PathPoint>;
            if (coll == null)
                return null;

            PointCollection outp = new PointCollection();
            foreach (var a in coll)
                outp.Add(new Point(a.X, a.Y));
            return outp;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // TODO: Finish fixing the path converter when I figure out how to do it https://stackoverflow.com/questions/52334480/binding-observablecollectionpoint-to-pathfigure

    [ValueConversion(typeof(UndertalePath), typeof(PathGeometry))]
    public class PointsDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            UndertalePath path = value as UndertalePath;
            if (path == null)
                return null;
            if (path.Points.Count == 0)
                return null;

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

            PathFigure target = new PathFigure();
            target.StartPoint = new Point(path.Points[0].X, path.Points[0].Y);
            target.Segments = new PathSegmentCollection(); 
            for (int i = 1; i < path.Points.Count; i++)
            {
                LineSegment segment = new LineSegment();
                segment.Point = new Point(path.Points[i].X, path.Points[i].Y);
                target.Segments.Add(segment);
            }
            target.IsClosed = path.IsClosed;
            target.Freeze();

            PathGeometry myPathGeometry = new PathGeometry();
            myPathGeometry.Figures = new PathFigureCollection();
            myPathGeometry.Figures.Add(target);
            myPathGeometry.Freeze();

            return myPathGeometry;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
