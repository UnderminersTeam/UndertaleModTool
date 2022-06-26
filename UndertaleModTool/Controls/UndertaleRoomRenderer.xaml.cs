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

        private Canvas roomCanvas;
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

        private void RoomRenderer_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            (DataContext as UndertaleRoom)?.SetupRoom(!bgGridDisabled, !bgGridDisabled);
            UndertaleRoomEditor.GenerateSpriteCache(DataContext as UndertaleRoom);
        }

        private void RoomCanvas_Loaded(object sender, RoutedEventArgs e)
        {
            roomCanvas = sender as Canvas;
        }

        public void SaveImagePNG(Stream outfile, bool displayGrid = false, bool last = false)
        {
            Dispatcher.Invoke(DispatcherPriority.ContextIdle, (Action)(() =>
            {
                if (roomCanvas is null)
                {
                    if (MainWindow.FindVisualChild<Canvas>(RoomGraphics) is Canvas canv && canv.Name == "RoomCanvas")
                        roomCanvas = canv;
                    else
                        throw new Exception("\"RoomCanvas\" not found.");
                }

                object prevOffset = null;
                if (last)
                    prevOffset = visualOffProp.GetValue(roomCanvas);

                visualOffProp.SetValue(roomCanvas, new Vector(0, 0)); // (probably, there is a better way to fix the offset of the rendered picture)

                if (!displayGrid && !bgGridDisabled)
                {
                    if (gridPen is null)
                    {
                        gridPen = ((roomCanvas.Background as DrawingBrush).Drawing as GeometryDrawing).Pen;
                        initialGridBrush = gridPen.Brush;
                    }

                    gridPen.Brush = null;
                    bgGridDisabled = true;
                }

                RenderTargetBitmap target = new((int)roomCanvas.RenderSize.Width, (int)roomCanvas.RenderSize.Height, 96, 96, PixelFormats.Pbgra32);

                target.Render(roomCanvas);

                PngBitmapEncoder encoder = new() { Interlace = PngInterlaceOption.Off };
                encoder.Frames.Add(BitmapFrame.Create(target));
                encoder.Save(outfile);

                if (!displayGrid && last)
                {
                    visualOffProp.SetValue(roomCanvas, prevOffset);
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

    public class LayersOrderedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is IList<UndertaleRoom.Layer> layers)
                return layers.OrderByDescending(l => l.LayerDepth);
            else
                return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
