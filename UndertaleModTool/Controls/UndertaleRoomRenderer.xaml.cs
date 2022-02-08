using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
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
using System.Windows.Threading;
using UndertaleModLib.Models;

namespace UndertaleModTool
{
    /// <summary>
    /// Interaction logic for UndertaleRoomRenderer.xaml
    /// </summary>
    public partial class UndertaleRoomRenderer : DataUserControl
    {
        public static DependencyProperty PreviewPathProperty =
            DependencyProperty.Register("PreviewPath", typeof(UndertalePath),
                typeof(UndertaleRoomRenderer),
                new FrameworkPropertyMetadata(null));

        public static readonly PropertyInfo visualOffProp = typeof(Canvas).GetProperty("VisualOffset", BindingFlags.NonPublic | BindingFlags.Instance);

        public static DataTemplate RoomRendererTemplate { get; set; }

        public Canvas RoomCanvas { get; set; }
        public UndertalePath PreviewPath
        {
            get => (UndertalePath)GetValue(PreviewPathProperty);
            set => SetValue(PreviewPathProperty, value);
        }

        private bool bgGridDisabled;
        private Pen gridPen;
        private Brush initialGridBrush;

        public UndertaleRoomRenderer()
        {
            InitializeComponent();
        }

        private void RoomRenderer_Loaded(object sender, RoutedEventArgs e)
        {
            (DataContext as UndertaleRoom)?.SetupRoom();
        }
        private void RoomRenderer_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            (DataContext as UndertaleRoom)?.SetupRoom();
        }

        public void SaveImagePNG(Stream outfile, bool displayGrid = false, bool last = false)
        {
            Dispatcher.Invoke(DispatcherPriority.ContextIdle, (Action)(() =>
            {
                if (RoomCanvas is null)
                {
                    if (MainWindow.FindVisualChild<Canvas>(RoomGraphics) is Canvas canv && canv.Name == "RoomCanvas")
                        RoomCanvas = canv;
                    else
                        throw new Exception("\"RoomCanvas\" not found.");
                }

                object prevOffset = null;
                if (last)
                    prevOffset = visualOffProp.GetValue(RoomCanvas);

                visualOffProp.SetValue(RoomCanvas, new Vector(0, 0)); // (probably, there is a better way to fix the offset of the rendered picture)

                if (!displayGrid && !bgGridDisabled)
                {
                    if (gridPen is null)
                    {
                        gridPen = ((RoomCanvas.Background as DrawingBrush).Drawing as GeometryDrawing).Pen;
                        initialGridBrush = gridPen.Brush;
                    }

                    gridPen.Brush = null;
                    bgGridDisabled = true;
                }

                RenderTargetBitmap target = new((int)RoomCanvas.RenderSize.Width, (int)RoomCanvas.RenderSize.Height, 96, 96, PixelFormats.Pbgra32);

                target.Render(RoomCanvas);

                PngBitmapEncoder encoder = new() { Interlace = PngInterlaceOption.Off };
                encoder.Frames.Add(BitmapFrame.Create(target));
                encoder.Save(outfile);

                if (!displayGrid && last)
                {
                    visualOffProp.SetValue(RoomCanvas, prevOffset);
                    gridPen.Brush = initialGridBrush;
                }
            }));
        }
    }

    public class RoomCaptionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            UndertaleRoom room = value as UndertaleRoom;
            return room is not null ? $"{room.Name.Content}: {room.Width}x{room.Height}" : "null";
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
