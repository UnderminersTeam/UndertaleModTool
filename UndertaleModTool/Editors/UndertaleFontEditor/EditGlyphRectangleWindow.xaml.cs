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

        private bool isNewCharacter;
        private bool dragInProgress = false;
        private Point initPoint;
        private HitType initType;
        private short initShift;
        private Canvas canvas;

#pragma warning disable CS0067 // Event is never used (this is actually used)
        public event PropertyChangedEventHandler PropertyChanged;
#pragma warning restore CS0067

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

            if (SelectedGlyph.SourceWidth == 0 || SelectedGlyph.SourceHeight == 0)
            {
                isNewCharacter = true;
                TextureViewbox.MouseMove += TextureViewbox_MouseMove_New;
                TextureViewbox.Cursor = Cursors.Cross;
            }
            else
                TextureViewbox.MouseMove += TextureViewbox_MouseMove;
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
            TextureScroll.Focus();
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

            if (isNewCharacter)
            {
                ToolTipService.SetIsEnabled(selectedRect, false);
                selectedRect.SetCurrentValue(WidthProperty, (double)1);
                selectedRect.SetCurrentValue(HeightProperty, (double)1);
            }
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
            if (!isNewCharacter)
            {
                if (e.OriginalSource is not Rectangle rect
                    || rect.DataContext is not UndertaleFont.Glyph glyph)
                    return;

                if (e.ClickCount >= 2)
                {
                    SelectedGlyph = glyph;
                    UpdateSelectedRect();

                    if (isNewCharacter)
                    {
                        isNewCharacter = false;
                        TextureViewbox.MouseMove -= TextureViewbox_MouseMove_New;
                        TextureViewbox.MouseMove += TextureViewbox_MouseMove;
                    }
                    return;
                }
            }

            initPoint = e.GetPosition(TextureViewbox);
            if (isNewCharacter)
            {
                SelectedGlyph.SourceX = (ushort)Math.Round(initPoint.X);
                SelectedGlyph.SourceY = (ushort)Math.Round(initPoint.Y);
                initShift = SelectedGlyph.Shift;
            }
            else
            {
                initType = GetHitType(selectedRect, initPoint);
                if (initType == HitType.T || initType == HitType.UL || initType == HitType.UR)
                    GlyphTopLine.Visibility = Visibility.Visible;
            }

            dragInProgress = true;
        }
        private void TextureViewbox_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isNewCharacter)
            {
                isNewCharacter = false;
                TextureViewbox.MouseMove -= TextureViewbox_MouseMove_New;
                TextureViewbox.MouseMove += TextureViewbox_MouseMove;
                TextureViewbox.Cursor = Cursors.Arrow;
                ToolTipService.SetIsEnabled(selectedRect, true);
            }
            else
                GlyphTopLine.Visibility = Visibility.Collapsed;

            dragInProgress = false;
        }
        private void TextureViewbox_MouseMove(object sender, MouseEventArgs e)
        {
            var pos = e.GetPosition(TextureViewbox);

            if (dragInProgress)
            {
                double offsetX = pos.X - initPoint.X;
                double offsetY = pos.Y - initPoint.Y;

                double newX = SelectedGlyph.SourceX;
                double newY = SelectedGlyph.SourceY;
                double newWidth = SelectedGlyph.SourceWidth;
                double newHeight = SelectedGlyph.SourceHeight;
                double newShift = SelectedGlyph.Shift;

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
                        newShift -= offsetX;
                        break;
                    case HitType.UR:
                        newY += offsetY;
                        newWidth += offsetX;
                        newHeight -= offsetY;
                        newShift += offsetX;
                        break;
                    case HitType.LR:
                        newWidth += offsetX;
                        newHeight += offsetY;
                        newShift += offsetX;
                        break;
                    case HitType.LL:
                        newX += offsetX;
                        newWidth -= offsetX;
                        newHeight += offsetY;
                        newShift -= offsetX;
                        break;
                    case HitType.L:
                        newX += offsetX;
                        newWidth -= offsetX;
                        newShift -= offsetX;
                        break;
                    case HitType.R:
                        newWidth += offsetX;
                        newShift += offsetX;
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
                if (newWidth > 0 && !outOfRight)
                    SelectedGlyph.SourceWidth = (ushort)Math.Round(newWidth);
                if (newHeight > 0 && !outOfBottom)
                    SelectedGlyph.SourceHeight = (ushort)Math.Round(newHeight);

                if (outOfLeft)
                    SelectedGlyph.SourceX = 0;
                if (outOfRight)
                    SelectedGlyph.SourceX = (ushort)(Font.Texture.BoundingWidth - SelectedGlyph.SourceWidth);
                if (outOfTop)
                    SelectedGlyph.SourceY = 0;
                if (outOfBottom)
                    SelectedGlyph.SourceY = (ushort)(Font.Texture.BoundingHeight - SelectedGlyph.SourceHeight);

                SelectedGlyph.Shift = (short)Math.Round(newShift);

                initPoint.X = Math.Round(pos.X);
                initPoint.Y = Math.Round(pos.Y);
            }
            else
            {
                var hitType = GetHitType(selectedRect, pos);
                TextureViewbox.Cursor = GetCursorForType(hitType);
            }
        }
        private void TextureViewbox_MouseMove_New(object sender, MouseEventArgs e)
        {
            var pos = e.GetPosition(TextureViewbox);

            if (dragInProgress)
            {
                double offsetX = pos.X - initPoint.X;
                double offsetY = pos.Y - initPoint.Y;

                if (offsetX < 1 || offsetY < 1)
                    return;

                bool outOfRight = SelectedGlyph.SourceX + offsetX > Font.Texture.BoundingWidth;
                bool outOfBottom = SelectedGlyph.SourceY + offsetY > Font.Texture.BoundingHeight;
                if (!outOfRight)
                    SelectedGlyph.SourceWidth = (ushort)Math.Round(offsetX);
                if (!outOfBottom)
                    SelectedGlyph.SourceHeight = (ushort)Math.Round(offsetY);

                SelectedGlyph.Shift = (short)(initShift + (short)Math.Round(offsetX));
            }
            else
            {
                SelectedGlyph.SourceX = (ushort)Math.Round(pos.X);
                SelectedGlyph.SourceY = (ushort)Math.Round(pos.Y);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < Font.Glyphs.Count; i++)
                Font.Glyphs[i] = Glyphs[i];

            DialogResult = true;
            Close();
        }

        private void TextureScroll_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (SelectedGlyph is null)
                return;

            if (e.Key == Key.LeftShift || e.Key == Key.RightShift)
                GlyphTopLine.Visibility = Visibility.Visible;

            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                switch (e.Key)
                {
                    case Key.Left:
                        if (SelectedGlyph.SourceWidth <= 1)
                            return;
                        SelectedGlyph.SourceWidth--;
                        break;

                    case Key.Right:
                        if (SelectedGlyph.SourceX + SelectedGlyph.SourceWidth >= Font.Texture.BoundingWidth)
                            return;
                        SelectedGlyph.SourceWidth++;
                        break;

                    case Key.Up:
                        if (SelectedGlyph.SourceHeight <= 1)
                            return;
                        SelectedGlyph.SourceHeight--;
                        break;

                    case Key.Down:
                        if (SelectedGlyph.SourceY + SelectedGlyph.SourceHeight >= Font.Texture.BoundingHeight)
                            return;
                        SelectedGlyph.SourceHeight++;
                        break;
                }

                return;
            }

            switch (e.Key)
            {
                case Key.Left:
                    if (SelectedGlyph.SourceX <= 0)
                        return;
                    SelectedGlyph.SourceX--;
                    break;

                case Key.Right:
                    if (SelectedGlyph.SourceX + SelectedGlyph.SourceWidth >= Font.Texture.BoundingWidth)
                        return;
                    SelectedGlyph.SourceX++;
                    break;

                case Key.Up:
                    if (SelectedGlyph.SourceY <= 0)
                        return;
                    SelectedGlyph.SourceY--;
                    break;

                case Key.Down:
                    if (SelectedGlyph.SourceY + SelectedGlyph.SourceHeight >= Font.Texture.BoundingHeight)
                        return;
                    SelectedGlyph.SourceY++;
                    break;
            }
        }
        private void TextureScroll_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftShift || e.Key == Key.RightShift)
                GlyphTopLine.Visibility = Visibility.Collapsed;
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            this.ShowMessage("1) Double-click an inactive rectangle to select it.\n" +
                             "2) You can move the selected rectangle with the arrow keys (held Shift - resize).\n" +
                             "3) Drag mouse on desired region if it's an empty glyph.", "Help");
        }
    }

    // Based on http://csharphelper.com/howtos/howto_wpf_resize_rectangle.html
    internal class RectangleHelper
    {
        public enum HitType
        {
            None, Body, L, R, T, B, UL, UR, LR, LL
        };
        private const int rectEdgeWidth = 1;

        public static HitType GetHitType(Rectangle rect, Point point)
        {
            if (rect?.DataContext is not UndertaleFont.Glyph glyph)
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
