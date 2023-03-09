using ICSharpCode.AvalonEdit.Editing;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;

namespace UndertaleModTool
{
    /// <summary>
    /// A standard context menu which compatible with the dark mode.
    /// </summary>
    public partial class ContextMenuDark : ContextMenu
    {
        private static readonly Brush separatorDarkBrush = new SolidColorBrush(Color.FromArgb(255, 60, 60, 60));
        private static readonly Rectangle[] rectList = new Rectangle[3];

        /// <summary>Initializes a new instance of the context menu.</summary>
        public ContextMenuDark()
        {
            Loaded += ContextMenuDark_Loaded;
        }

        private Rectangle[] GetSortedRectList(DependencyObject parent)
        {
            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            int rectCount = 0;
            for (int i = 0; i < childCount; i++)
            {
                if (rectCount > 3)
                    break;

                var rect = VisualTreeHelper.GetChild(parent, i) as Rectangle;
                if (rect is null)
                    continue;
                rectList[rectCount++] = rect;
            }
            if (rectCount != 3)
                return null;

            Array.Sort(rectList, (x, y) => x.Margin.Left.CompareTo(y.Margin.Left));
            return rectList;
        }
        private void ContextMenuDark_Loaded(object sender, RoutedEventArgs e)
        {
            if (Tag is int v)
            {
                // If the object is the same and the style is also the same
                // (0 - light mode, 1 - dark mode)
                if (v == 0 && !Settings.Instance.EnableDarkMode
                    || v == 1 && Settings.Instance.EnableDarkMode)
                    return;
            }

            Border border = MainWindow.FindVisualChild<Border>(this);
            if (border is null)
                return;

            if (Tag is null)
                border.SetResourceReference(BackgroundProperty, SystemColors.MenuBrushKey);

            var parent = MainWindow.FindVisualChild<ItemsPresenter>(border)?.Parent;
            if (parent is null)
                return;

            if (Settings.Instance.EnableDarkMode)
            {
                var rects = GetSortedRectList(parent);
                if (rects is null)
                    return;

                // Leave only the last rectangle on the right visible,
                // and also change its color
                for (int i = 0; i < 2; i++)
                {
                    var rect = rects[i];

                    rect.Tag = rect.Fill;
                    rect.Fill = Brushes.Transparent;
                }
                rects[2].Tag = rects[2].Fill;
                rects[2].Fill = separatorDarkBrush;

                Tag = 1;
            }
            else
            {
                var rects = GetSortedRectList(parent);
                if (rects is null)
                    return;

                if (rects[0].Tag is not null)
                {
                    // Restore rectangles state
                    for (int i = 0; i < 3; i++)
                    {
                        var rect = rects[i];

                        rect.Fill = rect.Tag as Brush;
                        rect.Tag = null;
                    }
                }

                Tag = 0;
            }
        }
    }
}
