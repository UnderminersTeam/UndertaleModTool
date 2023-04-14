using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using System.Windows.Threading;
using UndertaleModLib.Models;

namespace UndertaleModTool.Editors.UndertaleFontEditor
{
    /// <summary>
    /// Interaction logic for EditGlyphRectangleWindow.xaml
    /// </summary>
    public partial class EditGlyphRectangleWindow : Window
    {
        public UndertaleFont.Glyph SelectedGlyph { get; set; }
        private readonly IList<UndertaleFont.Glyph> glyphs;

        public EditGlyphRectangleWindow(UndertaleFont font, UndertaleFont.Glyph selectedGlyph)
        {
            InitializeComponent();

            if (font is null || selectedGlyph is null)
            {
                Close();
                return;
            }

            DataContext = font;
            SelectedGlyph = selectedGlyph;
            glyphs = font.Glyphs;


        }

        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!IsVisible || IsLoaded)
                return;

            if (Settings.Instance.EnableDarkMode)
                MainWindow.SetDarkTitleBarForWindow(this, true, false);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var scrollPres = MainWindow.FindVisualChild<ScrollContentPresenter>(TextureScroll);
            if (scrollPres is null)
                return;

            double initScale = 1;
            if (DataContext is UndertaleFont font)
            {
                int textureWidth = font.Texture?.BoundingWidth ?? 1;
                if (textureWidth < scrollPres.ActualWidth)
                    initScale = scrollPres.ActualWidth / textureWidth;
            }

            TextureViewbox.LayoutTransform = new MatrixTransform(initScale, 0, 0, initScale, 0, 0); ;
            TextureViewbox.UpdateLayout();
            TextureScroll.ScrollToTop();
            TextureScroll.ScrollToLeftEnd();
        }

        private void TextureScroll_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.ExtentHeightChange != 0 || e.ExtentWidthChange != 0)
            {
                double xMousePositionOnScrollViewer = Mouse.GetPosition(TextureScroll).X;
                double yMousePositionOnScrollViewer = Mouse.GetPosition(TextureScroll).Y;
                double offsetX = e.HorizontalOffset + xMousePositionOnScrollViewer;
                double offsetY = e.VerticalOffset + yMousePositionOnScrollViewer;

                double oldExtentWidth = e.ExtentWidth - e.ExtentWidthChange;
                double oldExtentHeight = e.ExtentHeight - e.ExtentHeightChange;

                double relx = offsetX / oldExtentWidth;
                double rely = offsetY / oldExtentHeight;

                offsetX = Math.Max(relx * e.ExtentWidth - xMousePositionOnScrollViewer, 0);
                offsetY = Math.Max(rely * e.ExtentHeight - yMousePositionOnScrollViewer, 0);

                TextureScroll.ScrollToHorizontalOffset(offsetX);
                TextureScroll.ScrollToVerticalOffset(offsetY);
            }
        }

        private void TextureScroll_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;
            var mousePos = e.GetPosition(TextureScroll);
            var transform = TextureViewbox.LayoutTransform as MatrixTransform;
            var matrix = transform.Matrix;
            var scale = e.Delta >= 0 ? 1.1 : (1.0 / 1.1); // choose appropriate scaling factor

            if ((matrix.M11 > 0.2 || (matrix.M11 <= 0.2 && scale > 1)) && (matrix.M11 < 3 || (matrix.M11 >= 3 && scale < 1)))
            {
                matrix.ScaleAtPrepend(scale, scale, mousePos.X, mousePos.Y);
            }
            TextureViewbox.LayoutTransform = new MatrixTransform(matrix);
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount >= 2)
            {

            }
        }
        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {

        }
    }
}
