using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using static UndertaleModTool.Editors.UndertaleFontEditor.RectangleHelper;

namespace UndertaleModTool.Editors.UndertaleFontEditor
{
    /// <summary>
    /// Interaction logic for EditGlyphRectangleWindow.xaml
    /// </summary>
    public partial class EditGlyphRectangleWindow : Window, INotifyPropertyChanged
    {
        public UndertaleFont Font { get; set; }
        public UndertaleFont.Glyph[] Glyphs { get; set; }
        public UndertaleFont.Glyph SelectedGlyph { get; set; }
        private Rectangle selectedRect;
        
        private bool dragInProgress = false;
        private Point initPoint;
        private HitType initType;
        private Canvas canvas;

        public event PropertyChangedEventHandler PropertyChanged;

        public EditGlyphRectangleWindow(UndertaleFont font, UndertaleFont.Glyph selectedGlyph)
        {
            InitializeComponent();

            if (font is null || selectedGlyph is null)
            {
                Close();
                return;
            }

            Font = font;
            Glyphs = font.Glyphs.Select(x => x.Clone())
                                .ToArray();
            SelectedGlyph = Glyphs.FirstOrDefault(x => x.SourceX == selectedGlyph.SourceX
                                                       && x.SourceY == selectedGlyph.SourceY
                                                       && x.SourceWidth == selectedGlyph.SourceWidth
                                                       && x.SourceHeight == selectedGlyph.SourceHeight
                                                       && x.Character == selectedGlyph.Character
                                                       && x.Shift == selectedGlyph.Shift
                                                       && x.Offset == selectedGlyph.Offset);
            if (SelectedGlyph is null)
            {
                this.ShowError("Cannot find the selected glyph.");
                Close();
            }
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
            int textureWidth = Font.Texture?.BoundingWidth ?? 1;
            if (textureWidth < scrollPres.ActualWidth)
                initScale = scrollPres.ActualWidth / textureWidth;

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
            if (dragInProgress)
                return;

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

        private void Canvas_Loaded(object sender, RoutedEventArgs e)
        {
            canvas = sender as Canvas;
            UpdateSelectedRect();
        }

        private void UpdateSelectedRect()
        {
            if (canvas is null)
                return;

            canvas.UpdateLayout();
            foreach (var rect in MainWindow.FindVisualChildren<Rectangle>(canvas))
            {
                if (rect.DataContext == SelectedGlyph)
                {
                    selectedRect = rect;
                    return;
                }
            }
        }

        private void TextureViewbox_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is not Rectangle rect
                || rect.DataContext is not UndertaleFont.Glyph glyph)
                return;

            if (e.ClickCount >= 2)
            {
                SelectedGlyph = glyph;
                UpdateSelectedRect();
                return;
            }

            if (canvas is null)
                return;
            initPoint = e.GetPosition(canvas);
            initType = GetHitType(selectedRect, initPoint);
            dragInProgress = true;
        }
        private void TextureViewbox_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            dragInProgress = false;
        }
        private void TextureViewbox_MouseMove(object sender, MouseEventArgs e)
        {
            var pos = e.GetPosition(canvas);

            if (dragInProgress)
            {
                double offsetX = pos.X - initPoint.X;
                double offsetY = pos.Y - initPoint.Y;

                double newX = SelectedGlyph.SourceX;
                double newY = SelectedGlyph.SourceY;
                double newWidth = SelectedGlyph.SourceWidth;
                double newHeight = SelectedGlyph.SourceHeight;

                switch (initType)
                {
                    case HitType.Body:
                        newX += offsetX;
                        newY += offsetY;
                        break;
                    case HitType.UL:
                        newX += offsetX;
                        newY += offsetY;
                        newWidth -= offsetX;
                        newHeight -= offsetY;
                        break;
                    case HitType.UR:
                        newY += offsetY;
                        newWidth += offsetX;
                        newHeight -= offsetY;
                        break;
                    case HitType.LR:
                        newWidth += offsetX;
                        newHeight += offsetY;
                        break;
                    case HitType.LL:
                        newX += offsetX;
                        newWidth -= offsetX;
                        newHeight += offsetY;
                        break;
                    case HitType.L:
                        newX += offsetX;
                        newWidth -= offsetX;
                        break;
                    case HitType.R:
                        newWidth += offsetX;
                        break;
                    case HitType.B:
                        newHeight += offsetY;
                        break;
                    case HitType.T:
                        newY += offsetY;
                        newHeight -= offsetY;
                        break;
                }

                if (Math.Abs(offsetX) < 1 && Math.Abs(offsetY) < 1)
                    return;

                bool outOfLeft = newX < 0;
                bool outOfTop = newY < 0;
                bool outOfRight = newX + newWidth > Font.Texture.BoundingWidth;
                bool outOfBottom = newY + newHeight > Font.Texture.BoundingHeight;
                if (!outOfLeft && !outOfRight)
                    SelectedGlyph.SourceX = (ushort)Math.Round(newX);
                if (!outOfTop && !outOfBottom)
                    SelectedGlyph.SourceY = (ushort)Math.Round(newY);
                if (newWidth >= 0 && !outOfRight)
                    SelectedGlyph.SourceWidth = (ushort)Math.Round(newWidth);
                if (newHeight >= 0 && !outOfBottom)
                    SelectedGlyph.SourceHeight = (ushort)Math.Round(newHeight);

                if (outOfLeft)
                    SelectedGlyph.SourceX = 0;
                if (outOfRight)
                    SelectedGlyph.SourceX = (ushort)(Font.Texture.BoundingWidth - SelectedGlyph.SourceWidth);
                if (outOfTop)
                    SelectedGlyph.SourceY = 0;
                if (outOfBottom)
                    SelectedGlyph.SourceY = (ushort)(Font.Texture.BoundingHeight - SelectedGlyph.SourceHeight);

                initPoint.X = Math.Round(pos.X);
                initPoint.Y = Math.Round(pos.Y);
            }
            else
            {
                var hitType = GetHitType(selectedRect, pos);
                canvas.Cursor = GetCursorForType(hitType);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < Font.Glyphs.Count; i++)
                Font.Glyphs[i] = Glyphs[i];

            DialogResult = true;
            Close();
        }
    }


    internal class RectangleHelper
    {
        public enum HitType
        {
            None, Body, L, R, T, B, UL, UR, LR, LL
        };
        private const int rectEdgeWidth = 1;

        public static HitType GetHitType(Rectangle rect, Point point)
        {
            if (rect.DataContext is not UndertaleFont.Glyph glyph)
                return HitType.None;

            ushort left = glyph.SourceX;
            ushort top = glyph.SourceY;
            int right = left + glyph.SourceWidth;
            int bottom = top + glyph.SourceHeight;
            if (point.X < left || point.X > right
                || point.Y < top || point.Y > bottom)
                return HitType.None;

            if (point.X - left < rectEdgeWidth)
            {
                // Left edge.
                if (point.Y - top < rectEdgeWidth)
                    return HitType.UL;
                if (bottom - point.Y < rectEdgeWidth)
                    return HitType.LL;
                return HitType.L;
            }
            else if (right - point.X < rectEdgeWidth)
            {
                // Right edge.
                if (point.Y - top < rectEdgeWidth)
                    return HitType.UR;
                if (bottom - point.Y < rectEdgeWidth)
                    return HitType.LR;
                return HitType.R;
            }
            if (point.Y - top < rectEdgeWidth)
                return HitType.T;
            if (bottom - point.Y < rectEdgeWidth)
                return HitType.B;

            return HitType.Body;
        }

        public static Cursor GetCursorForType(HitType hitType)
        {
            return hitType switch
            {
                HitType.None => Cursors.Arrow,
                HitType.Body => Cursors.SizeAll,
                HitType.UL or HitType.LR => Cursors.SizeNWSE,
                HitType.LL or HitType.UR => Cursors.SizeNESW,
                HitType.T or HitType.B => Cursors.SizeNS,
                HitType.L or HitType.R => Cursors.SizeWE,
                _ => Cursors.Arrow
            };
        }
    }


}
